using GermanyDashboard.Application.DTOs;
using GermanyDashboard.Application.Interfaces;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Services;

public class SearchService : ISearchService
{
    private const int MaxResultsPerType = 8;
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        var trimmed = query.Trim();
        if (trimmed.Length < 2) return Array.Empty<SearchResultDto>();

        var pattern = $"%{trimmed}%";

        var regions = await _db.Regions
            .Where(r => EF.Functions.ILike(r.Name, pattern) || EF.Functions.ILike(r.NameEnglish, pattern))
            .OrderBy(r => r.Name)
            .Take(MaxResultsPerType)
            .Select(r => new SearchResultDto("region", r.Slug, r.Name, r.Capital, $"/{r.Slug}"))
            .ToListAsync(ct);

        var categories = await _db.Categories
            .Where(c => EF.Functions.ILike(c.Name, pattern))
            .OrderBy(c => c.Name)
            .Take(MaxResultsPerType)
            .Select(c => new SearchResultDto("category", c.Slug, c.Name, c.Description, $"/category/{c.Slug}"))
            .ToListAsync(ct);

        // Indicators don't have their own page — they live inside their category's page.
        var indicators = await _db.Indicators
            .Include(i => i.Category)
            .Where(i => EF.Functions.ILike(i.Name, pattern))
            .OrderBy(i => i.Name)
            .Take(MaxResultsPerType)
            .Select(i => new SearchResultDto("indicator", i.Slug, i.Name, i.Category!.Name, $"/category/{i.Category!.Slug}"))
            .ToListAsync(ct);

        return regions.Concat(categories).Concat(indicators).ToList();
    }
}
