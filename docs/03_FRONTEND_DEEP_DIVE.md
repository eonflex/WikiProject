# Frontend Deep Dive

> **Audience:** A newer developer who is smart and motivated but still getting oriented in this codebase.  
> **Goal:** Not just "what files exist" — but *why* they exist, *how* they work together, and *what you should study next*.

---

## Table of Contents

1. [What the Frontend Is Responsible For](#1-what-the-frontend-is-responsible-for)
2. [If You Are New to React, Read This First](#2-if-you-are-new-to-react-read-this-first)
3. [Technology Stack at a Glance](#3-technology-stack-at-a-glance)
4. [Directory Structure Explained](#4-directory-structure-explained)
5. [App Startup: How the Browser Gets a Running App](#5-app-startup-how-the-browser-gets-a-running-app)
6. [Routing: How URLs Map to Pages](#6-routing-how-urls-map-to-pages)
7. [Pages: One Component Per Screen](#7-pages-one-component-per-screen)
8. [Shared Components: Reusable Building Blocks](#8-shared-components-reusable-building-blocks)
9. [TypeScript Types: Your Contract with the API](#9-typescript-types-your-contract-with-the-api)
10. [Services / API Layer: Talking to the Backend](#10-services--api-layer-talking-to-the-backend)
11. [Custom Hooks: Reusable Data-Fetching Logic](#11-custom-hooks-reusable-data-fetching-logic)
12. [State Handling: How the App Remembers Things](#12-state-handling-how-the-app-remembers-things)
13. [Forms and Validation](#13-forms-and-validation)
14. [Loading, Error, and Empty States](#14-loading-error-and-empty-states)
15. [How Data Flows from API to Rendered UI](#15-how-data-flows-from-api-to-rendered-ui)
16. [Styling Approach](#16-styling-approach)
17. [Why the Project Is Organized This Way](#17-why-the-project-is-organized-this-way)
18. [Alternative Approaches and Tradeoffs](#18-alternative-approaches-and-tradeoffs)
19. [What to Study Next](#19-what-to-study-next)

---

## 1. What the Frontend Is Responsible For

The frontend is a **single-page application (SPA)** — a web app that loads once and then changes what is displayed on screen without doing full-page reloads. It is the part the user sees and interacts with.

In this project, the frontend:

- **Renders the UI**: shows a knowledge-base wiki for a team. Users can browse, search, filter, create, edit, and delete articles.
- **Handles navigation**: as the user clicks around, the URL changes and different "pages" appear — but no real page reload happens. React Router manages this.
- **Fetches and displays data**: calls a backend REST API (a .NET server running on port 5018 during development) and uses the JSON responses to render content.
- **Manages form input and validation**: the create/edit article form lives entirely in the frontend. It collects input, validates it, and sends the result to the backend.
- **Shows feedback**: loading spinners, error messages, and empty-state illustrations so the user always knows what is happening.

The frontend does **not** store data permanently. It has no database. Data lives in the backend; the frontend is purely a view and interaction layer.

---

## 2. If You Are New to React, Read This First

This section gives you just enough React background to follow the rest of the document without getting lost. If you are already comfortable with React hooks and components, skip ahead to [Section 3](#3-technology-stack-at-a-glance).

### React's core idea: UI as a function of state

React is a JavaScript library for building user interfaces. Its core promise is simple:

> **UI = f(state)**
>
> Your interface is just a function that takes some data (state) and returns what to display on screen.

When the data (state) changes, React re-runs those functions and updates only the parts of the page that changed. You never manually manipulate the DOM; you just tell React *what the UI should look like* for a given set of data.

### Components

Everything visible in a React app is a **component** — a JavaScript/TypeScript function that returns JSX (HTML-like syntax).

```tsx
// A simple component
function Greeting({ name }: { name: string }) {
  return <h1>Hello, {name}!</h1>;
}
```

Components are composed: you build small components and nest them inside larger ones. The `App` component at the top contains all others.

### JSX

JSX looks like HTML but is actually JavaScript. You can embed expressions with `{}`:

```tsx
<p>There are {articles.length} articles.</p>
```

It gets compiled to regular JavaScript function calls by Vite/TypeScript before the browser ever sees it.

### Props

**Props** (short for *properties*) are the inputs to a component — the same idea as function arguments:

```tsx
<ArticleCard article={someArticle} />
//            ↑ this is a prop
```

Props flow **down** from parent to child. A child component cannot change its own props; only the parent can pass different ones.

### State

**State** is data that belongs to a component and can change over time. When state changes, React re-renders the component:

```tsx
const [count, setCount] = useState(0);
// count is the current value
// setCount is how you change it
```

State flows down through props. To share state between sibling components, you "lift" it up to their common parent.

### Hooks

**Hooks** are special React functions (always starting with `use`) that let you tap into React features inside function components:

- `useState` — declare a piece of state
- `useEffect` — run side effects (like fetching data) after rendering
- `useCallback` — memoize a function so it doesn't get recreated every render
- Custom hooks (like `useArticles`) — your own reusable logic built from the hooks above

### The component lifecycle (simplified)

1. **Mount**: component appears on screen for the first time. `useEffect` callbacks with `[]` dependency array run once here.
2. **Update**: state or props change → component re-renders. `useEffect` callbacks whose dependencies changed also run.
3. **Unmount**: component disappears from screen. Cleanup functions returned from `useEffect` run here.

### Common beginner confusion: stale state in effects

```tsx
useEffect(() => {
  fetchData(search); // "search" is captured at the time this effect runs
}, [search]);       // this tells React: re-run the effect when "search" changes
```

If you forget to list a variable in the dependency array, the effect will use an outdated ("stale") value. ESLint's `react-hooks/exhaustive-deps` rule catches this automatically.

---

## 3. Technology Stack at a Glance

| Concern | Tool | Version | Why this choice |
|---|---|---|---|
| UI framework | React | 19.2.4 | Industry standard, component model, huge ecosystem |
| Language | TypeScript | 5.9.3 | Catches type errors at compile time, improves editor experience |
| Build tool | Vite | 8.0.0 | Near-instant dev server startup, fast HMR, modern ESM |
| Routing | React Router DOM | 7.13.1 | De-facto standard router for React SPAs |
| HTTP client | Axios | 1.13.6 | Cleaner API than `fetch`, automatic JSON parsing, interceptor support |
| Styling | Vanilla CSS + CSS variables | — | Zero dependencies, no build step overhead |
| Linting | ESLint 9 + typescript-eslint | 9.39.4 | Enforces code quality and React best practices |

All source code lives in `frontend/src/`. The compiled output goes to `frontend/dist/` (ignored by git).

---

## 4. Directory Structure Explained

```
frontend/
├── index.html              ← The one HTML file the browser loads
├── vite.config.ts          ← Vite dev server + build config
├── tsconfig.json           ← TypeScript root config (references app + node)
├── tsconfig.app.json       ← TS config for src/ code
├── tsconfig.node.json      ← TS config for vite.config.ts itself
├── eslint.config.js        ← ESLint rules
├── package.json            ← Dependencies and npm scripts
├── .env.example            ← Documents required environment variables
└── src/
    ├── main.tsx            ← JS entry point — mounts React into the DOM
    ├── App.tsx             ← Root component — sets up router + all routes
    ├── index.css           ← ALL styles (global + every component)
    ├── App.css             ← Mostly unused legacy styles
    ├── assets/             ← Static images (hero.png, svgs)
    ├── components/         ← Reusable UI pieces (not tied to any one route)
    │   ├── Header.tsx
    │   ├── ArticleCard.tsx
    │   ├── ArticleForm.tsx
    │   ├── FilterControls.tsx
    │   ├── Pagination.tsx
    │   ├── SearchBar.tsx
    │   └── StateDisplay.tsx
    ├── hooks/              ← Custom React hooks (data-fetching logic)
    │   ├── useArticles.ts
    │   └── useArticle.ts
    ├── pages/              ← One component per URL route
    │   ├── HomePage.tsx
    │   ├── ArticlesPage.tsx
    │   ├── ArticleDetailPage.tsx
    │   ├── NewArticlePage.tsx
    │   └── EditArticlePage.tsx
    ├── services/           ← All HTTP calls to the backend API
    │   └── articleService.ts
    ├── types/              ← TypeScript interfaces shared across the app
    │   └── index.ts
    └── utils/              ← Small pure utility functions
        └── format.ts
```

### Why separate `pages/`, `components/`, `hooks/`, `services/`?

This is the **feature-agnostic layer separation** pattern. Each folder answers a different question:

- `pages/` — "Which route does this belong to?"
- `components/` — "Can I reuse this on more than one page?"
- `hooks/` — "Does this contain stateful fetching logic I want to share?"
- `services/` — "Does this make HTTP calls?"
- `types/` — "What shape does my data have?"
- `utils/` — "Is this a pure function with no React dependency?"

This makes it very fast to answer "where is the code that fetches articles?" (`services/articleService.ts`) or "where is the form for editing articles?" (`components/ArticleForm.tsx`).

---

## 5. App Startup: How the Browser Gets a Running App

Understanding what happens in those first few milliseconds helps demystify the whole system.

### Step 1 — `index.html` is served

```html
<!-- frontend/index.html -->
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>frontend</title>
  </head>
  <body>
    <div id="root"></div>                              <!-- ① empty container -->
    <script type="module" src="/src/main.tsx"></script> <!-- ② entry JS -->
  </body>
</html>
```

The browser loads this file first. At this point the page is completely blank — just an empty `<div id="root">`. The `type="module"` attribute tells the browser this is a modern ES module, which Vite serves directly during development.

> **Why is there almost nothing in `index.html`?** Because React renders everything dynamically. The HTML is just a shell. The real app lives in JavaScript.

### Step 2 — `main.tsx` boots React

```tsx
// frontend/src/main.tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
```

`createRoot` is the React 18+ API for attaching React to a DOM node. It replaces the older `ReactDOM.render()`. It finds the `<div id="root">` in the HTML and tells React: "own this node — render everything here."

`StrictMode` is a development-only wrapper that intentionally double-invokes certain functions (like `useEffect` setup and cleanup) to help you catch bugs early. It has **no effect in production builds**.

`index.css` is imported here so its styles apply globally. Because Vite treats CSS imports as side effects, the styles are injected into `<style>` tags in the browser's `<head>`.

### Step 3 — `App.tsx` sets up routing

```tsx
// frontend/src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import HomePage from './pages/HomePage';
import ArticlesPage from './pages/ArticlesPage';
import ArticleDetailPage from './pages/ArticleDetailPage';
import NewArticlePage from './pages/NewArticlePage';
import EditArticlePage from './pages/EditArticlePage';

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
        </Routes>
      </main>
    </BrowserRouter>
  );
}
```

`BrowserRouter` listens to the browser's URL bar. `Routes` looks at the current URL and renders the matching `Route`. `Header` is outside `Routes`, so it renders on every page.

### The startup sequence in a diagram

```
Browser requests /
  → index.html served (blank shell)
    → main.tsx loads
      → createRoot(...).render(<App />)
        → App renders BrowserRouter
          → URL is "/" → HomePage renders
            → useEffect fires → fetch recent articles
              → API response arrives → state updates → re-render with articles
```

---

## 6. Routing: How URLs Map to Pages

### What is client-side routing?

Traditional websites: the browser requests `/articles` → the server sends a *new HTML page*.

SPAs: the browser requests `/articles` → the server always sends the *same `index.html`* → JavaScript reads the URL and renders the right component. **No network round-trip, no page flash.**

### React Router DOM v7

The app uses [React Router DOM](https://reactrouter.com/) v7. The key components are:

- **`BrowserRouter`** — enables the HTML5 History API so URLs look like real URLs (not hash-based `/#/articles`). Wraps the entire app in `App.tsx`.
- **`Routes`** — a container that looks at the current URL and renders only the first matching `Route`.
- **`Route`** — pairs a URL pattern with a React component.
- **`Link`** — renders an `<a>` tag that triggers a client-side navigation (no page reload).
- **`useNavigate`** — a hook that lets you navigate programmatically (e.g., after a form submission).
- **`useParams`** — a hook that reads dynamic URL segments like `:id`.
- **`useLocation`** — a hook that gives the current URL's pathname, used in `Header` for active link highlighting.

### Route table

| Path | Component | What it shows |
|---|---|---|
| `/` | `HomePage` | Hero section + 6 most recently updated published articles |
| `/articles` | `ArticlesPage` | Searchable, filterable, paginated list of all articles |
| `/articles/new` | `NewArticlePage` | Blank form to create a new article |
| `/articles/:id` | `ArticleDetailPage` | Full content of one article + Edit/Delete buttons |
| `/articles/:id/edit` | `EditArticlePage` | Pre-filled form to edit an existing article |

### Dynamic route segments

`:id` is a **URL parameter** (also called a dynamic segment). When the URL is `/articles/42`, React Router captures `42` as `id`. The component retrieves it with:

```tsx
const { id } = useParams<{ id: string }>();
const numericId = Number(id);
```

> **Common beginner confusion:** `useParams` always returns strings, even if the segment looks like a number. That is why `Number(id)` is needed before passing it to `articleService.getById()`.

### Route ordering: why `/articles/new` is before `/articles/:id`

```tsx
<Route path="/articles/new" element={<NewArticlePage />} />
<Route path="/articles/:id" element={<ArticleDetailPage />} />
```

React Router v6+ uses **best-match** routing (not first-match), so `:id` would never accidentally capture the literal string `"new"`. In older React Router v5, order mattered more. This is safe in v7 but still a good habit to put specifics before wildcards.

### Navigation after actions

After creating an article:
```tsx
const navigate = useNavigate();
// ...
navigate(`/articles/${article.id}`); // go to the new article's detail page
```

After deleting an article:
```tsx
navigate('/articles'); // go back to the list
```

---

## 7. Pages: One Component Per Screen

Pages are the top-level components rendered by the router. Each page owns the data and state for its screen. Think of a page as the "screen" in a mobile app: one thing the user is looking at at a time.

### `HomePage` — Landing Page

**File:** `frontend/src/pages/HomePage.tsx`

**What it does:** Shows a hero section with a call-to-action, plus up to 6 recently updated *Published* articles.

**Key patterns:**
- Fetches directly with `articleService.list(...)` (not via a hook). This is fine because this is a simple one-time fetch with no filter controls.
- Uses `useEffect` with an empty `[]` dependency array, so it runs once on mount.
- Manages `loading` state locally with `useState`.

```tsx
const [recent, setRecent] = useState<ArticleSummary[]>([]);
const [loading, setLoading] = useState(true);

useEffect(() => {
  articleService
    .list({ status: 'Published', pageSize: 6 })
    .then((res) => setRecent(res.items))
    .catch(() => setRecent([]))           // fail silently — recent articles not critical
    .finally(() => setLoading(false));
}, []);
```

**Why no hook here?** The `useArticles` hook is designed for the full articles list with dynamic filters. The homepage just needs a quick, static fetch. Reusing the hook would work but adds unnecessary complexity here.

---

### `ArticlesPage` — Main Browse/Search Page

**File:** `frontend/src/pages/ArticlesPage.tsx`

**What it does:** The most feature-rich page. Shows the full list of articles with:
- A text search input (with debounce)
- Dropdowns for category, tag, and status filtering
- Pagination (12 per page)
- A "Clear filters" button

**Key patterns:**

**Debounced search:** The user types into the search box, but we do not immediately fire an API call on every keystroke — that would hammer the server. Instead, we wait 300ms after the user stops typing:

```tsx
const [search, setSearch] = useState('');
const [debouncedSearch, setDebouncedSearch] = useState('');

useEffect(() => {
  const timer = setTimeout(() => {
    setDebouncedSearch(search);
    setPage(1);       // reset to page 1 on new search
  }, 300);
  return () => clearTimeout(timer); // cancel if user keeps typing
}, [search]);
```

`search` updates immediately (so the input feels responsive). `debouncedSearch` updates only after a 300ms pause (so the API call fires only once per "burst" of typing). The cleanup function cancels the timer if a new keystroke arrives before 300ms.

**Loading categories and tags once:**

```tsx
useEffect(() => {
  Promise.all([articleService.getCategories(), articleService.getTags()])
    .then(([cats, tgs]) => {
      setCategories(cats);
      setTags(tgs);
    })
    .catch(() => {/* non-critical, filter dropdowns just show empty */});
}, []); // [] means: run once when the page mounts
```

`Promise.all` runs both requests in parallel (they don't depend on each other) and waits for both to finish.

**Using the `useArticles` hook:**

```tsx
const { data, loading, error } = useArticles({
  search: debouncedSearch || undefined,
  category: category || undefined,
  tag: tag || undefined,
  status: status || undefined,
  page,
  pageSize: 12,
});
```

The hook automatically re-fetches when any of these values change.

---

### `ArticleDetailPage` — View a Single Article

**File:** `frontend/src/pages/ArticleDetailPage.tsx`

**What it does:** Shows the full content of one article, with metadata and Edit/Delete actions.

**Key patterns:**

```tsx
const { id } = useParams<{ id: string }>();
const numericId = Number(id);
const { article, loading, error } = useArticle(numericId);
```

The `id` comes from the URL. The `useArticle` hook fetches the article and returns the three standard states: `article`, `loading`, and `error`.

**Delete with confirmation:**

```tsx
async function handleDelete() {
  if (!article) return;
  if (!window.confirm(`Delete "${article.title}"? This cannot be undone.`)) return;
  setDeleting(true);
  try {
    await articleService.delete(article.id);
    navigate('/articles');
  } catch {
    alert('Failed to delete article.');
    setDeleting(false);
  }
}
```

This uses the browser's built-in `window.confirm()` dialog. It is simple but not particularly polished. A future improvement would be a custom modal component.

**Early returns for loading/error:**

```tsx
if (loading) return <LoadingSpinner />;
if (error || !article) return <ErrorMessage message={error ?? 'Article not found.'} />;
```

"Early returns" are a common React pattern. If data is not ready, return the fallback UI immediately. This avoids deeply nested conditionals and keeps the "happy path" code uncluttered.

---

### `NewArticlePage` — Create an Article

**File:** `frontend/src/pages/NewArticlePage.tsx`

**What it does:** Renders the `ArticleForm` with no initial values, and on submit calls `articleService.create`.

```tsx
async function handleSubmit(data: CreateArticleRequest) {
  const article = await articleService.create(data);
  navigate(`/articles/${article.id}`);
}

return (
  <div className="page">
    <div className="page-header">
      <h1>New Article</h1>
    </div>
    <ArticleForm onSubmit={handleSubmit} submitLabel="Create Article" />
  </div>
);
```

This page is intentionally minimal. All the form logic lives in `ArticleForm`. `NewArticlePage` only knows two things: what to do on submit, and what label to put on the submit button.

---

### `EditArticlePage` — Edit an Existing Article

**File:** `frontend/src/pages/EditArticlePage.tsx`

**What it does:** Fetches the existing article, then renders `ArticleForm` with that article's data as initial values.

```tsx
const { article, loading, error } = useArticle(numericId);

async function handleSubmit(data: CreateArticleRequest) {
  await articleService.update(numericId, data);
  navigate(`/articles/${numericId}`);
}

// ...
<ArticleForm initial={article} onSubmit={handleSubmit} submitLabel="Save Changes" />
```

The `initial` prop is what makes `ArticleForm` reusable for both creation and editing. When `initial` is provided, the form pre-fills its fields. Without it, the form starts blank.

> **Common beginner confusion:** You might wonder why the page fetches the article again instead of reading it from some global cache. The answer: there is no global cache in this app (no Redux, no React Query). Each page fetches its own data fresh. For a small wiki this is perfectly fine. See [Section 12](#12-state-handling-how-the-app-remembers-things) for the tradeoffs.

---

## 8. Shared Components: Reusable Building Blocks

Components in `components/` are used by more than one page (or are complex enough to deserve their own file even if currently used in one place).

### `Header` — Navigation Bar

**File:** `frontend/src/components/Header.tsx`

The sticky navigation bar shown on every page.

```tsx
const { pathname } = useLocation();

const navLinks = [
  { to: '/', label: 'Home' },
  { to: '/articles', label: 'Articles' },
  { to: '/articles/new', label: 'New Article' },
];
```

It uses `useLocation()` to get the current URL path and highlights the active link:

```tsx
className={`nav-link${pathname === to ? ' nav-link--active' : ''}`}
```

This is a simple but effective approach: compare the current path to each link's `to` path. The active link gets an extra CSS class.

> **Common beginner confusion:** Many newcomers wonder why there is no `<NavLink>` component from React Router here. `NavLink` does the same active-class logic automatically. Using `useLocation` and manual class concatenation is equally valid — just more explicit.

---

### `ArticleCard` — Article Summary Card

**File:** `frontend/src/components/ArticleCard.tsx`

Displays a single article in the grid on `HomePage` and `ArticlesPage`.

```tsx
interface Props {
  article: ArticleSummary;
}

export default function ArticleCard({ article }: Props) { ... }
```

It receives one prop: an `ArticleSummary` object. Notice it uses `ArticleSummary` (not the full `Article`) — the card only needs the fields in the summary (title, summary text, category, tags, status, date). The `content` field is not needed here, so we do not fetch it.

This is a good example of interface segregation: use the narrowest type that satisfies the need.

The status badge uses a utility function:

```tsx
<span className={`status-badge ${statusClass(article.status)}`}>
  {article.status}
</span>
```

`statusClass()` is defined in `utils/format.ts`:

```ts
export function statusClass(status: string): string {
  switch (status) {
    case 'Published': return 'status-published';
    case 'Draft':     return 'status-draft';
    case 'Archived':  return 'status-archived';
    default:          return '';
  }
}
```

---

### `ArticleForm` — Create / Edit Form

**File:** `frontend/src/components/ArticleForm.tsx`

This is the most complex component in the codebase. It handles the form for both creating and editing articles.

**Props:**

```tsx
interface Props {
  initial?: Article;            // when editing: the existing article data
  onSubmit: (data: CreateArticleRequest) => Promise<void>; // what to do on submit
  submitLabel?: string;         // button text (default: 'Save')
}
```

The form is a **controlled form** — every input's value is stored in React state and every keystroke updates that state. See [Section 13](#13-forms-and-validation) for the full explanation.

**Internal state:**

```tsx
interface FormState {
  title: string;
  slug: string;
  summary: string;
  content: string;
  category: string;
  tags: string;    // comma-separated in the UI
  status: number;  // 0=Draft, 1=Published, 2=Archived
}
```

Note that `tags` is a plain comma-separated string *inside the form* (`"react, typescript"`). It is only split into an array (`["react", "typescript"]`) when the form is submitted. This is a UI convenience: letting users type tags naturally without a complex tag-chip input.

---

### `SearchBar`

**File:** `frontend/src/components/SearchBar.tsx`

A simple controlled text input with an emoji icon prefix. The parent (`ArticlesPage`) owns the `value` and `onChange` handler — the SearchBar itself is "dumb" (it has no state of its own).

```tsx
interface Props {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}
```

The debounce logic lives in the parent, not here. This keeps `SearchBar` reusable: any page can use it with its own debounce strategy (or no debounce at all).

---

### `FilterControls`

**File:** `frontend/src/components/FilterControls.tsx`

Three `<select>` dropdowns (category, tag, status) with a conditional "Clear filters" button.

```tsx
const hasFilters = selectedCategory || selectedTag || selectedStatus;
// ...
{hasFilters && (
  <button className="btn btn-ghost" onClick={onReset} type="button">
    Clear filters
  </button>
)}
```

The "Clear filters" button only appears when at least one filter is active — a small UX touch.

All values and handlers are passed as props from `ArticlesPage`. `FilterControls` has no state; it is a pure rendering component.

---

### `Pagination`

**File:** `frontend/src/components/Pagination.tsx`

Shows Previous/Next buttons and a "x–y of z" count. Returns `null` (renders nothing) when there is only one page:

```tsx
if (totalPages <= 1) return null;
```

This is the idiomatic React way to conditionally hide a component: return `null` from the render function.

---

### `StateDisplay` — Loading, Error, Empty

**File:** `frontend/src/components/StateDisplay.tsx`

Three named exports from one file:

```tsx
export function LoadingSpinner({ message = 'Loading…' }: Props) { ... }
export function ErrorMessage({ message = 'Something went wrong.' }: Props) { ... }
export function EmptyState({ message = 'No articles found.' }: Props) { ... }
```

They are grouped in one file because they are thematically related (all handle non-happy-path states) and are all small. See [Section 14](#14-loading-error-and-empty-states) for how they are used.

---

## 9. TypeScript Types: Your Contract with the API

**File:** `frontend/src/types/index.ts`

TypeScript **interfaces** and **types** define the shape of data objects. They exist only at compile time — they compile away to nothing in the final JavaScript. Their job is to catch mistakes before the code runs.

```ts
// A union type: ArticleStatus can only be one of these three strings
export type ArticleStatus = 'Draft' | 'Published' | 'Archived';

// The shape of a summary article (used in lists)
export interface ArticleSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  category: string;
  tags: string[];
  status: ArticleStatus;
  createdAt: string;  // ISO 8601 date string from the backend
  updatedAt: string;
}

// Full article — extends ArticleSummary, adds the body
export interface Article extends ArticleSummary {
  content: string;
}

// Shape of the paginated list response from GET /api/articles
export interface ArticleListResponse {
  items: ArticleSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Query parameters for filtering articles
export interface ArticleFilters {
  search?: string;    // '?' means optional
  category?: string;
  tag?: string;
  status?: ArticleStatus;
  page?: number;
  pageSize?: number;
}

// Payload for POST /api/articles
export interface CreateArticleRequest {
  title: string;
  slug?: string;    // optional — backend can auto-generate
  summary: string;
  content: string;
  category: string;
  tags: string[];
  status: number;   // backend expects numeric 0/1/2, not string 'Draft'/'Published'
}

// Payload for PUT /api/articles/:id (same fields as create)
export interface UpdateArticleRequest extends CreateArticleRequest {}

// Backend validation error shape (for 400 responses)
export interface ValidationErrors {
  [field: string]: string[];
}
```

### Why two types for articles (`ArticleSummary` vs `Article`)?

The list endpoint (`GET /api/articles`) returns many articles but omits the `content` field for performance — you don't need the full body of every article just to render the list. The detail endpoint (`GET /api/articles/:id`) returns the full object including `content`.

Having two separate types makes this explicit. If you try to access `article.content` where you only have an `ArticleSummary`, TypeScript will give you a compile-time error rather than a silent `undefined` at runtime.

### Why is `status` a number in `CreateArticleRequest` but a string in `ArticleSummary`?

The backend accepts numeric status codes (0=Draft, 1=Published, 2=Archived) in POST/PUT request bodies, but returns the string label ("Draft", "Published", "Archived") in GET response bodies. This is a backend design choice. The frontend adapts to both sides: `ArticleForm` stores status as a number, converts from the string when loading initial data, and sends the number to the service.

---

## 10. Services / API Layer: Talking to the Backend

**File:** `frontend/src/services/articleService.ts`

### Why have a separate service layer?

Without a service layer, you would have `axios.get('/api/articles/...')` calls scattered through every component. This creates several problems:
- If the API URL changes, you have to find and update every component.
- Components become harder to test because they directly depend on network calls.
- You cannot reuse the same fetch logic from different components.

The service layer centralizes all HTTP calls in one place. Components and hooks import from `articleService` — they never touch `axios` directly.

### The Axios instance

```ts
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5018',
  headers: { 'Content-Type': 'application/json' },
});
```

`axios.create()` creates a pre-configured Axios instance. `baseURL` means you write `'/api/articles'` instead of `'http://localhost:5018/api/articles'` in every call. The `Content-Type` header is set automatically on every request so you never forget it.

`import.meta.env.VITE_API_URL` is a Vite environment variable. During development this is typically unset, so the fallback `http://localhost:5018` is used. In production (after deployment), you set `VITE_API_URL` in your hosting environment to point at the real backend server.

> **Important:** Vite only exposes environment variables that start with `VITE_` to browser code (for security). See `.env.example` for the expected variables.

### The Vite dev server proxy

```ts
// vite.config.ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5018',
      changeOrigin: true,
    },
  },
},
```

During development, the Vite dev server runs on port 5173 and the .NET backend runs on port 5018. Normally, a browser would refuse to let JavaScript on `localhost:5173` call `localhost:5018` — this is a **CORS** (Cross-Origin Resource Sharing) security restriction.

The Vite proxy solves this: any request from the frontend to `/api/*` is *forwarded* by the Vite server to `http://localhost:5018/api/*`. From the browser's perspective, both the frontend and the API live on port 5173, so no CORS issue.

> **Why this matters:** In production, the frontend is typically served from the same domain as the backend (or CORS headers are set on the backend). The proxy is only a development convenience.

### All service methods

```ts
export const articleService = {
  // GET /api/articles?search=...&category=...&page=...
  async list(filters: ArticleFilters = {}): Promise<ArticleListResponse> { ... },

  // GET /api/articles/:id
  async getById(id: number): Promise<Article> { ... },

  // GET /api/articles/slug/:slug
  async getBySlug(slug: string): Promise<Article> { ... },

  // POST /api/articles
  async create(request: CreateArticleRequest): Promise<Article> { ... },

  // PUT /api/articles/:id
  async update(id: number, request: UpdateArticleRequest): Promise<Article> { ... },

  // DELETE /api/articles/:id
  async delete(id: number): Promise<void> { ... },

  // GET /api/categories
  async getCategories(): Promise<string[]> { ... },

  // GET /api/tags
  async getTags(): Promise<string[]> { ... },
};
```

All methods are `async` and return Promises. Callers `await` them or use `.then()`.

### Error handling utility

```ts
export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const detail = error.response?.data;
    if (detail?.title) return detail.title;       // ASP.NET ProblemDetails format
    if (typeof detail === 'string') return detail;
    return error.message;                          // network error etc.
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred.';
}
```

Axios throws an `AxiosError` when the server returns a 4xx or 5xx status. This function extracts a human-readable message regardless of how the error arrived. It handles the ASP.NET ProblemDetails format (`{ title: "...", status: 400, ... }`), plain string responses, and generic Error objects.

> **Note:** `getErrorMessage` is defined but not consistently used throughout the codebase. Some components catch errors and read `error.message` directly. A future improvement would be to replace those with `getErrorMessage` for better error handling across the board.

---

## 11. Custom Hooks: Reusable Data-Fetching Logic

Custom hooks extract stateful logic from components so it can be reused. They are just functions whose names start with `use`.

### `useArticles`

**File:** `frontend/src/hooks/useArticles.ts`

```ts
export function useArticles(filters: ArticleFilters = {}): UseArticlesResult {
  const [data, setData] = useState<ArticleListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const key = JSON.stringify(filters);

  const fetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await articleService.list(filters);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load articles.');
    } finally {
      setLoading(false);
    }
  }, [key]);

  useEffect(() => { fetch(); }, [fetch]);

  return { data, loading, error, refetch: fetch };
}
```

**How the re-fetch trigger works:**

React's `useCallback` / `useEffect` dependency arrays only re-run when a dependency reference changes. Objects (`filters`) are compared by reference, not by value — so even if you pass `{ page: 1 }` on every render, each render creates a *new object* and `useCallback` would be called every time (infinite loop risk).

The trick: `JSON.stringify(filters)` produces a *string* that is the same when the filter values are the same. The `useCallback` is recreated only when the serialized string changes. This is a common pattern for object dependencies.

> **Alternative:** [React Query](https://tanstack.com/query) / [TanStack Query](https://tanstack.com/query) handles all of this automatically — caching, re-fetch-on-focus, stale time, etc. The custom hook approach is simpler to understand but requires you to handle edge cases yourself.

**Return values:**
- `data`: the `ArticleListResponse` when loaded, `null` while loading
- `loading`: `true` while the request is in flight
- `error`: an error message string if the request failed, `null` otherwise
- `refetch`: a function you can call to manually re-trigger the fetch (e.g., after a delete)

---

### `useArticle` and `useArticleBySlug`

**File:** `frontend/src/hooks/useArticle.ts`

```ts
export function useArticle(id: number): UseArticleResult {
  const [article, setArticle] = useState<Article | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    articleService
      .getById(id)
      .then(setArticle)
      .catch(() => setError('Article not found.'))
      .finally(() => setLoading(false));
  }, [id]);        // re-fetch when id changes

  return { article, loading, error };
}
```

`useArticleBySlug` is identical but calls `articleService.getBySlug(slug)` instead.

These hooks are simpler than `useArticles` because there are no complex dependencies to serialize — just a single primitive (`id` or `slug`).

---

## 12. State Handling: How the App Remembers Things

### The approach: local component state only

This app uses **no global state manager** (no Redux, no Zustand, no MobX, no Recoil). All state is local: stored in the component that needs it, passed down as props to children.

The state lives in three places:

| Where | What | Mechanism |
|---|---|---|
| Page components | Filters, pagination, fetch results, form data | `useState` |
| Custom hooks | Fetch results, loading/error flags | `useState` inside the hook |
| No global store | — | — |

### Why no Redux or Zustand?

**Redux** is a pattern (and library) for a single centralized store of application state. It is powerful but adds significant boilerplate and complexity. It pays off when:
- Many unrelated components need the same data
- The state has complex update logic with many actions
- You need time-travel debugging or a strict unidirectional data flow

**Zustand** is a much lighter alternative to Redux that avoids a lot of the boilerplate.

In this app, neither is needed because:
- Most state is local to a single page (the article list is only needed on `ArticlesPage`; the article detail is only needed on `ArticleDetailPage`).
- There is no "shopping cart" or "authenticated user" data that needs to be read from many places simultaneously.
- Data is fetched fresh per page. There is no caching layer that multiple components share.

> **When would you add Zustand here?** If you added user authentication (the logged-in user needs to be readable everywhere), or if you wanted to cache article data so navigating back to the list doesn't re-fetch. Those are real future needs, but the current scope doesn't require it.

### Props drilling

When a parent has state that a child needs, the parent passes it as props. For example, `ArticlesPage` owns the filter state and passes it down to `FilterControls`:

```tsx
<FilterControls
  categories={categories}
  selectedCategory={category}
  onCategoryChange={(v) => { setCategory(v); setPage(1); }}
  // ...
/>
```

The downside of props drilling is that adding a new level to the component tree means threading props through every intermediate level. In this app the nesting is shallow (page → component), so it is fine.

---

## 13. Forms and Validation

### Controlled forms

This app uses **controlled forms**: every `<input>`, `<textarea>`, and `<select>` has its `value` bound to a React state variable, and an `onChange` handler that updates that state.

```tsx
const [form, setForm] = useState<FormState>({ title: '', ... });

function set(field: keyof FormState) {
  return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
    setErrors((prev) => ({ ...prev, [field]: undefined })); // clear field error on change
  };
}

// In JSX:
<input
  value={form.title}
  onChange={set('title')}
/>
```

The helper function `set(field)` returns an event handler that updates a single field in the form state. This avoids writing a separate handler for each field.

### Why controlled forms?

With controlled forms, React is the single source of truth. You can:
- Validate the value on every keystroke (or on blur, or on submit)
- Programmatically reset the form (`setForm(initialState)`)
- Derive computed values from form state
- Pre-populate the form with existing data (as `EditArticlePage` does)

**Uncontrolled forms** (where you use `ref` to read the DOM's current value instead of managing it in state) are simpler to write but harder to validate and harder to pre-fill.

### Validation

```ts
function validate(): boolean {
  const next: Partial<Record<keyof FormState, string>> = {};
  if (!form.title.trim()) next.title = 'Title is required.';
  if (!form.summary.trim()) next.summary = 'Summary is required.';
  if (!form.content.trim()) next.content = 'Content is required.';
  if (!form.category.trim()) next.category = 'Category is required.';
  if (form.slug && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.slug)) {
    next.slug = 'Slug must be lowercase alphanumeric with hyphens only.';
  }
  setErrors(next);
  return Object.keys(next).length === 0;
}
```

Validation runs when the form is submitted (`e.preventDefault()` stops the browser default). If any field fails, its error message is stored in `errors` state and displayed below the input. Validation does not run on every keystroke (only when you try to submit). Each field's error is cleared as soon as the user changes that field's value.

**Slug regex breakdown:** `/^[a-z0-9]+(?:-[a-z0-9]+)*$/`
- `^` — start of string
- `[a-z0-9]+` — one or more lowercase letters or digits
- `(?:-[a-z0-9]+)*` — zero or more hyphen-followed-by-alphanumeric groups
- `$` — end of string
- Examples that match: `my-article`, `article123`, `foo`
- Examples that don't match: `My Article`, `foo--bar`, `-start`, `end-`

### Tags as comma-separated string

The tags input is a plain text field where the user types `"react, typescript, tutorial"`. On submit, this is split:

```ts
const tags = form.tags
  .split(',')
  .map((t) => t.trim().toLowerCase())
  .filter(Boolean);   // remove empty strings from trailing commas
```

This results in `["react", "typescript", "tutorial"]` — the array the API expects.

### Alternative: form libraries

**React Hook Form** and **Formik** are popular libraries for managing form state. They reduce boilerplate and handle performance optimizations (like only re-rendering the changed field). They also integrate well with schema validators like **Zod** or **Yup**.

For a form with 7 fields, the manual approach used here is perfectly reasonable. As the form grows in complexity or as more forms are added, migrating to React Hook Form + Zod would be a good investment.

---

## 14. Loading, Error, and Empty States

Every data-dependent component must handle three states: loading, error, and empty. Ignoring any of them creates a broken user experience.

### The three components

From `frontend/src/components/StateDisplay.tsx`:

**`LoadingSpinner`** — while data is in flight:
```tsx
<div className="state-container">
  <div className="spinner" aria-label="Loading" />
  <p className="state-message">Loading…</p>
</div>
```
The `.spinner` class uses a CSS `@keyframes` animation to create a rotating ring.

**`ErrorMessage`** — when the request fails:
```tsx
<div className="state-container state-error">
  <span className="state-icon">⚠️</span>
  <p className="state-message">Something went wrong.</p>
</div>
```

**`EmptyState`** — when the request succeeds but returns no data:
```tsx
<div className="state-container state-empty">
  <span className="state-icon">📭</span>
  <p className="state-message">No articles found.</p>
</div>
```

### How pages use them

**Early-return pattern** (good for full-screen states):
```tsx
if (loading) return <LoadingSpinner />;
if (error || !article) return <ErrorMessage message={error ?? 'Article not found.'} />;
// now render the happy path
```

**Inline conditional pattern** (good when only part of the page depends on the data):
```tsx
{loading && <LoadingSpinner />}
{error && <ErrorMessage message={error} />}
{!loading && !error && data?.items.length === 0 && (
  <EmptyState message="No articles match your search." />
)}
{!loading && !error && data && data.items.length > 0 && (
  <div className="article-grid">
    {data.items.map((a) => <ArticleCard key={a.id} article={a} />)}
  </div>
)}
```

> **Common beginner confusion:** The `key` prop on list items (`key={a.id}`) is required by React when rendering arrays. React uses it to track which item is which across re-renders. Without it, React may re-use DOM nodes incorrectly, leading to subtle bugs. Always use a stable, unique identifier (like an ID from the database) — never the array index unless the list is static.

---

## 15. How Data Flows from API to Rendered UI

This is the end-to-end journey of a piece of data — from the backend database to pixels on screen.

### Example: ArticlesPage loads and displays articles

```
User navigates to /articles
  │
  ├─ App.tsx matches route → renders <ArticlesPage />
  │
  ├─ ArticlesPage renders → initializes state: loading=true, data=null
  │
  ├─ useArticles({ page: 1, pageSize: 12 }) hook is called
  │   └─ useEffect fires (on mount)
  │       └─ articleService.list({ page: 1, pageSize: 12 })
  │           └─ axios.get('http://localhost:5018/api/articles?page=1&pageSize=12')
  │               └─ HTTP GET request leaves the browser
  │                   └─ .NET backend queries the database
  │                       └─ returns JSON:
  │                           {
  │                             "items": [...],
  │                             "totalCount": 45,
  │                             "page": 1,
  │                             "pageSize": 12,
  │                             "totalPages": 4
  │                           }
  │
  ├─ Axios parses JSON automatically → Promise resolves with ArticleListResponse
  │
  ├─ useArticles sets data=response, loading=false
  │
  ├─ ArticlesPage re-renders with data
  │
  ├─ data.items.map(article => <ArticleCard key={article.id} article={article} />)
  │
  └─ Browser renders 12 article cards on screen ✓
```

### When filters change

```
User selects category "Engineering" from dropdown
  │
  ├─ ArticlesPage: setCategory('Engineering'), setPage(1)
  │
  ├─ useArticles receives new filters object
  │   └─ JSON.stringify changes → useCallback recreates fetch function
  │       └─ useEffect fires again
  │           └─ articleService.list({ category: 'Engineering', page: 1, pageSize: 12 })
  │               └─ HTTP GET /api/articles?category=Engineering&page=1&pageSize=12
  │
  ├─ loading=true briefly → LoadingSpinner shown
  │
  └─ New results arrive → re-render with filtered articles
```

### Data transformation points

Data is not completely unchanged as it flows through the system:

| Step | Transformation |
|---|---|
| Backend → Axios | JSON string → JavaScript object (done by Axios automatically) |
| `ArticleListResponse.items` | `ArticleSummary[]` (missing `content`) |
| `ArticleForm` initial load | `article.status` string `"Draft"` → number `0` via `toStatusNumber()` |
| `ArticleForm` submit | `form.tags` string `"a, b"` → `["a", "b"]` array |
| `ArticleForm` submit | `form.status` string `"0"` → `Number(form.status)` = `0` |
| Display | `article.createdAt` ISO string → `"Mar 15, 2025"` via `formatDate()` |
| Display | `article.status` string → CSS class via `statusClass()` |

---

## 16. Styling Approach

### Vanilla CSS with CSS Custom Properties

The app uses plain CSS — no Tailwind, no CSS Modules, no styled-components. All styles live in two files:
- `frontend/src/index.css` — the main stylesheet (global reset, CSS variables, all component styles)
- `frontend/src/App.css` — legacy file from the Vite template, mostly unused

### CSS Custom Properties (CSS Variables)

The `:root` block defines a design token system:

```css
:root {
  /* Colors */
  --color-bg: #f8f9fa;
  --color-surface: #ffffff;
  --color-border: #e2e8f0;
  --color-text: #1a202c;
  --color-text-muted: #718096;
  --color-primary: #3b82f6;
  --color-primary-dark: #2563eb;
  --color-secondary: #64748b;
  --color-danger: #ef4444;
  --color-danger-dark: #dc2626;
  --color-success: #10b981;
  --color-warning: #f59e0b;
  /* Status badge colors */
  --color-draft: #64748b;
  --color-published: #10b981;
  --color-archived: #94a3b8;
  /* Sizing */
  --radius: 6px;
  --shadow: 0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.06);
  --shadow-md: 0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -2px rgba(0,0,0,.1);
  /* Typography */
  --font-sans: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-mono: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
}
```

These variables are used throughout the stylesheet: `background: var(--color-bg)`, `color: var(--color-primary)`, etc. This makes it easy to change the colour scheme in one place.

### CSS class naming conventions

Classes follow a loose **BEM-like** (Block, Element, Modifier) naming convention:

- `.article-card` — the block (the card component)
- `.article-card-title` — an element (the title within a card)
- `.btn-primary` — a modifier (a variant of the button)
- `.form-input--error` — a modifier with double-dash (error state for an input)

This is not strictly BEM, but it follows the same spirit: descriptive, hierarchical, no global conflicts.

### Layout

The main content is centred and max-width constrained:

```css
.main-content {
  max-width: 1100px;
  margin: 0 auto;
  padding: 1.5rem;
}
```

Article grids use CSS Grid:

```css
.article-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.25rem;
}
```

`auto-fill` + `minmax` gives responsive columns without media queries: as the viewport narrows, fewer columns fit and the grid reflows automatically.

### Responsive design

Media queries handle smaller screens:

```css
@media (max-width: 768px) {
  .hero-title { font-size: 1.75rem; }
  .article-grid { grid-template-columns: 1fr; }
  .toolbar { flex-direction: column; align-items: stretch; }
}

@media (max-width: 560px) {
  .form-row { grid-template-columns: 1fr; }
}
```

### Why vanilla CSS instead of Tailwind?

**Tailwind** would make styling faster by eliminating the CSS file entirely — utility classes (`bg-white text-blue-500 px-4 py-2`) go directly in JSX. The tradeoff is that JSX becomes cluttered with long class strings and you need to learn Tailwind's class names. For a project this size, vanilla CSS is readable and maintainable.

**CSS Modules** would scope class names to each component, preventing accidental name collisions. The tradeoff is a more complex import pattern. With careful naming conventions (as used here), collisions are not a problem in practice for this codebase.

---

## 17. Why the Project Is Organized This Way

### The organizing principle: separate concerns by responsibility

The folder structure asks a question: "What does this file *do* in the architecture?"

- **`pages/`**: This file *is a screen*. It owns data fetching and layout for one URL.
- **`components/`**: This file *is a reusable UI piece*. It receives props, renders HTML, emits events.
- **`hooks/`**: This file *manages stateful asynchronous logic*. It knows how to fetch data and track its status.
- **`services/`**: This file *talks to the network*. It knows the API URLs and shapes.
- **`types/`**: This file *defines data shapes*. No logic, no side effects.
- **`utils/`**: This file *is a pure function*. No React, no side effects.

### Why not organize by feature?

A **feature-based** (or "domain-driven") structure groups everything related to a feature together:

```
src/
  articles/
    ArticlesPage.tsx
    ArticleCard.tsx
    useArticles.ts
    articleService.ts
    types.ts
```

This scales better when the application grows large and teams own different domains. In this codebase, all features are about articles, so the feature-based and layer-based structures converge. The current layer-based structure is standard for small to medium React apps.

### Why separate pages from components?

Pages and components are both React components. The distinction is conceptual:
- A **page** is owned by a route. It knows about navigation (`useNavigate`, `useParams`). It is the place where data fetching is initiated. Ideally it is not reused across routes.
- A **component** is reusable. It does not know which route it is on. It gets its data entirely from props.

This separation makes it easy to see at a glance "what screens does this app have?" (look at `pages/`) and "what reusable pieces exist?" (look at `components/`).

---

## 18. Alternative Approaches and Tradeoffs

### React local state vs Redux / Zustand

| Approach | Best when | Downside |
|---|---|---|
| Local state (`useState`) | State belongs to one component or a shallow tree | Props drilling becomes painful at depth |
| Redux | Complex shared state, many consumers, strict update patterns | Boilerplate-heavy; overkill for simple apps |
| Zustand | Shared state with minimal boilerplate | Adds a dependency; global state can be harder to trace |
| React Context + `useReducer` | App-wide but infrequently updated state (theme, auth user) | Re-renders all consumers when context value changes |

**In this project:** Local state is appropriate. If authentication or cross-page caching is added, Zustand would be a clean first step.

---

### `fetch` vs Axios

| | `fetch` (built-in) | Axios |
|---|---|---|
| JSON parsing | Manual (`res.json()`) | Automatic |
| Error on 4xx/5xx | **No** — only network errors throw | **Yes** — throws on any non-2xx status |
| Request cancellation | `AbortController` | `CancelToken` or `AbortController` |
| Request/response interceptors | No built-in | Yes |
| Bundle size | 0 KB (built in) | ~13 KB minified |

The key difference: with `fetch`, a 404 response does **not** throw an error — you have to check `res.ok` manually. With Axios, it throws automatically. This makes error handling simpler with Axios.

---

### Vite vs older build tools (Create React App / Webpack)

| | Vite | Create React App (CRA) / Webpack |
|---|---|---|
| Dev server startup | Near-instant (native ESM) | Slow (full bundle rebuild) |
| HMR (hot reload) | Very fast | Slower |
| Config | Minimal | Highly configurable but complex |
| Build output | Rollup (fast, small) | Webpack (mature, more plugins) |
| Status | Actively developed | CRA is deprecated |

CRA was the standard way to start a React project from 2016–2022. It used Webpack internally and is now effectively deprecated. Vite is the modern replacement — it does not bundle code during development (it serves ES modules directly to the browser), which is why startup is so fast.

---

### Page-based organization vs component-first organization

| | Page-based (this project) | Component-first (e.g., Storybook-driven) |
|---|---|---|
| Good for | App-centric thinking, small–medium apps | Design systems, large component libraries |
| Navigation | Easy to see all screens | May require digging to find which page uses what |
| Reusability focus | Separate `components/` folder | Every component designed for isolation first |

---

### Controlled forms vs React Hook Form / Formik

| | Manual controlled forms (this project) | React Hook Form | Formik |
|---|---|---|---|
| Setup | None | `npm install react-hook-form` | `npm install formik` |
| Bundle size | 0 KB | ~9 KB | ~15 KB |
| Performance | Re-renders on every keystroke | Minimizes re-renders | Re-renders on most changes |
| Validation | Manual functions | `register` + `resolver` (Zod, Yup) | Yup schema |
| Learning curve | Familiar React patterns | New API to learn | New API to learn |

For 7 fields, manual controlled forms are clear and maintainable. React Hook Form would shine when the form has 20+ fields, complex conditional visibility, or needs maximum performance.

---

## 19. What to Study Next

To deepen your understanding of this frontend, explore these topics in roughly this order:

### React fundamentals
- **Official React docs**: https://react.dev — especially the "Thinking in React" guide
- `useState`, `useEffect`, `useCallback`, `useMemo` — know when and why to use each
- The React DevTools browser extension — inspect component state and props interactively

### TypeScript
- TypeScript Handbook: https://www.typescriptlang.org/docs/handbook/intro.html
- Focus on: interfaces, union types, generics, optional properties, `Partial<T>`, `Record<K,V>`

### React Router
- React Router v6/v7 docs: https://reactrouter.com/home
- Understand `useNavigate`, `useParams`, `useLocation`, nested routes

### Vite
- Vite docs: https://vite.dev/guide/
- Understand the dev server, env variables, the build output, and the proxy

### Axios
- Axios docs: https://axios-http.com/docs/intro
- Understand interceptors, error handling, cancellation

### Related documentation in this project
- **Backend API** (future doc: `docs/02_BACKEND_DEEP_DIVE.md`) — explains what the API endpoints accept and return, how the .NET backend validates requests, and how the database is structured
- **Project setup and running locally** (see `README.md` in the repo root and `frontend/README.md`) — environment variables, running the dev server, running the backend

### Concepts to explore when the app grows
- **React Query / TanStack Query**: https://tanstack.com/query — replaces custom hooks with a full data-fetching and caching layer
- **Zustand**: https://zustand-demo.pmnd.rs/ — lightweight global state if you add auth
- **React Hook Form + Zod**: https://react-hook-form.com — as forms grow more complex
- **Tailwind CSS**: https://tailwindcss.com — utility-first styling alternative
- **Vitest + React Testing Library**: testing React components in this Vite-based setup

---

## Quick Reference

### File locations at a glance

| Question | Where to look |
|---|---|
| All API calls | `frontend/src/services/articleService.ts` |
| All TypeScript types | `frontend/src/types/index.ts` |
| All routes | `frontend/src/App.tsx` |
| The article form | `frontend/src/components/ArticleForm.tsx` |
| Data-fetching hooks | `frontend/src/hooks/` |
| Global styles + CSS variables | `frontend/src/index.css` |
| Utility functions (date format, status class) | `frontend/src/utils/format.ts` |
| Environment variable config | `frontend/.env.example` |
| Dev server proxy | `frontend/vite.config.ts` |

### Key commands

```bash
cd frontend
npm install        # install dependencies
npm run dev        # start dev server at http://localhost:5173
npm run build      # TypeScript compile + Vite production build → dist/
npm run lint       # run ESLint
npm run preview    # serve the dist/ folder locally (simulates production)
```

### Environment variables

```bash
# frontend/.env.example
VITE_API_URL=http://localhost:5018   # backend API base URL
```

During development, if `VITE_API_URL` is not set, the service defaults to `http://localhost:5018` and the Vite proxy handles CORS.
