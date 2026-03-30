using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Route("api/cart")]
public class CartApiController : ControllerBase
{
    private readonly QuanLyTiemCatTocContext _context;

    public CartApiController(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    // Cart is stored in session via CartSessionExtensions
    // For React SPA, we keep same session-based approach

    [HttpGet]
    public IActionResult GetCart()
    {
        var items = HttpContext.Session.GetCart();
        var totalQuantity = items.Sum(i => i.Quantity);
        var totalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        return Ok(new
        {
            items = items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.ImageUrl,
                i.UnitPrice,
                i.Quantity,
                lineTotal = i.UnitPrice * i.Quantity
            }),
            totalQuantity,
            totalAmount
        });
    }

    public record AddToCartRequest(int ProductId, int Quantity = 1);

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null || product.IsActive == false)
            return NotFound(new { message = "Không tìm thấy sản phẩm." });

        var qty = Math.Max(1, request.Quantity);
        var cart = HttpContext.Session.GetCart();

        var existing = cart.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            cart.Add(new CartItemViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ImageUrl = product.ImageUrl,
                UnitPrice = product.Price,
                Quantity = qty
            });
        }

        HttpContext.Session.SaveCart(cart);
        var totalQty = cart.Sum(i => i.Quantity);

        return Ok(new
        {
            success = true,
            message = $"Đã thêm {product.ProductName} vào giỏ hàng.",
            cartQuantity = totalQty
        });
    }

    public record UpdateCartRequest(int ProductId, int Quantity);

    [HttpPut("update")]
    public IActionResult Update([FromBody] UpdateCartRequest request)
    {
        var cart = HttpContext.Session.GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null)
            return NotFound(new { message = "Sản phẩm không có trong giỏ." });

        if (request.Quantity <= 0)
        {
            cart.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(request.Quantity, 20);
        }

        HttpContext.Session.SaveCart(cart);
        return Ok(new { success = true, cartQuantity = cart.Sum(i => i.Quantity) });
    }

    [HttpDelete("remove/{productId:int}")]
    public IActionResult Remove(int productId)
    {
        var cart = HttpContext.Session.GetCart();
        cart.RemoveAll(i => i.ProductId == productId);
        HttpContext.Session.SaveCart(cart);
        return Ok(new { success = true, cartQuantity = cart.Sum(i => i.Quantity) });
    }

    [HttpDelete("clear")]
    public IActionResult Clear()
    {
        HttpContext.Session.SaveCart(new List<CartItemViewModel>());
        return Ok(new { success = true, cartQuantity = 0 });
    }

    public record CheckoutRequest(string FullName, string Phone, string Address, string Method);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var cart = HttpContext.Session.GetCart();
        if (!cart.Any())
            return BadRequest(new { message = "Giỏ hàng trống." });

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ tên." });
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

        // Create invoice for product order
        var total = cart.Sum(i => i.UnitPrice * i.Quantity);

        var invoice = new Invoice
        {
            Total = total,
            FinalAmount = total,
            PaymentMethod = request.Method == "VietQR" ? "Transfer" : "Cash",
            CreatedAt = DateTime.Now,
            ShippingAddress = request.Address?.Trim(),
            FulfillmentStatus = ProductOrderStatusHelper.Pending
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var item in cart)
        {
            _context.InvoiceDetails.Add(new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.UnitPrice * item.Quantity
            });
        }
        await _context.SaveChangesAsync();

        HttpContext.Session.SaveCart(new List<CartItemViewModel>());

        return Ok(new
        {
            success = true,
            message = "Đặt hàng thành công!",
            invoiceId = invoice.InvoiceId
        });
    }
}
