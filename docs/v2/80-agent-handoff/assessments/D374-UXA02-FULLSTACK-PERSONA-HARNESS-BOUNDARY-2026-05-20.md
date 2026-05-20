# D374 - UXA02 full-stack persona harness boundary

Date: 2026-05-20

Status: done locally

## Assessment

D373 made the autonomous UX harness useful for local fixture review, but it still blurred three different ideas:

- deterministic fixture review of the real Svelte shell
- local full-stack review against app/API/database state
- future provider-backed persona action loops

That ambiguity would cause bad tickets. A reviewer could not tell whether a complaint came from mocked product read models or from real local backend state, and the harness had no safe action protocol for future non-deterministic persona behavior.

## Decision

Split autonomous review by data mode before adding mutation:

- `fixture` remains the default deterministic mode.
- `fullstack` disables product read-model mocks and is local-only.
- missions declare supported data modes and unsupported combinations fail before browser launch.
- evidence records the selected data mode and whether product read-model mocks were enabled.
- provider/persona actions must pass through a strict local UI action protocol before the harness executes anything.

## Task

Implemented UXA02 boundary work:

- added `--data-mode fixture|fullstack`
- added data-mode mission compatibility checks
- added the `fullstack-workspace-inspection` mission
- kept existing fixture missions fixture-only
- disabled local product read-model mocks in full-stack mode
- added evidence markers for `autonomousDataMode` and `productReadModelMocks`
- added a strict persona action-driver parser for `click-link`, `click-button`, `fill`, `complain`, and `stop`
- rejected remote navigation, malformed JSON, unknown action kinds, empty selectors, credential/secret fill labels, and invalid complaint severity
- changed full-stack workspace-access blockers to ask for local full-stack auth/session and seed data, not fixture mocking

## Verification

Focused UXA tests:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/autonomous-loop.test.ts scripts/ux-agent-audit/runner-options.test.ts scripts/ux-agent-audit/autonomous-fixtures.test.ts scripts/ux-agent-audit/persona-action-driver.test.ts
```

Result: 4 files passed, 35 tests passed.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 14 files passed, 92 tests passed.

Local browser proof:

- `artifacts/ux-agent-runs/local/run-2026-05-20T17-59-59-792Z/` - `fullstack-workspace-inspection`, `autonomousDataMode=fullstack`, `productReadModelMocks=disabled`, blocked on missing local auth/session/seed
- `artifacts/ux-agent-runs/local/run-2026-05-20T18-00-06-976Z/` - `fixture-first-study-setup`, `autonomousDataMode=fixture`, `productReadModelMocks=enabled`, completed

## Remaining risk

This does not yet mutate real app state end-to-end. Full-stack mode proves the non-mocked boundary and reports the missing local auth/seed prerequisite honestly. The next slice should add disposable local synthetic seed/reset and one mutating mission before any provider-backed persona loop is allowed to drive the app.

No staging or production autonomous browsing was added.
