using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class AppointmentDetail
{
    [Key]
    [Column("AppointmentDetailID")]
    public int AppointmentDetailId { get; set; }

    [Column("AppointmentID")]
    public int AppointmentId { get; set; }

    [Column("ServiceID")]
    public int ServiceId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal UnitPrice { get; set; }

    public int? Duration { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("AppointmentDetails")]
    public virtual Appointment Appointment { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("AppointmentDetails")]
    public virtual Service Service { get; set; } = null!;
}
