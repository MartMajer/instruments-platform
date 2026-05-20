# D372 Goal-Based UX Personas Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Convert UXA01 autonomous review from route-only checks into goal-based persona review with concrete user roles, app goals, success criteria, and report-visible goal assessment.

**Architecture:** Keep the deterministic `/app` browser actor and local product read models from D370/D371. Add structured persona profiles that describe who the user is, what they are trying to accomplish, what success means, and what should confuse them. Store the persona goal and goal assessment in mission evidence, reviewer output, normalized summary, and markdown reports.

**Tech Stack:** TypeScript, Vitest, Playwright, existing UXA01 evidence/report/prompt tooling.

---

### Task 1: Persona contracts

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/persona-goals.ts`
- Create: `apps/web/scripts/ux-agent-audit/persona-goals.test.ts`

**Steps:**
1. Write failing tests requiring named goal profiles for first-time researcher, OSH consultant, and busy professor.
2. Implement structured profiles with role, domain knowledge, patience, app goal, success criteria, confusion triggers, hard failure triggers, and reviewer instructions.
3. Run focused Vitest.

### Task 2: Mission and evidence integration

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.ts`
- Modify: `apps/web/scripts/ux-agent-audit/autonomous-loop.ts`
- Modify: `apps/web/scripts/ux-agent-audit/review-prompt.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-fixtures.test.ts`
- Test: `apps/web/scripts/ux-agent-audit/autonomous-loop.test.ts`

**Steps:**
1. Write failing tests requiring missions to carry a persona profile and evidence to include persona goal assessment.
2. Attach persona profiles to autonomous missions.
3. Add persona goal and persona goal assessment to observations and generated reviewer JSON.
4. Add safe prompt keys for persona-goal evidence.
5. Run focused Vitest.

### Task 3: Report rendering and local proof

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/report.ts`
- Test: `apps/web/scripts/ux-agent-audit/report.test.ts`
- Modify: `docs/v2/40-ops/local-ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
1. Write failing report test requiring normalized markdown to show `Persona goal`.
2. Preserve persona goal and assessment in normalized summary.
3. Render persona goal, app goal, criteria count, and target coverage in markdown reports.
4. Run full UXA01 Vitest.
5. Run all three local autonomous missions against `/app`.
6. Commit the slice.
