# D379 - UXA02 Action-File Actor Mode Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the provider-backed persona action path runnable from the UXA CLI without executing arbitrary external commands.

**Architecture:** Add an `action-file` actor mode that consumes local JSONL actions one step at a time through the provider actor adapter. The existing parser and visible-control validator remain mandatory before browser execution.

**Tech Stack:** TypeScript, Vitest, Playwright browser runner, local JSONL file input.

---

## Task 1: Action-file provider

**Files:**

- Create: `apps/web/scripts/ux-agent-audit/persona-action-file-provider.ts`
- Test: `apps/web/scripts/ux-agent-audit/persona-action-file-provider.test.ts`

**Steps:**

- Write failing tests for sequential line consumption and safe stop on exhausted file.
- Implement provider builder and file loader.

## Task 2: CLI and browser wiring

**Files:**

- Modify: `apps/web/scripts/ux-agent-audit/run.ts`
- Modify: `apps/web/scripts/ux-agent-audit/browser.ts`
- Modify: `apps/web/scripts/ux-agent-audit/types.ts`
- Test: `apps/web/scripts/ux-agent-audit/runner-options.test.ts`

**Steps:**

- Add `--actor-mode scripted|action-file`.
- Add `--persona-action-file`.
- Require a local file when actor mode is `action-file`.
- Wire browser capture to `buildProviderPersonaActionActor(loadPersonaActionFileProvider(...))`.

## Task 3: Verification and docs

**Files:**

- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`
- Add: `docs/v2/80-agent-handoff/assessments/D379-UXA02-ACTION-FILE-ACTOR-MODE-2026-05-20.md`

**Steps:**

- Run focused action-file/runner tests.
- Run full UXA suite.
- Run a local browser proof with a small JSONL action file.
- Document proof artifact and remaining full-stack Docker blocker.
