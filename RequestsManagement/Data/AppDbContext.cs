
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Models;

namespace RequestsManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Request> Requests { get; set; }

    public DbSet<RequestStatusHistory> RequestStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(r => r.OrganizationName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(r => r.RowVersion)
                  .IsRowVersion();

            // Sorting and filtering by creation date
            entity.HasIndex(r => r.CreatedAt);

            // Filtering by status and sorting by date
            entity.HasIndex(r => new { r.Status, r.CreatedAt });

            // Filtering by priority
            entity.HasIndex(r => r.Priority);

            // Filtering by organization
            entity.HasIndex(r => r.OrganizationName);

            entity.HasIndex(r => new
            {
                r.OrganizationName,
                r.Priority,
                r.CreatedAt,
                r.Id
            });


            // Filtering by assigned employee
            entity.HasIndex(r => r.AssignedTo);
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.HasOne(h => h.Request)
                  .WithMany()
                  .HasForeignKey(h => h.RequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(h => h.ChangedBy)
                  .HasMaxLength(100);

            // Fetching request history in date order
            entity.HasIndex(h => new { h.RequestId, h.ChangedAt });
        });
    }
}
