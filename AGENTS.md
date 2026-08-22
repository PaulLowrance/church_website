# AGENTS.md

## Project Overview

Church website for Brentwood Hills Primitive Baptist Church (bhpbc.org), an Independent Primitive Baptist church located in East Fort Worth, Texas, near the Brentwood Hills neighborhood. Replaces an aging Django site. Provides static page management, sermon hosting, and podcast RSS feed.

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
- **Orchestration:** .NET Aspire AppHost (`server/ChurchWebsite.AppHost`) in dev; **`Aspire.Hosting.JavaScript`** → `AddViteApp` (Node hosting; the older `AddNodeApp` API is gone)
- **Dev Proxy:** Vite forwards `/api` and `/uploads` → API. Under Aspire the target is injected via `VITE_API_PROXY_TARGET` (falls back to `http://localhost:5001` standalone)

## Development Workflow

1. **Branching:** Create feature branches from `main`. Do NOT push directly to `main`.
2. **Commits:** Make focused commits with conventional commit style messages.
3. **Pull Requests:** Open PRs on GitHub for all feature work.
4. **Testing:** Smoke test via curl before opening PRs.

## Running Locally

### Preferred: .NET Aspire AppHost (launches everything)

```bash
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
cd server/ChurchWebsite.AppHost
dotnet run
```

> The `SSL_CERT_DIR` export is required on Linux so OpenSSL-based clients trust the Aspire dev certificate. Without it, the dashboard logs `UntrustedRoot` errors while watching interactions ("The SSL connection could not be established... UntrustedRoot") **and the API loses the system CA bundle** — outbound HTTPS to external services (e.g. AssemblyAI transcript uploads) fails with `PartialChain` (`AuthenticationException: The remote certificate is invalid because of errors in the certificate chain`). Add the export to your shell profile to avoid repeating it.

Launches PostgreSQL container, API, and Vue dev server together. Open the Aspire dashboard (URL + login token printed in the log) to see resource status, logs, and the assigned ports. The Vite proxy target and DB connection string are injected automatically via `WithEnvironment`/`WithReference`, so no manual port wiring is needed.

### Manual / standalone (still supported)

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

> Do not run both workflows at once — the AppHost creates its own anonymous Postgres container on a random port, which conflicts with the compose container.

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
- `SummaryStatus` (`none | processing | completed | error`) — summary runs independently of transcription; a failed summary doesn't flip transcript status
- `SummaryError` (nullable)
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
| POST | `/api/podcast/episodes/{id}/retry-transcription` | Admin | Re-submit failed transcription (new AssemblyAI job, new cost) |
| POST | `/api/podcast/episodes/{id}/retry-summary` | Admin | Re-run LLM summary from saved transcript (new cost) |
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
  2. Marks transcript status `completed` (or `error` with a message).
  3. Generates a summary **only if description is empty**, tracking `summary_status` (`none | processing | completed | error`) independently — a failed summary leaves transcript status `completed`.
- Transcript is exposed on the public podcast page as viewable + downloadable text. The RSS feed references it **only as a download link** in the item description — never inline.
- **Retries are independent admin actions, each with monetary cost:**
  - `POST /api/podcast/episodes/{id}/retry-transcription` — re-submits the audio (new AssemblyAI job) only when `transcript_status = error`.
  - `POST /api/podcast/episodes/{id}/retry-summary` — re-runs the LLM summary from the saved `.txt` only when transcript is `completed`. A 400 keeps `summary_status = error` with the persisted `summary_error`.
- Summary generation uses the **LLM Gateway** API (the legacy `summarization`/`summary_type` transcript params are deprecated). See `AssemblyAITranscriptionService`.
- Auth header is the raw AssemblyAI key with **no `Bearer` prefix** (both STT and LLM Gateway). `/v2/upload` takes **raw binary**, not multipart.
- No official .NET SDK exists → raw HTTP via `HttpClient` (named client `"AssemblyAI"` registered with `AddHttpClient`).
- **Never commit `AssemblyAI:ApiKey`.** Supply via env var (`AssemblyAI__ApiKey`) or user-secrets.

#### Storing the AssemblyAI key locally (dev)
```bash
# Init user-secrets for the API project (adds UserSecretsId to the csproj)
dotnet user-secrets init --project server/src/ChurchWebsite.Api

# Save the key
dotnet user-secrets set "AssemblyAI:ApiKey" "<your-key>" --project server/src/ChurchWebsite.Api
```
The key is stored in `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` and is never written to appsettings or git. It loads in both standalone `dotnet run` and under the Aspire AppHost. Verify: `dotnet user-secrets list --project server/src/ChurchWebsite.Api`. **Do not commit the appsettings files with a real key** — keep `"ApiKey": ""` in `appsettings.json` / `appsettings.Development.json`.

#### Storing the AssemblyAI key in production (deploy)
Supply it as an environment variable; the `__` separator maps to the config key:
```bash
export AssemblyAI__ApiKey="<your-key>"
```
When deploying (e.g. a systemd unit, container, or platform env config), set `AssemblyAI__ApiKey` on the API process. This overrides both appsettings files and user-secrets, so no key file ships to the server. If the key is ever pushed in a commit, rotate it at AssemblyAI — git history won't hide it.

## Decision Log

1. **Custom JWT (not Identity):** Faster bootstrap, Dapper-friendly. Using `System.IdentityModel.Tokens.Jwt` + `BCrypt.Net-Next`.
2. **In-code migrations (not EF Migrations):** Consistent with Dapper approach. `DbInitializer` checks column existence before `ALTER TABLE`.
3. **Vite proxy (not CORS):** Avoids CORS issues during local development. Proxy config in `vite.config.ts`.
4. **FastEndpoints v8:** Response methods accessed via `Send.OkAsync()`, `Send.NotFoundAsync()`, etc. (not `SendAsync()`).
5. **No docker compose for app yet:** Deferred per user request. Only PostgreSQL container in `docker-compose.yml`.
6. **AssemblyAI polling (not webhook):** `BackgroundService` polls `GET /v2/transcript/{id}`. No public URL needed, survives restarts, state persisted in DB.
7. **LLM Gateway for summaries (not `summarization` param):** The transcript-level `summarization`/`summary_type`/`auto_chapters` params are deprecated. Summaries come from a second call to the LLM Gateway chat-completions API after transcription completes.
8. **Raw HTTP (not SDK):** AssemblyAI ships Python/Node SDKs only; no .NET SDK, so the API is called directly via `HttpClient`.
9. **Aspire AppHost for dev (not just compose):** `dotnet run` in the AppHost launches Postgres + API + Vue together. Uses `Aspire.Hosting.PostgreSQL` (postgres + database resources) and `Aspire.Hosting.JavaScript` (`AddViteApp` for the Vue dev server, since `AddNodeApp` is gone in 13.x). Vite proxy target and DB connection string are injected via `WithEnvironment`/`WithReference`. Compose + manual `dotnet run`/`npm run dev` remains as a standalone fallback.

## Common Gotchas

- **Port mismatch:** `launchSettings.json` must match Vite proxy target (both port 5001). Was previously 5006.
- **Aspire runs on random ports:** The AppHost assigns random ports to the API and Vite dev server. Don't hardcode these — the Vite proxy target comes from `VITE_API_PROXY_TARGET` (Aspire-injected) and the API port comes from Aspire's `AddProject`. The old fixed 5001/5173 only apply to the standalone workflow.
- **AddViteApp working dir:** `AddViteApp("frontend", "../../app")` is relative to the AppHost project dir, so it resolves to the repo-root `app/`.
- **Class libraries aren't resources:** Only the executable API project gets `AddProject`. Don't add Core/Infrastructure as resources (they aren't runnable and produce ASPIRE004 warnings).
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
