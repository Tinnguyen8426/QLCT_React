using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public HomeController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Home";
            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var thisMonth = DateTime.Today.Month;
            var thisYear = DateTime.Today.Year;

            // Tổng số khách hàng
            ViewBag.TotalCustomers = _context.Customers.Count();

            // Số khách hàng mới trong tháng
            ViewBag.NewCustomersThisMonth = _context.Customers
                .Where(c => c.CreatedAt.HasValue && 
                       c.CreatedAt.Value.Month == thisMonth && 
                       c.CreatedAt.Value.Year == thisYear)
                .Count();

            // Lịch hẹn hôm nay
            ViewBag.AppointmentsToday = _context.Appointments
                .Where(a => a.AppointmentDate == today)
                .Count();

            // Lịch hẹn đang chờ
            ViewBag.PendingAppointments = _context.Appointments
                .Where(a => a.Status == "Pending" || a.Status == "Confirmed")
                .Count();

            // Lịch hẹn đã hoàn thành hôm nay
            ViewBag.CompletedToday = _context.Appointments
                .Where(a => a.AppointmentDate == today && a.Status == "Completed")
                .Count();

            // Hóa đơn hôm nay
            ViewBag.InvoicesToday = _context.Invoices
                .Where(i => i.CreatedAt.HasValue && 
                       DateOnly.FromDateTime(i.CreatedAt.Value) == today)
                .Count();

            // Doanh thu hôm nay
            ViewBag.RevenueToday = _context.Invoices
                .Where(i => i.CreatedAt.HasValue && 
                       DateOnly.FromDateTime(i.CreatedAt.Value) == today)
                .Sum(i => (decimal?)i.FinalAmount) ?? 0;

            // Doanh thu tháng này
            ViewBag.RevenueThisMonth = _context.Invoices
                .Where(i => i.CreatedAt.HasValue && 
                       i.CreatedAt.Value.Month == thisMonth && 
                       i.CreatedAt.Value.Year == thisYear)
                .Sum(i => (decimal?)i.FinalAmount) ?? 0;
            
            // Lịch hẹn sắp tới (7 ngày tới)
            var nextWeek = today.AddDays(7);
            ViewBag.UpcomingAppointments = _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Staff)
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate <= nextWeek)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(10)
                .ToList();

            // Dịch vụ phổ biến nhất
            var topServices = _context.InvoiceDetails
                .Include(id => id.Service)
                .Where(id => id.Service != null)
                .GroupBy(id => new { id.ServiceId, id.Service!.ServiceName })
                .Select(g => new StatisticsViewModel
                {
                    Name = g.Key.ServiceName,
                    Count = g.Sum(x => x.Quantity ?? 1),
                    Revenue = g.Sum(x => x.Subtotal ?? 0)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            ViewBag.TopServices = topServices;

            // Nhân viên có nhiều lịch hẹn nhất trong tháng
            var topStaff = _context.Appointments
                .Include(a => a.Staff)
                .Where(a => a.StaffId.HasValue && 
                       a.AppointmentDate.Month == thisMonth && 
                       a.AppointmentDate.Year == thisYear)
                .GroupBy(a => new { a.StaffId, a.Staff!.FullName })
                .Select(g => new StatisticsViewModel
                {
                    Name = g.Key.FullName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            ViewBag.TopStaff = topStaff;

            // Thêm các thống kê khác
            ViewBag.TotalServices = _context.Services.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalAppointments = _context.Appointments.Count();
            ViewBag.TotalInvoices = _context.Invoices.Count();
            ViewBag.TotalRevenue = _context.Invoices.Sum(i => (decimal?)i.FinalAmount) ?? 0;

            return View();
        }
    }
}
