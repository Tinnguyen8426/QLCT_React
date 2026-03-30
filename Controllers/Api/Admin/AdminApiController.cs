using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Controllers.Api.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,Administrator")]
public class AdminApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminApiController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // ===== DASHBOARD =====
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var invoicesToday = await _context.Invoices.CountAsync(i => i.CreatedAt != null && DateOnly.FromDateTime(i.CreatedAt.Value) == today);
        var revenueThisMonth = await _context.Invoices
            .Where(i => i.CreatedAt != null && DateOnly.FromDateTime(i.CreatedAt.Value) >= monthStart)
            .SumAsync(i => i.FinalAmount ?? 0);
        var totalCustomers = await _context.Customers.CountAsync();
        var appointmentsToday = await _context.Appointments.CountAsync(a => a.AppointmentDate == today);
        var totalAppointments = await _context.Appointments.CountAsync();
        var totalInvoices = await _context.Invoices.CountAsync();
        var totalRevenue = await _context.Invoices.SumAsync(i => i.FinalAmount ?? 0);
        var totalServices = await _context.Services.CountAsync(s => s.IsActive != false);
        var totalUsers = await _context.Users.CountAsync(u => u.IsActive == true);
        var pendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending" || a.Status == null || a.Status == "");
        var completedToday = await _context.Appointments.CountAsync(a => a.AppointmentDate == today && a.Status == "Completed");
        var newCustomersThisMonth = await _context.Customers.CountAsync(c => c.CreatedAt != null && DateOnly.FromDateTime(c.CreatedAt.Value) >= monthStart);

        return Ok(new
        {
            invoicesToday,
            revenueThisMonth,
            totalCustomers,
            appointmentsToday,
            totalAppointments,
            totalInvoices,
            totalRevenue,
            totalServices,
            totalUsers,
            pendingAppointments,
            completedToday,
            newCustomersThisMonth
        });
    }

    [HttpGet("dashboard/recent-appointments")]
    public async Task<IActionResult> RecentAppointments()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Customer)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .Take(10)
            .Select(a => new
            {
                a.AppointmentId,
                customerName = a.Customer != null ? a.Customer.FullName : "Không có",
                a.Status,
                date = a.AppointmentDate.ToString("dd/MM/yyyy"),
                time = a.AppointmentTime.ToString("HH:mm")
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // ===== UPLOAD FILE =====
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Không có file" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Loại file không được hỗ trợ" });

        var fileName = Guid.NewGuid().ToString() + ext;
        var uploadPath = Path.Combine(_env.WebRootPath, "images", "uploads");

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { url = $"/images/uploads/{fileName}" });
    }

    // ===== APPOINTMENTS CRUD =====
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var query = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Staff)
            .Include(a => a.AppointmentDetails).ThenInclude(d => d.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            query = query.Where(a => a.AppointmentDate == d);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.AppointmentId,
                customerName = a.Customer != null ? a.Customer.FullName : null,
                staffName = a.Staff != null ? a.Staff.FullName : null,
                date = a.AppointmentDate.ToString("yyyy-MM-dd"),
                time = a.AppointmentTime.ToString("HH:mm"),
                a.Status,
                a.Note,
                services = a.AppointmentDetails.Where(ad => ad.Service != null).Select(ad => ad.Service!.ServiceName).ToList()
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("appointments/{id:int}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var a = await _context.Appointments
            .Include(x => x.Customer).Include(x => x.Staff)
            .Include(x => x.AppointmentDetails).ThenInclude(d => d.Service)
            .Include(x => x.Invoices)
            .FirstOrDefaultAsync(x => x.AppointmentId == id);

        if (a == null) return NotFound();

        return Ok(new
        {
            a.AppointmentId,
            a.CustomerId,
            customerName = a.Customer?.FullName,
            customerPhone = a.Customer?.Phone,
            a.StaffId,
            staffName = a.Staff?.FullName,
            date = a.AppointmentDate.ToString("yyyy-MM-dd"),
            time = a.AppointmentTime.ToString("HH:mm"),
            a.Status,
            a.Note,
            a.CreatedAt,
            services = a.AppointmentDetails.Select(d => new
            {
                d.Service?.ServiceId,
                d.Service?.ServiceName,
                d.UnitPrice,
                d.Quantity
            }),
            hasInvoice = a.Invoices.Any()
        });
    }

    public record UpdateAppointmentStatusRequest(string Status);

    [HttpPut("appointments/{id:int}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusRequest req)
    {
        var a = await _context.Appointments.FindAsync(id);
        if (a == null) return NotFound();
        a.Status = req.Status;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật trạng thái." });
    }

    // ===== CUSTOMERS CRUD =====
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var query = _context.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(c => EF.Functions.Like(c.FullName, $"%{kw}%") || EF.Functions.Like(c.Phone, $"%{kw}%"));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new { c.CustomerId, c.FullName, c.Phone, c.Email, c.Gender, c.Birthday, c.CreatedAt })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    public record CustomerRequest(string FullName, string Phone, string? Email, string? Gender, DateOnly? Birthday, string? Note);

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerRequest req)
    {
        var customer = new Customer
        {
            FullName = req.FullName.Trim(),
            Phone = req.Phone.Trim(),
            Email = req.Email?.Trim(),
            Gender = req.Gender,
            Birthday = req.Birthday,
            Note = req.Note?.Trim(),
            CreatedAt = DateTime.Now
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Tạo thành công.", customerId = customer.CustomerId });
    }

    [HttpPut("customers/{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerRequest req)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.FullName = req.FullName.Trim();
        c.Phone = req.Phone.Trim();
        c.Email = req.Email?.Trim();
        c.Gender = req.Gender;
        c.Birthday = req.Birthday;
        c.Note = req.Note?.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("customers/{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound();
        _context.Customers.Remove(c);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa." });
    }

    // ===== SERVICES CRUD =====
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var services = await _context.Services.OrderBy(s => s.Category).ThenBy(s => s.ServiceName)
            .Select(s => new { s.ServiceId, s.ServiceName, s.Description, s.Price, s.Duration, s.Category, s.ImageUrl, s.IsActive })
            .ToListAsync();
        return Ok(services);
    }

    public record ServiceRequest(string ServiceName, string? Description, decimal Price, int? Duration, string? Category, string? ImageUrl, bool? IsActive);

    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] ServiceRequest req)
    {
        var s = new Service
        {
            ServiceName = req.ServiceName.Trim(),
            Description = req.Description?.Trim(),
            Price = req.Price,
            Duration = req.Duration,
            Category = req.Category?.Trim(),
            ImageUrl = req.ImageUrl?.Trim(),
            IsActive = req.IsActive ?? true
        };
        _context.Services.Add(s);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Tạo dịch vụ thành công.", serviceId = s.ServiceId });
    }

    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceRequest req)
    {
        var s = await _context.Services.FindAsync(id);
        if (s == null) return NotFound();
        s.ServiceName = req.ServiceName.Trim();
        s.Description = req.Description?.Trim();
        s.Price = req.Price;
        s.Duration = req.Duration;
        s.Category = req.Category?.Trim();
        s.ImageUrl = req.ImageUrl?.Trim();
        s.IsActive = req.IsActive ?? true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var s = await _context.Services.FindAsync(id);
        if (s == null) return NotFound();
        s.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã ẩn dịch vụ." });
    }

    // ===== PRODUCTS CRUD =====
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(p => EF.Functions.Like(p.ProductName, $"%{kw}%"));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.ProductId, p.ProductName, p.Description, p.Category, p.Brand, p.ImageUrl, p.Price, p.StockQuantity, p.IsActive })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    public record ProductRequest(string ProductName, string? Description, decimal Price, string? Category, string? Brand, string? ImageUrl, int StockQuantity, string? Unit, decimal? CostPrice, bool? IsActive);

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductRequest req)
    {
        var p = new Product
        {
            ProductName = req.ProductName.Trim(),
            Description = req.Description?.Trim(),
            Price = req.Price,
            Category = req.Category?.Trim(),
            Brand = req.Brand?.Trim(),
            ImageUrl = req.ImageUrl?.Trim(),
            StockQuantity = req.StockQuantity,
            Unit = req.Unit?.Trim(),
            CostPrice = req.CostPrice,
            IsActive = req.IsActive ?? true,
            CreatedAt = DateTime.Now
        };
        _context.Products.Add(p);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Tạo sản phẩm thành công.", productId = p.ProductId });
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductRequest req)
    {
        var p = await _context.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.ProductName = req.ProductName.Trim();
        p.Description = req.Description?.Trim();
        p.Price = req.Price;
        p.Category = req.Category?.Trim();
        p.Brand = req.Brand?.Trim();
        p.ImageUrl = req.ImageUrl?.Trim();
        p.StockQuantity = req.StockQuantity;
        p.Unit = req.Unit?.Trim();
        p.CostPrice = req.CostPrice;
        p.IsActive = req.IsActive ?? true;
        p.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var p = await _context.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã ẩn sản phẩm." });
    }

    // ===== USERS CRUD =====
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var total = await _context.Users.CountAsync();
        var items = await _context.Users.Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.UserId, u.FullName, u.Email, u.Phone, role = u.Role.RoleName, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    public record UserRequest(string FullName, string Email, string? Phone, int RoleId, bool? IsActive, string? Password);

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] UserRequest req)
    {
        if (await _context.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email đã tồn tại." });

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            Phone = req.Phone?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password ?? "123456"),
            RoleId = req.RoleId,
            IsActive = req.IsActive ?? true,
            CreatedAt = DateTime.Now
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Tạo tài khoản thành công.", userId = user.UserId });
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserRequest req)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.FullName = req.FullName.Trim();
        u.Email = req.Email.Trim().ToLowerInvariant();
        u.Phone = req.Phone?.Trim();
        u.RoleId = req.RoleId;
        u.IsActive = req.IsActive ?? true;
        if (!string.IsNullOrWhiteSpace(req.Password))
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã vô hiệu hóa tài khoản." });
    }

    // ===== INVOICES =====
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var query = _context.Invoices
            .Include(i => i.Customer).Include(i => i.Staff)
            .OrderByDescending(i => i.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new
            {
                i.InvoiceId,
                customerName = i.Customer != null ? i.Customer.FullName : null,
                staffName = i.Staff != null ? i.Staff.FullName : null,
                i.Total,
                i.Discount,
                i.FinalAmount,
                i.PaymentMethod,
                i.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var i = await _context.Invoices
            .Include(x => x.Customer).Include(x => x.Staff)
            .Include(x => x.InvoiceDetails).ThenInclude(d => d.Service)
            .Include(x => x.InvoiceDetails).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (i == null) return NotFound();

        return Ok(new
        {
            i.InvoiceId,
            i.AppointmentId,
            customerName = i.Customer?.FullName,
            staffName = i.Staff?.FullName,
            i.Total,
            i.Discount,
            i.FinalAmount,
            i.PaymentMethod,
            i.CreatedAt,
            i.FulfillmentStatus,
            i.ShippingAddress,
            details = i.InvoiceDetails.Select(d => new
            {
                serviceName = d.Service?.ServiceName,
                productName = d.Product?.ProductName,
                d.Quantity,
                d.UnitPrice,
                d.Subtotal
            })
        });
    }

    // ===== REPORTS =====
    [HttpGet("reports")]
    public async Task<IActionResult> Reports()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var revenueByMonth = await _context.Invoices
            .Where(i => i.CreatedAt != null)
            .GroupBy(i => new { i.CreatedAt!.Value.Year, i.CreatedAt!.Value.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                revenue = g.Sum(i => i.FinalAmount ?? 0),
                count = g.Count()
            })
            .OrderByDescending(x => x.year).ThenByDescending(x => x.month)
            .Take(12)
            .ToListAsync();

        var topServices = await _context.InvoiceDetails
            .Where(d => d.ServiceId != null && d.Service != null)
            .GroupBy(d => d.Service!.ServiceName)
            .Select(g => new { serviceName = g.Key, totalRevenue = g.Sum(d => d.Subtotal), count = g.Count() })
            .OrderByDescending(x => x.totalRevenue)
            .Take(10)
            .ToListAsync();

        return Ok(new { revenueByMonth, topServices });
    }

    // ===== ROLES (for user create form) =====
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles.Select(r => new { r.RoleId, r.RoleName }).ToListAsync();
        return Ok(roles);
    }
}
