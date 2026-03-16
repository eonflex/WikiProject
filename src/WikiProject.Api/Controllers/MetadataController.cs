using Microsoft.AspNetCore.Mvc;
using WikiProject.Api.Services;

namespace WikiProject.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class MetadataController : ControllerBase
{
    private readonly IArticleService _articleService;

    public MetadataController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    /// <summary>Returns all distinct article categories.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories()
    {
        var categories = await _articleService.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>Returns all tag names.</summary>
    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTags()
    {
        var tags = await _articleService.GetTagsAsync();
        return Ok(tags);
    }
}
