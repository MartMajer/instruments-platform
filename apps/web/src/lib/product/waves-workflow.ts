import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import type {
	CampaignSeriesWaveComparisonProofResponse,
	WaveScoreComparisonResponse
} from '$lib/api/setup';
import type { AppLocale } from '$lib/i18n/localization';
import { appMessage, type AppMessageId, type AppMessageValues } from '$lib/i18n/messages';
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

type WavesWorkflowActionCopy = {
	title: string;
	description: string;
};

export type SelectedSeriesWavesWorkflowCopy = {
	locale?: AppLocale;
	stepNumber: (number: number) => string;
	plan: {
		createFirstTitle: string;
		createFirstDescription: string;
		openSetupLabel: string;
		createFirstGuidance: string[];
		reviewWavePairTitle: (wavePairTitle: string) => string;
		groupTrendReviewDescription: string;
		reviewGroupTrendLabel: string;
		groupTrendReviewGuidance: (nextWaveNumber: number) => string[];
		oneWaveTitle: (nextWaveNumber: number) => string;
		oneWaveDescription: string;
		reviewWaveResultsLabel: (waveNumber: number) => string;
		planWaveLaterLabel: (waveNumber: number) => string;
		oneWaveGuidance: (nextWaveNumber: number) => string[];
		checkReadinessTitle: string;
		checkReadinessDescription: string;
		runChecksBelowLabel: string;
		reviewResultsLabel: string;
		checkReadinessGuidance: string[];
		sameRespondentTitle: string;
		sameRespondentDescription: string;
		runLinkedChecksBelowLabel: string;
		sameRespondentGuidance: string[];
	};
	groupTrend: {
		notReadyTitle: string;
		notReadyDescription: string;
		sameRespondentComparisonLabel: string;
		notReadySameRespondentValue: string;
		disclosureStatusLabel: string;
		notReadyDisclosureValue: string;
		notReadyGuidance: string[];
		title: (baselineName: string, comparisonName: string) => string;
		readyDescription: string;
		pendingDescription: string;
		firstWaveScoresLabel: string;
		secondWaveScoresLabel: string;
		runComparisonChecksValue: string;
		notConfiguredValue: string;
		disclosureNotAvailableValue: string;
		suppressedLinkedComparisonsLabel: string;
		openResultsLabel: string;
		readyGuidance: string[];
	};
	comparisonReview: {
		title: string;
		description: string;
	};
	scoreMethodReview: {
		title: string;
		description: string;
	};
	actions: {
		twoWaveProof: WavesWorkflowActionCopy;
		waveComparisonProof: WavesWorkflowActionCopy;
	};
	disabled: {
		unlinkedWavesUseGroupTrend: string;
		addRepeatedWaves: string;
		chooseBaselineAndComparison: string;
		checkReadinessBeforeReview: string;
	};
	inactiveReason: {
		groupTrend: string;
		noWaves: string;
		oneWave: string;
		needScoredResponses: string;
	};
};

export const defaultSelectedSeriesWavesWorkflowCopy: SelectedSeriesWavesWorkflowCopy = {
	locale: 'en',
	stepNumber: (number) => `Step ${number}`,
	plan: {
		createFirstTitle: 'Create the first wave',
		createFirstDescription: 'Start by creating Wave 1 as the first collection round for this study.',
		openSetupLabel: 'Open setup',
		createFirstGuidance: [
			'Each wave is a collection round inside this study. Create Wave 1 in Setup, then launch it from Collection.',
			'After responses arrive, review the wave in Results before adding a follow-up wave.',
			'Use anonymous longitudinal from the first wave if you need linked change-over-time comparison later.'
		],
		reviewWavePairTitle: (wavePairTitle) => `Review ${wavePairTitle}`,
		groupTrendReviewDescription:
			'These waves can be reviewed as group-level results. Linked same-respondent change needs repeat participation from the first wave.',
		reviewGroupTrendLabel: 'Review group trend',
		groupTrendReviewGuidance: (nextWaveNumber) => [
			'Review these waves as a group-level trend. Do not describe the change as same-respondent movement because the waves are anonymous.',
			'Use repeat participation from Wave 1 when the study needs linked change-over-time comparison later.',
			`Review or export Wave 1 and Wave 2 before using Setup to create Wave ${nextWaveNumber}.`
		],
		oneWaveTitle: (nextWaveNumber) => `Review Wave 1 before planning Wave ${nextWaveNumber}`,
		oneWaveDescription:
			'Wave 1 exists. Review the current results first; plan a follow-up only when the next collection round is intentional.',
		reviewWaveResultsLabel: (waveNumber) => `Review Wave ${waveNumber} results`,
		planWaveLaterLabel: (waveNumber) => `Plan Wave ${waveNumber} later`,
		oneWaveGuidance: (nextWaveNumber) => [
			`Review or export Wave 1 before using Setup to create Wave ${nextWaveNumber}.`,
			'Use anonymous longitudinal when the same respondent should be linked across waves for change-over-time comparison.',
			'Review recipients before launching the new wave; do not assume the recipient list is unchanged unless Collection shows it.'
		],
		checkReadinessTitle: 'Check comparison readiness',
		checkReadinessDescription:
			'Two longitudinal waves exist. Now confirm linked trajectories and scoring compatibility.',
		runChecksBelowLabel: 'Run checks below',
		reviewResultsLabel: 'Review results',
		checkReadinessGuidance: [
			'Use the checks below to confirm both waves can be linked safely.',
			'Results remain wave-by-wave until linked trajectories, disclosure, and scoring compatibility are ready.',
			'If the comparison is blocked, use the details section to see which prerequisite is missing.'
		],
		sameRespondentTitle: 'Check same-respondent change',
		sameRespondentDescription:
			'Two repeat-participation waves exist. Run the comparison checks before treating this as same-respondent change.',
		runLinkedChecksBelowLabel: 'Run linked checks below',
		sameRespondentGuidance: [
			'Use the comparison checks below to confirm linked responses, disclosure, scoring compatibility, and visible deltas before making change-over-time claims.',
			'Use Results for wave-level exports; use Waves only when you need reviewed change-over-time context.',
			'Create another follow-up wave from Setup when the next collection round starts.'
		]
	},
	groupTrend: {
		notReadyTitle: 'Group trend not ready',
		notReadyDescription: 'Collect responses in at least two waves before reviewing wave-level trend.',
		sameRespondentComparisonLabel: 'Same-respondent comparison',
		notReadySameRespondentValue: 'Not available until two repeated waves exist',
		disclosureStatusLabel: 'Disclosure status',
		notReadyDisclosureValue: 'Review after follow-up wave results exist',
		notReadyGuidance: [
			'A group trend compares wave-level results. It does not require respondent linking.',
			'Launch and collect a follow-up wave before reading a trend.',
			'Use repeat participation if you need same-respondent change instead of wave-level movement.'
		],
		title: (baselineName, comparisonName) =>
			`Aggregate group trend only: ${baselineName} to ${comparisonName}`,
		readyDescription:
			'Aggregate group-level results are ready to review as a trend. This is not same-respondent change.',
		pendingDescription:
			'Both waves have responses. Finish score output before treating the trend as ready.',
		firstWaveScoresLabel: 'First wave scores',
		secondWaveScoresLabel: 'Second wave scores',
		runComparisonChecksValue: 'Run comparison checks before making same-respondent claims',
		notConfiguredValue: 'Not configured for same-respondent linked change',
		disclosureNotAvailableValue: 'Review wave-level disclosure in Results before making claims',
		suppressedLinkedComparisonsLabel: 'Suppressed linked comparisons',
		openResultsLabel: 'Open Results',
		readyGuidance: [
			'Use this for anonymous or unlinked waves where the question is whether the group moved between rounds.',
			'Do not describe this as individual improvement or decline unless linked change is ready.',
			'Review scoring and disclosure in Results before making claims from the trend.'
		]
	},
	comparisonReview: {
		title: 'Comparison plan',
		description:
			'See whether this study is ready for a follow-up wave, aggregate group trend, or same-respondent linked change.'
	},
	scoreMethodReview: {
		title: 'What is being compared?',
		description:
			'Review scoring rules, linked-pair method, compared outputs, missingness, and interpretation limits before using wave change.'
	},
	actions: {
		twoWaveProof: {
			title: 'Check linked change readiness',
			description:
				'Confirm this study has repeat-participation waves and linked responses for same-respondent comparison.'
		},
		waveComparisonProof: {
			title: 'Review linked change',
			description: 'Review disclosure-safe same-respondent change between the selected waves.'
		}
	},
	disabled: {
		unlinkedWavesUseGroupTrend:
			'Linked same-respondent comparison is unavailable because these waves were not created with repeat participation. Review group trend instead.',
		addRepeatedWaves: 'Add at least two repeated waves before comparing change over time.',
		chooseBaselineAndComparison: 'Choose baseline and comparison waves before reviewing change over time.',
		checkReadinessBeforeReview: 'Check comparison readiness before reviewing change over time.'
	},
	inactiveReason: {
		groupTrend:
			'This study supports aggregate group trend only. Linked-change checks are not required and would be misleading here.',
		noWaves: 'Create and collect the first waves before linked-change checks apply.',
		oneWave:
			'Review Wave 1 in Results. Plan Wave 2 from Setup only when the next collection round is intentional.',
		needScoredResponses: 'Collect scored responses in at least two waves before comparison tasks apply.'
	}
};

export function toSelectedSeriesWavePlan(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
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
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace, copy);

	if (!hasAnyWave) {
		return {
			title: copy.plan.createFirstTitle,
			description: copy.plan.createFirstDescription,
			status: 'pending',
			primaryLabel: copy.plan.openSetupLabel,
			primaryHref: setupHref,
			secondaryLabel: null,
			secondaryHref: null,
			guidance: copy.plan.createFirstGuidance
		};
	}

	if (!hasTwoLongitudinalWaves && workspace.summary.campaignCount >= 2 && groupTrendPlan.status !== 'blocked') {
		const wavePairTitle =
			groupTrendPlan.baselineName && groupTrendPlan.comparisonName
				? `${groupTrendPlan.baselineName} and ${groupTrendPlan.comparisonName}`
				: 'the selected waves';

		return {
			title: copy.plan.reviewWavePairTitle(wavePairTitle),
			description: copy.plan.groupTrendReviewDescription,
			status: groupTrendPlan.status,
			primaryLabel: copy.plan.reviewGroupTrendLabel,
			primaryHref: reportsHref,
			secondaryLabel: copy.plan.planWaveLaterLabel(workspace.summary.campaignCount + 1),
			secondaryHref: setupHref,
			guidance: copy.plan.groupTrendReviewGuidance(workspace.summary.campaignCount + 1)
		};
	}

	if (!hasTwoLongitudinalWaves) {
		const nextWaveNumber = workspace.summary.campaignCount + 1;
		return {
			title: copy.plan.oneWaveTitle(nextWaveNumber),
			description: copy.plan.oneWaveDescription,
			status: 'pending',
			primaryLabel: copy.plan.reviewWaveResultsLabel(1),
			primaryHref: reportsHref,
			secondaryLabel: copy.plan.planWaveLaterLabel(nextWaveNumber),
			secondaryHref: setupHref,
			guidance: copy.plan.oneWaveGuidance(nextWaveNumber)
		};
	}

	if (!comparisonReady) {
		return {
			title: copy.plan.checkReadinessTitle,
			description: copy.plan.checkReadinessDescription,
			status: 'pending',
			primaryLabel: copy.plan.runChecksBelowLabel,
			primaryHref: null,
			secondaryLabel: copy.plan.reviewResultsLabel,
			secondaryHref: reportsHref,
			guidance: copy.plan.checkReadinessGuidance
		};
	}

	return {
		title: copy.plan.sameRespondentTitle,
		description: copy.plan.sameRespondentDescription,
		status: 'pending',
		primaryLabel: copy.plan.runLinkedChecksBelowLabel,
		primaryHref: null,
		secondaryLabel: copy.plan.reviewResultsLabel,
		secondaryHref: reportsHref,
		guidance: copy.plan.sameRespondentGuidance
	};
}

export function toSelectedSeriesGroupTrendPlan(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
): SelectedSeriesGroupTrendPlan {
	const setupHref = `/app/campaign-series/${workspace.series.id}/setup`;
	const reportsHref = `/app/campaign-series/${workspace.series.id}/reports`;
	const nextWaveLabel = copy.plan.planWaveLaterLabel(workspace.summary.campaignCount + 1);
	const waves = toGroupTrendWaves(workspace);

	if (waves.length < 2) {
		return {
			title: copy.groupTrend.notReadyTitle,
			description: copy.groupTrend.notReadyDescription,
			status: 'blocked',
			baselineName: waves[0]?.name ?? null,
			comparisonName: null,
			baselineResponseCount: waves[0]?.submittedResponseCount ?? null,
			comparisonResponseCount: null,
			safetyRows: [
				{
					label: copy.groupTrend.sameRespondentComparisonLabel,
					value: copy.groupTrend.notReadySameRespondentValue
				},
				{
					label: copy.groupTrend.disclosureStatusLabel,
					value: copy.groupTrend.notReadyDisclosureValue
				}
			],
			primaryLabel: nextWaveLabel,
			primaryHref: setupHref,
			secondaryLabel: workspace.summary.campaignCount > 0 ? copy.plan.reviewResultsLabel : null,
			secondaryHref: workspace.summary.campaignCount > 0 ? reportsHref : null,
			guidance: copy.groupTrend.notReadyGuidance
		};
	}

	const [baselineWave, comparisonWave] = waves;
	const scoresReady = baselineWave.scoreCount > 0 && comparisonWave.scoreCount > 0;

	return {
		title: copy.groupTrend.title(baselineWave.name, comparisonWave.name),
		description: scoresReady ? copy.groupTrend.readyDescription : copy.groupTrend.pendingDescription,
		status: scoresReady ? 'ready' : 'pending',
		baselineName: baselineWave.name,
		comparisonName: comparisonWave.name,
		baselineResponseCount: baselineWave.submittedResponseCount,
		comparisonResponseCount: comparisonWave.submittedResponseCount,
		safetyRows: [
			{ label: copy.groupTrend.firstWaveScoresLabel, value: String(baselineWave.scoreCount) },
			{ label: copy.groupTrend.secondWaveScoresLabel, value: String(comparisonWave.scoreCount) },
			{
				label: copy.groupTrend.sameRespondentComparisonLabel,
				value:
					workspace.summary.longitudinalWaveCount >= 2
						? copy.groupTrend.runComparisonChecksValue
						: copy.groupTrend.notConfiguredValue
			},
			{
				label: copy.groupTrend.disclosureStatusLabel,
				value:
					workspace.comparison.disclosureState === 'not_available'
						? copy.groupTrend.disclosureNotAvailableValue
						: workspace.comparison.disclosureState.replaceAll('_', ' ')
			},
			{
				label: copy.groupTrend.suppressedLinkedComparisonsLabel,
				value: String(workspace.summary.suppressedComparisonCount)
			}
		],
		primaryLabel: copy.groupTrend.openResultsLabel,
		primaryHref: reportsHref,
		secondaryLabel: nextWaveLabel,
		secondaryHref: setupHref,
		guidance: copy.groupTrend.readyGuidance
	};
}

export function toSelectedSeriesWaveComparisonReview(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveComparisonReview {
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace, copy);
	const sameRespondentReady = isSameRespondentComparisonReady(workspace);
	const groupTrendReady = groupTrendPlan.status === 'ready';
	const items: SelectedSeriesWaveComparisonReviewItem[] = [
		toWaveSequenceReviewItem(workspace, copy),
		toComparisonTypeReviewItem(workspace, groupTrendPlan, sameRespondentReady, copy),
		toDataReadinessReviewItem(workspace, groupTrendPlan, sameRespondentReady, copy),
		toClaimBoundaryReviewItem(workspace, groupTrendPlan, sameRespondentReady, copy)
	];

	return {
		title: copy.comparisonReview.title,
		description: copy.comparisonReview.description,
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
	comparisonProof: CampaignSeriesWaveComparisonProofResponse | null = null,
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReview {
	const proofScores = comparisonProof?.scores ?? [];
	const interpretationReviewed = isInterpretationValidated(
		comparisonProof?.interpretationStatus ?? workspace.comparison.interpretationStatus
	);
	const hasIncompleteInputs = proofScores.some(hasIncompleteComparisonInputs);
	const hasAnyWave = workspace.summary.campaignCount > 0;
	const hasRepeatedWaves = workspace.summary.longitudinalWaveCount >= 2;
	const items: SelectedSeriesWaveScoreMethodReviewItem[] = [
		toWaveScoringRulesItem(workspace, copy),
		toWaveComparisonMethodItem(workspace, copy),
		toWaveComparedOutputsItem(
			hasRepeatedWaves,
			workspace.comparison.visibleScoreCount,
			proofScores,
			copy
		),
		toWaveMissingnessItem(hasRepeatedWaves, proofScores, copy),
		toWaveInterpretationBoundaryItem(hasAnyWave, hasRepeatedWaves, interpretationReviewed, copy)
	];

	return {
		title: copy.scoreMethodReview.title,
		description: copy.scoreMethodReview.description,
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
	localState: SelectedSeriesWavesWorkflowLocalState = {},
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
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
			step: copy.stepNumber(1),
			title: copy.actions.twoWaveProof.title,
			description: copy.actions.twoWaveProof.description,
			status: toTwoWaveProofStatus(hasTwoLongitudinalWaves, twoWaveProofViewed),
			available: hasTwoLongitudinalWaves,
			disabledReason: hasTwoLongitudinalWaves
				? null
				: toTwoWaveProofDisabledReason(workspace, copy)
		},
		{
			id: 'waveComparisonProof',
			step: copy.stepNumber(2),
			title: copy.actions.waveComparisonProof.title,
			description: copy.actions.waveComparisonProof.description,
			status: toWaveComparisonStatus(
				hasSelectedComparison,
				comparisonProofReady,
				waveComparisonProofViewed
			),
			available: comparisonProofReady,
			disabledReason: toWaveComparisonDisabledReason(hasSelectedComparison, comparisonProofReady, copy)
		}
	];
}

export function toSelectedSeriesWavesPath(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWavesWorkflowLocalState = {},
	copy: SelectedSeriesWavesWorkflowCopy = defaultSelectedSeriesWavesWorkflowCopy
): SelectedSeriesWavesPath {
	const actions = toSelectedSeriesWavesWorkflowActions(workspace, localState, copy);
	const groupTrendPlan = toSelectedSeriesGroupTrendPlan(workspace, copy);
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
		inactiveReason: toWavesPathInactiveReason(workspace, mode, copy)
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
	mode: SelectedSeriesWavesPathMode,
	copy: SelectedSeriesWavesWorkflowCopy
) {
	if (mode === 'linked_change') {
		return null;
	}

	if (mode === 'group_trend') {
		return copy.inactiveReason.groupTrend;
	}

	if (workspace.summary.campaignCount === 0) {
		return copy.inactiveReason.noWaves;
	}

	if (workspace.summary.campaignCount === 1) {
		return copy.inactiveReason.oneWave;
	}

	return copy.inactiveReason.needScoredResponses;
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

function toTwoWaveProofDisabledReason(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy
) {
	if (workspace.summary.campaignCount >= 2) {
		return copy.disabled.unlinkedWavesUseGroupTrend;
	}

	return copy.disabled.addRepeatedWaves;
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
	comparisonProofReady: boolean,
	copy: SelectedSeriesWavesWorkflowCopy
) {
	if (!hasSelectedComparison) {
		return copy.disabled.chooseBaselineAndComparison;
	}

	return comparisonProofReady
		? null
		: copy.disabled.checkReadinessBeforeReview;
}

function waveMessage(
	copy: SelectedSeriesWavesWorkflowCopy,
	id: AppMessageId,
	values: AppMessageValues = {}
) {
	return appMessage(copy.locale ?? 'en', id, values);
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
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveComparisonReviewItem {
	if (workspace.summary.campaignCount === 0) {
		return {
			id: 'wave_sequence',
			label: waveMessage(copy, 'waves.review.waveSequence.label'),
			status: 'blocked',
			summary: waveMessage(copy, 'waves.review.waveSequence.noWave.summary'),
			detail: waveMessage(copy, 'waves.review.waveSequence.noWave.detail')
		};
	}

	if (workspace.summary.campaignCount === 1) {
		return {
			id: 'wave_sequence',
			label: waveMessage(copy, 'waves.review.waveSequence.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.review.waveSequence.oneWave.summary'),
			detail: waveMessage(copy, 'waves.review.waveSequence.oneWave.detail')
		};
	}

	return {
		id: 'wave_sequence',
		label: waveMessage(copy, 'waves.review.waveSequence.label'),
		status: 'ready',
		summary: waveMessage(copy, 'waves.review.waveSequence.multiple.summary', {
			count: workspace.summary.campaignCount
		}),
		detail: waveMessage(copy, 'waves.review.waveSequence.multiple.detail')
	};
}

function toComparisonTypeReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveComparisonReviewItem {
	if (workspace.summary.campaignCount < 2) {
		return {
			id: 'comparison_type',
			label: waveMessage(copy, 'waves.review.comparisonType.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.review.comparisonType.noComparison.summary'),
			detail: waveMessage(copy, 'waves.review.comparisonType.noComparison.detail')
		};
	}

	if (sameRespondentReady) {
		return {
			id: 'comparison_type',
			label: waveMessage(copy, 'waves.review.comparisonType.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.review.comparisonType.sameRespondent.summary'),
			detail: waveMessage(copy, 'waves.review.comparisonType.sameRespondent.detail')
		};
	}

	if (workspace.summary.longitudinalWaveCount >= 2) {
		return {
			id: 'comparison_type',
			label: waveMessage(copy, 'waves.review.comparisonType.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.review.comparisonType.linkedNeedsChecks.summary'),
			detail: waveMessage(copy, 'waves.review.comparisonType.linkedNeedsChecks.detail')
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		return {
			id: 'comparison_type',
			label: waveMessage(copy, 'waves.review.comparisonType.label'),
			status: groupTrendPlan.status,
			summary: waveMessage(copy, 'waves.review.comparisonType.groupTrend.summary'),
			detail: waveMessage(copy, 'waves.review.comparisonType.groupTrend.detail')
		};
	}

	return {
		id: 'comparison_type',
		label: waveMessage(copy, 'waves.review.comparisonType.label'),
		status: 'blocked',
		summary: waveMessage(copy, 'waves.review.comparisonType.noComparison.summary'),
		detail: waveMessage(copy, 'waves.review.comparisonType.collectTwo.detail')
	};
}

function toDataReadinessReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveComparisonReviewItem {
	if (sameRespondentReady) {
		return {
			id: 'data_readiness',
			label: waveMessage(copy, 'waves.review.dataReadiness.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.review.dataReadiness.linkedReady.summary', {
				linkedPairs: workspace.comparison.linkedPairCount,
				visibleScores: workspace.comparison.visibleScoreCount
			}),
			detail: waveMessage(copy, 'waves.review.dataReadiness.linkedReady.detail')
		};
	}

	if (workspace.summary.campaignCount < 2) {
		return {
			id: 'data_readiness',
			label: waveMessage(copy, 'waves.review.dataReadiness.label'),
			status: 'blocked',
			summary: waveMessage(copy, 'waves.review.dataReadiness.followUpFirst.summary'),
			detail: waveMessage(copy, 'waves.review.dataReadiness.followUpFirst.detail')
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		const firstScores = groupTrendPlan.safetyRows.find(
			(row) => row.label === copy.groupTrend.firstWaveScoresLabel
		);
		const secondScores = groupTrendPlan.safetyRows.find(
			(row) => row.label === copy.groupTrend.secondWaveScoresLabel
		);

		return {
			id: 'data_readiness',
			label: waveMessage(copy, 'waves.review.dataReadiness.label'),
			status: groupTrendPlan.status,
			summary:
				groupTrendPlan.status === 'ready'
					? waveMessage(copy, 'waves.review.dataReadiness.groupTrendReady.summary', {
							firstScores: Number(firstScores?.value ?? 0),
							secondScores: Number(secondScores?.value ?? 0)
						})
					: waveMessage(copy, 'waves.review.dataReadiness.groupTrendPending.summary'),
			detail: waveMessage(copy, 'waves.review.dataReadiness.groupTrend.detail')
		};
	}

	return {
		id: 'data_readiness',
		label: waveMessage(copy, 'waves.review.dataReadiness.label'),
		status: 'pending',
		summary: waveMessage(copy, 'waves.review.dataReadiness.comparisonNotReady.summary'),
		detail: waveMessage(copy, 'waves.review.dataReadiness.comparisonNotReady.detail')
	};
}

function toClaimBoundaryReviewItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	groupTrendPlan: SelectedSeriesGroupTrendPlan,
	sameRespondentReady: boolean,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveComparisonReviewItem {
	if (sameRespondentReady) {
		return {
			id: 'claim_boundary',
			label: waveMessage(copy, 'waves.review.claimBoundary.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.review.claimBoundary.sameReady.summary'),
			detail: waveMessage(copy, 'waves.review.claimBoundary.sameReady.detail')
		};
	}

	if (groupTrendPlan.status !== 'blocked') {
		return {
			id: 'claim_boundary',
			label: waveMessage(copy, 'waves.review.claimBoundary.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.review.claimBoundary.groupTrend.summary'),
			detail: waveMessage(copy, 'waves.review.claimBoundary.groupTrend.detail')
		};
	}

	if (workspace.summary.campaignCount === 1) {
		return {
			id: 'claim_boundary',
			label: waveMessage(copy, 'waves.review.claimBoundary.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.review.claimBoundary.oneWave.summary'),
			detail: waveMessage(copy, 'waves.review.claimBoundary.oneWave.detail')
		};
	}

	return {
		id: 'claim_boundary',
		label: waveMessage(copy, 'waves.review.claimBoundary.label'),
		status: 'blocked',
		summary: waveMessage(copy, 'waves.review.claimBoundary.noClaim.summary'),
		detail: waveMessage(copy, 'waves.review.claimBoundary.noClaim.detail')
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
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReviewItem {
	const baseline = workspace.selectedBaselineWave;
	const comparison = workspace.selectedComparisonWave;

	if (!baseline || !comparison) {
		return {
			id: 'scoring_rules',
			label: waveMessage(copy, 'waves.method.scoringRules.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.scoringRules.twoWavesNeeded.summary'),
			detail: waveMessage(copy, 'waves.method.scoringRules.twoWavesNeeded.detail')
		};
	}

	const baselineRule = formatRuleVersion(baseline.scoringRuleKey, baseline.scoringRuleVersion, copy);
	const comparisonRule = formatRuleVersion(
		comparison.scoringRuleKey,
		comparison.scoringRuleVersion,
		copy
	);
	const sameRule =
		baselineRule === comparisonRule &&
		baselineRule !== waveMessage(copy, 'waves.method.rule.notConfigured');

	return {
		id: 'scoring_rules',
		label: waveMessage(copy, 'waves.method.scoringRules.label'),
		status: sameRule ? 'ready' : 'pending',
		summary: sameRule
			? waveMessage(copy, 'waves.method.scoringRules.sameRule.summary', { rule: baselineRule })
			: waveMessage(copy, 'waves.method.scoringRules.different.summary', {
					baselineRule,
					comparisonRule
				}),
		detail: sameRule
			? waveMessage(copy, 'waves.method.scoringRules.sameRule.detail')
			: waveMessage(copy, 'waves.method.scoringRules.different.detail')
	};
}

function toWaveComparisonMethodItem(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReviewItem {
	if (workspace.summary.longitudinalWaveCount < 2) {
		return {
			id: 'comparison_method',
			label: waveMessage(copy, 'waves.method.comparisonMethod.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.comparisonMethod.waveOnly.summary'),
			detail: waveMessage(copy, 'waves.method.comparisonMethod.waveOnly.detail')
		};
	}

	const visibleScores = workspace.comparison.visibleScoreCount;
	const linkedPairs = workspace.comparison.linkedPairCount;

	return {
		id: 'comparison_method',
		label: waveMessage(copy, 'waves.method.comparisonMethod.label'),
		status: linkedPairs > 0 && visibleScores > 0 ? 'ready' : 'pending',
		summary: waveMessage(copy, 'waves.method.comparisonMethod.linked.summary', {
			linkedPairs,
			visibleScores
		}),
		detail: waveMessage(copy, 'waves.method.comparisonMethod.linked.detail')
	};
}

function toWaveComparedOutputsItem(
	hasRepeatedWaves: boolean,
	visibleScoreCount: number,
	proofScores: WaveScoreComparisonResponse[],
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasRepeatedWaves) {
		return {
			id: 'outputs',
			label: waveMessage(copy, 'waves.method.outputs.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.outputs.noRepeated.summary'),
			detail: waveMessage(copy, 'waves.method.outputs.noRepeated.detail')
		};
	}

	if (proofScores.length > 0) {
		return {
			id: 'outputs',
			label: waveMessage(copy, 'waves.method.outputs.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.method.outputs.ready.summary', {
				count: proofScores.length,
				outputs: proofScores.map((score) => score.dimensionCode).join(', ')
			}),
			detail: waveMessage(copy, 'waves.method.outputs.ready.detail')
		};
	}

	return {
		id: 'outputs',
		label: waveMessage(copy, 'waves.method.outputs.label'),
		status: visibleScoreCount > 0 ? 'pending' : 'blocked',
		summary:
			visibleScoreCount > 0
				? waveMessage(copy, 'waves.method.outputs.pending.summary')
				: waveMessage(copy, 'waves.method.outputs.blocked.summary'),
		detail: waveMessage(copy, 'waves.method.outputs.review.detail')
	};
}

function toWaveMissingnessItem(
	hasRepeatedWaves: boolean,
	proofScores: WaveScoreComparisonResponse[],
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasRepeatedWaves) {
		return {
			id: 'missingness',
			label: waveMessage(copy, 'waves.method.missingness.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.missingness.noRepeated.summary'),
			detail: waveMessage(copy, 'waves.method.missingness.noRepeated.detail')
		};
	}

	if (proofScores.length === 0) {
		return {
			id: 'missingness',
			label: waveMessage(copy, 'waves.method.missingness.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.missingness.pending.summary'),
			detail: waveMessage(copy, 'waves.method.missingness.pending.detail')
		};
	}

	const incomplete = proofScores.flatMap((score) => toIncompleteComparisonInputSummaries(score, copy));

	if (incomplete.length === 0) {
		return {
			id: 'missingness',
			label: waveMessage(copy, 'waves.method.missingness.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.method.missingness.complete.summary'),
			detail: waveMessage(copy, 'waves.method.missingness.complete.detail')
		};
	}

	return {
		id: 'missingness',
		label: waveMessage(copy, 'waves.method.missingness.label'),
		status: 'pending',
		summary: waveMessage(copy, 'waves.method.missingness.incomplete.summary'),
		detail: incomplete.join('; ')
	};
}

function toWaveInterpretationBoundaryItem(
	hasAnyWave: boolean,
	hasRepeatedWaves: boolean,
	interpretationReviewed: boolean,
	copy: SelectedSeriesWavesWorkflowCopy
): SelectedSeriesWaveScoreMethodReviewItem {
	if (!hasAnyWave) {
		return {
			id: 'interpretation_boundary',
			label: waveMessage(copy, 'waves.method.interpretationBoundary.label'),
			status: 'not_available',
			summary: waveMessage(copy, 'waves.method.interpretation.noWave.summary'),
			detail: waveMessage(copy, 'waves.method.interpretation.noWave.detail')
		};
	}

	if (!hasRepeatedWaves) {
		return {
			id: 'interpretation_boundary',
			label: waveMessage(copy, 'waves.method.interpretationBoundary.label'),
			status: 'pending',
			summary: waveMessage(copy, 'waves.method.interpretation.waveOnly.summary'),
			detail: waveMessage(copy, 'waves.method.interpretation.waveOnly.detail')
		};
	}

	if (interpretationReviewed) {
		return {
			id: 'interpretation_boundary',
			label: waveMessage(copy, 'waves.method.interpretationBoundary.label'),
			status: 'ready',
			summary: waveMessage(copy, 'waves.method.interpretation.ready.summary'),
			detail: waveMessage(copy, 'waves.method.interpretation.ready.detail')
		};
	}

	return {
		id: 'interpretation_boundary',
		label: waveMessage(copy, 'waves.method.interpretationBoundary.label'),
		status: 'pending',
		summary: waveMessage(copy, 'waves.method.interpretation.custom.summary'),
		detail: waveMessage(copy, 'waves.method.interpretation.custom.detail')
	};
}

function formatRuleVersion(
	key: string | null | undefined,
	version: string | null | undefined,
	copy: SelectedSeriesWavesWorkflowCopy
) {
	if (!key) {
		return waveMessage(copy, 'waves.method.rule.notConfigured');
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
	return toIncompleteComparisonInputSummaries(
		score,
		defaultSelectedSeriesWavesWorkflowCopy
	).length > 0;
}

function toIncompleteComparisonInputSummaries(
	score: WaveScoreComparisonResponse,
	copy: SelectedSeriesWavesWorkflowCopy
) {
	const summaries: string[] = [];

	if (
		typeof score.baselineNValidTotal === 'number' &&
		typeof score.baselineNExpectedTotal === 'number' &&
		score.baselineNValidTotal < score.baselineNExpectedTotal
	) {
		summaries.push(
			waveMessage(copy, 'waves.method.missingness.incomplete.baseline', {
				dimension: score.dimensionCode,
				valid: score.baselineNValidTotal,
				expected: score.baselineNExpectedTotal
			})
		);
	}

	if (
		typeof score.comparisonNValidTotal === 'number' &&
		typeof score.comparisonNExpectedTotal === 'number' &&
		score.comparisonNValidTotal < score.comparisonNExpectedTotal
	) {
		summaries.push(
			waveMessage(copy, 'waves.method.missingness.incomplete.followUp', {
				dimension: score.dimensionCode,
				valid: score.comparisonNValidTotal,
				expected: score.comparisonNExpectedTotal
			})
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

function toTimestamp(value: string | null | undefined) {
	if (!value) {
		return 0;
	}

	const timestamp = Date.parse(value);
	return Number.isFinite(timestamp) ? timestamp : 0;
}
