# UXA02 Full-Stack Persona Harness Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a safe full-stack data-mode boundary and persona action-driver contract to the local UX audit harness.

**Architecture:** Keep UXA01 deterministic fixture mode as the default. Add an explicit data-mode contract so fixture read-model mocks cannot be mistaken for full-stack proof, and add a strict JSON action-driver protocol that can later be backed by an LLM or Codex persona loop.

**Tech Stack:** TypeScript, Vitest, Playwright, existing UXA01 evidence/report tooling.

---

### Task 1: Data-mode contract

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/types.ts`
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.ts`
- Modify: `apps/web/scripts/ux-agent-audit/run.ts`
- Modify: `apps/web/scripts/ux-agent-audit/browser.ts`
- Test: `apps/web/scripts/ux-agent-audit/runner-options.test.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.test.ts`

**Steps:**
1. Write failing tests requiring autonomous runner options to parse `--data-mode fixture|fullstack`.
2. Write failing tests requiring fixture-only missions to reject `fullstack`.
3. Add `AutonomousDataMode`.
4. Add mission `supportedDataModes`.
5. Wire data mode into browser capture.
6. Ensure `fullstack` mode does not install fixture product route mocks.
7. Run focused tests.

### Task 2: Persona action-driver protocol

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/persona-action-driver.ts`
- Test: `apps/web/scripts/ux-agent-audit/persona-action-driver.test.ts`

**Steps:**
1. Write failing tests requiring a sanitized action request from a persona context.
2. Write failing tests requiring raw/fenced JSON action parsing.
3. Write failing tests rejecting malformed, unknown, or unsafe action output shapes.
4. Implement the minimal parser/request builder.
5. Run focused tests.

### Task 3: Docs and handoff

**Files:**
- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Create: `docs/v2/80-agent-handoff/assessments/D374-UXA02-FULLSTACK-PERSONA-HARNESS-BOUNDARY-2026-05-20.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
1. Document fixture versus fullstack data mode.
2. Document persona action-driver boundary and remaining seed/reset work.
3. Run focused UXA01 tests.
4. Run full UXA01 tests.
5. Commit the slice.
