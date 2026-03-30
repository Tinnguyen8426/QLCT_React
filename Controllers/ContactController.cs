using Microsoft.AspNetCore.Mvc;

namespace QuanLyTiemCatToc.Controllers
{
    public class ContactController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string fullName, string email, string message)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin.");
                return View();
            }

            TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";
            return View();
        }
    }
}

