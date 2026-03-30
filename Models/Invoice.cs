using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Invoice
{
    [Key]
    [Column("InvoiceID")]
    public int InvoiceId { get; set; }

    [Column("AppointmentID")]
    public int? AppointmentId { get; set; }

    [Column("CustomerID")]
    public int? CustomerId { get; set; }

    [Column("StaffID")]
    public int? StaffId { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal? Discount { get; set; }

    [Column(TypeName = "decimal(13, 2)")]
    public decimal? FinalAmount { get; set; }

    [StringLength(20)]
    public string? PaymentMethod { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [StringLength(30)]
    public string? FulfillmentStatus { get; set; }

    [StringLength(255)]
    public string? ShippingAddress { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("Invoices")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Invoices")]
    public virtual Customer? Customer { get; set; }

    [InverseProperty("Invoice")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [ForeignKey("StaffId")]
    [InverseProperty("Invoices")]
    public virtual User? Staff { get; set; }
}
