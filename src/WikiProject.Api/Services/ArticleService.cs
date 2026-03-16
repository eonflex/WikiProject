using Microsoft.EntityFrameworkCore;
using WikiProject.Api.Data;
using WikiProject.Api.DTOs;
using WikiProject.Api.Entities;
using WikiProject.Api.Mappings;

namespace WikiProject.Api.Services;

public class ArticleService : IArticleService
{
    private readonly WikiDbContext _db;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(WikiDbContext db, ILogger<ArticleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ArticleListResponse> GetArticlesAsync(ArticleQueryParams query)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        var q = _db.Articles
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            q = q.Where(a =>
                a.Title.ToLower().Contains(search) ||
                a.Summary.ToLower().Contains(search) ||
                a.Content.ToLower().Contains(search) ||
                a.Category.ToLower().Contains(search) ||
                a.ArticleTags.Any(at => at.Tag.Name.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(a => a.Category.ToLower() == query.Category.ToLower());

        if (!string.IsNullOrWhiteSpace(query.Tag))
            q = q.Where(a => a.ArticleTags.Any(at => at.Tag.Name.ToLower() == query.Tag.ToLower()));

        if (query.Status.HasValue)
            q = q.Where(a => a.Status == query.Status.Value);

        var totalCount = await q.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderByDescending(a => a.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ArticleListResponse(
            items.Select(a => a.ToSummaryDto()).ToList(),
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }

    public async Task<ArticleDto?> GetByIdAsync(int id)
    {
        var article = await _db.Articles
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return article?.ToDto();
    }

    public async Task<ArticleDto?> GetBySlugAsync(string slug)
    {
        var article = await _db.Articles
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug);

        return article?.ToDto();
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleRequest request)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Title)
            : request.Slug.Trim().ToLower();

        slug = await EnsureUniqueSlugAsync(slug);

        var tags = await ResolveTagsAsync(request.Tags);
        var now = DateTime.UtcNow;

        var article = new Article
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Summary = request.Summary.Trim(),
            Content = request.Content.Trim(),
            Category = request.Category.Trim(),
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now,
            ArticleTags = tags.Select(t => new ArticleTag { Tag = t }).ToList()
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created article {Id} '{Title}'", article.Id, article.Title);

        return await GetByIdAsync(article.Id) ?? article.ToDto();
    }

    public async Task<ArticleDto?> UpdateAsync(int id, UpdateArticleRequest request)
    {
        var article = await _db.Articles
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
            return null;

        var newSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Title)
            : request.Slug.Trim().ToLower();

        // Only re-check uniqueness if slug changed
        if (newSlug != article.Slug)
            newSlug = await EnsureUniqueSlugAsync(newSlug, excludeId: id);

        var tags = await ResolveTagsAsync(request.Tags);

        article.Title = request.Title.Trim();
        article.Slug = newSlug;
        article.Summary = request.Summary.Trim();
        article.Content = request.Content.Trim();
        article.Category = request.Category.Trim();
        article.Status = request.Status;
        article.UpdatedAt = DateTime.UtcNow;

        // Replace tags
        article.ArticleTags.Clear();
        foreach (var tag in tags)
            article.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, Tag = tag });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated article {Id} '{Title}'", article.Id, article.Title);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article is null)
            return false;

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted article {Id}", id);
        return true;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        return await _db.Articles
            .AsNoTracking()
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync()
    {
        return await _db.Tags
            .AsNoTracking()
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToListAsync();
    }

    // --- Helpers ---

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug.Length > 100 ? slug[..100] : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludeId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (true)
        {
            var query = _db.Articles.Where(a => a.Slug == slug);
            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);

            if (!await query.AnyAsync())
                return slug;

            slug = $"{baseSlug}-{counter++}";
        }
    }

    private async Task<List<Tag>> ResolveTagsAsync(IReadOnlyList<string>? tagNames)
    {
        if (tagNames is null || tagNames.Count == 0)
            return new List<Tag>();

        var normalized = tagNames
            .Select(t => t.Trim().ToLower())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        var existing = await _db.Tags
            .Where(t => normalized.Contains(t.Name))
            .ToListAsync();

        var existingNames = existing.Select(t => t.Name).ToHashSet();

        var newTags = normalized
            .Where(n => !existingNames.Contains(n))
            .Select(n => new Tag { Name = n })
            .ToList();

        if (newTags.Count > 0)
        {
            _db.Tags.AddRange(newTags);
            await _db.SaveChangesAsync();
        }

        return existing.Concat(newTags).ToList();
    }
}
