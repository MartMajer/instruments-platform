# instruments-platform

Working repository for the v2 Instruments Platform.

## Structure

- `docs/v2/` - product, architecture, roadmap, and handoff docs.
- `apps/web/` - SvelteKit frontend tenant setup workspace.
- `src/` - .NET 9 backend source.
- `tests/` - .NET and frontend automated tests.
- `deploy/` - local and future deployment infrastructure.

## Local Setup

Install frontend dependencies and restore local .NET tools:

```powershell
npm install --prefix apps/web
dotnet tool restore
```

Start local Postgres, apply migrations, and seed the local development tenant:

```powershell
.\deploy\local\setup-dev-db.ps1
```

The script uses tenant id `11111111-1111-4111-8111-111111111111`, matching the frontend development auth headers. If Docker was just installed and `docker` is not recognized, restart the shell so Docker Desktop updates `PATH`; the script also checks Docker Desktop's default install path.

Manual migration command, if needed:

```powershell
$env:PLATFORM_DESIGN_TIME_CONNECTION='Host=localhost;Port=5432;Database=instruments_platform_dev;Username=platform_app;Password=platform_app_dev'
dotnet ef database update --project src/Platform.Infrastructure/Platform.Infrastructure.csproj --startup-project src/Platform.Infrastructure/Platform.Infrastructure.csproj
```

Start the API:

```powershell
dotnet run --project src/Platform.Api
```

Start the worker in a separate shell when you want the outbox relay scheduler
to process due rows locally:

```powershell
dotnet run --project src/Platform.Workers
```

Start the frontend in a second shell:

```powershell
$env:PUBLIC_DEV_AUTH_ENABLED='true'
$env:PUBLIC_AUTH_LOGIN_URL='/auth/login'
$env:PUBLIC_AUTH_LOGOUT_URL='/auth/logout'
npm run dev --prefix apps/web -- --host 127.0.0.1
```

The frontend defaults to `http://localhost:5055`, matching the API HTTP launch profile. Override with `PUBLIC_API_BASE_URL` if needed. `PUBLIC_AUTH_LOGIN_URL` and `PUBLIC_AUTH_LOGOUT_URL` are provider-neutral placeholders until Q-020 chooses the production Auth0/Keycloak path.

## Local Docker Deployment Proof

DEP01-A adds a local staging-shaped Docker package for proving the current tenant-private path in containers:

```powershell
.\deploy\staging\start-local-staging.ps1
.\deploy\staging\smoke-local-staging.ps1
.\deploy\staging\stop-local-staging.ps1 -RemoveVolumes
```

The stack runs Postgres, a one-shot EF migrator, a one-shot seed step, the API, and the SvelteKit Node web server. It uses development auth defaults from `deploy/staging/env.example` and is not a production deployment, VPS runbook, TLS setup, backup plan, or final auth implementation.

For portable VPS staging adaptation, see `docs/v2/40-ops/vps-staging-runbook.md`. The VPS package uses subdomain routing through nginx and keeps API/web services bound to loopback host ports.

## Verification

```powershell
dotnet build Platform.slnx
dotnet test tests/Platform.UnitTests/Platform.UnitTests.csproj
dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj
npm run lint --prefix apps/web
npm run check --prefix apps/web
npm run test --prefix apps/web -- --run
npm run build --prefix apps/web
node --test tests/license-gate/check-licenses.test.mjs
node tools/check-licenses.mjs
```

The license gate writes attribution artifacts to `artifacts/dependency-licenses/`; that directory is generated output and is git-ignored.
