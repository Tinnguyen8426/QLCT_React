using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class Shift
{
    [Key]
    [Column("ShiftID")]
    public int ShiftId { get; set; }

    [StringLength(50)]
    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    [InverseProperty("Shift")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
