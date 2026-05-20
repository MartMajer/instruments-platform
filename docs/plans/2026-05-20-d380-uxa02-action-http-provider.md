# D380 UXA02 action-http provider actor mode plan

## Assessment

D379 made the provider-style loop runnable through a local JSONL file, but that still made the persona action sequence deterministic before the browser run started. The next safe step is a local HTTP provider mode: the harness captures the current visible UI state, sends one bounded action request to a local provider, receives one proposed JSON action, and then applies the existing parser and visible-control validator before executing anything in the browser.

## Acceptance criteria

- Add `--actor-mode action-http`.
- Add `--persona-action-url <local-loopback-http-url>`.
- Reject non-loopback provider URLs.
- Reject provider URLs with credentials.
- Require the URL only when `action-http` mode is selected.
- Send one `PersonaActionRequest` per decision step by HTTP POST.
- Return the provider response as raw persona action JSON for the existing parser.
- Keep browser execution gated by existing visible local UI validation.
- Document proof and remaining limits.

## Verification

- RED focused tests failed on missing provider module, unknown `--persona-action-url`, and unsupported `action-http`.
- GREEN focused tests passed: 2 files, 33 tests.
- Full UX audit suite passed: 18 files, 120 tests.
- Local browser proof passed with Vite plus localhost provider: `artifacts/ux-agent-runs/local/run-2026-05-20T18-39-22-610Z/`, completed, 0 findings, 0 tickets.

## Remaining risk

The local HTTP bridge enables live local persona decision loops, but it does not by itself provide an LLM runtime or subagent bridge. The full-stack mutation proof still requires Docker Engine/local staging to run.
