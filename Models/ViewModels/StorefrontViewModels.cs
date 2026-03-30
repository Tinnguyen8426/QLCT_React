using System;
using System.Collections.Generic;
using System.Linq;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class ProductCardViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int? PurchasedQuantity { get; set; }
    public DateTime? LastPurchasedAt { get; set; }
    public bool IsHighlighted { get; set; }

    public bool IsLowStock => StockQuantity <= 5;
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();

    public int TotalQuantity => Items.Sum(i => i.Quantity);

    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}

public class StorefrontViewModel
{
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
    public List<ProductCardViewModel> BestSellers { get; set; } = new();
    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }
    public string? Keyword { get; set; }
    public CartViewModel Cart { get; set; } = new();
    public bool IsCompact { get; set; }
}

public class CustomerProductsViewModel
{
    public User User { get; set; } = null!;
    public Customer? Customer { get; set; }
    public CartViewModel Cart { get; set; } = new();
    public List<ProductCardViewModel> PurchasedProducts { get; set; } = new();
    public List<ProductCardViewModel> RecommendedProducts { get; set; } = new();
}

public class OrderStatusViewModel
{
    public Invoice Invoice { get; set; } = null!;
    public string? ShippingAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string FulfillmentStatus { get; set; } = ProductOrderStatusHelper.Pending;
}
