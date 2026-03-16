# WikiProject – Starting Task List

A prioritized backlog for building out this knowledge base application.

---

## Phase 1: Project Setup and Scaffolding

- [x] Create .NET 10 ASP.NET Core Web API project
- [x] Add EF Core + SQLite packages
- [x] Define `Article`, `Tag`, `ArticleTag` entities
- [x] Configure `WikiDbContext` with Fluent API
- [x] Create initial EF Core migration (`InitialCreate`)
- [x] Add seed data for local development
- [x] Configure CORS for React dev server
- [x] Add Swagger / OpenAPI support
- [x] Set up solution file (`WikiProject.sln`)
- [x] Create React + TypeScript frontend (Vite)
- [x] Set up project folder structure (pages, components, services, hooks, types, utils)
- [x] Add React Router for client-side routing
- [x] Create `.gitignore` for build artifacts
- [x] Write README with setup instructions

---

## Phase 2: Core CRUD

- [x] `GET /api/articles` – list articles
- [x] `GET /api/articles/{id}` – get by ID
- [x] `GET /api/articles/slug/{slug}` – get by slug
- [x] `POST /api/articles` – create article
- [x] `PUT /api/articles/{id}` – update article
- [x] `DELETE /api/articles/{id}` – delete article
- [x] `GET /api/categories` – list categories
- [x] `GET /api/tags` – list tags
- [x] Auto-generate slug from title
- [x] Enforce slug uniqueness
- [x] Refresh `UpdatedAt` on every save
- [x] DTOs + FluentValidation request validation
- [x] Return appropriate HTTP status codes
- [x] Article list page (ArticlesPage)
- [x] Article detail page (ArticleDetailPage)
- [x] Create article page (NewArticlePage)
- [x] Edit article page (EditArticlePage)
- [x] Delete article with confirmation dialog
- [x] Home / dashboard page (HomePage)

---

## Phase 3: Search and Filtering

- [x] Backend: full-text search across title, summary, content, category, and tags
- [x] Backend: filter by category, tag, and status
- [x] Backend: pagination support
- [x] Frontend: search bar with debounced input
- [x] Frontend: category / tag / status filter dropdowns
- [x] Frontend: paginator component
- [x] Frontend: show total count and page info
- [ ] Backend: SQLite FTS5 integration for better full-text search performance (stretch)
- [ ] Frontend: URL-driven search state (sync filters with query params)

---

## Phase 4: UX Polish

- [x] Article cards with title, summary, tags, category, status badge, and date
- [x] Loading, error, and empty state components
- [x] Controlled forms with client-side validation
- [x] Responsive layout (mobile-friendly)
- [x] Sticky header with navigation
- [ ] Add a Markdown renderer for article content (e.g. `react-markdown`)
- [ ] Add a Markdown editor for the content textarea (e.g. `@uiw/react-md-editor`)
- [ ] Toast / snackbar notifications for create, update, delete actions
- [ ] Breadcrumb navigation on article detail page
- [ ] Keyboard shortcut for new article (e.g. `N`)
- [ ] Confirm-before-leave prompt on unsaved form changes

---

## Phase 5: Stretch Goals

- [ ] **Authentication:** JWT or cookie-based auth with ASP.NET Core Identity
- [ ] **User attribution:** track created-by / updated-by on articles
- [ ] **Article history / versioning:** store previous versions of article content
- [ ] **Favorites / bookmarks:** let users bookmark articles
- [ ] **Related articles:** link articles to each other
- [ ] **Print / export:** export articles to PDF or Markdown file
- [ ] **Dark mode:** respect `prefers-color-scheme` and allow manual toggle
- [ ] **Bulk actions:** archive or delete multiple articles at once
- [ ] **API rate limiting:** prevent abuse with ASP.NET Core rate limiting middleware
- [ ] **Docker support:** add `Dockerfile` and `docker-compose.yml` for containerized dev
- [ ] **CI pipeline:** add GitHub Actions workflow for build + test on push
- [ ] **Unit tests:** xUnit tests for service layer; Vitest + React Testing Library for frontend
