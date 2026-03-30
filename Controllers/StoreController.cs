using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyTiemCatToc.Services;

namespace QuanLyTiemCatToc.Controllers;

public class StoreController : Controller
{
    private readonly StorefrontComposer _storefrontComposer;

    public StoreController(StorefrontComposer storefrontComposer)
    {
        _storefrontComposer = storefrontComposer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category, string? keyword)
    {
        var model = await _storefrontComposer.BuildStoreAsync(HttpContext.Session, category, keyword);
        return View(model);
    }
}
