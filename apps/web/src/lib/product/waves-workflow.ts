import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesWavesWorkflowActionId = 'twoWaveProof' | 'waveComparisonProof';

export type SelectedSeriesWavesWorkflowLocalState = {
	twoWaveProofViewed?: boolean;
	waveComparisonProofViewed?: boolean;
};

export type SelectedSeriesWavesWorkflowAction = {
	id: SelectedSeriesWavesWorkflowActionId;
	step: string;
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	disabledReason: string | null;
};

export type SelectedSeriesWavePlan = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	primaryLabel: string;
	primaryHref: string | null;
	secondaryLabel: string | null;
	secondaryHref: string | null;
	guidance: string[];
};

export type SelectedSeriesWavesPathStepState = 'done' | 'current' | 'blocked';

export type SelectedSeriesWavesPathStep = SelectedSeriesWavesWorkflowAction & {
	pathState: SelectedSeriesWavesPathStepState;
};

export type SelectedSeriesWavesPath = {
	steps: SelectedSeriesWavesPathStep[];
	currentActionId: SelectedSeriesWavesWorkflowActionId;
	currentAction: SelectedSeriesWavesWorkflowAction;
	completedCount: number;
	totalCount: number;
};

export function toSelectedSeriesWavePlan(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesWavePlan {
	const setupHref = `/app/campaign-series/${workspace.series.id}/setup`;
	const reportsHref = `/app/campaign-series/${workspace.series.id}/reports`;
	const hasAnyWave = workspace.summary.campaignCount > 0;
	const hasTwoLongitudinalWaves = workspace.summary.longitudinalWaveCount >= 2;
	const hasSelectedComparison = Boolean(
		workspace.selectedBaselineWave && workspace.selectedComparisonWave
	);
	const comparisonReady =
		hasSelectedComparison && workspace.comparison.status !== 'not_available';

	if (!hasAnyWave) {
		return {
			title: 'Create the first wave',
			description:
				'Start by creating Wave 1 as the first collection round for this study.',
			status: 'pending',
			primaryLabel: 'Open setup',
			primaryHref: setupHref,
			secondaryLabel: null,
			secondaryHref: null,
			guidance: [
				'Each wave is a collection round inside this study. Create Wave 1 in Setup, then launch it from Collection.',
				'After responses arrive, review the wave in Results before adding a follow-up wave.',
				'Use anonymous longitudinal from the first wave if you need linked change-over-time comparison later.'
			]
		};
	}

	if (!hasTwoLongitudinalWaves) {
		return {
			title: `Create Wave ${workspace.summary.campaignCount + 1}`,
			description:
				'Add another collection round when you want to repeat the study later.',
			status: 'pending',
			primaryLabel: `Set up Wave ${workspace.summary.campaignCount + 1}`,
			primaryHref: setupHref,
			secondaryLabel: 'Review Wave 1 results',
			secondaryHref: reportsHref,
			guidance: [
				'Use Setup to create the next campaign draft inside this same study, then launch it from Collection.',
				'Use anonymous longitudinal when the same respondent should be linked across waves for change-over-time comparison.',
				'Review recipients before launching the new wave; do not assume the audience is unchanged unless Collection shows it.'
			]
		};
	}

	if (!comparisonReady) {
		return {
			title: 'Check comparison readiness',
			description:
				'Two longitudinal waves exist. Now confirm linked trajectories and scoring compatibility.',
			status: 'pending',
			primaryLabel: 'Run checks below',
			primaryHref: null,
			secondaryLabel: 'Review results',
			secondaryHref: reportsHref,
			guidance: [
				'Use the workflow below to confirm both waves can be linked safely.',
				'Results remain wave-by-wave until linked trajectories, disclosure, and scoring compatibility are ready.',
				'If the comparison is blocked, use the details section to see which prerequisite is missing.'
			]
		};
	}

	return {
		title: 'Compare waves',
		description:
			'Two longitudinal waves are ready for disclosure-safe change-over-time review.',
		status: 'ready',
		primaryLabel: 'Run comparison checks below',
		primaryHref: null,
		secondaryLabel: 'Review results',
		secondaryHref: reportsHref,
		guidance: [
			'Use the comparison workflow below to check linked trajectories, disclosure, scoring compatibility, and visible deltas.',
			'Use Results for wave-level exports; use Waves when you need change-over-time context.',
			'Create another follow-up wave from Setup when the next collection round starts.'
		]
	};
}

export function toSelectedSeriesWavesWorkflowActions(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWavesWorkflowLocalState = {}
): SelectedSeriesWavesWorkflowAction[] {
	const hasTwoLongitudinalWaves = workspace.summary.longitudinalWaveCount >= 2;
	const hasSelectedComparison = Boolean(
		workspace.selectedBaselineWave && workspace.selectedComparisonWave
	);
	const comparisonProofReady =
		hasSelectedComparison &&
		(workspace.comparison.status !== 'not_available' || Boolean(localState.twoWaveProofViewed));
	const twoWaveProofViewed = Boolean(localState.twoWaveProofViewed);
	const waveComparisonProofViewed = Boolean(localState.waveComparisonProofViewed);

	return [
		{
			id: 'twoWaveProof',
			step: 'Step 1',
			title: 'Check comparison readiness',
			description: 'Confirm this study has repeated waves and linked responses for comparison.',
			status: toTwoWaveProofStatus(hasTwoLongitudinalWaves, twoWaveProofViewed),
			available: hasTwoLongitudinalWaves,
			disabledReason: hasTwoLongitudinalWaves
				? null
				: 'Add at least two repeated waves before comparing change over time.'
		},
		{
			id: 'waveComparisonProof',
			step: 'Step 2',
			title: 'Review comparison',
			description: 'Review disclosure-safe change over time between the selected waves.',
			status: toWaveComparisonStatus(
				hasSelectedComparison,
				comparisonProofReady,
				waveComparisonProofViewed
			),
			available: comparisonProofReady,
			disabledReason: toWaveComparisonDisabledReason(hasSelectedComparison, comparisonProofReady)
		}
	];
}

export function toSelectedSeriesWavesPath(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWavesWorkflowLocalState = {}
): SelectedSeriesWavesPath {
	const actions = toSelectedSeriesWavesWorkflowActions(workspace, localState);
	const doneByActionId: Record<SelectedSeriesWavesWorkflowActionId, boolean> = {
		twoWaveProof: Boolean(localState.twoWaveProofViewed),
		waveComparisonProof: Boolean(localState.waveComparisonProofViewed)
	};
	const currentAction =
		actions.find((action) => !doneByActionId[action.id] && action.available) ??
		actions.find((action) => !doneByActionId[action.id]) ??
		actions.at(-1) ??
		actions[0];
	const steps = actions.map((action) => ({
		...action,
		pathState: toPathStepState(action.id, currentAction.id, doneByActionId)
	}));

	return {
		steps,
		currentActionId: currentAction.id,
		currentAction,
		completedCount: steps.filter((step) => step.pathState === 'done').length,
		totalCount: steps.length
	};
}

function toTwoWaveProofStatus(
	hasTwoLongitudinalWaves: boolean,
	twoWaveProofViewed: boolean
): ProductReadModelBadgeStatus {
	if (!hasTwoLongitudinalWaves) {
		return 'blocked';
	}

	return twoWaveProofViewed ? 'ready' : 'pending';
}

function toWaveComparisonStatus(
	hasSelectedComparison: boolean,
	comparisonProofReady: boolean,
	waveComparisonProofViewed: boolean
): ProductReadModelBadgeStatus {
	if (!hasSelectedComparison || !comparisonProofReady) {
		return 'blocked';
	}

	return waveComparisonProofViewed ? 'ready' : 'pending';
}

function toWaveComparisonDisabledReason(
	hasSelectedComparison: boolean,
	comparisonProofReady: boolean
) {
	if (!hasSelectedComparison) {
		return 'Choose baseline and comparison waves before reviewing change over time.';
	}

	return comparisonProofReady
		? null
		: 'Check comparison readiness before reviewing change over time.';
}

function toPathStepState(
	actionId: SelectedSeriesWavesWorkflowActionId,
	currentActionId: SelectedSeriesWavesWorkflowActionId,
	doneByActionId: Record<SelectedSeriesWavesWorkflowActionId, boolean>
): SelectedSeriesWavesPathStepState {
	if (doneByActionId[actionId]) {
		return 'done';
	}

	if (actionId === currentActionId) {
		return 'current';
	}

	return 'blocked';
}
