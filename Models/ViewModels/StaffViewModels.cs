using System.Collections.Generic;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class StaffDashboardViewModel
{
    public int TotalToday { get; set; }
    public int ConfirmedToday { get; set; }
    public int CompletedToday { get; set; }
    public int PendingToday { get; set; }
    public Appointment? NextAppointment { get; set; }
    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public List<Appointment> RecentCompletedAppointments { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}

public class StaffAppointmentListViewModel
{
    public required IPagedList<Appointment> Appointments { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public DateOnly? DateFilter { get; set; }
    public Dictionary<string, int> StatusOverview { get; set; } = new();
}
