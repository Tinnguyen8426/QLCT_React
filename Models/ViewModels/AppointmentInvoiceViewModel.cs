using System.Collections.Generic;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class AppointmentInvoiceViewModel
{
    public Appointment Appointment { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public string? PaymentMethod { get; set; }

    public IReadOnlyCollection<string> PaymentMethods { get; set; } = new List<string>();

    public IReadOnlyCollection<Invoice> ExistingInvoices { get; set; } = new List<Invoice>();

    public bool AllowCreation { get; set; }
}

public class AppointmentInvoiceInputModel
{
    public int AppointmentId { get; set; }

    public decimal Discount { get; set; }

    public string? PaymentMethod { get; set; }
}
