import { describe, expect, it } from 'vitest';
import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
import type { CampaignSeriesWaveComparisonProofResponse } from '$lib/api/setup';
import {
	toSelectedSeriesGroupTrendPlan,
	toSelectedSeriesWaveScoreMethodReview,
	toSelectedSeriesWaveComparisonReview,
	toSelectedSeriesWavePlan,
	toSelectedSeriesWavesPath,
	toSelectedSeriesWavesWorkflowActions
} from './waves-workflow';
import { routePageCopy } from '../i18n/route-copy';

describe('selected-series waves workflow model', () => {
	it('maps an empty study into first-wave setup guidance', () => {
		const plan = toSelectedSeriesWavePlan(emptyWorkspace);

		expect(plan).toMatchObject({
			title: 'Create the first wave',
			primaryLabel: 'Open setup',
			primaryHref: '/app/campaign-series/series-id/setup',
			status: 'pending'
		});
		expect(plan.guidance).toContain(
			'Each wave is a collection round inside this study. Create Wave 1 in Setup, then launch it from Collection.'
		);
	});

	it('maps a one-wave study into review-first next-wave guidance', () => {
		const plan = toSelectedSeriesWavePlan(oneWaveWorkspace);

		expect(plan).toMatchObject({
			title: 'Review Wave 1 before planning Wave 2',
			primaryLabel: 'Review Wave 1 results',
			primaryHref: '/app/campaign-series/series-id/reports',
			secondaryLabel: 'Plan Wave 2 later',
			secondaryHref: '/app/campaign-series/series-id/setup',
			status: 'pending'
		});
		expect(plan.guidance).toContain(
			'Review or export Wave 1 before using Setup to create Wave 2.'
		);
	});

	it('maps a two-wave study into comparison guidance', () => {
		const plan = toSelectedSeriesWavePlan(comparisonReadyWorkspace);

		expect(plan).toMatchObject({
			title: 'Check same-respondent change',
			primaryLabel: 'Run linked checks below',
			primaryHref: null,
			secondaryLabel: 'Review results',
			secondaryHref: '/app/campaign-series/series-id/reports',
			status: 'pending'
		});
		expect(plan.guidance).toContain(
			'Use the comparison checks below to confirm linked responses, disclosure, scoring compatibility, and visible deltas before making change-over-time claims.'
		);
	});

	it('maps two anonymous waves into group trend guidance instead of next-wave setup as the primary action', () => {
		const plan = toSelectedSeriesWavePlan(twoAnonymousClosedWorkspace);

		expect(plan).toMatchObject({
			title: 'Review Wave 1 and Wave 2',
			primaryLabel: 'Review group trend',
			primaryHref: '/app/campaign-series/series-id/reports',
			secondaryLabel: 'Plan Wave 3 later',
			secondaryHref: '/app/campaign-series/series-id/setup',
			status: 'ready'
		});
		expect(plan.guidance).toContain(
			'Review these waves as a group-level trend. Do not describe the change as same-respondent movement because the waves are anonymous.'
		);
		expect(plan.guidance).toContain(
			'Use repeat participation from Wave 1 when the study needs linked change-over-time comparison later.'
		);
		expect(plan.guidance).toContain(
			'Review or export Wave 1 and Wave 2 before using Setup to create Wave 3.'
		);
	});

	it('labels the group trend itself as aggregate-only', () => {
		const groupTrendPlan = toSelectedSeriesGroupTrendPlan(twoAnonymousClosedWorkspace);

		expect(groupTrendPlan.title).toBe('Aggregate group trend only: Wave 1 to Wave 2');
		expect(groupTrendPlan.description).toContain('group-level results');
		expect(groupTrendPlan.safetyRows).toContainEqual({
			label: 'Same-respondent comparison',
			value: 'Not configured for same-respondent linked change'
		});
		expect(groupTrendPlan.safetyRows).toContainEqual({
			label: 'Disclosure status',
			value: 'Review wave-level disclosure in Results before making claims'
		});
	});

	it('explains that one wave is not yet a comparison', () => {
		const review = toSelectedSeriesWaveComparisonReview(oneWaveWorkspace);

		expect(review).toMatchObject({
			title: 'Comparison plan',
			status: 'pending'
		});
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'wave_sequence',
				status: 'pending',
				summary: 'Only Wave 1 exists'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'comparison_type',
				status: 'pending',
				summary: 'No comparison yet'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'claim_boundary',
				status: 'ready',
				summary: 'Current results are wave-level only'
			})
		);
	});

	it('explains that two anonymous waves support group trend only', () => {
		const review = toSelectedSeriesWaveComparisonReview(twoAnonymousClosedWorkspace);

		expect(review).toMatchObject({
			title: 'Comparison plan',
			status: 'ready'
		});
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'comparison_type',
				status: 'ready',
				summary: 'Group trend only'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'claim_boundary',
				status: 'ready',
				summary: 'Do not call this same-respondent change'
			})
		);
	});

	it('explains when same-respondent linked change is ready for review', () => {
		const review = toSelectedSeriesWaveComparisonReview(comparisonReadyWorkspace);

		expect(review).toMatchObject({
			title: 'Comparison plan',
			status: 'ready'
		});
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'comparison_type',
				status: 'ready',
				summary: 'Same-respondent linked change'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'data_readiness',
				status: 'ready',
				summary: '6 linked pairs, 1 visible comparison score'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'claim_boundary',
				status: 'ready',
				summary: 'Disclosure-gated custom-study comparison'
			})
		);
	});

	it('blocks wave actions when the series has no longitudinal waves', () => {
		const actions = toSelectedSeriesWavesWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'twoWaveProof',
				status: 'blocked',
				available: false,
				disabledReason:
					'Add at least two repeated waves before comparing change over time.'
			}),
			expect.objectContaining({
				id: 'waveComparisonProof',
				status: 'blocked',
				available: false,
				disabledReason: 'Choose baseline and comparison waves before reviewing change over time.'
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
			disabledReason: 'Choose baseline and comparison waves before reviewing change over time.'
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
			disabledReason: 'Check comparison readiness before reviewing change over time.'
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

		expect(path.showWorkflow).toBe(false);
		expect(path.mode).toBe('setup');
		expect(path.inactiveReason).toBe(
			'Create and collect the first waves before linked-change checks apply.'
		);
		expect(path.currentActionId).toBe('twoWaveProof');
		expect(path.completedCount).toBe(0);
		expect(path.steps).toEqual([
			expect.objectContaining({ id: 'twoWaveProof', pathState: 'current' }),
			expect.objectContaining({ id: 'waveComparisonProof', pathState: 'blocked' })
		]);
		expect(path.currentAction.disabledReason).toBe(
			'Add at least two repeated waves before comparing change over time.'
		);
	});

	it('does not show linked-change workflow tasks for a one-wave setup state', () => {
		const path = toSelectedSeriesWavesPath(oneWaveWorkspace);

		expect(path.showWorkflow).toBe(false);
		expect(path.mode).toBe('setup');
		expect(path.inactiveReason).toBe(
			'Review Wave 1 in Results. Plan Wave 2 from Setup only when the next collection round is intentional.'
		);
	});

	it('does not show blocked linked-change tasks when aggregate group trend is already the valid review path', () => {
		const path = toSelectedSeriesWavesPath(twoAnonymousClosedWorkspace);

		expect(path.showWorkflow).toBe(false);
		expect(path.mode).toBe('group_trend');
		expect(path.inactiveReason).toBe(
			'This study supports aggregate group trend only. Linked-change checks are not required and would be misleading here.'
		);
	});

	it('shows linked-change workflow tasks only for repeated-wave comparison studies', () => {
		const path = toSelectedSeriesWavesPath(comparisonReadyWorkspace);

		expect(path.showWorkflow).toBe(true);
		expect(path.mode).toBe('linked_change');
		expect(path.inactiveReason).toBeNull();
	});

	it('summarizes wave scoring method before comparison proof exposes output rows', () => {
		const method = toSelectedSeriesWaveScoreMethodReview(comparisonReadyWorkspace);

		expect(method).toMatchObject({
			title: 'What is being compared?',
			status: 'pending'
		});
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'scoring_rules',
				status: 'ready',
				summary: 'Wave 1 and Wave 2 use burnout.total 1.0.0'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'comparison_method',
				status: 'ready',
				summary: '6 linked pairs, 1 visible comparison score'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'outputs',
				status: 'pending',
				summary: 'Compared output names available after reviewing linked change'
			})
		);
	});

	it('summarizes compared score outputs and missingness after comparison proof', () => {
		const method = toSelectedSeriesWaveScoreMethodReview(
			comparisonReadyWorkspace,
			waveComparisonProof
		);

		expect(method).toMatchObject({
			title: 'What is being compared?',
			status: 'pending'
		});
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'outputs',
				status: 'ready',
				summary: '1 compared output: burnout_total'
			})
		);
		expect(method.items.find((item) => item.id === 'missingness')?.detail).toContain(
			'burnout_total baseline used 5 of 6 expected answer contributions'
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'interpretation_boundary',
				status: 'pending',
				summary: 'Custom-study change, not a benchmark'
			})
		);
	});
	it('localizes the waves workflow model for Croatian route copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.wavesWorkflow;
		const plan = toSelectedSeriesWavePlan(oneWaveWorkspace, copy);
		const groupTrend = toSelectedSeriesGroupTrendPlan(twoAnonymousClosedWorkspace, copy);
		const path = toSelectedSeriesWavesPath(comparisonReadyWorkspace, {}, copy);
		const review = toSelectedSeriesWaveComparisonReview(twoAnonymousClosedWorkspace, copy);
		const method = toSelectedSeriesWaveScoreMethodReview(
			comparisonReadyWorkspace,
			waveComparisonProof,
			copy
		);

		expect(plan).toMatchObject({
			title: 'Pregledajte Mjerenje 1 prije planiranja Mjerenja 2',
			primaryLabel: 'Pregledaj rezultate Mjerenja 1',
			secondaryLabel: 'Planiraj Mjerenje 2 kasnije'
		});
		expect(groupTrend).toMatchObject({
			title: 'Samo grupni trend: Wave 1 prema Wave 2',
			primaryLabel: 'Otvori Rezultate'
		});
		expect(path.steps[0]).toMatchObject({
			step: '1',
			title: 'Provjera povezane promjene'
		});
		expect(review).toMatchObject({
			title: 'Plan usporedbe'
		});
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'comparison_type',
				label: 'Vrsta usporedbe',
				summary: 'Samo grupni trend'
			})
		);
		expect(review.items).toContainEqual(
			expect.objectContaining({
				id: 'data_readiness',
				label: 'Spremnost podataka',
				summary: '1 rezultat u prvom mjerenju, 1 rezultat u drugom mjerenju'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'outputs',
				label: 'Uspoređeni izlazi',
				summary: '1 uspoređeni izlaz rezultata: burnout_total'
			})
		);
		expect(method.items.find((item) => item.id === 'missingness')?.detail).toContain(
			'burnout_total u početnom mjerenju koristi 5 od 6 očekivanih doprinosa odgovora'
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

const anonymousWave1 = {
	...baselineWave,
	id: 'anonymous-wave-1',
	name: 'Wave 1',
	status: 'closed',
	responseIdentityMode: 'anonymous',
	latestLaunchSnapshotId: 'anonymous-launch-1',
	latestLaunchAt: '2026-05-05T08:30:00Z',
	submittedResponseCount: 1,
	scoreCount: 1,
	linkedTrajectoryCount: 0
};

const anonymousWave2 = {
	...anonymousWave1,
	id: 'anonymous-wave-2',
	name: 'Wave 2',
	latestLaunchSnapshotId: 'anonymous-launch-2',
	latestLaunchAt: '2026-05-12T08:30:00Z'
};

const twoAnonymousClosedWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		...emptyWorkspace.summary,
		campaignCount: 2,
		submittedWaveCount: 2,
		missingPrerequisiteCount: 0
	},
	waves: [anonymousWave1, anonymousWave2]
};

const waveComparisonProof: CampaignSeriesWaveComparisonProofResponse = {
	campaignSeriesId: 'series-id',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: 'baseline-wave-id',
		name: 'Wave 1',
		status: 'closed',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-05T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'hash',
		submittedResponseCount: 6
	},
	comparisonWave: {
		campaignId: 'comparison-wave-id',
		name: 'Wave 2',
		status: 'closed',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-12T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'hash',
		submittedResponseCount: 6
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_small_groups'
	},
	scores: [
		{
			dimensionCode: 'burnout_total',
			compatibilityStatus: 'compatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 6,
			comparisonSubmittedResponseCount: 6,
			linkedPairCount: 6,
			baselineScoreCount: 5,
			comparisonScoreCount: 6,
			baselineNValidTotal: 5,
			baselineNExpectedTotal: 6,
			baselineMissingPolicyStatusSummary: 'partial',
			comparisonNValidTotal: 6,
			comparisonNExpectedTotal: 6,
			comparisonMissingPolicyStatusSummary: 'complete',
			baselineMean: 6.1,
			comparisonMean: 5.2,
			aggregateDelta: -0.9,
			pairedDeltaMean: -0.8,
			suppressionReason: null,
			compatibilityReason: null
		}
	]
};

