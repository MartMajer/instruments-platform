# Validation Demo Walkthrough Packet

Status: internal owner/operator packet for VPS-current validation demos.
Decision source: [D356](assessments/D356-POST-VAL08-VALIDATION-PACKET-REFRESH-ASSESSMENT-2026-05-20.md).
Last updated: 2026-05-20.

## Purpose

Use this packet when preparing or running private validator demos with the three
proof-only validation tenants or an owner-created private-beta workspace. It
turns the current registration/sign-in path, Auth0-backed membership bootstrap,
study setup, audience preparation, collection, Results, Waves, Team, Directory,
Settings, and VPS staging proof into a repeatable owner-led walkthrough.

The validation tenants are ordinary tenant records in the normal app path, not
a separate `/app/demo` fixture surface. "Demo" describes the proof-only data
and owner-controlled test identities, not a special product mode.

This packet is not public marketing copy, not final UX guidance, and not a
production launch runbook. It assumes the operator has already followed the
fixture setup in the
[validation demo fixtures README](../../../deploy/staging/validation-demo-fixtures/README.md)
and the source boundaries in
[Validation Demo Tenants](validation-demo-tenants.md).

For the current live proof boundary and validation-call framing, also read the
[current proof demo brief](../50-business/current-proof-demo-brief.md).
For the short owner-run rehearsal checklist before O01/O02/O03, use
[Private Beta Validation Rehearsal](private-beta-validation-rehearsal.md).

## Hard Boundary

Everything here is proof/demo only:

- Data must be synthetic, seed-generated, or owner-controlled test data.
- Do not invite real participants, students, employees, patients, or customers.
- Do not claim final GDPR compliance, DPA readiness, privacy notice readiness,
  SLA readiness, legal compliance, clinical advice, or statutory OSH advice.
- Do not claim platform-shipped canonical named instruments or official score
  labels.
- Do not paste real Auth0 identities, provider subjects, domains, client IDs,
  secrets, tokens, invitation links, participant codes, answers, screenshots
  with secrets, or customer data into committed docs.

Q-053 remains the blocker for real-person data and production legal/GDPR
claims. Use the
[Q-053 counsel intake packet](../40-ops/q053-counsel-ready-legal-packet.md)
only when a concrete real-person pilot appears.

Q-054 remains the blocker for outbound operational-notification email routing,
requester/admin email workflows, notification preferences, SMTP-backed
operational alerting, and claims that operational events are emailed.

## Tenant Selection Matrix

Only one validation tenant is selected by the web app at a time through
`PUBLIC_TENANT_ID`. A user whose platform membership belongs to another tenant
should fail cleanly for the currently selected tenant.

| Tenant | `PUBLIC_TENANT_ID` | Primary validator | Use when the call cares about |
| --- | --- | --- | --- |
| `validation-oh-research` | `33333333-3333-4333-8333-333333333333` | Occupational-health professor or research validator | Burnout-style research, psychosocial work environment, ergonomics/MSK, anonymous longitudinal tracking, CSV/codebook exports. |
| `validation-se-education` | `44444444-4444-4444-8444-444444444444` | University/software-engineering education validator | Student surveys, programming-course pulse, AI-assisted learning/assessment, gamification pre/post research, course-study exports. |
| `validation-osh-consulting` | `55555555-5555-4555-8555-555555555555` | Workplace-safety or OSH consultant validator | Safety training feedback, safety climate, ergonomics checks, near-miss learning, client portfolio proof. |

## Smoke User Rule

Use role slots, not real email values, in notes and screenshots:

| Role slot | Use in walkthrough | Current limitation |
| --- | --- | --- |
| `tenant_owner` | Primary operator user for tenant setup, launch, and full walkthrough. | Demo role only, not final production owner UX. |
| `researcher` | Primary alternative user for research workflows and campaign/report/export walkthrough. | Demo role only, not final production researcher UX. |
| `analyst` | Read-only product reviewer for covered portfolio, team roster, setup, operations, reports, and waves surfaces. | RBAC01 blocks current setup-management actions, and TEAM01 shows current role/permission mapping, but this is not a final analyst export/permission taxonomy. |
| `viewer` | Read-only demo reviewer for covered product surfaces and current tenant roster. | RBAC01 blocks current setup-management actions, and TEAM01 shows current role/permission mapping, but this is not a final viewer/user-management UX. |

The ignored local file
`deploy/staging/validation-demo-fixtures/validation-demo-auth-users.local.json`
is the only place where owner-controlled Auth0 test identities should live.
Committed docs should refer to role slots or placeholder values only.

## Live VPS Rehearsal Proof

Use the live VPS staging lane when the owner needs to show that the workflow is
not only a local proof. The current public origins are:

```text
Web: https://validatedscale-staging.croat.dev
API: https://validatedscale-api-staging.croat.dev
```

Before a validator call, capture fresh engineering evidence if the owner browser
session is still valid:

```bash
ssh instruments-vps-codex
cd /opt/instruments-platform
bash deploy/staging/run-vps-release-checks.sh \
  --evidence-dir /tmp/validation-vps-release-evidence \
  --session-cookie-file /tmp/staging-session.cookie \
  --require-authenticated-session
```

For the broad product-spine proof, run from an operator machine that can execute
PowerShell. Read the Cookie header from an ignored owner-controlled file or
`STAGING_SESSION_COOKIE`; do not paste cookie values into chat or committed
docs:

```powershell
.\deploy\staging\smoke-product-spine.ps1 `
  -ApiBaseUrl https://validatedscale-api-staging.croat.dev `
  -WebBaseUrl https://validatedscale-staging.croat.dev `
  -TenantId <staging-tenant-id> `
  -SessionCookiePath <ignored-cookie-file> `
  -RequireAuthenticatedSession `
  -EvidencePath .\artifacts\authenticated-vps-product-spine.json
```

The release evidence proves remote public smoke, authenticated `/auth/session`,
target backup/restore, and safe Q-053/Q-054 boundaries. The product-spine
evidence proves Auth0-backed owner session, `setup.manage`, CSRF, setup/launch,
respondent flow, scoring, reports/waves, CSV/codebook/PDF artifacts,
withdrawal lifecycle, in-app operational notifications, and campaign invitation
delivery proof with SMTP/outbound-operational-email boundaries.

The current product walkthrough can also be run manually through the browser:
register or sign in, create/open a workspace, create a study, complete the
five-step setup workflow, build a questionnaire, define multiple Results
outputs, import or prepare an audience, choose recipients, launch/collect,
review Results, download CSV, compare Waves, and inspect Team/Directory/Settings.
Use [Private Beta Validation Rehearsal](private-beta-validation-rehearsal.md)
for the timed pre-call version of that route.

This is the preferred current engineering proof for O01/O02/O03 conversations
when the browser session and staging stack are healthy. Use local/dev fallback
when the VPS session is expired, network access is unreliable, or the call does
not need live staging evidence.

## Local Auth0 Preflight

Before a serious walkthrough:

1. Confirm Auth0 application URLs match the local API and web origins in use.
   Local proof commonly uses API `http://127.0.0.1:5055` and web
   `http://127.0.0.1:5174`.
2. Confirm the ignored local auth-user file exists and contains owner-controlled
   test identities for the tenant you will show.
3. Confirm platform memberships and seeded content have been prepared:

```powershell
deploy/staging/bootstrap-validation-demo-auth.ps1 -ValidateOnly
deploy/staging/bootstrap-validation-demo-auth.ps1
deploy/staging/seed-validation-demo.ps1
```

4. Confirm `deploy/staging/.env` selects the tenant and approved web origin.
   For local manual smoke, the relevant values should have this shape:

```text
PUBLIC_TENANT_ID=<selected-validation-tenant-id>
PUBLIC_API_BASE_URL=http://127.0.0.1:5055
Cors__AllowedOrigins__0=http://127.0.0.1:5174
PUBLIC_AUTH_LOGIN_URL=http://127.0.0.1:5055/auth/login?returnUrl=http%3A%2F%2F127.0.0.1%3A5174%2Fapp&prompt=login
PUBLIC_AUTH_LOGOUT_URL=http://127.0.0.1:5055/auth/logout?returnUrl=http%3A%2F%2F127.0.0.1%3A5174%2F
```

5. Rebuild/restart the local staging shape after changing tenant or auth URLs:

```powershell
docker compose --env-file deploy/staging/.env `
  -f deploy/staging/docker-compose.yml `
  -f deploy/staging/docker-compose.vps.yml `
  up -d --build
```

6. Run the read-only preflight for the selected tenant:

```powershell
deploy/staging/smoke-validation-demo-preflight.ps1 validation-oh-research
```

For static preparation without a running local stack, use `-SkipLiveChecks`.
For remote staging endpoint rehearsal from an operator machine, use
`-RemoteOnly` with explicit public origins:

```powershell
deploy/staging/smoke-validation-demo-preflight.ps1 validation-oh-research `
  -RemoteOnly `
  -ApiOrigin https://staging-api.example.com `
  -WebOrigin https://staging.example.com
```

`-RemoteOnly` skips local `.env`, auth role-slot file, tenant-switch helper,
and database-count checks. It verifies only remote API health, web `/app`,
session/CORS behavior, and Auth0 login redirect shape. Run the full preflight
on the VPS or local machine when you also need selected-tenant config,
role-slot file, and database count evidence.

The preflight validates selected tenant, URL shape, fixture catalog, auth
role-slot file shape, live API/web/session/CORS/login redirect behavior, and
non-sensitive selected-tenant counts when available. It does not create Auth0
users, mutate Auth0 or VPS resources, seed data, automate browser login, or
approve real-person data.

The preflight also prints the self-serve walkthrough contract for the selected
tenant. Use it as the operator checklist: open home, open Studies, inspect the
setup sample, collection sample, results sample, and longitudinal sample, then
confirm `Duplicate as study` appears for setup-manager roles while
analyst/viewer smoke roles stay read-only.

7. Open `http://127.0.0.1:5174/app` and sign in with the selected tenant's
   `tenant_owner` or `researcher` role slot.

`prompt=login` is useful for local smoke because it prevents Auth0 from silently
reusing the previous browser SSO account. Remove it when the desired staging
behavior is normal SSO.

## Switching Tenants Locally

To switch from one validation tenant to another:

Use the helper by default:

```powershell
deploy/staging/select-validation-demo-tenant.ps1 validation-oh-research -Restart
deploy/staging/select-validation-demo-tenant.ps1 validation-se-education -Restart
deploy/staging/select-validation-demo-tenant.ps1 validation-osh-consulting -Restart
```

The helper edits only the ignored local `deploy/staging/.env` file. It preserves
secrets and unrelated values, sets `PUBLIC_TENANT_ID`, aligns the local web/API
return URL shape, and can restart the local staging stack. It is an operator
convenience for the normal app path, not a product tenant-switching feature.

Manual fallback:

1. Edit only the ignored local environment file: `deploy/staging/.env`.
2. Replace `PUBLIC_TENANT_ID` with the target tenant ID from the matrix above.
3. Keep `Cors__AllowedOrigins__0` equal to the exact web origin that loads the
   Svelte app.
4. Keep `PUBLIC_AUTH_LOGIN_URL` returning to the web app `/app`, not to the API
   `/app`.
5. Rebuild/restart Compose with the command above.
6. Use sign out plus `prompt=login`, a separate browser profile, or cleared
   Auth0/browser cookies before testing another role slot.

Expected selected-tenant behavior:

- OH user under OH `PUBLIC_TENANT_ID`: should enter the app.
- SE user under OH `PUBLIC_TENANT_ID`: should fail cleanly.
- OSH user under OH `PUBLIC_TENANT_ID`: should fail cleanly.
- SE user under SE `PUBLIC_TENANT_ID`: should enter the app.
- OSH user under OSH `PUBLIC_TENANT_ID`: should enter the app.

## Failure Meaning

| Symptom | Most likely meaning | Operator action |
| --- | --- | --- |
| `auth=failed` after login | Declined Auth0 consent, wrong selected tenant, unverified email, missing platform membership, or provider-subject mismatch. | Use the matching tenant role slot; verify email in Auth0; rerun membership bootstrap if needed; use `prompt=login` to force account selection. |
| Auth0 reuses the wrong account without password prompt | Existing Auth0 SSO session is active. | Use `prompt=login`, log out, use another browser profile, or clear provider/browser cookies. |
| Browser lands on API `/app` and shows 404 | Login return URL is still API-origin `/app` instead of web-origin `/app`. | Fix `PUBLIC_AUTH_LOGIN_URL` and rebuild/restart the web container. |
| `Session check failed` or `Failed to fetch` | API unavailable, CORS origin mismatch, or browser calling the wrong API origin. | Check API health, `PUBLIC_API_BASE_URL`, and `Cors__AllowedOrigins__0`. |
| CORS preflight blocked | Approved web origin is missing or different by scheme/host/port. | Set `Cors__AllowedOrigins__0` exactly to the web origin and restart API. |
| Analyst/viewer can trigger setup/export/launch actions | RBAC regression or stale frontend/API path that is not using `setup.manage`. | Treat as a bug before a serious read-only role walkthrough. Use owner/researcher for mutation workflows. |
| Public respondent route is missing | The seed output did not preserve or print a usable proof route for this run, or the selected campaign has no open-link proof. | Rerun the seed or use the owner inspection routes printed by `seed-validation-demo.ps1`. Do not commit raw tokens. |

## Current Product Walkthrough

Keep the live demo to 7-10 minutes. The goal is not to explain every feature;
it is to make the validator name a real use case, missing blocker, or pilot.

1. `/register` or `/signin` - show the current account/workspace boundary only if the call needs it. For seeded tenant demos, start from sign-in. For a custom private-beta proof, create a workspace and explain that email verification/legal production use remain bounded by Q-053.
2. `/app` - show the tenant-private workspace home after Auth0 and use it as the orientation point.
3. `/app/campaign-series` - create or choose the study relevant to the validator.
4. `/app/campaign-series/{seriesId}/setup` - show the five-step setup path: instrument shell, questionnaire builder, multi-output Results setup, draft wave, and launch readiness.
5. `/app/directory` - show people/groups/manager links and CSV audience import if the call cares about realistic audience preparation.
6. `/app/campaign-series/{seriesId}/operations` - show the Collection hub: choose recipients, preview/save recipient selection, start collection, share respondent access, monitor submissions, and close collection.
7. `/r/{generated-demo-token}` - optional respondent view. Show only a generated owner inspection route from the current seed/output. Do not save or commit raw tokens.
8. `/app/campaign-series/{seriesId}/reports` - show Results widgets, preliminary/final state, export review, and browser CSV download.
9. `/app/campaign-series/{seriesId}/waves` - show wave comparison where the tenant has compatible longitudinal proof data.
10. `/app/team` and `/app/settings` - show only if the validator asks about workspace administration, team access, or handoff to colleagues.

## Tenant Walkthroughs

### Occupational Health Research

Recommended account slot: `tenant_owner` first, `researcher` second.

Recommended series:

- `OH_LONGITUDINAL_TWO_WAVE` for anonymous longitudinal proof;
- `OH_BURNOUT_BASELINE` for baseline report/export proof;
- `OH_ERGO_SCREEN` if ergonomics/MSK is the validator's main interest.

Show:

- tenant-private/non-canonical content posture;
- anonymous or anonymous-longitudinal identity mode;
- questionnaire formats and multi-output Results setup for dimensions/subscales;
- Directory/audience preparation and recipient selection;
- participant-code linkage concept without exposing raw codes;
- preliminary versus closed report labels;
- disclosure suppression for small subgroups;
- CSV/codebook export provenance.

Ask:

- What real study would you run in the next 3-6 months?
- Which instruments are required, and who holds the rights?
- Is anonymous longitudinal linking important enough to change your study
  design?
- What ethics/consent/DPO material would the platform need before real people?
- Is CSV/codebook enough for first use, or is SPSS/Stata/PDF mandatory?
- What is missing before you would recommend it or write it into a grant?

### Software Engineering Education

Recommended account slot: `tenant_owner` first, `researcher` second.

Recommended series:

- `SE_GAMIFICATION_PRE_POST` for pre/post research proof;
- `SE_AI_ASSESSMENT_RESEARCH` for AI-assisted learning/assessment workflow;
- `SE_COURSE_PULSE_CURRENT` for fast course feedback.

Show:

- course/programming education tenant context;
- synthetic student survey content, with no real student records;
- custom questionnaire formats and result outputs for course-study constructs;
- anonymous or anonymous-longitudinal course study flow;
- consent and codebook/export shape;
- how a research or teaching workflow could reuse the same campaign-series
  mechanics.

Ask:

- What student/course/research survey would you actually run?
- Would this fit teaching quality, student research, a thesis project, or a
  funded research workflow?
- Do you need anonymous, anonymous longitudinal, or identified follow-up?
- What metadata must exist for course, cohort, wave, and analysis?
- What would a university approval path require?
- Who else at the university should validate the workflow?

### Workplace Safety Consulting

Recommended account slot: `tenant_owner` first, `researcher` second.

Recommended series:

- `OSH_CLIENT_A_TRAINING` for training feedback;
- `OSH_CLIENT_A_SAFETY_CLIMATE` for aggregate safety climate and suppression;
- `OSH_CLIENT_B_ERGONOMICS` for client-portfolio and ergonomics proof;
- `OSH_NEAR_MISS_LEARNING` only as synthetic near-miss learning, not real
  incident collection.

Show:

- fake client portfolio shape;
- training/safety/ergonomics campaign split;
- audience import/recipient selection as a consultant preparation workflow;
- anonymous employee response posture;
- aggregate report and subgroup suppression;
- export proof labelled as consultant proof, not legal compliance output.

Ask:

- How many client assessments do you run yearly?
- Which questionnaires or local forms are mandatory?
- What report format must clients receive?
- How much time would this need to save to justify paying?
- Would a reusable Croatian report template be worth a setup fee?
- What would block use with a real client: legal, language, PDF, delivery,
  anonymity, or price?

## Evidence Capture

During or immediately after the call, capture evidence in the format from
[Owner Blockers Action Pack](OWNER-BLOCKERS-ACTION-PACK.md#owner-capture-form).

Minimum useful notes:

- contact, role/org, date, and blocker bucket: `O01`, `O02`, `O03`, `Q-053`,
  or future canonical-publish decision;
- demo tenant shown and role slot used;
- green/yellow/red outcome;
- real campaign/study/client they named;
- required instruments and rights owner;
- required identity mode;
- approval path: ethics, DPO, procurement, client, department, or grant;
- report/export requirement;
- minimum acceptable production posture;
- missing proof or production blocker;
- introductions, pilot/referral/grant/funding path, or explicit decline reason;
- recommended next doc/code action.

Treat "looks good" as weak evidence unless the validator names a real workflow,
blocker, referral, pilot, grant path, or budget path.

## Do Not Claim

Do not say or imply:

- "GDPR compliant" as a final production legal claim;
- production DPA/privacy notice/SLA readiness;
- that real participant/student/employee/customer data is approved;
- that Auth0 roles, Auth0 Organizations, enterprise SSO/SCIM, or paid Auth0
  are in use;
- final role taxonomy, export-specific analyst permissions, complete
  user-management UX, invitation email flows, Auth0 user creation, hard delete,
  or complete production owner/researcher/analyst/viewer semantics;
- platform-shipped canonical CBI, COPSOQ, NMQ, NOSACQ, MBI, OLBI, or other
  named-instrument presets;
- official validated interpretation, norms, thresholds, clinical advice, or
  legal OSH compliance advice;
- production email deliverability, final PDF reporting product, object storage,
  signed URLs, async export workers, or complete download audit;
- final visual design or final UX.

Use this phrasing instead:

> This is a proof-only tenant-private workflow with synthetic or owner-controlled
> test data. It shows the mechanics we can validate: auth, tenant membership,
> instrument setup, response collection, scoring, reports, waves, and
> CSV/codebook exports. The current builder also supports multiple custom
> result outputs for private-beta validation. Real-person data and production
> legal/GDPR claims still need the Q-053 path for the exact pilot scenario.

## After The Call

Update handoff evidence before changing the roadmap:

1. Paste cleaned notes into `SESSION-LOG.md` or a future
   `docs/v2/50-business/validation-notes.md`.
2. If the call produces a concrete missing product blocker, run an assessment
   before pulling implementation.
3. If the call asks for real people, start Q-053 for that exact use case before
   collecting any real data.
4. If the call asks for a platform-shipped named instrument, treat it as a
   future canonical-publish decision, not an implied permission.
5. If the call only exposes operator confusion, improve this packet before
   changing product code.
