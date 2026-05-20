# D380 UXA02 action-http provider actor mode

## Assessment

D379 proved that the provider-backed actor can be driven by a local JSONL action file, but that still precomputes the action sequence. For the harness to support a real local persona/LLM bridge, the actor needs to request one action at a time from a live local provider using the current visible UI state.

The safety boundary remains the key requirement. A provider may propose an action, but it must not gain arbitrary browser control, remote navigation, staging access, or shell execution.

## Task

Added `action-http` actor mode:

- `--actor-mode action-http`
- `--persona-action-url http://127.0.0.1:<port>/<path>`

The HTTP provider receives the bounded `PersonaActionRequest` by POST and returns one raw JSON persona action. The response still passes through `parsePersonaActionResponse` and the existing visible-control validator before Playwright executes anything.

Provider URL validation is intentionally narrow:

- HTTP only.
- Loopback host only: `127.0.0.1`, `localhost`, `*.localhost`, or `::1`.
- No username/password credentials.
- Non-loopback staging/production URLs are rejected before browser launch.

## Verification

- RED focused tests failed on the missing provider module, unknown `--persona-action-url`, and unsupported `action-http`.
- GREEN focused tests passed: `persona-action-http-provider.test.ts` and `runner-options.test.ts`, 33/33 tests.
- Full UX audit suite passed: 18/18 files, 120/120 tests.
- Local browser proof passed with a real local Vite app plus a localhost HTTP provider:
  - command mode: `autonomous --actor-mode action-http`
  - final URL: `/app/campaign-series`
  - status: `completed`
  - findings: 0
  - tickets: 0
  - artifact: `artifacts/ux-agent-runs/local/run-2026-05-20T18-39-22-610Z/`

## Remaining risk

This adds the live local provider transport, not an actual LLM/subagent runtime. The provider can now be implemented as a local process that calls an LLM or other persona agent, but that is a separate integration.

The green full-stack local mutation proof remains blocked until Docker Engine/local staging is running.
