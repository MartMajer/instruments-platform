import { describe, expect, it } from 'vitest';
import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import {
	toSelectedSeriesWaveComparisonSnapshotState,
	toSelectedSeriesWaveDashboardView
} from './wave-comparison-snapshot';

describe('selected-series wave comparison snapshot model', () => {
	it('marks the snapshot not available when selected waves are missing', () => {
		const state = toSelectedSeriesWaveComparisonSnapshotState(emptyWorkspace);

		expect(state).toEqual({
			status: 'not_available',
			available: false,
			seriesId: 'series-id',
			baselineWaveName: null,
			comparisonWaveName: null,
			badgeLabel: 'Not available',
			disabledReason: 'Select two comparable waves before loading the wave comparison snapshot.'
		});
	});

	it('blocks the snapshot when selected waves are not comparison-ready', () => {
		const state = toSelectedSeriesWaveComparisonSnapshotState(twoWavePendingWorkspace);

		expect(state).toEqual({
			status: 'blocked',
			available: false,
			seriesId: 'series-id',
			baselineWaveName: 'Pulse wave 1',
			comparisonWaveName: 'Pulse wave 2',
			badgeLabel: 'Blocked',
			disabledReason: 'Run the linked trajectory check before loading the wave comparison snapshot.'
		});
	});

	it('allows a proof-ready comparison before the snapshot is loaded', () => {
		const state = toSelectedSeriesWaveComparisonSnapshotState(comparisonReadyWorkspace);

		expect(state).toEqual({
			status: 'pending',
			available: true,
			seriesId: 'series-id',
			baselineWaveName: 'Pulse wave 1',
			comparisonWaveName: 'Pulse wave 2',
			badgeLabel: 'Proof-only',
			disabledReason: null
		});
	});

	it('marks the selected series snapshot ready after it loads', () => {
		const state = toSelectedSeriesWaveComparisonSnapshotState(comparisonReadyWorkspace, {
			loadedSeriesId: 'series-id'
		});

		expect(state).toMatchObject({
			status: 'ready',
			available: true,
			seriesId: 'series-id',
			baselineWaveName: 'Pulse wave 1',
			comparisonWaveName: 'Pulse wave 2',
			badgeLabel: 'Proof/local',
			disabledReason: null
		});
	});

	it('keeps a proof-ready comparison pending when the loaded snapshot belongs to another series', () => {
		const state = toSelectedSeriesWaveComparisonSnapshotState(comparisonReadyWorkspace, {
			loadedSeriesId: 'previous-series-id'
		});

		expect(state).toMatchObject({
			status: 'pending',
			available: true,
			seriesId: 'series-id',
			badgeLabel: 'Proof-only',
			disabledReason: null
		});
	});

	it('maps selected waves into wave dashboard sections', () => {
		const dashboard = toSelectedSeriesWaveDashboardView(comparisonReadyWorkspace, {
			loadedSeriesId: 'series-id'
		});

		expect(dashboard).toMatchObject({
			title: 'Pulse wave 1 vs Pulse wave 2 wave dashboard',
			status: 'ready',
			badgeLabel: 'Proof/local',
			available: true,
			emptyMessage: null
		});
		expect(dashboard.readinessRows).toEqual([
			{ label: 'Baseline wave', value: 'Pulse wave 1' },
			{ label: 'Baseline status', value: 'live' },
			{ label: 'Baseline submitted responses', value: '6' },
			{ label: 'Comparison wave', value: 'Pulse wave 2' },
			{ label: 'Comparison status', value: 'live' },
			{ label: 'Comparison submitted responses', value: '6' },
			{ label: 'Linked trajectories', value: '6' },
			{ label: 'Complete trajectories', value: '6' }
		]);
		expect(dashboard.comparisonRows).toEqual([
			{ label: 'Proof status', value: 'proof only' },
			{ label: 'Interpretation', value: 'not validated interpretation' },
			{ label: 'Linked pairs', value: '6' }
		]);
		expect(dashboard.guardrailRows).toEqual([
			{ label: 'Disclosure', value: 'visible' },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Compatibility', value: 'compatible' },
			{ label: 'Visible scores', value: '1' },
			{ label: 'Suppressed scores', value: '0' },
			{ label: 'Blocked scores', value: '0' }
		]);
		expect(dashboard.provenanceRows).toEqual([
			{ label: 'Baseline launch snapshot', value: 'baseline-launch-id', mono: true },
			{ label: 'Baseline latest launch', value: '2026-05-05T08:30:00Z' },
			{ label: 'Baseline scoring rule', value: 'burnout.total 1.0.0' },
			{ label: 'Baseline disclosure policy', value: 'disclosure-id', mono: true },
			{ label: 'Comparison launch snapshot', value: 'comparison-launch-id', mono: true },
			{ label: 'Comparison latest launch', value: '2026-05-12T08:30:00Z' },
			{ label: 'Comparison scoring rule', value: 'burnout.total 1.0.0' },
			{ label: 'Comparison disclosure policy', value: 'disclosure-id', mono: true }
		]);
	});

	it('maps missing selected waves into an unavailable wave dashboard', () => {
		const dashboard = toSelectedSeriesWaveDashboardView(emptyWorkspace);

		expect(dashboard).toMatchObject({
			title: 'Wave dashboard unavailable',
			status: 'not_available',
			badgeLabel: 'Not available',
			available: false,
			emptyMessage: 'Select two comparable waves before reviewing the wave dashboard.'
		});
		expect(dashboard.readinessRows).toEqual([
			{ label: 'Campaigns', value: '0' },
			{ label: 'Longitudinal waves', value: '0' },
			{ label: 'Submitted waves', value: '0' },
			{ label: 'Missing prerequisites', value: '1' }
		]);
		expect(dashboard.comparisonRows).toEqual([]);
		expect(dashboard.guardrailRows).toEqual([]);
		expect(dashboard.provenanceRows).toEqual([]);
	});
});

const emptyWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	series: {
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 0,
		liveCampaignCount: 0,
		longitudinalWaveCount: 0,
		submittedWaveCount: 0,
		linkedTrajectoryCount: 0,
		completeTrajectoryCount: 0,
		comparableScoreCount: 0,
		visibleComparisonCount: 0,
		suppressedComparisonCount: 0,
		blockedComparisonCount: 0,
		missingPrerequisiteCount: 1
	},
	selectedBaselineWave: null,
	selectedComparisonWave: null,
	comparison: {
		status: 'not_available',
		disclosureState: 'not_available',
		compatibilityState: 'not_available',
		interpretationStatus: 'not_available',
		disclosureKMin: null,
		linkedPairCount: 0,
		visibleScoreCount: 0,
		suppressedScoreCount: 0,
		blockedScoreCount: 0
	},
	missingPrerequisites: [],
	waves: []
};

const baselineWave = {
	id: 'baseline-wave-id',
	name: 'Pulse wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous_longitudinal',
	defaultLocale: 'en',
	latestLaunchSnapshotId: 'baseline-launch-id',
	latestLaunchAt: '2026-05-05T08:30:00Z',
	scoringRuleId: 'scoring-rule-id',
	scoringRuleKey: 'burnout.total',
	scoringRuleVersion: '1.0.0',
	disclosurePolicyId: 'disclosure-id',
	disclosureKMin: 5,
	submittedResponseCount: 6,
	scoreCount: 6,
	linkedTrajectoryCount: 6,
	waveState: 'wave'
};

const comparisonWave = {
	...baselineWave,
	id: 'comparison-wave-id',
	name: 'Pulse wave 2',
	latestLaunchSnapshotId: 'comparison-launch-id',
	latestLaunchAt: '2026-05-12T08:30:00Z'
};

const twoWavePendingWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		...emptyWorkspace.summary,
		campaignCount: 2,
		liveCampaignCount: 2,
		longitudinalWaveCount: 2,
		submittedWaveCount: 2,
		linkedTrajectoryCount: 6,
		completeTrajectoryCount: 6
	},
	selectedBaselineWave: baselineWave,
	selectedComparisonWave: comparisonWave,
	waves: [baselineWave, comparisonWave]
};

const comparisonReadyWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	...twoWavePendingWorkspace,
	summary: {
		...twoWavePendingWorkspace.summary,
		comparableScoreCount: 1,
		visibleComparisonCount: 1
	},
	comparison: {
		...twoWavePendingWorkspace.comparison,
		status: 'proof_only',
		disclosureState: 'visible',
		compatibilityState: 'compatible',
		interpretationStatus: 'not_validated_interpretation',
		disclosureKMin: 5,
		linkedPairCount: 6,
		visibleScoreCount: 1
	}
};
