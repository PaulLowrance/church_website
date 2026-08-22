# UI Image Asset Specifications

> Companion to `UI_RECOMMENDATIONS.md`. These sizes are what the first four UI PRs expect for a clean demo. Each component's image source, display size, and responsive fallback are documented below.
> 
> All dimensions are in CSS pixels (1×). Provide 2×/3× source assets where noted. Prefer **sRGB** color space. Use **progressive JPEG**, **WebP**, or **AVIF** for photographs; use **SVG** for logos and icons.

---

## 1. Sermon Cover Image

The square artwork shown on sermon cards, sermon detail pages, and the homepage hero.

| Attribute | Value | Notes |
|---|---|---|
| **Aspect ratio** | 1:1 | Square |
| **Source asset** | 1024×1024 px | Master upload. Stored at `/uploads/images/` via existing `Storage:ImagesPath`. |
| **Display sizes** | 128×128 – 512×512 px | Card thumbnail (256px), detail page (512px), hero (≈300px), OG fallback (1200×630) |
| **Formats to serve** | AVIF primary, WebP fallback, JPEG ultimate fallback | See `<picture>` / `srcset` strategy below |
| **File size budget** | Source ≤ 500 KB; served 512w ≤ 80 KB | Image-optimise on upload or at build |
| **Alt text** | Required | e.g. `"Cover art for '[Sermon Title]'"` |
| **Safe area** | All four edges safe to 8% | Artwork should not depend on text within 8% of edges; cards may crop/round slightly |
| **Text on image** | Optional | If present, keep text ≥ 24 px high at 1024×1024 source; expect downscaling to 256px |
| **Fallback** | Canvas-generated placeholder | If no cover image, generate a 1:1 placeholder from title + speaker using church brand colors (burgundy `#6f2e2a` on paper `#fbf8f3`) |

### `srcset` for sermon cover

```html
<img
  src="/uploads/images/sermon-cover-256.jpg"
  srcset="/uploads/images/sermon-cover-256.avif 256w,
          /uploads/images/sermon-cover-512.avif 512w,
          /uploads/images/sermon-cover-1024.avif 1024w"
  sizes="(max-width: 600px) 128px, (max-width: 1200px) 256px, 512px"
  width="256"
  height="256"
  loading="lazy"
  decoding="async"
  alt="Cover art for 'The Promises of God'"
/>
```

> `width`/`height` attributes prevent layout shift (CLS). The `sizes` value should match the largest display width in each breakpoint.

---

## 2. Sermon Card

`PodcastListView.vue` (public sermons list) displays the cover image in a two-column grid.

| Attribute | Value | Notes |
|---|---|---|
| **Component** | `<article class="card">` | Replaces `q-card` |
| **Grid** | 1 column < 768px; 2 columns ≥ 768px | Gap: `24px` (`1.5rem`) |
| **Card max width** | 100% / 2 columns (≈ 560px each at 1200px viewport) | Fluid, not fixed |
| **Cover image display** | 256×256 px on desktop; 128×128 px on mobile | Square, `object-fit: cover` |
| **Title** | Serif, 20px / 1.25rem | 2-line max; clamp to `line-clamp: 2` |
| **Meta** | Sans, 14px / 0.875rem | Speaker, date, series overlay |
| **Series overlay** | Bottom-left of cover | `bg-rgba(0,0,0,0.6)` text, 12px, white |
| **Audio player** | Styled bar below meta | Play + duration + download |

### Card layout sketch

```
┌─────────────────────────────────────┐
│ ┌────────┐                          │
│ │ 256×256 │  Title (serif, 20px)     │
│ │ cover   │  Speaker · Date          │
│ │         │  [▶ 45:12] [Download]    │
│ └────────┘                          │
└─────────────────────────────────────┘
```

### Image required for card
- One **1024×1024 px source** per sermon, scaled to 256/512 px.
- If no cover exists, render a 256×256 CSS-generated placeholder (not an image file) to avoid blank grey rectangles.

---

## 3. Homepage Hero

Replaces the current `<h1>Sermons</h1>` with a content-first hero. The hero can either use the latest sermon cover or a church photograph.

### Option A: Latest-sermon hero

| Attribute | Value | Notes |
|---|---|---|
| **Image source** | Latest sermon cover image (1024×1024) | Reuses existing sermon cover asset |
| **Display size** | 300×300 px – 400×400 px | Square, left-aligned or centered |
| **Eyebrow** | Sans, 12px / 0.75rem, uppercase, letter-spacing 0.05em | "This Sunday's Sermon" or "Latest Sermon" |
| **Title** | Serif, 36px / 2.25rem | Latest sermon title |
| **Subtitle** | Sans, 18px / 1.125rem | 1–2 sentence description or scripture |
| **Buttons** | "Listen" + "Read Transcript" | Tertiary text links or small buttons |

### Option B: Church photograph hero

| Attribute | Value | Notes |
|---|---|---|
| **Aspect ratio** | 16:9 or 3:2 | Hero section height: 50–70vh, max 720px |
| **Source asset** | 1920×1080 px (16:9) or 1800×1200 px (3:2) | Hero background image |
| **Display size** | Full viewport width, max 1440px container | `object-fit: cover` |
| **Safe area** | Center 60% of image | Text overlay on left/bottom; keep faces/building out of text zone |
| **Overlay** | Subtle gradient `linear-gradient(to right, rgba(0,0,0,0.5) 0%, transparent 70%)` | Only if text is directly on image |
| **Alt text** | Required | e.g. `"Brentwood Hills Primitive Baptist Church building"` |

### Hero text overlay safe zone

```
┌────────────────────────────────────────┐
│  TEXT SAFE ZONE          ┊             │
│  (left 50%, top 70%)     ┊   image    │
│                          ┊   safe     │
│  [Listen] [Read Transcript]            │
└────────────────────────────────────────┘
```

### Recommendation
Start with **Option A** (latest sermon cover). It requires no new asset type beyond the sermon cover image, so it can ship in the same PR as the hero. Option B can be added later when a high-quality church photograph is available.

---

## 4. Sermon Detail Page (`/sermon/:id`)

New long-form page for a single sermon.

| Attribute | Value | Notes |
|---|---|---|
| **Cover image display** | 512×512 px on desktop; 256×256 px on mobile | Centered above title |
| **Title** | Serif, 42px / 2.625rem | Single sermon title |
| **Speaker / scripture** | Sans, 16px / 1rem | Below title |
| **Audio player** | Prominent, full-width max 720px | Custom controls |
| **Description** | Serif body, max-width 36rem | Auto-generated or admin override |
| **Transcript** | `<pre>` / `<div>` with `white-space: pre-wrap` | Disclosure panel |
| **Download section** | Audio + transcript download links | Below player |

### Image required
- Same **1024×1024 sermon cover** source as the card and hero.

---

## 5. Series Artwork

Optional separate entity. PR 2 may only implement per-sermon covers; series artwork is a follow-up.

| Attribute | Value | Notes |
|---|---|---|
| **Aspect ratio** | 1:1 | Square, same as sermon cover |
| **Source asset** | 1024×1024 px | Stored at `/uploads/images/` |
| **Display sizes** | Card thumbnail 256px, series page 512px | Series list page |
| **Fallback** | First sermon's cover or CSS-generated title card | If no series artwork |

### Relationship

- A **Series** has many **Sermons**.
- A **Sermon** can override its cover with a per-sermon image; otherwise it inherits the series cover.
- If neither exists, generate a placeholder.

---

## 6. Church Logo / Wordmark

Used in the header and footer.

| Attribute | Value | Notes |
|---|---|---|
| **Preferred format** | SVG | Scales cleanly; small file size |
| **PNG fallback** | 1024×256 px (4:1 horizontal) | For email clients / older browsers |
| **Display size** | 180–240 px wide × auto height in header | Max height 48px |
| **Display size (footer)** | 140–180 px wide | Slightly smaller |
| **Alt text** | `"Brentwood Hills Primitive Baptist Church"` | Use as text alternative |
| **Safe area** | 16px padding inside logo bounding box | Prevents clipping when scaled |

### Text-only wordmark
If no logo file exists, render the church name in the heading serif font as a text-only wordmark. This is the MVP approach and avoids a missing-asset state.

---

## 7. Social / Open Graph Image

Used when a sermon, page, or the homepage is shared.

| Attribute | Value | Notes |
|---|---|---|
| **Aspect ratio** | 1.91:1 | Open Graph / Twitter standard |
| **Source asset** | 1200×630 px | JPG or PNG, ≤ 8 MB |
| **Display** | Social previews | Facebook, Slack, iMessage, etc. |
| **Fallback** | Homepage: church hero photo or generated church wordmark on brand background | Sermon: sermon cover image padded to 1200×630 |
| **Safe area** | Center 80% of image | Some platforms crop edges |
| **Text on image** | Optional | Keep minimal; 48px minimum at 1200×630 |

### Sermon OG fallback generation
If no dedicated OG image is uploaded, generate a 1200×630 canvas from:
- Background: church brand color or sermon cover image blurred/darkened
- Center: sermon cover image (300×300)
- Text: sermon title + speaker

This can be done server-side at share time or client-side at build time if using prerendering.

---

## 8. Favicon / Touch Icons

| Attribute | Value | Notes |
|---|---|---|
| **favicon.svg** | Vector | Preferred modern format |
| **favicon.png** | 32×32 px | Fallback for older browsers |
| **Apple touch icon** | 180×180 px | `apple-touch-icon.png` |
| **Maskable icon** | 512×512 px | `maskable-icon.png` for PWA |
| **Theme color** | `#6f2e2a` (burgundy) | Matches brand accent |

---

## 9. Summary of Required Asset Counts

| Asset | Count | Source Size | Generated Sizes | PR |
|---|---|---|---|---|
| Sermon cover image | 1 per sermon | 1024×1024 | 256, 512, 1024 | PR 2 |
| Series artwork | 1 per series | 1024×1024 | 256, 512 | Future |
| Church logo / wordmark | 1 | SVG + 1024×256 PNG | header/footer sizes | PR 1 (text-only fallback) or PR 4 |
| Homepage hero photo | 1 (optional) | 1920×1080 | 720, 1440 | PR 4 |
| OG / social image | 1 per sermon + 1 homepage | 1200×630 | — | PR 3 or PR 4 |
| Favicon set | 1 set | 32, 180, 512 | — | PR 1 |

---

## 10. Responsive Image Strategy

Use this `<picture>` pattern for all photographic images (sermon cover, series artwork, hero):

```html
<picture>
  <source
    type="image/avif"
    srcset="/uploads/images/cover-256.avif 256w,
            /uploads/images/cover-512.avif 512w,
            /uploads/images/cover-1024.avif 1024w"
    sizes="(max-width: 600px) 128px, (max-width: 1200px) 256px, 512px"
  />
  <source
    type="image/webp"
    srcset="/uploads/images/cover-256.webp 256w,
            /uploads/images/cover-512.webp 512w,
            /uploads/images/cover-1024.webp 1024w"
    sizes="(max-width: 600px) 128px, (max-width: 1200px) 256px, 512px"
  />
  <img
    src="/uploads/images/cover-256.jpg"
    srcset="/uploads/images/cover-256.jpg 256w,
            /uploads/images/cover-512.jpg 512w,
            /uploads/images/cover-1024.jpg 1024w"
    sizes="(max-width: 600px) 128px, (max-width: 1200px) 256px, 512px"
    width="256"
    height="256"
    alt="..."
    loading="lazy"
    decoding="async"
  />
</picture>
```

### Server-side generation
The .NET backend should accept the 1024×1024 source upload and generate 256/512/1024 WebP/AVIF/JPEG variants on upload. Store variants next to the source:

```
/uploads/images/
  sermon-cover-uuid-1024.jpg   (source)
  sermon-cover-uuid-1024.webp
  sermon-cover-uuid-1024.avif
  sermon-cover-uuid-512.webp
  sermon-cover-uuid-512.avif
  sermon-cover-uuid-256.webp
  sermon-cover-uuid-256.avif
```

API responses should include URLs for the source + at least 256 and 512 variants.

---

## 11. Placeholder / Missing Image Policy

Never render a blank grey rectangle. For any missing image:

1. **Sermon cover missing** → Generate a 1:1 placeholder canvas using:
   - Background: `--paper` (#fbf8f3) or `--accent-burgundy` (#6f2e2a)
   - Text: sermon title (truncated), speaker, date
   - Font: heading serif at 48px equivalent
2. **Series artwork missing** → Use first sermon cover or generate series title card.
3. **Church logo missing** → Render text-only wordmark in heading serif.
4. **Hero photo missing** → Use latest sermon cover (Option A hero).

---

## 12. Accessibility Notes

- Every `<img>` must have meaningful `alt` text.
- Decorative background images (hero Option B) should use `aria-hidden="true"` and keep text in foreground HTML.
- Avoid text embedded inside images unless alt text reproduces the text exactly.
- Maintain color contrast ≥ 4.5:1 for any text on image overlays.

---

## 13. Performance Budget

| Surface | Total image weight target |
|---|---|
| Sermons list (first 10 cards) | ≤ 400 KB |
| Homepage hero | ≤ 200 KB |
| Sermon detail page | ≤ 250 KB |
| Whole page (above the fold) | ≤ 800 KB |

Use `loading="lazy"` for all images below the fold. Preload the hero image if it is the LCP element.
