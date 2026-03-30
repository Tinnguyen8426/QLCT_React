using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

[Index("ProductId", Name = "IX_InvoiceDetails_ProductID")]
public partial class InvoiceDetail
{
    [Key]
    [Column("InvoiceDetailID")]
    public int InvoiceDetailId { get; set; }

    [Column("InvoiceID")]
    public int InvoiceId { get; set; }

    [Column("ServiceID")]
    public int? ServiceId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(23, 2)")]
    public decimal? Subtotal { get; set; }

    [Column("ProductID")]
    public int? ProductId { get; set; }

    [StringLength(20)]
    public string? ItemType { get; set; }

    [ForeignKey("InvoiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Invoice Invoice { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ServiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Service? Service { get; set; }
}
