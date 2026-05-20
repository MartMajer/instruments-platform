# D370 Autonomous Local UX Persona Harness Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a local-only autonomous UX persona audit harness that can drive the local app through safe visible actions, use deterministic local fixture states, capture evidence, and emit ticket-ready persona findings.

**Architecture:** Keep the D369 browser transcript foundation and add a bounded autonomous layer above it. A scripted persona actor chooses only structured safe actions against visible UI state; the Playwright adapter executes those actions locally, captures rich snapshots after every step, and writes evidence/report artifacts. Deterministic missions enter the normal `/app` product shell and use local product read models seeded from fixture scenarios instead of staging/prod or real user data. `/app/demo` is a fixture catalog/debug aid only, not the primary audit target.

**Tech Stack:** TypeScript, Vitest, Playwright, existing UXA01 evidence/report/prompt tooling, existing Svelte product fixture catalog.

---

### Task 1: Safe action schema and validation

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/autonomous-actions.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-actions.test.ts`

**Steps:**
1. Write RED tests for safe visible-control actions.
2. Implement `validateAgentActionAgainstSnapshot` for `goto`, `click-link`, `click-button`, `fill`, `complain`, and `stop`.
3. Reject external URLs, query/fragment navigation, missing controls, disabled buttons, and missing fields.
4. Run focused Vitest for action tests.

### Task 2: Fixture-backed autonomous mission catalog

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.test.ts`

**Steps:**
1. Write RED tests that require at least three autonomous missions: setup, wave/results, questionnaire/scoring.
2. Reuse the existing product fixture catalog by relative import, preserving local/dev-only provenance.
3. Expose mission entry path, target product paths, fixture provenance, persona id, viewport, and expected review focus.
4. Run focused Vitest.

### Task 3: Autonomous loop and scripted persona actor

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/autonomous-loop.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-loop.test.ts`

**Steps:**
1. Write RED tests using a fake page adapter.
2. Implement loop: capture entry snapshot, ask actor for action, validate action, execute safe action, capture next snapshot, repeat until complete, stop, complain, or blocked.
3. Emit action log, persona findings, visited product paths, local-only policy, and transcript snapshots.
4. Run focused Vitest.

### Task 4: Browser and CLI integration

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/browser.ts`
- Modify: `apps/web/scripts/ux-agent-audit/run.ts`
- Modify: `apps/web/scripts/ux-agent-audit/types.ts`
- Test: `apps/web/scripts/ux-agent-audit/runner-options.test.ts`

**Steps:**
1. Add RED CLI tests for `autonomous` subcommand and local-only URL validation reuse.
2. Add browser adapter methods for `gotoPath`, `clickLink`, `clickButton`, `fillField`, and `captureSnapshot`.
3. Add `runAutonomousAudit` that writes evidence, transcript, review prompt, and normalized report with generated persona findings.
4. Keep current observe mode unchanged.
5. Run focused UXA01 Vitest.

### Task 5: Docs, assessment, and local proof

**Files:**
- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`
- Create: `docs/v2/80-agent-handoff/assessments/D370-UXA01-AUTONOMOUS-LOCAL-PERSONA-HARNESS-2026-05-20.md`

**Steps:**
1. Document autonomous local mode, safety wall, fixture-state boundary, and next gaps.
2. Run focused Vitest.
3. Start local web and run all three autonomous missions against `http://127.0.0.1:5174/app`.
4. Assert artifacts include transcript, action log, persona finding/ticket output, local-only evidence, and no `/app/demo` dependency.
5. Commit the slice.
