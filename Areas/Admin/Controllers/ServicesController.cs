using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;
        private readonly IWebHostEnvironment _env;

        public ServicesController(QuanLyTiemCatTocContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActiveMenu = "Services";
            base.OnActionExecuting(context);
        }

        // GET: Admin/Services
        public async Task<IActionResult> Index(string? category, bool? isActive, int page = 1, int pageSize = 10)
        {
            
            var query = _context.Services.AsQueryable();

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.Category == category);
                ViewBag.SelectedCategory = category;
            }

            // Lọc theo trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
                ViewBag.SelectedStatus = isActive.Value;
            }

            var services = await query.OrderBy(s => s.Category).ThenBy(s => s.ServiceName).ToPagedListAsync(page, pageSize);

            // Lấy danh sách categories
            ViewBag.Categories = await _context.Services
                .Select(s => s.Category)
                .Distinct()
                .ToListAsync();

            return View(services);
        }

        // GET: Admin/Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            
            if (id == null) return NotFound();

            var service = await _context.Services
                .Include(s => s.AppointmentDetails)
                    .ThenInclude(d => d.Appointment)
                .Include(s => s.InvoiceDetails)
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null) return NotFound();

            // Thống kê
            var appointmentCount = service.AppointmentDetails
                .Select(d => d.AppointmentId)
                .Distinct()
                .Count();
            ViewBag.TotalAppointments = appointmentCount;
            ViewBag.TotalRevenue = service.InvoiceDetails.Sum(d => d.Subtotal ?? 0);
            ViewBag.TimesUsed = service.InvoiceDetails.Sum(d => d.Quantity ?? 1);

            return View(service);
        }

        // GET: Admin/Services/Create
        public IActionResult Create()
        {
                        return View();
        }

        // POST: Admin/Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceName,Description,Price,Duration,Category,IsActive")] Service service, IFormFile? ImageFile)
        {
            
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(_env.WebRootPath, "images", "services");
                    
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    service.ImageUrl = "/images/services/" + fileName;
                }

                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Admin/Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            
            if (id == null) return NotFound();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            return View(service);
        }

        // POST: Admin/Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,ServiceName,Description,Price,Duration,Category,IsActive,ImageUrl")] Service service, IFormFile? ImageFile)
        {
            
            if (id != service.ServiceId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload ảnh mới
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ
                        if (!string.IsNullOrEmpty(service.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_env.WebRootPath, service.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        // Upload ảnh mới
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(_env.WebRootPath, "images", "services");
                        
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        service.ImageUrl = "/images/services/" + fileName;
                    }

                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.ServiceId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // POST: Admin/Services/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                // Xóa ảnh
                if (!string.IsNullOrEmpty(service.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, service.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa dịch vụ thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.ServiceId == id);
        }
    }
}
