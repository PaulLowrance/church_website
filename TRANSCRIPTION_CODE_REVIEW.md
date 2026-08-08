# Code Review Plan: AssemblyAI Sermon Transcription & Summarization

Branch: `feature/sermon-transcription-assemblyai`
Reviewer: (another agent — follow this checklist)

## Context

When an admin uploads (or replaces) podcast/sermon audio, the backend submits it to
AssemblyAI for transcription. A background service polls until done, then:

1. Writes the full transcript to a `.txt` file served at `/uploads/transcripts`.
2. Sets the AssemblyAI LLM-Gateway summary as the episode **description only if empty**.
3. Exposes the transcript on the public podcast page (viewable + downloadable).
4. In the RSS feed, references the transcript **only as a download link** — never inline.

Key architectural decisions (from AGENTS.md Decision Log):
- **Polling, not webhook** — `BackgroundService` polls `GET /v2/transcript/{id}`; survives restarts.
- **LLM Gateway for summaries** — `summarization`/`summary_type` transcript params are deprecated.
- **Raw HTTP, not SDK** — AssemblyAI has no official .NET SDK.

## Files to review

### Backend
- `server/src/ChurchWebsite.Core/Entities/PodcastEpisode.cs` (new transcript fields)
- `server/src/ChurchWebsite.Core/Interfaces/ITranscriptionService.cs` (new)
- `server/src/ChurchWebsite.Core/Interfaces/IPodcastEpisodeRepository.cs` (new method)
- `server/src/ChurchWebsite.Core/Interfaces/IFileStorageService.cs` (transcript methods)
- `server/src/ChurchWebsite.Infrastructure/Services/AssemblyAITranscriptionService.cs` (new — AssemblyAI client)
- `server/src/ChurchWebsite.Infrastructure/Services/TranscriptionProcessingService.cs` (new — poller)
- `server/src/ChurchWebsite.Infrastructure/Services/LocalFileStorageService.cs` (transcript storage)
- `server/src/ChurchWebsite.Infrastructure/Repositories/PodcastEpisodeRepository.cs` (new column SQL)
- `server/src/ChurchWebsite.Infrastructure/Data/DbInitializer.cs` (migrations)
- `server/src/ChurchWebsite.Infrastructure/ServiceCollectionExtensions.cs` (DI)
- `server/src/ChurchWebsite.Infrastructure/ChurchWebsite.Infrastructure.csproj` (new packages)
- `server/src/ChurchWebsite.Api/Endpoints/Podcast/CreatePodcastEpisodeEndpoint.cs`
- `server/src/ChurchWebsite.Api/Endpoints/Podcast/UpdatePodcastEpisodeEndpoint.cs`
- `server/src/ChurchWebsite.Api/Endpoints/Podcast/DeletePodcastEpisodeEndpoint.cs`
- `server/src/ChurchWebsite.Api/Endpoints/Podcast/PodcastEpisodeDto.cs`
- `server/src/ChurchWebsite.Api/Endpoints/Podcast/PodcastRssEndpoint.cs`
- `server/src/ChurchWebsite.Api/Program.cs` (static file serving)
- `server/src/ChurchWebsite.Api/appsettings.json` + `appsettings.Development.json`

### Frontend
- `app/src/views/PodcastListView.vue` (transcript view/download UI)
- `app/src/views/PodcastAdminView.vue` (status column)
- `app/src/views/PodcastEditView.vue` (status display)

## Verification commands

```bash
dotnet build server/ChurchWebsite.slnx
cd app && npm run build
```

## Checklist

### Correctness vs. AssemblyAI API (docs change often — verify against
https://www.assemblyai.com/docs/agent-instructions.md and
https://www.assemblyai.com/docs/llms.txt)

- [ ] Auth header is the **raw key with no `Bearer` prefix** on both `SubmitAsync`,
      `GetResultAsync`, and `SummarizeAsync`.
- [ ] `/v2/upload` sends **raw binary** (`application/octet-stream`), not multipart.
- [ ] Submit payload uses `audio_url` (from upload) + `speech_models`
      `["universal-3-5-pro", "universal-2"]`; no deprecated params
      (`summarization`, `summary_type`, `auto_chapters`).
- [ ] Poll handles statuses `queued`, `processing`, `completed`, `error`.
- [ ] Summary comes from LLM Gateway `POST /v1/chat/completions` with an exact valid
      model string (default `gpt-5-mini` — confirm still valid) and `max_tokens`.
- [ ] Base URLs configurable (US vs EU) for both STT and LLM Gateway.

### Data / state machine
- [ ] DB migration adds `transcript_status` (`none|queued|processing|completed|error`),
      `transcript_file_path`, `assemblyai_transcript_id`, `transcript_error`.
- [ ] Create: episode saved first, then transcription submitted; submit failure marks
      `error` but does NOT fail the create.
- [ ] Update with new audio: deletes old transcript file + resets status + resubmits.
      Update without new audio: transcript untouched.
- [ ] Delete: removes transcript file too.
- [ ] Poller only processes rows with `queued`/`processing`; N+1 queries acceptable for
      low volume.
- [ ] Summary only overwrites `Description` when empty (per user decision).

### Edge cases
- [ ] Missing `AssemblyAI:ApiKey` → graceful `error` status, episode still created
      (smoke tested).
- [ ] AssemblyAI `completed` but no `text` → treated as error.
- [ ] Summary generation failure → transcript still posted, status `completed`,
      `transcript_error` records the summary failure.
- [ ] Transcript URL only surfaced in DTO/RSS when status is `completed`.
- [ ] Poll interval parse failure falls back to 15s.

### Frontend
- [ ] Transcript fetched via `fetch(transcriptUrl)`; loading/error states handled.
- [ ] Status chip shows queued/processing/error/completed appropriately.
- [ ] RSS description appends `Transcript (download): <url>` — never the full text.
- [ ] Accessibility: `aria-expanded`/`aria-label` on toggle; Quasar components used.

### Security
- [ ] `AssemblyAI:ApiKey` empty in committed config; documented via env var
      (`AssemblyAI__ApiKey`) / user-secrets. No secret committed.
- [ ] No API key exposed to the browser (all calls server-side).

## Known gaps / deferred (for reviewer awareness)
- Old AssemblyAI jobs orphaned when audio is replaced (we only poll rows we track).
- No live end-to-end test was run (no API key available locally); only graceful-failure
  path smoke tested.
