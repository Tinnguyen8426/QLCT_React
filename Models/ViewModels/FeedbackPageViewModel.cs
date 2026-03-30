using System.Collections.Generic;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class FeedbackPageViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? Phone { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public List<Feedback> RecentFeedbacks { get; set; } = new();
    public List<Appointment> CompletedAppointments { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
