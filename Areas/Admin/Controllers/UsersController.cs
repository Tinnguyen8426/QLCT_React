using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Users";
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(int? roleId, bool? isActive, int page = 1, int pageSize = 10)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Shift)
                .AsQueryable();

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
                ViewBag.SelectedRoleId = roleId.Value;
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
                ViewBag.SelectedIsActive = isActive.Value;
            }

            var users = await query.OrderBy(u => u.FullName).ToPagedListAsync(page, pageSize);
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Shift)
                .Include(u => u.Appointments)
                .Include(u => u.Invoices)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null) return NotFound();

            var thisMonth = DateTime.Today.Month;
            var thisYear = DateTime.Today.Year;
            
            ViewBag.TotalAppointments = user.Appointments.Count;
            ViewBag.TotalRevenue = user.Invoices.Sum(i => i.FinalAmount ?? 0);

            return View(user);
        }

        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewData["ShiftId"] = new SelectList(_context.Shifts, "ShiftId", "ShiftName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Phone,PasswordHash,RoleId,ShiftId,CommissionPercent,IsActive")] User user, IFormFile? AvatarFile)
        {
            user.Email = user.Email?.Trim();
            user.Phone = string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone!.Trim();
            ModelState.Remove("Role");
            ModelState.Remove("Shift");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "Vui lòng nhập mật khẩu.");
            }

            if (!string.IsNullOrWhiteSpace(user.Email) && await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            }

            if (!string.IsNullOrWhiteSpace(user.Phone) && await _context.Users.AnyAsync(u => u.Phone == user.Phone))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(AvatarFile.FileName);
                        var uploadPath = Path.Combine(_env.WebRootPath, "images", "avatars");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                        
                        using var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create);
                        await AvatarFile.CopyToAsync(stream);
                        user.AvatarUrl = "/images/avatars/" + fileName;
                    }

                    user.CreatedAt = DateTime.Now;
                    user.IsActive ??= true;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash!);

                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Không thể lưu nhân viên. Vui lòng thử lại.");
                }
            }

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            ViewData["ShiftId"] = new SelectList(_context.Shifts, "ShiftId", "ShiftName", user.ShiftId);
            return View(user);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            ViewData["ShiftId"] = new SelectList(_context.Shifts, "ShiftId", "ShiftName", user.ShiftId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Email,Phone,RoleId,ShiftId,AvatarUrl,CommissionPercent,CreatedAt,IsActive")] User user, string? NewPassword, IFormFile? AvatarFile)
        {
            if (id != user.UserId) return NotFound();

            user.Email = user.Email?.Trim();
            user.Phone = string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone!.Trim();
            ModelState.Remove("Role");
            ModelState.Remove("Shift");
            ModelState.Remove("PasswordHash");

            if (!string.IsNullOrWhiteSpace(user.Email) && await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserId != id))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            }

            if (!string.IsNullOrWhiteSpace(user.Phone) && await _context.Users.AnyAsync(u => u.Phone == user.Phone && u.UserId != id))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
                    
                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(AvatarFile.FileName);
                        var uploadPath = Path.Combine(_env.WebRootPath, "images", "avatars");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                        
                        using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await AvatarFile.CopyToAsync(stream);
                        }
                        user.AvatarUrl = "/images/avatars/" + fileName;
                    }

                    if (string.IsNullOrEmpty(NewPassword))
                    {
                        user.PasswordHash = existingUser!.PasswordHash;
                    }
                    else
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.UserId == id)) return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Không thể cập nhật nhân viên. Vui lòng thử lại.");
                }
                if (ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            ViewData["ShiftId"] = new SelectList(_context.Shifts, "ShiftId", "ShiftName", user.ShiftId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = user.IsActive });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
