# Database and EF Core

> **Audience:** A developer who is new to the project, or new to .NET and Entity Framework Core.
> **Goal:** Teach you not just *what* the database layer does, but *why* it is structured the way it is, *how* it works under the hood, and *what trade-offs were made*.

---

## Table of Contents

1. [Why Does This App Need a Database?](#1-why-does-this-app-need-a-database)
2. [Why SQLite?](#2-why-sqlite)
3. [What Is an ORM?](#3-what-is-an-orm)
4. [What Is EF Core?](#4-what-is-ef-core)
5. [Project Setup: Packages and Configuration](#5-project-setup-packages-and-configuration)
6. [The DbContext: The Heart of EF Core](#6-the-dbcontext-the-heart-of-ef-core)
7. [Entities: The C# Representation of Your Data](#7-entities-the-c-representation-of-your-data)
8. [Entity-to-Table Mapping](#8-entity-to-table-mapping)
9. [Relationships and Foreign Keys](#9-relationships-and-foreign-keys)
10. [Indexes and Unique Constraints](#10-indexes-and-unique-constraints)
11. [Change Tracking](#11-change-tracking)
12. [SaveChanges: Committing Work to the Database](#12-savechanges-committing-work-to-the-database)
13. [Migrations](#13-migrations)
14. [Seed Data](#14-seed-data)
15. [Schema Evolution: What Happens When You Add a Field?](#15-schema-evolution-what-happens-when-you-add-a-field)
16. [Slugs, Tags, Categories, and Statuses as Stored Data](#16-slugs-tags-categories-and-statuses-as-stored-data)
17. [Querying the Database: How the Service Layer Uses EF Core](#17-querying-the-database-how-the-service-layer-uses-ef-core)
18. [AsNoTracking and When to Use It](#18-asnotracking-and-when-to-use-it)
19. [Comparisons and Alternatives](#19-comparisons-and-alternatives)
20. [Common Beginner Confusion](#20-common-beginner-confusion)
21. [What to Study Next](#21-what-to-study-next)

---

## 1. Why Does This App Need a Database?

WikiProject is a knowledge base: a place to create, store, update, search, and delete articles. The word "store" is the key one. Without a database, any data you entered would only exist while the server process is running. The moment the server restarts, everything disappears. That is called *in-memory storage*, and it is only useful for caching or very temporary work.

A database provides:

- **Persistence** – data survives server restarts, crashes, and deployments
- **Querying** – you can search, filter, sort, and paginate large amounts of data efficiently
- **Consistency** – rules (like "slug must be unique") are enforced at the storage level, not just in code
- **Concurrency** – multiple users can read and write at the same time without corrupting data (within the limits of the database engine)
- **Relationships** – an article can be linked to many tags, and that link is stored and enforced

Without these guarantees, you would need to implement them all yourself in application code, and that is both difficult and error-prone.

### Why this matters

Every feature in WikiProject depends on the database. When you search for articles, a database query is run. When you create an article, a row is inserted. When you delete it, the row and its tag links are removed. Understanding the database layer means understanding everything that actually *persists*.

---

## 2. Why SQLite?

SQLite is a database engine where the entire database lives in a **single file on disk** (in this project, `wiki.db` in the `src/WikiProject.Api/` directory). There is no separate server process to install, configure, or manage.

The connection string in `appsettings.json` reflects this simplicity:

```json
"ConnectionStrings": {
  "Default": "Data Source=wiki.db"
}
```

That is the entire configuration. `wiki.db` is a relative path, so the file is created next to the running executable when the application starts for the first time.

### Why SQLite was chosen for this project

| Reason | Explanation |
|--------|-------------|
| **Zero setup** | No server installation needed. Clone the repo, run `dotnet run`, and the database is created automatically. |
| **Self-contained** | The `.db` file can be copied, backed up, or deleted with a single file operation. |
| **Good enough for small workloads** | An internal knowledge base typically has hundreds or thousands of articles read by tens of users. SQLite handles this easily. |
| **Development parity** | Every developer uses the same database type. There is no "it works on my machine" problem caused by different database configurations. |
| **EF Core support is first-class** | Microsoft ships `Microsoft.EntityFrameworkCore.Sqlite`, so all EF Core features work with SQLite without hacks. |

### Where SQLite stops being ideal

SQLite is excellent for development and small-to-medium production workloads, but it has real limitations you should know about:

| Limitation | Explanation |
|------------|-------------|
| **Write concurrency** | SQLite uses file-level locking. Only one writer can operate at a time. Under heavy simultaneous writes, requests will queue and may time out. |
| **Network access** | The database file must be on the same machine as the application. You cannot share one SQLite database across multiple application servers (horizontal scaling). |
| **Advanced data types** | SQLite has limited data types (TEXT, INTEGER, REAL, BLOB, NULL). Things like native JSON columns, arrays, and geographic types require workarounds. |
| **Full-text search** | SQLite has FTS5 (Full-Text Search), but it is not directly supported by EF Core's LINQ-to-SQL translation. You would need raw SQL. |
| **Online schema changes** | Some `ALTER TABLE` operations are not supported in SQLite. Migrations that rename columns or change constraints require table recreation. |

### SQLite vs SQL Server vs PostgreSQL

| | SQLite | SQL Server | PostgreSQL |
|--|--------|------------|------------|
| **Setup** | None (file-based) | Complex (server process) | Moderate (server process) |
| **Cost** | Free | Commercial (free Express edition has limits) | Free and open-source |
| **Concurrency** | Low | High | High |
| **Scalability** | Single machine, low write volume | Enterprise-grade | Enterprise-grade |
| **EF Core support** | Full | Full | Full |
| **Best for** | Development, small apps, embedded | Enterprise Windows/.NET apps | General production use, complex queries |
| **Docker** | Not needed | `mcr.microsoft.com/mssql/server` | `postgres` |

**Practical migration path:** If WikiProject needed to scale, you would change one line in `Program.cs`:

```csharp
// From:
opts.UseSqlite(connectionString)

// To:
opts.UseNpgsql(connectionString)  // PostgreSQL
// or
opts.UseSqlServer(connectionString)  // SQL Server
```

EF Core abstracts the database engine so well that this change, combined with new migrations, would be all that is required for most features.

### Section recap

SQLite was chosen because it requires zero infrastructure setup and is sufficient for a small internal application. If user numbers or write volume grow significantly, migrating to PostgreSQL or SQL Server is straightforward with EF Core.

---

## 3. What Is an ORM?

**ORM** stands for **Object-Relational Mapper**. It is a library that bridges two different worlds:

- **The object world:** Your C# classes (`Article`, `Tag`, `ArticleTag`) with properties, inheritance, and methods
- **The relational world:** Database tables with rows, columns, and SQL queries

Without an ORM, you would write SQL yourself, execute it against the database, and then manually map each row's columns back into C# objects. This works, but it is tedious:

```csharp
// Without an ORM — raw ADO.NET
using var connection = new SqliteConnection(connectionString);
connection.Open();
using var command = connection.CreateCommand();
command.CommandText = "SELECT Id, Title, Slug FROM Articles WHERE Id = @id";
command.Parameters.AddWithValue("@id", 42);
using var reader = command.ExecuteReader();
if (reader.Read())
{
    var article = new Article
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        Slug = reader.GetString(2)
    };
}
```

With an ORM, the same thing looks like this:

```csharp
// With EF Core
var article = await _db.Articles.FindAsync(42);
```

The ORM generates the SQL, executes it, and maps the result back to your `Article` class. You write C# instead of SQL.

### Why teams use ORMs

| Reason | Explanation |
|--------|-------------|
| **Less boilerplate** | No manual column-to-property mapping |
| **Type safety** | Your IDE can autocomplete `article.Title` and warn if it doesn't exist. Raw SQL strings give no such help. |
| **Refactoring safety** | Rename `Title` to `Heading` and the compiler tells you everywhere it breaks. |
| **Database portability** | Swap SQLite for PostgreSQL without rewriting queries (mostly) |
| **Migrations** | The ORM can compare your C# model to the database and generate the SQL to bring it in sync |

### Alternative approaches

**Dapper** is a popular "micro-ORM" for .NET. It gives you the convenience of mapping query results to objects, but you still write SQL yourself. It is faster than EF Core for read-heavy workloads, but it gives up migration tools, change tracking, and LINQ queries.

**Raw ADO.NET** gives full control but requires maximum boilerplate. Good when you need very specific query optimization or stored procedures.

**Neither is wrong**. EF Core is the right default for a full-stack application like this one. Dapper or raw SQL can be added alongside EF Core for queries where performance is critical.

| | EF Core | Dapper | Raw ADO.NET |
|--|---------|--------|-------------|
| **Abstraction level** | High | Medium | Low |
| **Write queries as** | LINQ | SQL strings | SQL strings |
| **Change tracking** | Yes | No | No |
| **Migrations** | Yes | No | No |
| **Raw SQL possible** | Yes (escape hatch) | Always | Always |
| **Performance** | Good | Excellent | Excellent |
| **Best for** | Full-stack CRUD apps | Read-heavy services, complex queries | Maximum control |

### Section recap

An ORM translates between C# objects and relational database tables, eliminating most manual SQL and column-mapping boilerplate. EF Core is the most fully-featured ORM for .NET.

---

## 4. What Is EF Core?

**Entity Framework Core** (EF Core) is Microsoft's official ORM for .NET. It is not the first version — there was an older "Entity Framework" (sometimes called "EF6") — but EF Core is the modern, actively developed replacement. It runs on .NET 5+ (and .NET Core before that).

EF Core's main responsibilities in this project are:

1. **Defining the database schema** from your C# entity classes
2. **Generating and applying migrations** to keep the database in sync with the schema
3. **Translating LINQ queries** (C# code) into SQL statements
4. **Tracking changes** to loaded entities and generating the right `INSERT`, `UPDATE`, or `DELETE` statements when you call `SaveChanges`
5. **Managing database connections** and transactions

EF Core is configured in `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=wiki.db";

builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

This registers `WikiDbContext` in ASP.NET Core's dependency injection container with the SQLite provider. When a controller or service needs a `WikiDbContext`, the framework automatically creates one, passes it in, and disposes it when the request is done.

### EF Core packages used

From `WikiProject.Api.csproj`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.5" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.5">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.5">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

| Package | What it does |
|---------|--------------|
| `Microsoft.EntityFrameworkCore.Sqlite` | The SQLite database provider. Translates EF Core operations to SQLite SQL. |
| `Microsoft.EntityFrameworkCore.Design` | Needed at build time to run `dotnet ef` commands. Not deployed to production. |
| `Microsoft.EntityFrameworkCore.Tools` | The `dotnet ef` CLI tool for generating and applying migrations. |

The `Design` and `Tools` packages have `<PrivateAssets>all</PrivateAssets>`, which means they are not included as dependencies when this project is referenced by another. They are development tools, not runtime dependencies.

### Section recap

EF Core is Microsoft's production-grade ORM. It handles schema definition, migrations, LINQ-to-SQL translation, and change tracking. For this project, it uses the SQLite provider.

---

## 5. Project Setup: Packages and Configuration

Before EF Core does anything, it needs to be registered and configured. In this project, that happens in two places.

### Connection string in appsettings

`src/WikiProject.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=wiki.db"
  }
}
```

`Data Source=wiki.db` is SQLite's connection string format. The database file will be created (or opened) at the path `wiki.db` relative to the application's working directory. In development, that is typically `src/WikiProject.Api/wiki.db`.

In `appsettings.Development.json`, the connection string is the same, but EF Core SQL logging is elevated:

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

This causes every SQL statement EF Core generates to be printed to the console during development, which is extremely useful for debugging query behavior.

### Startup registration

`src/WikiProject.Api/Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=wiki.db";

builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

`AddDbContext<T>` registers the `WikiDbContext` with a **scoped lifetime**, meaning a new instance is created per HTTP request. This is the correct lifetime for database contexts because:

- A context instance tracks the state of entities loaded during the current operation
- You want that state to be fresh per request, not shared across requests
- Long-lived contexts accumulate tracked entities and can consume memory and cause stale data issues

### Automatic migration and seeding at startup

Also in `Program.cs`, immediately after the application is built:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
}
```

`db.Database.Migrate()` applies any pending migrations when the app starts. This means you never need to remember to run `dotnet ef database update` manually after deploying; the app updates itself. This is a common pattern for small applications.

> **Caution:** For large production systems, running migrations at startup can cause issues (e.g., if two instances start simultaneously). For those scenarios, migrations are typically run as a separate deployment step.

---

## 6. The DbContext: The Heart of EF Core

The **DbContext** is the central class in every EF Core application. It is the gateway to your database. Think of it as the live connection session that knows about your entities and can execute queries and save changes.

In this project, the context is `WikiDbContext`:

**File:** `src/WikiProject.Api/Data/WikiDbContext.cs`

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

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Article)
            .WithMany(a => a.ArticleTags)
            .HasForeignKey(at => at.ArticleId);

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Tag)
            .WithMany(t => t.ArticleTags)
            .HasForeignKey(at => at.TagId);

        // Unique index on slug for fast lookups and uniqueness enforcement
        modelBuilder.Entity<Article>()
            .HasIndex(a => a.Slug)
            .IsUnique();

        // Unique index on tag name
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();
    }
}
```

### DbSet properties

Each `DbSet<T>` property represents one database table. The property name usually matches the table name:

- `Articles` → the `Articles` table
- `Tags` → the `Tags` table
- `ArticleTags` → the `ArticleTags` table

`DbSet<T>` is like a queryable, changeable view of a table. You use it to read, add, update, and delete rows.

### OnModelCreating and the Fluent API

`OnModelCreating` is called by EF Core when it is building its internal model of your database. It is where you configure things that cannot be expressed with C# property types alone. This configuration approach is called the **Fluent API** because you chain method calls together.

The configurations in this project:

1. **Composite primary key for `ArticleTag`:** Since `ArticleTag` is a join table, neither `ArticleId` nor `TagId` alone is a unique identifier — the combination of the two is. EF Core requires this to be explicitly specified.

2. **Relationships with `HasOne`/`WithMany`:** Tells EF Core that each `ArticleTag` row belongs to one `Article`, and each `Article` has many `ArticleTag` rows. EF Core uses this to understand how to write JOINs.

3. **Unique indexes on `Slug` and `Name`:** These create `UNIQUE INDEX` constraints in the database, ensuring no two articles can have the same slug and no two tags can have the same name. The index also speeds up lookups by slug or name.

> **Beginner confusion:** EF Core can also read configuration from **data annotation attributes** on your entity classes (e.g., `[Key]`, `[Required]`, `[MaxLength(100)]`). This project uses the Fluent API instead because it keeps entity classes clean and separates infrastructure concerns (database configuration) from domain concerns (what data looks like).

---

## 7. Entities: The C# Representation of Your Data

An **entity** is a C# class whose instances correspond to rows in a database table. EF Core maps each entity class to a table.

### Article

**File:** `src/WikiProject.Api/Entities/Article.cs`

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

| Property | C# Type | DB Column Type | Notes |
|----------|---------|----------------|-------|
| `Id` | `int` | `INTEGER` | Primary key, auto-incremented |
| `Title` | `string` | `TEXT` | Article title, required |
| `Slug` | `string` | `TEXT` | URL-safe identifier, unique |
| `Summary` | `string` | `TEXT` | Short teaser text |
| `Content` | `string` | `TEXT` | Full Markdown content |
| `Category` | `string` | `TEXT` | Free-form category name |
| `Status` | `ArticleStatus` | `INTEGER` | Stored as int (0=Draft, 1=Published, 2=Archived) |
| `CreatedAt` | `DateTime` | `TEXT` | Stored as ISO-8601 string in SQLite |
| `UpdatedAt` | `DateTime` | `TEXT` | Stored as ISO-8601 string in SQLite |
| `ArticleTags` | `ICollection<ArticleTag>` | *(not a column)* | Navigation property |

> **Why DateTime is stored as TEXT in SQLite:** SQLite does not have a native `DATETIME` type. EF Core stores `DateTime` values as ISO-8601 strings (e.g., `"2026-03-15T23:58:34.0000000Z"`). This is transparent to you as a developer — you work with `DateTime` objects, and EF Core handles the conversion.

### Tag

**File:** `src/WikiProject.Api/Entities/Tag.cs`

```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
```

A tag is simple: an auto-incremented integer ID and a unique string name. The `ArticleTags` collection is a navigation property that lets you find all the articles that have this tag.

Tags in this project are always stored in lowercase (normalized in `ArticleService.ResolveTagsAsync`), which prevents duplicates like `"API"` and `"api"` from coexisting.

### ArticleTag (join table)

**File:** `src/WikiProject.Api/Entities/ArticleTag.cs`

```csharp
public class ArticleTag
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
```

`ArticleTag` is a **join entity** that represents the many-to-many relationship between articles and tags. It is not a "real" domain concept in the sense that a user never creates an `ArticleTag` directly — it is an implementation detail of the relationship.

The `null!` syntax on the navigation properties (`Article` and `Tag`) tells the C# null-reference analyzer "I know this looks nullable, but EF Core guarantees it will be populated when loaded from the database." It suppresses a false compiler warning.

### ArticleStatus enum

**File:** `src/WikiProject.Api/Entities/ArticleStatus.cs`

```csharp
public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
```

EF Core stores enum values as integers by default. In the `Articles` table, the `Status` column contains `0`, `1`, or `2`. When EF Core loads a row, it automatically converts the integer back to the enum value. This is transparent in application code: you always work with `ArticleStatus.Draft`, never with `0`.

> **Alternative:** EF Core can also store enums as their string names (e.g., `"Draft"` instead of `0`) by adding `.HasConversion<string>()` in `OnModelCreating`. This makes the database more readable but uses slightly more storage and requires care if enum names ever change.

---

## 8. Entity-to-Table Mapping

Here is exactly how each C# entity maps to the SQLite schema as defined in the initial migration:

### Articles table

```sql
CREATE TABLE "Articles" (
    "Id"        INTEGER NOT NULL CONSTRAINT "PK_Articles" PRIMARY KEY AUTOINCREMENT,
    "Title"     TEXT    NOT NULL,
    "Slug"      TEXT    NOT NULL,
    "Summary"   TEXT    NOT NULL,
    "Content"   TEXT    NOT NULL,
    "Category"  TEXT    NOT NULL,
    "Status"    INTEGER NOT NULL,
    "CreatedAt" TEXT    NOT NULL,
    "UpdatedAt" TEXT    NOT NULL
);

CREATE UNIQUE INDEX "IX_Articles_Slug" ON "Articles" ("Slug");
```

### Tags table

```sql
CREATE TABLE "Tags" (
    "Id"   INTEGER NOT NULL CONSTRAINT "PK_Tags" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT    NOT NULL
);

CREATE UNIQUE INDEX "IX_Tags_Name" ON "Tags" ("Name");
```

### ArticleTags table

```sql
CREATE TABLE "ArticleTags" (
    "ArticleId" INTEGER NOT NULL,
    "TagId"     INTEGER NOT NULL,
    CONSTRAINT "PK_ArticleTags" PRIMARY KEY ("ArticleId", "TagId"),
    CONSTRAINT "FK_ArticleTags_Articles_ArticleId"
        FOREIGN KEY ("ArticleId") REFERENCES "Articles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ArticleTags_Tags_TagId"
        FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ArticleTags_TagId" ON "ArticleTags" ("TagId");
```

### How EF Core derives the schema from C# code

EF Core uses **conventions** to infer most of the schema:

| C# convention | EF Core inference |
|--------------|-------------------|
| Class name `Article` | Table name `Articles` (pluralized) |
| Property named `Id` (or `ArticleId`) | Primary key |
| `int` primary key | `INTEGER` + auto-increment |
| Non-nullable `string` | `TEXT NOT NULL` |
| `DateTime` | `TEXT NOT NULL` (in SQLite) |
| `int` enum property | `INTEGER NOT NULL` |

Things that **cannot** be inferred by convention (and must be configured in `OnModelCreating`):
- Composite primary keys (`ArticleTag`)
- Unique indexes
- Custom relationship configuration

---

## 9. Relationships and Foreign Keys

### The many-to-many problem

An article can have many tags. A tag can belong to many articles. This is a **many-to-many relationship**.

In a relational database, you cannot store a list of tag IDs directly in an `Articles` row (well, not without sacrificing normalization and queryability). The standard solution is a **join table**: a third table where each row says "this article is connected to this tag."

That is exactly what `ArticleTags` is. A row in `ArticleTags` like `(ArticleId=3, TagId=1)` means "article 3 has tag 1."

```
Articles            ArticleTags         Tags
────────────        ───────────────     ────────────
Id=3                ArticleId=3 ─────►  Id=1
Title="..."         TagId=1             Name="database"
...
```

### Navigation properties

Instead of writing joins yourself, EF Core lets you define **navigation properties** on entities. When you load an `Article` and include its tags, EF Core runs the join for you:

```csharp
var article = await _db.Articles
    .Include(a => a.ArticleTags)
        .ThenInclude(at => at.Tag)
    .FirstOrDefaultAsync(a => a.Id == id);

// Now you can access:
var tagNames = article.ArticleTags.Select(at => at.Tag.Name).ToList();
```

EF Core translates this to a SQL query with a JOIN across three tables.

### Cascade delete

Both foreign keys in `ArticleTags` are configured with `ON DELETE CASCADE`:

```
FOREIGN KEY ("ArticleId") REFERENCES "Articles" ("Id") ON DELETE CASCADE
FOREIGN KEY ("TagId")     REFERENCES "Tags" ("Id")     ON DELETE CASCADE
```

This means:
- When you delete an `Article`, all its `ArticleTag` rows are automatically deleted by the database.
- When you delete a `Tag`, all its `ArticleTag` rows are automatically deleted.

Without this, attempting to delete an article that has tags would fail with a foreign key constraint violation. The cascade rule prevents orphaned data.

In `ArticleService.DeleteAsync`:

```csharp
public async Task<bool> DeleteAsync(int id)
{
    var article = await _db.Articles.FindAsync(id);
    if (article is null) return false;

    _db.Articles.Remove(article);
    await _db.SaveChangesAsync();
    return true;
}
```

Notice that the code only removes the `Article`. It does not touch `ArticleTags` at all. The database handles that via cascade delete. EF Core also generates the correct SQL because it knows about the cascade relationship from the model configuration.

---

## 10. Indexes and Unique Constraints

An **index** is a database data structure (typically a B-tree) that allows rows to be found by a column's value quickly, without scanning the entire table. Without an index, finding an article by its slug would require reading every row in the `Articles` table.

### Indexes in this project

**`IX_Articles_Slug` — Unique index on `Article.Slug`**

```csharp
modelBuilder.Entity<Article>()
    .HasIndex(a => a.Slug)
    .IsUnique();
```

This does two things:
1. Makes slug lookups fast (e.g., `GET /api/articles/slug/{slug}`)
2. Prevents two articles from having the same slug (enforced at the database level)

**`IX_Tags_Name` — Unique index on `Tag.Name`**

```csharp
modelBuilder.Entity<Tag>()
    .HasIndex(t => t.Name)
    .IsUnique();
```

Prevents duplicate tag names and makes tag lookups by name fast.

**`IX_ArticleTags_TagId` — Non-unique index on `ArticleTag.TagId`**

EF Core automatically creates this index because `TagId` is a foreign key. It speeds up queries that filter or join by tag (e.g., "find all articles with tag X").

The `ArticleId` column in `ArticleTags` is part of the composite primary key, so it is already indexed by SQLite automatically.

### Why unique constraints matter

Enforcing uniqueness in the database (not just in application code) is important because:

1. **Concurrent requests:** Two requests might try to create an article with the same slug at almost the same time. Application code checks do not protect against this race condition. A database constraint does.
2. **Direct database access:** If someone inserts data directly with a SQL client or migration script, application-level checks are bypassed. Database constraints are always enforced.

---

## 11. Change Tracking

**Change tracking** is one of EF Core's most important (and most misunderstood) features. When you load an entity from the database, EF Core keeps a copy of its original state. When you call `SaveChanges`, EF Core compares the current state of each tracked entity to its original state and generates the minimal SQL to update only what changed.

### How it works, step by step

```csharp
// Step 1: Load article from database. EF Core tracks its original state.
var article = await _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .FirstOrDefaultAsync(a => a.Id == id);
// The context now holds: { Id=5, Title="Old Title", Status=Draft, ... }

// Step 2: Modify the entity in memory.
article.Title = "New Title";
article.Status = ArticleStatus.Published;
// The context detects: Title changed, Status changed

// Step 3: Save changes. EF Core generates an UPDATE for only the changed columns.
await _db.SaveChangesAsync();
// SQL: UPDATE Articles SET Title='New Title', Status=1 WHERE Id=5
```

EF Core does **not** update every column — only the ones that changed. This makes updates efficient.

### Entity states

Every tracked entity is in one of five states:

| State | Meaning |
|-------|---------|
| `Detached` | Not tracked by the context |
| `Unchanged` | Loaded from the database, no changes made |
| `Modified` | Loaded, then one or more properties changed |
| `Added` | Created in code and added to the context, not yet in the database |
| `Deleted` | Marked for deletion, not yet removed from the database |

When you call `SaveChanges`:
- `Added` → `INSERT`
- `Modified` → `UPDATE`
- `Deleted` → `DELETE`
- `Unchanged` → nothing

### How entities become tracked

```csharp
// Querying automatically tracks the result:
var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
// article is now Unchanged

// Adding tracks and marks as Added:
_db.Articles.Add(new Article { Title = "New" });
// The new article is Added

// Remove marks as Deleted:
_db.Articles.Remove(article);
// article is now Deleted
```

### The change tracker and navigation properties

When you add a new `Article` with `ArticleTags`:

```csharp
var article = new Article
{
    Title = "New Article",
    ArticleTags = new List<ArticleTag>
    {
        new ArticleTag { Tag = existingTag }
    }
};
_db.Articles.Add(article);
await _db.SaveChangesAsync();
```

EF Core's change tracker is smart enough to:
1. Insert the `Article` row first (to get the auto-generated `Id`)
2. Insert the `ArticleTag` row with the correct `ArticleId` from step 1
3. Not insert the `Tag` again (it already exists in the database)

This graph-aware behavior is one of the biggest advantages of EF Core over raw SQL.

---

## 12. SaveChanges: Committing Work to the Database

`SaveChanges` (and its async version `SaveChangesAsync`) is the method that sends all pending work to the database. Until you call it, changes only exist in memory.

```csharp
_db.Articles.Add(article);
// Nothing has been written to the database yet

await _db.SaveChangesAsync();
// NOW the INSERT runs
```

### What SaveChanges does internally

1. Inspects the change tracker for all modified/added/deleted entities
2. Orders operations correctly (inserts parents before children to satisfy foreign keys)
3. Wraps everything in a **database transaction**
4. Generates and executes the SQL statements
5. Reads back any database-generated values (like auto-incremented IDs)
6. Updates the tracked entities with those generated values (e.g., `article.Id` is now set)
7. Resets all entity states to `Unchanged`
8. Commits the transaction

### Transactions

By default, everything inside a single `SaveChanges` call is wrapped in an atomic transaction. Either all changes succeed, or none of them do. This is critical for maintaining data consistency.

For example, when creating an article with new tags in `ArticleService.CreateAsync`:

```csharp
// First SaveChanges: insert new tags (in ResolveTagsAsync)
await _db.SaveChangesAsync();

// Second SaveChanges: insert the article and its ArticleTag links
_db.Articles.Add(article);
await _db.SaveChangesAsync();
```

Note that this code calls `SaveChangesAsync` twice. The first call saves new tags; the second saves the article and its links. These are two separate transactions. If the second call fails, the tags will have been created but the article will not exist. In a higher-stakes scenario, you might wrap both operations in an explicit transaction using `_db.Database.BeginTransactionAsync()`.

### Why SaveChanges returns an integer

`SaveChanges` returns the number of rows affected. This is rarely used in this codebase, but it is useful for verifying that an expected update actually happened.

---

## 13. Migrations

**Migrations** are the mechanism by which EF Core keeps the database schema in sync with your C# entity classes. They are a versioned history of every change to the schema.

### The conceptual model

Imagine your database as a building under construction:
- Your C# entity classes define what the building *should* look like
- The database is what the building *currently* looks like
- A migration is a set of instructions for bringing the current building closer to the desired design

Each migration has two parts:
- **`Up()`:** The instructions to apply the change (e.g., add a column)
- **`Down()`:** The instructions to undo the change (e.g., remove the column)

### The current migration

This project has one migration: `InitialCreate`, created on 2026-03-15.

**File:** `src/WikiProject.Api/Migrations/20260315235834_InitialCreate.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Articles",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                .Annotation("Sqlite:Autoincrement", true),
            Title = table.Column<string>(type: "TEXT", nullable: false),
            Slug = table.Column<string>(type: "TEXT", nullable: false),
            // ... more columns
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Articles", x => x.Id);
        });

    // ... creates Tags and ArticleTags tables

    migrationBuilder.CreateIndex(
        name: "IX_Articles_Slug",
        table: "Articles",
        column: "Slug",
        unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "ArticleTags");
    migrationBuilder.DropTable(name: "Articles");
    migrationBuilder.DropTable(name: "Tags");
}
```

### Migration files explained

When you run `dotnet ef migrations add InitialCreate`, EF Core generates three files:

| File | What it is |
|------|------------|
| `20260315235834_InitialCreate.cs` | The migration logic (`Up` and `Down` methods) |
| `20260315235834_InitialCreate.Designer.cs` | Auto-generated metadata used by EF Core internally. Do not edit. |
| `WikiDbContextModelSnapshot.cs` | A snapshot of the entire model at the current migration point. Used to compute what changed for the *next* migration. |

The timestamp prefix (`20260315235834`) ensures migrations sort in creation order and prevents naming conflicts.

### The migrations history table

When `db.Database.Migrate()` runs, EF Core checks a special table called `__EFMigrationsHistory` in your database. This table records which migrations have already been applied:

```sql
-- What's inside __EFMigrationsHistory after first run:
MigrationId                          | ProductVersion
-------------------------------------|---------------
20260315235834_InitialCreate         | 10.0.5
```

Before applying a migration, EF Core checks this table. If the migration is already there, it skips it. This is how `Migrate()` is idempotent — safe to call on every startup without re-applying migrations.

### How to create a new migration

```bash
cd src/WikiProject.Api
dotnet ef migrations add <MigrationName>
```

Example: If you add a `ViewCount` property to the `Article` entity:

```bash
dotnet ef migrations add AddViewCountToArticles
```

EF Core compares the current model to `WikiDbContextModelSnapshot.cs` and generates:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "ViewCount",
        table: "Articles",
        type: "INTEGER",
        nullable: false,
        defaultValue: 0);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "ViewCount",
        table: "Articles");
}
```

### How to apply migrations

```bash
dotnet ef database update
```

Or, in production, via `db.Database.Migrate()` in `Program.cs` (which this project already does).

### Rollback

```bash
dotnet ef database update <PreviousMigrationName>
```

This runs the `Down()` methods of all migrations that came after the target. After rolling back, delete the migration file if you want to remove it from history.

### Common beginner confusion: migrations vs the database

> "I changed my entity class. Why doesn't my database have the new column?"

Because changing the C# class does not automatically change the database. You must:
1. Add a migration: `dotnet ef migrations add YourMigrationName`
2. Apply it: `dotnet ef database update` (or start the app, which calls `Migrate()`)

The database and the C# model are *separate things* that migrations keep in sync. The C# model is the desired state; the database is the actual state.

### Section recap

Migrations are a versioned, reversible history of schema changes. Each migration has an `Up()` to apply and a `Down()` to undo. EF Core tracks which migrations have been applied in the `__EFMigrationsHistory` table. This project applies migrations automatically at startup.

---

## 14. Seed Data

**Seed data** is initial data inserted into the database when it is first set up. It gives developers a realistic starting point without having to manually create data after every clean install.

### Seed data in this project

**File:** `src/WikiProject.Api/Data/SeedData.cs`

```csharp
public static class SeedData
{
    public static async Task SeedAsync(WikiDbContext db)
    {
        if (await db.Articles.AnyAsync())
            return;  // Already seeded, do nothing
        
        // ... create tags and articles
    }
}
```

The guard clause `if (await db.Articles.AnyAsync()) return;` prevents re-seeding. If the `Articles` table has any rows at all, seeding is skipped entirely. This is the simplest possible idempotency check.

### What gets seeded

**Tags (7):**

```
getting-started, guide, api, database, architecture, tips, reference
```

**Articles (6):**

| Title | Slug | Category | Status | Tags |
|-------|------|----------|--------|------|
| Welcome to WikiProject | `welcome-to-wikiproject` | General | Published | getting-started, guide |
| API Reference Overview | `api-reference-overview` | Reference | Published | api, reference |
| Setting Up the Development Environment | `setting-up-dev-environment` | DevOps | Published | getting-started, guide |
| Database Schema and EF Core Setup | `database-schema-ef-core` | Architecture | Published | database, architecture |
| Writing Good Knowledge Base Articles | `writing-good-kb-articles` | General | Published | guide, tips |
| Authentication Integration (Planned) | `authentication-integration-planned` | Architecture | Draft | architecture, tips |

### How the seed data creates relationships

```csharp
// Tags are created first and stored in a dictionary keyed by name
var tags = tagNames.Select(n => new Tag { Name = n }).ToList();
db.Tags.AddRange(tags);
await db.SaveChangesAsync();

var tagMap = tags.ToDictionary(t => t.Name);

// Articles reference tags from the dictionary
new Article
{
    Title = "Welcome to WikiProject",
    // ...
    ArticleTags = new List<ArticleTag>
    {
        new ArticleTag { Tag = tagMap["getting-started"] },
        new ArticleTag { Tag = tagMap["guide"] }
    }
}
```

`new ArticleTag { Tag = tagMap["getting-started"] }` creates a join table entry that links the article to the tag. EF Core resolves the ID values when it saves the graph because the `Tag` object is already tracked (it was just saved in the first `SaveChangesAsync` call).

### How to reset the database

To start fresh with clean seed data:

1. Delete `wiki.db` from `src/WikiProject.Api/`
2. Restart the application

`db.Database.Migrate()` will recreate the database and run `InitialCreate` again. `SeedData.SeedAsync` will then re-insert the sample data.

### Alternative seeding strategies

EF Core also supports **model-level seed data** via `modelBuilder.Entity<Tag>().HasData(...)`. This approach bakes seed data into migrations, which means:
- The data is always present after `dotnet ef database update`
- Changes to seed data require a new migration
- IDs must be hardcoded (EF Core cannot auto-increment in `HasData`)

The current approach (code-based seeding in `SeedData.cs`) is more flexible. It is the right choice for sample/development data. The `HasData` approach is better for reference data that is part of the schema (e.g., a list of countries that should always exist).

---

## 15. Schema Evolution: What Happens When You Add a Field?

This is one of the most important practical topics. The database schema will change as the application evolves. Here is the full workflow.

### Scenario: adding a ViewCount field to Article

**Step 1: Add the property to the entity**

```csharp
// In Entities/Article.cs:
public class Article
{
    // ... existing properties ...
    public int ViewCount { get; set; } = 0;  // new property
}
```

**Step 2: Create a migration**

```bash
cd src/WikiProject.Api
dotnet ef migrations add AddViewCountToArticles
```

EF Core compares the current `Article` class to the `WikiDbContextModelSnapshot.cs` and generates:

```csharp
// In Migrations/20260320000000_AddViewCountToArticles.cs:
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "ViewCount",
        table: "Articles",
        type: "INTEGER",
        nullable: false,
        defaultValue: 0);  // Existing rows get ViewCount = 0
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "ViewCount",
        table: "Articles");
}
```

**Step 3: Apply the migration**

Either run `dotnet ef database update`, or restart the app (which runs `db.Database.Migrate()` automatically).

**Step 4: Use the new field**

The `ViewCount` property is now available in all queries and can be written like any other property.

### What happens to existing data

When a column is added with `defaultValue: 0`, all existing rows in the `Articles` table get the value `0` for the new column. This is the safest approach. If you need a non-zero default, set it in `defaultValue`.

If the new column is non-nullable and you do not provide a default value, the migration will fail on a non-empty table because existing rows cannot satisfy the `NOT NULL` constraint. Always set a sensible default for non-nullable columns.

### Adding a nullable column

If the new field is optional:

```csharp
public string? ExternalUrl { get; set; }
```

EF Core generates a nullable column with no default, and existing rows get `NULL`. This is safe.

### Adding a column with a required relationship

If you add a required navigation property (a new foreign key), you need to think carefully about what value existing rows will get for the foreign key column. This is one of the trickier migration scenarios. Options include:
- Making the foreign key nullable
- Providing a default value pointing to a known existing row
- Populating the column in a subsequent data migration

### What you must NOT do

Do not modify an existing migration file that has already been applied to any real database. Migrations are immutable history. If you modify them, the `__EFMigrationsHistory` table will no longer match the actual migration content, and EF Core's idempotency checks will break.

If you make a mistake in a migration that has not yet been applied to production, you can:
1. Roll back locally: `dotnet ef database update PreviousMigrationName`
2. Delete the bad migration file
3. Make the correct change and regenerate

---

## 16. Slugs, Tags, Categories, and Statuses as Stored Data

### Slugs

A **slug** is a URL-safe, human-readable identifier for an article. Instead of `GET /api/articles/42`, you can use `GET /api/articles/slug/setting-up-dev-environment`. Slugs make URLs meaningful and bookmarkable.

**How slugs are generated** (in `ArticleService`):

```csharp
private static string GenerateSlug(string title)
{
    var slug = title.ToLower().Trim();
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");  // remove special chars
    slug = Regex.Replace(slug, @"\s+", "-");             // spaces to hyphens
    slug = Regex.Replace(slug, @"-+", "-");              // collapse multiple hyphens
    slug = slug.Trim('-');                               // strip leading/trailing hyphens
    return slug.Length > 100 ? slug[..100] : slug;       // cap at 100 chars
}
```

`"Setting Up the Dev Environment!"` → `"setting-up-the-dev-environment"`

**Uniqueness enforcement** happens at two levels:
1. In application code: `EnsureUniqueSlugAsync` appends `-1`, `-2`, etc. if the slug already exists
2. In the database: the `UNIQUE INDEX` on `Slug` prevents duplicate slugs even in race conditions

**As stored data:** `Slug` is a `TEXT` column with a unique index. It is stored exactly as generated (lowercase, hyphenated).

### Tags

Tags are stored in a **normalized** separate table rather than as a comma-separated string in the `Articles` row. This is the right approach because:

- It allows filtering articles by tag efficiently (via an index join)
- It prevents data inconsistencies (e.g., `"api"` vs `"API"` counted as different tags)
- It makes the tag list queryable without string parsing

**Tag normalization** happens in `ResolveTagsAsync`:

```csharp
var normalized = tagNames
    .Select(t => t.Trim().ToLower())
    .Where(t => !string.IsNullOrEmpty(t))
    .Distinct()
    .ToList();
```

All tags are lowercased and deduplicated before lookup or creation.

**As stored data:** A `Tags` row looks like `(Id=3, Name="api")`. The `ArticleTags` join table records which articles have which tags: `(ArticleId=2, TagId=3)`.

**Tag creation is implicit:** If an article is submitted with tag `"docker"` and no `Tag` row with that name exists, `ResolveTagsAsync` creates it:

```csharp
var newTags = normalized
    .Where(n => !existingNames.Contains(n))
    .Select(n => new Tag { Name = n })
    .ToList();

if (newTags.Count > 0)
    _db.Tags.AddRange(newTags);
```

This means the `Tags` table grows automatically as new tags are used. There is no separate "manage tags" UI required.

### Categories

Unlike tags, categories are **not normalized** — they are stored as a plain string in the `Articles.Category` column:

```
"General", "DevOps", "Architecture", "Reference"
```

There is no `Categories` table. The list of available categories is derived by querying distinct values:

```csharp
public async Task<IReadOnlyList<string>> GetCategoriesAsync()
{
    return await _db.Articles
        .AsNoTracking()
        .Select(a => a.Category)
        .Distinct()
        .OrderBy(c => c)
        .ToListAsync();
}
```

**Trade-off:** This is simpler than maintaining a separate `Categories` table, but it means category names can drift over time (e.g., `"Devops"` vs `"DevOps"`). For a small internal tool, this is an acceptable trade-off. A more rigorous approach would use a `Categories` table with a foreign key, similar to `Tags`.

### Status

Status is an enum stored as an integer:

| Enum value | Integer stored | Meaning |
|-----------|----------------|---------|
| `Draft` | `0` | Work in progress, not visible to readers |
| `Published` | `1` | Live and visible |
| `Archived` | `2` | Retired, kept for reference |

The integer is stored in the `Articles.Status` column. When EF Core reads a row, it converts `1` to `ArticleStatus.Published` automatically.

When the API returns article data, the status is serialized as a string (`"Published"`) rather than the integer. This happens in the mapping layer:

```csharp
// In ArticleMappings.cs:
article.Status.ToString()  // converts ArticleStatus.Published to "Published"
```

This makes the API response more readable for frontend consumers without requiring the frontend to know what `1` means.

---

## 17. Querying the Database: How the Service Layer Uses EF Core

The `ArticleService` class is where all database interaction happens. It uses EF Core's LINQ provider to build queries that are translated to SQL.

### Loading articles with related data

Every article query that returns tags uses `.Include` and `.ThenInclude`:

```csharp
var article = await _db.Articles
    .Include(a => a.ArticleTags)
        .ThenInclude(at => at.Tag)
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

Without `.Include`, the `ArticleTags` collection on the loaded article would be empty (or null), and tag names would not be available.

### Filtering and searching

```csharp
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

EF Core translates this to a SQL `WHERE` clause with `LIKE` conditions. The `.ToLower().Contains(search)` pattern generates SQL like:

```sql
WHERE lower(a.Title) LIKE '%searchterm%'
   OR lower(a.Summary) LIKE '%searchterm%'
   OR ...
```

> **Performance note:** `LIKE '%term%'` with a leading wildcard cannot use an index — it requires a full table scan. For a small knowledge base, this is fine. For large datasets, you would use a full-text search system (SQLite FTS5, PostgreSQL `tsvector`, or a dedicated search service like Elasticsearch).

### Pagination

```csharp
var items = await q
    .OrderByDescending(a => a.UpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

EF Core translates this to:

```sql
SELECT ...
FROM Articles a
ORDER BY a.UpdatedAt DESC
LIMIT @pageSize OFFSET @skip
```

Pagination is essential for performance — without it, every list request would load all articles.

### Deferred execution

EF Core queries are **lazy by default**. The query is not actually sent to the database until you call a "terminal" method like:

- `.ToListAsync()` — execute and get all results
- `.FirstOrDefaultAsync()` — execute and get one result (or null)
- `.CountAsync()` — execute and get a count
- `.AnyAsync()` — execute and get a boolean

This means you can build up a query with multiple `.Where()` calls before executing it:

```csharp
var q = _db.Articles.AsQueryable();
// q is a query object, not data yet

if (!string.IsNullOrWhiteSpace(query.Search))
    q = q.Where(a => a.Title.Contains(query.Search));  // still not executed

if (query.Status.HasValue)
    q = q.Where(a => a.Status == query.Status.Value);  // still not executed

var results = await q.ToListAsync();  // NOW it executes
```

This composability is one of LINQ's most powerful features.

---

## 18. AsNoTracking and When to Use It

When EF Core loads entities, it normally tracks them in the change tracker (as described in section 11). For read-only queries where you do not need to modify and save the entities, this tracking wastes memory and CPU.

**`AsNoTracking()`** tells EF Core to load entities without tracking them:

```csharp
var articles = await _db.Articles
    .Include(a => a.ArticleTags)
        .ThenInclude(at => at.Tag)
    .AsNoTracking()
    .ToListAsync();
```

The returned `Article` objects are **detached** — they hold their data, but the EF Core context does not track them. Modifying a detached entity and calling `SaveChanges` would have no effect.

### When to use it

Use `AsNoTracking()` when:
- You are doing a read-only operation (displaying data, returning it in an API response)
- You will not modify the returned entities
- Performance matters (less overhead)

Do **not** use `AsNoTracking()` when:
- You intend to modify the entity and call `SaveChanges`
- You need EF Core to detect changes automatically

In this project, all list and single-get queries use `.AsNoTracking()` because they are read-only. The `UpdateAsync` and `DeleteAsync` methods do **not** use it because they need tracked entities:

```csharp
// UpdateAsync — no AsNoTracking, because we need change tracking
var article = await _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .FirstOrDefaultAsync(a => a.Id == id);

article.Title = request.Title;  // change tracked
await _db.SaveChangesAsync();   // EF Core generates UPDATE
```

---

## 19. Comparisons and Alternatives

### EF Core vs Dapper

| | EF Core | Dapper |
|--|---------|--------|
| **Query style** | LINQ (C#) | SQL strings |
| **Schema management** | Migrations | Manual or separate tool |
| **Change tracking** | Yes | No |
| **Performance** | Good | Excellent for reads |
| **Learning curve** | Higher | Lower |
| **Best for** | Full CRUD apps with schema evolution | Read-heavy services, complex queries |

Dapper is an extension on `IDbConnection` that maps query results to objects. You write SQL yourself, which gives you full control but requires SQL knowledge and manual maintenance.

Example comparison:

```csharp
// EF Core:
var article = await _db.Articles
    .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
    .FirstOrDefaultAsync(a => a.Id == id);

// Dapper (equivalent):
var article = await connection.QueryFirstOrDefaultAsync<Article>(
    @"SELECT a.*, at.TagId, t.Name
      FROM Articles a
      LEFT JOIN ArticleTags at ON at.ArticleId = a.Id
      LEFT JOIN Tags t ON t.Id = at.TagId
      WHERE a.Id = @Id",
    new { Id = id }
);
// Note: you'd still need to manually assemble the ArticleTags collection
```

Both are valid choices. EF Core is better when you need migrations and change tracking. Dapper is better when SQL optimization is critical.

### EF Core vs raw SQL

EF Core does not prevent you from writing raw SQL when needed. The escape hatch is:

```csharp
var articles = await _db.Articles
    .FromSqlRaw("SELECT * FROM Articles WHERE Status = 1")
    .ToListAsync();
```

Or for non-entity queries:

```csharp
var count = await _db.Database.ExecuteSqlRawAsync(
    "UPDATE Articles SET ViewCount = ViewCount + 1 WHERE Id = {0}", id);
```

Raw SQL in EF Core is useful when:
- LINQ cannot express the query you need
- Performance requires hand-tuned SQL
- You need database-specific features (e.g., SQLite FTS5)

### SQLite vs SQL Server vs PostgreSQL (extended)

| Feature | SQLite | SQL Server | PostgreSQL |
|---------|--------|------------|------------|
| **Write concurrency** | Single writer | High (row-level locking) | High (MVCC) |
| **Horizontal scaling** | No | Yes (replicas) | Yes (replicas, Citus) |
| **JSON support** | Limited (JSON1 extension) | Good (JSON functions) | Excellent (jsonb) |
| **Full-text search** | FTS5 (limited EF support) | Full-text indexes | tsvector, tsquery |
| **Geographic data** | No | Yes (Spatial) | Yes (PostGIS) |
| **Maintenance** | None | DBA work | Some DBA work |
| **Connection pooling** | Not needed | PgBouncer / built-in | PgBouncer |
| **Docker image size** | N/A (embedded) | Large | Small |
| **EF Core package** | `EFCore.Sqlite` | `EFCore.SqlServer` | `Npgsql.EFCore.PostgreSQL` |

For WikiProject's use case (internal tool, low write volume, single server), SQLite is the right choice. If the app grew to hundreds of simultaneous users or needed to run on multiple servers, PostgreSQL would be the natural next step.

---

## 20. Common Beginner Confusion

### "I changed the entity but the database didn't update"

You must create a migration and apply it. Changing a C# class does not automatically change the database. See [section 15](#15-schema-evolution-what-happens-when-you-add-a-field).

### "My query returns articles but ArticleTags is empty"

You forgot `.Include(a => a.ArticleTags).ThenInclude(at => at.Tag)`. EF Core does not load related data unless you explicitly ask for it. This behavior is called **lazy loading disabled by default** — EF Core Core uses explicit loading unless you opt into lazy loading.

### "I modified an entity with AsNoTracking but SaveChanges did nothing"

`AsNoTracking()` means the entity is detached — the context does not track it and will not generate SQL for it. You cannot modify a no-tracking entity through the context. If you need to modify it, load it without `AsNoTracking()`.

### "I deleted an article but got a foreign key error"

This would happen if cascade delete was not configured. In this project, cascade delete is configured for `ArticleTags`, so deleting an article also removes its tag links. If you add a new entity with a foreign key to `Article`, make sure cascade behavior is configured.

### "Migrations are out of sync"

If you edit a migration file after applying it, the `__EFMigrationsHistory` table will still record the original migration ID, but the file has changed. EF Core does not re-detect this. The only safe fix is to roll back all affected migrations and regenerate them.

### "What is the `null!` syntax on navigation properties?"

```csharp
public Article Article { get; set; } = null!;
```

This tells C#'s nullable reference type analyzer "this will not be null at runtime, trust me." It is used on navigation properties because EF Core populates them from the database, but C#'s static analysis cannot know that. The `null!` suppresses the warning. If you access these properties before loading the entity from the database, you will get a `NullReferenceException` at runtime.

### "Is the DbContext thread-safe?"

No. `DbContext` is not thread-safe and should not be shared across threads. In ASP.NET Core, each HTTP request gets its own `DbContext` instance (scoped lifetime), which is the correct pattern.

### "My dates look wrong in the database"

SQLite stores `DateTime` as ISO-8601 text strings. The format will be `"2026-03-15T23:58:34.0000000Z"` (UTC). When EF Core loads them, it converts them back to `DateTime` objects. If you read the `.db` file with a SQLite browser, dates look like strings, not like formatted dates. This is normal.

---

## 21. What to Study Next

### Official documentation

- [EF Core documentation (Microsoft)](https://learn.microsoft.com/en-us/ef/core/) — the canonical reference
- [EF Core change tracking (Microsoft)](https://learn.microsoft.com/en-us/ef/core/change-tracking/) — deep dive on the topic
- [EF Core migrations (Microsoft)](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) — full migration reference
- [SQLite documentation](https://www.sqlite.org/docs.html) — SQLite's own reference
- [EF Core performance (Microsoft)](https://learn.microsoft.com/en-us/ef/core/performance/) — optimization guide

### Related documentation in this project

- **API layer doc** (future): How controllers call the service layer and handle HTTP status codes
- **Service layer doc** (future): Business logic, slug generation, tag resolution in detail
- **Frontend doc** (future): How the React app consumes the API and displays article data

### Key concepts to explore next

1. **Eager vs lazy loading** — Loading related data explicitly (`.Include`) vs automatically on property access
2. **Query splitting** — EF Core's `.AsSplitQuery()` for queries that generate large Cartesian products with multiple includes
3. **Compiled queries** — Pre-compiling frequently executed queries for performance
4. **Value converters** — Storing custom types (e.g., enums as strings, value objects) in the database
5. **Database transactions** — `_db.Database.BeginTransactionAsync()` for multi-step operations
6. **Optimistic concurrency** — Using a `RowVersion` or `Timestamp` column to detect conflicting updates
7. **Integration testing with EF Core** — Using `UseInMemoryDatabase` or SQLite in-memory for test isolation
8. **Repository pattern** — Abstracting `DbContext` behind an interface for easier testing and swapping

---

*This document covers the database and persistence layer of WikiProject as of the `InitialCreate` migration (2026-03-15). If the schema has been extended since then, check the `Migrations/` directory for newer migration files and update this document accordingly.*
