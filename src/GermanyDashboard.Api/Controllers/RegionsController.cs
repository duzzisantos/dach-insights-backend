using GermanyDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GermanyDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RegionsController : ControllerBase
{
    private readonly IRegionService _regionService;

    public RegionsController(IRegionService regionService)
    {
        _regionService = regionService;
    }

    /// <summary>All regions (national + the 16 Bundesländer).</summary>
    [HttpGet]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var regions = await _regionService.GetAllRegionsAsync(ct);
        return Ok(regions);
    }

    /// <summary>A single region's profile: headline stats + category list, keyed by slug (e.g. "bayern").</summary>
    [HttpGet("{slug}")]
    [ResponseCache(Duration = 120)]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var profile = await _regionService.GetRegionProfileAsync(slug, ct);
        return profile is null ? NotFound() : Ok(profile);
    }
}
