using GermanyDashboard.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Persistence;

public class DbMigrator
{
    private readonly AppDbContext _db;
    private readonly DbSeeder _seeder;

    public DbMigrator(AppDbContext db, DbSeeder seeder)
    {
        _db = db;
        _seeder = seeder;
    }

    public async Task MigrateAndSeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);
        await _seeder.SeedAsync(ct);
    }
}
