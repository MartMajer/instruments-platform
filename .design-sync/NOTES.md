# design-sync notes — instruments-platform

- 2026-07-03: First sync. Product frontend is **SvelteKit (Svelte 5)** at `apps/validatedscale` — components cannot be converted for the React design runtime; owner chose a styles/tokens/guidelines-only sync ("push it how we can"). Bundle is hand-assembled in `ds-bundle/` (gitignored? currently untracked): tokens converted from Tailwind v4 `@theme` in `apps/validatedscale/src/app.css` to `:root` custom properties; vocabulary classes copied verbatim; fonts from the app's @fontsource node_modules (latin + latin-ext woff2).
- Keep `ds-bundle/tokens/tokens.css` and `styles.css` in lockstep with `apps/validatedscale/src/app.css` — that file is the source of truth (console tokens were lightened 2026-07-03; chart colors are dataviz-validated).
- No `_ds_sync.json` uploaded (off-script, honest omission) — re-syncs re-verify and re-upload everything; the set is small.
- If components ever need to ship: they would have to be React reimplementations, which the owner has not asked for and the skill's ship-what-they-built principle discourages. Revisit only on explicit request.
