using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Appointment
{
    [Key]
    [Column("AppointmentID")]
    public int AppointmentId { get; set; }

    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("StaffID")]
    public int? StaffId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Appointment")]
    public virtual ICollection<AppointmentDetail> AppointmentDetails { get; set; } = new List<AppointmentDetail>();

    [ForeignKey("CustomerId")]
    [InverseProperty("Appointments")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Appointment")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [ForeignKey("StaffId")]
    [InverseProperty("Appointments")]
    public virtual User? Staff { get; set; }
}
