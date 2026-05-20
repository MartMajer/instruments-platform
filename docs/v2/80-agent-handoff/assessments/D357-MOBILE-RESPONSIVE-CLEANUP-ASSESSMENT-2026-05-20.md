# D357 - Mobile Responsive Cleanup Assessment

Date: 2026-05-20

## Assessment

The current app had received substantial desktop product cleanup, but the shared layout primitives still behaved like desktop-first surfaces on narrow screens. The biggest risk was not one broken page; it was repeated horizontal overflow and content displacement from the app shell, sidebar navigation, selected-study route cards, workflow path cards, action rows, record fields, and dense data areas.

The owner explicitly scoped this as a first responsive cleanup pass, not the larger mobile navigation redesign. The right move was therefore shared CSS hardening rather than page-by-page redesign or a new drawer/hamburger system.

## Completed task

Added a mobile-aware CSS override layer in `apps/web/src/routes/app.css`.

Changed behavior:

- Below desktop width, the app sidebar becomes a compact sticky top region.
- Product navigation becomes horizontally scrollable instead of consuming a tall vertical rail before the page content.
- Selected-study route navigation becomes horizontally scrollable on narrow screens.
- Setup, Collection, Results, and Waves path cards become horizontally scrollable on narrow tablets and stack cleanly on phones.
- Common action rows, panel headers, record fields, provenance strips, and technical values collapse or wrap safely.
- Long UUIDs, emails, generated names, and technical values use overflow-safe wrapping.
- Dense table wrappers receive local horizontal scrolling instead of forcing page-level overflow.
- Phone widths reduce low-value nav descriptions and icon columns to keep tap targets usable.

## Verification

Passed:

- Web production build for CSS/Svelte compilation.
- `git diff --check` with only CRLF normalization warnings.

## Remaining risk

- This is not the full mobile navigation redesign. A later goal should still evaluate a real mobile drawer/bottom-nav pattern, route-specific mobile information architecture, and screenshots across target breakpoints.
- This pass does not visually inspect every route on real devices.
- Vite still reports the existing large chunk warning during production build.
