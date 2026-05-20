# D381 UXA02 full-stack bootstrap mutation proof plan

## Assessment

D380 completed the safe live local action provider path, but the full-stack local mutation proof was still unproven. Once Docker Desktop started, the existing bootstrap path exposed three concrete blockers:

- relative `--repo-root ..\..` was passed through to PowerShell from `apps/web`, so `start-local-staging.ps1` was not found
- the bootstrap ran preflight immediately after `docker compose up -d`, before the API was consistently ready
- the existing local `.env` had dev-auth disabled, so preflight returned 401 despite `--fullstack-dev-auth`

The harness should solve those without editing `.env`, dumping secrets, or creating a second local stack.

## Acceptance criteria

- `fullstack-bootstrap --start --repo-root ..\..` resolves the repo root to an absolute path before invoking scripts.
- When `--fullstack-dev-auth` is present, the bootstrap starts Compose with process env overrides for local dev auth.
- Bootstrap retries full-stack preflight after startup until ready or attempt exhaustion.
- Full-stack preflight reaches `ready` against the local Docker stack.
- `fullstack-create-study` creates a real local study and ends on `/app/campaign-series/{id}/setup`.
- The successful run records full-stack mode with product read-model mocks disabled.

## Verification

- RED focused tests failed for relative repo-root normalization, preflight retry, dev-auth env override, and stale snapshot URL after create-study redirect.
- GREEN focused tests passed: `fullstack-bootstrap.test.ts` and `autonomous-loop.test.ts`, 17/17 tests.
- Full UXA suite passed: 18/18 files, 124/124 tests.
- Real bootstrap proof passed with Docker Desktop running:
  - `fullstack-bootstrap --start --repo-root ..\.. --fullstack-dev-auth`
  - status `ready`
  - API health HTTP 200
  - dev-auth session HTTP 200
  - tenant study read model HTTP 200
- Real browser mutation proof passed:
  - artifact `artifacts/ux-agent-runs/local/run-2026-05-20T18-54-36-933Z/`
  - status `completed`
  - final URL `/app/campaign-series/019e46bd-6d56-7d30-807b-adcc0caaa475/setup`
  - findings `0`
  - next-action tickets `0`

## Remaining risk

The local Docker stack contains accumulated synthetic QA smoke data. That is acceptable for local-only harness proof, but future work should add a disposable reset/seed command if each persona run needs a clean database.
