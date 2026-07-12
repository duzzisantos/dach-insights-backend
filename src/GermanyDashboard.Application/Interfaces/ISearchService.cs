using GermanyDashboard.Application.DTOs;

namespace GermanyDashboard.Application.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, CancellationToken ct = default);
}
