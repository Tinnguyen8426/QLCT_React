using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyTiemCatToc.Models.ViewModels;

public class ProfileViewModel
{
    public int UserId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [Url, StringLength(255)]
    public string? AvatarUrl { get; set; }

    [StringLength(100)]
    public string? RoleName { get; set; }

    public DateTime? CreatedAt { get; set; }

    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string? ConfirmPassword { get; set; }
}
