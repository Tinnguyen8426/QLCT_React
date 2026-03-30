using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AppointmentsController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;
        private static readonly IReadOnlyList<string> PaymentMethods = new List<string>
        {
            "Tiền mặt",
            "Chuyển khoản",
            "Thẻ"
        };

        public AppointmentsController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Appointments";
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var query = _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Invoices)
                .Include(a => a.Staff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
                ViewBag.SelectedStatus = status;
            }

            if (fromDate.HasValue)
            {
                var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                query = query.Where(a => a.AppointmentDate >= fromDateOnly);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                query = query.Where(a => a.AppointmentDate <= toDateOnly);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToPagedListAsync(page, pageSize);

            ViewBag.Statuses = new List<string> { "Pending", "Confirmed", "Completed", "Cancelled", "No-show" };

            return View(appointments);
        }

        public IActionResult Calendar()
        {
            return View();
        }

        public async Task<IActionResult> GetCalendarData(DateTime start, DateTime end)
        {
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(end);

            var appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .ToListAsync();

            var events = appointments.Select(a =>
            {
                var customerName = a.Customer != null ? a.Customer.FullName : "Khách vãng lai";
                var serviceNames = a.AppointmentDetails
                    .Where(d => d.Service != null)
                    .Select(d => d.Service!.ServiceName)
                    .Distinct()
                    .ToList();
                var serviceLabel = serviceNames.Any() ? " - " + string.Join(", ", serviceNames) : "";

                return new
                {
                    id = a.AppointmentId,
                    title = customerName + serviceLabel,
                    start = a.AppointmentDate.ToString("yyyy-MM-dd") + "T" + a.AppointmentTime.ToString("HH:mm:ss"),
                    backgroundColor = a.Status == "Completed" ? "#28a745" :
                                      a.Status == "Confirmed" ? "#007bff" :
                                      a.Status == "Cancelled" ? "#dc3545" : "#ffc107"
                };
            }).ToList();

            return Json(events);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Staff)
                .Include(a => a.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                        .ThenInclude(d => d.Service)
                .Include(a => a.Invoices)
                    .ThenInclude(i => i.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();
            return View(appointment);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var appointment = await LoadAppointmentForInvoiceAsync(id);
            if (appointment == null) return NotFound();

            if (!appointment.AppointmentDetails.Any())
            {
                TempData["ErrorMessage"] = "Lịch hẹn chưa có dịch vụ nên không thể tạo hóa đơn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            SetupInvoiceViewBag(appointment.AppointmentId);
            return View("~/Views/Shared/AppointmentInvoice.cshtml", BuildInvoiceViewModel(appointment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invoice(AppointmentInvoiceInputModel input)
        {
            var appointment = await LoadAppointmentForInvoiceAsync(input.AppointmentId);
            if (appointment == null) return NotFound();

            if (!appointment.AppointmentDetails.Any())
            {
                ModelState.AddModelError(string.Empty, "Lịch hẹn chưa có dịch vụ nên không thể tạo hóa đơn.");
            }

            if (appointment.Invoices.Any())
            {
                ModelState.AddModelError(string.Empty, "Lịch hẹn này đã có hóa đơn. Vui lòng kiểm tra lại.");
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
            TempData["SuccessMessage"] = $"Đã tạo hóa đơn #{invoice.InvoiceId} cho lịch hẹn.";
            return RedirectToAction("Details", "Invoices", new { area = "Admin", id = invoice.InvoiceId });
        }

        public async Task<IActionResult> Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName");
            ViewBag.Services = await _context.Services
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();
            ViewBag.SelectedServiceIds = new List<int>();
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,StaffId,AppointmentDate,AppointmentTime,Status,Note")] Appointment appointment)
        {
            var availableServices = await _context.Services
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();
            var serviceLookup = availableServices.ToDictionary(s => s.ServiceId);
            var selectedServiceIds = ParseServiceIds(Request.Form, serviceLookup);

            if (!selectedServiceIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một dịch vụ.");
            }

            if (ModelState.IsValid)
            {
                appointment.CreatedAt = DateTime.Now;
                appointment.Status = string.IsNullOrEmpty(appointment.Status) ? "Pending" : appointment.Status;
                _context.Add(appointment);
                await _context.SaveChangesAsync();

                foreach (var serviceId in selectedServiceIds)
                {
                    var service = serviceLookup[serviceId];
                    _context.AppointmentDetails.Add(new AppointmentDetail
                    {
                        AppointmentId = appointment.AppointmentId,
                        ServiceId = service.ServiceId,
                        Quantity = 1,
                        UnitPrice = service.Price,
                        Duration = service.Duration
                    });
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm lịch hẹn thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", appointment.CustomerId);
            ViewBag.Services = availableServices;
            ViewBag.SelectedServiceIds = selectedServiceIds;
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", appointment.StaffId);
            return View(appointment);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.AppointmentDetails)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            var currentServiceIds = appointment.AppointmentDetails.Select(d => d.ServiceId).ToList();
            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", appointment.CustomerId);
            ViewBag.Services = await _context.Services
                .Where(s => s.IsActive == true || currentServiceIds.Contains(s.ServiceId))
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();
            ViewBag.SelectedServiceIds = currentServiceIds;
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", appointment.StaffId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,CustomerId,StaffId,AppointmentDate,AppointmentTime,Status,Note,CreatedAt")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            var existingAppointment = await _context.Appointments
                .Include(a => a.AppointmentDetails)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (existingAppointment == null) return NotFound();

            var existingDetailServiceIds = existingAppointment.AppointmentDetails.Select(d => d.ServiceId).ToList();
            var availableServices = await _context.Services
                .Where(s => s.IsActive == true || existingDetailServiceIds.Contains(s.ServiceId))
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();
            var serviceLookup = availableServices.ToDictionary(s => s.ServiceId);
            var selectedServiceIds = ParseServiceIds(Request.Form, serviceLookup);

            if (!selectedServiceIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một dịch vụ.");
            }

            // Giữ nguyên khách hàng cũ nếu form không gửi lên (tránh lỗi "Customer is required" khi chỉ đổi trạng thái)
            if (!Request.Form.ContainsKey("CustomerId") || string.IsNullOrWhiteSpace(Request.Form["CustomerId"]))
            {
                appointment.CustomerId = existingAppointment.CustomerId;
                ModelState.Remove(nameof(appointment.CustomerId));
            }

            if (ModelState.IsValid)
            {
                existingAppointment.CustomerId = appointment.CustomerId;
                existingAppointment.StaffId = appointment.StaffId;
                existingAppointment.AppointmentDate = appointment.AppointmentDate;
                existingAppointment.AppointmentTime = appointment.AppointmentTime;
                existingAppointment.Status = string.IsNullOrEmpty(appointment.Status) ? "Pending" : appointment.Status;
                existingAppointment.Note = appointment.Note;

                var currentDetailIds = existingAppointment.AppointmentDetails.Select(d => d.ServiceId).ToList();
                var toRemove = existingAppointment.AppointmentDetails.Where(d => !selectedServiceIds.Contains(d.ServiceId)).ToList();
                if (toRemove.Any())
                {
                    _context.AppointmentDetails.RemoveRange(toRemove);
                }

                var toAdd = selectedServiceIds.Except(currentDetailIds).ToList();
                foreach (var serviceId in toAdd)
                {
                    var service = serviceLookup[serviceId];
                    _context.AppointmentDetails.Add(new AppointmentDetail
                    {
                        AppointmentId = existingAppointment.AppointmentId,
                        ServiceId = service.ServiceId,
                        Quantity = 1,
                        UnitPrice = service.Price,
                        Duration = service.Duration
                    });
                }

                // Đánh dấu entity đã được chỉnh sửa để Entity Framework cập nhật các trường primitive
                _context.Update(existingAppointment);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật lịch hẹn thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(e => e.AppointmentId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", appointment.CustomerId);
            ViewBag.Services = availableServices;
            ViewBag.SelectedServiceIds = selectedServiceIds.Any() ? selectedServiceIds : existingDetailServiceIds;
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", appointment.StaffId);
            return View(existingAppointment);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            appointment.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa lịch hẹn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<Appointment?> LoadAppointmentForInvoiceAsync(int id)
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
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
        }

        private void SetupInvoiceViewBag(int appointmentId)
        {
            ViewBag.LayoutPath = "~/Areas/Admin/Views/Shared/_AdminLayout.cshtml";
            ViewBag.InvoicePageTitle = $"Hóa đơn cho lịch hẹn #{appointmentId}";
            ViewBag.BackUrl = Url.Action(nameof(Details), new { id = appointmentId });
        }

        private static List<int> ParseServiceIds(IFormCollection form, IDictionary<int, Service> serviceLookup)
        {
            var selectedIds = new List<int>();

            if (form != null && form.TryGetValue("serviceIds", out var values))
            {
                foreach (var value in values)
                {
                    if (int.TryParse(value, out var id) && serviceLookup.ContainsKey(id))
                    {
                        selectedIds.Add(id);
                    }
                }
            }

            return selectedIds.Distinct().ToList();
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
