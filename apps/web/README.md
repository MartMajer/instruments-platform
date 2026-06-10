# Instruments Platform Web

SvelteKit frontend for the tenant setup workspace.

## Development

From the repository root:

```powershell
npm install --prefix apps/web
$env:PUBLIC_DEV_AUTH_ENABLED='true'
$env:PUBLIC_AUTH_LOGIN_URL='/auth/login'
$env:PUBLIC_AUTH_LOGOUT_URL='/auth/logout'
npm run dev --prefix apps/web -- --host 127.0.0.1
```

```bash
PUBLIC_DEV_AUTH_ENABLED=true \
PUBLIC_AUTH_LOGIN_URL='/auth/login' \
PUBLIC_AUTH_LOGOUT_URL='/auth/logout' \
PUBLIC_API_BASE_URL='http://localhost:5055' \
npm run dev --prefix apps/web -- --host 127.0.0.1
```

If you keep local toolchain shims in `tools/frameworks/bin`, run:

```bash
source tools/frameworks/activate.sh
```

Useful optional environment variables:

```powershell
$env:PUBLIC_API_BASE_URL='http://localhost:5055'
$env:PUBLIC_DEV_TENANT_ID='11111111-1111-4111-8111-111111111111'
$env:PUBLIC_DEV_USER_ID='22222222-2222-4222-8222-222222222222'
$env:PUBLIC_DEMO_SURFACES_ENABLED='true'
```

```bash
export PUBLIC_API_BASE_URL='http://localhost:5055'
export PUBLIC_DEV_TENANT_ID='11111111-1111-4111-8111-111111111111'
export PUBLIC_DEV_USER_ID='22222222-2222-4222-8222-222222222222'
export PUBLIC_DEMO_SURFACES_ENABLED='true'
```

The setup workspace checks `/auth/session` before showing tenant controls. The dev auth headers are only sent when `PUBLIC_DEV_AUTH_ENABLED=true`. `PUBLIC_AUTH_LOGIN_URL` and `PUBLIC_AUTH_LOGOUT_URL` are provider-neutral placeholders until Q-020 chooses the production provider-specific auth path.

`PUBLIC_DEMO_SURFACES_ENABLED=true` enables `/app/demo` fixture-backed frontend planning states. Keep it off by default outside local/dev use; demo fixtures are labelled as demo data and do not prove backend semantics.

## Container Runtime

The web app now builds with `@sveltejs/adapter-node` for the DEP01-A local Docker proof. The container entry point runs the SvelteKit Node server on `PORT=3000`; `deploy/staging/docker-compose.yml` maps it to the local host port from `WEB_HTTP_PORT`.

Runtime `PUBLIC_*` values are still treated as public frontend configuration. Do not put secrets there. The local staging package intentionally uses development auth until Q-020 chooses and implements the production login/callback/session-store path.

## Checks

```powershell
npm run lint --prefix apps/web
npm run check --prefix apps/web
npm run test --prefix apps/web -- --run
npm run build --prefix apps/web
npm run check:bundles --prefix apps/web
```

`check:bundles` expects a fresh production build. It prints the route-owned
bundle budget table and writes the ignored
`artifacts/bundle-budgets/report.json` artifact for inspection.
