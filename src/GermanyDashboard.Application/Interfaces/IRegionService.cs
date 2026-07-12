using GermanyDashboard.Application.DTOs;

namespace GermanyDashboard.Application.Interfaces;

public interface IRegionService
{
    Task<IReadOnlyList<RegionSummaryDto>> GetAllRegionsAsync(CancellationToken ct = default);
    Task<RegionProfileDto?> GetRegionProfileAsync(string slug, CancellationToken ct = default);
}
