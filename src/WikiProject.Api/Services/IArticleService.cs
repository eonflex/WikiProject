using WikiProject.Api.DTOs;
using WikiProject.Api.Entities;

namespace WikiProject.Api.Services;

public interface IArticleService
{
    Task<ArticleListResponse> GetArticlesAsync(ArticleQueryParams query);
    Task<ArticleDto?> GetByIdAsync(int id);
    Task<ArticleDto?> GetBySlugAsync(string slug);
    Task<ArticleDto> CreateAsync(CreateArticleRequest request);
    Task<ArticleDto?> UpdateAsync(int id, UpdateArticleRequest request);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<IReadOnlyList<string>> GetTagsAsync();
}

public record ArticleQueryParams(
    string? Search = null,
    string? Category = null,
    string? Tag = null,
    ArticleStatus? Status = null,
    int Page = 1,
    int PageSize = 20
);
