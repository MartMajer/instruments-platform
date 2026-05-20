# D362 - Wave workflow clarity assessment - 2026-05-20

## Context

Owner completed a broader study-creation walkthrough and reported the workflow was acceptable but still finicky. The clearest product confusion was multiple waves: the Waves hub explained comparison mechanics only after waves existed, not how a researcher creates Wave 2.

## Assessment

The backend and read models already treat waves as campaigns inside a campaign series. Adding a new wave should therefore stay inside the existing setup/collection/results flow:

- Setup creates another campaign draft in the same study.
- Collection launches and delivers that campaign.
- Results reviews and exports that wave.
- Waves compares linked longitudinal waves after two suitable waves have responses, linked trajectories, scoring compatibility, and disclosure clearance.

The product gap was guidance and naming, not a new backend endpoint. Adding a fake "copy previous wave" promise would be misleading because recipient copying is not currently guaranteed.

## Task

- Added `toSelectedSeriesWavePlan` to map the current wave state into researcher-facing next steps.
- Rendered a "How multiple waves work" plan on the Waves hub above comparison actions.
- Updated Waves route copy to describe follow-up collection waves plus comparison.
- Added `defaultCampaignWaveName` so Setup defaults to `Wave 2`, `Wave 3`, etc. when the study already has campaigns.
- Updated unit tests for current setup/waves copy and new wave-plan behavior.

## Verification

- RED: new Waves plan test failed because `toSelectedSeriesWavePlan` did not exist.
- GREEN: `waves-workflow.test.ts` passed 12/12.
- RED: new Setup naming test failed because `defaultCampaignWaveName` did not exist.
- GREEN: `setup-workflow.test.ts` passed 7/7.
- Combined focused run passed 19/19.
- `apps/web` SvelteKit sync and Vite production build passed with the existing large-chunk warning.

## Remaining risk

This is a clarity and safe-default pass. It does not add automatic wave cloning, recipient copying, schedule templates, or a dedicated `Create next wave` backend command. Those should be separate product slices if owner validation shows they are necessary.
