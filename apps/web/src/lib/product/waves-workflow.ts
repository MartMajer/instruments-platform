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

export type SelectedSeriesGroupTrendPlan = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	baselineName: string | null;
	comparisonName: string | null;
	baselineResponseCount: number | null;
	comparisonResponseCount: number | null;
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
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace);

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

	if (!hasTwoLongitudinalWaves && workspace.summary.campaignCount >= 2 && groupTrendPlan.status !== 'blocked') {
		const wavePairTitle =
			groupTrendPlan.baselineName && groupTrendPlan.comparisonName
				? `${groupTrendPlan.baselineName} and ${groupTrendPlan.comparisonName}`
				: 'the selected waves';

		return {
			title: `Review ${wavePairTitle}`,
			description:
				'These waves can be reviewed as group-level results. Linked same-respondent change needs repeat participation from the first wave.',
			status: groupTrendPlan.status,
			primaryLabel: 'Review group trend',
			primaryHref: reportsHref,
			secondaryLabel: `Set up Wave ${workspace.summary.campaignCount + 1}`,
			secondaryHref: setupHref,
			guidance: [
				'Review these waves as a group-level trend. Do not describe the change as same-respondent movement because the waves are anonymous.',
				'Use repeat participation from Wave 1 when the study needs linked change-over-time comparison later.',
				'Create another wave only when you want a new collection round, not as a prerequisite for reviewing these two waves.'
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

export function toSelectedSeriesGroupTrendPlan(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesGroupTrendPlan {
	const setupHref = `/app/campaign-series/${workspace.series.id}/setup`;
	const reportsHref = `/app/campaign-series/${workspace.series.id}/reports`;
	const nextWaveLabel = `Set up Wave ${workspace.summary.campaignCount + 1}`;
	const waves = toGroupTrendWaves(workspace);

	if (waves.length < 2) {
		return {
			title: 'Group trend not ready',
			description:
				'Collect responses in at least two waves before reviewing wave-level trend.',
			status: 'blocked',
			baselineName: waves[0]?.name ?? null,
			comparisonName: null,
			baselineResponseCount: waves[0]?.submittedResponseCount ?? null,
			comparisonResponseCount: null,
			primaryLabel: nextWaveLabel,
			primaryHref: setupHref,
			secondaryLabel: workspace.summary.campaignCount > 0 ? 'Review results' : null,
			secondaryHref: workspace.summary.campaignCount > 0 ? reportsHref : null,
			guidance: [
				'A group trend compares wave-level results. It does not require respondent linking.',
				'Launch and collect a follow-up wave before reading a trend.',
				'Use repeat participation if you need same-respondent change instead of wave-level movement.'
			]
		};
	}

	const [baselineWave, comparisonWave] = waves;
	const scoresReady = baselineWave.scoreCount > 0 && comparisonWave.scoreCount > 0;

	return {
		title: `${baselineWave.name} to ${comparisonWave.name}`,
		description: scoresReady
			? 'Wave-level results are ready to review as a group trend. This is not same-respondent change.'
			: 'Both waves have responses. Finish score output before treating the trend as ready.',
		status: scoresReady ? 'ready' : 'pending',
		baselineName: baselineWave.name,
		comparisonName: comparisonWave.name,
		baselineResponseCount: baselineWave.submittedResponseCount,
		comparisonResponseCount: comparisonWave.submittedResponseCount,
		primaryLabel: 'Open Results',
		primaryHref: reportsHref,
		secondaryLabel: nextWaveLabel,
		secondaryHref: setupHref,
		guidance: [
			'Use this for anonymous or unlinked waves where the question is whether the group moved between rounds.',
			'Do not describe this as individual improvement or decline unless linked change is ready.',
			'Review scoring and disclosure in Results before making claims from the trend.'
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
			title: 'Check linked change readiness',
			description:
				'Confirm this study has repeat-participation waves and linked responses for same-respondent comparison.',
			status: toTwoWaveProofStatus(hasTwoLongitudinalWaves, twoWaveProofViewed),
			available: hasTwoLongitudinalWaves,
			disabledReason: hasTwoLongitudinalWaves
				? null
				: toTwoWaveProofDisabledReason(workspace)
		},
		{
			id: 'waveComparisonProof',
			step: 'Step 2',
			title: 'Review linked change',
			description: 'Review disclosure-safe same-respondent change between the selected waves.',
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

function toTwoWaveProofDisabledReason(workspace: CampaignSeriesWavesWorkspaceResponse) {
	if (workspace.summary.campaignCount >= 2) {
		return 'Linked same-respondent comparison is unavailable because these waves were not created with repeat participation. Review group trend instead.';
	}

	return 'Add at least two repeated waves before comparing change over time.';
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

function toGroupTrendWaves(workspace: CampaignSeriesWavesWorkspaceResponse) {
	return [...workspace.waves]
		.filter((wave) => wave.submittedResponseCount > 0)
		.sort((left, right) => toTimestamp(left.latestLaunchAt) - toTimestamp(right.latestLaunchAt))
		.slice(-2);
}

function toTimestamp(value: string | null | undefined) {
	if (!value) {
		return 0;
	}

	const timestamp = Date.parse(value);
	return Number.isFinite(timestamp) ? timestamp : 0;
}
