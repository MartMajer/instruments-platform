# D373 UXA01 Criterion Validity Hardening Implementation Plan

**Goal:** Make the autonomous UXA01 persona harness produce internally consistent mission evidence and avoid claiming that route visitation proves persona-goal success.

**Architecture:** Keep the deterministic local `/app` actor. Tighten mission target paths and local read models so each mission reviews one coherent product context. Keep generated persona assessments conservative: criteria may be `observed`, `unclear`, or `not_observed`, and must cite transcript evidence instead of generic route-coverage text.

**Tech Stack:** TypeScript, Vitest, Playwright, existing UXA01 evidence/report/prompt tooling.

## Task 1: Mission-context consistency

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.ts`
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-product-read-models.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.test.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-product-read-models.test.ts`

**Steps:**
1. Write failing tests requiring the wave/results mission to use the longitudinal series for both Waves and Reports.
2. Write failing tests requiring the export library to expose a longitudinal artifact tied to the same reviewed study.
3. Write failing tests requiring the consultant mission to include Collection, not only Setup and Results.
4. Update mission paths and local read models.
5. Run focused Vitest.

## Task 2: Criterion evidence quality

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-loop.ts`
- Modify: `apps/web/scripts/ux-agent-audit/report.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-loop.test.ts`
- Test: `apps/web/scripts/ux-agent-audit/report.test.ts`

**Steps:**
1. Write failing tests requiring persona success criteria to carry conservative statuses and transcript-backed evidence.
2. Replace the generic route-visitation evidence text with transcript evidence matching.
3. Render criterion evidence in normalized markdown reports.
4. Run focused Vitest.

## Task 3: Handoff and proof

**Files:**
- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Create: `docs/v2/80-agent-handoff/assessments/D373-UXA01-CRITERION-VALIDITY-HARDENING-2026-05-20.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
1. Run full UXA01 Vitest.
2. Run all three local autonomous missions against `/app`.
3. Send the new artifacts to persona reviewer agents for qualitative review.
4. Record verification, artifact paths, and remaining risk in handoff docs.
