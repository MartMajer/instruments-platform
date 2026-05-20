# UXA01 Local UX Agent Audit Harness Design

## Decision

Build a local-first UX agent audit harness. It should use Playwright for browser control and evidence capture, then use persona-style agent review to turn the evidence into structured UX findings.

The first version should not be a random browser monkey. It should run goal-driven missions with bounded autonomy, clear personas, screenshots, page text, visible controls, step logs, and a severity rubric.

## Problem

Owner validation has become tiring because the owner is acting as every user: researcher, respondent, workspace owner, professor, consultant, and confused first-time visitor. Manual review finds real UX issues, but it does not scale and it is hard to repeat after fixes.

The app needs a repeatable way to ask:

- Can a fresh researcher understand the next action?
- Does wording sound fake, technical, or AI-generated?
- Does a user get stuck in setup, collection, results, or waves?
- Does mobile or narrow viewport usage break the story?
- Are known beta limits clearly explained instead of hidden as confusing blockers?

## Goals

- Run local browser UX audits against the normal app route.
- Let agents review the app as specific user personas with minimal internal product knowledge.
- Produce evidence-backed reports with screenshots, route state, observed text, steps, and findings.
- Convert findings into a normalized backlog: blocker, confusion, polish, or acceptable beta limit.
- Make reruns cheap after fixes.

## Non-goals

- Do not replace real owner/validator calls.
- Do not ship a production monitoring tool.
- Do not add broad UI automation to CI yet.
- Do not let agents automatically modify product code from complaints.
- Do not require live Auth0 or staging cookies in the first version.
- Do not build a generic app crawler that clicks everything randomly.

## Recommended architecture

| Layer | Responsibility |
|---|---|
| Mission catalog | Defines user goals such as create study, launch wave, complete respondent flow, review results, compare waves. |
| Persona catalog | Defines reviewer lenses such as first-time researcher, busy professor, OSH consultant, respondent on mobile, workspace owner. |
| Playwright runner | Opens local app, executes mission steps, captures screenshots, URL, visible text, buttons/links, and console/page errors. |
| Evidence pack | Stores screenshots and structured JSON/Markdown per mission run under `artifacts/ux-agent-runs/...`. |
| Agent review prompt | Gives the persona, mission, evidence, and rubric to an agent reviewer. |
| Finding normalizer | Produces a consistent Markdown report with severity, route, screenshot reference, user impact, and suggested next action. |
| Handoff integration | Summarizes high-severity findings in `SESSION-LOG.md` or a future UX backlog doc only after owner review. |

## Local-first environment

Use local/dev auth first because it is repeatable and avoids Auth0 session noise.

Preferred first target:

```text
Web: http://127.0.0.1:5174
API: http://127.0.0.1:5055
Auth: local/dev authenticated session or seeded dev headers where current test helpers already support it
```

Staging-cookie mode should be deferred until local missions are useful.

## Mission model

Each mission should be a small YAML or TypeScript object:

```yaml
id: create-first-study
persona: first-time-researcher
viewport: desktop
startUrl: /app
goal: Create a new study and reach the setup path.
successCriteria:
  - A study exists
  - Setup page is visible
  - User can identify the next setup step
maxSteps: 40
```

The first missions should be:

| ID | Goal | Persona |
|---|---|---|
| `auth-enter-workspace` | Sign in or enter local workspace. | Workspace owner |
| `create-first-study` | Create/open a study and understand setup. | First-time researcher |
| `configure-study` | Add questionnaire and results outputs. | Busy professor |
| `prepare-audience` | Import or prepare audience and recipient selection. | OSH consultant |
| `launch-wave` | Run launch check and start collection. | First-time researcher |
| `respondent-submit-mobile` | Complete respondent flow on phone viewport. | Respondent |
| `review-results-export` | Find Results and download CSV. | Busy professor |
| `review-waves` | Understand group trend versus linked change. | Researcher |

## Review rubric

Every finding should use this shape:

```text
Severity: blocker / confusion / polish / acceptable-beta-limit
Route:
Persona:
Mission step:
Evidence:
Observed:
Why it matters:
Suggested next action:
```

Severity rules:

| Severity | Meaning |
|---|---|
| Blocker | The persona cannot complete the mission or is likely to abandon. |
| Confusion | The persona can continue, but wording/layout creates wrong expectations. |
| Polish | Cosmetic or wording improvement that does not block comprehension. |
| Acceptable beta limit | Limitation is real but explainable for current private beta. |

## Agent behavior

The reviewer should be told:

- You are not a developer.
- Do not assume internal product intent.
- Use only the evidence pack.
- Be strict about unclear next actions, fake-sounding text, and hidden prerequisites.
- Do not request new features unless the mission genuinely fails without them.
- Prefer fewer high-quality findings over long complaint lists.

## Output

Each run should produce:

```text
artifacts/ux-agent-runs/<timestamp>/
  run.json
  summary.md
  missions/
    <mission-id>/
      evidence.json
      screenshots/
      transcript.md
      persona-review.md
```

The first version can produce reports locally only. Later slices can add issue creation or handoff-doc integration.

## Risks

| Risk | Mitigation |
|---|---|
| Agent hallucinates missing features | Restrict it to evidence and rubric; require screenshot/route reference. |
| Harness becomes brittle UI test suite | Treat it as UX audit evidence, not pass/fail CI. |
| Random exploration creates noise | Start with bounded missions and max steps. |
| Auth setup consumes time | Use local/dev auth first; defer staging cookies. |
| Owner receives too many findings | Normalize by severity and review only blockers/confusion first. |

## Approval

Approved by owner on 2026-05-20 as local-first automated UX agent review.
