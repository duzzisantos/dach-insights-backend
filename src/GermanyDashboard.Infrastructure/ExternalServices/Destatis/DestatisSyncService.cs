using GermanyDashboard.Domain.Entities;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GermanyDashboard.Infrastructure.ExternalServices.Destatis;

/// <summary>
/// Pulls configured GENESIS tables and upserts them as DataPoints for an existing Indicator.
/// This is invoked manually (see Program.cs "sync-destatis" command) rather than on a
/// background timer, so an operator always chooses when outbound calls with credentials
/// happen. Table-code-to-indicator mapping is supplied by the caller because it depends on
/// which GENESIS tables the operator has registered access to.
/// </summary>
public class DestatisSyncService
{
    private readonly IGenesisApiClient _client;
    private readonly AppDbContext _db;
    private readonly GenesisApiOptions _options;
    private readonly ILogger<DestatisSyncService> _logger;

    public DestatisSyncService(
        IGenesisApiClient client,
        AppDbContext db,
        IOptions<GenesisApiOptions> options,
        ILogger<DestatisSyncService> logger)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncAsync(IReadOnlyDictionary<string, string> tableCodeToIndicatorSlug, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Skipping Destatis sync: Destatis__Username / Destatis__Password are not set. " +
                "The app will keep serving the seeded demo dataset.");
            return;
        }

        var regionsByName = await _db.Regions.ToDictionaryAsync(r => Normalize(r.Name), r => r, ct);

        foreach (var (tableCode, indicatorSlug) in tableCodeToIndicatorSlug)
        {
            var indicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == indicatorSlug, ct);
            if (indicator is null)
            {
                _logger.LogWarning("No indicator with slug '{Slug}' found, skipping table '{TableCode}'.", indicatorSlug, tableCode);
                continue;
            }

            _logger.LogInformation("Syncing GENESIS table '{TableCode}' into indicator '{Slug}'...", tableCode, indicatorSlug);
            var rows = await _client.GetTableDataAsync(tableCode, ct);

            var upserts = 0;
            foreach (var row in rows)
            {
                if (!regionsByName.TryGetValue(Normalize(row.RegionName), out var region))
                {
                    continue; // e.g. "Deutschland insgesamt" totals row, or an unmapped region name
                }

                var existing = await _db.DataPoints.FirstOrDefaultAsync(
                    d => d.IndicatorId == indicator.Id && d.RegionId == region.Id && d.Year == row.Year, ct);

                if (existing is null)
                {
                    _db.DataPoints.Add(new DataPoint { Indicator = indicator, Region = region, Year = row.Year, Value = row.Value });
                }
                else
                {
                    existing.Value = row.Value;
                }

                upserts++;
            }

            if (indicator.DataSource is not null)
            {
                indicator.DataSource.LastSyncedAtUtc = DateTime.UtcNow;
            }

            _logger.LogInformation("Upserted {Count} data points for '{Slug}'.", upserts, indicatorSlug);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
