# D376 - UXA02 full-stack preflight command

Date: 2026-05-20

Status: done locally

## Assessment

D375 added the browser-side full-stack mutation mission, but runtime proof stayed blocked because the workstation had no local API listening at `127.0.0.1:5055`. Letting the browser discover that indirectly produces a confusing workspace-access screen. The harness needed a direct preflight gate that separates environment readiness from product UX findings.

## Decision

Add a local-only `fullstack-preflight` CLI subcommand that checks the required full-stack prerequisites before running mutation missions:

- API health
- development-auth session
- tenant study read model

The preflight fails closed and skips dependent checks after the first failure.

## Task

Implemented:

- `fullstack-preflight` CLI subcommand
- `checkFullstackPreflight`
- shared development-auth header helper
- parser coverage for preflight flags
- focused tests for API-down, dev-auth-rejected, and ready paths
- docs and handoff updates

## Verification

Focused tests:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/fullstack-preflight.test.ts scripts/ux-agent-audit/runner-options.test.ts scripts/ux-agent-audit/browser-safety.test.ts
```

Result: 3 files passed, 31 tests passed.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 15 files passed, 101 tests passed.

Real workstation preflight:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-preflight --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth
```

Result:

- status: `blocked`
- `api-health`: failed, `fetch failed`
- `dev-auth-session`: skipped
- `tenant-study-read-model`: skipped

## Remaining risk

The harness now reports local full-stack readiness clearly, but this workstation still cannot prove a green real mutation until the local API/database stack is running and seeded.

Next slice: start or document the owner-runnable local API/database bootstrap path, then rerun preflight and `fullstack-create-study`.
