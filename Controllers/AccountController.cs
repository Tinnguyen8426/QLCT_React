using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;
using System;

namespace QuanLyTiemCatToc.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public AccountController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ email và mật khẩu.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive == true);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                if (user.Role == null)
                {
                    ModelState.AddModelError("", "Tài khoản không có quyền truy cập. Vui lòng liên hệ quản trị viên.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.RoleName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                var roleName = user.Role.RoleName;

                if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                else if (roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home", new { area = "Staff" });
                }
                else if (roleName.Equals("Customer", StringComparison.OrdinalIgnoreCase) || roleName.Equals("KhachHang", StringComparison.OrdinalIgnoreCase) || roleName.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản với email này.");
                return View();
            }

            TempData["InfoMessage"] = "Chức năng đặt lại mật khẩu đang được phát triển. Vui lòng liên hệ quản trị viên.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Cho phép đăng ký luôn; lần đầu sẽ tạo Admin, các lần sau là Khách hàng
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
            var hasAdmin = false;
            if (adminRole != null)
            {
                hasAdmin = await _context.Users.AnyAsync(u => u.RoleId == adminRole.RoleId && u.IsActive == true);
            }
            ViewBag.HasAdmin = hasAdmin;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string phone, string password, string confirmPassword)
        {
            // Preserve form values for display on error
            ViewData["fullName"] = fullName;
            ViewData["email"] = email;
            ViewData["phone"] = phone;

            // Validation
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError("fullName", "Vui lòng nhập họ tên.");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("email", "Vui lòng nhập email.");
                hasErrors = true;
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("email", "Email không hợp lệ.");
                hasErrors = true;
            }
            else if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("email", "Email này đã được sử dụng.");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("password", "Vui lòng nhập mật khẩu.");
                hasErrors = true;
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError("password", "Mật khẩu phải có ít nhất 6 ký tự.");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu.");
                hasErrors = true;
            }
            else if (!string.IsNullOrWhiteSpace(password) && password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
                hasErrors = true;
            }

            // Kiểm tra xem sẽ tạo role gì để validate phone
            var adminRoleCheck = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
            var hasAdminCheck = false;
            if (adminRoleCheck != null)
            {
                hasAdminCheck = await _context.Users.AnyAsync(u => u.RoleId == adminRoleCheck.RoleId && u.IsActive == true);
            }
            
            // Nếu sẽ tạo Customer (không phải admin đầu tiên), phone là bắt buộc
            if (hasAdminCheck && string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError("phone", "Số điện thoại là bắt buộc khi đăng ký tài khoản khách hàng.");
                hasErrors = true;
            }

            // Kiểm tra phone đã được sử dụng trong Customer chưa (nếu có phone)
            if (!string.IsNullOrWhiteSpace(phone) && await _context.Customers.AnyAsync(c => c.Phone == phone.Trim()))
            {
                ModelState.AddModelError("phone", "Số điện thoại này đã được sử dụng.");
                hasErrors = true;
            }

            if (hasErrors || !ModelState.IsValid)
            {
                ViewBag.HasAdmin = hasAdminCheck;
                return View();
            }

            try
            {
                // Lấy hoặc tạo các role cần thiết
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
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

                // Quyết định cấp quyền: nếu chưa có admin thì tạo admin, ngược lại là khách hàng
                var hasAdmin = await _context.Users.AnyAsync(u => u.RoleId == adminRole.RoleId && u.IsActive == true);
                var assignedRoleId = hasAdmin ? customerRole.RoleId : adminRole.RoleId;

                // Kiểm tra lại email trước khi tạo (tránh race condition)
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    ModelState.AddModelError("email", "Email này đã được sử dụng.");
                    ViewBag.HasAdmin = hasAdminCheck;
                    return View();
                }

                // Tạo user mới với role phù hợp
                var user = new User
                {
                    FullName = fullName.Trim(),
                    Email = email.Trim().ToLowerInvariant(),
                    Phone = !string.IsNullOrWhiteSpace(phone) ? phone.Trim() : null,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = assignedRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Nếu là Customer, tạo record trong bảng Customer
                if (assignedRoleId == customerRole.RoleId)
                {
                    // Đảm bảo phone không null (đã validate ở trên)
                    var customerPhone = !string.IsNullOrWhiteSpace(phone) ? phone.Trim() : null;
                    
                    if (customerPhone != null)
                    {
                        // Kiểm tra Customer đã tồn tại chưa (theo phone hoặc email)
                        var existingCustomer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.Phone == customerPhone ||
                                                       (c.Email != null && c.Email == email.Trim().ToLowerInvariant()));
                        
                        if (existingCustomer == null)
                        {
                            // Tạo Customer mới
                            var customer = new Customer
                            {
                                FullName = fullName.Trim(),
                                Phone = customerPhone,
                                Email = email.Trim().ToLowerInvariant(),
                                CreatedAt = DateTime.Now
                            };
                            _context.Customers.Add(customer);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Nếu đã tồn tại Customer nhưng chưa có email, cập nhật email
                            if (string.IsNullOrWhiteSpace(existingCustomer.Email))
                            {
                                existingCustomer.Email = email.Trim().ToLowerInvariant();
                                _context.Customers.Update(existingCustomer);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                TempData["SuccessMessage"] = hasAdmin
                    ? "Đăng ký tài khoản khách hàng thành công! Vui lòng đăng nhập."
                    : "Đăng ký tài khoản admin đầu tiên thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi khi đăng ký: {ex.Message}");
                // Reload admin check for view
                var adminRoleCheckException = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
                var hasAdminCheckException = false;
                if (adminRoleCheckException != null)
                {
                    hasAdminCheckException = await _context.Users.AnyAsync(u => u.RoleId == adminRoleCheckException.RoleId && u.IsActive == true);
                }
                ViewBag.HasAdmin = hasAdminCheckException;
                return View();
            }
        }
    }
}
