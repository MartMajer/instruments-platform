# D354 Post-AUD01 Recipient Selection Assessment - 2026-05-20

## Context

BR01 marked audience setup as a private-beta required path and selected AUD01 because the working backend mechanics still appeared to researchers as respondent-rule programming. DIR04 added CSV import support so a realistic Directory can be prepared, but collection setup still needed a plain-language way to choose who receives a wave.

AUD01 intentionally kept the respondent-rule backend contract stable. The slice changed the setup experience around previewing and saving recipient selections.

## Assessment

AUD01 closes the main researcher-facing audience-selection blocker for M1 private beta. The setup workflow now presents recipient choice as product language:

- "Send invitations to" instead of rule kind/source.
- "Everyone in the study audience", "Everyone in a selected group", "One person's manager", and "One person's direct reports" instead of raw rule identifiers.
- "Preview recipients" and "Save recipient selection" instead of preview/save rule.
- Saved selections are shown as readable recipient selections with invitation-pair counts.
- Prepared delivery rows are shown as an invitation roster with readable person-to-person labels.
- Anonymous wave copy stays explicit: invitations control access, but reporting remains anonymous.

The backend still stores respondent-rule JSON and campaign assignments. That is acceptable for this slice because the researcher path no longer exposes those terms during normal setup, and the existing preview/save endpoints already enforce tenant scope and readiness checks.

Remaining risks:

- The Directory import store path from DIR04 still needs Docker-backed proof before deployment.
- Relationship-based selections depend on Directory manager/report data quality.
- The app still lacks multi-output scoring setup for dimensions/subscales, which is now the stronger private-beta product gap.

## Verification

Passed:

- Production web build with explicit Node/Vite command.
- Focused Playwright e2e against direct Vite preview:
  - `setup workflow previews respondent-rule audience from the selected campaign`
  - `setup workflow saves respondent rules and shows safe assignments`

Notes:

- The standard Playwright `webServer.command` could not be used because this local shell cannot resolve `npm` from the config command. The verification bypassed that by building with explicit Node, starting Vite preview directly, and running the same focused tests with a temporary no-webserver Playwright config.
- Vite still reports the existing large-chunk warning.

## Decision

Mark AUD01 complete for the M1 private-beta acceptance lane.

Proceed to RSLT01 next. The next major product gap is that Results setup still behaves like a one-output scoring setup, while validated instruments commonly require multiple dimensions/subscales.

## Next slice

RSLT01 - add multi-output Results setup for dimensions/subscales.

Acceptance direction:

- Let a researcher define more than one named result output from one questionnaire.
- Keep the current one-output total score path as the simplest case.
- Make included questions, calculation, and missing-answer policy clear per output.
- Keep exports/reports compatible with multiple dimension codes.
