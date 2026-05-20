# Current Proof Demo Brief

Status: active for VPS-current owner validation.
Last reviewed: 2026-05-20.

## Purpose

Use this brief for owner-led validation calls where the goal is to learn whether
the buyer would use, recommend, fund, or pilot the platform.

This is not public marketing copy. It is an internal walkthrough for showing
the current owner-controlled tenant-private proof without overstating
production legal/GDPR/SLA readiness or platform-shipped named-instrument rights.

For the three seeded validation tenants, use the private
[validation demo walkthrough packet](../80-agent-handoff/validation-demo-walkthrough-packet.md)
as the operator script. This brief remains the shared validation frame.

## Demo Boundary

The current proof can show a tenant-private workflow through the live VPS
staging rehearsal lane, with local/dev staging as a fallback when the owner is
working offline:

```text
registration or existing workspace sign-in
-> Auth0-backed owner session
-> CSRF-protected tenant-member app path
workspace and study creation
-> five-step study setup
-> questionnaire formats and respondent parity
-> multi-output Results setup
-> Directory CSV audience import and recipient selection
-> launch readiness
-> public respondent consent/code flow
-> save/review/submit
-> submit-time scoring
-> collection monitoring and close
-> Results widgets, CSV/codebook proof exports, and Waves comparison
-> report PDF artifact proof
-> Team, Directory, Settings, and sign-out/account recovery
-> withdrawal request/review/execute lifecycle
-> in-app operational notification proof
-> close/finality labels
-> target backup/restore, redeploy, and rollback/restore evidence
```

Say this plainly:

> This is owner-controlled staging proof with synthetic, seed, demo, or
> owner-controlled test data only. It is not approval for real-person production
> data, not final legal/GDPR/DPA copy, not an SLA or managed-hosting claim, not
> outbound operational-email readiness, and not a platform-shipped canonical
> instrument library.

Do not show or imply public item text or official status for rights-unclear
named instruments. For any validated-scale example, say:

> This is private/tenant-attested demo content, not a platform-shipped canonical
> preset or official platform-granted instrument.

## What To Show

Use the latest owner-runnable VPS evidence or a freshly created local proof
series. The exact route ids may change; show the workflow, not a fixed dataset.
For seeded validation tenants, first select the correct validation tenant and
use the role-slot guidance in the walkthrough packet.

| Step | Show | Validation question |
|---|---|---|
| Trust posture | Live VPS staging domains, Auth0-backed owner session, CSRF-protected app mutations, and safe evidence boundaries. | Is this enough for a proof/demo conversation before real-person legal review? |
| Problem frame | The platform is for repeatable validated measurement, not one-off forms. | Is this a real pain in your workflow? |
| Registration and workspace | Create or sign in to a workspace, recover from wrong Auth0 account, and enter the private app surface. | Would this account/workspace model be acceptable for a private pilot? |
| Study setup | Create a study, move through the five-step setup path, define questionnaire formats, and create one or more Results outputs. | Which setup fields, formats, and subscales must exist for your study/client? |
| Audience and launch | Import or prepare people/groups, choose recipients in plain language, and run launch readiness. | What would make launch scientifically, ethically, or operationally unacceptable? |
| Respondent flow | Public respondent entry, consent, participant code when longitudinal, answer save/review/submit. | Would respondents understand and trust this flow? |
| Scoring | Submit-time score materialization, multiple result outputs, missing-answer policy, and score provenance. | What scoring details must be visible for trust? |
| Results and waves | Results widgets, preliminary/final state, CSV download, wave comparison, score metadata, and finality labels. | Which result view would you need before using it? |
| Exports and PDFs | CSV/codebook artifacts, browser CSV download, report PDF artifact proof, signed-download/local-storage behavior, and artifact provenance. | Is CSV/codebook/PDF enough now, or do you need SPSS/other formats? |
| Withdrawal and auditability | Anonymous withdrawal request, tenant review, approve/execute, artifact invalidation/regeneration, and in-app terminal notifications. | What data-subject or audit workflow would your ethics/DPO path require? |
| Team, directory, and settings | Team member preparation, Directory people/groups/import, workspace settings shortcuts, and sign-out/wrong-account recovery. | What administration must exist before a pilot? |
| Close/finality | Closed-wave labels and continued result/export/PDF readability. | What lifecycle states matter in your study/client work? |
| Rehearsal evidence | VPS backup/restore, release evidence, redeploy, rollback/restore, and authenticated product-spine proof. | What deployment proof would your institution/client need before a real pilot? |

## What Not To Claim

Do not claim:

- paid production launch, real-person production data approval, SLA readiness,
  or managed-hosting readiness;
- final DPA/privacy notice/GDPR legal signoff;
- outbound operational-notification email routing, requester/admin email
  workflows, notification preferences, SMTP-backed operational alerting, or
  claims that operational events are emailed;
- platform-shipped named-instrument presets, public item text for
  rights-unclear instruments, or official score labels;
- validated interpretation, norms, thresholds, or clinical/OSH advice;
- production SMTP deliverability, object-storage/S3 production credentials,
  async export worker scale, or download-audit completeness;
- full offline/PWA sync, public score feedback, re-consent, or a final
  production role/permission taxonomy;
- that the shown flow is the final UX, final PDF reporting product, or final
  multi-workspace account model.

## Questions To Capture

Every validation call should capture:

- real study, course, client, or campaign they would run in the next 3-6 months;
- required instruments and who has rights to use them;
- required response identity mode: anonymous, anonymous longitudinal, or identified;
- required audience import/selection workflow;
- required questionnaire formats, subscales, scoring rules, and missing-answer policy;
- consent, ethics, DPO, procurement, or client approval path;
- report/export format required for the work to be valuable;
- minimum acceptable production posture;
- minimum acceptable hosting, backup/restore, auth, and legal evidence for a
  real pilot;
- willingness to pilot, refer, cite, fund, or pay;
- objections that would kill adoption.

## Persona Emphasis

| Persona | Emphasize | Do not over-focus on |
|---|---|---|
| O01/P1 prof lead | Research workflow, anonymous longitudinal linking, ethics/consent, export/codebook, grant credibility. | Consultant white-label PDFs. |
| O02 academic validators | Whether P1 pain repeats across labs and institutions. | One person's preferred instrument only. |
| O03 OSH consultants | Repeatable client delivery, white-label reporting need, Croatian-language/compliance requirements, pricing tolerance. | Academic grant language. |

## Close Ask

End each call with one concrete ask:

- name a real study/client/campaign where this would be used;
- introduce two more validators;
- send one sample report/export expectation;
- confirm the smallest pilot that would prove value;
- state the blocker that must be solved before a pilot.

Record notes in [`../80-agent-handoff/OWNER-BLOCKERS-ACTION-PACK.md`](../80-agent-handoff/OWNER-BLOCKERS-ACTION-PACK.md#owner-capture-form) format.
