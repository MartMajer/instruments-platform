# D359 - Mobile auth shell regression assessment - 2026-05-20

## Status

Fixed and deployed to staging.

## Assessment

Owner testing exposed two regressions after D358:

- `/app` on mobile could render only the bottom navigation.
- `/signin` and `/register` still used the old desktop-first registration composition, leaving the actual form too far down the mobile page.

The `/app` failure was a real Svelte runtime crash, not just CSS. On `/app`, the mobile bottom-nav item for `Studies` and the inactive fallback `Study` item both resolved to `/app/campaign-series`. Because the keyed each block used `item.href`, Svelte raised `each_key_duplicate` during hydration and aborted most of the shell render.

The auth-entry issue was layout priority. The pages were technically responsive but put explanatory copy and step cards before the form on phones. That made the mobile task path feel untouched and blocked.

## Fix

- Added stable `id` keys for mobile bottom-nav items.
- Removed the inactive `Study` mobile bottom-nav item until a campaign series is selected.
- Changed the mobile bottom nav from fixed five-column grid to flexible equal-width items.
- Added mobile overrides for `/signin` and `/register` so the form appears first, with compact navigation and reduced explainer card weight below it.

## Verification

- Local web production build passed with the existing large-chunk warning.
- Local production-preview mobile render smoke passed for `/signin`, `/register`, and `/app` at 390px width.
- `git diff --check` passed with only CRLF warnings.
- Staging deployed app commit: `1ae5a09`.
- Initial VPS redeploy rebuilt the web image but hit a transient web-root 502 during container warm-up.
- Follow-up checks returned API ready 200, web root 200, and `/app` 200.
- Follow-up VPS release checks passed at `/tmp/d359-mobile-auth-shell-regression-vps-release-20260520-followup`.
- Staging browser mobile render smoke passed for `/signin`, `/register`, and `/app` at 390px width with no page errors, no horizontal overflow, auth forms at `y=100`, `/app` mobile topbar present, `/app` body present, and bottom nav labels `Home Studies Directory More`.

## Remaining risk

Authenticated `/app` with a real owner session still needs owner visual judgment on device. The unauthenticated shell crash is fixed at the root cause and verified against staging.
