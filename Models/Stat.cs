using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Stat
{
    [Key]
    [Column("StatID")]
    public int StatId { get; set; }

    [StringLength(7)]
    [Unicode(false)]
    public string Month { get; set; } = null!;

    [Column(TypeName = "decimal(12, 2)")]
    public decimal? TotalRevenue { get; set; }

    public int? TotalAppointments { get; set; }

    [Column("TopServiceID")]
    public int? TopServiceId { get; set; }

    [Column("TopStaffID")]
    public int? TopStaffId { get; set; }

    [ForeignKey("TopServiceId")]
    [InverseProperty("Stats")]
    public virtual Service? TopService { get; set; }

    [ForeignKey("TopStaffId")]
    [InverseProperty("Stats")]
    public virtual User? TopStaff { get; set; }
}
