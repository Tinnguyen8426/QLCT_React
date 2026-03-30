using System;
using System.Collections.Generic;

namespace QuanLyTiemCatToc.Models.Api;

public class StaffLoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class StaffLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public StaffProfileDto Staff { get; set; } = new();
}

public class StaffProfileDto
{
    public int StaffId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class StaffDashboardDto
{
    public int TotalToday { get; set; }
    public int ConfirmedToday { get; set; }
    public int CompletedToday { get; set; }
    public int PendingToday { get; set; }
    public AppointmentSummaryDto? NextAppointment { get; set; }
    public List<AppointmentSummaryDto> UpcomingAppointments { get; set; } = new();
    public List<AppointmentSummaryDto> RecentCompletedAppointments { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}

public class StaffAppointmentListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, int> StatusOverview { get; set; } = new();
    public List<AppointmentSummaryDto> Items { get; set; } = new();
}

public class AppointmentSummaryDto
{
    public int AppointmentId { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public CustomerBriefDto Customer { get; set; } = new();
    public List<AppointmentServiceDto> Services { get; set; } = new();
}

public class AppointmentDetailDto
{
    public int AppointmentId { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public CustomerBriefDto Customer { get; set; } = new();
    public List<AppointmentServiceDto> Services { get; set; } = new();
    public List<InvoiceDto> Invoices { get; set; } = new();
}

public class CustomerBriefDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class AppointmentServiceDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Duration { get; set; }
    public int Quantity { get; set; }
}

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CreatedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public string ItemType { get; set; } = string.Empty;
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class UpdateStatusRequest
{
    public string? Status { get; set; }
}

public class CreateInvoiceRequest
{
    public decimal? Discount { get; set; }
    public string? PaymentMethod { get; set; }
}

public class CreateInvoiceResponse
{
    public int InvoiceId { get; set; }
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}
