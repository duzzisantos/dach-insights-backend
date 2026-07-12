using GermanyDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GermanyDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private const int MaxQueryLength = 100;
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<object>());
        }

        if (q.Length > MaxQueryLength)
        {
            return BadRequest(new { error = $"Query must be {MaxQueryLength} characters or fewer." });
        }

        var results = await _searchService.SearchAsync(q, ct);
        return Ok(results);
    }
}
