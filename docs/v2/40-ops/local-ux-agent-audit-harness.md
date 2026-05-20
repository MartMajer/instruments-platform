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
- autonomous realistic OSH full-stack mission that creates a believable local study and saves the first instrument setup step

The mission is intentionally conservative:

- starts from local `/signin` and `/app`
- does not depend on staging credentials or Auth0 success
- records sign-in blocking as an observation instead of failing the run
- navigates product routes when local dev auth gives access
- avoids launch/export/invite/delete actions outside explicit local full-stack mutation missions
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

## D388 post-seed assessment and copy-smell hardening

UXA05 exposed two harness-validity issues and one product-copy issue after the D384-D387 product fixes.

The harness now:

- checks primary product route text for proof/artifact wording such as `Proof foundation`, `Proof only`, and `Export artifact`
- recomputes `personaGoalAssessment` after realistic full-stack seeding inspects Collection and Results
- adds seeded Collection and Results paths to `targetProductPaths` and `visitedProductPaths`
- requires route-specific transcript evidence for route-specific persona criteria
- reads the raw created-study URL only in memory for local full-stack seeding, validates that the extracted series id is GUID-shaped, and writes only sanitized/redacted URLs to evidence

The app copy cleanup paired with this harness pass removed remaining primary Results leaks from widget titles/messages/status values and formatted report dates as Croatian-style date/time without nanoseconds.

Latest local proof artifact:

- `artifacts/ux-agent-runs/local/run-2026-05-20T21-07-25-451Z/`

That proof used the real local app/API/database with product read-model mocks disabled. It created a realistic OSH warehouse study, seeded 21 submitted/scored responses, inspected Collection and Results, and completed with 0 findings and 0 next-action tickets.

## D390 full-stack synthetic study cleanup

UXA07 adds a local-only cleanup command for disposable full-stack UXA studies:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-cleanup --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth
```

Dry-run is the default. To archive matched rows:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-cleanup --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth --apply
```

Safety boundaries:

- local loopback API URLs only
- development auth required
- archives through `POST /campaign-series/{id}/archive`
- no direct database deletes
- no staging or production cleanup
- only known UXA synthetic study names are matched

Matched names currently include:

- `UXA full-stack mutation ...`
- `UXA local study ...`
- `Warehouse workload and recovery pulse`
- `Academic workload and recovery follow-up`

D390 runtime proof archived 20 active UXA synthetic studies from the disposable local stack. A post-apply dry-run returned `matchedCount: 0`.
## D389 repeated-wave full-stack mission

UXA06 adds a realistic busy-professor full-stack mission:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-academic-repeated-wave-review --data-mode fullstack --fullstack-dev-auth --headless true --output ../../artifacts/ux-agent-runs/local
```

The mission creates `Academic workload and recovery follow-up`, saves the first instrument setup step, then seeds the created study through the local API with two closed anonymous-longitudinal waves:

- `Baseline academic workload survey - May 2026`
- `Follow-up academic workload survey - June 2026`

The repeated-wave seed uses campaign open links plus stable participant codes. It does not use invitation batches because the API intentionally rejects email invitation batches for `anonymous_longitudinal` campaigns.

D389 local proof artifact:

- `artifacts/ux-agent-runs/local/run-2026-05-20T21-38-40-538Z/`
- product read-model mocks disabled
- two anonymous-longitudinal waves
- 32 submitted responses
- 32 score runs
- two closed campaigns
- Collection, Waves, and Results inspected after seeding
- busy-professor persona criteria observed: 5 of 5
- findings: 0
- next-action tickets: 0

D389 also hardens persona-goal assessment so it recognizes disclosure/anonymity and not-validated claim-safety evidence from real product transcripts, including `Disclosure visible / k 5`, `internal review only`, and client-facing claims not being validated.

Known follow-up: repeated full-stack missions now leave many synthetic studies in the disposable local database. Add a safe UXA cleanup/reset path before using the harness for long multi-run review sessions.
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

D378 wires the protocol into a provider-backed actor adapter:

- the provider receives a bounded `PersonaActionRequest`
- the provider returns one raw JSON action
- the adapter parses only allowed action kinds
- malformed or unsafe provider output becomes a blocker complaint
- the existing browser mission loop still validates the parsed action against visible local UI before execution

This is provider-agnostic wiring. No staging/prod provider bridge and no remote browsing is added.

D379 makes the safe provider-style loop runnable through a local JSONL action file:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --actor-mode action-file --persona-action-file ..\..\artifacts\ux-agent-runs\local\action-file-proof.jsonl --headless true --output ../../artifacts/ux-agent-runs/local
```

Each non-empty, non-comment line in the file is one raw JSON action. The runner consumes one line per step, passes it through `buildProviderPersonaActionActor`, parses the action protocol, then applies the existing visible-control validator before browser execution. If the file runs out, the provider returns a safe stop action.

Local proof artifact:

- `artifacts/ux-agent-runs/local/run-2026-05-20T18-29-34-204Z/`

That proof used fixture mode, clicked the visible `Studies Plan studies` link from `/app`, stopped through the action-file provider, and completed with 0 findings / 0 tickets.

D380 adds a live local HTTP provider mode for the same safe action loop:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fixture-first-study-setup --actor-mode action-http --persona-action-url http://127.0.0.1:8765/persona-action --headless true --output ../../artifacts/ux-agent-runs/local
```

In `action-http` mode, the harness posts one bounded `PersonaActionRequest` to the local provider for each decision step. The provider returns one raw JSON persona action. The runner then uses the same provider actor parser and visible-control validator before Playwright executes anything.

Safety boundaries:

- provider URL must be local loopback HTTP only
- staging and production provider URLs are rejected before browser launch
- provider URLs with credentials are rejected
- the provider receives sanitized visible UI state, persona goal, mission progress, and allowed action kinds
- provider output still cannot bypass action parsing or visible-control validation
- no shell command execution is introduced by this mode

Local proof artifact:

- `artifacts/ux-agent-runs/local/run-2026-05-20T18-39-22-610Z/`

That proof used fixture mode, posted live requests to a localhost provider, clicked the visible `Studies Plan studies` link from `/app`, stopped through the HTTP provider, and completed with 0 findings / 0 tickets.

Next required slice: start Docker Desktop and prove `fullstack-create-study` green against disposable local full-stack state.

## D381 green full-stack mutation proof

UXA02 now has a proven local full-stack mutation path. With Docker Desktop running, the bootstrap command can start the repo's local staging Compose stack, enable local development authentication for that child start without editing `.env`, retry preflight until the API is ready, and then run a browser mutation mission against the real local app/API/database.

Bootstrap command from `apps/web`:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-bootstrap --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth --repo-root ..\.. --start --timeout-ms 300000
```

The bootstrap now handles these local-only harness concerns:

- relative `--repo-root` values are resolved to absolute paths before invoking PowerShell scripts
- `--fullstack-dev-auth` starts Compose with process env overrides for `Authentication__Dev__Enabled=true` and `PUBLIC_DEV_AUTH_ENABLED=true`
- the existing local `.env` file is not modified
- preflight is retried after startup until the API/dev-auth/read-model checks are ready or attempts are exhausted

The mutation command:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-create-study --data-mode fullstack --fullstack-dev-auth --headless true --output ../../artifacts/ux-agent-runs/local
```

Verified local proof:

- bootstrap status: `ready`
- API health: HTTP 200
- development-auth session: HTTP 200
- tenant study read model: HTTP 200
- mutation artifact: `artifacts/ux-agent-runs/local/run-2026-05-20T18-54-36-933Z/`
- mutation status: `completed`
- final URL: `/app/campaign-series/019e46bd-6d56-7d30-807b-adcc0caaa475/setup`
- findings: 0
- next-action tickets: 0

Evidence records `autonomousDataMode=fullstack`, `productReadModelMocks=disabled`, and `fullstackDevAuth=enabled`.

## D382 realistic OSH full-stack mission

UXA03 now has its first realistic local persona mission plus evidence-level synthetic response simulation:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-osh-warehouse-pulse --data-mode fullstack --fullstack-dev-auth --headless true --output ../../artifacts/ux-agent-runs/local
```

The mission uses the OSH consultant persona and the synthetic case `Warehouse workload and recovery pulse`.

It performs visible UI actions against the local full-stack app:

- opens `/app`
- clicks `Studies Plan studies`
- fills `Study name`
- clicks `Create study`
- fills `Instrument name`
- clicks `Save instrument`
- stops after the realistic study and first setup step are saved

Evidence includes the case profile:

- study name: `Warehouse workload and recovery pulse`
- instrument name: `Warehouse workload and recovery instrument`
- campaign name: `Baseline warehouse pulse - May 2026`
- ten OSH questions across workload, control, support, recovery, and musculoskeletal strain
- synthetic response simulation: 24 respondents, 21 completed response rows, 3 omitted responses, three workplace segments, and six dimension risk summaries

Verified local proof:

- full UXA suite: 19 files / 127 tests passed
- artifact: `artifacts/ux-agent-runs/local/run-2026-05-20T19-19-05-318Z/`
- status: `completed`
- data mode: `fullstack`
- product read-model mocks: `disabled`
- full-stack dev auth: `enabled`
- final URL: `/app/campaign-series/019e46d3-d649-7bf9-a0e0-3694c38b57c0/setup`
- findings: 0

D382 was the first realistic mission slice. D383 completes UXA03-B locally by adding app-visible campaign/results seeding and bundled persona tickets.

## D383 realistic campaign/results seed and persona bundle

`fullstack-osh-warehouse-pulse` now has a local-only full-stack seed plan. After the browser creates the study and saves the instrument step, the harness derives the created campaign-series id from the local URL and seeds the same local study through the local API.

The seeder refuses non-loopback API URLs. It is for disposable local API/database state only and must not be pointed at staging or production.

The seed bridge creates:

- a template version with the realistic OSH questions
- a scoring rule for the case dimensions
- a campaign inside the newly created study
- a launch snapshot
- an invitation batch with synthetic local recipients
- 21 public open-link respondent sessions
- 21 submitted response sessions
- 21 scored response sessions

After seeding, the browser captures Collection and Results. The D383 proof reached Results with 21 submitted responses and 126 visible scores.

Run the mission:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-osh-warehouse-pulse --data-mode fullstack --fullstack-dev-auth --headless true --output ../../artifacts/ux-agent-runs/local
```

Bundle persona reviewer output after reviewer JSON files are saved under `<run-dir>/persona-reviews/`:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/persona-review-bundle.ts --run-dir ../../artifacts/ux-agent-runs/local/<run-dir> --review-dir ../../artifacts/ux-agent-runs/local/<run-dir>/persona-reviews --mission fullstack-osh-warehouse-pulse
```

D383 local proof:

- full UXA suite: 21 files / 133 tests passed
- artifact: `artifacts/ux-agent-runs/local/run-2026-05-20T19-34-02-977Z/`
- status: `completed`
- final URL: `/app/campaign-series/[redacted]/reports`
- invitations: 21
- submitted responses: 21
- scored responses: 21
- visible scores in Results: 126
- persona reviews bundled: 3 reviewers, 9 findings, 5 themes, 9 next-action tickets

The first persona bundle themes are Results client-readiness, interpretation validity, Collection lifecycle clarity, audience/recipient context, questionnaire/scoring semantics, and primary-path technical wording.

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

## D376 full-stack preflight command

UXA02 now has a preflight command that checks local full-stack readiness before launching a browser mutation mission:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-preflight --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth
```

The command checks, in order:

- local API health at `/health`
- development-auth session at `/auth/session`
- tenant study read model at `/campaign-series`

If an earlier check fails, later checks are skipped. This prevents the harness from misreporting a missing API or disabled dev-auth as a product UX issue.

Current workstation proof:

- status: `blocked`
- `api-health`: failed with `fetch failed`
- `dev-auth-session`: skipped
- `tenant-study-read-model`: skipped

Next required slice: start or document the local API/database bootstrap path so `fullstack-preflight` reaches `ready`, then rerun `fullstack-create-study` for a green mutation proof.

## D377 full-stack bootstrap command

UXA02 now has a bootstrap wrapper around the project's existing local staging scripts:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-bootstrap --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth --repo-root ..\..
```

Default behavior is non-destructive. It checks Docker Engine readiness, skips starting Compose unless `--start` is provided, runs `fullstack-preflight`, and prints the exact follow-up commands:

- `deploy/staging/start-local-staging.ps1`
- `deploy/staging/smoke-local-staging.ps1`
- `fullstack-preflight`
- `fullstack-create-study`

To let UXA02 invoke the existing local staging bootstrap:

```powershell
& 'D:\Program Files\nodejs\node.exe' --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-bootstrap --api-base-url http://127.0.0.1:5055 --fullstack-dev-auth --repo-root ..\.. --start
```

The Docker check uses `docker info`, not `docker --version`, so it catches the common state where the Docker CLI exists but Docker Desktop/Engine is not running.

Current workstation proof:

- status: `blocked`
- `docker`: failed
- detail: Docker command is unavailable or Docker Desktop is not running

Next required slice: start Docker Desktop and run `fullstack-bootstrap --start`, then rerun `fullstack-create-study` once preflight reports `ready`.

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
