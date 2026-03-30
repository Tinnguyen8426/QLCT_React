using System;
using System.Collections.Generic;
using QuanLyTiemCatToc.Models;
using X.PagedList;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class ProductOrderListViewModel
{
    public required IPagedList<Invoice> Orders { get; set; }
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
