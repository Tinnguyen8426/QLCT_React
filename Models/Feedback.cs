using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Feedback
{
    [Key]
    [Column("FeedbackID")]
    public int FeedbackId { get; set; }

    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("ServiceID")]
    public int? ServiceId { get; set; }

    [Column("StaffID")]
    public int? StaffId { get; set; }

    public int? Rating { get; set; }

    [StringLength(255)]
    public string? Comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Feedbacks")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("Feedbacks")]
    public virtual Service? Service { get; set; }

    [ForeignKey("StaffId")]
    [InverseProperty("Feedbacks")]
    public virtual User? Staff { get; set; }
}
