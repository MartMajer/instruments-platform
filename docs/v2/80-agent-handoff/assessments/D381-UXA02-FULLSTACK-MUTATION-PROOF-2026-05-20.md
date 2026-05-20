# D381 UXA02 full-stack bootstrap mutation proof

## Assessment

UXA02 had a safe action loop, full-stack mode, dev-auth headers, preflight, and bootstrap wrapper, but no green real local mutation proof. Starting Docker Desktop exposed harness issues rather than product blockers:

- relative `--repo-root` values were not resolved before invoking PowerShell
- preflight ran too soon after Compose startup
- local `.env` disabled development authentication, causing 401 from `/auth/session`
- the create-study browser capture could observe a setup-route transcript while keeping a stale list-route snapshot URL, causing a false blocker finding

These were harness defects. The product API did create the campaign series once local dev-auth was enabled.

## Task

Updated UXA02 full-stack bootstrap and mutation detection:

- normalize bootstrap `repoRoot` to an absolute path
- pass local dev-auth process env overrides to the Compose start command when `--fullstack-dev-auth` is requested
- retry bootstrap preflight after `--start` until ready or attempts are exhausted
- normalize autonomous snapshot URL from the rich transcript when the transcript observes a later browser URL than the lightweight snapshot

No `.env` file was edited. No staging or production data was used.

## Verification

- Focused RED tests failed for each observed defect.
- Focused GREEN tests passed: 2/2 files and 17/17 tests.
- Full UXA suite passed: 18/18 files and 124/124 tests.
- Docker Desktop was started locally and reported Engine `29.3.1`.
- Real UXA02 bootstrap reached `ready`:
  - Docker availability passed
  - local staging start passed
  - full-stack preflight passed after 2 attempts
  - API health passed
  - development-auth session passed
  - tenant study read model passed
- Real full-stack mutation passed:
  - command: `autonomous --mission fullstack-create-study --data-mode fullstack --fullstack-dev-auth`
  - artifact: `artifacts/ux-agent-runs/local/run-2026-05-20T18-54-36-933Z/`
  - final URL: `/app/campaign-series/019e46bd-6d56-7d30-807b-adcc0caaa475/setup`
  - status: `completed`
  - findings: 0
  - next-action tickets: 0

## Remaining risk

The local Docker database is not automatically reset between harness runs. Repeated local proof runs create synthetic studies. That is acceptable for this objective because the proof is local-only and uses synthetic data, but a future reset/seed command would make repeated persona runs cleaner.
