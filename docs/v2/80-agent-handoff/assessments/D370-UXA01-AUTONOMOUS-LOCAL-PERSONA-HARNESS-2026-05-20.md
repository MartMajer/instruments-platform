# D370 UXA01 autonomous local persona harness assessment - 2026-05-20

## Trigger

D369 made local app text visible to persona agents, but it was still passive. The owner asked for a more-than-good-enough local automation loop where agents can use the app like real users, complain, and produce tickets without owner-assisted clicking.

## Assessment

The next necessary capability is a local-only autonomous action loop with deterministic fixture missions. The harness must not grant arbitrary browser control. It should let a persona choose from structured actions that are validated against currently visible local UI: local route navigation, visible link click, visible enabled button click, visible field fill, complaint, or stop.

The autonomous harness must audit the real product shell, not only the `/app/demo` fixture catalog. `/app/demo` remains useful as a fixture source and human/debug view, but the autonomous run must start at `/app`, click normal app links/cards, and inspect selected study setup/collection/results/waves/export routes using deterministic local product read models. This avoids Auth0, staging, and real production data while still exercising the same UI hierarchy the owner sees.

## Task

Implemented D370 autonomous local persona harness foundations:

- safe action schema and validator for local visible UI actions
- autonomous product mission catalog with three persona missions
- local deterministic product read models for `/app`, selected study setup/collection/results/waves, reports widget manifest, export artifacts, respondent rules, assignments, and wave proof endpoints
- scripted persona actor and loop that executes actions without owner clicks
- browser adapter support for local route navigation, visible link/button/field actions, screenshots, transcript snapshots, and fallback local path navigation after visible-link validation
- `run.ts autonomous` CLI path
- automatic local-only auth/session, CSRF, and product API route mocks for autonomous `/app` review
- generated persona reviewer output normalized into `review-report.md` and `review-summary.json`
- regression coverage for workspace-access loading so the harness complains instead of falsely completing

## Verification

Focused UXA01 verification passed:

```text
node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
Test Files 12 passed (12)
Tests 73 passed (73)
```

Local browser proof passed against `http://127.0.0.1:5174/app`:

```text
node --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --headless true --output ../../artifacts/ux-agent-runs/local
```

All three autonomous missions were run:

- `fixture-first-study-setup`
- `fixture-wave-results-comparison`
- `fixture-questionnaire-scoring`

Proof artifacts:

- `artifacts/ux-agent-runs/local/run-2026-05-20T15-54-43-046Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T15-54-51-389Z/`
- `artifacts/ux-agent-runs/local/run-2026-05-20T15-54-54-669Z/`

The proof generated transcripts, review prompts, screenshots, review reports, review summaries, findings, and next-action tickets. The proof command assertion-checked that generated evidence did not contain `/app/demo`.

## Remaining risk

This is an autonomous local UX review harness, not a replacement for validation calls or a full backend integration test. The local product read models are deterministic UX seeds; they do not prove backend mutations, production auth, or database persistence. The next useful product step is to triage generated findings and select one evidence-backed UX ticket.
