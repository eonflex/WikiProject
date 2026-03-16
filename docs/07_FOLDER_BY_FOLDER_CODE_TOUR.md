# Folder-by-Folder Code Tour

> **Audience:** Newer developers who are smart and motivated but may feel overwhelmed by the codebase.
> **Goal:** Not just to describe the files, but to teach you how to _think_ about this repository — what every folder is for, why each file exists, and how they relate to each other.

---

## Table of Contents

1. [Read These First](#1-read-these-first)
2. [How to Mentally Map This Repository](#2-how-to-mentally-map-this-repository)
3. [The Repository Root](#3-the-repository-root)
4. [Backend Tour — `src/WikiProject.Api/`](#4-backend-tour--srcwikiprojectapi)
   - [Entry Point: `Program.cs`](#entry-point-programcs)
   - [Configuration: `appsettings.json` and `appsettings.Development.json`](#configuration-appsettingsjson-and-appsettingsdevelopmentjson)
   - [Controllers](#controllers)
   - [Services](#services)
   - [DTOs](#dtos)
   - [Entities](#entities)
   - [Data](#data)
   - [Mappings](#mappings)
   - [Validation](#validation)
   - [Migrations](#migrations)
   - [Properties](#properties)
5. [Frontend Tour — `frontend/`](#5-frontend-tour--frontend)
   - [Entry Point: `main.tsx` and `index.html`](#entry-point-maintsx-and-indexhtml)
   - [Router: `App.tsx`](#router-apptsx)
   - [Pages](#pages)
   - [Components](#components)
   - [Hooks](#hooks)
   - [Services](#services-1)
   - [Types](#types)
   - [Utils](#utils)
   - [Styles: `App.css` and `index.css`](#styles-appcss-and-indexcss)
   - [Config Files](#config-files)
6. [If You Only Have One Hour, Read These Files](#6-if-you-only-have-one-hour-read-these-files)
7. [Files That Are Safe for Beginners to Change](#7-files-that-are-safe-for-beginners-to-change)
8. [Files to Be Careful With](#8-files-to-be-careful-with)
9. [The Data Flow: How a Request Travels End-to-End](#9-the-data-flow-how-a-request-travels-end-to-end)
10. [Quick Reference: Every Important File at a Glance](#10-quick-reference-every-important-file-at-a-glance)

---

## 1. Read These First

Before diving into any folder, read these four files. They give you the entire mental model in about 30 minutes.

| Order | File | Why |
|-------|------|-----|
| 1 | `README.md` | Project overview, tech stack, and how to start both servers locally. Read this before you run anything. |
| 2 | `src/WikiProject.Api/Program.cs` | Tiny (~80 lines) but it is the backbone of the backend. Every service, database connection, and middleware is wired up here. |
| 3 | `frontend/src/App.tsx` | Shows you every URL the frontend knows about (~25 lines). If you want to know what pages exist, start here. |
| 4 | `frontend/src/types/index.ts` | Defines the shape of every piece of data that flows between frontend and backend. Read this and you understand the data model instantly. |

---

## 2. How to Mentally Map This Repository

This is a **full-stack web application** with two completely separate halves that talk to each other over HTTP. Think of them as two programs that happen to live in the same folder.

```
WikiProject/
├── src/              ← The backend (a .NET 10 web API written in C#)
│   └── WikiProject.Api/
│
├── frontend/         ← The frontend (a React 19 app written in TypeScript)
│
├── README.md         ← Start here
├── STARTING_TASKS.md ← Project roadmap and task backlog
└── WikiProject.slnx  ← .NET solution file (used by Visual Studio / Rider)
```

### The Mental Model in One Sentence

> The **frontend** (React) is what users see and click. It sends HTTP requests to the **backend** (ASP.NET Core). The backend queries a **SQLite database** and returns JSON. The frontend renders that JSON as a web page.

### The Three-Tier Stack

```
Browser (user clicks a button)
    ↓  HTTP/JSON requests
frontend/  (React)
    ↓  /api/* requests, proxied by Vite dev server
src/WikiProject.Api/  (ASP.NET Core)
    ↓  EF Core queries
wiki.db  (SQLite file, created automatically on first run)
```

**Important:** In development, the frontend runs on port `5173` and the backend runs on port `5018`. You need **both servers running** at the same time. The Vite dev server automatically proxies any request starting with `/api` to `http://localhost:5018`, so you never have to worry about cross-origin errors during local development.

### Why Two Separate Folders?

Many beginners expect a single project. The reason for separation is clean **separation of concerns**:

- The backend is a pure JSON API. It has no knowledge of React, HTML, or CSS.
- The frontend is a pure static app. It has no database connections or business logic.
- This structure makes it easy to replace one half independently (e.g., swap React for Vue, or swap .NET for Node).

---

## 3. The Repository Root

```
WikiProject/
├── .gitignore
├── README.md
├── STARTING_TASKS.md
├── WikiProject.slnx
├── frontend/
└── src/
```

### `.gitignore`

**What it does:** Tells Git which files to never commit. This prevents build artifacts, local configuration, and secrets from polluting the repository.

**Key exclusions you should know:**
- `bin/`, `obj/` — .NET build output. These are regenerated every build; never commit them.
- `*.db`, `*.db-shm`, `*.db-wal` — The SQLite database files. Each developer has their own local copy; the database is _not_ shared through Git.
- `frontend/node_modules/` — JavaScript dependencies. Regenerated by `npm install`.
- `frontend/dist/` — The built frontend. Regenerated by `npm run build`.
- `.env`, `.env.local` — Environment secrets. Never commit these.

**Common beginner confusion:** If you add a new tool, library, or build system, you may need to add entries here. If you notice huge untracked files appearing in `git status`, they probably belong in `.gitignore`.

### `README.md`

**What it does:** The canonical project overview. It covers the tech stack, folder layout, how to run the app locally, how to create migrations, and a full list of API routes.

**When it matters:** Every time someone new joins the project. It is also a good reference for the API route list.

### `STARTING_TASKS.md`

**What it does:** A living task backlog organized into five phases (Setup → CRUD → Search → UX Polish → Stretch Goals). It shows which features are complete and which are still planned.

**When it matters:** When you are deciding what to work on next, or when you want context for _why_ the codebase looks the way it does. Many design decisions trace back to items in Phase 1 or Phase 2.

### `WikiProject.slnx`

**What it does:** The .NET solution file. It is used by IDEs like Visual Studio and JetBrains Rider to understand which projects are in the solution. The `.slnx` extension is the newer XML-based format (introduced with .NET 9).

**When it matters:** When you open the project in a .NET IDE. If you are working purely from the command line with `dotnet run`, you can ignore this file.

**Beginner note:** You never need to edit this file manually. If you add a new .NET project, your IDE will update it automatically.

---

## 4. Backend Tour — `src/WikiProject.Api/`

The backend is a single .NET 10 **ASP.NET Core Web API** project. All of its source code lives in one folder:

```
src/WikiProject.Api/
├── Program.cs                  ← App startup and wiring
├── appsettings.json            ← Configuration
├── appsettings.Development.json
├── WikiProject.Api.csproj      ← Project/package manifest
├── WikiProject.Api.http        ← Manual API test file
├── Controllers/
│   ├── ArticlesController.cs
│   └── MetadataController.cs
├── DTOs/
│   └── ArticleDtos.cs
├── Data/
│   ├── WikiDbContext.cs
│   └── SeedData.cs
├── Entities/
│   ├── Article.cs
│   ├── ArticleStatus.cs
│   ├── ArticleTag.cs
│   └── Tag.cs
├── Mappings/
│   └── ArticleMappings.cs
├── Migrations/
│   ├── 20260315235834_InitialCreate.cs
│   ├── 20260315235834_InitialCreate.Designer.cs
│   └── WikiDbContextModelSnapshot.cs
├── Services/
│   ├── IArticleService.cs
│   └── ArticleService.cs
├── Validation/
│   └── ArticleValidators.cs
└── Properties/
    └── launchSettings.json
```

The architecture follows a classic **N-tier** pattern:

```
HTTP Request
    ↓
Controllers/   (receive HTTP, validate input, return HTTP)
    ↓
Services/      (business logic, orchestration)
    ↓
Data/          (database access via Entity Framework Core)
    ↓
SQLite (wiki.db)
```

---

### Entry Point: `Program.cs`

**What it is:** The single file where the entire backend application is configured and started. In modern .NET (6+), there is no `Startup.cs` — everything happens in `Program.cs` using the **minimal hosting model**.

**What it does, line by line (simplified):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register controllers (so ASP.NET can find ArticlesController, etc.)
builder.Services.AddControllers();

// 2. Register Swagger (auto-generated API docs at /swagger)
builder.Services.AddSwaggerGen(...);

// 3. Register Entity Framework Core with a SQLite connection
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));

// 4. Register the ArticleService (so controllers can receive it via constructor injection)
builder.Services.AddScoped<IArticleService, ArticleService>();

// 5. Register validators (FluentValidation)
builder.Services.AddScoped<IValidator<CreateArticleRequest>, CreateArticleRequestValidator>();

// 6. Register CORS (allow the React frontend to call this API)
builder.Services.AddCors(...);

var app = builder.Build();

// 7. On startup: apply any pending database migrations, then seed sample data
db.Database.Migrate();
await SeedData.SeedAsync(db);

// 8. In development, enable Swagger UI
app.UseSwagger(); app.UseSwaggerUI();

// 9. Enable CORS
app.UseCors();

// 10. Map all controller routes (e.g. [Route("api/articles")])
app.MapControllers();

app.Run();
```

**Key concept — Dependency Injection (DI):** The calls to `builder.Services.Add*` are registering things into the **DI container**. This is ASP.NET Core's built-in system for wiring up dependencies. When `ArticlesController` declares `public ArticlesController(IArticleService articleService)`, ASP.NET Core reads the DI container and automatically passes in the registered `ArticleService`. You never call `new ArticleService()` yourself.

**Why this matters:** If you add a new service or feature, you will need to register it here. If a class is not in the DI container, you will get a runtime error when the app tries to construct it.

**Common beginner confusion:** CORS errors. If the React frontend cannot reach the API (you see "blocked by CORS policy" in the browser console), the allowed origin list in `Program.cs` (sourced from `appsettings.json`) needs to include the frontend's URL.

---

### Configuration: `appsettings.json` and `appsettings.Development.json`

**What they are:** JSON files that store configuration values. Think of them like a settings panel for the backend.

**`appsettings.json`** (always loaded):
```json
{
  "ConnectionStrings": {
    "Default": "Data Source=wiki.db"   // SQLite database path
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]  // React dev server
  },
  "Logging": { ... }
}
```

**`appsettings.Development.json`** (loaded only when `ASPNETCORE_ENVIRONMENT=Development`):
- Overrides values from `appsettings.json` for local development.
- Typically enables more verbose logging or uses a different database.

**When it matters:** If you change the port the React app runs on, update `AllowedOrigins` here. If you want to point to a different database file, update `ConnectionStrings.Default`.

**Alternative approaches:** For real deployments, secrets like connection strings are provided via environment variables (e.g., `ConnectionStrings__Default=Data Source=/data/wiki.db`), not hardcoded in JSON files. This is a .NET convention where `__` maps to `:` in the config hierarchy.

---

### Controllers

```
Controllers/
├── ArticlesController.cs   ← CRUD for articles
└── MetadataController.cs   ← Lists all categories and tags
```

**What controllers are:** The HTTP boundary of the application. A controller is a class that receives HTTP requests, passes work to a service, and returns an HTTP response. Nothing else. Controllers should be thin — all real logic belongs in services.

#### `ArticlesController.cs`

The most important file in the backend. It defines these API endpoints:

| Method | Route | What it does |
|--------|-------|--------------|
| `GET` | `/api/articles` | List articles with optional search, filters, and pagination |
| `GET` | `/api/articles/{id}` | Get one article by numeric ID |
| `GET` | `/api/articles/slug/{slug}` | Get one article by its URL slug |
| `POST` | `/api/articles` | Create a new article |
| `PUT` | `/api/articles/{id}` | Update an existing article |
| `DELETE` | `/api/articles/{id}` | Delete an article |

**Key pattern to notice:**
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<ArticleDto>> GetById(int id)
{
    var article = await _articleService.GetByIdAsync(id);
    return article is null ? NotFound() : Ok(article);
}
```
The controller does only three things: (1) receive the request, (2) call the service, (3) return the appropriate HTTP status. All real work is in `ArticleService`.

**`[FromServices]` for validators:** You may notice validators are injected via `[FromServices]` rather than the constructor. This is a valid technique for things that are only needed in one action method — it avoids polluting the constructor with dependencies used rarely.

#### `MetadataController.cs`

Two simple endpoints:

| Method | Route | What it does |
|--------|-------|--------------|
| `GET` | `/api/categories` | Returns a sorted list of all distinct categories |
| `GET` | `/api/tags` | Returns a sorted list of all tag names |

These are used by the frontend to populate the filter dropdowns.

---

### Services

```
Services/
├── IArticleService.cs   ← The contract (interface)
└── ArticleService.cs    ← The implementation
```

**What services are:** The business logic layer. If you want to understand what the app _does_ (not just what it exposes), read this folder.

#### Why an Interface?

`IArticleService.cs` declares the contract:
```csharp
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
```

`ArticleService.cs` provides the real implementation.

**Why separate them?** Interfaces allow you to swap implementations without changing the controller. For example, when you add tests, you can create a `FakeArticleService` or `MockArticleService` that returns hardcoded data — without touching `ArticlesController` at all. The DI container maps `IArticleService → ArticleService` in `Program.cs`.

#### `ArticleService.cs` — The Core Logic

This 255-line file is the heart of the backend. Let's walk through the most important methods:

**`GetArticlesAsync`** — Search, filter, and paginate:
```csharp
var q = _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .AsNoTracking()
    .AsQueryable();

if (!string.IsNullOrWhiteSpace(query.Search))
{
    var search = query.Search.ToLower();
    q = q.Where(a =>
        a.Title.ToLower().Contains(search) ||
        a.Summary.ToLower().Contains(search) ||
        a.Content.ToLower().Contains(search) || ...);
}
// ... more filters ...
var totalCount = await q.CountAsync();
var items = await q.OrderByDescending(a => a.UpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

Key points:
- **`Include` / `ThenInclude`:** This is EF Core's way of loading related data. Without it, `ArticleTags` and their `Tag` objects would be empty.
- **`AsNoTracking()`:** A performance optimization for read-only queries. It tells EF Core not to track changes to these objects, saving memory.
- **`AsQueryable()`:** Keeps the query as an `IQueryable<T>` so filters can be chained before the SQL is actually executed. No database trip happens until `CountAsync()` or `ToListAsync()` is called.

**`CreateAsync`** — Creating with auto-slug and tag resolution:
```csharp
// Auto-generate slug from title if not provided
var slug = string.IsNullOrWhiteSpace(request.Slug)
    ? GenerateSlug(request.Title)
    : request.Slug.Trim().ToLower();

// Ensure no two articles share a slug
slug = await EnsureUniqueSlugAsync(slug);

// Find or create tags
var tags = await ResolveTagsAsync(request.Tags);
```

**`ResolveTagsAsync`** — The tag normalization helper:
- Takes a list of tag name strings.
- Normalizes them to lowercase and trims whitespace.
- Looks up which ones already exist in the `Tags` table.
- Creates new `Tag` rows for any that are new.
- Returns the final list of `Tag` entities.

This means you never have duplicate tags and never have to manage tag IDs from the frontend.

---

### DTOs

```
DTOs/
└── ArticleDtos.cs
```

**What DTOs are:** Data Transfer Objects. These are the shapes of data that cross the HTTP boundary — what the API accepts in requests and what it returns in responses. They are the _contract_ between frontend and backend.

**Why use DTOs instead of exposing entities directly?** Entities are tied to the database schema and may contain navigation properties (like `ICollection<ArticleTag>`) that don't serialize cleanly to JSON. DTOs give you full control over what you expose.

**The DTOs in this project:**

| DTO | Direction | Purpose |
|-----|-----------|---------|
| `ArticleDto` | Response (full) | Full article including `Content` field |
| `ArticleSummaryDto` | Response (list) | Article without `Content` — for lists where content is not needed |
| `CreateArticleRequest` | Request | Fields needed to create an article |
| `UpdateArticleRequest` | Request | Fields needed to update an article (same shape as create) |
| `ArticleListResponse` | Response | Wrapper for paginated results: items + pagination metadata |
| `ArticleQueryParams` | Internal | Parsed query string parameters for `GetArticles` |

**Note on C# `record` types:** All DTOs are declared as `record` rather than `class`. Records are immutable value types — ideal for DTOs because they cannot be accidentally modified after creation, and they get equality comparison and a `ToString()` for free.

**Common beginner confusion:** `ArticleDto` has a `Content` field; `ArticleSummaryDto` does not. When you are building a list view, the API returns `ArticleSummaryDto` objects to avoid sending large content fields for every article. Only the detail view fetches the full `ArticleDto`.

---

### Entities

```
Entities/
├── Article.cs        ← The main domain object
├── ArticleStatus.cs  ← An enum: Draft=0, Published=1, Archived=2
├── ArticleTag.cs     ← The many-to-many join table entity
└── Tag.cs            ← A tag
```

**What entities are:** C# classes that map directly to database tables. Entity Framework Core reads these class definitions to know what tables and columns to create.

#### `Article.cs`

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

The `ArticleTags` collection is a **navigation property** — it represents the many-to-many relationship between articles and tags. EF Core uses it to join the `Tags` table when you call `.Include(a => a.ArticleTags).ThenInclude(at => at.Tag)`.

#### `ArticleTag.cs` — The Join Entity

In a many-to-many relationship (one article can have many tags; one tag can belong to many articles), you need a join table. `ArticleTag` is that table. It holds two foreign keys: `ArticleId` and `TagId`. There are no other columns.

```csharp
public class ArticleTag
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
```

**Why a separate entity instead of EF Core's implicit many-to-many?** EF Core 5+ supports implicit many-to-many (you just put `ICollection<Tag>` on `Article`). Using an explicit join entity gives you more control — if you ever want to add columns to the join table (e.g., a "primary tag" flag), you can do so without a migration nightmare.

#### `ArticleStatus.cs`

```csharp
public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
```

Stored in the database as an integer (0, 1, or 2). Serialized to JSON as a string ("Draft", "Published", "Archived") thanks to the `.ToString()` call in `ArticleMappings.cs`.

---

### Data

```
Data/
├── WikiDbContext.cs   ← The EF Core database context
└── SeedData.cs        ← Sample data for first-run seeding
```

#### `WikiDbContext.cs`

The **DbContext** is the main class through which all database operations go. Think of it as the gateway to the database.

```csharp
public class WikiDbContext : DbContext
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite primary key for the join table
        modelBuilder.Entity<ArticleTag>()
            .HasKey(at => new { at.ArticleId, at.TagId });

        // Unique constraint on slug and tag name
        modelBuilder.Entity<Article>().HasIndex(a => a.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
    }
}
```

`OnModelCreating` is where **Fluent API** configuration lives — things that cannot be expressed with simple attributes on the entity classes (like composite primary keys and unique indexes). When EF Core generates a migration, it reads both the entity classes and this method.

**Common beginner confusion:** The three `DbSet<>` properties are how you query each table. `_db.Articles` is like a `SELECT * FROM Articles` table handle. You can chain LINQ queries on it: `_db.Articles.Where(a => a.Status == ArticleStatus.Published)`.

#### `SeedData.cs`

**What it does:** Creates 7 sample tags and 6 sample articles the very first time the app starts (it checks `if (!await db.Articles.AnyAsync())` first, so it is safe to run repeatedly).

**When it matters:** When you are developing and need realistic data in the database. If you want to change the sample data, this is the file to edit. To reset: delete `wiki.db` and restart the backend.

---

### Mappings

```
Mappings/
└── ArticleMappings.cs
```

**What it does:** Provides extension methods that convert `Article` entities to `ArticleDto` or `ArticleSummaryDto`. This separates the mapping logic from both the entity and the DTO.

```csharp
public static ArticleDto ToDto(this Article article) =>
    new ArticleDto(
        article.Id,
        article.Title,
        article.Slug,
        article.Summary,
        article.Content,
        article.Category,
        article.ArticleTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
        article.Status.ToString(),   // Convert enum to string here
        article.CreatedAt,
        article.UpdatedAt
    );
```

**Why a separate file?** Mapping logic can become complex. Keeping it separate means `ArticleService` stays focused on business logic, and the mapping rules are easy to find and change.

**Alternative approaches:** Some teams use a library like [AutoMapper](https://automapper.org/) or [Mapster](https://github.com/MapsterMapper/Mapster) to automate this. Manual mapping (as done here) is explicit and easy to debug; auto-mappers are convenient but add a layer of magic.

---

### Validation

```
Validation/
└── ArticleValidators.cs
```

**What it does:** Defines the rules for valid `CreateArticleRequest` and `UpdateArticleRequest` objects using [FluentValidation](https://docs.fluentvalidation.net/).

```csharp
public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Slug)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase alphanumeric with hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));  // Only validate if provided
        // ... etc
    }
}
```

**Why FluentValidation instead of built-in Data Annotations?** Data Annotations (like `[Required]`, `[MaxLength(200)]` on properties) are simpler, but FluentValidation is far more expressive. You can write conditional rules (`.When(...)`), cross-field rules, and reuse validators. The rules read almost like English.

**Common beginner confusion:** Validation is checked in the controller before calling the service. If validation fails, the controller returns a `400 ValidationProblem` response immediately, and the service is never called.

---

### Migrations

```
Migrations/
├── 20260315235834_InitialCreate.cs           ← The migration (what changed)
├── 20260315235834_InitialCreate.Designer.cs  ← Metadata (auto-generated)
└── WikiDbContextModelSnapshot.cs             ← Current model snapshot (auto-generated)
```

**What migrations are:** A history of every change made to the database schema. Each migration file contains an `Up` method (apply the change) and a `Down` method (roll back the change).

**The `InitialCreate` migration** creates three tables: `Articles`, `Tags`, and `ArticleTags`.

**How migrations are applied:** In `Program.cs`, `db.Database.Migrate()` runs on every startup. It checks which migrations have already been applied and applies any new ones. This means you never need to manually run SQL scripts to set up the database.

**How to create a new migration** (when you change an entity):
```bash
cd src/WikiProject.Api
dotnet ef migrations add YourMigrationName
dotnet ef database update  # Optional: migrations also run on startup
```

**Files to never manually edit:** `*.Designer.cs` and `WikiDbContextModelSnapshot.cs` are auto-generated by the EF Core tooling. The snapshot represents the current state of the model and is used to compute what changed when you add a new migration. Editing these by hand usually corrupts the migration history.

---

### Properties

```
Properties/
└── launchSettings.json
```

**What it does:** Tells `dotnet run` and IDEs how to launch the app in development. Defines the port (`http://localhost:5018`), environment variables, and launch profiles.

**When it matters:** If you need to change the local port the backend runs on, edit `applicationUrl` here. You would then also need to update the Vite proxy in `frontend/vite.config.ts` and the CORS settings in `appsettings.json`.

---

## 5. Frontend Tour — `frontend/`

The frontend is a **React 19** single-page application built with [Vite](https://vitejs.dev/) and written in [TypeScript](https://www.typescriptlang.org/).

```
frontend/
├── index.html              ← HTML template (single page)
├── package.json            ← Dependencies and scripts
├── package-lock.json       ← Locked dependency versions
├── vite.config.ts          ← Build tool configuration
├── tsconfig.json           ← TypeScript project references
├── tsconfig.app.json       ← App-level TypeScript settings
├── tsconfig.node.json      ← Node-level TypeScript settings (for Vite config)
├── eslint.config.js        ← Linting rules
├── .env.example            ← Environment variable template
└── src/
    ├── main.tsx            ← React entry point
    ├── App.tsx             ← Router definition
    ├── App.css             ← Global component styles
    ├── index.css           ← Base/reset styles
    ├── assets/             ← Static images, icons
    ├── components/         ← Reusable UI building blocks
    ├── hooks/              ← Custom React hooks (data fetching)
    ├── pages/              ← One component per URL route
    ├── services/           ← API client
    ├── types/              ← TypeScript type definitions
    └── utils/              ← Pure utility functions
```

### Mental Model for the Frontend

```
URL in browser bar
    ↓
App.tsx (React Router decides which page component to render)
    ↓
pages/ArticlesPage.tsx (the page, e.g.)
    ↓
hooks/useArticles.ts (fetches data, manages loading/error state)
    ↓
services/articleService.ts (makes the HTTP request with Axios)
    ↓
Backend API (returns JSON)
    ↓
hooks/useArticles.ts (stores result in React state)
    ↓
pages/ArticlesPage.tsx (renders components with that data)
    ↓
components/ArticleCard.tsx (renders each individual article)
```

---

### Entry Point: `main.tsx` and `index.html`

#### `index.html`

The single HTML file. Vite uses this as a template. The important line:
```html
<div id="root"></div>
<script type="module" src="/src/main.tsx"></script>
```
This is where React mounts. Everything you see in the browser is injected into `<div id="root">` by React.

#### `main.tsx`

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
)
```

**What it does:** Boots React. It finds the `#root` div, creates a React root in it, and renders `<App />`. `StrictMode` is a development tool that helps catch bugs — it renders components twice in development to detect side effects.

**When it matters:** Rarely. You almost never need to touch this file unless you are adding a global provider (like a theme context or authentication context) that needs to wrap the entire app.

---

### Router: `App.tsx`

```tsx
export default function App() {
  return (
    <BrowserRouter>
      <Header />
      <main className="main-content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/articles" element={<ArticlesPage />} />
          <Route path="/articles/new" element={<NewArticlePage />} />
          <Route path="/articles/:id" element={<ArticleDetailPage />} />
          <Route path="/articles/:id/edit" element={<EditArticlePage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}
```

**What it does:** Defines every URL the app knows about. When you navigate to `/articles/42`, React Router renders `ArticleDetailPage` with `id = "42"` available as a URL parameter.

**`BrowserRouter`:** Uses the HTML5 History API for clean URLs (no `#` in the URL bar). The browser sends a request for `/articles/42` — the Vite dev server serves `index.html` regardless of the path, and React Router takes over from there.

**`<Header />`:** Rendered outside of `<Routes>`, so it appears on every page.

**When it matters:** Every time you add a new page. You add a new `<Route>` here and create the corresponding page component.

---

### Pages

```
pages/
├── HomePage.tsx         ← Landing page (recent articles)
├── ArticlesPage.tsx     ← Full article list with search and filters
├── ArticleDetailPage.tsx← Read a single article
├── NewArticlePage.tsx   ← Create a new article
└── EditArticlePage.tsx  ← Edit an existing article
```

**What pages are:** Each page is a React component that corresponds to one URL route. Pages are responsible for fetching data (usually via a hook), managing page-level state (like current search query or selected filter), and composing components together into a layout.

**Pages are the right place for:**
- URL parameter reading (`useParams()`)
- Page-level state (`useState` for filters, search terms, etc.)
- Coordinating multiple components
- Navigation after actions (`useNavigate()`)

**Pages are NOT the right place for:**
- Making raw `fetch()` or `axios` calls (use hooks/services)
- Reusable UI elements (use components)

#### `ArticlesPage.tsx` — The Most Complex Page

This page handles: debounced search, category/tag/status filters, pagination, loading/error states, and the article grid. It is the best example of how pages, hooks, and components collaborate.

Key pattern — debounced search:
```tsx
const [search, setSearch] = useState('');
const [debouncedSearch, setDebouncedSearch] = useState('');

useEffect(() => {
  const timer = setTimeout(() => {
    setDebouncedSearch(search);  // Only update after 300ms of no typing
    setPage(1);
  }, 300);
  return () => clearTimeout(timer);  // Cancel if user keeps typing
}, [search]);
```

The user sees keystrokes in real time (`search` state), but the API is only called after the user stops typing for 300ms (`debouncedSearch` state). This prevents the API from being called on every keystroke.

#### `ArticleDetailPage.tsx`

Uses the `useArticle` hook to fetch a single article by its numeric ID (from the URL: `/articles/:id`). Renders the content as plain text — a future improvement would render it as Markdown HTML.

#### `NewArticlePage.tsx` and `EditArticlePage.tsx`

Both use `ArticleForm`. `EditArticlePage` first fetches the existing article to pre-populate the form, then submits a `PUT` request. `NewArticlePage` submits a `POST` request.

---

### Components

```
components/
├── ArticleCard.tsx     ← A single article summary card in the list view
├── ArticleForm.tsx     ← The create/edit form (reused by both New and Edit pages)
├── FilterControls.tsx  ← The category/tag/status dropdowns
├── Header.tsx          ← The top navigation bar
├── Pagination.tsx      ← Page navigation (Previous / 1 2 3 / Next)
├── SearchBar.tsx       ← The search text input
└── StateDisplay.tsx    ← Loading spinner, error message, empty state
```

**What components are:** Reusable pieces of UI. A component takes `props` (inputs) and returns JSX (HTML-like output). Components know nothing about the API — they only care about what they receive as props and what they show.

#### `ArticleCard.tsx`

Displays one article summary. Receives an `ArticleSummary` object as a prop, renders the title, category, tags, status badge, and a link to the detail page.

#### `ArticleForm.tsx`

The most complex component. It manages its own form state (`FormState`), validates on submit, and calls an `onSubmit` callback provided by the parent page. Notice that it does _not_ know whether it is creating or editing — that is the parent's concern. This makes it reusable.

Key beginner learning point: **controlled inputs**. Every `<input>` and `<textarea>` has a `value={form.field}` and an `onChange` handler that updates that field. React controls the value at all times — there is no direct DOM manipulation.

#### `StateDisplay.tsx`

Three tiny components in one file: `LoadingSpinner`, `ErrorMessage`, and `EmptyState`. These handle the three states that every data-fetching component needs to deal with. Centralizing them means consistent UI across all pages.

#### `Header.tsx`

The global navigation bar. Contains links to Home and Articles, and a "New Article" button. Lives outside `<Routes>` in `App.tsx`, so it always shows.

---

### Hooks

```
hooks/
├── useArticle.ts    ← Fetch one article by ID
└── useArticles.ts   ← Fetch a paginated/filtered list of articles
```

**What custom hooks are:** Functions that start with `use` and encapsulate stateful logic. They are React's way of sharing logic between components without copying and pasting.

#### `useArticles.ts`

```ts
export function useArticles(filters: ArticleFilters = {}): UseArticlesResult {
  const [data, setData] = useState<ArticleListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const key = JSON.stringify(filters);  // Serialize filters for use as a dependency key

  const fetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await articleService.list(filters);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load articles.');
    } finally {
      setLoading(false);
    }
  }, [key]);  // Re-run whenever filters change

  useEffect(() => { fetch(); }, [fetch]);

  return { data, loading, error, refetch: fetch };
}
```

**What it returns:** `{ data, loading, error, refetch }`. A page component calls this hook and gets back the data (or null if still loading), a loading flag, and an error string (or null if no error). The `refetch` function can be called to manually re-fetch (e.g., after a delete).

**Why `JSON.stringify(filters)` as a dependency key?** React's `useCallback` and `useEffect` need a stable way to know when to re-run. Objects in JavaScript are compared by reference, so `{ search: 'foo' } !== { search: 'foo' }`. Serializing to a string makes the comparison work correctly.

**Common beginner confusion:** Why is `fetch` wrapped in `useCallback`? Without it, a new `fetch` function would be created on every render, causing `useEffect` to re-run on every render (infinite loop). `useCallback` memoizes the function so it only changes when `key` changes.

---

### Services

```
services/
└── articleService.ts
```

**What it does:** A single Axios-based API client. All HTTP calls to the backend go through this file. It knows the API's URL structure and maps TypeScript types to HTTP calls.

```ts
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5018',
  headers: { 'Content-Type': 'application/json' },
});

export const articleService = {
  async list(filters: ArticleFilters = {}): Promise<ArticleListResponse> { ... },
  async getById(id: number): Promise<Article> { ... },
  async create(request: CreateArticleRequest): Promise<Article> { ... },
  async update(id: number, request: UpdateArticleRequest): Promise<Article> { ... },
  async delete(id: number): Promise<void> { ... },
  async getCategories(): Promise<string[]> { ... },
  async getTags(): Promise<string[]> { ... },
};
```

**`import.meta.env.VITE_API_URL`:** Vite exposes environment variables prefixed with `VITE_` in the app. If you define `VITE_API_URL=https://api.example.com` in a `.env` file, the service will use that URL instead of the localhost default.

**`getErrorMessage(error)`:** A utility function exported from this file that extracts a human-readable message from an Axios error. The backend returns structured error bodies (especially for validation failures), and this function knows how to unwrap them.

**Why a service layer?** If you decide to replace Axios with the native `fetch` API, or change the base URL structure, you only change this one file — no page or hook needs to know.

---

### Types

```
types/
└── index.ts
```

**What it does:** The single source of truth for all TypeScript types shared across the frontend. Defines the shapes of objects the API returns and what the app sends.

```ts
export interface Article extends ArticleSummary {
  content: string;  // Only full articles have this
}

export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;  // 'Draft' | 'Published' | 'Archived'
  createdAt: string;      // ISO date string (not Date object)
  updatedAt: string;
}
```

**Why `createdAt: string` instead of `Date`?** JSON does not have a native Date type. Dates come over the wire as ISO strings (e.g., `"2026-03-15T23:58:34Z"`). The `utils/format.ts` file handles converting these strings to human-readable display.

**Why this file matters:** If the backend changes what it returns (e.g., adds a new field), you update this file first. TypeScript will then show you every place in the frontend that needs to handle the new field.

---

### Utils

```
utils/
└── format.ts
```

**What it does:** Pure utility functions with no side effects. Currently contains date and string formatting helpers.

**Example — formatting a date string:**
```ts
export function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    year: 'numeric', month: 'long', day: 'numeric'
  });
}
// "March 15, 2026"
```

**Safe to modify:** These are pure functions (input → output, no state). They are easy to test and safe to change.

---

### Styles: `App.css` and `index.css`

#### `index.css`

**What it does:** Base/reset styles. Sets font families, removes default margins, and defines CSS custom properties (variables) for the color palette and spacing. Applied globally to every element.

**Key patterns:** CSS custom properties like `--color-primary` mean you can change the entire color scheme in one place.

#### `App.css`

**What it does:** All component and layout styles for the application. Classes like `.article-grid`, `.article-card`, `.btn`, `.form-group`, `.page` are defined here.

**When it matters:** Any time you want to change how something looks. The class names are fairly descriptive.

**Note:** There is no CSS module system or styled-components in this project. All styles are global. If you add a component, add its styles to `App.css` and use unique, descriptive class names to avoid collisions.

---

### Config Files

#### `vite.config.ts`

```ts
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5018',
        changeOrigin: true
      }
    }
  }
})
```

**What it does:** Configures Vite, the build tool and development server.

The most important part is the **proxy**: any request the frontend makes to a URL starting with `/api` is forwarded to `http://localhost:5018`. This is why the `articleService.ts` can use `/api/articles` without specifying a full hostname — Vite rewrites it during development.

**Common beginner confusion:** The proxy only works in development (`npm run dev`). When you build the app for production (`npm run build`), the proxy is gone. In production, you either host the frontend and backend on the same domain, or set `VITE_API_URL` to the full backend URL.

#### `package.json`

```json
{
  "scripts": {
    "dev": "vite",           // Start dev server with hot reload
    "build": "tsc -b && vite build",  // Type-check then build for production
    "lint": "eslint .",      // Check code quality
    "preview": "vite preview" // Preview the production build locally
  }
}
```

**Key dependencies:**

| Package | Purpose |
|---------|---------|
| `react` + `react-dom` | The React library |
| `react-router-dom` | Client-side routing |
| `axios` | HTTP client for API calls |

**Key dev dependencies:**

| Package | Purpose |
|---------|---------|
| `vite` | Build tool and dev server |
| `typescript` | TypeScript compiler |
| `@vitejs/plugin-react` | Vite plugin for React JSX transformation |
| `eslint` + plugins | Code linting |

#### `tsconfig.app.json`

The TypeScript configuration for the application source. Key settings:
- `"strict": true` — Enables all strict type checks. This catches many bugs at compile time.
- `"moduleResolution": "bundler"` — Modern resolution strategy optimized for bundlers like Vite.
- `"jsx": "react-jsx"` — Enables the new JSX transform (no need to `import React` in every file).

#### `eslint.config.js`

ESLint v9 flat configuration. Enforces:
- React Hooks rules (prevents `useEffect` dependency array mistakes)
- TypeScript type safety rules
- React-specific best practices

Run with `npm run lint`. Fix many issues automatically with `npm run lint -- --fix`.

---

## 6. If You Only Have One Hour, Read These Files

If your time is limited, these seven files will give you the deepest understanding of how the app works. Read them in this order:

| # | File | Time | What You Learn |
|---|------|------|----------------|
| 1 | `src/WikiProject.Api/Program.cs` | 5 min | How the entire backend is wired together |
| 2 | `src/WikiProject.Api/Entities/Article.cs` | 2 min | The core data model |
| 3 | `src/WikiProject.Api/DTOs/ArticleDtos.cs` | 3 min | The contract between frontend and backend |
| 4 | `src/WikiProject.Api/Services/ArticleService.cs` | 15 min | All the business logic; how data is queried, created, and mutated |
| 5 | `frontend/src/types/index.ts` | 3 min | The frontend's view of the data model |
| 6 | `frontend/src/services/articleService.ts` | 5 min | How the frontend talks to the backend |
| 7 | `frontend/src/pages/ArticlesPage.tsx` | 10 min | The most complex page; shows how pages, hooks, and components work together |

After reading these seven files, you will understand:
- What data the app stores and how it is structured
- The full path of a request from browser to database and back
- Where to add a new feature, endpoint, or UI element

---

## 7. Files That Are Safe for Beginners to Change

These files have low blast radius — a mistake here is unlikely to break something far away, and the purpose is obvious.

### Backend

| File | Why it is safe | What you might change |
|------|---------------|-----------------------|
| `Data/SeedData.cs` | Only runs on first startup; does not affect production data | Add, remove, or change sample articles and tags |
| `Validation/ArticleValidators.cs` | Only affects request validation rules | Adjust character limits, add new validation rules |
| `appsettings.Development.json` | Dev-only config; not used in production | Change log levels, adjust dev-specific settings |

### Frontend

| File | Why it is safe | What you might change |
|------|---------------|-----------------------|
| `frontend/src/utils/format.ts` | Pure functions with no side effects | Change date format, add a new formatting helper |
| `frontend/src/components/StateDisplay.tsx` | Simple display-only components | Improve loading spinner, error messages, empty states |
| `frontend/src/components/Header.tsx` | Only the nav bar | Add/rename links, adjust branding |
| `frontend/src/App.css` | Styles only | Change colors, spacing, card layout |
| `frontend/src/index.css` | Base styles | Update CSS variables (colors, fonts) |
| `frontend/src/components/ArticleCard.tsx` | Isolated component with clear props | Change how articles are displayed in the list |
| `frontend/src/components/Pagination.tsx` | Isolated component | Change page navigation appearance |

### General Tips for Safe Changes

- **Add before you modify.** If you want to change a utility function, consider adding a new function rather than modifying the existing one until you are confident.
- **Test in the browser.** For frontend changes, watch the browser console for errors.
- **Check the Swagger UI.** For backend changes, `http://localhost:5018/swagger` shows you the API live.

---

## 8. Files to Be Careful With

These files have high impact. A mistake here can break the entire application, corrupt the database, or cause subtle bugs that are hard to trace.

### Backend

| File | Why to be careful | Common mistakes |
|------|------------------|----|
| `Program.cs` | Any error here prevents the app from starting at all | Removing a `builder.Services.Add*` call breaks dependency injection; the app will throw at startup or when the missing service is first needed |
| `Data/WikiDbContext.cs` | Changes here feed into migration generation | Removing an index or relationship can cause data integrity issues; always create a migration after changes |
| `Migrations/` (entire folder) | Manual edits here corrupt migration history | Never edit migration files by hand. Use `dotnet ef migrations add` and `dotnet ef database update` |
| `Services/ArticleService.cs` | Core business logic; all data flow passes through here | Bugs here affect every feature; test thoroughly after changes. Particularly careful with `UpdateAsync` — it replaces the tag list entirely |
| `Entities/*.cs` | Entities map directly to database tables | Removing or renaming a property requires a migration. Adding a non-nullable property without a default value will fail |

### Frontend

| File | Why to be careful | Common mistakes |
|------|------------------|----|
| `frontend/src/types/index.ts` | All components and hooks depend on these types | Removing or renaming a field breaks all places that use it; TypeScript will catch them, but there may be many |
| `frontend/src/services/articleService.ts` | All API calls go through here | Changing a method signature breaks all hooks that use it; changing the base URL affects all endpoints |
| `frontend/src/App.tsx` | Defines all routes | Removing a route makes that URL return a blank page; changing a path parameter name (`:id`) breaks pages that read `useParams()` |
| `frontend/src/hooks/useArticles.ts` | Used by `ArticlesPage`; manages complex state | The `JSON.stringify(filters)` dependency key is intentional — removing it causes stale data or infinite loops |
| `frontend/src/components/ArticleForm.tsx` | Used for both create and edit | The `toStatusNumber` function and the numeric status mapping are critical; getting this wrong silently saves wrong status values |

### Database-Specific Cautions

- **Do not delete `wiki.db` in a shared environment.** That is the live database. In development, deleting it resets everything, which is fine.
- **Do not modify migration files after they have been applied** to any environment. This is the number one cause of "the model used to create the database does not match the current model" errors.
- **Avoid renaming Entity properties.** EF Core cannot tell the difference between a rename and a delete + add. Use `HasColumnName()` in `OnModelCreating` if you need to rename a column without dropping it.

---

## 9. The Data Flow: How a Request Travels End-to-End

Let us trace exactly what happens when a user types "authentication" in the search box and presses Enter.

### 1. User Input (Frontend — `ArticlesPage.tsx`)

```tsx
const [search, setSearch] = useState('');
<SearchBar value={search} onChange={(v) => setSearch(v)} />
```

The search state updates to `"authentication"`.

### 2. Debounce (Frontend — `ArticlesPage.tsx`)

```tsx
useEffect(() => {
  const timer = setTimeout(() => setDebouncedSearch(search), 300);
  return () => clearTimeout(timer);
}, [search]);
```

After 300ms of no new keystrokes, `debouncedSearch` updates to `"authentication"`.

### 3. Hook Triggers (Frontend — `useArticles.ts`)

```ts
const { data, loading, error } = useArticles({
  search: debouncedSearch,  // "authentication"
  page: 1,
  pageSize: 12,
});
```

`useArticles` detects that `filters` changed (via `JSON.stringify`) and calls `articleService.list(filters)`.

### 4. HTTP Request (Frontend — `articleService.ts`)

```ts
await api.get<ArticleListResponse>('/api/articles', {
  params: { search: 'authentication', page: 1, pageSize: 12 }
});
```

Axios sends: `GET http://localhost:5018/api/articles?search=authentication&page=1&pageSize=12`

### 5. Controller Receives (Backend — `ArticlesController.cs`)

```csharp
[HttpGet]
public async Task<ActionResult<ArticleListResponse>> GetArticles(
    [FromQuery] string? search, ...)
{
    var query = new ArticleQueryParams(search, ...);
    var result = await _articleService.GetArticlesAsync(query);
    return Ok(result);
}
```

ASP.NET Core parses the query string into method parameters. `search = "authentication"`.

### 6. Service Builds the Query (Backend — `ArticleService.cs`)

```csharp
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var search = query.Search.ToLower();  // "authentication"
    q = q.Where(a =>
        a.Title.ToLower().Contains(search) ||
        a.Summary.ToLower().Contains(search) ||
        a.Content.ToLower().Contains(search) || ...);
}
```

EF Core translates this LINQ query to SQL:
```sql
SELECT * FROM Articles
WHERE LOWER(Title) LIKE '%authentication%'
   OR LOWER(Summary) LIKE '%authentication%'
   OR LOWER(Content) LIKE '%authentication%'
ORDER BY UpdatedAt DESC
LIMIT 12 OFFSET 0
```

### 7. Database Returns Results

SQLite finds matching rows and returns them to EF Core.

### 8. Mapping (Backend — `ArticleMappings.cs`)

Each `Article` entity is mapped to `ArticleSummaryDto` via the `.ToSummaryDto()` extension method.

### 9. JSON Response (Backend → Frontend)

ASP.NET Core serializes the `ArticleListResponse` record to JSON and sends it back with status `200 OK`.

### 10. Frontend Updates (Frontend — `useArticles.ts` → `ArticlesPage.tsx`)

```ts
setData(result);   // Triggers re-render
setLoading(false);
```

React re-renders `ArticlesPage`, which maps `data.items` to `<ArticleCard />` components.

---

## 10. Quick Reference: Every Important File at a Glance

### Backend Files

| File | Purpose | When it runs / matters |
|------|---------|------------------------|
| `Program.cs` | App startup and service wiring | On every app start |
| `appsettings.json` | Connection strings and CORS config | On app start; read once |
| `appsettings.Development.json` | Dev overrides | On app start in Development env |
| `Controllers/ArticlesController.cs` | HTTP endpoints for articles | On every article HTTP request |
| `Controllers/MetadataController.cs` | HTTP endpoints for categories and tags | When filter dropdowns are populated |
| `Services/IArticleService.cs` | Interface contract for the service | At DI registration time; guides implementation |
| `Services/ArticleService.cs` | All business logic | On every article-related operation |
| `DTOs/ArticleDtos.cs` | Request/response data shapes | Deserialized from requests; serialized to responses |
| `Entities/Article.cs` | Database table definition | At migration time; at EF Core query time |
| `Entities/ArticleStatus.cs` | Status enum | Wherever status is set or read |
| `Entities/ArticleTag.cs` | Join table entity | On tag association queries |
| `Entities/Tag.cs` | Tag table definition | Whenever tags are queried or created |
| `Data/WikiDbContext.cs` | EF Core database context | On every database query |
| `Data/SeedData.cs` | Sample data seeder | On first app startup only |
| `Mappings/ArticleMappings.cs` | Entity → DTO converters | In service methods before returning data |
| `Validation/ArticleValidators.cs` | Input validation rules | On POST and PUT requests |
| `Migrations/InitialCreate.cs` | Schema creation migration | On `db.Database.Migrate()` (first startup) |

### Frontend Files

| File | Purpose | When it runs / matters |
|------|---------|------------------------|
| `index.html` | HTML template | Every page load |
| `main.tsx` | React bootstrap | Once, on app load |
| `App.tsx` | Route definitions | On every URL change |
| `pages/HomePage.tsx` | Landing page | When user visits `/` |
| `pages/ArticlesPage.tsx` | Article list with search/filter | When user visits `/articles` |
| `pages/ArticleDetailPage.tsx` | Single article view | When user visits `/articles/:id` |
| `pages/NewArticlePage.tsx` | Article creation | When user visits `/articles/new` |
| `pages/EditArticlePage.tsx` | Article editing | When user visits `/articles/:id/edit` |
| `components/ArticleCard.tsx` | Article summary card | In ArticlesPage and HomePage |
| `components/ArticleForm.tsx` | Create/edit form | In NewArticlePage and EditArticlePage |
| `components/FilterControls.tsx` | Search filters | In ArticlesPage |
| `components/Header.tsx` | Navigation bar | On every page |
| `components/Pagination.tsx` | Page navigation | In ArticlesPage |
| `components/SearchBar.tsx` | Search text input | In ArticlesPage |
| `components/StateDisplay.tsx` | Loading/error/empty states | Everywhere data is fetched |
| `hooks/useArticle.ts` | Fetch single article | In ArticleDetailPage and EditArticlePage |
| `hooks/useArticles.ts` | Fetch article list | In ArticlesPage and HomePage |
| `services/articleService.ts` | API HTTP client | Called by hooks |
| `types/index.ts` | TypeScript interfaces | Compile time; referenced everywhere |
| `utils/format.ts` | Date/string formatting | In components that display dates |
| `App.css` | Component styles | Loaded once at startup |
| `index.css` | Base styles and CSS variables | Loaded once at startup |
| `vite.config.ts` | Dev server and build config | At `npm run dev` or `npm run build` |
| `package.json` | Dependencies and scripts | At `npm install` |

---

> **Related documentation (produced by other agents):**
> - `docs/01_PROJECT_OVERVIEW.md` — High-level purpose, architecture, and tech stack choices
> - `docs/02_BACKEND_ARCHITECTURE.md` — Deep dive into the N-tier backend, EF Core, and service patterns
> - `docs/03_FRONTEND_ARCHITECTURE.md` — React component model, hooks, and state management in depth
> - `docs/04_API_REFERENCE.md` — Complete API endpoint documentation with request/response examples
> - `docs/05_DATABASE_AND_MIGRATIONS.md` — EF Core schema, migrations workflow, and SQLite details
> - `docs/06_DEVELOPMENT_SETUP.md` — Step-by-step local environment setup guide
> - `docs/08_CONTRIBUTING.md` — How to contribute, coding conventions, and PR guidelines
