# D364 waves group-trend assessment - 2026-05-20

## Context

Owner testing after D362/D363 showed that two closed anonymous waves could exist, but the Waves hub still framed the next move as "Create Wave 3" and kept the only comparison path named like a general wave comparison. That was misleading because anonymous waves can support wave-level group trend review, but they cannot support linked same-respondent change unless the study used repeat participation from the first wave.

## Assessment

The existing backend semantics are valid: linked comparison is gated on anonymous-longitudinal waves and complete linked trajectories. The product/UI gap was that the Waves hub did not distinguish:

- Group trend: compare wave-level results between rounds, allowed for anonymous or otherwise unlinked waves when responses and scores exist.
- Linked change: compare same-respondent movement, allowed only when repeat-participation waves and linked trajectories are available.

Without that distinction, a researcher with Wave 1 and Wave 2 closed could reasonably think the app had no comparison story and that Wave 3 was required before anything useful happened.

## Decision

Keep linked change conservative, but add a researcher-facing group-trend path over the existing read model. Do not add a backend aggregate-delta endpoint in this slice. Results remains the source for concrete score/export values; Waves now explains whether the current waves can be reviewed as a group trend and why linked change is blocked.

## Implemented slice

- Added a two-anonymous-wave regression to the Waves workflow model.
- Added `toSelectedSeriesGroupTrendPlan` to derive group-trend readiness from submitted waves and score counts.
- Changed two-wave non-longitudinal plan copy from primary "Create Wave 3" to primary group-trend review with "Set up Wave 3" as secondary.
- Renamed the workflow action copy around linked change so non-longitudinal waves say repeat participation is missing rather than implying generic comparison is missing.
- Rendered a separate Group trend panel in the Waves hub with first/second wave names, response counts, guidance, and actions.

## Verification

- RED: focused `waves-workflow.test.ts` failed because two anonymous closed waves still returned `Create Wave 3`.
- GREEN: focused `waves-workflow.test.ts` passed 13/13.
- Web production build passed with the existing Vite large-chunk warning.
- `git diff --check` passed with only CRLF normalization warnings on touched files.

## Remaining risk

This is a UI/model clarity slice, not a full analytics feature. The app still lacks a dedicated backend group-trend proof endpoint, trend chart, or downloadable cross-wave aggregate comparison artifact. If owner testing confirms the distinction is understandable, the next wave analytics slice should add a real disclosure-safe group-trend summary from backend score aggregates instead of relying on Results navigation.
