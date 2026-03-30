using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public ProductsApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = _context.Products.Where(p => p.IsActive != false);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.ProductName, $"%{kw}%") ||
                (p.Brand != null && EF.Functions.Like(p.Brand, $"%{kw}%")) ||
                (p.Description != null && EF.Functions.Like(p.Description, $"%{kw}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var total = await query.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.Category,
                p.Brand,
                p.ImageUrl,
                p.Price,
                p.StockQuantity,
                isLowStock = p.StockQuantity <= 5
            })
            .ToListAsync();

        var categories = await _context.Products
            .Where(p => p.IsActive != false && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(new { products, categories, page, pageSize, total, totalPages });
    }

    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers()
    {
        // Top products by invoice detail quantity
        var bestSellers = await _context.InvoiceDetails
            .Where(d => d.ProductId != null)
            .GroupBy(d => d.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(d => d.Quantity) })
            .OrderByDescending(x => x.TotalQty)
            .Take(5)
            .Join(_context.Products, x => x.ProductId, p => p.ProductId,
                (x, p) => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.ImageUrl,
                    p.Price,
                    x.TotalQty
                })
            .ToListAsync();

        return Ok(bestSellers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .Where(p => p.ProductId == id && p.IsActive != false)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.Category,
                p.Brand,
                p.ImageUrl,
                p.Price,
                p.StockQuantity,
                p.Unit
            })
            .FirstOrDefaultAsync();

        if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });
        return Ok(product);
    }
}
