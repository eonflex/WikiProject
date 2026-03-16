# API Reference with Explanations

> **Audience:** Developers who are new to this project and want to understand not just _what_ the API does, but _why_ it is built this way and how to work with it confidently.
>
> **Related docs:** A backend architecture guide (covering EF Core, the service layer, and dependency injection) and a frontend guide (covering React components and custom hooks) are expected in separate documentation files produced by parallel agents. This document focuses solely on the API surface — the HTTP contract between client and server.

---

## Table of Contents

1. [What Is an API and Why Does This Project Have One?](#1-what-is-an-api-and-why-does-this-project-have-one)
2. [API Design Conventions](#2-api-design-conventions)
3. [How to Run and Test the API Locally](#3-how-to-run-and-test-the-api-locally)
4. [Endpoint Overview](#4-endpoint-overview)
5. [Articles Endpoints](#5-articles-endpoints)
   - [GET /api/articles — List Articles](#get-apiarticles--list-articles)
   - [GET /api/articles/{id} — Get Article by ID](#get-apiarticlesid--get-article-by-id)
   - [GET /api/articles/slug/{slug} — Get Article by Slug](#get-apiarticlesslugslug--get-article-by-slug)
   - [POST /api/articles — Create Article](#post-apiarticles--create-article)
   - [PUT /api/articles/{id} — Update Article](#put-apiarticlesid--update-article)
   - [DELETE /api/articles/{id} — Delete Article](#delete-apiarticlesid--delete-article)
6. [Metadata Endpoints](#6-metadata-endpoints)
   - [GET /api/categories — List Categories](#get-apicategories--list-categories)
   - [GET /api/tags — List Tags](#get-apitags--list-tags)
7. [Request and Response Shapes (DTOs)](#7-request-and-response-shapes-dtos)
8. [Validation Rules](#8-validation-rules)
9. [HTTP Status Codes Used in This API](#9-http-status-codes-used-in-this-api)
10. [How the Frontend Consumes the API](#10-how-the-frontend-consumes-the-api)
11. [Common Beginner Misconceptions About APIs](#11-common-beginner-misconceptions-about-apis)
12. [Security: Current State and Future Work](#12-security-current-state-and-future-work)
13. [Section Recap](#13-section-recap)

---

## 1. What Is an API and Why Does This Project Have One?

An **API** (Application Programming Interface) is a formal agreement between two pieces of software about how they communicate. In this project, the API is an HTTP-based service — a running web server that listens for requests shaped as URLs and JSON bodies, and replies with JSON data or status codes.

### Why not just use one application?

Many small projects combine frontend and backend in one codebase (a "monolith"). WikiProject deliberately separates them:

- **The backend** (`src/WikiProject.Api`) is a .NET 10 ASP.NET Core application. It handles database reads and writes, business rules, and data validation.
- **The frontend** (`frontend/`) is a React + TypeScript application. It handles what the user sees and interacts with in the browser.

These two applications communicate exclusively through the HTTP API. The frontend does not touch the database directly — it always asks the backend to do it.

**Why this matters:** This separation means the frontend and backend can be changed, deployed, or even replaced independently. A mobile app could call the same API. A different frontend framework could be swapped in. The business logic lives in one place.

### Why REST?

REST (Representational State Transfer) is an architectural style for designing APIs over HTTP. It is not a protocol or a specification — it is a set of conventions. REST is widely used because:

- It uses standard HTTP verbs (`GET`, `POST`, `PUT`, `DELETE`) that developers already understand
- It models data as "resources" accessed by URLs (e.g., `/api/articles/5`)
- It is stateless — each request contains all information needed; no session memory is required on the server
- It works natively with browsers, curl, Swagger, Postman, and any HTTP library

This project follows REST conventions closely but is not "pure" REST (few production systems are). The practical benefits of REST-style design are the goal, not strict academic compliance.

---

## 2. API Design Conventions

Before diving into individual endpoints, understand the patterns this API uses everywhere:

### 2.1 URL Structure

All API routes start with `/api/`. This makes it easy to distinguish API traffic from frontend assets in the browser's network tab and to proxy only API requests from the Vite dev server.

```
http://localhost:5018/api/articles
http://localhost:5018/api/articles/3
http://localhost:5018/api/articles/slug/welcome-to-wikiproject
http://localhost:5018/api/categories
http://localhost:5018/api/tags
```

Resources are **plural nouns** (`articles`, `categories`, `tags`), not verbs. This is a REST convention — the HTTP method (`GET`, `POST`, etc.) expresses the action, not the URL.

> **Beginner confusion:** Many beginners write URLs like `/api/getArticle` or `/api/deleteArticle`. This mixes verb logic into the URL, which REST avoids. Instead, use `GET /api/articles/{id}` and `DELETE /api/articles/{id}`.

### 2.2 HTTP Methods (Verbs)

| Verb     | Meaning                    | Example in this API                    |
|----------|----------------------------|----------------------------------------|
| `GET`    | Read data, no side effects | `GET /api/articles` — list articles    |
| `POST`   | Create a new resource      | `POST /api/articles` — new article     |
| `PUT`    | Replace an existing resource fully | `PUT /api/articles/3` — update article |
| `DELETE` | Remove a resource          | `DELETE /api/articles/3`               |

`PATCH` (partial update) is not used. The API uses `PUT` for updates, which means the client always sends the full article body even when changing only one field.

> **Why PUT instead of PATCH?** PATCH is semantically "send only what changed." PUT is "send the whole new state." PUT is simpler to implement and reason about for this use case. The tradeoff is that the client must always fetch the article first, then re-send the full body on save. Future improvements could introduce PATCH support.

### 2.3 JSON Everywhere

Both request bodies and response bodies use JSON (`application/json`). The `[Produces("application/json")]` attribute on the controllers enforces this in ASP.NET Core.

When sending a request body (`POST`, `PUT`), the client must set the `Content-Type: application/json` header. Axios does this automatically in the frontend service.

### 2.4 Camel Case JSON Keys

.NET models use PascalCase (`ArticleId`, `CreatedAt`). By default, ASP.NET Core serializes JSON with camelCase keys (`articleId`, `createdAt`) to match JavaScript conventions. This is the default behavior of `System.Text.Json` in .NET — no extra configuration is needed.

### 2.5 Consistent Error Shapes

Validation errors return the **RFC 7807 Problem Details** format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required."],
    "Summary": ["Summary must be 500 characters or fewer."]
  }
}
```

This is a standardized JSON error format. The `errors` object is a dictionary where each key is a field name and each value is an array of error messages for that field.

404 errors return an empty body with just the status code. This is a deliberate simplicity choice — the caller already knows which ID they requested.

### 2.6 Pagination

List endpoints return paginated results rather than all records at once. This protects the server from returning thousands of rows in one response and keeps the frontend responsive.

Pagination in this API uses **offset-based pagination** (also called "page number" pagination):
- `page=1` returns records 1–20
- `page=2` returns records 21–40
- `pageSize` controls how many items appear per page (1–100)

> **Alternative: Cursor-based pagination** uses an opaque cursor token instead of a page number. It is more efficient on large datasets and handles insertions during pagination better, but is harder to implement and expose to users. For a wiki application with hundreds or low thousands of articles, offset pagination is perfectly adequate.

### 2.7 Sorting

The list endpoint always returns articles sorted by `updatedAt` descending (most recently updated first). There is no client-controlled sort order in the current API. This is a pragmatic simplicity choice — for a wiki, showing recently-touched articles first is almost always what users want.

---

## 3. How to Run and Test the API Locally

### 3.1 Starting the Backend

```bash
cd src/WikiProject.Api
dotnet run
```

The API starts on `http://localhost:5018`. On first run, it automatically:
1. Creates the SQLite database file (`wiki.db`) using EF Core migrations
2. Seeds 6 sample articles via `SeedData.SeedAsync()`

### 3.2 Swagger UI (Recommended for Exploration)

The API ships with **Swagger** (also called OpenAPI or Swashbuckle). Swagger is an interactive web UI that automatically documents and lets you test all endpoints in the browser.

Open: **http://localhost:5018/swagger**

You can:
- See all endpoints grouped by controller
- Expand each endpoint to read its parameters and response schemas
- Click **"Try it out"** → fill in parameters → **"Execute"** to make a real HTTP request
- See the exact `curl` command that was sent and the response received

Swagger is only enabled in the development environment (`if (app.Environment.IsDevelopment())`). It will not appear in a production build.

### 3.3 curl Examples

`curl` is a command-line tool for making HTTP requests. It is available on macOS, Linux, and Windows (via WSL or Git Bash).

**List all articles (first page):**
```bash
curl http://localhost:5018/api/articles
```

**List articles filtered by category:**
```bash
curl "http://localhost:5018/api/articles?category=Development&page=1&pageSize=5"
```

**Get a specific article by ID:**
```bash
curl http://localhost:5018/api/articles/1
```

**Get a specific article by slug:**
```bash
curl http://localhost:5018/api/articles/slug/welcome-to-wikiproject
```

**Create a new article:**
```bash
curl -X POST http://localhost:5018/api/articles \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My First Article",
    "summary": "A quick overview of the topic.",
    "content": "Full content goes here.",
    "category": "Development",
    "tags": ["beginner", "guide"],
    "status": 1
  }'
```

**Update an article:**
```bash
curl -X PUT http://localhost:5018/api/articles/1 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated Title",
    "summary": "Updated summary.",
    "content": "Updated content.",
    "category": "General",
    "tags": ["updated"],
    "status": 1
  }'
```

**Delete an article:**
```bash
curl -X DELETE http://localhost:5018/api/articles/1
```

**List all categories:**
```bash
curl http://localhost:5018/api/categories
```

**List all tags:**
```bash
curl http://localhost:5018/api/tags
```

### 3.4 The `.http` File

The project includes `src/WikiProject.Api/WikiProject.Api.http`. This is a Visual Studio / VS Code REST Client format file for sending HTTP requests directly from the editor. At the time of writing, it contains only a placeholder request for `/weatherforecast` (a default template endpoint that has been removed). You can update it with the real endpoints listed above to build a convenient local test suite.

### 3.5 Starting the Frontend (Proxy Setup)

The Vite dev server is configured to proxy all `/api` requests to the backend:

```ts
// frontend/vite.config.ts
proxy: {
  '/api': {
    target: 'http://localhost:5018',
    changeOrigin: true,
  },
},
```

This means that when the frontend calls `/api/articles`, Vite forwards the request to `http://localhost:5018/api/articles`. You don't need CORS headers in this case because the browser thinks it is talking to `localhost:5173` (the same origin as the frontend).

Start the frontend:
```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173** to see the application running.

---

## 4. Endpoint Overview

| # | Method | Route | Purpose | Auth |
|---|--------|-------|---------|------|
| 1 | `GET` | `/api/articles` | List articles (paginated, searchable, filterable) | None |
| 2 | `GET` | `/api/articles/{id}` | Get one article by numeric ID | None |
| 3 | `GET` | `/api/articles/slug/{slug}` | Get one article by URL slug | None |
| 4 | `POST` | `/api/articles` | Create a new article | None |
| 5 | `PUT` | `/api/articles/{id}` | Update an existing article | None |
| 6 | `DELETE` | `/api/articles/{id}` | Delete an article | None |
| 7 | `GET` | `/api/categories` | List all distinct categories | None |
| 8 | `GET` | `/api/tags` | List all tag names | None |

All endpoints are currently public (no authentication required). See [Section 12](#12-security-current-state-and-future-work) for future auth plans.

---

## 5. Articles Endpoints

---

### GET /api/articles — List Articles

#### Purpose

Retrieves a paginated list of article summaries. Supports full-text search across multiple fields, filtering by category, tag, and publication status, and paging through large result sets.

This is the most-used endpoint in the application. Every time the Articles page loads or the user changes a search query or filter, this endpoint is called.

#### Route Details

```
GET /api/articles
```

All parameters are **query string parameters** (appended to the URL after a `?`).

#### Query Parameters

| Parameter  | Type            | Default | Constraints | Description |
|------------|-----------------|---------|-------------|-------------|
| `search`   | `string`        | _none_  | —           | Full-text search across title, summary, content, category, and tag names |
| `category` | `string`        | _none_  | Case-insensitive exact match | Filter by category name |
| `tag`      | `string`        | _none_  | Case-insensitive exact match | Filter by tag name |
| `status`   | `ArticleStatus` | _none_  | `0`, `1`, or `2` | Filter by status enum value |
| `page`     | `int`           | `1`     | Minimum `1` (enforced server-side) | Page number (1-indexed) |
| `pageSize` | `int`           | `20`    | Clamped to 1–100 server-side | Results per page |

**About the `status` parameter:** The `status` filter accepts the numeric enum value (`0` for Draft, `1` for Published, `2` for Archived), not the string label. This is because `ArticleStatus` is a C# enum and ASP.NET Core binds it from the query string as an integer.

> **Beginner confusion:** You might expect to pass `status=Published` as a string. This will fail because ASP.NET Core cannot bind the string `"Published"` to the `ArticleStatus?` enum parameter. Pass `status=1` instead.

#### Example Request

```
GET /api/articles?search=database&category=Development&page=1&pageSize=10
```

#### Success Response: 200 OK

```json
{
  "items": [
    {
      "id": 2,
      "title": "Database Design Best Practices",
      "slug": "database-design-best-practices",
      "summary": "A guide to relational schema design...",
      "category": "Development",
      "tags": ["database", "sql"],
      "status": "Published",
      "createdAt": "2025-03-06T00:00:00Z",
      "updatedAt": "2025-03-10T14:22:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

Note that each item is an **ArticleSummaryDto** — it does not include the `content` field. This is a deliberate design choice: the list endpoint returns lightweight summaries to avoid transferring large article bodies when the user only needs to browse titles.

#### Failure Cases

| Scenario | Status | Notes |
|----------|--------|-------|
| Invalid `page` value | `200` with page clamped to `1` | Server applies `Math.Max(query.Page, 1)` |
| Invalid `pageSize` value | `200` with pageSize clamped | Server applies `Math.Clamp(query.PageSize, 1, 100)` |
| No matching articles | `200` with empty `items` array | Not a 404; an empty result is a valid response |
| Server error | `500` | Unhandled database or application error |

#### How the Frontend Uses This

The `ArticlesPage` component calls this endpoint whenever the user types in the search box, changes a filter dropdown, or navigates to a different page. The custom hook `useArticles` manages the loading and error state:

```ts
// frontend/src/hooks/useArticles.ts (simplified)
const { data, loading, error } = useArticles({ search, category, tag, page });
```

The `ArticlesPage` also calls `GET /api/categories` and `GET /api/tags` once on load to populate the filter dropdowns.

The `HomePage` uses the same endpoint with no filters (fetching the most recently updated articles) to show a "recent articles" section on the landing page.

#### Why It Exists in This Form

Combining search, filter, and pagination into a single endpoint is more efficient than separate endpoints for each concern. The frontend makes one request per user interaction, and the backend assembles the SQL query dynamically. This is a common pattern called **dynamic query composition**.

The backend builds the LINQ query incrementally:

```csharp
var q = _db.Articles.Include(...).AsNoTracking().AsQueryable();

if (!string.IsNullOrWhiteSpace(query.Search))
    q = q.Where(a => a.Title.ToLower().Contains(search) || ...);

if (!string.IsNullOrWhiteSpace(query.Category))
    q = q.Where(a => a.Category.ToLower() == query.Category.ToLower());

// ... more filters
var totalCount = await q.CountAsync();
var items = await q.OrderByDescending(a => a.UpdatedAt).Skip(...).Take(...).ToListAsync();
```

First it counts (for pagination math), then it fetches just the current page.

#### Future Improvements

- Support sorting by fields other than `updatedAt` (e.g., `createdAt`, `title`)
- Support searching by multiple categories or tags at once
- Support cursor-based pagination for better performance at scale
- Return string-based status values (e.g., `status=Published`) via a custom model binder
- Add response caching headers so browsers don't re-fetch unchanged lists

---

### GET /api/articles/{id} — Get Article by ID

#### Purpose

Retrieves the full content of one article by its numeric database ID. This is used when a user opens an article detail page or when the edit form needs to pre-populate with existing data.

#### Route Details

```
GET /api/articles/{id}
```

#### Path Parameters

| Parameter | Type  | Required | Description |
|-----------|-------|----------|-------------|
| `id`      | `int` | ✓        | Numeric primary key of the article |

The route constraint `{id:int}` in ASP.NET Core ensures that only integer values match this route. If you pass `/api/articles/abc`, ASP.NET Core will return `404` because `abc` does not match the `int` constraint.

#### Example Request

```
GET /api/articles/1
```

#### Success Response: 200 OK

Unlike the list endpoint, this returns a full **ArticleDto** including the `content` field:

```json
{
  "id": 1,
  "title": "Welcome to WikiProject",
  "slug": "welcome-to-wikiproject",
  "summary": "An introduction to the team knowledge base.",
  "content": "# Welcome to WikiProject\n\nWikiProject is your team's internal...",
  "category": "General",
  "tags": ["getting-started", "guide"],
  "status": "Published",
  "createdAt": "2025-03-06T00:00:00Z",
  "updatedAt": "2025-03-06T00:00:00Z"
}
```

The `content` field contains the raw Markdown text. The frontend is responsible for rendering it as formatted HTML (using a Markdown rendering library).

Notice that `status` is returned as a **string** (`"Published"`) rather than an integer (`1`). The mapping layer calls `.ToString()` on the `ArticleStatus` enum. This is friendlier for frontend consumers — they can display or compare status values without needing to know the numeric mapping.

#### Failure Cases

| Scenario | Status | Body |
|----------|--------|------|
| Article not found | `404 Not Found` | Empty body |
| `id` is not an integer | `404 Not Found` | Route constraint rejects the request |

#### How the Frontend Uses This

The `ArticleDetailPage` calls this when a user navigates to `/articles/:id`. The `EditArticlePage` also calls this to load existing data before showing the edit form.

Using IDs in URLs (rather than slugs) for the edit page is intentional: slugs can change if someone renames an article, but IDs never change. The edit page URL `/articles/5/edit` will always work even after the article's slug is updated.

#### Why It Exists Alongside the Slug Endpoint

Two endpoints for getting a single article might seem redundant. They serve different use cases:

- **By ID:** Used internally — reliable, stable, used in edit flows where the ID was already known from a list response
- **By Slug:** Used for human-readable URLs — a user can bookmark `/articles/slug/setting-up-ci-pipeline` and share it

#### Future Improvements

- Add an `ETag` or `Last-Modified` response header to support conditional requests (`If-None-Match`) for caching

---

### GET /api/articles/slug/{slug} — Get Article by Slug

#### Purpose

Retrieves the full content of one article by its URL-friendly slug (e.g., `welcome-to-wikiproject`). A **slug** is a lowercase, hyphen-separated version of the article title that is safe to use in a URL.

#### Route Details

```
GET /api/articles/slug/{slug}
```

The prefix `slug/` in the route path distinguishes this from the ID-based route. Without it, the ASP.NET Core router could not tell whether `/api/articles/welcome-to-wikiproject` should match `{id:int}` (it would fail the int constraint) or a different route altogether. Using the explicit `slug/` prefix makes the routing unambiguous.

#### Path Parameters

| Parameter | Type     | Required | Description |
|-----------|----------|----------|-------------|
| `slug`    | `string` | ✓        | URL-safe slug (lowercase alphanumeric with hyphens) |

#### Example Request

```
GET /api/articles/slug/welcome-to-wikiproject
```

#### Success Response: 200 OK

Same shape as [GET /api/articles/{id}](#get-apiarticlesid--get-article-by-id) — a full `ArticleDto` including `content`.

#### Failure Cases

| Scenario | Status | Body |
|----------|--------|------|
| Slug not found | `404 Not Found` | Empty body |
| Slug contains URL-encoded characters | `200` or `404` depending on exact slug | ASP.NET Core decodes URL encoding automatically |

#### How Slugs Are Generated

If the client does not provide a slug when creating an article, the backend generates one automatically from the title:

```csharp
// ArticleService.cs — GenerateSlug
private static string GenerateSlug(string title)
{
    var slug = title.ToLower().Trim();
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");   // Remove special chars
    slug = Regex.Replace(slug, @"\s+", "-");             // Spaces → hyphens
    slug = Regex.Replace(slug, @"-+", "-");              // Collapse multiple hyphens
    slug = slug.Trim('-');                               // Remove leading/trailing hyphens
    return slug.Length > 100 ? slug[..100] : slug;       // Cap at 100 chars
}
```

If another article already has the same slug (e.g., two articles both titled "Introduction"), the backend appends a counter:

```
introduction
introduction-1
introduction-2
```

This is called **slug collision resolution** and is handled by `EnsureUniqueSlugAsync`.

> **Beginner confusion:** Slugs look like they're just the title lowercased. They are more than that — they also strip punctuation, collapse spaces to hyphens, and must be globally unique in the database. The validation pattern `^[a-z0-9]+(?:-[a-z0-9]+)*$` enforces this format if a slug is provided manually.

#### Why It Exists in This Form

Search engines and humans prefer readable URLs like `/articles/slug/setting-up-ci-pipeline` over `/articles/42`. The slug endpoint enables clean links to specific articles. Many CMS and wiki systems use similar patterns.

#### Future Improvements

- Support case-insensitive slug lookup (currently the slug must match exactly as stored)
- Redirect old slugs to new slugs when an article is renamed (permanent redirect / `301`)

---

### POST /api/articles — Create Article

#### Purpose

Creates a new article in the database. Returns the created article with its assigned ID, slug, and timestamps.

#### Route Details

```
POST /api/articles
Content-Type: application/json
```

#### Request Body

The body must be a JSON object matching the `CreateArticleRequest` shape:

```json
{
  "title": "Deploying to Kubernetes",
  "slug": "deploying-to-kubernetes",
  "summary": "A step-by-step guide to deploying services on our Kubernetes cluster.",
  "content": "# Deploying to Kubernetes\n\n## Prerequisites\n\n...",
  "category": "Infrastructure",
  "tags": ["kubernetes", "deployment", "devops"],
  "status": 1
}
```

| Field      | Type            | Required | Max Length | Notes |
|------------|-----------------|----------|------------|-------|
| `title`    | `string`        | ✓        | 200 chars  | Displayed as the article heading |
| `slug`     | `string`        | ✗        | 200 chars  | URL identifier; auto-generated from title if omitted |
| `summary`  | `string`        | ✓        | 500 chars  | Shown in article cards and previews |
| `content`  | `string`        | ✓        | No limit   | Full article body; Markdown recommended |
| `category` | `string`        | ✓        | 100 chars  | Single category string; free-form, not from a predefined list |
| `tags`     | `string[]`      | ✗        | 50 chars each | Array of tag strings; normalized to lowercase |
| `status`   | `int` (0/1/2)   | ✓        | —          | `0` = Draft, `1` = Published, `2` = Archived |

> **Beginner confusion about `status`:** The TypeScript type in the frontend defines `status` as `number` (integer), but the API response returns it as a `string` (`"Published"`). The frontend sends `status: 1` in the request; the response comes back with `"status": "Published"`. This asymmetry exists because the C# enum serializes to a string by default when reading, but the JSON body deserializes the integer when writing. Be aware of this if you compare or store status values.

#### Example curl Request

```bash
curl -X POST http://localhost:5018/api/articles \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Deploying to Kubernetes",
    "summary": "A step-by-step guide to deploying services.",
    "content": "# Deploying to Kubernetes\n\nFull content here...",
    "category": "Infrastructure",
    "tags": ["kubernetes", "deployment"],
    "status": 1
  }'
```

#### Success Response: 201 Created

The response includes:
- An HTTP `Location` header pointing to the new resource: `Location: api/articles/7`
- The full `ArticleDto` in the body (including the newly assigned `id`, generated `slug`, and timestamps)

```json
{
  "id": 7,
  "title": "Deploying to Kubernetes",
  "slug": "deploying-to-kubernetes",
  "summary": "A step-by-step guide to deploying services.",
  "content": "# Deploying to Kubernetes\n\nFull content here...",
  "category": "Infrastructure",
  "tags": ["deployment", "kubernetes"],
  "status": "Published",
  "createdAt": "2026-03-16T02:30:00Z",
  "updatedAt": "2026-03-16T02:30:00Z"
}
```

Note that `tags` are returned in alphabetical order (the mapping layer applies `.OrderBy(n => n)`). The tags you submitted as `["kubernetes", "deployment"]` come back as `["deployment", "kubernetes"]`.

#### Failure Cases

| Scenario | Status | Body |
|----------|--------|------|
| Validation failure (e.g., title missing) | `400 Bad Request` | Problem Details JSON (see below) |
| Slug format invalid (non-lowercase, special chars) | `400 Bad Request` | Problem Details JSON |
| Database error | `500 Internal Server Error` | Empty or generic error body |

**400 Validation Error Example:**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required."],
    "Summary": ["Summary is required."]
  }
}
```

#### How Validation Works

The controller uses **FluentValidation** to validate the request before calling the service:

```csharp
// ArticlesController.cs
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

The validator (`CreateArticleRequestValidator`) is injected via `[FromServices]`, meaning it comes from ASP.NET Core's dependency injection container. The rules are defined in `src/WikiProject.Api/Validation/ArticleValidators.cs`.

#### How the Frontend Uses This

`NewArticlePage` has a form component (`ArticleForm`) that collects all fields. On submit, it calls:

```ts
const created = await articleService.create(formData);
// Then navigates to the new article's detail page
navigate(`/articles/${created.id}`);
```

#### Why 201 Instead of 200?

HTTP `201 Created` is the semantically correct response for a successful POST that creates a new resource. It tells the caller: "Something new was made, and here is where to find it." The `Location` header provides the URL of the new resource. Using `200 OK` would also work but loses this semantic precision.

#### Future Improvements

- Add slug uniqueness validation in the FluentValidation layer (currently checked only at the database layer — a race condition is technically possible)
- Support bulk creation (multiple articles in one request)
- Return validation errors for duplicate slugs before hitting the database

---

### PUT /api/articles/{id} — Update Article

#### Purpose

Replaces the full content of an existing article. The client must send every field (not just the fields that changed). Returns the updated article.

#### Route Details

```
PUT /api/articles/{id}
Content-Type: application/json
```

#### Path Parameters

| Parameter | Type  | Required | Description |
|-----------|-------|----------|-------------|
| `id`      | `int` | ✓        | Numeric ID of the article to update |

#### Request Body

Same structure as `CreateArticleRequest` — all fields are required to be present (because this is a full replacement, not a partial patch):

```json
{
  "title": "Updated: Deploying to Kubernetes",
  "slug": "deploying-to-kubernetes",
  "summary": "Updated summary with new prerequisites.",
  "content": "# Updated Content...",
  "category": "Infrastructure",
  "tags": ["kubernetes", "deployment", "helm"],
  "status": 1
}
```

#### Example curl Request

```bash
curl -X PUT http://localhost:5018/api/articles/7 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated: Deploying to Kubernetes",
    "summary": "Updated summary.",
    "content": "# Updated Content...",
    "category": "Infrastructure",
    "tags": ["kubernetes", "deployment", "helm"],
    "status": 1
  }'
```

#### Success Response: 200 OK

Returns the full updated `ArticleDto`. The `updatedAt` timestamp is automatically set to the current UTC time.

```json
{
  "id": 7,
  "title": "Updated: Deploying to Kubernetes",
  "slug": "deploying-to-kubernetes",
  "summary": "Updated summary.",
  "content": "# Updated Content...",
  "category": "Infrastructure",
  "tags": ["deployment", "helm", "kubernetes"],
  "status": "Published",
  "createdAt": "2026-03-16T02:30:00Z",
  "updatedAt": "2026-03-16T03:15:00Z"
}
```

#### How Slug Updates Work

If the request sends a new slug (different from the current one), the service checks for uniqueness against all other articles (excluding the current article):

```csharp
if (newSlug != article.Slug)
    newSlug = await EnsureUniqueSlugAsync(newSlug, excludeId: id);
```

If you change the title but do not provide a slug, the slug is **regenerated from the new title** and uniqueness is checked. This means renaming an article also updates its slug — which can break bookmarked URLs. Consider this when renaming articles.

#### How Tags Are Updated

Tags are **fully replaced** on every update, not merged:

```csharp
article.ArticleTags.Clear();
foreach (var tag in tags)
    article.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, Tag = tag });
```

This is simpler but means if you want to add one tag, you must send all existing tags plus the new one. The frontend handles this automatically by pre-populating the tag list from the current article state.

#### Failure Cases

| Scenario | Status | Body |
|----------|--------|------|
| Article not found | `404 Not Found` | Empty body |
| Validation failure | `400 Bad Request` | Problem Details JSON |
| Server error | `500 Internal Server Error` | — |

#### How the Frontend Uses This

`EditArticlePage` first calls `GET /api/articles/{id}` to load the current data, then displays the `ArticleForm`. On submit, it calls:

```ts
const updated = await articleService.update(id, formData);
navigate(`/articles/${id}`);
```

#### Why Not PATCH?

See [Section 2.2](#22-http-methods-verbs). The short answer: full replacement is simpler for both the server and the client in this use case.

---

### DELETE /api/articles/{id} — Delete Article

#### Purpose

Permanently removes an article from the database. There is no soft-delete or trash/recycle bin — deletion is immediate and irreversible.

#### Route Details

```
DELETE /api/articles/{id}
```

#### Path Parameters

| Parameter | Type  | Required | Description |
|-----------|-------|----------|-------------|
| `id`      | `int` | ✓        | Numeric ID of the article to delete |

#### Example curl Request

```bash
curl -X DELETE http://localhost:5018/api/articles/7
```

#### Success Response: 204 No Content

An empty body with status `204`. There is nothing to return — the resource no longer exists.

> **Beginner confusion:** Some developers expect `DELETE` to return the deleted object (so you know what was deleted). HTTP best practices recommend `204 No Content` for successful deletions — the client already knows what it deleted because it sent the ID. Returning the deleted resource is also a valid choice (some APIs do this), but `204` is more common and communicates intent clearly.

#### Failure Cases

| Scenario | Status | Body |
|----------|--------|------|
| Article not found | `404 Not Found` | Empty body |
| Server error | `500 Internal Server Error` | — |

#### How the Frontend Uses This

The `ArticleDetailPage` has a Delete button. On click, it confirms with the user (likely a `window.confirm` dialog), then calls:

```ts
await articleService.delete(id);
navigate('/articles'); // Go back to list
```

#### What Happens to the Article's Tags?

When an article is deleted, EF Core cascades the deletion to the `ArticleTag` join table entries. The `Tag` records themselves are **not deleted** — orphaned tags remain in the `Tags` table. This means the tags list (`GET /api/tags`) may include tags that no longer have any associated articles.

> **Potential future improvement:** Add a cleanup step that removes orphaned tags after article deletion.

#### Why This Matters

Destructive operations without undo capability are a usability and data-safety concern. Current mitigations: none. Future mitigations might include soft-delete (setting an `IsDeleted` flag), an undo window, or audit logging.

---

## 6. Metadata Endpoints

---

### GET /api/categories — List Categories

#### Purpose

Returns a sorted list of all distinct category strings that exist across all articles in the database. The frontend uses this to populate the **Category** dropdown in the filter controls.

#### Route Details

```
GET /api/categories
```

No parameters.

#### Example Request

```bash
curl http://localhost:5018/api/categories
```

#### Success Response: 200 OK

```json
["Development", "General", "Infrastructure", "Operations", "Security"]
```

A plain JSON array of strings, sorted alphabetically.

#### Why This Exists

Categories in this system are **free-form strings** — there is no `Category` table with predefined options. Any string can be used as a category when creating or editing an article. This means:
- Categories are flexible and don't require schema changes to add new ones
- The set of available categories is whatever has been used so far
- This endpoint dynamically discovers all categories in use

The `DISTINCT` SQL query (generated by EF Core's `.Distinct()`) ensures each category string appears only once regardless of how many articles use it.

> **Beginner confusion:** Free-form categories can lead to inconsistencies — "Dev", "Development", and "development" would appear as three separate categories. In production systems, you might want to normalize categories to a controlled vocabulary or add fuzzy matching.

#### How the Frontend Uses This

`ArticlesPage` calls this endpoint once when the page mounts to populate the Category filter dropdown. Because categories rarely change, this request is not repeated on every filter change.

#### Alternative Approaches

- **Predefined category table:** Add a `Categories` table with `GET /api/categories` returning its contents and `POST /api/categories` to add new ones. This enforces consistency but requires more setup.
- **Enum in code:** Define categories as a C# enum, which prevents typos but requires a code change to add categories.
- **Tags as a superset:** Use only tags and eliminate categories. Tags already serve a similar purpose with more flexibility.

#### Future Improvements

- Add article counts per category: `{"name": "Development", "count": 12}`
- Support case-insensitive deduplication (so "Development" and "development" merge into one)

---

### GET /api/tags — List Tags

#### Purpose

Returns a sorted list of all tag names currently in the `Tags` table. The frontend uses this to populate the **Tag** filter dropdown.

#### Route Details

```
GET /api/tags
```

No parameters.

#### Example Request

```bash
curl http://localhost:5018/api/tags
```

#### Success Response: 200 OK

```json
["api", "database", "deployment", "devops", "getting-started", "guide", "kubernetes", "reference", "tips"]
```

#### How Tags Are Stored

Tags are stored as their own normalized entities in a dedicated `Tags` table and linked to articles via an `ArticleTag` join table. When you create or update an article with `tags: ["Kubernetes", "DEPLOYMENT"]`, the service normalizes them:

```csharp
var normalized = tagNames
    .Select(t => t.Trim().ToLower())   // "kubernetes", "deployment"
    .Where(t => !string.IsNullOrEmpty(t))
    .Distinct()
    .ToList();
```

Existing tags are reused; new tags are created. This prevents duplicates like `"kubernetes"` and `"Kubernetes"` from coexisting.

#### Why Tags Have Their Own Table

A simpler approach would be to store tags as a comma-separated string in the `Articles` table (`tags: "kubernetes,deployment"`). The dedicated table approach is chosen because:
1. It allows efficient filtering: `WHERE tag = 'kubernetes'` is a join query rather than a string search
2. It normalizes the data — the tag "kubernetes" exists in one place
3. It enables future features like tag descriptions, colors, or usage counts

> **The tradeoff:** More complex queries and more tables for a simple feature. For a project expected to have few tags and simple requirements, a CSV column would also be acceptable.

#### How the Frontend Uses This

`ArticlesPage` calls this endpoint once on mount to populate the Tag filter dropdown, alongside the categories call.

#### Future Improvements

- Include article count per tag: `{"name": "kubernetes", "count": 5}`
- Add `GET /api/tags/{name}` to look up articles by a specific tag
- Add `DELETE /api/tags/{name}` to remove orphaned tags
- Support tag autocomplete in the article edit form (currently the frontend uses a text input)

---

## 7. Request and Response Shapes (DTOs)

**DTO** stands for **Data Transfer Object**. A DTO is a simple data structure used to carry data between the API and its consumers. It is different from an entity (which represents a database row) — a DTO is shaped for API communication, not for persistence.

All DTOs are defined in `src/WikiProject.Api/DTOs/ArticleDtos.cs` using C# `record` types (immutable, value-based equality).

### ArticleDto

Used in: `GET /api/articles/{id}`, `GET /api/articles/slug/{slug}`, `POST /api/articles` (response), `PUT /api/articles/{id}` (response)

```
ArticleDto {
  id:         int       — numeric primary key
  title:      string    — article heading
  slug:       string    — URL-safe identifier
  summary:    string    — brief description (max 500 chars)
  content:    string    — full body text (Markdown)
  category:   string    — single category string
  tags:       string[]  — array of tag names (alphabetically sorted)
  status:     string    — "Draft", "Published", or "Archived"
  createdAt:  DateTime  — ISO 8601 UTC timestamp (e.g., "2026-03-16T02:30:00Z")
  updatedAt:  DateTime  — ISO 8601 UTC timestamp
}
```

### ArticleSummaryDto

Used in: `GET /api/articles` (items in the list response)

Same as `ArticleDto` but **without the `content` field**. This keeps list responses lightweight.

```
ArticleSummaryDto {
  id, title, slug, summary, category, tags, status, createdAt, updatedAt
  // No 'content' field
}
```

### ArticleListResponse

Wraps the paginated list returned by `GET /api/articles`.

```
ArticleListResponse {
  items:      ArticleSummaryDto[]  — array of article summaries
  totalCount: int                  — total matching articles (for pagination math)
  page:       int                  — current page number (1-indexed)
  pageSize:   int                  — items per page (as clamped by the server)
  totalPages: int                  — ceil(totalCount / pageSize)
}
```

**Why include `totalPages`?** The frontend could calculate this as `Math.ceil(totalCount / pageSize)`, but returning it pre-calculated is a convenience that prevents the frontend from doing arithmetic with potentially different rounding behavior.

### CreateArticleRequest

Used in: `POST /api/articles` (request body)

```
CreateArticleRequest {
  title:    string   — required, max 200 chars
  slug?:    string   — optional; auto-generated if absent
  summary:  string   — required, max 500 chars
  content:  string   — required
  category: string   — required, max 100 chars
  tags:     string[] — optional; each max 50 chars
  status:   int      — 0=Draft, 1=Published, 2=Archived
}
```

### UpdateArticleRequest

Used in: `PUT /api/articles/{id}` (request body)

Structurally identical to `CreateArticleRequest`. Defined as a separate type to allow the validators to diverge in the future if needed (e.g., if updating required different rules from creating).

### TypeScript Counterparts

The frontend TypeScript types mirror these DTOs:

```ts
// frontend/src/types/index.ts
export interface Article extends ArticleSummary {
  content: string;
}

export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;     // TypeScript union: 'Draft' | 'Published' | 'Archived'
  createdAt: string;         // ISO string, not a Date object
  updatedAt: string;
}
```

> **Note:** TypeScript uses `string` for `createdAt` and `updatedAt` rather than `Date`. JSON does not have a native date type — dates are serialized as ISO 8601 strings. The frontend's `format.ts` utility formats these strings for display.

---

## 8. Validation Rules

Validation happens in **two places**:

1. **FluentValidation (server-side, always):** `ArticleValidators.cs` defines rules that run before any database access
2. **Frontend form validation (client-side, UX only):** The `ArticleForm` component validates before submitting; this provides fast feedback but does not replace server-side validation

Never trust client-side validation alone. The server must always validate independently.

### Validation Rules Table

| Field      | Rules | Error Messages |
|------------|-------|----------------|
| `title`    | Required; max 200 chars | "Title is required." / "Title must be 200 characters or fewer." |
| `slug`     | Optional; if provided: max 200 chars; must match `^[a-z0-9]+(?:-[a-z0-9]+)*$` | "Slug must be 200 characters or fewer." / "Slug must be lowercase alphanumeric with hyphens." |
| `summary`  | Required; max 500 chars | "Summary is required." / "Summary must be 500 characters or fewer." |
| `content`  | Required (not empty) | "Content is required." |
| `category` | Required; max 100 chars | "Category is required." / "Category must be 100 characters or fewer." |
| `tags`     | Optional; each tag max 50 chars | "Each tag must be 50 characters or fewer." |
| `status`   | Must be a valid `ArticleStatus` enum value | Handled by model binding before validation |

### About the Slug Regex

```
^[a-z0-9]+(?:-[a-z0-9]+)*$
```

Breaking this down:
- `^` — start of string
- `[a-z0-9]+` — one or more lowercase letters or digits
- `(?:-[a-z0-9]+)*` — zero or more groups of (hyphen followed by lowercase letters or digits)
- `$` — end of string

Valid: `welcome-to-wikiproject`, `api-v2`, `getting-started`, `article123`
Invalid: `Welcome`, `api--v2` (double hyphen), `-leading-hyphen`, `trailing-hyphen-`, `has spaces`

This rule is checked only when a slug is explicitly provided (`When(x => !string.IsNullOrEmpty(x.Slug))`). If slug is omitted, the server generates a valid one automatically.

### Validation Conventions

- FluentValidation is used instead of built-in ASP.NET Core Data Annotations (`[Required]`, `[MaxLength]`) because it keeps validation logic separate from the DTO class itself and is more composable and testable
- The same rules are duplicated in `CreateArticleRequestValidator` and `UpdateArticleRequestValidator`. This duplication is intentional to allow future divergence (e.g., making certain fields optional during updates)
- Validation runs **before** the service layer is called, so the database is never touched for invalid requests

---

## 9. HTTP Status Codes Used in This API

Understanding status codes is fundamental to working with any HTTP API. Here is every code this API returns and what it means:

| Code | Name | When Returned | What To Do as a Client |
|------|------|---------------|------------------------|
| `200 OK` | Success | Successful `GET` or `PUT` | Read the response body |
| `201 Created` | Resource Created | Successful `POST` | Read the response body; the `Location` header points to the new resource |
| `204 No Content` | Success, No Body | Successful `DELETE` | No body to read |
| `400 Bad Request` | Validation Failed | `POST` or `PUT` with invalid data | Read `errors` in the Problem Details body to show field-level errors |
| `404 Not Found` | Resource Missing | `GET`/`PUT`/`DELETE` with an ID that doesn't exist | Show a "not found" message to the user |
| `500 Internal Server Error` | Server Error | Unhandled exception | Show a generic error message; check server logs |

### Why No 401 or 403?

`401 Unauthorized` means "you need to authenticate" (no token provided). `403 Forbidden` means "you are authenticated but not permitted." Since authentication is not yet implemented, neither of these codes is currently in use. See [Section 12](#12-security-current-state-and-future-work).

### How the Frontend Handles Status Codes

The `getErrorMessage` helper in `articleService.ts` extracts a user-friendly message from an Axios error:

```ts
export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const detail = error.response?.data;
    if (detail?.title) return detail.title;   // e.g., "One or more validation errors occurred."
    if (typeof detail === 'string') return detail;
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred.';
}
```

This function reads the Problem Details `title` field (for 400 errors) and falls back to the Axios error message for other errors. For field-level validation errors, the frontend would need to additionally inspect `error.response?.data?.errors` to show per-field messages.

---

## 10. How the Frontend Consumes the API

The frontend is a single-page application (SPA) built with React. All API communication goes through a single file: `frontend/src/services/articleService.ts`.

### The API Client Layer

```ts
// frontend/src/services/articleService.ts
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5018',
  headers: { 'Content-Type': 'application/json' },
});
```

An Axios instance is created with the base URL read from the `VITE_API_URL` environment variable (from `.env.example`). If not set, it defaults to `http://localhost:5018`. In development, the Vite proxy (`/api` → `http://localhost:5018`) means this base URL is effectively overridden for browser requests.

All service methods are `async` and return strongly-typed promises. Axios throws an error for any non-2xx status code — the caller is responsible for catching this.

### Custom Hooks

React's custom hooks wrap the service calls with loading and error state management, so components don't need to manage this themselves:

**`useArticles(filters)`** — wraps `articleService.list(filters)`:
- Tracks `loading`, `articles`, and `error` state
- Re-fetches automatically when `filters` changes (via a `useEffect` dependency array)
- Used by `ArticlesPage` and `HomePage`

**`useArticle(id)`** — wraps `articleService.getById(id)`:
- Tracks `loading`, `article`, and `error` state
- Used by `ArticleDetailPage` and `EditArticlePage`

### Page-by-Page API Usage

| Page | Route | API Calls Made |
|------|-------|----------------|
| `HomePage` | `/` | `GET /api/articles` (no filters, shows recent articles) |
| `ArticlesPage` | `/articles` | `GET /api/categories`, `GET /api/tags`, `GET /api/articles?...` |
| `ArticleDetailPage` | `/articles/:id` | `GET /api/articles/{id}`, `DELETE /api/articles/{id}` (on delete) |
| `NewArticlePage` | `/articles/new` | `POST /api/articles` |
| `EditArticlePage` | `/articles/:id/edit` | `GET /api/articles/{id}` (pre-populate form), `PUT /api/articles/{id}` (on save) |

### The CORS Setup

**CORS** (Cross-Origin Resource Sharing) is a browser security mechanism that prevents JavaScript from one origin (e.g., `http://localhost:5173`) from making requests to a different origin (e.g., `http://localhost:5018`) unless the server explicitly allows it.

The backend configures CORS in `Program.cs`:

```csharp
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ...
app.UseCors();
```

In development, `http://localhost:5173` (the Vite dev server) is allowed. In production, `Cors:AllowedOrigins` in `appsettings.json` must be updated to the real frontend URL.

The Vite proxy sidesteps CORS during development (since the browser only sees `localhost:5173`), but the CORS configuration still matters for non-proxied clients like Swagger or curl calling `localhost:5018` directly from a browser.

---

## 11. Common Beginner Misconceptions About APIs

### "The frontend and backend must be in the same programming language"

They do not. This project uses TypeScript for the frontend and C# for the backend. They communicate through JSON over HTTP — a language-neutral format. The frontend does not import or call C# code directly.

### "GET requests can't have a body"

Technically, HTTP allows GET requests to carry a body, but it is not recommended and most HTTP clients (including browsers and Axios) do not support it. For filtering, use query string parameters (`?search=foo&category=bar`).

### "Validation in the frontend is enough"

Client-side validation is user-experience sugar — it gives fast feedback without a round trip. It is **not** security. Any user can call the API directly with `curl` or Postman, bypassing the frontend entirely. Always validate on the server.

### "404 means the server is down"

`404` means the server responded but could not find the requested resource. If the server were down, you would get a network error (no response at all) rather than a 404 status code. When debugging, distinguish between "I got a response but it was 404" and "I got a connection error."

### "POST is for creating, GET is for reading — that's the full picture"

Almost. In practice:
- `POST` is sometimes used for complex queries (when query parameters aren't enough)
- `PUT` and `PATCH` are both for updates, with different semantics
- `DELETE` is the only method with near-universal agreement on meaning

### "The API returns live data instantly"

The API is a database-backed service. Every request involves a database query. There is no real-time push — the frontend polls by making new requests. For live collaboration features (multiple users editing simultaneously), you'd need WebSockets or Server-Sent Events, which are not implemented here.

### "HTTP status codes in the 200s always mean everything worked perfectly"

`2xx` means the request was processed successfully according to the server's understanding. A `200 OK` from `GET /api/articles` with an empty `items` array is still a success — no articles matched the filter. "Success" means the operation completed, not that it produced the result the user wanted.

---

## 12. Security: Current State and Future Work

### Current State

All endpoints are **publicly accessible with no authentication**. Anyone who can reach the server can read, create, update, and delete articles.

In the backend, there are no `[Authorize]` attributes on controllers or actions, and no authentication middleware is registered. The comment in `Program.cs` marks where authentication would be added:

```csharp
// Extension point: app.UseAuthentication(); app.UseAuthorization();
```

This is appropriate for a private internal tool running on a trusted network (e.g., a company intranet with no external exposure). It is not appropriate for a public internet deployment.

### Why Not Yet

Adding authentication increases complexity. The README and `STARTING_TASKS.md` indicate that auth is a planned future task. The codebase is structured so that adding `[Authorize]` to the controllers and adding the authentication middleware to `Program.cs` is a self-contained change.

### What Authentication Would Look Like

If JWT (JSON Web Token) bearer authentication were added:
1. A login endpoint (`POST /api/auth/login`) would accept credentials and return a JWT
2. Subsequent API calls would include `Authorization: Bearer <token>` in the request header
3. The frontend `articleService.ts` would attach this header to every Axios request
4. Controllers would be decorated with `[Authorize]`

Alternatively, session-based authentication via ASP.NET Core Identity would provide cookie-based auth for a browser-first application.

### Rate Limiting and Input Sanitization

- There is no rate limiting — a client can make unlimited requests
- User-submitted content (article body) is stored as raw text and not sanitized server-side; the frontend is responsible for safe rendering
- The `content` field length is unlimited, which could allow very large payloads

These are all known limitations appropriate for an internal early-stage tool.

---

## 13. Section Recap

This document has covered every HTTP endpoint exposed by WikiProject's API:

| Endpoint | What It Does |
|----------|-------------|
| `GET /api/articles` | Paginated list with full-text search and filters |
| `GET /api/articles/{id}` | Single article by numeric ID (includes content) |
| `GET /api/articles/slug/{slug}` | Single article by URL slug (includes content) |
| `POST /api/articles` | Create a new article (returns 201 with Location header) |
| `PUT /api/articles/{id}` | Full replacement update of an existing article |
| `DELETE /api/articles/{id}` | Permanent deletion (returns 204) |
| `GET /api/categories` | Dynamic list of all categories in use |
| `GET /api/tags` | All tags in the tags table |

Key design choices to remember:

- **REST conventions** throughout: plural nouns in URLs, HTTP verbs for actions
- **DTOs separate from entities**: `ArticleDto` vs `Article` — the API surface is decoupled from database schema
- **Summaries vs. full articles**: the list endpoint returns lightweight `ArticleSummaryDto` (no content), detail endpoints return full `ArticleDto`
- **FluentValidation** for server-side validation, returning RFC 7807 Problem Details on failure
- **Slug auto-generation** with collision resolution; slugs are normalized to lowercase with hyphens
- **Tags are normalized and reused**: submitting `["Kubernetes"]` stores and returns `["kubernetes"]`
- **No authentication** currently; structure is prepared for future JWT or Identity integration

**What to study next:**

- **Backend Architecture doc** (to be created by another agent): covers EF Core migrations, the service layer pattern, dependency injection, and data seeding
- **Frontend Architecture doc** (to be created by another agent): covers React component structure, routing, custom hooks, and state management
- **Official ASP.NET Core documentation:** https://learn.microsoft.com/en-us/aspnet/core/
- **REST API design best practices:** https://restfulapi.net/
- **FluentValidation docs:** https://docs.fluentvalidation.net/
- **Axios documentation:** https://axios-http.com/docs/intro
- **RFC 7807 (Problem Details for HTTP APIs):** https://www.rfc-editor.org/rfc/rfc7807
