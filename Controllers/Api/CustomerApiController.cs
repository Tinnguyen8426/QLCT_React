using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/customer")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "Customer,KhachHang,User,Admin,Administrator,Staff,NhanVien")]
public class CustomerApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public CustomerApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    private async Task<(User? user, Customer? customer)> GetCurrentCustomerAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || !int.TryParse(userId, out var uid)) return (null, null);

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == uid);
        if (user == null) return (null, null);

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
        return (user, customer);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var (user, customer) = await GetCurrentCustomerAsync();
        if (user == null) return Unauthorized();

        if (customer == null)
        {
            return Ok(new
            {
                userName = user.FullName,
                totalAppointments = 0,
                upcomingAppointments = 0,
                totalSpending = 0m,
                nextAppointments = new List<object>(),
                recentInvoices = new List<object>()
            });
        }

        var now = DateOnly.FromDateTime(DateTime.Now);
        var appointments = await _context.Appointments
            .Where(a => a.CustomerId == customer.CustomerId)
            .ToListAsync();

        var totalAppointments = appointments.Count;
        var upcomingAppointments = appointments.Count(a => a.AppointmentDate >= now && a.Status != "Cancelled");

        var totalSpending = await _context.Invoices
            .Where(i => i.CustomerId == customer.CustomerId)
            .SumAsync(i => i.FinalAmount ?? 0);

        var nextApts = await _context.Appointments
            .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
            .Include(a => a.Staff)
            .Where(a => a.CustomerId == customer.CustomerId && a.AppointmentDate >= now && a.Status != "Cancelled")
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
            .Take(5)
            .Select(a => new
            {
                a.AppointmentId,
                date = a.AppointmentDate.ToString("dd/MM/yyyy"),
                time = a.AppointmentTime.ToString("HH:mm"),
                a.Status,
                staffName = a.Staff != null ? a.Staff.FullName : "Đang phân công",
                services = a.AppointmentDetails
                    .Where(d => d.Service != null)
                    .Select(d => d.Service!.ServiceName)
                    .ToList()
            })
            .ToListAsync();

        var recentInvoices = await _context.Invoices
            .Include(i => i.Staff)
            .Where(i => i.CustomerId == customer.CustomerId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new
            {
                i.InvoiceId,
                i.FinalAmount,
                staffName = i.Staff != null ? i.Staff.FullName : null,
                createdAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            userName = user.FullName,
            totalAppointments,
            upcomingAppointments,
            totalSpending,
            nextAppointments = nextApts,
            recentInvoices
        });
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (user, customer) = await GetCurrentCustomerAsync();
        if (user == null) return Unauthorized();
        if (customer == null) return Ok(new { appointments = new List<object>(), total = 0 });

        var query = _context.Appointments
            .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
            .Include(a => a.Staff)
            .Where(a => a.CustomerId == customer.CustomerId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime);

        var total = await query.CountAsync();
        var appointments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AppointmentId,
                date = a.AppointmentDate.ToString("dd/MM/yyyy"),
                time = a.AppointmentTime.ToString("HH:mm"),
                a.Status,
                a.Note,
                staffId = a.StaffId,
                staffName = a.Staff != null ? a.Staff.FullName : null,
                services = a.AppointmentDetails
                    .Where(d => d.Service != null)
                    .Select(d => new { d.Service!.ServiceId, d.Service.ServiceName, d.Service.Price })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { appointments, total, page, pageSize });
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (user, customer) = await GetCurrentCustomerAsync();
        if (user == null) return Unauthorized();
        if (customer == null) return Ok(new { invoices = new List<object>(), total = 0 });

        var query = _context.Invoices
            .Include(i => i.Staff)
            .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
            .Where(i => i.CustomerId == customer.CustomerId)
            .OrderByDescending(i => i.CreatedAt);

        var total = await query.CountAsync();
        var invoices = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.InvoiceId,
                i.Total,
                i.Discount,
                i.FinalAmount,
                i.PaymentMethod,
                i.CreatedAt,
                staffName = i.Staff != null ? i.Staff.FullName : null,
                details = i.InvoiceDetails.Select(d => new
                {
                    serviceName = d.Service != null ? d.Service.ServiceName : null,
                    d.Quantity,
                    d.UnitPrice,
                    d.Subtotal
                }).ToList()
            })
            .ToListAsync();

        return Ok(new { invoices, total, page, pageSize });
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products()
    {
        var (user, customer) = await GetCurrentCustomerAsync();
        if (user == null) return Unauthorized();
        if (customer == null) return Ok(new { purchasedProducts = new List<object>(), recommendedProducts = new List<object>() });

        var purchasedProducts = await _context.InvoiceDetails
            .Include(d => d.Invoice)
            .Include(d => d.Product)
            .Where(d => d.Invoice != null && d.Invoice.CustomerId == customer.CustomerId && d.ProductId != null)
            .GroupBy(d => d.ProductId)
            .Select(g => new
            {
                productId = g.Key,
                productName = g.First().Product!.ProductName,
                imageUrl = g.First().Product!.ImageUrl,
                description = g.First().Product!.Description,
                price = g.First().Product!.Price,
                purchasedQuantity = g.Sum(d => d.Quantity),
                lastPurchasedAt = g.Max(d => d.Invoice!.CreatedAt)
            })
            .OrderByDescending(p => p.lastPurchasedAt)
            .ToListAsync();

        var purchasedProductIds = purchasedProducts.Select(p => p.productId).ToList();

        var recommendedProducts = await _context.InvoiceDetails
            .Where(d => d.ProductId != null && !purchasedProductIds.Contains(d.ProductId))
            .GroupBy(d => d.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQty = g.Sum(d => d.Quantity)
            })
            .OrderByDescending(x => x.TotalQty)
            .Take(6)
            .Join(_context.Products, x => x.ProductId, p => p.ProductId,
                (x, p) => new
                {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    imageUrl = p.ImageUrl,
                    description = p.Description,
                    price = p.Price
                })
            .ToListAsync();

        if (!recommendedProducts.Any())
        {
            recommendedProducts = await _context.Products
                .Where(p => p.IsActive != false && !purchasedProductIds.Contains(p.ProductId))
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    imageUrl = p.ImageUrl,
                    description = p.Description,
                    price = p.Price
                })
                .ToListAsync();
        }

        return Ok(new { purchasedProducts, recommendedProducts });
    }
}
