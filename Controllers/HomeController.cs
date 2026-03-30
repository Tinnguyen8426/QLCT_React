using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Services;

namespace QuanLyTiemCatToc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly StorefrontComposer _storefrontComposer;

        public HomeController(ILogger<HomeController> logger, StorefrontComposer storefrontComposer)
        {
            _logger = logger;
            _storefrontComposer = storefrontComposer;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await _storefrontComposer.BuildHomepageAsync(HttpContext.Session);
            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
