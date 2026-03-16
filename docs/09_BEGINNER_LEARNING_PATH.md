# Beginner Learning Path — WikiProject Onboarding Guide

> **Who this is for:** A developer who is new to this codebase — or new to one or more of its technologies — and wants a structured path from "I just cloned this" to "I can confidently add features." You do not need to be an expert before starting. You need to be willing to read, try things, and look things up.
>
> **How to use this guide:** Work through the stages in order. Do not skip ahead. Each stage builds on the previous one. At the end of each stage, check the "Signs you understand" list. If you cannot answer the questions or complete the mini-exercises, re-read before moving on.

---

## Table of Contents

1. [Stage 1 — The Big Picture](#stage-1--the-big-picture)
2. [Stage 2 — The Backend Request Flow](#stage-2--the-backend-request-flow)
3. [Stage 3 — The Frontend Rendering Flow](#stage-3--the-frontend-rendering-flow)
4. [Stage 4 — The Database and EF Core](#stage-4--the-database-and-ef-core)
5. [Stage 5 — Making Safe Small Changes](#stage-5--making-safe-small-changes)
6. [Stage 6 — Extending the System with Confidence](#stage-6--extending-the-system-with-confidence)
7. [Reference: Key File Map](#reference-key-file-map)
8. [Reference: Recommended External Resources](#reference-recommended-external-resources)

---

## Stage 1 — The Big Picture

### What to Read First

- `/README.md` — Start here. Read the entire file (it is short). Pay attention to the folder structure and the list of technologies.
- `/STARTING_TASKS.md` — Skim this. Do not worry about understanding every item. It tells you the history of what was built and what is planned.
- The top-level directory listing — two folders matter: `src/` (the .NET backend) and `frontend/` (the React app).

### What This Project Actually Is

WikiProject is an **internal wiki and knowledge-base application**. Think of it like a private, team-owned version of a documentation site. Users can create, categorize, search, and read articles. Articles have a status (Draft, Published, Archived), tags, a category, and Markdown content.

The system has two separate, independent programs that run at the same time and communicate over HTTP:

1. **The backend API** — built with ASP.NET Core (.NET 10). It runs at `http://localhost:5018`. Its only job is to store and serve article data. It speaks JSON.
2. **The frontend** — built with React 19 + TypeScript compiled by Vite. It runs at `http://localhost:5173`. Its only job is to display data to the user and send user actions back to the API.

These two programs do not share any code. The frontend does not know that the backend is written in C#. The backend does not know that the frontend is written in TypeScript. They only agree on the shape of the JSON data they exchange (the "API contract").

```
Browser (User)
     │
     │  HTTP (port 5173)
     ▼
┌─────────────┐
│  React App  │  ← Vite dev server
│  (frontend) │
└──────┬──────┘
       │  HTTP /api/* (port 5018, proxied during dev)
       ▼
┌─────────────────┐
│  ASP.NET Core   │  ← .NET runtime
│  Web API        │
│  (backend)      │
└──────┬──────────┘
       │  SQL
       ▼
┌────────────┐
│  SQLite DB │  ← wiki.db file on disk
└────────────┘
```

### Key Concepts to Focus On

**Separation of concerns:** Each layer does one thing. The database stores data. The backend validates and organizes that data. The frontend presents it. This is the most important structural idea in this codebase. Every design decision flows from it.

**REST API:** The backend exposes a REST (Representational State Transfer) API. This is a convention for designing HTTP endpoints. In REST, a URL identifies a *resource* (like an article), and the HTTP method (GET, POST, PUT, DELETE) identifies the *action* you want to take on it. For example:
- `GET /api/articles/42` means "give me article 42"
- `DELETE /api/articles/42` means "delete article 42"

**JSON (JavaScript Object Notation):** The format used to send data between the frontend and the backend. You will see it everywhere. It looks like a JavaScript object: `{ "title": "My Article", "status": "Published" }`.

**SPA (Single-Page Application):** The frontend is a SPA. This means the browser loads one HTML file once, and React takes over all subsequent navigation. When you click a link, React updates the URL and swaps the displayed component — no full page reload. This is why `App.tsx` has a Router with `<Route>` elements.

**ORM (Object-Relational Mapper):** The backend uses Entity Framework Core (EF Core) as its ORM. An ORM lets you interact with a SQL database using regular C# objects instead of writing raw SQL queries. EF Core translates your C# LINQ expressions into SQL at runtime.

### Questions You Should Be Able to Answer After Stage 1

1. What does the backend do? What does the frontend do? Could one exist without the other?
2. What URL does the backend run on? What URL does the frontend run on?
3. If you open the browser and visit `http://localhost:5173/articles/42`, which program is running? Which program actually fetches the data?
4. What is a REST API? Give one example using an HTTP method and a URL from this project.
5. What does SPA mean? Why does it matter for how navigation works?
6. Where is the database stored? What format is it?

### Practical Mini-Exercise

1. Start both the backend and the frontend (follow the README). Do not skip this — you need them running to learn.
2. Open a browser to `http://localhost:5018/swagger`. Explore the Swagger UI. Find the endpoint for listing articles. Click "Try it out" and execute a request. Look at the raw JSON response.
3. Open a browser to `http://localhost:5173`. Browse around. Click on an article. Then open the browser DevTools (F12 → Network tab) and reload. Watch what HTTP requests the frontend makes. You should see calls to `/api/articles`.
4. Use the Swagger UI to create a new article with a title, summary, content, and category. Then find it in the frontend.

### Common Beginner Confusion: Stage 1

**"Why are there two separate programs? Couldn't we just put everything in one?"**

You could, with a framework like Next.js (which renders React on the server) or Razor Pages (which renders HTML server-side in ASP.NET). The choice here is a *decoupled* architecture: the frontend and backend are developed and deployed independently. This is common in teams where frontend and backend are owned by different people, or where the same API must serve multiple clients (web, mobile, etc.). The tradeoff is extra complexity: you need to manage CORS, run two processes during development, and keep the API contract in sync.

**"The frontend runs on port 5173 but all the `/api` calls go to port 5018 — how?"**

Vite's dev proxy (configured in `frontend/vite.config.ts`) intercepts any request starting with `/api` and forwards it to `http://localhost:5018`. This only happens during development. In a real production deployment, you would configure a reverse proxy (like nginx or a cloud load balancer) to do the same routing.

**"What is `wiki.db`?"**

It is the SQLite database — a single file that contains all your data. SQLite is a file-based database engine, meaning there is no separate database server process. EF Core connects to it directly by opening the file. This is ideal for small apps and development. In production you would likely switch to PostgreSQL or SQL Server.

### Stage 1 Recap

You now know: what the project does, what its two main programs are, how they communicate, and what technologies are involved. You have run both programs and seen data flow from the database to your browser.

---

## Stage 2 — The Backend Request Flow

### What to Read

Read these files in order:

1. `src/WikiProject.Api/Program.cs` — The entry point. Everything that runs is wired up here.
2. `src/WikiProject.Api/Controllers/ArticlesController.cs` — Where HTTP requests land.
3. `src/WikiProject.Api/Services/IArticleService.cs` — The contract for business logic.
4. `src/WikiProject.Api/Services/ArticleService.cs` — Where the actual work happens.
5. `src/WikiProject.Api/DTOs/ArticleDtos.cs` — The shapes of data coming in and going out.
6. `src/WikiProject.Api/Validation/ArticleValidators.cs` — Rules for what data is allowed.
7. `src/WikiProject.Api/Mappings/ArticleMappings.cs` — How database objects become response objects.

### The Request Lifecycle — Step by Step

When the frontend sends `GET /api/articles?search=database&page=1`, here is *exactly* what happens inside the backend:

```
1. HTTP request arrives at the .NET runtime
       │
       ▼
2. ASP.NET Core routing matches the URL to ArticlesController.GetArticles()
       │
       ▼
3. Query parameters (search, page, etc.) are automatically bound to
   the ArticleQueryParams object by the framework's model binder
       │
       ▼
4. ArticlesController calls _articleService.GetArticlesAsync(queryParams)
       │
       ▼
5. ArticleService builds a LINQ query against WikiDbContext (the EF Core gateway)
   It applies search, category, tag, status filters
   It counts total results, then applies pagination (Skip/Take)
       │
       ▼
6. EF Core translates the LINQ query to SQL and runs it against wiki.db
       │
       ▼
7. EF Core returns a list of Article entity objects (C# objects from the DB)
       │
       ▼
8. ArticleMappings.ToSummaryDto() converts each Article entity to an ArticleSummaryDto
   (strips out unnecessary fields, applies sorting on tags)
       │
       ▼
9. ArticleService wraps results in ArticleListResponse (includes pagination metadata)
       │
       ▼
10. Controller returns Ok(response) — HTTP 200 with the JSON-serialized response
       │
       ▼
11. JSON travels back to the frontend
```

### Understanding Program.cs

`Program.cs` is the composition root — the single place where the application is assembled. It does four main things:

**1. Register services (Dependency Injection)**
```csharp
builder.Services.AddScoped<IArticleService, ArticleService>();
```
This tells the framework: "whenever someone asks for `IArticleService`, give them an instance of `ArticleService`." This is Dependency Injection (DI). It means controllers do not create their own service instances — the framework injects them. This makes the code testable and flexible.

**2. Configure middleware**
Middleware is code that runs on *every* request, in order, before the request reaches a controller. In this project:
- CORS middleware — checks that requests come from allowed origins
- Routing middleware — matches the URL to a controller action
- Authorization middleware — (not yet used, but its position in the pipeline matters)

**3. Register the database context**
```csharp
builder.Services.AddDbContext<WikiDbContext>(options =>
    options.UseSqlite(connectionString));
```
This registers `WikiDbContext` as a scoped service. EF Core manages one DB connection per HTTP request.

**4. Auto-run migrations and seeding on startup**
```csharp
db.Database.Migrate();
await SeedData.SeedAsync(db);
```
Every time the app starts, it checks whether the database schema is up to date and applies any pending migrations. If the database is empty, it seeds sample data.

### Understanding Controllers

`ArticlesController` is a thin layer. Its only job is:
- Accept HTTP requests
- Validate them minimally (FluentValidation does the real validation)
- Delegate to the service
- Return the appropriate HTTP status code

Notice what the controller does **not** do:
- Does not write LINQ queries
- Does not access the database directly
- Does not build slugs or resolve tags

This is intentional. Keeping controllers thin makes the business logic easier to test and reuse.

**HTTP Status Codes used in this project:**
| Code | Meaning | When used |
|------|---------|-----------|
| 200 OK | Success with body | GET, PUT (returns updated object) |
| 201 Created | Resource created | POST (returns new article, sets Location header) |
| 204 No Content | Success, no body | DELETE |
| 400 Bad Request | Validation failed | Invalid input from FluentValidation |
| 404 Not Found | Resource missing | Article ID or slug does not exist |

### Understanding Services

`ArticleService` contains all business logic. Key things to understand:

**Slug generation:**
A *slug* is a URL-friendly version of a title. "My First Article" becomes `my-first-article`. The `GenerateSlug()` method:
1. Converts to lowercase
2. Replaces spaces and special chars with hyphens using a regex
3. Collapses multiple hyphens into one
4. Strips leading/trailing hyphens

If you create two articles with the same title, `EnsureUniqueSlugAsync()` automatically appends a number: `my-first-article`, `my-first-article-1`, `my-first-article-2`, etc.

**Tag resolution:**
When creating/updating an article, tags arrive as a list of strings (e.g., `["csharp", "tutorial"]`). `ResolveTagsAsync()` looks up each tag by name. If it exists, it uses the existing `Tag` record. If not, it creates a new one. This prevents duplicate tags and maintains referential integrity.

**Search:**
The full-text search filters articles where the search string appears in the title, summary, content, category, or any tag name. The search is case-insensitive (`.ToLower()` on both sides). This is done in memory after loading from the database, which is simple but has scaling limits (see Stage 6).

### Understanding DTOs

**DTO** stands for Data Transfer Object. DTOs are plain record classes that define the shape of data crossing the API boundary. They exist for two reasons:

1. **Security:** You do not want to expose your internal entity structure directly. For example, if you later add a `PasswordHash` field to a user entity, you do not want it accidentally returned in an API response.
2. **Shape:** The API response might need a different structure than the database entity. `ArticleSummaryDto` omits the `Content` field to keep list responses lightweight.

In this project:
- `ArticleDto` — full article (used for detail view)
- `ArticleSummaryDto` — lightweight card (used for list view)
- `CreateArticleRequest` — input shape for POST
- `UpdateArticleRequest` — input shape for PUT
- `ArticleListResponse` — paginated list wrapper

### Understanding Validation

FluentValidation runs *before* the service. In `Program.cs`, the line:
```csharp
builder.Services.AddFluentValidationAutoValidation();
```
hooks FluentValidation into the MVC pipeline. When a `CreateArticleRequest` arrives, the framework automatically runs `CreateArticleRequestValidator`. If any rule fails, the controller action never executes — ASP.NET Core returns a 400 response with the error details automatically.

Key rules in `ArticleValidators.cs`:
- Title is required, max 200 characters
- Slug (if provided) must match `^[a-z0-9]+(?:-[a-z0-9]+)*$` — lowercase letters, digits, and hyphens only
- Summary is required, max 500 characters
- Each tag is max 50 characters

### Understanding Mappings

`ArticleMappings.cs` provides extension methods on the `Article` entity:

```csharp
// Used in detail responses
article.ToDto()

// Used in list responses
article.ToSummaryDto()
```

The mapping methods join across the `ArticleTags` navigation property to extract tag names, then sort them alphabetically. This ensures consistent ordering regardless of insertion order.

### Questions You Should Be Able to Answer After Stage 2

1. What is Dependency Injection and why does this project use it?
2. Trace a `POST /api/articles` request from the HTTP layer to the database. Name every class it passes through.
3. What does the controller do? What does the service do? Why are they separate?
4. What is a DTO? Why do we not just return the `Article` entity directly from the API?
5. What happens if you send a POST request with a title that is 300 characters long?
6. What is a slug? How does the system guarantee slugs are unique?
7. What does `AddScoped` mean in the context of DI lifetime? (Look it up if needed: Transient vs Scoped vs Singleton.)

### Practical Mini-Exercise

1. Open `ArticlesController.cs`. Find the `GetArticle` action (GET by ID). Without running the code, trace what happens if the article does not exist. What does the service return? What does the controller return to the client?
2. Open `ArticleService.cs`. Find the `GetArticlesAsync` method. Add a temporary `Console.WriteLine` to print the SQL query being generated. (Hint: EF Core can log queries — look at `appsettings.Development.json` and the EF Core logging docs.) Run the backend and trigger a search request from Swagger. Read the log output.
3. Open Swagger UI. Try to `POST /api/articles` with an empty title. What error do you get? What HTTP status code? Where in the code does that error come from?

### Low-Risk Code Changes to Try

- In `ArticleValidators.cs`, change the max length of a tag from 50 to 30. Save and re-run. Try to create an article with a tag that has 35 characters via Swagger. Does the validation fire?
- In `ArticleMappings.cs`, change the tag sort from alphabetical to reverse alphabetical (`OrderByDescending`). Create an article with multiple tags and check that the order changed in the API response.

### Common Beginner Confusion: Stage 2

**"Why is `IArticleService` separate from `ArticleService`? Why not just use the class directly?"**

The interface (`IArticleService`) defines *what* the service can do. The class (`ArticleService`) defines *how* it does it. By depending on the interface, the controller does not know or care about the implementation. You could swap in a mock implementation for tests, or a caching implementation, without touching the controller. This is the Dependency Inversion Principle — the "D" in SOLID.

**"What is `async/await`? Why does every method have it?"**

Database calls (and any I/O) can take time. `async/await` allows the thread to do other work while waiting for the database to respond. Without it, the thread would block, and your server could not handle other requests during that wait. Think of it as "tell me when you're done, I'll go help someone else in the meantime." Every method that calls the database should be async.

**"What is `IQueryable` and why does the service build a query before calling `.ToListAsync()`?"**

`IQueryable` represents a query that *has not run yet*. When you write:
```csharp
var query = _db.Articles.Where(a => a.Status == Published);
query = query.Where(a => a.Title.Contains(search));
var results = await query.ToListAsync();
```
The SQL is only sent to the database at `ToListAsync()`. This lets you compose filters dynamically without hitting the database multiple times. It is called *deferred execution*.

### Stage 2 Recap

You now understand the full backend request lifecycle: HTTP request → routing → controller → service → database → DTO → HTTP response. You understand DI, middleware, validation, mapping, and the role of each layer.

---

## Stage 3 — The Frontend Rendering Flow

### What to Read

Read these files in order:

1. `frontend/src/main.tsx` — Entry point. Mounts the React app.
2. `frontend/src/App.tsx` — Routing setup. Maps URLs to page components.
3. `frontend/src/pages/ArticlesPage.tsx` — Most complete example of state + hooks + components working together.
4. `frontend/src/hooks/useArticles.ts` — Custom hook that fetches data.
5. `frontend/src/services/articleService.ts` — Axios client that talks to the API.
6. `frontend/src/components/ArticleCard.tsx` — Simple presentational component.
7. `frontend/src/types/index.ts` — TypeScript type definitions.
8. `frontend/src/utils/format.ts` — Utility functions.

### How React Renders — The Mental Model

React is a UI library based on *components*. A component is a function that returns JSX (HTML-like syntax in JavaScript). When data changes, React re-renders the affected components automatically.

The key insight: **React components are functions. They run top-to-bottom every time their state changes.** This is different from traditional DOM manipulation where you imperatively update individual elements.

```
User action (click, type)
       │
       ▼
State changes (useState, useReducer)
       │
       ▼
React re-renders affected components
       │
       ▼
Browser DOM is updated efficiently (via React's virtual DOM diffing)
```

### The URL-to-Component Map

`App.tsx` defines all routes using React Router:

```
/ (root)              → HomePage
/articles             → ArticlesPage
/articles/:id         → ArticleDetailPage
/articles/new         → NewArticlePage
/articles/:id/edit    → EditArticlePage
```

When you visit `/articles`, React Router renders `ArticlesPage` inside the `<Layout>` (which includes the `<Header>`). When you click a link, React Router updates the URL and swaps the page component — no full page reload.

### Tracing a Page Render — ArticlesPage

`ArticlesPage.tsx` is the richest example. Here is what happens when a user navigates to `/articles`:

```
1. React Router matches /articles → renders ArticlesPage

2. ArticlesPage initializes state:
   - search: "" (text input)
   - category: "" (dropdown)
   - tag: "" (dropdown)
   - status: "" (dropdown)
   - page: 1

3. ArticlesPage calls useArticles({ search, category, tag, status, page })

4. useArticles runs a useEffect when its filter dependencies change:
   - Calls articleService.list(filters)
   - articleService sends GET /api/articles?search=&page=1 via Axios
   - Sets loading: true while waiting
   - When data arrives, sets data: ArticleListResponse, loading: false

5. ArticlesPage receives { data, loading, error } from useArticles

6. ArticlesPage renders:
   - <Header /> (always visible, from App.tsx layout)
   - <SearchBar /> — calls setSearch on change (debounced 300ms)
   - <FilterControls /> — calls setCategory/setTag/setStatus on change
   - If loading: <LoadingSpinner />
   - If error: <ErrorMessage />
   - If data.items.length === 0: <EmptyState />
   - Else: data.items.map(article => <ArticleCard key={article.id} article={article} />)
   - <Pagination /> — calls setPage on previous/next click

7. User types "database" in search → setSearch("database")
   → state changes → ArticlesPage re-renders
   → useArticles fires again (after 300ms debounce in SearchBar)
   → new API call with search=database
   → new data arrives → ArticleCard list updates
```

### Understanding Custom Hooks

A custom hook is a function whose name starts with `use`. It can call other hooks. Custom hooks are how React apps extract shared stateful logic.

`useArticles.ts` encapsulates:
1. State management (`loading`, `error`, `data`)
2. The side effect (API call) via `useEffect`
3. A `refetch` function to manually re-trigger

Without the custom hook, every page that lists articles would repeat this logic. With it, any component can call `useArticles(filters)` and get back `{ data, loading, error }`.

**Why `useEffect`?**
React renders synchronously. You cannot do async work (like fetch data) during the render itself. `useEffect` is the hook for side effects — work that happens *after* the render. Its second argument (the dependency array) controls when it re-runs:
- `[]` — runs once on mount
- `[search, page]` — runs when `search` or `page` changes
- No array — runs after every render (avoid this)

In `useArticles.ts`, the dependency is `JSON.stringify(filters)` — this is a common pattern when the dependency is an object, because React's shallow comparison would otherwise think the object is new every render (since a new object reference is created on each render).

### Understanding the Service Layer

`articleService.ts` creates an Axios instance:
```typescript
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5018',
});
```

`import.meta.env.VITE_API_URL` is how Vite exposes environment variables to the frontend. During development, the Vite proxy makes this transparent — all `/api` requests hit the local backend. In production, you set this variable to the real API URL.

Each method in `articleService.ts` is a thin wrapper around an Axios call. Error handling is centralized in `getErrorMessage(error)` — it extracts a human-readable message whether the error is an Axios HTTP error (with a JSON body) or a generic JavaScript error.

### Understanding TypeScript Types

`frontend/src/types/index.ts` defines the shapes of data the frontend works with. These match the DTO shapes returned by the backend API.

```typescript
// The shape of a list item
interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;
  createdAt: string;
  updatedAt: string;
}

// Extends summary with full content
interface Article extends ArticleSummary {
  content: string;
}
```

TypeScript enforces that you use these types correctly throughout the app. If the backend changes a field name, TypeScript will highlight every place in the frontend that breaks — before you run the code.

### Understanding Component Composition

React apps are built by nesting components. `ArticlesPage` composes several smaller components:
- `SearchBar` — knows nothing about articles. Just fires `onSearch(value)`.
- `FilterControls` — knows nothing about articles. Just fires callbacks when dropdowns change.
- `ArticleCard` — knows about a single `ArticleSummary`. Renders it. Knows nothing about lists or pagination.
- `Pagination` — knows about `page`, `totalPages`, and fires `onPageChange(newPage)`.
- `StateDisplay` (LoadingSpinner, ErrorMessage, EmptyState) — purely visual, no data awareness.

This is the "smart vs dumb" component pattern (also called "container vs presentational"). `ArticlesPage` is the "smart" component that holds state and coordinates data flow. The smaller components are "dumb" — they receive props and render, or fire callbacks.

### Questions You Should Be Able to Answer After Stage 3

1. What happens when a user types in the search bar? Trace the change from keypress to API call to DOM update.
2. What is `useEffect` for? What happens if you forget the dependency array?
3. What is the difference between `useState` and a prop?
4. Why is `articleService.ts` a separate file from the component files?
5. What does `ArticleSummary` in `types/index.ts` correspond to on the backend?
6. What is JSX? Is it valid JavaScript? How does it get compiled?
7. Why does `useArticles.ts` use `JSON.stringify(filters)` in the dependency array?

### Practical Mini-Exercise

1. Open `useArticles.ts`. Add a `console.log('fetching articles', filters)` inside the `useEffect`. Open the browser console. Load the articles page. Change the search. Count how many times "fetching articles" is logged per keystroke. Explain why.
2. Open `ArticleCard.tsx`. Add a new piece of text below the category: the article ID (e.g., `<span>ID: {article.id}</span>`). Save and see the change in the browser. Then revert it.
3. Open `ArticleDetailPage.tsx`. Find where it handles the case where the article is not found. What does it render? What HTTP status code would the backend have returned?

### Low-Risk Code Changes to Try

- In `utils/format.ts`, change the date format to include the time (hours and minutes). Check that dates update in the article cards.
- In `components/ArticleCard.tsx`, change the status badge from displaying the raw status string to displaying a friendlier label ("Live" instead of "Published", "Work in Progress" instead of "Draft"). This is a pure UI change with no backend impact.

### Common Beginner Confusion: Stage 3

**"Why does the component re-render so often? Is that slow?"**

React's virtual DOM diffing means that re-rendering a component does not necessarily mean the browser redraws the DOM. React calculates the minimum set of actual DOM changes needed. Re-rendering is cheap. Actual DOM mutations are expensive. That said, unnecessary re-renders can cause performance issues at scale. The `useCallback`, `useMemo`, and `React.memo` hooks exist to optimize this. Do not add them pre-emptively — add them only when you measure a real problem.

**"What is the difference between `interface` and `type` in TypeScript?"**

For object shapes, they are almost identical. Interfaces can be extended with `extends`. Type aliases use `=` and can represent more complex types (unions, intersections). This codebase uses both. For new data shapes, follow the existing convention (interfaces for object shapes like `Article`, type aliases for union types like `ArticleStatus = 'Draft' | 'Published' | 'Archived'`).

**"Why do we call `refetch` from `useArticle`? When does React not re-fetch automatically?"**

`useEffect` only re-runs when its dependencies change. After a successful `PUT` (update) request, the data in the database has changed — but the component's state (the article ID in the URL) has not. React does not magically know the data is stale. Calling `refetch` is how you tell the hook "go get the latest data right now."

**"What is JSX?"**

JSX is a syntax extension for JavaScript that looks like HTML. `<ArticleCard article={item} />` is JSX. It compiles to `React.createElement(ArticleCard, { article: item })`. The TypeScript compiler (via Vite) handles this compilation. You never write `React.createElement` directly.

### Stage 3 Recap

You now understand how React renders, how components communicate via props and callbacks, how custom hooks encapsulate data-fetching logic, how Axios sends requests to the backend, and how TypeScript types keep the data contract honest.

---

## Stage 4 — The Database and EF Core

### What to Read

Read these files in order:

1. `src/WikiProject.Api/Entities/Article.cs`
2. `src/WikiProject.Api/Entities/Tag.cs`
3. `src/WikiProject.Api/Entities/ArticleTag.cs`
4. `src/WikiProject.Api/Entities/ArticleStatus.cs`
5. `src/WikiProject.Api/Data/WikiDbContext.cs`
6. `src/WikiProject.Api/Migrations/20260315235834_InitialCreate.cs` (skim, do not memorize)
7. `src/WikiProject.Api/Data/SeedData.cs` (skim for the seeding pattern)
8. The EF Core "Getting Started" section at [learn.microsoft.com/ef/core](https://learn.microsoft.com/en-us/ef/core/)

### The Data Model

There are four entities (C# classes that map to database tables):

```
Article
───────────────────────────────
Id          int  (PK, auto-increment)
Title       string (required)
Slug        string (unique index)
Summary     string
Content     string
Category    string
Status      ArticleStatus (int: 0=Draft, 1=Published, 2=Archived)
CreatedAt   DateTime (UTC)
UpdatedAt   DateTime (UTC)
ArticleTags ICollection<ArticleTag>   ← navigation property

Tag
───────────────────────────────
Id          int  (PK, auto-increment)
Name        string (unique index)
ArticleTags ICollection<ArticleTag>   ← navigation property

ArticleTag  (join table: Articles ↔ Tags, many-to-many)
───────────────────────────────
ArticleId   int  (FK → Article.Id) ─┐
TagId       int  (FK → Tag.Id)     ─┘ composite PK

ArticleStatus (enum stored as int in the DB)
───────────────────────────────
Draft     = 0
Published = 1
Archived  = 2
```

**Why a join table?**
An article can have many tags. A tag can appear on many articles. This is a *many-to-many* relationship. In a relational database, you cannot store a list of IDs directly in a column. Instead, you use a *join table* (also called a junction table or bridge table) — `ArticleTag` — that has one row per article-tag pair.

### Understanding EF Core — The Big Picture

EF Core is an ORM (Object-Relational Mapper). It sits between your C# code and the database and does three things:

1. **Maps** C# classes to SQL tables and columns
2. **Translates** LINQ queries to SQL
3. **Tracks changes** to loaded objects so you can save them back

`WikiDbContext` is the *gateway* to the database. It has three `DbSet<T>` properties:
```csharp
public DbSet<Article> Articles { get; set; }
public DbSet<Tag> Tags { get; set; }
public DbSet<ArticleTag> ArticleTags { get; set; }
```

A `DbSet<T>` represents a database table. You query it with LINQ and EF Core turns your query into SQL.

### Understanding Fluent API Configuration

`WikiDbContext.OnModelCreating()` uses the Fluent API to configure relationships that EF Core cannot infer automatically:

```csharp
// ArticleTag has a composite primary key (two columns together)
modelBuilder.Entity<ArticleTag>()
    .HasKey(at => new { at.ArticleId, at.TagId });

// One Article has many ArticleTags; one ArticleTag belongs to one Article
modelBuilder.Entity<ArticleTag>()
    .HasOne(at => at.Article)
    .WithMany(a => a.ArticleTags)
    .HasForeignKey(at => at.ArticleId);

// Unique constraint: no two articles can have the same slug
modelBuilder.Entity<Article>()
    .HasIndex(a => a.Slug)
    .IsUnique();
```

**Why Fluent API instead of Data Annotations?**
Data Annotations are attributes like `[Required]` or `[MaxLength(200)]` placed on the entity class. Fluent API is configured in `OnModelCreating`. Both work. Fluent API is preferred for complex relationships (like composite keys) and for keeping entity classes clean. This project uses Fluent API for structural constraints (relationships, indexes) and keeps the entity classes as plain C# objects.

### Understanding Migrations

A *migration* is a versioned description of a schema change. When you add a new property to an entity, EF Core cannot automatically update the database. You generate a migration:

```bash
cd src/WikiProject.Api
dotnet-ef migrations add AddArticleViewCount
```

This creates a new file in `Migrations/` with `Up()` and `Down()` methods — `Up()` applies the change, `Down()` reverses it. When the app starts, `db.Database.Migrate()` applies all pending migrations in order.

**The migration file `20260315235834_InitialCreate.cs`** is the first (and currently only) migration. It creates all three tables and their indexes. Open it and read the `Up()` method — you will recognize the tables from the entity definitions.

**Why not just delete and recreate the database?**
In development, you could. In production, you cannot — you would lose all user data. Migrations let you evolve the schema incrementally without data loss.

### How EF Core Queries Work — Real Examples

Here is a simplified version of what `ArticleService.GetArticlesAsync()` does:

```csharp
// Start with all articles, including their tags
var query = _db.Articles
    .Include(a => a.ArticleTags)
        .ThenInclude(at => at.Tag)
    .AsQueryable();

// Conditionally add filters — SQL WHERE clauses are added here
if (!string.IsNullOrEmpty(queryParams.Category))
    query = query.Where(a => a.Category == queryParams.Category);

if (!string.IsNullOrEmpty(queryParams.Tag))
    query = query.Where(a => a.ArticleTags.Any(at => at.Tag.Name == queryParams.Tag));

// Count before pagination — this becomes SELECT COUNT(*)
var totalCount = await query.CountAsync();

// Apply pagination — this becomes LIMIT / OFFSET in SQL
var articles = await query
    .OrderByDescending(a => a.UpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**`Include` and `ThenInclude`** are how EF Core loads related data (called *eager loading*). Without `Include(a => a.ArticleTags).ThenInclude(at => at.Tag)`, the `ArticleTags` collection would be empty, and you could not access tag names.

**`AsQueryable()`** returns an `IQueryable<Article>`. Filters are added lazily — no SQL is sent until you call `.ToListAsync()` or `.CountAsync()`.

### Understanding Change Tracking

When EF Core loads an entity, it *tracks* it. When you change a property on a tracked entity and call `SaveChangesAsync()`, EF Core detects the change and generates an `UPDATE` SQL statement automatically.

```csharp
var article = await _db.Articles.FindAsync(id);
article.Title = "New Title";       // EF Core notices this change
article.UpdatedAt = DateTime.UtcNow;
await _db.SaveChangesAsync();      // Generates: UPDATE Articles SET Title=... WHERE Id=...
```

You do not write the `UPDATE` statement. EF Core does.

### Understanding Seed Data

`SeedData.cs` runs on every startup but only seeds if the database is empty (`if (await db.Articles.AnyAsync()) return;`). It creates 6 sample articles and 7 predefined tags. The seed data includes full Markdown content, which is why you see formatted text when you first run the app.

To re-seed from scratch: delete `src/WikiProject.Api/wiki.db` and restart the backend.

### Questions You Should Be Able to Answer After Stage 4

1. What is a join table? Why does `ArticleTag` exist instead of storing a list of tag names in the `Article` table?
2. What is a migration? Why not just write `CREATE TABLE` SQL directly?
3. What is `Include()` for? What would happen to the tags list if you removed it?
4. What does "change tracking" mean? If you load an article, change its title, and call `SaveChangesAsync()`, what SQL does EF Core generate?
5. What is the difference between `DbSet.Add()` and `DbSet.Update()`?
6. What does `await query.CountAsync()` vs `await query.ToListAsync()` do differently?
7. Why is `ArticleStatus` stored as an integer (0, 1, 2) in the database rather than a string?

### Practical Mini-Exercise

1. Open a SQLite viewer (e.g., DB Browser for SQLite, or the VS Code SQLite extension). Open `src/WikiProject.Api/wiki.db`. Browse the `Articles`, `Tags`, and `ArticleTags` tables. Create an article via the frontend with two tags. Refresh the viewer and find the new rows in all three tables.
2. Open `ArticleService.cs`. Find the section that creates a new article (`CreateAsync`). Add a `Console.WriteLine` that prints the generated slug before saving. Run the backend and create an article with the title "Hello World!". What slug is generated?
3. Add a new migration without changing any entities (just to see what the output looks like): run `dotnet-ef migrations add TestEmptyMigration` from `src/WikiProject.Api`. Then look at the generated file. Then remove it with `dotnet-ef migrations remove`.

### Low-Risk Code Changes to Try

- In `SeedData.cs`, change one of the seeded article titles. Delete `wiki.db`, restart the backend, and verify the change appears in the frontend.
- In `ArticleValidators.cs`, the summary max length is 500. Change it to 300. Try to create an article (via Swagger) with a 400-character summary. Does the validation reject it?

### Common Beginner Confusion: Stage 4

**"Why does EF Core sometimes load related data and sometimes it's null?"**

This is the most common EF Core confusion. By default, EF Core uses *lazy loading* (if configured) or simply returns `null` for navigation properties. In this project, related data must be explicitly requested using `Include()`. If you forget `Include(a => a.ArticleTags)`, the `ArticleTags` property will be an empty collection, and tag names will be missing from responses.

**"What is `AsNoTracking()` and should I use it?"**

When you load entities just to *read* them (not modify), you can add `.AsNoTracking()` to the query:
```csharp
var articles = await _db.Articles.AsNoTracking().ToListAsync();
```
This tells EF Core not to track these objects, which is faster and uses less memory. This project does not use it (it was likely omitted for simplicity), but it would be a safe optimization for the read-heavy list and detail endpoints.

**"What is the difference between `Find()` and `FirstOrDefaultAsync()`?"**

`Find(id)` looks in the EF Core change tracker first (returns a cached entity if already loaded) then hits the database. It only works with primary keys. `FirstOrDefaultAsync(a => a.Id == id)` always goes to the database and supports any predicate. For simple PK lookups, `FindAsync(id)` is slightly more efficient.

### Stage 4 Recap

You now understand the data model (entities, relationships, join table), how EF Core maps C# objects to SQL, how migrations version the schema, how LINQ queries become SQL, and how change tracking enables transparent updates.

---

## Stage 5 — Making Safe Small Changes

### The Right Mindset

Before making any change:
1. **Read the test.** Is there a test for this behavior? If yes, run it first, make sure it passes, make your change, run it again. (Note: this codebase currently has no automated tests — that is a known gap. The README mentions this as a future improvement. Until tests exist, verify changes manually.)
2. **Understand the blast radius.** Who calls this code? A change to `ArticleService` can affect every endpoint. A change to `ArticleCard.tsx` affects only the list views.
3. **Make one change at a time.** Do not refactor and add a feature simultaneously.
4. **Use version control.** Commit before you start. If something breaks, you can always revert.

### Change 1: Add a New Field to an Existing Article (Backend)

**Scenario:** Add a `ViewCount` field to track how many times each article has been viewed.

**Steps:**
1. Add `public int ViewCount { get; set; }` to `Article.cs`
2. Add `ViewCount = article.ViewCount` to both `ToDto()` and `ToSummaryDto()` in `ArticleMappings.cs`
3. Add `ViewCount` to `ArticleDto` and `ArticleSummaryDto` in `ArticleDtos.cs`
4. Generate a migration: `dotnet-ef migrations add AddViewCount`
5. Restart the backend — migrations run automatically

**What to verify:**
- The `GET /api/articles` response now includes `viewCount: 0` for all articles
- No existing functionality is broken
- The database has a new `ViewCount` column in the `Articles` table

**Safe because:** You are adding an optional field with a default value. Existing data is not affected. The frontend will simply receive the new field and ignore it until you choose to display it.

### Change 2: Add a New Filter to the Articles List (Backend)

**Scenario:** Add the ability to filter articles by date — only show articles updated after a given date.

**Steps:**
1. Add `public DateTime? UpdatedAfter { get; set; }` to `ArticleQueryParams` in `ArticleDtos.cs`
2. In `ArticleService.GetArticlesAsync()`, after the existing filters, add:
   ```csharp
   if (queryParams.UpdatedAfter.HasValue)
       query = query.Where(a => a.UpdatedAt >= queryParams.UpdatedAfter.Value);
   ```
3. In `ArticlesController.GetArticles()`, the new parameter is automatically bound from the query string (ASP.NET Core model binding handles this for `[FromQuery]` bound params)

**What to verify:**
- `GET /api/articles?updatedAfter=2025-01-01` returns only articles updated after Jan 1, 2025
- Omitting `updatedAfter` returns all articles as before

**Safe because:** You are adding a new optional filter. No existing code is removed or altered.

### Change 3: Display a New Field on the Frontend

**Scenario:** Show the article's category as a clickable badge that filters by that category.

**Steps:**
1. In `ArticleCard.tsx`, find where category is displayed. Wrap it in a `<button>` or `<span>` with an `onClick` handler.
2. Pass an `onCategoryClick?: (category: string) => void` prop to `ArticleCard`.
3. In `ArticlesPage.tsx`, pass `onCategoryClick={(cat) => setCategory(cat)}` to each `ArticleCard`.

**What to verify:**
- Clicking a category badge filters the list to show only articles in that category
- The filter dropdown updates to reflect the selected category
- The existing search and other filters still work

**Safe because:** You are adding a prop. Existing usages that do not pass the prop still work (the prop is optional).

### Change 4: Change a Validation Rule

**Scenario:** The product team decides that article titles should not exceed 150 characters (was 200).

**Steps:**
1. In `ArticleValidators.cs`, change `.MaximumLength(200)` to `.MaximumLength(150)` in both `CreateArticleRequestValidator` and `UpdateArticleRequestValidator`.

**What to verify:**
- Submit a POST request via Swagger with a 180-character title → should get 400 Bad Request
- Submit a POST request with a 100-character title → should succeed

**Note:** This is a breaking change for any article already stored with a title > 150 characters (they would fail to update). In a real system you would also add a database constraint and migrate existing data.

### Change 5: Add a New API Endpoint (Reading Only)

**Scenario:** Add `GET /api/articles/recent` that returns the 5 most recently updated published articles.

**Steps:**
1. Add a method to `IArticleService.cs`: `Task<IEnumerable<ArticleSummaryDto>> GetRecentAsync(int count);`
2. Implement in `ArticleService.cs`:
   ```csharp
   public async Task<IEnumerable<ArticleSummaryDto>> GetRecentAsync(int count)
   {
       var articles = await _db.Articles
           .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
           .Where(a => a.Status == ArticleStatus.Published)
           .OrderByDescending(a => a.UpdatedAt)
           .Take(count)
           .ToListAsync();
       return articles.Select(a => a.ToSummaryDto());
   }
   ```
3. Add the action to `ArticlesController.cs`:
   ```csharp
   [HttpGet("recent")]
   public async Task<IActionResult> GetRecent([FromQuery] int count = 5)
   {
       var articles = await _articleService.GetRecentAsync(count);
       return Ok(articles);
   }
   ```

**What to verify:**
- `GET /api/articles/recent` in Swagger returns 5 published articles
- `GET /api/articles/recent?count=3` returns 3

**Note:** You will notice that `HomePage.tsx` already uses this kind of logic — it fetches recent articles with specific query params. Adding a dedicated endpoint is a cleaner alternative.

### Questions You Should Be Able to Answer After Stage 5

1. Before making a change to `ArticleService.cs`, how do you know which controller endpoints are affected?
2. What is the minimum set of files you must change to add a new field to the API response?
3. If you add a new property to `Article.cs` without creating a migration, what happens when you start the app?
4. How do you verify that a validation change is working correctly?
5. What does "breaking change" mean in the context of an API?

### Common Beginner Confusion: Stage 5

**"I added a new property to `Article.cs` but I don't see it in the API response."**

You need to add it to the DTO too. EF Core will load the value from the database, but `ArticleMappings.ToDto()` only includes fields that are explicitly mapped. The entity and the DTO are separate — changing one does not change the other.

**"I created a migration but the database didn't update."**

Migrations run automatically when the app starts (`db.Database.Migrate()`). If you created a migration but the app did not restart, the migration has not run. Restart the backend. Alternatively, run `dotnet-ef database update` manually.

**"I changed a filter in the service but the old results keep coming back."**

Check that: (1) you saved the file, (2) the backend restarted (hot reload may not always trigger), (3) you are not looking at a cached response in the browser.

### Stage 5 Recap

You now have a systematic approach to making safe, incremental changes. You understand the blast radius of changes at each layer, how to verify your changes, and what to watch for.

---

## Stage 6 — Extending the System with Confidence

### What This Stage Is About

Stage 6 is about going beyond maintenance. You understand the system well enough to design and add new features from scratch. Each subsection below describes a significant feature, the design decisions involved, and the concrete steps to build it.

### Feature 1: User Authentication

**Why it matters:** Currently, anyone can create, edit, or delete any article. Authentication means only logged-in users can make changes.

**Recommended approach:** ASP.NET Core's built-in authentication + JWT (JSON Web Tokens).

**High-level design:**
1. Add a `User` entity with `Email`, `PasswordHash`, etc.
2. Add an `AuthController` with `POST /api/auth/register` and `POST /api/auth/login` endpoints
3. On login, return a JWT token
4. Add `[Authorize]` to any controller action that requires a logged-in user
5. In the frontend, store the token in memory or `localStorage`, attach it to Axios requests as an `Authorization: Bearer <token>` header

**Key decision — where to store the token:** `localStorage` is simple but vulnerable to XSS attacks (malicious scripts can read it). `httpOnly` cookies are more secure but require CORS configuration changes. For an internal wiki, `localStorage` is probably acceptable.

**Reference:** [ASP.NET Core Authentication overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/) | [JWT in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)

### Feature 2: Markdown Rendering

**Why it matters:** Article content is stored as Markdown text, but the frontend currently renders it as plain text inside a `<pre>` tag. Proper rendering means headings, code blocks, lists, tables, and links actually look like formatted content.

**Recommended approach:** Use the `react-markdown` npm package.

```bash
cd frontend
npm install react-markdown
```

In `ArticleDetailPage.tsx`, replace:
```tsx
<pre>{article.content}</pre>
```
with:
```tsx
import ReactMarkdown from 'react-markdown';
// ...
<ReactMarkdown>{article.content}</ReactMarkdown>
```

**Extension:** Add `remark-gfm` for GitHub Flavored Markdown (tables, strikethrough, task lists) and `react-syntax-highlighter` for code block syntax highlighting.

**Reference:** [react-markdown](https://github.com/remarkjs/react-markdown)

### Feature 3: Automated Testing

**Why it matters:** The README explicitly notes that testing is a future improvement. Without tests, refactoring is risky because you cannot automatically verify you haven't broken anything.

**Backend tests (xUnit):**
```bash
cd src
dotnet new xunit -n WikiProject.Api.Tests
cd WikiProject.Api.Tests
dotnet add reference ../WikiProject.Api/WikiProject.Api.csproj
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

Test the service layer using an in-memory database:
```csharp
[Fact]
public async Task CreateAsync_GeneratesSlug_FromTitle()
{
    var options = new DbContextOptionsBuilder<WikiDbContext>()
        .UseInMemoryDatabase("test_db")
        .Options;
    var db = new WikiDbContext(options);
    var service = new ArticleService(db, /* logger mock */);

    var request = new CreateArticleRequest { Title = "Hello World", ... };
    var result = await service.CreateAsync(request);

    Assert.Equal("hello-world", result.Slug);
}
```

**Frontend tests (Vitest + React Testing Library):**
```bash
cd frontend
npm install --save-dev vitest @testing-library/react @testing-library/jest-dom jsdom
```

Test that components render correctly:
```typescript
import { render, screen } from '@testing-library/react';
import ArticleCard from './ArticleCard';

test('renders article title', () => {
    render(<ArticleCard article={mockArticle} />);
    expect(screen.getByText(mockArticle.title)).toBeInTheDocument();
});
```

**Reference:** [xUnit docs](https://xunit.net/) | [Vitest docs](https://vitest.dev/) | [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)

### Feature 4: Improving Search with SQLite FTS5

**Why it matters:** The current search implementation loads all articles from the database into memory, then filters in C#. This is fine for small datasets but will become slow as the article count grows.

**SQLite FTS5** (Full-Text Search version 5) is a built-in SQLite extension that creates inverted indexes for efficient text search, without requiring a separate search service.

This is a more advanced change. It involves:
1. Adding a virtual FTS5 table via a migration (raw SQL in `Up()`)
2. Keeping the FTS5 index in sync when articles are created/updated/deleted (via triggers or manual calls)
3. Changing the search query to use FTS5's `MATCH` operator

**Reference:** [SQLite FTS5 documentation](https://www.sqlite.org/fts5.html)

**Alternative:** For larger scale, consider Elasticsearch, MeiliSearch, or Typesense. These are standalone services with powerful search capabilities and SDKs for .NET and JavaScript.

### Feature 5: Adding a Comment System

This is a full-feature exercise that touches every layer.

**Backend changes:**
1. Add `Comment` entity: `Id`, `ArticleId` (FK), `Content`, `CreatedAt`, `AuthorName`
2. Add `ArticleTags`-style relationship (one Article has many Comments)
3. Add `ArticleCommentDto`, `CreateCommentRequest`
4. Add `CommentsController` with `GET /api/articles/{id}/comments` and `POST /api/articles/{id}/comments`
5. Add migration

**Frontend changes:**
1. Add types for `Comment` and `CreateCommentRequest`
2. Add `commentService.ts` methods or extend `articleService.ts`
3. Add `useComments` hook
4. Add `CommentList` and `AddCommentForm` components
5. Include them in `ArticleDetailPage.tsx`

This exercise covers the entire full-stack development loop: entity → migration → service → controller → DTO → frontend types → service → hook → component.

### Design Principles to Internalize by Stage 6

**Single Responsibility Principle:** Each class does one thing. `ArticleService` handles article business logic. `WikiDbContext` handles database access. `ArticleCard` renders one article. When a class starts doing too many things, split it.

**Open/Closed Principle:** Add new behavior by adding new code, not by modifying existing code. The filter system in `ArticleService.GetArticlesAsync()` is a good example: adding a new filter (like `UpdatedAfter`) means adding a new `if` block without changing existing logic.

**DRY (Don't Repeat Yourself):** The custom hooks (`useArticles`, `useArticle`) exist because otherwise the fetch-loading-error pattern would be repeated in every page component. When you find yourself copying code, consider extracting it into a shared function or hook.

**YAGNI (You Aren't Gonna Need It):** Do not add complexity for future scenarios you are speculating about. The codebase is intentionally simple — it does not have caching, authentication, or search indexing because they were not needed yet. Add them when the need is real.

### Questions You Should Be Able to Answer After Stage 6

1. What is JWT authentication? How would you protect the "Delete article" endpoint so only logged-in users can call it?
2. What is the performance limitation of the current full-text search? At what scale would it become a problem?
3. What is the Single Responsibility Principle? Give an example of how it is (or is not) applied in this codebase.
4. If you wanted to add a `Comment` feature, what files would you create or modify? List them in order.
5. What is `react-markdown` and why would you use it instead of rendering content as plain text?

### Stage 6 Recap

You can now design and implement new features end-to-end. You understand the key design principles at work, know the current limitations, and have a roadmap for improving the system. You are ready to own and grow this codebase.

---

## Reference: Key File Map

Use this table when you need to find where something lives.

| What you want to do | Files to look at |
|--------------------|--------------------|
| Add a new API endpoint | `Controllers/ArticlesController.cs`, `Services/IArticleService.cs`, `Services/ArticleService.cs` |
| Change what an endpoint returns | `DTOs/ArticleDtos.cs`, `Mappings/ArticleMappings.cs` |
| Add or change a database field | `Entities/Article.cs` → run `dotnet-ef migrations add ...` |
| Change validation rules | `Validation/ArticleValidators.cs` |
| Change how slugs or tags work | `Services/ArticleService.cs` (search for `GenerateSlug` or `ResolveTagsAsync`) |
| Change startup / DI config | `Program.cs` |
| Change connection string or CORS | `appsettings.json` |
| Add a new page in React | `frontend/src/pages/` + add a `<Route>` in `App.tsx` |
| Add a new UI component | `frontend/src/components/` |
| Add or change how data is fetched | `frontend/src/hooks/useArticles.ts` or `useArticle.ts` |
| Change the API base URL or add a new API method | `frontend/src/services/articleService.ts` |
| Add or change TypeScript types | `frontend/src/types/index.ts` |
| Change date formatting | `frontend/src/utils/format.ts` |
| Re-seed the database | Delete `src/WikiProject.Api/wiki.db`, restart backend |

---

## Reference: Recommended External Resources

These are high-quality official resources organized by the technology they cover.

### .NET / ASP.NET Core
- [ASP.NET Core fundamentals overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/) — Start with "Dependency injection" and "Middleware"
- [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [FluentValidation documentation](https://docs.fluentvalidation.net/en/latest/)

### Entity Framework Core
- [EF Core Getting Started](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app) — Builds a complete example
- [EF Core relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships) — Explains one-to-many, many-to-many
- [EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core querying](https://learn.microsoft.com/en-us/ef/core/querying/)

### React
- [React official documentation](https://react.dev/) — The new React docs. Read "Learn React" start to finish.
- [React hooks reference](https://react.dev/reference/react) — `useState`, `useEffect`, `useCallback`, `useMemo`
- [React Router documentation](https://reactrouter.com/en/main)

### TypeScript
- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/intro.html) — Start with "The Basics" and "Object Types"

### Tooling
- [Vite documentation](https://vite.dev/guide/) — especially the proxy configuration and environment variables
- [Axios documentation](https://axios-http.com/docs/intro)
- [SQLite documentation](https://www.sqlite.org/docs.html)
- [Swagger / OpenAPI](https://swagger.io/docs/) — for understanding the generated API docs

### Concepts (not specific to this tech stack)
- [REST API design guide (MDN)](https://developer.mozilla.org/en-US/docs/Glossary/REST)
- [HTTP response status codes (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)
- [Dependency injection (Wikipedia)](https://en.wikipedia.org/wiki/Dependency_injection) — the conceptual introduction before reading the .NET docs

---

> **Related docs to watch for:** As this project's documentation suite grows, look for dedicated files covering the API contract in detail, the database schema history, deployment configuration, and the frontend component library. Each of these will build on what you have learned in this guide.
