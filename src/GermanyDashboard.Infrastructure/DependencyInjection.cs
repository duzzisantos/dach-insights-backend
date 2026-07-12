using GermanyDashboard.Application.Interfaces;
using GermanyDashboard.Infrastructure.ExternalServices.Destatis;
using GermanyDashboard.Infrastructure.ExternalServices.Eurostat;
using GermanyDashboard.Infrastructure.Persistence;
using GermanyDashboard.Infrastructure.Seed;
using GermanyDashboard.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GermanyDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing ConnectionStrings:Default. Set ConnectionStrings__Default.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<ISearchService, SearchService>();

        services.Configure<GenesisApiOptions>(configuration.GetSection(GenesisApiOptions.SectionName));
        services.AddHttpClient<IGenesisApiClient, GenesisApiClient>();
        services.AddScoped<DestatisSyncService>();

        services.AddHttpClient<IEurostatApiClient, EurostatApiClient>();
        services.AddScoped<EurostatSyncService>();

        services.AddScoped<DbSeeder>();
        services.AddScoped<DbMigrator>();

        return services;
    }
}
