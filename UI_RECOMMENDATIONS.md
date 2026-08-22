# Public-facing UI — review & recommendations

> Status: **suggestions only**, not implementation. To be revisited later.
> Scope: **public-facing pages only** (homepage, sermons, generic `/:slug` Pages, public nav).
> Out of scope: admin views (`/admin/**`, `/login`) — they keep the current Quasar style for clarity.

---

## 1. What is making the public UI feel cold / utilitarian

Audited the current public-facing surfaces: `app/src/views/PodcastListView.vue`, `HomeView.vue`, and `app/src/components/NavMenu.vue`.

| Pattern | Where | Issue |
|---|---|---|
| Material design ripples / `v-ripple` on nav buttons | `NavMenu.vue` mobile drawer | App-laundry vibration on a sacred-content page |
| Quasar `bg-primary` flat header with an elevated shadow | `NavMenu.vue:62` `<q-header elevated class="bg-primary text-white">` | Reads as a SaaS dashboard chrome, not a publication |
| Stock system font, 18px/145% | `app/src/style.css:14-31` | No character; renders identically to admin tools |
| Material icons sprinkled as bullets | `PodcastListView.vue:144,147` (`<q-icon name="person" />`, `name="event"`) | App-iconography convention, not editorial |
| Bright purple accent (`#aa3bff` background, `#c084fc` dark) | `app/src/style.css:7,40` | Default Quasar brand color — feels tech-startup |
| **No sermon cover art** | `PodcastListView.vue` cards | A list of grey rectangles; reads like placeholder data |
| Audio player dropped in with `<audio controls>` default | `PodcastListView.vue:228` | Browser-default chrome; no skinned control |
| Card with raised shadow + "Audio File" caption + tags-as-outlined-chips | `PodcastListView.vue:138-244` | Looks like a software settings card |
| Page heading uses `q-page padding` | `PodcastListView.vue:107` | App-shell padding rather than editorial line length |
| Quasar serifless gray on white (`text-grey-7` subtitles) | `PodcastListView.vue:108,143` | Form-letter grey reads as utility UI |
| No hero, no warmth, no church identity | `PodcastListView.vue:107-111` | `<h1>Sermons</h1>` + body paragraph is all there is |
| Mobile drawer opens with a "Navigation" header | `NavMenu.vue:158` | Generic app nav label |
| Generic 404 message body | `HomeView.vue:35-38` | "Page Not Found" is a developer message |
| No `aria-hidden`/`tabindex` on drawer when closed | `NavMenu.vue:155` | `aria-hidden="!drawerOpen"` is an inversion of the spec |
| Pipe-separated meta (speaker · date · series) | `PodcastListView.vue:144-153` | Looks like a CRM listing |

The admin views are unaffected — but visitors see none of the editorial care that other churches offer. The visual register is "ops console" rather than "invitation."

---

## 2. Reference sites (verified live)

| Site | What it does well |
|---|---|
| [desiringgod.org/sermons](https://www.desiringgod.org/sermons) | Long editorial feed; no card shadows; serif headlines; "Aug 8, 2026" date as plain text; square author photo; "Recent" + "Popular" sections |
| [thegospelcoalition.org/sermons](https://www.thegospelcoalition.org/sermons/) | Minimal list with a left-rail filter (Speaker / Topic / Scripture); white background; modest inner padding; no card chrome |
| [austinstone.org/sermons](https://austinstone.org/sermons) | Hero image; square series artwork per entry; "8.9.26" date stamp; preacher name as small caps pill; scripture "Reference:" line ("1 John 1:1-4"); filter sidebar (Book/Preacher/Format/Year); series list with current/upcoming badges; "Load More" pagination |
| [Helmsman Design case study on The Ranch Church](https://medium.com/helmsman-design/case-study-the-ranch-church-d5f2615821a5) (Aaron Salvato, 2025-08) | "Chill tiles. Space to breathe." / "Each resource has its own moment, but together they feel cohesive." / "Round, warm copy … no fog machine. No branding jargon. Just an invitation." |
| [Church Creation — Sermon archive guide](https://churchcreation.com/church-sermon-archive-guide/) (2026-06) | Argues for the "Netflix for sermons" pattern: series artwork, current-series hero, scripture/topic/series/speaker filters |
| [webaim.org/techniques/forms/](https://webaim.org/techniques/forms/) | Canonical accessibility checklist most church sites fail at: explicit `<label for=…>`, keyboard navigation, focusable errors, logical reading order, `<fieldset>` for radio groups |
| [Inclusive Components](https://inclusive-components.design/) (Heydon Pickering) | Cards, dialogs, and tables that hold up under screen reader testing |

---

## 3. Concrete recommendations

Organized by layer. Items are independently implementable.

### A. Typography — single biggest visible change

Current font is `system-ui, 'Segoe UI', Roboto, sans-serif` everywhere. Replace with a two-font system.

```css
:root {
  --sans: 'Inter', system-ui, -apple-system, sans-serif;
  --heading: 'Spectral', 'Iowan Old Style', Georgia, serif;
  --meta: 'Inter', system-ui, sans-serif;
}
```

- **Headings:** serif with liturgical character — **Spectral**, **Source Serif 4**, **Crimson Pro**, or **Lora**.
  Free via Google Fonts; **self-host** to avoid Google CDN tracking.
- **Body:** sans, but **Inter** or **Public Sans** (wider x-height + open apertures; helps at 14–16 px on low-end Android or older displays).
- **Numerals:** `font-variant-numeric: tabular-nums` on dates/file sizes/verse refs so they line up.
- **Scripture references:** small caps (`font-variant-caps: small-caps; letter-spacing: 0.05em`) on "1 John 1:1-4" — TGC, Austin Stone, Bible Gateway convention.

Both fonts are **variable woff2**, ~50–80 KB total, self-hostable. `font-display: swap`; don't preload more than one weight.

### B. Color palette + dark mode (real CSS, not Material)

Replace Quasar primary (`#aa3bff`) with church-flavored neutrals and two accents:

```css
:root {
  --paper: #fbf8f3;            /* light surface */
  --ink:   #1c1a17;            /* primary text */
  --rule:  #ebe5d8;            /* hairlines */
  --muted: #6b5f4f;            /* meta text */
  --accent-burgundy: #6f2e2a;  /* links, brand */
  --accent-gold:     #b08a3e;  /* highlights, hover */
}
@media (prefers-color-scheme: dark) {
  :root {
    --paper: #15140f;
    --ink:   #f0e9d8;
    --rule:  #2f2a22;
    --muted: #a8987c;
    --accent-burgundy: #d99c8a;
    --accent-gold:     #d8b566;
  }
}
```

- Respect `prefers-color-scheme` for users who haven't touched the toggle.
- Add a UI toggle in the header (sun/moon icon) that pins `data-theme="dark|light"` on `<html>`; persist to `localStorage.theme`. Don't ship three states — two, follow OS.
- Test contrast through [WebAIM's contrast checker](https://webaim.org/resources/contrastchecker/). Burgundy on paper must hit WCAG AA 4.5:1 for body text, 3:1 for large text.

Removes ~50% of "cold" feel without changing layout. The Austin Stone is closer to this palette; the Village Church is warmer still.

### C. Hero / header — give the home page a face

Replace `app/src/components/NavMenu.vue`'s stark purple bar with a quiet header that breathes:

- A **logo / wordmark** centered or left-aligned — text-only "Brentwood Hills Primitive Baptist Church" in the heading serif.
- On the homepage, a **serif hero block** below the logo with three lines:
  - Eyebrow: **This Sunday's Sermon** or **Sermons**
  - Title: the latest sermon's title
  - One-line *spoken* subtitle / exposition summary
  - "Listen" + "Read Transcript" tertiary buttons

The Austin Stone, Desiring God, and TGC all use this pattern. The hero is *content*, not chrome.

For other public pages (e.g. `/about`, `/contact`), insist the same hero pattern + a single sentence of body in a `30rem` measure.

### D. Sermon cards — give them cover art

`PodcastListView.vue:136-246` is currently a stack of identical grey rectangles. Switch to:

- **Square cover image** for every sermon (1024×1024 source, served at ~256×256 for cards). Without series artwork, generate a per-sermon image from title + speaker via `<canvas>` or `og-img`; with artwork, store one image per series and assign it on creation.
- **Two columns on `>= md`** (currently single column). Austin Stone does this; Desiring God does card-per-row.
- Card contents: cover art (1:1), title (serif, 18-20px), speaker (sans, 14px, `--muted`), date (sans italic, `--muted`), audio player + small "View transcript / Download" button group.
- **Series pill** as an overlay on the cover's bottom-left (`bg-rgba(0,0,0,0.6)`). TGC + Desiring God + Austin Stone convention. Chips read as Material; overlays read as design.

### E. Audio player polish

`PodcastListView.vue:228` uses `<audio controls>` which is fine, but a few small lifts:

- `preload="none"` (current default behavior fetches metadata — wasteful on slow networks).
- Wrap with a styled bar: play button + duration + ".mp3 / 56 MB" file hint + download button.
- Future: a waveform via **wavesurfer.js** (~28 KB gz) — visual impression without forcing stream.

### F. Sermon detail page (new)

Today every sermon *is* the list page. Add a long-form detail at `/sermon/:id`.

- Hero block with cover art, title, speaker, scripture ref.
- Audio player prominently.
- AI-generated description prose (only if non-empty). Use `prose` styling: `max-width 36rem`, `line-height 1.7`, serif body, ragged right. Avoid the q-card border.
- "Read the transcript" disclosure. When opened, a `<pre>` of the transcript file with `white-space: pre-wrap`.

Add as a new view; don't retrofit the list page.

### G. Discovery / filter

Inspiration: Austin Stone filter rail, TGC filter rail.

- A **static sidebar** on `>= lg` with: Series, Speaker, Scripture (book picker), Year. Native `<details>`/`<summary>`; no JS accordion bloat. Native `<form>` GET to filter.
- **Mobile**: a top "Filter" button opens a `<dialog>`-based sheet (the `popover` attribute has 90% browser support — use with fallback).
- **Search**: simple substring over title + speaker + description. Combine with a tag filter; `q=` query param. String match in the existing `GET /api/podcast/episodes` endpoint is fine.

### H. Accessibility — concrete gaps in the current code

| WCAG criterion | Current | Fix |
|---|---|---|
| **1.1.1 Non-text content** | `<audio>` has `aria-label="Audio player for ${episode.title}"` ✓ but the transcript icon is unlabeled | Ensure all `<q-btn icon="…">` have explicit `aria-label`; `<q-icon name="article">` is used without text |
| **1.3.1 Info & relationships** | `q-chip` for tags is decorative-only | Add `role="list"` + `<li>` semantics via `tag="ul"`, or plain `<ul>` markup |
| **1.4.3 Contrast (minimum)** | `--text: #6b6375` ≈ 4.6:1 on `--bg: #fff` (passes AA but is close to the line); dark `#9ca3af` on `#16171d` ≈ 4.6:1 | Use the burgundy + new ink palette above for more comfortable contrast |
| **1.4.4 Resize text** | `font-size: 16px @ <1024px` is a regression | Use `clamp()` for fluid; never shrink below 16px |
| **1.4.10 Reflow** | `width: 1126px` on `#app` (`style.css:159-167`) + inline border | Remove fixed width; let the public template set fluid width |
| **1.4.12 Text spacing** | Body line-height 145% | Push to 160–175% |
| **2.1.1 Keyboard** | Mobile drawer menu items have `v-ripple` and click handlers; `<q-item>` adds `tabindex` but worth a manual pass | Tab through at least once |
| **2.4.1 Bypass blocks** | No "Skip to main content" link | Add `<a href="#main">Skip to main content</a>` at top |
| **2.4.7 Focus visible** | Custom buttons rely on Quasar ripple — not always great for keyboard | ` :focus-visible { outline: 3px solid var(--accent-gold); outline-offset: 2px; }` |
| **3.3.2 Labels or instructions** | Speaker name field lacks `for` binding | Native `<label for>` pairs once we move to plain markup |
| **4.1.2 Name, role, value** | `aria-hidden="!drawerOpen"` is inverted | `aria-hidden="false"` when open; `tabindex="-1"` on closed drawer |
| **Reduced motion** | No `prefers-reduced-motion` handling | Wrap fades/slides in `@media (prefers-reduced-motion: no-preference)` |
| **Dark mode** | Toggle exists in `style.css:33` for OS only, no UI control | Add a toggle, persist to `localStorage` |
| **`<html lang>`** | Only `lang="en"` | Set per-page if you ever do bilingual |

Reference WebAIM's article on form accessibility, and **Inclusive Components** for accessible cards, dialogs, and tables.

### I. Performance — keep it small

Modest-clients goal means the public bundle should NOT inherit Quasar for routes the visitor never sees.

1. **Per-route code splitting** — already lazy-loading `LoginView`, `PageCreateView`, etc. via `() => import('@/views/...')`. Verify public routes use lazy-import (they do today).
2. **Strip Quasar from the public views entirely** (recommended). Visitor imports plain HTML + scoped CSS without Quasar:
   - `q-card` → `<article class="card">`
   - `q-btn` → `<a class="btn">` or `<button>`
   - `q-banner` → `<div role="status" class="banner">`
   - `q-icon` → inline SVG or skip
   - `q-page` → plain `<main id="main">`
   - This requires `App.vue` to render a route-aware layout shell: public routes get a plain semantic `<nav>` + `<main>` + `<footer>`; admin routes keep the existing `<q-layout>`/`<q-page-container>`.
3. **Bundle target**: measure `app/dist/assets/index-*.js` after a production build. A Quasar-heavy app typically lands 150–300 KB gzipped; the public views alone should drop sharply once Quasar components are no longer imported there. Target the public entry under **60–80 KB gz** as a first milestone.
4. **Audio**: self-host; **`<audio preload="none">`** and lazy-attach a poster image. Audio stays in `uploads/audio`.
5. **Images**: serve via `srcset` (256w, 512w, 1024w) and AVIF when supported. `<img loading="lazy" decoding="async">` on every card below the fold.
6. **Fonts**: load only what you need via `<link rel="preload" as="font" type="font/woff2" crossorigin>` for the heading serif only. Body stays `system-ui`.
7. **Third-party scripts**: kill everything. No analytics, no chat widgets.
8. **Service worker**: optional offline audio cache. Worth it if mobile listeners on poor connections matter.

### J. Navigation and routing changes

- Keep `NavMenu.vue` mounted in `App.vue`, but **conditionally swap its inner template** based on route. Admin routes render the Quasar nav. Public routes render plain semantic `<nav>` with `<a>` links, role-aware via Pinia store.
- Add a `<footer>` component on all public pages. The Austin Stone puts: legal links, contact, denomination note ("A Primitive Baptist Church"), email updates opt-in.

---

## 4. Tech-stack options for the public site

You said "no need to be restricted to the current UI library." Three concrete options, ordered by effort:

**Option A — Vue + plain CSS only (recommended)**
Inside the existing app, build `/sermon/:id`, `/podcast`, and `HomeView` using **plain `<template>` + scoped `<style>`** with no Quasar imports in those views. Coexist with Quasar for admin. Saves ~200 KB gz for visitors and looks just as editorial as the reference sites. CSS is just hand-written with custom properties (no Tailwind required). Easiest diff, no new dep.

**Option B — Vue + utility-first CSS (Tailwind)**
Add `tailwindcss` and `@tailwindcss/typography`. Use the `prose` class for long-form sermon descriptions. Smaller CSS class footprint than hand-rolling. Best for teams already familiar with Tailwind.

**Option C — Astro + Vue islands**
Static rendering for the home and sermon archive (huge SEO win since scripture/series text gets indexed), with Vue 3 islands for the audio player + filters only. Build the public routes as static HTML, hydrate what's interactive. Admin keeps the current Vue/Quasar stack. Significant restructure; recommend only if TTFB matters more than admin continuity.

Recommendation: **Option A**. Removes the cold feel without the cost of a second framework.

---

## 5. Constraints to keep

Per direction:

- **Admin views stay in Quasar** — `PodcastAdminView.vue`, `PodcastCreateView.vue`, `PodcastEditView.vue`, `AdminView.vue`, `LoginView.vue`. They work well for an information-density operator. Re-skinning hurts clarity. Theme only with the new burgundy accent so admin doesn't visually clash with the public site.
- **Routes stay as-is** — `/podcast`, `/admin/podcast`, `/podcast/rss`, etc. Don't rename; they exist as code references and in the RSS feeds.
- **API stays as-is** — `/api/podcast/episodes`, DTO fields (including `speakerDisplay`, `transcriptUrl`). Frontend-only changes from here.

---

## 6. Out of scope, but worth flagging later

- Sermon **series artwork** management — separate entity, image upload, optional admin input.
- Per-sermon **description** switching between auto-summary and admin-override (data model already supports because `episode.Description` overrides when non-empty).
- A **search bar** with cross-episode full-text.
- A **live "now playing"** UI for current Sunday's sermon (uses same DTO; just reads `publishedAt` closest-lte-now).

---

## 7. PR-sized breakdown (suggested order)

Each is independently reviewable. Admin views are not touched in any of them.

1. **Typography + color + dark mode PR** — palette swap, font loaders, `prefers-color-scheme` + UI toggle, focus-visible outline. ~6 files; small risk.
2. **Sermon cover image** entity + admin upload + display in card (uses existing `Storage:ImagesPath`).
3. **Sermon card layout PR** — two columns on `>= md`, cover art, series overlay, stylized meta. New sermon detail route at `/sermon/:id`.
4. **Hero on homepage PR** — replaces `PodcastListView.vue`'s `<h1>` with the eyebrow + title + subtitle + "Listen" pattern. New `/sermon/:id` page.
5. **Filter sidebar PR** — Series / Speaker / Scripture / Year with native `<details>`/`<summary>`. Mobile sheet via `<dialog>`.
6. **Public-shell rewrite + prerendering PR** — drop q-card / q-btn / q-banner in public templates; route-aware layout shell (plain `<main>` for public, `<q-layout>` for admin); switch build to `vite-ssg`. This is one PR because the same templates are rewritten for both goals; splitting doubles the cost and risk.
7. **Audio player polish PR** — `preload="none"`, custom controls, file hint, download button grouping.
8. **Accessibility pass** — WCAG items from §3.H. Tracked checklist; cheapest on its own PR.

The SEO plan (separate document) overlaps items 6 + an additional tier; recommend doing them together so the rewrite cost is amortized.
