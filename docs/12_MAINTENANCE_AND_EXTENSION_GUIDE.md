# Maintenance and Extension Guide

**WikiProject — Practical Change Reference for Developers**

---

## Who This Document Is For

This guide is written for a developer who is new to the WikiProject codebase. You are assumed to be smart and motivated, but possibly overwhelmed by the number of moving parts. You do not need deep experience with .NET or React — but you do need a willingness to read carefully and look things up when terms are unfamiliar.

By the end of this document you should be able to:

- Add a new HTTP endpoint without breaking existing ones
- Add a new page to the React frontend
- Add a field to the database schema
- Update DTOs, validators, and type definitions safely
- Extend search and filtering behaviour
- Understand where regressions hide and how to avoid them

This guide is **change-oriented**. It does not re-explain the general architecture (see a future Architecture Guide for that). Instead it focuses on the **practical mechanics** of making safe, incremental changes.

---

## Table of Contents

1. [Understanding the Change Surface](#1-understanding-the-change-surface)
2. [How to Add a New Backend Endpoint](#2-how-to-add-a-new-backend-endpoint)
3. [How to Add a New Frontend Page](#3-how-to-add-a-new-frontend-page)
4. [How to Add a New Database Field](#4-how-to-add-a-new-database-field)
5. [How to Update DTOs](#5-how-to-update-dtos)
6. [How to Update Validation](#6-how-to-update-validation)
7. [How to Extend Search and Filter Behaviour](#7-how-to-extend-search-and-filter-behaviour)
8. [How to Safely Change a Domain Model](#8-how-to-safely-change-a-domain-model)
9. [Where Regressions Are Likely to Happen](#9-where-regressions-are-likely-to-happen)
10. [How to Test Changes](#10-how-to-test-changes)
11. [Safe Workflow for Beginners](#11-safe-workflow-for-beginners)

---

## 1. Understanding the Change Surface

Before making any change, it helps to understand the path that data travels through the system. Almost every change you make will touch this path in one or more places.

### The Data Path

```
Browser (React)
    │  sends HTTP request via Axios (articleService.ts)
    ▼
ASP.NET Core Controller   ← validates request with FluentValidation
    │  delegates to
    ▼
Service Layer (ArticleService.cs)
    │  queries or modifies
    ▼
Entity Framework Core (WikiDbContext.cs)
    │  translates to SQL
    ▼
SQLite database (wiki.db)
```

On the way back, the flow is reversed:

```
SQLite → EF Core → Entity → Mapping (ArticleMappings.cs) → DTO → JSON → Axios → TypeScript type → React component
```

**Why this matters:** when you add a new field, you are not just adding one thing — you are adding a link in a chain that spans five or six files. Forgetting one link breaks the chain silently (the data just does not appear) or loudly (a compile error or a runtime exception).

### The Key Files at a Glance

| Layer | File | What It Contains |
|-------|------|-----------------|
| Entity (DB shape) | `src/WikiProject.Api/Entities/Article.cs` | C# class that EF Core maps to a DB table |
| DB config | `src/WikiProject.Api/Data/WikiDbContext.cs` | Indexes, relationships, constraints |
| Migration | `src/WikiProject.Api/Migrations/` | Auto-generated SQL schema changes |
| DTO (wire shape) | `src/WikiProject.Api/DTOs/ArticleDtos.cs` | What the API sends/receives as JSON |
| Mapping | `src/WikiProject.Api/Mappings/ArticleMappings.cs` | Converts Entity ↔ DTO |
| Validation | `src/WikiProject.Api/Validation/ArticleValidators.cs` | FluentValidation rules for request DTOs |
| Service | `src/WikiProject.Api/Services/ArticleService.cs` | Business logic |
| Controller | `src/WikiProject.Api/Controllers/ArticlesController.cs` | HTTP route handlers |
| Program.cs | `src/WikiProject.Api/Program.cs` | Service registration and middleware |
| TS types | `frontend/src/types/index.ts` | TypeScript interfaces matching the DTO shapes |
| API client | `frontend/src/services/articleService.ts` | Axios calls to the backend |
| Hooks | `frontend/src/hooks/` | React state management around API calls |
| Pages | `frontend/src/pages/` | Full-page React components |
| Components | `frontend/src/components/` | Reusable UI pieces |
| Routing | `frontend/src/App.tsx` | Maps URL paths to page components |

---

## 2. How to Add a New Backend Endpoint

### What "endpoint" means here

An **endpoint** is a URL path that the API responds to, like `GET /api/articles` or `DELETE /api/articles/5`. Each endpoint is a public method on a C# Controller class decorated with an HTTP attribute (`[HttpGet]`, `[HttpPost]`, etc.).

### Deciding where to put it

The project already has two controllers:

- `ArticlesController` — everything related to article CRUD and lookup (`/api/articles`)
- `MetadataController` — lightweight read-only lists of categories and tags (`/api/categories`, `/api/tags`)

**Rule of thumb:**
- If the new endpoint deals with articles, add it to `ArticlesController`.
- If it returns a simple list of metadata unrelated to a single article, add it to `MetadataController`.
- If it does not fit either, create a new controller file (see below).

### Step-by-step: adding a simple read endpoint

**Example goal:** Add `GET /api/articles/{id}/tags` — returns just the tags for one article.

#### Step 1 — Add the method to IArticleService

Open `src/WikiProject.Api/Services/IArticleService.cs` and add the signature:

```csharp
Task<IReadOnlyList<string>?> GetTagsByArticleIdAsync(int id);
```

The `?` (nullable) return means the method can return `null` when the article does not exist, letting the controller return a `404 Not Found`.

#### Step 2 — Implement it in ArticleService

Open `src/WikiProject.Api/Services/ArticleService.cs` and add the implementation near the other `Get` methods:

```csharp
public async Task<IReadOnlyList<string>?> GetTagsByArticleIdAsync(int id)
{
    var article = await _db.Articles
        .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == id);

    if (article is null)
        return null;

    return article.ArticleTags
        .Select(at => at.Tag.Name)
        .OrderBy(n => n)
        .ToList();
}
```

> **Beginner confusion:** `AsNoTracking()` tells EF Core not to keep a change-tracking record of this entity. Use it for any query that only reads data and does not then modify it. It makes reads faster.

#### Step 3 — Add the route to ArticlesController

Open `src/WikiProject.Api/Controllers/ArticlesController.cs` and add:

```csharp
/// <summary>Returns all tags on a specific article.</summary>
[HttpGet("{id:int}/tags")]
public async Task<ActionResult<IReadOnlyList<string>>> GetTagsByArticle(int id)
{
    var tags = await _articleService.GetTagsByArticleIdAsync(id);
    return tags is null ? NotFound() : Ok(tags);
}
```

> **Why the interface first?** The controller depends on `IArticleService`, not `ArticleService` directly. Adding the signature to the interface before implementing it keeps the compiler honest — it will refuse to compile if you forget to implement the method.

#### Step 4 — Verify in Swagger

Start the backend with `dotnet run` from `src/WikiProject.Api/`. Open `http://localhost:5018/swagger`. Your new endpoint should appear under the `Articles` group. Use the "Try it out" button to test it.

### Step-by-step: adding a write endpoint (POST/PUT/DELETE)

Write endpoints follow the same pattern but add validation. See [Section 6](#6-how-to-update-validation) for how to add validation rules.

**Example goal:** Add `POST /api/articles/{id}/duplicate` — duplicates an article.

The pattern is:

1. Add `Task<ArticleDto> DuplicateAsync(int id)` to `IArticleService`
2. Implement it in `ArticleService` (create a new `Article` entity, give it a unique slug with `EnsureUniqueSlugAsync`, save)
3. Add `[HttpPost("{id:int}/duplicate")]` action to `ArticlesController`
4. Return `CreatedAtAction` pointing to `GetById` with the new article's ID

### Creating a new controller

If you are adding a feature that does not belong in either existing controller (for example, a future `UserController` for authentication):

1. Create `src/WikiProject.Api/Controllers/UserController.cs`
2. Decorate the class with `[ApiController]` and `[Route("api/users")]`
3. Inject your service via the constructor (see how `ArticlesController` does it)
4. No registration needed — `AddControllers()` in `Program.cs` automatically discovers all controllers in the assembly

### Checklist: adding a backend endpoint

- [ ] Signature added to `IArticleService` (if using existing service)
- [ ] Implementation added to `ArticleService`
- [ ] Route action added to appropriate controller with correct HTTP attribute
- [ ] XML summary comment added to the action (keeps Swagger docs accurate)
- [ ] `[FromBody]` with validator injected if the endpoint accepts a request body
- [ ] Returns correct HTTP status codes (`Ok`, `NotFound`, `CreatedAtAction`, `NoContent`, `ValidationProblem`)
- [ ] Verified in Swagger UI

### Common beginner confusion: route templates

Route templates like `{id:int}` include a **route constraint**. The `:int` part tells ASP.NET Core to only match this route when the `{id}` segment is a valid integer. This is how `GET /api/articles/5` and `GET /api/articles/slug/my-article` can both exist without ambiguity — one matches `{id:int}` and the other does not.

### Alternative approaches

The current design puts business logic in a service class and keeps controllers thin. An alternative (common in very small projects) is to put database calls directly in the controller. **Do not do this here** — the project already has the service layer established, and mixing concerns will make the code harder to test and maintain.

---

## 3. How to Add a New Frontend Page

### What "page" means here

A **page** is a React component that occupies the entire screen for a given URL route. Pages live in `frontend/src/pages/`. They are typically responsible for:

- Fetching their own data (using hooks or direct `useEffect` calls)
- Composing smaller components (cards, forms, filter controls)
- Handling page-level state (search terms, selected filters)

Components (in `frontend/src/components/`) are reusable pieces that a page uses. The distinction matters because pages are registered in the router, while components are not.

### Step-by-step: adding a new page

**Example goal:** Add a `TagsPage` at `/tags` that lists all tags with article counts.

#### Step 1 — Create the page file

Create `frontend/src/pages/TagsPage.tsx`:

```tsx
import { useState, useEffect } from 'react';
import { articleService } from '../services/articleService';
import { LoadingSpinner, ErrorMessage, EmptyState } from '../components/StateDisplay';

export default function TagsPage() {
  const [tags, setTags] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    articleService.getTags()
      .then(setTags)
      .catch(() => setError('Failed to load tags.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} />;
  if (tags.length === 0) return <EmptyState message="No tags found." />;

  return (
    <div className="page">
      <h1>All Tags</h1>
      <ul>
        {tags.map((tag) => (
          <li key={tag}>{tag}</li>
        ))}
      </ul>
    </div>
  );
}
```

> **Beginner confusion:** `useEffect` with an empty dependency array `[]` runs exactly once — after the component first renders. This is how you fetch data when a page loads.

#### Step 2 — Register the route in App.tsx

Open `frontend/src/App.tsx` and add the import and route:

```tsx
import TagsPage from './pages/TagsPage';

// Inside <Routes>:
<Route path="/tags" element={<TagsPage />} />
```

The full `App.tsx` after the change:

```tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import HomePage from './pages/HomePage';
import ArticlesPage from './pages/ArticlesPage';
import ArticleDetailPage from './pages/ArticleDetailPage';
import NewArticlePage from './pages/NewArticlePage';
import EditArticlePage from './pages/EditArticlePage';
import TagsPage from './pages/TagsPage';   // ← new

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
          <Route path="/tags" element={<TagsPage />} />   {/* ← new */}
        </Routes>
      </main>
    </BrowserRouter>
  );
}
```

#### Step 3 — Add a navigation link (optional but expected)

Open `frontend/src/components/Header.tsx` and add a `<Link to="/tags">Tags</Link>` alongside the existing navigation links to make the page discoverable from the UI.

#### Step 4 — Test in the browser

Run `npm run dev` from the `frontend/` directory. Navigate to `http://localhost:5173/tags`. The page should render with the list of tags fetched from the backend.

### Using existing hooks vs. fetching directly

The project provides two custom hooks:

- `useArticles(filters)` — paginated article list
- `useArticle(id)` and `useArticleBySlug(slug)` — single article

If your page needs articles, use these hooks rather than calling `articleService` directly. The hooks handle loading/error state for you.

If you need data that no hook covers yet (like tags, or a new endpoint you added), fetch directly with `useEffect` as shown above, or create a new custom hook in `frontend/src/hooks/` following the same pattern as `useArticles.ts`.

### Checklist: adding a new frontend page

- [ ] New `PageName.tsx` file created in `frontend/src/pages/`
- [ ] Component is a default export
- [ ] Data fetching uses existing hooks or `useEffect`
- [ ] Loading, error, and empty states are handled (use `StateDisplay` components)
- [ ] Route registered in `frontend/src/App.tsx`
- [ ] Import added to `App.tsx`
- [ ] Navigation link added to `Header.tsx` if the page should be globally accessible
- [ ] TypeScript types used for all state variables (no `any`)
- [ ] Tested in browser at the registered URL

### Common beginner confusion: route ordering in React Router v7

React Router matches routes from top to bottom and stops at the first match. This matters for routes like `/articles/new` and `/articles/:id`. Because `new` is a string and `:id` is a wildcard, placing `/articles/new` **before** `/articles/:id` ensures React Router does not mistake the literal string "new" for an article ID. The existing `App.tsx` already orders routes correctly — follow the same pattern.

### Alternative approaches

Instead of using React state (`useState` + `useEffect`), you could use a data-fetching library like React Query or SWR, which provide caching, background refetching, and automatic error retries. The current project deliberately avoids this to keep dependencies minimal. If the project grows significantly, migrating the hooks to React Query would be a worthwhile improvement.

---

## 4. How to Add a New Database Field

### Why this is more involved than it looks

Adding a field is not just editing the C# class. The field needs to exist in the database too, and the tool that bridges C# and the database (Entity Framework Core, or "EF Core") generates the bridge automatically through a process called a **migration**. If you skip the migration step, the app will crash at startup because the database schema does not match what EF Core expects.

### Step-by-step: adding a nullable field to Article

**Example goal:** Add an `AuthorName` field to store the name of the person who wrote the article.

#### Step 1 — Add the property to the entity

Open `src/WikiProject.Api/Entities/Article.cs`:

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? AuthorName { get; set; }              // ← new (nullable)
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

> **Why nullable (`string?`)?** The existing database rows do not have an `AuthorName`. Making it nullable (`string?`) means EF Core will store `NULL` for existing rows rather than requiring a default value or failing. If the field is required for all new articles, you can make it non-nullable *after* filling existing rows, or set a default value.

#### Step 2 — Create a migration

Open a terminal in `src/WikiProject.Api/` and run:

```bash
dotnet ef migrations add AddAuthorNameToArticle
```

This command:
1. Compares your current entity model to the last migration snapshot
2. Generates a new C# migration file in `src/WikiProject.Api/Migrations/`
3. Adds an `ALTER TABLE` SQL command (for SQLite it re-creates the table)

> **Prerequisite:** You need the EF Core tools installed. If you get "command not found", run:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

#### Step 3 — Review the migration

Open the generated file (e.g., `Migrations/20260320_AddAuthorNameToArticle.cs`) and check that the `Up()` method adds only what you intended. A typical migration looks like:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "AuthorName",
        table: "Articles",
        type: "TEXT",
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "AuthorName",
        table: "Articles");
}
```

The `Down()` method reverses the migration — this is used if you need to roll back.

> **Beginner confusion:** You should never manually edit the auto-generated migration files unless you have a very specific reason. Editing them can corrupt the migration history and break future migrations.

#### Step 4 — Apply the migration

The app applies pending migrations automatically on startup (see `db.Database.Migrate()` in `Program.cs`). You can also apply manually:

```bash
dotnet ef database update
```

This updates `wiki.db` to include the new column.

#### Step 5 — Continue to DTOs, validation, and mapping

At this point the database has the new column, but the API does not expose it yet. Follow the steps in [Section 5](#5-how-to-update-dtos) to add the field to the DTO and mapping, and [Section 6](#6-how-to-update-validation) to add validation rules if needed.

### Adding a required (non-nullable) field to an existing table

Adding a non-nullable field to a table that already has rows is trickier because existing rows have no value for the new column. There are two safe approaches:

**Option A — Provide a default value in the migration:**

```csharp
migrationBuilder.AddColumn<string>(
    name: "AuthorName",
    table: "Articles",
    nullable: false,
    defaultValue: "Unknown");
```

**Option B — Make it nullable first, backfill the data, then alter it to non-nullable.**

For a development-only SQLite database with seed data, deleting `wiki.db` and letting the app re-seed is also acceptable. **Never do this in production.**

### Adding a field to the Tag or ArticleTag entity

The same process applies. For `Tag`, add the property to `Tag.cs`. For the join table `ArticleTag`, be aware that the composite primary key is configured in `WikiDbContext.OnModelCreating()` — do not accidentally disrupt that configuration.

### Checklist: adding a new database field

- [ ] Property added to the C# entity class in `Entities/`
- [ ] `dotnet ef migrations add <MigrationName>` run from `src/WikiProject.Api/`
- [ ] Generated migration file reviewed for correctness
- [ ] Existing rows handled (nullable field, or default value set in migration)
- [ ] `dotnet ef database update` run (or app restarted to auto-apply)
- [ ] DTOs updated (Section 5)
- [ ] Mapping updated (Section 5)
- [ ] Validation updated if the field should be validated (Section 6)
- [ ] Frontend TypeScript types updated (Section 5)

### Common beginner confusion: "database is locked" error

SQLite only allows one writer at a time. If you have the app running when you try to run `dotnet ef database update`, you may see a "database is locked" error. Stop the running app first.

---

## 5. How to Update DTOs

### What a DTO is and why it exists

A **DTO (Data Transfer Object)** is a simple data container that represents what the API sends or receives over the wire as JSON. DTOs deliberately differ from database entities in two important ways:

1. **They hide internal details.** For example, `ArticleSummaryDto` omits the `Content` field so that list responses are smaller. The `Article` entity always has `Content`, but not every API consumer needs it.
2. **They control the JSON shape.** The entity might have navigation properties (like `ArticleTags`) that would serialize to deeply nested JSON. The DTO flattens this into a simple `Tags: string[]`.

All DTOs for this project live in `src/WikiProject.Api/DTOs/ArticleDtos.cs`.

### The four DTO types and their roles

| DTO | Direction | Purpose |
|-----|-----------|---------|
| `ArticleDto` | Response | Full article including `Content` — used in detail views |
| `ArticleSummaryDto` | Response | Article without `Content` — used in list views to keep responses small |
| `CreateArticleRequest` | Request | Fields sent when creating a new article |
| `UpdateArticleRequest` | Request | Fields sent when updating an existing article |

`ArticleListResponse` is a wrapper that adds pagination metadata around a list of `ArticleSummaryDto`.

`ArticleQueryParams` is a record that models the query string parameters for the list endpoint. It is not JSON — it comes from the URL, not the request body.

### Step-by-step: adding a field to a response DTO

**Example goal:** Expose `AuthorName` in API responses after adding it to the entity (see Section 4).

#### Step 1 — Add the property to the DTO record

Open `src/WikiProject.Api/DTOs/ArticleDtos.cs`. The DTOs are C# `record` types, which means every property is declared as a constructor parameter:

```csharp
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
    DateTime UpdatedAt,
    string? AuthorName          // ← new
);
```

Do the same for `ArticleSummaryDto` if the field should appear in list responses too.

> **Beginner confusion — C# records:** Unlike a regular class, a `record` in C# treats all constructor parameters as its properties automatically. You define the shape entirely in the constructor signature. To add a property, you add a parameter. The order matters because callers that use positional construction (like `new ArticleDto(id, title, ...)`) break if you add parameters in the middle. Always add new parameters at the end.

#### Step 2 — Update the mapping

Open `src/WikiProject.Api/Mappings/ArticleMappings.cs`. The `ToDto()` extension method constructs the DTO from an `Article` entity. Add the new field:

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
        article.Status.ToString(),
        article.CreatedAt,
        article.UpdatedAt,
        article.AuthorName          // ← new
    );
```

Update `ToSummaryDto()` as well if you added `AuthorName` to `ArticleSummaryDto`.

#### Step 3 — Update the frontend TypeScript types

Open `frontend/src/types/index.ts` and add the field to the corresponding interfaces:

```typescript
export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;
  createdAt: string;
  updatedAt: string;
  authorName?: string;   // ← new (optional, matches C# nullable)
}
```

Because `Article extends ArticleSummary`, the field is automatically inherited by `Article`.

> **Beginner confusion:** TypeScript uses `?` to mark a property as optional (it may be `undefined`). This matches the C# `string?` nullable. If the backend sometimes omits the field (because it is null), TypeScript will not error when accessing it — you just have to handle the `undefined` case in your UI code.

### Step-by-step: adding a field to a request DTO

**Example goal:** Allow the `AuthorName` to be submitted when creating or editing an article.

#### Step 1 — Add to the request record

```csharp
public record CreateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status,
    string? AuthorName          // ← new
);
```

Repeat for `UpdateArticleRequest`.

#### Step 2 — Use the field in ArticleService.CreateAsync

In `ArticleService.CreateAsync`, assign the new field when constructing the entity:

```csharp
var article = new Article
{
    Title = request.Title.Trim(),
    Slug = slug,
    Summary = request.Summary.Trim(),
    Content = request.Content.Trim(),
    Category = request.Category.Trim(),
    Status = request.Status,
    AuthorName = request.AuthorName?.Trim(),   // ← new
    CreatedAt = now,
    UpdatedAt = now,
    ArticleTags = tags.Select(t => new ArticleTag { Tag = t }).ToList()
};
```

Do the same in `UpdateAsync`.

#### Step 3 — Update the frontend request type

```typescript
export interface CreateArticleRequest {
  title: string;
  slug?: string;
  summary: string;
  content: string;
  category: string;
  tags: string[];
  status: number;
  authorName?: string;   // ← new
}
```

#### Step 4 — Update the form (ArticleForm.tsx)

Add an input field to `frontend/src/components/ArticleForm.tsx` for the new property. Follow the existing pattern: a controlled `<input>` bound to a state variable, with an error display below it.

### Checklist: updating a DTO

- [ ] Response DTO record updated in `ArticleDtos.cs` (new parameter at the end)
- [ ] `ToDto()` mapping updated in `ArticleMappings.cs`
- [ ] `ToSummaryDto()` mapping updated if the field goes in list responses
- [ ] Request DTO updated if the field can be set via the API
- [ ] `ArticleService.CreateAsync` and `UpdateAsync` updated to use the new request field
- [ ] Validation rules added if needed (Section 6)
- [ ] Frontend `types/index.ts` updated
- [ ] Frontend form updated if the field is user-editable
- [ ] Project compiles without errors (`dotnet build` from `src/WikiProject.Api/`)

---

## 6. How to Update Validation

### Why validation matters here

Validation in WikiProject happens in two places:

1. **Frontend (ArticleForm.tsx)** — immediate feedback while the user types
2. **Backend (ArticleValidators.cs)** — authoritative enforcement, cannot be bypassed

**Both must be kept in sync.** If the backend rejects a value that the frontend allows, users will submit the form and see a confusing error they cannot explain. If the frontend is stricter than the backend, you are giving users misleading error messages.

### How backend validation works: FluentValidation

FluentValidation is a library that lets you express validation rules as code rather than annotations. Validators are separate classes, not mixed into the entity or DTO.

The two validators live in `src/WikiProject.Api/Validation/ArticleValidators.cs`:

```csharp
public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must be lowercase alphanumeric with hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));  // only validate if provided
        // ... etc.
    }
}
```

When a `POST /api/articles` request arrives, the controller explicitly calls the validator:

```csharp
var validation = await validator.ValidateAsync(request);
if (!validation.IsValid)
    return ValidationProblem(new ValidationProblemDetails(
        validation.ToDictionary()));
```

A validation failure returns HTTP `400 Bad Request` with a JSON body listing all the errors by field name.

### Step-by-step: adding a validation rule

**Example goal:** Validate that `AuthorName`, if provided, is at most 100 characters.

#### Step 1 — Add the rule to both validators

Open `ArticleValidators.cs`. Add the same rule to both `CreateArticleRequestValidator` and `UpdateArticleRequestValidator`:

```csharp
RuleFor(x => x.AuthorName)
    .MaximumLength(100).WithMessage("Author name must be 100 characters or fewer.")
    .When(x => !string.IsNullOrEmpty(x.AuthorName));
```

The `.When(...)` clause is important — it prevents the rule from firing on `null` or empty values (since `AuthorName` is optional).

#### Step 2 — Mirror the rule in the frontend

Open `frontend/src/components/ArticleForm.tsx`. Find the existing client-side validation (it is typically a `validate()` function or inline checks before `onSubmit`). Add the same constraint:

```typescript
if (authorName && authorName.length > 100) {
  errors.authorName = 'Author name must be 100 characters or fewer.';
}
```

### Step-by-step: adding a validator for a completely new request type

If you add a new request DTO (e.g., `DuplicateArticleRequest`):

1. Create a new class in `ArticleValidators.cs`:

```csharp
public class DuplicateArticleRequestValidator : AbstractValidator<DuplicateArticleRequest>
{
    public DuplicateArticleRequestValidator()
    {
        RuleFor(x => x.NewTitle)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

2. Register it in `Program.cs`:

```csharp
builder.Services.AddScoped<IValidator<DuplicateArticleRequest>, DuplicateArticleRequestValidator>();
```

3. Inject it in the controller action using `[FromServices]`:

```csharp
public async Task<ActionResult<ArticleDto>> Duplicate(
    int id,
    [FromBody] DuplicateArticleRequest request,
    [FromServices] IValidator<DuplicateArticleRequest> validator)
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
    // ...
}
```

> **Beginner confusion — `[FromServices]` vs. constructor injection:** Most dependencies are injected via the constructor. Validators are injected via `[FromServices]` on the action parameter so that each action only receives the validator it needs, rather than having the controller constructor take all possible validators.

### Checklist: updating validation

- [ ] Rule added to `CreateArticleRequestValidator` in `ArticleValidators.cs`
- [ ] Same rule added to `UpdateArticleRequestValidator`
- [ ] New validator registered in `Program.cs` if a new request type was added
- [ ] Equivalent check added to frontend `ArticleForm.tsx`
- [ ] Error messages in both places are consistent and user-friendly
- [ ] Tested by submitting a form that violates the rule

### Alternative approaches

FluentValidation is a popular choice in .NET but not the only option. The built-in `[Required]`, `[MaxLength]`, and `[RegularExpression]` data annotation attributes can be applied directly to DTO properties and validated automatically by ASP.NET Core's model binding. They are simpler for basic cases but harder to unit test and less expressive for conditional rules. The project chose FluentValidation for its readability and testability.

---

## 7. How to Extend Search and Filter Behaviour

### How the current search works

The search pipeline in `ArticleService.GetArticlesAsync` builds a LINQ query step by step:

```csharp
var q = _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .AsNoTracking()
    .AsQueryable();                         // start with all articles

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

if (!string.IsNullOrWhiteSpace(query.Category))
    q = q.Where(a => a.Category.ToLower() == query.Category.ToLower());

if (!string.IsNullOrWhiteSpace(query.Tag))
    q = q.Where(a => a.ArticleTags.Any(at => at.Tag.Name.ToLower() == query.Tag.ToLower()));

if (query.Status.HasValue)
    q = q.Where(a => a.Status == query.Status.Value);
```

Each condition narrows the result set. Conditions are only applied when the corresponding query parameter is non-null/non-empty.

### Step-by-step: adding a new filter parameter

**Example goal:** Add a filter for `AuthorName` so users can find articles by author.

#### Step 1 — Add the parameter to ArticleQueryParams

The query parameters record is in `ArticleDtos.cs`:

```csharp
public record ArticleQueryParams(
    string? Search = null,
    string? Category = null,
    string? Tag = null,
    ArticleStatus? Status = null,
    string? Author = null,          // ← new
    int Page = 1,
    int PageSize = 20
);
```

#### Step 2 — Apply the filter in ArticleService

In `GetArticlesAsync`, add a new filter block after the existing ones:

```csharp
if (!string.IsNullOrWhiteSpace(query.Author))
    q = q.Where(a => a.AuthorName != null &&
                     a.AuthorName.ToLower() == query.Author.ToLower());
```

#### Step 3 — Expose the parameter in the controller

In `ArticlesController.GetArticles`, add the new query string parameter:

```csharp
public async Task<ActionResult<ArticleListResponse>> GetArticles(
    [FromQuery] string? search,
    [FromQuery] string? category,
    [FromQuery] string? tag,
    [FromQuery] ArticleStatus? status,
    [FromQuery] string? author,          // ← new
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var query = new ArticleQueryParams(search, category, tag, status, author, page, pageSize);
    // ...
}
```

#### Step 4 — Update the frontend types and service

In `frontend/src/types/index.ts`:

```typescript
export interface ArticleFilters {
  search?: string;
  category?: string;
  tag?: string;
  status?: ArticleStatus;
  author?: string;         // ← new
  page?: number;
  pageSize?: number;
}
```

In `frontend/src/services/articleService.ts`, add the new filter to `buildParams`:

```typescript
function buildParams(filters: ArticleFilters): Record<string, string | number> {
  const params: Record<string, string | number> = {};
  if (filters.search)   params.search   = filters.search;
  if (filters.category) params.category = filters.category;
  if (filters.tag)      params.tag      = filters.tag;
  if (filters.status)   params.status   = filters.status;
  if (filters.author)   params.author   = filters.author;   // ← new
  if (filters.page)     params.page     = filters.page;
  if (filters.pageSize) params.pageSize = filters.pageSize;
  return params;
}
```

#### Step 5 — Add the filter control in ArticlesPage

In `frontend/src/pages/ArticlesPage.tsx`, add a state variable and input/dropdown for the new filter, following the same pattern as the existing category and tag filters.

### Adding a new search field

If you want the full-text search to also include a new field (like `AuthorName`), extend the `Where` clause in the search block:

```csharp
q = q.Where(a =>
    a.Title.ToLower().Contains(search) ||
    a.Summary.ToLower().Contains(search) ||
    a.Content.ToLower().Contains(search) ||
    a.Category.ToLower().Contains(search) ||
    (a.AuthorName != null && a.AuthorName.ToLower().Contains(search)) ||  // ← new
    a.ArticleTags.Any(at => at.Tag.Name.ToLower().Contains(search)));
```

### Sorting

Currently all results are sorted by `UpdatedAt DESC` (most recently updated first). To make the sort order configurable, add a `SortBy` parameter to `ArticleQueryParams` and a `switch` expression in `GetArticlesAsync` before the `OrderByDescending` call.

### Performance note

The current full-text search uses SQL `LIKE` via `Contains()`, translated by EF Core. For large datasets this becomes slow because the database cannot use standard indexes for substring searches. SQLite supports a built-in Full-Text Search extension (FTS5). Migrating to FTS5 would require a significant refactor of the search logic but would dramatically improve performance at scale.

### Checklist: adding a new filter

- [ ] Parameter added to `ArticleQueryParams` record
- [ ] Filter clause added to `GetArticlesAsync` in `ArticleService`
- [ ] Parameter added to the controller action's `[FromQuery]` parameters
- [ ] `ArticleQueryParams` constructor call in the controller updated
- [ ] `ArticleFilters` interface updated in `frontend/src/types/index.ts`
- [ ] `buildParams` function updated in `articleService.ts`
- [ ] Filter control added to `ArticlesPage.tsx`
- [ ] Tested via Swagger and the UI

---

## 8. How to Safely Change a Domain Model

### What "domain model" means here

The **domain model** is the set of C# entity classes that represent the core concepts of the application: `Article`, `Tag`, `ArticleTag`, and the `ArticleStatus` enum. These classes directly drive the database schema (via EF Core) and the shape of data throughout the backend.

Changing a domain model has cascading effects — it touches the database, the service layer, the DTOs, the mappings, the validators, and potentially the frontend. It requires the most care of all the changes described in this guide.

### Types of domain model changes and their risk levels

| Change Type | Risk | Notes |
|-------------|------|-------|
| Add nullable property to entity | Low | Does not break existing data |
| Add non-nullable property | Medium | Requires migration with default value |
| Rename a property | High | Must update all references; consider renaming DB column separately |
| Remove a property | High | Data loss in DB; must remove all references first |
| Add a new entity | Medium | Needs DbSet, migration, potential relationship config |
| Add a new enum value | Low | Existing values are preserved; add at the end |
| Change an enum value's integer | High | Existing stored integers become wrong |
| Add a relationship (FK) | Medium | Needs migration; affects loading queries |
| Change a relationship | High | Often requires dropping and re-creating constraints |

### Safe process for renaming a property

**Never** simply change a property name in the entity class without coordinating all references. EF Core will see the old column as "removed" and the new name as "added" — resulting in data loss.

Safe rename process:
1. Add the new property alongside the old one
2. Create a migration that copies data from the old column to the new one
3. Deploy and verify
4. Remove the old property and create a second migration to drop the old column

For a development database with no real data, it is acceptable to delete `wiki.db`, change the entity, delete all migration files, and run `dotnet ef migrations add InitialCreate` to regenerate from scratch. **Never do this with a database that has real data.**

### Safe process for adding a new relationship

**Example goal:** Add an `Author` entity so articles can reference a named author as a proper database entity rather than a string.

1. Create `src/WikiProject.Api/Entities/Author.cs`
2. Add `public DbSet<Author> Authors => Set<Author>();` to `WikiDbContext`
3. Configure the relationship in `OnModelCreating` if needed (optional FK, cascade behaviour)
4. Add `public int? AuthorId { get; set; }` and `public Author? Author { get; set; }` to `Article`
5. Run `dotnet ef migrations add AddAuthorEntity`
6. Update `GetArticlesAsync` and other queries to `.Include(a => a.Author)` if the author should be eager-loaded
7. Update DTOs and mappings to expose author information
8. Update validators if author fields are required

### Changing the ArticleStatus enum

`ArticleStatus` is stored as an integer in the database (0 = Draft, 1 = Published, 2 = Archived). If you add a new status value, always **add it at the end** of the enum definition with the next integer. If you reorder or reassign integers, existing stored values will map to the wrong status — a silent, dangerous bug.

Correct:
```csharp
public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
    UnderReview = 3     // ← safe: new value at the end (PascalCase — C# identifiers cannot contain spaces)
}
```

Dangerous:
```csharp
public enum ArticleStatus
{
    Draft = 0,
    UnderReview = 1,    // ← dangerous: renumbered Published and Archived
    Published = 2,
    Archived = 3
}
```

After adding a new status value, you must also:
- Update the `ArticleStatus` type in `frontend/src/types/index.ts`
- Add the new option to any frontend dropdowns that display statuses

### Checklist: changing a domain model

- [ ] Impact assessment done (list all files that reference the property being changed)
- [ ] Entity class updated
- [ ] Migration generated and reviewed
- [ ] Existing data handled (nullable, default value, or data migration)
- [ ] Service layer updated (queries, create/update logic)
- [ ] DTOs updated (Section 5)
- [ ] Mappings updated
- [ ] Validators updated (Section 6)
- [ ] Frontend types updated
- [ ] Frontend form and display components updated
- [ ] Verified: app starts without errors
- [ ] Verified: existing data is correct after migration

---

## 9. Where Regressions Are Likely to Happen

A **regression** is when a change that was intended to add or improve a feature accidentally breaks existing functionality. The following areas are where regressions most commonly occur in this codebase.

### 9.1 The Slug Uniqueness Logic

**File:** `ArticleService.EnsureUniqueSlugAsync`

The slug uniqueness check uses a loop that keeps appending `-{counter}` until it finds an unused slug. If you change the `GenerateSlug` function or the uniqueness check, you risk:
- Generating slugs that exceed the 200-character limit
- Allowing duplicate slugs to exist in the database (breaking the unique index)
- The counter logic running infinitely if the condition is accidentally inverted

**Regression signal:** An HTTP 500 on article create/update, or a database constraint violation.

### 9.2 Tag Resolution

**File:** `ArticleService.ResolveTagsAsync`

This method normalises tag names (trim, lowercase, distinct), retrieves existing tags, and creates new ones. A regression here could:
- Create duplicate tags with slightly different capitalisation (e.g., `"Database"` and `"database"`)
- Fail to associate existing tags (so the same tag appears twice in the DB)
- Cause `SaveChangesAsync` to throw due to a unique constraint violation on `Tag.Name`

**Regression signal:** Tags not appearing on articles, or constraint violation errors when creating articles with tags that already exist.

### 9.3 The Article-Tag Many-to-Many Update

**File:** `ArticleService.UpdateAsync`

When updating an article, the existing tags are cleared and replaced:
```csharp
article.ArticleTags.Clear();
foreach (var tag in tags)
    article.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, Tag = tag });
```

This works because EF Core tracks the change and generates the correct `DELETE` and `INSERT` statements. If you modify this logic — for example, trying to do a "smart diff" between old and new tags — you must ensure you do not accidentally create orphaned `ArticleTag` rows or violate the composite primary key constraint.

**Regression signal:** Tags not updating correctly on edit, duplicate key errors.

### 9.4 DTO Record Parameter Order

**File:** `ArticleDtos.cs` and `ArticleMappings.cs`

C# `record` types use positional constructor syntax. When you add a parameter, the `ToDto()` method in `ArticleMappings.cs` must be updated to pass the new value. If you add a parameter to the record but forget to add the corresponding argument to the mapping, the compiler will catch it — **but only if the mapping uses positional construction.** If the existing mapping accidentally passes the same count of arguments in the wrong order, the compiler will not catch a type mismatch between fields of the same type (e.g., two `string` fields swapped).

**Regression signal:** Data appearing in the wrong fields in API responses (e.g., `Summary` showing `Category` content).

### 9.5 Frontend Type Synchronisation

**File:** `frontend/src/types/index.ts`

TypeScript's type system only checks types at compile time. If the backend starts returning a new field that the frontend `interface` does not declare, TypeScript will not error — the field simply does not exist from TypeScript's perspective. The reverse (TypeScript expects a field the backend no longer returns) will cause `undefined` access errors at runtime.

**Regression signal:** Data not displaying in the UI with no console errors (frontend has old type that does not include the new field), or `undefined` errors in the browser console (frontend expects a field that no longer exists).

### 9.6 Filter Logic in GetArticlesAsync

**File:** `ArticleService.GetArticlesAsync`

Each filter condition is applied with `.Where()` which narrows the result set. Mistakes here have broad impact because they affect **every page load** that uses the articles list:
- An incorrect condition could return zero results for all queries
- A missing `null` check could cause a null reference exception for every request

**Regression signal:** Articles list suddenly empty, or HTTP 500 on any articles request.

### 9.7 CORS Configuration

**File:** `Program.cs`

The CORS policy allows requests from `http://localhost:5173` (the Vite dev server). If you add a new allowed origin incorrectly (e.g., with a trailing slash), the browser will block requests with an opaque CORS error. This does not break the API itself but breaks the entire frontend.

**Regression signal:** All API calls fail in the browser with "CORS policy" errors; calls from Swagger (same origin as the API) still work.

### 9.8 Migration Conflicts

**File:** `Migrations/` directory

If two developers both add migrations independently from the same base migration, the migration chain becomes forked. EF Core will refuse to apply the migrations. This requires manually resolving the conflict by squashing or reordering the migrations.

**Regression signal:** `dotnet ef database update` fails with "migration conflict" or similar error.

---

## 10. How to Test Changes

### Current state of testing

At the time this guide was written, the project has **no automated tests**. Testing is listed as a Phase 5 stretch goal in `STARTING_TASKS.md`. The planned test stack is:
- **Backend:** xUnit for unit tests, plus EF Core In-Memory provider for integration tests
- **Frontend:** Vitest + React Testing Library for component tests

Because there are no tests yet, all verification must be done manually. The sections below explain how to do this systematically.

### Manual testing workflow for backend changes

#### Using Swagger UI

The Swagger UI at `http://localhost:5018/swagger` is the fastest way to test individual endpoints without writing any code.

1. Start the backend: `dotnet run` from `src/WikiProject.Api/`
2. Open `http://localhost:5018/swagger` in a browser
3. Find your endpoint in the list
4. Click "Try it out"
5. Fill in the parameters and request body
6. Click "Execute"
7. Inspect the response code and body

Swagger is useful for:
- Verifying that a new endpoint appears and is correctly documented
- Testing happy-path requests (valid data that should succeed)
- Testing error cases (invalid data, missing required fields, non-existent IDs)

#### Using curl or a REST client

For more complex scenarios (e.g., testing sequences of requests, or testing with specific headers), use `curl` or a REST client like Postman or Bruno.

Example: test the article list endpoint with multiple filters:
```bash
curl "http://localhost:5018/api/articles?search=database&status=Published&page=1&pageSize=5"
```

#### Testing database changes

After applying a migration:
1. Start the app — it should start without errors and log "Applying migrations..."
2. Open Swagger and create a new article
3. Check that the new field is present in the response
4. If the field is optional, verify that articles created before the migration (in seed data) return `null` for the new field rather than crashing

### Manual testing workflow for frontend changes

1. Start the backend: `dotnet run` from `src/WikiProject.Api/`
2. Start the frontend: `npm run dev` from `frontend/`
3. Open `http://localhost:5173` in a browser
4. Open the browser's developer tools (F12) → Network tab
5. Navigate to the page you changed
6. Verify the API calls appear in the Network tab and return expected data
7. Verify the UI renders the data correctly
8. Test edge cases: empty results, long text, special characters

### Mental testing checklist

Before pushing a change, walk through these questions mentally:

**For a new database field:**
- What happens when the field is `null`? Does the frontend show "null" or handle it gracefully?
- What happens when the field contains very long text? Does it overflow the UI?
- What happens when the field contains special characters like `<script>` or `"quotes"`?

**For a new endpoint:**
- What does the endpoint return when the resource does not exist? `404` or `500`?
- What does the endpoint return when given invalid input? `400` with a helpful message?
- What does the endpoint return when given input designed to cause an error (e.g., an ID of -1, a title of 10,000 characters)?

**For a filter change:**
- Does the filter return the correct results when active?
- Does the filter return all results when cleared/inactive?
- Does combining multiple filters work correctly (AND logic)?

**For a frontend page:**
- Does the page display correctly when the API returns data?
- Does the page display a loading spinner while waiting?
- Does the page display an error message if the API call fails?
- Does the page display an empty-state message if the API returns zero results?
- Does the page work correctly at small screen widths (open browser DevTools → responsive mode)?

### When automated tests are added (future)

When the project moves to Phase 5 and adds automated tests, the recommended approach is:

**Backend unit tests (xUnit):**
- Test `GenerateSlug` with various inputs (spaces, special characters, very long titles)
- Test `EnsureUniqueSlugAsync` with an in-memory database
- Test validator classes directly (no HTTP required)
- Test `ResolveTagsAsync` to verify tag normalisation and deduplication

**Backend integration tests (xUnit + EF Core In-Memory or SQLite in-memory):**
- Test full request cycles through the controller → service → DB
- Test that creating an article with duplicate slug generates a unique slug
- Test that updating an article replaces tags correctly

**Frontend tests (Vitest + React Testing Library):**
- Test `ArticleForm` renders validation errors
- Test `useArticles` hook returns correct state transitions (loading → data)
- Test `ArticlesPage` renders articles and handles empty state

---

## 11. Safe Workflow for Beginners

### The general principle: smallest safe change

Every change you make should be the **smallest possible change that accomplishes the goal**. Large changes are harder to debug when something goes wrong. If you are adding a new feature, break it into multiple small commits:

1. Add the database field and migration
2. Add the DTO and mapping
3. Add the service method
4. Add the controller endpoint
5. Add the frontend type and service call
6. Add the frontend page or component

Each step should be independently verifiable and reversible.

### Start the app before and after each change

Before making a change, confirm the app starts and works. After making a change, confirm it still starts and works. This narrows down exactly which change introduced a problem.

```bash
# Terminal 1: Backend
cd src/WikiProject.Api
dotnet run

# Terminal 2: Frontend
cd frontend
npm run dev
```

### Build the backend after C# changes

TypeScript catches type errors in the browser console. C# catches errors at build time. After changing any backend file, build it before running:

```bash
cd src/WikiProject.Api
dotnet build
```

A successful build does not guarantee correct behaviour, but it confirms there are no syntax or type errors.

### Use version control as a safety net

Before making a significant change, create a git commit with the current working state. This gives you a checkpoint to roll back to if something goes wrong:

```bash
git add .
git commit -m "checkpoint: before adding AuthorName field"
```

### Reset the database safely during development

If a migration goes wrong or you want to start fresh:

1. Stop the backend
2. Delete `src/WikiProject.Api/wiki.db`
3. If you also want to reset migrations, delete all files in `src/WikiProject.Api/Migrations/` and run:
   ```bash
   dotnet ef migrations add InitialCreate
   ```
4. Restart the backend — it will create a fresh database with seed data

> **This is safe only for development.** Never delete a production database.

### How to approach an unfamiliar change

When you need to make a change you have not made before:

1. **Find an existing example.** The codebase follows consistent patterns. If you need to add a filter, find how the existing `Category` filter works and mirror it.
2. **Make one small change and verify it compiles.**
3. **Run the app and verify the change works in isolation before adding more.**
4. **Read the error message carefully.** Most errors in this stack are descriptive. EF Core errors often include the exact table name and column. FluentValidation errors include the field name and the violated rule.

### Understanding startup logs

When you run `dotnet run`, the console output includes useful information:

```
info: Microsoft.EntityFrameworkCore.Database.Command[...]
      Executed DbCommand (2ms) [...] CREATE TABLE IF NOT EXISTS "Articles" (...)
```

This tells you EF Core applied a migration. If migrations fail, the app will crash here with a descriptive error.

```
info: WikiProject.Api.Data.SeedData[...]
      Seeding initial data...
```

This tells you the seed data ran. If it fails, you may have a constraint violation in `SeedData.cs`.

```
info: Microsoft.Hosting.Lifetime[...]
      Now listening on: http://localhost:5018
```

This tells you the app is running and ready for requests.

### Summary: the safe beginner workflow for any change

1. Identify all the files in the chain that the change touches (use the table in Section 1)
2. Make the smallest change to the innermost layer first (entity, then DB migration)
3. Compile and verify no build errors
4. Work outward layer by layer (service, controller, DTO, mapping, validator)
5. Compile again after each layer
6. Start the backend, verify in Swagger
7. Update the frontend types and service
8. Update the frontend page or component
9. Start the frontend, verify in the browser
10. Review the full change against the relevant checklist in this guide

---

*This document covers the practical mechanics of extending and maintaining the WikiProject codebase. For the overall architecture design, see a future Architecture Guide. For the initial project setup and development environment, see the project README.*
