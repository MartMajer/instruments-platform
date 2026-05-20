# Local UX agent audit harness

Status: local-only tooling for owner/developer UX review. It is not a staging smoke, not a production monitor, and not a replacement for owner validation calls.

## Purpose

The harness creates a repeatable local evidence pack for a bounded UX mission, then generates a persona-review prompt and normalizes reviewer output into ticket-ready findings.

Use it when the app feels confusing and the next question is: "What would a fresh user complain about, and what tickets should we cut?"

## Current scope

Implemented paths:

- fixed evidence mission `create-first-study` for persona `first-time-researcher`
- autonomous fixture missions for local persona review
- autonomous full-stack boundary mission for proving non-mocked local app/API/database access
- autonomous full-stack create-study mutation mission for a disposable local development stack

The mission is intentionally conservative:

- starts from local `/signin` and `/app`
- does not depend on staging credentials or Auth0 success
- records sign-in blocking as an observation instead of failing the run
- navigates product routes when local dev auth gives access
- avoids create/save/launch/export/invite/delete actions
- captures sanitized structural observations, not raw tenant data

## Data-safety policy

Default evidence must not persist:

- raw screenshots
- raw body text
- raw query strings or fragments
- full hrefs
- emails
- UUIDs
- participant codes
- invitation tokens
- cookies or authorization values
- local filesystem paths
- data/javascript/vbscript URI payloads

Screenshots are opt-in and default off. Visible text capture is bounded and sanitized when explicitly used.

## Run a local mission

Start the local web app separately, then run from `apps/web`:

```powershell
npm run ux:audit -- --base-url http://127.0.0.1:5174 --mission create-first-study --persona first-time-researcher --viewport desktop --output ../../artifacts/ux-agent-runs/local
```

On Windows shells where `npm.ps1` or script `PATH` resolution is broken, use the direct Node form from `apps/web`:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts --base-url http://127.0.0.1:5174 --mission create-first-study --persona first-time-researcher --viewport desktop --output ../../artifacts/ux-agent-runs/local
```

Useful options:

```powershell
--viewport desktop|mobile
--headless true|false
--data-mode fixture|fullstack
--output ../../artifacts/ux-agent-runs/local
```

The run writes a run directory under the output root. The useful files are:

- `run.json`
- `missions/create-first-study/evidence.json`
- `missions/create-first-study/review-prompt.md`

If local auth blocks app access, the run should still produce evidence and a prompt explaining the blocked state.

## Review the evidence as a persona

Open `missions/create-first-study/review-prompt.md` and give it to a reviewer agent or LLM. The prompt requires raw JSON or a fenced `json` block.

Expected reviewer shape:

```json
{
  "summary": "Short review summary.",
  "findings": [
    {
      "severity": "high",
      "affectedStep": "Step or route",
      "surface": "UI surface",
      "userExpectation": "What the persona expected",
      "observedConfusion": "What confused the persona",
      "suggestedFix": "Concrete fix",
      "ticketReadyWording": "Backlog-ready ticket wording"
    }
  ],
  "openQuestions": []
}
```

## Normalize reviewer output

Save reviewer output to a local text file, then run from `apps/web`:

```powershell
npm run ux:audit -- normalize-review --run-dir ../../artifacts/ux-agent-runs/local/<run-id> --mission create-first-study --review-input <review-output.txt>
```

If `--review-input` is omitted, the report is written as pending instead of failing.

Normalizer outputs:

- `review-report.md`
- `review-summary.json`

## Triage rule

Treat generated findings as candidate tickets, not automatic product truth.

Before implementation:

- confirm the finding is evidence-backed
- merge duplicates
- reject findings caused only by missing local seed/auth state
- classify owner decision blockers separately from implementation bugs
- keep Q-053 and Q-054 boundaries intact

D371 triage note: the scripted autonomous actor intentionally stops only on hard app-shell or route failures such as workspace access loading, sign-in/tenant access failures, unavailable selected-study routes, API request failures, network failures, service outage pages, or generic "something went wrong" screens. Normal product prerequisite wording such as "blocked", "missing", or "not available" is not enough to create a ticket by itself because Setup/Results/Waves legitimately use those states to guide the user.

## D372 goal-based personas

Autonomous missions now carry structured persona profiles so the run is tied to a real user job, not only a route list.

Current goal personas:

- Dr. Ana Kovac, first-time academic researcher: create and prepare a first study from `/app`, understand questionnaire/scoring/recipient/launch readiness, and know whether launch is safe.
- Marko Horvat, OSH consultant: prepare a client-ready workplace pulse, choose recipients, start collection, monitor submissions, and export client-usable results.
- Prof. Ivana Radic, busy PI: review Wave 1 and Wave 2, understand comparison validity, confirm anonymity/disclosure limits, and know whether export is analysis-ready.

Each profile defines role, domain knowledge, patience, app goal, success criteria, confusion triggers, hard failure triggers, and reviewer instructions. Evidence now includes `personaGoal` and `personaGoalAssessment`. Normalized reports render a `Persona goal` section with the app goal, checked criteria count, and target coverage.

D372 local proof ran all three autonomous missions against `/app`; each mission completed, visited 3 target product paths, checked 5 persona criteria, and rendered the persona goal in the markdown report.

## D373 criterion-validity hardening

The first persona reviewer pass found harness-validity gaps rather than product tickets. D373 tightened those gaps before treating autonomous output as ticket input:

- The wave/results mission now keeps the same longitudinal study context across Waves, Reports, and Exports.
- The export library includes a longitudinal export artifact tied to the reviewed study context.
- The OSH-consultant mission now includes Collection between Setup and Results/export, so it covers setup, collection state, and client handoff.
- `personaGoalAssessment.successCriteria` now uses `observed`, `unclear`, or `not_observed` and cites transcript excerpts instead of saying route visitation proves success.
- Normalized markdown reports now render criterion evidence.

D373 local proof ran all three autonomous missions against `/app`. Each mission visited 3 target product paths. All three completed with 0 generated findings and 0 next-action tickets.

## D374 full-stack boundary and persona action protocol

UXA02 adds an explicit autonomous data-mode boundary:

- `fixture` is the default mode. It enters the real local Svelte `/app` shell and uses deterministic local auth/session plus product read-model mocks.
- `fullstack` disables product read-model mocks. It is for local app/API/database proof only and must not point at staging or production.

Supported commands from `apps/web`:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --data-mode fixture --output ../../artifacts/ux-agent-runs/local
```

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-workspace-inspection --data-mode fullstack --output ../../artifacts/ux-agent-runs/local
```

The full-stack mission currently proves the boundary, not end-to-end mutation. With no local authenticated tenant session and seeded workspace, it should block with a ticket that asks for local full-stack auth/session and seed data. Evidence records `autonomousDataMode` and `productReadModelMocks` so reviewers can tell whether a run used fixture data or real local full-stack state.

UXA02 also adds a safe persona action-driver protocol for future LLM-backed persona loops. The protocol accepts only strict JSON actions for visible local UI controls:

- `click-link`
- `click-button`
- `fill`
- `complain`
- `stop`

It rejects remote navigation, malformed JSON, unknown action kinds, empty selectors, credential/secret fill labels, and invalid complaint severity. Persona reviewers remain evidence reviewers until a provider bridge is wired; they are not allowed to browse staging or production.

Next required slice: local full-stack synthetic seed/reset plus the first mutating mission that creates or modifies app state against a disposable local database.

## D375 full-stack development-auth mutation mission

UXA02 now has an explicit local development-auth path for full-stack autonomous runs. This is not Auth0 automation and must not be used against staging or production.

Development-auth options:

```powershell
--fullstack-dev-auth
--fullstack-tenant-id 11111111-1111-4111-8111-111111111111
--fullstack-user-id 22222222-2222-4222-8222-222222222222
--fullstack-email ux-agent@example.test
--fullstack-permissions setup.manage,team.manage,export.read
```

If `--fullstack-dev-auth` is present, the Playwright browser context injects development-auth headers into full-stack API requests:

- `X-Tenant-Id`
- `X-Dev-User-Id`
- `X-Dev-Tenant-Memberships`
- `X-Dev-Permissions`
- optional `X-Dev-Email`

Evidence records only `fullstackDevAuth=enabled|disabled|not-applicable`; it does not write tenant ids, user ids, or email values into the evidence observations.

New full-stack mutation mission:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-create-study --data-mode fullstack --fullstack-dev-auth --output ../../artifacts/ux-agent-runs/local
```

The mission starts at `/app`, opens Studies, fills the visible `Study name` field, clicks `Create study`, and stops only after the browser reaches `/app/campaign-series/{id}/setup`.

Current local proof status: the D375 mutation proof was attempted with full-stack development-auth enabled and read-model mocks disabled, but this workstation had no local API listening at `http://127.0.0.1:5055` and Docker was not running. The run therefore blocked at workspace access with local full-stack auth/session/seed guidance. That is the correct safe failure for the current machine state, not a product finding.

Next required slice: provide an owner-runnable local API/database bootstrap or preflight wrapper for UXA02 so the mutation mission can be proven green without manually reconstructing the full-stack environment.

## Known limits

- Fixed non-autonomous mode has one mission/persona. Autonomous mode has three local persona missions.
- It is local-first; staging-cookie mode is intentionally deferred.
- It does not do random monkey clicking.
- It does not mutate app state through risky product actions.
- It does not replace a real validation call with O01/O02/O03 contacts.

## D369 local-full transcript mode

UXA01 now defaults to `local-full` capture mode. This mode is local-only and refuses non-loopback base URLs before launching the browser. Accepted hosts are loopback/local development hosts such as `localhost`, `127.0.0.1`, `::1`, `0.0.0.0`, and `.localhost` names.

`local-full` captures page-visible UX structure for persona review:

- visible text
- headings
- buttons and disabled state
- links and route paths
- form fields, placeholder/value text, and required state
- sections/cards
- status and alert text

The harness writes the structured snapshot into `evidence.json` and a readable `transcript.md`. Review prompts now describe this as local audit evidence so persona agents can critique wording, navigation, blocked states, and task sequencing from what the user would actually see.

`safe` mode remains available with `--capture-mode safe` when a thinner non-text evidence artifact is desired.

Next required slice: autonomous local UX action loop, where a persona proposes structured actions against visible UI controls and the harness executes only allowed local browser actions.

## D370 autonomous local persona mode

UXA01 now has an autonomous local mode:

```powershell
npm run ux:audit -- autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --output ../../artifacts/ux-agent-runs/local
```

Direct Node form from `apps/web`:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --output ../../artifacts/ux-agent-runs/local
```

Autonomous mode is local-only. It validates every action against the current captured UI state before execution. Allowed actions are:

- local app route navigation
- visible link click
- visible enabled button click
- visible field fill
- complaint
- stop

Implemented autonomous product missions:

- `fixture-first-study-setup`
- `fixture-wave-results-comparison`
- `fixture-questionnaire-scoring`

Autonomous missions enter the real product shell at `/app`, not `/app/demo`. The harness mocks local auth/session, CSRF, and deterministic product read-model endpoints so the normal app cockpit, selected study setup, collection, results, waves, and export surfaces can render without Auth0, staging, or a local production database.

`/app/demo` remains useful as a human/debug fixture catalog, but it is not the primary autonomous audit target.

Outputs now include the normal mission evidence plus:

- screenshots per autonomous step
- action log
- local-full transcript
- generated persona reviewer JSON
- normalized `review-report.md`
- normalized `review-summary.json`
- next-action tickets

Verified local proof on 2026-05-20 ran all three autonomous missions against `http://127.0.0.1:5174/app` and asserted that generated evidence did not reference `/app/demo`.

Known D370 limits:

- The persona actor is scripted and deterministic, not a free-form browser agent.
- The local product read models are realistic seeds for UX review, not full backend state mutation.
- Candidate findings are still review inputs. Triage them before turning them into implementation tickets.
- Staging-cookie mode and random monkey exploration remain intentionally deferred.
