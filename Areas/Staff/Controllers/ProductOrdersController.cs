using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff")]
    public class ProductOrdersController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public ProductOrdersController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? status, int page = 1, int pageSize = 10)
        {
            ViewBag.ActiveMenu = "ProductOrders";

            var baseQuery = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                .Where(i => i.InvoiceDetails.Any(d => d.ProductId != null));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                var hasInvoiceId = int.TryParse(keyword, out var invoiceId);
                baseQuery = baseQuery.Where(i =>
                    (hasInvoiceId && i.InvoiceId == invoiceId) ||
                    (i.Customer != null && (
                        EF.Functions.Like(i.Customer.FullName, $"%{keyword}%") ||
                        EF.Functions.Like(i.Customer.Phone, $"%{keyword}%")
                    )) ||
                    i.InvoiceDetails.Any(d => d.Product != null && EF.Functions.Like(d.Product.ProductName, $"%{keyword}%"))
                );
            }

            var overviewData = await baseQuery
                .GroupBy(i => string.IsNullOrWhiteSpace(i.FulfillmentStatus) ? ProductOrderStatusHelper.Pending : i.FulfillmentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var normalizedStatus = ProductOrderStatusHelper.All.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                baseQuery = baseQuery.Where(i => (i.FulfillmentStatus ?? ProductOrderStatusHelper.Pending) == normalizedStatus);
            }

            var orders = await baseQuery
                .OrderByDescending(i => i.CreatedAt)
                .ToPagedListAsync(page, pageSize);

            var overview = ProductOrderStatusHelper.All.ToDictionary(s => s, _ => 0, StringComparer.OrdinalIgnoreCase);
            foreach (var item in overviewData)
            {
                overview[ProductOrderStatusHelper.Normalize(item.Status)] = item.Count;
            }

            var viewModel = new ProductOrderListViewModel
            {
                Orders = orders,
                Keyword = searchTerm,
                Status = normalizedStatus,
                StatusCounts = overview
            };

            ViewBag.StatusOptions = ProductOrderStatusHelper.All;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnSearch, string? returnStatus, int page = 1)
        {
            var normalized = ProductOrderStatusHelper.All.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase))
                ?? ProductOrderStatusHelper.Pending;

            var order = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.InvoiceDetails.Any(d => d.ProductId != null));

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần cập nhật.";
            }
            else
            {
                order.FulfillmentStatus = normalized;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = $"Đã chuyển trạng thái đơn #{id} sang \"{ProductOrderStatusHelper.Translate(normalized)}\".";
            }

            return RedirectToAction(nameof(Index), new
            {
                page = page < 1 ? 1 : page,
                searchTerm = returnSearch,
                status = returnStatus
            });
        }
    }
}
