# Owner Blockers Action Pack

Status: Active.
Updated: 2026-05-20.

Purpose: convert owner-only blockers into concrete outreach, call scripts, decision criteria, and evidence-capture steps. These are not agent-executable closures; the owner must send messages, take calls, and decide based on responses.

## Priority Order

Use this as the current owner action queue:

1. O01 / Q-004: send the prof-lead validation message and schedule the call.
2. O02: identify 2 secondary academic validators and send the academic-validator message.
3. O03 / Q-005: identify 3 OSH consultants and send the OSH-consultant message.
4. Q-053: keep validation proof-only until a real-person pilot is requested; prepare legal path before any real participant/student/employee data.
5. Q-021 is closed by ADR-0012; maintain the portable Docker hosting boundary when demoing or planning live validation.

M1 code can start before all owner-only blockers close. D73A closed Q-047 by
product de-scope: the active product is not an OLBI product and must not claim
OLBI as included, official, platform-canonical, or platform-granted. Tenant-
provided/private named-instrument content is allowed only with tenant
attestation.

Implementation alignment for all teams: follow [`olbi-capable-engine-principle.md`](olbi-capable-engine-principle.md).

## This Week Checklist

- [x] Record Q-020 decision: Auth0 Free for M1 with Keycloak fallback.
- [x] Record Q-021 final M1 decision: portable Docker Compose posture with the existing Hostinger VPS as current validation host.
- [x] Record Q-053 interim decision: proof/demo data only until legal/entity path exists.
- [x] Build Q-053 counsel-ready legal intake artifact:
      [`../40-ops/q053-counsel-ready-legal-packet.md`](../40-ops/q053-counsel-ready-legal-packet.md).
- [x] Build the O01 prof validation tenant plan around occupational-health / burnout / ergonomics.
- [x] Build the O02-A Danijel Kucak validation tenant plan around software-engineering education and learning analytics.
- [x] Build the O03 validation tenant plan around Croatian `zastita na radu` / workplace safety workflows.
- [x] Record current VPS rehearsal proof: authenticated remote smoke, authenticated product-spine smoke, target backup/restore, redeploy, and rollback/restore.
- [x] Refresh current validation packet after private-beta UI changes through setup, audience, Results, Waves, settings, and sidebar cleanup.
- [x] Add D366 owner-run private-beta validation rehearsal script.
- [ ] Identify O02-B secondary academic validator.
- [ ] Pick 3 O03 OSH consultant targets when outreach starts.

Do not wait for every call to finish before starting the Q-020/Q-021/Q-053 decision notes. The point is to stop vague blockers from staying vague.

## Blocker Packet Status

| ID | Owner action | Agent help allowed | Close only when |
|---|---|---|---|
| O01 / Q-004 | Send message, take call, capture evidence. | Keep script/demo/capture form current. | Prof lead gives a usable green/yellow/red result with a named study, referral path, grant/pilot path, or explicit decline reason. |
| O02 | Choose and contact 2 secondary academic validators. | Refine message and capture form after O01 feedback. | Two calls are scheduled/completed, or two named contacts decline with useful reasons. |
| O03 / Q-005 | Choose and contact 3 OSH consultants. | Keep target categories, pricing questions, and report-template questions current. | Three named consultants are contacted and at least one call is scheduled, or all three decline with reasons. |
| Q-020 | Done for M1: Auth0 Free accepted, Keycloak fallback retained. | Implement ADR-0011 follow-up and provider-specific auth later. | Closed by ADR-0011; paid Auth0 still requires a new owner decision. |
| Q-021 | Closed for M1 by ADR-0012: portable Docker Compose, current Hostinger VPS as validation host, migration triggers for stronger hosting. | Keep runbook/cost docs current; do not claim SLA/DPA/GDPR readiness from hosting alone. | Closed. Reopen only for managed DB, hyperscaler, single-tenant, hospital/public-sector, or customer-specific hosting demand. |
| Q-053 | Keep validation proof-only until real people are involved. | Keep [`../40-ops/q053-counsel-ready-legal-packet.md`](../40-ops/q053-counsel-ready-legal-packet.md) current for the first real pilot scenario. | Qualified counsel or owner legal authority approves production-facing DPA/privacy/DPIA/retention/sub-processor posture for the tenant type. |

Except where this packet explicitly cites an accepted owner decision or ADR,
recommended defaults below are decision proposals, not closed decisions. They
do not authorize production launch, paid service adoption, hosting procurement,
or tenant-facing legal claims by themselves.

## Owner Decision Snapshot - 2026-05-14

```text
O01:
Prof lead reacted strongly positively and has time to test in about a month.
Prepare a private occupational-health validation tenant with CBI, COPSOQ III,
and NMQ-style workflows after source/license checks. Do not seed MBI, OLBI, or
rights-unclear proprietary item text.

O02:
O02-A is Danijel Kucak at Algebra University. Prepare a separate
university/software-engineering education tenant around student surveys,
course pulse workflows, learning analytics, gamification/programming education,
and AI-assisted assessment. O02-B is still missing.

O03:
No consultants contacted yet. Prepare a simple Croatian `zastita na radu`
validation tenant first: training feedback, safety climate, ergonomics check,
and near-miss learning pulse. O03 remains open until consultants are contacted.

Q-020:
Auth0 Free accepted for M1 validation and first production-like auth.
Keep Keycloak as fallback. No paid Auth0 plan without explicit owner decision.

Q-021:
Closed by ADR-0012. Use a portable Docker Compose posture for M1 validation,
with the existing Hostinger VPS as the current validation host. Hostinger is
not a hard product dependency; move when capacity, reliability, backup/restore,
procurement, data-residency, hospital/public-sector, or paid production needs
require it.

Q-053:
No company/legal entity yet. All validation hosting is proof-of-concept with
synthetic, seed, demo, or owner-controlled test data only. Real participant,
student, or employee production data requires legal/entity/GDPR path first.
```

## Current Proof Demo Boundary

Use [`../50-business/current-proof-demo-brief.md`](../50-business/current-proof-demo-brief.md) when an owner-only validation call needs a walkthrough.

VAL02 added the validation seed gate and tenant seed specification:

- [`validation-demo-source-checks.md`](validation-demo-source-checks.md)
  records CBI, COPSOQ III, NMQ, NOSACQ-50, Danijel/Algebra, and Croatian
  OSH source boundaries.
- [`validation-demo-tenants.md`](validation-demo-tenants.md) defines the three
  proof-only tenants, instruments, campaign states, report/export labels, and
  VAL03 acceptance checklist.

The current proof can show an owner-controlled VPS staging tenant-private path
through registration or existing workspace sign-in, Auth0-backed login,
CSRF-protected tenant-member actions, workspace/study creation, five-step setup,
questionnaire formats, multi-output Results setup, Directory CSV audience import,
recipient selection, launch readiness, public respondent consent/code flow,
answer save/review/submit, submit-time scoring, collection close, governed
Results/Waves, CSV/codebook proof exports, report PDF artifact proof, Team and
Settings administration, withdrawal request/review/execute lifecycle, in-app
operational notifications, close/finality labels, target backup/restore, redeploy,
rollback/restore, and safe evidence capture. Local/dev staging remains the
fallback when the owner is working offline.
Run [`private-beta-validation-rehearsal.md`](private-beta-validation-rehearsal.md)
once before O01/O02/O03 so the call uses a timed walkthrough instead of an
improvised product tour.

Frame it as current proof, not production software:

- owner-controlled ADR-0012 M1 validation host only, not a paid production,
  managed-hosting, legal/GDPR/DPA, privacy-notice, or SLA claim;
- tenant-private or owner-controlled demo content only;
- no platform-shipped named-instrument claim, public item text for
  rights-unclear instruments, or official score labels;
- no final DPA/privacy notice/GDPR legal signoff while Q-053 remains open;
- no outbound operational-notification email, requester/admin email workflow,
  notification preference workflow, SMTP-backed operational alerting, or claim
  that operational events are emailed while Q-054 remains open;
- no production SMTP deliverability, object-storage/S3 production credentials,
  validated interpretation, norms, thresholds, clinical advice, or statutory OSH
  advice.

The proof is useful for validation because it makes the workflow concrete. Do
not treat positive reaction to the UI as validation unless the contact also
names a real study/client, must-have instrument, report/export need, approval
path, pilot condition, referral, or purchase/funding path.

## O01 - Prof Lead Validation

Goal: get an explicit yes/no on the P1 wedge:

> If I gave you a tenant-provided validated-instrument workflow with anonymous longitudinal tracking, SPSS/analysis-ready export path, and EU-hosted data, would you use it in your next study, recommend it to 5 colleagues, and write it into your next grant?

### Message to Send

Subject: 20-minute validation call on burnout / occupational health research platform

Hi [Name],

I am validating a focused platform for academic occupational-health studies: tenant-provided validated instruments, anonymous longitudinal response tracking, ethics/consent capture, analysis-ready exports, and EU-hosted data.

I have a local proof of the workflow and would value a blunt 20-minute conversation before committing the production version:

- would you use this for a real upcoming study;
- what would make it scientifically unacceptable;
- which instruments/export/reporting details are mandatory;
- whether you would recommend it to colleagues or include it in a grant.

Could we schedule a short call this week or next?

Best,
[Owner]

### Call Agenda

1. Current workflow: how they run burnout / occupational-health studies today.
2. Instrument reality: required burnout, psychosocial, ergonomic, or wellbeing instruments, and who has rights to use them.
3. Optional current-proof walkthrough: VPS staging login, study setup, questionnaire builder, multi-output Results setup, audience import/recipient selection, launch, respondent flow, scoring, Results/Waves, CSV/codebook/PDF export, withdrawal, in-app notifications, finality, Team/Directory/Settings, sign-out/wrong-account recovery, and backup/restore/redeploy/rollback evidence, all framed as owner-controlled staging engineering proof.
4. Launch test: "Would you use this for the next real study? Which one?"
5. Recommendation test: "Would you introduce me to 3-5 colleagues who run similar studies?"
6. Grant test: "Would this be credible enough to include as infrastructure in a grant?"
7. Deal-killers: ethics approval, license, translations, data residency, exports, audit trail, cost, institution procurement.
8. Must-have M1: ask them to force-rank the top 5 production gaps after seeing the proof, including audience preparation, custom scoring/results, export/report format, legal posture, and hosting evidence.
9. Follow-up: ask for two introductions and one sample export/report expectation.

### Decision Criteria

Green:

- names a real study or grant where this could be used;
- agrees to introduce at least 2 colleagues;
- accepts EU hosting in principle;
- names the minimum report/export and instrument-rights posture needed for a pilot;
- identifies concrete must-haves rather than vague interest.

Yellow:

- likes the idea but no specific study yet;
- wants a demo before introductions;
- cares about translation/license issues but sees the value;
- likes the current proof but needs production auth/legal/export details before a pilot.

Red:

- says "nice idea" but will not use it;
- will not introduce anyone;
- says existing tools are good enough;
- requires institutional procurement before pilot validation;
- cannot name any study, referral, grant, or pilot path after seeing the proof.

### O01 Evidence Capture

```text
Contact:
Role/org:
Message sent:
Call date:
Next real study named:
Required instruments and rights holder:
Identity mode needed: anonymous / anonymous_longitudinal / identified / unsure
Required export/report:
Ethics/DPO/procurement path:
Current proof reaction:
M1 blocker ranked 1:
M1 blocker ranked 2:
M1 blocker ranked 3:
Would use in next study: yes / maybe / no
Would introduce colleagues: yes / maybe / no
Would include in grant: yes / maybe / no
Follow-up owner action:
Green/yellow/red:
```

## Q-047 - OLBI Source and License Evidence (closed/de-scoped)

Current conclusion: Q-047 is closed by product de-scope, not by legal permission. The active product will not ship, seed, market, or claim OLBI as platform-canonical M1 content.

Owner decision on 2026-05-06 (reconfirmed 2026-05-12): this does not block tenant-provided OLBI-shaped imports or owner-controlled private/internal test tenants. Launching private demo campaigns should use owner/tenant-attested status; unverified internal-demo status remains non-launching development content.
The product ship path remains: sell a generic validated-instrument workflow, scoring rule engine, derivative flow, and campaign lifecycle tooling. Customers/tenants can create or import instruments they can attest rights for. Rights-unclear examples can exist only as private owner/demo-tenant content or tenant-provided content with attestation, never as platform-shipped canonical presets.

D73A owner decision on 2026-05-13: do not pursue OLBI as active
platform-canonical proof content. Keep the evidence/playbook below only as a
dormant reference if the owner later reopens canonical named-instrument content.

### Evidence to Capture

Minimum acceptable evidence:

- canonical item source and scoring source;
- whether the English OLBI items may be displayed inside a closed-source SaaS;
- whether customer studies, paid tenants, and consultant use are allowed;
- attribution wording;
- whether Croatian translation/back-translation is allowed;
- whether translations can be stored and distributed to platform tenants;
- whether derivative wording is allowed;
- whether a license fee or written agreement is required.

### Source Notes Found

- Core OLBI literature includes Demerouti/Bakker work and the English validation literature.
- The English validation article is Halbesleben & Demerouti, 2005, Work & Stress, DOI `10.1080/02678370500340728`. Publisher page: <https://www.tandfonline.com/doi/abs/10.1080/02678370500340728>
- TU/e research portal lists Demerouti, Bakker, Vardakou, and Kantas, 2003, "The convergent validity of two burnout instruments", DOI `10.1027//1015-5759.19.1.12`: <https://research.tue.nl/en/publications/the-convergent-validity-of-two-burnout-instruments-a-multitrait-m>
- Third-party pages reproduce OLBI items, but those pages are not license evidence.
- Some academic adaptations report obtaining author permission, which supports the "ask authors directly" path but does not grant platform rights.

Useful starting links:

- OLBI English validation: <https://www.tandfonline.com/doi/abs/10.1080/02678370500340728>
- TU/e publication record for the 2003 convergent-validity paper: <https://research.tue.nl/en/publications/the-convergent-validity-of-two-burnout-instruments-a-multitrait-m>
- Permission request targets: official university profile pages for Evangelia Demerouti, Arnold B. Bakker, and Jonathon R. B. Halbesleben.

### Permission Email

Subject: Permission clarification for OLBI use in EU research platform

Dear [Professor Name],

I am building an EU-hosted research platform for validated occupational-health instruments. The first intended proof instrument is the Oldenburg Burnout Inventory.

Before implementing it, I want to clarify permission and attribution. The platform would:

- show OLBI items to respondents in academic and organizational studies;
- calculate OLBI scores according to the published scoring rules;
- export response and score data to researchers;
- support an English version first and potentially a Croatian translation/back-translation later;
- be proprietary closed-source SaaS, with free/low-cost academic use and paid organizational tenants.

Could you confirm whether this use is permitted, and under what conditions?

Specifically:

1. Which publication or document should we treat as the canonical item/scoring source?
2. Is commercial SaaS use permitted, or is a separate license required?
3. Are translations permitted, and may translated items be stored/displayed to tenant respondents?
4. What attribution text should appear in the instrument catalog and reports?
5. Are there restrictions on modified/derivative item wording?

If you are not the right person to grant permission, could you point me to the correct contact?

Best regards,
[Owner]

### If Q-047 Is Reopened

Only reopen the legal-clearance path when the owner explicitly wants OLBI or
another named instrument as platform-shipped canonical content. In that case,
permission is sufficient only when written permission or a clearly applicable
license is saved with:

- source URL or email PDF;
- permission date;
- permitted use cases;
- forbidden use cases;
- required attribution;
- translation rules;
- whether paid tenant use is allowed.

If permission is denied or unclear after follow-up, keep the active product on
the tenant-provided/private proof spine and choose any future canonical
candidate only through a new owner/legal decision.

### Q-047 unblock playbook (practical)

Use `Q047-OLBI-clearance-playbook.md` only if the owner explicitly reopens
OLBI as a platform-shipped canonical candidate. Implementation of the rest of
the product continues through the generic tenant-provided proof spine.

#### Execution notes from the playbook

- Save all evidence in one folder so one-owner handoff has a clear source-of-truth:
  - `docs/v2/10-domain/olbi-clearance/`
- If evidence is complete in 7 days, close Q-047 by setting canonical flags in:
  - `10-domain/instruments-catalog.md`
  - `10-domain/olbi-source-evidence.md`
  - `80-agent-handoff/OPEN-QUESTIONS.md`
- If evidence is incomplete or negative, keep OLBI out of the platform-shipped
  canonical lane and choose a different future canonical candidate only through
  a new owner/legal decision.

Use this only if the owner explicitly reopens OLBI as a platform-shipped
canonical candidate.

#### Phase 0 (today)
- Do not claim OLBI as included/preset on any public artifact.
- Keep OLBI usage to:
  - tenant-provided imports (attested_by_tenant), or
  - owner/demo private workspaces that remain non-canonical.
- Verify no API/catalog seed path exposes `platform_granted` OLBI canonical launchability unless `rights_status == verified`.

#### Phase 1 (48 hours) — evidence minimum set
- Download and archive one canonical source package:
  - OLBI instrument paper/reference set (English source or official manual),
  - scoring definition from source,
  - any official licensing note for redistribution in paid settings.
- Send permission email to each likely rights holder contact (authors and/or university/IP offices).
- Create a single local evidence record with:
  - response date,
  - permission text/attachment,
  - permitted use cases (academic research, commercial tenant, SaaS),
  - forbidden use cases,
  - allowed derivative/translation and tenant redistribution.

#### Phase 2 (7 days) — decision
- If we get explicit permission for SaaS/commercial platform grant, set `OLBI` as canonical candidate:
  - `license_type` and `rights_status` to a confirmed value (not `unknown`),
  - add official scoring fixture pack + source provenance references,
  - update catalog to `canonical published` scope only for allowed locales.
- If permission remains absent/ambiguous:
  - close Q-047 as blocked,
  - remove OLBI from canonical roadmap for now,
  - promote a cleaner canonical path (for example `COPSOQ-III (short)` or `UWES-9`) with same wedge mechanics,
  - keep tenant-owned OLBI support via attested imports.

#### Decision matrix

| Evidence quality | What it enables | What to do now |
|---|---|---|
| Full commercialization permission | Canonical OLBI seed + marketing + cross-tenant use | `platform_granted` + `verified` path, scoring fixtures, and canonical launch support |
| Academic-only permission | No cross-tenant canonical claims | Keep OLBI private/import path only |
| No response / ambiguous | No platform-canonical OLBI | Keep OLBI out of the active product claim and use tenant-provided paths only |

#### Product safety hold in any “not verified” state
- No public OLBI marketing/sample screenshots with item text.
- No `platform_granted` canonical OLBI seed.
- No tenant-facing claim of official OLBI validity.
- Continue generic builder/scoring/report infrastructure (already allowed).

### Practical Product Rule

- Do not market "OLBI included".
- Do not seed OLBI globally as a platform preset.
- Do not put OLBI item text in public docs, screenshots, sample repos, or marketing.
- Allow tenant-created/imported instruments with rights attestation.
- Mark owner/demo-tenant rights-unclear examples as private and not platform-canonical.
- Allow internal testing and private demos with owner/tenant-supplied OLBI-shaped content. Use `attested_by_tenant` when the demo must launch campaigns; keep `unverified_internal_demo` non-launching.
- Add notice/takedown handling before broad public tenant onboarding.

## O02 - Two Secondary Academic Validators

Goal: prevent the P1 signal from being a sample of one.

Target profiles:

- occupational health / work psychology researcher;
- ergonomics or occupational medicine researcher;
- public health / nursing / organizational wellbeing researcher with survey-study experience.

Selection rule: choose the two warmest paths first, not the two most prestigious names. A fast candid answer beats a prestigious non-response.

Current O02-A (2026-05-14): Danijel Kucak, Algebra University. Treat him as a
warm, high-value academic validator for university/software-engineering
education workflows rather than as another occupational-health validator. His
demo tenant should focus on student surveys, course/program evaluation,
learning analytics, gamification/programming education research, AI-assisted
assessment, and exportable research data. O02-B is still missing.

### Message to Send

Subject: Quick validation question on occupational-health research tooling

Hi [Name],

I am validating a platform idea for academic occupational-health studies: validated instruments, anonymous longitudinal response tracking, ethics/consent capture, and analysis-ready exports. I can show a local proof if that makes the workflow easier to judge.

I am not asking you to buy anything. I need a blunt 20-minute research-workflow reality check:

- what tools you use today;
- what breaks or wastes time;
- whether this would be useful for a real study;
- which instrument/export/ethics features are mandatory.

Would you be open to a short call?

Best,
[Owner]

### Questions

- What was the last study where survey tooling caused friction?
- Which instruments do you actually use?
- Do you need anonymous longitudinal tracking, or is anonymous cross-sectional enough?
- Is SPSS export mandatory, or is CSV enough?
- Would EU hosting satisfy your ethics/institutional requirements?
- Would you use this if OLBI/COPSOQ were turnkey?
- If shown the current proof, what is the first missing piece that would stop you from piloting it?
- Would the current proof be credible enough to show a PI, ethics board, or research collaborator?
- Who else should I talk to?

Close O02 when two calls are scheduled or two named contacts explicitly decline with useful reasons.

### O02 Mini Capture

```text
Contact:
Role/org:
Message sent:
Response:
Current tool/process:
Pain worth switching for:
Required instrument/export/ethics feature:
Academic Free reaction:
Researcher Pro price reaction:
Current proof reaction:
Main blocker:
Referral offered:
Green/yellow/red:
```

## O03 - Three OSH Consultant Validators

Goal: validate whether P2/M3 has a paid path: white-label psychosocial risk / burnout / ergonomics assessment with compliance-oriented reporting.

Seed target categories:

- Croatian occupational safety consulting firms;
- workplace health / risk assessment consultants;
- firms already selling `zaštita na radu` services to employers.

Possible seed targets found:

- Konspekt d.o.o. Zagreb: <https://konspekt.hr/> and service page <https://konspekt.hr/usluga/zastita-na-radu/>
- Vizor d.o.o. Varaždin: <https://vizor.hr/> and service page <https://web.vizor.hr/zastita-na-radu/zastita/>
- Total Inspect d.o.o. Šibenik: <https://totalinspect.hr/> and service page <https://totalinspect.hr/pages/zastita-na-radu/>
- Proatest d.o.o. Zagreb / regional offices: <https://proatest.hr/> and service page <https://proatest.hr/zastita-na-radu/>
- REST-ING d.o.o. Bjelovar/Zagreb: <https://resting.hr/usluge/sigurnost/zastita-na-radu>

Pick 3 using this mix:

- one Zagreb-based provider;
- one regional provider;
- one provider with broader compliance/service coverage.

Current demo posture (2026-05-14): lead with a simple Croatian
`zastita na radu` / workplace-safety tenant, not a broad psychosocial-risk
suite. Seed recognizable workflows first: training feedback, safety climate,
ergonomics/musculoskeletal check, and incident or near-miss learning pulse.
Psychosocial/COPSOQ-style depth can come later if consultants validate that
need.

### Message to Send

Subject: Quick question on psychosocial risk / burnout assessment tooling

Hi [Name],

I am validating a white-label tool for occupational-safety consultants: burnout/psychosocial-risk questionnaires, anonymous employee response collection, consultant dashboard, and employer-ready PDF/Excel reports. I can show a local proof of the workflow, but the white-label PDF/report package is still a production requirement to validate.

I am trying to understand whether this solves a real consulting pain point, not selling yet. Could I ask for 15-20 minutes?

The key questions are:

- do clients ask you for psychosocial-risk or burnout assessment;
- how you run those assessments today;
- whether white-label reporting would help;
- what pricing model would make sense: monthly, per client, or per assessment.

Best,
[Owner]

### Call Questions

- Do clients currently ask for psychosocial risk, stress, burnout, ergonomics, or wellbeing surveys?
- Which tools do you use today?
- What reports do employers expect?
- Would a white-label dashboard/report help you sell or deliver the service?
- Would you pay monthly for this, or only per project?
- Price test: is EUR 30-50/month too low, plausible, or too high? What about per-client/per-assessment pricing?
- What would make it unusable: Croatian language, legal template, data hosting, anonymity, PDF branding, support?
- If shown the current proof, which part maps to your delivery workflow and which missing production/reporting piece blocks a paid pilot?

Close O03 when three named consultants are contacted and at least one call is scheduled, or when all three explicitly decline with reasons.

### O03 Mini Capture

```text
Contact:
Role/org:
Message sent:
Response:
Clients assessed per year:
Current assessment/report workflow:
Hours spent per client/report:
Required Croatian/legal/report template:
Support owner: consultant / platform / shared
Pricing preference: monthly / per client / per assessment / annual
Setup fee reaction:
Current proof reaction:
Paid pilot blocker:
Green/yellow/red:
```

## Q-020 - Auth Provider Decision Packet (closed for M1 by ADR-0011)

Purpose: record the owner-accepted M1 provider posture and the remaining
implementation guardrails.

Current implementation state:

- Backend auth is OIDC/JWT-provider neutral.
- `/auth/session` exists as the frontend auth boundary.
- AU02 readiness now fails closed outside Development when OIDC authority/audience are missing or unsafe.
- No provider login/callback, refresh-token rotation, app session persistence, or provider user-management UX exists yet.

### Accepted Owner Decision

ADR-0011 closes Q-020 for M1:

- Auth0 Free is accepted for M1 validation and first production-like auth.
- Keycloak remains the documented fallback if Auth0 becomes financially,
  operationally, data-residency, or procurement-wise unsustainable.
- The platform stays OIDC-provider neutral.
- The platform database remains the source of truth for tenants, roles,
  permissions, campaign access, and authorization.
- No paid Auth0 plan may be adopted without a new explicit owner decision.

### Options

| Option | Why choose it | Main cost/risk | When it fits |
|---|---|---|---|
| Auth0 Free for M1 | Fastest path to production-like OIDC, MFA, hosted login, lower identity ops, and zero current monthly auth cost. | Vendor lock-in, pricing/sub-processor review, public-cloud SaaS dependency, free-tier limits can change. | Chosen by ADR-0011. |
| Keycloak self-host | More control, no per-MAU SaaS bill, easier self-host story. | Owner operates upgrades, security hardening, backups, availability, email, realms, admin lockout recovery. | Tenant/procurement requires self-host/control, or Auth0 cost/sub-processor posture is unacceptable. |
| Hybrid: Auth0 now, Keycloak trigger later | Preserves M1 speed while naming the escape hatch. | Migration work later; must isolate provider-specific code now. | This is the accepted posture. |

### Remaining Guardrails

1. Do not use Auth0 Organizations as the platform tenant model.
2. Do not use Auth0 roles/permissions as the app authorization model.
3. Do not adopt a paid Auth0 plan without a new owner decision.
4. Include Auth0 in Q-053 sub-processor/legal review before real production data.
5. Reassess Keycloak if Auth0 pricing, limits, data residency, or procurement
   becomes a blocker.

### Evidence To Capture

```text
Decision date:
Owner:
Chosen posture: Auth0 M1 / Keycloak M1 / hybrid / defer
Reason:
Rejected option and reason:
Cost limit:
Sub-processor/legal concern:
Self-host trigger:
ADR needed: yes / no
Implementation follow-up allowed:
```

Q-020 is closed by ADR-0011. The next auth implementation slice should be
provider-specific login/callback, refresh/session persistence, cookie/CSRF
posture, secrets, and smoke tests.

## Q-021 - Hosting Decision Packet (closed for M1 by ADR-0012)

ADR-0012 accepts a portable Docker Compose hosting posture for M1 validation and
first production-like proof. The owner's existing Hostinger VPS is the current
validation host because it is already paid and configured. This is a hosting
posture decision, not a permanent vendor commitment.

### Accepted Decision

- Use the current Hostinger VPS for proof/demo/staging validation tenants and
  Auth0 callback testing.
- Keep the runtime portable across Hostinger, another EU VPS, or a future
  homelab-plus-interface-VPS setup by preserving the Docker Compose, `.env`,
  nginx/TLS, backup/restore, smoke-test, and Auth0 callback contract.
- Do not require GitHub Actions, registry promotion, Terraform, or managed
  PostgreSQL before validation demos.
- Before real-person data, Q-053 must be resolved for the tenant scenario and a
  backup/restore smoke must pass for the target environment.
- Before hospital, public-sector, enterprise, SLA, DPA, or high-sensitivity
  production claims, reassess whether the plain VPS posture is enough or a
  managed database, different EU provider, hyperscaler, or single-tenant path
  is required.

### Migration Triggers

Move beyond the current VPS when one of these is true:

- the VPS cannot run the validation tenants comfortably;
- backup/restore cannot be proven before real-person data;
- uptime, disk, CPU, memory, or network reliability becomes a validation risk;
- a validator/customer rejects the current provider or no-SLA posture;
- hospital/public-sector/enterprise procurement needs a different provider,
  managed database, single-tenant deployment, or security questionnaire answer;
- a future homelab/interface-VPS path is ready and does not weaken externally
  needed guarantees.

### Hosting Capture For Future Reassessment

```text
Reassessment date:
Tenant/customer scenario:
Current host:
Data class: synthetic / owner-controlled test / real participant / paid tenant / hospital-public sector
Q-053 status:
Backup/restore proof status:
Monthly infra budget:
Managed database required: yes / no / later
Data residency/procurement constraint:
Single-tenant trigger:
Decision: stay portable VPS / move VPS provider / managed DB / hyperscaler / self-host
ADR update needed: yes / no
Implementation follow-up allowed:
```

Q-021 is closed. Reopen hosting only when a concrete tenant/customer/data
scenario breaks ADR-0012's portable Docker posture.

## Q-053 - Production GDPR Legal Package Packet

Purpose: convert "legal/compliance later" into a counsel-ready packet without presenting product docs as qualified legal advice.

OB04 added the dedicated counsel-intake artifact:
[`../40-ops/q053-counsel-ready-legal-packet.md`](../40-ops/q053-counsel-ready-legal-packet.md).
Use that file as the current handoff artifact for legal review. This section is
the owner action summary.

Current implementation state:

- `compliance-gdpr.md` is an implementation and legal-review brief, not final legal text.
- Current proof can be demoed through the owner-controlled VPS staging rehearsal lane or local/dev fallback, but production tenant launch remains blocked by Q-053.
- Tenant-facing DPA, privacy notice, DPIA wording, retention commitments, sub-processor schedule, and transfer language need qualified EU/Croatian counsel or explicit owner legal authority before external use.

### Decision Proposal For Owner Review

Owner-confirmed interim posture: there is no company/legal entity yet, so the
platform must not claim production GDPR/DPA/legal readiness. Hosted validation
tenants are proof-of-concept only and may contain synthetic, seed, demo, or
owner-controlled test data. If a validator asks to use the platform with real
participants, students, employees, patients, or other real people, Q-053
escalates before launch.

This unblocks proof/demo work and production-grade engineering with synthetic,
seed, demo, or owner-controlled test data. It does not unblock real-person or
paid production use, and it does not authorize tenant-facing GDPR/DPA/legal
claims.

### Counsel Packet Questions

Send counsel or legal owner these questions with `compliance-gdpr.md`, `security.md`, `deployment.md`, `runbook.md`, and the first tenant scenario:

1. Platform legal entity, address, governing law, and contracting party.
2. Controller/processor posture for academic research, OSH consultant portfolio, employer-run wellbeing, and hospital/public-sector pilots.
3. DPA structure, processor obligations, audit wording, sub-processor notice/objection, and support-access terms.
4. Privacy notice split: platform-controller records vs tenant campaign processing.
5. DPIA trigger rules for health/wellbeing, employee monitoring, anonymous longitudinal trajectories, small cohorts, free text, and special category data.
6. DPO/privacy lead requirement before first production tenant.
7. Lawful-basis and Article 9 presets for academic, OSH, employer, hospital, and education contexts.
8. Retention numbers for responses, scores, consent records, withdrawal events, audit logs, support records, exports, and backups.
9. Sub-processor schedule after Q-020/Q-021/email/monitoring decisions.
10. Non-EEA transfer stance for auth, email, monitoring, support, and payments.
11. Breach notification workflow, processor-to-controller timing, and tenant incident evidence.
12. Data-subject rights workflow limits for anonymous, anonymous longitudinal, and identified modes.
13. Minimum TOMs/security schedule to attach to DPA/security packet.

### Owner Must Do

- Choose counsel or a qualified legal-review route.
- Pick the first tenant scenario to review: academic pilot, OSH consultant, direct employer, hospital/public-sector, or education institution.
- Confirm interim rule: validation demos remain non-production until Q-053 closes for the tenant type.
- Provide legal entity/contact details and any jurisdiction preference.
- Approve or reject whether a formal DPO is needed before first production tenant.

### Evidence To Capture

```text
Counsel/legal owner:
Review date:
Tenant scenario:
Legal entity/governing law:
DPO/privacy lead decision:
Controller/processor conclusion:
Required DPA/privacy/DPIA changes:
Retention numbers approved:
Sub-processor/transfer constraints:
Production launch allowed for scenario: yes / no / conditional
Conditions before launch:
```

Close Q-053 only after qualified counsel or explicit owner legal authority approves the production-facing package for the target tenant type. Do not close it with internal implementation notes alone.

## Source Checks Reviewed 2026-05-14

These sources were checked only to keep decision packets current. Re-check before procurement, legal signoff, or external quotes.

- Auth0 pricing and deployment-model signal: <https://auth0.com/pricing>
- Keycloak production configuration: <https://www.keycloak.org/server/configuration-production>
- Keycloak administration guide: <https://www.keycloak.org/docs/latest/server_admin/>
- Hostinger VPS pricing and self-managed VPS posture: <https://www.hostinger.com/vps-hosting>
- Hetzner 2026 cloud price adjustment: <https://docs.hetzner.com/general/infrastructure-and-availability/price-adjustment/>
- OVHcloud public cloud pricing and managed PostgreSQL signal: <https://www.ovhcloud.com/en/public-cloud/prices/>
- AWS RDS for PostgreSQL product/pricing entry point: <https://aws.amazon.com/rds/postgresql/>
- Azure Database for PostgreSQL pricing model: <https://azure.microsoft.com/en-us/pricing/details/postgresql/>
- European Commission DPIA guidance: <https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/when-data-protection-impact-assessment-dpia-required_en>
- EDPB controller/processor concepts: <https://www.edpb.europa.eu/our-work-tools/our-documents/guidelines/guidelines-072020-concepts-controller-and-processor-gdpr_en>
- EDPB lawful-processing guide: <https://www.edpb.europa.eu/sme-data-protection-guide/process-personal-data-lawfully_en>
- AZOP data-breach notification guidance: <https://azop.hr/data-breach-notifications/>

## Owner Capture Form

For each call/email, record:

```text
Contact:
Role/org:
Date:
Blocker: O01 / O02 / O03 / Q-020 / Q-021 / Q-053 / future canonical-publish decision
Demo shown: yes / no
Outcome: green / yellow / red
Key quotes:
Must-haves:
Deal-killers:
Current proof reaction:
Missing proof/production gap:
Introductions offered:
Follow-up promised:
Owner decision needed:
Recommended next doc/code action:
Decision impact:
```

Paste completed notes into `SESSION-LOG.md` or a future `50-business/validation-notes.md` before closing blockers.
