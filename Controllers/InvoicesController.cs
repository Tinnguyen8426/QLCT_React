using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using System.Security.Claims;

namespace QuanLyTiemCatToc.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public InvoicesController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var phone = await GetCurrentUserPhoneAsync();
            ViewBag.Phone = phone;
            ViewBag.IsLockedPhone = !string.IsNullOrWhiteSpace(phone);

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var invoices = await LoadInvoicesByPhoneAsync(phone);
                return View(invoices);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> History(string phone)
        {
            var currentPhone = await GetCurrentUserPhoneAsync();
            var isLocked = !string.IsNullOrWhiteSpace(currentPhone);
            var lookupPhone = isLocked ? currentPhone : phone?.Trim();

            if (string.IsNullOrWhiteSpace(lookupPhone))
            {
                ModelState.AddModelError("", "Vui lòng nhập số điện thoại.");
                return View();
            }

            if (!isLocked)
            {
                var existingUser = await _context.Users.AnyAsync(u => u.IsActive == true && u.Phone == lookupPhone);
                if (existingUser)
                {
                    TempData["ErrorMessage"] = "Số điện thoại này đã liên kết tài khoản. Vui lòng đăng nhập để xem lịch sử.";
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("History", "Invoices") });
                }
            }

            var invoices = await LoadInvoicesByPhoneAsync(lookupPhone);
            ViewBag.Phone = lookupPhone;
            ViewBag.IsLockedPhone = isLocked;
            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Staff)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
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

        private async Task<List<Invoice>> LoadInvoicesByPhoneAsync(string phone)
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Staff)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Where(i => i.Customer.Phone == phone)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
    }
}
