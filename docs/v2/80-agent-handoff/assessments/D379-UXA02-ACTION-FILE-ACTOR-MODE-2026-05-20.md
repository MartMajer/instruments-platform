# D379 - UXA02 action-file actor mode

Date: 2026-05-20

Status: done locally

## Assessment

D378 added a safe provider actor seam, but it still was not runnable from the CLI. A live external command provider would be useful later, but it creates command-execution risk. The safer next step is a local JSONL action-file actor mode: another process or subagent can write actions to a file, and UXA02 consumes them through the same parser and visible-control validator.

## Decision

Add `action-file` actor mode:

- default remains `scripted`
- `action-file` requires `--persona-action-file`
- each non-empty, non-comment line is one raw JSON action
- exhausted files return a safe stop action
- parsed actions still pass through `validateAgentActionAgainstSnapshot`

## Task

Implemented:

- `AutonomousActorMode = scripted | action-file`
- `persona-action-file-provider.ts`
- `--actor-mode`
- `--persona-action-file`
- browser wiring to `buildProviderPersonaActionActor(loadPersonaActionFileProvider(...))`

No arbitrary external command execution was added.

## Verification

Focused tests:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit/persona-action-file-provider.test.ts scripts/ux-agent-audit/runner-options.test.ts
```

Result: 2 files passed, 28 tests passed.

Full UXA suite:

```powershell
& 'D:\Program Files\nodejs\node.exe' node_modules/vitest/vitest.mjs run scripts/ux-agent-audit
```

Result: 17 files passed, 113 tests passed.

Local browser proof:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --actor-mode action-file --persona-action-file <local-jsonl> --headless true --output ../../artifacts/ux-agent-runs/local
```

Proof artifact:

- `artifacts/ux-agent-runs/local/run-2026-05-20T18-29-34-204Z/`

Result: completed with 0 findings and 0 next-action tickets.

## Remaining risk

This proves a runnable local safe action-loop path in fixture mode. The green full-stack mutation proof remains blocked until Docker Desktop/Engine is running and the local staging stack can start.
