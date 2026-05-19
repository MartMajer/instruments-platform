# D350 Follow-Up Execution Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Execute the D350 private-beta acceptance follow-up queue in disciplined slices, with assessment and handoff correction after each slice.

**Architecture:** Start with a documentation acceptance bar, then implement product changes only where the bar exposes concrete beta blockers. Keep changes tenant-private, synthetic-data safe, and aligned with the active M1 proof spine.

**Tech Stack:** SvelteKit frontend, .NET 9 API/Application/Infrastructure, Postgres/EF Core migrations, PowerShell staging scripts, docs/v2 handoff docs.

---

### Task 1: BR01 acceptance checklist

**Files:**
- Create: `docs/v2/80-agent-handoff/private-beta-acceptance-checklist.md`
- Create: `docs/v2/80-agent-handoff/assessments/D351-POST-BR01-PRIVATE-BETA-ACCEPTANCE-ASSESSMENT-2026-05-20.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- Modify: `docs/v2/80-agent-handoff/OPEN-QUESTIONS.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Steps:**
- Define acceptance route map from registration through respondent completion, results, exports, waves, team, directory, and settings.
- Classify each route as required, known beta limit, or later backlog.
- Close Q-058 by pointing to the checklist.
- Select QB01 as the next implementation slice.
- Commit BR01 docs.

**Verification:**
- Run `git diff --check`.
- Run `git status --short --branch`.

### Task 2: QB01 respondent-format parity

**Files:**
- Inspect before editing: setup workflow, respondent rendering, question/template DTOs, response answer mapping, export/codebook tests.
- Modify only after a failing test identifies the specific gap.

**Steps:**
- Write failing tests or route-smoke assertions for every exposed builder format: rating, recommendation, single choice, multiple choice, number, text, date, and ranking.
- Fix only the gaps the tests expose.
- Update acceptance checklist with result.
- Add post-QB01 assessment and queue correction.
- Commit.

**Verification:**
- Run the focused tests created for QB01.
- Run frontend build or type check only if touched frontend behavior requires it.

**Result - 2026-05-20:**
- Complete. Added respondent payload to the API contract, rendered all exposed beta formats through the respondent SurveyJS adapter, normalized JSON-array answers for multiple-choice and ranking, updated D352, and moved DIR04 to the queue head.

### Task 3: DIR04 CSV audience import MVP

**Files:**
- Inspect before editing: directory page, product API client, subject/group endpoints, command handlers/stores.

**Steps:**
- Define CSV format for people and optional groups.
- Add tests for parsing, validation, duplicate handling, group creation or matching, and safe error messages.
- Add API and UI import path.
- Update checklist and docs.
- Add post-DIR04 assessment and queue correction.
- Commit.

**Verification:**
- Run focused backend tests and frontend build/type check for touched UI.

**Result - 2026-05-20:**
- Implemented locally. Added CSV import endpoint, store path, web API client, and Directory UI panel. Frontend API and endpoint tests passed. Docker-backed store tests were added but could not run because Docker is unavailable locally; run them before deployment.

### Task 4: AUD01 researcher-facing audience selection

**Files:**
- Inspect before editing: setup collection step, respondent-rule preview/save paths, directory read models.

**Steps:**
- Write tests for audience selection labels and saved-rule semantics.
- Replace model-driven rule controls with recipient-oriented selection.
- Keep anonymous invite-only distinction clear.
- Add post-AUD01 assessment and queue correction.
- Commit.

**Verification:**
- Run focused frontend/backend tests for audience save/preview.

**Result - 2026-05-20:**
- Complete. Reworked Setup collection audience controls into recipient-selection language, added readable saved selection and prepared invitation roster presentation, kept existing respondent-rule API mechanics stable, updated D354, and moved RSLT01 to the queue head.
- Verification passed: production web build with explicit Node/Vite command; focused Playwright preview/save tests passed 2/2 against direct Vite preview. Standard Playwright webServer remains locally blocked by `npm` PATH lookup.

### Task 5: RSLT01 multi-output Results setup

**Files:**
- Inspect before editing: Results setup UI, scoring-rule request builder, scoring validation, report/export projections.

**Steps:**
- Write failing tests for two outputs/subscales from one questionnaire.
- Expand visual Results setup to define multiple named outputs.
- Preserve current one-output path as the simplest case.
- Update custom study builder docs and acceptance checklist.
- Add post-RSLT01 assessment and queue correction.
- Commit.

**Verification:**
- Run focused scoring-rule and setup workflow tests.
- Run frontend build/type check for touched UI.

**Result - 2026-05-20:**
- Complete locally. Added multi-output Results setup for dimensions/subscales, generated multi-output scoring graph documents and `produces.scores`, added aggregate node-local missing-policy support in the scoring engine, and updated D355/checklist/queue handoff.
- Verification passed: focused template-authoring Vitest 9/9; focused scoring engine and validator tests 2/2; focused setup-authoring Playwright test 1/1 against direct Vite preview after production build; `git diff --check` passed with only CRLF warnings.

### Task 6: VAL08 validation packet refresh

**Files:**
- Modify: `docs/v2/50-business/current-proof-demo-brief.md`
- Modify: `docs/v2/80-agent-handoff/validation-demo-walkthrough-packet.md`
- Modify: `docs/v2/80-agent-handoff/OWNER-BLOCKERS-ACTION-PACK.md`
- Modify handoff docs as needed.

**Steps:**
- Update walkthrough wording to match current registration/setup/collection/results/waves UI.
- Keep proof-only and Q-053/Q-054 limits explicit.
- Add final assessment and queue correction.
- Commit.

**Verification:**
- Run `git diff --check`.
- If deployed code changed in earlier slices, deploy at stable checkpoint and run VPS public/release checks.
