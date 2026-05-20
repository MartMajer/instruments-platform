# Next Session Plan - Post-D357

Current queue head: no agent-executable slice is selected by default.

Recommended next move: if the owner wants mobile to become a first-class product surface, start a larger responsive/mobile navigation goal. That should include screenshot review at phone/tablet widths, a real mobile navigation decision, route-specific content priority, and staging deployment proof. D357 only added the first shared CSS cleanup pass.

If deployment is requested first, remember the branch is ahead locally and DIR04 CSV audience import still has Docker-backed store proof pending.

# Next Session Plan - Post-D356

Current queue head: no agent-executable D350 follow-up slice is selected by default.

Recommended next move: either owner-run validation using the refreshed packet, or an explicit deployment/runtime proof pass if the owner wants these local commits on staging before calls. Do not invent another product slice without validator feedback, a deployment need, or a new owner priority.

If deployment is requested, remember the open gate: DIR04 CSV audience import has Docker-backed store tests that could not run locally because Testcontainers could not connect to Docker. Run a Docker-enabled proof or include the risk explicitly in release notes.

O01/O02/O03 remain owner-only. Q-053 still blocks real-person production legal/GDPR/DPA claims. Q-054 still blocks outbound operational-notification email claims.

# Next Session Plan - Post-D355

Current queue head: VAL08 validation packet refresh.

Recommended next slice: update the owner-facing validation materials so they match the current app after registration/auth hardening, setup cleanup, collection/results/waves/settings/sidebar cleanup, QB01 respondent-format parity, DIR04 CSV import, AUD01 recipient selection, and RSLT01 multi-output Results setup.

Keep the packet honest. The app is still an owner-controlled private-beta proof, Q-053 still blocks real-person production legal/GDPR/DPA claims, Q-054 still blocks outbound operational-notification email claims, and DIR04 Docker-backed store proof still needs a Docker-enabled run before deployment. Do not imply platform-canonical named instruments, norms, interpretation bands, or a multi-workspace account picker exist.

# Next Session Plan - Post-D354

Current queue head: RSLT01 multi-output Results setup for dimensions/subscales.

Recommended next slice: make Results setup support more than one named output from the same questionnaire. Keep the current one-output total score path as the simplest case, but let a researcher add dimensions/subscales with their own included questions, calculation, and missing-answer policy.

Do not turn this into a full psychometrics workbench, norms engine, interpretation-band system, or SurveyMonkey clone. The beta need is production-shaped custom-study scoring: multiple named outputs, clear per-output question selection, safe validation, and compatibility with the report/export paths that already operate on dimension codes.

AUD01 is complete for the normal setup path. DIR04 remains a deployment gate because its Docker-backed store tests still need a Docker-enabled run.

# Next Session Plan - Post-D353

Current queue head: AUD01 researcher-facing audience selection.

Recommended next slice: make collection audience setup read like recipient selection instead of respondent-rule programming. A researcher should be able to choose "everyone in this study audience", "people in this group", or the relevant manager/report relationship in plain language, preview who will receive invitations, save the selection, and understand why launch is blocked when the audience is empty.

Preserve the existing backend respondent-rule mechanics unless the UX cannot be made coherent on top of them. Do not build HRIS sync, cryptographic anonymity guarantees, or a full audience-builder DSL. CSV import exists from DIR04, but its Docker-backed store tests still need a Docker-enabled run before deployment.

# Next Session Plan - Post-D352

Current queue head: DIR04 CSV audience import MVP for people and groups.

Recommended next slice: implement the smallest production-shaped CSV import that lets a researcher prepare a normal study audience without manually creating every person and group. The import should be tenant-scoped, explicit about accepted columns, safe on duplicates, and understandable from the Directory/collection preparation path.

Keep the scope narrow. Do not build a full HRIS integration, background import pipeline, enrichment system, or complex merge wizard. The beta need is: upload/paste a CSV, validate rows, create/update people and group memberships predictably, show row-level failures, and leave enough audit/session-log context for a researcher to trust the audience before launch.

QB01 is complete for respondent-format renderer parity. Remaining broader web check issues are unrelated existing TypeScript/Svelte errors captured in D352; do not mix those cleanup items into DIR04 unless they block the import path directly.

# Next Session Plan - Post-D351

Current queue head: QB01 respondent-rendering parity for every exposed question format.

Recommended next slice: prove the builder-to-respondent path for rating, recommendation, single choice, multiple choice, number, text, date, and ranking. If a format cannot be rendered, saved, submitted, exported, or explained safely in the current respondent runtime, either fix the path or remove/reclassify that format from the beta builder.

Use `private-beta-acceptance-checklist.md` as the acceptance bar. Do not add branching, matrix/grid, file upload, SurveyJS Creator, platform-canonical named instruments, or real-person data. Keep RSLT01 multi-output Results setup separate unless QB01 exposes a narrow scoring compatibility issue.

# Next Session Plan - Post-D350

Current queue head: BR01 M1 private-beta acceptance checklist and route audit.

Recommended next slice: define the exact private-beta acceptance path before
adding another product feature. The current app can now register/sign in, create
or open a workspace, build a study, configure questionnaire/results/collection,
launch, collect, review results, export CSV, compare waves, manage team,
prepare audiences, and run on VPS staging. The risk is that those paths are not
yet expressed as one executable beta bar.

BR01 should produce a checklist that classifies each beta route as pass,
blocker, known beta limit, or later backlog. It should cover registration and
sign-in, study creation, questionnaire formats, Results setup, audience setup,
anonymous invite-only or open-link collection, respondent completion, scoring,
Results/Waves, CSV/codebook/PDF availability, Team, Directory, Settings, and
sign-out/account recovery.

Do not reopen dormant platform-canonical named-instrument content. Do not treat
Q-053 as closed. Do not jump to M2/M3 breadth before the current M1 builder and
audience flow are beta-coherent.

# Next Session Plan - Post-D348

Current queue head: staging owner retest and post-auth/onboarding assessment.

Recommended next slice: do not deploy automatically from this local hardening pass without owner approval. If the owner wants runtime proof, deploy the current auth/onboarding hardening commit to staging, then retest the full browser route: new workspace registration, email verification reminder, sign-out, existing workspace sign-in through `/signin`, wrong-account recovery, team member first sign-in link, `/app` first-run runway, `/app/team`, and `/app/directory`. If staging passes, remove this from the active auth queue and return to owner-only O01/O02/O03 validation unless the owner selects a specific product/design slice.

Verification already completed locally for D348: web production build passed; focused Playwright auth/onboarding run passed 8/8 against local production preview; Q-056 and Q-057 remain open; no deployment was performed.

# Next Session Plan - Post-D347

Current queue head: owner-only O01/O02/O03. No unblocked agent-executable slice is selected by default.

Recommended next move: the owner should run at least O01 or provide validation feedback/notes. Agent work should resume only from concrete owner feedback, a specific owner-requested implementation slice, or a reopened deferred item. If the owner provides call notes, start with feedback intake: classify green/yellow/red, extract named study/client/pilot/referral/grant signals, update O01/O02/O03 status, then select the next product/runtime slice from actual objections.

Do not treat this pause as project completion. Q-053 still blocks real-person production legal/GDPR/DPA claims. Q-054 still blocks outbound operational-notification email workflows/claims. Dormant canonical named-instrument content remains blocked without a future owner/legal canonical-publish decision.

# Next Session Plan - Post-D346

Current queue head: D347 post-D346 product/runtime assessment.

Recommended next slice: reassess deliberately. The owner-facing validation materials now match the live VPS rehearsal proof, while O01/O02/O03 remain owner-only and strategically highest priority. Candidate outcomes are: pause agent work until owner validation feedback, prepare a narrow observability/runbook polish slice if evidence capture remains hard to operate, or return to product/runtime only if a concrete gap is stronger than waiting for validation. Preserve Q-053/Q-054 and avoid standalone UX.

Verification already completed for D346: docs-only update; no code changed; refreshed current proof demo brief, owner blocker action pack, and validation demo walkthrough packet; Q-053/Q-054 and canonical named-instrument boundaries remain explicit; local/dev fallback remains documented.

# Next Session Plan - Post-D345

Current queue head: D346 VPS-current validation packet refresh.

Recommended next slice: update owner-facing validation materials to match the current live VPS proof. The current proof demo brief, owner blocker action pack, and validation walkthrough packet still describe the proof mostly as local/dev and still underclaim now-proven PDF, withdrawal, Auth0, VPS backup/restore, redeploy/rollback, and authenticated VPS product-spine evidence. Keep this docs-only: no product code, no new claims beyond engineering proof, no real-person data, no outbound operational-notification email, and no standalone UX.

Verification target for D346: docs updated to mention the current VPS rehearsal lane and D342/D344 proof boundaries; local/dev fallback remains documented; Q-053/Q-054 boundaries stay explicit; canonical named-instrument boundaries stay explicit; no tests/build needed unless code changes.

# Next Session Plan - Post-D344

Current queue head: D345 post-D344 product/runtime assessment.

Recommended next slice: reassess from evidence. The real VPS target now has both the infrastructure rehearsal proof from D342 and the broad authenticated QA01 product-spine proof from D344. Do not add more smoke depth by default. Candidate limiters are owner validation prep, report/export/PDF residual quality, tenant-private import/setup ergonomics, observability/runbook gaps, or another runtime proof only if the D344 evidence shows a weak path. Preserve Q-053 and Q-054 limits and avoid standalone UX.

Verification already completed for D344: RED focused static test failed on missing `SessionCookiePath`; GREEN focused static test passed 1/1; required-auth no-cookie command exited 1 before remote calls; authenticated VPS product-spine smoke passed against `https://validatedscale-api-staging.croat.dev` and `https://validatedscale-staging.croat.dev` using the owner cookie loaded from the VPS in memory; evidence recorded `remoteCookieAuthenticated`, authenticated session true, setup.manage true, CSRF token true, product milestones true, in-app-only operational notification proof true, operational email false, SMTP delivery false, and failed-requeue recovery false; full `StagingWorkerDeploymentPackageTests` passed 93/93; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D343

Current queue head: D344 authenticated VPS product-spine smoke harness.

Recommended next slice: implement D344 test-first. Extend the existing QA01 product-spine smoke path so it can run against the real VPS API/web origins using an owner-supplied browser Cookie header from an ignored file or `STAGING_SESSION_COOKIE`, without printing or writing the cookie. Keep the local/dev-auth path working. The remote path should prove authenticated tenant-member product-spine behavior with owner-controlled synthetic/demo data, write safe evidence, and remain explicit that Q-053 and Q-054 are not closed.

Verification target for D344: RED focused deployment-package/static test fails on missing authenticated remote cookie support for product-spine smoke; GREEN focused test passes; required-auth no-cookie path fails closed before remote calls; public/local product-spine smoke still works or static parser validation passes if runtime local stack is not being exercised; authenticated VPS product-spine smoke passes with `/tmp/staging-session.cookie` if the owner cookie is still valid; full relevant staging deployment-package tests pass; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D342

Current queue head: D343 post-VPS-hardening product/runtime assessment.

Recommended next slice: reassess from product/runtime evidence rather than extending the VPS hardening thread by default. The VPS staging rehearsal lane now has owner-authenticated remote smoke, target backup/restore proof, an owner-runnable release evidence command, redeploy proof, rollback/restore proof, and safe evidence boundaries. Preserve Q-053 and Q-054: no real-person production legal/GDPR/DPA claims and no outbound operational-notification email workflows/claims. Avoid standalone UX unless it exposes already-proven backend lanes.

Verification already completed for D342: VPS checkout `staging...origin/staging` at `d220e5172ff5a0e59648f456b5579d5b58fc2c60`; `/tmp/staging-session.cookie` existed with mode `rw-------`; `bash deploy/staging/run-vps-release-checks.sh --evidence-dir /tmp/d342-authenticated-vps-release-evidence --session-cookie-file /tmp/staging-session.cookie --require-authenticated-session` passed; release evidence recorded `remotePublicSmokeProven=true`, `vpsBackupRestoreProven=true`, `authenticatedRemoteSmokeProven=true`, `legalGdprReady=false`, and `operationalNotificationEmailReady=false`; backup/restore evidence recorded backup bytes `333277`, restore public table count `53`, restore public relation count `52`, and required platform tables present true; committed redeploy and rollback evidence remained available under `/tmp/d340-vps-redeploy-evidence-committed` and `/tmp/d341-vps-rollback-evidence-committed`.

# Next Session Plan - Post-D333

Current queue head: D334 post-live-VPS staging assessment.

Recommended next slice: reassess deliberately before adding more scope. The real VPS staging stack is live behind Nginx/TLS, OIDC callback generation honors forwarded HTTPS, OIDC failures fall back to the web origin, the owner Auth0 email is seeded as a staging admin, API data-protection keys persist in a named volume, and admin seeding is repeatable through `deploy/staging/seed-staging-admin.ps1`.

Next assessment should choose between authenticated remote product-spine smoke, target-environment backup/restore proof, staging runbook/operator documentation, or another deployment-operability hardening step. Do not claim real-person legal/GDPR readiness while Q-053 is open, do not add outbound operational-notification email while Q-054 is open, and keep UX/UI out unless it exposes proven backend lanes.

Verification already completed for D333: RED focused staging hardening tests failed 2/2; GREEN focused tests passed 2/2; full `StagingWorkerDeploymentPackageTests` passed 86/86; local/VPS Compose config rendered; `dotnet build --no-restore` passed with 0 warnings/errors; `seed-staging-admin.ps1` reran idempotently for the owner Auth0 email with counts `1/1/2/1`; VPS API mount check showed `platform_staging_data_protection_keys /root/.aspnet/DataProtection-Keys`; public smoke returned API health 200, web root 200, unauthenticated session 401, and login 302.

# Next Session Plan - Post-D255

Current queue head: D256 product-spine withdrawal report PDF artifact invalidation smoke.

Recommended next slice: implement D256 test-first. Extend the D254 artifact invalidation assertion list so the pre-withdrawal report PDF artifact is included when it succeeded. Do not force report PDF success in environments where renderer/storage is unavailable, and do not add new report PDF invalidation semantics, UI/UX, notification email routing, or legal/GDPR production claims.

Verification target for D256: RED focused static deployment test fails on missing report PDF invalidation smoke; GREEN focused static test passes; full `StagingWorkerDeploymentPackageTests` passes; parser validation passes; live product-spine smoke passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D254

Current queue head: D255 post-D254 product/runtime assessment.

Recommended next slice: reassess deliberately. Withdrawal execution is now live-proven through token issue/consume, approve/execute, terminal notification, notification summary/mark-read, and derived artifact invalidation. Choose the next limiter from evidence: report/PDF residuals if artifact delivery/report value remains weakest, deployment/operability if runtime gates need hardening, operational-notification email routing only if recipient policy is ready, SMTP live proof only if an owner-controlled SMTP target is available, withdrawal/admin workflow closure if another trust gap remains, or UX only where it exposes proven backend lanes.

Verification already completed for D254: RED focused static deployment test failed on missing invalidation smoke; GREEN focused static test passed; full `StagingWorkerDeploymentPackageTests` passed 25/25; PowerShell parser validation passed; live product-spine smoke passed after narrowing the assertion to the existing invalidation contract; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D253

Current queue head: D254 product-spine withdrawal derived artifact invalidation smoke.

Recommended next slice: implement D254 test-first. Extend the product-spine smoke so after withdrawal execution it refetches the affected wave-1 report export and campaign-series response export artifacts and asserts they are deleted, non-downloadable, have `deletedAt`, clear checksums, and do not expose old CSV payload markers. Do not add new withdrawal semantics, report/PDF artifact type changes, UI/UX, notification email routing, or legal/GDPR production claims.

Verification target for D254: RED focused static deployment test fails on missing invalidation smoke; GREEN focused static test passes; full `StagingWorkerDeploymentPackageTests` passes; parser validation passes; live product-spine smoke passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D252

Current queue head: D253 post-D252 product/runtime assessment.

Recommended next slice: reassess deliberately. Operational notifications now have backend events, list, summary, mark-read, and live product-spine proof. Choose the next limiter from evidence: operational-notification email routing only if recipient policy is ready, report/export residual value if artifact/report delivery remains more important, withdrawal/admin workflow closure if trust workflow needs more runtime proof, deployment operability if the stack needs another runtime gate, SMTP live proof if an owner-controlled SMTP environment is ready, or UX only where it exposes proven backend lanes.

Verification already completed for D252: RED focused static deployment test failed on missing summary/mark-read smoke; GREEN focused static test passed 1/1; full `StagingWorkerDeploymentPackageTests` passed 24/24; PowerShell parser validation passed; live `smoke-product-spine.ps1` passed against local staging; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D251

Current queue head: D252 product-spine operational notification summary and mark-read smoke.

Recommended next slice: implement D252 test-first. Extend the product-spine smoke so the withdrawal terminal operational notification is also checked through `/operational-notifications/summary` and `/operational-notifications/{id}/mark-read`, with safe-output assertions and summary decrement checks. Do not add notification UI, outbound operational-notification email routing, new notification types, SMTP sending, or schema changes.

Verification target for D252: RED focused static deployment test fails on missing summary/mark-read smoke; GREEN focused static test passes; full `StagingWorkerDeploymentPackageTests` passes; PowerShell parser validation passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D250

Current queue head: D251 post-D250 product/runtime assessment.

Recommended next slice: reassess deliberately. Campaign email delivery now has local-dev live smoke, staging config, readiness validation, aggregate operations visibility, and manual failed-invitation requeue recovery. Choose the next limiter from product evidence: live SMTP proof if an owner-controlled SMTP environment is ready, operational-notification email routing if recipient policy is ready, report/export residual value if artifact/report delivery remains the bigger owner-demo gap, withdrawal/admin workflow closure if trust workflow is still incomplete, deployment operability if runtime proof is weaker, or UX only where it exposes proven backend lanes.

Verification already completed for D250: RED focused tests failed on missing requeue contracts; GREEN focused unit tests passed 5/5; focused endpoint plus Docker-backed store recovery tests passed 3/3; broader notification delivery integration regression passed 20/20; related unit regression passed 59/59; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D249

Current queue head: D250 failed campaign email invitation requeue foundation.

Recommended next slice: implement D250 test-first. Add a setup-authorized `POST /campaigns/{id}/notification-deliveries/requeue-failed` action that requeues failed email invitation notifications for a live campaign, excludes withdrawal-scrubbed notifications, preserves prior delivery attempts, and returns only safe aggregate counts. Do not add SMTP live sending, automatic retry scheduling/backoff, notification UI, operational-notification email routing, bounce/complaint/unsubscribe handling, or provider-specific dependencies.

Verification target for D250: RED focused tests fail on missing contracts/domain/store endpoint; GREEN focused tests pass; Docker-backed store recovery proves failed invitation requeue then delivery and withdrawal-scrubbed exclusion; existing notification endpoint/store regression passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D248

Current queue head: D249 post-D248 product/runtime assessment.

Recommended next slice: reassess deliberately. Email delivery now has local-dev live smoke, explicit staging provider/SMTP config, and readiness validation. The next assessment should choose between SMTP live proof, campaign delivery retry/visibility, operational-notification email routing, report/export residual value, withdrawal/admin workflow closure, deployment operability, or UX only where it exposes proven backend lanes.

Verification already completed for D248: RED focused health/config test failed on missing `EmailDeliveryConfigurationHealthCheck`; GREEN focused health/config tests passed 4/4; broader `HealthEndpointTests|EmailDeliveryOptionsContractTests|EmailDeliveryProviderTests|NotificationDeliveryContractTests` passed 45/45.

# Next Session Plan - Post-D247

Current queue head: D248 email delivery configuration readiness guard.

Recommended next slice: implement D248 test-first. Add a safe `email_delivery_configuration` platform health check that reuses `EmailDeliveryOptions.EnsureValidProviderConfiguration()`, returns `ok` for local-dev, returns `unready` for invalid provider/SMTP config, and exposes only health-check name/status. Do not add real SMTP sending, operational-notification email routing, scheduled workers, notification UI, or provider-specific dependencies.

Verification target for D248: RED focused health/config test fails on missing check; GREEN focused health/config tests pass; existing email delivery option/provider tests pass; `HealthEndpointTests` pass; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D246

Current queue head: D247 post-D246 product/runtime assessment.

Recommended next slice: reassess deliberately before implementation. The campaign email delivery path is now live-smoke proven through `local-dev` and has explicit staging configuration. Choose the next limiter from product evidence, not UI desire: SMTP live proof only if staging needs real outbound mail now, operational-notification email routing if in-app notification events need outbound delivery, report/export residual value if artifact delivery remains the bigger owner-demo gap, withdrawal/admin workflow closure if withdrawal trust is still incomplete, or deployment/operability if runtime proof is the blocker.

Verification already completed for D246: RED focused deployment package test failed on missing API email config; GREEN focused test passed 1/1; full `StagingWorkerDeploymentPackageTests` passed 23/23; `node --test tests/deployment-package/*.test.mjs` passed 24/24; local and VPS Compose configs rendered and showed the explicit local-dev provider plus SMTP placeholders.

# Next Session Plan - Post-D245

Current queue head: D246 staging email delivery configuration surface.

Recommended next slice: implement D246 test-first. Expose `EmailDelivery` provider/from-address/SMTP settings in API Compose and local/VPS env examples, keep `local-dev` as the safe default, and document the SMTP switch in the VPS staging runbook. Do not add SMTP secrets, notification UI, scheduled email workers, operational-notification email routing, or provider-specific vendor dependencies.

Verification target for D246: RED focused deployment test fails on missing config; GREEN focused deployment test passes; full `StagingWorkerDeploymentPackageTests` passes; deployment-package Node tests pass if relevant; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D244

Current queue head: D245 post-D244 product/runtime assessment.

Recommended next slice: reassess deliberately. Operational notifications now have a generic `sourceStatus` contract and compatibility alias, live-proven through product-spine smoke. The next assessment should choose between operational-notification email routing, report/export residual value, deployment/operability hardening, withdrawal closure cleanup, or UX only where it exposes proven backend lanes.

Verification already completed for D244: RED focused notification contract tests failed on missing `SourceStatus`; GREEN focused notification contract tests passed 2/2; Docker-backed operational-notification API/store regression passed 9/9; full `StagingWorkerDeploymentPackageTests` passed 22/22; PowerShell parser validation passed; local staging API/worker rebuild passed; live product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D243

Current queue head: D244 operational notification source-status contract.

Recommended next slice: implement D244 test-first. Add generic `sourceStatus` to operational notification responses, preserve `artifactStatus` as a report/PDF compatibility alias, and update the product-spine withdrawal terminal notification smoke to assert completed status through `sourceStatus`. Do not add notification UI, operational-notification email routing, new notification types, or persistence migrations.

Verification target for D244: RED focused notification contract tests fail before implementation; GREEN focused notification contract tests pass; operational-notification API/store regression passes; staging deployment package tests and parser validation pass; live product-spine smoke passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D242

Current queue head: D243 post-D242 product/runtime assessment.

Recommended next slice: reassess deliberately. Withdrawal lifecycle and terminal notifications are live-smoke proven, and existing campaign email invitation delivery is now live-smoke proven through the local-dev provider. The next assessment should choose between operational-notification email routing, report/export residual value, deployment/operability hardening, withdrawal closure cleanup, or UX only where it exposes proven backend lanes.

Verification already completed for D242: RED focused static test failed on missing email delivery smoke helper; GREEN focused static test passed; full `StagingWorkerDeploymentPackageTests` passed 22/22; PowerShell parser validation passed; live product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D241

Current queue head: D242 product-spine campaign email invitation delivery smoke.

Recommended next slice: implement D242 test-first. Extend `deploy/staging/smoke-product-spine.ps1` with a separate anonymous email campaign so invitation batches can be queued and delivered through `/campaigns/{id}/notification-deliveries/process` using the existing local-dev provider. Do not add operational-notification email routing, SMTP deployment changes, notification UI, scheduled workers, or new campaign semantics.

Verification target for D242: RED focused static test fails on missing email delivery smoke helper; GREEN focused static test passes; full `StagingWorkerDeploymentPackageTests` passes; PowerShell parser validation passes; live product-spine smoke passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D240

Current queue head: D241 post-D240 product/runtime assessment.

Recommended next slice: reassess deliberately. Withdrawal terminal notification evidence is now Docker-backed and live product-spine proven for completed requests. The next assessment should decide whether the limiting risk is notification/email routing, report/export residual value, deployment/operability hardening, withdrawal closure cleanup, or UX only where it exposes proven backend lanes.

Verification already completed for D240: RED focused static test failed on missing terminal notification smoke helper; GREEN focused static test passed; full `StagingWorkerDeploymentPackageTests` passed 21/21; PowerShell parser validation passed; local staging API/worker rebuild passed; live product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D239

Current queue head: D240 product-spine withdrawal terminal notification smoke.

Recommended next slice: implement D240 test-first. Extend `deploy/staging/smoke-product-spine.ps1` so the withdrawal request approved and executed by the smoke is also asserted through the operational notification API as a safe terminal `withdrawal_request_terminal` notification. Do not add email routing, notification UI, scheduled retention jobs, deployment automation, legal/GDPR production claims, or new withdrawal graph semantics.

Verification target for D240: RED focused static deployment test fails on missing smoke assertion; GREEN focused static test passes; full `StagingWorkerDeploymentPackageTests` passes; PowerShell parser validation passes; live product-spine smoke passes if staging is available; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D238

Current queue head: D239 post-D238 product/runtime assessment.

Recommended next slice: reassess deliberately. Withdrawal now has request-created and terminal denied/completed/failed in-app notification evidence, plus live product-spine issue/consume/approve/execute proof. The next assessment should choose between live smoke coverage for terminal withdrawal notifications, notification/email routing, report/export residual value, deployment/operability hardening, or UX only where it exposes proven backend lanes.

Verification already completed for D238: RED focused terminal-notification tests failed on missing constants; GREEN focused terminal-notification tests passed 2/2; Docker-backed withdrawal/notification regression passed 34/34; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D237

Current queue head: D238 withdrawal terminal operational notifications.

Recommended next slice: implement D238 test-first. The full withdrawal lifecycle is now live-proven through local staging, but operational notifications still stop at request-created. Add pointer-only terminal in-app notifications for denied, completed, and failed withdrawal states inside the existing tenant-scoped runtime transactions. Do not add email routing, notification UI, scheduled retention jobs, deployment automation, legal/GDPR production claims, or new delete/anonymize semantics.

Verification target for D238: RED focused denied/completed terminal notification tests fail before implementation; GREEN focused tests pass after implementation; Docker-backed withdrawal/notification regression filter passes; `dotnet build --no-restore` passes.

# Next Session Plan - Post-D236

Current queue head: D237 post-D236 product/runtime assessment.

Recommended next slice: reassess deliberately. The withdrawal lane now has tenant/admin create/review/approve/deny/execute, anonymous token issue/consume, live product-spine issue/consume/approve/execute proof, request-created operational notifications, and runtime delete/anonymize execution coverage. The next assessment should choose between terminal withdrawal notifications, report/export residual value, deployment/operability hardening, notification/email routing, or UX only where it supports these proven backend lanes.

Verification already completed for D236: RED focused static smoke test failed on missing approve/execute helper; GREEN focused static smoke test passed; full `StagingWorkerDeploymentPackageTests` passed 20/20; PowerShell parser validation passed; staging API/worker rebuild passed; live product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D235

Current queue head: D236 product-spine withdrawal approve/execute smoke coverage.

Recommended next slice: implement D236 test-first. Extend `deploy/staging/smoke-product-spine.ps1` so the withdrawal request created by token issue/consume is approved and executed through the live staging API after existing report/export assertions. Do not add terminal notifications, email routing, UI, scheduled automation, or new mutation semantics in this slice.

Verification target for D236: focused static deployment test, full staging deployment package test class, PowerShell parser validation, live product-spine smoke, and `dotnet build --no-restore`.

# Next Session Plan - Post-D234

Current queue head: D235 post-D234 product/runtime assessment.

Recommended next slice: reassess before building. Withdrawal now has tenant/admin create/review/approve/deny/execute, anonymous token issue/consume, live product-spine token smoke, request-created operational notifications, and runtime delete/anonymize execution coverage. The next assessment should choose deliberately between terminal withdrawal notifications, report/export residual value, deployment/operability hardening, notification/email routing, or UX only where it supports these proven lanes.

Verification already completed for D234: RED focused withdrawal endpoint tests failed on missing execute route; GREEN focused withdrawal endpoint tests passed 20/20; Docker-backed response-session execution/request-decision/anonymous-token regression passed 16/16; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D233

Current queue head: D234 tenant/admin withdrawal request execution API foundation.

Recommended next slice: implement D234 test-first. Response-session withdrawal execution is implemented and Docker-tested in the runtime store, but the tenant/admin API currently stops at approve/deny. Add `POST /withdrawal-requests/{id}/execute` behind tenant context plus `setup.manage`, delegate to `IWithdrawalRuntimeStore.ExecuteWithdrawalAsync`, and return the existing safe execution-state response. Do not add UI, terminal notifications, scheduled automation, email, or new mutation semantics in this slice.

Verification target for D234: focused withdrawal endpoint tests, Docker-backed response-session withdrawal execution/request/token regression, and `dotnet build --no-restore`.

# Next Session Plan - Post-D232

Current queue head: D233 post-D232 product/runtime assessment.

Recommended next slice: reassess deliberately. D232 proved authenticated withdrawal token issue plus public anonymous consume through live local staging and fixed the audit tenant-context bug that live smoke exposed. The next assessment should choose between report/export residuals, deployment/operability hardening, terminal withdrawal notifications, notification/email routing, or UX only where it supports proven backend lanes.

Verification already completed for D232: RED static smoke test failed on missing helper; GREEN static deployment test passed; full `StagingWorkerDeploymentPackageTests` passed 19/19; PowerShell parser validation passed; first live product-spine smoke failed on missing application tenant context for audited anonymous consume; Docker-backed audit regression passed after fix; Docker-backed `Anonymous_withdrawal_token` filter passed 8/8; staging API/worker rebuild passed; live product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D231

Current queue head: D232 product-spine withdrawal token issue/consume smoke coverage.

Recommended next slice: implement D232 test-first. Extend `deploy/staging/smoke-product-spine.ps1` so local staging proves an authenticated tenant operator can issue a one-shot withdrawal token for a submitted response session and the public anonymous endpoint can consume it into a requested withdrawal request without echoing the raw token. Do not add respondent UI, email delivery, token recovery/listing, automatic issuance, or delete/anonymize execution in this slice.

Verification target for D232: focused deployment package test, PowerShell parser validation, live product-spine smoke if local staging is available, and `dotnet build --no-restore`.

# Next Session Plan - Post-D230

Current queue head: D231 post-D230 product/runtime assessment.

Recommended next slice: reassess, do not jump straight to UX. D230 made anonymous withdrawal tokens reachable from a setup-authorized tenant API while preserving hash-only storage and one-time raw-token return. The next assessment should choose deliberately between terminal withdrawal notifications, respondent token delivery UI, report/export residual value, deployment/operability hardening, notification/email routing, or UX only where it supports proven backend lanes.

Verification already completed for D230: RED focused withdrawal endpoint tests failed on missing request contract; GREEN focused withdrawal endpoint tests passed 18/18; Docker-backed `Anonymous_withdrawal_token` tests passed 7/7 with `RUN_POSTGRES_INTEGRATION_TESTS=1`; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-D229

Current queue head: D230 authenticated withdrawal request token issue API foundation.

Recommended next slice: implement D230 test-first. Anonymous withdrawal token consumption is implemented and safety-tested, but no mapped product/API boundary issues a token. Add a setup-authorized tenant endpoint that issues a one-shot withdrawal token for a known response session, returns the raw token once, and preserves hash-only storage. Do not add email delivery, respondent UI, token recovery/listing, or automatic submit-time issuance in this slice.

Verification target for D230: focused withdrawal endpoint tests, existing Docker-backed anonymous withdrawal token tests if store behavior is touched, and `dotnet build --no-restore`.

# Next Session Plan - Post-D228

Current queue head: D229 withdrawal trust closure assessment after live staging proof.

Recommended next slice: withdrawal trust, not UX. D228 proved the local staging worker/outbox/report-export product spine and fixed real runtime blockers behind worker heartbeat, outbox relay tenant context, report-PDF terminal notifications, operational notification transaction reuse, and smoke portability. Use that stronger runtime base to reassess the withdrawal lane: authenticated tenant/admin withdrawal request lifecycle, anonymous withdrawal-token residuals, audit/anonymization safety, retention integration, notification needs, and remaining non-UX closure gaps.

Course to keep hammering through goals and assessment/task pairs:

- D229/D230: withdrawal trust closure.
- D231/D232: report/export residual value after live product-spine proof.
- D233/D234: deployment/operability gates after local staging proof.
- D235/D236: notifications only where they support withdrawal/report/export operations.
- D237: UX only for proven backend product lanes.

Verification already completed for D228: focused staging worker deployment tests passed 18/18; Docker-backed outbox/notification/heartbeat/store tests passed; runtime outbox/report-PDF handler tests passed 38/38; deployment-package Node tests passed 24/24; local and VPS Compose config rendered; local staging smoke passed; product-spine smoke passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.

# Next Session Plan - Post-OPS17

Current queue head: D228 report/export product completeness assessment after deployment readiness.

Recommended next slice: assess report/export value depth before UX. Candidate directions are live product-spine smoke, product-level PDF rendering proof through the running API/worker path, enabling report PDF worker automation for a controlled staging profile, or closing remaining owner-demo report content gaps.

Verification already completed for OPS17: local and VPS compose configs passed; focused staging worker deployment tests passed 16/16; deployment-package Node tests passed 24/24; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
# Next Session Plan - Post-NOTIF07

Current queue head: D227 deployment/operability readiness assessment after notification finalization.

Recommended next slice: deployment/operability. Report/export, withdrawal, and operational notifications now have backend trust paths; before UX, reassess live staging readiness, worker automation toggles, smoke coverage, env defaults, runbook gaps, and backup/restore gates.

Verification already completed for NOTIF07: focused anonymous withdrawal token consume test passed 1/1 after RED; Docker-backed withdrawal/operational-notification store regression passed 79/79; operational notification API filter passed 6 with 1 Docker-gated skip; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
# Next Session Plan - Post-QA19

Current queue head: D226 notification finalization assessment after withdrawal/report trust checks.

Recommended next slice: notification work only where it supports operational trust for withdrawal and report/export events. Candidate gaps: requester/admin withdrawal request notifications, email delivery routing, smoke coverage for terminal notifications, or proving current notification surface is sufficient and moving to deployment/operability.

Verification already completed for QA19: fresh withdrawal/model/RLS/endpoint regression passed 124/124 with Docker-backed tests enabled after one stale export-artifact model assertion was corrected; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
# Next Session Plan - Post-PDF07

Current queue head: D225 withdrawal finalization assessment after report/export delivery hardening.

Recommended next slice: zoom back into withdrawal trust. Assess anonymous withdrawal tokens, GDPR/data withdrawal support, retention policy enforcement, audit-safe deletion/anonymization, and tenant/admin workflow coverage. Prefer backend/domain gaps over UX until the withdrawal semantics are closed.

Verification already completed for PDF07: focused deployment package tests passed 16/16 after RED; product-spine smoke script parsed; deployment-package Node tests passed 24/24; signed-download-url endpoint tests passed 3/3; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
# Next Session Plan - Post-QA18

Current queue head: D224 report/export delivery assessment after browser runtime proof.

Recommended next slice: prove or harden product-level report PDF delivery now that the container browser runtime is real. Candidate directions are signed/local artifact download behavior, product-spine smoke coverage for successful PDF delivery, or enabling report PDF worker automation only if the existing architecture supports it cleanly.

Verification already completed for QA18: Docker 29.3.1 and Compose v5.1.1 available; staging API/worker images built; direct Chromium headless checks inside both images rendered pdf-runtime-ok with exit code 0.

# Next Session Plan - Post-OPS16

Current queue head: D223 post-OPS16 product/runtime regression assessment.

Recommended next slice: decide whether to run live container PDF smoke, signed URL/object-store delivery hardening, notification email routing, withdrawal finalization, deployment/operability hardening, or UX. Prefer live container PDF/deployment verification first because OPS16 changed Docker image runtime dependencies.

Verification already completed for OPS16: focused StagingWorkerDeploymentPackageTests passed 15/15, deployment-package Node tests passed 24/24, and dotnet build --no-restore passed with 0 warnings and 0 errors.

Assessment cadence: keep one assessment checkpoint per task. Use a D-series document when the next step changes direction, affects runtime/deployment semantics, closes or opens owner questions, or creates durable backlog. Use inline assessment for direct hardening under the accepted D223 decision.

# Next Session — Course of Action

Written 2026-05-05 after rate-limit cascade. Read this FIRST next session before any other handoff file.

---

## Current Queue Override - 2026-05-18

Latest update: D221 selected `OPS15 - Staging report PDF renderer configuration surface`, and OPS15 is done. API and worker Compose services now receive `Reports:PdfRenderer` settings, with env defaults and VPS runbook documentation. Start next with `D222 - Post-OPS15 product/runtime assessment`: choose deliberately between browser-runtime packaging, signed URL smoke, report/export value depth, targeted notification/email work, withdrawal workflow depth, deployment/operability hardening, or UX only where it supports a proven backend lane.

D65 through D202, EXP09/EXP10/EXP11/EXP12/EXP13/EXP14/EXP15/EXP16/EXP17/EXP18, OPS05/OPS06/OPS07/OPS08/OPS09/OPS10, QA12/QA13, PDF02, PDF03, QA11, and PDF04 are done. EXP07 added inline-vs-external storage metadata, EXP08 made downloads byte-safe, EXP09 added a dependency-free local private byte-store provider plus provider-backed external-object artifact downloads with byte-size/SHA-256 validation, EXP10 added object-store readiness/startup gating, PDF02 proved backend HTML-to-PDF byte rendering through PuppeteerSharp, PDF03 persisted the first governed campaign-series PDF artifact as private external-object bytes, QA11 hardened that PDF seam with tenant-scope, dependency-failure, object-integrity, and product-surface sensitive-field coverage, PDF04 added a `report_pdf_renderer` readiness gate, D187 selected EXP11, EXP11 persisted safe failed PDF artifact attempt rows for renderer/storage failures after target validation, D188 selected EXP12, EXP12 added a durable queued/worker-ready PDF lifecycle path, D189 selected EXP13, EXP13 made existing direct PDF creation use queue-then-process, D190 selected EXP14, EXP14 added a disabled-by-default hosted PDF artifact worker bootstrap, D191 selected EXP15, EXP15 added stale-rendering recovery to safe failed terminal state, D192 selected EXP16, EXP16 added pointer-only terminal outbox intents for report PDF artifacts plus a validating no-op handler, D193 selected EXP17, EXP17 added artifact-centered failed-PDF retry queueing, D194 selected OPS05, OPS05 added an `outbox_dead_letters` readiness guard, D195 selected OPS06, OPS06 added an aggregate-only outbox operational snapshot behind readiness, D196 selected OPS07, OPS07 added an `outbox_due_backlog` readiness guard, D197 selected QA12, QA12 locked the public outbox readiness health surface, D198 selected OPS08, OPS08 memoized outbox operational snapshots per request scope, D199 selected OPS09, OPS09 added durable worker-process heartbeat writing, D200 selected OPS10, OPS10 added config-gated worker heartbeat readiness, D201 selected QA13, QA13 locked the worker heartbeat health surface, D202 selected EXP18, and EXP18 added S3-compatible export artifact object-store provider foundation. Start next with `D203 - Post-EXP18 product/runtime assessment`: reassess before signed URLs, notification routing, deployment docs, withdrawal expansion, or UX.

Older snapshots in this file are historical context; trust `NEXT-ACTIONS.md` and `current-roadmap.md` for the current queue.

Owner-controlled Auth0 test identities for the validation tenants were created
for proof/demo smoke and must remain only in the ignored
`deploy/staging/validation-demo-fixtures/validation-demo-auth-users.local.json`
or private operator notes. VAL05 added the private walkthrough packet at
`docs/v2/80-agent-handoff/validation-demo-walkthrough-packet.md`. Q-053 still
keeps all validation data proof/demo only. D84 selected a narrow helper for
switching the selected validation tenant in the ignored local `.env` file; this
is not an in-app tenant switcher. VAL06 added
`deploy/staging/select-validation-demo-tenant.ps1` for that operator workflow.
D85 selected a read-only preflight helper next so the owner can check one
selected validation tenant before a live walkthrough without mutating Auth0,
VPS resources, or real-person data. VAL07 added
`deploy/staging/smoke-validation-demo-preflight.ps1`. Owner clarified on
2026-05-15 that validator feedback should steer priority, not block product
work. The product goal is a near-production demo app with auth, deployment,
graphs/reports, and usable tenant workflows that future users could use. Q-053
is a launch/legal-use gate: it still blocks real-person data and production
legal/GDPR claims, but it does not block production-grade engineering with
synthetic, seed, demo, or owner-controlled test data. D86 selected RBAC01
because current Auth0 sessions and validation roles already carry permissions,
but many setup/product/report mutation endpoints still require only tenant
membership and the app shell does not consistently hide owner/researcher
controls for analyst/viewer sessions.
RBAC01 then made `setup.manage` authoritative for unsafe setup/product/report/
export/notification/lab-response/scoring actions, kept safe reads tenant-member,
and made analyst/viewer sessions read-only across covered product surfaces.
D87 then selected VAL08 because the next concrete demo limiter is the seeded
validation-tenant content depth and story shape: the tenants prove breadth, but
the current catalog still mostly contains shallow four-question instruments,
generic seed labels, and smoke-like response profiles.
VAL08 then deepened the ordinary validation tenants: committed instruments now
have at least eight synthetic questions, tenant story metadata supplies
audience-specific series/campaign names, response profiles create richer
partial/completed/two-wave data, and the seed script validates and uses that
metadata with VAL08 provenance instead of generic VAL03 labels. A stale local
Compose staging database volume was wiped on owner request and can be reseeded
from the enriched fixtures.
D88 then selected VAL09 because the owner-run validation preflight false-failed
on Windows PowerShell: `System.Net.Http` was not loaded before the helper used
`HttpClient`, and the wait loop masked that as a `/health/live` timeout. VAL09
loaded the assembly, preserved the last safe live-check failure in diagnostics,
and verified the OH validation tenant full preflight in the Auth0/VPS-style
local Compose shape after restarting through the tenant-selection helper.
D89 then selected VAL10 because seed hygiene was the next concrete demo risk:
the owner had already seen stale/cluttered campaigns, and the validation seed
still appended a full demo set on every run. VAL10 added a fail-closed
duplicate-seed guard: default seed runs count validation-tenant instruments,
campaign-series, campaigns, response sessions, and export artifacts before API
writes, then stop with count-only diagnostics unless `-AllowDuplicateSeed` is
explicitly passed.
D90 then selected VAL11 because the remaining VPS/Auth0 rehearsal gap was a
safe public-origin check from an operator machine. The full validation preflight
is still the right local/on-VPS check, but it intentionally validates local
`.env`, ignored role-slot file, tenant-switch helper, and Docker database
counts. VAL11 added `-RemoteOnly`, which requires explicit API/web origins,
builds auth URLs from those origins and the selected validation tenant, runs
read-only remote API/web/session/CORS/login redirect checks, and skips local
operator-state plus database-count checks.
D91 then selected TEAM01 because the actual manual VPS/Auth0 rehearsal remains
owner/environment work, while app-owned users, tenant roles, and effective
permissions were still invisible inside the authenticated app. TEAM01 added
tenant-member `GET /tenant-members` and `/app/team`, showing current tenant
members, role assignments, current-user marker, and effective app-owned
permissions without adding invitations, Auth0 Management API, role editing,
SCIM, provider-subject exposure, or final role taxonomy.
D92 then reframed the queue through private-beta product readiness after the
owner clarified that the app should be shaped as a normal production-quality
product and the demo should be shaped around it through tenants, instruments,
response data, and walkthroughs. The immediate mismatch was the first user path:
root `/` still mounted the all-in-one proof workflow, while `/app` was already
the authenticated product workspace. D92 selected BETA01.
BETA01 then made `/` a compact product entry/gateway, moved the proof workflow
to `/proof-lab` as an explicitly labelled internal route, and hid authenticated
`Demo fixtures` navigation unless `PUBLIC_DEMO_SURFACES_ENABLED=true`.
Decision note: `/proof-lab` is labelled internal rather than fully env-gated so
the proof-workflow tests can still exercise it outside the normal product nav;
`/app/demo` remains the flag-gated demo fixture surface. D93 then selected
TEAM02 because `/app/team` is visible but not actionable: tenant access still
depends on Auth0 Dashboard work, ignored local files, seed scripts, or
database/bootstrap knowledge. TEAM02 should add the first app-owned member
preparation and role assignment path while keeping Auth0 as identity provider
only and avoiding Auth0 Management API, real email delivery, SCIM, paid Auth0,
real-person data approval, and legal/GDPR claims.
TEAM02 then added `team.manage`, assignable tenant roles, platform-side member
preparation by verified email/role/locale, primary role changes for other
members, pending/active provider-link status, and target local-session
revocation on role change. `/app/team` now exposes manager-only add-member and
role-change controls while analyst/viewer sessions keep read-only roster
access. Validation owner slots now carry `team.manage`; researcher slots remain
setup-only. Auth0 is still identity provider only: TEAM02 did not add Auth0
Management API, Auth0 Organizations/roles, SCIM, real email delivery, paid
Auth0 dependency, member hard delete, real-person data approval, or legal/GDPR
claims. D94 then reassessed the next limiter, explicitly including UI
component-system and hierarchy concerns, and selected ORG01. The next slice
should add `/app/directory` over tenant-scoped subjects, groups, memberships,
and manager relationships, while reusing existing UI primitives and deferring
full respondent-rule execution, bulk import/export, org chart visualization,
and broad standalone UI-system work.
ORG01 then added `/app/directory`, setup-authorized subject/group/membership
and manager controls, and synthetic validation hierarchy data through normal
product APIs. D95 selected RR01 because campaigns still could not preview how
respondent rules resolve against that graph. RR01 then added setup-manager-only
audience preview for `self`, `all_in_group`, `manager_of_target`, and
`reports_of_target` from `/app/campaign-series/{id}/setup`, without creating
assignments, invitations, reseeding, launch mutations, 360 reports, real-person
data approval, or legal/GDPR claims. D96 then selected RR02 because campaigns
can preview respondent rules but still cannot save those rules or use them at
launch to create durable assignments. RR02 then added saved campaign
respondent-rule configuration, launch-readiness validation over stored rules,
safe assignment roster reads, and identified assignment materialization for the
narrow supported rule subset. It did not expand into anonymous invitation
generation from rules, open-link automation, reseeding, sampling,
peer/mentor/predicate/external-email rules, bulk import, full Designer work,
dashboards, real-person data approval, or legal/GDPR claims. D97 followed and
selected WGT01 as the next normal-product limiter. WGT01 then added the first
backend-served Reports widget manifest and local Svelte widget registry over
existing aggregate report semantics. Reports now render supported manifest
widgets before the existing workflow, unknown widget kinds fall back safely,
and manifest delay/failure does not block the route's existing Reports panels.
D98 then selected HOME01 because the first authenticated screen still does not
guide the owner or validator toward the next ordinary product action across
Setup, Operations, Reports, Waves, Directory, and Team.
HOME01 then extended `GET /workspace-overview` with a backend-computed command
center and rendered it first on `/app`. The command center derives safe,
prioritized route links from current-tenant campaign-series readiness, live
campaigns, submitted report data, missing successful score runs, export
artifacts, longitudinal wave availability, empty Directory posture, and pending
provider-linked tenant members, filtering setup/team prompts by app-owned
permissions. No mutating home actions, full dashboard builder, arbitrary widget
plugin system, Designer work, deployment automation, real-person data approval,
or legal/GDPR claim was added. D99 then selected UX25 because command-center
links now need to land on selected-series routes that show the current task,
current outcome, or reportable state before dense reference details. UX25
then made those destination routes task/result-first over existing APIs:
Setup and Operations render current workflows first, Reports renders widgets,
the selected report snapshot, and workflow first, and Waves renders the
wave-comparison snapshot plus workflow first. D100 then selected WGT02 because
the backend Reports widget manifest emits six current widget kinds, but the
frontend only renders readiness and score coverage as concrete widgets. WGT02
should render selected campaign report state, export artifact registry, visual
analytics entry, and finality/provenance summary as supported widgets over the
existing manifest contract before broader manifest migration or deeper
dashboard/report work. WGT02 then added strict frontend data guards and concrete
Svelte renderers for all four missing current widget kinds, so the Reports
route now renders all six backend-known manifest kinds without unsupported
fallback cards while still preserving the fallback for truly unknown future
widgets. D101 then selected UI01 because the app now works broadly enough to
evaluate as a product, but still feels visually provisional. UI01 should turn
the current authenticated product surfaces into a coherent hybrid
research-grade/operational SaaS foundation over `/app`, campaign-series,
selected-series hub, Reports, and shared primitives. The old
`docs/Design.MockToAdapt-UseWhatYouWant` mocks are visual reference only, not
brand, OLBI, route, or canonical-content truth.
UI01 then added the first reusable product UI foundation over the authenticated
normal app: shared shell/sidebar/topbar primitives, named `Product workspace`
main landmark, stable `Product navigation`, tighter surface headers and product
panels, report-widget grid/card primitives, and route-level structural groups
for workspace totals, campaign-series portfolio controls, selected-series
campaign rows, and Reports reference context. It did not change backend
semantics, add final brand, landing page, report math, dashboards/PDFs,
Designer, deployment automation, real-person data approval, legal/GDPR claims,
or platform-canonical named-instrument content. D102 then selected UX26 because
visual coherence alone does not make the app easy to operate: selected-series
identity, current route, primary work/result, and dense reference context still
compete across hub, Setup, Operations, Reports, and Waves. UX26 should add a
shared selected-series workspace frame/navigation and clearer primary-vs-
reference composition over existing APIs.
UX26 then added the shared selected-series workspace frame/navigation across
hub, Setup, Operations, Reports, and Waves. Selected-series identity,
child-route navigation, active-route state, and route posture now appear before
route-owned primary work/result regions, while UX25 primary-before-reference
ordering remains intact on child routes. D103 should reassess the normal product
path after this wayfinding foundation and choose the next limiter.
D103 then selected UX27 because the normal app route structure is clearer, but
primary workflow copy still exposes proof-era language such as "proof workflow",
"proof actions", "proof entry", and "local delivery proof". UX27 should clean
normal app product vocabulary while preserving proof-only/local/private-beta and
provenance labels that prevent overclaiming.
UX27 then added a normal-app vocabulary guardrail and cleaned visible workflow
language across selected-series view models, Operations, Reports, Waves, route
headers, report/wave snapshots, report widgets, demo fixtures, and the
validation walkthrough packet. Normal app workflow copy now uses entry link,
local delivery, report preview, export artifact, linked trajectory check, and
wave comparison preview language while preserving Proof/local, Proof-only, and
not-validated provenance labels. D104 then reassessed the product path and
selected DES01 because Setup authoring is now sharper than another copy/UI pass:
the template task still centers on a fixed three-question seed and separately
seeded scoring JSON. DES01 then added editable question rows and generated
default scoring over existing setup APIs. D105 should reassess the full product
path before choosing another implementation lane.

## State snapshot (what's done)

- All 8 ADRs Accepted (0001–0008). ADR-0009 (outbox + audit interceptor) Proposed. ADR-0010 (RLS-enforced tenancy) Proposed.
- Schema spec done: `db-schema.md` reflects ADR-0003 + ADR-0005 (5 tables reshaped, 2 new sections).
- Scoring spec done: `scoring-rules-spec.md` 9 sections at 🟡 Proposed.
- Crypto spec done: `longitudinal-linking.md`, `respondent-ui.md`, `security.md` carry ADR-0005 Argon2id + per-series salt + ≥60-bit floor + no-fallback.
- Architecture batch done: A07 (`system-overview.md`), A08 (`multi-tenancy.md`), A09 (`auth-and-authz.md`), A20 (`0010-rls-enforced-tenancy.md`).
- Security production-gate doc done: A36 (`40-ops/security.md` RLS cross-tenant test pattern). Q-048 closed.
- Domain branching spec done: A05 (`10-domain/branching-engine.md`). A21/A27 no longer blocked by A05.
- Domain psychometrics spec done: A06 (`10-domain/psychometrics.md`). A25 no longer blocked by A06.
- Realtime/integrations architecture docs done: A11 (`20-architecture/realtime.md`) and A12 (`20-architecture/integrations.md`). Q-023 closed with SSE-first M1 decision; SignalR deferred until a bidirectional use case exists.
- Owner-blocker action pack created: `80-agent-handoff/OWNER-BLOCKERS-ACTION-PACK.md` gives scripts, call questions, decision criteria, OSH consultant seed targets, and capture form for O01/O02/O03/Q-021. Q-047 is now closed by product de-scope.
- Rights posture clarified: platform can proceed with generic builder + tenant-owned/private imports; rights-unclear instruments are not platform-shipped presets, not marketed as included, and not globally seeded. Tenant setup launches should use owner/tenant-attested status; unverified internal-demo status remains non-launching. D73A removed OLBI from the active product claim; future platform-shipped named-instrument content needs a new owner/legal canonical-publish decision.
  Implementation intent is captured in [`olbi-capable-engine-principle.md`](olbi-capable-engine-principle.md).
- F01-F11 and GF01-GF05 backend foundation done in the local `instruments-platform` repo: .NET 9 solution, vertical-slice health smoke, EF Core 9/Npgsql migration tooling, Testcontainers Postgres, tenant/user/role/permission model, per-request tenant context, RLS policy tests, audit interceptor/table foundation, outbox table/interceptor/relay foundation, provider-neutral JWT/OIDC auth/session foundation, subject graph model, instrument metadata model, template/version/question model, generic derivative lifecycle, generic scoring-rule persistence, generic campaign shell/assignment model, generic response identity mode assignment invariants, and UI-enabling setup APIs for private tenant imports/template/scoring/campaign drafts.
- F29 frontend scaffold done: `apps/web` SvelteKit + TypeScript + Tailwind app package, tenant setup workspace shell, design tokens, setup-stage navigation, Playwright smoke coverage, and typed API client boundary.
- F41 tenant setup workspace UI done: guided browser workflow for tenant-private instrument import, template-version creation, draft scoring-rule creation, campaign series/draft creation, and launch-readiness diagnostics against GF05 endpoints. Local Development auth/CORS bridge added so the browser can call protected GF05 endpoints before production OIDC login UX exists.
- F42/F43 tenant setup workspace polish done: visual alignment from the design-reference pass, provenance strip, generated setup-run values, duplicate-import conflict handling, stale-result clearing, and compact existing-instrument list. The design mock folder remains untracked reference material.
- R01 generic respondent capture MVP done: minimal `response_session` + `answer` persistence, tenant RLS/guard coverage, respondent-capture endpoints, real `ResponseCaptureStore`, frontend API client calls, and setup-workspace Response Lab for load campaign -> create lab assignment -> start session -> save answer -> submit. This is still a setup-serving lab path, not a public respondent launch flow.
- S01 generic scoring execution MVP done: simple `mean`/`sum` evaluator, `score_run` + `score` persistence with tenant RLS/guards, response-session score computation endpoint/store, and setup-workspace "Compute score" action after submit. This proves the generic setup loop through computed score, but it is not full ADR-0008 scoring.
- S02 setup score result panel done: frontend-only setup-lab presentation over S01 score results, with formatted value, `n`, score-run provenance, "Interpretation pending", and "Not a production report" copy. This is not validated interpretation, thresholding, reporting, exports, or canonical instrument content.
- S03 fuller ADR-0008 scoring execution slice done: `SimpleScoringEngine` now executes a narrow graph document subset (`inputs`/`nodes`/`outputs`) with `select_answers`, `reverse_code`, `mean`, `sum`, `count_valid`, and `subscale_aggregate` (`mean`/`sum` only), plus `require_all` and `min_valid_count` missing-data policy. Legacy `operations` documents still work. Setup-created sample rules now use the graph shape. This is not the full ADR-0008 engine: no norms, thresholds, buckets, percentiles, wave deltas, weighted ops, if/else, visual builder, full validation envelope, async worker, reports/exports, or canonical OLBI fixtures.
- L01 launch-lite campaign freeze done: added `campaign_launch_snapshot`, `Campaign.Launch(...)`, setup launch API/store, snapshot-aware response capture/scoring reads, and setup-workspace launch action. This is not public anonymous-longitudinal entry, reports, exports, norms, thresholds, offline sync, or full ADR-0008 scoring.
- P01 public open-link respondent entry done: added hashed open-link token issuing for launched anonymous campaigns, public token-scoped respondent API endpoints, reusable open-link sessions, setup-workspace open-link creation after launch, and a minimal `/r/[token]` page that submits without setup auth headers. This is not invitation batch/email delivery, consent/disclosure enforcement, identified entry, anonymous-longitudinal participant-code entry, resume secrets, rate limiting/CAPTCHA, SurveyJS runtime, public score feedback, reports, exports, offline sync, or platform-canonical OLBI content.
- C01 consent/disclosure MVP done: added `consent_document`/`consent_record`, setup-created default consent documents, launch-readiness consent blocking, launch snapshot `consent_document_id`, public open-link consent payloads, accepted-grant session creation, consent-record linkage, and a consent-first `/r/[token]` UI. This is anonymous open-link only; no legal template library, retention/disclosure policy records, withdrawal, re-consent, PDF artifacts, identified public entry, anonymous-longitudinal entry, invitation delivery, or platform-canonical OLBI content.
- D18 setup/public-flow assessment after C01 done: assessment note `assessments/D18-SETUP-PUBLIC-FLOW-ASSESSMENT-2026-05-07.md`. Course decision: do C02 retention/disclosure policy foundation next before invitations, reports/exports, or fuller ADR-0008 scoring because disclosure/retention policy is the safety boundary those slices need.
- C02 retention/disclosure policy foundation done: added `retention_policy` and `disclosure_policy` domain/EF/migration/RLS support, setup-created proof defaults, launch-readiness blockers, and launch snapshot/response freeze of `retention_policy_id` and `disclosure_policy_id`. This is policy provenance only; no retention worker, withdrawal/re-consent, report/export k-anonymity enforcement, owner-editable policy UI, final legal templates, invitation delivery, or production retention commitments. Q-053 still controls the final GDPR/legal package and final retention numbers.
- C03 invitation delivery MVP done: added email invitation token issuing, `notification` persistence, launched-anonymous `POST /campaigns/{id}/invitation-batches`, anonymous invited assignments, queued notification/outbox intent metadata without raw invite paths, public respondent resolution for email invite tokens, and setup-workspace queued invitation proof. This is queued delivery intent only; no SMTP/provider, delivery worker, real send, reminders, bounce/complaint/unsubscribe handling, identified entry, anonymous-longitudinal entry, tenant template editor, rate limiting/CAPTCHA, or platform-canonical OLBI content.
- R02 report/export proof path done: added tenant-member `GET /campaigns/{campaignId}/report-proof`, a disclosure-safe aggregate score projection over launched campaigns, frozen launch/scoring/consent/retention/disclosure provenance, `k_min` suppression before exposing aggregate values, draft/unlaunched blocking, cross-tenant guard coverage, and a setup-workspace "View report proof" panel. This is proof-only: no CSV, codebook, `export_artifact`, object storage, checksum, download link, PDF/PuppeteerSharp renderer, chart/dashboard builder, materialized projection worker, export audit workflow, individual reports, anonymous-longitudinal trajectory exports, wave comparison, thresholds, norms, or validated interpretation.
- D19 post-R02 project assessment done: assessment note `assessments/D19-POST-R02-PROJECT-ASSESSMENT-2026-05-07.md`. Course decision: the proof path is still aligned, but R02 must not be treated as finished reports/exports. Pull `X01` next: a narrow report-proof CSV/codebook artifact MVP over the existing R02 governed projection. Do not pull `participant_code`, real SMTP/provider delivery, full F27 raw exports, frontend report design, or another broad backend slice forward unless explicitly selected.
- X01 report-proof CSV/codebook artifact MVP done: added tenant-member `POST /campaigns/{campaignId}/report-proof/exports`, `export_artifact` persistence with tenant RLS/guard coverage, deterministic aggregate CSV and codebook JSON over the R02 projection, checksum/row-count metadata, inline proof content, and a setup-workspace "Create export proof" panel. This is proof-only and aggregate-only: no raw answers, question-level answer codebook, anonymous-longitudinal trajectory export, identified subject export, object storage, signed download, async worker, PDF, dashboard charts, thresholds, norms, validated interpretation, or platform-canonical instrument content.
- D20 post-X01 report/export artifact assessment done: assessment note `assessments/D20-POST-X01-REPORT-EXPORT-ASSESSMENT-2026-05-07.md`. Course decision: keep X01 framed as aggregate proof only, then pull `E01` next: provider-safe email delivery worker MVP over C03 queued notifications, with a local/dev sink first and external provider credentials/config gated by environment/secrets.
- D21 roadmap consolidation done: `60-roadmap/current-roadmap.md` is now the current navigation anchor. D73A later updated it to separate the active tenant-private proof spine from dormant platform-shipped named-instrument content, and keeps original M1 mechanics as the long exit target rather than the immediate task list.
- E01 provider-safe email delivery worker MVP done: added `notification_delivery_attempt`, provider-safe delivery contracts, local/dev sink, config-gated SMTP provider boundary, tenant-member delivery processing endpoint, queued notification sent/failed transitions, invitation-token hash reissue at delivery time, and setup-workspace local delivery proof. Raw invite paths are only in the provider message/setup response, not durable notification/outbox/attempt storage. This is not production deliverability, reminders, bounce/complaint/unsubscribe handling, tenant template editing, identified/anonymous-longitudinal invitations, marketing email, or production legal copy.
- P02 participant-code foundation done: added tenant-scoped `participant_code` persistence, Argon2id hashing with `campaign_series.code_salt`, stored Argon2id parameter provenance, RLS/tenant guard coverage, response-session guard rails for non-null `participant_code_id`, and a neutral lookup-or-create application boundary. Raw and normalized participant codes are not stored or returned. This is not public anonymous-longitudinal entry, wave UI, duplicate-submit policy enforcement, trajectory reports/exports, withdrawal, or recovery.
- P03 anonymous-longitudinal public entry done: launched `anonymous_longitudinal` campaigns can create public open-link tokens, public entry returns `requiresParticipantCode`, `/r/{token}` collects participant code before session creation, session creation resolves the code through the P02 store and stores only `response_session.participant_code_id`, plain anonymous campaigns reject participant-code payloads, and duplicate submitted campaign/code responses are blocked at session creation and submit. This is not W01 two-wave proof, wave-comparison reports, trajectory exports, withdrawal/recovery, production respondent runtime polish, or platform-canonical OLBI content.
- D22 post-P03 assessment done: assessment note `assessments/D22-POST-P03-ANONYMOUS-LONGITUDINAL-ASSESSMENT-2026-05-07.md`. Its course decision pulled W01 as a narrow two-wave campaign-series proof path; W01 is now complete, and wave-comparison reporting remains separate as R03.
- W01 two-wave campaign-series proof path done: tenant-member `GET /campaign-series/{id}/two-wave-proof`, a proof read model, setup-workspace two-wave proof creation/refresh controls, and API/client/e2e/Docker coverage now prove two launched anonymous-longitudinal waves in one campaign series can be completed with the same participant code and recognized as one anonymous trajectory. Source design/plan: `../../plans/2026-05-07-w01-two-wave-campaign-series-proof-design.md` and `../../plans/2026-05-07-w01-two-wave-campaign-series-proof.md`. This is not wave-comparison report semantics, trajectory export, withdrawal/recovery, production respondent runtime hardening, production deliverability, full respondent-rule materialization, or platform-canonical OLBI content.
- R03 wave-comparison aggregate report proof done: tenant-member `GET /campaign-series/{id}/wave-comparison-proof` now returns disclosure-gated side-by-side aggregate score means, linked-pair counts, aggregate deltas, and paired delta means for the first two launched `anonymous_longitudinal` waves. Mixed scoring-rule versions use compatibility metadata: same rule/version/hash is compatible, declared compatible/descriptive/incompatible states are honored, and missing mixed-version compatibility blocks deltas while keeping side-by-side means visible. Setup workspace can call and display the proof. This is not raw answer export, trajectory export, object storage, signed URLs, async export workers, dashboards/PDFs, validated interpretation, norms, thresholds, respondent runtime hardening, or platform-canonical OLBI fixtures.
- X02 export artifact retrieval/storage/download boundary done: tenant-member `GET /export-artifacts/{id}` and `GET /export-artifacts/{id}/download` now retrieve/download existing aggregate report-proof `export_artifact` rows under tenant scope and RLS. Setup workspace can create an export proof, fetch it back by artifact id, and download the persisted CSV. This is not raw answer export, anonymous-longitudinal trajectory export, object storage, signed URLs, async export workers, PDFs, dashboards, retention expiry, production download audit workflow, validated interpretation, norms, thresholds, respondent runtime hardening, or platform-canonical OLBI fixtures.
- U01 respondent runtime hardening done: public `/r/{token}` now has fatal entry-load retry, local consent/session/submit errors, accessible inline alerts, required-answer and numeric scale validation before save/submit, submit retry that preserves answers, and mobile viewport coverage including anonymous-longitudinal participant-code entry. Public respondent calls still avoid setup auth headers. This is not SurveyJS runtime, offline sync, autosave, resume secrets, identified entry, URL scrubbing, rate limiting/CAPTCHA, public score feedback, withdrawal/re-consent, production deliverability, dashboards/PDFs, raw exports, trajectory exports, or platform-canonical OLBI fixtures.
- AU01 provider-neutral auth/session UX boundary done: the setup workspace now checks `/auth/session` before protected controls render, shows checking/sign-in-required/tenant-access/session-failed states, keeps dev headers behind `PUBLIC_DEV_AUTH_ENABLED=true`, and uses `PUBLIC_AUTH_LOGIN_URL` / `PUBLIC_AUTH_LOGOUT_URL` placeholders. This was not Auth0-vs-Keycloak selection, provider login/callback, refresh-token rotation, app session persistence, user-management UX, or staging deployment. OB02 later closed Q-020 for M1 through ADR-0011.
- DEP01-A local Docker deployment proof done: `deploy/staging` now has API/web/migrator Dockerfiles, compose Postgres/migrator/seed/API/web services, safe local env defaults, and start/smoke/stop PowerShell scripts. The proof builds containers, applies migrations, seeds the proof tenant, and smoke-tests API health, unauth/dev-auth `/auth/session`, tenant-private import/list, and web shell. This is local Docker only: no VPS staging, GitHub Actions deploy, registry, TLS/reverse proxy, production secrets, backup/restore, production auth, or observability.
- DEP01-B portable VPS staging adaptation done: added `deploy/staging/docker-compose.vps.yml`, `vps.env.example`, `nginx.example.conf`, and `40-ops/vps-staging-runbook.md`. The override binds API/web to loopback-only ports, removes Postgres host exposure with Compose reset semantics, and documents nginx subdomain routing. No live VPS resources were touched. Deployment work now pauses by owner decision; D23 followed and is now done.
- D23 post-DEP01-B product/runtime assessment done: assessment note `assessments/D23-POST-DEP01B-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md` and frontend contract `20-architecture/frontend-product-surfaces.md`. Course decision: pivot to UX/UI now as frontend product-surface foundation work, not a throwaway page cleanup and not final visual design. Do not start SurveyJS Creator/designer, production dashboards, production auth, raw exports, or more deployment automation by default.
- UX01 frontend product surface foundation done: `/app` authenticated shell, workspace overview, campaign-series route map/hub, setup/operations/reports/waves surfaces, gated `/app/demo` fixtures, view-model adapters, reusable state/provenance primitives, route-level proof workflow mounting, and respondent isolation regressions. `/` remains a transitional all-in-one proof entry. No backend semantics, final visual design, SurveyJS Creator/runtime, production auth, raw exports, charts, PDFs, or deployment automation were added.
- D24 post-UX01 product/runtime assessment done: assessment note `assessments/D24-POST-UX01-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX01 solved route/product-surface shape, but `/app`, `/app/campaign-series`, and selected-series hub still need backend-backed read models before final visual design. Next default action is `UX02`, a product-surface read-model bridge over existing semantics.
- UX02 product-surface read-model bridge done: tenant-member `GET /workspace-overview`, `GET /campaign-series`, and `GET /campaign-series/{id}` now back `/app`, `/app/campaign-series`, and selected-series hub. Read models are tenant-scoped, scalar-only, and tested for tenant boundaries, not-found, sensitive-field omission, stale selected-series navigation, retry recovery, and demo gating. Setup/operations/reports/waves still mount the proof workflow; UX02 did not add final visual design, SurveyJS runtime/Creator, production auth, dashboards/charts/norms/thresholds/PDFs, raw/trajectory exports, retention/withdrawal UX, deployment automation, or platform-canonical OLBI content.
- D25 post-UX02 product/runtime assessment done: assessment note `assessments/D25-POST-UX02-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX02 solved the top-level read-model gap, but setup/operations/reports/waves still mount the all-in-one proof workflow. Next default action is `UX03`: selected-series product workflow composition and route-loading hygiene over existing semantics before final visual design.
- UX03 selected-series product workflow composition done: setup/operations/reports/waves now load selected-series context from `GET /campaign-series/{id}`, render selected-series title/id, summaries, governance/campaign context, loading/error/retry states, and keep existing proof workflow actions mounted underneath. Added shared frontend route-state helpers, selected-series surface view models, reusable `SelectedSeriesSurface`, and e2e coverage for child-route context, not-found, retry, and stale navigation. This is not final visual design, SurveyJS runtime/Creator, production auth, dashboards/charts/norms/thresholds/PDFs, retention/withdrawal, deployment automation, or platform-canonical OLBI content.
- D26 post-UX03 product/runtime assessment done: assessment note `assessments/D26-POST-UX03-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX03 removed the last major structural blocker to visual/product-surface work. Next default action is `UX04`: visual product surface pass over existing semantics for `/app`, campaign-series list/hub, setup, operations, reports, and waves.
- UX04 visual product surface pass done: authenticated `/app` workspace now has durable shell labels, improved active product navigation, compact route headers, shared product visual primitives, restyled workspace/list/hub surfaces, selected-series child route panels, and named proof-only workbench containment for setup/operations/reports/waves. Product-surface e2e coverage now guards shell copy, single active nav state, selected-series context, and proof workbench containment. This did not add backend endpoints, public respondent runtime changes, SurveyJS runtime/Creator, production auth, portfolio management, dashboard/report semantics, charts, norms, thresholds, validated interpretation, PDFs, retention/withdrawal, deployment automation, or platform-canonical OLBI content.
- D27 post-UX04 product/runtime assessment done: assessment note `assessments/D27-POST-UX04-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX04 closed the main authenticated workspace presentation gap, so the next default risk is the public respondent route still using a custom proof question loop. Next default action is `U02`: respondent SurveyJS runtime foundation on `/r/[token]` over existing public entry/session/save/submit contracts.
- U02 respondent SurveyJS runtime foundation done: `/r/[token]` now renders the current proof question shape through a lazy-loaded SurveyJS Form Library adapter after entry/consent/participant-code gates. Added `survey-core`/`survey-js-ui`, typed adapter tests, SurveyJS answer normalization back to backend question ids, hidden SurveyJS-owned completion, and respondent e2e coverage for gate timing, payload mapping, auth isolation, validation, submit retry, and mobile usability. This is narrow only: no SurveyJS Creator/designer, offline/autosave/resume, full branching runtime, locale switching, identified entry, URL token scrubbing, withdrawal/re-consent, public score feedback, new backend semantics, production auth, dashboards/reports, deployment automation, or platform-canonical OLBI content.
- D28 post-U02 product/runtime assessment done: assessment note `assessments/D28-POST-U02-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: U02 fixed the public respondent renderer mismatch, but selected-series authenticated pages still mount proof actions that can create unrelated local campaign series. Next default action is `UX05`: campaign-series workflow anchoring. Keep it narrow: create/select routing plus route-series mutation anchoring only, not full portfolio management, final setup wizard, SurveyJS Creator, deeper respondent runtime, production auth, deployment, reports, or platform-canonical OLBI.
- UX05 campaign-series workflow anchoring done: `/app/campaign-series` now has narrow create/select routing through the existing setup `POST /campaign-series` endpoint and routes newly created series to setup. Selected-series child routes pass route series context into `ProofWorkflowSurface`; campaign draft creation reuses the route series instead of creating a hidden local series, and the same resolver makes two-wave proof creation route-series-aware when selected context exists. This is still not full portfolio management, final setup/operations/reports/waves UX, SurveyJS Creator, deeper respondent offline/resume, production auth, deployment automation, report/dashboard semantic expansion, or platform-canonical OLBI content.
- D29 post-UX05 product/runtime assessment done: assessment note `assessments/D29-POST-UX05-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX05 fixed route-series mutation anchoring, but setup is still driven by local proof-workbench state rather than a backend-backed selected-series setup workspace read model. Next default action is `UX06`: selected-series setup state bridge. Keep it narrow: selected-series setup read model plus setup page state rendering only, not final setup wizard, full portfolio management, respondent offline runtime, production auth, deployment, reports, or platform-canonical OLBI.
- UX06 selected-series setup state bridge done: tenant-member `GET /campaign-series/{id}/setup-workspace` now derives selected setup state from existing campaign-series/campaign/template/scoring/policy rows under tenant RLS; `/app/campaign-series/{id}/setup` renders selected campaign, template, scoring, policy, readiness, missing prerequisites, and campaign rows from that read model; proof actions remain secondary/proof-only and refresh setup state after anchored mutations. This is still not the final setup wizard, full portfolio management, respondent offline runtime, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content.
- D30 post-UX06 product/runtime assessment done: assessment note `assessments/D30-POST-UX06-PRODUCT-RUNTIME-ASSESSMENT-2026-05-08.md`. Course decision: UX06 closed the setup read-state gap, but selected setup actions still live inside the transitional proof workflow. Next default action is `UX07`: selected-series setup workflow foundation. Keep it narrower than a final wizard/designer: setup-specific primary actions over existing setup APIs plus `setup-workspace` refresh, not a broad new backend state machine, operations/reports/waves replacement, production auth, deployment, reports, or platform-canonical OLBI.
- UX07 selected-series setup workflow foundation done: `/app/campaign-series/{id}/setup` now renders a setup-specific primary action workflow instead of a setup-route proof workbench. It uses existing setup APIs, derives action availability from setup-workspace plus local action results, creates campaign drafts inside the selected route series, refreshes setup-workspace after successful actions, and keeps action failures local. Operations, reports, and waves still use the existing proof workbench. This is still not the final setup wizard/designer, setup-progress aggregate, operations/reports/waves replacement, full portfolio management, respondent offline runtime, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content.
- D31 post-UX07 product/runtime assessment done: assessment note `assessments/D31-POST-UX07-PRODUCT-RUNTIME-ASSESSMENT-2026-05-09.md`. Course decision: UX07 made selected setup coherent enough for the current proof spine, so move downstream to `UX08`: selected-series operations state bridge. UX08 should add backend-backed operations workspace state for `/app/campaign-series/{id}/operations` over existing launch/public-entry/invitation/notification/delivery-attempt data, render operations from that state, and keep proof actions secondary. Keep it narrower than a final operations console, production deliverability slice, reports/waves replacement, production auth, deployment, or platform-canonical OLBI.
- UX08 selected-series operations state bridge done: tenant-member `GET /campaign-series/{id}/operations-workspace` now derives selected operations state from existing campaign-series/campaign/launch/public-entry/invitation/notification/delivery/response rows under tenant RLS; `/app/campaign-series/{id}/operations` renders operations summary, selected campaign, campaign rows, and missing prerequisites from that read model; proof actions remain secondary/proof-only and refresh operations state after selected-campaign operations mutations where practical. This is still not the final operations console, primary operations workflow, production deliverability, reports/waves replacement, production auth/deployment, report/dashboard expansion, or platform-canonical OLBI content.
- A49a generic scoring fixture replay harness done: added non-canonical synthetic JSON fixtures under `fixtures/instruments/dummy-burnout/noncanonical/` and xUnit replay coverage over the current S01 `mean`/`sum` scoring engine. This is not the canonical YAML fixture path, not OLBI fixture legalization, and not the full ADR-0008 operation graph.
- A13 competitor analysis done: `50-business/competitor-analysis.md` now has sourced competitor map, per-competitor notes, buyer wedge mapping, positioning guardrails, and source index. A30 sales narratives is unblocked.
- A14 GDPR compliance done: `40-ops/compliance-gdpr.md` now has the implementation-facing DPA/DPIA/lawful-basis/rights/breach/DPO/retention/transfer brief. It is not final legal advice; tenant-facing contract and privacy text still need qualified counsel review.
- A15 cost model done and refreshed by OB03: `40-ops/cost-model.md` now has vendor unit costs, deployment envelopes, variable tenant costs, support/onboarding cost rules, breakeven formula, buyer guardrails, and source index. A31 pricing is unblocked; Q-020 and Q-021 are closed for M1 by ADR-0011/ADR-0012, while Q-002 still drives hospital/self-host assumptions.
- A16 security spec done: `40-ops/security.md` now has the operating security baseline: STRIDE threat model, OWASP Top 10 2025 + API Security Top 10 2023 mapping, secrets, headers, CSP, rate limits, CSRF/CORS/cookies, validation/encoding, logging/audit integrity, dependency/license policy, vulnerability management, and security test gates. A47 later implemented the dependency-license CI gate; vulnerability/SCA scanning remains future work.
- A21 respondent UI spec done, including A41 offline conflict rules: `30-features/respondent-ui.md` covers entry modes, token landing, locale selection, consent, SurveyJS/SvelteKit rendering, branching snapshots, autosave/resume, offline drafts and sync, WCAG 2.2 AA, i18n, privacy/security constraints, and implementation gates. A43 later added service-worker versioning/update prompt rules; actual respondent PWA implementation remains future work.
- A25 threshold alerts spec done: `30-features/threshold-alerts.md` now defines aggregate-first score/direct-item threshold rules, identity-mode routing limits, wellbeing/EAP workflow boundaries, respondent messages, alert event states, audit/compliance controls, and testing gates. Q-013 remains open for suicide/self-harm risk protocol before PHQ-9 or similar production alerting.
- A27 designer UI spec done: `30-features/designer-ui.md` now defines the M3 designer/admin workspace, SurveyJS Creator-as-optional-adapter boundary, platform-domain source-of-truth rule, template/question authoring, translation, branching, scoring, audience/respondent-rule, consent/disclosure, threshold-alert, preview, publish/versioning, validation, permissions, accessibility, security, bundle, and implementation gates.
- A28 federated campaigns spec done: `30-features/federated-campaigns.md` now defines the M5 multi-PI/cross-institution workflow, explicit federation protocol/legal packet, tenant-local raw data boundary, aggregate-by-default cross-tenant composition, collaborator/site lifecycle, data/export scope, federation-level anonymous-longitudinal salt rules, RLS/audit/security gates, report/export levels, and implementation tests.
- A29 risks register done: `60-roadmap/risks.md` now defines the operating risk register with rating model, risk appetite, top risks, market/technical/regulatory/financial/key-person registers, owners, early warnings, mitigations, contingencies, blocked-work map, review cadence, and official EU source index.
- A30 sales narratives done: `50-business/sales-narratives.md` now defines current sales posture, qualification/disqualification rules, shared demo and leave-behind structure, common objections, persona narratives for P1-P7, immediate O01/O02/O03 outreach scripts, pipeline stages, and sales metrics.
- A31 pricing done: `50-business/pricing.md` now defines internal pricing posture, cost-floor rules, tier definitions, annual terms, setup/service fees, support caps, usage bands, paid-instrument pass-through, discount/deal-review rules, buyer-specific quote posture, validation questions, and market anchors.
- A32 grants and funding done: `50-business/grants-and-funding.md` now defines grant/program funding posture, route fit for HRZZ/Erasmus+/Horizon/MSCA/ESF+/EU4Health/Wellcome/Open Society/EIC, budget-line menu, safe proposal wording, do-not-chase rules, owner-call questions, and funding open questions.
- A33 deployment done: `40-ops/deployment.md` now defines the target deployment contract, explicit current repo gaps, environment tiers, M1 topology, artifact model, GitHub Actions/container/registry flow, frontend adapter decision, migration order, RLS/post-deploy SQL handling, rollout/rollback strategy, smoke tests, secrets, backup/DR gate, production blockers, and implementation backlog.
- A34 observability done: `40-ops/observability.md` now defines the telemetry contract, explicit current observability gaps, OpenTelemetry/OTLP pipeline, Grafana/Loki/Tempo/Prometheus direction, correlation rules, logging/PII exclusions, metrics/tracing/health-check targets, initial SLOs, dashboards, alert classes, release observability, operational-vs-audit log boundary, production gates, and implementation backlog.
- A35 runbook done: `40-ops/runbook.md` now defines severity, incident-command roles, triage, communication, rollback/forward-fix, restore drills, break-glass rules, post-incident review, on-call rotation, production gates, and top incident procedures across DB, deploy, migration, outbox/dead-letter, worker, email/webhook, frontend/runtime, storage/export, RLS/tenant leak, PII/log leak, token brute-force, auth, SLO, backup, and dependency/security events.
- A37 slice scaffolding generator done: `templates/platform-slice` now provides `dotnet new platform-slice` with command/query request-kind support, endpoint verb parameter, generated MediatR/FluentValidation/Result files, generated unit-test scaffold, real template tests, and backend-stack usage docs.
- A39 ADR-0003 scope broadening done: `10-domain/instruments-catalog.md` and `GLOSSARY.md` now define `instrument` as a broad published/citable content-locked assessment aggregate across psychometric, ergonomic, medical, educational, regulatory, and other domains, while keeping M1-M3 positioning narrow around validated psychometric / burnout and workplace wellbeing workflows.
- A42 CI bundle-size budgets done: `40-ops/build-gates.md` now defines respondent entry/shell <= 150 KB gzip, dashboard <= 800 KB gzip, SurveyJS/ECharts/Creator lazy-loading and leak rules, future Vite/SvelteKit manifest measurement, CI failure behavior, exception process, production gates, and implementation backlog. No CI workflow or npm budget script exists yet.
- A43 PWA service-worker versioning done: `30-features/respondent-ui.md` now defines versioned SvelteKit service-worker cache namespaces, deployment `version` use, safe update prompt UX, `skipWaiting()`/`clients.claim()` guardrails, version mismatch handling, multi-tab behavior, and integration gates for offline draft + sync. No `apps/web/src/service-worker.*` implementation exists yet.
- A44 PuppeteerSharp PDF integration spec done: `30-features/research-exports.md` now defines the shared HTML/CSS template renderer contract for research-export summaries, consent documents, compliance reports, and scored result PDFs. It locks the no-SurveyJS-PDF boundary, platform-owned template rule, browser lifecycle, PDF options baseline, privacy/provenance metadata, font/localization requirements, failure states, and test gates. No PuppeteerSharp package, renderer code, Docker/browser install, or PDF templates exist yet.
- A46 v1 reference-review practice done: root `AGENTS.md` now governs whole-repo implementation work, including the ADR-0006 rule to spend 5-10 minutes reviewing v1 equivalents before coding v2 features with v1 precedent. It also carries ADR-0009 audit/data-safety reminders for feature authors.
- A47 dependency-license CI gate done: `tools/check-licenses.mjs` scans npm package-lock metadata and NuGet transitive packages/license metadata, enforces `tools/license-policy.json`, fails forbidden/missing/unknown/unreviewed conditional licenses, and writes attribution artifacts under `artifacts/dependency-licenses/`. `.github/workflows/license-gate.yml` runs it on push/PR and uploads the attribution artifact.
- D17 project assessment/course re-evaluation is done. Assessment note: `assessments/D17-PROJECT-ASSESSMENT-2026-05-06.md`. Course decision was to keep backend work setup-serving and avoid another broad foundation slice. Owner later explicitly chose the narrow R01/S01/S02/L01/P01/C01/C02/C03/S03/R02/X01/E01/P02/P03/W01/R03/X02 proof path, so `response_session`/`answer`, score persistence, setup-lab score presentation, a minimal scoring graph subset, a minimal launch freeze, anonymous open-link respondent entry, anonymous open-link consent capture, retention/disclosure policy provenance, queued anonymous email invitation intent, a proof-only report/export aggregate projection, an inline aggregate CSV/codebook proof artifact, provider-safe local/dev delivery proof, participant-code persistence/hash foundation, public anonymous-longitudinal entry, two-wave campaign-series proof, wave-comparison aggregate proof, export artifact retrieval/download, and a proof-only campaign-series response CSV/codebook export now exist. Still not done: production trajectory exports, real production email deliverability, validated interpretations, full production raw/export lifecycle, production `export_artifact` object storage/signed URLs/workers, PDFs, dashboards, retention worker, withdrawal/re-consent, and offline sync.
- F12 evidence pass captured in `10-domain/olbi-source-evidence.md`. D73A later closed Q-047 by product de-scope, not by legal permission: private/tenant OLBI-shaped work can exist only with rights flags and tenant attestation, and platform-global OLBI seeding is not active scope. Future canonical named-instrument content requires a new owner/legal decision.
- Bookkeeping consolidated: NEXT-ACTIONS Done entries for A05/A06/A07/A08/A09/A11/A12/A13/A14/A15/A16/A19/A20/A21/A25/A27/A28/A29/A30/A31/A32/A33/A34/A35/A36/A37/A38/A39/A41/A42/A43/A44/A45/A46/A47/A48/A49a, GF01-GF05, F29, F41-F43, R01, S01, S02, S03, L01, P01, C01, C02, C03, D17, D18, D19, D20, D21, R02, and X01; OPEN-QUESTIONS has Q-049/Q-050/Q-051/Q-053 open and Q-052 closed; SESSION-LOG has the 2026-05-05/2026-05-06/2026-05-07 codex entries.
- Active workspace is the local `instruments-platform` repo. Do not carry forward the old `AnketasProject` dirty-worktree assumptions unless the owner explicitly switches back.

## Rate-limit lesson — DO NOT REPEAT

**What blew the cap:** dispatched 4 Opus agents in parallel with 2-3k-token prompts; each agent then loads 5-10 docs (~50k tokens) before any write. Total ~200k input × Opus rate weight + active conversation context = cap dead.

**Rule for next session:**

1. Use **Sonnet 4.6** for doc-writing agents. Opus is reserved for owner-conversation + decision walkthroughs.
2. Tight prompts (~500 tokens), not exhaustive specs. Reference the ADR + say "follow ADR/template conventions"; don't restate every section.
3. **Sequential dispatch** for Opus. Parallel only on Sonnet, max 2 at a time.
4. Verify quota state before dispatching (if a single fast prompt errors → quota tight → wait, don't dispatch).

## Immediate next session — first 30 minutes

1. Read this file.
2. Read `NEXT-ACTIONS.md` Top of queue + Done tail.
3. Read `../60-roadmap/current-roadmap.md` for the active tenant-private proof spine and next 10 slices.
4. Read `OPEN-QUESTIONS.md` Q-049 through Q-053, noting Q-052 is closed by F11 and Q-053 remains open.
5. Quick `git status`: it should be clean except any intentionally untracked reference material. The design mock folder is reference material only and should not be assumed to be product code.
6. DEP01-B, D23, UX01, D24, UX02, D25, UX03, D26, UX04, D27, U02, D28, UX05, D29, UX06, D30, UX07, D31, UX08, D32, UX09, D33, UX10, D34, UX11, D35, UX12, D36, UX13, D37, UX14, UX15, D38, UX16, D39, UX17, D40, UX18, D41, X03, D42, U03, D43, X04, D44, X05, D45, X06, D46, UX19, D47, BUILD-001, D48, BUILD-004, D49, R04, D50, PM01, D51, U04, D52, U05, D53, U06, D54, U07, D55, OPS01, D56, UX20, D57, UX21, D58, UX22, D59, UX23, D60, UX24, D61, QA01, D62, and PM02 are now done. This is a historical snapshot superseded by the current queue override above; A49b later completed, Q-047 later closed by D73A de-scope, and Q-020 later closed for M1 by ADR-0011.

## Agent batch to retry (the one that died)

Switch model to **Sonnet 4.6** for all four. Tighten prompts to ~500 tokens each.

| Agent | File                                                | Source ADRs                              | Notes                                                                                                |
| ----- | --------------------------------------------------- | ---------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| A07   | `docs/v2/20-architecture/system-overview.md`        | 0001, 0002, 0004, 0006, 0007, 0009, 0010 | **Done 2026-05-05 by codex.** Existing populated C4 draft verified and ADR-0010 cross-linked.        |
| A08   | `docs/v2/20-architecture/multi-tenancy.md`          | 0001, 0002, 0003, 0009, 0010             | **Done 2026-05-05 by codex.** Tenant resolution + RLS policy template + 3-role split + pitfalls.     |
| A09   | `docs/v2/20-architecture/auth-and-authz.md`         | 0002, 0005 + Q-020 (Auth0 trial)         | **Done 2026-05-05 by codex.** OIDC + claim shape + role/permission tables + ASP.NET policy patterns. |
| A20   | `docs/v2/70-decisions/0010-rls-enforced-tenancy.md` | 0001, 0002, 0003, 0009                   | **Done 2026-05-05 by codex.** Proposed ADR drafted; A36 still owns exact test fixture pattern.       |

**Current state after codex retry:** A05/A06/A07/A08/A09/A11/A12/A13/A14/A15/A16/A19/A20/A21/A25/A27/A28/A29/A30/A31/A32/A33/A34/A35/A36/A37/A38/A39/A41/A42/A43/A44/A45/A46/A47/A48/A49a are done. A10 remains intentionally skipped per owner instruction on 2026-05-06. F01-F11, GF01-GF05, D17, D18, D19, D20, D21, F29, F41-F43, R01, S01, S02, S03, S04, L01, P01, C01, C02, C03, R02, X01, E01, P02, P03, W01, R03, X02, U01, AU01, DEP01-A, DEP01-B, D44, X05, D45, X06, OPS01, UX20, D57, UX21, D58, UX22, D59, UX23, D60, UX24, D61, QA01, and D62 are done. Later D73A closed Q-047 by product de-scope, A49b is done, and OB02 closed Q-020 for M1 by ADR-0011. O01/O02/O03 owner outreach still runs in parallel.

Detailed prompt spec for each lives in this conversation's transcript at `~/.claude/projects/.../4fe4ffea-9c80-4c2e-b27b-bfc64d836340.jsonl` if you need to recover the verbose versions and trim.

## After A07/A08/A09/A20 landed

Then code-start unlocks. Three things needed:

### 1. Agent-doable

| Agent | What                                                           | Effort |
| ----- | -------------------------------------------------------------- | ------ |
| A37   | DONE — slice scaffolding generator `dotnet new platform-slice` | M      |

### 2. Owner-only (cannot delegate)

- **O01** — Conversation with prof lead. Validates wedge per Q-004. Blocks M1 commitment.
- **O02** — 2 academic alternates identified.
- **O03** — 3 OSH consultants identified.
- **Q-047** — Closed by D73A product de-scope. Future platform-shipped named-instrument presets require a new owner/legal decision.
- **Hosting decision** — closed for M1 by ADR-0012: portable Docker Compose with the current Hostinger VPS as validation host.
- **Repo creation** — `instruments-platform` codename per ADR-0006/Q-039. Owner creates the GitHub repo.

### 3. F-track scaffold

| Step | What                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F01  | DONE — local repo `instruments-platform` exists.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| F02  | DONE — .NET 9 solution layout with Api, Application, Domain, Infrastructure, Workers, SharedKernel, unit tests, and integration tests.                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| F03  | DONE — Postgres + EF Core 9 + migration tooling + Testcontainers verification setup.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| F04  | DONE — tenant, user, role, permission, role_permission, and role_assignment model + initial migration.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| F05  | DONE — RLS policies, transaction-local tenant DB scope, per-request tenant context middleware, and cross-tenant Postgres tests.                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| F06  | DONE — audit entity, `AuditSaveChangesInterceptor`, partitioned `audit_event` table, tenant RLS, append-only trigger, and Docker-backed tests. Final retention numbers and automated monthly partition maintenance remain Q-050 and future deployment/runbook implementation work.                                                                                                                                                                                                                                                                                                                         |
| F07  | DONE — outbox entity/table/RLS, scoped buffer, `OutboxSaveChangesInterceptor`, relay unit, retry/dead-letter mechanics, and Docker-backed tests. Hangfire scheduling/bootstrap remains Q-049/deployment wiring; payload cap/pointer strategy was later closed for M1 by OUT01.                                                                                                                                                                                                                                                                                                                                                |
| F08  | DONE — provider-neutral JWT bearer configuration, claim/policy constants, current actor projection, tenant membership authorization, permission requirement primitives, and `/auth/session` tests. Q-020 still decides Auth0 vs Keycloak; login/callback/refresh/session-store wiring remains later auth work.                                                                                                                                                                                                                                                                                             |
| F09  | DONE — subject, subject group, subject membership, and subject relationship domain/EF/migration/RLS model. Subject attributes are JSONB, not a separate physical table. Q-007 closed by preserving separate subject and user-account tables.                                                                                                                                                                                                                                                                                                                                                               |
| F10  | DONE — versioned instrument metadata, license/provenance fields, subscales, items, norms, initial instrument-target translations, canonical global read/tenant write-block RLS, and tenant derivative write path. F11 added template/template_version tables and the `canonical_template_version_id` FK.                                                                                                                                                                                                                                                                                                   |
| F11  | DONE — template/template_version/section/question/scale/choice option domain + EF model, template RLS, template translation targets, `instrument.canonical_template_version_id` FK, and `instrument_item.question_id` FK.                                                                                                                                                                                                                                                                                                                                                                                  |
| F12  | DEFERRED/DORMANT FOR PLATFORM-CANONICAL CONTENT — OLBI evidence dossier exists, but D73A closed Q-047 by product de-scope. Tenant-provided/private/internal demo named-instrument content is allowed only when non-canonical and rights-attested; future canonical content requires a new owner/legal decision.                                                                                                                                                                                                                                        |
| F13  | OWNER — HR translation/back-translation metadata; do not fake this in-agent.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| F14  | DEFERRED for future platform-canonical named-instrument seed work; do not seed global canonical instruments without an owner/legal canonical-publish decision.                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| F15  | DEFERRED for future platform-canonical derivative lifecycle because it depends on F14. Generic derivative/private-import lifecycle is already GF01.                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| F16  | DEFERRED for future platform-canonical scoring fixture/schema packaging because it depends on F14/F15. Existing ADR-0008/A48 scoring spec and GF02 remain usable for tenant-provided/private rules.                                                                                                                                                                                                                                                                                                                                                                                                        |
| F17  | FUTURE — visual scoring builder depends on explicit designer/editor assessment, not OLBI.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| F18  | DONE by the generic scoring proof-spine slices; see S03/S04/S05/S06/S07/A49b.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| F19  | BLOCKED — campaign series/audience/assignment model depends on F14 canonical instrument seeding.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| F20  | BLOCKED — response identity modes depend on F19.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| GF01 | DONE — generic derivative/private-import lifecycle foundation: rights scope/status, validity labels, provenance note, private_import validity status, migration, and RLS regression.                                                                                                                                                                                                                                                                                                                                                                                                                       |
| GF02 | DONE — generic scoring-rule persistence for any `template_version`: JSONB rule document, normalized metadata, publish lock state, migration, and template-version-owned RLS.                                                                                                                                                                                                                                                                                                                                                                                                                               |
| GF03 | DONE — generic campaign shell and assignment model against any allowed `template_version`. No launch engine/checks, respondent-rule resolver, response sessions, participant-code hashing, consent/disclosure/retention/ethics tables, scoring snapshots, or OLBI-specific seeding.                                                                                                                                                                                                                                                                                                                        |
| GF04 | DONE — generic response identity mode invariants over campaign/assignment boundaries, plus documented F23 `response_session` invariants. No response capture, `response_session`, `answer`, `participant_code`, Argon2id hashing, consent, scoring, launch snapshots, respondent-rule resolution, UI, or OLBI canonical content.                                                                                                                                                                                                                                                                           |
| D17  | DONE — project assessment/course re-evaluation. Decision: shift to thin tenant setup/UI proof path; no F21/F22/F23 or broad backend foundation slice yet.                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| GF05 | DONE — UI-enabling setup APIs for tenant-private instrument imports, template-version graph creation/detail reads, draft scoring rules, campaign series, campaign drafts, and launch-readiness diagnostics. No response capture, UI, or OLBI canonical content.                                                                                                                                                                                                                                                                                                                                            |
| F29  | DONE — `apps/web` SvelteKit + TypeScript + Tailwind scaffold, tenant setup workspace shell, design tokens, Playwright smoke test, and typed API client boundary.                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| F41  | DONE — guided tenant setup workspace UI against GF05, plus local Development auth/CORS bridge for protected browser calls.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| F42  | DONE — tenant setup visual alignment from the design-reference pass, with generic provenance language and no mock-specific leakage.                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| F43  | DONE — setup-run hygiene, generated sample values, stale-result clearing, compact existing-instrument list, and duplicate-import conflict handling.                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| R01  | DONE — generic respondent capture MVP for setup lab: `response_session`/`answer` domain + EF + migration + RLS guards, respondent-capture API/store, frontend API client, and Response Lab UI. No public token resolver, launch snapshot, consent enforcement, participant-code hashing/anonymous-longitudinal capture, scoring, reports, exports, offline sync, or platform-canonical OLBI content. `answer` is not partitioned yet.                                                                                                                                                                      |
| S01  | DONE — generic scoring execution MVP for setup lab: simple `mean`/`sum` evaluator, `score_run`/`score` domain + EF + migration + RLS guards, score computation API/store, frontend API client, and "Compute score" UI. No full ADR-0008 graph/registry, launch scoring snapshots, worker/outbox scoring, reports, exports, norms, thresholds, longitudinal deltas, public token flow, or platform-canonical OLBI fixtures.                                                                                                                                                                                 |
| S02  | DONE - setup score result panel in the Response Lab: formatted score value, `n`, score-run provenance, "Interpretation pending", and "Not a production report". Frontend-only; no backend/API change, validated interpretation, thresholds, reports, exports, norms, public token flow, consent, participant-code hashing, offline sync, or platform-canonical OLBI content.                                                                                                                                                                                                                               |
| S03  | DONE - fuller ADR-0008 scoring execution subset: graph documents with `inputs`/`nodes`/`outputs`, `select_answers`, `reverse_code`, `mean`, `sum`, `count_valid`, `subscale_aggregate` (`mean`/`sum` only), and `require_all`/`min_valid_count`; legacy `operations` still works. No norms, thresholds, buckets, percentiles, wave deltas, weighted ops, if/else, report/export projection, visual builder, async scoring worker, full validation envelope, or canonical OLBI fixtures.                                                                                                                    |
| S04  | DONE - scoring validation and metadata hardening: setup-created scoring rules validate graph metadata, graph shape/type references, legacy `operations`, required `produces.scores`, output/produces alignment, and compatibility metadata shape/scope references before persistence. Store-level validation protects direct callers. No semver-range compatibility evaluation, visual builder UX, final ADR-0008 editor validation envelope, norms, thresholds, wave deltas, exports, async scoring worker, or canonical OLBI fixtures.                                                                   |
| L01  | DONE - launch-lite campaign freeze: `campaign_launch_snapshot` domain/EF/migration/RLS, `Campaign.Launch(...)`, setup launch command/API/store, snapshot-aware response capture/scoring reads, and setup-workspace "Launch campaign" UI. No public anonymous-longitudinal entry, full ADR-0008 graph/registry, norms, thresholds, reports, exports, offline sync, or platform-canonical OLBI content.                                                                                                                                                                                                      |
| P01  | DONE - public open-link respondent entry for launched anonymous campaigns: hashed token issuing, setup open-link creation endpoint, public token-scoped entry/session/answer/submit endpoints, reusable open-link assignment sessions, setup-workspace open-link action, and minimal `/r/[token]` respondent UI. No invitation batches/email sending, consent/disclosure enforcement, identified entry, anonymous-longitudinal participant-code entry, resume secrets, rate limiting/CAPTCHA, SurveyJS runtime, public score feedback, reports, exports, offline sync, or platform-canonical OLBI content. |
| C01  | DONE - consent/disclosure MVP for anonymous open links: `consent_document`/`consent_record`, default setup disclosure per campaign series, launch-readiness consent check, launch snapshot `consent_document_id`, public consent payload, accepted-grant validation, consent-record/session linkage, and `/r/[token]` consent-first UI. No legal template library, retention/disclosure policy tables, withdrawal, re-consent, PDF artifact, identified public entry, anonymous-longitudinal entry, invitation delivery, or platform-canonical OLBI content.                                               |
| C02  | DONE - retention/disclosure policy foundation: `retention_policy`/`disclosure_policy`, setup proof defaults, launch-readiness blockers, and launch snapshot/response freeze of policy ids. No retention worker, withdrawal/re-consent, report/export k-anonymity enforcement, owner-editable policy UI, final legal templates, invitation delivery, or production retention commitments.                                                                                                                                                                                                                   |
| C03  | DONE - invitation delivery MVP as queued intent: email invite token issuing, `notification` persistence, launched-anonymous invitation batch API, invited anonymous assignments, outbox intent metadata, public email invite token resolution, and setup-workspace queued-invite proof. No SMTP/provider, delivery worker, real send, reminders, bounce/complaint/unsubscribe handling, identified entry, anonymous-longitudinal entry, tenant template editor, rate limiting/CAPTCHA, or platform-canonical OLBI content.                                                                                 |
| R02  | DONE - report/export proof path: tenant-member aggregate score projection for launched campaigns with frozen launch/scoring/consent/retention/disclosure provenance and `k_min` suppression before aggregate values are exposed, plus setup-workspace proof panel. No CSV, codebook, `export_artifact`, object storage, checksum, download link, PDF/PuppeteerSharp renderer, chart/dashboard builder, materialized projection worker, export audit workflow, individual reports, anonymous-longitudinal trajectory export, wave comparison, thresholds, norms, or validated interpretation.               |
| X01  | DONE - report-proof CSV/codebook artifact MVP over R02: tenant-member export endpoint, `export_artifact` domain/EF/migration/RLS, aggregate CSV + codebook JSON, checksum/row-count metadata, inline proof content, and setup-workspace export proof panel. No raw answers, question-level answer codebook, anonymous-longitudinal trajectories, identified subject export, object storage, signed downloads, async worker, PDF, dashboard charts, validated interpretation, norms, thresholds, or platform-canonical OLBI content.                                                                        |
| X02  | DONE - export artifact retrieval/storage/download boundary: tenant-member artifact retrieval and CSV download endpoints over existing aggregate proof `export_artifact` rows, RLS-scoped store reads, missing/cross-tenant not-found behavior, and setup-workspace fetch/download proof. No raw answers, anonymous-longitudinal trajectory exports, object storage, signed URLs, async worker, PDF, dashboard charts, production download audit workflow, validated interpretation, norms, thresholds, or platform-canonical OLBI content.                                                                 |
| U01  | DONE - respondent runtime hardening for mobile proof flow: public `/r/{token}` entry-load retry, local action errors, accessible inline alerts, required/scale answer validation before save/submit, submit retry preserving answers, and mobile e2e coverage for anonymous-longitudinal participant-code entry. No SurveyJS runtime, offline sync, autosave, resume secrets, identified entry, URL scrubbing, rate limiting/CAPTCHA, public score feedback, withdrawal/re-consent, production deliverability, dashboards/PDFs, raw exports, trajectory exports, or platform-canonical OLBI content.       |
| AU01 | DONE - provider-neutral setup auth/session UX boundary: typed `/auth/session` client, setup page checking/sign-in-required/forbidden/session-failed states, dev headers still only behind `PUBLIC_DEV_AUTH_ENABLED=true`, and provider-neutral login/logout URL placeholders. It did not include provider login/callback, refresh-token rotation, app session persistence, user-management UX, or staging deployment. OB02 later closed Q-020 for M1 by ADR-0011.                                                                                                                                                      |

| DEP01-A | DONE - local Docker deployment proof: `deploy/staging` API/web/migrator Dockerfiles, compose Postgres/migrator/seed/API/web services, safe env template, start/smoke/stop scripts, and SvelteKit Node adapter. Local smoke covers health, auth-session boundary, tenant-private import/list, and web shell. No VPS staging, production hardening, GitHub Actions deploy, TLS/reverse proxy, secrets, backups, observability, or provider-specific auth. |

| DEP01-B | DONE - portable VPS staging adaptation: `deploy/staging/docker-compose.vps.yml`, `vps.env.example`, `nginx.example.conf`, and `40-ops/vps-staging-runbook.md` define a provider-neutral nginx/subdomain VPS staging shape. API/web bind to loopback-only ports, Postgres has no host port, and no live VPS resources were touched. DEP01-C/GitHub Actions is deferred; deployment work pauses by owner decision. |

| D23 | DONE - post-DEP01-B product/runtime assessment: the proof spine is ready for UX/UI as frontend product-surface foundation work over existing APIs. Final visual design, SurveyJS Creator/designer, production dashboards, production auth, raw exports, and further deployment automation remain deferred. |

| UX01 | DONE - frontend product surface foundation: `/app` overview, campaign-series hub, setup/operations/reports/waves routes, view-model/adapters, demo fixtures, reusable state/provenance components, route-level proof workflow mounting, and respondent isolation regressions over existing APIs. No backend semantic expansion, SurveyJS Creator/runtime, final dashboards, production auth, raw exports, or deployment automation. |

| D24 | DONE - post-UX01 product/runtime assessment: route/product-surface shape is solved enough; next blocker is static/foundation product surfaces that need backend-backed read models before final visual design. |

| UX02 | DONE - product-surface read-model bridge: tenant-member read models and route data now back `/app`, campaign-series list, and selected-series hub over existing runtime semantics. No final visual design, SurveyJS runtime/Creator, production auth, raw exports, production dashboards, retention workers, deployment automation, or platform-canonical OLBI content were added. |

| D25 | DONE - post-UX02 product/runtime assessment: top-level product read models are now real, but setup/operations/reports/waves still mount the old proof workflow. Course decision: pull `UX03` next for selected-series product workflow composition and route-loading hygiene over existing semantics before final visual design. |

| UX03 | DONE - selected-series product workflow composition: setup/operations/reports/waves now load selected-series hub context, render route-specific summaries and loading/error/retry states, and keep existing proof actions mounted underneath. No final visual design, SurveyJS runtime/Creator, production auth, dashboard/report semantics, retention/withdrawal, deployment automation, or platform-canonical OLBI content was added. |

| D26 | DONE - post-UX03 product/runtime assessment: selected-series product surfaces are now structurally ready for a visual pass. Course decision: pull `UX04` next as a visual product surface pass over existing semantics, not a backend/runtime expansion. |

| UX04 | DONE - visual product surface pass: authenticated `/app`, campaign-series list/hub, setup, operations, reports, and waves now share a more coherent work-focused visual language over existing semantics. Added product visual primitives, refreshed shell/header/nav, restyled product rows/metrics, and contained proof workflows in named proof-only workbenches. No backend semantics, public respondent runtime changes, SurveyJS runtime/Creator, production auth, portfolio management, dashboards/charts/norms/thresholds/PDFs, retention/withdrawal, deployment automation, or platform-canonical OLBI content was added. |

| D27 | DONE - post-UX04 product/runtime assessment: authenticated product surfaces are coherent enough; next default slice is `U02`, a narrow SurveyJS Form Library runtime foundation for `/r/[token]` over existing public respondent contracts. No SurveyJS Creator, offline/autosave/resume, full branching runtime, production auth, new backend semantics, deployment automation, or platform-canonical OLBI content. |

| U02 | DONE - respondent SurveyJS runtime foundation: `/r/[token]` now renders current proof questions through a lazy-loaded SurveyJS Form Library adapter over existing public respondent contracts. Preserves consent/code gates, validation, submit retry, mobile behavior, and public-call auth isolation. No SurveyJS Creator, offline/autosave/resume, full branching runtime, locale switching, production auth, new backend semantics, dashboards/reports, deployment automation, or platform-canonical OLBI content. |

| D28 | DONE - post-U02 product/runtime assessment: U02 fixed the public respondent renderer mismatch, but selected-series authenticated pages still mount proof actions that can create unrelated local campaign series. Course decision: pull `UX05` next for campaign-series workflow anchoring over existing APIs. No full portfolio management, final setup wizard, SurveyJS Creator, deeper respondent runtime, production auth, deployment, report/dashboard semantics, or platform-canonical OLBI content. |

| UX05 | DONE - campaign-series workflow anchoring: `/app/campaign-series` now creates a series through the existing setup endpoint and routes it to setup; selected-series child routes pass route series context into the proof workbench; campaign draft creation reuses the route series instead of creating a hidden local series; the same resolver makes two-wave proof creation route-series-aware when selected context exists. No full portfolio management, final setup/operations/reports/waves UX, SurveyJS Creator, deeper respondent runtime, production auth, deployment, report/dashboard semantic expansion, or platform-canonical OLBI content. |

| D29 | DONE - post-UX05 product/runtime assessment: route-series mutation anchoring is solved enough, but selected-series setup is still not backend-backed as its own setup workspace state. Course decision: pull `UX06` next for a narrow selected-series setup read model and setup route rendering over existing semantics. No final setup wizard, full portfolio management, respondent offline runtime, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content. |

| UX06 | DONE - selected-series setup state bridge: tenant-member `GET /campaign-series/{id}/setup-workspace` derives setup state from existing setup/runtime rows under tenant RLS and `/app/campaign-series/{id}/setup` renders selected campaign, template, scoring, policy, readiness, missing prerequisites, and campaign rows from that read model. Sensitive fields stay omitted. Proof workbench remains secondary/proof-only. No final setup wizard, full portfolio management, respondent offline runtime, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content. |

| D30 | DONE - post-UX06 product/runtime assessment: UX06 closed the selected setup read-state gap, but setup actions still live in the transitional proof workflow. Course decision: pull `UX07` next for selected-series setup workflow foundation over existing setup APIs and `GET /campaign-series/{id}/setup-workspace`, not a final wizard/designer, broad backend state machine, operations/reports/waves replacement, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content. |

| UX07 | DONE - selected-series setup workflow foundation: the selected setup route now has a setup-specific primary action workflow over existing setup APIs and `GET /campaign-series/{id}/setup-workspace`. Action availability uses setup-workspace state, campaign drafts use the selected route series id, successful actions refresh setup-workspace, and action failures stay local. Operations/reports/waves still use proof workbenches. No final setup wizard/designer, setup-progress aggregate, production auth, deployment, report/dashboard expansion, or platform-canonical OLBI content. |

| D31 | DONE - post-UX07 product/runtime assessment: UX07 made selected setup coherent enough for the current proof spine, so the next default slice moves downstream to operations state. Course decision: pull `UX08` next for selected-series operations workspace state over existing launch/public-entry/invitation/notification/delivery-attempt data, not final operations console, production deliverability, reports/waves replacement, production auth, deployment, or platform-canonical OLBI content. |

| UX08 | DONE - selected-series operations state bridge: tenant-member `GET /campaign-series/{id}/operations-workspace` derives operations state from existing launch/public-entry/invitation/notification/delivery/response rows under tenant RLS and `/app/campaign-series/{id}/operations` renders selected campaign, summary counts, missing prerequisites, and campaign operation rows from that read model. Sensitive fields stay omitted. Proof workbench remains secondary/proof-only and can refresh operations state after selected-campaign proof mutations. No final operations console, production deliverability, reports/waves replacement, production auth/deployment, report/dashboard expansion, or platform-canonical OLBI content. |

| D32 | DONE - post-UX08 product/runtime assessment: UX08 closed the selected operations read-state gap enough for the proof spine, but reports and waves still use generic selected-series context plus proof workbenches. Course decision: pull `UX09` next for selected-series reports state bridge over existing R02/X01/X02 report/export proof data. No final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw exports, trajectory exports, production export workers/storage, production auth/deployment, or platform-canonical OLBI content. |

| UX09 | DONE - selected-series reports state bridge: tenant-member `GET /campaign-series/{id}/reports-workspace` derives reports state from existing campaign-series/campaign/launch/response/score/disclosure/export rows under tenant RLS and `/app/campaign-series/{id}/reports` renders selected report campaign, summary counts, provenance/export state, disclosure state, visible/suppressed score counts, missing prerequisites, and campaign report rows from that read model. Sensitive raw answers, score values, scoring document hashes, raw exports, CSV/codebook content, token/path/recipient/provider data, participant-code hashes, and IP/user-agent hashes stay omitted. Proof workbench remains secondary/proof-only and can refresh reports state after export proof creation. No final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw/trajectory exports, production export lifecycle, production auth/deployment, or platform-canonical OLBI content. |

| D33 | DONE - post-UX09 product/runtime assessment: UX09 closed the reports read-state gap enough for the proof spine, leaving waves as the only selected-series child route still on generic selected-series hub state plus proof actions. Course decision: pull `UX10` next for selected-series waves state bridge over existing W01/R03 wave proof data. No final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw exports, trajectory exports, production export workers/storage, production auth/deployment, operations primary workflow, or platform-canonical OLBI content. |

| UX10 | DONE - selected-series waves state bridge: tenant-member `GET /campaign-series/{id}/waves-workspace` derives waves state from existing campaign-series/campaign/launch/response-session/participant-code-linkage/score/scoring-rule/disclosure rows under tenant RLS and `/app/campaign-series/{id}/waves` renders selected baseline/comparison waves, linked and complete trajectory counts, disclosure/compatibility state, safe provenance, missing prerequisites, and wave rows from that read model. Sensitive raw answers, answer values, participant-code values/ids/hashes, code salts, token/path/recipient/provider data, scoring document hashes, raw exports, and trajectory exports stay omitted. Proof workbench remains secondary/proof-only and can refresh waves state after wave proof interactions. No final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw/trajectory exports, production export lifecycle, production auth/deployment, operations primary workflow, or platform-canonical OLBI content. |

| D34 | DONE - post-UX10 product/runtime assessment: UX10 closed the last selected-series read-state gap, leaving setup, operations, reports, and waves all backend-backed. Course decision: pull `UX11` next for selected-series operations workflow foundation because setup has primary route actions but operations still uses the secondary proof workbench for launch, open-link, invitation-batch, and local delivery proof. UX11 should make those operations actions primary over existing endpoints and `operations-workspace` state, target the selected campaign, refresh state after successful mutations, keep proof/local labels, and avoid production deliverability, durable raw recipient/path redisplay, final operations console, dashboards, export lifecycle, production auth/deployment, or platform-canonical OLBI content. |

| UX11 | DONE - selected-series operations workflow foundation: `/app/campaign-series/{id}/operations` now renders launch-readiness, launch, open-link, invitation-batch, and local/dev delivery actions as a primary selected-series workflow over existing setup API endpoints and `operations-workspace` state. Actions target `operationsWorkspace.selectedCampaign`, refresh operations workspace after successful mutations, and keep transient open-link paths, invitation recipients, respondent paths, provider labels, and delivery proof outputs local to proof/local UI. The operations route no longer mounts the generic proof workbench. No production SMTP/provider deliverability, reminders, bounce/complaint/unsubscribe handling, tenant email templates, full recipient management/imports, durable raw respondent-path or raw recipient redisplay, final operations console, dashboards, export lifecycle, production auth/deployment, or platform-canonical OLBI content. |

| D35 | DONE - post-UX11 product/runtime assessment: setup and operations now have backend-backed state plus primary selected-series workflows, while reports and waves remain backend-backed read surfaces with secondary proof actions. Course decision: pull `UX12` next for selected-series reports workflow foundation because report proof, export artifact creation, artifact retrieval, and CSV download still live inside the generic proof workbench despite `reports-workspace` state. UX12 should target `reportsWorkspace.selectedCampaign`, use existing report-proof/export endpoints, refresh reports workspace after export mutations, keep transient response/score/export outputs proof/local, and avoid final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw/trajectory exports, production export lifecycle, production auth/deployment, waves workflow replacement, or platform-canonical OLBI content. |

| UX12 | DONE - selected-series reports workflow foundation: `/app/campaign-series/{id}/reports` now renders report proof, export artifact creation, stored artifact fetch, and CSV download actions as a primary selected-series workflow over existing setup API endpoints and `reports-workspace` state. Actions target `reportsWorkspace.selectedCampaign`; export creation refreshes reports workspace; transient report proof scores, export CSV/codebook/checksum details, stored artifact payloads, and CSV download content stay local to proof/local UI. The reports route no longer mounts the generic proof workbench. No final dashboards, charting, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw answer exports, trajectory exports, object storage/signed URLs/workers, waves workflow replacement, respondent runtime hardening, production auth/deployment, or platform-canonical OLBI content. |

| D36 | DONE - post-UX12 product/runtime assessment: setup, operations, and reports now have backend-backed state plus primary selected-series workflows, while waves remains backend-backed but still uses proof-workbench actions. Course decision: pull `UX13` next for selected-series waves workflow foundation. UX13 should target `wavesWorkspace.series.id`, use existing two-wave and wave-comparison proof endpoints, refresh waves workspace after proof reads where useful, keep transient proof/comparison outputs proof/local, avoid porting the generic workbench's `Create two-wave proof` demo factory, and avoid final dashboards, charts, PDFs, norms, thresholds, validated interpretation, raw/trajectory exports, production export lifecycle, production auth/deployment, respondent runtime hardening, or platform-canonical OLBI content. |

| UX13 | DONE - selected-series waves workflow foundation: `/app/campaign-series/{id}/waves` now renders two-wave proof refresh and wave-comparison proof reads as a primary selected-series workflow over existing setup API endpoints and `waves-workspace` state. Actions target `wavesWorkspace.series.id`, proof reads refresh waves workspace where useful, and transient linked trajectory counts, proof wave metadata, disclosure policy, and comparison scores stay local to proof/local UI. The waves route no longer mounts the generic proof workbench; the generic workbench's `Create two-wave proof` demo factory was not ported. No final dashboards, charts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime hardening, production auth/deployment, portfolio management, or platform-canonical OLBI content. |

| D37 | DONE - post-UX13 product/runtime assessment: setup, operations, reports, and waves now all have backend-backed selected-series state plus primary route workflows. The next mismatch is no longer route/action ownership; report and wave outputs are still mostly proof-click/local action results rather than stable route-owned decision surfaces. Course decision: pull `UX14` next for selected-series report snapshot foundation. UX14 should render selected campaign aggregate report proof as route-owned content on `/app/campaign-series/{id}/reports` using `reportsWorkspace.selectedCampaign` and existing `GET /campaigns/{campaignId}/report-proof`; keep proof-only/not-validated labels visible; keep export actions separate; and avoid charts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, production auth/deployment, respondent runtime hardening, portfolio management, or platform-canonical OLBI content. |

| UX14 | DONE - selected-series report snapshot foundation: `/app/campaign-series/{id}/reports` now renders a route-owned selected report snapshot over existing `GET /campaigns/{campaignId}/report-proof`. The snapshot targets `reportsWorkspace.selectedCampaign`, auto-loads and refreshes the selected campaign proof, and renders proof status, not-validated interpretation status, launch/scoring/disclosure provenance, visible score rows, and suppressed score rows. Report proof/export artifact creation, stored artifact fetch, and CSV download stay in the separate selected-series reports workflow. No final dashboards, charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime hardening, production auth/deployment, portfolio management, wave comparison snapshot, or platform-canonical OLBI content. |

| UX15 | DONE - selected-series wave comparison snapshot foundation: `/app/campaign-series/{id}/waves` now renders a route-owned selected wave comparison snapshot over existing `GET /campaign-series/{id}/wave-comparison-proof`. The snapshot targets `waves-workspace` selected baseline/comparison waves, auto-loads and refreshes the selected series proof, and renders proof status, not-validated interpretation status, selected wave provenance, disclosure policy, linked/complete trajectory counts, visible comparison rows, suppressed comparison rows, and compatibility notes. Two-wave proof refresh and wave-comparison proof actions stay in the separate selected-series waves workflow. No final dashboards, charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime hardening, production auth/deployment, portfolio management, or platform-canonical OLBI content. |

| D38 | DONE - post-UX15 product/runtime assessment: selected reports and waves now have backend-backed state, primary workflows, and route-owned proof snapshots, so the next limiting gap is snapshot resilience rather than basic route ownership or output visibility. Course decision: pull `UX16` next for selected-series report/wave snapshot blocked/error/retry hardening. No final dashboards, charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| UX16 | DONE - selected-series report/wave snapshot resilience: report and wave snapshots now have Playwright coverage for blocked/no-call prerequisites, local proof endpoint failures, explicit failed status, retry recovery, workflow visibility, and suppression safety. `SelectedSeriesReportSnapshot.svelte` and `SelectedSeriesWaveComparisonSnapshot.svelte` now render failed loads as `Failed` instead of `Local`. No final dashboards, charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| D39 | DONE - post-UX16 product/runtime assessment: UX16 closed the report/wave snapshot resilience gap, so the next limiting mismatch is the Reports route still reading like a resilient proof snapshot plus local export actions rather than a coherent report dashboard/decision surface. Course decision: pull `UX17` next for selected-series report dashboard semantics foundation over existing `reports-workspace` and `GET /campaigns/{campaignId}/report-proof`. Keep it table/provenance first, preserve UX16 blocked/error/retry/suppression behavior, keep export actions separate, and avoid waves dashboard semantics, charts/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| UX17 | DONE - selected-series report dashboard semantics foundation: `/app/campaign-series/{id}/reports` now renders a named `Report dashboard` decision surface inside the existing route-owned report snapshot panel. It uses existing `reports-workspace` selected-campaign state plus `GET /campaigns/{campaignId}/report-proof`, groups report readiness, disclosure guardrails, report provenance, latest export readiness, visible score rows, and suppressed rows, preserves UX16 blocked/error/retry/suppression behavior, and keeps export artifact creation/fetch/download in the separate reports workflow. No waves dashboard semantics, charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, object storage, signed URLs, async workers, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| D40 | DONE - post-UX17 product/runtime assessment: UX17 closed the Reports route dashboard semantics gap, but Waves still reads as a resilient wave comparison snapshot plus separate waves workflow. Course decision: pull `UX18` next for selected-series wave dashboard semantics foundation over existing `waves-workspace` and `GET /campaign-series/{id}/wave-comparison-proof`. Keep it table/provenance first, preserve UX16 blocked/error/retry/suppression behavior, keep two-wave and wave-comparison actions separate, and avoid charts/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| UX18 | DONE - selected-series wave dashboard semantics foundation: `/app/campaign-series/{id}/waves` now renders a named `Wave dashboard` decision surface inside the existing route-owned wave comparison snapshot panel. It uses existing `waves-workspace` selected baseline/comparison state plus `GET /campaign-series/{id}/wave-comparison-proof`, groups wave readiness, comparison status, disclosure/compatibility guardrails, baseline/comparison provenance, visible comparison rows, suppressed rows, and blocked rows, preserves UX16 blocked/error/retry/suppression behavior, and keeps two-wave proof refresh plus wave-comparison proof actions in the separate waves workflow. No charting/ECharts, PDFs, norms, thresholds, validated interpretation, production export lifecycle, raw/trajectory exports, object storage, signed URLs, async workers, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| D41 | DONE - post-UX18 product/runtime assessment: selected reports and waves now both have named table/provenance-first dashboard semantics over governed proof projections, so the next limiting gap is export artifact state. Course decision: pull `X03` next for selected-series export artifact registry foundation over the existing `export_artifact` table and proof export endpoints. Keep it table/provenance first, preserve proof-only/not-validated labels and suppression safety, keep fetch/download actions anchored to stored artifact ids, and avoid charts/ECharts, PDFs, raw/trajectory exports, full production export lifecycle, object storage, signed URLs, async workers, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| X03 | DONE - selected-series export artifact registry foundation: `GET /campaign-series/{id}/reports-workspace` now returns a metadata-only `exportArtifacts` registry over recent aggregate report-proof `export_artifact` rows. `/app/campaign-series/{id}/reports` renders those stored artifacts inside the `Report dashboard` with artifact id, campaign label, artifact type/status/format, file name, row count, byte size, checksum, and created/completed timestamps. Report proof creation, stored artifact fetch, and CSV download stay in the separate reports workflow. No raw answer exports, trajectory exports, charting/ECharts, PDFs, norms, thresholds, validated interpretation, object storage, signed URLs, async workers, retention/deletion workflow, download audit workflow, respondent runtime expansion, portfolio management, production auth/deployment, or platform-canonical OLBI content. |

| D42 | DONE - post-X03 product/runtime assessment: X03 closed the selected report artifact visibility gap, and the authenticated selected-series app is now coherent enough to pause the UX/X route-polish lane. Course decision: pull `U03` next for respondent runtime productization foundation. U03 should make `/r/{token}` a clearer public respondent product surface over existing public entry/session/save/submit APIs by adding explicit route states and manual save/review before final submit. No full offline/PWA sync, autosave, resume secrets, URL-token scrubbing, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production rate limiting/CAPTCHA, production deliverability, reports/exports, production auth/deployment, SurveyJS Creator/designer, or platform-canonical OLBI content. |

| U03 | DONE - respondent runtime productization foundation: public `/r/{token}` now separates SurveyJS answer capture from final submit. Respondents use `Save and review` to persist answers through the existing public save endpoint, see saved answer count and session provenance in a review state, can return to edit without losing answers, and submit only through `Submit reviewed response`. Public respondent calls remain isolated from setup/admin auth headers, and SurveyJS remains public-route-only after entry/consent/code gates. No full offline/PWA sync, autosave, resume secrets, URL-token scrubbing, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production rate limiting/CAPTCHA, production deliverability, reports/exports, production auth/deployment, SurveyJS Creator/designer, or platform-canonical OLBI content. |

| D43 | DONE - post-U03 product/runtime assessment: U03 made online respondent capture credible enough for the active proof spine, while the export path remains aggregate-only. Course decision: pull `X04` next for governed campaign-series response export proof over submitted sessions, using the existing `export_artifact` storage/retrieval/download pattern, CSV plus JSON codebook, artifact-local response row ids, and artifact-local trajectory ids for anonymous-longitudinal rows when disclosure rules allow. No production export lifecycle, object storage, signed URLs, async workers, PDFs, download audit workflow, identified subject export, free-text redaction, charting, norms, thresholds, validated interpretation, deeper respondent runtime, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| X04 | DONE - governed campaign-series response export proof: tenant-member `POST /campaign-series/{campaignSeriesId}/response-exports`, `campaign_series_response_csv_codebook` artifact type, export-artifact check-constraint migration, CSV/codebook generation over submitted series sessions, artifact-local response row ids, artifact-local trajectory ids where disclosure allows, reports route `Create response export` action, and reports-workspace registry support for both aggregate proof and response-export artifacts. Narrowness: response exports are anchored to the latest submitted campaign because `export_artifact.campaign_id` is still required; `campaign_series_id` is the semantic export target. No production export lifecycle, object storage, signed URLs, async workers, PDFs, download audit workflow, identified subject export, free-text redaction, charting, norms, thresholds, validated interpretation, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| D44 | DONE - post-X04 product/runtime assessment: X04 made response exports useful, but exposed a structural artifact-model gap: `export_artifact.campaign_id` is still required for a series-targeted response export. Course decision: pull `X05` next for export artifact target-scope foundation. X05 should add explicit campaign vs campaign-series target semantics, support series-targeted response exports without a fake campaign anchor, update constraints/EF/domain/RLS/tenant guards/migration-backfill/tests, expose target kind/id/label in read models, and keep retrieval/download behavior intact. No production export lifecycle, object storage, signed URLs, async workers, queued status transitions, download audit workflow, retention/delete workflow, PDFs, charting/ECharts, validated interpretation, norms, thresholds, identified subject export, free-text redaction, deeper respondent runtime, setup/operations final workflow, portfolio management, production auth/deployment, production deliverability, SurveyJS Creator/designer, or platform-canonical OLBI content. |
| X05 | DONE - export artifact target-scope foundation: `export_artifact.target_kind` now distinguishes `campaign` and `campaign_series`; response exports are series-targeted with nullable `campaign_id`, aggregate proof exports remain campaign-targeted, migration/backfill/RLS/tenant guards enforce the scope, reports DTOs expose target kind/id/label, and the selected-series Reports registry labels response exports with the series target while preserving artifact-id retrieval/download. No production export lifecycle, object storage, signed URLs, async workers, queued status transitions, download audit workflow, retention/delete workflow, PDFs, charting/ECharts, validated interpretation, norms, thresholds, identified subject export, free-text redaction, deeper respondent runtime, setup/operations final workflow, portfolio management, production auth/deployment, production deliverability, SurveyJS Creator/designer, or platform-canonical OLBI content. |
| D45 | DONE - post-X05 product/runtime assessment: X05 fixed artifact target semantics, so the next limiting gap is artifact lifecycle. Current artifacts are immediate `succeeded` proof rows with no queued/rendering/failed/expired/deleted state, lifecycle timestamps beyond created/completed, privacy-safe failure metadata, or download gating for non-downloadable states. Course decision: pull `X06` next for export artifact lifecycle foundation. X06 should add lifecycle statuses, transition invariants, lifecycle timestamps, failure metadata, backfill existing artifacts to `succeeded`, DTO/read-model/registry support, and download blocking for queued/rendering/failed/expired/deleted artifacts while keeping proof exporters synchronous. No object storage, signed URLs, async workers, queue infrastructure, retry/dead-letter processing, retention/delete worker automation, production download audit workflow, PDFs, charting/ECharts, raw/trajectory export expansion, identified exports, validated interpretation, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| X06 | DONE - export artifact lifecycle foundation: `export_artifact` now supports `queued`, `rendering`, `succeeded`, `failed`, `expired`, and `deleted`; lifecycle timestamps, nullable materialization for non-succeeded rows, privacy-safe failure reason codes, migration/backfill, database constraints, DTO/read-model lifecycle fields, frontend Reports registry lifecycle rows, and `canDownload` workflow gating are in place. Existing proof exporters remain synchronous immediate `succeeded` writers, and non-downloadable artifact downloads return conflict. No object storage, signed URLs, async workers, queue infrastructure, retry/dead-letter processing, retention/delete worker automation, production download audit workflow, PDFs, charting/ECharts, raw/trajectory export expansion, identified exports, validated interpretation, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| D46 | DONE - post-X06 product/runtime assessment: X06 fixed the success-only artifact model with lifecycle states, lifecycle metadata, nullable materialization, and download gating. Full production export lifecycle remains broad, so D46 selected `UX19`: selected-series visual analytics foundation. UX19 should add a narrow chart/visual-inspection layer over existing governed report-proof and wave-comparison-proof visible values, preserve table/provenance-first dashboards, keep proof-only/not-validated labels visible, and keep suppressed/blocked/incompatible/not-ready values out of numeric chart points. No validated interpretation, norms, thresholds, PDFs, object storage, signed URLs, async workers, queue infrastructure, retry/dead-letter processing, retention/delete worker automation, production download audit workflow, raw/trajectory export expansion, identified exports, deeper respondent runtime, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| UX19 | DONE - selected-series visual analytics foundation: added pure report/wave chart view models, `echarts` dependency, lazy-loaded `VisualAnalyticsChart.svelte`, Reports visual analytics over visible report-proof means, Waves visual analytics over visible compatible wave-comparison deltas, and suppression-safe accessible value/excluded-row lists. Suppressed, incompatible, missing, or not-ready values are not numeric chart points. No validated interpretation, norms, thresholds, PDFs, object storage, signed URLs, async workers, queue infrastructure, production export lifecycle, raw/trajectory exports, production auth/deployment, production deliverability, or platform-canonical OLBI content. |
| D47 | DONE - post-UX19 product/runtime assessment: UX19 made narrow charting useful, but it also introduced real ECharts/dashboard weight while the A42 route-owned bundle gate is still only documented. Course decision: pull `BUILD-001` next for a local route-owned frontend bundle-budget checker foundation. BUILD-001 should add the checker script, `npm run check:bundles`, route groups, gzip measurements, forbidden dependency leak checks, console output, and JSON report. No GitHub Actions/DEP01-C, live VPS rollout, production auth/deployment, PDFs, object storage, signed URLs, async workers, validated interpretation, norms, thresholds, deeper respondent runtime, production deliverability, or platform-canonical OLBI content. |
| BUILD-001 | DONE - local route-owned frontend bundle-budget checker foundation: added `apps/web/scripts/check-bundle-budgets.mjs`, `npm run check:bundles`, route groups, SvelteKit/Vite manifest route mapping, gzip JS/CSS measurement, forbidden dependency leak checks, console table output, JSON report output, and focused unit coverage. Current hard gates pass locally after `npm run build`. Known warnings remain for conditional lazy ECharts references advertised from shared setup/operations chunks; BUILD-004 later proved them manifest-only for covered runtime paths. No GitHub Actions/DEP01-C, CI artifact upload, live VPS rollout, production auth/deployment, PDFs, object storage, signed URLs, async workers, validated interpretation, norms, thresholds, deeper respondent runtime, production deliverability, or platform-canonical OLBI content. |
| D48 | DONE - post-BUILD-001 product/runtime assessment: BUILD-001 closed the broad first-load route-ownership risk, with respondent entry at 46.4 KB gzip JS, selected Reports/Waves at 87.2 KB gzip JS, and hard local gates passing. The remaining gap is narrower: setup/operations have conditional lazy ECharts warnings from the manifest. Course decision: pull `BUILD-004` next for local Playwright production-route dependency request proof. BUILD-004 should prove whether the warnings are manifest-only or actual runtime requests before more dashboard/PDF/validated-interpretation work. No GitHub Actions/DEP01-C, live VPS rollout, production auth/deployment, PDFs, object storage, signed URLs, async workers, validated interpretation, norms, thresholds, deeper respondent runtime, production deliverability, or platform-canonical OLBI content. |
| BUILD-004 | DONE - local route-smoke dependency request proof: added `apps/web/src/routes/bundle-route-smoke.e2e.ts`, a production-preview Playwright smoke that records route asset requests and maps hashed chunks through the Vite manifest. It proves respondent entry does not request ECharts, SurveyJS runtime, or SurveyJS Creator before consent/session gates; setup and operations do not request ECharts on initial route load; and Reports/Waves request ECharts only when visual analytics mounts. BUILD-001's setup/operations ECharts warning is manifest-only for the covered runtime paths. No GitHub Actions/DEP01-C, live VPS rollout, production auth/deployment, PDFs, object storage, signed URLs, async workers, validated interpretation, norms, thresholds, deeper respondent runtime, production deliverability, or platform-canonical OLBI content. |
| D49 | DONE - post-BUILD-004 product/runtime assessment: BUILD-004 removed the route-splitting pressure by proving the setup/operations ECharts warning is manifest-only for covered runtime paths. Course decision: pull `R04` next for tenant-attested score interpretation foundation over existing report/wave proof outputs. R04 should support tenant-defined score bands/labels with explicit not-validated/not-official provenance, expose labels only for disclosure-safe visible score rows, preserve export/codebook provenance, and avoid official OLBI score labels, platform-canonical fixtures, published norms, threshold alerts, clinical/OSH advice, PDFs, object storage, async workers, validated interpretation, production auth/deployment, and GitHub Actions/DEP01-C. |
| R04 | DONE - tenant-attested score interpretation foundation: optional tenant-defined score bands now live under `scoring_rule.produces.interpretation`, scoring-rule validation rejects unsafe/malformed metadata, report/wave proof outputs expose labels only for disclosure-visible means, aggregate report CSV/codebooks preserve interpretation provenance, and Reports/Waves UI labels stay explicit: tenant-defined, not validated, and not official. No platform-canonical OLBI, official labels, validated interpretation, norms, thresholds, clinical/OSH advice, PDFs, object storage, async workers, production export lifecycle, production auth/deployment, GitHub Actions/DEP01-C, or live VPS rollout. |
| PM01 | DONE - campaign-series portfolio management foundation: `GET /campaign-series` now supports tenant-scoped search/readiness/sort query semantics; `PATCH /campaign-series/{id}` provides narrow non-destructive rename under tenant RLS; `/app/campaign-series` has URL-backed search/filter/sort controls, filtered empty states, and inline row rename that reloads the current query. No archive/delete, ownership transfer, tenant-wide bulk admin, production auth/deployment, GitHub Actions/DEP01-C, PDF/export infrastructure, validated interpretation, norms, thresholds, platform-canonical OLBI, official OLBI score labels, or Q-047 closure. |
| D51 | DONE - post-PM01 product/runtime assessment: PM01 closed the immediate portfolio entry-point gap, so D51 selected `U04` respondent autosave and dirty-state hardening as the next default slice. U04 should add autosave after session creation, visible dirty/saving/saved/save-failed/review-ready state, stale-review invalidation after editing, navigation protection for unsaved answers, and retry behavior over the existing open-link save endpoint. No offline IndexedDB sync, resume secrets, tokenless public session handles, URL-token scrubbing, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, or platform-canonical OLBI content. |

| U04 | DONE - respondent autosave and dirty-state hardening: `/r/[token]` now marks changed answers dirty after session creation, autosaves through the existing open-link save endpoint, shows visible save status, clears stale review after editing, warns before unload while answers are dirty/saving/failed, and keeps failed autosaves retryable through manual save/review. No offline IndexedDB sync, resume secrets, tokenless public session handles, URL-token scrubbing, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, or platform-canonical OLBI content. |

| D52 | DONE - post-U04 product/runtime assessment: U04 made active answering safer, but saved server drafts are not restored after reload/revisit. Course decision: pull `U05` next as respondent same-browser server-draft resume foundation. U05 should add a narrow public draft-read contract over the existing open-link token plus response-session id, restore saved answers/submitted state for same-browser session pointers, ignore stale pointers without membership leaks, and avoid offline IndexedDB sync, service-worker queues, cross-device resume, tokenless public session handles, URL-token scrubbing, identified entry, production auth/deployment, PDFs, or platform-canonical OLBI content. |

| U05 | DONE - respondent same-browser server-draft resume foundation: added a public draft-read contract over the existing open-link token plus response-session id, token/session-bound Postgres draft reads, same-browser non-token session pointers keyed by assignment id, saved-answer and submitted-state restoration after entry load, stale-pointer cleanup, and public-call auth isolation. No offline IndexedDB sync, service-worker queues, cross-device resume, tokenless public session handles, URL-token scrubbing, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, or platform-canonical OLBI content. |

| D53 | DONE - post-U05 product/runtime assessment: U05 closed same-browser server-draft restore, so D53 selected `U06` as the next respondent-runtime slice. U06 should exchange the initial open-link/invitation token for a narrow public response-session handle after session establishment, scrub raw token-bearing URLs from browser history, use the handle for save/draft/submit where possible, preserve neutral stale/invalid handling, and preserve public-call auth isolation. No offline IndexedDB sync, service-worker queues, cross-device resume, account login, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, object storage, async workers, validated interpretation, norms, thresholds, platform-canonical OLBI, official OLBI score labels, or Q-047 closure. |

| U06 | DONE - respondent tokenless session handle and URL-scrubbing foundation: open-link/invitation session creation now issues a server-side `rsh_...` public response-session handle and stores only `response_session.public_handle_hash`; public-handle draft/save/submit endpoints let `/r/[token]` replace `/r/{rawToken}` with `/r/{publicHandle}`, use handle endpoints after session establishment, and reload `/r/{publicHandle}` without raw token entry. No offline IndexedDB sync, service-worker queues, cross-device resume, account login, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, object storage, async workers, validated interpretation, norms, thresholds, platform-canonical OLBI, official OLBI score labels, or Q-047 closure. |

| D54 | DONE - post-U06 product/runtime assessment: U06 closed raw-token steady-state exposure, so D54 selected `U07` as the next respondent-runtime slice. U07 should add a narrow local unsaved-draft mirror after a public response session exists, keyed by public session handle/session pointer rather than raw tokens, restore locally newer answers after same-tab reload or draft-read failure, keep server-saved versus locally-restored state explicit, and preserve public-call auth isolation. No full IndexedDB/PWA sync unless the design proves it is the smallest safe storage boundary, service-worker queues, background sync, offline campaign start, cross-device resume, account login, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, object storage, async workers, validated interpretation, norms, thresholds, platform-canonical OLBI, official OLBI score labels, or Q-047 closure. |
| U07 | DONE - respondent local unsaved-draft resilience foundation: `/r/[token]` now mirrors current unsaved answers in same-browser session storage after a public response session exists, keyed by public session handle/session pointer rather than raw open-link/invitation tokens. Locally newer answers can be restored after reload or draft-read failure, the UI marks locally restored unsaved state honestly, and local mirrors clear after save/submit or stale-session cleanup. No full IndexedDB/PWA sync, service-worker queues, background sync, offline campaign start, cross-device resume, account login, identified entry, full branching runtime, locale switching, public score feedback, withdrawal/re-consent, production deliverability, production auth/deployment, PDFs, object storage, async workers, validated interpretation, norms, thresholds, platform-canonical OLBI, official OLBI score labels, or Q-047 closure. |
| D55 | DONE - post-U07 product/runtime assessment: U04-U07 now cover the immediate online respondent answer-loss path well enough to stop defaulting to deeper offline/PWA work. D55 selected `OPS01` as the next slice: add aggregate started response-session, draft/in-progress session, submitted session, latest activity, and collection status fields to the selected-series Operations workspace and render a route-owned collection monitor. No raw answers, raw tokens, participant codes, recipient addresses, IP/user-agent, per-respondent identity tables, production deliverability, deployment automation, PDFs, production export lifecycle, validated interpretation, or platform-canonical OLBI. |
| OPS01 | DONE - selected-series operations collection monitor foundation: `GET /campaign-series/{id}/operations-workspace` now exposes aggregate started response-session, draft/in-progress session, submitted response, latest response activity, collection status, report-visibility status, and collection guidance fields. The read model derives them from existing tenant-scoped assignments, response sessions, launches, disclosure policy `k_min`, invitation/open-link rows, and delivery attempts. `/app/campaign-series/{id}/operations` renders a route-owned Collection monitor before selected-campaign rows. No raw answers, raw tokens, participant codes, recipient addresses, IP/user-agent, per-respondent identity tables, production deliverability, full offline/PWA sync, deployment automation, PDFs, production export lifecycle, validated interpretation, or platform-canonical OLBI. |
| D56 | DONE - post-OPS01 product/runtime assessment: OPS01 closed the immediate operator collection-visibility gap. D56 selected `UX20` as the next slice because the selected-series hub still lacks lifecycle guidance across Setup, Operations, Reports, and Waves. UX20 should add hub-level status, guidance, route target, and next-action copy derived from existing tenant-scoped state. No final setup wizard/designer, production deliverability, full offline/PWA sync, PDFs, deployment automation, validated interpretation, raw respondent/channel data, or platform-canonical OLBI. |
| UX20 | DONE - selected-series lifecycle guidance foundation: `GET /campaign-series/{id}` now exposes aggregate-only lifecycle guidance for Setup, Operations, Reports, and Waves, derived from existing tenant-scoped governance, live collection, submitted/export, and linked-trajectory state. `/app/campaign-series/{id}` renders the Lifecycle guide between governance and campaign rows. No final setup wizard/designer, final operations console, production deliverability, full offline/PWA sync, PDFs, full production export lifecycle, validated interpretation, raw respondent/channel data, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D57 | DONE - post-UX20 product/runtime assessment: UX20 made the selected-series hub useful as route-to-route orientation, but live inspection confirmed the next bottleneck is first-operator comprehension on Setup. D57 selected `UX21`: selected-series setup path clarity foundation. UX21 should reshape the setup route over existing setup-workspace/actions so private import, template, scoring rule, campaign draft, and readiness read as an ordered current-task path. No final setup wizard/designer, SurveyJS Creator, operations/reports/waves redesign, production deliverability, full offline/PWA sync, PDFs, production auth/deployment, validated interpretation, raw respondent data, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| UX21 | DONE - selected-series setup path clarity foundation: `/app/campaign-series/{id}/setup` now derives an ordered setup path over existing setup-workspace/actions, renders done/current/blocked task state, expands only the current task form, and keeps generated proof defaults in secondary details. Existing setup action calls, route-series campaign draft anchoring, local action errors, and setup-workspace refresh behavior remain intact. No final setup wizard/designer, SurveyJS Creator, visual template/scoring builders, operations/reports/waves redesign, production deliverability, full offline/PWA sync, PDFs, production auth/deployment, validated interpretation, raw respondent data, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D58 | DONE - post-UX21 product/runtime assessment: UX21 made Setup clear enough to move the next bottleneck downstream. Operations is now the next operator route after setup, but it still presents launch readiness, launch, open-link entry, invitation batch, and local delivery proof as equal action cards. D58 selected `UX22`: selected-series operations path clarity foundation. UX22 should add an ordered current-task path over existing operations-workspace/actions while preserving the Collection monitor and proof/local boundaries. No production deliverability, recipient management, final operations console, raw respondent/channel data, full offline/PWA sync, PDFs, production auth/deployment, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| UX22 | DONE - selected-series operations path clarity foundation: `/app/campaign-series/{id}/operations` now derives an ordered operations path over existing operations-workspace/actions, renders done/current/blocked task state, expands only the current task form, and keeps local proof action results visible after the path advances. Existing launch-readiness, launch, open-link, invitation-batch, local delivery, workspace refresh, Collection monitor, and proof/local boundaries remain intact. No production deliverability, recipient management, final operations console, raw respondent/channel data, full offline/PWA sync, PDFs, production auth/deployment, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D59 | DONE - post-UX22 product/runtime assessment: UX22 made Operations clear enough to move the bottleneck downstream. Reports is now the next inspection route after Operations, but it still presents report proof, aggregate export creation, response export creation, stored artifact fetch, and CSV download as equal action cards. D59 selected `UX23`: selected-series reports path clarity foundation. UX23 should add an ordered current-task path over existing reports-workspace/actions while preserving selected report snapshot, visual analytics, artifact registry, lifecycle/download gates, local proof results, and proof/local boundaries. No PDFs, object storage, signed URLs, async workers, production export lifecycle, raw exports beyond existing governed proof, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| UX23 | DONE - selected-series reports path clarity foundation: `/app/campaign-series/{id}/reports` now derives an ordered reports path over existing reports-workspace/actions, renders done/current/blocked state, expands only the current reports task, and keeps local proof/export results visible after the path advances. Selected report snapshot, visual analytics, export artifact registry, lifecycle/download gates, report/export action calls, workspace refresh, and proof/local labels remain intact. No PDFs, object storage, signed URLs, async workers, production export lifecycle, materialized dashboards, validated interpretation, norms, thresholds, raw exports beyond existing governed proof, production auth/deployment, full offline/PWA sync, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D60 | DONE - post-UX23 product/runtime assessment: UX23 made Reports clear enough to stop defaulting to reporting/export depth. Setup, Operations, and Reports now have ordered current-task paths, while Waves still presents two-wave proof refresh and wave-comparison proof as equal action cards. D60 selected `UX24`: selected-series waves path clarity foundation. UX24 should add an ordered current-task path over existing waves-workspace/proof actions while preserving selected wave comparison snapshot, wave dashboard semantics, visual analytics, selected wave rows, disclosure/compatibility labels, local proof results, and proof/local boundaries. No trajectory exports, PDFs, object storage, signed URLs, async workers, production export lifecycle, validated interpretation, respondent offline/PWA, production auth/deployment, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| UX24 | DONE - selected-series waves path clarity foundation: `/app/campaign-series/{id}/waves` now derives an ordered waves path over existing waves-workspace/proof actions, renders done/current/blocked state, expands only the current waves task, and keeps local two-wave/wave-comparison proof results visible after the path advances. Selected wave comparison snapshot, wave dashboard semantics, visual analytics, selected wave rows, disclosure/compatibility labels, proof action calls, workspace refresh, and proof/local labels remain intact. No trajectory exports, PDFs, object storage, signed URLs, async workers, production export lifecycle, materialized dashboards, new wave math, validated interpretation, norms, thresholds, raw exports, production auth/deployment, full offline/PWA sync, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D61 | DONE - post-UX24 product/runtime assessment: Setup, Operations, Reports, and Waves now all have ordered current-task paths, so D61 stopped the default UX path lane and selected `QA01`: local live product-spine smoke and owner walkthrough readiness. QA01 should add one owner-runnable local/dev smoke over existing API/web runtime, development auth, and synthetic tenant-private data, then print created ids/routes for inspection. No GitHub Actions, live VPS rollout, production auth, production deliverability, PDFs, production export lifecycle, full offline/PWA sync, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| QA01 | DONE - local live product-spine smoke and owner walkthrough readiness: `deploy/staging/smoke-product-spine.ps1` now creates unique synthetic tenant-private content, launches two anonymous-longitudinal waves, submits linked public responses without tenant auth headers, computes scores, verifies selected-series hub/setup/operations/reports/waves read models, verifies report/export/two-wave/wave-comparison proof endpoints, checks the web shell, and prints owner inspection routes. This is local/dev acceptance proof only: no GitHub Actions, live VPS rollout, production auth, production deliverability, PDFs, production export lifecycle, full offline/PWA sync, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D62 | DONE - post-QA01 product/runtime assessment: QA01 proved the live local tenant-private spine; the next limiting issue is portfolio hygiene from repeated durable synthetic proof runs. D62 selected `PM02`: campaign-series archive/restore visibility foundation. PM02 is non-destructive archive/restore visibility only. No hard delete/purge of campaign data, responses, answers, scores, launches, artifacts, participant codes, notification rows, consent records, instruments, templates, or tenants; no production auth/deployment, production deliverability, PDFs, full offline/PWA sync, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| PM02 | DONE - campaign-series archive/restore visibility foundation: `campaign_series` now has nullable archive metadata, product surfaces expose tenant-scoped archive/restore mutations, portfolio reads default to active and support active/archived/all visibility, direct selected-series hub reads still work, and the web portfolio/hub render archive state plus restore controls. This is non-destructive visibility only: no purge, retention automation, hard delete, ownership transfer, tenant-wide bulk admin, production auth/deployment, PDFs, full offline/PWA sync, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D63 | DONE - post-PM02 product/runtime assessment: PM02 closed portfolio hygiene from repeated QA01 runs, so D63 selected `CL01` as the next slice. The gap is campaign collection lifecycle: the spine can launch, collect, score, report/export, wave-compare, and archive the containing series, but operators cannot intentionally close a live campaign wave from the product surface. CL01 should add tenant-scoped close action/provenance, stop public response work after close through non-live guards, and keep reports/exports readable. No cancellation policy, scheduled auto-close, retention purge, withdrawal/re-consent, hard delete, archive coupling, reopen, production deliverability, PDFs, deployment/auth, full offline/PWA sync, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| CL01 | DONE - campaign close lifecycle foundation: `campaign` now has nullable `closed_at`, `closed_by_user_id`, and `close_reason`; the domain exposes `Campaign.Close(...)` for live campaigns only; product surfaces expose `POST /campaign-series/{seriesId}/campaigns/{campaignId}/close`; Operations read models and `/app/campaign-series/{id}/operations` expose close state/action; and QA01 closes one wave, verifies its public link fails, and confirms reports/waves remain readable. No cancellation policy, scheduled auto-close, retention purge, withdrawal/re-consent, hard delete, archive coupling, reopen, production deliverability, PDFs, deployment/auth, full offline/PWA sync, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D64 | DONE - post-CL01 product/runtime assessment: CL01 closed the normal stop-collection gap, so D64 selected `R05` as the next slice. The gap is that Reports, Waves, and export/codebook output remain readable after close but do not yet explicitly distinguish preliminary live data from closed-wave data. R05 should add close/finality metadata and labels to report/wave read models and exports before PDFs, object storage, async exports, cancellation policy, or dashboard polish. No cancellation policy, scheduled auto-close, retention purge, automatic final scoring refresh, materialized projections, PDFs, object storage, async workers, production export lifecycle, production auth/deployment, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| R05 | DONE - closed-wave report/export finality foundation: Reports and Waves workspaces now expose preliminary-live, closed-wave, and not-reportable data-finality labels plus campaign close metadata where source campaigns/waves are shown; report proof payloads and aggregate report CSV/codebook exports carry campaign status, closed-at, and finality provenance; campaign-series response exports carry campaign lifecycle columns and codebook summary counts; selected-series Reports/Waves UI surfaces the labels when present; QA01 smoke verifies the post-close finality path. No cancellation policy, scheduled auto-close, retention purge, automatic final scoring refresh, materialized projections, PDFs, object storage, signed URLs, async workers, production export lifecycle, production auth/deployment, validated interpretation, platform-canonical OLBI, Q-047 closure, or official OLBI score labels. |
| D65 | DONE - post-R05 product/runtime assessment: R05 closed the immediate finality/provenance gap, so D65 selected `A49b` as the next slice. The limiting risk is now score confidence: A49a covered an earlier simple non-canonical mean/sum harness, while current report/export/wave behavior depends on the newer generic scoring graph subset, compatibility metadata, score labels, and provenance. A49b should add non-canonical, tenant-safe scoring fixture parity over the current supported engine path. No platform-canonical OLBI fixtures, public OLBI item text, official OLBI labels, Q-047 closure, norms/thresholds/validated interpretation, full ADR-0008 palette/editor work, PDFs/object storage/async exports, production auth/deployment, or cancellation/retention/withdrawal policy. |
| A49b | DONE - per-instrument scoring fixture parity foundation: the synthetic non-canonical fixture replay harness now validates `produces.scores`, tenant-attested interpretation metadata, and compatibility metadata through `ScoringRuleValidator`; covers the current graph subset (`select_answers`, `reverse_code`, `mean`, `sum`, `count_valid`, `subscale_aggregate`), multi-output replay, and `min_valid_count` success/failure boundaries; and asserts value, `n_valid`, engine-level `n_expected`, and `missing_policy_status=ok`. No platform-shipped named-instrument fixtures, public item text, official labels, canonical YAML loader/CODEOWNERS gate, full ADR-0008 palette/editor, norms, thresholds, validated interpretation, score persistence schema change, PDFs/object storage/async exports, production auth/deployment, or cancellation/retention/withdrawal policy. |
| D66 | DONE - post-A49b product/runtime assessment: A49b closed generic fixture parity for the current graph subset, but exposed that engine-level `n_expected` and `missing_policy_status` are still dropped by score persistence and `ComputeScoresResponse`. D66 selected `S05`: score output metadata persistence foundation. S05 should persist engine output metadata through score rows and the scoring API, treating existing `score.n` as `n_valid` or safely renaming it. No platform-canonical OLBI fixtures, public OLBI item text, official labels, Q-047 closure, norms, thresholds, validated interpretation, full ADR-0008 palette/editor, async scoring workers, export/PDF infrastructure, production auth/deployment, cancellation/retention/withdrawal policy, or full offline/PWA runtime. |
| S05 | DONE - score output metadata persistence foundation: score rows now persist engine output metadata through `n_valid`/physical `score.n`, `n_expected`, and `missing_policy_status`; `ComputeScoresResponse` returns `nValid`, `nExpected`, and `missingPolicyStatus`; existing rows backfill `n_expected = n` and `missing_policy_status = 'ok'`; and the setup proof score panel displays valid/expected count metadata. No platform-canonical OLBI fixtures, public OLBI item text, official labels, Q-047 closure, norms, thresholds, validated interpretation, async scoring workers, export/PDF infrastructure, production auth/deployment, cancellation/retention/withdrawal policy, or full offline/PWA runtime. |

| D67 | DONE - post-S05 product/runtime assessment: S05 made score-output metadata durable in `score` rows and the scoring API, but report proof, wave-comparison proof, aggregate report CSV/codebook, and campaign-series response exports still do not carry enough valid/expected/missing-policy context. D67 selected `R06`: report/export score metadata propagation foundation. R06 should thread durable `n_valid`, `n_expected`, and `missing_policy_status` through existing report/export/wave contracts and compact UI labels without adding platform-canonical OLBI, official labels, validated interpretation, new scoring operations, async workers, PDFs/object storage, production export workers, production auth/deployment, lifecycle policy, or full offline/PWA runtime. |

| R06 | DONE - report/export score metadata propagation foundation: report proof and wave-comparison proof now expose visible-only score-output metadata totals/status summaries while keeping suppressed rows null; aggregate report CSV/codebook and campaign-series response CSV/codebook exports carry score-output metadata columns; and selected Reports/Waves proof displays show compact valid/expected/status labels. No platform-canonical OLBI, official labels, validated interpretation, new scoring operations, async workers, PDFs/object storage, production export workers, production auth/deployment, lifecycle policy, or full offline/PWA runtime. |

| D68 | DONE - post-R06 product/runtime assessment: R06 closed downstream score-output metadata propagation, but the owner-runnable QA01 smoke does not yet assert S05/R06 metadata through live API responses and export artifacts. D68 selected `QA02`: local product-spine score metadata smoke refresh. QA02 should extend the synthetic local/dev product-spine smoke to assert computed score response metadata, report proof metadata, wave-comparison metadata, aggregate report CSV/codebook metadata, and campaign-series response CSV/codebook score metadata. No platform-canonical OLBI, official labels, validated interpretation, new scoring operations, dashboard redesign, PDFs/object storage, async export workers, production auth/deployment, retention/withdrawal policy, or full offline/PWA runtime. |

| QA02 | DONE - local product-spine score metadata smoke refresh: `deploy/staging/smoke-product-spine.ps1` now asserts score-output metadata through computed score responses, report proof, wave comparison, aggregate report CSV/codebook artifacts, and campaign-series response CSV/codebook artifacts while preserving owner inspection route/artifact output. No platform-canonical OLBI, official labels, validated interpretation, new scoring operations, dashboard redesign, PDFs/object storage, async export workers, production auth/deployment, retention/withdrawal policy, or full offline/PWA runtime. |

| D69 | DONE - post-QA02 product/runtime assessment: QA02 closed the live-smoke score metadata evidence gap, but exposed that reports/waves/exports still depend on explicit per-session calls to the score endpoint after public submit. D69 selected `S06`: submitted-response score materialization foundation. S06 should materialize score rows automatically after successful setup-lab, open-link, and public-handle session submit while preserving tenant scope, launch snapshots, idempotency, and explicit scoring failure handling. No platform-canonical OLBI, official labels, validated interpretation, new scoring operations, dashboard redesign, PDFs/object storage, async score workers, async export workers, production auth/deployment, retention/withdrawal policy, or full offline/PWA runtime. |

| S06 | DONE - submitted-response score materialization foundation: setup-lab, open-link, and public-handle submits now materialize score rows in the submit transaction when a scoring rule exists. Unscored instruments still submit without scores; invalid scoring rules fail submit explicitly. The explicit score endpoint reuses the same materializer and returns existing materialized scores without duplicate score runs or rows. The product-spine smoke no longer calls the score endpoint per response; reports, exports, and waves are asserted before a single manual score endpoint compatibility proof. No async score workers, new scoring operations, platform-canonical OLBI, official labels, validated interpretation, PDFs/object storage, production auth/deployment, retention/withdrawal policy, or full offline/PWA runtime. |

| D70 | DONE - post-S06 product/runtime assessment: S06 closed the manual per-session scoring-call gap, but submit-time scoring means a bad scoring rule can now block respondent submit. D70 selected `S07`: scoring-rule launch safety preview foundation. S07 should extend launch readiness so the selected scoring rule must resolve against selected template question codes and pass a deterministic execution preview through the current engine before launch. No new scoring operations, visual builder/editor, norms, thresholds, validated interpretation, async score workers, batch backfill, score monitoring dashboards, export/PDF infrastructure, respondent runtime expansion, deployment/auth, or platform-canonical OLBI. |

| S07 | DONE - scoring-rule launch safety preview foundation: launch readiness now extracts selected scoring-rule item codes from legacy operations and graph answer inputs, resolves them against selected template question codes, and runs deterministic preview execution through the current `SimpleScoringEngine` before launch. Missing item-code references return `scoring_rule.item_code_missing`; engine preview failures return `scoring_rule.preview_failed`. Launch snapshot semantics remain unchanged. No new scoring operations, visual builder/editor, norms, thresholds, validated interpretation, async score workers, batch backfill, score monitoring dashboards, export/PDF infrastructure, respondent runtime expansion, deployment/auth, or platform-canonical OLBI. |

| D71 | DONE - broad post-S07 product/runtime assessment: D71 confirmed the vision is still clear but selected a broader validation-support slice instead of another default scoring/runtime hardening slice. The current local tenant-private proof spine can now show setup, launch, public respondent flow, submit-time scoring, reports/waves/exports, finality, and smoke evidence, while sales/demo docs still underclaim the proof. D71 selected `VAL01`: current proof demo and buyer-validation packet refresh. No product code, dashboards, scoring operations, score monitoring, batch backfill, PDF/export infrastructure, production auth/deployment, pricing overhaul, legal templates, validated interpretation, public item text, official labels, Q-047 closure, or platform-canonical claims. |

| VAL01 | DONE - current proof demo and buyer-validation packet refresh: added `50-business/current-proof-demo-brief.md`, refreshed `50-business/sales-narratives.md`, and updated `OWNER-BLOCKERS-ACTION-PACK.md`. Owner-facing materials now state that the local/dev tenant-private proof can show setup, launch readiness, public respondent consent/code flow, answer save/review/submit, submit-time scoring, governed reports/waves, CSV/codebook proof exports, and close/finality labels. O01/O02/O03 now have current-proof call framing, validation asks, evidence-capture prompts, and do-not-claim guardrails. No product code, dashboards, scoring operations, score monitoring, batch backfill, PDF/export infrastructure, production auth/deployment, legal templates, validated interpretation, public item text, official labels, Q-047 closure, or platform-canonical claims. |

| D72 | DONE - post-VAL01 product/runtime assessment: VAL01 made the validation packet accurate enough, so D72 selected runtime trust instead of more sales copy. Next slice is `OPS02`: selected-series score coverage monitor foundation. OPS02 should expose aggregate submitted/scored/unscored/no-scoring-rule/latest-scoring state, preferably from Operations with a compact Reports signal, while staying tenant-scoped and aggregate-only. No raw respondent data, batch backfill, async score workers, new scoring operations, dashboards, PDFs, production auth/deployment, lifecycle policy, validated interpretation, official labels, Q-047 closure, or platform-canonical claims. |

| OPS02 | DONE - selected-series score coverage monitor foundation: Operations and Reports selected-series read models now expose aggregate submitted/scored/unscored/not-configured score coverage, campaign scoring-rule counts, latest scoring activity, status, and guidance. Operations renders the route-owned score coverage monitor and Reports renders a compact readiness signal. No raw answers, tokens, participant codes, recipient data, per-respondent rows, batch backfill, async score workers, dashboards, PDFs, production auth/deployment, validated interpretation, official labels, or platform-canonical claims. |

| D73 | DONE - post-OPS02 roadmap blocker assessment: blocker drift from dormant canonical named-instrument work was separated from the active generic tenant-private runtime. D73 selected `S08` as the next slice to remediate unscored submitted sessions with applicable scoring rules. |

| D73A | DONE - OLBI claim narrative cleanup: active product docs no longer claim or center OLBI. Q-047 is closed by product de-scope, not by legal permission. Future platform-shipped named instruments need a new owner/legal canonical-publish decision. |

| S08 | DONE - selected-series score remediation/backfill foundation: tenant-member `POST /campaign-series/{id}/score-remediation` now reuses the submitted-response score materializer to materialize missing scores for already-submitted sessions in a selected series, preserving launch snapshot semantics and idempotency. It returns only aggregate remediation counts plus latest scoring activity. The Operations score coverage monitor has a compact remediation action and refreshes after completion. No async scoring workers, scheduling, dashboards, PDFs/export infrastructure, lifecycle policy, validated interpretation, official labels, or platform-shipped named-instrument claims. |

| D74 | DONE - post-S08 product/runtime assessment: D74 selected `U08` because the broadest unblocked runtime mismatch is identified respondent entry. Anonymous and anonymous-longitudinal public paths are productized, but `identified` is still only covered by domain/setup-lab primitives. U08 should add a narrow identified self-assessment entry foundation while keeping full respondent-rule resolver depth, production auth, subject import UI, individual dashboards/exports, PDFs, lifecycle policy, validated interpretation, and platform-shipped named-instrument claims out of scope. |

| U08 | DONE - identified respondent entry foundation: live `identified` campaigns can create a narrow `idn_...` operational entry proof with `POST /campaigns/{id}/identified-entry`. The public respondent route resolves identified entries, creates subject-linked consent and response sessions, then exchanges the raw entry proof for the existing `rsh_...` public response-session handle. Identity remains on `assignment.respondent_subject_id` and `consent_record.subject_id`; durable storage keeps token hashes only. No full respondent-rule resolver, production auth, subject import UI, relationship target resolution, individual dashboards/exports, PDFs, lifecycle policy, validated interpretation, or platform-shipped named-instrument claims. |

| D75 | DONE - post-U08 product/runtime assessment plus bounded invariant sweep: identified entry breadth is coherent enough to stop defaulting to deeper identified-entry work. UUIDv7 remediation showed a clear spec-code invariant drift pattern. The assessment found UUIDv7 now guarded, no current production EF bulk-operation offender, broad RLS coverage, partial sensitive-value guards, and one worker/outbox bootstrap mismatch candidate. D75 selected `INV01` before worker bootstrap, full respondent-rule resolver depth, dashboards/PDFs, lifecycle/withdrawal, offline/PWA, production auth/deployment, or buyer-feedback-driven work. |

| INV01 | DONE - spec-code invariant guardrail pack: added `20-architecture/spec-code-invariants.md` as a general invariant ledger; added architecture guards for no production EF `ExecuteUpdate`/`ExecuteDelete`, no production direct console writes, and required ledger coverage; and removed the `Platform.Workers` `Hello, World!` placeholder by making the worker entry point inert pending Q-049. This was a general non-UUID drift pass. No worker bootstrap, production auth/deployment, full respondent-rule resolver, PDFs, lifecycle/withdrawal, offline/PWA, or platform-shipped named-instrument work. |

| D76 | DONE - post-INV01 product/runtime assessment: D76 found the outbox is now the broadest runtime mismatch, but selected `OUT01` before worker bootstrap because production code still lacks dispatcher/bootstrap wiring and Q-051 leaves payload-size semantics undefined. OUT01 should define and enforce a hard JSON outbox payload byte cap, preserve `last_error` truncation, prove current invitation/email-intent payloads remain under cap and free of raw paths/tokens/recipient addresses, and document the future pointer-pattern for large events. No Hangfire/hosted-service scheduling, real dispatcher, provider email delivery, object storage, PDFs, production auth/deployment, lifecycle policy, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| OUT01 | DONE - outbox payload safety boundary: Q-051 is closed for M1. `OutboxPayload.Create` and `OutboxEvent.Create` enforce a 64 KiB UTF-8 JSON hard cap before persistence, event payloads are not truncated, and `last_error` truncation remains separate at 2,000 characters. Unit tests cover below-cap/above-cap/direct-event cases, and the invitation-batch integration proof now asserts queued notification payloads stay under cap while excluding raw respondent paths, invitation tokens, and recipient addresses. Verification also fixed a pre-existing circular RLS policy dependency between `assignment` and `invitation_token` that caused Postgres `42P17 infinite recursion`. No worker scheduler/bootstrap, concrete dispatcher handlers, provider email delivery, payload-size metrics/alerts, object storage, PDFs, production auth/deployment, lifecycle policy, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| D77 | DONE - post-OUT01 outbox/runtime assessment: D77 found that OUT01 removed the payload-size blocker, but relay scheduling should still wait because `OutboxRelay.ProcessDueAsync` marks rows published whenever `IOutboxEventDispatcher.DispatchAsync` returns successfully and production code has no concrete event-type router, handler registration, unknown-event failure rule, or subscriber contract. D77 selected `OUT02`: outbox dispatcher contract foundation. No Hangfire/hosted-service scheduling, provider email delivery, real webhooks, MassTransit/RabbitMQ, observability plumbing, auth, PDFs/object storage, lifecycle policy, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| OUT02 | DONE - outbox dispatcher contract foundation: `Platform.Workers.Outbox` now has `IOutboxEventHandler`, `OutboxEventDispatcher`, `OutboxEventHandlerNotFoundException`, `AddOutboxDispatching()`, and `InvitationEmailQueuedOutboxHandler`. Dispatch routes by exact event type, rejects empty/duplicate registrations, and fails unknown/unregistered event types so relay retry/dead-letter handling retains the event. `InvitationEmailQueued` is an explicit known no-op intent; provider-safe email delivery remains the notification delivery path. No scheduler/bootstrap, provider email delivery, real webhooks, MassTransit/RabbitMQ, observability plumbing, auth, PDFs/object storage, lifecycle policy, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| D78 | DONE - post-OUT02 outbox/runtime assessment: D78 found no remaining pre-scheduler outbox hardening blocker. The relay unit and dispatcher contract exist, but `Platform.Workers` remains inert and production source has no Hangfire, hosted-service, or other scheduler path that calls `OutboxRelay.ProcessDueAsync`. D78 selected `OUT03`: outbox relay scheduler/bootstrap foundation. OUT03 should first verify dependency-policy posture for any Hangfire package or storage adapter, then wire worker host/DI/scheduler/config and focused verification while preserving `InvitationEmailQueued` as a no-op known event and unknown-event retry behavior. No provider SMTP/email delivery, real webhooks, MassTransit/RabbitMQ, observability dashboards/alerts, auth, PDFs/object storage, lifecycle/retention automation, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| OUT03 | DONE - outbox relay scheduler/bootstrap foundation: `Platform.Workers` now builds a real generic host and schedules `OutboxRelay.ProcessDueAsync` through a standard .NET hosted service. Hangfire was not added because current Hangfire packages are LGPL/commercial and conditional under dependency policy; Q-049 remains open for future owner/legal review, scaling pressure, or M2 broker topology. The worker binds `OutboxRelay` options, scopes each tick, registers the OUT02 dispatcher/known handlers, and isolates tick failures. No provider SMTP/email delivery, real webhooks, MassTransit/RabbitMQ, observability dashboards/alerts, production worker deployment/health, auth, PDFs/object storage, lifecycle/retention automation, full respondent-rule resolver, captured-log sentinels, or platform-shipped named-instrument work. |

| D79 | DONE - post-OUT03 runtime assessment: D79 found that OUT03 closed the immediate worker bootstrap mismatch, but active worker logging and relay failure metadata make diagnostics the next privacy boundary. The assessment selected `LOG01`: captured-log sensitive-value sentinel foundation. LOG01 should add a test-only captured logger/sentinel helper, cover outbox worker tick failure logging, outbox relay failure metadata, and public respondent/participant-code boundary failures, then sanitize raw exception diagnostics if they leak participant codes, raw tokens, public session handles, raw answers, connection strings, provider credentials, salts, or raw respondent paths. No OpenTelemetry, dashboards, alert rules, provider SMTP/email delivery, webhooks, broker transport, production auth, deployment automation, PDFs/object storage, lifecycle/retention automation, full respondent-rule resolver, or platform-shipped named-instrument work. |

| LOG01 | DONE - captured-log sensitive-value sentinel foundation: added test-only captured logger support and sentinel assertions for raw participant codes, invitation/open-link/identified-entry tokens, public response-session handles, raw answer text, connection strings, provider credentials, salts, and raw respondent paths. Worker tick failures now log safe exception type only. Outbox relay `last_error` now stores a safe `DISPATCH_FAILED` category with event type, aggregate type, and exception type instead of raw exception text. Public respondent boundary failure paths have a captured-log guard. No OpenTelemetry, dashboards, alert rules, provider delivery, webhooks, broker transport, production auth provider selection, deployment automation, PDFs/object storage, lifecycle/retention automation, full respondent-rule resolver, or platform-shipped named-instrument content. |

| D80 | DONE - post-LOG01 runtime assessment: D80 found that LOG01 closed the immediate diagnostics privacy gap, and the broadest small runtime mismatch is now health/readiness. The API has only one simple `/health` route, target `/health/live`, `/health/ready`, and `/health/startup` routes are not implemented, and current health routing can be rejected before endpoint execution by malformed tenant headers. `Platform.Workers` is a real hosted outbox relay now, but production worker health/heartbeat/metrics remain future work. D80 selected `HLT01`: health endpoint readiness foundation. No OpenTelemetry, dashboards, alert rules, provider-specific auth, deployment automation, product runtime depth, provider delivery, webhooks, broker transport, PDFs/object storage, lifecycle automation, full respondent-rule resolver, or platform-shipped named-instrument content. |

| HLT01 | DONE - health endpoint readiness foundation: API probes now include `/health`, `/health/live`, `/health/ready`, and `/health/startup`. Probe routes are anonymous and tenant-header independent. Readiness/startup checks cover runtime database connectivity, required platform DB configuration presence, and development auth disabled outside Development, returning only check name/status and HTTP 503 on unready. No OpenTelemetry, dashboards, alert rules, provider delivery, webhooks, broker transport, production auth provider selection, deployment automation, PDFs/object storage, lifecycle automation, full respondent-rule resolver, worker health/heartbeat, or platform-shipped named-instrument content. |

| D81 | DONE - post-HLT01 runtime assessment: D81 found that HLT01 made readiness useful enough for the next runtime guard, but non-development provider-neutral JWT/OIDC registration can still tolerate blank authority/audience config while disabling issuer/audience validation and reporting ready. D81 selected `AU02`: provider-neutral auth configuration readiness guard. Q-020 still owns Auth0-vs-Keycloak and provider-specific login/callback/refresh/session decisions. |

| AU02 | DONE - provider-neutral auth configuration readiness guard: `/health/ready` and `/health/startup` now include `oidc_configuration`. Outside Development, missing/blank OIDC authority or audience, disabled HTTPS metadata, or non-HTTPS authority reports unready with only check name/status. Development remains permissive. Q-020 still owns Auth0-vs-Keycloak and provider-specific login/callback/refresh/session decisions. AU02 deliberately did not add provider auth UX, dashboards, deployment automation, worker health, or legal/compliance work. |

| E01 | DONE - provider-safe email delivery worker MVP over C03 queued notifications: `notification_delivery_attempt` domain/EF/migration/RLS, local/dev delivery sink, config-gated SMTP provider boundary, tenant-member process endpoint, sent/failed transitions, invitation-token hash reissue at delivery time, and setup-workspace local delivery proof. No production deliverability operations, SPF/DKIM/DMARC, reminders, bounce/complaint/unsubscribe/suppression handling, tenant template editor, identified/anonymous-longitudinal invitations, marketing email, or production legal copy. |

| P02 | DONE - participant-code foundation with Argon2id per campaign series: `participant_code` domain/EF/migration/RLS, per-series salt hashing, parameter provenance, neutral lookup-or-create store, response-session guard coverage for non-null `participant_code_id`, and launch-readiness cleanup. No raw or normalized participant-code storage/response exposure, no public anonymous-longitudinal entry, no wave UI, no duplicate-submit policy enforcement, no trajectory reports/exports, no withdrawal/recovery, and no platform-canonical OLBI content. |

| W01 | DONE - two-wave campaign-series proof path: tenant-member `GET /campaign-series/{id}/two-wave-proof`, proof read model counts for launched waves/submitted waves/linked trajectories/complete two-wave trajectories, setup-workspace proof creation and refresh controls, and API/client/e2e/Docker coverage. No wave-comparison report semantics, trajectory exports, withdrawal/recovery, production respondent runtime hardening, production deliverability, full respondent-rule materialization, or platform-canonical OLBI content. |

Historical anchor list below is retained for context. Current next item:
`NAV02` simplified global navigation grouped by user intent. Start from
`NEXT-ACTIONS.md`, `current-roadmap.md`,
`../30-features/self-serve-ux.md`,
`assessments/D117-POST-HOME02-SELF-SERVE-UX-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-nav02-simplified-global-navigation.md`,
`assessments/D116-POST-SEEDUX01-SELF-SERVE-UX-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-home02-self-serve-home-cockpit.md`,
`assessments/D115-POST-UXS01-SELF-SERVE-UX-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-seedux01-starter-content-taxonomy-sample-labels.md`,
`assessments/D114-POST-EXP01-SELF-SERVE-UX-COURSE-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-self-serve-ux-simplification-program-design.md`,
`../../plans/2026-05-16-uxs01-self-serve-ux-friction-map.md`,
`assessments/D113-POST-LIB01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-exp01-export-artifact-library-read-only-product-surface.md`,
`assessments/D112-POST-SET01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-lib01-instrument-library-read-only-product-surface.md`,
`assessments/D111-POST-OPS03-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-set01-tenant-settings-read-only-foundation.md`,
`assessments/D110-POST-POL01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-ops03-launch-snapshot-review-foundation.md`,
`assessments/D109-POST-AUTH03-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-pol01-policy-review-details-foundation.md`,
`assessments/D108-POST-U09-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-auth03-session-profile-trust-foundation.md`,
`assessments/D107-POST-RPT01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-u09-respondent-receipt-trust-foundation.md`,
`assessments/D104-POST-UX27-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-des01-setup-template-authoring-foundation.md`,
`assessments/D103-POST-UX26-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-ux27-product-language-and-trust-vocabulary.md`,
`assessments/D102-POST-UI01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-ux26-product-wayfinding-and-workspace-focus.md`,
`assessments/D101-POST-WGT02-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-product-ui-foundation-design.md`, and
`../../plans/2026-05-16-ui01-product-ui-foundation.md`.
Use `../60-roadmap/current-roadmap.md`,
`../20-architecture/spec-code-invariants.md`,
`../70-decisions/0009-outbox-and-audit-interceptor.md`,
`../20-architecture/backend-stack.md`,
`../20-architecture/auth-and-authz.md`,
`../70-decisions/0011-m1-auth-provider.md`,
`../40-ops/q053-counsel-ready-legal-packet.md`,
`../40-ops/observability.md`,
`../40-ops/runbook.md`,
`../40-ops/security.md`,
`OWNER-BLOCKERS-ACTION-PACK.md`,
`OPEN-QUESTIONS.md`,
`NEXT-ACTIONS.md`, latest
`SESSION-LOG.md`,
`assessments/D81-POST-HLT01-RUNTIME-ASSESSMENT-2026-05-14.md`,
`assessments/D82-POST-DEPLOY-DR01-PRODUCT-RUNTIME-ASSESSMENT-2026-05-14.md`, and
`assessments/D86-PRODUCTION-LIKE-DEMO-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D87-POST-RBAC01-PRODUCTION-LIKE-DEMO-ASSESSMENT-2026-05-15.md`,
`assessments/D88-POST-VAL08-PRODUCTION-LIKE-DEMO-ASSESSMENT-2026-05-15.md`,
`assessments/D89-POST-VAL09-PRODUCTION-LIKE-DEMO-ASSESSMENT-2026-05-15.md`,
`assessments/D90-POST-VAL10-PRODUCTION-LIKE-DEMO-ASSESSMENT-2026-05-15.md`,
`assessments/D91-POST-VAL11-PRODUCTION-LIKE-DEMO-ASSESSMENT-2026-05-15.md`,
`assessments/D92-POST-TEAM01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D93-POST-BETA01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D94-POST-TEAM02-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D95-POST-ORG01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D96-POST-RR01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`assessments/D97-POST-RR02-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-15.md`,
`../../plans/2026-05-15-rbac01-role-aware-product-access.md`, and
`../../plans/2026-05-15-val08-validation-demo-data-depth.md`, and
`../../plans/2026-05-15-val09-validation-preflight-compatibility.md`, and
`../../plans/2026-05-15-val10-validation-demo-duplicate-seed-guard.md`, and
`../../plans/2026-05-15-val11-validation-remote-preflight.md`, and
`../../plans/2026-05-15-team01-tenant-member-roster.md`,
`../../plans/2026-05-15-private-beta-product-readiness-lens-design.md`, and
`../../plans/2026-05-15-beta01-product-entry-and-proof-lab-demotion.md`, and
`../../plans/2026-05-15-team02-tenant-member-invite-and-role-assignment.md`,
`../../plans/2026-05-15-org01-subject-directory-and-hierarchy-design.md`,
`../../plans/2026-05-15-org01-subject-directory-and-hierarchy.md`,
`../../plans/2026-05-15-rr01-respondent-rule-audience-preview-design.md`,
`../../plans/2026-05-15-rr02-respondent-rule-materialization-design.md`,
`../../plans/2026-05-15-widget-manifest-system-design.md`,
`../../plans/2026-05-15-wgt01-report-widget-manifest-foundation.md`, and
`../../plans/2026-05-15-home01-workspace-command-center.md` as anchors.
Use `assessments/D102-POST-UI01-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-ux26-product-wayfinding-and-workspace-focus.md`,
`assessments/D103-POST-UX26-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-ux27-product-language-and-trust-vocabulary.md`, and
`assessments/D104-POST-UX27-PRIVATE-BETA-PRODUCT-READINESS-ASSESSMENT-2026-05-16.md`,
`../../plans/2026-05-16-des01-setup-template-authoring-foundation.md`, and
the recent UI01 through NAV02 session-log entries for background, then run
D118. D73A clarified
that the active product is a generic tenant-provided validated-instrument
runtime, not an OLBI product, and Q-047 is closed by de-scope rather than legal
permission. OPS02 added aggregate selected-series score coverage visibility.
S08 added the narrow tenant-scoped remediation action over unscored submitted
sessions. U08 added a narrow identified self-assessment entry path without
turning it into the full F22 respondent-rule resolver or production auth. The
UUIDv7 remediation centralized app-side persistent ID creation in
`PlatformIds.NewId()` and added a source guard against future direct GUID
creation. D75 selected INV01 to convert that drift lesson into a maintained
invariant guardrail pack before pulling more product surface depth. INV01 then
made the ledger general: UUIDv7 is only INV-001, with additional rows for EF
bulk operations, console placeholder output, sensitive values, tenant scope,
worker/outbox bootstrap, identity modes, launch snapshots, and reports/exports.
OUT01 added INV-010 and closed Q-051 with a 64 KiB UTF-8 JSON hard cap for
outbox payloads before persistence. D77 selected OUT02 because dispatch success
must be explicit before scheduler/bootstrap can safely mark rows published.
OUT02 then added the dispatcher contract and known `InvitationEmailQueued`
handler. D78 selected OUT03 because the relay was ready for a narrow M1 worker
bootstrap/scheduler slice. OUT03 then implemented that bootstrap with a standard
.NET hosted service and left Q-049 open because Hangfire packages are
conditional under dependency policy. D79 selected LOG01 because active worker
logging and relay failure metadata needed sensitive-value sentinels before more
observability or handler expansion. LOG01 is now done. D80 selected HLT01
because the current API health surface was still a simple `/health` smoke,
target split health endpoints were missing, and probe routes should not be
rejected by tenant-header parsing before endpoint execution. HLT01 then added
split API health endpoints and non-sensitive readiness checks for runtime
DB/config/dev auth posture. D81 selected AU02 because non-development
provider-neutral JWT/OIDC registration can still tolerate missing
authority/audience config and disable issuer/audience validation while
readiness reports ready. AU02 then added the provider-neutral readiness guard.
The owner explicitly agreed that after AU02 the work should stop dancing around
owner blockers. OB01 refreshed `OWNER-BLOCKERS-ACTION-PACK.md` with a this-week
owner checklist, packet-status table, O01/O02/O03 capture templates,
Q-020/Q-021/Q-053 decision packets, and current official source checks. OB02
then added ADR-0011, closed Q-020 for M1 with Auth0 Free as the accepted
provider and Keycloak as fallback, recorded the interim Hostinger demo/staging
posture for Q-021, and recorded Q-053 as proof-only/no-real-data until legal/entity
path exists. VAL02 then added `validation-demo-source-checks.md` and
`validation-demo-tenants.md`, setting the source gate and seed specification
for the prof occupational-health tenant, Danijel/Algebra education tenant, and
Croatian `zastita na radu` tenant. VAL03 then added the repeatable proof-only
seed path under `deploy/staging`: synthetic catalog, tenant bootstrap SQL,
fixture README, and `seed-validation-demo.ps1` for tenant-private instruments,
campaign states, report/response exports, two-wave proof, and owner inspection
routes. AUTH01 then added the first Auth0 Free interactive login/callback/
logout/app-cookie foundation: opt-in OIDC mode, `/auth/login` tenant guardrails,
verified-email tenant-user/role-assignment projection into platform claims,
frontend cookie credentials and `PUBLIC_TENANT_ID`, and VPS staging Auth0
placeholders with development auth disabled. OB03 then added ADR-0012 and
closed Q-021 for M1: portable Docker Compose with the current owner-paid
Hostinger VPS as validation host, plus explicit migration triggers for another
EU VPS, managed database, hyperscaler, single-tenant/self-host, or future
homelab/interface-VPS path. Q-053 still controls real-person data and
production legal/GDPR claims. OB04 then added
`../40-ops/q053-counsel-ready-legal-packet.md` as the structured counsel intake
artifact for the first real-person pilot scenario while keeping Q-053 open.
AUTH02 then added provider-subject hash binding, tenant-scoped local app
session persistence and validation, logout local-session revocation, backend
cookie-CSRF enforcement for unsafe tenant-member mutations, and opt-in Svelte
CSRF token attachment for authenticated app clients. Auth0 remains identity
provider only, and Q-053 still blocks real-person data and production
legal/GDPR claims.
DEPLOY-DR01 then added `deploy/staging/backup-restore-smoke.ps1`, an
owner-runnable PostgreSQL `pg_dump`/`pg_restore` smoke for the ADR-0012
validation host. It backs up the current Compose Postgres service, restores
into a disposable project, verifies schema/table presence without printing row
data or secrets, and cleans up restore volumes by default. It is not scheduled
backup automation, PITR/WAL archiving, off-host encrypted retention, live VPS
provisioning, GitHub Actions, or real-person data approval.
D82 then reassessed product/runtime direction after AUTH02 and DEPLOY-DR01.
It selected VAL04 because VAL03 seeds validation tenants through Development
auth headers while Auth0-backed staging requires platform `user_account` and
tenant-scoped `role_assignment` rows for owner-controlled verified Auth0 test
emails before a tenant can be entered through the real app-cookie login path.
Q-049 still controls future worker/outbox scaling, owner/legal Hangfire review,
and M2 broker revisit.

## Hard truths

- Formal M1 buyer validation is still gated by **O01**, but owner currently deems v2 demand/direction sufficient to continue because v2 will substantially surpass v1. Platform-shipped named-instrument presets are de-scoped from active M1 and require a future owner/legal canonical-publish decision.
- The current risk is building a strong generic survey foundation while delaying the proof path, owner validation, and visible setup workflow. D17 chose a tenant setup/UI proof path to counter that risk.
- Local `instruments-platform` repo exists at `C:\Users\Martin\source\repos\MartMajer\instruments-platform`, containing `docs/v2/`, the backend .NET 9 solution, and backend tests. A remote/GitHub repo still needs owner action if desired.
- Opus quota will recurring-bite us if we keep using it for bulk doc work. Switch to Sonnet for docs as the standing rule.

## What NOT to do next session

- Do NOT dispatch 3+ Opus agents in parallel.
- Do NOT write 2-3k-token agent prompts. Reference + delegate.
- Do NOT re-read the full session transcript at start; trust this file + the handoff trio.
- Do NOT commit-then-immediately-redispatch — verify quota first.

## Suggested first-message template for owner next session

> "Resume from NEXT-SESSION-PLAN.md. Read `../60-roadmap/current-roadmap.md`,
> `../20-architecture/spec-code-invariants.md`,
> `../70-decisions/0009-outbox-and-audit-interceptor.md`,
> `../70-decisions/0011-m1-auth-provider.md`,
> `../70-decisions/0012-m1-portable-docker-hosting-posture.md`,
> `../20-architecture/backend-stack.md`,
> `../20-architecture/auth-and-authz.md`,
> `../40-ops/q053-counsel-ready-legal-packet.md`,
> `../40-ops/observability.md`, `../40-ops/runbook.md`,
> `../40-ops/security.md`, `OWNER-BLOCKERS-ACTION-PACK.md`,
> `OPEN-QUESTIONS.md`, `NEXT-ACTIONS.md`,
> `validation-demo-source-checks.md`, `validation-demo-tenants.md`,
> `assessments/D81-POST-HLT01-RUNTIME-ASSESSMENT-2026-05-14.md`,
> `assessments/D82-POST-DEPLOY-DR01-PRODUCT-RUNTIME-ASSESSMENT-2026-05-14.md`, and the
> latest `SESSION-LOG.md` entry after the handoff files. Current engineering
> is the tenant-private validated-instrument proof spine. Deployment automation
> remains deferred unless the owner reopens it. A49b through D82 are
> done. Q-047 is closed by de-scope, not permission; future platform-shipped
> named-instrument content requires a new owner/legal canonical-publish
> decision. Q-020 is closed for M1 by ADR-0011: Auth0 Free with Keycloak
> fallback. AUTH01 added the first Auth0 Free interactive login/callback/
> logout/app-cookie foundation for demo/staging validation. AUTH02 added
> provider-subject hash binding, local app-session persistence/validation/
> revocation, and cookie-CSRF enforcement for authenticated app mutations.
> Q-021 is closed for M1 by ADR-0012: portable Docker Compose with the current
> owner-paid Hostinger VPS as validation host and migration triggers for
> stronger hosting. OB04 added the Q-053 counsel-intake packet, but Q-053 still
> allows proof/demo data only until legal/entity/GDPR path exists for real
> people. VAL03 through VAL07 added the proof-only validation tenant seed,
> Auth0 membership bootstrap, walkthrough packet, selected-tenant helper, and
> preflight helper. DEPLOY-DR01 added an owner-runnable backup/restore smoke
> under `deploy/staging/backup-restore-smoke.ps1`. D86 selected RBAC01, and
> RBAC01 is complete: existing Auth0/session permissions now gate unsafe
> setup/product/report/export/notification/lab-response/scoring actions through
> `setup.manage`, while analyst/viewer sessions keep read-only product access.
> VAL08 deepened the validation tenants with eight-question synthetic
> instruments, audience-specific story names, and richer response profiles.
> D88 selected VAL09, and VAL09 fixed the Windows PowerShell preflight live-check
> compatibility issue; OH full preflight now passes in the Auth0/VPS-style local
> Compose shape.
> D89 selected VAL10, and VAL10 made validation demo seeding fail closed before
> duplicate API writes unless `-AllowDuplicateSeed` is explicitly passed.
> D90 selected VAL11, and VAL11 added remote-only validation preflight for
> public API/web origins without secrets, local `.env`, Docker, or database
> access.
> D91 selected TEAM01, and TEAM01 added read-only in-app tenant member/role
> visibility through `GET /tenant-members` and `/app/team` without adding user
> provisioning or role editing.
> D92 selected BETA01 after the owner clarified that the app should be shaped
> as a normal product and the demo should be shaped around it. BETA01 made root
> `/` a private-beta product gateway, moved the proof workflow to labelled
> internal `/proof-lab`, and hid `Demo fixtures` navigation unless
> `PUBLIC_DEMO_SURFACES_ENABLED=true`.
> D93 selected TEAM02 because tenant member access is visible in `/app/team`
> but still not product-manageable. TEAM02 then added app-owned member
> preparation and role assignment while keeping Auth0 as identity provider only.
> D94 selected ORG01 because the next normal-product limiter is the missing
> visible subject directory and hierarchy surface for CEO-manager-user,
> department/team, course/cohort, and future respondent-rule work.
> ORG01 then added setup-authorized subject/group/membership/manager APIs,
> `/app/directory`, selected-subject editing, and synthetic validation-tenant
> hierarchy. D95 selected RR01 because the next limiter is previewing how
> respondent rules resolve against that graph during campaign setup. RR01
> should stay preview-only and narrow: no immutable launch assignment
> generation, reseeding, invitation creation, full 360 reporting, bulk import,
> real-person data approval, or legal/GDPR claims.
> WGT01 then added the first backend-served Reports widget manifest and local
> Svelte registry. Reports render supported widgets before the existing
> workflow, unknown widget kinds fail safely, and manifest delay/failure does
> not block existing Reports panels.
> D98 selected HOME01 because `/app` still needed to tell the user what to do
> next across normal product routes. HOME01 then added the backend-backed
> command center.
> D99 selected UX25, and UX25 made selected-series command-center destinations
> task/result-first. D100 selected WGT02 because the Reports widget manifest
> still rendered four backend-known widget kinds as unsupported. WGT02 then
> added concrete frontend renderers for all six current Reports widget kinds
> while preserving the unsupported fallback for truly unknown future widgets.
> D101 selected UI01 because the normal app is now broad enough but visually
> rough. UI01 added the first shared product UI foundation across the normal
> app without changing backend semantics. D102 selected UX26 because
> selected-series wayfinding and route focus are now the sharpest usability
> limit. UX26 then added the shared selected-series workspace frame/navigation
> across hub, Setup, Operations, Reports, and Waves, keeping primary route work
> before dense reference context. D103 selected UX27 because normal app primary
> copy exposed proof-era workflow language. UX27 then cleaned normal app
> primary workflow vocabulary while preserving trust/provenance labels. D104
> selected DES01 because Setup template authoring is now the sharper limiter.
> DES01 added editable Setup question rows and generated scoring defaults.
> D105 selected QG01, and QG01 restored `svelte-check` to green. D106
> selected RPT01, and RPT01 added the selected report executive summary.
> D107 selected U09, and U09 added the public respondent receipt state. D108
> selected AUTH03, and AUTH03 added email-backed session profile data plus the
> account/permission shell summary. D109 selected POL01, and POL01 added
> privacy-safe consent, retention, and disclosure review details to the
> selected-series Setup policy surface. D110 selected OPS03, and OPS03 added
> the selected-campaign Operations launch snapshot review over frozen template,
> scoring, policy, identity, locale, question-count, and launch provenance
> fields. D111 selected SET01, and SET01 added the read-only tenant settings
> surface. D112 selected LIB01, and LIB01 added the read-only instrument
> library surface over existing visible instruments. D113 selected EXP01, and
> EXP01 added the read-only export artifact library over existing artifact
> metadata. D114 selected a self-serve UX simplification program: starter
> sample studies are normal tenant data, clearly labelled, read-only by
> default, and duplicated into editable own studies for experimentation.
> UXS01 added the route friction map, sample-vs-own contract, and first-session
> acceptance criteria in `30-features/self-serve-ux.md`.
> D115 selected SEEDUX01 because sample-vs-own study separation needs durable
> campaign-series metadata before home, navigation, campaign-series grouping,
> selected-series simplification, help, or duplicate-to-edit can be safe.
> SEEDUX01 added persisted campaign-series study kind/sample scenario metadata,
> projected sample ownership through product read models, marked validation
> starter series as sample studies, rendered Sample study/Your study labels and
> read-only banners, and blocked sample rename/archive/restore.
> D116 selected HOME02 because `/app` now has the sample/own metadata it needs
> but still leads with workspace/command-center/read-model wording, totals, and
> raw permission-code detail instead of teaching samples, own studies, and the
> prepare/collect/review/export lifecycle.
> HOME02 added backend workspace study collections and turned `/app` into a
> self-serve study cockpit: lifecycle phases first, Sample studies before Your
> studies, state-based sample/own study links, workspace totals secondary, and
> suggested next actions without raw permission-code rows.
> D117 selected NAV02 because the shell still mixes Workspace, Campaign series,
> support/admin links, and selected-series links into one flat navigation list.
> NAV02 grouped product navigation into Studies, People and access, Workspace
> admin, conditional Selected study, and flag-gated Internal tools sections
> while preserving routes and backend contracts.
> D118 selected SERIES03 because `/app/campaign-series` is now the next
> unaided destination but still reads like a technical campaign-series list
> rather than a study portfolio.
> SERIES03 made `/app/campaign-series` render as Studies / Study portfolio,
> grouped Sample studies and Your studies by lifecycle, exposed primary study
> action links, and kept filters/mutations secondary over existing APIs.
> SETUP03 made `/app/campaign-series/{seriesId}/setup` render as Prepare study
> / Study preparation with a checklist before setup reference detail.
> D121 selected OPS04 because `/app/campaign-series/{seriesId}/operations`
> still leads with runtime operations/reference framing while collection
> progress and respondent access are buried lower on the page.
> OPS04 made `/app/campaign-series/{seriesId}/operations` render as Collect
> responses / Study collection, with Collection progress before Collection
> actions and Collection reference.
> D122 selected RPT02 because `/app/campaign-series/{seriesId}/reports`
> still leads with reporting/widgets/workflow/reference framing while findings,
> coverage, limitations, and export next use are not the first story.
> RPT02 made `/app/campaign-series/{seriesId}/reports` render as Review
> results / Study results, with Results overview before report widgets,
> report snapshot, review/export actions, and Results reference.
> D123 selected EXP02 because `/app/exports` still leads with artifact
> inventory/file metadata before artifact purpose, readiness, source context,
> and next use.
> EXP02 made `/app/exports` render as Use exports / Study support, with Export
> overview before artifact cards and Export reference.
> D124 selected HELP01 because route understanding was still scattered across
> headers, primary panels, empty states, and sample read-only banners.
> HELP01 added contextual route guidance across the main product routes and
> tightened generic empty-state copy.
> D125 selected DUP01 because the promised sample-to-own transition is now the
> sharpest self-serve gap.
> DUP01 added the setup-authorized duplicate action and backend copy path so a
> sample study can become a new editable own study without copying operational
> evidence.
> SETUP04 made the first editable setup landing lead with Current setup task
> before setup path/progress and generated defaults.
> D130 selected DIR02 because Directory already carries subject/group/manager
> hierarchy state but still leads with raw setup forms.
> DIR02 made Directory lead with People and targeting overview, counts, and
> subject/group read models before setup action forms.
> D131 selected WAVES02 because Waves still exposes repeated-wave capability
> through snapshot/proof/local/workflow/reference language before explaining
> when longitudinal analysis matters.
> WAVES02 made Waves lead with Longitudinal analysis overview and current
> baseline/comparison, trajectory, disclosure, compatibility, and comparison
> availability before snapshot/workflow/reference context. Current waves task
> now leads before Waves path.
> D132 then selected UI03 after the owner reviewed the refreshed app and called
> out the remaining card-heavy visual quality problem. UI03 removed the
> redundant product topbar from `/app`, changed shared product panels into
> unframed sections, changed route guidance into a strip, metrics into
> table-like strips, session context into a flat band, and repeated records
> into separator-based rows. It added a Playwright contract against raised-card
> shadows/radii on shared primitives while preserving APIs, route semantics,
> sample/read-only labels, proof/finality/provenance posture, RBAC, Q-053, and
> platform-canonical exclusions.
> D133 then selected HOME03 as the first new UI/UX run slice because `/app`
> still put immediate work below sample/own/lifecycle/totals context. HOME03
> moved Suggested next actions to the top of the Home cockpit and added a
> Playwright ordering contract while preserving workspace overview API use,
> route guidance, sample/own labels, sample read-only behavior, navigation,
> RBAC, Q-053, and the no-card UI03 direction.
> D134 then selected STUDIES05 because Studies still kept Create your study
> below portfolio rows and filters. STUDIES05 moved create/read-only state into
> the top of the Study portfolio surface and added a Playwright ordering
> contract while preserving create, duplicate, rename, archive, restore, query,
> routing, sample labels, sample read-only behavior, RBAC, Q-053, and the
> no-card UI03 direction.
> D135 then selected SERIES05 because selected-study overview is the next route
> after Studies and still renders lifecycle phases and reference details as
> card-like boxes. SERIES05 should turn lifecycle phases into action rows and
> reference/governance details into row/table primitives while preserving
> selected-study routes, duplicate/restore behavior, sample read-only state,
> route guidance, RBAC, Q-053, and platform-canonical exclusions.
> SERIES05 then implemented that selected-study cleanup. Study lifecycle now
> renders as action rows instead of lifecycle cards, and selected-study
> reference/governance detail uses row/table primitives instead of inline
> rounded boxes. It preserved selected-study loading, retry, duplicate, restore,
> child-route links, route guidance, sample/read-only state, archive state,
> RBAC, Q-053, and platform-canonical exclusions.
> D136 then selected TEAM03 because Team is the next unaided-product access
> limiter. The route has TEAM01/TEAM02 mechanics, but the visible roster still
> exposes raw permission codes such as setup.manage and team.manage. TEAM03
> should keep Team APIs, create/role-change behavior, role names, identity
> status, RBAC, Q-053, and platform-canonical exclusions while leading with
> access capabilities and rendering member access in human language.
> TEAM03 then implemented that cleanup. Team now leads with a Team access
> overview, uses flat stat/capability rows, and renders member access as Study
> setup and launch, Team access management, Reports and exports, or neutral
> fallback capabilities instead of visible raw permission codes. It preserved
> Team APIs, create/role-change behavior, role names, identity status, RBAC,
> Q-053, deployment posture, and platform-canonical exclusions.
> D137 then selected HOME04 because Home is still the first authenticated screen
> and it still renders Study lifecycle and Workspace totals as metric-card
> tiles. HOME04 should keep HOME03 ordering, workspace overview data, route
> guidance, sample/own/action rows, RBAC, Q-053, and platform-canonical
> exclusions while replacing those Home tiles with flat row/list primitives.
> HOME04 then implemented that first-screen cleanup. Home keeps the HOME03
> order, but Study lifecycle now uses flat lifecycle rows, Workspace totals uses
> flat total rows, and the Home cockpit has a zero metric-card Playwright
> contract.
> D138 then selected SERIES06 because selected-study overview still has one
> remaining no-card drift after SERIES05: Study reference totals use
> metric-card. SERIES06 should replace those totals with flat rows and add a
> selected-study zero metric-card contract while preserving lifecycle rows,
> duplicate/restore behavior, route guidance, RBAC, Q-053, and platform-
> canonical exclusions.
> SERIES06 then implemented that cleanup. Study reference totals now use flat
> selected-reference total rows, and the selected-study overview has a zero
> metric-card Playwright contract.
> D139 then selected DIR03 because Directory is the next normal route where
> hierarchy comprehension is good but the counts still use metric-card.
> DIR03 should replace People and targeting and Directory graph count cards
> with flat rows while preserving DIR02 ordering, advanced attributes behavior,
> RBAC, Q-053, and platform-canonical exclusions.
> DIR03 then implemented that cleanup. Directory count clusters now use flat
> rows, and the Directory contract checks hierarchy order, hidden advanced
> attributes, and zero metric-card elements together.
> D140 then selected EXP03 because Exports is the next normal lifecycle route
> with route-local metric-card drift: Export overview and Export artifact counts.
> EXP03 should replace those clusters with flat rows while preserving artifact
> purpose, next-use copy, artifact rows, report links, RBAC, Q-053, and
> platform-canonical exclusions.
> EXP03 then implemented that cleanup. Export overview and artifact counts now
> use flat rows, and the Exports contract checks artifact purpose, ordering,
> counts, and zero metric-card elements together.
> D141 then selected LIB02 because Instrument library is the next small
> route-local no-card drift on a core product asset surface. LIB02 should
> replace Instrument library count cards with flat rows while preserving visible
> instrument rows, launch eligibility labels, management links, RBAC, Q-053, and
> platform-canonical exclusions.
> LIB02 then implemented that cleanup. Instrument library counts now use flat
> rows, and the route contract checks visible instruments, counts, management
> link, and zero metric-card elements together.
> D142 then selected SET02 because Settings is the last small standalone
> route-local count-card cleanup. SET02 should replace Tenant workspace count
> cards with flat rows while preserving profile details, management links, RBAC,
> Q-053, and platform-canonical exclusions.
> SET02 then implemented that cleanup. Tenant workspace counts now use flat
> settings count rows, and the Settings contract checks profile details, counts,
> management links, and zero metric-card elements together.
> Current UI/UX run status:
> the requested 10 implementation-slice goal is complete and paused,
> D143 post-SET02 UI/UX assessment is deferred unless the owner resumes the run,
> unless the owner changes priority."

That's it. No re-orient, no re-summary. The handoff files carry it.

## LS01 validation follow-up - 2026-05-17

Current LS01 state: implementation drafted, validation pending.

Recommended next checks before closing Q-042:

1. Run targeted domain/model/migration tests covering `CampaignLaunchSnapshot`, EF model mapping, migration script generation, and launch snapshot persistence.
2. Run a broader backend build or integration pass if targeted checks pass.
3. If validation passes, update `OPEN-QUESTIONS.md` to close or narrow Q-042 and move the queue to the next runtime slice.

## Next runtime slice - LS02 launch packet downstream provenance consumers

Start here after LS01:

1. Inspect existing report proof, export artifact, scoring proof, and wave comparison proof stores for launch snapshot reads.
2. Identify which outputs currently reconstruct provenance from mutable campaign/template/scoring/policy state.
3. Add packet-aware provenance reading with fallback to scalar launch snapshot fields for existing rows.
4. Add targeted regression coverage for post-launch setup mutation not changing packet-derived provenance.
5. Keep UX/UI deferred.

## After LS02 - candidate next runtime slices

LS02 report/export/proof packet provenance is implemented. Before closing the branch, run Docker-enabled report/export/wave tests if available.

Recommended next slice: **LS03 - Response export packet provenance parity**.

Scope:

- Add launch packet schema version and section names to campaign-series response export rows/codebook.
- Keep raw answers and existing item-level export behavior unchanged.
- Preserve sensitive-field exclusions: no tokens, salts, tenant IDs, recipient/provider identifiers, or participant-code hashes.
- Add regression coverage that response export provenance is packet-derived and remains stable after post-launch setup mutation.

## 2026-05-17 next session note

- Start from the completed five-pair launch-packet provenance pass: exports, report proof, wave proof, and product-surface operations/waves/reports now expose sanitized packet provenance.
- First verification step should be Docker-enabled integration execution for the tests that skipped locally: `ProductSurfaceReadStoreTests` and `Campaign_series_response_export_store_persists_item_csv_codebook_and_local_trajectory_ids`.
- Keep the next slice non-UX: assess legacy/backfill behavior for launch snapshots with no packet, then add regression coverage before implementation.

## 2026-05-17 next session note after D145/LS04

- Assessment cadence is now documented: every task gets a checkpoint, but D-series documents are reserved for direction/semantic/safety/backlog decisions.
- LS04 source provenance is implemented and non-Docker contracts/build pass. First next action is Docker-enabled execution of the skipped LS04 integration filters.
- Keep next work non-UX unless the owner explicitly resumes UI/UX.

## 2026-05-17 next session note after D146/LS05

- LS05 completed five inline safety hardening tasks under D146. Non-Docker contracts and build pass.
- First next action remains Docker-enabled verification for the skipped report-proof export, response export, and ProductSurfaceReadStore filters.
- If Docker verification is clean, the next non-UX assessment should consider invitation/reminder runtime hardening rather than further launch-packet provenance polishing.
## Post-D256 - 2026-05-18
- Completed D256 by extending the live product-spine withdrawal invalidation smoke so succeeded report PDF artifacts are included in the same deleted/non-downloadable/cleared-content assertion set as report CSV and response-export artifacts.
- Verification passed: focused D256 static test, full staging deployment package static suite, PowerShell parser validation, live product-spine smoke, and `dotnet build --no-restore`.
- Resume with D257 post-D256 product/runtime assessment. Keep the next task outside UX unless it directly supports withdrawal trust, reports/PDF/export value, deployment/operability, or notifications.
## Post-D258 - 2026-05-18
- D257 assessment found that anonymous withdrawal tokens, withdrawal execution, report PDF delivery/signed-url/invalidation, worker heartbeat/readiness, and staging email config were already covered enough for now. The next real gap was deployment operability: release checks were scattered across handoff memory.
- D258 added `deploy/staging/run-release-checks.ps1`, an owner-runnable staging release verification runner with optional live-smoke skipping.
- Verification passed: RED missing-runner static test, GREEN focused static test, full deployment package static suite 27/27, parser validation, release runner with `-SkipLiveSmoke`, and final `dotnet build --no-restore`.
- Resume with D259 post-D258 product/runtime assessment. Prefer release automation depth, notification/email recovery, report/export value, or withdrawal/legal-operability evidence before any standalone UX/UI work.
## Post-D260 - 2026-05-18
- D259 found that D258's release runner still missed the staging web app build gate.
- D260 added default `apps/web` `npm ci`, `npm run check`, and `npm run build` steps to `deploy/staging/run-release-checks.ps1`, with `-SkipWebBuild` for constrained environments.
- Debug note: initial `npm run check` failed because Windows `cmd.exe` lost `node` from the oversized ambient PATH. The runner now uses a short explicit Node/System32 PATH for web npm commands.
- Verification passed: RED missing-web-gate static test, GREEN focused static test, parser validation, full deployment package static suite 28/28, release runner with `-SkipLiveSmoke`, and final `dotnet build --no-restore`.
- Resume with D261 post-D260 product/runtime assessment. Consider the non-failing npm audit and Vite large-chunk warnings as evidence, but do not jump to UI/UX unless it directly supports the product lanes.
## Post-D262 - 2026-05-18
- D261 selected release/security hardening. D260 exposed high-severity production dependency audit risk; the Vite large-chunk warning remains non-failing performance debt.
- D262 added a default web production dependency audit gate to `deploy/staging/run-release-checks.ps1`: `npm audit --omit=dev --audit-level=high`, with `-SkipWebAudit` for constrained/offline environments.
- `npm audit fix` patched the web dependency graph so the high-severity production gate reports 0 vulnerabilities. Full npm audit still reports low-severity advisories requiring a forced breaking change according to npm.
- Verification passed: RED missing-audit-gate static test, GREEN focused static test, direct production audit, parser validation, full deployment package static suite 29/29, release runner with `-SkipLiveSmoke`, and final `dotnet build --no-restore`.
- Resume with D263 post-D262 product/runtime assessment. Consider bundle-budget enforcement next, but compare it against notification/email recovery, report/export value, and withdrawal/legal-operability evidence before selecting.
## Post-D264 - 2026-05-18
- D263 selected bundle-budget release hardening after confirming `npm run check:bundles` passes with route-aware bundle measurements despite Vite's generic large-chunk warning.
- D264 added `npm run check:bundles` to `deploy/staging/run-release-checks.ps1` after the web build, with `-SkipWebBundleCheck` for constrained environments.
- Verification passed: RED missing-bundle-gate static test, GREEN focused static test, direct bundle check, parser validation, full deployment package static suite 30/30, release runner with `-SkipLiveSmoke`, and final `dotnet build --no-restore`.
- Resume with D265 post-D264 product/runtime assessment. Prefer notification/email recovery, report/export value, withdrawal/legal-operability evidence, or deeper release automation before more web-gate work unless a concrete blocker appears.

## Post-D266 - 2026-05-18
- D265 found that further web-gate work was not the next highest-leverage gap after D264. Operational notifications are now durable in-app events, but outbound operational-notification email routing still lacks a product/legal recipient policy.
- D266 added Q-054 to `OPEN-QUESTIONS.md` and explicitly blocks operational-notification email routing, requester/admin email workflows, notification preference UI, SMTP-backed operational alerting, and any claim that operational events are emailed until the recipient policy is decided.
- Verification passed: `Select-String` found Q-054 plus the operational-notification email routing blocker; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D267 post-D266 product/runtime assessment. Do not build operational-notification email routing while Q-054 is open. Prefer withdrawal legal-operability evidence, report/PDF/export value, deployment/live-smoke depth, or in-app notification/admin workflow gaps before standalone UX.

## Post-D268 - 2026-05-18
- D267 found a deployment adoption gap: D258-D264 built a strong release runner, but the VPS staging runbook still did not make it the default operator gate.
- D268 added static coverage for runbook adoption and updated `docs/v2/40-ops/vps-staging-runbook.md` to document `deploy/staging/run-release-checks.ps1`, default live-smoke behavior, and constrained skip flags.
- Verification passed: RED missing-runner-reference static test, GREEN focused static test 1/1, full deployment package static suite 31/31, and final `dotnet build --no-restore` with 0 warnings and 0 errors.
- Resume with D269 post-D268 product/runtime assessment. Do not build operational-notification email routing while Q-054 is open. Prefer remote VPS rehearsal evidence, withdrawal legal-operability evidence, report/PDF/export value, or in-app notification/admin workflow gaps before standalone UX.

## Post-D270 - 2026-05-18
- D269 found that backup/restore proof was still manual-only even though the consolidated release runner had become the documented staging gate.
- D270 added `-SkipBackupRestoreSmoke` and default backup/restore smoke execution to `deploy/staging/run-release-checks.ps1` inside the live-smoke group, after the product-spine smoke.
- The VPS runbook now states that default staging release checks include local staging smoke, product-spine smoke, and backup/restore smoke, and documents when to use `-SkipBackupRestoreSmoke`.
- Verification passed: RED focused backup/restore static tests failed 2/2; GREEN focused tests passed 2/2; full deployment package static suite passed 33/33; `deploy/staging/run-release-checks.ps1 -SkipLiveSmoke` passed; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D271 post-D270 product/runtime assessment. Do not build outbound operational-notification email while Q-054 is open. Prefer live/remote release proof, withdrawal legal-operability evidence, report/PDF/export value, or in-app notification/admin workflow gaps before standalone UX.

## Post-D272 - 2026-05-18
- D271 selected full default release-runner proof after D270 added backup/restore smoke to the live gate.
- D272 initially failed at backup/restore and then fixed the root causes: split pg_dump shell arguments, PowerShell native stderr handling, missing restore-side `platform_worker` role, missing local env fallback for `POSTGRES_WORKER_USER`, and nested shell quoting around restore verification SQL.
- Final verification passed: direct backup/restore smoke created a non-empty dump and restored 53 public tables / 52 public relations; full default `deploy/staging/run-release-checks.ps1` passed with no skip flags; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D273 post-D272 product/runtime assessment. Treat the local staging release gate as live-proven. Do not build outbound operational-notification email while Q-054 is open. Prefer remote/VPS proof, withdrawal legal-operability evidence, report/PDF/export value, or in-app notification/admin workflow gaps before standalone UX.

## Post-D274 - 2026-05-18
- D273 found that after the local/default staging release gate was live-proven, the remaining deployment evidence gap was remote/VPS preflight integration.
- D274 added optional remote validation preflight support to `deploy/staging/run-release-checks.ps1` with `-RemoteValidationTenantSlug`, `-RemoteApiOrigin`, `-RemoteWebOrigin`, and `-SkipRemotePreflight`.
- The runner now invokes `deploy/staging/smoke-validation-demo-preflight.ps1 <tenant> -RemoteOnly -ApiOrigin <api> -WebOrigin <web>` when both remote origins are supplied, skips when none are supplied, and fails closed when only one origin is supplied.
- Verification passed: RED focused remote-preflight tests failed 2/2; GREEN focused tests passed 2/2; full deployment package static suite passed 36/36; `deploy/staging/run-release-checks.ps1 -SkipLiveSmoke -SkipRemotePreflight` passed; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D275 post-D274 product/runtime assessment. Remote preflight is integrated but not executed against a real target. Do not build outbound operational-notification email while Q-054 is open. Prefer target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, or in-app notification/admin workflow gaps before standalone UX.

## Post-D276 - 2026-05-18
- D275 selected in-app operational notification batch acknowledgement because remote preflight execution still needs owner origins and Q-054 blocks outbound operational-notification email routing.
- D276 added setup-authorized `POST /operational-notifications/mark-all-read`, safe aggregate response fields, MediatR command wiring, and tenant-scoped EF store behavior using entity `MarkRead` rather than EF bulk update/delete.
- Verification passed: RED focused tests failed on missing mark-all-read contract; GREEN focused API tests passed 2/2 with the Docker-backed store test initially skipped; explicit Docker-backed store test passed 1/1 with `RUN_POSTGRES_INTEGRATION_TESTS=1`; broader operational notification regression passed 30/30 with Docker enabled; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D277 post-D276 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D278 - 2026-05-18
- D277 selected owner-runnable live-smoke proof for D276's operational notification mark-all-read backend control.
- D278 extended the product-spine smoke with `Assert-OperationalNotificationMarkAllRead`, proving `POST /operational-notifications/mark-all-read` through the live API and safe aggregate response/summary-zero behavior.
- Verification passed: RED focused static test failed on missing mark-all-read smoke; GREEN focused static test passed; full staging deployment package static suite passed 37/37; PowerShell parser validation passed; live `deploy/staging/smoke-product-spine.ps1` passed after rebuilding/recreating local staging with dev auth enabled through process environment; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Debug note: the first live run reached the new route but got 404 because local staging was stale. Rebuild fixed the route but restarted API with dev auth disabled from local environment; recreating local staging with dev auth enabled through process environment restored the owner-runnable smoke posture.
- Resume with D279 post-D278 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D280 - 2026-05-18
- D279 selected smoke operability hardening from D278's dev-auth restart failure.
- D280 added `Invoke-AuthenticatedDevSession` helpers to both local staging smokes. They keep dev-header auth semantics, but 401/403 now produce explicit guidance that development authentication is disabled and local staging should be recreated with `Authentication__Dev__Enabled=true` and `PUBLIC_DEV_AUTH_ENABLED=true`.
- Verification passed: RED focused static test failed on missing helper/guidance; GREEN focused static test passed; full staging deployment package static suite passed 38/38; PowerShell parser validation passed for both smokes; live `deploy/staging/smoke-local-staging.ps1` passed; live `deploy/staging/smoke-product-spine.ps1` passed; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D281 post-D280 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D282 - 2026-05-18
- D281 selected full default release-runner regression proof because D276-D280 changed live-smoke behavior inside the consolidated release gate.
- D282 ran `deploy/staging/run-release-checks.ps1` with no skip flags. The runner passed build, deployment static tests, Node deployment package tests, web install/audit/check/build/bundle gates, local/VPS Compose config rendering, local staging smoke, product-spine smoke, backup/restore smoke, and skipped remote preflight explicitly because no origins were supplied.
- Final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and low-severity full npm audit advisories requiring a forced breaking change.
- Resume with D283 post-D282 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D284 - 2026-05-18
- D283 selected release-runner local staging refresh because earlier verification exposed stale local staging images and dev-auth-disabled recreation as owner-runnable release-gate failure modes.
- D284 added `-SkipLocalStagingRefresh` plus a default `Refresh local staging services` step before live smokes. The runner now rebuilds/recreates local `api`, `worker`, and `web` services with development auth forced only in the runner process, then restores prior process env values.
- Verification passed: RED focused static test failed on missing refresh/skip/dev-auth strings; GREEN focused static test passed; full staging deployment package static suite passed 39/39; runner parser validation passed; full default `deploy/staging/run-release-checks.ps1` passed including Docker image rebuild/recreate, local staging smoke, product-spine smoke, backup/restore smoke, and remote-preflight skip; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, full npm audit low-severity advisories requiring a forced breaking change, and Docker web runtime `npm ci --omit=dev` logs `svelte-kit: not found` during `prepare` but exits successfully because the package script already tolerates it.
- Resume with D285 post-D284 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D286 - 2026-05-18
- D285 selected release-signal cleanup because D284's full runner passed but the web runtime Docker image still emitted false `svelte-kit: not found` noise during production dependency install.
- D286 changed the web runtime Docker stage to `npm ci --omit=dev --ignore-scripts`, leaving the build-stage dependency install and SvelteKit build unchanged.
- Verification passed: RED focused Node deployment-package test failed on bare runtime `npm ci --omit=dev`; GREEN focused Node test passed; full Node deployment-package tests passed 25/25; full default `deploy/staging/run-release-checks.ps1` passed with refreshed Docker images, local staging smoke, product-spine smoke, backup/restore smoke, and remote-preflight skip because no origins were supplied; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and full npm audit low-severity advisories requiring a forced breaking change.
- Resume with D287 post-D286 product/runtime assessment. Push back toward product value now: compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export value, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D288 - 2026-05-18
- D287 selected report/PDF/export value after D286 cleaned up the release-runner signal. Remote target proof still needs owner origins, Q-054 blocks outbound operational-notification email work, and Q-053 blocks production legal/GDPR claims.
- D288 extended the product-spine smoke so a post-close campaign-series report PDF artifact is created after closed-wave finality is established, then fetched, delivery-checked, signed-url-checked, notification-checked, printed for owner inspection, and included in withdrawal invalidation when it succeeded.
- Verification passed: RED focused static test failed on the missing post-close PDF path; GREEN focused static test passed; full staging deployment package static suite passed 40/40; PowerShell parser validation passed; full default `deploy/staging/run-release-checks.ps1` passed including the live product-spine post-close report PDF artifact; final `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and full npm audit low-severity advisories requiring a forced breaking change.
- Resume with D289 post-D288 product/runtime assessment. Prefer product-value residuals over release cleanup unless a concrete gate failure appears. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D290 - 2026-05-18
- D289 selected withdrawal/report-export trust after D288 because anonymizing withdrawal intentionally keeps response/answer/score rows for aggregate analysis while invalidating old derived artifacts.
- D290 extended the product-spine smoke so after withdrawal execution and old artifact invalidation it creates fresh wave-1 report and campaign-series response exports, fetches the artifacts, verifies succeeded/downloadable/not-deleted state, reuses score-output metadata assertions, and checks the payloads do not expose raw withdrawal tokens, raw participant codes, `wdr_`, storage keys, connection-string markers, password markers, or secret markers.
- Verification passed: RED focused static test failed on the missing post-withdrawal fresh export path; GREEN focused static test passed; full staging deployment package static suite passed 41/41; PowerShell parser validation passed; full default `deploy/staging/run-release-checks.ps1` passed including the live post-withdrawal response export artifact proof.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and full npm audit low-severity advisories requiring a forced breaking change.
- Resume with D291 post-D290 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D292 - 2026-05-18
- D291 selected the remaining PDF-specific withdrawal/report gap after D290: fresh CSV exports were live-proven after anonymizing withdrawal, but fresh report PDF artifact generation after withdrawal was not.
- D292 extended the product-spine smoke so after withdrawal execution, old artifact invalidation, and fresh CSV export regeneration it creates a fresh post-withdrawal report PDF artifact, fetches it, verifies safe terminal metadata, reuses existing PDF delivery and signed-url assertions, waits for the terminal operational notification, checks forbidden markers are absent, and prints the artifact id.
- Verification passed: RED focused static test failed on the missing post-withdrawal PDF path; GREEN focused static test passed; full staging deployment package static suite passed 42/42; PowerShell parser validation passed; full default `deploy/staging/run-release-checks.ps1` passed including the live post-withdrawal report PDF artifact proof.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and full npm audit low-severity advisories requiring a forced breaking change.
- Resume with D293 post-D292 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D294 - 2026-05-18
- D293 selected tenant/admin withdrawal review visibility after D292 because create/approve/execute was live-proven but the owner-runnable product spine did not prove the operator list/get review path.
- D294 added `Assert-WithdrawalRequestReviewVisibility` to the product-spine smoke. The smoke now proves `GET /withdrawal-requests` and `GET /withdrawal-requests/{id}` show the request as `requested` after token consume and `completed` after approve/execute, with expected target/action/count metadata and safe response checks.
- Verification passed: RED focused static test failed on the missing review helper/path; GREEN focused static test passed; full staging deployment package static suite passed 43/43; PowerShell parser validation passed; full default `deploy/staging/run-release-checks.ps1` passed including the live withdrawal review visibility proof.
- Remaining non-failing signals: Vite large-chunk warning, two conditional lazy `echarts` warnings, and full npm audit low-severity advisories requiring a forced breaking change.
- Resume with D295 post-D294 product/runtime assessment. Compare target-environment proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, or standalone notification UI while Q-054 is open.

## Post-D296 - 2026-05-18
- D295 selected deployment/operability evidence quality after D294 because withdrawal/report/PDF/product-spine proof was strong locally, but the consolidated release runner still left pass/skip/limitation evidence mostly in console output.
- D296 added optional `-EvidencePath` to `deploy/staging/run-release-checks.ps1`. A passing run writes safe release evidence JSON with passed gates, skipped gates, remote-preflight configured booleans without raw origins, and Q-053/Q-054 limitations.
- The VPS staging runbook documents `-EvidencePath` for owner/demo handoff and explicitly says the artifact is engineering evidence only, not production legal/GDPR/DPA approval, email-notification policy, SLA claim, or remote VPS proof unless remote preflight actually runs against owner-supplied origins.
- Verification passed: RED focused release-evidence static tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 45/45; constrained runner evidence proof passed with 2 passed gates and 5 skipped gates; full default runner with `-EvidencePath` passed and parsed evidence with 14 passed gates and 1 remote-preflight skip.
- Resume with D297 post-D296 product/runtime assessment. Compare target-environment remote proof, withdrawal legal-operability evidence, report/PDF/export residuals, remaining non-email notification/admin gaps, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D298 - 2026-05-18
- D297 selected product-spine structured evidence after D296 because the release gate became auditable, but the actual product-spine milestones were still console-only.
- D298 added optional `-EvidencePath` to `deploy/staging/smoke-product-spine.ps1` and wired the release runner to pass a derived `.product-spine.json` sidecar when release evidence is requested.
- The product-spine sidecar records owner inspection routes, product milestones, artifact proof summaries, withdrawal proof summary, and Q-053/Q-054 limitations. It omits raw withdrawal tokens, participant codes, answers, storage keys, credential values, connection strings, and raw remote origins.
- Verification passed: RED focused product-spine evidence tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 47/47; constrained runner proof passed; full default runner with `-EvidencePath` passed and parsed both release and product-spine artifacts, proving 14 release gates plus 5 owner routes. Initial full proof caught forbidden-marker wording in limitations and it was corrected.
- Resume with D299 post-D298 product/runtime assessment. Compare target-environment remote proof, withdrawal legal-operability evidence, report/PDF/export residuals, remaining non-email notification/admin gaps, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D300 - 2026-05-18
- D299 selected an offline release-evidence verifier because D298 made the evidence bundle durable, but saved evidence validation still lived only in the terminal parser used during verification.
- D300 added `deploy/staging/verify-release-evidence.ps1`. It validates release evidence and optional product-spine sidecar evidence without rerunning Docker, web build, smokes, or backup/restore.
- The verifier checks schema/version/status/runner, passed/skipped gates, Q-053/Q-054 limitations, remote-preflight expectations, owner routes, artifact proofs, withdrawal proof, product milestones, and forbidden markers.
- Verification passed: RED focused verifier tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 49/49; constrained evidence plus verifier passed; full default release runner plus verifier `-RequireProductSpineEvidence` passed with 14 gates, 1 skip, and validated product-spine sidecar.
- Resume with D301 post-D300 product/runtime assessment. Compare target-environment remote proof, withdrawal legal-operability evidence, report/PDF/export residuals, remaining non-email notification/admin gaps, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D302 - 2026-05-18
- D301 selected release-runner evidence self-verification because D300 created the verifier, but the release runner still did not fail automatically when its generated evidence bundle was incomplete or unsafe.
- D302 wired `run-release-checks.ps1 -EvidencePath <path>` to invoke `verify-release-evidence.ps1` after writing evidence and before reporting success.
- The runner now requires product-spine sidecar evidence when live smokes ran, requires remote-preflight proof only when remote origins were supplied and remote preflight was not skipped, and keeps constrained/skipped-live evidence valid without requiring the sidecar.
- Verification passed: RED focused tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 51/51; constrained runner self-verification passed with 2 gates and 5 skips; full default runner self-verification passed with 14 gates, 1 skip, and validated product-spine sidecar.
- Resume with D303 post-D302 product/runtime assessment. Compare target-environment remote proof, withdrawal legal-operability evidence, report/PDF/export residuals, remaining non-email notification/admin gaps, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D304 - 2026-05-18
- D303 selected backup/restore structured evidence because release and product-spine evidence were already durable and self-verifying, while backup/restore proof still depended on console output.
- D304 added optional `-EvidencePath` to `deploy/staging/backup-restore-smoke.ps1`, producing a safe `.backup-restore.json` sidecar only after successful proof. The sidecar records schema/version, runner/status, backup bytes, restore table/relation counts, required platform table proof, skip/cleanup flags, and Q-053 limitations without database names, users, passwords, connection strings, env values, credential values, or host paths.
- The release runner now derives `backupRestoreEvidencePath`, passes it to the backup/restore smoke, includes it in release evidence, and requires backup/restore evidence during self-verification when the live backup/restore smoke ran. The offline verifier now supports `-RequireBackupRestoreEvidence`.
- Verification passed: RED focused backup/restore evidence tests failed 4/4; GREEN focused tests passed 4/4; full staging deployment package static suite passed 55/55; constrained release runner self-verified with 2 gates and 5 skips; full default release runner self-verified with 14 gates, 1 skip, product-spine sidecar validated, and backup/restore sidecar validated with backup bytes 1602041, 53 restored public tables, and 52 restored public relations.
- Resume with D305 post-D304 product/runtime assessment. Compare target-environment remote proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email, requester/admin email workflows, notification preferences, real-person legal/GDPR claims, or standalone UX while Q-053 and Q-054 remain open.

## Post-D305 - 2026-05-18
- D305 selected remote preflight structured evidence because local release, product-spine, and backup/restore evidence are now durable and self-verifying, while remote preflight details remain console-only when owner origins are supplied.
- D306 should add optional `-EvidencePath` to `deploy/staging/smoke-validation-demo-preflight.ps1`, write safe `.remote-preflight.json` evidence only after successful remote preflight, wire the release runner to derive/pass `remotePreflightEvidencePath`, and extend the verifier/runbook so `-RequireRemotePreflight` validates the sidecar.
- Do not claim actual remote proof without owner-supplied origins. Do not build deployment automation, outbound operational-notification email, requester/admin email workflows, notification preferences, real-person legal/GDPR claims, or standalone UX while Q-053 and Q-054 remain open.
- Verification target for D306: RED focused static tests fail on missing remote-preflight evidence; GREEN focused tests pass; full `StagingWorkerDeploymentPackageTests` passes; constrained release runner proof still passes when remote preflight is skipped; safe remote evidence proof only if an appropriate target is available; `dotnet build --no-restore` passes.

## Post-D306 - 2026-05-18
- D306 added structured remote-preflight evidence support. `smoke-validation-demo-preflight.ps1 -EvidencePath` writes `.remote-preflight.json` after successful preflight; `run-release-checks.ps1 -EvidencePath` derives/passes `remotePreflightEvidencePath`; `verify-release-evidence.ps1 -RequireRemotePreflight` validates the sidecar when remote origins were supplied.
- Verification passed: RED focused remote-preflight evidence tests failed 4/4; GREEN focused tests passed 4/4; full staging deployment package static suite passed 59/59; PowerShell parser validation passed for touched scripts; constrained release runner self-verified with 2 gates and 5 skips; remote-preflight writer proof passed with `-SkipLiveChecks`; verifier fixture passed with `-RequireRemotePreflight`; Node deployment-package tests passed 25/25; full default release runner passed with 14 gates, 1 skip, product-spine sidecar validated, backup/restore sidecar validated, and no remote sidecar required because remote origins were not supplied.
- A safe local-origin RemoteOnly live proof was attempted but failed at `/auth/login` because local dev-auth staging is not an Auth0-style remote target. Do not claim real remote VPS proof from D306.
- Resume with D307 post-D306 product/runtime assessment. Compare actual owner-run remote target proof, withdrawal legal-operability evidence, report/PDF/export residuals, and remaining non-email notification/admin controls. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D307 - 2026-05-18
- D307 selected product-spine operational notification admin evidence detail. Actual remote proof is still owner-origin blocked; Q-053 blocks real-person legal/GDPR claims; Q-054 blocks outbound operational-notification email.
- D308 should make existing in-app notification admin proof first-class in the `.product-spine.json` sidecar: terminal notification, summary, mark-read decrement, mark-all-read clearing, in-app-only status, and explicit non-proof of email routing.
- Verification target for D308: RED focused static tests fail on missing notification admin evidence fields; GREEN focused tests pass; full `StagingWorkerDeploymentPackageTests` passes; full default `run-release-checks.ps1 -EvidencePath <temp>` validates the product-spine sidecar with `operationalNotificationProof`; `dotnet build --no-restore` passes.

## Post-D308 - 2026-05-18
- D308 made in-app operational notification admin controls first-class in product-spine structured evidence. `.product-spine.json` now includes `operationalNotificationProof` with terminal notification, summary, mark-read, mark-all-read, in-app-only, explicit non-email-routing state, and safe aggregate counts.
- Verification passed: RED focused operational-notification admin evidence tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 61/61; PowerShell parser validation passed for touched scripts; full default `run-release-checks.ps1 -EvidencePath <temp>` passed with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, and product-spine evidence proving summary=True, markRead=True, markAllRead=True, inAppOnly=True, emailRouting=False, unreadAfterMarkAllRead=0.
- Resume with D309 post-D308 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, and remaining non-email notification/admin gaps. Do not build outbound operational-notification email while Q-054 is open.

## Post-D309 - 2026-05-18
- D309 selected product-spine report/export evidence detail. Remote target proof still needs owner origins; Q-053 blocks real-person legal/GDPR claims; Q-054 blocks outbound operational-notification email. The next unblocked gap is structured evidence for report/export/PDF value already proven by the product-spine smoke.
- D310 should add safe `reportExportProof` fields to `.product-spine.json` and extend `verify-release-evidence.ps1` to require them: score metadata, report export codebook metadata, response export score metadata, wave comparison metadata, PDF delivery check, signed-download check, post-withdrawal export regeneration, post-withdrawal PDF regeneration, and artifact leak checks.
- Verification target for D310: RED focused static tests fail on missing report/export evidence fields; GREEN focused tests pass; full `StagingWorkerDeploymentPackageTests` passes; full default `run-release-checks.ps1 -EvidencePath <temp>` validates the product-spine sidecar with `reportExportProof`; `dotnet build --no-restore` passes.

## Post-D310 - 2026-05-18
- D310 made report/export/PDF value proof first-class in product-spine structured evidence. `.product-spine.json` now includes `reportExportProof` with score metadata, report export codebook metadata, response export score metadata, wave comparison score metadata, PDF delivery check, signed-download check, post-close PDF proof, post-withdrawal export regeneration, post-withdrawal PDF regeneration, and artifact leak checks.
- Verification passed: RED focused report/export evidence tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 63/63; PowerShell parser validation passed for touched scripts; full default `run-release-checks.ps1 -EvidencePath <temp>` passed with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, and report/export evidence booleans all true.
- Resume with D311 post-D310 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, and remaining non-email notification/admin controls before standalone UX.

## Post-D312 - 2026-05-18
- D311 selected release evidence proof-scope/claim-boundary metadata because D310 made product value proof explicit, but saved release evidence still needed stronger guardrails against confusing local/default engineering proof with remote VPS proof, legal/GDPR approval, or outbound operational-notification email readiness.
- D312 added top-level `proofScope` and `claimBoundary` to release evidence. The verifier now requires these fields, ties local/default staging, product-spine, backup/restore, and remote-preflight booleans to actual passed gates, and rejects evidence that implies remote VPS deployment proof, real-person legal/GDPR approval, or outbound operational-notification email proof.
- Verification passed: RED focused proof-scope/claim-boundary tests failed 3/3; GREEN focused tests passed 3/3; full staging deployment package static suite passed 66/66; PowerShell parser validation passed; constrained release runner self-verified with 2 gates and 5 skips and all proof booleans false; full default release runner self-verified with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, and proofScope proving local/product/backup true while remote/remoteVps/legal/operationalEmail remained false.
- Resume with D313 post-D312 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D314 - 2026-05-18
- D313 selected evidence sidecar integrity because D312 made proof scope and claim boundaries explicit, but the top-level release evidence still did not bind sidecar files by digest.
- D314 added top-level `evidenceArtifacts` to release evidence. It records product-spine, backup/restore, and remote-preflight sidecar paths, presence, and SHA-256 hashes when present. The verifier now recomputes hashes before validating sidecar contents and fails closed on mismatch while keeping skipped optional sidecars valid when absent.
- Verification passed: RED focused sidecar-integrity tests failed 3/3; GREEN focused tests passed 3/3; full staging deployment package static suite passed 69/69; PowerShell parser validation passed; constrained release runner self-verified with 2 gates and 5 skips and empty hashes for absent optional sidecars; full default release runner self-verified with 14 gates, 1 remote-preflight skip, product-spine hash length 64, backup/restore hash length 64, product-spine sidecar validated, and backup/restore sidecar validated.
- Resume with D315 post-D314 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D316 - 2026-05-18
- D315 selected backup/restore dump digest evidence because D314 protected evidence sidecar files with hashes, but the backup/restore sidecar itself still proved backup non-emptiness by byte size only.
- D316 added `backup.backupSha256` to backup/restore sidecar evidence and extended the verifier to require a 64-character lowercase hex digest.
- Verification passed: RED focused backup-SHA tests failed 3/3; GREEN focused tests passed 3/3; full staging deployment package static suite passed 72/72; PowerShell parser validation passed; full default release runner self-verified with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, backup bytes 1838211, backupSha256 length 64, and backup/restore sidecar hash length 64.
- Resume with D317 post-D316 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D318 - 2026-05-18
- D317 selected release evidence verifier sidecar-hash validation summary because D314/D316 made integrity checks real, but the verifier success line did not expose which hash checks actually ran.
- D318 updated `verify-release-evidence.ps1` so sidecar hash validation returns booleans and the final success output reports product-spine, backup/restore, and remote-preflight hash-validation status.
- Verification passed: RED focused hash-validation-summary tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 74/74; PowerShell parser validation passed; constrained release runner self-verified with all three hash validations false; full default release runner self-verified with product-spine and backup/restore hash validations true and remote-preflight hash validation false.
- Resume with D319 post-D318 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Q-053 and Q-054 remain blockers for their stated areas.

## Post-D320 - 2026-05-18
- D319 selected campaign email delivery evidence detail because successful local-dev campaign invitation delivery was live-smoke proven, but the product-spine sidecar only carried a broad milestone boolean.
- D320 added safe `campaignEmailDeliveryProof` to `.product-spine.json`, recording invitation batch proof, delivery processing proof, local-dev provider proof, aggregate created/processed/sent/failed counts, and explicit non-proof of SMTP delivery or failed-delivery requeue recovery.
- Verification passed: RED focused campaign-email-delivery-evidence tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 76/76; PowerShell parser validation passed; full default release runner self-verified with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, campaign provider local-dev, created/processed/sent 1, failed 0, SMTP proof false, and failed-requeue recovery proof false.
- Resume with D321 post-D320 product/runtime assessment. Compare failed-delivery recovery proof, actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes.

## Post-D322 - 2026-05-18
- D321 selected campaign failed-delivery requeue no-op proof because the failed-requeue endpoint existed, but product-spine evidence did not prove the endpoint was owner-runnable or safely bounded after a clean local-dev delivery.
- D322 extended the product-spine smoke to call `POST /campaigns/{id}/notification-deliveries/requeue-failed` after successful local-dev invitation delivery, require a safe zero-requeue response, and record `failedRequeueNoopProven = true` plus `failedRequeueNoopRequeuedCount = 0` in `campaignEmailDeliveryProof`. The verifier now requires those fields while keeping `failedRequeueRecoveryProven = false`.
- Verification passed: RED focused campaign-email-requeue-noop tests failed 2/2; GREEN focused tests passed 2/2; full staging deployment package static suite passed 78/78; PowerShell parser validation passed; full default release runner self-verified with 14 gates, 1 remote-preflight skip, product-spine sidecar validated, backup/restore sidecar validated, product-spine and backup/restore hashes validated, campaign provider local-dev, created/processed/sent 1, failed 0, no-op proof true, no-op count 0, and failed-requeue recovery proof false.
- Resume with D323 post-D322 product/runtime assessment. Compare actual failed-delivery recovery proof, actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim SMTP delivery, recovery from a real failed delivery, remote VPS proof, real-person legal/GDPR readiness, or outbound operational-notification email while the evidence and owner decisions remain absent.

## Post-D324 - 2026-05-18
- D323 selected campaign email failed-delivery recovery release-evidence gating because D322 proved only a safe no-op in the live product-spine smoke, while the real backend fail/requeue/retry-success regression was still hidden in integration tests and absent from owner-runnable release evidence.
- D324 added `-SkipEmailRecoveryRegression` to `deploy/staging/run-release-checks.ps1` and made the default runner execute the existing Docker-backed `Email_delivery_requeues_failed_invitations_for_retry_without_retrying_withdrawal_scrubbed` regression with `RUN_POSTGRES_INTEGRATION_TESTS=1`.
- Release evidence now includes `proofScope.campaignFailedDeliveryRecoveryRegressionProven`; the offline verifier ties that boolean to the actual passed gate and reports it in the success line. The VPS staging runbook documents that this is backend synthetic regression proof, not SMTP live delivery proof, not live product-spine failed-recovery proof, and not operational-notification email proof.
- Verification passed: RED focused static tests failed 4/5; GREEN focused static tests passed 5/5; targeted Docker-backed integration regression passed 1/1; PowerShell parser validation passed; full staging deployment package static suite passed 80/80; constrained release runner with `-SkipLiveSmoke -EvidencePath <temp>` self-verified with 11 passed gates, 2 skipped gates, campaign recovery proof true, and local/product/backup/remote proof false.
- Resume with D325 post-D324 product/runtime assessment. Compare live product-spine failed-recovery feasibility, actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim SMTP delivery, live failed-delivery recovery in product-spine smoke, remote VPS proof, real-person legal/GDPR readiness, or outbound operational-notification email without direct evidence and owner decisions.

## Post-D326 - 2026-05-18
- D325 selected full default release evidence proof because D324 changed the default release runner, but only constrained `-SkipLiveSmoke` release evidence had been run after that change.
- D326 ran `deploy/staging/run-release-checks.ps1 -EvidencePath <temp>` with no skip flags. The evidence self-verifier passed with 15 passed gates and 1 remote-preflight skip.
- The full evidence now proves campaign failed-delivery recovery regression, local/default staging, product-spine, backup/restore, product-spine sidecar validation, backup/restore sidecar validation, product-spine hash validation, and backup/restore hash validation together. Remote preflight remains false/skipped because no owner origins were supplied.
- Parsed proof summary: campaignRecovery=True, local=True, product=True, backup=True, remote=False, productSidecar=True, backupSidecar=True, productHashLen=64, backupHashLen=64, campaignProvider=local-dev, created=1, sent=1, noop=True, liveRecovery=False, passed=15, skipped=1.
- Resume with D327 post-D326 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim SMTP delivery, live failed-delivery recovery in product-spine smoke, remote VPS proof, real-person legal/GDPR readiness, or outbound operational-notification email without direct evidence and owner decisions.

## Post-D328 - 2026-05-18
- D327 selected retention due-batch automation release-evidence gating because the retention automation stack was implemented and regression-tested through RET14/RET15/QA06, but the consolidated release evidence did not yet run or record that proof.
- D328 added `-SkipRetentionAutomationRegression` to `deploy/staging/run-release-checks.ps1` and made the default runner execute the existing Docker-backed `Due_batch_automation` regression filter with `RUN_POSTGRES_INTEGRATION_TESTS=1`.
- Release evidence now includes `proofScope.retentionDueBatchAutomationRegressionProven`; the offline verifier ties that boolean to the actual passed gate and reports it in the success line. The VPS staging runbook documents that this is backend synthetic retention enforcement proof, not retention automation worker enablement, remote VPS proof, real-person legal/GDPR approval, or operational-notification email proof.
- Verification passed: RED focused static tests failed 4/5; GREEN focused static tests passed 5/5; targeted Docker-backed `Due_batch_automation` regression passed 2/2; PowerShell parser validation passed; full staging deployment package static suite passed 82/82; constrained release runner with `-SkipLiveSmoke -EvidencePath <temp>` self-verified with 12 passed gates, 2 skipped gates, campaign recovery proof true, retention automation proof true, and local/product/backup/remote proof false.
- Resume with D329 post-D328 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim remote VPS proof, real-person legal/GDPR readiness, outbound operational-notification email, or live retention worker enablement without direct evidence and owner decisions.

## Post-D330 - 2026-05-18
- D329 selected full default release evidence after the retention automation gate because D328 changed the default release runner and only constrained proof had run after that change.
- D330 ran `deploy/staging/run-release-checks.ps1 -EvidencePath <temp>` with no skip flags. The evidence self-verifier passed with 16 passed gates and 1 remote-preflight skip.
- The full evidence now proves campaign failed-delivery recovery regression, retention due-batch automation regression, local/default staging, product-spine, backup/restore, product-spine sidecar validation, backup/restore sidecar validation, product-spine hash validation, and backup/restore hash validation together. Remote preflight remains false/skipped because no owner origins were supplied.
- Parsed proof summary: campaignRecovery=True, retentionAutomation=True, local=True, product=True, backup=True, remote=False, productSidecar=True, backupSidecar=True, productHashLen=64, backupHashLen=64, campaignProvider=local-dev, created=1, sent=1, noop=True, liveRecovery=False, passed=16, skipped=1.
- Resume with D331 post-D330 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim SMTP live delivery, live product-spine failed-recovery, live retention worker enablement, remote VPS proof, real-person legal/GDPR readiness, or outbound operational-notification email without direct evidence and owner decisions.

## Post-D332 - 2026-05-18
- D331 selected release-evidence verifier negative gate-proof tests because D330 made the release evidence bundle strong, but the campaign/retention proof-boundary tests were still mostly static string assertions.
- D332 added executable negative tests that generate minimal safe release-evidence fixtures, forge campaign recovery or retention automation proof booleans without the corresponding passed gate, invoke `deploy/staging/verify-release-evidence.ps1`, and require non-zero verifier exit with the forged proof-scope field in output.
- The verifier behavior was already correct; the task converted that boundary into durable regression tests. No production scripts, SMTP behavior, remote proof, live retention worker enablement, legal/GDPR claim, operational email, or UX/UI changed.
- Verification passed: initial focused negative tests failed 2/2 on brittle full-message assertions while the verifier rejected forged evidence; GREEN focused negative tests passed 2/2; full staging deployment package static suite passed 84/84; `dotnet build --no-restore` passed with 0 warnings and 0 errors.
- Resume with D333 post-D332 product/runtime assessment. Compare actual owner-run remote target proof, Q-053 legal-operability, report/PDF/export residuals, remaining non-email notification/admin controls, and UX only where it exposes proven product lanes. Do not claim SMTP live delivery, live product-spine failed-recovery, live retention worker enablement, remote VPS proof, real-person legal/GDPR readiness, or outbound operational-notification email without direct evidence and owner decisions.

## Post-D334 next-session plan

Start from D335. Current staging proof covers public health/web/auth/CORS/login redirect. If authenticated remote proof is needed, use `deploy/staging/smoke-remote-staging.ps1 -SessionCookie '<browser cookie header>'` and avoid printing or committing the cookie. Otherwise continue the product course with the next assessment/task pair in reports/PDF/export, withdrawal trust, or notification readiness depending on the highest current gap.

## Post-D335 next-session plan

Start from D336. Current best options: (1) authenticated remote staging smoke ergonomics if owner wants staging confidence, (2) withdrawal trust/admin workflow if compliance/product trust is the limiting product lane, or (3) another reports/export assessment only if it identifies a concrete runtime/API gap beyond the export library action readiness fixed in D335.

## Post-D336 next-session plan

Start from D337. Reassess whether withdrawal admin action-readiness needs product-spine/release-evidence coverage, or whether the highest value is now authenticated remote staging smoke with a safe human-supplied cookie flow. Keep Q-053 and Q-054 boundaries visible: no real-person legal/GDPR claim and no outbound operational-notification email workflow.

## Post-D337 next-session plan

Start from D338. Either run authenticated remote smoke with `deploy/staging/smoke-remote-staging.ps1 -RequireAuthenticatedSession -SessionCookiePath <ignored-cookie-file>` after the owner supplies a current browser cookie, or continue VPS hardening with target backup/restore rehearsal. Do not print cookies, commit cookie files, or treat owner-controlled staging proof as Q-053/Q-054 readiness.

## Post-D338 next-session plan

Start from D339. The VPS target backup/restore rehearsal is proven through `deploy/staging/backup-restore-vps-smoke.sh` and safe evidence exists at `/tmp/d338-vps-backup-restore.json` on the VPS. Recommended next slice: assess whether to wire VPS backup/restore and public remote smoke into a single owner-runnable VPS release-evidence command, prove rollback/redeploy, run authenticated remote smoke with an owner-supplied browser cookie, or tighten safe ops evidence capture. Keep Q-053 and Q-054 boundaries explicit and avoid standalone UX.

## Post-D339 next-session plan

Start from D340. The VPS release-evidence command is `bash deploy/staging/run-vps-release-checks.sh --evidence-dir <path>` from `/opt/instruments-platform`; the latest temporary proof wrote evidence under `/tmp/d339-vps-release-evidence`. Recommended next slice: rollback/redeploy proof, probably a safe command/script that records current revision, redeploys or restarts the stack, runs VPS release checks, and proves recovery evidence without touching secrets. Authenticated remote proof still needs an owner-supplied browser cookie. Keep Q-053/Q-054 false in evidence.

## Post-D340 next-session plan

Start from D341. Redeploy proof now exists through `bash deploy/staging/redeploy-vps-stack.sh --evidence-dir <path>`; the latest temporary proof wrote evidence under `/tmp/d340-vps-redeploy-evidence`. Recommended next slice: decide whether to implement actual rollback proof by selecting a previous known-good revision and round-tripping back to the current revision, or perform a completion audit and leave rollback as not achieved until an owner-approved rollback target is selected. Authenticated remote proof still needs an owner-supplied browser cookie.

## Post-D341 next-session plan

Start from D342. Rollback proof now exists through `bash deploy/staging/rollback-vps-stack.sh --evidence-dir <path>`; the latest temporary proof wrote evidence under `/tmp/d341-vps-rollback-evidence`, rolled back from `7429093` to `2500fdf`, restored `7429093`, and left the VPS checkout on `staging...origin/staging`. Recommended next slice: completion audit against the active VPS hardening goal. Do not mark the goal complete unless authenticated remote proof is actually run with an owner-supplied browser cookie, or the owner explicitly descopes that proof.

## 2026-05-19 - Next session plan after D348

Recommended start:

1. Check `git status --short --branch` and confirm the D348 auth/team changes are still the active slice.
2. Restore web dependencies if the owner approves dependency install, then run `npm run check` from `apps/web`.
3. Run or owner-drive the manual beta path: create workspace -> add pending member -> open first-sign-in link -> sign in as invited email -> confirm active roster/session.
4. If verified, commit and deploy the D348 slice; if frontend validation fails, fix only the auth/team files touched by D348.

Known validation gap: local `npm run check` failed because `svelte-kit` was not installed in `apps/web/node_modules`.

Deploy note:
- Staging currently runs commit `8997637` after D348.
- Start next session by owner-testing `/register` and `/app/team` on staging before adding more auth/team scope.
