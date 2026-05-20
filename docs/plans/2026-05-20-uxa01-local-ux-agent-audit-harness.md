# UXA01 Local UX Agent Audit Harness Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a local-first UX audit harness that captures Playwright evidence for user missions and produces persona-based UX review reports.

**Architecture:** Add a repo-local Node/TypeScript script under `apps/web/scripts/ux-agent-audit/` that uses Playwright to drive local app missions and write evidence packs under `artifacts/ux-agent-runs/`. Keep persona review as prompt/report generation first; do not require live model API wiring in the first slice unless the owner explicitly provides a runner.

**Tech Stack:** SvelteKit web app, Node 24, TypeScript, Playwright, Markdown/JSON artifacts.

---

### Task 1: Define UX audit mission and persona contracts

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/types.ts`
- Create: `apps/web/scripts/ux-agent-audit/personas.ts`
- Create: `apps/web/scripts/ux-agent-audit/missions.ts`
- Test: `apps/web/scripts/ux-agent-audit/mission-contract.test.ts`

**Step 1: Write the failing contract tests**

Create tests that assert every mission has:

```ts
expect(mission.id).toMatch(/^[a-z0-9-]+$/);
expect(mission.goal.length).toBeGreaterThan(20);
expect(mission.maxSteps).toBeGreaterThan(0);
expect(personas[mission.personaId]).toBeDefined();
expect(mission.successCriteria.length).toBeGreaterThan(0);
```

**Step 2: Run the test to verify it fails**

Run:

```powershell
Push-Location apps/web
& 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs run scripts/ux-agent-audit/mission-contract.test.ts
Pop-Location
```

Expected: fails because files do not exist.

**Step 3: Implement minimal contracts**

Create:

- `PersonaDefinition`
- `MissionDefinition`
- `ViewportPreset`
- `FindingSeverity`

Add initial personas:

- `workspace-owner`
- `first-time-researcher`
- `busy-professor`
- `osh-consultant`
- `mobile-respondent`

Add initial missions:

- `auth-enter-workspace`
- `create-first-study`
- `configure-study`
- `prepare-audience`
- `launch-wave`
- `respondent-submit-mobile`
- `review-results-export`
- `review-waves`

**Step 4: Run the test to verify it passes**

Run the same Vitest command.

Expected: tests pass.

**Step 5: Commit**

```powershell
git add apps/web/scripts/ux-agent-audit
git commit -m "Add UX audit mission contracts"
```

### Task 2: Add evidence-pack writer

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/evidence.ts`
- Test: `apps/web/scripts/ux-agent-audit/evidence.test.ts`

**Step 1: Write failing tests**

Test that `createRunDirectory` and `writeMissionEvidence` create:

```text
artifacts/ux-agent-runs/<timestamp>/run.json
artifacts/ux-agent-runs/<timestamp>/missions/<mission-id>/evidence.json
artifacts/ux-agent-runs/<timestamp>/missions/<mission-id>/transcript.md
```

Use a temporary test directory, not the real artifacts folder.

**Step 2: Run the test to verify it fails**

Run:

```powershell
Push-Location apps/web
& 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs run scripts/ux-agent-audit/evidence.test.ts
Pop-Location
```

Expected: fails because writer does not exist.

**Step 3: Implement minimal evidence writer**

Use `node:fs/promises` and `node:path`. Keep output JSON safe and deterministic.

**Step 4: Run the test to verify it passes**

Run the same Vitest command.

Expected: tests pass.

**Step 5: Commit**

```powershell
git add apps/web/scripts/ux-agent-audit
git commit -m "Add UX audit evidence writer"
```

### Task 3: Add Playwright mission runner skeleton

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/run.ts`
- Create: `apps/web/scripts/ux-agent-audit/browser.ts`
- Modify: `apps/web/package.json`
- Test: `apps/web/scripts/ux-agent-audit/runner-options.test.ts`

**Step 1: Write failing tests**

Test CLI option parsing for:

```text
--base-url http://127.0.0.1:5174
--mission create-first-study
--persona first-time-researcher
--viewport desktop
--output ../../artifacts/ux-agent-runs/test
```

Expected parsed config contains base URL, mission filter, persona override, viewport override, and output root.

**Step 2: Run the test to verify it fails**

Run:

```powershell
Push-Location apps/web
& 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs run scripts/ux-agent-audit/runner-options.test.ts
Pop-Location
```

Expected: fails because runner does not exist.

**Step 3: Implement minimal CLI and browser helpers**

Add script to `apps/web/package.json`:

```json
"ux:audit": "tsx scripts/ux-agent-audit/run.ts"
```

If `tsx` is not already available, avoid adding a dependency; use plain JavaScript output or `node --import` only if existing tooling supports it. Do not add a new dependency without checking dependency policy.

Browser helper should:

- launch Chromium through Playwright;
- open `baseUrl`;
- capture page title, URL, visible text excerpt, buttons/links, screenshot;
- write evidence.

**Step 4: Run the test to verify it passes**

Run the same Vitest command.

Expected: tests pass.

**Step 5: Commit**

```powershell
git add apps/web/package.json apps/web/scripts/ux-agent-audit
git commit -m "Add UX audit runner skeleton"
```

### Task 4: Implement first fixed mission

**Files:**
- Modify: `apps/web/scripts/ux-agent-audit/missions.ts`
- Modify: `apps/web/scripts/ux-agent-audit/browser.ts`
- Test: `apps/web/scripts/ux-agent-audit/mission-execution.test.ts`

**Step 1: Write failing tests**

Create a fake page adapter test if direct Playwright is too heavy for unit tests. Assert mission execution records:

- start URL;
- at least one step;
- visible controls;
- screenshot path;
- completion status `completed`, `blocked`, or `error`.

**Step 2: Run the test to verify it fails**

Run focused Vitest.

**Step 3: Implement minimal fixed mission execution**

Start with `auth-enter-workspace` and `create-first-study` as fixed scripted missions. Use visible text and robust roles where possible. Keep selectors conservative.

**Step 4: Run focused tests**

Expected: mission execution unit tests pass.

**Step 5: Run local smoke manually only if local app is running**

Command:

```powershell
Push-Location apps/web
npm run ux:audit -- --base-url http://127.0.0.1:5174 --mission auth-enter-workspace
Pop-Location
```

Expected: evidence directory is created. If local app is not running, command should fail with clear message.

**Step 6: Commit**

```powershell
git add apps/web/scripts/ux-agent-audit
git commit -m "Add first UX audit mission execution"
```

### Task 5: Add persona review prompt generation

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/review-prompt.ts`
- Test: `apps/web/scripts/ux-agent-audit/review-prompt.test.ts`

**Step 1: Write failing tests**

Test prompt includes:

- persona name;
- mission goal;
- hard instruction to use only evidence;
- severity rubric;
- required finding schema;
- screenshot/evidence references.

**Step 2: Run failing test**

Run focused Vitest.

**Step 3: Implement prompt generator**

Do not call any external AI API yet. Write `review-prompt.md` into the mission evidence folder.

**Step 4: Run passing test**

Run focused Vitest.

**Step 5: Commit**

```powershell
git add apps/web/scripts/ux-agent-audit
git commit -m "Add UX persona review prompt generation"
```

### Task 6: Add report normalizer

**Files:**
- Create: `apps/web/scripts/ux-agent-audit/report.ts`
- Test: `apps/web/scripts/ux-agent-audit/report.test.ts`

**Step 1: Write failing tests**

Given fake mission evidence and fake persona findings, assert `summary.md` includes:

- run metadata;
- mission table;
- blocker/confusion counts;
- findings grouped by severity;
- screenshot links.

**Step 2: Run failing test**

Run focused Vitest.

**Step 3: Implement report generator**

Keep schema simple. If no persona findings exist, write "Review pending" and link generated prompts.

**Step 4: Run passing test**

Run focused Vitest.

**Step 5: Commit**

```powershell
git add apps/web/scripts/ux-agent-audit
git commit -m "Add UX audit report summary"
```

### Task 7: Document operation and handoff

**Files:**
- Create: `docs/v2/80-agent-handoff/ux-agent-audit-harness.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`

**Step 1: Write operation doc**

Include:

- purpose;
- local setup assumptions;
- command examples;
- artifact locations;
- how to interpret blocker/confusion/polish/beta-limit findings;
- rule that findings do not become product changes until owner reviews them.

**Step 2: Run docs diff check**

Run:

```powershell
git diff --check
```

Expected: no errors, CRLF warnings acceptable.

**Step 3: Commit**

```powershell
git add docs/v2/80-agent-handoff/ux-agent-audit-harness.md docs/v2/80-agent-handoff/NEXT-ACTIONS.md docs/v2/80-agent-handoff/SESSION-LOG.md
git commit -m "Document UX audit harness operation"
```

### Task 8: Final verification

**Files:**
- All files touched by UXA01.

**Step 1: Run focused tests**

Run:

```powershell
Push-Location apps/web
& 'D:\Program Files\nodejs\node.exe' node_modules\vitest\vitest.mjs run scripts/ux-agent-audit
Pop-Location
```

Expected: UX audit tests pass.

**Step 2: Run web build if package wiring changed**

Run:

```powershell
Push-Location apps/web
& 'D:\Program Files\nodejs\node.exe' node_modules\@sveltejs\kit\svelte-kit.js sync
& 'D:\Program Files\nodejs\node.exe' node_modules\vite\bin\vite.js build
Pop-Location
```

Expected: build passes with only known large-chunk warning.

**Step 3: Run docs/code whitespace check**

Run:

```powershell
git diff --check
```

Expected: no errors, CRLF warnings acceptable.

**Step 4: Commit final fixes if needed**

Commit any final fixups.

**Step 5: Report**

Report:

- commands run;
- artifact path from any local smoke;
- known limits;
- whether staging/cookie mode is still deferred.

## D369 implementation note - local-full transcript primitive

The harness now has the transcript primitive needed before autonomous persona driving:

- runner default capture mode is `local-full`
- `--capture-mode safe` keeps the original thinner capture path available
- non-local base URLs fail closed before browser launch
- fixed missions attach local transcript snapshots to evidence observations
- prompt generation presents transcript data as local audit evidence for persona reviewers

The remaining implementation gap is not evidence capture; it is autonomous action selection. A future slice should introduce a bounded action schema such as `click(label)`, `fill(label,value)`, `select(label,value)`, and `stop_and_complain`, then let persona reviewers choose the next safe action from captured visible controls.
