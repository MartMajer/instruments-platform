# Custom Study Builder

Status: Proposed product boundary for the current MVP.

## Purpose

The custom study builder is not a generic survey-builder clone. Generic survey tools are stronger at broad form authoring, templates, branching, and commodity collection. This product should use survey-authoring quality as a usability bar, but the product category is governed, scored, repeatable research studies.

The builder must answer four user questions:

- What are we measuring?
- What will respondents answer?
- How do answers become results?
- What campaign, wave, hierarchy, report, and export evidence comes out?

## Current MVP model

The selected-study setup flow has five ordered steps:

- Instrument: confirm the study instrument shell and tenant-attested ownership boundary.
- Questionnaire: build the respondent questions for the study.
- Results setup: choose which numeric/rating answers become a score and how missing answers are handled.
- Collection setup: name the collection wave and choose how respondents will answer.
- Launch check: verify prerequisites before collection starts.

## Response identity and distribution

🟡 Proposed by [ADR-0013](../70-decisions/0013-anonymous-invite-only-distribution.md): selected audience and answer identity are separate concerns.

Response modes mean:

- Anonymous: answers are treated as anonymous in product reports and exports. The wave can use a general respondent link or saved audience invitations.
- Anonymous with repeat participation: respondents enter their own repeat-participation code. Saved audience invitations are not supported until participant-code and invitation semantics are designed together.
- Identified invite-only: answers remain connected to named respondents for workflows that require identified response handling.

Saved audience rules answer "who receives access." They do not automatically make answers identified. For anonymous invite-only waves, launch creates anonymous invitation assignments and email notifications for selected recipients. The platform still has operational delivery metadata for sending, retry, audit, and withdrawal workflows; this is not cryptographic unlinkability.

The Questionnaire step currently exposes these answer formats:

- Rating scale with custom min, max, and endpoint labels.
- Recommendation scale from 0 to 10.
- Single choice.
- Multiple choice.
- Number.
- Text.
- Date.
- Ranking.

The Results setup step currently supports one score output:

- Average selected answers.
- Sum selected answers.
- Include rating, recommendation, and number questions.
- Require every selected answer, or allow a score after a minimum number of selected answers.
- Mark rating/recommendation questions as reverse-scored.

Choice, text, date, and ranking answers are collected but not scored by the current visual setup step.

## Differentiation from generic survey tools

The product should win by making research-study mechanics first-class:

- Versioned instruments, questionnaires, scoring rules, campaigns, and launch snapshots.
- Tenant-owned/private instruments with explicit rights and provenance boundaries.
- Scoring semantics that are inspectable and reproducible.
- Missing-data policy, reverse-coding, output names, and report/export meaning tied to a frozen setup.
- Campaign series, waves, and future longitudinal comparison.
- Subject hierarchy and role-aware collection rules.
- Consent, retention, disclosure, access, audit, export, and proof evidence around the study.

If a user only needs a one-off form with branching and commodity analytics, a generic survey platform is a better fit. The intended wedge is a researcher or organization that needs a scored study workflow they can defend later.

## Explicit current limits

Do not claim these capabilities from the current visual builder:

- Full SurveyMonkey-class generic survey authoring.
- Branching, skip logic, piping, quotas, randomized blocks, or advanced page logic.
- Matrix/grid authoring.
- File upload question support in the builder.
- Multi-section or multi-page questionnaire design.
- Choice scoring, ranking scoring, text coding, or custom formulas.
- Multiple dimensions/subscales from the visual Results setup.
- Interpretation bands, norms, percentiles, reliability metrics, or clinical/validated cutoffs.
- Canonical named-instrument publishing or platform-granted rights.
- Full respondent-rendering parity for every authored question type until verified end to end.

These limits are product boundaries, not hidden technical excuses. When a limit matters to a beta user, create a focused slice instead of widening the builder silently.

## Next product slices

The next high-leverage slices are:

- Multi-output Results setup for dimensions/subscales.
- Interpretation bands with explicit tenant-attested/non-official wording.
- Respondent-rendering parity proof for every exposed question type.
- Page/section grouping before advanced branching.
- Choice scoring only after the scoring document model and reports can explain it clearly.
