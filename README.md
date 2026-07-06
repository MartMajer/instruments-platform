# instruments-platform

Working repository for **ValidatedScale** (v2 Instruments Platform): a validated-instrument engine for research-grade measurement — tenant-provided instruments plus a rights-cleared free gallery, EU-hosted, GDPR-native, en+hr.

## Structure

- `apps/validatedscale/` — **the product frontend** (SvelteKit, port 5174 in dev). Researcher app, landing page, and the mobile respondent runner.
- `apps/web/` — the previous frontend. **Retired reference code, read-only**; used as a behavior checklist, never edited.
- `src/` — .NET 9 backend: `Platform.Api` (HTTP, port 5055), `Platform.Application` (vertical slices/CQRS-lite), `Platform.Domain`, `Platform.Infrastructure` (EF Core/Postgres, RLS tenancy, outbox/audit), `Platform.Workers` (outbox relay), `Platform.SharedKernel`.
- `tests/` — .NET unit + integration tests, license-gate tests. Frontend unit/E2E tests live in `apps/validatedscale/`.
- `deploy/` — `local/` dev Postgres, `staging/` VPS packaging. Deploys are owner-run.
- `docs/` — product, architecture, and handoff docs (**a separate git repository**; orient from `docs/v2/80-agent-handoff/STATE.md`).
- `tools/` — license gate and framework scripts.

## Local development

One-time setup — Postgres, migrations, and the dev tenant/user seed:

```bash
npm install --prefix apps/validatedscale
dotnet tool restore
# Windows: .\deploy\local\setup-dev-db.ps1
# Linux/macOS equivalent:
docker compose -f deploy/local/docker-compose.yml up -d
PLATFORM_DESIGN_TIME_CONNECTION='Host=localhost;Port=5432;Database=instruments_platform_dev;Username=platform_app;Password=platform_app_dev' \
  dotnet ef database update --project src/Platform.Infrastructure --startup-project src/Platform.Infrastructure
docker exec -i local-postgres-1 psql -U platform_app -d instruments_platform_dev < deploy/local/seed-dev.sql
```

The dev tenant id is `11111111-1111-4111-8111-111111111111` and the dev user is `dev@local.test` (`22222222-2222-4222-8222-222222222222`), matching the frontend development auth headers.

Run the API (and optionally the worker for outbox processing):

```bash
dotnet run --project src/Platform.Api        # http://localhost:5055
dotnet run --project src/Platform.Workers    # optional, separate shell
```

Run the frontend:

```bash
cd apps/validatedscale
PUBLIC_DEV_AUTH_ENABLED=true PUBLIC_API_BASE_URL=http://localhost:5055 npm run dev   # http://localhost:5174
```

Dev CORS allows ports 5173 and 5174 (`src/Platform.Api/appsettings.Development.json` → `AllowedOrigins`; restart the API after changing it). Without `PUBLIC_DEV_AUTH_ENABLED` the app uses the real OIDC flow, same as `apps/web` did.

## Verification

```bash
dotnet build Platform.slnx
dotnet test tests/Platform.UnitTests/Platform.UnitTests.csproj
dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj
npm run check --prefix apps/validatedscale
npm run test:unit --prefix apps/validatedscale -- --run
npm run test:e2e --prefix apps/validatedscale   # needs the local API + seeded dev DB
npm run build --prefix apps/validatedscale
node --test tests/license-gate/check-licenses.test.mjs
node tools/check-licenses.mjs
```

Notes:

- Playwright manages its own web servers (a dev-auth server on 5173 and an auth-less one on 5174 for anonymous-visitor specs).
- Database-backed integration tests (Testcontainers) are opt-in: `RUN_POSTGRES_INTEGRATION_TESTS=1` with Docker running.
- The license gate scans both frontend lockfiles and the .NET solution, and writes attribution artifacts to `artifacts/dependency-licenses/` (git-ignored).

## Staging

Staging runs on a VPS via `deploy/staging/` (nginx subdomain routing; the `web` service builds `apps/validatedscale`). Deployment is **owner-run** (`deploy/staging/redeploy-vps-stack.sh`); testing is staging-first — commits land with announced hashes and any EF migrations flagged loudly. See `docs/v2/40-ops/vps-staging-runbook.md`.
