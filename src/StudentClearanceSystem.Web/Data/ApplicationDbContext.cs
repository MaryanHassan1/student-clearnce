using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClearanceDepartment> ClearanceDepartments => Set<ClearanceDepartment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<ClearanceRequest> ClearanceRequests => Set<ClearanceRequest>();
    public DbSet<ClearanceItem> ClearanceItems => Set<ClearanceItem>();
    public DbSet<GraduationRecord> GraduationRecords => Set<GraduationRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Student>(entity =>
        {
            entity.HasIndex(s => s.RegistrationNumber).IsUnique();
            entity.HasIndex(s => s.UserId).IsUnique();

            entity.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Faculty)
                .WithMany(f => f.Students)
                .HasForeignKey(s => s.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Department)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Department>(entity =>
        {
            entity.HasOne(d => d.Faculty)
                .WithMany(f => f.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClearanceRequest>(entity =>
        {
            entity.HasOne(r => r.Student)
                .WithMany(s => s.ClearanceRequests)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.CompletedByUser)
                .WithMany()
                .HasForeignKey(r => r.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClearanceItem>(entity =>
        {
            entity.HasOne(i => i.ClearanceRequest)
                .WithMany(r => r.ClearanceItems)
                .HasForeignKey(i => i.ClearanceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.ClearanceDepartment)
                .WithMany(cd => cd.ClearanceItems)
                .HasForeignKey(i => i.ClearanceDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.ReviewedByUser)
                .WithMany()
                .HasForeignKey(i => i.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GraduationRecord>(entity =>
        {
            entity.HasIndex(g => g.ClearanceRequestId).IsUnique();

            entity.HasOne(g => g.Student)
                .WithMany()
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.ClearanceRequest)
                .WithOne(r => r.GraduationRecord)
                .HasForeignKey<GraduationRecord>(g => g.ClearanceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.ApprovedByUser)
                .WithMany()
                .HasForeignKey(g => g.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
