# Spectra — the rebuilt frontend

A from-zero SvelteKit frontend for the platform, built against the existing API contracts (`src/lib/api/` is the tested client layer carried over from `apps/web`; everything visual is new). It runs **side by side** with the old app: same backend, different port. Nothing in `apps/web` was touched.

## Design language

"The instrument and the manuscript": app chrome behaves like a precision instrument (graphite ink, engraved tick rails, IBM Plex Mono data); study content reads like a research manuscript (Source Serif 4 titles and prose, Archivo UI). One identity accent — stain violet `#4530a6`, after the laboratory stains that make invisible structures visible. Field view is the app's only dark surface: a live console. Chart colors are validated for CVD separation and contrast (light trio `#08569a/#008065/#5b46c8`, dark meter fill `#8673ee`).

## Screens

- `/` landing, `/signin`, `/register` (same OIDC + registration flow as apps/web)
- `/app` Today — live state summary, needs-attention queue, workspace counters
- `/app/studies` portfolio → `/app/studies/[id]` **Protocol** (manuscript with chapter rail, launch check) → `/field` **dark console** (stat board, k-threshold coverage meters, waves, 20s live polling) → `/evidence` (score profile, findings table, across-waves matrix, exports)
- `/app/instruments` (tenant-provided imports), `/app/people`, `/app/exports`, `/app/settings`
- `/r/[token]` respondent flow, mobile-first, en + hr: consent sheet → item runner (likert/nps, single, multi, number, date, comment) with tick-rail progress and draft autosave → submit; `/r/[token]/unsubscribe`

## Run (side by side with apps/web)

```bash
cd apps/spectra
PUBLIC_DEV_AUTH_ENABLED=true PUBLIC_API_BASE_URL=http://localhost:5055 npm run dev   # port 5174
```

The API's dev CORS list must include the port you run on (`src/Platform.Api/appsettings.Development.json` → `AllowedOrigins`; 5173 and 5174 are both listed — restart the API after changing it). Real auth (no dev headers) works the same as apps/web: unset `PUBLIC_DEV_AUTH_ENABLED`.

Checks: `npm run check` (svelte-check), `npm run build`, `npm run test:unit`.

## Deliberately not ported yet

Matrix/ranking question types (fall back to a text field with a note), the designer/template builder UI, Microsoft Graph directory import screens, tenant email template editing, withdrawal admin. The old app remains the surface for those until this one covers them.
