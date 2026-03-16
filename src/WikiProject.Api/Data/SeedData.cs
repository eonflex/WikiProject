using Microsoft.EntityFrameworkCore;
using WikiProject.Api.Entities;

namespace WikiProject.Api.Data;

/// <summary>
/// Seeds initial sample articles for local development.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(WikiDbContext db)
    {
        if (await db.Articles.AnyAsync())
            return;

        var tagNames = new[] { "getting-started", "guide", "api", "database", "architecture", "tips", "reference" };
        var tags = tagNames.Select(n => new Tag { Name = n }).ToList();
        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();

        var tagMap = tags.ToDictionary(t => t.Name);

        var now = DateTime.UtcNow;

        var articles = new List<Article>
        {
            new Article
            {
                Title = "Welcome to WikiProject",
                Slug = "welcome-to-wikiproject",
                Summary = "An introduction to this internal knowledge base and how to use it effectively.",
                Content = """
# Welcome to WikiProject

WikiProject is your team's internal knowledge base for storing notes, documentation, and how-to guides.

## Getting Started

1. Browse the article list to find existing content.
2. Use the search bar to find articles by keyword.
3. Filter by category or tag to narrow results.
4. Click **New Article** to start documenting something new.

## Conventions

- Use clear, concise titles.
- Add a short summary so readers know what to expect.
- Tag articles appropriately for discoverability.
- Keep articles focused on a single topic.
""",
                Category = "General",
                Status = ArticleStatus.Published,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-10),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["getting-started"] },
                    new ArticleTag { Tag = tagMap["guide"] }
                }
            },
            new Article
            {
                Title = "API Reference Overview",
                Slug = "api-reference-overview",
                Summary = "A quick reference guide to the WikiProject REST API endpoints.",
                Content = """
# API Reference Overview

The WikiProject API is a RESTful JSON API available at `/api`.

## Articles

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/articles` | List articles (supports search and filter) |
| GET | `/api/articles/{id}` | Get article by ID |
| GET | `/api/articles/slug/{slug}` | Get article by slug |
| POST | `/api/articles` | Create a new article |
| PUT | `/api/articles/{id}` | Update an article |
| DELETE | `/api/articles/{id}` | Delete an article |

## Query Parameters for GET /api/articles

- `search` – full-text search across title, summary, content, category, and tags
- `category` – filter by category name
- `tag` – filter by tag name
- `status` – filter by status (Draft, Published, Archived)
- `page` – page number (default: 1)
- `pageSize` – items per page (default: 20, max: 100)

## Metadata

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | List all unique categories |
| GET | `/api/tags` | List all tags |
""",
                Category = "Reference",
                Status = ArticleStatus.Published,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-2),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["api"] },
                    new ArticleTag { Tag = tagMap["reference"] }
                }
            },
            new Article
            {
                Title = "Setting Up the Development Environment",
                Slug = "setting-up-dev-environment",
                Summary = "Step-by-step instructions for getting the project running on your local machine.",
                Content = """
# Setting Up the Development Environment

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- Git

## Backend

```bash
cd src/WikiProject.Api
dotnet restore
dotnet ef database update
dotnet run
```

The API will be available at `http://localhost:5000`.

## Frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend dev server will start at `http://localhost:5173`.

## Applying Migrations

```bash
cd src/WikiProject.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Troubleshooting

- If the database doesn't exist, run `dotnet ef database update` first.
- Check that CORS settings in `appsettings.Development.json` match your frontend URL.
""",
                Category = "DevOps",
                Status = ArticleStatus.Published,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-1),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["getting-started"] },
                    new ArticleTag { Tag = tagMap["guide"] }
                }
            },
            new Article
            {
                Title = "Database Schema and EF Core Setup",
                Slug = "database-schema-ef-core",
                Summary = "Details the data model, EF Core configuration, and migration strategy used in this project.",
                Content = """
# Database Schema and EF Core Setup

## Entities

### Article

| Field | Type | Notes |
|-------|------|-------|
| Id | int | Primary key |
| Title | string | Required |
| Slug | string | Unique, URL-safe identifier |
| Summary | string | Short description |
| Content | string | Markdown content |
| Category | string | Free-form category string |
| Status | enum | Draft, Published, Archived |
| CreatedAt | DateTime | Set on create |
| UpdatedAt | DateTime | Updated on every save |

### Tag

Tags are stored in a normalized table with a many-to-many relationship to articles via `ArticleTag`.

## EF Core Configuration

The `WikiDbContext` uses Fluent API to configure:
- Composite primary key on `ArticleTag`
- Unique index on `Article.Slug`
- Unique index on `Tag.Name`

## Migrations

Migrations live in `src/WikiProject.Api/Migrations`.
""",
                Category = "Architecture",
                Status = ArticleStatus.Published,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["database"] },
                    new ArticleTag { Tag = tagMap["architecture"] }
                }
            },
            new Article
            {
                Title = "Writing Good Knowledge Base Articles",
                Slug = "writing-good-kb-articles",
                Summary = "Tips and conventions for writing clear, useful knowledge base articles.",
                Content = """
# Writing Good Knowledge Base Articles

A good KB article is specific, scannable, and actionable.

## Structure

1. **Title** – clear and specific, describes exactly what the article covers
2. **Summary** – one or two sentences for the article list view
3. **Content** – use headings, bullets, and code blocks

## Tips

- Write for someone who hasn't seen this before.
- Keep articles short and focused. Split large topics.
- Use code blocks for all commands and code snippets.
- Include the "why", not just the "how".
- Review and update articles when things change.

## Content Format

Articles support Markdown. Use it for readability:
- `#` headings for structure
- `**bold**` for emphasis
- `` `code` `` for inline code
- Fenced code blocks with language hints

## Tagging Strategy

Choose tags that describe the subject matter, not the format. For example:
- Good: `api`, `database`, `deployment`
- Avoid: `important`, `misc`, `todo` (use Draft status instead)
""",
                Category = "General",
                Status = ArticleStatus.Published,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["guide"] },
                    new ArticleTag { Tag = tagMap["tips"] }
                }
            },
            new Article
            {
                Title = "Authentication Integration (Planned)",
                Slug = "authentication-integration-planned",
                Summary = "Draft notes on how authentication will be integrated in a future phase.",
                Content = """
# Authentication Integration (Planned)

This article is a draft placeholder for the upcoming authentication work.

## Options Under Consideration

- ASP.NET Core Identity with JWT
- External provider via OpenID Connect (e.g., Microsoft Entra, Auth0)
- API key authentication for internal tooling

## Extension Points Already in Place

The backend controllers are written without authentication attributes so they are easy to secure later. To add auth:

1. Register an authentication scheme in `Program.cs`
2. Add `[Authorize]` to controllers or apply a global policy
3. Update CORS policy to allow credentials

## Frontend Considerations

- Store JWT in memory or httpOnly cookie
- Add an auth context / hook
- Redirect unauthenticated users to login page
""",
                Category = "Architecture",
                Status = ArticleStatus.Draft,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1),
                ArticleTags = new List<ArticleTag>
                {
                    new ArticleTag { Tag = tagMap["architecture"] },
                    new ArticleTag { Tag = tagMap["tips"] }
                }
            }
        };

        db.Articles.AddRange(articles);
        await db.SaveChangesAsync();
    }
}
