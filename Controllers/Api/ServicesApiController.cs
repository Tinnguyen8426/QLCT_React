using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/services")]
public class ServicesApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public ServicesApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category)
    {
        var query = _context.Services.Where(s => s.IsActive != false);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        var services = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.ServiceName)
            .Select(s => new
            {
                s.ServiceId,
                s.ServiceName,
                s.Description,
                s.Price,
                s.Duration,
                s.Category,
                s.ImageUrl
            })
            .ToListAsync();

        var categories = await _context.Services
            .Where(s => s.IsActive != false && s.Category != null)
            .Select(s => s.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(new { services, categories });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var service = await _context.Services
            .Where(s => s.ServiceId == id && s.IsActive != false)
            .Select(s => new
            {
                s.ServiceId,
                s.ServiceName,
                s.Description,
                s.Price,
                s.Duration,
                s.Category,
                s.ImageUrl
            })
            .FirstOrDefaultAsync();

        if (service == null) return NotFound(new { message = "Không tìm thấy dịch vụ." });
        return Ok(service);
    }
}
