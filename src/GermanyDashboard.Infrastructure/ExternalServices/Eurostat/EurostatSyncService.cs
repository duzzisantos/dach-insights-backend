using GermanyDashboard.Domain.Entities;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GermanyDashboard.Infrastructure.ExternalServices.Eurostat;

/// <summary>
/// Pulls real, live data from Eurostat's public REST API (no registration or API key
/// required) and upserts it over the seeded demo DataPoints. Germany's 16 Bundesländer
/// are NUTS-1 regions; Austria's 9 Bundesländer and Switzerland's 7 "Greater Regions"
/// are both NUTS-2 — Eurostat's "geo" filter accepts any mix of levels in one request, so
/// all three countries are fetched together. Invoked explicitly (see Program.cs
/// "sync-eurostat"), never automatically, so an operator always chooses when outbound
/// calls happen.
/// </summary>
public class EurostatSyncService
{
    private static readonly IReadOnlyList<string> AllGeoCodes = new[]
    {
        // Germany — country + 16 NUTS-1 states
        "DE", "DE1", "DE2", "DE3", "DE4", "DE5", "DE6", "DE7", "DE8", "DE9", "DEA", "DEB", "DEC", "DED", "DEE", "DEF", "DEG",
        // Austria — country + 9 NUTS-2 states
        "AT", "AT11", "AT12", "AT13", "AT21", "AT22", "AT31", "AT32", "AT33", "AT34",
        // Switzerland — country + 7 NUTS-2 greater regions
        "CH", "CH01", "CH02", "CH03", "CH04", "CH05", "CH06", "CH07",
    };

    private static readonly IReadOnlyList<string> CountryCodes = new[] { "DE", "AT", "CH" };

    private static readonly IReadOnlyList<string> Years =
        Enumerable.Range(Seed.TrendGenerator.FirstYear, Seed.TrendGenerator.LastYear - Seed.TrendGenerator.FirstYear + 1)
            .Select(y => y.ToString())
            .ToList();

    private static readonly int LatestYear = Years.Select(int.Parse).Max();

    private record IndicatorMapping(string IndicatorSlug, string DatasetCode, IReadOnlyDictionary<string, IReadOnlyList<string>> FixedFilters);

    private static readonly IReadOnlyList<IndicatorMapping> Mappings = new List<IndicatorMapping>
    {
        new("population", "demo_r_pjangrp3", new Dictionary<string, IReadOnlyList<string>>
        {
            ["sex"] = new[] { "T" },
            ["age"] = new[] { "TOTAL" },
            ["unit"] = new[] { "NR" },
        }),
        new("life-expectancy", "demo_r_mlifexp", new Dictionary<string, IReadOnlyList<string>>
        {
            ["sex"] = new[] { "T" },
            ["age"] = new[] { "Y_LT1" }, // life expectancy at birth
            ["unit"] = new[] { "YR" },
        }),
        new("unemployment-rate", "lfst_r_lfu3rt", new Dictionary<string, IReadOnlyList<string>>
        {
            ["sex"] = new[] { "T" },
            ["age"] = new[] { "Y15-74" },
            ["isced11"] = new[] { "TOTAL" },
            ["unit"] = new[] { "PC" },
        }),
        new("gdp-per-capita", "nama_10r_2gdp", new Dictionary<string, IReadOnlyList<string>>
        {
            ["unit"] = new[] { "EUR_HAB" },
        }),
    };

    private readonly IEurostatApiClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<EurostatSyncService> _logger;

    public EurostatSyncService(IEurostatApiClient client, AppDbContext db, ILogger<EurostatSyncService> logger)
    {
        _client = client;
        _db = db;
        _logger = logger;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        var regionByLabel = await BuildRegionLookupAsync(ct);
        var source = await GetOrCreateEurostatSourceAsync(ct);

        foreach (var mapping in Mappings)
        {
            var indicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == mapping.IndicatorSlug, ct);
            if (indicator is null)
            {
                _logger.LogWarning("No indicator with slug '{Slug}', skipping dataset '{Dataset}'.", mapping.IndicatorSlug, mapping.DatasetCode);
                continue;
            }

            var filters = new Dictionary<string, IReadOnlyList<string>>(mapping.FixedFilters)
            {
                ["geo"] = AllGeoCodes,
                ["time"] = Years,
            };

            var dataset = await _client.GetDatasetAsync(mapping.DatasetCode, filters, ct);
            var geoLabels = dataset.Dimension["geo"].Category.Label ?? new Dictionary<string, string>();

            var upserts = 0;
            var skippedGeo = new HashSet<string>();

            foreach (var (coordinates, value) in dataset.Rows())
            {
                var geoCode = coordinates["geo"];
                var label = geoLabels.GetValueOrDefault(geoCode, geoCode);

                if (!regionByLabel.TryGetValue(Normalize(label), out var region))
                {
                    skippedGeo.Add(label);
                    continue;
                }

                if (!int.TryParse(coordinates["time"], out var year))
                {
                    continue;
                }

                var existing = await _db.DataPoints.FirstOrDefaultAsync(
                    d => d.IndicatorId == indicator.Id && d.RegionId == region.Id && d.Year == year, ct);

                if (existing is null)
                {
                    _db.DataPoints.Add(new DataPoint { Indicator = indicator, Region = region, Year = year, Value = value });
                }
                else
                {
                    existing.Value = value;
                }

                // Keep the region's descriptive snapshot population in step with the
                // latest real figure from the same series, so the header stat and the
                // population chart never disagree.
                if (mapping.IndicatorSlug == "population" && year == LatestYear)
                {
                    region.Population = (long)Math.Round(value);
                }

                upserts++;
            }

            indicator.DataSourceId = source.Id;

            if (skippedGeo.Count > 0)
            {
                _logger.LogWarning("Unmatched Eurostat geo labels for '{Slug}': {Labels}", mapping.IndicatorSlug, string.Join(", ", skippedGeo));
            }

            _logger.LogInformation("Upserted {Count} real data points for '{Slug}' from Eurostat '{Dataset}'.", upserts, mapping.IndicatorSlug, mapping.DatasetCode);
        }

        source.LastSyncedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Not every country has a national total in Eurostat's *regional* tables — e.g.
        // Switzerland isn't an EU member, so nama_10r_2gdp carries its 7 greater regions
        // but no "CH" row. Backfill any such gap as a population-weighted aggregate of
        // that country's own regions, computed from data already synced above.
        await FillMissingNationalAggregatesAsync(ct);

        // Switzerland has *no* GDP data at all (national or regional) in nama_10r_2gdp —
        // it's an EU regional-accounts table and Switzerland isn't a member. Its 7
        // greater regions genuinely have no free, no-auth equivalent, so remove the
        // seeded placeholder numbers there rather than leave fake figures standing next
        // to real ones. Its national total GDP *is* separately published (international
        // cooperation series), so derive a real national per-capita figure from that.
        await RemoveUnavailableSwitzerlandRegionalGdpAsync(ct);
        await FillSwitzerlandNationalGdpPerCapitaAsync(ct);

        await _db.SaveChangesAsync(ct);
    }

    private async Task RemoveUnavailableSwitzerlandRegionalGdpAsync(CancellationToken ct)
    {
        var gdpIndicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == "gdp-per-capita", ct);
        var switzerland = await _db.Regions.FirstOrDefaultAsync(r => r.Code == "CH", ct);
        if (gdpIndicator is null || switzerland is null) return;

        var swissRegionIds = await _db.Regions
            .Where(r => r.ParentRegionId == switzerland.Id)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var placeholders = await _db.DataPoints
            .Where(d => d.IndicatorId == gdpIndicator.Id && swissRegionIds.Contains(d.RegionId))
            .ToListAsync(ct);

        if (placeholders.Count == 0) return;

        _db.DataPoints.RemoveRange(placeholders);
        _logger.LogInformation(
            "Removed {Count} seeded placeholder GDP data points for Switzerland's 7 greater regions — Eurostat has no real data at that level.",
            placeholders.Count);
    }

    private async Task FillSwitzerlandNationalGdpPerCapitaAsync(CancellationToken ct)
    {
        var gdpIndicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == "gdp-per-capita", ct);
        var populationIndicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == "population", ct);
        var switzerland = await _db.Regions.FirstOrDefaultAsync(r => r.Code == "CH", ct);
        if (gdpIndicator is null || populationIndicator is null || switzerland is null) return;

        var totalGdpMillionEur = await _client.GetDatasetAsync(
            "naida_10_gdp",
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["geo"] = new[] { "CH" },
                ["na_item"] = new[] { "B1GQ" }, // GDP at market prices
                ["unit"] = new[] { "CP_MEUR" }, // current prices, million EUR
                ["time"] = Years,
            },
            ct);

        var population = await _db.DataPoints
            .Where(d => d.IndicatorId == populationIndicator.Id && d.RegionId == switzerland.Id)
            .ToDictionaryAsync(d => d.Year, d => d.Value, ct);

        var existingByYear = await _db.DataPoints
            .Where(d => d.IndicatorId == gdpIndicator.Id && d.RegionId == switzerland.Id)
            .ToDictionaryAsync(d => d.Year, ct);

        var upserts = 0;
        foreach (var (coordinates, millionEur) in totalGdpMillionEur.Rows())
        {
            if (!int.TryParse(coordinates["time"], out var year) || !population.TryGetValue(year, out var pop) || pop == 0)
            {
                continue;
            }

            var perCapita = Math.Round(millionEur * 1_000_000m / pop, 0);

            if (existingByYear.TryGetValue(year, out var existing))
            {
                existing.Value = perCapita;
            }
            else
            {
                _db.DataPoints.Add(new DataPoint { Indicator = gdpIndicator, Region = switzerland, Year = year, Value = perCapita });
            }

            upserts++;
        }

        _logger.LogInformation(
            "Derived {Count} years of Switzerland's national GDP per capita from total GDP ÷ population (Eurostat has no Swiss regional GDP data at any level).",
            upserts);
    }

    private async Task FillMissingNationalAggregatesAsync(CancellationToken ct)
    {
        var populationIndicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == "population", ct);
        if (populationIndicator is null) return;

        foreach (var countryCode in CountryCodes)
        {
            var country = await _db.Regions.FirstOrDefaultAsync(r => r.Code == countryCode, ct);
            if (country is null) continue;

            var states = await _db.Regions.Where(r => r.ParentRegionId == country.Id).ToListAsync(ct);
            if (states.Count == 0) continue;

            foreach (var mapping in Mappings)
            {
                var indicator = await _db.Indicators.FirstOrDefaultAsync(i => i.Slug == mapping.IndicatorSlug, ct);
                if (indicator is null) continue;

                foreach (var year in Years.Select(int.Parse))
                {
                    var hasNational = await _db.DataPoints.AnyAsync(
                        d => d.IndicatorId == indicator.Id && d.RegionId == country.Id && d.Year == year, ct);
                    if (hasNational) continue;

                    var stateValues = await _db.DataPoints
                        .Where(d => d.IndicatorId == indicator.Id && d.Year == year && states.Select(s => s.Id).Contains(d.RegionId))
                        .ToListAsync(ct);
                    if (stateValues.Count != states.Count) continue; // incomplete year, skip rather than guess

                    decimal aggregate;
                    if (mapping.IndicatorSlug == "population")
                    {
                        aggregate = stateValues.Sum(d => d.Value);
                    }
                    else
                    {
                        var populations = await _db.DataPoints
                            .Where(d => d.IndicatorId == populationIndicator.Id && d.Year == year && states.Select(s => s.Id).Contains(d.RegionId))
                            .ToDictionaryAsync(d => d.RegionId, d => d.Value, ct);
                        if (populations.Count != states.Count) continue;

                        var weightedSum = stateValues.Sum(d => d.Value * populations[d.RegionId]);
                        var totalPopulation = populations.Values.Sum();
                        aggregate = totalPopulation == 0 ? 0 : Math.Round(weightedSum / totalPopulation, 1);
                    }

                    _db.DataPoints.Add(new DataPoint { Indicator = indicator, Region = country, Year = year, Value = aggregate });
                    _logger.LogInformation("Backfilled {Country} national '{Slug}' for {Year} as a population-weighted aggregate of its regions.", countryCode, mapping.IndicatorSlug, year);

                    if (mapping.IndicatorSlug == "population" && year == LatestYear)
                    {
                        country.Population = (long)Math.Round(aggregate);
                    }
                }
            }
        }
    }

    private async Task<Dictionary<string, Region>> BuildRegionLookupAsync(CancellationToken ct)
    {
        var regions = await _db.Regions.ToListAsync(ct);
        var lookup = new Dictionary<string, Region>();

        foreach (var region in regions)
        {
            lookup[Normalize(region.Name)] = region;
            lookup[Normalize(region.NameEnglish)] = region;
        }

        return lookup;
    }

    private async Task<DataSource> GetOrCreateEurostatSourceAsync(CancellationToken ct)
    {
        var source = await _db.DataSources.FirstOrDefaultAsync(s => s.Name == "Eurostat", ct);
        if (source is not null) return source;

        source = new DataSource
        {
            Name = "Eurostat",
            Url = "https://ec.europa.eu/eurostat/web/main/data/database",
            License = "© European Union, 1995–present. Reuse permitted, see Eurostat's data reuse policy.",
        };
        _db.DataSources.Add(source);
        await _db.SaveChangesAsync(ct);
        return source;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
