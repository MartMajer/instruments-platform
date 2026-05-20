# D373 UXA01 criterion validity hardening assessment - 2026-05-20

## Trigger

The first subagent review of the D372 autonomous persona artifacts found that the harness was mechanically passing while still producing weak qualitative evidence. The issues were mostly harness-validity problems, not product UI bugs.

## Assessment

Three gaps needed correction before using the persona reports as trustworthy ticket input:

- The busy-professor wave mission mixed product contexts: Waves used the longitudinal sample, but Reports used the completed sample.
- The OSH-consultant mission goal mentioned collection and monitoring, but the mission only visited Setup and Results.
- `personaGoalAssessment.successCriteria` claimed criteria were satisfied because the mission visited configured routes. That overstates what deterministic route traversal can prove.

## Task

Implemented D373 harness hardening:

- Added `longitudinalSampleResults` and changed the wave/results/export mission to visit longitudinal Waves, longitudinal Reports, and Exports.
- Added a longitudinal export artifact to the local export library so Exports evidence carries the same reviewed study context.
- Changed the OSH-consultant mission to use one consistent completed sample across Setup, Collection, and Results/export.
- Changed persona-goal criterion assessment to use conservative statuses: `observed`, `unclear`, or `not_observed`.
- Replaced generic route-visitation evidence with route/section-specific transcript excerpts tied to matching criterion terms.
- Added a guard so questionnaire/scoring criteria cannot be satisfied by instrument-only setup text.
- Rendered criterion evidence in normalized markdown reports.

## Verification

RED focused tests failed on the intended gaps:

```text
4 failed test files
5 failed tests
```

GREEN focused tests passed:

```text
Test Files 4 passed (4)
Tests 25 passed (25)
```

Full UXA01 verification passed:

```text
Test Files 13 passed (13)
Tests 82 passed (82)
```

Local browser proof passed against `http://127.0.0.1:5174/app` for all three missions:

- `fixture-first-study-setup`: completed, 3 of 3 target paths, 0 findings, 0 tickets.
- `fixture-wave-results-comparison`: completed, 3 of 3 target paths, 0 findings, 0 tickets.
- `fixture-questionnaire-scoring`: completed, 3 of 3 target paths, 0 findings, 0 tickets.

Proof artifacts:

- `artifacts/ux-agent-runs/local/run-2026-05-20T17-20-14-506Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T17-20-20-192Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T17-20-23-757Z/`

## Remaining risk

The autonomous actor is still deterministic and local-only. Criterion matching is intentionally conservative and keyword-based; it is better than generic route success, but it is not a human-grade qualitative judgment. Persona reviewer agents still need to read the route/section transcript evidence and decide whether a finding is ticket-ready.
