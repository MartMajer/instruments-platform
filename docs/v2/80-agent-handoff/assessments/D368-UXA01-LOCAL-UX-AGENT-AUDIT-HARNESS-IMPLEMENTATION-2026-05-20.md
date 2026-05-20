# D368 - UXA01 local UX agent audit harness implementation assessment - 2026-05-20

## Assessment

Owner testing and the D367 design showed that manual route-by-route UI review was too tiring and inconsistent for the current app breadth. The useful next tool was not another product polish pass, but a repeatable local review loop: collect bounded evidence, ask a persona reviewer to complain from sanitized evidence, then normalize findings into tickets.

The main implementation risk was data safety. A UX audit harness can easily become unsafe if it writes raw screenshots, raw body text, invite tokens, participant codes, emails, tenant identifiers, or local paths into artifacts intended for reviewer agents.

## Task

Implemented UXA01 as local-only tooling under `apps/web/scripts/ux-agent-audit`:

- mission/persona contracts
- JSON evidence pack writer
- CLI runner and Playwright browser helper
- fixed `create-first-study` mission for `first-time-researcher`
- safe evidence capture policy
- persona review prompt generation
- reviewer output normalizer
- markdown report and JSON summary output
- focused Vitest coverage for contracts, evidence, runner options, browser safety, mission execution, prompt generation, and report normalization

## Safety decisions

The harness defaults to sanitized structural evidence:

- screenshots are opt-in and default off
- visible text capture is bounded/sanitized and not enabled for product pages by default
- URLs strip query strings/fragments and do not preserve raw origins
- non-http URI schemes are redacted
- emails, UUIDs, token-like values, participant-code-like values, local filesystem paths, markdown/HTML/data URI content, and unsafe evidence keys are redacted or omitted
- reviewer output must be structured JSON; prose-only or schema-invalid output becomes `needs-structured-review`, not reviewed/no-findings

## Verification

Focused UX audit suite passed after the final sanitizer fix:

```powershell
Push-Location apps/web; & 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs run scripts/ux-agent-audit; $code=$LASTEXITCODE; Pop-Location; exit $code
```

Result: 7 test files passed, 55 tests passed.

Final independent review approved the last sanitizer/report wording fix.

## Remaining risk

- The harness has not yet been run against a live local app session in this final slice.
- Only `create-first-study` and `first-time-researcher` are implemented.
- Staging-cookie mode remains deferred intentionally.
- Generated UX findings still require human triage before becoming product work.
