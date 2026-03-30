using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductOrdersController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public ProductOrdersController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, string? keyword, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            ViewBag.ActiveMenu = "ProductOrders";

            var baseQuery = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                .Where(i => i.InvoiceDetails.Any(d => d.ProductId != null));

            if (fromDate.HasValue)
            {
                baseQuery = baseQuery.Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value.Date >= fromDate.Value.Date);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value < endDate);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim();
                var hasInvoiceId = int.TryParse(trimmed, out var invoiceId);

                baseQuery = baseQuery.Where(i =>
                    (hasInvoiceId && i.InvoiceId == invoiceId) ||
                    (i.Customer != null && (
                        EF.Functions.Like(i.Customer.FullName, $"%{trimmed}%") ||
                        EF.Functions.Like(i.Customer.Phone, $"%{trimmed}%")
                    )) ||
                    i.InvoiceDetails.Any(d => d.Product != null && EF.Functions.Like(d.Product.ProductName, $"%{trimmed}%"))
                );
            }

            var overviewData = await baseQuery
                .GroupBy(i => string.IsNullOrWhiteSpace(i.FulfillmentStatus) ? ProductOrderStatusHelper.Pending : i.FulfillmentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var filteredQuery = baseQuery;
            var normalizedStatus = ProductOrderStatusHelper.All.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                filteredQuery = filteredQuery.Where(i => (i.FulfillmentStatus ?? ProductOrderStatusHelper.Pending) == normalizedStatus);
                ViewBag.SelectedStatus = normalizedStatus;
            }

            var orders = await filteredQuery
                .OrderByDescending(i => i.CreatedAt)
                .ToPagedListAsync(page, pageSize);

            var overview = ProductOrderStatusHelper.All.ToDictionary(s => s, _ => 0, StringComparer.OrdinalIgnoreCase);
            foreach (var item in overviewData)
            {
                var key = ProductOrderStatusHelper.Normalize(item.Status);
                overview[key] = item.Count;
            }

            var viewModel = new ProductOrderListViewModel
            {
                Orders = orders,
                Status = normalizedStatus,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                StatusCounts = overview
            };

            ViewBag.StatusOptions = ProductOrderStatusHelper.All;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnStatus, string? returnKeyword, string? returnFrom, string? returnTo, int page = 1)
        {
            var normalized = ProductOrderStatusHelper.All.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase))
                ?? ProductOrderStatusHelper.Pending;

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.InvoiceDetails.Any(d => d.ProductId != null));

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn mỹ phẩm cần cập nhật.";
            }
            else
            {
                invoice.FulfillmentStatus = normalized;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn #{id} thành \"{ProductOrderStatusHelper.Translate(normalized)}\".";
            }

            return RedirectToAction(nameof(Index), new
            {
                status = returnStatus,
                keyword = returnKeyword,
                fromDate = returnFrom,
                toDate = returnTo,
                page
            });
        }
    }
}
