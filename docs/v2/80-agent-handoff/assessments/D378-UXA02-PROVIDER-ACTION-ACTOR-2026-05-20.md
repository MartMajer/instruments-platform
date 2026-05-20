# D378 - UXA02 provider-backed safe action actor

Date: 2026-05-20

Status: done locally

## Assessment

D374 introduced the persona action JSON protocol, but it was only a parser/request builder. The autonomous mission loop still had no provider-backed actor that could receive current UI state, ask a persona provider for one action, and pass the result through the existing safe executor.

## Decision

Add provider-agnostic actor wiring before adding any external LLM/subagent bridge:

- provider receives bounded, sanitized `PersonaActionRequest`
- provider returns one raw JSON action
- parser accepts only allowed action kinds
- malformed or unsafe provider output becomes a blocker complaint
- browser execution still goes through `validateAgentActionAgainstSnapshot`

This keeps the safety boundary independent of any future provider.

## Task

Implemented:

- `PersonaActionProvider`
- `buildProviderPersonaActionActor`
- provider request/parse test
- invalid provider output blocker test

No remote provider bridge, staging/prod browsing, or random clicking was added.

## Verification

Focused test:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/persona-action-driver.test.ts
```

Result: 1 file passed, 5 tests passed.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 16 files passed, 109 tests passed.

## Remaining risk

This adds the safe provider actor seam, not a live provider CLI bridge. If the owner wants a live local persona provider, the next slice should add a local-only CLI bridge that feeds provider output through `buildProviderPersonaActionActor` and keeps the visible-control validator as the final browser execution gate.
