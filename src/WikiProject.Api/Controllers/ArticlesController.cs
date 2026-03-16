using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WikiProject.Api.DTOs;
using WikiProject.Api.Entities;
using WikiProject.Api.Services;

namespace WikiProject.Api.Controllers;

[ApiController]
[Route("api/articles")]
[Produces("application/json")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>Gets a paginated, searchable, filterable list of articles.</summary>
    [HttpGet]
    public async Task<ActionResult<ArticleListResponse>> GetArticles(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? tag,
        [FromQuery] ArticleStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new ArticleQueryParams(search, category, tag, status, page, pageSize);
        var result = await _articleService.GetArticlesAsync(query);
        return Ok(result);
    }

    /// <summary>Gets a single article by its numeric ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Gets a single article by its slug.</summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ArticleDto>> GetBySlug(string slug)
    {
        var article = await _articleService.GetBySlugAsync(slug);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Creates a new article.</summary>
    [HttpPost]
    public async Task<ActionResult<ArticleDto>> Create(
        [FromBody] CreateArticleRequest request,
        [FromServices] IValidator<CreateArticleRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.ToDictionary()));

        var article = await _articleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    /// <summary>Updates an existing article.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArticleDto>> Update(
        int id,
        [FromBody] UpdateArticleRequest request,
        [FromServices] IValidator<UpdateArticleRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.ToDictionary()));

        var article = await _articleService.UpdateAsync(id, request);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Deletes an article by ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _articleService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
