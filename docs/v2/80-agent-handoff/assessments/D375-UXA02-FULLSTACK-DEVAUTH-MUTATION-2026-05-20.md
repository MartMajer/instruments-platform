# D375 - UXA02 full-stack development-auth mutation mission

Date: 2026-05-20

Status: done locally, runtime proof environment-blocked

## Assessment

D374 proved the boundary between fixture mode and full-stack mode, but it still could not attempt real local mutation. The harness needed a safe way to authenticate a browser to a local development API without Auth0 and without using staging or production.

The smallest useful mutation is study creation from the real Studies page:

- open `/app`
- navigate to Studies
- fill the visible `Study name` field
- click `Create study`
- stop only after reaching `/app/campaign-series/{id}/setup`

## Decision

Add explicit local development-auth support for full-stack autonomous runs:

- disabled by default
- opt-in through `--fullstack-dev-auth`
- injects development-auth headers into the Playwright browser context
- records only an enabled/disabled marker in evidence
- does not write tenant id, user id, or email into evidence observations
- remains local-only and refuses staging/production by the existing base-url guard

Do not fake a green mutation proof when the local API/database stack is absent.

## Task

Implemented:

- `--fullstack-dev-auth`
- `--fullstack-tenant-id`
- `--fullstack-user-id`
- `--fullstack-email`
- `--fullstack-permissions`
- `resolveFullstackDevAuthHeaders`
- evidence marker `fullstackDevAuth`
- `fullstack-create-study` full-stack-only mutation mission
- create-study actor logic for fill/click/created-setup-route stop
- docs and handoff updates

## Verification

Focused RED/GREEN:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/runner-options.test.ts scripts/ux-agent-audit/autonomous-fixtures.test.ts scripts/ux-agent-audit/autonomous-loop.test.ts scripts/ux-agent-audit/browser-safety.test.ts
```

Result: 4 files passed, 42 tests passed after the RED assertions failed on missing parser flags, mission contract, actor mutation sequence, and header builder.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 14 files passed, 97 tests passed.

Local browser proof:

- `artifacts/ux-agent-runs/local/run-2026-05-20T18-09-02-499Z/` - full-stack inspection, dev auth disabled, product read-model mocks disabled, blocked safely at workspace access
- `artifacts/ux-agent-runs/local/run-2026-05-20T18-09-25-783Z/` - fixture first-study setup, product read-model mocks enabled, completed
- `artifacts/ux-agent-runs/local/run-2026-05-20T18-09-37-309Z/` - full-stack create-study, dev auth enabled, product read-model mocks disabled, blocked at workspace access because local API was unavailable

Environment checks:

- `http://127.0.0.1:5055/health` was unreachable.
- `docker ps` failed because Docker Desktop was not running.

## Remaining risk

D375 proves the mutation mechanics in unit/loop tests and proves safe runtime failure with no local full-stack stack. It does not prove a green real database mutation on this workstation.

Next slice: add an owner-runnable local API/database bootstrap or preflight wrapper that starts or verifies the disposable local stack, then rerun `fullstack-create-study` until it reaches the created setup route.
