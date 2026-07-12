using GermanyDashboard.Domain.Entities;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GermanyDashboard.Infrastructure.Seed;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Regions.AnyAsync(ct))
        {
            _logger.LogInformation("Database already seeded, skipping.");
            return;
        }

        _logger.LogInformation("Seeding demo dataset for the DACH region (Germany, Austria, Switzerland)...");

        var source = new DataSource
        {
            Name = "Seeded placeholder (run `dotnet run -- sync-eurostat` for real data)",
            Url = "https://ec.europa.eu/eurostat/web/main/data/database",
            License = "Demo data for development only — not an official statistical release.",
        };
        _db.DataSources.Add(source);

        var categories = new List<Category>
        {
            new() { Slug = "demographics", Name = "Demographics", Description = "Population and life expectancy across the DACH region.", Icon = "users", ColorSlot = "blue", SortOrder = 1 },
            new() { Slug = "economy", Name = "Economy", Description = "Economic output per capita across regions.", Icon = "trending-up", ColorSlot = "aqua", SortOrder = 2 },
            new() { Slug = "employment", Name = "Employment", Description = "Labor market indicators across regions.", Icon = "briefcase", ColorSlot = "violet", SortOrder = 3 },
        };
        _db.Categories.AddRange(categories);

        var demographics = categories.Single(c => c.Slug == "demographics");
        var economy = categories.Single(c => c.Slug == "economy");
        var employment = categories.Single(c => c.Slug == "employment");

        var populationIndicator = new Indicator { Slug = "population", Name = "Population", Unit = "persons", ValueFormat = "number", Category = demographics, DataSource = source, Description = "Total resident population." };
        var lifeExpectancyIndicator = new Indicator { Slug = "life-expectancy", Name = "Life Expectancy at Birth", Unit = "years", ValueFormat = "years", Category = demographics, DataSource = source, Description = "Average life expectancy at birth." };
        var gdpIndicator = new Indicator { Slug = "gdp-per-capita", Name = "GDP per Capita", Unit = "EUR", ValueFormat = "currency-eur", Category = economy, DataSource = source, Description = "Gross domestic product per capita, nominal." };
        var unemploymentIndicator = new Indicator { Slug = "unemployment-rate", Name = "Unemployment Rate", Unit = "%", ValueFormat = "percent", Category = employment, DataSource = source, Description = "Share of the labor force that is unemployed." };

        _db.Indicators.AddRange(populationIndicator, lifeExpectancyIndicator, gdpIndicator, unemploymentIndicator);

        var dataPoints = new List<DataPoint>();
        var regionCount = 0;

        foreach (var country in RegionSeedData.Countries)
        {
            var nationalRegion = ToRegionEntity(country.National, parent: null);
            _db.Regions.Add(nationalRegion);
            AddSeriesForRegion(nationalRegion, country.National, populationIndicator, lifeExpectancyIndicator, gdpIndicator, unemploymentIndicator, dataPoints);
            regionCount++;

            foreach (var stateSeed in country.States)
            {
                var stateRegion = ToRegionEntity(stateSeed, parent: nationalRegion);
                _db.Regions.Add(stateRegion);
                AddSeriesForRegion(stateRegion, stateSeed, populationIndicator, lifeExpectancyIndicator, gdpIndicator, unemploymentIndicator, dataPoints);
                regionCount++;
            }
        }

        _db.DataPoints.AddRange(dataPoints);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seed complete: {RegionCount} regions, {IndicatorCount} indicators, {DataPointCount} data points.",
            regionCount, 4, dataPoints.Count);
    }

    private static Region ToRegionEntity(RegionSeed seed, Region? parent) => new()
    {
        Code = seed.Code,
        Slug = seed.Slug,
        Name = seed.Name,
        NameEnglish = seed.NameEnglish,
        Type = seed.Type,
        Population = seed.Population2023,
        AreaKm2 = seed.AreaKm2,
        Capital = seed.Capital,
        GeoJsonKey = seed.GeoJsonKey,
        ParentRegion = parent,
    };

    private static void AddSeriesForRegion(
        Region region,
        RegionSeed seed,
        Indicator population,
        Indicator lifeExpectancy,
        Indicator gdp,
        Indicator unemployment,
        List<DataPoint> sink)
    {
        foreach (var (year, value) in TrendGenerator.Population(seed.Population2023))
            sink.Add(new DataPoint { Region = region, Indicator = population, Year = year, Value = value });

        foreach (var (year, value) in TrendGenerator.LifeExpectancy(seed.LifeExpectancy2023))
            sink.Add(new DataPoint { Region = region, Indicator = lifeExpectancy, Year = year, Value = value });

        foreach (var (year, value) in TrendGenerator.GdpPerCapita(seed.GdpPerCapita2023))
            sink.Add(new DataPoint { Region = region, Indicator = gdp, Year = year, Value = value });

        foreach (var (year, value) in TrendGenerator.UnemploymentRate(seed.UnemploymentRate2023))
            sink.Add(new DataPoint { Region = region, Indicator = unemployment, Year = year, Value = value });
    }
}
