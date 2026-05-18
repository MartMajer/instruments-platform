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

export function toSelectedSeriesWaveComparisonSnapshotState(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWaveComparisonSnapshotLocalState = {}
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
			badgeLabel: 'Not available',
			disabledReason: 'Select two comparable waves before loading the wave comparison snapshot.'
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
			badgeLabel: 'Blocked',
			disabledReason: 'Run the linked trajectory check before loading the wave comparison snapshot.'
		};
	}

	const loadedForSelectedSeries = localState.loadedSeriesId === workspace.series.id;

	return {
		status: loadedForSelectedSeries ? 'ready' : 'pending',
		available: true,
		seriesId: workspace.series.id,
		baselineWaveName: baselineWave.name,
		comparisonWaveName: comparisonWave.name,
		badgeLabel: loadedForSelectedSeries ? 'Proof/local' : 'Proof-only',
		disabledReason: null
	};
}

export function toSelectedSeriesWaveDashboardView(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	localState: SelectedSeriesWaveComparisonSnapshotLocalState = {}
): SelectedSeriesWaveDashboardView {
	const snapshotState = toSelectedSeriesWaveComparisonSnapshotState(workspace, localState);
	const baselineWave = workspace.selectedBaselineWave;
	const comparisonWave = workspace.selectedComparisonWave;

	if (!baselineWave || !comparisonWave) {
		return {
			title: 'Wave dashboard unavailable',
			status: snapshotState.status,
			available: false,
			badgeLabel: snapshotState.badgeLabel,
			emptyMessage: 'Select two comparable waves before reviewing the wave dashboard.',
			readinessRows: [
				{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
				{ label: 'Longitudinal waves', value: formatCount(workspace.summary.longitudinalWaveCount) },
				{ label: 'Submitted waves', value: formatCount(workspace.summary.submittedWaveCount) },
				{
					label: 'Missing prerequisites',
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			],
			comparisonRows: [],
			guardrailRows: [],
			provenanceRows: []
		};
	}

	return {
		title: `${waveName(baselineWave)} vs ${waveName(comparisonWave)} wave dashboard`,
		status: snapshotState.status,
		available: snapshotState.available,
		badgeLabel: snapshotState.badgeLabel,
		emptyMessage: snapshotState.available ? null : snapshotState.disabledReason,
		readinessRows: toWaveReadinessRows(workspace, baselineWave, comparisonWave),
		comparisonRows: toWaveComparisonRows(workspace.comparison),
		guardrailRows: toWaveGuardrailRows(workspace.comparison),
		provenanceRows: [
			...toWaveProvenanceRows('Baseline', baselineWave),
			...toWaveProvenanceRows('Comparison', comparisonWave)
		]
	};
}

function toWaveReadinessRows(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	baselineWave: CampaignSeriesWavesWaveResponse,
	comparisonWave: CampaignSeriesWavesWaveResponse
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: 'Baseline wave', value: waveName(baselineWave) },
		{ label: 'Baseline status', value: formatCodeLabel(baselineWave.status) },
		{
			label: 'Baseline submitted responses',
			value: formatCount(baselineWave.submittedResponseCount)
		},
		{ label: 'Comparison wave', value: waveName(comparisonWave) },
		{ label: 'Comparison status', value: formatCodeLabel(comparisonWave.status) },
		{
			label: 'Comparison submitted responses',
			value: formatCount(comparisonWave.submittedResponseCount)
		},
		{ label: 'Linked trajectories', value: formatCount(workspace.summary.linkedTrajectoryCount) },
		{
			label: 'Complete trajectories',
			value: formatCount(workspace.summary.completeTrajectoryCount)
		}
	];
}

function toWaveComparisonRows(
	comparison: CampaignSeriesWavesComparisonResponse
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: 'Proof status', value: formatCodeLabel(comparison.status) },
		{ label: 'Interpretation', value: formatCodeLabel(comparison.interpretationStatus) },
		{ label: 'Linked pairs', value: formatCount(comparison.linkedPairCount) }
	];
}

function toWaveGuardrailRows(
	comparison: CampaignSeriesWavesComparisonResponse
): SelectedSeriesWaveDashboardRow[] {
	return [
		{ label: 'Disclosure', value: formatCodeLabel(comparison.disclosureState) },
		{
			label: 'Disclosure k',
			value: comparison.disclosureKMin === null ? 'Not available' : String(comparison.disclosureKMin)
		},
		{ label: 'Compatibility', value: formatCodeLabel(comparison.compatibilityState) },
		{ label: 'Visible scores', value: formatCount(comparison.visibleScoreCount) },
		{ label: 'Suppressed scores', value: formatCount(comparison.suppressedScoreCount) },
		{ label: 'Blocked scores', value: formatCount(comparison.blockedScoreCount) }
	];
}

function toWaveProvenanceRows(
	prefix: string,
	wave: CampaignSeriesWavesWaveResponse
): SelectedSeriesWaveDashboardRow[] {
	return [
		idRow(`${prefix} launch snapshot`, wave.latestLaunchSnapshotId),
		{ label: `${prefix} latest launch`, value: wave.latestLaunchAt ?? 'Not available' },
		{ label: `${prefix} scoring rule`, value: scoringRuleLabel(wave) },
		idRow(`${prefix} disclosure policy`, wave.disclosurePolicyId)
	];
}

function waveName(wave: CampaignSeriesWavesWaveResponse) {
	return wave.name.trim() || 'Untitled wave';
}

function idRow(label: string, value: string | null): SelectedSeriesWaveDashboardRow {
	return value ? { label, value, mono: true } : { label, value: 'Not available' };
}

function scoringRuleLabel(wave: CampaignSeriesWavesWaveResponse) {
	if (wave.scoringRuleKey && wave.scoringRuleVersion) {
		return `${wave.scoringRuleKey} ${wave.scoringRuleVersion}`;
	}

	return wave.scoringRuleId ?? 'Not available';
}

function formatCount(value: number) {
	return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
}

function formatCodeLabel(value: string) {
	return value.replaceAll('_', ' ');
}
