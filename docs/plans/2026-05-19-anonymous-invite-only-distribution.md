# Anonymous Invite-Only Distribution Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow selected-audience invitation distribution for anonymous collection waves without turning answers into identified responses.

**Architecture:** Keep response identity mode as `anonymous`. Saved audience rules resolve selected subjects, and launch materializes anonymous email invitation assignments from those subjects when they have email addresses. Product UI explains that delivery metadata exists while reports and exports remain anonymous.

**Tech Stack:** .NET 9 minimal API/application/infrastructure store, EF Core/Postgres, SvelteKit/Svelte 5, docs under `docs/v2`.

---

### Task 1: Backend readiness and launch materialization

**Files:**
- Modify: `src/Platform.Infrastructure/Setup/SetupWorkflowStore.cs`
- Test: `tests/Platform.IntegrationTests/Infrastructure/PostgresMigrationTests.cs`

**Steps:**
- Replace the anonymous saved-audience launch blocker with email-recipient validation.
- Materialize anonymous invitation assignments from saved respondent rules at campaign launch.
- Queue invitation notifications and outbox messages with anonymous assignments.
- Keep anonymous repeat-participation saved-audience invitations blocked until designed.
- Update the previous blocker regression into an anonymous invite-only launch regression.

### Task 2: Product UI wording and workflow state

**Files:**
- Modify: `apps/web/src/lib/product/SelectedSeriesSetupWorkflow.svelte`
- Modify: `apps/web/src/lib/product/SelectedSeriesOperationsWorkflow.svelte`
- Modify: `apps/web/src/lib/product/operations-workflow.ts`

**Steps:**
- Explain that anonymous audience controls distribution, not answer identity.
- Treat queued invitations as respondent access in the Collection path.
- Show prepared invitation counts on the respondent-access step.
- Replace the old "switch to identified" readiness guidance with recipient-email and unsupported repeat-participation guidance.

### Task 3: Documentation

**Files:**
- Create: `docs/v2/70-decisions/0013-anonymous-invite-only-distribution.md`
- Modify: `docs/v2/30-features/custom-study-builder.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
- Document distribution identity vs response identity.
- Record the current implemented scope and privacy limits.
- Log the assessment, task, and verification status for the next session.
