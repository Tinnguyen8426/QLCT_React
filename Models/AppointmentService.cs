using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyTiemCatToc.Models;

public partial class AppointmentService
{
    [Key]
    [Column("AppointmentServiceID")]
    public int AppointmentServiceId { get; set; }

    [Column("AppointmentID")]
    public int AppointmentId { get; set; }

    [Column("ServiceID")]
    public int ServiceId { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("AppointmentServices")]
    public virtual Appointment Appointment { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("AppointmentServices")]
    public virtual Service Service { get; set; } = null!;
}
