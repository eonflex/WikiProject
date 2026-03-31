# Glossary and Core Concepts

> **Who this is for:** A developer who is new to this codebase (or to full-stack web development in general) and wants a plain-English explanation of the terms that appear throughout WikiProject's source code, documentation, and team conversations.
>
> **What this is not:** A broad computer-science encyclopedia. Every term here is explained *in the context of this project*, with concrete examples pulled from the real code. Where a concept belongs to a separate layer (backend, frontend, database), that context is stated clearly.

---

## Table of Contents

1. [API](#1-api)
2. [REST](#2-rest)
3. [Endpoint](#3-endpoint)
4. [CRUD](#4-crud)
5. [DTO (Data Transfer Object)](#5-dto-data-transfer-object)
6. [Serialization](#6-serialization)
7. [Validation](#7-validation)
8. [Entity](#8-entity)
9. [Model](#9-model)
10. [ORM (Object-Relational Mapper)](#10-orm-object-relational-mapper)
11. [DbContext](#11-dbcontext)
12. [Migration](#12-migration)
13. [Seeding](#13-seeding)
14. [Service Layer](#14-service-layer)
15. [Dependency Injection](#15-dependency-injection)
16. [Middleware](#16-middleware)
17. [Routing (Backend)](#17-routing-backend)
18. [CORS](#18-cors)
19. [Slug](#19-slug)
20. [Component](#20-component)
21. [Props](#21-props)
22. [State](#22-state)
23. [Hook](#23-hook)
24. [Client-Side Routing](#24-client-side-routing)

---

## 1. API

### Plain-English Definition

An **API** (Application Programming Interface) is a contract that defines how two pieces of software talk to each other. Think of it as a restaurant menu: the menu tells you what you can order and in what format. The kitchen (the server) handles the actual cooking; you (the client) just need to know the menu.

In a web context, this usually means: "send me an HTTP request shaped *this* way, and I'll send back data shaped *that* way."

### What It Means in This Project

WikiProject has a backend API built with **ASP.NET Core** that the React frontend talks to. The API lives at `http://localhost:5018` and exposes routes under `/api/…`. The frontend does not read from the database directly — it always goes through the API.

```
Browser (React)  ──HTTP request──►  ASP.NET Core API  ──SQL──►  SQLite database
                 ◄──JSON response──                   ◄────────
```

The Swagger UI at `http://localhost:5018/swagger` is a visual representation of the API's menu — you can read it and even test calls from your browser.

### Common Misunderstanding

> "API" only means a web API.

APIs exist everywhere: the operating system has APIs for opening files, your browser has a DOM API for manipulating web pages, and third-party libraries expose APIs for using their code. "Web API" or "HTTP API" is the specific kind we use here.

### Why It Matters

Without a clearly defined API, the frontend and backend become tightly coupled — any change in one breaks the other. By agreeing on a contract (the API), the two teams (or the same developer in two moods) can work independently.

---

## 2. REST

### Plain-English Definition

**REST** (Representational State Transfer) is a *style* for designing HTTP APIs. It is not a protocol or a library — it is a set of conventions that make APIs predictable. The most important convention: use **HTTP verbs** (GET, POST, PUT, DELETE) to describe *what action* you're taking on a **resource** (a "thing" your API manages), and use **URLs** to identify *which* resource you're operating on.

| HTTP Verb | Conventional Meaning     |
|-----------|--------------------------|
| GET       | Read / fetch data        |
| POST      | Create new data          |
| PUT       | Replace / update data    |
| DELETE    | Remove data              |

### What It Means in This Project

WikiProject's API is RESTful. The "resource" is an *article*. Look at `ArticlesController.cs`:

```csharp
[HttpGet]                   // GET  /api/articles       → list articles
[HttpGet("{id:int}")]       // GET  /api/articles/5     → get one article
[HttpPost]                  // POST /api/articles       → create new article
[HttpPut("{id:int}")]       // PUT  /api/articles/5     → update article 5
[HttpDelete("{id:int}")]    // DELETE /api/articles/5   → delete article 5
```

The URL names the *thing* (`articles`), and the verb names the *action*.

### Common Misunderstanding

> REST requires JSON.

REST is transport-agnostic. You could use XML, plain text, or any format. JSON is simply the most popular choice today, and it is what WikiProject uses.

> REST is a strict standard you can pass or fail.

REST is a set of guidelines. Very few real-world APIs follow every guideline perfectly. Most developers say "RESTful" to mean "roughly following REST conventions," not "100% compliant."

### Why It Matters

A RESTful design means a new developer (or a new frontend) can guess what an unknown endpoint does just from its URL and verb. `DELETE /api/articles/7` is self-explanatory. That predictability reduces bugs and documentation burden.

---

## 3. Endpoint

### Plain-English Definition

An **endpoint** is a specific URL + HTTP verb combination that your API responds to. It is one entry on the menu. Calling an endpoint means sending an HTTP request to that URL with the matching verb.

### What It Means in This Project

WikiProject has eight endpoints defined across two controllers:

| Verb   | URL                          | Controller              |
|--------|------------------------------|-------------------------|
| GET    | `/api/articles`              | `ArticlesController`    |
| GET    | `/api/articles/{id}`         | `ArticlesController`    |
| GET    | `/api/articles/slug/{slug}`  | `ArticlesController`    |
| POST   | `/api/articles`              | `ArticlesController`    |
| PUT    | `/api/articles/{id}`         | `ArticlesController`    |
| DELETE | `/api/articles/{id}`         | `ArticlesController`    |
| GET    | `/api/categories`            | `MetadataController`    |
| GET    | `/api/tags`                  | `MetadataController`    |

Each endpoint is a C# method decorated with an `[Http*]` attribute:

```csharp
// src/WikiProject.Api/Controllers/ArticlesController.cs
[HttpGet("{id:int}")]
public async Task<ActionResult<ArticleDto>> GetById(int id)
{
    var article = await _articleService.GetByIdAsync(id);
    return article is null ? NotFound() : Ok(article);
}
```

### Common Misunderstanding

> Every URL is an endpoint.

Only URLs that your application explicitly handles are endpoints. If someone requests `/api/unicorns`, ASP.NET Core returns a 404 because there is no matching endpoint registered.

### Why It Matters

Endpoints are the seams between systems. When the frontend is broken, the first debugging step is often: "Is the endpoint returning the right data?" Tools like Swagger and the browser's DevTools Network tab let you inspect endpoint calls in real time.

---

## 4. CRUD

### Plain-English Definition

**CRUD** is an acronym for the four fundamental operations you can do with persistent data:

- **C**reate — add a new record
- **R**ead — fetch existing records
- **U**pdate — change an existing record
- **D**elete — remove a record

Almost every data-driven application is, at its core, a CRUD application.

### What It Means in This Project

WikiProject is a CRUD application for articles. The full CRUD cycle maps directly to REST endpoints and `IArticleService` methods:

| CRUD   | HTTP Verb | Endpoint               | Service Method          |
|--------|-----------|------------------------|-------------------------|
| Create | POST      | `/api/articles`        | `CreateAsync()`         |
| Read   | GET       | `/api/articles`        | `GetArticlesAsync()`    |
| Read   | GET       | `/api/articles/{id}`   | `GetByIdAsync()`        |
| Update | PUT       | `/api/articles/{id}`   | `UpdateAsync()`         |
| Delete | DELETE    | `/api/articles/{id}`   | `DeleteAsync()`         |

### Common Misunderstanding

> CRUD and REST are the same thing.

CRUD describes *what operations* you perform on data. REST describes *how you expose those operations* over HTTP. REST is one way to implement CRUD. You could also do CRUD through a desktop UI, a background job, or a command-line tool — none of which are REST.

### Why It Matters

Thinking in CRUD terms helps you structure new features. When asked to add a new entity (e.g., "comments on articles"), you'll naturally think: "I need Create, Read, Update, and Delete for comments" — which translates to a new controller, service, and set of endpoints.

---

## 5. DTO (Data Transfer Object)

### Plain-English Definition

A **DTO** (Data Transfer Object) is a simple data container used specifically for moving information *across a boundary* — for example, between the database layer and the API layer, or between the API and the frontend. DTOs typically have no behavior (no methods that do complex logic); they just hold data.

Think of a DTO as a *shipping container*: it carries exactly what you want to send, packed in a way that makes sense for the recipient, without exposing everything in your warehouse.

### What It Means in This Project

WikiProject uses DTOs to separate the API's public shape from the database entity's internal shape. They live in `src/WikiProject.Api/DTOs/ArticleDtos.cs`.

**Example: `ArticleDto`** (what the API returns to the frontend):

```csharp
public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,   // ← tags as plain strings, not ArticleTag objects
    string Status,                 // ← "Draft", "Published", "Archived" — human-readable
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

Compare this to the `Article` entity. The entity has `ICollection<ArticleTag> ArticleTags` (a list of join-table objects). The DTO flattens that into `IReadOnlyList<string> Tags` — just the names. The frontend doesn't need to know about `ArticleTag` at all.

**Why two DTOs for articles?**

- `ArticleDto` — the *full* article, including `Content`. Used when you're viewing a single article.
- `ArticleSummaryDto` — no `Content` field. Used in the article list. Sending full article content for every item in a list of 100 articles wastes bandwidth and slows the page down.

**Request DTOs** carry data *from* the frontend *to* the backend:

```csharp
public record CreateArticleRequest(
    string Title,
    string? Slug,      // optional; auto-generated if not provided
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status
);
```

### Common Misunderstanding

> DTOs are the same as entities.

An entity represents a row in a database and is tied to EF Core's change-tracking. A DTO is a plain data container with no database awareness. Exposing entities directly through your API is an anti-pattern because it leaks internal implementation details and creates tight coupling between your database schema and your API contract.

> DTOs are only for responses.

DTOs are used for both request bodies (what the client sends) and responses (what the server sends back). `CreateArticleRequest` is a request DTO; `ArticleDto` is a response DTO.

### Why It Matters

DTOs give you control over *exactly* what data travels across the wire. They let you:

- Omit sensitive fields (e.g., internal IDs or audit columns you don't want exposed)
- Rename fields for clarity without changing the database schema
- Include computed or flattened data (like the tag names list)
- Version your API without changing your database

---

## 6. Serialization

### Plain-English Definition

**Serialization** is the process of converting an in-memory object (a C# class or TypeScript object) into a format that can be stored or transmitted — most commonly a string of JSON. The reverse process — converting JSON back into an in-memory object — is called **deserialization**.

```
C# ArticleDto object  ──serialize──►  {"id":1,"title":"Welcome",...}  ──HTTP──►  Browser
                      ◄──deserialize──
```

### What It Means in This Project

This happens transparently when ASP.NET Core processes a request or builds a response.

**Incoming request (deserialization):** When the React frontend sends a POST to `/api/articles` with a JSON body, ASP.NET Core automatically reads the JSON and converts it into a `CreateArticleRequest` C# object before your controller method even runs.

**Outgoing response (serialization):** When your controller calls `Ok(article)`, ASP.NET Core converts the `ArticleDto` object into JSON and writes it into the HTTP response body.

The default serializer in ASP.NET Core (System.Text.Json) uses **camelCase** for JSON property names, even though C# uses **PascalCase** for properties. So `ArticleDto.CreatedAt` becomes `"createdAt"` in JSON.

On the frontend, Axios (`articleService.ts`) handles deserialization automatically: the `data` property of an Axios response is already a JavaScript object.

### Common Misunderstanding

> You have to write serialization code yourself.

In modern frameworks, serialization is automatic. You rarely need to touch it unless you need custom behavior (e.g., special date formatting or skipping null fields).

> JSON and a JavaScript object are the same thing.

JSON is a *string* — a piece of text. A JavaScript object is an in-memory data structure. `JSON.stringify()` converts the object to a string; `JSON.parse()` does the reverse. Axios hides this distinction for you.

### Why It Matters

Understanding serialization helps you diagnose mismatches between the frontend and backend. If the frontend receives `createdAt` but your TypeScript type says `CreatedAt`, the value will be `undefined`. Knowing that ASP.NET Core camelCases field names by default saves hours of debugging.

---

## 7. Validation

### Plain-English Definition

**Validation** is the process of checking that data meets your rules before you try to use it. For example, "the title must not be blank and must be at most 200 characters." If validation fails, you return an error to the caller instead of processing the bad data.

### What It Means in This Project

WikiProject uses **FluentValidation** for backend validation. The validators live in `src/WikiProject.Api/Validation/`.

```csharp
// Inferred from project structure and csproj dependencies
public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => x.Slug is not null);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Tags).ForEach(tag => tag.MaximumLength(50));
    }
}
```

The controller runs the validator before calling the service. If validation fails, it returns a **400 Bad Request** with a structured error body:

```csharp
var validation = await validator.ValidateAsync(request);
if (!validation.IsValid)
    return ValidationProblem(new ValidationProblemDetails(
        validation.ToDictionary()));
```

The frontend also does client-side validation in `ArticleForm.tsx` (e.g., checking max lengths and the slug regex). This is a **defense-in-depth** approach: the frontend catches obvious mistakes early (better UX), but the backend *always* validates too (security).

### Common Misunderstanding

> Client-side validation is enough.

Never trust data from the browser. A user can bypass frontend validation by sending raw HTTP requests (e.g., with `curl` or Postman). Backend validation is mandatory for security and data integrity.

> Validation and authorization are the same thing.

Validation checks the *shape* of data ("is this a valid email address?"). Authorization checks *permissions* ("is this user allowed to delete this article?"). They are separate concerns and both are important.

### Why It Matters

Validation prevents bad data from entering your database and provides clear error messages to users. Without it, you'd get cryptic database errors or — worse — corrupt data that's hard to clean up later.

---

## 8. Entity

### Plain-English Definition

An **entity** is a class that represents a single row in a database table. It is EF Core's representation of your data in C#. When you load data from the database, EF Core creates entity instances; when you add or change an entity and save, EF Core writes the changes back to the database.

### What It Means in This Project

WikiProject has three entities, all in `src/WikiProject.Api/Entities/`:

**`Article`** — maps to the `Articles` table:

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

**`Tag`** — maps to the `Tags` table. Stores unique tag names.

**`ArticleTag`** — the **join entity** for the many-to-many relationship between `Article` and `Tag`. Because an article can have many tags and a tag can belong to many articles, you need a middle table (and a middle class) that holds pairs of `(ArticleId, TagId)`.

### Common Misunderstanding

> Entities and DTOs are interchangeable.

Entities are tied to EF Core's change tracking — EF Core watches them for modifications and generates SQL accordingly. DTOs are plain containers with no database awareness. Mixing them up leads to accidental database writes or exposing implementation details through the API.

> Navigation properties are automatically loaded.

The `ArticleTags` property on `Article` is a *navigation property* — it points to related records in another table. It is **not** loaded automatically. You must explicitly tell EF Core to load it using `.Include()` and `.ThenInclude()`, or it will be null or empty.

```csharp
// Without Include — ArticleTags will be empty
var article = db.Articles.FirstOrDefault(a => a.Id == id);

// With Include — ArticleTags and their Tags are loaded
var article = db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .FirstOrDefault(a => a.Id == id);
```

### Why It Matters

Entities are the bridge between your C# code and your database. Understanding how EF Core tracks changes through entities (and the lazy vs. eager loading distinction) is key to writing correct and performant data access code.

---

## 9. Model

### Plain-English Definition

"**Model**" is an overloaded term in software development. Broadly, it means a class that represents a concept or piece of data in your application. In different contexts it can mean:

- A **database entity** (the EF Core sense — a row in a table)
- A **view model** (data shaped specifically for what the UI needs to display)
- A **domain model** (a rich object with business logic)
- In MVC, the "M" in Model-View-Controller

### What It Means in This Project

WikiProject's C# codebase tends to use the more specific terms **entity** (for database-backed classes) and **DTO** (for API-shaped data). You will hear "model" used informally to refer to any of these.

On the TypeScript/frontend side, `frontend/src/types/index.ts` defines interfaces like `Article`, `ArticleSummary`, and `ArticleFilters`. These are TypeScript models — they describe the shape of data the frontend works with, mirroring the JSON the backend sends.

```typescript
interface Article {
  id: number;
  title: string;
  slug: string;
  summary: string;
  content: string;
  category: string;
  tags: string[];
  status: ArticleStatus;
  createdAt: string;
  updatedAt: string;
}
```

### Common Misunderstanding

> Every class is a model.

Not everything in the codebase is a "model." Controllers handle HTTP, services contain business logic, validators check data shapes. The term "model" specifically refers to classes that describe data structure.

### Why It Matters

Being clear about which type of "model" you mean when talking to teammates prevents confusion. When a teammate says "update the model," ask: "Do you mean the entity, the DTO, or the frontend TypeScript interface?"

---

## 10. ORM (Object-Relational Mapper)

### Plain-English Definition

An **ORM** is a library that bridges the gap between object-oriented code (C# classes) and relational databases (tables and rows). Without an ORM, you'd write raw SQL strings in your code and manually convert result sets into objects. With an ORM, you write C# and the library generates the SQL for you.

The "impedance mismatch" problem: databases think in tables and rows; programming languages think in objects and references. An ORM translates between these two worlds.

### What It Means in This Project

WikiProject uses **Entity Framework Core** (EF Core) as its ORM. Instead of writing:

```sql
SELECT a.*, t.Name FROM Articles a
LEFT JOIN ArticleTags at ON at.ArticleId = a.Id
LEFT JOIN Tags t ON t.Id = at.TagId
WHERE a.Id = @id
```

You write C#:

```csharp
// src/WikiProject.Api/Services/ArticleService.cs
var article = await _db.Articles
    .Include(a => a.ArticleTags)
    .ThenInclude(at => at.Tag)
    .AsNoTracking()
    .FirstOrDefaultAsync(a => a.Id == id);
```

EF Core translates this LINQ query into the equivalent SQL and maps the results back into `Article` and `ArticleTag` objects.

### Common Misunderstanding

> The ORM always generates the most efficient SQL.

ORMs are convenient but can generate slow or redundant SQL if used carelessly. Common pitfalls include the "N+1 problem" (loading a list of articles, then making one extra database query per article to load its tags) and forgetting to use `.AsNoTracking()` for read-only queries. Understanding what SQL your ORM generates is important for performance.

> You never need to know SQL when using an ORM.

You still need a basic understanding of SQL and relational databases. ORMs generate SQL — if you can't read SQL, you can't diagnose query problems.

### Why It Matters

EF Core dramatically reduces boilerplate and keeps your data access code in one language (C#). Migrations (see below) are another EF Core feature that automates schema changes. However, for complex reports or performance-critical queries, raw SQL can still be executed through EF Core using `FromSqlRaw()`.

**Alternative ORM:** [Dapper](https://github.com/DapperLib/Dapper) is a lighter-weight "micro-ORM" popular in the .NET ecosystem. It requires you to write SQL but handles the mapping from rows to objects. It's faster than EF Core for reads but doesn't handle migrations or change tracking.

---

## 11. DbContext

### Plain-English Definition

A **DbContext** is the central class in EF Core. It is your application's gateway to the database. It manages:

- Which entities it knows about (via `DbSet<T>` properties)
- The database connection
- Change tracking (knowing which objects you've added, modified, or removed)
- Query execution

Think of it as the "session" with the database.

### What It Means in This Project

`WikiDbContext` lives in `src/WikiProject.Api/Data/WikiDbContext.cs`:

```csharp
public class WikiDbContext : DbContext
{
    public WikiDbContext(DbContextOptions<WikiDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure composite PK on ArticleTag
        modelBuilder.Entity<ArticleTag>()
            .HasKey(at => new { at.ArticleId, at.TagId });

        // Unique index on Article.Slug
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.Slug)
            .IsUnique();

        // Unique index on Tag.Name
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();
    }
}
```

The three `DbSet<T>` properties are how you query each table. For example, `_db.Articles.Where(a => a.Status == ArticleStatus.Published)` generates `SELECT ... FROM Articles WHERE Status = 1`.

`OnModelCreating` is the **Fluent API** configuration — it's where you define relationships, indexes, and constraints that can't be inferred automatically from the entity classes.

The `DbContext` is registered with the DI container in `Program.cs` as a **scoped** service (a new instance per HTTP request):

```csharp
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

### Common Misunderstanding

> You should make `DbContext` a singleton.

`DbContext` is **not thread-safe** and holds open connections. Making it a singleton in a web app will cause bugs ranging from stale data to race conditions. Always use scoped lifetime (the default) so each HTTP request gets its own `DbContext`.

> `DbContext` auto-saves changes.

You must call `await _db.SaveChangesAsync()` to persist changes. Until you do, everything lives only in memory. EF Core batches your changes and writes them all at once, which is more efficient.

### Why It Matters

`WikiDbContext` is the single place where all database configuration lives. If you add a new entity, you add a `DbSet` here and configure relationships in `OnModelCreating`. If you change indexes or add constraints, you do it here and then create a new migration.

---

## 12. Migration

### Plain-English Definition

A **migration** is a versioned, code-generated description of how to change your database schema from one state to another. Think of it like a version-control commit for your database. Each migration has an "up" (apply the change) and a "down" (undo the change).

Without migrations, you'd have to manually write and run SQL `CREATE TABLE`, `ALTER TABLE`, and `DROP TABLE` statements every time your data model changed, and coordinate those changes across every developer's local database and every deployment environment.

### What It Means in This Project

WikiProject has one migration so far: `20260315235834_InitialCreate`, generated automatically by EF Core and stored in `src/WikiProject.Api/Migrations/`.

It was created by running:

```bash
cd src/WikiProject.Api
dotnet-ef migrations add InitialCreate
```

EF Core compared the current entity classes to the (empty) database and generated C# code that creates all three tables with correct columns, indexes, and foreign key constraints.

**Migrations are applied automatically on startup** in `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
    db.Database.Migrate();   // applies any pending migrations
    await SeedData.SeedAsync(db);
}
```

**To add a migration after changing an entity:**

```bash
cd src/WikiProject.Api
dotnet-ef migrations add YourMigrationName
dotnet-ef database update   # optional; startup auto-applies it
```

**To undo the last migration:**

```bash
dotnet-ef migrations remove
```

### Common Misunderstanding

> You can just edit entity classes and the database updates automatically.

You must explicitly create a migration. EF Core does not watch your entity classes for changes and silently alter the database. The migration step is intentional — it gives you a chance to review and test the SQL before it runs.

> Migrations are only for adding things.

Migrations can handle adding tables, removing tables, renaming columns, adding indexes, changing column types, and more. Destructive operations (like dropping a column) are perfectly legal in a migration, but be careful — they are irreversible once applied to production data.

### Why It Matters

Migrations are how the database stays in sync with your C# code as the project evolves. Without them, each developer would have to manually maintain their local database, and deployments would require out-of-band SQL scripts. With EF Core migrations, the database schema is version-controlled alongside the code.

---

## 13. Seeding

### Plain-English Definition

**Database seeding** is the process of inserting initial or sample data into a database when it is first created. "Seeds" are the data you plant so the database isn't empty.

### What It Means in This Project

`SeedData.cs` in `src/WikiProject.Api/Data/` runs automatically on startup. It checks whether any articles already exist; if not, it inserts six sample articles and seven sample tags:

```csharp
public static async Task SeedAsync(WikiDbContext db)
{
    if (await db.Articles.AnyAsync())
        return;   // already seeded — don't run again

    // Create tags
    var tagNames = new[] { "getting-started", "guide", "api", "database", ... };
    var tags = tagNames.Select(n => new Tag { Name = n }).ToList();
    db.Tags.AddRange(tags);
    await db.SaveChangesAsync();

    // Create articles with linked tags
    var articles = new List<Article> { ... };
    db.Articles.AddRange(articles);
    await db.SaveChangesAsync();
}
```

This means a new developer can clone the repo, run `dotnet run`, and immediately have a working app with example content — no manual data entry required.

**To re-seed:** Delete the `wiki.db` file in `src/WikiProject.Api/` and restart the backend.

### Common Misunderstanding

> Seeding happens every time the app starts.

In WikiProject, the seeder checks `if (await db.Articles.AnyAsync()) return;` — so it only runs once, when the database is empty. Running it multiple times would create duplicate articles.

> Seed data is for production use.

Seed data is typically for development and testing. Production databases usually start with only the data that users actually create. However, some applications do use seeds for required system data (e.g., default roles or configuration records).

### Why It Matters

Seed data makes the developer experience much smoother. Without it, every developer who sets up the project has to manually create test data before they can see anything meaningful in the UI.

---

## 14. Service Layer

### Plain-English Definition

The **service layer** is a layer of code that contains your application's *business logic* — the rules about what your application actually *does*, independent of how data is stored or how requests arrive. Think of it as the "brain" of your application: it coordinates between the database (entities) and the API (controllers/DTOs).

A well-designed service layer can be tested without HTTP, without a database, and without a frontend.

### What It Means in This Project

WikiProject's service layer consists of:

- **`IArticleService`** — the interface defining the contract (`src/WikiProject.Api/Services/IArticleService.cs`)
- **`ArticleService`** — the implementation (`src/WikiProject.Api/Services/ArticleService.cs`)

The controller delegates all real work to the service:

```csharp
// In ArticlesController — thin, no business logic
[HttpPost]
public async Task<ActionResult<ArticleDto>> Create(
    [FromBody] CreateArticleRequest request,
    [FromServices] IValidator<CreateArticleRequest> validator)
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return ValidationProblem(...);

    var article = await _articleService.CreateAsync(request);  // delegate to service
    return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
}
```

The service handles the actual work:

```csharp
// In ArticleService — business logic lives here
public async Task<ArticleDto> CreateAsync(CreateArticleRequest request)
{
    var slug = request.Slug ?? GenerateSlug(request.Title);
    slug = await EnsureUniqueSlugAsync(slug);
    var tags = await ResolveTagsAsync(request.Tags);

    var article = new Article
    {
        Title = request.Title,
        Slug = slug,
        // ...
    };
    _db.Articles.Add(article);
    await _db.SaveChangesAsync();
    return article.ToDto();
}
```

Business logic in `ArticleService` includes:
- Auto-generating slugs from titles
- Ensuring slug uniqueness by appending `-1`, `-2`, etc.
- Resolving tag names (creating new tags if they don't exist)
- Setting `CreatedAt` / `UpdatedAt` timestamps
- Applying pagination limits (max page size: 100)

### Common Misunderstanding

> Business logic belongs in the controller.

This pattern — called a "fat controller" — is a common anti-pattern. Controllers should be thin HTTP adapters: parse the request, call a service, return a response. When controllers contain business logic, the logic becomes untestable (you can't call it without spinning up an HTTP stack) and hard to reuse.

> The service layer talks directly to the database.

The service layer in WikiProject does talk directly to the `DbContext` (through constructor injection). In larger projects, you might add a separate **repository layer** between the service and the database. WikiProject keeps it simple by letting the service use EF Core directly — a pragmatic choice for a project of this size.

### Why It Matters

The service layer makes business logic:

1. **Testable** — you can unit-test `ArticleService` by mocking `WikiDbContext`
2. **Reusable** — the same service can be called from multiple controllers, background jobs, or CLI tools
3. **Readable** — controllers stay thin and easy to understand

---

## 15. Dependency Injection

### Plain-English Definition

**Dependency Injection** (DI) is a technique where an object receives the other objects (its *dependencies*) from the outside, instead of creating them itself. Instead of `var service = new ArticleService(...)`, you ask the framework to give you an `IArticleService` instance and it figures out how to build one.

This is related to the *Inversion of Control* principle: instead of your code controlling how dependencies are created, you hand that control to a framework.

### What It Means in This Project

In `Program.cs`, services are **registered** with the DI container:

```csharp
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));          // "When someone needs a WikiDbContext, give them one with SQLite"

builder.Services.AddScoped<IArticleService, ArticleService>();
// "When someone needs an IArticleService, give them an ArticleService"
```

In `ArticlesController`, the dependencies are **injected** through the constructor:

```csharp
public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
{
    _articleService = articleService;
    _logger = logger;
}
```

ASP.NET Core sees that the controller needs an `IArticleService` and an `ILogger`, and automatically provides them when the controller is created for each request.

**Lifetimes in DI:**

| Lifetime   | Description                                        | Used For                       |
|------------|----------------------------------------------------|---------------------------------|
| Transient  | New instance every time it is requested            | Lightweight, stateless services |
| Scoped     | One instance per HTTP request                      | `DbContext`, business services  |
| Singleton  | One instance for the entire application lifetime   | Configuration, caching         |

`ArticleService` and `WikiDbContext` are both **scoped** — one per request.

### Common Misunderstanding

> DI is just a fancy way of passing parameters.

DI is more than passing parameters. The DI container manages lifetimes, resolves chains of dependencies automatically (if `ArticleService` needs a `DbContext`, the container provides it without you wiring it up manually), and makes testing much easier (you can swap real implementations for mocks in tests).

> `[FromServices]` is the only way to inject things.

In WikiProject, most injection happens via constructor (the most common pattern). `[FromServices]` is used on individual action method parameters — useful for injecting services that are only needed in one action, like the validator:

```csharp
public async Task<ActionResult<ArticleDto>> Create(
    [FromBody] CreateArticleRequest request,
    [FromServices] IValidator<CreateArticleRequest> validator)
```

### Why It Matters

DI is foundational to how ASP.NET Core works. Understanding it helps you:

- Know where to register new services
- Understand why you shouldn't `new` up services inside controllers
- Debug "no service registered" errors
- Write unit tests by injecting mock dependencies

---

## 16. Middleware

### Plain-English Definition

**Middleware** is code that runs in the middle of an HTTP request/response cycle, before your controller (or after it on the way back). Middleware components form a **pipeline** — the request passes through each piece of middleware in order, and the response passes back through them in reverse order.

Think of it like a series of airport security checks: every flight (request) goes through the same checkpoints in the same order. Each checkpoint can inspect the request, modify it, block it, or pass it along.

### What It Means in This Project

The middleware pipeline is configured in `Program.cs` using `app.Use*` calls:

```csharp
// Middleware registered in order:
app.UseSwagger();          // 1. Serve Swagger JSON (dev only)
app.UseSwaggerUI(...);     // 2. Serve Swagger HTML UI (dev only)
app.UseCors();             // 3. Add CORS headers to responses
// app.UseAuthentication(); // 4. (commented out, reserved for future auth)
// app.UseAuthorization();  // 5. (commented out, reserved for future auth)
app.MapControllers();      // 6. Route request to the right controller action
```

Order matters significantly:
- CORS must run before controllers so headers are added even if the request is rejected.
- Authentication must run before Authorization.
- `MapControllers` is always last — it is the "terminal" middleware that actually handles the request.

Built-in ASP.NET Core middleware used in this project:
- **Swagger/SwaggerUI** — generates and serves API documentation (development only)
- **CORS** — adds `Access-Control-Allow-Origin` headers
- **Routing/Controllers** — maps incoming URLs to controller methods

### Common Misunderstanding

> Middleware is only for authentication and logging.

Middleware can do anything: compression, rate limiting, caching, exception handling, request logging, response modification. Authentication and logging are the most common examples, but they're not the only use cases.

> Order doesn't matter.

Order matters a great deal. Placing `UseAuthentication` after `MapControllers` means requests reach your controllers before being authenticated — a security hole. Always check the recommended middleware order in Microsoft's documentation.

### Why It Matters

Middleware lets you add cross-cutting concerns (things that apply to every request) in one place, without touching every controller. When WikiProject eventually adds authentication, it will be one line in `Program.cs`, not changes to every controller action.

---

## 17. Routing (Backend)

### Plain-English Definition

**Routing** is the process of looking at an incoming HTTP request and deciding which piece of code should handle it. The router matches the request's URL and HTTP verb against a set of registered patterns and calls the matching handler.

### What It Means in This Project

ASP.NET Core uses **attribute routing** — routes are defined directly on controller classes and methods using attributes:

```csharp
[ApiController]
[Route("api/articles")]          // ← base route for this controller
public class ArticlesController : ControllerBase
{
    [HttpGet]                    // GET  /api/articles
    [HttpGet("{id:int}")]        // GET  /api/articles/5  (id must be an int)
    [HttpGet("slug/{slug}")]     // GET  /api/articles/slug/welcome-to-wikiproject
    [HttpPost]                   // POST /api/articles
    [HttpPut("{id:int}")]        // PUT  /api/articles/5
    [HttpDelete("{id:int}")]     // DELETE /api/articles/5
}
```

Route constraints like `{id:int}` ensure that only numeric values match — a request to `/api/articles/abc` won't match the `GetById` action and will return a 404.

The route `slug/{slug}` must be listed before `{id:int}` in concept but since ASP.NET Core's routing is smart about literal vs. parameter segments, `GET /api/articles/slug/my-article` correctly maps to `GetBySlug` rather than `GetById`.

### Common Misunderstanding

> The URL must exactly match the route template.

Route templates can have parameters (`{id}`) and constraints (`{id:int}`, `{slug:alpha}`). A single route template can match many different URLs.

> Route order in the file matters.

With attribute routing, each route is registered independently. The router uses a specificity algorithm to pick the best match — literal segments win over parameter segments. In contrast, older-style "conventional" routing (using `app.MapControllerRoute`) does depend on registration order.

### Why It Matters

Understanding routing helps you add new endpoints without accidentally creating conflicts. It also helps you read 404 errors: if an endpoint returns 404, check first whether the URL and verb match the route template exactly.

---

## 18. CORS

### Plain-English Definition

**CORS** (Cross-Origin Resource Sharing) is a browser security mechanism. Browsers enforce the **Same-Origin Policy** by default: JavaScript running on `http://localhost:5173` is not allowed to make HTTP requests to `http://localhost:5018` (a different port = a different "origin"). CORS is the protocol that lets a server tell the browser: "It's OK, I trust requests from `localhost:5173`."

This restriction only applies to browser JavaScript. Tools like `curl`, Postman, and server-to-server requests are not affected.

### What It Means in This Project

The backend's React dev server runs on port 5173; the API runs on port 5018. Without CORS, the browser would block the frontend from calling the API.

CORS is configured in `Program.cs`:

```csharp
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Later in the pipeline:
app.UseCors();
```

The README notes an alternative approach: the Vite dev server can **proxy** `/api` requests to `http://localhost:5018`, making both the frontend and API appear to be on the same origin from the browser's perspective. If that proxy was used, CORS wouldn't be needed. WikiProject uses server-side CORS configuration instead (more realistic for production scenarios).

### Common Misunderstanding

> CORS is a backend security feature.

CORS is enforced by the *browser*, not the server. The server says "I allow these origins"; the browser decides whether to honour the restriction. A malicious server that ignores CORS rules still can't protect users — but it also can't "turn off" the browser's same-origin enforcement by lying.

> A CORS error means the server is down.

A CORS error in the browser's DevTools console ("has been blocked by CORS policy") usually means the server is running fine but hasn't added the right `Access-Control-Allow-Origin` header. The fix is on the server side (add the origin to the allowed list), not the client side.

### Why It Matters

CORS errors are one of the most common frustrations for new full-stack developers. Understanding what CORS does — and that it only applies to browsers — eliminates a huge class of confusing bugs.

---

## 19. Slug

### Plain-English Definition

A **slug** is a URL-safe string that uniquely identifies a resource in human-readable form. Instead of `/articles/42` (which tells you nothing), a slug-based URL looks like `/articles/welcome-to-wikiproject`. Slugs are lowercase, use hyphens instead of spaces, and contain no special characters.

The word "slug" comes from newspaper typesetting, where a "slug" was a short label used to identify an article internally.

### What It Means in This Project

Every article in WikiProject has a `Slug` field. The `ArticleService` auto-generates a slug from the title if one isn't provided:

```csharp
// src/WikiProject.Api/Services/ArticleService.cs (inferred from behavior)
private static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant();
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");   // remove non-alphanumeric
    slug = Regex.Replace(slug, @"\s+", "-");              // spaces to hyphens
    slug = Regex.Replace(slug, @"-+", "-");               // collapse multiple hyphens
    return slug[..Math.Min(slug.Length, 100)];            // max 100 chars
}
```

So "Setting Up the Development Environment" becomes `setting-up-the-development-environment`.

**Uniqueness:** If a slug already exists, the service appends `-1`, `-2`, etc. until it finds a unique one.

**API endpoint:** There is a dedicated `GET /api/articles/slug/{slug}` endpoint so you can look up articles by slug without knowing their numeric ID.

**Validation:** Slugs must match `^[a-z0-9]+(?:-[a-z0-9]+)*$` — this enforces the lowercase-with-hyphens convention.

### Common Misunderstanding

> Slugs replace IDs.

Slugs are an *addition*, not a replacement. WikiProject keeps both the numeric `Id` (used internally, for foreign keys and API calls) and the `Slug` (used for friendly URLs and external links). Internal service calls use `Id`; publicly-visible URLs could use slugs.

> Slugs are always stable.

If the article title changes, a slug generated from the old title becomes stale. WikiProject allows updating the slug when updating an article, but it doesn't automatically change slugs when titles change. This is a deliberate tradeoff: changing a slug breaks existing links (bookmarks, search results, shared URLs).

### Why It Matters

Slugs make URLs readable and shareable. `GET /api/articles/slug/api-reference-overview` is more informative and easier to type than `/api/articles/3`. They also improve SEO (Search Engine Optimization) for public-facing sites.

---

## 20. Component

### Plain-English Definition

In React, a **component** is a reusable, self-contained piece of UI. Think of it like a LEGO brick: individual bricks (components) snap together to build a complete structure (the page). A component receives inputs (called props), manages its own state, and returns JSX (the HTML-like syntax React uses to describe the UI).

### What It Means in This Project

The frontend is composed of components across two directories:

**Shared (reusable) components** in `frontend/src/components/`:

| Component         | What It Does                                              |
|-------------------|-----------------------------------------------------------|
| `Header`          | Top navigation bar with links to all pages                |
| `SearchBar`       | Text input with search icon                               |
| `ArticleCard`     | Summary card for one article in the article list          |
| `FilterControls`  | Three dropdowns for category, tag, and status filtering   |
| `Pagination`      | Prev/Next controls with page count display                |
| `ArticleForm`     | Full create/edit form for an article                      |
| `StateDisplay`    | `LoadingSpinner`, `ErrorMessage`, `EmptyState` components |

**Page-level components** in `frontend/src/pages/`:

| Component            | Route                  |
|----------------------|------------------------|
| `HomePage`           | `/`                    |
| `ArticlesPage`       | `/articles`            |
| `ArticleDetailPage`  | `/articles/:id`        |
| `NewArticlePage`     | `/articles/new`        |
| `EditArticlePage`    | `/articles/:id/edit`   |

A page component composes smaller components together. `ArticlesPage` uses `SearchBar`, `FilterControls`, `ArticleCard`, `Pagination`, and the state components.

### Common Misunderstanding

> Components are like HTML elements.

HTML elements are predefined (e.g., `<div>`, `<button>`). React components are custom, user-defined elements that you create. Under the hood, React eventually renders them into actual HTML elements, but at the component level you're working with your own abstractions.

> Every file should be one component.

One file per component is the convention (and what WikiProject follows), but React doesn't enforce this. Small helper components that are only ever used by one parent are sometimes co-located in the same file.

### Why It Matters

Components are the fundamental building block of every React application. Understanding them — how they accept data, manage state, and render UI — is the foundation for understanding everything else in the frontend.

---

## 21. Props

### Plain-English Definition

**Props** (short for "properties") are the inputs to a React component. Just like a function can take parameters, a component takes props. Props flow *down* from parent to child — a parent component passes data to a child by setting prop values on the child's JSX element.

Props are **read-only** inside the component that receives them. A child cannot modify its own props.

### What It Means in This Project

A clear example from `ArticleCard.tsx`:

```tsx
// The component declares its props with a TypeScript interface
interface ArticleCardProps {
  article: ArticleSummary;
}

export default function ArticleCard({ article }: ArticleCardProps) {
  return (
    <div className="article-card">
      <h2><Link to={`/articles/${article.id}`}>{article.title}</Link></h2>
      <span className={statusClass(article.status)}>{article.status}</span>
      <p>{article.summary}</p>
      ...
    </div>
  );
}
```

The parent (`ArticlesPage`) uses it like this:

```tsx
{articles.map(article => (
  <ArticleCard key={article.id} article={article} />
))}
```

Another example — `SearchBar` takes a value and a change handler as props:

```tsx
<SearchBar
  value={searchInput}
  onChange={setSearchInput}
  placeholder="Search articles..."
/>
```

### Common Misunderstanding

> Props and state are the same thing.

Props are *external* data passed in from a parent. State is *internal* data managed by the component itself. A component can change its state; it cannot change its props. The parent controls props; the component controls state.

> All props must be primitive types.

Props can be any JavaScript value: strings, numbers, booleans, arrays, objects, and even functions (like event handlers). The `onChange` prop in `SearchBar` is a function — the parent passes in a callback that the child calls when the user types.

### Why It Matters

Props are how components communicate. Understanding the distinction between "what this component owns" (state) and "what it receives from outside" (props) is fundamental to reasoning about React data flow.

---

## 22. State

### Plain-English Definition

**State** is data that a component owns and manages, which can change over time and causes the component to re-render when it does. Unlike props (which come from outside), state is internal. When state changes, React automatically re-renders the component to reflect the new data.

State is created using the `useState` hook (or other state management solutions).

### What It Means in This Project

`ArticlesPage.tsx` manages several pieces of state:

```tsx
const [searchInput, setSearchInput] = useState('');         // current search text
const [debouncedSearch, setDebouncedSearch] = useState(''); // search after 300ms delay
const [selectedCategory, setSelectedCategory] = useState('');
const [selectedTag, setSelectedTag] = useState('');
const [selectedStatus, setSelectedStatus] = useState('');
const [page, setPage] = useState(1);
const [categories, setCategories] = useState<string[]>([]);
const [tags, setTags] = useState<string[]>([]);
```

Each `useState` call returns a pair: the current value and a function to update it. When you call `setSearchInput('wiki')`, React schedules a re-render with `searchInput` set to `'wiki'`.

Inside `ArticleForm.tsx`, the form fields are "controlled inputs" — their values come from state:

```tsx
const [title, setTitle] = useState(initial?.title ?? '');
// The input's value is tied to state; changes update the state
<input value={title} onChange={e => setTitle(e.target.value)} />
```

### Common Misunderstanding

> You can mutate state directly.

`setXxx(value)` is the only correct way to update state. Direct mutation (e.g., `searchInput = 'wiki'`) does not trigger a re-render, so the UI won't update.

> State updates are synchronous.

`useState` setters are asynchronous in React. If you call `setPage(page + 1)` and immediately read `page`, you'll get the old value. If you need the new value right away, compute it explicitly: `const newPage = page + 1; setPage(newPage);`.

### Why It Matters

State is what makes React UIs interactive and dynamic. Understanding when to use state, what to put in state, and how state updates trigger re-renders is essential for building a functional React application.

---

## 23. Hook

### Plain-English Definition

A **hook** is a special React function that lets you "hook into" React features — like state, lifecycle events, and context — from a function component. Hooks start with the word `use` (e.g., `useState`, `useEffect`, `useCallback`). You can also write *custom hooks* that combine built-in hooks to create reusable stateful logic.

Hooks were introduced in React 16.8 to replace class components for managing state and lifecycle.

### What It Means in This Project

**Built-in hooks used in WikiProject:**

- **`useState`** — manages local component state (see above)
- **`useEffect`** — runs side effects (like fetching data) after a render
- **`useCallback`** — memoizes a function so it isn't recreated on every render
- **`useMemo`** — memoizes a computed value
- **`useParams`** — from `react-router-dom`; reads URL parameters (e.g., `:id`)
- **`useNavigate`** — from `react-router-dom`; programmatically navigate to a route

**Custom hooks in `frontend/src/hooks/`:**

`useArticles.ts` encapsulates fetching a list of articles:

```tsx
export function useArticles(filters: ArticleFilters = {}): UseArticlesResult {
  const [data, setData] = useState<ArticleListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const filtersKey = useMemo(() => JSON.stringify(filters), [filters]);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const result = await articleService.list(filters);
      setData(result);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, [filtersKey]);

  useEffect(() => { fetch(); }, [fetch]);

  return { data, loading, error, refetch: fetch };
}
```

Now any component that needs article data can call `const { data, loading, error } = useArticles(filters)` instead of duplicating this fetch logic.

### Common Misunderstanding

> Hooks can be called conditionally.

Hooks must be called at the **top level** of a component or custom hook — never inside `if` statements, loops, or nested functions. React relies on the *order* of hook calls to associate state with the right component.

> Custom hooks are magical.

Custom hooks are just regular JavaScript functions that happen to call other hooks inside them. There's no special API for creating them — the `use` prefix is a convention that tells React (and tools like linters) to enforce the rules of hooks on that function.

### Why It Matters

Hooks are the primary way React components manage state and side effects. Understanding `useState` and `useEffect` is the minimum needed to work with any React codebase. Custom hooks like `useArticles` and `useArticle` are how WikiProject keeps page components clean and avoids duplicating API fetch logic.

---

## 24. Client-Side Routing

### Plain-English Definition

**Client-side routing** means that navigation between "pages" happens in the browser's JavaScript without making a full HTTP request to the server. The browser URL changes (using the History API), but the server is not involved — the JavaScript application intercepts the navigation and renders the appropriate component.

This is contrasted with **server-side routing**, where each navigation loads a brand new HTML page from the server.

### What It Means in This Project

WikiProject uses **React Router** (v7) for client-side routing. The routes are defined in `App.tsx`:

```tsx
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
```

When a user clicks a `<Link to="/articles">` component, React Router intercepts the click, updates the URL to `/articles`, and renders `<ArticlesPage />` — all without loading a new HTML page from the server.

URL parameters like `:id` are accessible in the component via the `useParams` hook:

```tsx
const { id } = useParams<{ id: string }>();
```

**Important caveat:** Because the server only serves one HTML file (`index.html`), if a user navigates directly to `http://localhost:5173/articles/5` (e.g., by refreshing the page), the server must still serve `index.html` for the app to work. Vite's dev server handles this automatically. Production deployments need a similar "fallback to index.html" configuration on the web server.

### Common Misunderstanding

> Client-side routing eliminates the need for a backend.

The routing logic (which component to show) moves to the browser, but the actual *data* still comes from the backend API. When `ArticleDetailPage` renders, it calls `GET /api/articles/5` to fetch the article content.

> Client-side and server-side routing can't coexist.

They do coexist in WikiProject. The React app handles all navigation between pages (client-side), but the ASP.NET Core API handles all data requests using its own routing (server-side, attribute-based routing).

### Why It Matters

Client-side routing is what makes React applications feel like native apps — navigation is instant because there's no server round-trip. It's also why you'll see a browser address bar update when you click around WikiProject, even though only one HTML page is ever loaded.

---

## Recap Table

| Term                     | Layer          | One-line summary                                                          |
|--------------------------|----------------|---------------------------------------------------------------------------|
| API                      | Both           | The contract defining how frontend and backend communicate                |
| REST                     | Backend        | A convention for designing HTTP APIs using verbs + resource URLs          |
| Endpoint                 | Backend        | A specific URL + verb combination that your API handles                   |
| CRUD                     | Both           | The four basic data operations: Create, Read, Update, Delete              |
| DTO                      | Backend        | A data container shaped for crossing the API boundary                     |
| Serialization            | Both           | Converting objects to/from JSON for transmission                          |
| Validation               | Both           | Checking that incoming data meets your rules before using it              |
| Entity                   | Backend        | A C# class that maps to a database table row via EF Core                  |
| Model                    | Both           | A generic term for a class representing a data concept                    |
| ORM                      | Backend        | Library that translates between C# objects and SQL database tables        |
| DbContext                | Backend        | EF Core's gateway to the database; manages connections and queries        |
| Migration                | Backend        | A versioned, code-generated change to the database schema                 |
| Seeding                  | Backend        | Inserting initial sample data into a fresh database                       |
| Service Layer            | Backend        | The layer containing business logic, separate from HTTP concerns          |
| Dependency Injection     | Backend        | Framework-managed wiring of class dependencies at runtime                 |
| Middleware               | Backend        | Code that runs on every request before/after your controller logic        |
| Routing (Backend)        | Backend        | Matching incoming URLs and verbs to the right controller method           |
| CORS                     | Backend        | Browser security mechanism for cross-origin API requests                  |
| Slug                     | Both           | A URL-safe, human-readable string identifier for an article               |
| Component                | Frontend       | A reusable, self-contained piece of React UI                              |
| Props                    | Frontend       | Inputs passed from a parent component to a child component                |
| State                    | Frontend       | Internal component data that triggers a re-render when changed            |
| Hook                     | Frontend       | A React function for accessing state and lifecycle from function components|
| Client-Side Routing      | Frontend       | Browser-based navigation between pages without full server reloads        |

---

## Where to Go Next

The following documentation sections (produced by parallel agents or in future consolidation passes) will expand on areas this glossary touches:

- **Backend Architecture** — deep dive into the controller → service → repository flow
- **Database Schema and Migrations** — entity relationships, migration lifecycle
- **Frontend Architecture** — component hierarchy, state management patterns
- **API Contract Reference** — full endpoint documentation with request/response shapes
- **Development Setup Guide** — how to run the project locally end-to-end
- **Testing Guide** — unit testing services with mocks, integration testing endpoints

**Recommended external reading:**

- [ASP.NET Core fundamentals (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/)
- [EF Core — Getting Started](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app)
- [React – Thinking in React](https://react.dev/learn/thinking-in-react) — the best introduction to components, props, and state
- [React Router – Tutorial](https://reactrouter.com/en/main/start/tutorial)
- [FluentValidation documentation](https://docs.fluentvalidation.net/en/latest/)
- [MDN – HTTP overview](https://developer.mozilla.org/en-US/docs/Web/HTTP/Overview)
- [MDN – CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
