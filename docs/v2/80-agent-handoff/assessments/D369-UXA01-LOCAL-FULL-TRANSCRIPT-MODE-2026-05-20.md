# D369 UXA01 local-full transcript mode assessment - 2026-05-20

## Trigger

The first UXA01 harness implementation produced safe but thin evidence. Persona agents could review mission status and visible controls, but they could not meaningfully judge the app copy, page hierarchy, blocked states, or task guidance because most user-visible text was absent.

## Assessment

The next useful primitive is local-only full transcript capture. The harness must refuse non-local base URLs, then capture enough visible text and UI structure from the local browser session for downstream persona agents to review the product like a real user without the owner clicking through every screen.

This is intentionally not production/staging automation. The harness remains restricted to loopback/local development URLs and still redacts sensitive-looking strings in generated transcript content. The evidence contract now supports richer local review while preserving the deployment safety boundary.

## Task

Implement `local-full` capture mode for UXA01:

- default runner capture mode to `local-full`
- reject non-loopback `--base-url` values before launching the browser
- capture visible text, headings, buttons with disabled state, links, fields, sections/cards, and status messages
- write a readable `transcript.md` from rich snapshots
- include local transcript evidence in mission observations and persona review prompts
- keep `safe` capture mode available for thinner evidence

## Verification

Focused verification passed:

```text
node node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/runner-options.test.ts scripts/ux-agent-audit/rich-transcript.test.ts
Test Files 2 passed (2)
Tests 15 passed (15)
```

Local browser proof also passed against `http://127.0.0.1:5174` after starting the local web app. The run generated `artifacts/ux-agent-runs/local/run-2026-05-20T15-13-35-479Z/missions/create-first-study/transcript.md` and `review-prompt.md`; the proof asserted that the transcript contains local-full visible text and the review prompt contains `localFullTranscript` under local audit evidence.

## Remaining risk

UXA01 can now produce meaningful transcript artifacts, but persona agents still do not autonomously choose actions. The next slice should add a controlled action loop: transcript snapshot -> persona action proposal -> harness executes only visible safe actions -> repeat -> complaint/ticket output.
