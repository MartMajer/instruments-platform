# M1 Private-Beta Acceptance Checklist

Status: BR01 baseline plus QB01, DIR04 local implementation, AUD01 recipient-selection UX, RSLT01 multi-output Results setup, and VAL08 validation-packet refresh.
Last reviewed: 2026-05-20.

## Purpose

This checklist defines the browser-level acceptance bar for using the current app in owner-led validation calls.

The app is not production-ready for real-person participant/student/employee data while Q-053 remains open. This bar is for synthetic, seed, demo, or owner-controlled test data.

## Classification

Each row uses one of these classifications:

| Class | Meaning |
|---|---|
| Required | Must work before the current app is used as the main validation walkthrough. |
| Beta known limit | Acceptable if stated clearly during validation. |
| Later backlog | Not required for M1 private-beta validation. |
| Blocker | Stops the walkthrough until fixed. |

## Acceptance route

| Area | Acceptance path | Class | Current expected state |
|---|---|---|---|
| Landing | Visitor understands that this is a governed study platform, not a generic survey toy. | Required | Landing is improved, but copy should be refreshed after validation feedback. |
| Registration | New owner can create an account, verify email when prompted, create a workspace, and enter the app. | Required | Implemented on staging after Auth0 hardening. |
| Existing sign-in | Existing workspace user can sign in through `/signin` without being trapped by stale Auth0 sessions. | Required | Implemented with switch/sign-out recovery. |
| Account boundary | One normalized email maps to one active workspace membership for beta. | Beta known limit | Q-057 keeps multi-workspace picker future. |
| Study creation | Researcher creates a study from `/app/campaign-series` with a normal name. | Required | Implemented. |
| Setup navigation | Researcher sees a five-step setup path and can move previous/next without scrolling through debug panels. | Required | Implemented after setup cleanup. |
| Instrument step | Researcher gets a tenant-private instrument shell without raw IDs or fake validation claims. | Required | Implemented at UI level. |
| Questionnaire step | Researcher can add questions with rating, recommendation, single choice, multiple choice, number, text, date, and ranking formats. | Required | QB01 added respondent renderer parity for these exposed beta formats. |
| Results setup | Researcher can define one or more scores from rating/recommendation/number answers. | Required | RSLT01 implemented default total score plus multiple named outputs with per-output question selection, calculation, and missing-answer policy. |
| Results dimensions | Researcher can define multiple dimensions/subscales. | Required | RSLT01 implemented multi-output result setup for custom-study dimensions/subscales. |
| Collection setup | Researcher can create a wave and choose response mode. | Required | Implemented. |
| Audience setup | Researcher can choose who receives the study in understandable language. | Required | AUD01 reworked setup into plain-language recipient selection, preview, save, and invitation roster. |
| Audience import | Researcher can import a normal study audience without manual record-by-record entry. | Required for realistic beta | DIR04 added CSV import locally; Docker-backed store proof must run before deployment. |
| Launch check | Researcher can run pre-launch check and understand how to unblock collection. | Required | Implemented after collection readiness cleanup. |
| Anonymous open-link collection | Researcher can launch an anonymous open-link wave and collect a response. | Required | Supported by proof spine. |
| Anonymous invite-only collection | Researcher can invite a selected audience while keeping answers anonymous in reports/exports. | Required | Implemented with recipient-selection UX; Directory data quality still matters. |
| Anonymous longitudinal | Respondent can use participant code across waves. | Required for M1 target | Supported by proof spine; not necessarily first beta walkthrough path. |
| Identified collection | Identified entry works for identified campaigns. | Beta known limit | Foundation exists; full resolver depth remains future. |
| Respondent completion | Respondent can consent, answer, review, submit, and see completion/receipt state. | Required | Proof spine exists; QB01 covers exposed beta-format rendering and answer normalization. |
| Submit-time scoring | Submitted responses produce scores when scoring is configured. | Required | Implemented. |
| Collection close | Researcher can close collection and still review/export data. | Required | Implemented. |
| Results review | Researcher can see widgets and understand preliminary/final state. | Required | Implemented with widget-led Results cleanup. |
| CSV download | Researcher can download selected CSV in browser. | Required | Implemented. |
| Codebook/provenance | Export includes enough provenance to defend the study mechanics. | Required | Proof exists; export package polish remains later. |
| PDF artifact | Report PDF artifact proof can be shown as engineering proof. | Beta known limit | Do not claim final PDF reporting product. |
| Waves | Researcher can compare compatible waves where data exists. | Required for M1 target | Implemented proof path. |
| Team | Owner can prepare team member access and share first sign-in link. | Required | Implemented. |
| Directory | Researcher can manage people, groups, memberships, and manager links. | Required | Implemented manually; import MVP selected. |
| Settings | Workspace settings page is honest and does not duplicate the sidebar as a fake admin center. | Beta known limit | Cleaned but intentionally thin. |
| Sign out | User can sign out completely enough to switch accounts. | Required | Implemented after auth hardening. |
| Deployment | Staging app responds on public API, web root, and `/app`; release checks pass at stable checkpoints. | Required before owner validation | Last verified after sidebar cleanup. |

## Blockers selected by BR01

| Rank | ID | Blocker | Reason |
|---:|---|---|---|
| 1 | VAL08 | Validation packet refresh | Owner walkthrough must match current UI and limits. |

## Known limits to say out loud

- This is owner-controlled private-beta proof, not real-person production legal approval.
- One email maps to one workspace membership for beta.
- Platform-canonical named instruments are not included.
- Interpretation bands, norms, validated thresholds, SPSS/Stata export, branching, matrix questions, and production SMTP claims are not part of this acceptance bar.
- Anonymous invite-only distribution has operational delivery metadata; it is not cryptographic unlinkability.
