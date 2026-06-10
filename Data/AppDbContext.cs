using Microsoft.EntityFrameworkCore;
using CanaryTracker.Models;

namespace CanaryTracker.Data;

public class AppDbContext : DbContext
{
    public DbSet<Canary> Canaries => Set<Canary>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=canary.db");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Canary>().HasIndex(c => c.Name);
        model.Entity<Alert>().HasIndex(a => a.DetectedAt);
    }
}
