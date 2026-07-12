using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<DataPoint> DataPoints => Set<DataPoint>();
    public DbSet<DataSource> DataSources => Set<DataSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
