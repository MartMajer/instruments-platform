# D363 - Setup editable wave selection assessment - 2026-05-20

## Context

After D362, owner tested the Waves -> Setup path and hit `Saved recipient selection` / `API request failed with status 409` while working around Wave 2. Staging campaign data showed only historical live/closed waves in the relevant path, and the backend has a deliberate conflict guard: respondent rules can only be changed before launch.

## Assessment

The backend behavior is correct. Recipient selection is launch configuration, so it must be locked after a wave is live or closed. The product bug was frontend workflow semantics: Setup used the selected historical campaign both as context and as the editable campaign.

The setup read model still needs historical campaigns for template/scoring context, but the UI must only send mutation calls for draft or scheduled waves. If no editable wave exists, Setup should guide the researcher to create the next collection wave instead of offering recipient save controls.

## Task

- Changed `selectSetupCampaignId` so it returns only local newly-created campaigns or workspace-selected campaigns whose status is `draft` or `scheduled`.
- Added regression coverage for a closed selected wave: no editable campaign id, campaign step current, readiness blocked until a new wave is saved.
- Updated the Setup UI so recipient-selection controls render only when an editable campaign id exists.
- Added a locked-wave message explaining that previous-wave recipient selection is frozen after launch and that the next draft wave must be saved first.

## Verification

- RED: focused `setup-workflow.test.ts` failed because a closed selected wave still returned `campaign-id`.
- GREEN: focused `setup-workflow.test.ts` passed 8/8.
- `apps/web` SvelteKit sync and Vite production build passed with the existing large-chunk warning.

## Remaining risk

This fixes the confusing save path and prevents the `409` from the normal UI. It does not yet create a dedicated backend `Create next wave` command that clones/reviews questionnaire, scoring, recipients, and delivery settings. That is still the next deeper product slice if multi-wave setup remains finicky.
