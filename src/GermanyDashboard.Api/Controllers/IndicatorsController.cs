using GermanyDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GermanyDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IndicatorsController : ControllerBase
{
    private readonly IIndicatorService _indicatorService;

    public IndicatorsController(IIndicatorService indicatorService)
    {
        _indicatorService = indicatorService;
    }

    /// <summary>Full multi-year series for an indicator across all regions (e.g. for a choropleth map or ranking).</summary>
    [HttpGet("{slug}")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "year" })]
    public async Task<IActionResult> GetSeries([FromRoute] string slug, [FromQuery] int? year, CancellationToken ct)
    {
        var series = await _indicatorService.GetIndicatorSeriesAsync(slug, year, ct);
        return series is null ? NotFound() : Ok(series);
    }
}
