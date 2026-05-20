# Mobile Responsive Cleanup Design

Date: 2026-05-20

## Goal

Make the current private-beta app usable on phone and narrow tablet widths without redesigning the full mobile navigation model.

## Scope

This is a bounded responsive cleanup pass. It targets shared layout primitives first:

- app shell and sidebar behavior below desktop width;
- product navigation density;
- selected-study route navigation;
- setup/collection/results/waves path cards;
- common panels, action bars, forms, record grids, tables, and summary rows;
- long text, IDs, emails, and technical values that can force horizontal overflow.

## Non-goals

- No full mobile drawer or hamburger redesign.
- No new routes.
- No product copy rewrite except where spacing/wrapping requires it.
- No API/backend changes.
- No public marketing redesign.

## Design

Below `1024px`, the sidebar becomes a compact top navigation region instead of a tall desktop rail. Product nav sections flow horizontally with safe overflow so users can reach content quickly. Below phone widths, links and cards reduce copy density, action buttons become full-width where appropriate, and wide grids collapse to one column.

The selected study nav keeps its visual identity, but stops trying to fit many cards across small screens. Workflow path cards become horizontally scrollable on narrow tablets and single-column cards on phones. Technical and record fields use `overflow-wrap:anywhere` so UUIDs, emails, and generated names cannot break the viewport.

## Acceptance criteria

- No normal app route should require horizontal page scrolling at `360px`.
- Sidebar navigation should not consume the first full screen on mobile.
- Primary actions remain visible and tappable with at least comfortable touch height.
- Setup, Collection, Results, and Waves workflow cards remain readable on phone widths.
- Tables and dense data areas scroll inside their own containers rather than breaking the page.
