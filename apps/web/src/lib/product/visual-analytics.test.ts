import { describe, expect, it } from 'vitest';
import type {
	CampaignReportProofResponse,
	CampaignSeriesWaveComparisonProofResponse
} from '$lib/api/setup';
import { toReportVisualAnalyticsView, toWaveVisualAnalyticsView } from './visual-analytics';

describe('selected-series visual analytics view models', () => {
	it('maps visible report means into chart points', () => {
		const view = toReportVisualAnalyticsView(sampleReportProof);

		expect(view).toMatchObject({
			title: 'Report visual analytics',
			ariaLabel: 'Report visual analytics',
			primarySeriesLabel: 'Mean',
			secondarySeriesLabel: null,
			yAxisLabel: 'Score mean',
			statusLabel: 'Preview / not validated'
		});
		expect(view.points).toEqual([
			{
				id: 'total',
				label: 'total',
				primaryValue: 3.75,
				primaryDisplay: '3.75',
				secondaryValue: null,
				secondaryDisplay: null,
				meta: ['scores 120', 'submitted 128', 'range 1.00-5.00']
			}
		]);
	});

	it('uses score output labels and method metadata in report chart points', () => {
		const view = toReportVisualAnalyticsView({
			...sampleReportProof,
			scores: [
				{
					...sampleReportProof.scores[0],
					displayLabel: 'Recovery readiness index',
					calculationLabel: 'Normalized 0-100 weighted average',
					scoreRangeMin: 0,
					scoreRangeMax: 100
				}
			]
		});

		expect(view.points[0]?.label).toBe('Recovery readiness index');
		expect(view.points[0]?.meta).toContain('Normalized 0-100 weighted average');
		expect(view.points[0]?.meta).toContain('score range 0-100');
	});

	it('keeps suppressed report rows out of numeric chart points', () => {
		const view = toReportVisualAnalyticsView(sampleReportProof);

		expect(view.points.map((point) => point.id)).not.toContain('exhaustion');
		expect(view.excludedRows).toEqual([
			{
				id: 'exhaustion',
				label: 'exhaustion',
				state: 'suppressed',
				reason: 'cohort_lt_k_min'
			}
		]);
	});

	it('does not render missing visible score counts as zero', () => {
		const view = toReportVisualAnalyticsView({
			...sampleReportProof,
			scores: [{ ...sampleReportProof.scores[0], scoreCount: null }]
		});

		expect(view.points[0]?.meta[0]).toBe('scores not available');
	});

	it('maps visible compatible wave deltas into chart points', () => {
		const view = toWaveVisualAnalyticsView(sampleWaveComparisonProof);

		expect(view).toMatchObject({
			title: 'Wave visual analytics',
			ariaLabel: 'Wave visual analytics',
			primarySeriesLabel: 'Aggregate delta',
			secondarySeriesLabel: 'Paired delta',
			yAxisLabel: 'Delta',
			statusLabel: 'Preview / not validated'
		});
		expect(view.points).toEqual([
			{
				id: 'total',
				label: 'total',
				primaryValue: -0.3,
				primaryDisplay: '-0.30',
				secondaryValue: -0.25,
				secondaryDisplay: '-0.25',
				meta: ['baseline 3.70', 'comparison 3.40', 'linked pairs 6']
			}
		]);
	});

	it('uses score output labels and method metadata in wave chart points', () => {
		const view = toWaveVisualAnalyticsView({
			...sampleWaveComparisonProof,
			scores: [
				{
					...sampleWaveComparisonProof.scores[0],
					displayLabel: 'Recovery readiness index',
					baselineCalculationLabel: 'Normalized 0-100 weighted average',
					baselineScoreRangeMin: 0,
					baselineScoreRangeMax: 100,
					comparisonCalculationLabel: 'Normalized 0-100 weighted average',
					comparisonScoreRangeMin: 0,
					comparisonScoreRangeMax: 100
				}
			]
		});

		expect(view.points[0]?.label).toBe('Recovery readiness index');
		expect(view.points[0]?.meta).toContain('baseline Normalized 0-100 weighted average / score range 0-100');
		expect(view.points[0]?.meta).toContain('comparison Normalized 0-100 weighted average / score range 0-100');
	});

	it('localizes visual analytics chart chrome and metric labels for Croatian', () => {
		const reportView = toReportVisualAnalyticsView(sampleReportProof, 'hr-HR');
		const waveView = toWaveVisualAnalyticsView(sampleWaveComparisonProof, 'hr-HR');

		expect(reportView).toMatchObject({
			title: 'Vizualni pregled izvještaja',
			kicker: 'Vizualni pregled',
			primarySeriesLabel: 'Prosjek',
			noChartableValuesTitle: 'Nema vrijednosti za graf'
		});
		expect(reportView.points[0]?.meta).toEqual([
			'rezultati 120',
			'predano 128',
			'raspon 1.00-5.00'
		]);
		expect(waveView).toMatchObject({
			title: 'Vizualni pregled mjerenja',
			primarySeriesLabel: 'Agregirana promjena',
			secondarySeriesLabel: 'Promjena u paru'
		});
		expect(waveView.points[0]?.meta).toEqual([
			'početno 3.70',
			'usporedno 3.40',
			'povezanih parova 6'
		]);
	});

	it('keeps suppressed and incompatible wave rows out of numeric chart points', () => {
		const view = toWaveVisualAnalyticsView(sampleWaveComparisonProof);

		expect(view.points.map((point) => point.id)).toEqual(['total']);
		expect(view.excludedRows).toEqual([
			{
				id: 'exhaustion',
				label: 'exhaustion',
				state: 'suppressed',
				reason: 'linked_pairs_lt_k_min'
			},
			{
				id: 'cynicism',
				label: 'cynicism',
				state: 'incompatible',
				reason: 'scoring_rule_mismatch'
			}
		]);
	});
});

const sampleReportProof: CampaignReportProofResponse = {
	campaignId: 'campaign-id',
	campaignSeriesId: 'series-id',
	campaignName: 'Pulse wave 1',
	campaignStatus: 'live',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: 'launch-snapshot-id',
		templateVersionId: 'template-version-id',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleDocumentHash: 'scoring-hash',
		consentDocumentId: 'consent-id',
		retentionPolicyId: 'retention-id',
		disclosurePolicyId: 'disclosure-id',
		responseIdentityMode: 'anonymous',
		launchedAt: '2026-05-05T08:30:00Z'
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress'
	},
	scores: [
		{
			dimensionCode: 'total',
			disclosure: 'visible',
			submittedResponseCount: 128,
			scoreCount: 120,
			mean: 3.75,
			min: 1,
			max: 5,
			suppressionReason: null
		},
		{
			dimensionCode: 'exhaustion',
			disclosure: 'suppressed',
			submittedResponseCount: 4,
			scoreCount: null,
			mean: null,
			min: null,
			max: null,
			suppressionReason: 'cohort_lt_k_min'
		}
	]
};

const sampleWaveComparisonProof: CampaignSeriesWaveComparisonProofResponse = {
	campaignSeriesId: 'series-id',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: 'baseline-campaign-id',
		name: 'Pulse wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-05T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'scoring-hash',
		submittedResponseCount: 6
	},
	comparisonWave: {
		campaignId: 'comparison-campaign-id',
		name: 'Pulse wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-12T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'scoring-hash',
		submittedResponseCount: 6
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress'
	},
	scores: [
		{
			dimensionCode: 'total',
			compatibilityStatus: 'compatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 6,
			comparisonSubmittedResponseCount: 6,
			linkedPairCount: 6,
			baselineScoreCount: 6,
			comparisonScoreCount: 6,
			baselineMean: 3.7,
			comparisonMean: 3.4,
			aggregateDelta: -0.3,
			pairedDeltaMean: -0.25,
			suppressionReason: null,
			compatibilityReason: null
		},
		{
			dimensionCode: 'exhaustion',
			compatibilityStatus: 'compatible',
			disclosure: 'suppressed',
			baselineSubmittedResponseCount: 4,
			comparisonSubmittedResponseCount: 4,
			linkedPairCount: 4,
			baselineScoreCount: null,
			comparisonScoreCount: null,
			baselineMean: null,
			comparisonMean: null,
			aggregateDelta: null,
			pairedDeltaMean: null,
			suppressionReason: 'linked_pairs_lt_k_min',
			compatibilityReason: null
		},
		{
			dimensionCode: 'cynicism',
			compatibilityStatus: 'incompatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 6,
			comparisonSubmittedResponseCount: 6,
			linkedPairCount: 6,
			baselineScoreCount: null,
			comparisonScoreCount: null,
			baselineMean: null,
			comparisonMean: null,
			aggregateDelta: null,
			pairedDeltaMean: null,
			suppressionReason: null,
			compatibilityReason: 'scoring_rule_mismatch'
		}
	]
};
