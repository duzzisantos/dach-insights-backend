using GermanyDashboard.Application.DTOs;
using GermanyDashboard.Application.Interfaces;
using GermanyDashboard.Domain.Entities;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Services;

public class RegionService : IRegionService
{
    private readonly AppDbContext _db;

    public RegionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RegionSummaryDto>> GetAllRegionsAsync(CancellationToken ct = default)
    {
        return await _db.Regions
            .Include(r => r.ParentRegion)
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Name)
            .Select(r => ToSummary(r))
            .ToListAsync(ct);
    }

    public async Task<RegionProfileDto?> GetRegionProfileAsync(string slug, CancellationToken ct = default)
    {
        var region = await _db.Regions.Include(r => r.ParentRegion).FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (region is null) return null;

        var regionDataPoints = await _db.DataPoints
            .Where(d => d.RegionId == region.Id)
            .Include(d => d.Indicator!).ThenInclude(i => i!.Category)
            .ToListAsync(ct);

        var highlights = regionDataPoints
            .GroupBy(d => d.IndicatorId)
            .Select(g => g.OrderByDescending(d => d.Year).Take(2).ToList())
            .Where(series => series.Count > 0)
            .Select(series =>
            {
                var latest = series[0];
                var previous = series.Count > 1 ? series[1] : null;
                var indicator = latest.Indicator!;
                return new HighlightStatDto(
                    indicator.Slug,
                    indicator.Name,
                    indicator.Unit,
                    indicator.ValueFormat,
                    latest.Year,
                    latest.Value,
                    previous?.Value,
                    indicator.Category!.Slug);
            })
            .OrderBy(h => h.CategorySlug)
            .ToList();

        var categories = await _db.Categories
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategorySummaryDto(c.Id, c.Slug, c.Name, c.Description, c.Icon, c.ColorSlot))
            .ToListAsync(ct);

        return new RegionProfileDto(ToSummary(region), highlights, categories);
    }

    private static RegionSummaryDto ToSummary(Region r) => new(
        r.Id, r.Code, r.Slug, r.Name, r.NameEnglish, r.Type.ToString(), r.Population, r.AreaKm2, r.Capital, r.GeoJsonKey,
        r.ParentRegion?.Slug);
}
