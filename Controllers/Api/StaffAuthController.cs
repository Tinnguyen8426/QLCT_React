using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.Api;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/staff/auth")]
public class StaffAuthController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;
    private readonly IConfiguration _configuration;

    public StaffAuthController(QuanLyTiemCatTocContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] StaffLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email và mật khẩu là bắt buộc." });
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true);

        if (user == null || user.Role == null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        if (!user.Role.RoleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            return StatusCode(500, new { message = "Thiếu cấu hình Jwt:Key." });
        }
        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            return StatusCode(500, new { message = "Jwt:Key phải tối thiểu 32 bytes cho HS256." });
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "QuanLyTiemCatToc";
        var audience = _configuration["Jwt:Audience"] ?? "QuanLyTiemCatTocMobile";
        var expiresMinutes = _configuration.GetValue<int?>("Jwt:ExpiresMinutes") ?? 60;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        var response = new StaffLoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = token.ValidTo,
            Staff = new StaffProfileDto
            {
                StaffId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            }
        };

        return Ok(response);
    }
}
