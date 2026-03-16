# Backend Deep Dive — WikiProject.Api

> **If you are new to backend development, read this section first.**
>
> A _backend_ is the server-side part of an application. It is responsible for receiving requests from clients (browsers, mobile apps, other servers), deciding what to do with them, reading from or writing to a database, and sending a response back. The client never directly touches the database; it only ever talks to the backend through a well-defined interface called an **API** (Application Programming Interface).
>
> In this project the backend is a **.NET 10 ASP.NET Core Web API** — a framework built by Microsoft for writing HTTP APIs in C#. You don't need deep .NET experience to follow this guide; each concept is explained plain-English first, technical detail second.

---

## Table of Contents

1. [What the backend is responsible for](#1-what-the-backend-is-responsible-for)
2. [Project layout at a glance](#2-project-layout-at-a-glance)
3. [Application startup — `Program.cs`](#3-application-startup--programcs)
4. [Dependency injection](#4-dependency-injection)
5. [Middleware pipeline](#5-middleware-pipeline)
6. [Routing](#6-routing)
7. [CORS](#7-cors)
8. [Swagger / OpenAPI](#8-swagger--openapi)
9. [Logging](#9-logging)
10. [Configuration and `appsettings`](#10-configuration-and-appsettings)
11. [Controllers](#11-controllers)
12. [Services](#12-services)
13. [DTOs — Data Transfer Objects](#13-dtos--data-transfer-objects)
14. [Entities](#14-entities)
15. [Validation](#15-validation)
16. [Mappings](#16-mappings)
17. [DbContext](#17-dbcontext)
18. [Entity Framework Core — what it does under the hood](#18-entity-framework-core--what-it-does-under-the-hood)
19. [SQLite integration](#19-sqlite-integration)
20. [Seeding](#20-seeding)
21. [Migrations](#21-migrations)
22. [A complete request flow, end to end](#22-a-complete-request-flow-end-to-end)
23. [Alternative approaches and trade-offs](#23-alternative-approaches-and-trade-offs)
24. [What to study next](#24-what-to-study-next)

---

## 1. What the backend is responsible for

The backend (`src/WikiProject.Api`) is the single source of truth for all business logic and data in WikiProject. Its responsibilities are:

| Responsibility | Why it lives in the backend |
|---|---|
| Storing and retrieving wiki articles | The database lives on the server, not in the browser |
| Enforcing business rules (e.g., slug must be unique) | The frontend cannot be trusted to enforce these — it can be bypassed |
| Validating incoming data | Malformed data would corrupt the database if written unchecked |
| Managing tags and categories | Normalised data (no duplicates) requires server-side coordination |
| Providing a stable HTTP API | The frontend and any future client speak to this API |
| Running database migrations at startup | Keeps the schema in sync without manual steps |

The backend does **not** render HTML, serve the React app, or contain any presentation logic. It produces and consumes JSON exclusively.

---

## 2. Project layout at a glance

```
src/WikiProject.Api/
├── Controllers/          # HTTP endpoint classes — receive requests, return responses
├── Data/
│   ├── WikiDbContext.cs  # EF Core database context — the gateway to SQLite
│   └── SeedData.cs       # Sample data inserted on first run
├── DTOs/
│   └── ArticleDtos.cs    # Request and response shapes the API exposes
├── Entities/             # C# classes that represent database tables
│   ├── Article.cs
│   ├── ArticleTag.cs     # Join table for the Article ↔ Tag relationship
│   ├── ArticleStatus.cs  # Enum: Draft, Published, Archived
│   └── Tag.cs
├── Mappings/
│   └── ArticleMappings.cs  # Extension methods: Entity → DTO
├── Migrations/           # Auto-generated EF Core SQL scripts
├── Services/
│   ├── IArticleService.cs  # Interface (contract) for the service
│   └── ArticleService.cs   # Implementation: all business logic
├── Validation/
│   └── ArticleValidators.cs  # FluentValidation rules
├── appsettings.json
├── appsettings.Development.json
└── Program.cs            # Entry point — wires everything together
```

Every folder has a single, clear job. This is not accidental — it is the **separation of concerns** principle. When you need to change how validation works, you go to `Validation/`. When you need to change what SQL is generated, you go to `Data/` or `Services/`. You don't have to read the whole codebase to understand what needs changing.

---

## 3. Application startup — `Program.cs`

`Program.cs` is the entry point of the application. In .NET 6 and later (including .NET 10 used here), the older `Startup.cs` + `Program.cs` split was merged into a single top-level file. Everything that used to live in two files now lives in one.

The file has two distinct phases:

### Phase 1 — registering services (the "builder" phase)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(...);
builder.Services.AddDbContext<WikiDbContext>(...);
builder.Services.AddScoped<IArticleService, ArticleService>();
// ... more registrations
```

`WebApplication.CreateBuilder(args)` creates a **builder object**. Think of it as a configuration accumulator — you describe what your application needs, and later those things get built and wired together automatically.

`builder.Services` is the **dependency injection (DI) container** (explained fully in §4). Every call to `Add...` registers something — a service, a database context, middleware — into this container. Nothing runs yet; you are only declaring intent.

### Phase 2 — building the app and defining the middleware pipeline

```csharp
var app = builder.Build();

// Apply migrations + seed
using (var scope = app.Services.CreateScope()) { ... }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}

app.UseCors();
app.MapControllers();

app.Run();
```

`builder.Build()` finalises the DI container and constructs the application. After this point you cannot add new services.

`app.Use...` calls add **middleware** — pieces of code that run for every request, in order (see §5).

`app.MapControllers()` tells the framework to discover all `[ApiController]` classes and register their routes.

`app.Run()` starts the HTTP server and blocks until the process is stopped.

### Why migrations run at startup

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
}
```

This block runs **before** any HTTP traffic is served. It applies any pending database migrations and seeds sample data if the database is empty.

**Why do this at startup instead of manually?** It removes a setup step for every developer and for every deployment. A new developer can clone the repo, run `dotnet run`, and get a working database automatically.

**The `using (var scope = ...)` pattern:** `WikiDbContext` is registered as a _scoped_ service (one instance per HTTP request). But at startup there is no request. This pattern creates a temporary, manually managed scope just to resolve the `WikiDbContext` once. The `using` statement disposes it when done.

> **Common beginner confusion:** Why not just call `new WikiDbContext(...)` directly? Because the context needs database connection options that were registered in the DI container. Bypassing the container would mean duplicating that configuration. Always get services from the container.

---

## 4. Dependency injection

### What it is, in plain English

"Dependency injection" means: instead of a class creating the things it needs (its _dependencies_) itself, those things are handed to it from the outside. This makes code easier to test and change.

Imagine `ArticleService` needs a database connection. Without DI:

```csharp
// Bad — hard-coded dependency
public class ArticleService
{
    private readonly WikiDbContext _db = new WikiDbContext(); // hard to test, hard to change
}
```

With DI:

```csharp
// Good — dependency is injected
public class ArticleService
{
    private readonly WikiDbContext _db;

    public ArticleService(WikiDbContext db)  // framework calls this constructor
    {
        _db = db;
    }
}
```

The framework reads the constructor signature, sees that `WikiDbContext` is needed, finds it in the container, and passes it in. You never call `new ArticleService(...)` yourself — the framework does it.

### How it works in this project

`Program.cs` registers these services:

```csharp
// MVC controllers (the framework resolves their constructors automatically)
builder.Services.AddControllers();

// The database context
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));

// The article service — registered as the interface it implements
builder.Services.AddScoped<IArticleService, ArticleService>();

// Validators
builder.Services.AddScoped<IValidator<CreateArticleRequest>, CreateArticleRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateArticleRequest>, UpdateArticleRequestValidator>();
```

### Service lifetimes

Every registered service has a **lifetime** that controls how many instances exist and how long they live:

| Lifetime | Meaning | Used for |
|---|---|---|
| `Singleton` | One instance for the whole app lifetime | Config, caches |
| `Scoped` | One instance per HTTP request | DbContext, services that touch the DB |
| `Transient` | A new instance every time it is requested | Stateless utilities |

**Why is `WikiDbContext` scoped?** EF Core's change tracker accumulates changes during a request and writes them all at once when `SaveChangesAsync()` is called. If the context were a singleton, one request's uncommitted state would bleed into another request — catastrophic. Scoped lifetime means each request gets its own clean context.

### Interfaces and the service layer

Notice that `ArticleService` is registered against `IArticleService`:

```csharp
builder.Services.AddScoped<IArticleService, ArticleService>();
```

The controller depends on the _interface_, not the concrete class:

```csharp
public ArticlesController(IArticleService articleService, ...)
```

This means you could swap `ArticleService` for a test double (a fake that returns canned data) without touching `ArticlesController`. The controller doesn't know or care which class it gets — just that it implements `IArticleService`.

> **Why this matters:** When you write unit tests for a controller, you provide a mock service. The controller's logic is tested in isolation, without hitting the real database.

---

## 5. Middleware pipeline

A **middleware** component is a piece of code that sits in the middle of the request/response flow. Every HTTP request passes through all the middleware components in order, and the response passes back through them in reverse order.

Think of it like a series of checkpoints at an airport: security screening, passport control, boarding gate. Every passenger passes through all of them in order.

In `Program.cs`:

```csharp
app.UseSwagger();       // (development only) serves the OpenAPI JSON
app.UseSwaggerUI(...);  // (development only) serves the interactive UI
app.UseCors();          // adds CORS headers to responses
app.MapControllers();   // routes requests to the right controller action
```

The **order matters**. CORS headers must be added before the response is finalised, so `UseCors()` comes before `MapControllers()`. If you added authentication in the future, `UseAuthentication()` must come before `UseAuthorization()`, and both before `MapControllers()`.

### Request journey through middleware

```
HTTP Request
     │
     ▼
[Swagger middleware]  ← only matches /swagger/* paths; passes others through
     │
     ▼
[CORS middleware]     ← adds Access-Control-Allow-Origin header
     │
     ▼
[Controller routing]  ← matches route, invokes controller action
     │
     ▼
Controller action runs
     │
     ▼
Response travels back through middleware in reverse
     │
     ▼
HTTP Response sent to client
```

> **Common beginner confusion:** People sometimes register middleware _after_ `app.MapControllers()` and wonder why it never runs for regular requests. `MapControllers()` is a terminal middleware — if a route matches, the pipeline ends there. Put cross-cutting concerns like CORS and logging **before** it.

---

## 6. Routing

**Routing** is how the framework matches an incoming HTTP request URL to the right piece of code to handle it.

In this project, routing is **attribute-based** — each controller class and each action method carries attributes that declare its route.

### Controller-level route

```csharp
[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
```

Every action in this controller is relative to `api/articles`.

### Action-level routes

```csharp
[HttpGet]                      // → GET  /api/articles
[HttpGet("{id:int}")]          // → GET  /api/articles/42
[HttpGet("slug/{slug}")]       // → GET  /api/articles/slug/welcome-to-wikiproject
[HttpPost]                     // → POST /api/articles
[HttpPut("{id:int}")]          // → PUT  /api/articles/42
[HttpDelete("{id:int}")]       // → DELETE /api/articles/42
```

The `{id:int}` syntax is a **route constraint** — it only matches if the segment is a valid integer. A request to `/api/articles/abc` would not match `{id:int}` and would get a 404.

**Route parameter binding:** The `{id}` in the route template is automatically bound to the `int id` parameter in the action method:

```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<ArticleDto>> GetById(int id)  // id comes from the URL
```

**Query string binding:** Parameters not in the route template are read from the query string by default:

```csharp
public async Task<ActionResult<ArticleListResponse>> GetArticles(
    [FromQuery] string? search,   // → /api/articles?search=efcore
    [FromQuery] int page = 1)
```

**Body binding:**

```csharp
public async Task<ActionResult<ArticleDto>> Create(
    [FromBody] CreateArticleRequest request)  // JSON body
```

> **Common beginner confusion:** Why is there both `/api/articles/{id:int}` (get by ID) and `/api/articles/slug/{slug}` (get by slug)? Because the slug is a string, it would ambiguously match `{id:int}` if it were just `{slug}`. The explicit `slug/` prefix disambiguates the routes.

### The `MetadataController`

```csharp
[Route("api")]
public class MetadataController : ControllerBase
{
    [HttpGet("categories")]  // → GET /api/categories
    [HttpGet("tags")]        // → GET /api/tags
}
```

This controller uses a different base route (`api` rather than `api/articles`) because categories and tags are not article sub-resources — they are independent metadata lists.

---

## 7. CORS

**CORS** (Cross-Origin Resource Sharing) is a browser security mechanism. By default, a browser running JavaScript at `http://localhost:5173` is not allowed to make HTTP requests to `http://localhost:5018` (a different **origin** — protocol + hostname + port). This is called the **same-origin policy**.

To allow this, the backend must explicitly say "I accept requests from `http://localhost:5173`" by adding specific HTTP headers to its responses. ASP.NET Core's CORS middleware does this automatically when configured.

### Configuration

In `appsettings.json`:

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173" ]
}
```

In `Program.cs`:

```csharp
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Later, in the pipeline:
app.UseCors();
```

The fallback (`?? new[] { "http://localhost:5173" }`) means the app still works even if the configuration section is missing.

**`AllowAnyHeader()` and `AllowAnyMethod()`:** These allow the frontend to send any HTTP method (GET, POST, PUT, DELETE) and any headers (including `Content-Type: application/json`). In production you would tighten these, but for a development-stage app they are fine.

> **Why this matters for development:** Without this configuration, every API call from the React dev server would fail silently with a CORS error in the browser's developer console. This is one of the most common "the API is running but nothing works" issues new developers hit.

> **Note on the Vite proxy:** The frontend's `vite.config.ts` includes a proxy that rewrites `/api` requests to `http://localhost:5018`. When using the Vite proxy, the browser sees requests going to `localhost:5173`, so CORS doesn't apply. CORS _does_ matter when the frontend is deployed to a different host than the API, and it matters if you call the API directly (e.g., from Postman or curl from a browser extension).

---

## 8. Swagger / OpenAPI

**OpenAPI** is a standard JSON format for describing an HTTP API — its endpoints, parameters, request bodies, and response shapes. **Swagger** is the original tool that created this standard; the terms are used interchangeably.

**Swashbuckle** is the .NET library that generates an OpenAPI document automatically from your controller attributes and C# types, and serves a browser-based interactive UI.

### Configuration

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WikiProject API",
        Version = "v1",
        Description = "Internal knowledge base REST API"
    });
});
```

`AddEndpointsApiExplorer()` tells the framework to make its route information available to Swashbuckle.

`AddSwaggerGen(...)` registers the OpenAPI document generator with metadata (title, version, description) that appears in the UI.

### In the pipeline (development only)

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();          // serves /swagger/v1/swagger.json
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WikiProject API v1");
        c.RoutePrefix = "swagger";  // UI at /swagger
    });
}
```

**Why only in development?** Exposing an interactive API explorer in production is a security risk — it helps attackers understand your API surface. The conditional ensures it only runs when `ASPNETCORE_ENVIRONMENT=Development`.

### What Swagger gives you

Navigate to `http://localhost:5018/swagger` while the backend is running and you get:

- A list of all API endpoints grouped by controller
- The expected request body shape for POST/PUT endpoints
- The expected response shape for each endpoint
- A "Try it out" button to make live requests directly from the browser

This is invaluable for debugging and for communicating the API contract to the frontend team.

### XML doc comments

The XML `/// <summary>` comments on controller actions are picked up by Swashbuckle:

```csharp
/// <summary>Gets a paginated, searchable, filterable list of articles.</summary>
[HttpGet]
public async Task<ActionResult<ArticleListResponse>> GetArticles(...)
```

These appear as descriptions in the Swagger UI. Writing them is a low-effort way to document your API.

---

## 9. Logging

**Logging** is the practice of writing informational messages to an output stream (the console, a file, a log aggregation service) during program execution. Good logging is essential for diagnosing bugs in production where you cannot attach a debugger.

### Console logging

```csharp
builder.Logging.AddConsole();
```

This is the only logging provider registered. In development, messages appear in the terminal where you ran `dotnet run`. In a containerised deployment, the container platform (Docker, Kubernetes) captures stdout and forwards it to whatever log aggregator is configured.

### Log levels

In `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
  }
}
```

And in `appsettings.Development.json` (which _overrides_ the base file in development):

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

The difference: in Development, `Microsoft.EntityFrameworkCore.Database.Command` is set to `Information`, which means EF Core logs every SQL query it executes. This is helpful when debugging. In production (or the base file), it is `Warning`, which suppresses the SQL noise.

Log levels from most to least verbose: `Trace → Debug → Information → Warning → Error → Critical`. Setting a level means "log this level and everything above it."

### Structured logging

`ArticleService` uses the standard `ILogger<T>` interface injected by the DI container:

```csharp
public ArticleService(WikiDbContext db, ILogger<ArticleService> logger)
{
    _db = db;
    _logger = logger;
}
```

And logs structured messages at key points:

```csharp
_logger.LogInformation("Created article {Id} '{Title}'", article.Id, article.Title);
_logger.LogInformation("Updated article {Id} '{Title}'", article.Id, article.Title);
_logger.LogInformation("Deleted article {Id}", id);
```

The `{Id}` and `{Title}` placeholders are **structured log properties**, not just string format parameters. Log aggregators like Seq, Elastic, or Application Insights can index these as searchable fields rather than flat strings. Even if you are only logging to the console today, using structured logging from day one means you don't have to rewrite all your log statements when you add a real log aggregator later.

---

## 10. Configuration and `appsettings`

ASP.NET Core uses a layered configuration system. Values from multiple sources are merged, with later sources overriding earlier ones.

The default order is:
1. `appsettings.json` — base values, committed to source control
2. `appsettings.{Environment}.json` — environment-specific overrides (e.g., `appsettings.Development.json`)
3. Environment variables — override any file-based setting
4. Command-line arguments — highest priority

### `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Data Source=wiki.db"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

### `appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "Default": "Data Source=wiki.db"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

This file is loaded automatically when `ASPNETCORE_ENVIRONMENT=Development`, which is set in `launchSettings.json` and therefore active when you run `dotnet run` from your machine.

### Reading configuration in code

```csharp
// Reading a connection string (has a shortcut method)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=wiki.db";

// Reading an arbitrary section as a typed array
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
```

The `?? "fallback"` pattern ensures the app still runs even if a key is missing from the configuration files. This is defensive programming — useful when the app is run without any config files (e.g., in a minimal Docker image).

> **Never put secrets (passwords, API keys) in `appsettings.json`** — that file is committed to source control. Use environment variables or a secrets manager (dotnet user-secrets for local development, Azure Key Vault / AWS Secrets Manager for production).

---

## 11. Controllers

A **controller** is a class that receives HTTP requests, delegates work to services, and returns HTTP responses. Controllers should be thin — they handle the HTTP-specific concerns and nothing else.

### `[ApiController]` attribute

This attribute enables several automatic behaviours:
- **Automatic model binding:** query strings, route values, and request bodies are automatically bound to action parameters
- **Automatic 400 responses:** if model binding fails, a 400 Bad Request is returned without you writing any code
- **Automatic `[FromBody]` inference:** action parameters with complex types are assumed to come from the request body

### `ArticlesController`

```csharp
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
    ...
}
```

Notice:
- `ControllerBase` (not `Controller`) — the non-view variant; for APIs you never need `Controller`
- The constructor receives `IArticleService` (the _interface_, not the class) — this is DI in action
- `[Produces("application/json")]` — advertises the response content type in Swagger

### Return types

Each action returns `ActionResult<T>` or `IActionResult`:

```csharp
public async Task<ActionResult<ArticleDto>> GetById(int id)
{
    var article = await _articleService.GetByIdAsync(id);
    return article is null ? NotFound() : Ok(article);
}
```

`ActionResult<T>` is a union type — it can be either an HTTP result (like `NotFound()`) or the typed value (`ArticleDto`). This gives Swagger enough type information to document the success response shape, while still letting you return error responses.

Helper methods like `Ok()`, `NotFound()`, `NoContent()`, and `CreatedAtAction()` produce HTTP responses with the appropriate status codes:

| Method | Status code | Used when |
|---|---|---|
| `Ok(value)` | 200 | Successful read/update |
| `CreatedAtAction(...)` | 201 | Successful creation |
| `NoContent()` | 204 | Successful deletion (no body) |
| `NotFound()` | 404 | Resource not found |
| `ValidationProblem(...)` | 400 | Validation failed |

### `CreatedAtAction` — the correct response for POST

```csharp
return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
```

This returns a 201 response with a `Location` header pointing to the new resource's URL (e.g., `Location: /api/articles/7`). This is the HTTP standard for POST responses — the client now knows where to find the created resource.

### Why controllers should not do everything directly

It might seem simpler to put all the database logic directly in the controller:

```csharp
// Tempting but wrong
[HttpPost]
public async Task<ActionResult<ArticleDto>> Create([FromBody] CreateArticleRequest request)
{
    var article = new Article { Title = request.Title, ... };
    _db.Articles.Add(article);
    await _db.SaveChangesAsync();
    return Ok(article);
}
```

The problems with this approach:

1. **Untestable:** To test this method, you need a real database
2. **Not reusable:** If another endpoint needs the same logic (e.g., a bulk import endpoint), you have to copy-paste it
3. **Mixed concerns:** The controller now knows about the database schema, slug generation, tag resolution, etc.
4. **Grows without bound:** Over time, controllers with database logic become hundreds of lines long, mixing HTTP concerns with business logic

The service pattern solves all of this: the controller delegates to `_articleService.CreateAsync(request)` and just handles the HTTP response. Business logic lives in `ArticleService`, which can be tested independently.

### `[FromServices]` for per-action DI

```csharp
public async Task<ActionResult<ArticleDto>> Create(
    [FromBody] CreateArticleRequest request,
    [FromServices] IValidator<CreateArticleRequest> validator)
```

`[FromServices]` resolves a service from the DI container just for this action method, without adding it to the controller's constructor. This is useful for dependencies that only a subset of actions need — keeping the constructor clean.

---

## 12. Services

The **service layer** contains all the business logic. A service is a class that the controller calls to get things done.

### Why services exist

See the "Why controllers should not do everything directly" section in §11. The service layer enforces:
- **Single Responsibility:** one class does one thing
- **Testability:** services can be tested with an in-memory database without an HTTP layer
- **Reusability:** multiple controllers or background jobs can use the same service

### `IArticleService` — the interface

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

The interface defines the contract. The controller knows about the interface; the DI container wires it to `ArticleService` at runtime.

The `?` on some return types (e.g., `ArticleDto?`) means "this might be null" — for cases where the item is not found.

`Task<T>` means the method is asynchronous — it does I/O without blocking the thread (see below).

### Async all the way

Every method that touches the database is `async` and returns `Task<T>`. The `await` keywords tell the runtime "pause here until the I/O completes, but let other requests use this thread in the meantime."

This is crucial for web applications. A thread blocked on a database query cannot serve other requests. With async/await, a .NET thread can handle hundreds of concurrent requests because it only holds the thread while CPU work is happening, not while waiting for I/O.

### Key operations in `ArticleService`

#### Querying with filters

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
        a.Content.ToLower().Contains(search));
}
// ... more filters

var items = await q
    .OrderByDescending(a => a.UpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

This builds an EF Core query lazily. None of the `Where`, `Skip`, or `Take` calls hit the database — they build an expression tree that EF Core translates to SQL. Only `ToListAsync()` executes the query.

`AsNoTracking()` tells EF Core not to track the returned objects for change detection — because this is a read-only query. This improves performance (less memory, less work for the change tracker).

#### Slug generation

```csharp
private static string GenerateSlug(string title)
{
    var slug = title.ToLower().Trim();
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");   // remove special chars
    slug = Regex.Replace(slug, @"\s+", "-");              // spaces → hyphens
    slug = Regex.Replace(slug, @"-+", "-");               // collapse multiple hyphens
    slug = slug.Trim('-');                                 // remove leading/trailing hyphens
    return slug.Length > 100 ? slug[..100] : slug;        // enforce max length
}
```

A **slug** is a URL-safe, human-readable identifier derived from a title. "Hello World" becomes `hello-world`. Slugs appear in URLs like `/api/articles/slug/hello-world` and are more readable than IDs.

#### Ensuring slug uniqueness

```csharp
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
```

If `hello-world` is taken, this produces `hello-world-1`, then `hello-world-2`, etc. The `excludeId` parameter is used during updates so an article doesn't conflict with itself.

#### Tag resolution — upsert pattern

```csharp
private async Task<List<Tag>> ResolveTagsAsync(IReadOnlyList<string>? tagNames)
{
    var existing = await _db.Tags
        .Where(t => normalized.Contains(t.Name))
        .ToListAsync();

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
```

This is an **upsert** (update-or-insert) pattern for tags. Tags are shared across articles (the `Tags` table is normalised). When an article is created with tags `["api", "guide"]`:
1. It queries for tags that already exist
2. Creates any that are missing
3. Returns all of them (existing + newly created) for linking to the article

Without this pattern, creating two articles both tagged `"api"` would create two duplicate `Tag` rows and violate the unique index.

---

## 13. DTOs — Data Transfer Objects

A **DTO** (Data Transfer Object) is a simple class whose only job is to carry data between layers — specifically between the HTTP layer and the service layer, or between the service layer and the client.

### DTOs in this project

```csharp
// Response DTO — what the API returns for a full article
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

// Summary DTO — used in list responses (no full Content field)
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

// Request DTO — what the client sends to create an article
public record CreateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status
);

// Pagination wrapper
public record ArticleListResponse(
    IReadOnlyList<ArticleSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
```

These are declared as **C# records** rather than classes. A record is an immutable reference type with value semantics and auto-generated equality. Since DTOs are just data bags that are created, read, and discarded, immutability is ideal.

### Why DTOs and entities are different

This is one of the most important concepts for new backend developers to internalise. The short answer: **entities model the database; DTOs model the API contract**.

Consider the `Article` entity:

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    // ... other properties
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;  // ← int enum
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();  // ← join table
}
```

And the `ArticleDto`:

```csharp
public record ArticleDto(
    // ...
    IReadOnlyList<string> Tags,   // ← flat list of tag names
    string Status,                 // ← string, not enum
    // ...
);
```

Key differences:

| Concern | Entity | DTO |
|---|---|---|
| Tags | `ICollection<ArticleTag>` (join objects with IDs) | `IReadOnlyList<string>` (just names) |
| Status | `ArticleStatus` enum (stored as int in DB) | `string` (human-readable for JSON) |
| Navigation props | Present (`ArticleTags`, etc.) | Absent |
| Mutable | Yes | No (record) |

**Why not just expose entities directly?**

1. **Over-exposure:** Entities often contain fields that clients shouldn't see (internal IDs of join tables, audit fields, foreign keys)
2. **Coupling:** If you change the database schema, the API contract changes too — breaking all clients
3. **Serialisation problems:** EF Core navigation properties can cause circular reference exceptions in JSON serialisers
4. **Shape mismatch:** The client wants `tags: ["api", "guide"]` not `articleTags: [{ articleId: 1, tagId: 3, tag: { id: 3, name: "api", articleTags: [...] } }]`

The DTO shapes the data for the consumer. The entity shapes the data for the database. They are intentionally different.

---

## 14. Entities

**Entities** are C# classes that EF Core maps to database tables. Each property typically maps to a database column.

### `Article`

```csharp
public class Article
{
    public int Id { get; set; }                                        // PK, auto-increment
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;                  // unique index
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;               // Markdown text
    public string Category { get; set; } = string.Empty;
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;  // stored as INT
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

`ICollection<ArticleTag>` is a **navigation property** — it allows EF Core to load the related `ArticleTag` rows when you include them in a query (`Include(a => a.ArticleTags)`).

### `Tag`

```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

Tags are stored in their own table so that the same tag can be associated with many articles without duplication. This is the **normalisation** principle.

### `ArticleTag` — the join entity

```csharp
public class ArticleTag
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
```

This represents the many-to-many relationship between articles and tags. One article can have many tags; one tag can be applied to many articles. The join table stores pairs `(ArticleId, TagId)`.

EF Core 5+ supports many-to-many with skip navigation (no explicit join entity), but using an explicit join entity like `ArticleTag` gives you more control — if you wanted to add metadata to the relationship (e.g., `AddedAt`, `AddedByUserId`), you would add it here.

### `ArticleStatus` — enum

```csharp
public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
```

Stored as an `INTEGER` in SQLite. The explicit values `= 0, 1, 2` are important — without them, C# assigns values automatically, and if someone reorders the enum members later, the stored integers change meaning.

> **Common beginner confusion:** The `= null!` syntax on navigation properties (e.g., `public Article Article { get; set; } = null!`) tells the C# nullable reference types compiler "I know this looks like it could be null, but I guarantee it won't be null at runtime — trust me." This is because EF Core sets these properties via reflection after construction, bypassing the constructor. The `!` suppresses the nullable warning.

---

## 15. Validation

**Validation** is the process of checking that incoming data is well-formed before acting on it. This project uses **FluentValidation**, a popular .NET library that lets you express validation rules as expressive, fluent code.

### Validators

```csharp
public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must be 200 characters or fewer.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));  // only validate if slug was provided
        
        // ... more rules
    }
}
```

Rules are composed using method chaining. Each `RuleFor(x => x.Field)` declares a rule chain for a property. Multiple `.NotEmpty()`, `.MaximumLength()`, `.Matches()` etc. can be chained.

`.When(condition)` makes a rule conditional — the slug format rule only applies when the caller provided a slug (it is optional; the service generates one from the title if omitted).

### How validation is triggered in the controller

```csharp
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
```

The validator is injected via `[FromServices]`. The controller calls `ValidateAsync`, checks if it passed, and returns a 400 response with the error details if not. Only if validation passes does it call the service.

**What does the 400 response look like?**

`ValidationProblemDetails` produces a standard [RFC 7807](https://tools.ietf.org/html/rfc7807) "Problem Details" JSON response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required."],
    "Summary": ["Summary is required."]
  }
}
```

This is a widely adopted standard for error responses — many frontend libraries and API clients know how to parse it.

> **Why not use Data Annotations (`[Required]`, `[MaxLength]`) on the request DTO?** You could. Data Annotations work fine for simple cases. FluentValidation gives you more expressiveness (conditional rules, cross-property rules, async rules), and it keeps validation logic in a dedicated validator class rather than scattered across the DTO definition. For a project this size, either would work.

---

## 16. Mappings

**Mappings** convert entities to DTOs. In this project they are simple **extension methods** on the entity types, collected in `ArticleMappings.cs`.

```csharp
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
```

These are called inside `ArticleService`:

```csharp
return article?.ToDto();
return items.Select(a => a.ToSummaryDto()).ToList();
```

### Why manual mapping rather than AutoMapper?

**AutoMapper** is a popular library that automatically maps between types based on property name conventions. The alternative used here — manual extension methods — has these characteristics:

| | Manual mapping | AutoMapper |
|---|---|---|
| Verbosity | More code | Less code |
| Explicitness | You can see exactly what maps to what | Implicit; must know conventions |
| Debugging | Easy to step through | Harder to trace |
| Compile-time safety | Yes, always | Configuration errors surface at runtime |
| Good for beginners | Yes | Adds learning curve |

For a codebase this size, manual mapping is the right call. AutoMapper shines when you have many types with similar shapes to map across. Here there are only two mappings, and they do non-trivial transformations (the tag list flattening, the enum-to-string conversion) that AutoMapper would need custom configuration to handle anyway.

---

## 17. DbContext

`WikiDbContext` is the **gateway to the database**. EF Core uses it as the central object through which all database operations flow.

```csharp
public class WikiDbContext : DbContext
{
    public WikiDbContext(DbContextOptions<WikiDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite PK for the join table
        modelBuilder.Entity<ArticleTag>()
            .HasKey(at => new { at.ArticleId, at.TagId });

        // Relationships
        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Article)
            .WithMany(a => a.ArticleTags)
            .HasForeignKey(at => at.ArticleId);

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Tag)
            .WithMany(t => t.ArticleTags)
            .HasForeignKey(at => at.TagId);

        // Unique indexes
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.Slug).IsUnique();

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name).IsUnique();
    }
}
```

### `DbSet<T>` properties

Each `DbSet<T>` represents a database table. You use them to query and modify data:

```csharp
_db.Articles.Add(article);      // insert
_db.Articles.Remove(article);   // delete
_db.Articles.Where(...).ToListAsync();  // query
await _db.SaveChangesAsync();   // commit all pending changes
```

### `OnModelCreating` — the Fluent API

EF Core can infer a lot from convention (a property named `Id` becomes the primary key, a property named `ArticleId` becomes a foreign key). But some things require explicit configuration:

- **Composite primary key:** `ArticleTag` has no single-column PK; the PK is `(ArticleId, TagId)`. This cannot be expressed with an attribute and requires the Fluent API
- **Unique indexes:** Slug uniqueness and tag name uniqueness are database-level constraints defined here
- **Explicit relationships:** Though EF Core could infer the `ArticleTag` relationships from the navigation properties and naming conventions, making them explicit improves clarity

### Change tracking

By default, when you load an entity from the database, EF Core's **change tracker** takes a snapshot of its original state. When you call `SaveChangesAsync()`, EF Core compares the current state to the snapshot and generates `UPDATE` statements only for changed columns.

```csharp
// Load article (tracked)
var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);

// Modify it
article.Title = "New Title";
article.UpdatedAt = DateTime.UtcNow;

// EF Core detects Title and UpdatedAt changed, generates:
// UPDATE Articles SET Title = 'New Title', UpdatedAt = '...' WHERE Id = 42
await _db.SaveChangesAsync();
```

For read-only queries, use `.AsNoTracking()` to skip tracking — EF Core won't take snapshots, saving memory and time.

---

## 18. Entity Framework Core — what it does under the hood

EF Core is an **ORM** (Object-Relational Mapper) — a library that bridges the gap between object-oriented C# code and relational SQL databases.

### The conceptual model

You write C# LINQ (Language-Integrated Query):

```csharp
var articles = await _db.Articles
    .Where(a => a.Status == ArticleStatus.Published)
    .OrderByDescending(a => a.UpdatedAt)
    .Take(20)
    .ToListAsync();
```

EF Core translates this to SQL:

```sql
SELECT a."Id", a."Title", a."Slug", ...
FROM "Articles" AS a
WHERE a."Status" = 1
ORDER BY a."UpdatedAt" DESC
LIMIT 20
```

It then reads the result rows and materialises them back into `Article` C# objects. This translation is called **query translation**.

### The unit of work pattern

`WikiDbContext` implements the **unit of work** pattern. All changes you make to tracked entities (via Add, Remove, or property mutations) are held in memory. `SaveChangesAsync()` wraps everything in a single database transaction:

```sql
BEGIN TRANSACTION;
  INSERT INTO Articles (...) VALUES (...);
  INSERT INTO ArticleTags (ArticleId, TagId) VALUES (7, 3), (7, 5);
COMMIT;
```

Either everything succeeds or nothing does. This prevents partial writes that would leave the database in an inconsistent state.

### Deferred query execution

EF Core queries use **deferred execution** — the SQL is not sent to the database until you iterate the result. Calls like `Where()`, `OrderBy()`, `Skip()`, `Take()`, and `Include()` all build up a query expression. Only materialising calls like `ToListAsync()`, `FirstOrDefaultAsync()`, `CountAsync()`, and `AnyAsync()` actually execute the SQL.

This means you can compose queries conditionally:

```csharp
var q = _db.Articles.AsQueryable();

if (hasSearch)
    q = q.Where(...);
if (hasCategory)
    q = q.Where(...);

var results = await q.ToListAsync();  // only ONE query is sent, with all conditions
```

This is far better than making multiple round-trips to the database.

### `Include` and `ThenInclude` — eager loading

By default, EF Core **does not** load navigation properties. If you load an `Article`, its `ArticleTags` collection is empty unless you explicitly request it:

```csharp
var article = await _db.Articles
    .Include(a => a.ArticleTags)      // load ArticleTags with each Article
        .ThenInclude(at => at.Tag)    // for each ArticleTag, also load its Tag
    .FirstOrDefaultAsync(a => a.Id == id);
```

This generates a SQL JOIN:

```sql
SELECT a.*, at.*, t.*
FROM Articles a
LEFT JOIN ArticleTags at ON at.ArticleId = a.Id
LEFT JOIN Tags t ON t.Id = at.TagId
WHERE a.Id = @id
```

Without `Include`, accessing `article.ArticleTags` would return an empty list (not a lazy-loaded list, since lazy loading is not configured).

> **Common beginner confusion — N+1 queries:** If you loaded articles without `Include` and then tried to access tags for each article in a loop, EF Core would execute one query for the articles and then one additional query per article to load its tags. This is the infamous **N+1 problem**. The `Include` approach loads everything in one query. Always use `Include` for related data you know you will need.

---

## 19. SQLite integration

**SQLite** is a serverless, file-based relational database. The entire database is stored in a single file (`wiki.db`).

### Why SQLite for this project?

| Property | SQLite | PostgreSQL / SQL Server |
|---|---|---|
| Setup | Zero setup — it's just a file | Requires a server process |
| Dependencies | None | Must install and run DB server |
| Portability | File can be copied anywhere | Connection strings, credentials needed |
| Concurrency | Limited (file-level locking) | Full concurrent access |
| Data size | Excellent for <1 GB | Scales to any size |
| Production suitability | Fine for small, single-instance apps | Required for multi-server deployments |

For a small, single-developer or small-team knowledge base, SQLite is excellent. It would become a bottleneck if WikiProject ever needed multiple API server instances (horizontal scaling), but the architecture is designed to make switching databases easy.

### Switching to a different database

The entire database dependency is encapsulated in one call in `Program.cs`:

```csharp
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

To switch to PostgreSQL:
1. Install `Npgsql.EntityFrameworkCore.PostgreSQL`
2. Replace `.UseSqlite(...)` with `.UseNpgsql(...)`
3. Update the connection string
4. Run `dotnet ef migrations add PostgresMigration` to generate a new migration
5. Run `dotnet ef database update`

The entity classes, `WikiDbContext`, services, and controllers require no changes. This is the benefit of abstracting the database behind EF Core.

### The database file location

```json
"ConnectionStrings": {
  "Default": "Data Source=wiki.db"
}
```

`wiki.db` is a relative path, resolved from the application's working directory. When running with `dotnet run` from `src/WikiProject.Api/`, the file is created in that directory.

The `wiki.db` file is listed in `.gitignore` and should never be committed to source control — it is a runtime artefact, not source code.

---

## 20. Seeding

**Seeding** means populating the database with initial data. Without seed data, developers would need to manually create articles to have anything to look at in the UI.

### `SeedData.SeedAsync`

```csharp
public static async Task SeedAsync(WikiDbContext db)
{
    if (await db.Articles.AnyAsync())
        return;  // already seeded; skip

    var tagNames = new[] { "getting-started", "guide", "api", "database", "architecture", "tips", "reference" };
    var tags = tagNames.Select(n => new Tag { Name = n }).ToList();
    db.Tags.AddRange(tags);
    await db.SaveChangesAsync();

    var tagMap = tags.ToDictionary(t => t.Name);

    var articles = new List<Article> { /* ... 6 sample articles ... */ };

    db.Articles.AddRange(articles);
    await db.SaveChangesAsync();
}
```

Key points:

- **Idempotent:** The `if (await db.Articles.AnyAsync()) return;` guard means seeding only runs when the database is empty. Restart the app 100 times — it seeds exactly once
- **Called from `Program.cs`** at startup, after migrations, so the schema always exists before the seed data is inserted
- **Sample articles** cover multiple categories and statuses (Published and Draft), giving the UI meaningful data to display and test with

### Re-seeding

To start fresh:

```bash
rm src/WikiProject.Api/wiki.db
dotnet run
```

The app will recreate the database, apply migrations, and re-seed.

---

## 21. Migrations

A **migration** is a versioned record of a change to the database schema. EF Core generates migrations as C# files that contain `Up()` and `Down()` methods — SQL (expressed as method calls) to apply or reverse the change.

### The `InitialCreate` migration

Located at `Migrations/20260315235834_InitialCreate.cs`:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable("Articles", columns => new {
        Id = table.Column<int>(type: "INTEGER", nullable: false)
            .Annotation("Sqlite:Autoincrement", true),
        Title = table.Column<string>(type: "TEXT", nullable: false),
        // ... etc
    }, constraints: table => {
        table.PrimaryKey("PK_Articles", x => x.Id);
    });

    migrationBuilder.CreateTable("Tags", ...);
    migrationBuilder.CreateTable("ArticleTags", ...);

    migrationBuilder.CreateIndex("IX_Articles_Slug", "Articles", "Slug", unique: true);
    migrationBuilder.CreateIndex("IX_Tags_Name", "Tags", "Name", unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable("ArticleTags");
    migrationBuilder.DropTable("Articles");
    migrationBuilder.DropTable("Tags");
}
```

This is the complete schema: three tables, two unique indexes, and a composite primary key on `ArticleTags`.

### How migrations work

1. You change an entity class (e.g., add a `ViewCount` property to `Article`)
2. You run `dotnet ef migrations add AddViewCount`
3. EF Core compares the current entity model to the **model snapshot** (`WikiDbContextModelSnapshot.cs`) and generates a new migration file with the delta
4. You run `dotnet ef database update` — or, in this project, just restart the app, since `db.Database.Migrate()` runs at startup

The model snapshot (`Migrations/WikiDbContextModelSnapshot.cs`) is EF Core's record of what the database schema looks like after all migrations have been applied. Do not hand-edit it.

### Common migration commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply pending migrations to the database
dotnet ef database update

# Roll back to a specific migration
dotnet ef database update <MigrationName>

# Remove the last (unapplied) migration
dotnet ef migrations remove

# See migration history
dotnet ef migrations list
```

> **Common beginner confusion:** "I changed my entity but the database didn't update." You need to run `dotnet ef migrations add <Name>` to generate a migration file, and then apply it (either via `database update` or by restarting the app). Changing the entity class alone does nothing to the database.

---

## 22. A complete request flow, end to end

Let's trace what happens when the frontend calls `POST /api/articles` to create a new article.

### The request

```http
POST /api/articles HTTP/1.1
Host: localhost:5018
Content-Type: application/json

{
  "title": "Understanding EF Core",
  "summary": "A deep dive into Entity Framework Core",
  "content": "# EF Core\n\nEF Core is an ORM...",
  "category": "Architecture",
  "tags": ["database", "efcore"],
  "status": 1
}
```

### Step 1 — ASP.NET Core receives the request

The Kestrel web server (built into ASP.NET Core) receives the TCP connection and parses the HTTP request.

### Step 2 — Middleware pipeline runs

1. **Swagger middleware** — sees the path is `/api/articles`, not `/swagger/*`. Passes through
2. **CORS middleware** — checks the `Origin` header. If it matches `http://localhost:5173`, adds `Access-Control-Allow-Origin: http://localhost:5173` to the response (this will be sent when the response is built)

### Step 3 — Routing matches the controller action

The routing system matches `POST /api/articles` to `ArticlesController.Create`:

- `[Route("api/articles")]` on the class matches `api/articles`
- `[HttpPost]` on the action matches the POST method

### Step 4 — Model binding

ASP.NET Core reads the `Content-Type: application/json` header and uses the JSON deserialiser to parse the request body into a `CreateArticleRequest` record:

```csharp
var request = new CreateArticleRequest(
    Title: "Understanding EF Core",
    Slug: null,
    Summary: "A deep dive into Entity Framework Core",
    Content: "# EF Core\n\nEF Core is an ORM...",
    Category: "Architecture",
    Tags: ["database", "efcore"],
    Status: ArticleStatus.Published
);
```

### Step 5 — Validation

```csharp
var validation = await validator.ValidateAsync(request);
```

`CreateArticleRequestValidator` runs all rules:
- `Title` is not empty and ≤ 200 chars ✓
- `Slug` is null (optional) — rule skipped ✓
- `Summary` is not empty and ≤ 500 chars ✓
- `Content` is not empty ✓
- `Category` is not empty and ≤ 100 chars ✓
- Each tag is ≤ 50 chars ✓

All rules pass. Execution continues.

### Step 6 — Service layer

```csharp
var article = await _articleService.CreateAsync(request);
```

Inside `ArticleService.CreateAsync`:

**6a.** Generate a slug from the title:

```csharp
var slug = GenerateSlug("Understanding EF Core");
// → "understanding-ef-core"
```

**6b.** Ensure the slug is unique:

```csharp
slug = await EnsureUniqueSlugAsync("understanding-ef-core");
// Queries DB: SELECT 1 FROM Articles WHERE Slug = 'understanding-ef-core'
// → No match; slug is "understanding-ef-core"
```

**6c.** Resolve tags:

```csharp
var tags = await ResolveTagsAsync(["database", "efcore"]);
// Query: SELECT * FROM Tags WHERE Name IN ('database', 'efcore')
// "database" exists; "efcore" does not
// INSERT INTO Tags (Name) VALUES ('efcore')
// Returns: [Tag{Id=3, Name="database"}, Tag{Id=8, Name="efcore"}]
```

**6d.** Create the entity and insert it:

```csharp
var article = new Article
{
    Title = "Understanding EF Core",
    Slug = "understanding-ef-core",
    // ...
    ArticleTags = [
        new ArticleTag { Tag = tags[0] },  // database
        new ArticleTag { Tag = tags[1] }   // efcore
    ]
};

_db.Articles.Add(article);
await _db.SaveChangesAsync();
// EF Core generates:
// INSERT INTO Articles (Title, Slug, ...) VALUES ('Understanding EF Core', 'understanding-ef-core', ...)
// INSERT INTO ArticleTags (ArticleId, TagId) VALUES (7, 3), (7, 8)
```

**6e.** Log the creation:

```csharp
_logger.LogInformation("Created article {Id} '{Title}'", 7, "Understanding EF Core");
// Console: info: WikiProject.Api.Services.ArticleService[0]
//          Created article 7 'Understanding EF Core'
```

**6f.** Re-fetch and return the DTO:

```csharp
return await GetByIdAsync(article.Id) ?? article.ToDto();
// SELECT a.*, at.*, t.* FROM Articles a
// JOIN ArticleTags at ON ... JOIN Tags t ON ...
// WHERE a.Id = 7
```

The `Article` entity is converted to `ArticleDto` via `ArticleMappings.ToDto()`.

### Step 7 — Controller builds the response

```csharp
return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
```

ASP.NET Core serialises the `ArticleDto` to JSON and builds:

```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: /api/articles/7

{
  "id": 7,
  "title": "Understanding EF Core",
  "slug": "understanding-ef-core",
  "summary": "A deep dive into Entity Framework Core",
  "content": "# EF Core\n\nEF Core is an ORM...",
  "category": "Architecture",
  "tags": ["database", "efcore"],
  "status": "Published",
  "createdAt": "2026-03-16T02:20:04Z",
  "updatedAt": "2026-03-16T02:20:04Z"
}
```

### Step 8 — CORS headers are added

The CORS middleware adds `Access-Control-Allow-Origin: http://localhost:5173` to the response headers before it is sent.

### Step 9 — Response is sent

Kestrel sends the HTTP response over the TCP connection to the React frontend.

### Summary of the flow

```
Browser
  └─ POST /api/articles (JSON body)
       │
       ▼
  Kestrel (HTTP server)
       │
       ▼
  Middleware: CORS (check origin, queue header)
       │
       ▼
  Routing → ArticlesController.Create
       │
       ▼
  Model binding (JSON → CreateArticleRequest)
       │
       ▼
  Validation (FluentValidation)
       │
       ▼
  ArticleService.CreateAsync
       │
       ├─ GenerateSlug
       ├─ EnsureUniqueSlugAsync (DB query)
       ├─ ResolveTagsAsync (DB query + insert)
       ├─ Article insert (DB)
       ├─ Log message
       └─ GetByIdAsync (DB query) → ArticleDto
       │
       ▼
  201 Created + JSON body
       │
       ▼
  CORS header appended
       │
       ▼
Browser receives response
```

---

## 23. Alternative approaches and trade-offs

### DTOs vs. exposing entities directly

**Exposing entities:** Some frameworks (especially in tutorials) serialise database entities directly to JSON. This works for trivial cases but creates serious problems at scale:
- Your API contract is tied to your database schema
- Circular references in navigation properties break the JSON serialiser
- You leak internal database details (raw IDs of join tables, etc.)
- Adding `[JsonIgnore]` attributes to entities mixes presentation concerns into the data model

**DTOs:** More code upfront, but the API contract is stable and independent. This project takes the DTO approach, which is the industry standard for production APIs.

### Services vs. fat controllers

**Fat controllers:** All logic in the controller. Simple for demos. Quickly becomes a maintenance nightmare. The controller file grows without bound, is hard to test, and mixes HTTP concerns with business logic.

**Service layer (this project):** Controllers are thin HTTP adapters. Business logic lives in services. Services are testable in isolation. This is the industry standard.

**CQRS / MediatR:** A more advanced pattern where every operation is a separate command or query handler. Excellent for large systems but adds significant boilerplate for a project this size.

### EF Core vs. raw SQL vs. Dapper

| | EF Core | Dapper | Raw SQL (ADO.NET) |
|---|---|---|---|
| Developer experience | High-level LINQ, change tracking | Thin wrapper, write SQL | Low-level, verbose |
| Code volume | Less code for CRUD | Medium | Most code |
| Performance | Good; some overhead for complex queries | Excellent | Excellent |
| Type safety | Full C# type safety | SQL strings are not type-checked | SQL strings are not type-checked |
| Migration support | Built-in | None | None |
| Learning curve | Higher (understand ORM abstractions) | Lower | Lowest |
| Best for | Standard CRUD apps | Performance-critical queries, complex SQL | Full control needed |

For WikiProject, EF Core is the right choice: the queries are not particularly complex, and the migration and change-tracking features save significant effort. In practice, many applications use EF Core for standard CRUD and Dapper for complex analytical queries.

### SQLite vs. larger database engines

| | SQLite | PostgreSQL | SQL Server |
|---|---|---|---|
| Setup | None — file only | Server required | Server required |
| Concurrency | Limited (single writer) | Full | Full |
| Data types | Limited (no native JSON column, date types stored as TEXT or INTEGER) | Rich (JSON, arrays, etc.) | Rich |
| Max DB size | ~281 TB (theoretical); practical limit ~1 GB | Unlimited | Unlimited |
| Cost | Free | Free | Expensive license |
| Horizontal scaling | Not supported | Supported | Supported |

For WikiProject in its current form (single server, small team), SQLite is appropriate and convenient. The EF Core abstraction means switching to PostgreSQL later is a configuration change, not a rewrite.

---

## 24. What to study next

If this document made sense and you want to go deeper, here are the recommended next steps:

### Official documentation
- [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/) — comprehensive, well-written
- [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/) — especially the "Querying Data" and "Saving Data" sections
- [FluentValidation documentation](https://docs.fluentvalidation.net/) — covers all built-in validators and custom rules

### Topics this document did not cover
- **Authentication and authorisation** — the codebase has extension points for this (see the "Authentication Integration (Planned)" seed article and the comment in `Program.cs`). Study ASP.NET Core Identity and JWT bearer tokens
- **Unit testing the service layer** — use xUnit + EF Core's in-memory provider to test `ArticleService` without a real database
- **Integration testing** — use `WebApplicationFactory<T>` to test the full HTTP pipeline in memory
- **Full-text search** — the current `LIKE '%search%'` approach does not use indexes; for larger datasets, consider SQLite's FTS5 extension
- **Response caching** — `IMemoryCache` or `IDistributedCache` for read-heavy endpoints
- **API versioning** — for when you need to change the API contract without breaking existing clients

### Related documentation files
- `docs/01_*` — project overview and setup (if present)
- `docs/03_*` — frontend deep dive (React, TypeScript, API client layer)
- `docs/04_*` — deployment and infrastructure guide
