using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/upload")]
public class UploadApiController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadApiController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("image")]
    [Authorize(AuthenticationSchemes = "CookieAuth,Bearer")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn một file ảnh hợp lệ." });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { message = "File tải lên không phải là hình ảnh." });

        try
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "images");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"img_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Tạo absolute URL (bao gồm giao thức và Domain/IP thực tế) giúp mọi thiết bị đều có thể xem ảnh.
            var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            var absoluteUrl = $"{baseUrl}/uploads/images/{fileName}";

            return Ok(new 
            { 
                message = "Tải ảnh thành công", 
                url = absoluteUrl,
                imageUrl = absoluteUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu file", error = ex.Message });
        }
    }
}
