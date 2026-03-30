using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Product
{
    [Key]
    [Column("ProductID")]
    public int ProductId { get; set; }

    [StringLength(100)]
    public string ProductName { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Price { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public int StockQuantity { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal? CostPrice { get; set; }

    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
}
