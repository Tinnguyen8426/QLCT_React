using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace QuanLyTiemCatToc.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                RoleName = user.Role?.RoleName,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            // Kiểm tra email trùng (khác chính mình)
            var emailExists = await _context.Users
                .AnyAsync(u => u.UserId != user.UserId && u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            // Upload avatar nếu có file
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"avatar_{user.UserId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(avatarFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                var relativePath = $"/uploads/avatars/{fileName}";
                user.AvatarUrl = relativePath;
                model.AvatarUrl = relativePath;
            }
            else
            {
                user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword.Length < 6)
                {
                    ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu cần ít nhất 6 ký tự.");
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }
    }
}

