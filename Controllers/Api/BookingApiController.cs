using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/booking")]
public class BookingApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public BookingApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    public record BookingRequest(
        string FullName,
        string Phone,
        string Date,
        string Time,
        int[] ServiceIds,
        int? StaffId,
        string? Note);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ tên." });
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại." });
        if (request.ServiceIds == null || request.ServiceIds.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ít nhất một dịch vụ." });

        if (!DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new { message = "Ngày không hợp lệ." });
        if (!TimeOnly.TryParse(request.Time, out var time))
            return BadRequest(new { message = "Giờ không hợp lệ." });

        // Validate services exist
        var services = await _context.Services
            .Where(s => request.ServiceIds.Contains(s.ServiceId) && s.IsActive != false)
            .ToListAsync();

        if (services.Count == 0)
            return BadRequest(new { message = "Không tìm thấy dịch vụ hợp lệ." });

        // Find or create customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == request.Phone.Trim());

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Phone = request.Phone.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        // Assign staff
        int? staffId = request.StaffId;
        if (!staffId.HasValue)
        {
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole != null)
            {
                var staff = await _context.Users
                    .Where(u => u.RoleId == staffRole.RoleId && u.IsActive == true)
                    .OrderBy(_ => Guid.NewGuid())
                    .FirstOrDefaultAsync();
                staffId = staff?.UserId;
            }
        }

        var appointment = new Appointment
        {
            CustomerId = customer.CustomerId,
            StaffId = staffId,
            AppointmentDate = date,
            AppointmentTime = time,
            Status = "Pending",
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Add appointment details
        foreach (var svc in services)
        {
            _context.AppointmentDetails.Add(new AppointmentDetail
            {
                AppointmentId = appointment.AppointmentId,
                ServiceId = svc.ServiceId,
                UnitPrice = svc.Price,
                Quantity = 1
            });
        }
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đặt lịch thành công!",
            appointmentId = appointment.AppointmentId
        });
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffList()
    {
        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
        if (staffRole == null) return Ok(new List<object>());

        var staff = await _context.Users
            .Where(u => u.RoleId == staffRole.RoleId && u.IsActive == true)
            .Select(u => new { u.UserId, u.FullName, u.AvatarUrl })
            .ToListAsync();

        return Ok(staff);
    }
}
