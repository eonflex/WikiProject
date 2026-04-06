# Domain Model and Data Flow

## Table of Contents

1. [Introduction and Purpose](#introduction-and-purpose)
2. [The MVC Pattern and WikiProject](#the-mvc-pattern-and-wikiproject)
3. [Core Domain Concepts](#core-domain-concepts)
   - [Article](#article)
   - [Category](#category)
   - [Tag](#tag)
   - [ArticleStatus](#articlestatus)
   - [Slug](#slug)
   - [Metadata: CreatedAt and UpdatedAt](#metadata-createdat-and-updatedat)
4. [Relationships Between Concepts](#relationships-between-concepts)
5. [How Concepts Are Represented in Code and Data](#how-concepts-are-represented-in-code-and-data)
   - [Database Layer](#database-layer)
   - [EF Core Entities](#ef-core-entities)
   - [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
   - [Service Layer Objects](#service-layer-objects)
   - [API Response JSON](#api-response-json)
   - [Frontend TypeScript Types](#frontend-typescript-types)
   - [Rendered UI](#rendered-ui)
6. [Search and Filtering Concepts](#search-and-filtering-concepts)
7. [End-to-End Example: One Article Through the Full Stack](#end-to-end-example-one-article-through-the-full-stack)
   - [Creating an Article (POST)](#creating-an-article-post)
   - [Reading an Article (GET)](#reading-an-article-get)
   - [Updating an Article (PUT)](#updating-an-article-put)
8. [Why Data Transformations Happen Between Layers](#why-data-transformations-happen-between-layers)
9. [What Bugs This Separation Helps Avoid](#what-bugs-this-separation-helps-avoid)
10. [Where Serialization Happens](#where-serialization-happens)
11. [How Validation Protects the System](#how-validation-protects-the-system)
12. [How Updates Propagate Through the Stack](#how-updates-propagate-through-the-stack)
13. [Summary and Next Steps](#summary-and-next-steps)

---

## Introduction and Purpose

WikiProject is an internal knowledge base application. Its central job is to store, organize, and surface written documents called **articles**. Everything the application does — create, display, search, filter, and edit — revolves around article data.

This document answers three interconnected questions:

1. **What** are the core data concepts (the "things" the application models)?
2. **Why** do they exist — what problem does each concept solve?
3. **How** does a piece of data travel from the user's browser, through every layer of the stack, and back again?

Understanding these questions gives you a mental map of the whole system. Once you can trace a single article from the database row all the way to the rendered HTML element, navigating any part of the code becomes much easier.

> **Note for the reader:** This document assumes you have a general understanding of how web applications work (browser → server → database) but does not assume deep knowledge of any specific framework. Technical terms are defined the first time they appear.

---

## The MVC Pattern and WikiProject

**MVC** stands for **Model-View-Controller**. It is one of the most widely used design patterns in web development. Understanding it unlocks a lot of vocabulary you will encounter in documentation, tutorials, and code reviews — so it is worth learning here before diving into the specific domain objects.

The core idea is to separate an application into three distinct roles:

- **Model** — the data and the rules that govern it. What does an article look like? What fields does it have? What constraints apply? How is it stored and retrieved?
- **View** — the presentation. What does the user see? How is the data rendered to the screen?
- **Controller** — the intermediary. It receives user input (an HTTP request), asks the Model for data or instructs it to change, then returns a result to the View (or the API caller).

This separation of concerns is valuable because it prevents any one piece of code from knowing too much. A View should not directly query a database. A Model should not know what color its text is rendered in. A Controller should not contain complex business rules.

### How WikiProject Maps to MVC

WikiProject uses ASP.NET Core on the backend and React on the frontend — each with their own conventions — but the three MVC roles are clearly visible across the stack:

| MVC Role | WikiProject Representation | Location |
|----------|---------------------------|----------|
| **Model** | `Article`, `Tag`, `ArticleTag`, `ArticleStatus` entity classes | `src/WikiProject.Api/Entities/` |
| **Controller** | `ArticlesController`, `MetadataController` | `src/WikiProject.Api/Controllers/` |
| **View** | React page and component files | `frontend/src/pages/`, `frontend/src/components/` |

#### The Model — `Article`, `Tag`, and Friends

The domain objects described in [Core Domain Concepts](#core-domain-concepts) below are the **Models** of this application. In MVC terminology, a Model represents:

- The **shape of the data** — what fields an article has, what types they are
- The **business rules** — a slug must be unique; a status must be Draft, Published, or Archived; timestamps must be set by the server
- The **persistence mapping** — how EF Core translates C# properties into database rows and columns

In WikiProject, the C# entity classes (`Article.cs`, `Tag.cs`, `ArticleTag.cs`) are the canonical Models. When tutorials or documentation say "define your Model," they mean exactly these kinds of classes. The DTOs (`ArticleDto`, `CreateArticleRequest`, etc.) are closely related but serve a different role — they define the **shape of the API contract** rather than the shape of the stored data. See [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos) for more on this distinction.

#### The Controller — `ArticlesController`

`ArticlesController` is the **Controller** in MVC. It:

1. Receives an incoming HTTP request (e.g., `POST /api/articles`)
2. Validates the input using FluentValidation
3. Delegates business logic to the service layer, which works with the Models
4. Returns an HTTP response with the result

Controllers in WikiProject do not contain business logic themselves — they hand off to `ArticleService`. This is a refinement of classic MVC sometimes called the **service layer pattern** or **thin controller** approach: keep controllers lean and push logic into dedicated service classes.

#### The View — React Components

On the frontend, React components like `ArticleCard`, `ArticleDetailPage`, and `ArticlesPage` are the **Views** in MVC. They receive data and render HTML for the browser. They do not fetch data directly from the database — they receive it through the API, just as a traditional View receives it from a Controller.

> **Common beginner confusion:** In classic server-rendered MVC frameworks (like ASP.NET Core MVC with Razor Pages, or Ruby on Rails), the Controller directly populates the View in the same process on the same server. In WikiProject's architecture, the backend Controller speaks JSON over HTTP to the frontend, and the React View is a separate JavaScript application running in the browser. This is called an **API-first** or **SPA + API** (Single Page Application + API) architecture. The MVC roles still apply, but they now span a network boundary. The benefit is that the same API can serve mobile apps, third-party integrations, or future clients — not just the React frontend.

### Why This Pattern Matters

When you open any file in this project, knowing its MVC role tells you immediately what it should and should not do:

- If you are in `Article.cs` (Model), you are looking at data structure. No HTTP, no rendering.
- If you are in `ArticlesController.cs` (Controller), you are handling HTTP routing and delegating to services. No complex SQL, no HTML.
- If you are in `ArticleDetailPage.tsx` (View), you are rendering UI and handling user events. No direct DB calls.

Violations of these boundaries are a common source of bugs and maintainability problems in larger codebases.

---

## Core Domain Concepts

A **domain** is the real-world subject area an application works with. The domain here is "a knowledge base full of team documentation." Domain concepts are the things that matter to users: articles, categories, tags, and so on. In MVC terms, these are your **Models**.

### Article

An **article** is the primary unit of content — and the central **Model** of this application. It is a document written by a team member to capture knowledge — instructions, reference material, architectural notes, or how-to guides.

An article has these properties:

| Property    | Purpose |
|-------------|---------|
| `Id`        | Internal database identifier. An auto-incrementing integer. Never shown to users, used by the system to identify records unambiguously. |
| `Title`     | Human-readable name of the article. Required. Max 200 characters. |
| `Slug`      | A URL-safe shorthand for the title (see [Slug](#slug) section below). |
| `Summary`   | A one-or-two-sentence description shown in article lists so readers can decide whether to read the full article. Max 500 characters. |
| `Content`   | The full body of the article, stored as plain text intended to be Markdown. There is no maximum length enforced by the application. |
| `Category`  | A broad grouping string (see [Category](#category) below). |
| `Tags`      | A set of keyword labels (see [Tag](#tag) below). |
| `Status`    | Lifecycle state of the article: Draft, Published, or Archived (see [ArticleStatus](#articlestatus)). |
| `CreatedAt` | UTC timestamp set when the article is first saved. |
| `UpdatedAt` | UTC timestamp updated every time the article is modified. |

#### Why this matters

Every other concept in the system either describes an article, organizes articles, or helps users find articles. If you understand the `Article` concept completely, you can understand the purpose of everything else.

---

### Category

A **category** is a free-form string that provides the broadest grouping for an article.

Examples from the seed data: `"General"`, `"Reference"`, `"DevOps"`, `"Architecture"`.

#### Why categories exist

Users need a way to quickly narrow down a long list of articles without reading every title. Categories provide a top-level filter — "show me only architecture-related articles."

#### How categories are stored

This is an important design choice: **categories are not stored in a separate table**. Each `Article` row has a plain `Category` text column. There is no `Categories` table, no foreign key, and no normalization.

**Implication:** The same logical category can be spelled differently across articles. If one article says `"devops"` and another says `"DevOps"`, they are technically different categories. The service layer handles this by comparing case-insensitively when filtering, and the `GetCategoriesAsync()` method returns distinct values sorted alphabetically. But a typo at write time will create a new category that users see in the filter dropdown.

**Why this design was chosen:** Simplicity. A full `Categories` table with foreign keys adds complexity (extra migration, extra UI to manage categories, enforcement logic). For a small team knowledge base where categories are relatively stable and maintainable, storing the string directly is a pragmatic choice. The tradeoff is the typo risk described above.

> **Common beginner confusion:** Beginners sometimes expect every concept to have its own table. Denormalized string columns are a legitimate and often correct choice for attributes that are semi-structured and low-cardinality. The "right" answer depends on whether you need to rename a category everywhere at once, enforce uniqueness, or maintain referential integrity — none of which WikiProject currently needs.

**Alternative approaches:**
- A separate `Categories` table with a foreign key on `Article`. This enforces spelling consistency and lets you rename a category in one place.
- An enum, similar to `ArticleStatus`. This enforces a strict fixed set of categories, which is too rigid for a knowledge base.

---

### Tag

A **tag** is a short keyword label that can be attached to an article. Unlike categories (one per article, broad), tags are many-per-article and more specific.

Examples from the seed data: `"getting-started"`, `"api"`, `"database"`, `"architecture"`, `"guide"`, `"tips"`, `"reference"`.

#### Why tags exist

Tags solve a problem categories cannot: a single article can belong to multiple subjects. An article about "Setting Up the Database for CI/CD" belongs to both the `database` and `devops` conceptual areas. Using tags, it can be labeled with both.

#### How tags are stored

Tags are stored in a dedicated `Tags` table and linked to articles via a **join table** called `ArticleTags`. This is a standard **many-to-many relationship** pattern:

- One article can have many tags.
- One tag can be on many articles.
- The `ArticleTags` table records each (article, tag) pair.

```
Articles          ArticleTags          Tags
---------         -----------          ----
Id (PK)  <------- ArticleId (FK)       Id (PK)
Title             TagId (FK)   ------> Name
...               [composite PK]
```

Tag names are normalized: always stored in **lowercase** and **trimmed** of whitespace. This prevents duplicate tags like `"API"` and `"api"` from coexisting.

The `Tags` table has a unique index on `Name`, enforced both at the database level (SQLite unique index) and logically in the service layer.

> **Common beginner confusion:** Students often wonder why there is an `ArticleTags` table rather than just storing tag names as a comma-separated string in the `Article` row. The comma-separated approach is simpler to implement but makes it impossible to query efficiently ("find all articles tagged 'api'" requires scanning and splitting every row's text). A proper join table enables exact lookups and proper indexing.

**Alternative approaches:**
- Comma-separated tags in a text column (simpler, but poor query performance and no referential integrity).
- Arrays in PostgreSQL (native array column + GIN index). Not available in SQLite.
- A tag cloud or taxonomy system with hierarchical tags (overkill for this use case).

---

### ArticleStatus

`ArticleStatus` is an **enum** (short for enumeration) — a type that can only hold one of a fixed set of named values. The three allowed values are:

| Name        | Integer Value | Meaning |
|-------------|--------------|---------|
| `Draft`     | `0`          | Work in progress. The author is still writing or reviewing. |
| `Published` | `1`          | Complete and ready for the team to read. |
| `Archived`  | `2`          | No longer active or relevant, but preserved for historical reference. |

#### Why status exists

A simple boolean "published yes/no" is too coarse. Authors need a place to park work in progress (`Draft`) without exposing it to readers, and a way to retire old content without deleting it (`Archived`). Status provides this lifecycle control.

#### How status is stored

In the **database**, status is stored as an integer (`0`, `1`, or `2`). This is the default EF Core behavior for C# enums: they are stored as their underlying numeric value.

In the **API response JSON** and the **frontend TypeScript types**, status is represented as a **string** (`"Draft"`, `"Published"`, `"Archived"`). This conversion happens in the mapping layer (see [Data Transfer Objects](#data-transfer-objects-dtos) below).

> **Common beginner confusion:** The backend stores `0/1/2` but the frontend receives and works with `"Draft"/"Published"/"Archived"`. When the frontend *sends* a create or update request, it sends a *number* (`status: 0`), not a string. This asymmetry is intentional: numbers are more compact on the wire and less error-prone than free-text strings for write operations. The string representation in responses is for readability. See [Where Serialization Happens](#where-serialization-happens) for exactly where this conversion occurs.

**Alternative approaches:**
- Store status as a string in the database. Easier to read in a DB browser, but wastes space and loses the guarantee that only valid values are stored.
- Use a `Statuses` lookup table (like a "reference data" table). More normalized, but overkill for a small fixed enum.

---

### Slug

A **slug** is a URL-friendly identifier derived from an article's title. It consists only of lowercase letters, numbers, and hyphens — no spaces, no special characters.

**Examples (auto-generated from title):**
- Title: `"Welcome to WikiProject"` → Auto-slug: `"welcome-to-wikiproject"`
- Title: `"API Reference Overview"` → Auto-slug: `"api-reference-overview"`
- Title: `"Setting Up the Development Environment"` → Auto-slug: `"setting-up-the-development-environment"`

> **Note:** The seed data (`SeedData.cs`) provides slugs explicitly for each seeded article. For example, the seed article "Setting Up the Development Environment" uses the manually shortened slug `"setting-up-dev-environment"` rather than the auto-generated `"setting-up-the-development-environment"`. Both are valid slugs — the auto-generation algorithm only runs when the author leaves the slug field blank.

#### Why slugs exist

Slugs solve two problems:

1. **Human-readable URLs**: `/articles/slug/welcome-to-wikiproject` is far more informative than `/articles/42`. If you share a link, the URL itself conveys context.
2. **Stable identifiers**: Auto-increment IDs depend on insertion order and can expose business information (e.g., total number of articles). A slug is explicitly chosen and stable.

#### How slugs are generated

When creating an article, the author can optionally supply a slug. If they do not, the service generates one automatically using this algorithm (from `ArticleService.GenerateSlug`):

```csharp
private static string GenerateSlug(string title)
{
    var slug = title.ToLower().Trim();
    // Remove anything that is not a letter, digit, space, or hyphen
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    // Replace one or more spaces with a single hyphen
    slug = Regex.Replace(slug, @"\s+", "-");
    // Collapse multiple consecutive hyphens into one
    slug = Regex.Replace(slug, @"-+", "-");
    // Remove hyphens from the start and end
    slug = slug.Trim('-');
    // Truncate to 100 characters to avoid excessively long URLs
    return slug.Length > 100 ? slug[..100] : slug;
}
```

#### Slug uniqueness

Because slugs are used in URLs, they must be globally unique. The database enforces this with a unique index:

```csharp
// WikiDbContext.cs
modelBuilder.Entity<Article>()
    .HasIndex(a => a.Slug)
    .IsUnique();
```

The service layer also proactively ensures uniqueness *before* writing to the database, to avoid confusing SQL constraint violation errors:

```csharp
private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludeId = null)
{
    var slug = baseSlug;
    var counter = 1;
    while (await _db.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId)))
    {
        slug = $"{baseSlug}-{counter}";
        counter++;
    }
    return slug;
}
```

If `"getting-started"` is already taken, the new article gets `"getting-started-1"`, then `"getting-started-2"`, and so on.

**The `excludeId` parameter** handles a subtle edge case: when *updating* an article, you want to check uniqueness but not flag the article's own current slug as a conflict. Without `excludeId`, updating an article without changing its slug would incorrectly fail the uniqueness check.

#### Slug validation

If a user manually supplies a slug, the validator enforces the format:

```csharp
// ArticleValidators.cs
RuleFor(x => x.Slug)
    .MaximumLength(200)
    .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
    .When(x => !string.IsNullOrEmpty(x.Slug));
```

The regex `^[a-z0-9]+(?:-[a-z0-9]+)*$` means: one or more lowercase alphanumeric characters, optionally followed by groups of (hyphen + alphanumeric). This allows `"my-slug"` but rejects `"My Slug"`, `"my_slug"`, or `"-slug"`.

---

### Metadata: CreatedAt and UpdatedAt

`CreatedAt` and `UpdatedAt` are **UTC timestamps** (Coordinated Universal Time — a timezone-free standard time reference) stored on every article.

| Field       | Set when?                           | Changed after creation? |
|-------------|-------------------------------------|------------------------|
| `CreatedAt` | Article is first saved to DB        | No                     |
| `UpdatedAt` | Article is created or updated       | Yes, on every update   |

#### Why metadata exists

- `CreatedAt`: Tells you when an article first appeared. Useful for audit trails and sorting by "newest."
- `UpdatedAt`: Tells you how fresh the content is. The default sort order in the articles list is by `UpdatedAt` descending — the most recently modified articles appear first. This helps team members see what has changed recently.

#### Where the timestamps are set

Timestamps are set in the **service layer**, not the database and not the client:

```csharp
// ArticleService.cs — CreateAsync
var now = DateTime.UtcNow;
var article = new Article
{
    ...
    CreatedAt = now,
    UpdatedAt = now,
};
```

```csharp
// ArticleService.cs — UpdateAsync
article.UpdatedAt = DateTime.UtcNow;
```

> **Why not let the database set timestamps?** SQLite does not have a built-in `DEFAULT CURRENT_TIMESTAMP` for ORM-managed models in this setup, and relying on the database would require more complex EF Core configuration. Letting the server code set the timestamp is the simpler, more portable choice. The tradeoff is that you need to remember to set the timestamp in every code path that saves data — a bug risk for complex systems, but manageable here.

> **Why UTC?** Storing UTC avoids ambiguity from timezones and Daylight Saving Time. The frontend converts UTC strings to local time for display using JavaScript's `Date` API.

---

## Relationships Between Concepts

Here is how all the concepts connect:

```
                ┌─────────────────────────────┐
                │           Article           │
                │  - Title (string)           │
                │  - Slug (string, unique)    │
                │  - Summary (string)         │
                │  - Content (string)         │
                │  - Category (string)        │  ← embedded, not a FK
                │  - Status (enum: 0/1/2)     │
                │  - CreatedAt (UTC DateTime) │
                │  - UpdatedAt (UTC DateTime) │
                └──────────────┬──────────────┘
                               │ one article
                               │ has many ArticleTags
                               │
                ┌──────────────▼──────────────┐
                │          ArticleTag          │
                │  - ArticleId (FK → Article) │
                │  - TagId (FK → Tag)         │
                └──────────────┬──────────────┘
                               │ many ArticleTags
                               │ point to one Tag
                               │
                ┌──────────────▼──────────────┐
                │             Tag             │
                │  - Id (int, PK)             │
                │  - Name (string, unique)    │
                └─────────────────────────────┘
```

**Key observations:**
- `Category` is a property *of* Article, not a separate entity.
- Tags are separate entities connected via the `ArticleTags` join table.
- `ArticleStatus` is an enum embedded in the `Article` entity.
- `Slug` is a string property on `Article` with a unique database index.

---

## How Concepts Are Represented in Code and Data

The same article exists in multiple forms as it moves through the application. Each layer has its own representation, suited to that layer's purpose. This section describes each representation and why it is designed the way it is.

### Database Layer

At the lowest level, an article is rows in three SQLite tables.

**Table: `Articles`**

| Column      | SQLite Type | Notes |
|-------------|-------------|-------|
| `Id`        | `INTEGER`   | Auto-increment primary key |
| `Title`     | `TEXT`      | Not nullable |
| `Slug`      | `TEXT`      | Not nullable; unique index `IX_Articles_Slug` |
| `Summary`   | `TEXT`      | Not nullable |
| `Content`   | `TEXT`      | Not nullable |
| `Category`  | `TEXT`      | Not nullable |
| `Status`    | `INTEGER`   | Enum stored as `0`, `1`, or `2` |
| `CreatedAt` | `TEXT`      | ISO 8601 date string (SQLite stores dates as text) |
| `UpdatedAt` | `TEXT`      | ISO 8601 date string |

> **Why does SQLite store DateTime as TEXT?** SQLite has only five storage types: NULL, INTEGER, REAL, TEXT, and BLOB. It does not have a dedicated DATETIME type. EF Core maps C# `DateTime` to SQLite's `TEXT` column and stores ISO 8601 strings like `"2026-03-10T14:23:00.0000000Z"`. EF Core handles the conversion back to `DateTime` objects transparently when you read the data.

**Table: `Tags`**

| Column | SQLite Type | Notes |
|--------|-------------|-------|
| `Id`   | `INTEGER`   | Auto-increment primary key |
| `Name` | `TEXT`      | Not nullable; unique index `IX_Tags_Name` |

**Table: `ArticleTags`**

| Column      | SQLite Type | Notes |
|-------------|-------------|-------|
| `ArticleId` | `INTEGER`   | FK → `Articles.Id`, cascade delete |
| `TagId`     | `INTEGER`   | FK → `Tags.Id`, cascade delete |

The composite primary key `(ArticleId, TagId)` prevents duplicate tag associations on a single article.

**Cascade delete** means: when an `Article` is deleted, all its `ArticleTag` rows are automatically deleted too. Similarly, deleting a `Tag` removes its links from all articles. This prevents "orphan" rows in the join table.

---

### EF Core Entities

**EF Core** (Entity Framework Core) is the **ORM** (Object-Relational Mapper) used in this project. An ORM is a library that translates between the object model in your code (C# classes) and the relational model in the database (tables and rows). Instead of writing SQL by hand, you work with regular C# objects and EF Core generates the SQL for you.

Each database table corresponds to a C# **entity** class. An entity is just a plain C# class — it has no special base class or marker interface. EF Core learns about entities through the `DbContext`.

> **MVC connection:** These entity classes are the **Models** in MVC. They live in `src/WikiProject.Api/Entities/` and represent the authoritative definition of the data the application manages. Everything else in the stack — DTOs, TypeScript interfaces, React components — is ultimately derived from or shaped by these classes.

**`Article.cs`**

```csharp
namespace WikiProject.Api.Entities;

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

    // Navigation property: the join table rows linking this article to its tags
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

**`Tag.cs`**

```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

**`ArticleTag.cs`** (the join entity)

```csharp
public class ArticleTag
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
```

The `ICollection<ArticleTag>` on `Article` and `Tag` are called **navigation properties**. They are not separate database columns — they are in-memory references that EF Core populates when you explicitly ask it to (using `.Include()`). If you load an `Article` without `.Include(a => a.ArticleTags).ThenInclude(at => at.Tag)`, the `ArticleTags` collection will be empty.

**`WikiDbContext.cs`** registers the entities and configures relationships:

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

        // Relationships
        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Article)
            .WithMany(a => a.ArticleTags)
            .HasForeignKey(at => at.ArticleId);

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Tag)
            .WithMany(t => t.ArticleTags)
            .HasForeignKey(at => at.TagId);

        // Unique constraints
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.Slug).IsUnique();
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name).IsUnique();
    }
}
```

> **Why is there an explicit join entity (ArticleTag) instead of just letting EF handle many-to-many automatically?** EF Core 5+ can manage many-to-many relationships entirely behind the scenes, without you writing a join class. The explicit join entity (`ArticleTag`) is used here to be explicit and maintainable. If extra columns are ever needed on the relationship (e.g., a "primary tag" flag, or the date a tag was added), having an explicit join class makes that straightforward.

---

### Data Transfer Objects (DTOs)

**DTOs** (Data Transfer Objects) are simple data-holder classes (or records in C#) used to define the shape of data going into and out of the API. They are different from entities:

- **Entities** map to database structure.
- **DTOs** map to API contract (what clients send and receive).

Keeping them separate is a core principle of this design (see [Why Data Transformations Happen Between Layers](#why-data-transformations-happen-between-layers)).

All DTOs are defined in `ArticleDtos.cs`. The C# `record` keyword is used — a record is a class where equality is based on value (all properties match), not object identity. This makes records a clean choice for immutable data containers.

**`ArticleDto`** — full article, returned from GET by ID and slug

```csharp
public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,       // Full article body — included
    string Category,
    IReadOnlyList<string> Tags,   // Just the names, not the Tag objects
    string Status,        // "Draft", "Published", or "Archived" — string, not enum
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

**`ArticleSummaryDto`** — partial article, returned in list responses (no `Content`)

```csharp
public record ArticleSummaryDto(
    int Id,
    string Title,
    string Slug,
    string Summary,       // Summary only — Content is omitted
    string Category,
    IReadOnlyList<string> Tags,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

> **Why omit `Content` from the list response?** The content field can be very large (whole articles). Returning the full content of 20 articles on a list page would be wasteful — the user only sees the title, summary, and metadata in the list. Omitting `Content` from `ArticleSummaryDto` keeps list responses lean.

**`CreateArticleRequest`** — shape of the JSON body when creating an article

```csharp
public record CreateArticleRequest(
    string Title,
    string? Slug,         // Optional: null means "auto-generate"
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status  // Accepts the enum (0/1/2) — not the string form
);
```

**`UpdateArticleRequest`** — same fields, used for PUT

```csharp
public record UpdateArticleRequest(
    string Title,
    string? Slug,
    string Summary,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    ArticleStatus Status
);
```

**`ArticleListResponse`** — the paginated wrapper returned from GET /api/articles

```csharp
public record ArticleListResponse(
    IReadOnlyList<ArticleSummaryDto> Items,  // Articles on this page
    int TotalCount,                          // Total matching articles
    int Page,                                // Current page (1-based)
    int PageSize,                            // Items per page
    int TotalPages                           // Ceiling(TotalCount / PageSize)
);
```

**`ArticleQueryParams`** — internal record used by the service to carry filter parameters (not exposed directly to the HTTP layer)

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

---

### Service Layer Objects

The **service layer** sits between the HTTP controller (which handles routing and HTTP concerns) and the database (which handles persistence). Its job is pure business logic: slug generation, tag resolution, pagination math, and data assembly.

The service is defined by an interface and a concrete implementation:

```csharp
// IArticleService.cs
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

The service uses `WikiDbContext` directly (injected via constructor) to query and persist data. It does not use a generic repository — EF Core's `DbContext` is already a unit-of-work + repository pattern, and adding another layer would be redundant for a project of this size.

**The key mapping step** happens in the service, via extension methods defined in `ArticleMappings.cs`:

```csharp
// ArticleMappings.cs
public static ArticleDto ToDto(this Article article) =>
    new ArticleDto(
        article.Id,
        article.Title,
        article.Slug,
        article.Summary,
        article.Content,
        article.Category,
        // Convert navigation property objects → simple string list
        article.ArticleTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
        // Convert enum → string
        article.Status.ToString(),
        article.CreatedAt,
        article.UpdatedAt
    );
```

Two key transformations happen here:
1. `article.ArticleTags.Select(at => at.Tag.Name)` — the complex object graph (Article → ArticleTag → Tag) is flattened into a simple `List<string>` of tag names.
2. `article.Status.ToString()` — the integer-backed enum (`Draft = 0`) is converted to its string representation `"Draft"`.

---

### API Response JSON

When a client calls `GET /api/articles/42`, the ASP.NET Core runtime serializes the `ArticleDto` object to JSON. ASP.NET Core uses `System.Text.Json` by default, with **camelCase** property naming.

> **camelCase** means property names start with a lowercase letter: `Id` becomes `id`, `Title` becomes `title`, `CreatedAt` becomes `createdAt`. This is the standard convention for JSON APIs.

A real response for a published article looks like this (using the seeded article, whose slug was explicitly set to `"setting-up-dev-environment"` in `SeedData.cs` — shorter than what auto-generation would produce):

```json
{
  "id": 3,
  "title": "Setting Up the Development Environment",
  "slug": "setting-up-dev-environment",
  "summary": "Step-by-step instructions for getting the project running on your local machine.",
  "content": "# Setting Up the Development Environment\n\n## Prerequisites\n\n- .NET 10 SDK...",
  "category": "DevOps",
  "tags": ["getting-started", "guide"],
  "status": "Published",
  "createdAt": "2026-03-11T02:27:47.078Z",
  "updatedAt": "2026-03-15T02:27:47.078Z"
}
```

Observations:
- `status` is `"Published"` (string), not `1` (integer). This is the `Status.ToString()` conversion from the mapping layer.
- `tags` is a flat array of strings, not an array of `{id, name}` objects.
- `createdAt` and `updatedAt` are ISO 8601 strings with a trailing `Z` indicating UTC.
- Property names are camelCase (`createdAt`, not `CreatedAt`).

The list endpoint `GET /api/articles` returns an `ArticleListResponse`:

```json
{
  "items": [
    {
      "id": 3,
      "title": "Setting Up the Development Environment",
      "slug": "setting-up-dev-environment",
      "summary": "Step-by-step instructions for getting the project running on your local machine.",
      "category": "DevOps",
      "tags": ["getting-started", "guide"],
      "status": "Published",
      "createdAt": "2026-03-11T02:27:47.078Z",
      "updatedAt": "2026-03-15T02:27:47.078Z"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

Note that `content` is absent from list items — that is the `ArticleSummaryDto` at work.

---

### Frontend TypeScript Types

On the frontend, TypeScript **interfaces** define the expected shape of data received from or sent to the API. These mirror the DTOs on the backend, but expressed in TypeScript.

```typescript
// types/index.ts

// Status as a union type — only these three strings are valid
export type ArticleStatus = 'Draft' | 'Published' | 'Archived';

// Shape of a list-view article (no content field)
export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;   // "Draft" | "Published" | "Archived"
  createdAt: string;       // ISO 8601 string from JSON
  updatedAt: string;       // ISO 8601 string from JSON
}

// Shape of a full article (extends ArticleSummary with content)
export interface Article extends ArticleSummary {
  content: string;
}

// Paginated response wrapper
export interface ArticleListResponse {
  items: ArticleSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Request shape for creating or updating
export interface CreateArticleRequest {
  title: string;
  slug?: string;         // Optional
  summary: string;
  content: string;
  category: string;
  tags: string[];
  status: number;        // 0=Draft, 1=Published, 2=Archived (numeric for writes)
}

export interface UpdateArticleRequest extends CreateArticleRequest {}
```

> **Important asymmetry:** `ArticleSummary.status` is typed as `ArticleStatus` (a string union), but `CreateArticleRequest.status` is typed as `number`. This matches the API contract: the server sends strings in responses but expects numbers in requests. This asymmetry is visible in the `ArticleForm` component, which converts the selected string status back to a number before submitting.

**Date handling:** The TypeScript interfaces use `string` for `createdAt` and `updatedAt`, not `Date` objects. JSON has no native date type — dates arrive as strings. The `format.ts` utility converts them for display:

```typescript
// utils/format.ts
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}
```

This converts `"2026-03-15T02:27:47.078Z"` into something like `"Mar 15, 2026"` in the user's local timezone.

---

### Rendered UI

The final destination of the article data is the browser's DOM (Document Object Model — the in-memory tree of HTML elements that the browser renders to screen).

> **MVC connection:** React pages and components are the **Views** in MVC. They live in `frontend/src/pages/` and `frontend/src/components/`. Their job is purely presentational: take data and produce HTML. They do not contain business rules or talk directly to the database.

**Article list view** (`ArticlesPage.tsx` + `ArticleCard.tsx`):

The `ArticlesPage` calls `useArticles(filters)`, which calls `articleService.list(filters)`, which makes an HTTP GET to `/api/articles`. The resulting `ArticleListResponse` is stored in component state. Each `ArticleSummary` in `items` is rendered as an `ArticleCard`:

```tsx
// ArticleCard.tsx
export default function ArticleCard({ article }: Props) {
  return (
    <div className="article-card">
      <div className="article-card-header">
        <Link to={`/articles/${article.id}`} className="article-card-title">
          {article.title}
        </Link>
        <span className={`status-badge ${statusClass(article.status)}`}>
          {article.status}
        </span>
      </div>
      <p className="article-card-summary">{article.summary}</p>
      <div className="article-card-meta">
        <span className="meta-category">📁 {article.category}</span>
        {article.tags.length > 0 && (
          <div className="meta-tags">
            {article.tags.map((tag) => (
              <span key={tag} className="tag">{tag}</span>
            ))}
          </div>
        )}
        <span className="meta-date">Updated {formatDate(article.updatedAt)}</span>
      </div>
    </div>
  );
}
```

In the rendered HTML, the article card shows: a clickable title, a status badge, the summary paragraph, the category, a row of tag pills, and the last-updated date. The `content` field is not displayed here because this uses `ArticleSummary` (no content field).

**Article detail view** (`ArticleDetailPage.tsx`):

The detail page fetches a full `Article` (with `content`) by ID and renders the complete body. Currently content is rendered in a `<pre>` element (preformatted text), which preserves whitespace and newlines but does not process Markdown. A future enhancement (noted in `STARTING_TASKS.md`) is to render the content through a Markdown library.

---

## Search and Filtering Concepts

WikiProject supports two related but distinct user actions: **search** and **filter**.

### Search

**Search** is a free-text query that looks for a string anywhere in article data. It is implemented as a substring match across five fields: `Title`, `Summary`, `Content`, `Category`, and `Tag.Name`.

```csharp
// ArticleService.cs
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
```

All comparisons are case-insensitive (both sides converted to lowercase). This means searching for `"API"` will match articles containing `"api"`, `"Api"`, or `"API"`.

> **Performance note:** The current search uses `LIKE '%search%'` style queries (contains), which cannot use a B-tree index and requires a full table scan. For a small knowledge base this is fine. For large datasets, SQLite's FTS5 (Full Text Search) extension or a dedicated search engine like Elasticsearch would be more appropriate. This is acknowledged in `STARTING_TASKS.md` as a future stretch goal.

### Filters

**Filters** narrow results to a specific value of a structured field:

| Filter parameter | How it matches |
|-----------------|----------------|
| `category`      | Exact match, case-insensitive: `a.Category.ToLower() == query.Category.ToLower()` |
| `tag`           | Exact match on tag name, case-insensitive: `a.ArticleTags.Any(at => at.Tag.Name.ToLower() == query.Tag.ToLower())` |
| `status`        | Exact match on enum value: `a.Status == query.Status.Value` |

Filters can be combined with each other and with search — they are AND conditions, all applied to the same query.

### Pagination

The service implements **offset pagination**: results are sorted, then a window of rows is selected by skipping a number of rows and taking a fixed count.

```csharp
var pageSize = Math.Clamp(query.PageSize, 1, 100);  // between 1 and 100
var page = Math.Max(query.Page, 1);                  // at least 1

var totalCount = await q.CountAsync();
var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

var items = await q
    .OrderByDescending(a => a.UpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

The `ArticleListResponse` includes `TotalCount`, `Page`, `PageSize`, and `TotalPages` so the frontend can build a pagination UI without needing additional API calls.

### On the Frontend

The `ArticlesPage` component manages filter state and passes it to `useArticles`:

```tsx
// ArticlesPage.tsx (simplified)
const [search, setSearch] = useState('');
const [debouncedSearch, setDebouncedSearch] = useState('');
const [category, setCategory] = useState('');
const [tag, setTag] = useState('');
const [status, setStatus] = useState<ArticleStatus | ''>('');
const [page, setPage] = useState(1);

// 300ms debounce on search input — avoids API call on every keypress
useEffect(() => {
  const timer = setTimeout(() => setDebouncedSearch(search), 300);
  return () => clearTimeout(timer);
}, [search]);

const { data, loading, error } = useArticles({
  search: debouncedSearch || undefined,
  category: category || undefined,
  tag: tag || undefined,
  status: (status as ArticleStatus) || undefined,
  page,
  pageSize: 10,
});
```

The **debounce** (300ms delay) is an important UX and performance detail. Without it, every single keystroke in the search box would trigger an API call. With debouncing, the API is only called after the user pauses typing for 300ms.

---

## End-to-End Example: One Article Through the Full Stack

This section traces a specific, real article — **"Setting Up the Development Environment"** — through every layer of the system, for both the create and read paths.

---

### Creating an Article (POST)

The user fills out the `ArticleForm` with:
- Title: `"Setting Up the Development Environment"`
- Slug: (left blank)
- Summary: `"Step-by-step instructions for getting the project running on your local machine."`
- Content: `"# Setting Up the Development Environment\n\n## Prerequisites..."`
- Category: `"DevOps"`
- Status: `Published` (selected from dropdown)
- Tags: `"getting-started, guide"` (typed as comma-separated string)

#### Step 1: Frontend — Form Submit

The `ArticleForm` component collects the form values and calls `onSubmit`:

```tsx
// ArticleForm.tsx (simplified)
const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  const request: CreateArticleRequest = {
    title: form.title.trim(),
    slug: form.slug.trim() || undefined,
    summary: form.summary.trim(),
    content: form.content.trim(),
    category: form.category.trim(),
    tags: form.tags.split(',').map(t => t.trim()).filter(Boolean),
    status: toStatusNumber(form.status),  // "Published" → 1
  };
  await onSubmit(request);
};
```

Key transformations at this point:
- Tags string `"getting-started, guide"` → `["getting-started", "guide"]`
- Status string `"Published"` → integer `1`
- Slug is `undefined` because the user left it blank

#### Step 2: Frontend — HTTP Request via Axios

`NewArticlePage` calls `articleService.create(request)`:

```typescript
// articleService.ts
async create(request: CreateArticleRequest): Promise<Article> {
  const { data } = await api.post<Article>('/api/articles', request);
  return data;
}
```

Axios serializes the `CreateArticleRequest` TypeScript object to JSON and sends it as the request body:

```http
POST http://localhost:5018/api/articles
Content-Type: application/json

{
  "title": "Setting Up the Development Environment",
  "slug": null,
  "summary": "Step-by-step instructions for getting the project running on your local machine.",
  "content": "# Setting Up the Development Environment\n\n## Prerequisites...",
  "category": "DevOps",
  "tags": ["getting-started", "guide"],
  "status": 1
}
```

#### Step 3: Backend — Controller Receives Request

`ArticlesController.Create` is invoked by ASP.NET Core's routing system:

```csharp
[HttpPost]
public async Task<ActionResult<ArticleDto>> Create(
    [FromBody] CreateArticleRequest request,
    [FromServices] IValidator<CreateArticleRequest> validator)
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return BadRequest(validation.Errors);

    var result = await _service.CreateAsync(request);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

ASP.NET Core's JSON deserialization converts the incoming JSON into a `CreateArticleRequest` C# record. The framework handles camelCase → PascalCase property name mapping automatically.

#### Step 4: Backend — Validation

`CreateArticleRequestValidator` runs before the service is called:

- `Title = "Setting Up the Development Environment"` → not empty, ≤ 200 chars ✓
- `Slug = null` → skipped (the `When` condition means rules only apply if slug is non-empty) ✓
- `Summary = "Step-by-step..."` → not empty, ≤ 500 chars ✓
- `Content = "# Setting Up..."` → not empty ✓
- `Category = "DevOps"` → not empty, ≤ 100 chars ✓
- `Tags = ["getting-started", "guide"]` → each ≤ 50 chars ✓

All rules pass. The controller proceeds to call the service.

#### Step 5: Backend — Service Layer

`ArticleService.CreateAsync` executes the core business logic:

```csharp
public async Task<ArticleDto> CreateAsync(CreateArticleRequest request)
{
    // 1. Generate slug from title (since request.Slug is null/empty)
    var slug = string.IsNullOrWhiteSpace(request.Slug)
        ? GenerateSlug(request.Title)
        : request.Slug.Trim().ToLower();
    // slug = "setting-up-the-development-environment"

    // 2. Ensure slug is unique (check DB, append -1/-2 if needed)
    slug = await EnsureUniqueSlugAsync(slug);
    // Assuming no conflict: slug = "setting-up-the-development-environment"

    // 3. Resolve tags: find existing or create new Tag entities
    var tags = await ResolveTagsAsync(request.Tags);
    // Looks up "getting-started" and "guide" in Tags table
    // Both already exist in seed data, so no new rows created

    // 4. Set timestamps
    var now = DateTime.UtcNow;

    // 5. Build the Article entity
    var article = new Article
    {
        Title = "Setting Up the Development Environment",
        Slug = "setting-up-the-development-environment",
        Summary = "Step-by-step instructions...",
        Content = "# Setting Up...",
        Category = "DevOps",
        Status = ArticleStatus.Published,  // 1
        CreatedAt = now,
        UpdatedAt = now,
        ArticleTags = tags.Select(t => new ArticleTag { Tag = t }).ToList()
    };

    // 6. Save to database
    _db.Articles.Add(article);
    await _db.SaveChangesAsync();

    // 7. Reload from DB to ensure tags are fully populated, then map to DTO
    return await GetByIdAsync(article.Id) ?? article.ToDto();
}
```

**Step 5a: `GenerateSlug("Setting Up the Development Environment")`**

Following the algorithm:
1. Lowercase: `"setting up the development environment"`
2. Remove non-alphanumeric (keeps letters, digits, spaces, hyphens): unchanged
3. Replace spaces with hyphens: `"setting-up-the-development-environment"`
4. Collapse duplicate hyphens: unchanged
5. Trim leading/trailing hyphens: unchanged
6. Truncate to 100 chars: unchanged (41 chars)

Result: `"setting-up-the-development-environment"`

**Step 5b: `ResolveTagsAsync(["getting-started", "guide"])`**

```csharp
private async Task<List<Tag>> ResolveTagsAsync(IReadOnlyList<string>? tagNames)
{
    if (tagNames == null || tagNames.Count == 0) return new List<Tag>();

    // Normalize: lowercase, trim, remove empty, deduplicate
    var normalized = tagNames
        .Select(t => t.Trim().ToLower())
        .Where(t => !string.IsNullOrEmpty(t))
        .Distinct()
        .ToList();
    // ["getting-started", "guide"]

    // Find existing tags
    var existingTags = await _db.Tags
        .Where(t => normalized.Contains(t.Name))
        .ToListAsync();
    // Returns Tag{Id=1, Name="getting-started"} and Tag{Id=2, Name="guide"}

    // Create any missing tags
    var existingNames = existingTags.Select(t => t.Name).ToHashSet();
    var newTags = normalized
        .Where(n => !existingNames.Contains(n))
        .Select(n => new Tag { Name = n })
        .ToList();
    // Empty — both tags already exist

    if (newTags.Any())
    {
        _db.Tags.AddRange(newTags);
        await _db.SaveChangesAsync();
    }

    return existingTags.Concat(newTags).ToList();
}
```

#### Step 6: Database Write

EF Core executes these SQL statements (simplified):

```sql
-- Insert the article row
INSERT INTO Articles (Title, Slug, Summary, Content, Category, Status, CreatedAt, UpdatedAt)
VALUES ('Setting Up the Development Environment', 'setting-up-the-development-environment',
        'Step-by-step instructions...', '# Setting Up...', 'DevOps', 1,
        '2026-03-10T08:00:00.0000000Z', '2026-03-10T08:00:00.0000000Z');

-- Insert the tag associations
INSERT INTO ArticleTags (ArticleId, TagId) VALUES (3, 1);  -- article 3, tag "getting-started"
INSERT INTO ArticleTags (ArticleId, TagId) VALUES (3, 2);  -- article 3, tag "guide"
```

After `SaveChangesAsync()`, the `article.Id` is populated by EF Core (SQLite returns the auto-increment value).

#### Step 7: Map Entity to DTO

The service calls `GetByIdAsync(article.Id)` to reload with tags, then calls `.ToDto()`:

```csharp
public static ArticleDto ToDto(this Article article) =>
    new ArticleDto(
        3,                                         // Id
        "Setting Up the Development Environment",  // Title
        "setting-up-the-development-environment",  // Slug
        "Step-by-step instructions...",            // Summary
        "# Setting Up...",                         // Content
        "DevOps",                                  // Category
        new List<string> { "getting-started", "guide" },  // Tags (names only, sorted)
        "Published",                               // Status.ToString()
        new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),  // CreatedAt
        new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc)   // UpdatedAt
    );
```

#### Step 8: API Response

The controller returns `CreatedAtAction(nameof(GetById), new { id = 3 }, articleDto)`, which produces:

```http
HTTP/1.1 201 Created
Location: /api/articles/3
Content-Type: application/json

{
  "id": 3,
  "title": "Setting Up the Development Environment",
  "slug": "setting-up-the-development-environment",
  "summary": "Step-by-step instructions for getting the project running on your local machine.",
  "content": "# Setting Up the Development Environment\n\n## Prerequisites...",
  "category": "DevOps",
  "tags": ["getting-started", "guide"],
  "status": "Published",
  "createdAt": "2026-03-10T08:00:00Z",
  "updatedAt": "2026-03-10T08:00:00Z"
}
```

#### Step 9: Frontend Processes Response

Axios receives the response. The JSON is automatically parsed into a JavaScript object matching the `Article` TypeScript interface. The `NewArticlePage` handles success:

```tsx
// NewArticlePage.tsx
const handleSubmit = async (data: CreateArticleRequest) => {
  const created = await articleService.create(data);
  navigate(`/articles/${created.id}`);  // Navigate to the new article's detail page
};
```

The browser navigates to `/articles/3`, where `ArticleDetailPage` renders the full article.

#### Step 10: Rendered UI

`ArticleDetailPage` calls `useArticle(3)`, which fetches `GET /api/articles/3`. The `Article` object (TypeScript) populates the component's state. React renders:

```
┌─────────────────────────────────────────────────────┐
│  ← Back to Articles              [Edit] [Delete]    │
│                                                     │
│  Setting Up the Development Environment             │
│                                                     │
│  📁 DevOps     ✅ Published                        │
│  Created Mar 10, 2026   Updated Mar 10, 2026       │
│  Tags: getting-started  guide                       │
│                                                     │
│  # Setting Up the Development Environment           │
│                                                     │
│  ## Prerequisites                                   │
│  - .NET 10 SDK                                      │
│  - Node.js 20+                                      │
│  ...                                                │
└─────────────────────────────────────────────────────┘
```

The journey is complete: from a form submission → HTTP request → validation → business logic → database write → response serialization → frontend state → rendered HTML.

---

### Reading an Article (GET)

When a user navigates to `/articles/3`, the flow is simpler because no mutations occur.

1. **React Router** matches the route `/articles/:id` → renders `ArticleDetailPage`
2. **`useArticle(3)`** hook calls `articleService.getById(3)`
3. **Axios** sends `GET http://localhost:5018/api/articles/3`
4. **`ArticlesController.GetById(3)`** calls `_service.GetByIdAsync(3)`
5. **`ArticleService.GetByIdAsync`** executes:
   ```csharp
   return await _db.Articles
       .Include(a => a.ArticleTags)
           .ThenInclude(at => at.Tag)
       .AsNoTracking()       // Read-only: no change tracking overhead
       .Where(a => a.Id == 3)
       .Select(a => a.ToDto())
       .FirstOrDefaultAsync();
   ```
   EF Core translates this to:
   ```sql
   SELECT a.Id, a.Title, a.Slug, ..., t.Name
   FROM Articles a
   LEFT JOIN ArticleTags at ON at.ArticleId = a.Id
   LEFT JOIN Tags t ON t.Id = at.TagId
   WHERE a.Id = 3
   ```
6. The `Article` entity (with populated `ArticleTags` navigation property) is mapped to `ArticleDto` via `.ToDto()`
7. The `ArticleDto` is returned to the controller, which returns `200 OK` with the DTO serialized to JSON
8. Axios parses JSON → TypeScript `Article` object → React state → rendered component

> **Why `AsNoTracking()`?** EF Core normally tracks every entity it loads, keeping a copy in memory so it can detect changes when `SaveChangesAsync` is called. For read-only queries, this tracking is wasted overhead. `AsNoTracking()` skips tracking, which is faster and uses less memory.

---

### Updating an Article (PUT)

When a user edits and saves an article:

1. `EditArticlePage` pre-fills `ArticleForm` with the existing `Article` data
2. User modifies the title to `"Setting Up the Dev Environment (Updated)"`
3. Form submits → `articleService.update(3, request)` → `PUT /api/articles/3`
4. Controller validates the request with `UpdateArticleRequestValidator`
5. `ArticleService.UpdateAsync(3, request)`:
   ```csharp
   var article = await _db.Articles
       .Include(a => a.ArticleTags)
       .FirstOrDefaultAsync(a => a.Id == id);

   if (article == null) return null;

   // Only re-check slug uniqueness if the slug is changing
   var newSlug = string.IsNullOrWhiteSpace(request.Slug)
       ? GenerateSlug(request.Title)
       : request.Slug.Trim().ToLower();

   if (newSlug != article.Slug)
       newSlug = await EnsureUniqueSlugAsync(newSlug, excludeId: id);

   // Replace tags entirely
   article.ArticleTags.Clear();
   var tags = await ResolveTagsAsync(request.Tags);
   foreach (var tag in tags)
       article.ArticleTags.Add(new ArticleTag { Tag = tag });

   // Update all fields
   article.Title = request.Title.Trim();
   article.Slug = newSlug;
   article.Summary = request.Summary.Trim();
   article.Content = request.Content.Trim();
   article.Category = request.Category.Trim();
   article.Status = request.Status;
   article.UpdatedAt = DateTime.UtcNow;  // CreatedAt is NOT changed

   await _db.SaveChangesAsync();
   ```
6. Database: `UPDATE Articles SET Title=..., UpdatedAt=... WHERE Id=3`
7. Response: `200 OK` with updated `ArticleDto`
8. Frontend: navigates back to article detail page which re-fetches and shows updated data

---

## Why Data Transformations Happen Between Layers

Each layer in the stack has a different job, and each job requires a different shape of data.

| Layer            | Primary Job                             | Data Shape Used |
|------------------|-----------------------------------------|-----------------|
| Database         | Persist data, enforce constraints       | Normalized rows and columns |
| EF Core Entity   | Map DB to objects, enable LINQ queries  | C# classes with navigation properties |
| Service Layer    | Business logic, tag resolution, slugs   | Entities + DTOs |
| Controller       | HTTP concerns (routing, status codes)   | DTOs only |
| API Response     | Communicate with clients over HTTP      | JSON (camelCase) |
| TypeScript Types | Type-safe data handling in frontend     | Interfaces matching JSON shape |
| React Components | Render UI, handle user interaction      | TypeScript objects + DOM |

**Why not just use entities everywhere?**

If you exposed your EF Core entities directly from the API (sometimes called "entity exposure"), you would face several problems:

1. **Circular reference in JSON serialization**: `Article` → `ArticleTag` → `Article` → … would cause infinite recursion during JSON serialization. You'd need configuration workarounds.
2. **Over-posting attacks**: A malicious client could include fields like `Id` or `CreatedAt` in a create request and accidentally (or intentionally) corrupt data. DTOs define exactly which fields the client is allowed to provide.
3. **Leaking internal structure**: The database schema is an implementation detail. If you expose entities, changing the schema breaks your API contract. DTOs insulate the API from internal changes.
4. **Different shapes for different operations**: `ArticleSummaryDto` has no `Content` field, which avoids transferring large blobs in list responses. You cannot have this optimization if you use the same entity for both.

---

## What Bugs This Separation Helps Avoid

The layered design with DTOs, entities, and services is not just organizational — it prevents real categories of bugs.

### Over-posting

Without input DTOs, a user could POST `{"id": 999, "createdAt": "2000-01-01T00:00:00Z"}` and potentially corrupt the `Id` or `CreatedAt` of a newly created article (depending on how EF Core handles unexpected properties). `CreateArticleRequest` only accepts `Title`, `Slug`, `Summary`, `Content`, `Category`, `Tags`, and `Status` — there is no way to set `Id` or `CreatedAt` from the outside.

### Status string confusion

The backend stores `Status` as an integer (`0/1/2`), but the frontend displays and works with it as a string (`"Draft"/"Published"/"Archived"`). Without a clear mapping layer, a developer might accidentally try to compare `article.Status == "Published"` in C# (comparing an enum to a string, which would fail silently in some contexts) or try to send `"Published"` in the API request body instead of `1`. The explicit DTO design and the `ToDto()` mapping make the conversion explicit and testable.

### Stale timestamp

If timestamps were set by client-provided data, a client could submit a fake `createdAt` or `updatedAt`. Setting them in the service layer (`DateTime.UtcNow`) guarantees they reflect the actual server time.

### Orphan tags

If tag resolution were not centralized in `ResolveTagsAsync`, different code paths might create duplicate `Tag` rows with the same name. The centralized resolution function queries the database for existing tags before creating new ones, and the unique index on `Tag.Name` provides a final safety net.

### Unintended slug conflicts

The `EnsureUniqueSlugAsync` method with its `excludeId` parameter prevents a subtle bug: updating an article without changing its slug would fail the uniqueness check if the code naively checked whether *any* article had that slug, including the article being updated.

---

## Where Serialization Happens

**Serialization** means converting an in-memory object to a transmittable format (JSON). **Deserialization** is the reverse.

### Backend → Frontend (Serialization)

When the controller returns an `ActionResult<ArticleDto>`, ASP.NET Core's pipeline invokes `System.Text.Json` to serialize the `ArticleDto` C# object to a JSON string. This happens automatically — you don't call any serialization code yourself.

Key behaviors of `System.Text.Json` in this project:
- **camelCase property naming**: `CreatedAt` → `createdAt`, `ArticleId` → `articleId`
- **Enum serialization**: Enums are serialized by default as their integer values. But in this app, the `Status` in the DTO is already a `string` (converted via `Status.ToString()` in `ToDto()`), so JSON gets `"Published"` not `1`.
- **DateTime serialization**: `DateTime` values are serialized as ISO 8601 strings: `"2026-03-10T08:00:00Z"`

### Frontend → Backend (Deserialization)

When Axios makes an HTTP request with a JSON body, ASP.NET Core's model binder deserializes the JSON string back into the C# DTO type:

- `"title"` in JSON → `Title` property in `CreateArticleRequest` (camelCase ↔ PascalCase mapping is handled automatically)
- `"status": 1` in JSON → `ArticleStatus.Published` enum value
- `null` for optional `"slug"` → `string? Slug = null`

### Frontend: JSON → TypeScript Object

Axios automatically calls `JSON.parse()` on the response body, converting the JSON string into a plain JavaScript object. TypeScript's type system applies the interface shape at compile time — it doesn't enforce types at runtime. If the server returns unexpected data, TypeScript won't throw an error at runtime; it will just silently have properties of the wrong type.

---

## How Validation Protects the System

Validation is applied at two levels: the frontend (user experience) and the backend (data integrity). Both are necessary for different reasons.

### Frontend Validation (ArticleForm)

The `ArticleForm` component validates before submitting:
- Required fields are checked (title, summary, content, category)
- Slug format is validated against the same regex as the backend: `^[a-z0-9]+(?:-[a-z0-9]+)*$`
- Max lengths are checked for each field

**Purpose:** Provide immediate, user-friendly error messages without waiting for a network round-trip. A user who types a slug with spaces sees the error instantly.

**Limitation:** Frontend validation can be bypassed. Anyone with a browser's developer tools or `curl` can send any request to the API. Frontend validation is for UX, not security.

### Backend Validation (FluentValidation)

`CreateArticleRequestValidator` and `UpdateArticleRequestValidator` run inside the controller before the service is called:

```csharp
var validation = await validator.ValidateAsync(request);
if (!validation.IsValid)
    return BadRequest(validation.Errors);
```

If validation fails, the request is rejected with a `400 Bad Request` response and an error list. The service layer is never called.

Rules defined (same for both Create and Update):

| Field      | Rules |
|-----------|-------|
| `Title`   | Required, ≤ 200 chars |
| `Slug`    | When provided: ≤ 200 chars, matches `^[a-z0-9]+(?:-[a-z0-9]+)*$` |
| `Summary` | Required, ≤ 500 chars |
| `Content` | Required |
| `Category`| Required, ≤ 100 chars |
| `Tags`    | Each tag ≤ 50 chars |

**Why use FluentValidation instead of Data Annotations?** Data Annotations (e.g., `[Required]`, `[MaxLength(200)]`) are attributes on model properties. FluentValidation rules are code in a separate class. FluentValidation is more flexible (conditional rules with `.When()`), more testable (you can unit test validators in isolation), and keeps validation logic out of the DTO classes.

### Database-Level Constraints

Even beyond application-level validation, the database enforces structural integrity:
- `Slug` has a unique index → duplicate slugs fail at the SQL level even if the app-level uniqueness check is bypassed (e.g., in a race condition)
- `Tag.Name` has a unique index → same protection for tag names
- Foreign keys with cascade delete → `ArticleTag` rows are always consistent with their parent `Article` and `Tag` rows

This defense-in-depth approach (validate in the frontend → validate in the backend → enforce in the database) means that invalid data has multiple opportunities to be caught, and invalid data in the database is nearly impossible.

---

## How Updates Propagate Through the Stack

When an article changes in the database, the update must reach the user's browser. Here is how that works.

### Immediate Propagation (Current Behavior)

WikiProject uses a **request-response** model with no real-time push. Updates propagate only when the client makes a new HTTP request:

1. User edits article → `PUT /api/articles/3` → database updated
2. Server returns updated `ArticleDto` in the `200 OK` response
3. Frontend navigation goes to the detail page → `GET /api/articles/3` → fresh data fetched
4. React re-renders with the new data

**Important:** If two users have the same article open simultaneously, user B will not see user A's edit until B refreshes or navigates away and back. This is acceptable for a small team knowledge base but would be a problem for collaborative real-time editing.

### The `refetch` Pattern

The `useArticles` hook exposes a `refetch` function:

```typescript
const { data, loading, error, refetch } = useArticles(filters);
```

Components can call `refetch()` after a mutation to force a fresh load from the server. For example, after deleting an article, the articles list page calls `refetch()` to update the list.

### How React Re-renders

When the hook's state changes (e.g., after `refetch()` completes), React re-renders all components that use that state. The `ArticleCard` components in the list automatically reflect the new data because they receive article data as props from the parent component's state.

### Future Enhancement: Real-Time Updates

For real-time collaborative editing or instant notification of updates, the application could be extended with:
- **WebSockets** (or the lighter-weight **Server-Sent Events**): the server pushes messages to clients when articles change
- **Polling**: the frontend periodically refetches on a timer (simple, but wasteful)
- **Optimistic updates**: the frontend immediately shows the updated state before the server confirms, then corrects if the server rejects the change

These are not currently implemented. `STARTING_TASKS.md` lists real-time features as future stretch goals.

---

## Summary and Next Steps

### What you learned in this document

- **MVC (Model-View-Controller)** is the organizing design pattern. `Article`, `Tag`, and other entity classes are the **Models**; `ArticlesController` is the **Controller**; React pages and components are the **Views**.
- **Article** is the central Model. All other concepts (Category, Tag, Status, Slug, Metadata) are properties of or relationships to Articles.
- **Category** is a plain text field — simple, with the tradeoff of possible inconsistency.
- **Tags** use a proper many-to-many relationship with a join table, enabling multi-label classification and efficient querying.
- **Status** is an enum stored as an integer in the database and serialized as a string in API responses.
- **Slug** is auto-generated from the title and enforced unique at both the service layer and database level.
- **CreatedAt/UpdatedAt** timestamps are set by server-side code in UTC.
- **Data transformations between layers** are intentional and prevent over-posting, circular JSON references, and tight coupling between the database schema and the API contract.
- **Validation** operates at three levels: frontend (UX), backend (FluentValidation), and database (constraints and indexes).
- **Updates propagate** through a request-response cycle with no real-time push in the current implementation.

### Where to look next

- **`src/WikiProject.Api/Services/ArticleService.cs`** — the most important file for understanding business logic
- **`src/WikiProject.Api/Mappings/ArticleMappings.cs`** — the explicit transformation between entities and DTOs
- **`src/WikiProject.Api/Data/WikiDbContext.cs`** — EF Core configuration and relationships
- **`frontend/src/services/articleService.ts`** — the frontend's API client
- **`frontend/src/types/index.ts`** — TypeScript type definitions mirroring the API

### Related documentation (future)

- `docs/01_*` or similar: Architecture overview and project setup
- `docs/06_*` or similar: Search and pagination in depth
- `docs/07_*` or similar: Validation strategy and error handling
- `docs/08_*` or similar: EF Core migrations and database management

### Recommended further reading

- [MVC design pattern — Microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview)
- [Entity Framework Core documentation — Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [FluentValidation documentation](https://docs.fluentvalidation.net/)
- [System.Text.Json serialization documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)
- [React hooks documentation — useState and useEffect](https://react.dev/reference/react)
- [Axios documentation](https://axios-http.com/docs/intro)
- [REST API design best practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)
