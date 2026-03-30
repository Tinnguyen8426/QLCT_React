using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public ReportsController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Reports";
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddMonths(-1);
            var to = toDate ?? DateTime.Today;

            // Doanh thu
            var revenue = await _context.Invoices
                .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value >= from && i.CreatedAt.Value <= to)
                .SumAsync(i => (decimal?)i.FinalAmount) ?? 0;

            // Dịch vụ phổ biến
            var topServices = await _context.InvoiceDetails
                .Include(d => d.Service)
                .Where(d => d.Service != null)
                .GroupBy(d => new { d.ServiceId, d.Service!.ServiceName })
                .Select(g => new 
                { 
                    ServiceName = g.Key.ServiceName, 
                    Count = g.Sum(x => x.Quantity ?? 1), 
                    Revenue = g.Sum(x => x.Subtotal ?? 0) 
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Nhân viên xuất sắc
            var topStaff = await _context.Invoices
                .Include(i => i.Staff)
                .Where(i => i.StaffId.HasValue && i.CreatedAt.HasValue && i.CreatedAt.Value >= from && i.CreatedAt.Value <= to)
                .GroupBy(i => new { i.StaffId, i.Staff!.FullName })
                .Select(g => new 
                { 
                    StaffName = g.Key.FullName, 
                    Revenue = g.Sum(x => x.FinalAmount ?? 0), 
                    Count = g.Count() 
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            // Khách hàng mới
            var newCustomers = await _context.Customers
                .Where(c => c.CreatedAt.HasValue && c.CreatedAt.Value >= from && c.CreatedAt.Value <= to)
                .CountAsync();

            // Lịch hẹn theo trạng thái
            var appointmentsByStatus = await _context.Appointments
                .Where(a => a.AppointmentDate >= DateOnly.FromDateTime(from) && a.AppointmentDate <= DateOnly.FromDateTime(to))
                .GroupBy(a => a.Status ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.TotalRevenue = revenue;
            ViewBag.TopServices = topServices;
            ViewBag.TopStaff = topStaff;
            ViewBag.NewCustomers = newCustomers;
            ViewBag.AppointmentsByStatus = appointmentsByStatus;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            return View();
        }
    }
}
