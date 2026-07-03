# ValidatedScale layout patterns

Reference compositions from the shipped product. Use these shapes when designing new ValidatedScale screens.

## Page anatomy

Topbar: graphite (`--color-ink`) bar, brand word in `.eyebrow` white, nav items with a 2px `--color-stain-bright` underline on the active tab. Content: centered column, max-width 74rem, `--color-ground` page background.

Page head: `.eyebrow` kicker (e.g. "PORTFOLIO", a date, a breadcrumb), then a large `.doc-title` headline — often a full sentence stating live state ("Nothing is collecting today."), then a `.datum` meta line in `--color-ink-3`.

## Protocol (document) pages

A study reads as a manuscript: numbered chapters (`01 Design`, `02 Instrument`…) with serif `.doc-title` headings, chapter number as small violet `.datum`. Left margin: a vertical `.rail-v` ruler with a sticky chapter list. Right margin: sticky action panel (`.panel` with `border-top: 3px solid var(--color-stain)`) holding the primary action and prerequisite list (violet-linked hints, left-border severity bars). Facts render as label/value rows separated by dashed hairlines: label in `--color-ink-3`, value in ink or `.datum`.

## Field (console) pages

The one dark surface. Full-bleed `--color-console` canvas. Stat board: a row of tiles on `--color-console-2` separated by 1px `--color-console-line`, each tile = `.eyebrow` label in `--color-console-dim`, big `.datum` value in `--color-console-ink`, small sub-line; the key tile carries an inset 3px top bar in `--color-chart-violet-dark`. Coverage meters: bullet bars with the k-threshold engraved as a 2px vertical tick; verdicts written as text ("reportable" / "below k=5"). A pulsing violet pip + "Collecting" marks live state.

## Portfolio / list rows

Rows separated by hairlines, never cards-in-grids. Row = serif `.doc-title` name (hover → violet), `.datum` meta line beneath (counts · dates), right-aligned `.chip` state and a violet text action ("Evidence →"). Group rows under `.eyebrow` section headings with an em-dash note ("IN THE FIELD — collecting now").

## Evidence (results) pages

Results read citable: a provenance `.datum` line under the title (n, scores visible, suppressed count, k threshold). Findings as proper tables: uppercase `.eyebrow`-style column headers, `.datum` right-aligned numerals, dashed row hairlines. Horizontal profile bars in `--color-chart-violet` with the value direct-labeled at the bar end; suppressed rows say "Suppressed — below reporting threshold" in italic `--color-ink-3`. Trends as per-row sparklines, single hue.

## Respondent (mobile) surfaces

One centered sheet (`.panel` with violet top border) on `--color-ground`. Consent first: serif study title, consent prose in `--color-ink-2`, an agree checkbox row that fills `--color-stain-wash` when checked. Items numbered with violet `.datum` codes; scale buttons are 44px squares that fill `--color-stain` when selected, anchors under the extremes in `--color-ink-3`. Sticky progress: one thin tick per item, filled ticks in violet, count as `.datum`.
