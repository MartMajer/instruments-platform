# D352 - Post-QB01 respondent-format parity assessment - 2026-05-20

## Context

BR01 made the M1 private-beta acceptance bar explicit and selected QB01 as the first blocker because the study builder exposed more answer formats than the respondent runtime proved.

The exposed beta formats are rating, recommendation/NPS, single choice, multiple choice, number, text, date, and ranking. Branching, matrix/grid, file upload, pairwise, SurveyJS Creator, and platform-canonical named instruments remain outside this slice.

## Assessment

QB01 found a real builder-to-respondent contract gap.

The frontend authoring path already stored choice/ranking options in question `payload`, but the respondent API contract did not expose that payload. As a result, downstream respondent rendering could know a question was `single`, `multi`, or `ranking`, but could not know which choices to show.

The respondent SurveyJS adapter also rendered every question as a text input with numeric bounds only when scale metadata existed. That made the product look like it supported richer questionnaire formats while the response surface still behaved like a proof-only form.

Multiple-choice and ranking answers also needed explicit array serialization. The response store persists answer values as strings, so array-valued SurveyJS controls need a stable wire representation instead of JavaScript comma coercion.

Finally, the Svelte input bridge was reading every visible input by ordinal. That was acceptable for the old all-text runtime but unsafe once radio, checkbox, rating, and ranking controls exist, because it could overwrite normalized SurveyJS values with individual rendered input values.

## Decision

QB01 is complete at the M1 private-beta parity level.

The respondent contract now exposes `payload`, and the frontend respondent adapter maps the supported beta formats to SurveyJS controls:

- `likert` -> bounded rating control.
- `nps` -> bounded rating control, defaulting to 0-10 when scale metadata is absent.
- `single` -> radio group from payload options.
- `multi` -> checkbox group from payload options.
- `ranking` -> ranking control from payload options.
- `number` -> numeric text input.
- `date` -> date text input.
- `text` -> text input.

Saved initial values are mapped back into SurveyJS shape, including JSON-array answers for `multi` and `ranking`. SurveyJS values are normalized back to string answer values before save, with arrays serialized as JSON arrays.

## Verification

Passed:

- `& 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs --run src/lib/respondent/surveyjs-adapter.test.ts` from `apps/web`: 5 tests passed.
- `dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj --filter FullyQualifiedName~ResponseCaptureEndpointTests.Respondent_campaign_endpoint_returns_template_questions --no-restore`: 1 test passed.

Attempted broader web check:

- Direct `svelte-kit sync` plus `svelte-check --tsconfig ./tsconfig.json` ran, but failed on 21 existing TypeScript/Svelte errors outside the QB01 respondent adapter slice. The reported files were registration tests, operations workflow tests, setup-workspace e2e fixture typing, `/app` auth-failed layout derived ordering, `/register` route typing, and a setup workflow test/handler mismatch. No errors were reported for `SurveyRuntime.svelte` or `surveyjs-adapter.ts`.

Environment note:

- `npm run test:unit` and npm script shims did not resolve `node` correctly in this shell, so focused web verification used the explicit Node executable path. `npm ci` was run in `apps/web` to restore local dependencies; it reported 5 npm audit advisories, 4 low and 1 moderate.

## Remaining risk

QB01 proves renderer/control parity, not every downstream analytics/export presentation polish issue.

The raw saved representation for `multi` and `ranking` is a JSON array string. That is stable and machine-readable, but export display labels/codebook presentation may need later polish if validators expect human-readable choice labels in CSV.

The adapter now depends on backend `payload` for choices. If older templates have malformed or empty payload for choice-backed questions, the respondent runtime will render the question with no choices rather than invent unsafe defaults.

## Next selected slice

DIR04 remains the next queue head: add a CSV audience import MVP for people and groups.

The reason is unchanged from BR01: manual directory entry blocks realistic private-beta studies more than another UI polish pass does.
