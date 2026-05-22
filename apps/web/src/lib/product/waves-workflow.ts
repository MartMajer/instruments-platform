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
	safetyRows: Array<{ label: string; value: string }>;
	primaryLabel: string;
	primaryHref: string | null;
	secondaryLabel: string | null;
	secondaryHref: string | null;
	guidance: string[];
};

export type SelectedSeriesWaveComparisonReviewItemId =
	| 'wave_sequence'
	| 'comparison_type'
	| 'data_readiness'
	| 'claim_boundary';

export type SelectedSeriesWaveComparisonReviewItem = {
	id: SelectedSeriesWaveComparisonReviewItemId;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	detail: string;
};

export type SelectedSeriesWaveComparisonReview = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	items: SelectedSeriesWaveComparisonReviewItem[];
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
				'Review recipients before launching the new wave; do not assume the recipient list is unchanged unless Collection shows it.'
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
				'Use the checks below to confirm both waves can be linked safely.',
				'Results remain wave-by-wave until linked trajectories, disclosure, and scoring compatibility are ready.',
				'If the comparison is blocked, use the details section to see which prerequisite is missing.'
			]
		};
	}

	return {
		title: 'Check same-respondent change',
		description:
			'Two repeat-participation waves exist. Run the comparison checks before treating this as same-respondent change.',
		status: 'pending',
		primaryLabel: 'Run linked checks below',
		primaryHref: null,
		secondaryLabel: 'Review results',
		secondaryHref: reportsHref,
		guidance: [
			'Use the comparison checks below to confirm linked responses, disclosure, scoring compatibility, and visible deltas before making change-over-time claims.',
			'Use Results for wave-level exports; use Waves only when you need reviewed change-over-time context.',
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
			safetyRows: [
				{ label: 'Same-respondent comparison', value: 'Not available until two repeated waves exist' },
				{ label: 'Disclosure status', value: 'Review after follow-up wave results exist' }
			],
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
		title: `Aggregate group trend only: ${baselineWave.name} to ${comparisonWave.name}`,
		description: scoresReady
			? 'Aggregate group-level results are ready to review as a trend. This is not same-respondent change.'
			: 'Both waves have responses. Finish score output before treating the trend as ready.',
		status: scoresReady ? 'ready' : 'pending',
		baselineName: baselineWave.name,
		comparisonName: comparisonWave.name,
		baselineResponseCount: baselineWave.submittedResponseCount,
		comparisonResponseCount: comparisonWave.submittedResponseCount,
		safetyRows: [
			{ label: 'First wave scores', value: String(baselineWave.scoreCount) },
			{ label: 'Second wave scores', value: String(comparisonWave.scoreCount) },
			{
				label: 'Same-respondent comparison',
				value:
					workspace.summary.longitudinalWaveCount >= 2
						? 'Run comparison checks before making same-respondent claims'
						: 'Not configured for same-respondent linked change'
			},
			{
				label: 'Disclosure status',
				value:
					workspace.comparison.disclosureState === 'not_available'
						? 'Review wave-level disclosure in Results before making claims'
						: workspace.comparison.disclosureState.replaceAll('_', ' ')
			},
			{
				label: 'Suppressed linked comparisons',
				value: String(workspace.summary.suppressedComparisonCount)
			}
		],
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

export function toSelectedSeriesWaveComparisonReview(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesWaveComparisonReview {
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace);
	const sameRespondentReady = isSameRespondentComparisonReady(workspace);
	const groupTrendReady = groupTrendPlan.status === 'ready';
	const items: SelectedSeriesWaveComparisonReviewItem[] = [
		toWaveSequenceReviewItem(workspace),
		toComparisonTypeReviewItem(workspace, groupTrendPlan, sameRespondentReady),
		toDataReadinessReviewItem(workspace, groupTrendPlan, sameRespondentReady),
		toClaimBoundaryReviewItem(workspace, groupTrendPlan, sameRespondentReady)
	];

	return {
		title: 'Comparison plan',
		description:
			'See whether this study is ready for a follow-up wave, aggregate group trend, or same-respondent linked change.',
		status:
			workspace.summary.campaignCount === 0
				? 'blocked'
				: sameRespondentReady || groupTrendReady
					? 'ready'
					: 'pending',
		items
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

function toWaveSequenceReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesWaveComparisonReviewItem {
	if (workspace.summary.campaignCount === 0) {
		return {
			id: 'wave_sequence',
			label: 'Wave sequence',
			status: 'blocked',
			summary: 'No wave exists yet',
			detail: 'Create Wave 1 in Setup, launch it from Collection, then return here after responses arrive.'
		};
	}

	if (workspace.summary.campaignCount === 1) {
		return {
			id: 'wave_sequence',
			label: 'Wave sequence',
			status: 'pending',
			summary: 'Wave 2 is the next study round',
			detail:
				'Use Setup to create the follow-up wave inside this same study when you are ready to repeat collection.'
		};
	}

	return {
		id: 'wave_sequence',
		label: 'Wave sequence',
		status: 'ready',
		summary: `${workspace.summary.campaignCount} waves exist`,
		detail:
			'Review the latest two waves below. Add another wave only when a new collection round is actually planned.'
	};
}

function toComparisonTypeReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean
): SelectedSeriesWaveComparisonReviewItem {
	if (workspace.summary.campaignCount < 2) {
		return {
			id: 'comparison_type',
			label: 'Comparison type',
			status: 'pending',
			summary: 'No comparison yet',
			detail: 'A comparison needs at least two waves with responses.'
		};
	}

	if (sameRespondentReady) {
		return {
			id: 'comparison_type',
			label: 'Comparison type',
			status: 'ready',
			summary: 'Same-respondent linked change',
			detail:
				'These waves have repeat-participation linking, scoring compatibility, and disclosure-visible comparison output.'
		};
	}

	if (workspace.summary.longitudinalWaveCount >= 2) {
		return {
			id: 'comparison_type',
			label: 'Comparison type',
			status: 'pending',
			summary: 'Linked comparison needs checks',
			detail:
				'The study has repeat-participation waves, but linked pairs, scoring compatibility, or disclosure output still need confirmation.'
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		return {
			id: 'comparison_type',
			label: 'Comparison type',
			status: groupTrendPlan.status,
			summary: 'Group trend only',
			detail:
				'These waves can show aggregate movement between rounds, but not individual respondent change.'
		};
	}

	return {
		id: 'comparison_type',
		label: 'Comparison type',
		status: 'blocked',
		summary: 'No comparison yet',
		detail: 'Collect responses in at least two waves before reviewing change over time.'
	};
}

function toDataReadinessReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean
): SelectedSeriesWaveComparisonReviewItem {
	if (sameRespondentReady) {
		return {
			id: 'data_readiness',
			label: 'Data readiness',
			status: 'ready',
			summary: `${workspace.comparison.linkedPairCount} linked ${pluralize(
				workspace.comparison.linkedPairCount,
				'pair',
				'pairs'
			)}, ${workspace.comparison.visibleScoreCount} visible ${pluralize(
				workspace.comparison.visibleScoreCount,
				'score',
				'scores'
			)}`,
			detail:
				'The linked comparison is visible after disclosure and scoring checks. Suppressed scores stay hidden.'
		};
	}

	if (workspace.summary.campaignCount < 2) {
		return {
			id: 'data_readiness',
			label: 'Data readiness',
			status: 'blocked',
			summary: 'Collect a follow-up wave first',
			detail: 'One wave can be reviewed in Results, but it cannot show change over time.'
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		const firstScores = groupTrendPlan.safetyRows.find((row) => row.label === 'First wave scores');
		const secondScores = groupTrendPlan.safetyRows.find((row) => row.label === 'Second wave scores');

		return {
			id: 'data_readiness',
			label: 'Data readiness',
			status: groupTrendPlan.status,
			summary:
				groupTrendPlan.status === 'ready'
					? `${firstScores?.value ?? 0} first-wave scores, ${secondScores?.value ?? 0} second-wave scores`
					: 'Finish score output before reading the trend',
			detail:
				'Use Results for the actual score tables and exports before making claims from this trend.'
		};
	}

	return {
		id: 'data_readiness',
		label: 'Data readiness',
		status: 'pending',
		summary: 'Comparison output is not ready',
		detail: 'Run the checks below to see whether linked pairs, scoring, or disclosure are blocking review.'
	};
}

function toClaimBoundaryReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean
): SelectedSeriesWaveComparisonReviewItem {
	if (sameRespondentReady) {
		return {
			id: 'claim_boundary',
			label: 'Claim boundary',
			status: 'ready',
			summary: 'Disclosure-gated custom-study comparison',
			detail:
				'Describe this as change in this tenant-provided custom study, not as an official benchmark or clinical threshold.'
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		return {
			id: 'claim_boundary',
			label: 'Claim boundary',
			status: 'ready',
			summary: 'Do not call this same-respondent change',
			detail:
				'Use group-level wording such as aggregate trend between waves unless repeat-participation linking is ready.'
		};
	}

	if (workspace.summary.campaignCount === 1) {
		return {
			id: 'claim_boundary',
			label: 'Claim boundary',
			status: 'ready',
			summary: 'Current results are wave-level only',
			detail: 'Review Wave 1 on its own. Do not describe movement until a follow-up wave exists.'
		};
	}

	return {
		id: 'claim_boundary',
		label: 'Claim boundary',
		status: 'blocked',
		summary: 'No change claim available',
		detail: 'Create and collect waves before writing any change-over-time interpretation.'
	};
}

function isSameRespondentComparisonReady(workspace: CampaignSeriesWavesWorkspaceResponse) {
	return (
		workspace.summary.longitudinalWaveCount >= 2 &&
		workspace.comparison.status !== 'not_available' &&
		workspace.comparison.compatibilityState === 'compatible' &&
		workspace.comparison.disclosureState === 'visible' &&
		workspace.comparison.linkedPairCount > 0 &&
		workspace.comparison.visibleScoreCount > 0
	);
}

function toGroupTrendWaves(workspace: CampaignSeriesWavesWorkspaceResponse) {
	return [...workspace.waves]
		.filter((wave) => wave.submittedResponseCount > 0)
		.sort((left, right) => toTimestamp(left.latestLaunchAt) - toTimestamp(right.latestLaunchAt))
		.slice(-2);
}

function pluralize(value: number, singular: string, plural: string) {
	return value === 1 ? singular : plural;
}

function toTimestamp(value: string | null | undefined) {
	if (!value) {
		return 0;
	}

	const timestamp = Date.parse(value);
	return Number.isFinite(timestamp) ? timestamp : 0;
}
