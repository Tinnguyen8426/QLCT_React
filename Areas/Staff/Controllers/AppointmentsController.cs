using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff")]
    public class AppointmentsController : Controller
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
        private static readonly IReadOnlyList<string> PaymentMethods = new[]
        {
            "Tiền mặt",
            "Chuyển khoản",
            "Thẻ"
        };

        public AppointmentsController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? status, DateOnly? date, int page = 1, int pageSize = 10)
        {
            ViewBag.ActiveMenu = "Appointments";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var staffId))
            {
                return RedirectToAction("Login", "Account", new { area = string.Empty });
            }

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

            string? normalizedFilterStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                normalizedFilterStatus = AllowedStatuses.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilterStatus))
            {
                filteredQuery = filteredQuery.Where(a => a.Status == normalizedFilterStatus);
            }

            if (date.HasValue)
            {
                filteredQuery = filteredQuery.Where(a => a.AppointmentDate == date.Value);
            }

            var appointments = await filteredQuery
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToPagedListAsync(page, pageSize);

            var overview = AllowedStatuses.ToDictionary(s => s, s => 0, StringComparer.OrdinalIgnoreCase);

            foreach (var item in overviewData)
            {
                overview[item.Status] = item.Count;
            }

            var viewModel = new StaffAppointmentListViewModel
            {
                Appointments = appointments,
                SearchTerm = searchTerm,
                StatusFilter = normalizedFilterStatus,
                DateFilter = date,
                StatusOverview = overview
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnSearchTerm, string? returnStatus, string? returnDate, int returnPage = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var staffId))
            {
                TempData["ErrorMessage"] = "Không xác định được nhân viên hiện tại.";
                return RedirectToAction("Login", "Account", new { area = string.Empty });
            }

            var targetStatus = AllowedStatuses.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase)) ?? "Pending";

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id && a.StaffId == staffId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch hẹn cần cập nhật.";
            }
            else
            {
                appointment.Status = targetStatus;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = $"Đã cập nhật trạng thái lịch hẹn #{id} thành \"{GetStatusLabel(targetStatus)}\".";
            }

            return RedirectToAction(nameof(Index), new
            {
                page = returnPage < 1 ? 1 : returnPage,
                searchTerm = returnSearchTerm,
                status = returnStatus,
                date = returnDate
            });
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var staffId = GetCurrentStaffId();
            if (!staffId.HasValue)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("Login", "Account", new { area = string.Empty });
            }

            var appointment = await LoadAppointmentForInvoiceAsync(id, staffId.Value);
            if (appointment == null) return NotFound();

            if (!appointment.AppointmentDetails.Any())
            {
                TempData["ErrorMessage"] = "Lịch hẹn chưa có dịch vụ nên không thể tạo hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            SetupInvoiceViewBag(appointment.AppointmentId);
            return View("~/Views/Shared/AppointmentInvoice.cshtml", BuildInvoiceViewModel(appointment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invoice(AppointmentInvoiceInputModel input)
        {
            var staffId = GetCurrentStaffId();
            if (!staffId.HasValue)
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("Login", "Account", new { area = string.Empty });
            }

            var appointment = await LoadAppointmentForInvoiceAsync(input.AppointmentId, staffId.Value);
            if (appointment == null) return NotFound();

            if (!appointment.AppointmentDetails.Any())
            {
                ModelState.AddModelError(string.Empty, "Lịch hẹn chưa có dịch vụ.");
            }

            if (appointment.Invoices.Any())
            {
                ModelState.AddModelError(string.Empty, "Lịch hẹn này đã có hóa đơn.");
            }

            var subtotal = appointment.AppointmentDetails.Sum(d => (d.Quantity ?? 1) * d.UnitPrice);
            var discount = input.Discount;
            if (discount < 0) discount = 0;
            if (discount > subtotal) discount = subtotal;

            if (!ModelState.IsValid)
            {
                SetupInvoiceViewBag(appointment.AppointmentId);
                return View("~/Views/Shared/AppointmentInvoice.cshtml",
                    BuildInvoiceViewModel(appointment, discount, input.PaymentMethod));
            }

            var invoice = new Invoice
            {
                AppointmentId = appointment.AppointmentId,
                CustomerId = appointment.CustomerId,
                StaffId = appointment.StaffId,
                Total = subtotal,
                Discount = discount,
                FinalAmount = subtotal - discount,
                PaymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod)
                    ? PaymentMethods.First()
                    : input.PaymentMethod,
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
            TempData["StatusMessage"] = $"Đã tạo hóa đơn #{invoice.InvoiceId} cho lịch hẹn #{appointment.AppointmentId}.";
            return RedirectToAction(nameof(Index));
        }

        private static string GetStatusLabel(string status) =>
            status switch
            {
                "Confirmed" => "Đã xác nhận",
                "Completed" => "Hoàn thành",
                "Cancelled" => "Đã hủy",
                "No-show" => "Vắng mặt",
                _ => "Chờ xác nhận"
            };

        private int? GetCurrentStaffId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var staffId))
            {
                return null;
            }

            return staffId;
        }

        private Task<Appointment?> LoadAppointmentForInvoiceAsync(int appointmentId, int staffId)
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

        private void SetupInvoiceViewBag(int appointmentId)
        {
            ViewBag.LayoutPath = "~/Areas/Staff/Views/Shared/_StaffLayout.cshtml";
            ViewBag.InvoicePageTitle = $"Hóa đơn cho lịch hẹn #{appointmentId}";
            ViewBag.BackUrl = Url.Action(nameof(Index));
        }

        private AppointmentInvoiceViewModel BuildInvoiceViewModel(Appointment appointment, decimal? discountOverride = null, string? paymentMethodOverride = null)
        {
            var subtotal = appointment.AppointmentDetails.Sum(d => (d.Quantity ?? 1) * d.UnitPrice);
            var existingInvoices = appointment.Invoices
                .OrderByDescending(i => i.CreatedAt ?? DateTime.MinValue)
                .ToList();

            var chosenPaymentMethod = paymentMethodOverride
                ?? existingInvoices.FirstOrDefault()?.PaymentMethod
                ?? PaymentMethods.First();

            var discount = discountOverride ?? 0;
            if (discount < 0) discount = 0;
            if (discount > subtotal) discount = subtotal;

            return new AppointmentInvoiceViewModel
            {
                Appointment = appointment,
                Subtotal = subtotal,
                Discount = discount,
                PaymentMethod = chosenPaymentMethod,
                PaymentMethods = PaymentMethods,
                ExistingInvoices = existingInvoices,
                AllowCreation = !existingInvoices.Any()
            };
        }
    }
}
