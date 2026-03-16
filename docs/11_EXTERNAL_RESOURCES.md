# 11 — External Resources

A curated guide to learning and reference material for every technology used in this project. This is **not** a general internet dump. Every resource below is chosen because it directly helps you understand, build on, or debug some part of this codebase.

---

## How to Use This Guide

Each section corresponds to a layer or concept in this project. If you are completely new, work through resources marked **beginner** first, then **intermediate**, then **advanced**. If you already know the language but are new to a specific framework, jump directly to the relevant section.

At the start of each section you will find a one-sentence reminder of *where* that technology appears in the codebase so you always have context for why you are reading.

---

## Table of Contents

1. [.NET](#1-net)
2. [ASP.NET Core](#2-aspnet-core)
3. [React](#3-react)
4. [TypeScript](#4-typescript)
5. [Entity Framework Core](#5-entity-framework-core)
6. [SQLite](#6-sqlite)
7. [REST APIs](#7-rest-apis)
8. [Dependency Injection](#8-dependency-injection)
9. [Validation](#9-validation)
10. [Routing](#10-routing)
11. [Frontend State and Forms](#11-frontend-state-and-forms)
12. [Software Architecture Basics](#12-software-architecture-basics)

---

## 1. .NET

> **Where it appears:** The entire backend (`src/WikiProject.Api/`) targets `net10.0`. Every `.cs` file compiles with the .NET SDK.

.NET is Microsoft's open-source, cross-platform developer platform. The backend of this project is a .NET 10 application — the runtime that executes the compiled C# code, the base class library that provides collections, strings, async/await, LINQ, and everything else C# code calls upon.

Understanding .NET itself (as distinct from ASP.NET Core or EF Core) gives you the mental model for things like the garbage collector, the `Task`-based async pattern, nullable reference types, and LINQ — all of which appear throughout `ArticleService.cs` and the controllers.

### Why This Matters

This project uses `net10.0`, which is .NET 10. All the modern C# features you'll see (record types, nullable annotations, top-level statements in `Program.cs`, implicit usings, pattern matching) are .NET features, not ASP.NET Core features. Confusing the two is a very common beginner mistake.

### Common Beginner Confusion

- **"Is .NET the same as C#?"** No. C# is the programming language. .NET is the runtime and platform. You could write .NET code in F# or Visual Basic. You cannot run C# without .NET.
- **"Which .NET version should I care about?"** This project uses .NET 10. Anything targeting .NET 5+ is part of the unified modern .NET (not the old .NET Framework 4.x).

---

### Resources

#### Microsoft .NET Documentation — Official Landing Page
- **Link:** <https://learn.microsoft.com/en-us/dotnet/>
- **What it covers:** The authoritative top-level documentation for everything in the .NET platform: runtime, SDK, languages, tools, NuGet, and all framework components.
- **Why it is relevant:** This is the canonical reference when you need to look up any .NET API or feature used in the project.
- **Difficulty:** Beginner → Advanced (organized by topic; start with "Get Started")

#### C# Documentation — Official Microsoft Docs
- **Link:** <https://learn.microsoft.com/en-us/dotnet/csharp/>
- **What it covers:** The complete C# language specification, tutorials, and reference. Covers features like `async`/`await`, LINQ, records, nullable reference types, pattern matching, and top-level statements — all of which appear in this project.
- **Why it is relevant:** `ArticleService.cs` uses LINQ (`Where`, `Select`, `OrderByDescending`, `Distinct`), `async`/`await` throughout, and nullable reference types (`string?`, `ArticleDto?`). This doc explains all of them.
- **Difficulty:** Beginner → Advanced

#### C# Language Reference — async/await
- **Link:** <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/>
- **What it covers:** How the `async` and `await` keywords work, the `Task<T>` type, how .NET schedules asynchronous work without blocking threads.
- **Why it is relevant:** Every public method in `ArticleService` and every controller action is `async`. Understanding `Task<T>` vs `void`, what `await` actually does, and why you should never use `.Result` synchronously is essential to read and write the backend code correctly.
- **Difficulty:** Intermediate

#### LINQ (Language-Integrated Query) Overview
- **Link:** <https://learn.microsoft.com/en-us/dotnet/csharp/linq/>
- **What it covers:** LINQ syntax and method syntax, deferred vs. immediate execution, common operators (`Where`, `Select`, `OrderBy`, `GroupBy`, `Any`, `FirstOrDefault`).
- **Why it is relevant:** `ArticleService.GetArticlesAsync` builds a LINQ query against `WikiDbContext` and adds `Where` clauses conditionally. The distinction between LINQ-to-Objects and LINQ-to-EF (where queries are translated to SQL) is critical.
- **Difficulty:** Beginner → Intermediate

#### Nullable Reference Types in C#
- **Link:** <https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references>
- **What it covers:** The `?` annotation on reference types, the compiler's flow analysis, `#nullable enable`, and why this feature was added.
- **Why it is relevant:** The project has `<Nullable>enable</Nullable>` in the `.csproj`. Return types like `ArticleDto?` and `Article?` rely on this feature. Without understanding it you will misread method signatures and get unexpected null-reference warnings.
- **Difficulty:** Intermediate

---

**Section recap:** .NET provides the runtime, C#, and the base class library. Every async pattern, LINQ expression, and null annotation in the backend is a .NET/C# feature. Start with the C# documentation if you are new to the language; refer to the async and LINQ guides when reading `ArticleService.cs`.

---

## 2. ASP.NET Core

> **Where it appears:** `Program.cs` (the application startup and middleware pipeline), `Controllers/ArticlesController.cs`, `Controllers/MetadataController.cs`, and the service registration (`AddDbContext`, `AddScoped`, `AddCors`).

ASP.NET Core is the web framework that sits on top of .NET. It handles incoming HTTP requests, routes them to the right controller action, calls your service code, and sends a response back. It also provides the middleware pipeline (CORS, Swagger, logging), the dependency injection container, and the `[ApiController]` infrastructure.

### Why This Matters

If you want to add a new endpoint, change the CORS policy, add authentication, or understand why `Program.cs` is structured the way it is, you need ASP.NET Core knowledge. The entire HTTP lifecycle — from a request arriving at `GET /api/articles?search=foo` to the JSON response the React app receives — is handled by ASP.NET Core.

### Common Beginner Confusion

- **"What is `WebApplication.CreateBuilder`?"** It is the entry point for the modern "minimal hosting" model introduced in .NET 6. It replaces the older `Startup.cs` / `ConfigureServices` pattern. This project uses the modern model: `Program.cs` is the only startup file.
- **"What is `[ApiController]`?"** It is an attribute that adds several default behaviors: automatic model validation, automatic `400 Bad Request` responses, `[FromBody]` inference, and `[FromRoute]` inference. This is why the controller code is so clean — ASP.NET Core handles boilerplate automatically.
- **"What is middleware?"** Middleware are components arranged in a pipeline. Each component can inspect/modify the request before passing it to the next component. In `Program.cs`: `app.UseCors()` and `app.MapControllers()` are middleware registrations.

---

### Resources

#### ASP.NET Core Documentation — Official
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/>
- **What it covers:** Everything from the hosting model, middleware, routing, controllers, filters, dependency injection, configuration, and logging to advanced topics like authentication and caching.
- **Why it is relevant:** This is the canonical reference for everything in `Program.cs` and the controllers. Start with "Get Started" → "Web APIs" to understand how this project is structured.
- **Difficulty:** Beginner → Advanced

#### Create a Web API with ASP.NET Core — Tutorial
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api>
- **What it covers:** Step-by-step guide to building a REST API with controllers, EF Core, and SQLite — which is exactly what this project is.
- **Why it is relevant:** This tutorial is the canonical "hello world" for this exact technology combination. Working through it gives you a concrete mental model for how `ArticlesController.cs` is organized.
- **Difficulty:** Beginner

#### Controller-based APIs in ASP.NET Core
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/web-api/>
- **What it covers:** `ControllerBase`, `[ApiController]`, `[Route]`, `[HttpGet]`/`[HttpPost]`/etc., action return types (`IActionResult`, `ActionResult<T>`), model binding, and problem details.
- **Why it is relevant:** `ArticlesController` uses all of these. Understanding `ActionResult<ArticleDto>` vs `IActionResult`, and how `CreatedAtAction` constructs the `Location` header, requires this reference.
- **Difficulty:** Intermediate

#### Middleware in ASP.NET Core
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/>
- **What it covers:** What middleware is, how the pipeline is ordered, and how to write custom middleware. Covers built-in middleware like CORS and routing.
- **Why it is relevant:** `Program.cs` in this project configures a middleware pipeline. The ordering of `app.UseCors()` before `app.MapControllers()` is not arbitrary — it matters.
- **Difficulty:** Intermediate

#### Configuration in ASP.NET Core
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/>
- **What it covers:** `appsettings.json`, environment variables, `IConfiguration`, the `GetConnectionString` / `GetSection` APIs.
- **Why it is relevant:** `Program.cs` uses `builder.Configuration.GetConnectionString("Default")` and `builder.Configuration.GetSection("Cors:AllowedOrigins")`. Understanding how `appsettings.json` and `appsettings.Development.json` layer together is essential to configuring the app correctly.
- **Difficulty:** Beginner → Intermediate

#### Logging in ASP.NET Core
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/>
- **What it covers:** The `ILogger<T>` interface, log levels, structured logging with message templates, and console/file providers.
- **Why it is relevant:** `ArticleService` and `ArticlesController` both accept `ILogger<T>` via constructor injection and call `_logger.LogInformation(...)` with structured message templates (`{Id}`, `{Title}`). This doc explains what that syntax means and how to filter log output.
- **Difficulty:** Beginner

#### Swagger / OpenAPI with ASP.NET Core (Swashbuckle)
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle>
- **What it covers:** How to add Swagger UI to an ASP.NET Core API, configure `SwaggerDoc`, and use XML comments for documentation.
- **Why it is relevant:** `Program.cs` registers Swashbuckle (`AddSwaggerGen`) and enables the Swagger UI at `/swagger`. This is the interactive API documentation UI that's available when you run the backend in development.
- **Difficulty:** Beginner

---

**Section recap:** ASP.NET Core is the HTTP backbone of the backend. It routes requests, runs middleware, manages DI, and serializes JSON. The official Microsoft docs and the "first web API" tutorial together give you everything you need to understand and extend the controllers and `Program.cs`.

---

## 3. React

> **Where it appears:** The entire `frontend/src/` directory. `App.tsx` is the root component. Pages live in `frontend/src/pages/`, reusable components in `frontend/src/components/`, and custom hooks in `frontend/src/hooks/`.

React is a JavaScript/TypeScript library for building user interfaces by composing small, reusable *components*. A component is a function that takes *props* (inputs) and returns JSX (markup). React re-renders components when their *state* or props change. This project uses React 19 — the latest stable version at time of writing.

### Why This Matters

All the UI in this project is React. Understanding how components, props, state, effects, and hooks fit together is the prerequisite for reading any file in `frontend/src/`. React also owns the concept of the *virtual DOM* — the mechanism that makes UI updates efficient.

### Common Beginner Confusion

- **"What is JSX?"** JSX looks like HTML inside JavaScript/TypeScript files but it is not HTML. It is a syntax extension that the TypeScript compiler transforms into `React.createElement(...)` calls. The `.tsx` extension means TypeScript + JSX.
- **"Why can't I use a regular `for` loop in JSX?"** JSX expects an *expression*, not a *statement*. Use `.map()` to render lists instead.
- **"When does a component re-render?"** When its state changes (via `useState` setter), when a parent re-renders and passes new props, or when a context it subscribes to changes.

---

### Resources

#### React — Official Documentation
- **Link:** <https://react.dev/>
- **What it covers:** The complete official React documentation, entirely rewritten for the hooks era. Covers components, JSX, state, effects, refs, context, custom hooks, and performance. The interactive "learn" section is particularly good.
- **Why it is relevant:** This project uses only function components and hooks — the modern React model. The official docs teach exactly this model. Pages like `ArticlesPage.tsx` and `ArticleDetailPage.tsx` use `useState`, `useEffect`, and custom hooks, all explained here.
- **Difficulty:** Beginner → Intermediate

#### React — Quick Start
- **Link:** <https://react.dev/learn>
- **What it covers:** A one-page tour of components, JSX, state, and event handling. The fastest on-ramp to reading React code.
- **Why it is relevant:** Read this before opening any `.tsx` file in the project.
- **Difficulty:** Beginner

#### React — `useState` Hook
- **Link:** <https://react.dev/reference/react/useState>
- **What it covers:** How `useState` works, the difference between the current state value and the setter function, functional updates, and when state updates are batched.
- **Why it is relevant:** Every component and custom hook in this project uses `useState`. For example, `useArticles.ts` holds `data`, `loading`, and `error` as separate state variables.
- **Difficulty:** Beginner

#### React — `useEffect` Hook
- **Link:** <https://react.dev/reference/react/useEffect>
- **What it covers:** How effects synchronize React components to external systems (API calls, timers, subscriptions), the dependency array, and cleanup functions.
- **Why it is relevant:** `useArticles.ts` and `useArticle.ts` call the backend API inside effects triggered by `useEffect`. Understanding the dependency array is essential to avoid infinite request loops — a classic beginner mistake.
- **Difficulty:** Intermediate

#### React — `useCallback` Hook
- **Link:** <https://react.dev/reference/react/useCallback>
- **What it covers:** How `useCallback` memoizes a function so it is not recreated on every render. When it helps and when it does not.
- **Why it is relevant:** `useArticles.ts` wraps the `fetch` function in `useCallback` with `[key]` as a dependency. This is a specific pattern to avoid re-triggering the effect when the filters have not changed.
- **Difficulty:** Intermediate

#### Vite — Official Documentation
- **Link:** <https://vitejs.dev/guide/>
- **What it covers:** Vite's development server, HMR (hot module replacement), build configuration, environment variables (`import.meta.env`), and plugins.
- **Why it is relevant:** The frontend uses Vite 8 as the build tool (`vite.config.ts`). The `VITE_API_URL` environment variable in `articleService.ts` is a Vite-specific convention. Understanding how to configure the dev server and proxy is necessary for local development.
- **Difficulty:** Beginner

---

**Section recap:** React is all of the frontend. Components receive props, manage local state with `useState`, run side effects with `useEffect`, and are composed into pages. The official React documentation at react.dev is the single best resource and is completely up to date with the hooks model this project uses.

---

## 4. TypeScript

> **Where it appears:** Every file in `frontend/src/` has a `.ts` or `.tsx` extension. Types are defined in `frontend/src/types/index.ts`. The `tsconfig.json` and `tsconfig.app.json` configure the TypeScript compiler.

TypeScript is a *typed superset of JavaScript* — every valid JavaScript is valid TypeScript, but TypeScript adds optional type annotations that are checked at compile time. In this project it means that the `Article` type defined in `types/index.ts` is used consistently across the service layer, the hooks, and the components. If you pass the wrong shape of object, the compiler tells you before the browser does.

### Why This Matters

TypeScript is what makes the frontend maintainable at scale. Without it, a refactor to `Article.slug` would require manually hunting every usage. With TypeScript, the compiler flags every place that breaks. Understanding TypeScript's type system is required to read and extend `types/index.ts` and `articleService.ts`.

### Common Beginner Confusion

- **"Does TypeScript run in the browser?"** No. TypeScript is compiled to plain JavaScript before being served. The `.tsx` files are compiled by `tsc` (or Vite's internal esbuild transform) and never reach the browser as TypeScript.
- **"What is the difference between `type` and `interface`?"** In most practical cases they are interchangeable. Interfaces support declaration merging (you can re-open them and add members). Types support union and intersection types more naturally. This project uses both.
- **"What does `import type` mean?"** The `import type` syntax imports a type only for compile-time checking; it is erased completely from the output. This avoids circular imports and keeps bundles smaller.

---

### Resources

#### TypeScript — Official Handbook
- **Link:** <https://www.typescriptlang.org/docs/handbook/intro.html>
- **What it covers:** The complete TypeScript language guide: basic types, interfaces, generics, union and intersection types, type narrowing, utility types (`Partial<T>`, `Readonly<T>`, `Pick<T,K>`), and `tsconfig` options.
- **Why it is relevant:** The `types/index.ts` file defines `Article`, `ArticleListResponse`, `ArticleFilters`, `CreateArticleRequest`, and `UpdateArticleRequest` — all TypeScript interfaces. These are the shapes that flow through `articleService.ts` into the hooks and components.
- **Difficulty:** Beginner → Advanced

#### TypeScript — The Basics (interactive introduction)
- **Link:** <https://www.typescriptlang.org/docs/handbook/2/basic-types.html>
- **What it covers:** Type annotations on variables and function parameters, the difference between `string` and `String`, inference vs. explicit annotations.
- **Why it is relevant:** Start here if TypeScript is new to you. The basics are enough to read most of the frontend code.
- **Difficulty:** Beginner

#### TypeScript — Generics
- **Link:** <https://www.typescriptlang.org/docs/handbook/2/generics.html>
- **What it covers:** Generic type parameters, generic functions, generic interfaces, and constraints.
- **Why it is relevant:** `articleService.ts` uses axios's generic form: `api.get<ArticleListResponse>('/api/articles', ...)`. The `<ArticleListResponse>` parameter tells TypeScript what type `data` will be. Without generics, every API call would return `any`.
- **Difficulty:** Intermediate

#### TypeScript — `tsconfig.json` Reference
- **Link:** <https://www.typescriptlang.org/tsconfig>
- **What it covers:** Every compiler option in `tsconfig.json`: strict mode flags, module resolution, target, lib, paths.
- **Why it is relevant:** The project has both `tsconfig.json` and `tsconfig.app.json`. The `strict` family of options (enabled in this project) includes `strictNullChecks`, which makes `string | null` and `string` different types — affecting how you write null checks.
- **Difficulty:** Intermediate

#### TypeScript — Everyday Types
- **Link:** <https://www.typescriptlang.org/docs/handbook/2/everyday-types.html>
- **What it covers:** Primitive types, object types, union types (`string | null`), optional properties (`slug?: string`), type aliases, and literal types.
- **Why it is relevant:** The `Article` and `ArticleFilters` types in this project use optional properties extensively. Understanding `?` on a property vs. `| undefined` is a common source of confusion.
- **Difficulty:** Beginner

---

**Section recap:** TypeScript adds compile-time type safety to the JavaScript frontend. The `types/index.ts` file is the single source of truth for data shapes on the frontend. The TypeScript Handbook is the definitive reference; start with "Basics" and "Everyday Types" then move to "Generics" when you encounter `api.get<T>(...)` patterns.

---

## 5. Entity Framework Core

> **Where it appears:** `src/WikiProject.Api/Data/WikiDbContext.cs`, `ArticleService.cs` (all database queries), `src/WikiProject.Api/Migrations/`, and the EF Core NuGet packages in `WikiProject.Api.csproj`.

Entity Framework Core (EF Core) is .NET's official Object-Relational Mapper (ORM). An ORM translates between the object-oriented world of C# classes and the relational world of database tables and SQL. Instead of writing raw SQL, you write LINQ queries against `DbSet<T>` collections and EF Core generates the SQL for you. This project uses EF Core 10 with a SQLite provider.

### Why This Matters

Every time the backend reads or writes data, it goes through EF Core. The `WikiDbContext` class maps C# entity classes (`Article`, `Tag`, `ArticleTag`) to database tables. The `Migrations/` folder contains the history of schema changes. Understanding EF Core is required to add new entities, change the schema, or optimize queries.

### Common Beginner Confusion

- **"What is a `DbContext`?"** It is the primary class that manages the connection to the database and provides `DbSet<T>` properties representing database tables. Think of it as a "unit of work" that tracks changes to entities and saves them with `SaveChangesAsync()`.
- **"What is a migration?"** A migration is a C# file that describes how to move the database schema from one version to the next (and back). Running `dotnet ef database update` applies pending migrations. The project auto-applies migrations on startup (`db.Database.Migrate()` in `Program.cs`).
- **"When should I call `AsNoTracking()`?"** When you only read data and don't intend to update it, `AsNoTracking()` tells EF Core not to keep a copy of each entity in memory. All the read queries in `ArticleService` use it for performance.

---

### Resources

#### EF Core — Official Documentation
- **Link:** <https://learn.microsoft.com/en-us/ef/core/>
- **What it covers:** Getting started, `DbContext`, `DbSet<T>`, querying, saving data, migrations, relationships (one-to-many, many-to-many), performance, and raw SQL.
- **Why it is relevant:** This is the canonical reference for everything in `WikiDbContext.cs`, the `Migrations/` folder, and `ArticleService.cs`. Start with "Get Started" → "Getting Started with EF Core".
- **Difficulty:** Beginner → Advanced

#### EF Core — Querying Data
- **Link:** <https://learn.microsoft.com/en-us/ef/core/querying/>
- **What it covers:** LINQ queries against `DbSet<T>`, `Include`/`ThenInclude` for eager loading, filtered queries, sorting, pagination (`Skip`/`Take`), `AsNoTracking`, and how queries are translated to SQL.
- **Why it is relevant:** `ArticleService.GetArticlesAsync` builds a complex query: it eagerly loads `ArticleTags` and their `Tag`, applies `Where` filters conditionally, sorts, paginates with `Skip`/`Take`, and uses `AsNoTracking`. This page explains every one of those techniques.
- **Difficulty:** Intermediate

#### EF Core — Migrations
- **Link:** <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>
- **What it covers:** How to create migrations (`dotnet ef migrations add`), apply them (`dotnet ef database update`), revert them, and what the `Designer.cs` snapshot file is for.
- **Why it is relevant:** The `Migrations/` folder contains `20260315235834_InitialCreate.cs`. Any time you change an entity class (add a property, rename a column), you must create a new migration. This guide explains the full workflow.
- **Difficulty:** Beginner → Intermediate

#### EF Core — Relationships
- **Link:** <https://learn.microsoft.com/en-us/ef/core/modeling/relationships>
- **What it covers:** One-to-many, many-to-many, and one-to-one relationships; navigation properties; foreign keys; join entities; `HasKey`, `HasOne`, `WithMany`, `HasForeignKey`.
- **Why it is relevant:** `WikiDbContext.OnModelCreating` configures a many-to-many relationship between `Article` and `Tag` through the `ArticleTag` join entity. It uses `HasOne(...).WithMany(...).HasForeignKey(...)` — all explained in this guide.
- **Difficulty:** Intermediate

#### EF Core — Saving Data
- **Link:** <https://learn.microsoft.com/en-us/ef/core/saving/>
- **What it covers:** How `Add`, `Update`, `Remove`, `SaveChangesAsync` work, change tracking, and cascade deletes.
- **Why it is relevant:** `ArticleService.CreateAsync`, `UpdateAsync`, and `DeleteAsync` all manipulate entity state and call `SaveChangesAsync()`. Understanding change tracking (why updating `article.Title` directly works without calling `Update()`) requires this doc.
- **Difficulty:** Intermediate

---

**Section recap:** EF Core translates between C# objects and the SQLite database. The `WikiDbContext` defines the schema in code; migrations keep the database schema in sync. All query and update logic in `ArticleService.cs` is EF Core LINQ. The official EF Core docs are comprehensive; focus on "Querying", "Migrations", and "Relationships" first.

---

## 6. SQLite

> **Where it appears:** The default connection string in `Program.cs` is `"Data Source=wiki.db"`. The `Microsoft.EntityFrameworkCore.Sqlite` NuGet package provides the EF Core provider. The `wiki.db` file is the database itself (created at runtime).

SQLite is a *serverless, file-based relational database*. Unlike PostgreSQL or SQL Server, there is no separate database process to run — the entire database is a single `.db` file on disk, and the SQLite library is embedded in the application. This makes it perfect for development and small-scale projects.

### Why This Matters

SQLite is the database backing all data in this project. Understanding SQLite's constraints (limited concurrency, no native UUID type, case-insensitive collation quirks) helps you understand some choices in `ArticleService` (such as the `.ToLower()` calls in queries to normalize case manually). It also matters when you need to inspect or manipulate the database directly during development.

### Common Beginner Confusion

- **"Is SQLite production-ready?"** For a low-traffic internal wiki: yes. For a heavily concurrent application: no. SQLite allows only one writer at a time.
- **"Where is the database file?"** When you run the application, EF Core creates `wiki.db` in the working directory (typically `src/WikiProject.Api/`). You can open it with any SQLite browser.
- **"Does SQLite support all SQL features?"** No. It has limited `ALTER TABLE` support (cannot drop a column in older versions), no foreign key enforcement by default, and some data type differences. EF Core abstracts most of this.

---

### Resources

#### SQLite — Official Documentation
- **Link:** <https://www.sqlite.org/docs.html>
- **What it covers:** The complete SQLite reference: SQL syntax, data types, limits, transaction behavior, the file format, and the C API.
- **Why it is relevant:** When you need to understand why a particular EF Core migration generates a specific SQL statement, or when inspecting the `wiki.db` file directly, this is the reference.
- **Difficulty:** Beginner → Advanced

#### SQLite — When to Use SQLite
- **Link:** <https://www.sqlite.org/whentouse.html>
- **What it covers:** The intended use cases for SQLite vs. client/server databases like PostgreSQL. Discusses concurrency limitations and file locking.
- **Why it is relevant:** Helps you decide when SQLite is the right choice for this project (development, small team, internal tool) and when you should consider switching to PostgreSQL (higher traffic, multi-process writes).
- **Difficulty:** Beginner

#### DB Browser for SQLite (Tool)
- **Link:** <https://sqlitebrowser.org/>
- **What it covers:** A GUI tool to open, inspect, query, and edit SQLite database files.
- **Why it is relevant:** During development you can open `wiki.db` directly in DB Browser to inspect the actual rows, check that seed data was inserted, or debug EF Core query output. Invaluable when things seem wrong but the API returns no useful error.
- **Difficulty:** Beginner

#### EF Core SQLite Provider — Official Docs
- **Link:** <https://learn.microsoft.com/en-us/ef/core/providers/sqlite/>
- **What it covers:** SQLite-specific EF Core behavior, limitations (no row version support, limited migration operations), and connection string format.
- **Why it is relevant:** Some EF Core features work differently with SQLite. For example, complex `ALTER TABLE` operations require a table rebuild in SQLite. This doc lists those limitations so you know what to expect.
- **Difficulty:** Intermediate

---

**Section recap:** SQLite is the simplest possible production-viable database: a single file, no server process, ideal for development and small workloads. The `wiki.db` file is the database; EF Core manages it. Use DB Browser to inspect it directly. Know its concurrency limits before scaling.

---

## 7. REST APIs

> **Where it appears:** `ArticlesController.cs` and `MetadataController.cs` define the REST API. The frontend's `articleService.ts` consumes it using `axios`.

REST (Representational State Transfer) is an architectural style for building web APIs. A RESTful API exposes *resources* (like articles, tags, categories) as URLs and uses standard HTTP methods (`GET`, `POST`, `PUT`, `DELETE`) to indicate what operation to perform. The response body is typically JSON.

### Why This Matters

Understanding REST conventions helps you read the controller routes and predict what the frontend expects. For example, `POST /api/articles` creates a resource and returns `201 Created` with a `Location` header pointing to the new resource — a REST convention that `ArticlesController.Create` implements via `CreatedAtAction`.

### Common Beginner Confusion

- **"Is REST a protocol?"** No. REST is a set of architectural constraints. HTTP is the protocol. A REST API is an API that follows REST constraints while using HTTP as its transport.
- **"What is the difference between `PUT` and `PATCH`?"** `PUT` replaces an entire resource; `PATCH` applies a partial update. This project uses `PUT` for updates, meaning the client must always send all fields.
- **"What does a `201 Created` response mean vs `200 OK`?"** `201` means a new resource was created; the `Location` header tells the client where to find it. `200` means the request succeeded and the response body contains the result.

---

### Resources

#### MDN Web Docs — HTTP Overview
- **Link:** <https://developer.mozilla.org/en-US/docs/Web/HTTP/Overview>
- **What it covers:** What HTTP is, how requests and responses work, HTTP methods, status codes, headers, and how the browser's network model works.
- **Why it is relevant:** The API in this project is HTTP-based. Understanding status codes (`200`, `201`, `204`, `400`, `404`) is required to read and debug the controller action return values.
- **Difficulty:** Beginner

#### MDN Web Docs — HTTP Status Codes
- **Link:** <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status>
- **What it covers:** Every HTTP status code with its definition and when to use it.
- **Why it is relevant:** `ArticlesController` returns `Ok(result)`, `NotFound()`, `CreatedAtAction(...)`, `NoContent()`, and `ValidationProblem(...)`. These map to `200`, `404`, `201`, `204`, and `400` respectively. This reference explains when each is appropriate.
- **Difficulty:** Beginner

#### RESTful Web API Design — Microsoft Architecture Guidance
- **Link:** <https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design>
- **What it covers:** REST resource naming conventions, URI design, HTTP methods semantics, versioning, pagination, and error responses (Problem Details).
- **Why it is relevant:** The API design in this project follows these conventions. Understanding why `/api/articles/{id}` is preferred over `/api/getArticleById?id=5`, and how pagination is expressed with query parameters, comes from these principles.
- **Difficulty:** Intermediate

#### RFC 7807 — Problem Details for HTTP APIs
- **Link:** <https://www.rfc-editor.org/rfc/rfc7807>
- **What it covers:** The "Problem Details" standard for JSON error responses (`type`, `title`, `status`, `detail`, `instance`).
- **Why it is relevant:** When validation fails, `ArticlesController` returns `ValidationProblem(new ValidationProblemDetails(...))`. This is an ASP.NET Core implementation of RFC 7807. The frontend's `getErrorMessage` function in `articleService.ts` reads `detail.title` because that is the Problem Details field.
- **Difficulty:** Intermediate

#### Axios — Official Documentation
- **Link:** <https://axios-http.com/docs/intro>
- **What it covers:** How to create an axios instance with `axios.create`, make `get`/`post`/`put`/`delete` requests, pass query parameters, send JSON bodies, and handle errors via `AxiosError`.
- **Why it is relevant:** The entire frontend HTTP layer (`articleService.ts`) is built on axios. The `buildParams` function constructs the query object that axios serializes. The `getErrorMessage` function handles `AxiosError`. This is the reference for all of that.
- **Difficulty:** Beginner

---

**Section recap:** REST is the design convention; HTTP is the transport; JSON is the format. The controller exposes resources at well-named URLs and uses HTTP verbs and status codes semantically. The frontend consumes those endpoints using axios. The MDN HTTP docs and the Microsoft REST API design guide are the best references for understanding why the API is designed as it is.

---

## 8. Dependency Injection

> **Where it appears:** `Program.cs` (`builder.Services.AddScoped<...>`, `AddDbContext<...>`, `AddScoped<IValidator<...>>`) and every constructor in `ArticleService`, `ArticlesController`, and `MetadataController` that receives injected services.

Dependency Injection (DI) is a design pattern where a class *declares* the services it needs (as constructor parameters) rather than *creating* them itself. A *DI container* is responsible for constructing objects and wiring up their dependencies. ASP.NET Core has a built-in DI container.

The key benefit is *decoupling* — `ArticlesController` depends on `IArticleService`, not `ArticleService`. This means you can swap the real service for a mock in tests without changing the controller.

### Why This Matters

Every service, controller, and validator in this project participates in DI. If you add a new service class, you must register it in `Program.cs` with the appropriate lifetime (`AddScoped`, `AddSingleton`, `AddTransient`). If you forget, ASP.NET Core will throw an exception at startup saying it cannot resolve the dependency.

### Common Beginner Confusion

- **"What is the difference between `AddScoped`, `AddTransient`, and `AddSingleton`?"**
  - `Scoped`: one instance per HTTP request. Used for `ArticleService` and `WikiDbContext` because database state should not leak between requests.
  - `Transient`: a new instance every time. Used for lightweight, stateless services.
  - `Singleton`: one instance for the entire application lifetime. Used for shared, thread-safe services like caches or configuration.
- **"Why do we register `IArticleService` mapped to `ArticleService`?"** So that the DI container knows which concrete implementation to use when something asks for `IArticleService`. The controller depends on the *interface*, which makes it testable.

---

### Resources

#### Dependency Injection in ASP.NET Core — Official Docs
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection>
- **What it covers:** The full DI system: service lifetimes, how to register services, constructor injection, `IServiceProvider`, and scope management.
- **Why it is relevant:** This is the definitive reference for understanding `Program.cs`'s service registrations and why `ArticleService` receives `WikiDbContext` and `ILogger` through its constructor.
- **Difficulty:** Beginner → Intermediate

#### Dependency Injection — Martin Fowler's Original Article
- **Link:** <https://martinfowler.com/articles/injection.html>
- **What it covers:** The conceptual motivation for DI: the inversion of control principle, constructor injection vs. setter injection, and why DI containers exist.
- **Why it is relevant:** If you want to understand *why* DI exists rather than just how to use it in ASP.NET Core, this article by the person who coined the pattern is the definitive conceptual introduction.
- **Difficulty:** Intermediate

#### Understanding Service Lifetimes in .NET DI
- **Link:** <https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes>
- **What it covers:** `Scoped`, `Transient`, and `Singleton` lifetimes with concrete examples of when to use each. Includes warnings about "captive dependency" problems (injecting a scoped service into a singleton).
- **Why it is relevant:** `WikiDbContext` is registered as scoped. If you accidentally inject it into a singleton service, you will get a runtime error or, worse, silent data corruption. This reference explains why.
- **Difficulty:** Intermediate

---

**Section recap:** DI is the mechanism by which ASP.NET Core wires together controllers, services, the database context, validators, and loggers without any of them needing to know about each other. Register services in `Program.cs`, declare them in constructors, and ASP.NET Core does the rest. Understand lifetimes to avoid subtle bugs.

---

## 9. Validation

> **Where it appears:** `src/WikiProject.Api/Validation/ArticleValidators.cs` (FluentValidation rules), `Program.cs` (validator registration), and `ArticlesController` (manual invocation of validators in `Create` and `Update` actions).

Input validation ensures that data entering the system meets requirements before being processed or stored. This project uses **FluentValidation**, a popular .NET library that defines validation rules as strongly-typed C# classes rather than data annotations on DTOs.

### Why This Matters

Without validation, a user could create an article with an empty title, an invalid slug, or a 10,000-word summary. The validators in `ArticleValidators.cs` enforce constraints like title length ≤ 200 characters and slug format matching the regex `^[a-z0-9]+(?:-[a-z0-9]+)*$`. When validation fails, the controller returns a `400 Bad Request` with a Problem Details response body listing each field error — which the frontend can display to the user.

### Common Beginner Confusion

- **"Why use FluentValidation instead of Data Annotations (`[Required]`, `[MaxLength]`)?"** Data annotations are simpler for basic cases but become messy for complex rules (conditional validation, cross-field rules, async validation). FluentValidation is more expressive and easier to test in isolation.
- **"Why does the controller call `validator.ValidateAsync(request)` manually instead of using automatic validation?"** This project intentionally avoids the `AddFluentValidationAutoValidation()` middleware and does manual validation to keep control explicit. The `[FromServices] IValidator<T>` pattern injects the validator directly into the action method.

---

### Resources

#### FluentValidation — Official Documentation
- **Link:** <https://docs.fluentvalidation.net/en/latest/>
- **What it covers:** Everything about FluentValidation: built-in validators (`NotEmpty`, `MaximumLength`, `Matches`, `Must`), custom validators, async validation, conditional rules (`When`), and integration with ASP.NET Core.
- **Why it is relevant:** `ArticleValidators.cs` uses `RuleFor`, `NotEmpty`, `MaximumLength`, `Matches`, `Must`, and `When` — all defined in this documentation. This is the primary reference.
- **Difficulty:** Beginner → Intermediate

#### FluentValidation — ASP.NET Core Integration
- **Link:** <https://docs.fluentvalidation.net/en/latest/aspnet.html>
- **What it covers:** How to register FluentValidation with ASP.NET Core's DI container and how to use `[FromServices] IValidator<T>` in action methods.
- **Why it is relevant:** This explains the specific integration pattern used in this project: validators are registered as `IValidator<T>` in `Program.cs` and injected into controller actions via `[FromServices]`.
- **Difficulty:** Beginner

#### Model Validation in ASP.NET Core — Official Docs
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation>
- **What it covers:** Data Annotations, the `ModelState` dictionary, `ValidationProblemDetails`, and `[ApiController]`'s automatic validation behavior.
- **Why it is relevant:** This project uses `ValidationProblemDetails` to format error responses. Understanding how `validation.ToDictionary()` maps to the Problem Details `errors` field is covered here.
- **Difficulty:** Beginner → Intermediate

---

**Section recap:** FluentValidation provides expressive, testable, rule-based input validation. Validators are registered in DI and invoked manually in controller actions. When validation fails, a `400` response with Problem Details is returned. The FluentValidation docs are the primary reference; the ASP.NET Core validation docs explain the underlying `ValidationProblemDetails` format.

---

## 10. Routing

> **Where it appears:** Backend — `[Route("api/articles")]`, `[HttpGet("{id:int}")]`, `[HttpGet("slug/{slug}")]` in `ArticlesController.cs`. Frontend — `BrowserRouter`, `Routes`, `Route` in `App.tsx`; `useNavigate` and `Link` in the page components.

Routing is the process of mapping an incoming URL to the right handler. This project has two routing systems that must work together:

1. **Backend routing (ASP.NET Core attribute routing):** decides which controller action handles a given URL like `GET /api/articles/42`.
2. **Frontend routing (React Router v7):** decides which React component (page) to render for a given browser URL like `/articles/42/edit`.

### Why This Matters

When the user navigates to `/articles/42`, the browser does not send a request to the backend for that URL — React Router intercepts it and renders `ArticleDetailPage`. That page then makes a separate `GET /api/articles/42` request to the backend. Understanding that the two routing systems are independent (one controls what the browser renders, one controls which API handler runs) prevents a lot of confusion.

### Common Beginner Confusion

- **"Why does the backend route `{id:int}` have a `:int` constraint?"** This is a *route constraint* that restricts the `id` parameter to integers. Without it, `GET /api/articles/slug` would also match `{id:int}` (it won't, because `slug` is not an integer) — but it helps distinguish it from the `/api/articles/slug/{slug}` route.
- **"What happens when I refresh the page in the React app?"** If the web server (or Vite dev server) does not know to serve `index.html` for all routes, you get a 404. Vite's dev server handles this automatically. In production you need to configure the web server accordingly.
- **"What is `BrowserRouter` vs `HashRouter`?"** `BrowserRouter` uses real URL paths (`/articles/42`). `HashRouter` uses URL hashes (`/#/articles/42`). This project uses `BrowserRouter`, which requires server-side configuration to serve `index.html` on all paths.

---

### Resources

#### ASP.NET Core Routing — Official Docs
- **Link:** <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing>
- **What it covers:** How ASP.NET Core routes requests to endpoints, attribute routing vs. conventional routing, route templates, route constraints (`:int`, `:guid`, `:minlength`), and route order.
- **Why it is relevant:** `ArticlesController` uses attribute routing: `[Route("api/articles")]` sets the base, and `[HttpGet("{id:int}")]` adds the `{id:int}` constraint. This doc explains how those templates are matched and what happens when two routes could match the same URL.
- **Difficulty:** Intermediate

#### React Router v7 — Official Documentation
- **Link:** <https://reactrouter.com/home>
- **What it covers:** `BrowserRouter`, `Routes`, `Route`, `Link`, `NavLink`, `useNavigate`, `useParams`, nested routes, loaders, and actions.
- **Why it is relevant:** `App.tsx` uses `BrowserRouter`, `Routes`, and `Route` to define the client-side page routes. `ArticleDetailPage.tsx` uses `useParams` to get the `id` from the URL. `EditArticlePage.tsx` uses `useNavigate` to redirect after a save. This is the reference for all of that.
- **Difficulty:** Beginner → Intermediate

#### React Router — `useParams` Hook
- **Link:** <https://reactrouter.com/api/hooks/useParams>
- **What it covers:** How to extract dynamic URL parameters (like `:id` in `/articles/:id`) from inside a component.
- **Why it is relevant:** `ArticleDetailPage.tsx` and `EditArticlePage.tsx` call `useParams()` to get the article `id` from the URL. Without understanding this hook you cannot trace how the URL becomes an API call.
- **Difficulty:** Beginner

---

**Section recap:** Routing exists at two levels: ASP.NET Core maps HTTP requests to controller actions using attribute routing; React Router maps browser URL changes to React page components. They are independent systems. Understanding both is essential to tracing the full path from a user clicking a link to data appearing on screen.

---

## 11. Frontend State and Forms

> **Where it appears:** Custom hooks in `frontend/src/hooks/` (`useArticles.ts`, `useArticle.ts`), form component `frontend/src/components/ArticleForm.tsx`, and page components like `NewArticlePage.tsx` and `EditArticlePage.tsx`.

State management is how a frontend application keeps track of data that changes over time — things like the list of articles fetched from the API, the current search filters, whether data is loading, or what a user has typed into a form. In React, state lives in `useState` hooks within components (or custom hooks). Forms are a specific case where state tracks what the user is typing and what errors exist.

### Why This Matters

This project does not use a global state library like Redux or Zustand. Instead, it uses *co-located state*: each page or hook manages its own state. `useArticles` manages fetching and caching the list; `useArticle` manages fetching a single article. This is a deliberate architectural choice that works well for small applications and is easier to understand than a global state store.

### Common Beginner Confusion

- **"When should I lift state up vs. keep it local?"** Keep state as close to where it is used as possible. Only lift it up when two unrelated components need to share it. This project keeps all state either in custom hooks (shared by a page and its children) or inside individual components.
- **"Why does `useArticles` use `JSON.stringify(filters)` as the `useCallback` dependency?"** Because `filters` is an object passed as a prop, and objects have reference equality in JavaScript — even `{}` !== `{}`. `JSON.stringify(filters)` produces a string that is equal when the filter values are equal.
- **"Why is there no form library (like React Hook Form)?"** This project is intentionally kept simple. For larger forms with complex validation, libraries like React Hook Form provide better ergonomics.

---

### Resources

#### React — Managing State (Official Docs)
- **Link:** <https://react.dev/learn/managing-state>
- **What it covers:** The principles of React state management: local state, lifting state up, choosing the right state structure, and when to reach for context vs. external stores.
- **Why it is relevant:** This is the conceptual foundation for understanding why `useArticles` and `useArticle` are structured the way they are, and when the project's approach is appropriate vs. when you would want something more sophisticated.
- **Difficulty:** Intermediate

#### React — Extracting State Logic into a Reducer
- **Link:** <https://react.dev/learn/extracting-state-logic-into-a-reducer>
- **What it covers:** The `useReducer` hook as an alternative to multiple `useState` calls when state transitions are complex.
- **Why it is relevant:** The `useArticles` hook currently uses three `useState` calls (`data`, `loading`, `error`). As the state logic grows, refactoring to `useReducer` would be a natural next step. This guide explains when and how to do that.
- **Difficulty:** Intermediate

#### React — Reusing Logic with Custom Hooks
- **Link:** <https://react.dev/learn/reusing-logic-with-custom-hooks>
- **What it covers:** How to extract repeated `useState`/`useEffect` logic into a custom hook, naming conventions (`use` prefix), and when hooks are the right abstraction.
- **Why it is relevant:** `useArticles.ts` and `useArticle.ts` are both custom hooks. This guide explains the pattern: why `useArticles` can be shared across `ArticlesPage` and `HomePage` without either component knowing about fetch logic.
- **Difficulty:** Intermediate

#### React Hook Form — Official Docs (Alternative Reference)
- **Link:** <https://react-hook-form.com/get-started>
- **What it covers:** A popular library for managing form state, validation, and submission in React. Covers controlled vs. uncontrolled inputs, `register`, `handleSubmit`, `formState.errors`.
- **Why it is relevant:** The current `ArticleForm.tsx` uses basic `useState` for form fields. If you want to add more complex validation, more fields, or better performance with large forms, React Hook Form is the most widely used solution in the React ecosystem. This is listed as an alternative approach.
- **Difficulty:** Beginner → Intermediate

#### TanStack Query (React Query) — Overview (Alternative Reference)
- **Link:** <https://tanstack.com/query/latest/docs/framework/react/overview>
- **What it covers:** A library for fetching, caching, and synchronizing server state in React. Provides `useQuery`, `useMutation`, `invalidateQueries`, and automatic background refetching.
- **Why it is relevant:** The `useArticles` and `useArticle` custom hooks solve the same problem that TanStack Query was built for. As the project grows, adopting TanStack Query would eliminate the need to manually manage `loading`/`error`/`data` state in every hook. This is listed as a natural evolution path, not a current dependency.
- **Difficulty:** Intermediate

---

**Section recap:** State is kept local to where it is used — in custom hooks for server data and in component state for form fields. `useArticles` and `useArticle` encapsulate the loading/error/data pattern. For future growth, React Hook Form (for complex forms) and TanStack Query (for server state) are the natural next steps.

---

## 12. Software Architecture Basics

> **Where it appears:** The overall structure of the project: `Controllers` → `Services` → `Data` → `Entities` on the backend; `Pages` → `Hooks` → `Services` → `Components` on the frontend. The use of DTOs, interfaces, and mappings.

Software architecture is the high-level organization of code into layers, modules, and responsibilities. This project uses a **layered architecture** (sometimes called N-tier or clean architecture light):

| Layer | Responsibility | Backend | Frontend |
|---|---|---|---|
| Presentation | Accept input, format output | Controllers | Pages |
| Application/Business Logic | Orchestrate use cases | Services (`ArticleService`) | Hooks (`useArticles`) |
| Infrastructure | Talk to external systems (DB, HTTP) | `WikiDbContext`, EF Core | `articleService.ts`, axios |
| Domain | Core data models | `Article`, `Tag`, `ArticleTag` | Types (`index.ts`) |

The key principles visible in the code:

- **Separation of Concerns:** Controllers do not contain business logic; services do not know about HTTP.
- **Dependency on interfaces, not concrete types:** `ArticlesController` depends on `IArticleService`, not `ArticleService`.
- **DTOs (Data Transfer Objects):** The API never exposes the raw `Article` entity. It uses `ArticleDto` and `ArticleSummaryDto` — shapes designed for transport, not for persistence.
- **Mapping:** `ArticleMappings.cs` defines extension methods (`ToDto()`, `ToSummaryDto()`) to convert between entity and DTO.

### Why This Matters

This architecture makes the codebase predictable. When you need to change what data is returned from `GET /api/articles`, you look in `ArticleService` and the DTO. When you need to change the database schema, you touch `WikiDbContext` and create a migration. When you need to change the API route, you touch the controller only. This separation is not overhead — it is what prevents the codebase from becoming an unmaintainable tangle.

### Common Beginner Confusion

- **"Why use DTOs? Why not just return the `Article` entity directly?"** Several reasons: (1) Entities may have circular references that break JSON serialization. (2) Entities expose internal database columns you don't want to leak (like foreign keys). (3) DTOs let the API shape evolve independently from the database schema.
- **"What is the difference between a service and a repository?"** A repository is responsible *only* for data access (CRUD operations on a single entity). A service contains business logic and may use multiple repositories. This project merges both into `ArticleService` for simplicity — a common and acceptable choice for small projects. Separating them is a refactoring option as complexity grows.
- **"Should the `ArticlesController` call `WikiDbContext` directly?"** No. That would bypass the service layer. The controller should only call services; services call the database context. Breaking this rule makes the code harder to test and change.

---

### Resources

#### Microsoft — N-Layer Architecture
- **Link:** <https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/n-tier>
- **What it covers:** The conceptual model of layered (N-tier) architecture: presentation, business logic, and data access layers, and their responsibilities.
- **Why it is relevant:** The `Controllers` / `Services` / `Data` structure in this project is a direct implementation of the N-tier model. This guide provides the vocabulary and rationale.
- **Difficulty:** Beginner

#### Martin Fowler — Patterns of Enterprise Application Architecture (Book)
- **Link:** <https://martinfowler.com/books/eaa.html>
- **What it covers:** The canonical catalog of application architecture patterns: Service Layer, Repository, Data Transfer Object, Domain Model, Transaction Script.
- **Why it is relevant:** Several patterns in this project (Service Layer, DTO, Repository-like `ArticleService`) are described here. This is the book that named many of the patterns developers use every day. Not free, but widely referenced online.
- **Difficulty:** Advanced

#### Clean Architecture — Robert C. Martin (Overview Article)
- **Link:** <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html>
- **What it covers:** The Clean Architecture principle: outer layers (controllers, infrastructure) depend on inner layers (domain, use cases), never the reverse.
- **Why it is relevant:** This project's interface-based service layer (`IArticleService`) and separation of domain entities from DTOs reflect Clean Architecture principles. This article provides the motivation for those choices.
- **Difficulty:** Intermediate

#### Microsoft — Data Transfer Object Pattern
- **Link:** <https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs> (see also the DTO section in ASP.NET Core Web API tutorials)
- **What it covers:** Why DTOs exist, how they differ from domain entities, and how to use AutoMapper or manual mapping.
- **Why it is relevant:** `ArticleDtos.cs` and `ArticleMappings.cs` implement this pattern manually (using extension methods instead of AutoMapper). Understanding the DTO pattern explains why `ArticleDto` exists separately from the `Article` entity.
- **Difficulty:** Intermediate

#### Microsoft — Repository Pattern with EF Core
- **Link:** <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design>
- **What it covers:** The Repository and Unit of Work patterns, why some teams apply them on top of EF Core, and the counterargument that `DbContext` already is a unit of work.
- **Why it is relevant:** `ArticleService` currently acts as a combined service + repository. This reference explains when to separate them (and why some teams argue you shouldn't bother when using EF Core).
- **Difficulty:** Advanced

---

**Section recap:** This project uses a layered architecture where each layer has a single responsibility. Controllers handle HTTP; services handle business logic; the DB context handles persistence; entities represent database rows; DTOs represent API shapes; mappings translate between them. This architecture is standard in ASP.NET Core projects and scales well as complexity grows.

---

## Appendix: Quick Reference Card

| Technology | Version in This Project | Primary Official Docs |
|---|---|---|
| .NET | 10 | <https://learn.microsoft.com/en-us/dotnet/> |
| ASP.NET Core | 10 | <https://learn.microsoft.com/en-us/aspnet/core/> |
| C# | 13 (implicit with .NET 10) | <https://learn.microsoft.com/en-us/dotnet/csharp/> |
| React | 19 | <https://react.dev/> |
| TypeScript | ~5.9.3 | <https://www.typescriptlang.org/docs/> |
| Entity Framework Core | 10 | <https://learn.microsoft.com/en-us/ef/core/> |
| SQLite | (EF Core provider) | <https://www.sqlite.org/docs.html> |
| FluentValidation | 11.3.1 | <https://docs.fluentvalidation.net/> |
| React Router | 7 | <https://reactrouter.com/> |
| axios | ^1.13.6 | <https://axios-http.com/docs/intro> |
| Vite | 8 | <https://vitejs.dev/guide/> |

---

*Related docs to read alongside this one: once the full docs suite is available, see the architecture overview doc for how these layers interact in practice, and the local development setup guide for running the project and applying database migrations.*
