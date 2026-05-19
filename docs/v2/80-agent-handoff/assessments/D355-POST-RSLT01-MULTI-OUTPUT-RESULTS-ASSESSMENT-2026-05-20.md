# D355 - Post-RSLT01 Multi-Output Results Assessment

Date: 2026-05-20

## Assessment

RSLT01 addressed the main validated-study gap in the current custom builder: a study could expose multiple questionnaire formats, but Results setup still behaved like a one-score proof path. That was too narrow for dimensions, subscales, or simple multi-domain instruments.

The implemented slice is production-shaped for private beta without becoming a full psychometrics workbench. A researcher can keep the default total score path, or add multiple named result outputs with explicit result code, mean/sum calculation, missing-answer policy, minimum answered count, and included questions.

The scoring document now represents each output as its own input, answer-selection node, optional reverse-coding node, aggregate node, and output entry. The submitted `produces.scores` list now includes every configured output so downstream report/export surfaces can reason over result codes instead of assuming one total score.

The domain scoring engine now honors node-local missing-answer policy for aggregate nodes. This matters because one score can require all items while another allows a minimum answered count.

## Completed task

RSLT01 multi-output Results setup is complete locally.

Changed behavior:

- Setup Results can define multiple named outputs from one questionnaire.
- Each output can select a subset of eligible rating/recommendation/number questions.
- Each output can choose mean or sum calculation.
- Each output can choose require-all or minimum-answered missing policy.
- Generated scoring rules emit multi-output graph JSON and multi-code `produces.scores`.
- Scoring engine evaluates aggregate node-local missing policy.
- Focused workflow coverage proves the setup form submits two outputs from one questionnaire.

## Verification

Passed:

- `apps/web`: `node_modules/vitest/vitest.mjs --run src/lib/product/template-authoring.test.ts` passed 9/9.
- `.NET`: focused scoring engine and validator tests passed 2/2.
- Browser workflow: focused Playwright setup-authoring test passed 1/1 against direct Vite preview after production build.
- `git diff --check` passed with only existing CRLF normalization warnings.

Known local verification constraint:

- Standard Playwright `webServer.command` remains locally blocked by the existing `npm` PATH lookup issue, so the focused browser proof used explicit Node/Vite preview.

## Remaining risk

- The current scoring engine still treats a failed required output as a failed scoring run rather than returning partial successful outputs. That is acceptable for private beta, but partial-output resilience is a future hardening option.
- The scoring-rule validator accepts the generated node-local `missing_data` shape, but it does not yet perform deep semantic validation of every node-local missing-policy field.
- Interpretation bands, norms, validated thresholds, and formal named-instrument scoring manuals remain out of scope.
- DIR04 Docker-backed CSV import store proof still needs a Docker-enabled run before deployment.

## Decision

Move RSLT01 to Done. VAL08 validation packet refresh is now the selected remaining D350 follow-up blocker before owner validation material should be treated as current.
