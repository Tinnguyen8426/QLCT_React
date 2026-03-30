using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.Api;

namespace QuanLyTiemCatToc.Controllers.Api;

[Route("api/staff/dashboard")]
public class StaffDashboardController : StaffApiControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public StaffDashboardController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var staffId = GetStaffId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được nhân viên hiện tại." });
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

        var nextAppointment = upcomingAppointments.FirstOrDefault();
        var dashboard = new StaffDashboardDto
        {
            TotalToday = todaysAppointments.Count,
            ConfirmedToday = todaysAppointments.Count(a => string.Equals(a.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)),
            CompletedToday = todaysAppointments.Count(a => string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            PendingToday = todaysAppointments.Count(a =>
                string.IsNullOrWhiteSpace(a.Status) || string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            UpcomingAppointments = upcomingAppointments.Select(StaffApiMapper.ToSummaryDto).ToList(),
            RecentCompletedAppointments = recentCompleted.Select(StaffApiMapper.ToSummaryDto).ToList(),
            StatusCounts = statusCounts,
            NextAppointment = nextAppointment == null ? null : StaffApiMapper.ToSummaryDto(nextAppointment)
        };

        return Ok(dashboard);
    }
}
