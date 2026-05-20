# UXA02 Full-Stack Persona Harness Design

Status: local-only tooling design.

## Problem

UXA01 can inspect the real Svelte UI, but it uses deterministic mocked product read models. It is fast and useful for copy/layout triage, but it cannot prove real backend mutations, real persistence, or real workflow continuity. Persona subagents currently review captured evidence after the run; they do not choose live browser actions.

## Decision

Keep UXA01 fixture mode. Add two separate capabilities instead of overloading the current mode:

- `fullstack` data mode: no product read-model mocks. The browser talks to the local app/API/database stack. If local auth, API, tenant data, or selected study state is missing, the harness records the blocker instead of silently falling back to fixtures.
- Persona action driver protocol: a strict JSON action bridge that turns a captured page snapshot and persona goal into one validated action. This is the seam for a future LLM provider or Codex-orchestrated driver.

## Architecture

The harness has three layers:

- Evidence layer: Playwright captures visible text, links, buttons, fields, screenshots, and transcript sections.
- Action safety layer: every proposed action is validated against the captured snapshot before Playwright executes it.
- Driver layer: either scripted fixture actor or future persona JSON/LLM actor proposes the next action.

`fixture` data mode keeps using local route mocks for deterministic UX checks. `fullstack` data mode refuses fixture-only missions unless a mission explicitly supports full-stack data. Full-stack mode must not intercept product API calls with deterministic read models.

## Boundaries

Allowed:

- local loopback web origins only
- synthetic tenants/users/data
- visible UI actions only
- bounded max steps
- generated evidence artifacts under `artifacts/ux-agent-runs/local`

Not allowed:

- staging or production targets
- arbitrary JavaScript execution from an agent
- direct filesystem access from the persona driver
- secrets, cookies, invitation tokens, participant codes, raw answers, or raw query strings in reviewer artifacts
- destructive actions without a dedicated future safety design

## Implementation sequence

1. Add `AutonomousDataMode = fixture | fullstack`.
2. Make autonomous missions declare supported data modes.
3. Parse `--data-mode` for autonomous runs.
4. In `fixture` mode, keep current route mocks.
5. In `fullstack` mode, install no product read-model mocks and fail closed for fixture-only missions.
6. Add persona action-driver request/response parsing.
7. Keep actual provider integration separate; the first slice is the safe seam, not a networked LLM client.

## Remaining future work

The next implementation slice after the safe seam should add a synthetic full-stack seed/reset command. That command must create an isolated tenant, owner/member session, subjects/groups, and one mutable study that the harness can mutate without touching owner/staging data.
