using Microsoft.EntityFrameworkCore;
using WorkItemTracker.Domain.Entities;

namespace WorkItemTracker.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItem>(builder =>
        {
            builder.ToTable("WorkItems");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Title)
                .IsRequired()
                .HasMaxLength(WorkItem.TitleMaxLength);

            builder.Property(w => w.Description);

            builder.Property(w => w.Status)
                .HasConversion<string>() // store enum as readable text ("Todo", "InProgress", "Done")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(w => w.CreatedAt).IsRequired();

            // Search and filter are the primary read patterns, so index the columns they hit.
            builder.HasIndex(w => w.Status);
            builder.HasIndex(w => w.Title);
        });
    }
}
