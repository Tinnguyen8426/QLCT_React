using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Controllers
{
    public class BookingController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public BookingController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] List<int>? serviceIds)
        {
            var services = await _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToListAsync();
            ViewBag.Services = services;
            ViewBag.SelectedServiceIds = (serviceIds ?? new List<int>()).Distinct().ToList();
            var staffList = await GetActiveStaffAsync();
            ViewData["StaffId"] = new SelectList(staffList, "UserId", "FullName");

            var (loggedInUser, loggedInCustomer) = await ResolveLoggedInCustomerAsync();
            PrepareBookingPrefill(loggedInUser, loggedInCustomer);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] List<int> serviceIds, int? staffId, DateTime date, TimeSpan time, string fullName, string phone, string? note)
        {
            var selectedServiceIds = serviceIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();

            if (!selectedServiceIds.Any() || date == default || time == default || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin bắt buộc và chọn ít nhất một dịch vụ.");
            }

            var availableServices = await _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToListAsync();
            var serviceLookup = availableServices.ToDictionary(s => s.ServiceId);
            if (selectedServiceIds.Any(id => !serviceLookup.ContainsKey(id)))
            {
                ModelState.AddModelError("", "Một hoặc nhiều dịch vụ không hợp lệ.");
            }

            var (loggedInUser, loggedInCustomer) = await ResolveLoggedInCustomerAsync();
            PrepareBookingPrefill(loggedInUser, loggedInCustomer);
            var isCustomerAccount = IsCustomerAccount(loggedInUser);

            if (!ModelState.IsValid)
            {
                ViewBag.Services = availableServices;
                ViewBag.SelectedServiceIds = selectedServiceIds;
                var staffList = await GetActiveStaffAsync();
                ViewData["StaffId"] = new SelectList(staffList, "UserId", "FullName", staffId);
                return View();
            }

            var normalizedFullName = fullName.Trim();
            var normalizedPhone = phone.Trim();

            // Tạo hoặc lấy khách hàng phù hợp
            var customer = loggedInCustomer;
            if (customer == null)
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == normalizedPhone);
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = normalizedFullName,
                    Phone = normalizedPhone,
                    Email = isCustomerAccount ? loggedInUser?.Email : null,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else if (isCustomerAccount)
            {
                var updated = false;
                if (!string.IsNullOrWhiteSpace(normalizedFullName) && !string.Equals(customer.FullName, normalizedFullName, StringComparison.Ordinal))
                {
                    customer.FullName = normalizedFullName;
                    updated = true;
                }
                if (!string.IsNullOrWhiteSpace(normalizedPhone) && !string.Equals(customer.Phone, normalizedPhone, StringComparison.Ordinal))
                {
                    customer.Phone = normalizedPhone;
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(customer.Email) && !string.IsNullOrWhiteSpace(loggedInUser?.Email))
                {
                    customer.Email = loggedInUser.Email;
                    updated = true;
                }

                if (updated)
                {
                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();
                }
            }

            var appointment = new Appointment
            {
                CustomerId = customer.CustomerId,
                StaffId = staffId,
                AppointmentDate = DateOnly.FromDateTime(date),
                AppointmentTime = TimeOnly.FromTimeSpan(time),
                Status = "Pending",
                Note = note,
                CreatedAt = DateTime.Now
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var selectedServices = selectedServiceIds.Select(id => serviceLookup[id]).ToList();
            foreach (var service in selectedServices)
            {
                _context.AppointmentDetails.Add(new AppointmentDetail
                {
                    AppointmentId = appointment.AppointmentId,
                    ServiceId = service.ServiceId,
                    Quantity = 1,
                    UnitPrice = service.Price,
                    Duration = service.Duration
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lịch thành công! Chúng tôi sẽ sớm xác nhận.";
            return RedirectToAction("Success", new { id = appointment.AppointmentId });
        }

        public async Task<IActionResult> Success(int id)
        {
            var apt = await _context.Appointments
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Staff)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (apt == null) return RedirectToAction("Index");
            return View(apt);
        }

        private async Task<(User? user, Customer? customer)> ResolveLoggedInCustomerAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return (null, null);
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive == true);

            if (user == null)
            {
                return (null, null);
            }

            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == user.Phone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var normalizedEmail = user.Email.Trim().ToLowerInvariant();
                customer = await _context.Customers.FirstOrDefaultAsync(c =>
                    c.Email != null && c.Email.ToLower() == normalizedEmail);
            }

            if (customer == null && IsCustomerAccount(user) && !string.IsNullOrWhiteSpace(user.Phone))
            {
                customer = new Customer
                {
                    FullName = user.FullName,
                    Phone = user.Phone.Trim(),
                    Email = user.Email,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            return (user, customer);
        }

        private static bool IsCustomerAccount(User? user)
        {
            if (user?.Role == null) return false;
            var roleName = user.Role.RoleName;
            return !string.IsNullOrWhiteSpace(roleName) &&
                   (roleName.Equals("Customer", StringComparison.OrdinalIgnoreCase)
                    || roleName.Equals("KhachHang", StringComparison.OrdinalIgnoreCase)
                    || roleName.Equals("User", StringComparison.OrdinalIgnoreCase));
        }

        private void PrepareBookingPrefill(User? loggedInUser, Customer? loggedInCustomer)
        {
            if (IsCustomerAccount(loggedInUser))
            {
                var prefill = new Dictionary<string, string>
                {
                    ["fullName"] = !string.IsNullOrWhiteSpace(loggedInCustomer?.FullName)
                        ? loggedInCustomer!.FullName
                        : loggedInUser!.FullName,
                    ["phone"] = loggedInCustomer?.Phone ?? loggedInUser?.Phone ?? string.Empty
                };
                ViewBag.BookingPrefill = prefill;
                ViewBag.IsCustomerLoggedIn = true;
            }
            else
            {
                ViewBag.BookingPrefill = new Dictionary<string, string>();
                ViewBag.IsCustomerLoggedIn = false;
            }
        }

        private Task<List<User>> GetActiveStaffAsync()
        {
            return _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive == true
                            && u.Role != null
                            && (u.Role.RoleName == "Staff"
                                || u.Role.RoleName == "NhanVien"
                                || u.Role.RoleName == "Nhân viên"))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
    }
}



