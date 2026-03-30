using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/feedback")]
public class FeedbackApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public FeedbackApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        var query = _context.Feedbacks
            .Include(f => f.Customer)
            .Include(f => f.Service)
            .Include(f => f.Staff)
            .OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);

        var feedbacks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.FeedbackId,
                customerName = f.Customer.FullName,
                serviceName = f.Service != null ? f.Service.ServiceName : null,
                staffName = f.Staff != null ? f.Staff.FullName : null,
                f.Rating,
                f.Comment,
                createdAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(new { feedbacks, page, pageSize, total, totalPages });
    }

    public record CreateFeedbackRequest(int? ServiceId, int? StaffId, int Rating, string? Comment);

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || !int.TryParse(userId, out var uid))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        // Find customer by email from user
        var user = await _context.Users.FindAsync(uid);
        if (user == null) return Unauthorized();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
        if (customer == null)
            return BadRequest(new { message = "Không tìm thấy thông tin khách hàng." });

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating phải từ 1 đến 5." });

        var feedback = new Feedback
        {
            CustomerId = customer.CustomerId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đánh giá thành công!", feedbackId = feedback.FeedbackId });
    }
    [HttpGet("completed-appointments")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetCompletedAppointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || !int.TryParse(userId, out var uid))
            return Unauthorized();

        var user = await _context.Users.FindAsync(uid);
        if (user == null) return Unauthorized();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
        if (customer == null) return Ok(new List<object>());

        var appointments = await _context.Appointments
            .Include(a => a.Staff)
            .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
            .Where(a => a.CustomerId == customer.CustomerId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .Take(20)
            .Select(a => new
            {
                a.AppointmentId,
                Date = a.AppointmentDate.ToString("dd/MM/yyyy"),
                Time = a.AppointmentTime.ToString("HH:mm"),
                StaffId = a.StaffId,
                StaffName = a.Staff != null ? a.Staff.FullName : "Không có",
                ServiceId = a.AppointmentDetails.FirstOrDefault() != null ? (int?)a.AppointmentDetails.FirstOrDefault()!.ServiceId : null,
                ServiceName = a.AppointmentDetails.FirstOrDefault() != null && a.AppointmentDetails.FirstOrDefault()!.Service != null 
                    ? a.AppointmentDetails.FirstOrDefault()!.Service!.ServiceName 
                    : "Không xác định",
            })
            .ToListAsync();

        return Ok(appointments);
    }
}
