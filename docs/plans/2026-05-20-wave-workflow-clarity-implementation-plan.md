# Wave Workflow Clarity Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the Waves hub explain how to create and compare multiple waves, and make Setup default the next collection wave name.

**Architecture:** Keep backend contracts unchanged. Add small frontend view-model helpers with unit coverage, then render those helpers in the existing selected-series Waves and Setup workflows.

**Tech Stack:** Svelte 5, SvelteKit, TypeScript, Vitest.

---

## Task 1: Add wave-plan model

**Files:**

- Modify: `apps/web/src/lib/product/waves-workflow.ts`
- Test: `apps/web/src/lib/product/waves-workflow.test.ts`

**Steps:**

1. Add failing tests for `toSelectedSeriesWavePlan` covering empty, one-wave, and comparison-ready workspaces.
2. Run `vitest` filtered to the first new test and confirm it fails because the helper does not exist.
3. Implement `toSelectedSeriesWavePlan`.
4. Run the full `waves-workflow.test.ts`.

## Task 2: Render wave-plan guidance

**Files:**

- Modify: `apps/web/src/lib/product/SelectedSeriesWavesWorkflow.svelte`
- Modify: `apps/web/src/routes/app/campaign-series/[seriesId]/waves/+page.svelte`

**Steps:**

1. Import `toSelectedSeriesWavePlan`.
2. Render a `Wave plan` panel above the comparison task path.
3. Update route header copy to describe follow-up wave creation plus comparison.
4. Run the web production build.

## Task 3: Default Setup to the next wave name

**Files:**

- Modify: `apps/web/src/lib/product/setup-workflow.ts`
- Test: `apps/web/src/lib/product/setup-workflow.test.ts`
- Modify: `apps/web/src/lib/product/SelectedSeriesSetupWorkflow.svelte`

**Steps:**

1. Add failing tests for `defaultCampaignWaveName`.
2. Run `vitest` filtered to the new test and confirm it fails because the helper does not exist.
3. Implement `defaultCampaignWaveName`.
4. Use it to initialize the campaign draft form without Svelte local-state capture warnings.
5. Run focused setup and waves unit tests plus the web production build.
