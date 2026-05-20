# D372 UXA01 goal-based personas assessment - 2026-05-20

## Trigger

D371 made the autonomous `/app` harness mechanically clean, but it became mostly a route and seed-health checker. The owner asked whether personas can be actual users with a goal, instructions for how to be that persona, and a clear app objective.

## Assessment

The next useful UXA01 improvement is persona intent, not random browser autonomy. The harness should carry explicit user roles and success criteria so reports can say what the user was trying to accomplish and what criteria were checked. This keeps deterministic local runs while moving the output toward human comprehension review.

## Task

Implemented D372 goal-based persona contracts:

- Dr. Ana Kovac, first-time academic researcher preparing a first study.
- Marko Horvat, OSH consultant preparing a client-ready workplace pulse.
- Prof. Ivana Radic, principal investigator reviewing repeated-wave evidence.

Each profile now includes role, domain knowledge, patience, app goal, success criteria, confusion triggers, hard failure triggers, and reviewer instructions. Autonomous missions carry the persona profile, evidence includes `personaGoal` and `personaGoalAssessment`, generated reviewer JSON includes goal context, review prompts keep the new fields, and normalized markdown reports render a `Persona goal` section.

## Verification

Focused RED/GREEN tests covered persona contracts, mission profile wiring, evidence output, and report rendering.

Full UXA01 verification passed:

```text
node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
Test Files 13 passed (13)
Tests 78 passed (78)
```

Local browser proof passed against `http://127.0.0.1:5174/app` for all three missions:

- `fixture-first-study-setup`: completed, Dr. Ana Kovac, 5 criteria, 3 target paths.
- `fixture-wave-results-comparison`: completed, Prof. Ivana Radic, 5 criteria, 3 target paths.
- `fixture-questionnaire-scoring`: completed, Marko Horvat, 5 criteria, 3 target paths.

Proof artifacts:

- `artifacts/ux-agent-runs/local/run-2026-05-20T16-31-02-209Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T16-31-08-791Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T16-31-13-233Z/`

## Remaining risk

The persona actor is still deterministic. It now carries real persona intent and criteria, but it does not yet make nuanced qualitative judgments from the transcript. The next step, if desired, is a reviewer layer that grades each criterion against captured text and emits qualitative, evidence-backed UX complaints.
