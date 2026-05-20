# D365 - Post-private-beta-hardening zoom-out assessment - 2026-05-20

Status: Current assessment.

## Scope

Assess the app after the D350 follow-up run and the later auth, mobile, setup, collection, results, waves, settings, and sidebar hardening work through D364.

Sources reviewed:

- `docs/v2/60-roadmap/current-roadmap.md`
- `docs/v2/60-roadmap/milestones.md`
- `docs/v2/60-roadmap/risks.md`
- `docs/v2/30-features/custom-study-builder.md`
- `docs/v2/50-business/current-proof-demo-brief.md`
- `docs/v2/80-agent-handoff/OPEN-QUESTIONS.md`
- `docs/v2/80-agent-handoff/private-beta-acceptance-checklist.md`
- `docs/v2/80-agent-handoff/assessments/D350-PROJECT-STATE-ROADMAP-ASSESSMENT-2026-05-20.md`

## Executive finding

The product has moved from "private-beta shell with obvious UX holes" to "owner-controlled private-beta rehearsal candidate."

That does not mean production-ready. It means the app is now coherent enough to show as the current proof in owner-led validation calls, as long as the owner says the limits clearly: synthetic or owner-controlled test data only, no final GDPR/DPA claim, no platform-shipped named instruments, no official norms/interpretation, no production SMTP or operational-email promise, and no multi-workspace account model.

The main risk has changed. Earlier, the biggest risk was that a researcher could not complete the path without running into fake panels, broken auth, or confusing setup/collection/results/waves semantics. After the recent work, the larger risk is repeating the v1 failure mode: continuing to improve the app without external validation.

## What changed since D350

| Area | D350 state | Current state | Assessment |
|---|---|---|---|
| Auth/onboarding | Working but recently painful. | Registration, sign-in, wrong-account guard, email verification recovery, and sign-out recovery have been hardened and deployed. | Good enough for owner-controlled beta, intentionally one-email-one-workspace. |
| Mobile shell | Not yet solved. | Public/auth/app mobile navigation and bottom nav were redesigned and deployed. | Good enough for walkthrough; route-by-route mobile polish remains later. |
| Questionnaire | Exposed formats exceeded verified respondent runtime. | Respondent-format parity for exposed beta formats was implemented. | The exposed format promise is much safer, but advanced authoring remains out of scope. |
| Results setup | One score output was too narrow. | Multi-output dimensions/subscales exist for rating/recommendation/number answers. | Strong beta improvement; interpretation bands/norms remain future. |
| Audience setup | Manual/dense respondent-rule UX. | CSV audience import, recipient selection language, preview/save, and roster UX exist. | Credible for a small beta study; import store proof and data-quality edges still matter. |
| Setup hub | Scrolling/debug-feeling setup path. | Five-step setup path is action-oriented and less technical. | Usable for validation. |
| Collection hub | Overwhelming and unclear. | Pre-launch/start/monitor/close flow is more action-first. | Usable for validation, delivery/retry edges still thin. |
| Results hub | Too much expanding proof detail. | Widget-led Results, CSV download, export/readiness wording, and settings/sidebar cleanup shipped. | Good enough to show proof; analytics meaning remains shallow. |
| Waves | Confusing after multiple waves. | D362/D363/D364 separated next-wave setup, locked historical waves, group trend, and linked change. | Conceptually safer; backend group-trend proof/chart remains future. |
| Deployment | VPS staging proof existed. | Current public staging checks and VPS release checks keep passing after product slices. | Strong owner-controlled rehearsal lane. |

## Current product state

There are now three different bars. Mixing them causes bad decisions.

| Bar | State | Meaning |
|---|---|---|
| Engineering proof spine | Strong | Core mechanics exist: tenant scope, auth, study setup, launch, respondent flow, scoring, reports, exports, waves, backup/restore, redeploy proof. |
| Owner-controlled private-beta validation | Ready with caveats | The owner can show the app to validators using synthetic, seed, demo, or owner-controlled test data and capture useful objections. |
| Real production / paid real-person use | Not ready | Q-053, legal entity/path, DPA/privacy notice, retention specifics, sub-processors, operational email policy, production support, and buyer-specific deployment posture are not closed. |

## M1 alignment

| M1 dimension | Current state | Gap |
|---|---|---|
| Tenant-private instrument lifecycle | Tenant-private setup exists and avoids platform-canonical claims. | Derivative lifecycle states/review wording remain thinner than final M1 language. |
| Questionnaire setup | Exposed beta formats render and submit. | No sections/pages, matrix, branching, piping, quotas, randomization, file upload, or advanced formulas. |
| Scoring | Multi-output numeric/rating/recommendation scoring exists. | No choice scoring, ranking scoring, text coding, reliability metrics, norms, or official interpretation. |
| Response identity modes | Anonymous, anonymous invite-only, anonymous longitudinal, and identified foundations exist. | Anonymous longitudinal invitations remain intentionally unsupported until participant-code/invitation semantics are designed together. |
| Waves | Linked change exists for repeat-participation waves; anonymous waves now have group-trend guidance. | Group trend is UI/read-model guidance, not a dedicated backend aggregate comparison endpoint/chart. |
| Reports/exports | Results widgets, CSV/codebook proof, browser download, finality labels, and PDF artifact proof exist. | SPSS/Stata, polished PDF product, final export package, and download-audit completeness are later. |
| Team/directory | Team access and people/groups/import exist. | Invite lifecycle, email invitations for team, merge workflow, and deeper data-quality handling remain later. |
| Legal/compliance | Compliance mechanics and docs exist. | Q-053 still blocks real-person production data and external legal/GDPR claims. |

## Product readiness score

Scale: 0 absent, 5 ready for owner-controlled private-beta validation without owner rescue.

| Area | Score | Rationale |
|---|---:|---|
| Auth and onboarding | 4 | Flow is stable enough on staging; multi-workspace remains intentionally out. |
| Landing/register/sign-in | 3 | Works and mobile is improved; copy still needs validation feedback. |
| Study setup | 4 | Researcher-facing five-step path exists and is no longer obviously proof-only. |
| Questionnaire builder | 3 | Exposed formats are safer; advanced authoring is deliberately absent. |
| Results/scoring setup | 3 | Multi-output setup is credible; interpretation/norms/reliability are absent. |
| Audience/directory | 3 | CSV/import and recipient selection exist; large messy real directories will expose edges. |
| Collection | 3 | Launch/monitor/close path is usable; delivery failure/retry UX is still thin. |
| Respondent runtime | 3 | Core flow works; mobile and every unusual authored format still deserve real walkthrough proof. |
| Results/exports | 3 | CSV/codebook and widgets are useful; PDF/export polish is not final. |
| Waves | 3 | Concepts are now honest; analytics depth is shallow for group trend. |
| Team/access | 3 | Basic team path exists; invite lifecycle and email remain future. |
| Settings/admin | 2 | Honest but thin. Acceptable for beta, not a full admin center. |
| Deployment/ops | 4 | VPS staging evidence is strong for validation; production operations are not complete. |
| Business validation | 1 | O01/O02/O03 remain the existential blocker. |

## Most important risks now

### 1. Building ahead of validation

The app is now good enough to learn from. Continuing broad feature/UI hammering without O01/O02/O03 feedback is the highest strategic risk.

### 2. Docs drift

Some product docs still describe older limits, such as one-output Results setup or unverified respondent parity. That can cause future agents to reopen solved work or underclaim current capability.

### 3. The app still looks stronger than its legal/commercial posture

The workflow is increasingly production-shaped, but Q-053 and Q-054 still block real-person production use, final legal/GDPR claims, and operational-notification email claims.

### 4. The builder could drift into SurveyMonkey competition

The product should use generic survey tools as a usability bar, not become a broad commodity form builder. The wedge remains governed, scored, repeatable studies with provenance, identity modes, disclosure, exports, and wave history.

### 5. Analytics meaning is still shallow

Multi-output scores and Waves clarity help, but the product still lacks interpretation bands, norms, reliability checks, and a true backend group-trend summary. That is acceptable for validation if stated clearly.

## Recommended posture

Stop broad polishing by default.

The default next move should be a private-beta validation rehearsal, not another feature sprint. Use the current staging app to run the exact owner demo path end to end, update stale docs/copy that would confuse the call, then run O01/O02/O03 conversations.

Engineering should continue only in one of three cases:

| Case | Action |
|---|---|
| Validation rehearsal exposes a blocker | Fix the blocker narrowly. |
| Validator feedback identifies a must-have | Convert it into a slice and reassess. |
| Owner needs a cleaner demo script | Update docs/walkthroughs, not broad product code. |

## Recommended next slices

| Order | Slice | Why |
|---:|---|---|
| 1 | D366 private-beta validation rehearsal and docs-drift closure | Run the current path as a validator would see it, update stale product docs/checklist, and produce a final owner-run walkthrough. |
| 2 | O01/O02/O03 owner validation calls | This is the actual business risk. |
| 3 | RSLT02 tenant-attested interpretation bands | Only if validators say raw outputs are not enough to judge usefulness. |
| 4 | WAV03 backend group-trend proof/chart | Only if validators care about anonymous wave-to-wave group movement beyond Results navigation. |
| 5 | BUILDER02 sections/pages and questionnaire organization | Only if real study setup feels too flat before branching or matrix questions. |

## What not to do next

- Do not resume platform-canonical named instruments without a new owner/legal canonical-publish decision.
- Do not build SurveyMonkey-class breadth before validation.
- Do not treat anonymous invite-only as cryptographic unlinkability.
- Do not add multi-workspace account switching before a beta user actually needs it.
- Do not invest in production SMTP/operational-email routing until Q-054 is decided.
- Do not treat the VPS staging rehearsal as production legal/GDPR readiness.

## Decision

D366 should be the next agent-executable slice if the owner wants one more agent pass before calls. It should be a rehearsal/docs-drift slice, not a feature slice.

Owner-only validation remains the highest-priority project work.
