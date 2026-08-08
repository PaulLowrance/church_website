# AGENTS.md

## Project Overview

Church website for Brentwood Hills Primitive Baptist Church (bhpbc.org). Replaces an aging Django site. Provides static page management, sermon hosting, and podcast RSS feed.

## Repository Structure

```
church_website/
├── first_prompt.md          # Original project requirements
├── docker-compose.yml       # PostgreSQL container only (MVP)
├── server/                  # .NET 10 backend
│   ├── ChurchWebsite.slnx
│   └── src/
│       ├── ChurchWebsite.Core/          # Entities, interfaces
│       ├── ChurchWebsite.Infrastructure/ # Dapper, DB, Auth, Repositories
│       └── ChurchWebsite.Api/           # FastEndpoints, Program.cs
└── app/                     # Vue 3 frontend
    ├── src/
    │   ├── views/             # Page views
    │   ├── components/        # Shared components (NavMenu, ImageUploadHelper)
    │   ├── stores/            # Pinia stores (auth)
    │   ├── router/            # Vue Router
    │   └── api/               # Axios client
    └── vite.config.ts
```

## Tech Stack

- **Backend:** .NET 10, FastEndpoints v8, Dapper, PostgreSQL, JWT authentication, BCrypt
- **Frontend:** Vue 3 (Composition API), TypeScript, Vite, Quasar v2, Pinia, Axios, `marked` for Markdown rendering
- **Database:** PostgreSQL 16 (Docker container)
- **Dev Proxy:** Vite forwards `/api` and `/uploads` → `http://localhost:5001`

## Development Workflow

1. **Branching:** Create feature branches from `main`. Do NOT push directly to `main`.
2. **Commits:** Make focused commits with conventional commit style messages.
3. **Pull Requests:** Open PRs on GitHub for all feature work.
4. **Testing:** Smoke test via curl before opening PRs.

## Running Locally

```bash
# Start database
docker compose up -d

# Backend
cd server/src/ChurchWebsite.Api
dotnet run
# (uses launchSettings.json → http://localhost:5001)

# Frontend (separate terminal)
cd app
npm run dev
# (uses Vite proxy to backend on port 5001)
```

## Authentication

- Superadmin seeded on startup: `admin` / `testing123`
- JWT token returned from `POST /api/auth/login`
- Token stored in `localStorage` and attached via Axios interceptor
- Role-based: `Admin` role required for all admin endpoints
- `User.Identity.IsAuthenticated` checked in `GetPageEndpoint` for unpublished pages

## Domain Model

### User
- `Id` (UUID)
- `Username`, `PasswordHash` (BCrypt)
- `Role` (Admin)
- `CreatedAt`

### Page
- `Id` (UUID)
- `Slug` (URL-friendly, unique, auto-generated from title)
- `Title`, `Body`
- `IsMarkdown` (bool)
- `IsPublished` (bool, default true)
- `ShowInNav` (bool, default true)
- `NavTitle` (string, max 25 chars, defaults to title)
- `UpdatedAt`

### PodcastEpisode
- `Id` (UUID)
- `Title`, `SpeakerName`, `Description` (nullable), `SeriesName` (nullable)
- `AudioFilePath`, `AudioFileName`, `AudioFileSize`, `AudioContentType`
- `PublishedAt`, `CreatedAt`, `UpdatedAt`
- `TranscriptStatus` (`none | queued | processing | completed | error`)
- `TranscriptFilePath` (path to generated `.txt`)
- `AssemblyAiTranscriptId` (AssemblyAI job id for polling)
- `TranscriptError` (nullable)
- `Tags` (many-to-many via `episode_tags`)

## API Endpoints

| Method | Path | Access | Purpose |
|--------|------|--------|---------|
| POST | `/api/auth/login` | Anonymous | Login, returns JWT |
| GET | `/api/pages` | Admin | List all pages |
| GET | `/api/pages/nav` | Anonymous | List published nav pages |
| GET | `/api/pages/{slug}` | Anonymous | Get single page (404 if unpublished + anon) |
| POST | `/api/pages` | Admin | Create page |
| PUT | `/api/pages/{slug}` | Admin | Update page |
| DELETE | `/api/pages/{slug}` | Admin | Delete page |
| POST | `/api/images` | Admin | Upload image (returns public URL) |
| GET | `/api/podcast/episodes` | Anonymous | List published episodes |
| GET | `/api/podcast/episodes/{id}` | Anonymous | Get single episode |
| POST | `/api/podcast/episodes` | Admin | Create episode (triggers transcription) |
| PUT | `/api/podcast/episodes/{id}` | Admin | Update episode (re-transcribes if audio replaced) |
| DELETE | `/api/podcast/episodes/{id}` | Admin | Delete episode |
| GET | `/podcast/rss` | Anonymous | Podcast RSS feed |
| GET | `/api/admin/dashboard` | Admin | Admin dashboard stub |

## Frontend Routes

| Path | Component | Access |
|------|-----------|--------|
| `/` | HomeView | Public (dynamic slug: home) |
| `/:slug` | HomeView | Public (any page slug) |
| `/login` | LoginView | Anonymous |
| `/admin` | AdminView | Admin (route guard) |
| `/admin/pages/create` | PageCreateView | Admin |
| `/admin/pages/:slug/edit` | PageEditorView | Admin |

## Database Conventions

- Dapper + Npgsql with snake_case column names
- `DefaultTypeMap.MatchNamesWithUnderscores = true` in Program.cs for PascalCase mapping
- Schema and seeding in-code via `DbInitializer` (runs on app startup)
- Migration strategy: `ALTER TABLE ADD COLUMN` checks in `DbInitializer` for schema changes

## Important Patterns

### Slug Generation
```
Lowercase → Remove special chars → Replace spaces with hyphens → Collapse multiple hyphens → Trim edges
```

### Markdown Rendering
- `marked` package converts Markdown → HTML on the frontend
- `v-html` renders the result
- HomeView uses a computed `renderedBody` property
- Images are uploaded via `POST /api/images` and inserted with standard Markdown syntax `![alt](url)` or HTML `<img>`

### Nav Menu
- `GET /api/pages/nav` filters: `ShowInNav = true AND IsPublished = true`
- Desktop: horizontal toolbar nav with active state highlighting
- Mobile: `q-drawer` with hamburger toggle
- Active state uses `bg-white text-dark` for contrast (NOT `text-primary` which clashed with `text-white`)

### Page Visibility Logic
- Unpublished pages: `404` for anonymous users
- Admins can view unpublished pages with an orange banner warning
- `GetPageEndpoint` checks `User.Identity?.IsAuthenticated`

### Sermon Transcription Pipeline (AssemblyAI)
- On episode create (or audio replacement on update), the backend submits the audio to AssemblyAI and stores the transcript id + `transcript_status` (`none | queued | processing | completed | error`) on the episode.
- `TranscriptionProcessingService` (a `BackgroundService` in Infrastructure) polls AssemblyAI every `AssemblyAI:PollIntervalSeconds`. On completion it:
  1. Writes the full transcript to a `.txt` file in `Storage:TranscriptPath` (served at `/uploads/transcripts`).
  2. Sets the AssemblyAI summary as the episode description **only if description is empty**.
  3. Marks status `completed` (or `error` with a message).
- Transcript is exposed on the public podcast page as viewable + downloadable text. The RSS feed references it **only as a download link** in the item description — never inline.
- Summary generation uses the **LLM Gateway** API (the legacy `summarization`/`summary_type` transcript params are deprecated). See `AssemblyAITranscriptionService`.
- Auth header is the raw AssemblyAI key with **no `Bearer` prefix** (both STT and LLM Gateway). `/v2/upload` takes **raw binary**, not multipart.
- No official .NET SDK exists → raw HTTP via `HttpClient` (named client `"AssemblyAI"` registered with `AddHttpClient`).
- **Never commit `AssemblyAI:ApiKey`.** Supply via env var (`AssemblyAI__ApiKey`) or user-secrets.

## Decision Log

1. **Custom JWT (not Identity):** Faster bootstrap, Dapper-friendly. Using `System.IdentityModel.Tokens.Jwt` + `BCrypt.Net-Next`.
2. **In-code migrations (not EF Migrations):** Consistent with Dapper approach. `DbInitializer` checks column existence before `ALTER TABLE`.
3. **Vite proxy (not CORS):** Avoids CORS issues during local development. Proxy config in `vite.config.ts`.
4. **FastEndpoints v8:** Response methods accessed via `Send.OkAsync()`, `Send.NotFoundAsync()`, etc. (not `SendAsync()`).
5. **No docker compose for app yet:** Deferred per user request. Only PostgreSQL container in `docker-compose.yml`.
6. **AssemblyAI polling (not webhook):** `BackgroundService` polls `GET /v2/transcript/{id}`. No public URL needed, survives restarts, state persisted in DB.
7. **LLM Gateway for summaries (not `summarization` param):** The transcript-level `summarization`/`summary_type`/`auto_chapters` params are deprecated. Summaries come from a second call to the LLM Gateway chat-completions API after transcription completes.
8. **Raw HTTP (not SDK):** AssemblyAI ships Python/Node SDKs only; no .NET SDK, so the API is called directly via `HttpClient`.

## Common Gotchas

- **Port mismatch:** `launchSettings.json` must match Vite proxy target (both port 5001). Was previously 5006.
- **QLayout required:** `QPage` components must be descendants of `QLayout`. `App.vue` wraps `<router-view>` in `<q-layout><q-page-container>`.
- **Vue Router dynamic route order:** `/:slug` must be AFTER all explicit routes (`/login`, `/admin`, etc.).
- **Node version:** Requires Node 22+ for latest Vite/Quasar packages.
- **Dapper + snake_case:** Always enable `MatchNamesWithUnderscores` or properties won't map.
- **AssemblyAI docs change often:** Before writing AssemblyAI code, read https://www.assemblyai.com/docs/agent-instructions.md and https://www.assemblyai.com/docs/llms.txt. Don't rely on memorized parameter names.

## Code Style

- C#: file-scoped namespaces, `ImplicitUsings`, nullable reference types enabled
- Vue: Composition API with `<script setup lang="ts">`
- Use Quasar components for UI controls (accessibility built-in)
- Pinia stores for global state (auth)
- Axios interceptors for JWT attachment and 401 handling

## Environment

- .NET 10 SDK
- Node.js 22+
- Docker + Docker Compose
- PostgreSQL 16 (via container)

## Contact / Context

- This project is for Brentwood Hills Primitive Baptist Church (bhpbc.org)
- Replacement of deprecated Django site
- Accessibility (WCAG) is a priority
- Mobile-friendly design required

## Multi-Organization Deployability

The application is designed to be deployed for any church or small organization without code changes. All branding and site-specific values are driven by configuration:

- **Church name:** `Site:ChurchName` in `appsettings.json` (returned by `GET /api/site-info`)
- **Podcast metadata:** `Podcast:Title`, `Podcast:Description`, `Podcast:Author`, `Podcast:BaseUrl` in `appsettings.json`
- **Storage paths:** `Storage:AudioPath`, `Storage:PublicPath`, `Storage:ImagesPath`, `Storage:ImagesPublicPath`, `Storage:TranscriptPath`, and `Storage:TranscriptPublicPath` in `appsettings.json`
- **Transcription:** `AssemblyAI:ApiKey` (secret, env var), `AssemblyAI:BaseUrl`, `AssemblyAI:LlmGatewayUrl`, `AssemblyAI:LlmModel`, `AssemblyAI:SummaryPrompt` in `appsettings.json`
- **Database connection:** `ConnectionStrings:DefaultConnection` in `appsettings.json`

When onboarding a new church, only `appsettings.json` (or environment-specific overrides) need to be updated. No frontend or backend code should be modified for rebranding.
