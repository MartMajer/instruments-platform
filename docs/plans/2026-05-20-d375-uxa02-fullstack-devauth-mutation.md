# D375 - UXA02 full-stack dev-auth mutation mission

## Objective

Move UXA02 beyond the D374 full-stack boundary by adding a safe local-only path that can authenticate the browser to a development API and execute one bounded mutating mission against a disposable local stack.

## Acceptance criteria

- Autonomous CLI accepts explicit local full-stack development-auth options.
- Development-auth headers are injected only for autonomous full-stack runs when explicitly requested.
- Evidence records whether full-stack development-auth header injection was enabled without exposing tenant/user ids.
- A full-stack-only mutation mission can fill the visible "Study name" field and click "Create study" on `/app/campaign-series`.
- The mission stops successfully only after it reaches a newly created study setup route.
- If local API/auth/seed state is absent, the run blocks with local full-stack auth/session/seed wording instead of fixture-mock wording.
- Fixture mode remains deterministic and unchanged.

## Non-goals

- No staging or production autonomous browsing.
- No Auth0 automation.
- No random clicking.
- No destructive cleanup against an unknown database.
- No provider-backed LLM loop yet.

## Test plan

- RED/GREEN `runner-options` coverage for the new full-stack dev-auth flags.
- RED/GREEN browser option coverage proving full-stack dev-auth headers are installed only when requested.
- RED/GREEN autonomous loop coverage for the create-study mission action sequence.
- Full UXA Vitest suite.
- Local browser proof:
  - full-stack mode without dev auth still blocks safely
  - fixture mode still completes
  - full-stack mutation proof is attempted only when a local API/database stack is available

## Handoff

Update `local-ux-agent-audit-harness.md`, D-series assessment, `NEXT-ACTIONS.md`, `NEXT-SESSION-PLAN.md`, and `SESSION-LOG.md`.
