# WikiProject — Project Overview

> **Who this is for:** A newer developer who is smart and motivated but needs a clear mental model of what this application is, how its pieces fit together, and why they were chosen before diving into any individual layer.
>
> **What this document covers:** The big picture — purpose, technologies, architecture, and the path data travels from a user's click to a database row and back. It deliberately does *not* deep-dive into backend implementation details, frontend component internals, or database schema design. Those are covered in the sibling documentation files listed at the end.

---

## Table of Contents

1. [What This Project Is](#1-what-this-project-is)
2. [What Problem It Solves](#2-what-problem-it-solves)
3. [Main Technologies and Their Responsibilities](#3-main-technologies-and-their-responsibilities)
4. [How a User Action Becomes Data on Screen](#4-how-a-user-action-becomes-data-on-screen)
5. [End-to-End Request Lifecycle](#5-end-to-end-request-lifecycle)
6. [Why This Architecture Is a Reasonable Starting Point](#6-why-this-architecture-is-a-reasonable-starting-point)
7. [What This App Does Not Yet Try to Solve](#7-what-this-app-does-not-yet-try-to-solve)
8. [Beginner Glossary](#8-beginner-glossary)
9. [What to Read Next](#9-what-to-read-next)

---

## 1. What This Project Is

WikiProject is an **internal knowledge base** — a web application that lets a team write, store, search, and manage articles (think: how-to guides, reference notes, onboarding documents, runbooks, and anything else a team needs to remember).

From a user's perspective, it looks and behaves like a stripped-down internal wiki:

- Browse a list of published articles with a search bar and category/tag/status filters
- Read full articles
- Create new articles with a title, summary, body, category, and tags
- Edit or delete existing articles
- Each article can be in one of three states: **Draft**, **Published**, or **Archived**

The application is made up of two distinct programs that run at the same time and talk to each other:

1. **Backend (API server):** A .NET 10 / ASP.NET Core application that runs on your machine (or a server) and is responsible for storing data, applying business rules, and serving data as JSON.
2. **Frontend (web app):** A React application that runs in the user's browser and is responsible for displaying data and collecting user input.

These two programs communicate over **HTTP**, the same protocol used by every website on the internet. The frontend sends requests to the backend; the backend responds with data; the frontend renders that data for the user.

---

## 2. What Problem It Solves

### The Problem

Software teams accumulate knowledge constantly: how to set up a development environment, how a tricky part of the system works, what decisions were made and why, how to handle common errors, what the on-call runbook says. This knowledge often lives in people's heads, in Slack threads, or in random text files. When someone leaves or forgets, the knowledge is lost.

### The Solution

A wiki-style knowledge base gives that knowledge a permanent, searchable, structured home. Articles can be tagged, categorized, and searched. Drafts let authors work privately before publishing. The archived status preserves history without cluttering the active view.

### Why Build It Instead of Using an Existing Tool?

This project is partly educational. It exists to demonstrate a modern, maintainable full-stack architecture using technologies that are standard in the industry (.NET + React + EF Core + SQLite). Building it from scratch lets developers understand every layer rather than relying on a black-box SaaS product. The `STARTING_TASKS.md` file in the repository root describes the next set of improvements planned for the application.

---

## 3. Main Technologies and Their Responsibilities

This section explains what each major technology is and exactly which job it does in this project. Think of these as four distinct layers, each with a clear responsibility.

---

### 3.1 React (Frontend — "What the user sees and interacts with")

**What React is:** React is a JavaScript library (by Meta/Facebook) for building user interfaces. Its core idea is that your UI is a function of your data: you describe *what* should appear given *this* state, and React figures out *how* to update the browser efficiently when the state changes.

**What React is responsible for in this project:**

- Rendering all the HTML the user sees (article cards, forms, navigation)
- Collecting user input (search queries, new article text, form submissions)
- Managing **state** (e.g., "what articles are currently loaded?", "is the form submitting?")
- Routing between pages without full page reloads
- Making HTTP requests to the backend API and displaying the results

**Technology details:**
- Version: React 19 with TypeScript
- Build tool: **Vite** (replaces the older Create React App; much faster)
- Routing: **React Router v7** (maps URL paths like `/articles/new` to specific page components)
- HTTP client: **Axios** (a library that makes HTTP requests easier than the browser's built-in `fetch`)
- Dev server: `http://localhost:5173`

**Key directory:** `frontend/src/`

**Example — how a page is declared in the router (`frontend/src/App.tsx`):**

```tsx
<Routes>
  <Route path="/"                    element={<HomePage />} />
  <Route path="/articles"            element={<ArticlesPage />} />
  <Route path="/articles/new"        element={<NewArticlePage />} />
  <Route path="/articles/:id"        element={<ArticleDetailPage />} />
  <Route path="/articles/:id/edit"   element={<EditArticlePage />} />
</Routes>
```

Each `<Route>` maps a URL pattern to a React component (a "page"). When the user navigates to `/articles/new`, React Router renders the `<NewArticlePage />` component without reloading the page.

**Why this matters:** React and the browser are entirely responsible for the user experience. The backend has no knowledge of what the user's screen looks like. All rendering decisions happen in JavaScript in the browser.

---

### 3.2 ASP.NET Core (.NET 10) (Backend — "The rules engine and data gateway")

**What ASP.NET Core is:** ASP.NET Core is Microsoft's open-source framework for building web servers and APIs in C#. It runs on **.NET** (a runtime and set of libraries), currently at version 10. An "API" (Application Programming Interface) in this context means a server that responds to HTTP requests with structured data (JSON) rather than HTML pages.

**What ASP.NET Core is responsible for in this project:**

- Receiving HTTP requests from the React frontend
- Validating incoming data (e.g., "is the title too long?")
- Applying business rules (e.g., "auto-generate a slug if none was provided", "enforce slug uniqueness")
- Querying and saving data through the ORM (described below)
- Returning structured JSON responses
- Exposing a Swagger UI for interactive API documentation

**Technology details:**
- Version: .NET 10 / ASP.NET Core
- Language: C# with nullable reference types enabled
- Dev server: `http://localhost:5018`
- API documentation: Swagger UI at `http://localhost:5018/swagger`
- Validation library: **FluentValidation** (expressive, testable validation rules)

**Key directory:** `src/WikiProject.Api/`

**Example — how an endpoint is declared (`src/WikiProject.Api/Controllers/ArticlesController.cs`):**

```csharp
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
```

This single method handles all `GET /api/articles` requests. It reads optional query parameters from the URL (e.g., `?search=authentication&page=2`), passes them to the service layer, and returns the result as JSON with HTTP status 200.

**Why this matters:** The backend is the only layer that enforces data integrity and business rules. The frontend is untrusted — any user could send arbitrary requests. All real validation must happen on the server.

#### 3.2.1 Service Layer

The project puts business logic in a separate **service layer** (`src/WikiProject.Api/Services/`), not directly in the controller. The `ArticleService` class handles slug generation, tag management, pagination, and search logic. The controller's only job is to translate HTTP requests into service calls and HTTP responses.

This separation means:
- Controllers stay thin and easy to read
- Business logic is easier to test without starting a web server
- The interface (`IArticleService`) makes it easy to swap the implementation (e.g., for a test fake)

#### 3.2.2 DTOs vs. Entities

The backend uses two distinct types of C# classes for data:

- **Entities** (`src/WikiProject.Api/Entities/`) — classes that map directly to database tables. They are EF Core's domain objects (e.g., `Article`, `Tag`, `ArticleTag`).
- **DTOs** (Data Transfer Objects, `src/WikiProject.Api/DTOs/`) — classes that represent what the API sends or receives over HTTP. For example, `ArticleDto` includes a flat list of tag names, while the `Article` entity stores them via a join table.

Keeping these separate means the database schema and the API contract can evolve independently. A change to the database table does not have to break the API shape, and vice versa.

---

### 3.3 Entity Framework Core (ORM — "The translator between C# and SQL")

**What an ORM is:** An **Object-Relational Mapper** (ORM) is a library that lets you work with database data using the same language and concepts as the rest of your code, instead of writing raw SQL strings. In .NET, the dominant ORM is **Entity Framework Core** (EF Core).

With EF Core you write:

```csharp
var articles = await _context.Articles
    .Include(a => a.ArticleTags)
    .ThenInclude(at => at.Tag)
    .Where(a => a.Status == ArticleStatus.Published)
    .ToListAsync();
```

EF Core translates that into SQL like:

```sql
SELECT a.*, t.*
FROM Articles a
LEFT JOIN ArticleTags at ON a.Id = at.ArticleId
LEFT JOIN Tags t ON at.TagId = t.Id
WHERE a.Status = 1;
```

You never have to write that SQL manually.

**What EF Core is responsible for in this project:**

- Translating C# LINQ queries into SQL queries that SQLite can execute
- Mapping results from database rows back into C# objects (hydration)
- Tracking which objects have changed so it knows what to `UPDATE`
- Running **migrations** — versioned scripts that create or alter database tables as the schema evolves

**Technology details:**
- Version: EF Core 10 (matches .NET 10)
- Database provider: `Microsoft.EntityFrameworkCore.Sqlite`
- Context class: `WikiDbContext` (`src/WikiProject.Api/Data/WikiDbContext.cs`)
- Migrations folder: `src/WikiProject.Api/Migrations/`

**The DbContext (`src/WikiProject.Api/Data/WikiDbContext.cs`):**

```csharp
public class WikiDbContext : DbContext
{
    public DbSet<Article>    Articles    => Set<Article>();
    public DbSet<Tag>        Tags        => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK for join table
        modelBuilder.Entity<ArticleTag>()
            .HasKey(at => new { at.ArticleId, at.TagId });

        // Unique index on slug
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.Slug).IsUnique();

        // Unique index on tag name
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name).IsUnique();
    }
}
```

`DbSet<T>` is EF Core's way of saying "there is a table for this entity, and you can query it." The `OnModelCreating` method lets you define constraints (like unique indexes) and relationships using a fluent C# API.

**Migrations:** When you change an entity class (add a field, change a type, add a relationship), you run `dotnet ef migrations add <MigrationName>` to generate a migration file. This file contains the `Up()` and `Down()` methods that tell EF Core exactly what SQL to run to apply or roll back the change. The project currently has one migration: `20260315235834_InitialCreate`.

On startup (`Program.cs`), the application automatically applies any pending migrations:

```csharp
db.Database.Migrate();
```

This means you can pull the latest code and the database will self-update without any manual SQL scripts.

**Why this matters:** Without an ORM, every database interaction would require carefully written SQL strings embedded in C# code. These are hard to read, easy to get wrong, and vulnerable to **SQL injection** (where user input can accidentally be interpreted as SQL commands). EF Core eliminates all of that.

#### Common Beginner Confusion: Entities vs. DTOs vs. ViewModels

| Type | Defined In | Used For |
|---|---|---|
| Entity (`Article`) | `Entities/` | Matches a database table; managed by EF Core |
| DTO (`ArticleDto`) | `DTOs/` | Shapes data going into/out of the API |
| TypeScript type (`Article`) | `frontend/src/types/` | Mirrors the DTO shape in the frontend |

All three often look similar but serve different purposes. The entity knows about database relationships (e.g., `ArticleTags` join collection). The DTO flattens those relationships for the API consumer (e.g., a plain `string[]` of tag names). The TypeScript type lets the frontend code be type-safe about what it receives.

---

### 3.4 SQLite (Database — "The persistent storage")

**What SQLite is:** SQLite is a **relational database** — data is stored in tables with rows and columns, and relationships between tables are expressed with foreign keys. Unlike most databases (PostgreSQL, MySQL, SQL Server), SQLite is **serverless**: there is no separate database server process. The entire database lives in a single file on disk (`wiki.db` in the `src/WikiProject.Api/` directory).

**What SQLite is responsible for in this project:**

- Persistently storing all articles, tags, and the relationships between them
- Enforcing constraints (unique slugs, unique tag names, foreign key integrity)
- Executing the SQL queries that EF Core generates

**Technology details:**
- Database file: `src/WikiProject.Api/wiki.db`
- Connection string (from `appsettings.json`): `Data Source=wiki.db`
- Provider: `Microsoft.EntityFrameworkCore.Sqlite`

**Tables created by the initial migration:**

| Table | Purpose |
|---|---|
| `Articles` | One row per article |
| `Tags` | One row per unique tag name |
| `ArticleTags` | Join table linking articles to tags (many-to-many) |

**Why SQLite for this project?**

SQLite is ideal for a local development project because it requires zero infrastructure: no installation, no server, no credentials. The entire database is a file you can copy, delete, and recreate instantly.

The tradeoff is that SQLite is not suitable for high-concurrency production workloads (many simultaneous writes). If the project were deployed to production with multiple users, switching to **PostgreSQL** would be a natural next step — and EF Core makes that switch almost trivially easy (change one package and one configuration line).

**Seeding the database:** On first startup, `SeedData.cs` (`src/WikiProject.Api/Data/SeedData.cs`) checks whether any articles exist and, if not, inserts six sample articles with tags. This lets a new developer see realistic data immediately without manually creating anything. To reset to a fresh state, delete `src/WikiProject.Api/wiki.db` and restart the backend.

---

### Technology Responsibility Summary

| Layer | Technology | Responsible For |
|---|---|---|
| **User Interface** | React 19 + TypeScript | Rendering, routing, state, user input, HTTP calls |
| **API Server** | ASP.NET Core (.NET 10) | HTTP endpoints, validation, business logic, responses |
| **ORM** | Entity Framework Core 10 | C#-to-SQL translation, migrations, change tracking |
| **Database** | SQLite | Persistent storage, constraints, query execution |

---

## 4. How a User Action Becomes Data on Screen

Before getting into the technical detail of every step, here is the plain-English story of what happens when a user visits the Articles page:

1. The user types `http://localhost:5173/articles` into their browser.
2. The browser downloads the React app (HTML + JavaScript) from the Vite dev server.
3. React renders the `ArticlesPage` component, which immediately fires an HTTP request to the backend: `GET http://localhost:5018/api/articles`.
4. The ASP.NET Core server receives the request, queries SQLite for a list of published articles (via EF Core), and returns a JSON payload.
5. React receives the JSON, updates its internal state, and re-renders the page to show the article cards.
6. The user sees a list of articles in their browser.

Now, suppose the user types "authentication" into the search bar:

1. After a short delay (debounce — the app waits for the user to stop typing before sending the request), the frontend fires a new HTTP request: `GET http://localhost:5018/api/articles?search=authentication`.
2. The backend applies a case-insensitive text search across the title, summary, content, category, and tag fields.
3. It returns a filtered, paginated JSON list.
4. React re-renders the article list with the new results.

This flow — user triggers event → frontend makes HTTP request → backend queries database → backend returns JSON → frontend re-renders — is the core loop of virtually every modern web application. Understanding it deeply will make the rest of the codebase readable.

---

## 5. End-to-End Request Lifecycle

Let's walk through the complete technical path of a single, realistic request: **creating a new article** (`POST /api/articles`).

### Step 1 — User fills in and submits the form (Frontend)

The user navigates to `/articles/new`. React Router renders `NewArticlePage`, which renders `ArticleForm`. The user types a title, summary, body, picks a category and some tags, selects "Published", and clicks Save.

The form's `onSubmit` handler calls `articleService.create(formData)` from `frontend/src/services/articleService.ts`:

```ts
async create(request: CreateArticleRequest): Promise<Article> {
  const { data } = await api.post<Article>('/api/articles', request);
  return data;
}
```

Axios serializes the JavaScript object to a JSON string and sends:

```http
POST http://localhost:5018/api/articles
Content-Type: application/json

{
  "title": "How to Use EF Core Migrations",
  "summary": "A concise guide to managing schema changes.",
  "content": "...",
  "category": "Development",
  "tags": ["database", "guide"],
  "status": 1
}
```

### Step 2 — HTTP crosses the network boundary

The request travels from the browser to the ASP.NET Core web server. In development, these are two processes on the same machine. Vite's dev proxy is not involved for `POST` (the request goes directly to port 5018).

**CORS check:** Because the React app at `localhost:5173` is calling an API at `localhost:5018` (a different port = a different **origin**), the browser performs a **CORS** (Cross-Origin Resource Sharing) check. ASP.NET Core's CORS middleware (configured in `Program.cs`) explicitly allows requests from `http://localhost:5173`, so the request is permitted.

### Step 3 — ASP.NET Core receives and routes the request

The ASP.NET Core **middleware pipeline** processes the request. After CORS headers are verified, **routing** determines which controller action should handle it. Because the URL is `POST /api/articles`, routing selects `ArticlesController.Create`.

ASP.NET Core's **model binding** automatically deserializes the JSON body into a `CreateArticleRequest` C# record.

### Step 4 — Validation runs (FluentValidation)

The controller immediately runs the injected `IValidator<CreateArticleRequest>`:

```csharp
var validation = await validator.ValidateAsync(request);
if (!validation.IsValid)
    return ValidationProblem(new ValidationProblemDetails(
        validation.ToDictionary()));
```

`CreateArticleRequestValidator` checks (among other things) that:
- Title is present and ≤ 200 characters
- Summary is present and ≤ 500 characters
- Content is present
- Category is present and ≤ 100 characters
- Each tag is ≤ 50 characters
- If a slug was provided, it matches `^[a-z0-9]+(?:-[a-z0-9]+)*$`

If any rule fails, the controller returns `400 Bad Request` with a structured list of errors immediately, **without touching the database at all**. This is a key security and data-quality boundary.

### Step 5 — Business logic runs (Service Layer)

If validation passes, the controller calls `_articleService.CreateAsync(request)`.

`ArticleService.CreateAsync` handles the real work:

1. **Slug generation:** If no slug was provided, it converts the title to a URL-safe slug (e.g., `"How to Use EF Core Migrations"` → `"how-to-use-ef-core-migrations"`). It then checks the database for collisions and appends a number suffix if needed.
2. **Tag resolution:** For each tag name in the request, the service checks whether a `Tag` row already exists in the database. If yes, it reuses it. If no, it creates a new `Tag` row. This prevents duplicate tag rows for the same name.
3. **Entity construction:** It builds a new `Article` entity with `CreatedAt` and `UpdatedAt` set to the current UTC time and the resolved `ArticleTag` join rows attached.

### Step 6 — EF Core persists the data (ORM → Database)

The service calls:

```csharp
_context.Articles.Add(article);
await _context.SaveChangesAsync();
```

EF Core inspects the change tracker, generates the appropriate SQL, and executes it against SQLite. For a new article with two new tags, this might be three `INSERT` statements (one for the `Article`, one for each `Tag`) and two more for the `ArticleTag` join rows — all wrapped in a single database transaction.

### Step 7 — Mapping and response

After saving, the service maps the `Article` entity (with its navigation properties loaded) to an `ArticleDto` using the helpers in `src/WikiProject.Api/Mappings/ArticleMappings.cs`. This flattens the EF Core navigation structure into a clean JSON-friendly shape.

The controller returns:

```csharp
return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
```

This sends HTTP status **201 Created** with:
- A `Location` header pointing to the new article's URL (e.g., `http://localhost:5018/api/articles/7`)
- The full `ArticleDto` as the JSON body

### Step 8 — Frontend receives the response and navigates

Axios receives the 201 response and returns the parsed `Article` object to the calling code in the page component. The page component navigates the user to the new article's detail page (`/articles/7`), where another `GET` request fetches and displays the article they just created.

---

### Lifecycle Summary (visual)

```
Browser (React)                  Network              Server (ASP.NET Core)            Database (SQLite)
─────────────────────────────────────────────────────────────────────────────────────────────────────
User fills form
articleService.create() ──────── POST /api/articles ──►  Middleware pipeline
                                                          CORS check
                                                          Route to ArticlesController.Create
                                                          Model bind JSON → CreateArticleRequest
                                                          FluentValidation.ValidateAsync()
                                                          ArticleService.CreateAsync()
                                                            slug generation
                                                            tag resolution ──────────────► SELECT Tags WHERE Name IN (...)
                                                            build Article entity
                                                            context.Articles.Add(article)
                                                            SaveChangesAsync() ──────────► BEGIN TRANSACTION
                                                                                            INSERT INTO Articles ...
                                                                                            INSERT INTO Tags ...
                                                                                            INSERT INTO ArticleTags ...
                                                                                            COMMIT
                                                          Map Entity → ArticleDto
                         ◄──────── 201 Created + JSON ──  return CreatedAtAction(...)
React receives ArticleDto
Navigate to /articles/:id
```

---

## 6. Why This Architecture Is a Reasonable Starting Point

### Separation of Concerns

Each layer has exactly one job. The frontend does not know about SQL. The backend does not know about CSS. The ORM does not know about HTTP. This separation means:
- A bug in the UI is unlikely to corrupt data (the server validates everything)
- The database schema can change without rewriting the entire frontend
- You can build and test each layer independently

### JSON API as the Contract

The backend exposes a **REST API** over JSON. This is deliberately technology-neutral: any client — a browser, a mobile app, a command-line tool — can consume the same API. The React frontend is just one possible client. The `.http` file at `src/WikiProject.Api/WikiProject.Api.http` contains raw HTTP examples you can run directly without any frontend.

### Code-First Migrations

Using EF Core migrations means the database schema is **version-controlled in C# code**. Every team member runs the same migrations in the same order, so everyone's database is identical. There are no "my DB has this table but yours doesn't" mysteries.

### Dependency Injection

All three "heavyweight" objects in ASP.NET Core — the `WikiDbContext`, the `ArticleService`, and the `FluentValidation` validators — are registered with the **dependency injection (DI) container** in `Program.cs`:

```csharp
builder.Services.AddDbContext<WikiDbContext>(...);
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IValidator<CreateArticleRequest>, CreateArticleRequestValidator>();
```

This means:
- You never write `new ArticleService(new WikiDbContext(...))` manually
- The framework creates and disposes these objects for you, managing their lifetimes correctly
- You can swap `ArticleService` for a fake in tests without changing the controller

### TypeScript End-to-End

The frontend uses TypeScript, not plain JavaScript. The `Article`, `ArticleListResponse`, `CreateArticleRequest`, and other types in `frontend/src/types/index.ts` mirror the DTOs defined in the backend. This gives the frontend compile-time safety: if you try to access a field that doesn't exist on an article, TypeScript tells you at build time rather than at runtime.

### Why Not a Monolith or a Single Page?

An alternative design would be a **server-side rendered** application where ASP.NET Core renders HTML directly (using Razor Pages or MVC views). That is simpler to deploy but harder to build highly interactive UIs with. The separate React frontend + JSON API approach is more complex to set up, but it is the dominant pattern in modern web development and the one you will encounter most in industry.

---

## 7. What This App Does Not Yet Try to Solve

Being honest about what is *not* there helps avoid confusion when you don't find certain features.

### Authentication and Authorization

There is no login, no user accounts, and no access control. Anyone who can reach the API can create, edit, or delete any article. The `Program.cs` file even has a comment marking where `UseAuthentication()` and `UseAuthorization()` would go when this is added. Until then, this app is appropriate only for trusted internal networks.

### Rich Text / Markdown Rendering

Articles are stored and displayed as plain text. The content field accepts Markdown syntax, but nothing currently parses or renders it. The README lists `react-markdown` as a planned addition.

### Full-Text Search Performance

The current search implementation uses EF Core's `LIKE` queries (roughly `WHERE Content LIKE '%searchterm%'`). This works fine for small datasets but does not scale to thousands of articles. SQLite has a dedicated **Full-Text Search (FTS5)** extension that would dramatically improve search performance. It is listed as a future improvement.

### Optimistic Concurrency / Conflict Resolution

If two users edit the same article simultaneously, the last save wins. There is no detection of conflicting edits.

### Audit Trail / Article History

The `UpdatedAt` timestamp tells you when an article last changed, but not what changed or who changed it. No versioning or diff history is stored.

### Real-Time Updates

If someone else publishes a new article while you are viewing the list, your browser does not automatically update. You must refresh. WebSockets or Server-Sent Events would be needed for live updates.

### Production Deployment

There is no Docker setup, no CI/CD pipeline, and no production configuration. The CORS settings and SQLite database are development-only conveniences. Deploying this to a real server would require additional work.

---

## 8. Beginner Glossary

These terms appear throughout the codebase and the rest of the documentation. Read this once and refer back as needed.

| Term | Plain-English Definition |
|---|---|
| **API** | Application Programming Interface. In this context: a web server that responds to HTTP requests with JSON data, not HTML pages. Think of it as the backend's "menu" of operations the frontend can call. |
| **ASP.NET Core** | Microsoft's open-source framework for building web servers in C#. It handles HTTP routing, middleware, dependency injection, and request/response processing. |
| **Axios** | A JavaScript library that makes HTTP requests easier. The frontend uses it to call the backend API. |
| **CORS** | Cross-Origin Resource Sharing. A browser security rule that prevents one website from making HTTP requests to a different domain/port without explicit permission. The backend explicitly allows requests from `localhost:5173`. |
| **Controller** | An ASP.NET Core class whose methods correspond to HTTP endpoints. `ArticlesController` has methods for GET, POST, PUT, DELETE on `/api/articles`. |
| **DbContext** | EF Core's main class. It represents a session with the database: you query through it, and you call `SaveChangesAsync()` on it to persist changes. `WikiDbContext` is this project's DbContext. |
| **DbSet\<T\>** | A property on the DbContext representing one database table. `context.Articles` lets you query and add `Article` rows. |
| **Dependency Injection (DI)** | A pattern where objects don't create their own dependencies; instead, a framework (the "DI container") creates and provides them. In ASP.NET Core, services are registered in `Program.cs` and injected automatically into constructors. |
| **DTO** | Data Transfer Object. A simple class or record used to represent data going into or out of the API. It is not a database entity and is not tracked by EF Core. |
| **EF Core** | Entity Framework Core. The ORM (see below) used by this project to communicate with SQLite. |
| **Entity** | A C# class that maps to a database table. EF Core tracks changes to entity instances and can generate SQL from them. In this project: `Article`, `Tag`, `ArticleTag`. |
| **FluentValidation** | A .NET library for writing validation rules as C# code rather than data annotations. Used to validate `CreateArticleRequest` and `UpdateArticleRequest` before saving. |
| **HTTP** | HyperText Transfer Protocol. The request/response protocol used by all web communication. Requests have a **method** (GET, POST, PUT, DELETE), a **URL**, optional **headers**, and an optional **body**. |
| **JSON** | JavaScript Object Notation. A text format for representing structured data. It is the lingua franca of modern web APIs. `{"title": "Hello", "status": 1}` is JSON. |
| **Migration** | A versioned, code-generated file that describes how to change the database schema (add a table, add a column, etc.). EF Core generates and applies migrations. |
| **Model Binding** | ASP.NET Core's automatic process of converting incoming HTTP request data (URL parameters, query strings, JSON bodies) into C# objects. |
| **ORM** | Object-Relational Mapper. A library that lets you use your programming language's objects to interact with a relational database, instead of writing raw SQL. EF Core is the ORM here. |
| **Pagination** | Breaking a large list of results into pages. `GET /api/articles?page=2&pageSize=20` fetches the second page of 20 articles. The `ArticleListResponse` includes `TotalCount`, `TotalPages`, etc. |
| **React** | A JavaScript library for building user interfaces. UI is described as components (functions that return JSX). When state changes, React efficiently updates only the parts of the page that need to change. |
| **React Router** | A library that maps URL paths to React components, enabling navigation within a single-page application without full page reloads. |
| **REST** | Representational State Transfer. An architectural style for APIs where resources (articles, tags) have URLs, and standard HTTP methods (GET, POST, PUT, DELETE) perform standard operations on them. |
| **Scoped Lifetime** | In ASP.NET Core DI, "scoped" means one instance is created per HTTP request and disposed when the request ends. Both `WikiDbContext` and `ArticleService` are scoped. |
| **Service Layer** | A C# class (`ArticleService`) that contains business logic. Controllers delegate to it. This keeps controllers thin and logic testable in isolation. |
| **Slug** | A URL-friendly version of a title: lowercase, words joined by hyphens, no special characters. `"How to Use EF Core"` → `"how-to-use-ef-core"`. Used in article URLs. |
| **SQLite** | A lightweight, serverless relational database stored as a single file. No installation or server process needed. Suitable for development; can be replaced with PostgreSQL for production. |
| **State (React)** | Data that a React component "remembers" between renders. When state changes, React re-renders the component. Examples: the current list of articles, whether the form is loading. |
| **TypeScript** | A superset of JavaScript that adds static type checking. Catches type errors at compile time. The frontend is written entirely in TypeScript. |
| **Vite** | A modern frontend build tool and development server. Replaces the older Create React App. Very fast hot-module replacement during development. |

---

## 9. What to Read Next

Once you have this big picture in mind, the recommended reading order is:

| Document | What It Covers |
|---|---|
| `docs/01_BACKEND_ARCHITECTURE.md` *(future)* | Deep dive into the ASP.NET Core backend: controllers, service layer, validation, dependency injection, middleware pipeline, startup configuration |
| `docs/02_DATABASE_AND_ORM.md` *(future)* | Deep dive into EF Core: entities, migrations, relationships, querying, change tracking, performance considerations |
| `docs/03_FRONTEND_ARCHITECTURE.md` *(future)* | Deep dive into the React frontend: component hierarchy, page components, state management, service layer, routing, TypeScript types |
| `docs/04_API_CONTRACT.md` *(future)* | Detailed documentation of every API endpoint, request/response shapes, validation rules, and HTTP status codes |
| `docs/05_DATA_SEEDING_AND_MIGRATIONS.md` *(future)* | How to manage database changes over time: creating migrations, rolling them back, re-seeding data |

**External resources worth reading now:**

- [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/) — Microsoft's official docs; the "Tutorial: Create a web API" is an excellent starting point
- [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/) — especially "Getting Started" and "Modeling your database"
- [React documentation](https://react.dev/) — the new official docs are excellent; start with "Quick Start" and "Thinking in React"
- [React Router documentation](https://reactrouter.com/en/main) — focus on the "Tutorial" section for v6/v7
- [Vite documentation](https://vitejs.dev/guide/) — explains what Vite does and how to configure it
- [FluentValidation documentation](https://docs.fluentvalidation.net/) — for understanding how validation rules are written
- [SQLite documentation](https://www.sqlite.org/docs.html) — particularly "When to Use SQLite" which honestly describes its tradeoffs

---

*This document covers the "big picture" only. It deliberately omits the internal implementation details of each layer so that you can build a clear mental model before diving into specifics. Once you understand the lifecycle described in Section 5, every other piece of the codebase will make more sense.*
