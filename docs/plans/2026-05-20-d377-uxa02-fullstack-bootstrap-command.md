# D377 - UXA02 Full-Stack Bootstrap Command Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add an owner-runnable UXA02 command that points the harness at the project's existing local staging bootstrap path and fails before browser mutation when Docker/API prerequisites are missing.

**Architecture:** Reuse `deploy/staging/start-local-staging.ps1` and `deploy/staging/smoke-local-staging.ps1`; do not create a parallel database bootstrap. Add a Node-side UXA wrapper that checks Docker Engine readiness, optionally invokes the existing start script, then runs the existing `fullstack-preflight`.

**Tech Stack:** Node/TypeScript CLI under `apps/web/scripts/ux-agent-audit`, Vitest, existing PowerShell staging scripts, Docker Compose.

---

## Task 1: Bootstrap report and command builder

**Files:**

- Create: `apps/web/scripts/ux-agent-audit/fullstack-bootstrap.ts`
- Test: `apps/web/scripts/ux-agent-audit/fullstack-bootstrap.test.ts`

**Steps:**

- Write failing tests for Docker unavailable, start requested, and no-start guidance.
- Implement bootstrap report types and command builder.
- Require Docker Engine readiness via `docker info`, not only Docker CLI presence.
- Return exact start/smoke/preflight/mutation commands.

## Task 2: CLI wiring

**Files:**

- Modify: `apps/web/scripts/ux-agent-audit/run.ts`
- Test: `apps/web/scripts/ux-agent-audit/runner-options.test.ts`

**Steps:**

- Write failing parser tests for `fullstack-bootstrap`.
- Add `--repo-root` and `--start`.
- Keep `--start` opt-in so the default command is non-destructive.
- Wire the subcommand to `runFullstackBootstrap`.

## Task 3: Verification and docs

**Files:**

- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`
- Add: `docs/v2/80-agent-handoff/assessments/D377-UXA02-FULLSTACK-BOOTSTRAP-COMMAND-2026-05-20.md`

**Steps:**

- Run focused Vitest for bootstrap and runner parser.
- Run full UXA suite.
- Run `fullstack-bootstrap` without `--start` on the workstation and record blocked output.
- Document that Docker Engine is the current blocker.
