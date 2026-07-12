using GermanyDashboard.Application.DTOs;

namespace GermanyDashboard.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategorySummaryDto>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDetailDto?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default);
}
