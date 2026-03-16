using WikiProject.Api.DTOs;
using WikiProject.Api.Entities;

namespace WikiProject.Api.Mappings;

public static class ArticleMappings
{
    public static ArticleDto ToDto(this Article article) =>
        new ArticleDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Summary,
            article.Content,
            article.Category,
            article.ArticleTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
            article.Status.ToString(),
            article.CreatedAt,
            article.UpdatedAt
        );

    public static ArticleSummaryDto ToSummaryDto(this Article article) =>
        new ArticleSummaryDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Summary,
            article.Category,
            article.ArticleTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
            article.Status.ToString(),
            article.CreatedAt,
            article.UpdatedAt
        );
}
