using GermanyDashboard.Application.DTOs;

namespace GermanyDashboard.Application.Interfaces;

public interface IIndicatorService
{
    Task<IndicatorSeriesDto?> GetIndicatorSeriesAsync(string slug, int? year = null, CancellationToken ct = default);
}
