using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuanLyTiemCatToc.Models.ViewModels;

namespace QuanLyTiemCatToc.Helpers;

public static class CartSessionExtensions
{
    private const string CartSessionKey = "SHOPPING_CART";

    public static List<CartItemViewModel> GetCart(this ISession session)
    {
        var data = session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(data))
        {
            return new List<CartItemViewModel>();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<CartItemViewModel>>(data);
            return items ?? new List<CartItemViewModel>();
        }
        catch
        {
            return new List<CartItemViewModel>();
        }
    }

    public static void SaveCart(this ISession session, List<CartItemViewModel> items)
    {
        var serialized = JsonSerializer.Serialize(items);
        session.SetString(CartSessionKey, serialized);
    }

    public static void ClearCart(this ISession session)
    {
        session.Remove(CartSessionKey);
    }
}
