# D351 - Post-BR01 Private-Beta Acceptance Assessment - 2026-05-20

Status: Current assessment after BR01.

## Assessment

BR01 closed the immediate ambiguity from Q-058 by defining the M1 private-beta acceptance bar as a route-level checklist.

The checklist confirms the next limiter is not more shell cleanup. The highest risk is that the setup builder now exposes question formats whose respondent rendering, submission, export, and report behavior have not been proven end to end.

## Decision

Select QB01 next: prove respondent-rendering parity for every exposed question format.

QB01 should cover:

- rating scale;
- recommendation scale;
- single choice;
- multiple choice;
- number;
- text;
- date;
- ranking.

The slice should test the real builder-to-respondent path where feasible. If the current backend or respondent runtime intentionally cannot support a format, QB01 must either fix it or remove/reclassify the format from the visible beta builder.

## Non-goals

- Do not add branching, matrix/grid, file upload, quotas, or SurveyJS Creator.
- Do not add canonical named-instrument content.
- Do not expand scoring beyond parity needs; multi-output Results setup remains RSLT01.
- Do not use real-person data.

## Verification

Docs-only BR01 verification:

- `git diff --check` should pass before commit.
- `private-beta-acceptance-checklist.md` defines the acceptance path and selected blockers.
- Q-058 should be closed with a pointer to the checklist.

## Next

Run QB01 before DIR04, AUD01, RSLT01, or VAL08.
