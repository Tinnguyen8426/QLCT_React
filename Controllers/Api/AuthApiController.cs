using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyTiemCatToc.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;
    private readonly IConfiguration _configuration;

    public AuthApiController(QuanLyTiemCatTocContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FullName, string Email, string? Phone, string Password, string ConfirmPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Vui lòng nhập đầy đủ email và mật khẩu." });

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        if (user.Role == null)
            return Unauthorized(new { message = "Tài khoản không có quyền truy cập." });

        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Phone,
                user.AvatarUrl,
                role = user.Role.RoleName
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.FullName))
            errors["fullName"] = "Vui lòng nhập họ tên.";

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = "Vui lòng nhập email.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors["email"] = "Email không hợp lệ.";
        else if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            errors["email"] = "Email này đã được sử dụng.";

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = "Vui lòng nhập mật khẩu.";
        else if (request.Password.Length < 6)
            errors["password"] = "Mật khẩu phải có ít nhất 6 ký tự.";

        if (request.Password != request.ConfirmPassword)
            errors["confirmPassword"] = "Mật khẩu xác nhận không khớp.";

        // Kiểm tra admin đã tồn tại chưa
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
        var hasAdmin = adminRole != null && await _context.Users.AnyAsync(u => u.RoleId == adminRole.RoleId && u.IsActive == true);

        if (hasAdmin && string.IsNullOrWhiteSpace(request.Phone))
            errors["phone"] = "Số điện thoại là bắt buộc.";

        if (!string.IsNullOrWhiteSpace(request.Phone) && await _context.Customers.AnyAsync(c => c.Phone == request.Phone.Trim()))
            errors["phone"] = "Số điện thoại này đã được sử dụng.";

        if (errors.Count > 0)
            return BadRequest(new { errors });

        // Tạo roles nếu chưa có
        if (adminRole == null)
        {
            adminRole = new Role { RoleName = "Admin" };
            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync();
        }
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer" || r.RoleName == "KhachHang" || r.RoleName == "User");
        if (customerRole == null)
        {
            customerRole = new Role { RoleName = "Customer" };
            _context.Roles.Add(customerRole);
            await _context.SaveChangesAsync();
        }
        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff" || r.RoleName == "NhanVien");
        if (staffRole == null)
        {
            staffRole = new Role { RoleName = "Staff" };
            _context.Roles.Add(staffRole);
            await _context.SaveChangesAsync();
        }

        hasAdmin = await _context.Users.AnyAsync(u => u.RoleId == adminRole.RoleId && u.IsActive == true);
        var assignedRoleId = hasAdmin ? customerRole.RoleId : adminRole.RoleId;

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = !string.IsNullOrWhiteSpace(request.Phone) ? request.Phone.Trim() : null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = assignedRoleId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Nếu là Customer, tạo record trong bảng Customer
        if (assignedRoleId == customerRole.RoleId && !string.IsNullOrWhiteSpace(request.Phone))
        {
            var customerPhone = request.Phone.Trim();
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == customerPhone);
            if (existing == null)
            {
                _context.Customers.Add(new Customer
                {
                    FullName = request.FullName.Trim(),
                    Phone = customerPhone,
                    Email = request.Email.Trim().ToLowerInvariant(),
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Đăng ký thành công!" });
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || !int.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return Unauthorized();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            user.AvatarUrl,
            role = user.Role?.RoleName
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // JWT is stateless — client removes token
        return Ok(new { message = "Đăng xuất thành công." });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "CHANGE_ME_SUPER_SECRET_KEY_32BYTES_LONG";
        var issuer = _configuration["Jwt:Issuer"] ?? "QuanLyTiemCatToc";
        var audience = _configuration["Jwt:Audience"] ?? "QuanLyTiemCatTocMobile";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
