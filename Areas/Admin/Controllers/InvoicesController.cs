using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoicesController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public InvoicesController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Invoices";
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? paymentMethod, int page = 1, int pageSize = 10)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Staff)
                .Include(i => i.Appointment)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value < endDate);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(i => i.PaymentMethod == paymentMethod);
                ViewBag.SelectedPaymentMethod = paymentMethod;
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToPagedListAsync(page, pageSize);

            ViewBag.TotalRevenue = await query.SumAsync(i => (decimal?)i.FinalAmount) ?? 0;

            return View(invoices);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Staff)
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName");
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName");
            ViewData["Services"] = _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToList();
            ViewData["Products"] = _context.Products.Where(p => p.IsActive == true && p.StockQuantity > 0).OrderBy(p => p.ProductName).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,StaffId,Total,Discount,PaymentMethod")] Invoice invoice, 
            int[] serviceIds, int[] productIds, int[] quantities, decimal[] unitPrices, string[] itemTypes)
        {
            if (ModelState.IsValid)
            {
                invoice.CreatedAt = DateTime.Now;
                
                // Tính toán Total từ các chi tiết hóa đơn
                decimal total = 0;
                if (itemTypes != null && itemTypes.Length > 0)
                {
                    for (int i = 0; i < itemTypes.Length; i++)
                    {
                        var quantity = quantities != null && i < quantities.Length ? quantities[i] : 1;
                        var unitPrice = unitPrices != null && i < unitPrices.Length ? unitPrices[i] : 0;
                        total += (decimal)quantity * unitPrice;
                    }
                }
                invoice.Total = total;

                // Tính FinalAmount (Total - Discount)
                invoice.FinalAmount = invoice.Total - (invoice.Discount ?? 0);

                _context.Add(invoice);
                await _context.SaveChangesAsync();

                // Thêm chi tiết hóa đơn (Service hoặc Product)
                if (itemTypes != null && itemTypes.Length > 0)
                {
                    for (int i = 0; i < itemTypes.Length; i++)
                    {
                        var quantity = quantities != null && i < quantities.Length ? quantities[i] : 1;
                        var unitPrice = unitPrices != null && i < unitPrices.Length ? unitPrices[i] : 0;
                        var itemType = itemTypes[i];
                        
                        var detail = new InvoiceDetail
                        {
                            InvoiceId = invoice.InvoiceId,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Subtotal = quantity * unitPrice,
                            ItemType = itemType
                        };

                        if (itemType == "Service" && serviceIds != null && i < serviceIds.Length)
                        {
                            detail.ServiceId = serviceIds[i];
                            
                            // Kiểm tra số lượng tồn kho nếu là Product
                            var service = await _context.Services.FindAsync(serviceIds[i]);
                            if (service == null || service.IsActive != true)
                            {
                                ModelState.AddModelError("", $"Dịch vụ không tồn tại hoặc đã ngừng hoạt động.");
                                ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", invoice.CustomerId);
                                ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", invoice.StaffId);
                                ViewData["Services"] = _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToList();
                                ViewData["Products"] = _context.Products.Where(p => p.IsActive == true && p.StockQuantity > 0).OrderBy(p => p.ProductName).ToList();
                                return View(invoice);
                            }
                        }
                        else if (itemType == "Product" && productIds != null && i < productIds.Length)
                        {
                            detail.ProductId = productIds[i];
                            
                            // Kiểm tra số lượng tồn kho
                            var product = await _context.Products.FindAsync(productIds[i]);
                            if (product == null || product.IsActive != true)
                            {
                                ModelState.AddModelError("", $"Sản phẩm không tồn tại hoặc đã ngừng bán.");
                                ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", invoice.CustomerId);
                                ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", invoice.StaffId);
                                ViewData["Services"] = _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToList();
                                ViewData["Products"] = _context.Products.Where(p => p.IsActive == true && p.StockQuantity > 0).OrderBy(p => p.ProductName).ToList();
                                return View(invoice);
                            }
                            
                            if (product.StockQuantity < quantity)
                            {
                                ModelState.AddModelError("", $"Sản phẩm '{product.ProductName}' chỉ còn {product.StockQuantity} trong kho.");
                                ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", invoice.CustomerId);
                                ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", invoice.StaffId);
                                ViewData["Services"] = _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToList();
                                ViewData["Products"] = _context.Products.Where(p => p.IsActive == true && p.StockQuantity > 0).OrderBy(p => p.ProductName).ToList();
                                return View(invoice);
                            }
                            
                            // Trừ số lượng tồn kho
                            product.StockQuantity -= quantity;
                            product.UpdatedAt = DateTime.Now;
                            _context.Products.Update(product);
                        }

                        _context.InvoiceDetails.Add(detail);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Tạo hóa đơn thành công!";
                return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName", invoice.CustomerId);
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName), "UserId", "FullName", invoice.StaffId);
            ViewData["Services"] = _context.Services.Where(s => s.IsActive == true).OrderBy(s => s.ServiceName).ToList();
            ViewData["Products"] = _context.Products.Where(p => p.IsActive == true && p.StockQuantity > 0).OrderBy(p => p.ProductName).ToList();
            return View(invoice);
        }

        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Staff)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Service)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices.Include(i => i.InvoiceDetails).FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice != null)
            {
                _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa hóa đơn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
