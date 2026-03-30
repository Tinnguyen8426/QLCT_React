using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;

namespace QuanLyTiemCatToc.Controllers;

public class CartController : Controller
{
    private readonly QuanLyTiemCatTocContext _context;

    public CartController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new CartViewModel
        {
            Items = HttpContext.Session.GetCart()
        };
        var (user, customer) = await ResolveLoggedInCustomerAsync();
        ViewBag.ContactName = customer?.FullName ?? user?.FullName ?? string.Empty;
        ViewBag.ContactPhone = customer?.Phone ?? user?.Phone ?? string.Empty;
        ViewBag.ContactAddress = HttpContext.Session.GetString(SessionOrderStatusKey + "_ADDRESS") ?? string.Empty;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive == true && p.StockQuantity > 0);

        if (product == null)
        {
            return BuildCartResponse(false, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
        }

        var stockLimit = Math.Max(1, Math.Min(product.StockQuantity, 99));
        var productImage = string.IsNullOrWhiteSpace(product.ImageUrl)
            ? "https://images.unsplash.com/photo-1501426026826-31c667bdf23d?auto=format&fit=crop&w=800&q=80"
            : product.ImageUrl;
        quantity = Math.Clamp(quantity, 1, stockLimit);

        var cart = HttpContext.Session.GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);

        if (item == null)
        {
            cart.Add(new CartItemViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ImageUrl = productImage,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }
        else
        {
            var newQuantity = Math.Min(item.Quantity + quantity, stockLimit);
            item.Quantity = Math.Clamp(newQuantity, 1, stockLimit);
        }

        HttpContext.Session.SaveCart(cart);
        return BuildCartResponse(true, $"{product.ProductName} đã được thêm vào giỏ hàng.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var cart = HttpContext.Session.GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            return BuildCartResponse(false, "Không tìm thấy sản phẩm trong giỏ hàng.", redirectToCart: true);
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        var stockLimit = product != null
            ? Math.Max(1, Math.Min(product.StockQuantity, 99))
            : 99;

        item.Quantity = Math.Clamp(quantity, 1, stockLimit);
        HttpContext.Session.SaveCart(cart);

        var message = product == null
            ? "Đã cập nhật số lượng sản phẩm."
            : $"Đã cập nhật số lượng (tối đa hiện tại: {stockLimit}).";
        return BuildCartResponse(true, message, redirectToCart: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var cart = HttpContext.Session.GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Remove(item);
            HttpContext.Session.SaveCart(cart);
            return BuildCartResponse(true, "Đã xóa sản phẩm khỏi giỏ hàng.", redirectToCart: true);
        }

        return BuildCartResponse(false, "Không tìm thấy sản phẩm cần xóa.", redirectToCart: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.ClearCart();
        return BuildCartResponse(true, "Đã dọn sạch giỏ hàng.", redirectToCart: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string fullName, string phone, string address, string? method)
    {
        var cartItems = HttpContext.Session.GetCart();
        if (!cartItems.Any())
        {
            return BuildCartResponse(false, "Giỏ hàng trống. Hãy thêm sản phẩm trước khi thanh toán.", redirectToCart: true);
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
        {
            return BuildCartResponse(false, "Vui lòng nhập họ tên, số điện thoại và địa chỉ giao hàng.", redirectToCart: true);
        }

        var customer = await ResolveOrCreateCustomerAsync(fullName, phone);
        var cartView = new CartViewModel { Items = cartItems };
        var preferredMethod = string.IsNullOrWhiteSpace(method) ? "Chờ liên hệ" : method.Trim();
        if (preferredMethod.Length > 20)
        {
            preferredMethod = preferredMethod[..20];
        }
        var shippingAddress = address.Trim();
        if (shippingAddress.Length > 255)
        {
            shippingAddress = shippingAddress[..255];
        }

        var invoice = new Invoice
        {
            CustomerId = customer.CustomerId,
            ShippingAddress = shippingAddress,
            FulfillmentStatus = ProductOrderStatusHelper.Pending,
            Total = cartView.TotalAmount,
            Discount = 0,
            FinalAmount = cartView.TotalAmount,
            PaymentMethod = preferredMethod,
            CreatedAt = DateTime.Now
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var item in cartItems)
        {
            _context.InvoiceDetails.Add(new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ProductId = item.ProductId,
                ItemType = "Product",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.LineTotal
            });
        }

        await _context.SaveChangesAsync();
        HttpContext.Session.ClearCart();

        SaveOrderStatusSession(new OrderStatusInfo
        {
            InvoiceId = invoice.InvoiceId,
            ShippingAddress = shippingAddress,
            ContactName = customer.FullName,
            ContactPhone = customer.Phone ?? phone.Trim()
        });

        var statusUrl = Url.Action(nameof(OrderStatus), new { id = invoice.InvoiceId });
        TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn #{invoice.InvoiceId}. Bạn có thể xem trạng thái đơn tại đây.";
        return BuildCartResponse(true, "Đặt hàng thành công! Đang chuyển tới trang trạng thái đơn hàng.", redirectToCart: true, redirectUrl: statusUrl);
    }

    private IActionResult BuildCartResponse(bool success, string message, bool redirectToCart = false, string? redirectUrl = null)
    {
        var cart = BuildCartViewModel();

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success,
                message,
                cartQuantity = cart.TotalQuantity,
                cartTotal = cart.TotalAmount,
                cartTotalFormatted = cart.TotalAmount.ToString("N0"),
                redirectUrl
            });
        }

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect(redirectUrl);
        }

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrWhiteSpace(referer) && !redirectToCart)
        {
            return Redirect(referer);
        }

        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private CartViewModel BuildCartViewModel()
    {
        return new CartViewModel
        {
            Items = HttpContext.Session.GetCart()
        };
    }

    private async Task<Customer> ResolveOrCreateCustomerAsync(string fullName, string phone)
    {
        var normalizedName = fullName.Trim();
        var normalizedPhone = phone.Trim();

        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == normalizedPhone);
        if (existing == null)
        {
            var customer = new Customer
            {
                FullName = normalizedName,
                Phone = normalizedPhone,
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        if (!string.Equals(existing.FullName, normalizedName, StringComparison.Ordinal))
        {
            existing.FullName = normalizedName;
            _context.Customers.Update(existing);
            await _context.SaveChangesAsync();
        }

        return existing;
    }

    private async Task<(User? user, Customer? customer)> ResolveLoggedInCustomerAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
        {
            return (null, null);
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive == true);

        if (user == null)
        {
            return (null, null);
        }

        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == user.Phone);
        }
        if (customer == null && !string.IsNullOrWhiteSpace(user.Email))
        {
            var normalizedEmail = user.Email.Trim().ToLowerInvariant();
            customer = await _context.Customers.FirstOrDefaultAsync(c =>
                c.Email != null && c.Email.ToLower() == normalizedEmail);
        }
        if (customer == null && !string.IsNullOrWhiteSpace(user.Phone))
        {
            customer = new Customer
            {
                FullName = user.FullName,
                Phone = user.Phone.Trim(),
                Email = user.Email,
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        return (user, customer);
    }

    private const string SessionOrderStatusKey = "LATEST_ORDER_STATUS";

    private void SaveOrderStatusSession(OrderStatusInfo info)
    {
        HttpContext.Session.SetString(SessionOrderStatusKey, JsonSerializer.Serialize(info));
        if (!string.IsNullOrWhiteSpace(info.ShippingAddress))
        {
            HttpContext.Session.SetString(SessionOrderStatusKey + "_ADDRESS", info.ShippingAddress);
        }
    }

    private OrderStatusInfo? GetOrderStatusSession()
    {
        var json = HttpContext.Session.GetString(SessionOrderStatusKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OrderStatusInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> OrderStatus(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceDetails).ThenInclude(d => d.Product)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var sessionInfo = GetOrderStatusSession();
        var normalizedStatus = ProductOrderStatusHelper.Normalize(invoice.FulfillmentStatus);
        var shippingAddress = sessionInfo?.InvoiceId == id && !string.IsNullOrWhiteSpace(sessionInfo.ShippingAddress)
            ? sessionInfo.ShippingAddress
            : invoice.ShippingAddress;
        var contactName = sessionInfo?.InvoiceId == id && !string.IsNullOrWhiteSpace(sessionInfo.ContactName)
            ? sessionInfo.ContactName
            : invoice.Customer?.FullName;
        var contactPhone = sessionInfo?.InvoiceId == id && !string.IsNullOrWhiteSpace(sessionInfo.ContactPhone)
            ? sessionInfo.ContactPhone
            : invoice.Customer?.Phone;

        var model = new OrderStatusViewModel
        {
            Invoice = invoice,
            ShippingAddress = shippingAddress,
            ContactName = contactName,
            ContactPhone = contactPhone,
            FulfillmentStatus = normalizedStatus
        };

        return View(model);
    }

    private class OrderStatusInfo
    {
        public int InvoiceId { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
    }
}
