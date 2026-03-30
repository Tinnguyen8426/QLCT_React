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
    public class ProductsController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string? category, bool? isActive, int page = 1, int pageSize = 10)
        {
            ViewBag.ActiveMenu = "Products";
            var query = _context.Products.AsQueryable();

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
                ViewBag.SelectedCategory = category;
            }

            // Lọc theo trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
                ViewBag.SelectedStatus = isActive.Value;
            }

            var products = await query.OrderBy(p => p.Category).ThenBy(p => p.ProductName).ToPagedListAsync(page, pageSize);

            // Lấy danh sách categories
            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            return View(products);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.ActiveMenu = "Products";
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.InvoiceDetails)
                    .ThenInclude(d => d.Invoice)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            // Thống kê
            ViewBag.TotalSales = product.InvoiceDetails.Sum(d => d.Quantity ?? 1);
            ViewBag.TotalRevenue = product.InvoiceDetails.Sum(d => d.Subtotal ?? 0);
            ViewBag.TotalInvoices = product.InvoiceDetails.Select(d => d.InvoiceId).Distinct().Count();

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewBag.ActiveMenu = "Products";
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Description,Price,Category,StockQuantity,Unit,Brand,CostPrice,IsActive")] Product product, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
                    
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    product.ImageUrl = "/images/products/" + fileName;
                }

                product.CreatedAt = DateTime.Now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.ActiveMenu = "Products";
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,Description,Price,Category,StockQuantity,Unit,Brand,CostPrice,IsActive,ImageUrl")] Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload ảnh mới
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        // Upload ảnh mới
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
                        
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        product.ImageUrl = "/images/products/" + fileName;
                    }

                    product.UpdatedAt = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Xóa ảnh
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}

