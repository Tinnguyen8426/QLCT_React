using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public CustomersController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        // GET: Admin/Customers
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            ViewBag.SearchString = searchString;
            var query = _context.Customers.AsQueryable();

            // Tìm kiếm theo tên hoặc số điện thoại
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.FullName.Contains(searchString) || 
                                        c.Phone.Contains(searchString) ||
                                        (c.Email != null && c.Email.Contains(searchString)));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToPagedListAsync(page, pageSize);

            return View(customers);
        }

        // GET: Admin/Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.ActiveMenu = "Customers";

            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Appointments)
                    .ThenInclude(a => a.AppointmentDetails)
                        .ThenInclude(d => d.Service)
                .Include(c => c.Appointments)
                    .ThenInclude(a => a.Staff)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                        .ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            // Thống kê
            ViewBag.TotalAppointments = customer.Appointments.Count;
            ViewBag.CompletedAppointments = customer.Appointments.Count(a => a.Status == "Completed");
            ViewBag.TotalSpent = customer.Invoices.Sum(i => i.FinalAmount ?? 0);

            return View(customer);
        }

        // GET: Admin/Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Phone,Email,Gender,Birthday,Note")] Customer customer)
        {
            ViewBag.ActiveMenu = "Customers";

            if (ModelState.IsValid)
            {
                customer.CreatedAt = DateTime.Now;
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Admin/Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.ActiveMenu = "Customers";

            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Admin/Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FullName,Phone,Email,Gender,Birthday,Note,CreatedAt")] Customer customer)
        {
            ViewBag.ActiveMenu = "Customers";

            if (id != customer.CustomerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // POST: Admin/Customers/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}
