# Wave Workflow Clarity Design

## Goal

Make the Waves hub explain how a researcher creates and uses multiple waves without adding unsupported backend promises.

## Current problem

The current Waves hub focuses on linked trajectory checks and comparison proof. That is useful after two longitudinal waves exist, but it does not answer the normal researcher question: "How do I make Wave 2?"

The setup form also defaulted new campaign drafts to `Wave 1`, even when the study already had one wave.

## Design

Add a researcher-facing wave plan above the comparison workflow:

- `Create the first wave` when the study has no campaigns.
- `Create Wave N` when the study has campaigns but not two anonymous-longitudinal waves.
- `Check comparison readiness` when two longitudinal waves exist but comparison is not ready.
- `Compare waves` when comparison data is ready.

The plan must state the actual product model:

- A wave is one campaign/collection round inside the study.
- Setup creates the campaign draft.
- Collection launches and delivers it.
- Results reviews and exports each wave.
- Waves compares linked change over time after longitudinal prerequisites are met.

The plan must not imply automatic audience copying. It should tell the user to review recipients before launching a follow-up wave.

## Implementation

- Add `toSelectedSeriesWavePlan` in `apps/web/src/lib/product/waves-workflow.ts`.
- Render the plan in `apps/web/src/lib/product/SelectedSeriesWavesWorkflow.svelte`.
- Update Waves route header copy in `apps/web/src/routes/app/campaign-series/[seriesId]/waves/+page.svelte`.
- Add `defaultCampaignWaveName` in `apps/web/src/lib/product/setup-workflow.ts`.
- Use the helper in `apps/web/src/lib/product/SelectedSeriesSetupWorkflow.svelte` so a study with one campaign opens the draft form as `Wave 2`.

## Verification

- Unit tests cover first-wave, next-wave, and comparison-ready plan states.
- Unit tests cover setup default wave naming.
- Web production build must pass.
