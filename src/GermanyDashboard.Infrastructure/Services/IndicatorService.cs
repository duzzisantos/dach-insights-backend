using GermanyDashboard.Application.DTOs;
using GermanyDashboard.Application.Interfaces;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Services;

public class IndicatorService : IIndicatorService
{
    private readonly AppDbContext _db;

    public IndicatorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IndicatorSeriesDto?> GetIndicatorSeriesAsync(string slug, int? year = null, CancellationToken ct = default)
    {
        var indicator = await _db.Indicators
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Slug == slug, ct);

        if (indicator is null) return null;

        var query = _db.DataPoints
            .Where(d => d.IndicatorId == indicator.Id)
            .Include(d => d.Region)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(d => d.Year == year.Value);
        }

        var points = await query
            .OrderBy(d => d.Region!.Name).ThenBy(d => d.Year)
            .Select(d => new IndicatorSeriesPointDto(d.Region!.Slug, d.Region!.Name, d.Region!.NameEnglish, d.Year, d.Value))
            .ToListAsync(ct);

        var summary = new IndicatorSummaryDto(
            indicator.Id, indicator.Slug, indicator.Name, indicator.Description,
            indicator.Unit, indicator.ValueFormat, indicator.Category!.Slug);

        return new IndicatorSeriesDto(summary, points);
    }
}
