# ValidatedScale design language

**What this project is:** tokens, fonts, and CSS vocabulary for ValidatedScale — a research-grade survey/psychometrics platform (studies run as Protocol → Field → Evidence). The product app is SvelteKit, so there are **no importable React components here**: build with generic components and style them **only** with the classes and `var(--*)` tokens below. All of them exist in `styles.css`'s import closure.

## Setup

No provider or wrapper is needed. Give the page `background: var(--color-ground)`; body text is Archivo at 15px (inherited from `html`). Content columns are centered, max-width ~74rem, padded 1.5rem.

## The idiom — "the instrument and the manuscript"

- **Light porcelain surfaces by default.** Panels are `.panel` (white, 1px `var(--color-line)` hairline, 4px radius). Never use drop shadows for decoration; structure comes from hairlines and tone (`--color-sunk` for recessed areas).
- **Three type roles, never mixed:** `.doc-title` (Source Serif 4 — study names, page titles, section headings of document-like pages), `.eyebrow` (Archivo expanded caps — section labels, kickers; `.eyebrow-stain` for the violet variant), `.datum` (IBM Plex Mono, tabular numerals — every count, code, ID, timestamp, tick label). Plain UI text is Archivo by inheritance.
- **One identity accent:** stain violet `var(--color-stain)` (`#4530a6`) for links, selected states, key actions (`.btn-stain`), and live-study emphasis. Hover/pressed goes `--color-stain-deep`; washes/borders use `--color-stain-wash`/`--color-stain-line`. Primary neutral buttons are `.btn-ink`; secondary are `.btn-ghost`.
- **No warm decorative colors.** Amber/red exist only as reserved status semantics: `.chip-live` (teal-green, collecting/ok), `.chip-stain` (violet, informational identity), `.chip-warn`, `.chip-danger`. Status is never conveyed by color alone — chips always carry a text label.
- **Signature motif — the calibrated rail:** `.rail-h` / `.rail-v` draw an engraved tick track (tune `--rail-pitch`, `--rail-ink`). Use it for wave timelines, progress, margin rulers — anywhere measurement is the message. Don't use it as random decoration.
- **Dark is an exception, not a theme.** The `--color-console*` tokens (lifted graphite `#242932`…) are only for live-monitoring surfaces (a "Field console"): stat tiles on `--color-console-2`, hairlines `--color-console-line`, text `--color-console-ink`/`--color-console-dim`, accent `--color-stain-bright` or `--color-chart-violet-dark`. Everything else stays light.
- **Charts:** single-hue marks in `var(--color-chart-violet)` on light (`--color-chart-violet-dark` on console), series pair `--color-series-1`/`--color-series-2`. Thin marks, direct value labels in `.datum`, suppressed values shown as labeled hollow/dashed marks — this product suppresses anything under a k-anonymity threshold and says so in words.

## Where the truth lives

Read `styles.css` (imports `tokens/tokens.css` and `fonts/fonts.css`) before styling. `guidelines/design-language.md` has layout patterns (protocol document with chapter rail, field console board, portfolio rows).

## Snippet

```html
<div class="panel" style="padding:1.25rem; border-top:3px solid var(--color-stain)">
  <span class="eyebrow">Launch check</span>
  <h2 class="doc-title" style="font-size:1.375rem">Nurse workload, wave 2</h2>
  <p style="color:var(--color-ink-2)">Collecting since <span class="datum">3 Jul, 13:07</span></p>
  <span class="chip chip-live">Collecting</span>
  <button class="btn btn-stain">Open field</button>
</div>
```

---

## Files

- `styles.css` — entry; imports tokens and fonts (the full closure designs receive)
- `tokens/tokens.css` — all custom properties
- `fonts/fonts.css` + `fonts/*.woff2` — Archivo Variable, Source Serif 4 Variable, IBM Plex Mono 400/500
- `guidelines/design-language.md` — layout patterns per surface (protocol, console, portfolio, evidence, respondent)

**Scope note:** the product's components are Svelte and are not synced (this tool renders React). This project carries the design language only; treat the product app `apps/validatedscale` as the component source of truth.
