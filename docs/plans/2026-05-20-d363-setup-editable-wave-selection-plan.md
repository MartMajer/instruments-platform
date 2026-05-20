# D363 - Setup editable wave selection plan - 2026-05-20

## Problem

Owner hit `409 Conflict` while saving recipient selection on Wave 2. Staging data and code showed the setup UI could treat a launched or closed wave as the editable setup campaign. The backend correctly rejects respondent-rule edits after launch, but the UI still exposed save controls.

## Root cause

`selectSetupCampaignId` returned `workspace.selectedCampaign.id` without checking campaign status. Product read models may select a historical launched/closed wave when no draft exists, because that wave still provides template/scoring context. Setup then rendered recipient-selection controls against that historical wave.

## Acceptance criteria

- Draft and scheduled waves remain editable in Setup.
- Live, closed, cancelled, or otherwise non-editable waves are not returned as the editable setup campaign.
- When only historical waves exist, Setup returns to the collection setup step so the researcher can create the next draft wave.
- Recipient-selection controls only render when an editable campaign id exists.
- The UI explains that previous-wave recipient selection is locked after launch.

## Verification plan

- Add a focused setup-workflow regression that fails while closed waves are treated as editable.
- Run the focused setup-workflow Vitest file.
- Run the `apps/web` production build.
- Deploy to staging and run public health/app checks.
