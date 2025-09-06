using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Data.Models._promo_tmp;

public partial class HrHubContext : DbContext
{
    public HrHubContext(DbContextOptions<HrHubContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Designation> Designations { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.DesignationId).HasName("PK__Designat__BABD60DE04591811");

            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F11D413D973");

            entity.HasIndex(e => e.Email, "UQ__Employee__A9D10534C800C03A").IsUnique();

            entity.HasIndex(e => e.EmpNo, "UQ__Employee__AF2D66D2E1623B1B").IsUnique();

            entity.Property(e => e.AspNetUserId).HasMaxLength(450);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EmpNo).HasMaxLength(20);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Designation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DesignationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Designations");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCF4E1365C1");

            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(d => d.Employee).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_Employees");

            entity.HasOne(d => d.NewDesignation).WithMany(p => p.PromotionNewDesignations)
                .HasForeignKey(d => d.NewDesignationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_Desig_New");

            entity.HasOne(d => d.OldDesignation).WithMany(p => p.PromotionOldDesignations)
                .HasForeignKey(d => d.OldDesignationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_Desig_Old");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
