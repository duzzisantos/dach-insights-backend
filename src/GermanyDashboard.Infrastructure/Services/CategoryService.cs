using GermanyDashboard.Application.DTOs;
using GermanyDashboard.Application.Interfaces;
using GermanyDashboard.Domain.Entities;
using GermanyDashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GermanyDashboard.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        return await _db.Categories
            .OrderBy(c => c.SortOrder)
            .Select(c => ToSummary(c))
            .ToListAsync(ct);
    }

    public async Task<CategoryDetailDto?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default)
    {
        var category = await _db.Categories
            .Include(c => c.Indicators)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

        if (category is null) return null;

        var indicators = category.Indicators
            .Select(i => new IndicatorSummaryDto(i.Id, i.Slug, i.Name, i.Description, i.Unit, i.ValueFormat, category.Slug))
            .OrderBy(i => i.Name)
            .ToList();

        return new CategoryDetailDto(ToSummary(category), indicators);
    }

    private static CategorySummaryDto ToSummary(Category c) => new(c.Id, c.Slug, c.Name, c.Description, c.Icon, c.ColorSlot);
}
