using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/profile")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ProfileApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;
    private readonly IWebHostEnvironment _env;

    public ProfileApiController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private int? GetUserId()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return uid != null && int.TryParse(uid, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            user.AvatarUrl,
            role = user.Role?.RoleName,
            user.CreatedAt
        });
    }

    public record UpdateProfileRequest(string FullName, string? Phone);

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();
        if (request.Phone != null)
            user.Phone = request.Phone.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thông tin thành công." });
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ảnh." });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật avatar thành công.", avatarUrl = user.AvatarUrl });
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "Mật khẩu xác nhận không khớp." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }
}
