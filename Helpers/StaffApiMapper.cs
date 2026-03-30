using System.Globalization;
using System.Linq;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.Api;

namespace QuanLyTiemCatToc.Helpers;

public static class StaffApiMapper
{
    public static AppointmentSummaryDto ToSummaryDto(Appointment appointment)
    {
        var customer = appointment.Customer;
        return new AppointmentSummaryDto
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentDate = FormatDate(appointment.AppointmentDate),
            AppointmentTime = FormatTime(appointment.AppointmentTime),
            Status = NormalizeStatus(appointment.Status),
            Note = appointment.Note,
            Customer = new CustomerBriefDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone
            },
            Services = appointment.AppointmentDetails
                .Select(d => new AppointmentServiceDto
                {
                    ServiceId = d.ServiceId,
                    ServiceName = d.Service?.ServiceName ?? string.Empty,
                    Price = d.UnitPrice,
                    Duration = d.Duration,
                    Quantity = d.Quantity ?? 1
                })
                .ToList()
        };
    }

    public static AppointmentDetailDto ToDetailDto(Appointment appointment)
    {
        var customer = appointment.Customer;
        return new AppointmentDetailDto
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentDate = FormatDate(appointment.AppointmentDate),
            AppointmentTime = FormatTime(appointment.AppointmentTime),
            Status = NormalizeStatus(appointment.Status),
            Note = appointment.Note,
            Customer = new CustomerBriefDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone
            },
            Services = appointment.AppointmentDetails
                .Select(d => new AppointmentServiceDto
                {
                    ServiceId = d.ServiceId,
                    ServiceName = d.Service?.ServiceName ?? string.Empty,
                    Price = d.UnitPrice,
                    Duration = d.Duration,
                    Quantity = d.Quantity ?? 1
                })
                .ToList(),
            Invoices = appointment.Invoices
                .OrderByDescending(i => i.CreatedAt ?? DateTime.MinValue)
                .Select(invoice => new InvoiceDto
                {
                    InvoiceId = invoice.InvoiceId,
                    Total = invoice.Total,
                    Discount = invoice.Discount ?? 0,
                    FinalAmount = invoice.FinalAmount ?? (invoice.Total - (invoice.Discount ?? 0)),
                    PaymentMethod = invoice.PaymentMethod,
                    CreatedAt = invoice.CreatedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    Items = invoice.InvoiceDetails.Select(item => new InvoiceItemDto
                    {
                        ItemType = item.ItemType ?? (item.ServiceId.HasValue ? "Service" : "Product"),
                        ServiceId = item.ServiceId,
                        ServiceName = item.Service?.ServiceName,
                        Quantity = item.Quantity ?? 1,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal ?? ((item.Quantity ?? 1) * item.UnitPrice)
                    }).ToList()
                })
                .ToList()
        };
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? "Pending" : status;
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatTime(TimeOnly time)
    {
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}
