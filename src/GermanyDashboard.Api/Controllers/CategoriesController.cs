using GermanyDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GermanyDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpGet("{slug}")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var category = await _categoryService.GetCategoryBySlugAsync(slug, ct);
        return category is null ? NotFound() : Ok(category);
    }
}
