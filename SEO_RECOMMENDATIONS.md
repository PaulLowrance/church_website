# SEO — recommendations for the public-facing site

> Status: **suggestions only**, not implementation. To be revisited later.
> Scope: **public pages** (home, about, contact, sermons, generic `/:slug` Pages).
> Out of scope: `/admin/**`, `/login` (`noindex`).
> Companion to `UI_RECOMMENDATIONS.md`. Both can land together; see "Implementation phasing" at the bottom.

---

## 1. Why this matters

Reference data informing this document, all fetched/verified:

- **Atlantis Digital Local SEO Schema Gap Analysis (2026-02)** — audited 16,673 local sites. Found only **51.1%** of churches have any schema; only **11.5%** use the `Church` schema type. LDS church sites hit **89.6%** (centralized template). Sites with any schema trend **0.3 positions better** in local SERP.
- **Site Architects — Schema Markup for Rich Results in 2025** — recommends "one primary entity per page", `PlaceOfWorship`/`Church` schema for contact/about pages, `BreadcrumbList` for navigation, `FAQPage` where appropriate, validation against Rich Results Test.
- **SEO Nimbus — Schema: PlaceOfWorship** — required: name, address, denomination, service times. Recommended: telephone, image, `sameAs` (social profiles). Pair with `LocalBusiness`.
- **Google Search Central** and **Schema.org** — JSON-LD is Google's preferred format for structured data. NAP consistency across website, Google Business Profile, and citations is the biggest local-SEO trust signal. Some industry audits (e.g., Gorilla Marketing 2019, BrightLocal 2026) correlate complete LocalBusiness schema with higher local-pack appearance, but correlation is not causation; the primary win is entity clarity and rich-result eligibility.
- **Patrick Stox — Vue SEO (2026-07)** — Vue 3 defaults to CSR. The HTML shell is empty. Non-negotiables: `createWebHistory()` (already done — `app/src/router/index.ts:5`), `@unhead/vue`'s `useSeoMeta()`, and a rendering strategy that puts content in HTML (vite-ssg / Nuxt / SSR).
- **Engineered.at — Vite SPA Static SEO Meta Tags Without SSR (2026-05)** — concrete recipe for prerendering public routes via `vite-ssg` and a `publicPages` route list.
- **Vue Router Dynamic Meta Tags Setup (2026-06)** — `@unhead/vue`'s `useSeoMeta()` is the modern standard, replacing `vue-meta` and avoiding duplicate/dangling tags.

---

## 2. Current state audit (the site as it is)

| Check | Current state | Severity |
|---|---|---|
| `<title>` per route | ❌ `app/index.html:7` is hard-coded to `<title>app</title>` — every page, including the homepage that is supposed to rank, ships the same title to Googlebot | **Critical** |
| `<meta name="description">` per route | ❌ none — Google makes its own snippet from copy on the page; you lose control of the SERP text | **Critical** |
| Open Graph / Twitter Card tags | ❌ none — when someone shares the About or Sermons page on Facebook/Slack/iMessage, the preview is blank or wrong | High |
| History-mode routing | ✅ `app/src/router/index.ts:5` already uses `createWebHistory()` | OK |
| Canonical URLs | ❌ none | High |
| `<html lang>` | `lang="en"` is hard-coded | OK |
| **Content in HTML for crawlers** | ❌ `app/index.html:10` `<div id="app"></div>` is empty — Googlebot *will* render it but Bing, social scrapers, and AI crawlers often don't. New-page-on-PR time-to-rank is delayed by days. | **Critical** |
| Structured data (JSON-LD) | ❌ none. `LocalBusiness`/`Church` schema makes the entity machine-readable and supports rich results, but the **local pack** is driven primarily by Google Business Profile, proximity, reviews, and prominence. Schema reinforces that signal; it does not create it. | High |
| Sitemap | ❌ no `sitemap.xml` | High |
| `robots.txt` | ❌ none | Medium |
| `<h1>` per page | Multiple Quasar `text-h*` classes; some semantic `<h1>` exists but inconsistent across files | Medium |
| Heading hierarchy / `<main>` / breadcrumb semantics | Mixed; `<main id="main">` and a single `<h1>` per page need to be the rule | Medium |
| Image `alt` | `aria-label` on icons; admin-img alt depends on what the body HTML contains | Medium |
| 404 status code | `HomeView.vue:35` renders the "Page Not Found" UI but probably still returns HTTP 200 | Medium |
| `<link rel="alternate" type="application/rss+xml">` | Not in `index.html` | Low-medium |
| HTTP→HTTPS, www→apex | Out of scroll: host/proxy concern | OK |

---

## 3. Tier 1 — do this first (highest leverage, small change)

### 3.1. Add `@unhead/vue` and `useSeoMeta()` for per-page tags

The single highest-leverage change. Replaces the hard-coded `<title>app</title>` with route-specific titles, descriptions, OG tags, and canonicals.

```sh
npm i @unhead/vue
```

```ts
// app/src/main.ts
import { createHead } from '@unhead/vue'
const head = createHead()
app.use(head)
```

```vue
<!-- app/src/views/HomeView.vue (inside <script setup>) -->
import { useSeoMeta, useHead } from '@unhead/vue'
import { useRoute } from 'vue-router'

const route = useRoute()

useSeoMeta({
  title: () => pageTitle.value
    ? `${pageTitle.value} | Brentwood Hills Primitive Baptist Church`
    : 'Brentwood Hills Primitive Baptist Church',
  description: () => pageDescription.value
    ?? 'Primitive Baptist church in East Fort Worth, Texas. Sermons, service times, and contact information.',
  ogTitle: () => pageTitle.value
    ? `${pageTitle.value} | Brentwood Hills Primitive Baptist Church`
    : undefined,
  ogDescription: () => pageDescription.value,
  ogType: 'website',
  ogUrl: () => `https://bhpbc.org${route.fullPath}`,
  twitterCard: 'summary_large_image'
})

useHead({
  link: [{
    rel: 'canonical',
    href: () => `https://bhpbc.org${route.fullPath}`
  }]
})
```

> `useSeoMeta()` handles flat SEO meta tags (title, description, OG/Twitter, robots). `useHead()` is the general-purpose API for `<link>` tags such as canonical, preconnect, and stylesheet. They compose freely.

For `/:slug` Page entity content (admin-defined pages), expose a `<meta name="description">` value: derive from the first 160 chars of body server-side in the DTO (single-line transformation), or add a `description` field to the `pages` table the admin can fill in. **Pick derivation first**; migrate to a real field later.

### 3.2. Per-route `<title>` overrides via Vue Router `meta`

Combine `@unhead/vue` with `meta: { title: 'About…' }` on each public route in `app/src/router/index.ts`. A small composable can read `route.meta.title` and feed it to `useSeoMeta()` with a `titleTemplate`. For the canonical link, still use `useHead()` (or the Unhead `CanonicalPlugin` set once in `createHead()`). Admin routes can use `'Admin | Brentwood Hills PBC'` as a default. Matches the type-safe pattern from Vue Router Dynamic Meta Tags Setup.

### 3.3. Static defaults in `index.html`

```html
<link rel="canonical" href="https://bhpbc.org/" />
<meta property="og:site_name" content="Brentwood Hills Primitive Baptist Church" />
<meta name="twitter:card" content="summary_large_image" />
<meta name="theme-color" content="#6f2e2a" />
<meta name="robots" content="index,follow" />
```

For admin routes, override with `<meta name="robots" content="noindex,nofollow">` via the per-route head manager.

### 3.4. `robots.txt`

`app/public/robots.txt`:

```txt
User-agent: *
Allow: /

Disallow: /admin
Disallow: /login

Sitemap: https://bhpbc.org/sitemap.xml
```

### 3.5. Sitemap

Two pieces:
- **Web app**: serve a `sitemap.xml` from a new endpoint `GET /sitemap.xml` in the .NET API. Reads all `pages` and all published `podcast_episodes`, generates XML, caches for 1 hour, returns `application/xml`. Add `/sitemap.xml` to the Vite proxy in `app/vite.config.ts` so dev works locally.
- **Generate from data** rather than relying on a hand-maintained list. Include `<lastmod>` from each entity's `UpdatedAt`.
- Validate with [xml-sitemaps.com](https://www.xml-sitemaps.com/validate-xml-sitemap.html) and submit to Google Search Console + Bing Webmaster Tools.

### 3.6. Skip to main content link

```html
<a href="#main" class="skip-link">Skip to main content</a>
```

A WCAG requirement for keyboard users. Google treats accessibility as a quality signal broadly, but it does **not** list skip links as a direct ranking factor. Do this because the site should be accessible, not because it moves rankings.

---

## 4. Tier 2 — get content into HTML (the structural unlock)

Two options:

### 4.1. Prerender public routes at build time (recommended)

Use `vite-ssg` (Engineered.at / Nuxt patterns). Routes known at build time (`/`, `/about`, `/contact`, `/podcast`, plus any slug that's also a `Page`) prerender to static `dist/<route>/index.html`. Admin routes don't need prerendering.

Publish-time increment: ~5–10 seconds per route to a static deploy. For Pages added by the admin, prerender them on deploy via a small CI step.

```ts
// app/vite.config.ts — replace vite-plugin calls
// script: replace "vite build" with "vite-ssg build"
```

This is a prerequisite for meaningful organic ranking: without prerendered HTML, Bing, social scrapers, and many AI crawlers see an empty page, and even Google delays indexing until it renders JavaScript. Prerendering does **not** guarantee ranking movement — content quality, authority, and competition still dominate — but it removes the structural blocker that prevents those factors from being evaluated.

### 4.2. Switch to Nuxt 3 (bigger commitment)

SSR + SSG + ISR + image optimization + native SEO module. Bigger commitment; trade-off is rewriting public views in Nuxt conventions. Recommend tier 4.1 first; revisit later.

Also: dynamic rendering and `prerender-spa-plugin` are deprecated as of 2024/2026 — don't build on them.

---

## 5. Tier 3 — JSON-LD structured data (entity clarity + rich results)

The Atlantis Digital study shows only 11.5% of church sites use `Church` schema. Adding it means you are in the minority that gives search engines explicit entity data; it does not by itself outrank the other 88.5%, but it removes a clarity gap they may have.

Add a `HeadSchemaService` (small backend static-generation helper) or just put `<script type="application/ld+json">` blocks at the homepage, /about, /podcast, and each sermon detail page. JSON-LD is what Google documents; the Rich Results Test validates it.

### 5.1. Homepage + About + Contact — `Church` + `LocalBusiness` + `Organization`/`Website` (via `@graph`)

Use **both** `Church` (semantic correctness; a subtype of `PlaceOfWorship`) and `LocalBusiness` (what Google's structured-data docs target for local rich results). A single entity can carry multiple `@type` values: `"@type": ["Church", "LocalBusiness"]`.

```json
{
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": ["Church", "LocalBusiness"],
      "@id": "https://bhpbc.org/#church",
      "name": "Brentwood Hills Primitive Baptist Church",
      "alternateName": "Brentwood Hills PBC",
      "url": "https://bhpbc.org",
      "logo": "https://bhpbc.org/icons/icon-512.png",
      "image": "https://bhpbc.org/og/home.jpg",
      "telephone": "+1-615-XXX-XXXX",
      "email": "office@bhpbc.org",
      "address": {
        "@type": "PostalAddress",
        "streetAddress": "...",
        "addressLocality": "Fort Worth",
        "addressRegion": "TX",
        "postalCode": "PLACEHOLDER",
        "addressCountry": "US"
      },
      "geo": {
        "@type": "GeoCoordinates",
        "latitude": 36.0331,
        "longitude": -86.7828
      },
      "openingHoursSpecification": [
        {
          "@type": "OpeningHoursSpecification",
          "dayOfWeek": ["Sunday"],
          "opens": "10:00",
          "closes": "12:00"
        }
      ],
      "denomination": "Primitive Baptist",
      "description": "Primitive Baptist church in East Fort Worth, Texas.",
      "sameAs": [
        "https://www.facebook.com/...",
        "https://www.youtube.com/@bhpbc"
      ]
    },
    {
      "@type": "WebSite",
      "@id": "https://bhpbc.org/#website",
      "url": "https://bhpbc.org",
      "name": "Brentwood Hills Primitive Baptist Church",
      "publisher": { "@id": "https://bhpbc.org/#church" },
      "potentialAction": {
        "@type": "SearchAction",
        "target": "https://bhpbc.org/search?q={search_term_string}",
        "query-input": "required name=search_term_string"
      }
    }
  ]
}
```

Notes:
- Use `Church` (subtype of `PlaceOfWorship`) for semantic correctness, and include `LocalBusiness` for Google's local rich-result eligibility. `"@type": ["Church", "LocalBusiness"]` is valid JSON-LD.
- `LocalBusiness` on its own is what Google explicitly documents for local business rich results; pairing it with `Church` keeps both schema.org and Google happy.
- `SearchAction` only makes sense once you have a working search page. Omit until then.
- Extract address and church name from config (`Site:ChurchName` already exists; add `Site:Address*` keys). Generate JSON-LD server-side from config.
- Validate with [Google's Rich Results Test](https://search.google.com/test/rich-results).

### 5.2. `BreadcrumbList` on every page that has hierarchy

```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://bhpbc.org/" },
    { "@type": "ListItem", "position": 2, "name": "About", "item": "https://bhpbc.org/about" }
  ]
}
```

### 5.3. `FAQPage` if you add an FAQ block

Don't invent questions — mark up only real FAQ content on the page.

### 5.4. Per-sermon `AudioObject` / `PodcastEpisode` schema

Gives the "Listen to this episode on your podcast app" badge in some search experiences. Defer or include now depending on capacity — same data model already exists, just an extra `<script type="application/ld+json">` block on each sermon page.

### 5.5. Per-blog-post `Article` schema

Skip until you actually have blog/devotional content.

---

## 6. Tier 4 — performance-driven SEO signals (overlaps UI doc)

Core Web Vitals are confirmed Google page-experience signals. The UI doc's performance recommendations **(stripping Quasar from public views is the single biggest win)** unlock SEO too, mainly by reducing the time to first contentful paint and improving crawl rendering.

- **vite-ssg** (Tier 4.1 above) — biggest LCP/INP win.
- **System font for body** + one preloaded serif for headings (`font-display: swap`).
- **AVIF + WebP fallback** via `<picture>` on every card image.
- **`<img loading="lazy" decoding="async">`** below the fold.
- **Reserve layout space** with `width`/`height` (or `aspect-ratio` in CSS) to avoid CLS.
- **`<link rel="preconnect">`** to any cross-origin fetcher used at first paint.
- **`<link rel="alternate" type="application/rss+xml" title="Sermons" href="/podcast/rss">`** in `<head>` — required by podcast directories and helps discovery of the sermon archive.

---

## 7. Tier 5 — content / off-code

These are not code changes but they unlock everything above:

- **Local SEO setup**: claim and verify the Google Business Profile. Match the church's name/address EXACTLY with the JSON-LD you ship (NAP consistency is the most frequently cited local-SEO trust signal). Keep hours aligned.
- **Embed text on the homepage** with the city name, denomination ("Primitive Baptist"), and address — text, not background image. Google reads it; organic local rankings benefit from it, and it reinforces the same facts shown in the Google Business Profile.
- **If absent, write an About page** (250–400 words of plain prose: pastor's name, denominational alignment, what to expect at a service).
- **Contact page** with the address, a static Google Maps embed (no JS), driving directions, and service times matching JSON-LD.
- **Internal linking**: each sermon should link to its Scripture reference (Bible Gateway is a common convention); each page links back to home and the sermons archive with descriptive anchor text.
- **Submit sitemap** to Google Search Console + Bing Webmaster Tools.

---

## 8. The "rank higher" punch list (ordered)

If the goal is "home, about, contact rank for *church near me* and related searches," these five in order:

1. **Claim and optimize the Google Business Profile** — name, address, phone, hours, service category, photos. The local pack is driven primarily by GBP, proximity, reviews, and prominence, not by on-page markup alone.
2. **NAP consistency** across website JSON-LD, Google Business Profile, and third-party listings. This is the #1 local trust signal.
3. **JSON-LD `Church` + `LocalBusiness` schema on homepage and About** — makes the church machine-readable, supports rich results, and reinforces the entity match with GBP. Industry audits (e.g., Gorilla Marketing 2019, BrightLocal 2026) correlate complete LocalBusiness schema with higher local-pack appearance, but correlation is not causation; the main win is NAP/entity clarity.
4. **Get content into HTML via prerendering** (`vite-ssg`). Currently the homepage is empty to Bing/scrapers and slow for Google. This is a prerequisite for organic ranking, not a direct local-pack lever.
5. **Per-route `<title>` and `<meta description>` via `@unhead/vue`'s `useSeoMeta()`** — replace `app/index.html:7` `<title>app</title>` — plus `robots.txt` and a generated `sitemap.xml`.

Five-step sprint, bulk of the structural gain. After this, compete on content quality and local authority.

---

## 9. Implementation phasing

| Phase | Scope | Outcome |
|---|---|---|
| **0 (this week)** | Add `@unhead/vue`. Replace `<title>app</title>`. Wire `useSeoMeta()` to one route (HomeView). | `<title>` and description finally correct on homepage |
| **1** | Apply `useSeoMeta()` to `/about`, `/contact`, `/podcast`; canonical + OG defaults in `index.html`; `robots.txt`; ship `/sitemap.xml` endpoint in .NET | Crawlers find all public pages; SERP snippets correct; rich-result eligibility unlocked |
| **2** | Ship `Church` + `LocalBusiness` JSON-LD on homepage + About. Link NAP data through `appsettings.json`. Claim Google Business Profile, ensure NAP matches | Entity clarity; local rich-result eligibility; reinforces GBP |
| **3** | Swap to `vite-ssg` for public routes; remove Quasar from public views (per UI doc §3.I.2) | Non-Google crawlers see content; index latency drops from days → hours |
| **4** | Performance: AVIF + WebP fallback, fluid font sizes, `prefers-color-scheme` dark mode, image aspect-ratio CSS to avoid CLS | Core Web Vitals; LCP under 2.5s |
| **5** | Per-sermon detail pages; `Article`/`PodcastEpisode` JSON-LD; `BreadcrumbList` for series hierarchy | Long-tail traffic; deeper index coverage |

Phases 2–3 are the same Vue refactor as the UI doc's PR 6 ("Public-shell rewrite + prerendering PR"). Recommend doing them together so the rewrite cost is amortized.

---

## 10. What stays admin-only

`/admin/**` and `/login` are excluded from indexing via `<meta name="robots" content="noindex,nofollow">`. Excluded from `robots.txt` `Disallow:`. Excluded from `sitemap.xml`. The Quasar app chrome is unchanged on those routes.
