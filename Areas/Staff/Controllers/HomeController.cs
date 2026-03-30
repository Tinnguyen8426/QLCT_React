using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff")]
    public class HomeController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public HomeController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveMenu = "Home";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var staffId))
            {
                return RedirectToAction("Login", "Account", new { area = string.Empty });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(ad => ad.Service)
                .Where(a => a.StaffId == staffId)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var todaysAppointments = appointments
                .Where(a => a.AppointmentDate == today)
                .ToList();

            var upcomingAppointments = appointments
                .Where(a => a.AppointmentDate > today || (a.AppointmentDate == today && a.AppointmentTime >= now))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(5)
                .ToList();

            var recentCompleted = appointments
                .Where(a => string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Take(4)
                .ToList();

            var statusCounts = todaysAppointments
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Status) ? "Pending" : a.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new StaffDashboardViewModel
            {
                TotalToday = todaysAppointments.Count,
                ConfirmedToday = todaysAppointments.Count(a => string.Equals(a.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)),
                CompletedToday = todaysAppointments.Count(a => string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
                PendingToday = todaysAppointments.Count(a =>
                    string.IsNullOrWhiteSpace(a.Status) || string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                UpcomingAppointments = upcomingAppointments,
                RecentCompletedAppointments = recentCompleted,
                StatusCounts = statusCounts,
                NextAppointment = upcomingAppointments.FirstOrDefault()
            };

            return View(viewModel);
        }
    }
}
