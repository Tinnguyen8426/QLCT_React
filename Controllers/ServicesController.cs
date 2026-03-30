using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Controllers
{
    public class ServicesController : Controller
    {
        private readonly QuanLyTiemCatTocContext _context;

        public ServicesController(QuanLyTiemCatTocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category, string? sort)
        {
            var query = _context.Services.Where(s => s.IsActive == true).AsQueryable();

            if (!string.IsNullOrEmpty(category)) query = query.Where(s => s.Category == category);

            query = sort switch
            {
                "price" => query.OrderBy(s => s.Price),
                "duration" => query.OrderBy(s => s.Duration),
                _ => query.OrderBy(s => s.ServiceName)
            };

            ViewBag.Categories = await _context.Services.Select(s => s.Category).Distinct().ToListAsync();
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedSort = sort;
            return View(await query.ToListAsync());
        }
    }
}


