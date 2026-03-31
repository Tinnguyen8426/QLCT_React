using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuanLyTiemCatToc.Models;

public partial class QuanLyTiemCatTocContext : DbContext
{
    public QuanLyTiemCatTocContext()
    {
    }

    public QuanLyTiemCatTocContext(DbContextOptions<QuanLyTiemCatTocContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentDetail> AppointmentDetails { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Stat> Stats { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=QuanLyTiemCatToc;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA215E4CD51");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Customer).WithMany(p => p.Appointments).HasConstraintName("FK_Appointments_Customers");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Appointments_Users");
        });

        modelBuilder.Entity<AppointmentDetail>(entity =>
        {
            entity.HasKey(e => e.AppointmentDetailId).HasName("PK__Appointm__B475AC1545A55219");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentDetails).HasConstraintName("FK_AppDet_Appointments");

            entity.HasOne(d => d.Service).WithMany(p => p.AppointmentDetails).HasConstraintName("FK_AppDet_Services");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B841846C3B");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF6494D697F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks).HasConstraintName("FK_Feedbacks_Customers");

            entity.HasOne(d => d.Service).WithMany(p => p.Feedbacks)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedbacks_Services");

            entity.HasOne(d => d.Staff).WithMany(p => p.Feedbacks)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedbacks_Users");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD59645436F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Discount).HasDefaultValue(0m);
            entity.Property(e => e.FinalAmount).HasComputedColumnSql("([Total]-[Discount])", true);

            entity.HasOne(d => d.Appointment).WithMany(p => p.Invoices)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Invoices_Appointments");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices).HasConstraintName("FK_Invoices_Customers");

            entity.HasOne(d => d.Staff).WithMany(p => p.Invoices)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Invoices_Users");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.InvoiceDetailId).HasName("PK__InvoiceD__1F1578F18E67EFEA");

            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasComputedColumnSql("([Quantity]*[UnitPrice])", true);

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails).HasConstraintName("FK_InvDet_Invoices");

            entity.HasOne(d => d.Product).WithMany(p => p.InvoiceDetails)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_InvDet_Products");

            entity.HasOne(d => d.Service).WithMany(p => p.InvoiceDetails)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InvDet_Services");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD12345678");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AC9FB712C");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB0EA7DAAAAD1");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A838E16B7D5FFF");
        });

        modelBuilder.Entity<Stat>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__Stats__3A162D1E22C63A33");

            entity.Property(e => e.Month).IsFixedLength();

            entity.HasOne(d => d.TopService).WithMany(p => p.Stats).HasConstraintName("FK_Stats_Services");

            entity.HasOne(d => d.TopStaff).WithMany(p => p.Stats).HasConstraintName("FK_Stats_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACB0EA2A55");

            entity.Property(e => e.CommissionPercent).HasDefaultValue(0.0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");

            entity.HasOne(d => d.Shift).WithMany(p => p.Users).HasConstraintName("FK_Users_Shifts");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
