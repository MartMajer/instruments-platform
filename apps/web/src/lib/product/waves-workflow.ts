import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import type {
	CampaignSeriesWaveComparisonProofResponse,
	WaveScoreComparisonResponse
} from '$lib/api/setup';
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

export type SelectedSeriesWaveScoreMethodReviewItemId =
	| 'scoring_rules'
	| 'comparison_method'
	| 'outputs'
	| 'missingness'
	| 'interpretation_boundary';

export type SelectedSeriesWaveScoreMethodReviewItem = {
	id: SelectedSeriesWaveScoreMethodReviewItemId;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	detail: string;
};

export type SelectedSeriesWaveScoreMethodReview = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	items: SelectedSeriesWaveScoreMethodReviewItem[];
};

export type SelectedSeriesWavesPathStepState = 'done' | 'current' | 'blocked';

export type SelectedSeriesWavesPathStep = SelectedSeriesWavesWorkflowAction & {
	pathState: SelectedSeriesWavesPathStepState;
};

export type SelectedSeriesWavesPathMode = 'setup' | 'group_trend' | 'linked_change';

export type SelectedSeriesWavesPath = {
	steps: SelectedSeriesWavesPathStep[];
	currentActionId: SelectedSeriesWavesWorkflowActionId;
	currentAction: SelectedSeriesWavesWorkflowAction;
	completedCount: number;
	totalCount: number;
	mode: SelectedSeriesWavesPathMode;
	showWorkflow: boolean;
	inactiveReason: string | null;
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
			secondaryLabel: `Plan Wave ${workspace.summary.campaignCount + 1} later`,
			secondaryHref: setupHref,
			guidance: [
				'Review these waves as a group-level trend. Do not describe the change as same-respondent movement because the waves are anonymous.',
				'Use repeat participation from Wave 1 when the study needs linked change-over-time comparison later.',
				`Review or export Wave 1 and Wave 2 before using Setup to create Wave ${workspace.summary.campaignCount + 1}.`
			]
		};
	}

	if (!hasTwoLongitudinalWaves) {
		const nextWaveNumber = workspace.summary.campaignCount + 1;
		return {
			title: `Review Wave 1 before planning Wave ${nextWaveNumber}`,
			description:
				'Wave 1 exists. Review the current results first; plan a follow-up only when the next collection round is intentional.',
			status: 'pending',
			primaryLabel: 'Review Wave 1 results',
			primaryHref: reportsHref,
			secondaryLabel: `Plan Wave ${nextWaveNumber} later`,
			secondaryHref: setupHref,
			guidance: [
				`Review or export Wave 1 before using Setup to create Wave ${nextWaveNumber}.`,
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
	const nextWaveLabel = `Plan Wave ${workspace.summary.campaignCount + 1} later`;
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

export function toSelectedSeriesWaveScoreMethodReview(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	comparisonProof: CampaignSeriesWaveComparisonProofResponse | null = null
): SelectedSeriesWaveScoreMethodReview {
	const proofScores = comparisonProof?.scores ?? [];
	const interpretationReviewed = isInterpretationValidated(
		comparisonProof?.interpretationStatus ?? workspace.comparison.interpretationStatus
	);
	const hasIncompleteInputs = proofScores.some(hasIncompleteComparisonInputs);
	const hasAnyWave = workspace.summary.campaignCount > 0;
	const hasRepeatedWaves = workspace.summary.longitudinalWaveCount >= 2;
	const items: SelectedSeriesWaveScoreMethodReviewItem[] = [
		toWaveScoringRulesItem(workspace),
		toWaveComparisonMethodItem(workspace),
		toWaveComparedOutputsItem(hasRepeatedWaves, workspace.comparison.visibleScoreCount, proofScores),
		toWaveMissingnessItem(hasRepeatedWaves, proofScores),
		toWaveInterpretationBoundaryItem(hasAnyWave, hasRepeatedWaves, interpretationReviewed)
	];

	return {
		title: 'What is being compared?',
		description:
			'Review scoring rules, linked-pair method, compared outputs, missingness, and interpretation limits before using wave change.',
		status: !hasAnyWave
			? 'not_available'
			: !hasRepeatedWaves
				? 'pending'
				: interpretationReviewed && proofScores.length > 0 && !hasIncompleteInputs
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
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace);
	const mode = toWavesPathMode(workspace, groupTrendPlan);
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
		totalCount: steps.length,
		mode,
		showWorkflow: mode === 'linked_change',
		inactiveReason: toWavesPathInactiveReason(workspace, mode)
	};
}

function toWavesPathMode(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan
): SelectedSeriesWavesPathMode {
	if (workspace.summary.longitudinalWaveCount >= 2) {
		return 'linked_change';
	}

	if (groupTrendPlan.status !== 'blocked') {
		return 'group_trend';
	}

	return 'setup';
}

function toWavesPathInactiveReason(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	mode: SelectedSeriesWavesPathMode
) {
	if (mode === 'linked_change') {
		return null;
	}

	if (mode === 'group_trend') {
		return 'This study supports aggregate group trend only. Linked-change checks are not required and would be misleading here.';
	}

	if (workspace.summary.campaignCount === 0) {
		return 'Create and collect the first waves before linked-change checks apply.';
	}

	if (workspace.summary.campaignCount === 1) {
		return 'Review Wave 1 in Results. Plan Wave 2 from Setup only when the next collection round is intentional.';
	}

	return 'Collect scored responses in at least two waves before comparison tasks apply.';
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
			summary: 'Only Wave 1 exists',
			detail:
				'Review Wave 1 in Results before deciding whether Wave 2 is needed for a follow-up collection.'
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

function toWaveScoringRulesItem(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesWaveScoreMethodReviewItem {
	const baseline = workspace.selectedBaselineWave;
	const comparison = workspace.selectedComparisonWave;

	if (!baseline || !comparison) {
		return {
			id: 'scoring_rules',
			label: 'Scoring rules',
			status: 'pending',
			summary: 'Two waves needed',
			detail: 'Select or create two waves before comparing scoring rules.'
		};
	}

	const baselineRule = formatRuleVersion(baseline.scoringRuleKey, baseline.scoringRuleVersion);
	const comparisonRule = formatRuleVersion(
		comparison.scoringRuleKey,
		comparison.scoringRuleVersion
	);
	const sameRule = baselineRule === comparisonRule && baselineRule !== 'not configured';

	return {
		id: 'scoring_rules',
		label: 'Scoring rules',
		status: sameRule ? 'ready' : 'pending',
		summary: sameRule
			? `Wave 1 and Wave 2 use ${baselineRule}`
			: `Wave 1 uses ${baselineRule}; Wave 2 uses ${comparisonRule}`,
		detail: sameRule
			? 'The selected waves use the same scoring rule key and version.'
			: 'Review compatibility before describing score movement between waves.'
	};
}

function toWaveComparisonMethodItem(
	workspace: CampaignSeriesWavesWorkspaceResponse
): SelectedSeriesWaveScoreMethodReviewItem {
	if (workspace.summary.longitudinalWaveCount < 2) {
		return {
			id: 'comparison_method',
			label: 'Comparison method',
			status: 'pending',
			summary: 'Wave-level review only',
			detail:
				'Same-respondent change needs repeat-participation waves. Anonymous unlinked waves support aggregate group trend only.'
		};
	}

	const visibleScores = workspace.comparison.visibleScoreCount;
	const linkedPairs = workspace.comparison.linkedPairCount;

	return {
		id: 'comparison_method',
		label: 'Comparison method',
		status: linkedPairs > 0 && visibleScores > 0 ? 'ready' : 'pending',
		summary: `${linkedPairs} linked ${pluralize(
			linkedPairs,
			'pair',
			'pairs'
		)}, ${visibleScores} visible comparison ${pluralize(visibleScores, 'score', 'scores')}`,
		detail:
			'Linked change uses repeat-participation trajectories after scoring compatibility and disclosure checks.'
	};
}

function toWaveComparedOutputsItem(
	hasRepeatedWaves: boolean,
	visibleScoreCount: number,
	proofScores: WaveScoreComparisonResponse[]
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasRepeatedWaves) {
		return {
			id: 'outputs',
			label: 'Compared outputs',
			status: 'pending',
			summary: 'No linked outputs yet',
			detail: 'Compared output names appear after repeated waves are ready for linked change.'
		};
	}

	if (proofScores.length > 0) {
		return {
			id: 'outputs',
			label: 'Compared outputs',
			status: 'ready',
			summary: `${proofScores.length} compared ${pluralize(
				proofScores.length,
				'output',
				'outputs'
			)}: ${proofScores.map((score) => score.dimensionCode).join(', ')}`,
			detail:
				'These output codes come from the linked-change proof. Keep the scoring plan with any interpretation.'
		};
	}

	return {
		id: 'outputs',
		label: 'Compared outputs',
		status: visibleScoreCount > 0 ? 'pending' : 'blocked',
		summary:
			visibleScoreCount > 0
				? 'Compared output names available after reviewing linked change'
				: 'No visible compared outputs yet',
		detail: 'Run Review linked change to load the compared score output rows.'
	};
}

function toWaveMissingnessItem(
	hasRepeatedWaves: boolean,
	proofScores: WaveScoreComparisonResponse[]
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasRepeatedWaves) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'pending',
			summary: 'No linked missingness yet',
			detail: 'Missing-answer comparison metadata appears after repeated waves are ready.'
		};
	}

	if (proofScores.length === 0) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'pending',
			summary: 'Missing-answer metadata available after reviewing linked change',
			detail: 'Run Review linked change to load valid/expected answer contribution counts.'
		};
	}

	const incomplete = proofScores.flatMap(toIncompleteComparisonInputSummaries);

	if (incomplete.length === 0) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'ready',
			summary: 'No missing-score input gap in comparison preview',
			detail: 'The compared outputs reported complete valid/expected answer contribution counts.'
		};
	}

	return {
		id: 'missingness',
		label: 'Missing answers',
		status: 'pending',
		summary: 'Some compared score inputs were incomplete',
		detail: incomplete.join('; ')
	};
}

function toWaveInterpretationBoundaryItem(
	hasAnyWave: boolean,
	hasRepeatedWaves: boolean,
	interpretationReviewed: boolean
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasAnyWave) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Create a wave before reviewing interpretation boundaries.'
		};
	}

	if (!hasRepeatedWaves) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'pending',
			summary: 'Wave-level results only',
			detail: 'Do not describe change until a follow-up wave exists.'
		};
	}

	if (interpretationReviewed) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'ready',
			summary: 'Interpretation reviewed for this comparison',
			detail: 'Keep disclosure and method notes with any wave-comparison export or report.'
		};
	}

	return {
		id: 'interpretation_boundary',
		label: 'Interpretation boundary',
		status: 'pending',
		summary: 'Custom-study change, not a benchmark',
		detail:
			'Describe this as change in this tenant-defined study only. Do not present it as an official benchmark, norm, clinical threshold, or externally validated claim.'
	};
}

function formatRuleVersion(key: string | null | undefined, version: string | null | undefined) {
	if (!key) {
		return 'not configured';
	}

	return version ? `${key} ${version}` : key;
}

function isInterpretationValidated(status: string | null | undefined) {
	const normalized = status ?? '';
	return (
		normalized === 'validated' ||
		normalized === 'validated_interpretation' ||
		normalized === 'official_validated'
	);
}

function hasIncompleteComparisonInputs(score: WaveScoreComparisonResponse) {
	return toIncompleteComparisonInputSummaries(score).length > 0;
}

function toIncompleteComparisonInputSummaries(score: WaveScoreComparisonResponse) {
	const summaries: string[] = [];

	if (
		typeof score.baselineNValidTotal === 'number' &&
		typeof score.baselineNExpectedTotal === 'number' &&
		score.baselineNValidTotal < score.baselineNExpectedTotal
	) {
		summaries.push(
			`${score.dimensionCode} baseline used ${score.baselineNValidTotal} of ${score.baselineNExpectedTotal} expected answer contributions`
		);
	}

	if (
		typeof score.comparisonNValidTotal === 'number' &&
		typeof score.comparisonNExpectedTotal === 'number' &&
		score.comparisonNValidTotal < score.comparisonNExpectedTotal
	) {
		summaries.push(
			`${score.dimensionCode} follow-up used ${score.comparisonNValidTotal} of ${score.comparisonNExpectedTotal} expected answer contributions`
		);
	}

	return summaries;
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
