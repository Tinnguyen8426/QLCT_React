using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public AppointmentsController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var phone = await GetCurrentUserPhoneAsync();
            ViewBag.Phone = phone;
            ViewBag.IsLockedPhone = !string.IsNullOrWhiteSpace(phone);

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var appointments = await LoadAppointmentsByPhoneAsync(phone);
                return View(appointments);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyAppointments(string phone)
        {
            var currentPhone = await GetCurrentUserPhoneAsync();
            var isLocked = !string.IsNullOrWhiteSpace(currentPhone);
            var lookupPhone = isLocked ? currentPhone : phone?.Trim();

            if (string.IsNullOrWhiteSpace(lookupPhone))
            {
                ModelState.AddModelError("", "Vui lòng nhập số điện thoại.");
                return View();
            }

            if (!isLocked)
            {
                var existingUser = await _context.Users.AnyAsync(u => u.IsActive == true && u.Phone == lookupPhone);
                if (existingUser)
                {
                    TempData["ErrorMessage"] = "Số điện thoại này đã liên kết tài khoản. Vui lòng đăng nhập để xem thông tin lịch hẹn.";
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyAppointments", "Appointments") });
                }
            }

            var appointments = await LoadAppointmentsByPhoneAsync(lookupPhone);
            ViewBag.Phone = lookupPhone;
            ViewBag.IsLockedPhone = isLocked;
            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var apt = await _context.Appointments.FindAsync(id);
            if (apt != null && apt.Status != "Completed" && apt.Status != "Cancelled")
            {
                apt.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy lịch hẹn.";
            }
            return RedirectToAction("MyAppointments");
        }

        private async Task<string?> GetCurrentUserPhoneAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Phone;
        }

        private async Task<List<Appointment>> LoadAppointmentsByPhoneAsync(string phone)
        {
            return await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(x => x.Service)
                .Include(a => a.Staff)
                .Where(a => a.Customer.Phone == phone)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }
    }
}
