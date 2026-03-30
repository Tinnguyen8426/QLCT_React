using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyTiemCatToc.Helpers;

public static class ProductOrderStatusHelper
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Packing = "Packing";
    public const string Shipping = "Shipping";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    private static readonly string[] Statuses = { Pending, Confirmed, Packing, Shipping, Completed, Cancelled };

    public static IReadOnlyList<string> All => Statuses;

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return Pending;
        return Statuses.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase)) ?? Pending;
    }

    public static string Translate(string status)
    {
        return status switch
        {
            Pending => "Chờ xác nhận",
            Confirmed => "Đã xác nhận",
            Packing => "Đang chuẩn bị",
            Shipping => "Đang giao",
            Completed => "Hoàn tất",
            Cancelled => "Đã hủy",
            _ => status
        };
    }
}
