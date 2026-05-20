# Private Beta Validation Rehearsal

Status: D366 owner-run rehearsal script.
Last reviewed: 2026-05-20.

## Purpose

Run this once before O01/O02/O03 calls. The goal is not to prove every feature again; it is to confirm the current staging app can support a 20-minute validator conversation without the owner improvising the flow.

If this rehearsal finds a blocker, fix only that blocker. Do not restart broad UI polishing.

## Hard boundary to say first

Use this exact framing:

```text
This is owner-controlled staging proof with synthetic, seed, demo, or owner-controlled test data only. It is not approval for real participant/student/employee data, not final GDPR/DPA/legal copy, not a production SLA claim, not operational email readiness, and not a platform-shipped canonical instrument library.
```

## Rehearsal preflight

| Check | Pass condition | If it fails |
|---|---|---|
| Staging public app | `https://validatedscale-staging.croat.dev` opens. | Use local/dev fallback or postpone the call. |
| Staging API | `https://validatedscale-api-staging.croat.dev/health` returns healthy. | Do not demo live staging until deployment/runtime is fixed. |
| Owner account | Owner can sign in through `/signin`. | Use sign out completely/wrong-account recovery; do not debug in front of validator. |
| Test data | Workspace/study/audience uses synthetic or owner-controlled data only. | Stop and replace the data before any call. |
| Email verification | New account path does not leave the owner stuck after verification. | Use an already verified account for the call. |
| Selected study | At least one study exists with setup, collection, results, and waves data. | Create a small rehearsal study before the call. |
| CSV export | One CSV download path works in browser. | Treat as a call blocker if the validator cares about exports. |
| Waves story | Anonymous waves show group trend; repeat-participation waves show linked change when available. | Explain the limit or fix wording before O01. |

## 20-minute walkthrough script

| Minute | Show | Say | Decide from reaction |
|---:|---|---|---|
| 0 | Landing or `/signin` | This is for governed scored studies, not generic one-off forms. | Do they understand the category? |
| 2 | Workspace home and Studies | Work happens inside a tenant workspace; one email maps to one workspace for beta. | Is account/workspace model acceptable? |
| 4 | Create or open study | A study contains setup, collection waves, results, exports, and wave history. | Can they name a real study/client/course? |
| 6 | Setup: instrument and questionnaire | Tenant provides or attests content rights; questionnaire formats are beta-supported. | Which formats/instruments are mandatory? |
| 8 | Setup: Results outputs | Numeric/rating/recommendation answers can become one or more dimensions/subscales. | What scoring and missing-data rules matter? |
| 10 | Directory/audience | People/groups can be prepared or imported, then selected as recipients. | Would their real audience fit this model? |
| 12 | Collection | Launch readiness, recipient selection, respondent access, monitoring, and close are visible. | What launch/ethics blocker would stop them? |
| 14 | Respondent flow | Consent, answers, save/review/submit, and repeat-participation code where relevant. | Would respondents trust and complete it? |
| 16 | Results and CSV | Widgets, finality labels, score metadata, CSV/codebook download. | Is this enough, or do they require SPSS/Stata/PDF? |
| 18 | Waves | Anonymous repeated waves support group trend; repeat-participation supports linked same-respondent change. | Which longitudinal comparison do they actually need? |
| 20 | Close ask | Ask for a real study, referral, pilot condition, sample report/export, or explicit no. | Green/yellow/red. |

## Must-capture notes

Capture this immediately after the call:

```text
Contact:
Persona: O01 prof lead / O02 academic / O03 OSH consultant
Date:
Real study/client/course named:
Required instruments:
Who owns or can attest instrument rights:
Required response mode:
Required audience workflow:
Required scoring/subscales:
Required report/export:
Ethics/DPO/procurement/legal blocker:
Deployment/security concern:
Top missing feature:
Would use: yes / maybe / no
Would refer: yes / maybe / no
Would pay/fund/include in grant: yes / maybe / no
Green/yellow/red:
Next owner action:
Next agent action, if any:
```

## Green/yellow/red criteria

| Result | Meaning | Next move |
|---|---|---|
| Green | Validator names a real use case and a concrete next action: pilot, referral, grant, sample export, or follow-up with decision-maker. | Prioritize the named blocker only. |
| Yellow | Validator likes it but cannot name a near-term use case, or needs one missing feature/legal artifact first. | Decide whether the missing item is worth one focused slice. |
| Red | Validator says existing tools are enough, cannot name a use case, refuses referral, or requires procurement before learning. | Do not build more for that persona until positioning changes. |

## Persona-specific emphasis

| Persona | Lead with | Avoid over-selling |
|---|---|---|
| O01 prof lead | Anonymous longitudinal studies, consent, scoring provenance, CSV/codebook, grant credibility. | OSH consultant client portfolios and white-label reports. |
| O02 academic validator | Whether the same research workflow pain exists outside the prof lead. | One person's favorite instrument. |
| O03 OSH consultant | Repeatable client delivery, audience preparation, anonymous employee response, aggregate reports, pricing tolerance. | Academic grant language. |

## Blocker rules

Treat these as real blockers before a serious call:

- Cannot sign in or enter workspace.
- Cannot create/open a study.
- Cannot complete setup enough to launch.
- Respondent flow cannot submit.
- Results do not show after submission.
- CSV download fails when exports are the call's core value.
- The app exposes real-person data, secrets, raw participant codes, invitation tokens, or customer data in the demo.

Treat these as known beta limits, not call blockers if stated clearly:

- No production legal/GDPR/DPA approval.
- No platform-shipped canonical named instruments.
- No official validated interpretation, norms, thresholds, or clinical/OSH advice.
- No SPSS/Stata export.
- No full PDF reporting product.
- No multi-workspace account picker.
- No branching, matrix questions, file uploads, or SurveyJS Creator breadth.
- Anonymous invite-only has operational delivery metadata; it is not cryptographic unlinkability.

## After rehearsal

If all blocker rules pass, run O01/O02/O03 instead of building more.

If one blocker fails, create one narrow slice. Do not reopen broad polishing unless the failure is systemic across the walkthrough.
