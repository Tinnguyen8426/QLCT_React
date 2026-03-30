using System.Collections.Generic;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class CustomerDashboardViewModel
{
    public User User { get; set; } = null!;
    public Customer? Customer { get; set; }
    public int TotalAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public decimal TotalSpending { get; set; }
    public List<Appointment> NextAppointments { get; set; } = new();
    public List<Invoice> RecentInvoices { get; set; } = new();
}

public class CustomerAppointmentsViewModel
{
    public User User { get; set; } = null!;
    public Customer? Customer { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}

public class CustomerInvoicesViewModel
{
    public User User { get; set; } = null!;
    public Customer? Customer { get; set; }
    public List<Invoice> Invoices { get; set; } = new();
}
