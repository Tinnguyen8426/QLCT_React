using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.Api;

namespace QuanLyTiemCatToc.Controllers.Api;

[Route("api/staff/appointments")]
public class StaffAppointmentsController : StaffApiControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    private static readonly string[] AllowedStatuses = new[]
    {
        "Pending",
        "Confirmed",
        "Completed",
        "Cancelled",
        "No-show"
    };

    private static readonly Dictionary<string, string> StatusAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pending"] = "Pending",
        ["chờ xác nhận"] = "Pending",
        ["cho xac nhan"] = "Pending",
        ["confirmed"] = "Confirmed",
        ["đã xác nhận"] = "Confirmed",
        ["da xac nhan"] = "Confirmed",
        ["completed"] = "Completed",
        ["hoàn thành"] = "Completed",
        ["hoan thanh"] = "Completed",
        ["cancelled"] = "Cancelled",
        ["canceled"] = "Cancelled",
        ["đã hủy"] = "Cancelled",
        ["da huy"] = "Cancelled",
        ["no-show"] = "No-show",
        ["no show"] = "No-show",
        ["vắng mặt"] = "No-show",
        ["vang mat"] = "No-show"
    };

    private static readonly Dictionary<string, string[]> StatusFilterValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pending"] = new[] { "Pending", "Chờ xác nhận", string.Empty },
        ["Confirmed"] = new[] { "Confirmed", "Đã xác nhận" },
        ["Completed"] = new[] { "Completed", "Hoàn thành" },
        ["Cancelled"] = new[] { "Cancelled", "Canceled", "Đã hủy" },
        ["No-show"] = new[] { "No-show", "No show", "Vắng mặt" }
    };

    private static readonly IReadOnlyList<string> PaymentMethods = new[]
    {
        "Tiền mặt",
        "Chuyển khoản",
        "Thẻ"
    };

    public StaffAppointmentsController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] string? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var staffId = GetStaffId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được nhân viên hiện tại." });
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.AppointmentDetails)
                .ThenInclude(ad => ad.Service)
            .Include(a => a.Invoices)
            .Where(a => a.StaffId == staffId);

        var overviewData = await baseQuery
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Status) ? "Pending" : a.Status!)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var filteredQuery = baseQuery;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();
            filteredQuery = filteredQuery.Where(a =>
                EF.Functions.Like(a.Customer.FullName, $"%{keyword}%") ||
                EF.Functions.Like(a.Customer.Phone, $"%{keyword}%") ||
                a.AppointmentDetails.Any(ad =>
                    ad.Service != null && EF.Functions.Like(ad.Service.ServiceName, $"%{keyword}%")));
        }

        var normalizedFilterStatus = NormalizeStatusFilter(status);
        if (!string.IsNullOrWhiteSpace(normalizedFilterStatus))
        {
            if (StatusFilterValues.TryGetValue(normalizedFilterStatus, out var acceptedValues))
            {
                var acceptedValuesNormalized = acceptedValues
                    .Select(v => (v ?? string.Empty).Trim().ToLower())
                    .ToArray();

                filteredQuery = filteredQuery.Where(a =>
                    acceptedValuesNormalized.Contains(((a.Status ?? string.Empty).Trim().ToLower())));
            }
            else
            {
                filteredQuery = filteredQuery.Where(a =>
                    string.Equals(a.Status, normalizedFilterStatus, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(date))
        {
            if (DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                filteredQuery = filteredQuery.Where(a => a.AppointmentDate == parsedDate);
            }
        }

        var totalItems = await filteredQuery.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page > totalPages) page = totalPages;

        var appointments = await filteredQuery
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var overview = AllowedStatuses.ToDictionary(s => s, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var item in overviewData)
        {
            overview[item.Status] = item.Count;
        }

        var response = new StaffAppointmentListResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            StatusOverview = overview,
            Items = appointments.Select(StaffApiMapper.ToSummaryDto).ToList()
        };

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAppointmentDetail(int id)
    {
        var staffId = GetStaffId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được nhân viên hiện tại." });
        }

        var appointment = await LoadAppointmentAsync(id, staffId.Value);
        if (appointment == null)
        {
            return NotFound(new { message = "Không tìm thấy lịch hẹn." });
        }

        return Ok(StaffApiMapper.ToDetailDto(appointment));
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var staffId = GetStaffId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được nhân viên hiện tại." });
        }

        var targetStatus = NormalizeStatusFilter(request.Status) ?? "Pending";

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == id && a.StaffId == staffId);

        if (appointment == null)
        {
            return NotFound(new { message = "Không tìm thấy lịch hẹn." });
        }

        appointment.Status = targetStatus;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã cập nhật trạng thái lịch hẹn #{id}." });
    }

    [HttpPost("{id:int}/invoice")]
    public async Task<IActionResult> CreateInvoice(int id, [FromBody] CreateInvoiceRequest request)
    {
        var staffId = GetStaffId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "Không xác định được nhân viên hiện tại." });
        }

        var appointment = await LoadAppointmentAsync(id, staffId.Value);
        if (appointment == null)
        {
            return NotFound(new { message = "Không tìm thấy lịch hẹn." });
        }

        if (!appointment.AppointmentDetails.Any())
        {
            return BadRequest(new { message = "Lịch hẹn chưa có dịch vụ." });
        }

        if (appointment.Invoices.Any())
        {
            return BadRequest(new { message = "Lịch hẹn này đã có hóa đơn." });
        }

        var subtotal = appointment.AppointmentDetails.Sum(d => (d.Quantity ?? 1) * d.UnitPrice);
        var discount = request.Discount ?? 0;
        if (discount < 0) discount = 0;
        if (discount > subtotal) discount = subtotal;

        var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
            ? PaymentMethods.First()
            : request.PaymentMethod;

        var invoice = new Invoice
        {
            AppointmentId = appointment.AppointmentId,
            CustomerId = appointment.CustomerId,
            StaffId = appointment.StaffId,
            Total = subtotal,
            Discount = discount,
            FinalAmount = subtotal - discount,
            PaymentMethod = paymentMethod,
            CreatedAt = DateTime.Now
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var detail in appointment.AppointmentDetails)
        {
            var quantity = detail.Quantity ?? 1;
            _context.InvoiceDetails.Add(new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ServiceId = detail.ServiceId,
                Quantity = quantity,
                UnitPrice = detail.UnitPrice,
                Subtotal = quantity * detail.UnitPrice
            });
        }

        await _context.SaveChangesAsync();

        var response = new CreateInvoiceResponse
        {
            InvoiceId = invoice.InvoiceId,
            Total = invoice.Total,
            Discount = invoice.Discount ?? 0,
            FinalAmount = invoice.FinalAmount ?? (invoice.Total - (invoice.Discount ?? 0)),
            PaymentMethod = invoice.PaymentMethod ?? PaymentMethods.First()
        };

        return Ok(response);
    }

    private string? NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        var trimmed = status.Trim();

        if (StatusAliases.TryGetValue(trimmed, out var normalized))
        {
            return normalized;
        }

        return AllowedStatuses.FirstOrDefault(s => s.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private Task<Appointment?> LoadAppointmentAsync(int appointmentId, int staffId)
    {
        return _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Staff)
            .Include(a => a.AppointmentDetails)
                .ThenInclude(d => d.Service)
            .Include(a => a.Invoices)
                .ThenInclude(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
            .Include(a => a.Invoices)
                .ThenInclude(i => i.Staff)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.StaffId == staffId);
    }
}
