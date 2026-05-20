# Local UX agent audit harness

Status: local-only tooling for owner/developer UX review. It is not a staging smoke, not a production monitor, and not a replacement for owner validation calls.

## Purpose

The harness creates a repeatable local evidence pack for a bounded UX mission, then generates a persona-review prompt and normalizes reviewer output into ticket-ready findings.

Use it when the app feels confusing and the next question is: "What would a fresh user complain about, and what tickets should we cut?"

## Current scope

Implemented mission:

- `create-first-study` for persona `first-time-researcher`

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

## Known limits

- Only one mission/persona is implemented.
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
