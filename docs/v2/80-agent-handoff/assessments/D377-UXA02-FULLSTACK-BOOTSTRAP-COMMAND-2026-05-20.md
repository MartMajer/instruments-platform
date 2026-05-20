# D377 - UXA02 full-stack bootstrap command

Date: 2026-05-20

Status: done locally, Docker-blocked runtime proof

## Assessment

D376 made local full-stack readiness explicit, but it still required the owner or next agent to know which staging scripts to run. The repository already has the correct local staging bootstrap path:

- `deploy/staging/start-local-staging.ps1`
- `deploy/staging/smoke-local-staging.ps1`
- `deploy/staging/docker-compose.yml`
- `deploy/staging/env.example`

UXA02 should reuse that path rather than inventing a parallel seed/bootstrap mechanism.

## Decision

Add a UXA02 `fullstack-bootstrap` command that:

- checks Docker Engine readiness using `docker info`
- does not start Compose unless `--start` is explicitly provided
- invokes `deploy/staging/start-local-staging.ps1` only in `--start` mode
- runs `fullstack-preflight` after start/no-start readiness
- prints the exact local staging, smoke, preflight, and mutation commands

## Task

Implemented:

- `apps/web/scripts/ux-agent-audit/fullstack-bootstrap.ts`
- `apps/web/scripts/ux-agent-audit/fullstack-bootstrap.test.ts`
- `parseFullstackBootstrapOptions`
- `fullstack-bootstrap` CLI subcommand
- docs and handoff updates

The Docker readiness check uses `docker info --format {{.ServerVersion}}`, not `docker --version`, so it catches Docker Desktop/Engine down states.

## Verification

Focused tests:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/fullstack-bootstrap.test.ts scripts/ux-agent-audit/runner-options.test.ts
```

Result: 2 files passed, 28 tests passed.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 16 files passed, 107 tests passed.

Real workstation command:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-bootstrap --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth --repo-root ..\..
```

Result:

- status: `blocked`
- `docker`: failed
- detail: Docker command is unavailable or Docker Desktop is not running

## Remaining risk

This closes the "which command do I run?" gap. It still does not prove green full-stack mutation because Docker Desktop/Engine is not running on the workstation.

Next slice: with Docker Desktop running, run `fullstack-bootstrap --start`, then rerun `fullstack-create-study` and capture a green mutation artifact.
