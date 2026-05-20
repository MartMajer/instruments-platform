# D364 waves group-trend clarity plan

## Goal

Make the Waves hub understandable when a study has two anonymous waves: show wave-level group trend review as available, keep linked same-respondent comparison explicitly limited to repeat-participation waves, and stop making "Create Wave 3" the primary message after Wave 1 and Wave 2 already exist.

## Acceptance criteria

- Two closed anonymous waves with responses map to a group-trend review path, not a primary "Create Wave 3" path.
- Linked change-over-time actions remain blocked for non-longitudinal waves with wording that explains why.
- The Waves UI separates "group trend" from "linked change" so researchers understand what can and cannot be compared.
- Handoff docs record the product distinction and remaining backend follow-up.

## Test plan

- Add a focused frontend workflow-model regression test for two anonymous waves.
- Run the focused `waves-workflow.test.ts` suite red, implement, then run it green.
- Run the web production build and whitespace verification before deploy.
