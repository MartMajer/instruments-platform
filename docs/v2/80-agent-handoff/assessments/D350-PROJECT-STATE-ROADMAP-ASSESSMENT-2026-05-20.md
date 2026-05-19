# D350 - Project State Roadmap Assessment - 2026-05-20

Status: Current assessment.

## Scope

Assess the current application state against the active roadmap, backlog, milestones, owner blockers, open questions, and recent shipped staging work.

Sources reviewed:

- `docs/v2/60-roadmap/current-roadmap.md`
- `docs/v2/60-roadmap/milestones.md`
- `docs/v2/60-roadmap/backlog.md`
- `docs/v2/60-roadmap/risks.md`
- `docs/v2/30-features/custom-study-builder.md`
- `docs/v2/50-business/current-proof-demo-brief.md`
- `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- `docs/v2/80-agent-handoff/NEXT-SESSION-PLAN.md`
- `docs/v2/80-agent-handoff/OPEN-QUESTIONS.md`
- `docs/v2/80-agent-handoff/OWNER-BLOCKERS-ACTION-PACK.md`
- Recent `SESSION-LOG.md` entries through the 2026-05-19 sidebar cleanup deployment.

## Executive finding

The project has crossed from "technical proof spine" into "private-beta product shell." That is real progress.

The active roadmap still correctly says the product is a tenant-private validated-instrument workflow, not a platform-shipped named-instrument catalog. That guardrail should stay.

The stale part is the handoff queue posture. `NEXT-ACTIONS.md` and `NEXT-SESSION-PLAN.md` still frame the default state as post-D348 auth/onboarding pause, but the app has since shipped substantial product work: landing/registration/sign-in hardening, Auth0 session recovery, setup builder redesign, anonymous invite-only audience distribution, collection/results/waves action-first flows, CSV browser download, settings cleanup, and broad sidebar cleanup.

The next limiter is not another generic proof-spine backend primitive. It is M1 private-beta acceptance quality:

- Can a researcher create a real tenant-private study without the owner explaining every step?
- Do all exposed questionnaire answer formats render and submit correctly for respondents?
- Can the researcher import or prepare an audience without manual record-by-record work?
- Can the researcher define useful result dimensions, not just one average or sum?
- Can the owner show the app to validators and capture useful green/yellow/red evidence?

## Milestone alignment

| M1 requirement | Current state | Assessment | Next action |
|---|---|---|---|
| Tenant and auth | Auth0-backed registration/sign-in, workspace owner creation, email verification recovery, sign-out/switch-account recovery, and team first-sign-in are implemented and deployed. | Strong enough for beta, but intentionally one-email-one-workspace. | Keep Q-056/Q-057 open. Do not add multi-workspace until beta needs it. |
| Tenant-provided instrument setup | Setup now exposes instrument shell, questionnaire, results setup, collection setup, and launch check in researcher language. | Usable spine exists. It is not yet a mature builder. | Prove respondent-rendering parity for every exposed question type. |
| Questionnaire authoring | Rating, recommendation, single choice, multiple choice, number, text, date, and ranking are visible in the builder. | Product promise has expanded faster than verified respondent parity. | Make parity proof the next product-quality gate. |
| Scoring/results setup | One visual score output supports average/sum over numeric/rating/recommendation answers with missing-answer policy and reverse scoring. Backend scoring engine is stronger than the visual builder. | Enough for a narrow beta study. Too narrow for serious multi-dimensional instruments. | Add multi-output dimensions/subscales before M2/M3 breadth. |
| Response identity modes | Backend and product flows support identified, anonymous, and anonymous-longitudinal foundations. Anonymous invite-only distribution was explicitly designed and implemented. | Strategically correct. This is a real differentiator versus generic survey builders. | Improve audience-builder UX and document the non-cryptographic delivery metadata boundary clearly. |
| Audience and directory | Directory supports people, groups, memberships, and manager links manually. | Model is good, UX is not scalable. Manual entry will hurt the first real study. | Add CSV audience import and a researcher-facing audience builder. |
| Consent, retention, disclosure | Consent, retention/disclosure policy gates, launch readiness, withdrawal lifecycle, and k-anonymity-safe report/export paths exist in the proof spine. | Strong backend proof. Legal production use remains blocked. | Keep Q-053 as launch/legal gate; do not claim production GDPR readiness. |
| Collection | Collection hub now starts with pre-launch/start/monitor/close actions and hands off to Results. | Good enough for beta walkthrough. | Add clearer delivery failure/retry visibility only after audience/import friction is reduced. |
| Reports, waves, exports | Results and Waves are action-first, widgets are preserved, CSV download works, export refresh state is fixed, wave comparison exists. | Strong proof and improving UX. Analytics usefulness is still shallow. | Improve result dimensions and interpretation before adding more chart chrome. |
| Team and roles | Team members can be prepared with roles and first sign-in links. | Beta-capable. | Polish pending-member invitation copy and invite email later. |
| Settings and sidebar shell | Remaining sidebar surfaces were cleaned; Settings is honest that workspace-level settings are still thin. | Acceptable for beta. | Do not spend more on shell polish until core study flow is verified. |
| Deployment | VPS staging, Auth0, backup/restore, redeploy, rollback evidence, and release checks exist. Latest staging public checks pass. | Strong enough for owner-controlled validation. | Keep DEP01-C deferred unless deployment repetition becomes the bottleneck. |
| External validation | O01/O02/O03 remain owner-only. | This is still the existential blocker. | Run validation calls with the improved app, not after another broad build sprint. |

## Product readiness score

Scale: 0 means absent, 5 means private-beta ready without owner explanation.

| Area | Score | Rationale |
|---|---:|---|
| Auth and onboarding | 4 | Flow works on staging after hardening. Multi-workspace remains intentionally out. |
| Landing/register/sign-in | 3 | Works and is less confusing, but still needs product-market copy after validator feedback. |
| Study setup | 3 | Researcher-facing wizard exists. Questionnaire/results setup are still constrained. |
| Questionnaire builder | 2 | Visual surface exists, but parity and richer structure are not proven. |
| Results/scoring setup | 2 | One score output is useful but not enough for most validated scales with dimensions. |
| Audience/directory | 2 | Data model exists; manual UX will not scale to a normal study. |
| Collection | 3 | Action-first launch/monitor/close path is credible. Audience and delivery edges remain thin. |
| Respondent runtime | 3 | Strong proof spine; exposed new question formats need parity verification. |
| Reports/results | 3 | Widgets, exports, waves, finality, and CSV exist. Interpretability remains limited. |
| Exports | 3 | CSV/codebook and browser download exist. SPSS/Stata and polished export package are later. |
| Team/access | 3 | Basic team management exists. Invite lifecycle and account picker are future work. |
| Settings/admin | 2 | Useful shortcuts and honest details; not a real settings center yet. |
| Deployment/ops | 4 | VPS staging proof is strong for validation. Production legal/SMTP/object-store/CI remain bounded. |
| Business validation | 1 | Owner conversations remain the highest-risk open lane. |

## Key gaps

### 1. The app can now promise more than it has verified

The builder exposes multiple answer formats, but the docs still say full respondent-rendering parity is not proven. That is the highest product-quality risk because it can turn a beta walkthrough into a trust failure quickly.

### 2. The next real study will need audience import

Manual people/groups/membership creation is fine for a proof tenant. It is not fine for a normal study with 30, 100, or 500 respondents. A CSV import MVP is likely higher leverage than another visual polish pass.

### 3. One score output is too narrow for validated instruments

Many validated studies need subscales/dimensions. The current Results setup is honest, but it will feel incomplete to a researcher as soon as they try a real instrument.

### 4. External validation is still the business bottleneck

The owner blocker packet remains right. The project can continue engineering with synthetic/owner-controlled data, but O01/O02/O03 must happen before treating M1 as market-validated.

### 5. The queue is stale

The active handoff queue says no default agent-executable work after D348. That is no longer true after the owner explicitly pulled the app through auth, landing, setup, collection, results, waves, settings, and sidebar cleanup.

## What not to do next

- Do not restart platform-canonical named-instrument content. The active lane remains tenant-private/attested content.
- Do not jump to M2 COPSOQ/UWES or M3 SurveyJS Creator breadth before the current builder is beta-coherent.
- Do not spend another broad pass on shell aesthetics before the exposed study flow is verified end to end.
- Do not treat Q-053 as closed. Real participant/student/employee production data remains blocked.
- Do not build multi-workspace account switching unless beta users actually need multiple workspaces.
- Do not reopen CI/deployment automation unless manual deployment cost becomes the bottleneck.

## Recommended next 10 slices

These are ordered by private-beta impact, not by implementation glamour.

| Order | ID | Slice | Why now |
|---:|---|---|---|
| 1 | BR01 | M1 private-beta acceptance checklist and route audit | Define the exact browser path that must work before owner validation calls use the app as current proof. |
| 2 | QB01 | Respondent-rendering parity proof for exposed question formats | The builder exposes formats that must render, save, submit, export, and report predictably. |
| 3 | DIR04 | CSV audience import MVP for people and groups | Manual directory entry is the next real-study usability wall. |
| 4 | AUD01 | Researcher-facing audience builder for collection setup | Saved audience rules need to feel like choosing recipients, not manipulating resolver records. |
| 5 | RSLT01 | Multi-output Results setup for dimensions/subscales | One average/sum output is not enough for most validated instruments. |
| 6 | RSLT02 | Tenant-attested interpretation bands | Makes reports more meaningful while preserving non-official wording. |
| 7 | TEAM04 | Pending member invitation lifecycle polish | Team first sign-in exists; owner-visible invitation state and copy can be cleaner. |
| 8 | EXP19 | Export package polish around CSV/codebook/PDF availability | Exports exist; beta users need clearer file readiness and purpose. |
| 9 | VAL08 | Refresh validation demo packet after product UI changes | The owner script should match the current app, not the older proof wording. |
| 10 | D351 | Post-BR01 reassessment | Reassess after the acceptance checklist exposes the strongest actual blocker. |

## Course decision

Select BR01 next.

BR01 should not be another feature. It should define and exercise the beta acceptance path:

1. Register or sign in to a workspace.
2. Create a study.
3. Build a questionnaire using each currently exposed answer format.
4. Configure at least one result output.
5. Create or import a small audience.
6. Launch an anonymous invite-only or anonymous open-link wave.
7. Complete respondent flow.
8. Confirm scoring/results/export/waves behavior.
9. Record every gap as either blocker, beta-known-limit, or later backlog.

If BR01 is too broad for one slice, split it into checklist documentation first, then respondent parity proof (QB01) second.

## Updated blocker posture

Owner-only blockers remain:

- O01/Q-004: prof lead validation.
- O02: secondary academic validators.
- O03/Q-005: OSH consultants.
- Q-053: legal/GDPR path before real-person production data.

Agent-executable work is not exhausted. It should now be disciplined around M1 private-beta acceptance, not broad feature invention.
