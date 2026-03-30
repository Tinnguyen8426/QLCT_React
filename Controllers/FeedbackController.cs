using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public FeedbackController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var viewModel = new FeedbackPageViewModel();
            var phone = await GetCurrentUserPhoneAsync();
            viewModel.Phone = phone;
            viewModel.IsAuthenticated = !string.IsNullOrWhiteSpace(phone);

            // Stats + recent feedbacks for mọi người
            var feedbacksQuery = _context.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Service)
                .Include(f => f.Staff);

            viewModel.TotalReviews = await feedbacksQuery.CountAsync();
            viewModel.AverageRating = viewModel.TotalReviews > 0
                ? Math.Round(await feedbacksQuery.AverageAsync(f => (double)(f.Rating ?? 0)), 1)
                : 0;

            var distribution = await feedbacksQuery
                .GroupBy(f => f.Rating ?? 0)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();
            for (int i = 1; i <= 5; i++)
            {
                viewModel.RatingDistribution[i] = distribution.FirstOrDefault(d => d.Rating == i)?.Count ?? 0;
            }

            viewModel.RecentFeedbacks = await feedbacksQuery
                .OrderByDescending(f => f.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Nếu đã đăng nhập, lấy các lịch hẹn đã hoàn thành của khách này
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
                if (customer != null)
                {
                    viewModel.CompletedAppointments = await _context.Appointments
                        .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
                        .Where(a => a.CustomerId == customer.CustomerId && a.Status == "Completed")
                        .OrderByDescending(a => a.AppointmentDate)
                        .ThenByDescending(a => a.AppointmentTime)
                        .Take(10)
                        .ToListAsync();
                }
            }

            viewModel.StatusMessage = TempData["SuccessMessage"] as string;
            viewModel.ErrorMessage = TempData["ErrorMessage"] as string;
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string phone, int? serviceId, int? staffId, int rating, string? comment)
        {
            if (string.IsNullOrWhiteSpace(phone) || rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin và đánh giá từ 1-5 sao.");
                return View();
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
            if (customer == null)
            {
                ModelState.AddModelError("", "Không tìm thấy khách hàng với số điện thoại này.");
                return View();
            }

            var feedback = new Feedback
            {
                CustomerId = customer.CustomerId,
                ServiceId = serviceId,
                StaffId = staffId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int appointmentId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn mức sao hợp lệ.";
                return RedirectToAction("Index");
            }

            var phone = await GetCurrentUserPhoneAsync();
            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đánh giá.";
                return RedirectToAction("Index");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng gắn với tài khoản.";
                return RedirectToAction("Index");
            }

            var appointment = await _context.Appointments
                .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.CustomerId == customer.CustomerId && a.Status == "Completed");

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch hẹn hợp lệ để đánh giá.";
                return RedirectToAction("Index");
            }

            var firstService = appointment.AppointmentDetails.FirstOrDefault(d => d.Service != null);

            var feedback = new Feedback
            {
                CustomerId = customer.CustomerId,
                ServiceId = firstService?.ServiceId,
                StaffId = appointment.StaffId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Index");
        }

        private async Task<string?> GetCurrentUserPhoneAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Phone;
        }
    }
}
