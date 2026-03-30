using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyTiemCatToc.Helpers;
using QuanLyTiemCatToc.Models;
using QuanLyTiemCatToc.Models.ViewModels;

namespace QuanLyTiemCatToc.Services;

public class StorefrontComposer
{
    private readonly QuanLyTiemCatTocContext _context;

    public StorefrontComposer(QuanLyTiemCatTocContext context)
    {
        _context = context;
    }

    public async Task<StorefrontViewModel> BuildHomepageAsync(ISession session)
    {
        var viewModel = new StorefrontViewModel
        {
            FeaturedProducts = await GetFeaturedProductsAsync(4),
            BestSellers = await GetBestSellersAsync(4),
            Products = await GetLatestProductsAsync(6),
            Categories = await GetActiveCategoriesAsync(),
            Cart = BuildCart(session),
            IsCompact = true
        };

        return viewModel;
    }

    public async Task<StorefrontViewModel> BuildStoreAsync(ISession session, string? category, string? keyword)
    {
        var viewModel = new StorefrontViewModel
        {
            FeaturedProducts = await GetFeaturedProductsAsync(5),
            BestSellers = await GetBestSellersAsync(6),
            Categories = await GetActiveCategoriesAsync(),
            Cart = BuildCart(session),
            SelectedCategory = category,
            Keyword = keyword,
            IsCompact = false
        };

        var productsQuery = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive == true && p.StockQuantity > 0);

        if (!string.IsNullOrWhiteSpace(category))
        {
            productsQuery = productsQuery.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            productsQuery = productsQuery.Where(p =>
                p.ProductName.Contains(keyword) ||
                (p.Description != null && p.Description.Contains(keyword)) ||
                (p.Brand != null && p.Brand.Contains(keyword)));
        }

        var products = await productsQuery
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        viewModel.Products = products.Select(MapProduct).ToList();
        return viewModel;
    }

    public async Task<CustomerProductsViewModel> BuildCustomerProductsAsync(ISession session, User user, Customer? customer)
    {
        var viewModel = new CustomerProductsViewModel
        {
            User = user,
            Customer = customer,
            Cart = BuildCart(session)
        };

        if (customer == null)
        {
            return viewModel;
        }

        var purchased = await _context.InvoiceDetails
            .AsNoTracking()
            .Include(d => d.Product)
            .Include(d => d.Invoice)
            .Where(d => d.ProductId != null && d.Invoice.CustomerId == customer.CustomerId)
            .GroupBy(d => d.ProductId!.Value)
            .Select(g => new
            {
                Product = g.First().Product!,
                Quantity = g.Sum(x => x.Quantity ?? 0),
                LastPurchase = g.Max(x => x.Invoice.CreatedAt ?? DateTime.MinValue)
            })
            .OrderByDescending(x => x.LastPurchase)
            .Take(12)
            .ToListAsync();

        viewModel.PurchasedProducts = purchased.Select(p =>
        {
            var card = MapProduct(p.Product);
            card.PurchasedQuantity = p.Quantity;
            card.LastPurchasedAt = p.LastPurchase == DateTime.MinValue ? null : p.LastPurchase;
            card.IsHighlighted = true;
            return card;
        }).ToList();

        var excludeIds = new HashSet<int>(viewModel.PurchasedProducts.Select(p => p.ProductId));
        viewModel.RecommendedProducts = await GetBestSellersAsync(6, excludeIds);

        return viewModel;
    }

    private CartViewModel BuildCart(ISession session)
    {
        var cartItems = session.GetCart();
        return new CartViewModel
        {
            Items = cartItems
        };
    }

    private async Task<List<ProductCardViewModel>> GetFeaturedProductsAsync(int take)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive == true && p.StockQuantity > 0)
            .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
            .ThenByDescending(p => p.ProductId)
            .Take(take)
            .ToListAsync();

        var cards = products.Select(MapProduct).ToList();
        cards.ForEach(c => c.IsHighlighted = true);
        return cards;
    }

    private async Task<List<ProductCardViewModel>> GetBestSellersAsync(int take, HashSet<int>? excludeIds = null)
    {
        excludeIds ??= new HashSet<int>();

        var sales = await _context.InvoiceDetails
            .AsNoTracking()
            .Where(d => d.ProductId != null)
            .GroupBy(d => d.ProductId!.Value)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity ?? 0)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(take * 3)
            .ToListAsync();

        var productIds = sales
            .Select(s => s.ProductId)
            .Where(id => !excludeIds.Contains(id))
            .Distinct()
            .Take(take)
            .ToList();

        if (!productIds.Any())
        {
            return new List<ProductCardViewModel>();
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync();

        var cards = new List<ProductCardViewModel>();
        foreach (var id in productIds)
        {
            var product = products.FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                cards.Add(MapProduct(product));
            }
        }

        return cards;
    }

    private async Task<List<ProductCardViewModel>> GetLatestProductsAsync(int take)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive == true && p.StockQuantity > 0)
            .OrderByDescending(p => p.ProductId)
            .Take(take)
            .ToListAsync();

        return products.Select(MapProduct).ToList();
    }

    private async Task<List<string>> GetActiveCategoriesAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive == true && !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    private static ProductCardViewModel MapProduct(Product product)
    {
        var fallbackImage = "https://images.unsplash.com/photo-1501426026826-31c667bdf23d?auto=format&fit=crop&w=800&q=80";
        return new ProductCardViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Description = product.Description,
            Category = product.Category,
            Brand = product.Brand,
            ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? fallbackImage : product.ImageUrl,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };
    }
}
