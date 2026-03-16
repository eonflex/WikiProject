# WikiProject

An internal wiki and knowledge base application for storing and managing notes, documentation, and how-to articles.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10 ASP.NET Core Web API |
| Frontend | React 19 + TypeScript (Vite) |
| ORM | Entity Framework Core 10 |
| Database | SQLite |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Validation | FluentValidation |

## Folder Structure

```
WikiProject/
├── src/
│   └── WikiProject.Api/          # ASP.NET Core backend
│       ├── Controllers/          # HTTP endpoints
│       ├── Data/                 # DbContext + seed data
│       ├── DTOs/                 # Request / response models
│       ├── Entities/             # EF Core entity classes
│       ├── Mappings/             # Entity → DTO mapping helpers
│       ├── Migrations/           # EF Core database migrations
│       ├── Services/             # Business logic layer
│       ├── Validation/           # FluentValidation validators
│       ├── appsettings.json
│       └── Program.cs
├── frontend/                     # React TypeScript frontend
│   └── src/
│       ├── components/           # Shared UI components
│       ├── hooks/                # Custom React hooks
│       ├── pages/                # Page-level components
│       ├── services/             # API client layer
│       ├── types/                # TypeScript type definitions
│       └── utils/                # Utility functions
├── WikiProject.sln
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (LTS recommended)
- [dotnet-ef CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
# Install the EF Core CLI tool (one-time setup)
dotnet tool install --global dotnet-ef
```

## Running Locally

### Backend

```bash
cd src/WikiProject.Api

# Restore packages
dotnet restore

# Apply migrations and start (migrations + seeding run automatically on startup)
dotnet run
```

The API will be available at:
- **API base:** `http://localhost:5018`
- **Swagger UI:** `http://localhost:5018/swagger`

### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Start dev server
npm run dev
```

The frontend will be available at `http://localhost:5173`.

> The Vite dev server proxies `/api` requests to `http://localhost:5018`, so no CORS issues during development.

## Applying Migrations

Migrations run automatically on startup. To add a new migration manually:

```bash
cd src/WikiProject.Api
dotnet-ef migrations add <MigrationName>
dotnet-ef database update
```

To remove the last migration:

```bash
dotnet-ef migrations remove
```

## Seeding the Database

Seed data is applied automatically on first startup if the database is empty. The seeder creates 6 sample articles across multiple categories and tags.

To re-seed, delete the `wiki.db` file (in `src/WikiProject.Api/`) and restart the backend.

## Example API Routes

```
GET    /api/articles                  List articles (supports search + filters)
GET    /api/articles/{id}             Get article by ID
GET    /api/articles/slug/{slug}      Get article by slug
POST   /api/articles                  Create article
PUT    /api/articles/{id}             Update article
DELETE /api/articles/{id}             Delete article
GET    /api/categories                List all categories
GET    /api/tags                      List all tags
```

### Query Parameters for `GET /api/articles`

| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Full-text search (title, summary, content, category, tags) |
| `category` | string | Filter by category |
| `tag` | string | Filter by tag |
| `status` | string | Filter by status: `Draft`, `Published`, `Archived` |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 20, max: 100) |

## Notes on Future Improvements

- **Authentication:** The backend is structured for easy auth integration. Add `AddAuthentication()` / `AddAuthorization()` in `Program.cs` and apply `[Authorize]` attributes to controllers.
- **Markdown rendering:** The frontend renders article content as preformatted text. Replace with a library like `react-markdown` for proper Markdown rendering.
- **Rich text editing:** Replace the plain textarea with a Markdown editor (e.g., `@uiw/react-md-editor`).
- **Search improvements:** For larger datasets, consider full-text search with SQLite FTS5 or a dedicated search service.
- **Caching:** Add response caching to the article list endpoint for read-heavy workloads.
- **Testing:** Add xUnit tests for the service layer and Vitest/React Testing Library for frontend components.
