import { describe, expect, it } from 'vitest';
import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import { toSelectedSeriesWavesPath, toSelectedSeriesWavesWorkflowActions } from './waves-workflow';

describe('selected-series waves workflow model', () => {
	it('blocks wave actions when the series has no longitudinal waves', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'twoWaveProof',
				status: 'blocked',
				available: false,
				disabledReason:
					'Create at least two anonymous-longitudinal waves before checking linked trajectories.'
			}),
			expect.objectContaining({
				id: 'waveComparisonProof',
				status: 'blocked',
				available: false,
				disabledReason: 'Select two comparable waves before viewing the wave comparison preview.'
			})
		]);
	});

	it('blocks comparison preview when only one longitudinal wave exists', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(oneWaveWorkspace);

		expect(actions.find((action) => action.id === 'twoWaveProof')).toMatchObject({
			status: 'blocked',
			available: false
		});
		expect(actions.find((action) => action.id === 'waveComparisonProof')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Select two comparable waves before viewing the wave comparison preview.'
		});
	});

	it('allows linked trajectory checks and waits on comparison preview until prerequisites are ready', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(twoWavePendingWorkspace);

		expect(actions.find((action) => action.id === 'twoWaveProof')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'waveComparisonProof')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Run the linked trajectory check before viewing the wave comparison preview.'
		});
	});

	it('allows wave comparison preview when the workspace comparison is proof-ready', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(comparisonReadyWorkspace);

		expect(actions.find((action) => action.id === 'twoWaveProof')).toMatchObject({
			status: 'pending',
			available: true
		});
		expect(actions.find((action) => action.id === 'waveComparisonProof')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
	});

	it('uses local action results to advance check and comparison state', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(twoWavePendingWorkspace, {
			twoWaveProofViewed: true,
			waveComparisonProofViewed: true
		});

		expect(actions.find((action) => action.id === 'twoWaveProof')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'waveComparisonProof')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
	});

	it('selects linked trajectory check as the current waves task for comparable waves', () => {
		const path = toSelectedSeriesWavesPath(twoWavePendingWorkspace);

		expect(path.currentActionId).toBe('twoWaveProof');
		expect(path.completedCount).toBe(0);
		expect(path.totalCount).toBe(2);
		expect(path.steps).toEqual([
			expect.objectContaining({ id: 'twoWaveProof', pathState: 'current' }),
			expect.objectContaining({ id: 'waveComparisonProof', pathState: 'blocked' })
		]);
	});

	it('advances the waves path to wave comparison after linked trajectory check is viewed', () => {
		const path = toSelectedSeriesWavesPath(twoWavePendingWorkspace, {
			twoWaveProofViewed: true
		});

		expect(path.currentActionId).toBe('waveComparisonProof');
		expect(path.completedCount).toBe(1);
		expect(path.steps).toEqual([
			expect.objectContaining({ id: 'twoWaveProof', pathState: 'done' }),
			expect.objectContaining({ id: 'waveComparisonProof', pathState: 'current' })
		]);
	});

	it('marks both waves path tasks done after wave comparison preview is viewed', () => {
		const path = toSelectedSeriesWavesPath(twoWavePendingWorkspace, {
			twoWaveProofViewed: true,
			waveComparisonProofViewed: true
		});

		expect(path.currentActionId).toBe('waveComparisonProof');
		expect(path.completedCount).toBe(2);
		expect(path.steps).toEqual([
			expect.objectContaining({ id: 'twoWaveProof', pathState: 'done' }),
			expect.objectContaining({ id: 'waveComparisonProof', pathState: 'done' })
		]);
	});

	it('keeps the waves path current task on blocked linked trajectory check without enough waves', () => {
		const path = toSelectedSeriesWavesPath(emptyWorkspace);

		expect(path.currentActionId).toBe('twoWaveProof');
		expect(path.completedCount).toBe(0);
		expect(path.steps).toEqual([
			expect.objectContaining({ id: 'twoWaveProof', pathState: 'current' }),
			expect.objectContaining({ id: 'waveComparisonProof', pathState: 'blocked' })
		]);
		expect(path.currentAction.disabledReason).toBe(
			'Create at least two anonymous-longitudinal waves before checking linked trajectories.'
		);
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

const oneWaveWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		...emptyWorkspace.summary,
		campaignCount: 1,
		liveCampaignCount: 1,
		longitudinalWaveCount: 1,
		submittedWaveCount: 1,
		linkedTrajectoryCount: 6,
		missingPrerequisiteCount: 1
	},
	selectedBaselineWave: baselineWave,
	waves: [baselineWave]
};

const twoWavePendingWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	...oneWaveWorkspace,
	summary: {
		...oneWaveWorkspace.summary,
		campaignCount: 2,
		liveCampaignCount: 2,
		longitudinalWaveCount: 2,
		submittedWaveCount: 2,
		completeTrajectoryCount: 6
	},
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
