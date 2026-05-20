# D378 - UXA02 Provider Action Actor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Wire the existing persona action JSON protocol into an actor adapter that can safely sit between a future persona provider and the browser mission loop.

**Architecture:** Keep provider integration provider-agnostic. A provider receives the bounded `PersonaActionRequest` and returns raw text. The adapter parses exactly one allowed JSON action or converts invalid output into a blocker complaint. The existing `runAutonomousFixtureMission` validator remains the final gate before any browser action executes.

**Tech Stack:** TypeScript, Vitest, existing UXA autonomous loop and action validator.

---

## Task 1: Provider actor tests

**Files:**

- Modify: `apps/web/scripts/ux-agent-audit/persona-action-driver.test.ts`

**Steps:**

- Write a failing test that a provider receives the bounded current-page request and returns a parsed action.
- Write a failing test that malformed/unsafe provider output becomes a blocker complaint.

## Task 2: Provider actor implementation

**Files:**

- Modify: `apps/web/scripts/ux-agent-audit/persona-action-driver.ts`

**Steps:**

- Add `PersonaActionProvider`.
- Add `buildProviderPersonaActionActor`.
- Catch provider/parse failures and return a blocker complaint instead of throwing.

## Task 3: Verification and docs

**Files:**

- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`
- Add: `docs/v2/80-agent-handoff/assessments/D378-UXA02-PROVIDER-ACTION-ACTOR-2026-05-20.md`

**Steps:**

- Run focused persona action driver tests.
- Run the full UXA suite.
- Document that provider actor wiring exists, but CLI provider bridge remains the next optional slice.
