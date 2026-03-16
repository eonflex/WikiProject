using WikiProject.Api.Entities;

namespace WikiProject.Api.DTOs;

public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ArticleSummaryDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Category,
    IReadOnlyList<string> Tags,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status
);

public record UpdateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status
);

public record ArticleListResponse(
    IReadOnlyList<ArticleSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
