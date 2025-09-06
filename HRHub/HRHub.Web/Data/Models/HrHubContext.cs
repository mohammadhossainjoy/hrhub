using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Data.Models;

public partial class HrHubContext : DbContext
{
    public HrHubContext() { }

    public HrHubContext(DbContextOptions<HrHubContext> options) : base(options) { }

    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Department> Departments { get; set; }
    public virtual DbSet<Designation> Designations { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<Leaf> Leaves { get; set; }
    public virtual DbSet<LeaveType> LeaveTypes { get; set; }

    // NEW: DbSet for Promotions
 
    public virtual DbSet<Promotion> Promotions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:HrHubConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Attendance ---
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C26F7EE3C");
            entity.ToTable("Attendance");

            entity.HasIndex(e => new { e.EmployeeId, e.Date }, "UQ_Attendance").IsUnique();

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Employees");
        });

        // --- Company ---
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("PK__Companie__2D971CACF07EB359");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        // --- Department ---
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED6AC5C720");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Company).WithMany(p => p.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Departments_Companies");
        });

        // --- Designation ---
        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.DesignationId).HasName("PK__Designat__BABD60DE04591811");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        // --- Employee ---
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

            entity.HasOne(d => d.Company).WithMany(p => p.Employees)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Companies");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Departments");

            entity.HasOne(d => d.Designation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DesignationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Designations");
        });

        // --- Leave (Leaf) ---
        modelBuilder.Entity<Leaf>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__Leaves__796DB959CB8CDFE6");
            entity.Property(e => e.Days).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");

            entity.HasOne(d => d.ApprovedByEmployee).WithMany(p => p.LeafApprovedByEmployees)
                .HasForeignKey(d => d.ApprovedByEmployeeId)
                .HasConstraintName("FK_Leaves_ApprovedBy");

            entity.HasOne(d => d.Employee).WithMany(p => p.LeafEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_Employees");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.Leaves)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_Types");
        });

        // --- LeaveType ---
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.LeaveTypeId).HasName("PK__LeaveTyp__43BE8F14E3C388E2");
            entity.Property(e => e.AnnualQuota).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        // --- Promotion (NEW) ---
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId);
            entity.ToTable("Promotions");

            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(e => e.Employee)
                  .WithMany() // or .WithMany(emp => emp.Promotions) if you add ICollection<Promotion> on Employee
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Promotions_Employees");

            entity.HasOne(e => e.OldDesignation)
                  .WithMany()
                  .HasForeignKey(e => e.OldDesignationId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Promotions_Desig_Old");

            entity.HasOne(e => e.NewDesignation)
                  .WithMany()
                  .HasForeignKey(e => e.NewDesignationId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Promotions_Desig_New");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
