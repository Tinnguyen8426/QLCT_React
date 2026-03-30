using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using QuanLyTiemCatToc.Services;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyTiemCatToc.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerPortalController : Controller
{
    private readonly QuanLyTiemCatTocContext _context;
    private readonly StorefrontComposer _storefrontComposer;

    public CustomerPortalController(QuanLyTiemCatTocContext context, StorefrontComposer storefrontComposer)
    {
        _context = context;
        _storefrontComposer = storefrontComposer;
    }

    public async Task<IActionResult> Dashboard()
    {
        var (user, customer) = await ResolveCustomerContextAsync();
        if (user == null) return RedirectToLogin();

        var viewModel = new CustomerDashboardViewModel
        {
            User = user,
            Customer = customer,
            NextAppointments = new List<Appointment>(),
            RecentInvoices = new List<Invoice>()
        };

        if (customer != null)
        {
            var appointmentsQuery = _context.Appointments
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Staff)
                .Where(a => a.CustomerId == customer.CustomerId);

            viewModel.TotalAppointments = await appointmentsQuery.CountAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            viewModel.UpcomingAppointments = await appointmentsQuery
                .Where(a => a.AppointmentDate >= today && a.Status != "Cancelled")
                .CountAsync();

            viewModel.NextAppointments = await appointmentsQuery
                .Where(a => a.AppointmentDate >= today)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(5)
                .ToListAsync();

            var invoicesQuery = _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Staff)
                .Where(i => i.CustomerId == customer.CustomerId);

            viewModel.TotalSpending = await invoicesQuery.SumAsync(i => (decimal?)(i.FinalAmount ?? 0)) ?? 0;
            viewModel.RecentInvoices = await invoicesQuery
                .OrderByDescending(i => i.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
        else
        {
            TempData["WarningMessage"] = "Hệ thống chưa tìm thấy thông tin khách hàng tương ứng. Vui lòng cập nhật số điện thoại trong hồ sơ.";
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Appointments()
    {
        var (user, customer) = await ResolveCustomerContextAsync();
        if (user == null) return RedirectToLogin();

        var model = new CustomerAppointmentsViewModel
        {
            User = user,
            Customer = customer,
            Appointments = new List<Appointment>()
        };

        if (customer != null)
        {
            model.Appointments = await _context.Appointments
                .Include(a => a.AppointmentDetails)
                    .ThenInclude(d => d.Service)
                .Include(a => a.Staff)
                .Where(a => a.CustomerId == customer.CustomerId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var (user, customer) = await ResolveCustomerContextAsync();
        if (user == null) return RedirectToLogin();
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Không xác định được khách hàng để hủy lịch.";
            return RedirectToAction(nameof(Appointments));
        }

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id && a.CustomerId == customer.CustomerId);
        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Lịch hẹn không hợp lệ hoặc không thuộc sở hữu của bạn.";
            return RedirectToAction(nameof(Appointments));
        }

        if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
        {
            TempData["ErrorMessage"] = "Chỉ có thể hủy các lịch hẹn chưa hoàn thành.";
            return RedirectToAction(nameof(Appointments));
        }

        appointment.Status = "Cancelled";
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã hủy lịch hẹn thành công.";
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> Invoices()
    {
        var (user, customer) = await ResolveCustomerContextAsync();
        if (user == null) return RedirectToLogin();

        var model = new CustomerInvoicesViewModel
        {
            User = user,
            Customer = customer,
            Invoices = new List<Invoice>()
        };

        if (customer != null)
        {
            model.Invoices = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Staff)
                .Where(i => i.CustomerId == customer.CustomerId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        return View(model);
    }

    public async Task<IActionResult> Products()
    {
        var (user, customer) = await ResolveCustomerContextAsync();
        if (user == null) return RedirectToLogin();

        var viewModel = await _storefrontComposer.BuildCustomerProductsAsync(HttpContext.Session, user, customer);

        if (customer == null)
        {
            TempData["WarningMessage"] = "Hãy cập nhật thông tin cá nhân để đồng bộ với lịch sử mua hàng.";
        }

        return View(viewModel);
    }

    private async Task<(User? user, Customer? customer)> ResolveCustomerContextAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
        {
            return (null, null);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive == true);
        if (user == null)
        {
            return (null, null);
        }

        var customer = await FindCustomerRecordAsync(user);
        return (user, customer);
    }

    private async Task<Customer?> FindCustomerRecordAsync(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            var byPhone = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == user.Phone);
            if (byPhone != null) return byPhone;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var normalizedEmail = user.Email.Trim().ToLowerInvariant();
            var byEmail = await _context.Customers.FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == normalizedEmail);
            if (byEmail != null) return byEmail;
        }

        return null;
    }

    private IActionResult RedirectToLogin()
    {
        return RedirectToAction("Login", "Account");
    }
}
