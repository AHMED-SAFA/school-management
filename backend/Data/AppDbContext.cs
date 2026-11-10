using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes { get; set; } = null!;
    public DbSet<Hold> Holds { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.TotalSeats).IsRequired();
        });

        modelBuilder.Entity<Hold>(entity =>
        {
            entity.HasKey(e => e.HoldId);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();
            entity.Property(e => e.ClassId).IsRequired();
            entity.Property(e => e.StudentId).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();

            entity.HasIndex(e => new { e.ClassId, e.StudentId, e.Status });
            entity.HasIndex(e => new { e.ClassId, e.Status });
            entity.HasIndex(e => e.StudentId);

            entity.HasOne(e => e.SchoolClass)
                .WithMany()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
