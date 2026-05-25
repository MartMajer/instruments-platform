import type {
	CampaignSeriesWavesComparisonResponse,
	CampaignSeriesWavesWaveResponse,
	CampaignSeriesWavesWorkspaceResponse
} from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesWaveComparisonSnapshotLocalState = {
	loadedSeriesId?: string | null;
};

export type SelectedSeriesWaveComparisonSnapshotState = {
	status: ProductReadModelBadgeStatus;
	available: boolean;
	seriesId: string;
	baselineWaveName: string | null;
	comparisonWaveName: string | null;
	badgeLabel: string;
	disabledReason: string | null;
};

export type SelectedSeriesWaveDashboardRow = {
	label: string;
	value: string;
	mono?: boolean;
};

export type SelectedSeriesWaveDashboardView = {
	title: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	badgeLabel: string;
	emptyMessage: string | null;
	readinessRows: SelectedSeriesWaveDashboardRow[];
	comparisonRows: SelectedSeriesWaveDashboardRow[];
	guardrailRows: SelectedSeriesWaveDashboardRow[];
	provenanceRows: SelectedSeriesWaveDashboardRow[];
};

export type SelectedSeriesWaveSnapshotCopy = {
	status: {
		notAvailable: string;
		blocked: string;
		previewReady: string;
		previewAvailable: string;
	};
	disabled: {
		selectComparableWaves: string;
		runLinkedTrajectoryCheck: string;
	};
	dashboard: {
		unavailableTitle: string;
		unavailableMessage: string;
		title: (baselineName: string, comparisonName: string) => string;
		campaigns: string;
		longitudinalWaves: string;
		submittedWaves: string;
		missingPrerequisites: string;
		baselineWave: string;
		baselineStatus: string;
		baselineSubmittedResponses: string;
		comparisonWave: string;
		comparisonStatus: string;
		comparisonSubmittedResponses: string;
		linkedTrajectories: string;
		completeTrajectories: string;
		previewStatus: string;
		interpretation: string;
		linkedPairs: string;
		disclosure: string;
		disclosureK: string;
		compatibility: string;
		visibleScores: string;
		suppressedScores: string;
		blockedScores: string;
		baselineLaunchSnapshot: string;
		baselineLatestLaunch: string;
		baselineScoringRule: string;
		baselineDisclosurePolicy: string;
		comparisonLaunchSnapshot: string;
		comparisonLatestLaunch: string;
		comparisonScoringRule: string;
		comparisonDisclosurePolicy: string;
		untitledWave: string;
	};
	codeLabels: Record<string, string>;
};

const defaultWaveSnapshotCopy: SelectedSeriesWaveSnapshotCopy = {
	status: {
		notAvailable: 'Not available',
		blocked: 'Blocked',
		previewReady: 'Preview ready',
		previewAvailable: 'Preview available'
	},
	disabled: {
		selectComparableWaves:
			'Select two comparable waves before loading the wave comparison snapshot.',
		runLinkedTrajectoryCheck:
			'Run the linked repeat response check before loading the wave comparison snapshot.'
	},
	dashboard: {
		unavailableTitle: 'Wave dashboard unavailable',
		unavailableMessage: 'Select two comparable waves before reviewing the wave dashboard.',
		title: (baselineName: string, comparisonName: string) =>
			`${baselineName} vs ${comparisonName} wave dashboard`,
		campaigns: 'Campaigns',
		longitudinalWaves: 'Repeat-participation waves',
		submittedWaves: 'Submitted waves',
		missingPrerequisites: 'Missing prerequisites',
		baselineWave: 'Baseline wave',
		baselineStatus: 'Baseline status',
		baselineSubmittedResponses: 'Baseline submitted responses',
		comparisonWave: 'Comparison wave',
		comparisonStatus: 'Comparison status',
		comparisonSubmittedResponses: 'Comparison submitted responses',
		linkedTrajectories: 'Linked repeat responses',
		completeTrajectories: 'Complete repeat-response pairs',
		previewStatus: 'Preview status',
		interpretation: 'Interpretation',
		linkedPairs: 'Linked pairs',
		disclosure: 'Disclosure',
		disclosureK: 'Disclosure k',
		compatibility: 'Compatibility',
		visibleScores: 'Visible scores',
		suppressedScores: 'Suppressed scores',
		blockedScores: 'Blocked scores',
		baselineLaunchSnapshot: 'Baseline launch snapshot',
		baselineLatestLaunch: 'Baseline latest launch',
		baselineScoringRule: 'Baseline scoring rule',
		baselineDisclosurePolicy: 'Baseline disclosure policy',
		comparisonLaunchSnapshot: 'Comparison launch snapshot',
		comparisonLatestLaunch: 'Comparison latest launch',
		comparisonScoringRule: 'Comparison scoring rule',
		comparisonDisclosurePolicy: 'Comparison disclosure policy',
		untitledWave: 'Untitled wave'
	},
	codeLabels: {
		proof_only: 'preview'
	}
};

export function toSelectedSeriesWaveComparisonSnapshotState(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWaveComparisonSnapshotLocalState = {},
	copy: SelectedSeriesWaveSnapshotCopy = defaultWaveSnapshotCopy
): SelectedSeriesWaveComparisonSnapshotState {
	const baselineWave = workspace.selectedBaselineWave;
	const comparisonWave = workspace.selectedComparisonWave;

	if (!baselineWave || !comparisonWave) {
		return {
			status: 'not_available',
			available: false,
			seriesId: workspace.series.id,
			baselineWaveName: baselineWave?.name ?? null,
			comparisonWaveName: comparisonWave?.name ?? null,
			badgeLabel: copy.status.notAvailable,
			disabledReason: copy.disabled.selectComparableWaves
		};
	}

	const comparisonReady = workspace.comparison.status !== 'not_available';

	if (!comparisonReady) {
		return {
			status: 'blocked',
			available: false,
			seriesId: workspace.series.id,
			baselineWaveName: baselineWave.name,
			comparisonWaveName: comparisonWave.name,
			badgeLabel: copy.status.blocked,
			disabledReason: copy.disabled.runLinkedTrajectoryCheck
		};
	}

	const loadedForSelectedSeries = localState.loadedSeriesId === workspace.series.id;

	return {
		status: loadedForSelectedSeries ? 'ready' : 'pending',
		available: true,
		seriesId: workspace.series.id,
		baselineWaveName: baselineWave.name,
		comparisonWaveName: comparisonWave.name,
		badgeLabel: loadedForSelectedSeries ? copy.status.previewReady : copy.status.previewAvailable,
		disabledReason: null
	};
}

export function toSelectedSeriesWaveDashboardView(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWaveComparisonSnapshotLocalState = {},
	copy: SelectedSeriesWaveSnapshotCopy = defaultWaveSnapshotCopy
): SelectedSeriesWaveDashboardView {
	const snapshotState = toSelectedSeriesWaveComparisonSnapshotState(workspace, localState, copy);
	const baselineWave = workspace.selectedBaselineWave;
	const comparisonWave = workspace.selectedComparisonWave;

	if (!baselineWave || !comparisonWave) {
		return {
			title: copy.dashboard.unavailableTitle,
			status: snapshotState.status,
			available: false,
			badgeLabel: snapshotState.badgeLabel,
			emptyMessage: copy.dashboard.unavailableMessage,
			readinessRows: [
				{ label: copy.dashboard.campaigns, value: formatCount(workspace.summary.campaignCount) },
				{
					label: copy.dashboard.longitudinalWaves,
					value: formatCount(workspace.summary.longitudinalWaveCount)
				},
				{ label: copy.dashboard.submittedWaves, value: formatCount(workspace.summary.submittedWaveCount) },
				{
					label: copy.dashboard.missingPrerequisites,
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			],
			comparisonRows: [],
			guardrailRows: [],
			provenanceRows: []
		};
	}

	return {
		title: copy.dashboard.title(waveName(baselineWave, copy), waveName(comparisonWave, copy)),
		status: snapshotState.status,
		available: snapshotState.available,
		badgeLabel: snapshotState.badgeLabel,
		emptyMessage: snapshotState.available ? null : snapshotState.disabledReason,
		readinessRows: toWaveReadinessRows(workspace, baselineWave, comparisonWave, copy),
		comparisonRows: toWaveComparisonRows(workspace.comparison, copy),
		guardrailRows: toWaveGuardrailRows(workspace.comparison, copy),
		provenanceRows: [
			...toWaveProvenanceRows(
				baselineWave,
				{
					launchSnapshot: copy.dashboard.baselineLaunchSnapshot,
					latestLaunch: copy.dashboard.baselineLatestLaunch,
					scoringRule: copy.dashboard.baselineScoringRule,
					disclosurePolicy: copy.dashboard.baselineDisclosurePolicy
				},
				copy
			),
			...toWaveProvenanceRows(
				comparisonWave,
				{
					launchSnapshot: copy.dashboard.comparisonLaunchSnapshot,
					latestLaunch: copy.dashboard.comparisonLatestLaunch,
					scoringRule: copy.dashboard.comparisonScoringRule,
					disclosurePolicy: copy.dashboard.comparisonDisclosurePolicy
				},
				copy
			)
		]
	};
}

function toWaveReadinessRows(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	baselineWave: CampaignSeriesWavesWaveResponse,
	comparisonWave: CampaignSeriesWavesWaveResponse,
	copy: SelectedSeriesWaveSnapshotCopy
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: copy.dashboard.baselineWave, value: waveName(baselineWave, copy) },
		{ label: copy.dashboard.baselineStatus, value: formatCodeLabel(baselineWave.status, copy) },
		{
			label: copy.dashboard.baselineSubmittedResponses,
			value: formatCount(baselineWave.submittedResponseCount)
		},
		{ label: copy.dashboard.comparisonWave, value: waveName(comparisonWave, copy) },
		{ label: copy.dashboard.comparisonStatus, value: formatCodeLabel(comparisonWave.status, copy) },
		{
			label: copy.dashboard.comparisonSubmittedResponses,
			value: formatCount(comparisonWave.submittedResponseCount)
		},
		{ label: copy.dashboard.linkedTrajectories, value: formatCount(workspace.summary.linkedTrajectoryCount) },
		{
			label: copy.dashboard.completeTrajectories,
			value: formatCount(workspace.summary.completeTrajectoryCount)
		}
	];
}

function toWaveComparisonRows(
	comparison: CampaignSeriesWavesComparisonResponse,
	copy: SelectedSeriesWaveSnapshotCopy
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: copy.dashboard.previewStatus, value: formatCodeLabel(comparison.status, copy) },
		{ label: copy.dashboard.interpretation, value: formatCodeLabel(comparison.interpretationStatus, copy) },
		{ label: copy.dashboard.linkedPairs, value: formatCount(comparison.linkedPairCount) }
	];
}

function toWaveGuardrailRows(
	comparison: CampaignSeriesWavesComparisonResponse,
	copy: SelectedSeriesWaveSnapshotCopy
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: copy.dashboard.disclosure, value: formatCodeLabel(comparison.disclosureState, copy) },
		{
			label: copy.dashboard.disclosureK,
			value: comparison.disclosureKMin === null ? copy.status.notAvailable : String(comparison.disclosureKMin)
		},
		{ label: copy.dashboard.compatibility, value: formatCodeLabel(comparison.compatibilityState, copy) },
		{ label: copy.dashboard.visibleScores, value: formatCount(comparison.visibleScoreCount) },
		{ label: copy.dashboard.suppressedScores, value: formatCount(comparison.suppressedScoreCount) },
		{ label: copy.dashboard.blockedScores, value: formatCount(comparison.blockedScoreCount) }
	];
}

function toWaveProvenanceRows(
	wave: CampaignSeriesWavesWaveResponse,
	labels: {
		launchSnapshot: string;
		latestLaunch: string;
		scoringRule: string;
		disclosurePolicy: string;
	},
	copy: SelectedSeriesWaveSnapshotCopy
): SelectedSeriesWaveDashboardRow[] {
	return [
		idRow(labels.launchSnapshot, wave.latestLaunchSnapshotId, copy),
		{ label: labels.latestLaunch, value: wave.latestLaunchAt ?? copy.status.notAvailable },
		{ label: labels.scoringRule, value: scoringRuleLabel(wave, copy) },
		idRow(labels.disclosurePolicy, wave.disclosurePolicyId, copy)
	];
}

function waveName(wave: CampaignSeriesWavesWaveResponse, copy: SelectedSeriesWaveSnapshotCopy) {
	return wave.name.trim() || copy.dashboard.untitledWave;
}

function idRow(
	label: string,
	value: string | null,
	copy: SelectedSeriesWaveSnapshotCopy
): SelectedSeriesWaveDashboardRow {
	return value ? { label, value, mono: true } : { label, value: copy.status.notAvailable };
}

function scoringRuleLabel(
	wave: CampaignSeriesWavesWaveResponse,
	copy: SelectedSeriesWaveSnapshotCopy
) {
	if (wave.scoringRuleKey && wave.scoringRuleVersion) {
		return `${wave.scoringRuleKey} ${wave.scoringRuleVersion}`;
	}

	return wave.scoringRuleId ?? copy.status.notAvailable;
}

function formatCount(value: number) {
	return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
}

function formatCodeLabel(value: string, copy: SelectedSeriesWaveSnapshotCopy) {
	const mapped = copy.codeLabels[value];
	if (mapped) {
		return mapped;
	}

	return value.replaceAll('_', ' ');
}
