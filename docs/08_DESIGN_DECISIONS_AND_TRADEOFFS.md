# Design Decisions and Tradeoffs

> **Who this is for:** A newer developer who is smart and motivated but may be wondering _why_ the project is built the way it is — not just _how_ it works. Engineering is not about picking perfect tools; it is about making reasonable tradeoffs given a set of constraints. This document explains those tradeoffs for WikiProject, decision by decision.

> **Scope:** This file captures the major architectural and technical choices made in this project. It does not describe every file or every line of code; other documentation files in this `docs/` directory cover those areas in detail. Where a related doc is relevant, it is mentioned as a pointer.

---

## Table of Contents

1. [React for the Frontend](#1-react-for-the-frontend)
2. [ASP.NET Core for the Backend](#2-aspnet-core-for-the-backend)
3. [Entity Framework Core as the ORM](#3-entity-framework-core-as-the-orm)
4. [SQLite as the Database](#4-sqlite-as-the-database)
5. [The Service Layer](#5-the-service-layer)
6. [DTO Separation](#6-dto-separation)
7. [Folder Organisation](#7-folder-organisation)
8. [Search and Filter Design](#8-search-and-filter-design)
9. [Frontend / Backend Split](#9-frontend--backend-split)
10. [FluentValidation for Input Validation](#10-fluentvalidation-for-input-validation)
11. [Slug-Based Routing](#11-slug-based-routing)
12. [Tag Normalisation Strategy](#12-tag-normalisation-strategy)
13. [Summary: The Big Picture](#13-summary-the-big-picture)

---

## 1. React for the Frontend

### What was chosen

**React 19** (the latest stable release at time of writing) with **TypeScript**, bundled and served by **Vite 8**. Client-side routing is handled by **React Router DOM v7**. HTTP communication with the backend is done via **Axios**.

The relevant files are in `frontend/src/`. The entry point is `frontend/src/main.tsx`, the routing configuration lives in `frontend/src/App.tsx`, and components are split between `frontend/src/components/` (reusable pieces) and `frontend/src/pages/` (full-page views).

### What problem it solves

A wiki is a read-heavy, interactive application. Users expect to search, filter, paginate and navigate articles without the page doing a full server round-trip on every click. React allows the browser to own the UI state and update only the parts of the page that have changed, making the experience feel fast and responsive.

### Why it fits this project

- **Component model matches the UI.** An article card, a search bar, a pagination control, and a filter panel are all self-contained visual pieces. React's component model maps directly onto that mental model.
- **TypeScript integration is first-class.** Vite scaffolds TypeScript out of the box. TypeScript catches errors at edit time (e.g., passing the wrong shape of data to a component) rather than at runtime in the browser.
- **Ecosystem size.** If the project later needs a Markdown renderer, a rich text editor, or a date picker, every one of those has a well-maintained React package.
- **Vite dev server proxy.** `vite.config.ts` forwards any request whose path starts with `/api` to the ASP.NET Core backend at `http://localhost:5018`. This means the frontend developer never has to configure CORS headers or change the URL they type into an address bar. See the `server.proxy` block in `vite.config.ts`.

### Pros

| Benefit | Detail |
|---|---|
| Declarative UI | You describe _what_ the UI should look like for a given state, not _how_ to mutate the DOM step by step |
| Fast iteration | Vite's hot-module replacement (HMR) applies your code change to the browser in milliseconds, without losing UI state |
| Composability | Small, focused components are easy to test, move, and reuse |
| Huge ecosystem | Libraries exist for nearly every UI requirement |
| Type safety | TypeScript catches shape mismatches between what the API returns and what the component expects |

### Cons

| Drawback | Detail |
|---|---|
| JavaScript bundle size | Even a minimal React app ships ~50 KB of framework code. For a wiki this is fine; for a very low-bandwidth audience it may matter |
| Client-rendered | Search engines index the initial HTML. Because this app renders in the browser, the article content is not present in the initial HTML. This is fine for an internal wiki; it matters for public SEO |
| Learning curve | Beginners can struggle with the mental model of "state drives the UI" versus the older DOM-mutation style |

### Common beginner confusion

> **"Why do my state changes not show up immediately?"**
>
> React state updates are _asynchronous and batched_. When you call `setState`, React schedules a re-render — it does not run synchronously on that line. If you log the state variable on the very next line, you will still see the old value. The new value appears on the next render.

> **"Why is my component rendering twice in development?"**
>
> React 18+ runs effects twice in development mode (in React Strict Mode) to help you catch side effects that are not properly cleaned up. This does not happen in production.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Server-Side Rendering (SSR) with Next.js** | When SEO matters. Next.js renders pages on the server so search engine crawlers see real content |
| **Plain HTML + HTMX** | When the UI is mostly server-driven and you want to avoid a JavaScript build step entirely. HTMX lets a server return HTML fragments and swap them into the page |
| **Vue 3 or Svelte** | Smaller bundle, gentler learning curve. If the team is more comfortable with these, the tradeoffs are roughly equivalent |
| **Blazor (WebAssembly)** | If the whole team knows C#, Blazor lets you write the frontend in C#. Slower initial load, but a single language across the stack |

### What would justify revisiting this decision

- The wiki becomes public-facing and SEO becomes important → migrate to Next.js for SSR.
- The development team is entirely C# developers with no JavaScript experience → consider Blazor.
- Bundle size becomes a meaningful concern (e.g., targeting very slow network regions) → consider Svelte or a lighter framework.

---

## 2. ASP.NET Core for the Backend

### What was chosen

**ASP.NET Core 10** (targeting `net10.0` in `WikiProject.Api.csproj`) running as a minimal-hosting-model Web API. The application starts from `src/WikiProject.Api/Program.cs`. There is no separate `Startup.cs` — the newer top-level statement style is used throughout.

### What problem it solves

The frontend is a JavaScript application running in the browser. The browser cannot safely talk directly to a database (it has no network path, and doing so would expose credentials to every visitor). The backend is a trusted intermediary: it receives HTTP requests, validates them, applies business rules, queries the database, and returns clean JSON responses.

### Why it fits this project

- **Mature, well-documented framework.** ASP.NET Core has thorough official documentation, a large community, and regular long-term-support (LTS) releases.
- **C# is statically typed.** Type safety catches many bugs before they reach production.
- **Dependency injection is built in.** You register services in `Program.cs` with `builder.Services.AddScoped<...>()` and ASP.NET Core injects them into controllers and services automatically. No third-party DI container is needed.
- **Swagger is built in.** Adding `Swashbuckle.AspNetCore` gives a browsable API documentation page at `/swagger` in development. This is invaluable when working across teams.
- **.NET 10 performance.** Modern .NET is among the fastest application server platforms available, comfortably handling thousands of requests per second on modest hardware.

### A quick tour of `Program.cs`

```csharp
// 1. Build the app's service container
var builder = WebApplication.CreateBuilder(args);

// 2. Register services (DI)
builder.Services.AddControllers();
builder.Services.AddScoped<IArticleService, ArticleService>();
// ... validators, DbContext, CORS, etc.

// 3. Build the HTTP pipeline
var app = builder.Build();

// 4. Add middleware (in order)
app.UseCors(...);
app.UseAuthorization();
app.MapControllers();

// 5. Run
app.Run();
```

The two-phase separation (register services → build pipeline) is a deliberate design in ASP.NET Core. Services are resolved lazily at request time, not eagerly at startup.

### Pros

| Benefit | Detail |
|---|---|
| Strong typing | Catches shape mismatches between request/response and service layer at compile time |
| Built-in DI | No extra library needed; encourages loose coupling |
| Attribute routing | `[HttpGet("{id}")]` directly on a method makes routes obvious |
| Middleware pipeline | Authentication, CORS, logging, and exception handling are composable middleware layers |
| Cross-platform | Runs on Linux, macOS and Windows; easily containerised |

### Cons

| Drawback | Detail |
|---|---|
| Verbosity | Compared to a Python/Flask or Node/Express backend, ASP.NET Core requires more boilerplate (controllers, DTOs, validators, services) |
| .NET SDK required | Developers need the .NET SDK installed; not universally familiar to all web developers |
| Longer cold start | In serverless or container scenarios, .NET apps can have a longer cold-start than Node.js, though this is much improved in .NET 8+ with NativeAOT and trimming |

### Common beginner confusion

> **"Where does the HTTP request actually arrive?"**
>
> Kestrel (the built-in ASP.NET Core web server) receives the raw TCP connection. It parses the HTTP headers and body, then passes a `HttpContext` object through the middleware pipeline. Eventually it reaches the routing middleware, which matches the URL pattern to a controller action method.

> **"What is `IActionResult` / `ActionResult<T>`?"**
>
> Controller methods can return `ActionResult<T>`, which is a union type that can be either a typed value `T` (which is serialised to JSON automatically) or an HTTP status code result like `NotFound()`, `BadRequest()`, or `Ok()`. This lets a method express both the happy path and the error paths cleanly.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Node.js / Express / Fastify** | When the team is primarily JavaScript developers. Shares the language with the frontend |
| **Python / FastAPI** | Excellent for data-heavy work or when the team has a Python background |
| **Go / Gin or Echo** | Extremely low memory footprint; good for microservices |
| **Java / Spring Boot** | Dominant in enterprise environments; similar patterns to ASP.NET Core |

### What would justify revisiting this decision

- The team shifts to a JavaScript-only codebase → Node.js backend would eliminate context switching.
- The project needs to be serverless → FastAPI on AWS Lambda or a small Go binary has a better cold-start story.
- Extreme performance requirements → Go or Rust backends have lower per-request overhead.

---

## 3. Entity Framework Core as the ORM

### What was chosen

**Entity Framework Core 10 (EF Core)** with the SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite`). The `DbContext` is `WikiDbContext` in `src/WikiProject.Api/Data/WikiDbContext.cs`. Entities are in `src/WikiProject.Api/Entities/`. Migrations live in `src/WikiProject.Api/Migrations/`.

An ORM (Object-Relational Mapper) is a library that lets you write database queries using your programming language's objects and methods instead of raw SQL strings. EF Core translates your C# LINQ expressions into SQL behind the scenes.

### What problem it solves

Without an ORM you would write SQL strings inside your C# code:
```csharp
// Raw SQL — fragile, no compile-time checking, manual mapping
var sql = "SELECT Id, Title FROM Articles WHERE Title LIKE @search";
```

With EF Core you write this instead:
```csharp
// Strongly typed, refactoring-safe, no SQL injection risk
var articles = await _db.Articles
    .Where(a => a.Title.Contains(search))
    .ToListAsync();
```

EF Core also manages the database schema through **migrations** — version-controlled SQL scripts generated from your C# model classes.

### Why it fits this project

- **Single language.** All data access logic is in C#. No SQL context-switching.
- **Migration workflow.** `dotnet ef migrations add <Name>` generates a migration file; `dotnet ef database update` applies it. The project applies migrations automatically on startup via `db.Database.MigrateAsync()` in `Program.cs`.
- **LINQ integration.** Filtering, sorting and pagination are expressed as LINQ chains that EF Core translates to efficient SQL.
- **AsNoTracking for reads.** Query results for the article list use `.AsNoTracking()`, which skips EF Core's change-tracking overhead since we are only reading data.
- **Include for eager loading.** The query uses `.Include(a => a.ArticleTags).ThenInclude(at => at.Tag)` to load related tags in a single SQL JOIN rather than issuing N+1 queries (one per article).

### The N+1 problem (important concept)

This is one of the most common performance bugs when using ORMs. Imagine you load 20 articles without `.Include()`:
```csharp
// BAD: 1 query for articles + 20 queries for tags = 21 queries
var articles = await _db.Articles.ToListAsync();
foreach (var a in articles)
{
    var tags = a.ArticleTags; // EF Core lazily queries each time
}
```

With `.Include()`:
```csharp
// GOOD: 1 query with a JOIN — all tags loaded upfront
var articles = await _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .ToListAsync();
```

The project correctly uses the second form.

### Pros

| Benefit | Detail |
|---|---|
| No raw SQL for common operations | CRUD, filtering, sorting, pagination all in C# |
| Migration history | Schema changes are version-controlled and reproducible |
| Change tracking | EF Core knows which properties changed and generates minimal UPDATE SQL |
| Provider portability | Switching from SQLite to PostgreSQL requires only changing the NuGet provider package and the connection string |

### Cons

| Drawback | Detail |
|---|---|
| Generated SQL can be suboptimal | Complex LINQ chains can produce SQL that is harder to tune than hand-written SQL |
| Abstraction leaks | Advanced SQL features (window functions, CTEs, full-text search) require dropping to raw SQL with `FromSql()` or `ExecuteSqlRaw()` |
| Magic can confuse | Beginners may not realise that `a.ArticleTags` is a database query, not a simple property access |
| Migration conflicts | In teams, two developers adding migrations simultaneously will cause conflicts |

### Common beginner confusion

> **"I changed my model class but the database didn't update."**
>
> EF Core does not watch your C# classes and auto-update the database. You must explicitly run:
> ```bash
> dotnet ef migrations add YourMigrationName
> dotnet ef database update
> ```
> The first command generates a new migration file in the `Migrations/` folder. The second command applies all pending migrations to the database. This project applies migrations automatically at startup, so you just need to add the migration file.

> **"Why does `dbContext.Articles` not return all articles?"**
>
> `dbContext.Articles` is an `IQueryable<Article>`, not a `List<Article>`. The database query is not executed until you call a terminal method like `.ToListAsync()`, `.FirstOrDefaultAsync()`, or `.CountAsync()`. You can chain `.Where()`, `.OrderBy()`, `.Include()` etc. before materialising the query — all of it composes into a single SQL statement.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Dapper** | A micro-ORM: you write SQL, Dapper maps results to C# objects. Great for performance-critical or complex queries |
| **Raw ADO.NET** | Maximum control, zero abstraction. Appropriate when every query needs to be hand-tuned |
| **NHibernate** | A full-featured ORM with a longer history; more configuration, more power |
| **No ORM (stored procedures)** | In large enterprise environments with a dedicated DBA team |

### What would justify revisiting this decision

- Query performance becomes critical and the generated SQL cannot be tuned enough → add Dapper for specific hot paths or switch to it entirely.
- The team needs advanced SQL features (full-text search with ranking, geospatial queries) that EF Core cannot express cleanly → use `FromSql()` for those specific queries.

---

## 4. SQLite as the Database

### What was chosen

**SQLite** — a file-based relational database stored in a single file, `wiki.db`, in `src/WikiProject.Api/`. Connection string: `"Data Source=wiki.db"` in `appsettings.json`.

### What problem it solves

Every application needs persistent storage. SQLite stores all data in a single `.db` file that lives alongside the application. No separate database server process is needed. You can check the file into a backup, copy it to another machine, or delete it to start fresh.

### Why it fits this project

- **Zero infrastructure.** There is no PostgreSQL server to install, no MySQL daemon to configure. `dotnet run` is all you need to get a running application.
- **Learning focus.** This project is a teaching vehicle. SQLite removes an entire category of setup friction, letting new developers focus on the application code.
- **EF Core portability.** Because data access goes through EF Core, switching to PostgreSQL later requires only changing one NuGet package and one connection string. The application code changes very little.
- **Appropriate data scale.** A wiki for a small team or learning project will contain hundreds to low thousands of articles. SQLite handles millions of rows comfortably for read-heavy workloads.

### Pros

| Benefit | Detail |
|---|---|
| No server process | Nothing to install, configure, or keep running |
| Single file backup | `cp wiki.db wiki.db.bak` is a complete backup |
| ACID compliant | SQLite provides real transactions, not eventual consistency |
| Fast for reads | For single-user or lightly concurrent workloads, SQLite is extremely fast |

### Cons

| Drawback | Detail |
|---|---|
| Write concurrency | SQLite uses a file-level write lock. Only one writer at a time. Under high write concurrency this becomes a bottleneck |
| No network mode | SQLite cannot be accessed from multiple machines simultaneously (without extensions like Litestream or Turso) |
| Limited SQL features | Some advanced SQL features (e.g., RETURNING clause for older versions, certain window functions) are not supported |
| Not production-standard | Most production deployments use a server database (PostgreSQL, MySQL, SQL Server) with proper connection pooling, replication, and backups |

### Common beginner confusion

> **"I changed the database schema but the app is crashing on startup."**
>
> The `wiki.db` file on disk has an older schema. The migration system detected a mismatch. Delete `wiki.db` (all data will be lost — that is fine in development), then re-run the app. The migrations will recreate the schema from scratch.

> **"Can I use SQLite in production?"**
>
> SQLite is used in production by many applications (it powers most mobile apps). However, it is not designed for multi-user web applications with concurrent writes. For a wiki with real users, migrate to PostgreSQL. EF Core makes this straightforward.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **PostgreSQL** | The recommended upgrade path: full-featured, concurrent, production-ready, free and open source |
| **SQL Server** | Natural fit if the team is already in the Microsoft ecosystem (Azure SQL, SSMS tooling) |
| **MySQL / MariaDB** | Widely deployed, good support, familiar to many developers |
| **MongoDB** | A document database (stores JSON, not tables). Good fit if articles have highly variable structure, but adds complexity for relational data like tags |

### What would justify revisiting this decision

- The project is deployed to a shared server or cloud → PostgreSQL is the standard choice.
- Multiple instances of the application need to run simultaneously → file-based SQLite cannot be shared between processes on different machines.
- Write throughput becomes a bottleneck → any server database handles concurrent writes better.

### Switching from SQLite to PostgreSQL: what actually changes

1. Remove `Microsoft.EntityFrameworkCore.Sqlite` NuGet package.
2. Add `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package.
3. In `Program.cs`, change `options.UseSqlite(...)` to `options.UseNpgsql(...)`.
4. Update the connection string in `appsettings.json`.
5. Delete all existing migrations and re-generate them (the SQL dialect is different).
6. Done. No service or controller code changes needed.

---

## 5. The Service Layer

### What was chosen

Business logic lives in a **service layer** — specifically `IArticleService` and its implementation `ArticleService` in `src/WikiProject.Api/Services/`. Controllers receive `IArticleService` via constructor injection and call its methods. Controllers themselves contain no data access code.

### What problem it solves

Without a service layer, controllers become god objects: they handle HTTP routing, input validation, database queries, business logic, and response formatting all in one place. This is sometimes called a "fat controller" anti-pattern. Fat controllers are hard to test, hard to reason about, and hard to change without breaking something unrelated.

The service layer extracts the "what should happen" logic away from the "how do I respond over HTTP" logic.

### How it works in this project

```csharp
// Controller: only HTTP concerns
[HttpGet]
public async Task<ActionResult<ArticleListResponse>> GetArticles(
    [FromQuery] ArticleQueryParams query)
{
    var result = await _articleService.GetArticlesAsync(query);
    return Ok(result);
}

// Service: only business/data concerns
public async Task<ArticleListResponse> GetArticlesAsync(ArticleQueryParams query)
{
    var q = _db.Articles
        .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
        .AsNoTracking()
        .AsQueryable();
    // ... filter, sort, paginate ...
    return new ArticleListResponse(...);
}
```

The controller does not know how articles are fetched. The service does not know whether the caller is an HTTP endpoint or a background job or a unit test.

### Why it fits this project

- **Testability.** In a future test suite you can create a mock implementation of `IArticleService` and test the controller without a database. You can also test the real `ArticleService` against an in-memory SQLite database without needing a running web server.
- **Single responsibility.** Each class does one job. The controller routes HTTP; the service orchestrates data access.
- **Replaceability.** If you later need to add caching (e.g., "only re-query the database every 30 seconds for the categories list"), you add a caching decorator that wraps the real `ArticleService`, without changing the controller at all.

### Pros

| Benefit | Detail |
|---|---|
| Testability | Services can be tested without a running HTTP server |
| Separation of concerns | Clear boundary between HTTP layer and business logic |
| Interface-based design | Easily mockable; can be swapped for a different implementation |
| Reusability | A background job or a different controller could call the same service |

### Cons

| Drawback | Detail |
|---|---|
| More files, more ceremony | For small CRUD operations the service is thin and can feel like boilerplate |
| Another layer to navigate | A newcomer has to understand three layers (Controller → Service → DbContext) instead of two |

### Common beginner confusion

> **"Why define `IArticleService` if there is only ever one implementation?"**
>
> The interface exists primarily for two reasons:
> 1. **Testing.** You can create a `MockArticleService` that returns fake data, allowing you to test the controller in isolation.
> 2. **Substitutability.** Tomorrow you might add a `CachedArticleService` wrapper or a `ReadOnlyArticleService` for a public API endpoint. The interface is the contract; implementations can be swapped without touching the controller.
>
> This is the "Dependency Inversion Principle" from SOLID: depend on abstractions, not on concrete implementations.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Repository pattern on top of services** | Adds another abstraction layer between the service and the DbContext. Useful in very large codebases with complex data access rules, but often considered over-engineering for projects of this size |
| **CQRS (Command/Query Responsibility Segregation)** | Separates read operations (queries) from write operations (commands). Powerful for complex domains; overkill here |
| **Minimal API handlers** | ASP.NET Core Minimal APIs let you write request handlers as lambdas, skipping the controller class entirely. Good for microservices; trades structure for brevity |
| **Fat controllers** | Acceptable for very small projects with one or two endpoints. Becomes a maintenance burden as the project grows |

### What would justify revisiting this decision

- Adding a real-time background job (e.g., a scheduled tag cleanup) → the service can be reused directly.
- Adding a separate admin API → both the public and admin controllers can share the same service.
- The project grows and the service needs to be split into multiple, more focused services (e.g., `ITagService`, `ISearchService`).

---

## 6. DTO Separation

### What was chosen

**Data Transfer Objects (DTOs)** — separate C# classes used to represent request bodies and response payloads — are defined in `src/WikiProject.Api/DTOs/ArticleDtos.cs`. Mapping between entity classes and DTOs is done by extension methods in `src/WikiProject.Api/Mappings/ArticleMappings.cs`.

The key DTOs are:
- `CreateArticleRequest` — the JSON body sent when creating an article.
- `UpdateArticleRequest` — the JSON body sent when editing an article.
- `ArticleDto` — the full article response (used on the detail page).
- `ArticleSummaryDto` — a lighter response without the full `Content` field (used in list views).
- `ArticleListResponse` — wraps a page of `ArticleSummaryDto` objects with pagination metadata.

### What problem it solves

If you expose your EF Core entity classes directly as API responses, you:
1. Expose internal database fields (e.g., navigation properties, shadow properties) that clients should not see.
2. Couple your API contract to your database schema. Changing the database table would break the API.
3. Risk **over-posting attacks**: a client could send a JSON field like `"Id": 999` and overwrite the database record's primary key.
4. Cannot easily shape the response differently for different consumers (e.g., a list view needs less data than a detail view).

### How it works in this project

The mapping extension methods are the bridge:
```csharp
// ArticleMappings.cs
public static ArticleDto ToDto(this Article article)
{
    return new ArticleDto(
        Id: article.Id,
        Title: article.Title,
        Slug: article.Slug,
        // ... all fields ...
        Tags: article.ArticleTags.Select(at => at.Tag.Name).ToList()
    );
}

public static ArticleSummaryDto ToSummaryDto(this Article article)
{
    // Same, but without the Content field
}
```

In the service, after fetching an entity, the mapping is applied:
```csharp
var article = await _db.Articles.FindAsync(id);
return article.ToDto(); // never returns the raw Article entity to the caller
```

### Why it fits this project

- **API stability.** If the `Article` entity gains an `InternalNotes` field, it does not automatically appear in the API response.
- **Right-sizing responses.** The article list page only needs title, slug, summary, category, status, and tags — not the full content body. `ArticleSummaryDto` carries exactly that. This reduces the amount of data transferred over the network.
- **Input validation boundary.** `CreateArticleRequest` contains only the fields a client is allowed to set (title, summary, content, etc.) — not `Id`, `CreatedAt`, or `UpdatedAt`, which are controlled by the server.

### Pros

| Benefit | Detail |
|---|---|
| Prevents over-posting | The client cannot set server-controlled fields |
| API / DB decoupling | Schema changes do not automatically break the API contract |
| Right-sized payloads | List views get lighter DTOs; detail views get richer ones |
| Explicit contract | The DTO type is the documented shape of the API |

### Cons

| Drawback | Detail |
|---|---|
| More files | Every entity needs corresponding DTO types and mapping code |
| Mapping maintenance | When you add a field to the entity, you must remember to add it to the DTO and the mapping too |
| Boilerplate | For simple CRUD with no security concerns, DTOs can feel like unnecessary ceremony |

### Common beginner confusion

> **"Why not just use AutoMapper?"**
>
> AutoMapper is a popular library that can automatically map between entity and DTO classes using naming conventions. This project does not use it. The hand-written extension methods are more explicit: you can see exactly what maps to what. AutoMapper's convention-based magic can silently ignore new fields, or fail at runtime with cryptic errors. For a small project, explicit mapping is clearer and safer.

> **"What is over-posting and why should I care?"**
>
> Over-posting is when a malicious client sends fields in the JSON body that the server should not accept. For example, if the controller accepted the `Article` entity directly, a client could send `{ "Title": "Hacked", "Status": 1, "Id": 42 }` and update record 42's status to Published without going through the intended workflow. Using a DTO that only includes `Title`, `Summary`, `Content`, etc. makes this impossible — `Id` and `Status` are simply not in the DTO.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **AutoMapper** | When there are dozens of entities and the manual mapping code becomes a significant maintenance burden |
| **Mapster** | A faster, more modern alternative to AutoMapper with better support for records |
| **Direct entity exposure** | Only acceptable in truly internal APIs where there is no security boundary and schema changes are tightly controlled |
| **GraphQL** | Clients describe exactly what fields they want in their query. Eliminates the need to define multiple DTO shapes. Adds complexity |

### What would justify revisiting this decision

- The number of entities grows significantly (20+ entities) → AutoMapper or Mapster would reduce the mapping boilerplate.
- The API needs to evolve independently of the database schema → DTOs are even more important in that case, not less.

---

## 7. Folder Organisation

### What was chosen

The project uses a **type-based** folder organisation on both the backend and frontend. Each folder groups files by what kind of thing they are (controllers, services, entities, DTOs) rather than by what feature they belong to.

**Backend (`src/WikiProject.Api/`):**
```
Controllers/      ← HTTP endpoint handlers
Data/             ← DbContext and seed data
DTOs/             ← Request/response shapes
Entities/         ← EF Core model classes
Mappings/         ← Entity-to-DTO conversion
Migrations/       ← EF Core generated SQL scripts
Services/         ← Business logic
Validation/       ← FluentValidation validators
```

**Frontend (`frontend/src/`):**
```
components/       ← Reusable UI pieces (ArticleCard, SearchBar, etc.)
hooks/            ← Custom React hooks (useArticles, useArticle)
pages/            ← Full-page route components
services/         ← HTTP API client
types/            ← TypeScript interfaces
utils/            ← Pure utility functions
```

### What problem it solves

Without a consistent folder structure, files accumulate in a root directory, and new developers cannot predict where to find things. A newcomer to the project should be able to open the `Controllers/` folder and immediately know they are looking at the HTTP-facing layer.

### Why it fits this project

This is a small-to-medium project with a single bounded context (articles/wiki). All the logic is about articles and tags. Type-based organisation works well here because there is no need to navigate across multiple feature areas. Everything about articles is spread across the type folders but the surface area is small enough that this is easy to hold in your head.

### Pros

| Benefit | Detail |
|---|---|
| Predictability | You always know where a new controller, service, or DTO goes |
| Simple for small projects | When there is only one domain (articles), this is all the structure you need |
| Framework alignment | ASP.NET Core templates default to this layout; it is familiar to most .NET developers |

### Cons

| Drawback | Detail |
|---|---|
| Feature sprawl | When you add a second domain (e.g., `Comments`, `Users`), all the files related to that domain are spread across Controllers/, Services/, DTOs/, Entities/ etc. |
| Scattered related code | To understand a full feature you navigate across many folders |

### Common beginner confusion

> **"Should I add a `Repositories/` folder?"**
>
> This project uses the Service layer to talk to the DbContext directly. There is no separate Repository layer. Adding one is a valid pattern (it adds a clean abstraction over EF Core) but it also adds a layer of indirection. The STARTING_TASKS.md notes this is a deliberate choice for simplicity. If you add one, create `Repositories/IArticleRepository.cs` and `Repositories/ArticleRepository.cs` and inject the repository into the service rather than the DbContext directly.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Feature-based (vertical slice) organisation** | Group all files for a feature together: `Features/Articles/ArticleController.cs`, `Features/Articles/ArticleService.cs`, etc. Scales better for large projects with many independent features |
| **Domain-Driven Design folder layout** | Organise by bounded context and aggregate root. Appropriate for large, complex business domains |

### What would justify revisiting this decision

- The project gains second and third features (Comments, User Profiles, Permissions) → feature-based folders prevent the type-based folders from becoming unwieldy.
- The team grows significantly and different people own different features → feature-based organisation reduces merge conflicts.

---

## 8. Search and Filter Design

### What was chosen

**Backend:** A single API endpoint (`GET /api/articles`) accepts optional query parameters (`search`, `category`, `tag`, `status`, `page`, `pageSize`). The service layer builds a LINQ query and conditionally adds `WHERE` clauses. This is sometimes called a "dynamic query" or "filter builder" pattern.

**Frontend:** A debounced search input (300 ms delay before the query fires) and dropdown filters for category and status. Filter state is held in React component state inside `ArticlesPage`. Changes to any filter reset the page number to 1.

The query parameter record:
```csharp
public record ArticleQueryParams(
    string? Search = null,
    string? Category = null,
    string? Tag = null,
    ArticleStatus? Status = null,
    int Page = 1,
    int PageSize = 20
);
```

The service builds the query incrementally:
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
        a.Content.ToLower().Contains(search) ||
        a.Category.ToLower().Contains(search) ||
        a.ArticleTags.Any(at => at.Tag.Name.ToLower().Contains(search)));
}
// Additional conditions for category, tag, status filters
```

### What problem it solves

Users should be able to narrow down the article list without loading all articles and filtering in the browser. Pushing the filtering to the database ensures the page size stays constant regardless of the total number of articles.

### Why it fits this project

- **Simple and correct for the current data scale.** LIKE-based search (`Contains`) against a few hundred articles is fast enough without any additional infrastructure.
- **No external service required.** Full-text search engines like Elasticsearch or Meilisearch add operational complexity. For a small wiki, they are overkill.
- **Debouncing on the frontend** prevents hammering the API with a request on every keystroke. A 300 ms pause after the last keystroke fires a single request.

### How debouncing works

Without debouncing, typing "react hooks" would fire 10 API requests, one per character. With a 300 ms debounce, only one request fires — 300 ms after the user stops typing. This is implemented with `setTimeout`/`clearTimeout` inside a `useEffect` in the `ArticlesPage` component.

```typescript
useEffect(() => {
    const timer = setTimeout(() => {
        setDebouncedSearch(search);
    }, 300);
    return () => clearTimeout(timer); // cancel on re-render
}, [search]);
```

### Pros

| Benefit | Detail |
|---|---|
| No extra infrastructure | Works with the existing database |
| Scales to thousands of rows | Database indexes handle LIKE queries reasonably well |
| Simple to understand | No query language, no special configuration |
| Composable filters | Each filter clause is independent and can be added or removed |

### Cons

| Drawback | Detail |
|---|---|
| LIKE is not ranked | "react" matches an article that mentions "react" once in the content and an article where it is the title equally |
| LIKE can be slow at scale | `%search%` patterns (leading wildcard) cannot use a B-tree index and require a full table scan |
| No fuzzy matching | Typos (`"Recat"` instead of `"React"`) will return no results |
| Content search is broad | Searching the full `Content` field on every query is heavier than searching just titles and tags |

### Common beginner confusion

> **"Why does the search not return results for a typo?"**
>
> The current implementation uses SQL `LIKE '%term%'`, which requires an exact substring match. There is no fuzzy or phonetic matching. Adding fuzzy search would require either a full-text search engine or a library like SQLite FTS5.

> **"Why reset the page to 1 when a filter changes?"**
>
> If you are on page 3 and change the category filter, page 3 of the new filtered result set may not exist, causing an empty result or an error. Resetting to page 1 is a safe default. The `ArticlesPage` component handles this with a `useEffect` that watches filter state and resets `page` to 1.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **SQLite FTS5** | SQLite's built-in full-text search extension. Provides ranked results, stemming, and efficient prefix matching. Requires a virtual table and `MATCH` queries rather than `LIKE`. The project's README flags this as a future improvement |
| **Meilisearch** | An open-source, typo-tolerant search engine. Run as a separate service, indexed from your database. Excellent developer experience |
| **Elasticsearch / OpenSearch** | Industry-standard full-text search. More powerful than Meilisearch; also more complex to operate |
| **URL-synced filter state** | Store filter values in the URL query string (e.g., `?search=react&category=tutorials`). Users can share links to filtered views and the back button works correctly. The project's STARTING_TASKS.md lists this as a stretch goal |

### What would justify revisiting this decision

- The article count grows beyond ~10,000 → full table scan LIKE queries become slow; upgrade to SQLite FTS5 or an external search engine.
- Users need relevance ranking, typo tolerance, or synonym matching → replace LIKE with a dedicated search engine.
- Users need to share filtered views via URL → add URL-synced filter state.

---

## 9. Frontend / Backend Split

### What was chosen

The application is split into two completely separate processes: a **React SPA (Single Page Application)** served by Vite (development) or a static file server (production), and an **ASP.NET Core Web API** serving JSON over HTTP. They communicate exclusively via HTTP requests to the `/api/...` endpoints.

There is no server-side rendering. There is no templating engine (Razor, Handlebars, etc.) on the backend. The backend is a pure API; it knows nothing about HTML, CSS, or JavaScript.

### What problem it solves

The alternative — a "monolithic" web application where the backend renders HTML pages — works fine for simple sites but becomes restrictive when you want rich interactivity (live search, real-time updates, optimistic UI). Separating frontend and backend lets each evolve at its own pace.

### How the split works in development

The Vite dev server runs on port 5173. Any request whose path starts with `/api` is forwarded (proxied) to the ASP.NET Core server on port 5018. This is configured in `vite.config.ts`:

```typescript
server: {
    proxy: {
        '/api': {
            target: 'http://localhost:5018',
            changeOrigin: true
        }
    }
}
```

The browser thinks it is talking to one server at `localhost:5173`. The proxy silently routes API calls to the backend. This avoids CORS issues during development.

In production, you would either:
1. Serve the built React files from the ASP.NET Core app itself (add a static files middleware).
2. Deploy the React files to a CDN or static host, and configure CORS on the ASP.NET Core app to allow requests from that origin.

### Why it fits this project

- **Separation of concerns at the deployment level.** Frontend developers can work on the UI without touching any C# code, and backend developers can work on the API without touching any TypeScript code.
- **Independently deployable.** In production you could deploy a new version of the frontend without redeploying the backend, and vice versa.
- **Standard industry pattern.** Most modern web applications are built this way. Learning it here prepares developers for real-world projects.

### Pros

| Benefit | Detail |
|---|---|
| Technology freedom | The frontend can be React today and Vue tomorrow without changing the backend |
| Independent scaling | A high-traffic API can be scaled separately from the frontend static files |
| Clear contract | The HTTP API is the explicit interface between the two halves; changes on either side only need to respect that contract |
| Mobile-ready | The same JSON API can power a native mobile app or CLI tool with no changes |

### Cons

| Drawback | Detail |
|---|---|
| Two processes to run | Developers must start both the backend and frontend. This is a common source of "it doesn't work" for newcomers |
| Network latency | Every piece of data goes over HTTP (even on localhost). This is negligible in practice but is conceptually different from a server-rendered page |
| No SEO without SSR | Search engines see the empty React shell page, not the article content |
| CORS configuration | In production, CORS must be configured correctly or browsers will block API calls |

### Common beginner confusion

> **"I started the frontend but the API calls fail."**
>
> You must also start the backend. The frontend is just a browser application — it cannot fetch data if the API server is not running. The Vite proxy only works if the backend is listening on port 5018. Both processes must be running simultaneously.

> **"What is CORS and why does it keep blocking my requests?"**
>
> CORS (Cross-Origin Resource Sharing) is a browser security feature. When a page at `http://localhost:5173` makes an HTTP request to `http://localhost:5018`, the browser considers this a cross-origin request (different port = different origin). The browser sends a `preflight` OPTIONS request asking: "Does the server at port 5018 allow requests from port 5173?" The backend must respond with the right headers to say yes.
>
> In this project, CORS is configured in `Program.cs` using the `AllowedOrigins` list from `appsettings.json`. In development, `http://localhost:5173` is in that list.
>
> In development the Vite proxy makes CORS largely irrelevant because the browser only ever sees one origin. In production you must configure CORS explicitly.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Server-side rendering (Next.js, Remix)** | When SEO matters; the HTML is generated on the server and search engines see real content |
| **ASP.NET Core Razor Pages / MVC** | When the team is entirely .NET developers; server renders HTML, no separate JavaScript framework needed |
| **Blazor Server or WebAssembly** | Keeps the entire stack in C#; Blazor Server uses a WebSocket to stream UI updates, Blazor WASM runs C# in the browser |
| **BFF (Backend For Frontend)** | A dedicated thin API layer sits between the React app and a more complex backend microservice cluster. Overkill for this project |

### What would justify revisiting this decision

- The wiki becomes public-facing and SEO matters → migrate to Next.js or Remix for SSR.
- The team has no JavaScript experience → Razor Pages or Blazor eliminates the frontend/backend context switch.
- The project needs to power a mobile app as well → the current API-first approach is already exactly right for that; no change needed.

---

## 10. FluentValidation for Input Validation

### What was chosen

**FluentValidation** (`FluentValidation.AspNetCore` v11.3.1) is used to validate incoming `CreateArticleRequest` and `UpdateArticleRequest` bodies before they reach the service layer. Validators live in `src/WikiProject.Api/Validation/ArticleValidators.cs`.

### What problem it solves

User input cannot be trusted. A client might send an empty title, a content body with 10 million characters, or a status value outside the valid range. Without validation, these could cause database constraint violations, unexpected application behaviour, or denial of service.

### Why it fits this project

- **Expressive rule definition.** Rules read like English: `.NotEmpty().MaximumLength(200)`. They are easy to understand and extend.
- **Separation from the controller.** Validation logic is not scattered across controller methods; it lives in dedicated validator classes.
- **Structured error responses.** When validation fails, the validator returns a dictionary of field → error messages, which ASP.NET Core wraps in a standard `ValidationProblemDetails` response (HTTP 400).

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Data Annotations** | Attributes directly on DTO properties (`[Required]`, `[MaxLength(200)]`). Simpler for basic cases, but validation rules are tightly coupled to the DTO class |
| **Manual validation in the controller** | Fine for one or two rules; quickly becomes unmaintainable |
| **Minimal API validation middleware** | ASP.NET Core 10 has improved built-in validation support for Minimal APIs |

---

## 11. Slug-Based Routing

### What was chosen

Each article has a `Slug` — a URL-safe, human-readable identifier derived from the title (e.g., `"Getting Started with React"` → `"getting-started-with-react"`). The API exposes both `GET /api/articles/{id}` (by integer ID) and `GET /api/articles/slug/{slug}` (by slug). The frontend's article detail page navigates to `/articles/{id}` but the slug is used for display in some contexts.

Slug generation strips special characters, replaces spaces with hyphens, lowercases the result, and truncates at 100 characters. Uniqueness is enforced by a database unique index and a collision suffix (`-1`, `-2`, etc.).

### What problem it solves

Integer IDs (`/articles/42`) are opaque — they convey no information about the content and change if articles are deleted and IDs are recycled. Slugs are human-readable, SEO-friendly, and more resistant to change.

### Why it fits this project

- **Readability.** A URL like `/articles/introduction-to-asp-net-core` is self-describing.
- **SEO.** Even if the project is not currently public, building good URL habits now means less URL restructuring later.
- **Simple implementation.** Slug generation is a small utility function with clear rules.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **UUID/GUID identifiers** | Globally unique without a database lookup; safe to generate client-side; useful in distributed systems |
| **Integer ID only** | Simpler; no slug collision handling needed; acceptable for purely internal tools |
| **Custom short codes** | Short, shareable identifiers (like `abc12`). Used by URL shorteners and tools like GitHub gists |

---

## 12. Tag Normalisation Strategy

### What was chosen

Tags are stored as separate `Tag` entities in their own table, linked to articles via a `ArticleTag` join table (a many-to-many relationship). When creating or updating an article, tag names are **normalised** (lowercased and trimmed). If a tag with that normalised name already exists in the database, the existing tag is reused. If not, a new tag is created.

This means `"React"`, `"react"`, and `"  REACT  "` all resolve to the same tag.

### What problem it solves

Without normalisation, `"React"` and `"react"` would be treated as different tags, leading to a fragmented tag taxonomy and confusing filter results.

### Why it fits this project

- **Clean data.** Users do not need to remember the exact capitalisation of a tag.
- **Simple deduplication.** The unique index on `Tag.Name` enforces the constraint at the database level as a safety net.
- **Reusable tags across articles.** Rather than storing tags as a comma-separated string on each article, the separate table allows efficient "all articles with tag X" queries and enables the `/api/tags` endpoint to list all unique tags.

### Alternative approaches

| Alternative | When it may be better |
|---|---|
| **Comma-separated string in the Article table** | Simpler schema; acceptable if tags are never queried independently. Breaks normalisation rules and makes filtering by tag require a LIKE query |
| **JSON array column** | Supported by PostgreSQL; flexible schema; querying is less efficient |
| **Hierarchical tags (tag categories)** | Useful for complex taxonomies; adds significant schema complexity |

---

## 13. Summary: The Big Picture

Engineering is not about picking "the best tool." It is about making the best tradeoff given your current constraints: team size, timeline, expected scale, learning goals, and operational budget.

Here is a one-line summary of each decision made in WikiProject and the core reasoning:

| Decision | Chosen | Core Reasoning |
|---|---|---|
| Frontend framework | React + TypeScript + Vite | Rich interactivity, huge ecosystem, fast dev loop |
| Backend framework | ASP.NET Core 10 | Mature, typed, DI built in, great for REST APIs |
| ORM | EF Core 10 | No raw SQL for CRUD, migration tooling, LINQ integration |
| Database | SQLite | Zero setup, portable, sufficient for this scale |
| Service layer | `IArticleService` / `ArticleService` | Keeps controllers thin, enables testing and substitution |
| DTOs | Separate request/response shapes | Prevents over-posting, decouples API from schema |
| Folder layout | Type-based | Predictable for a single-domain project |
| Search/filter | Dynamic LINQ + debounced frontend | Sufficient for the current scale, no extra infrastructure |
| App split | SPA + REST API | Independent evolution, standard industry pattern |
| Validation | FluentValidation | Expressive rules, structured error responses |
| Slugs | Derived from title, unique index | Human-readable URLs, good habits for the future |
| Tag normalisation | Separate table, lowercase deduplication | Clean taxonomy, efficient filter queries |

Every one of these decisions could be made differently — and in a different project with different constraints, the better choice might be different. The key skill to develop is understanding _why_ a decision was made, not just _what_ was chosen. That understanding is what allows you to confidently change the decision when the constraints change.

---

### Where to Go Next

- **Related docs (produced by parallel agents):** other files in this `docs/` directory will cover setup and running the project, the API reference, the frontend component architecture, the database schema, and the testing strategy in more depth.
- **Official documentation:**
  - [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/)
  - [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/)
  - [React documentation](https://react.dev/)
  - [Vite documentation](https://vite.dev/)
  - [FluentValidation documentation](https://docs.fluentvalidation.net/)
  - [SQLite documentation](https://www.sqlite.org/docs.html)
- **Concepts to study next:**
  - SOLID principles (especially Dependency Inversion — the reason for `IArticleService`)
  - The N+1 query problem in ORMs
  - CORS in depth
  - Database indexing strategies
  - REST API design principles
