import { describe, expect, it } from 'vitest';
import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import { toResultsWorkbenchModel, toScoreCards } from './results-workbench';

describe('results workbench model', () => {
	it('builds disclosure-safe score cards with configured range progress and method metadata', () => {
		const model = toResultsWorkbenchModel(workspaceWithResults);

		expect(model.selectedMeasurementLabel).toBe('June pulse');
		expect(model.scoreCards).toHaveLength(2);
		expect(model.scoreCards[0]).toMatchObject({
			id: 'output:workload',
			label: 'Workload pressure',
			dimensionCode: 'workload',
			disclosure: 'visible',
			valueLabel: '62.50',
			countLabel: '24',
			methodLabel: 'Normalized 0-100 weighted average',
			rangeLabel: 'Score range 0-100',
			progressPercent: 62.5,
			isSuppressed: false
		});
		expect(model.scoreCards[1]).toMatchObject({
			label: 'Recovery capacity',
			disclosure: 'suppressed',
			valueLabel: 'Suppressed',
			countLabel: 'Suppressed',
			methodLabel: 'Average selected answers',
			rangeLabel: 'Score range 1-5',
			progressPercent: null,
			isSuppressed: true,
			suppressionReason: 'insufficient_responses'
		});
	});

	it('falls back to observed dashboard scale when no configured score range exists', () => {
		const model = toResultsWorkbenchModel({
			...workspaceWithResults,
			resultsDashboard: {
				...workspaceWithResults.resultsDashboard!,
				outputBars: [
					{
						id: 'output:plain',
						label: 'plain',
						displayLabel: 'Plain score',
						dimensionCode: 'plain',
						disclosure: 'visible',
						value: 4,
						count: 8,
						detail: null,
						suppressionReason: null
					},
					{
						id: 'output:plain-high',
						label: 'plain-high',
						displayLabel: 'Plain high',
						dimensionCode: 'plain-high',
						disclosure: 'visible',
						value: 8,
						count: 8,
						detail: null,
						suppressionReason: null
					}
				]
			}
		});

		expect(model.scoreCards[0]).toMatchObject({
			label: 'Plain high',
			progressPercent: 100,
			rangeLabel: 'Observed scale 0-8'
		});
		expect(model.scoreCards[1]).toMatchObject({
			label: 'Plain score',
			progressPercent: 50,
			rangeLabel: 'Observed scale 0-8'
		});
	});

	it('summarizes comparison and export readiness without making matrix rows primary', () => {
		const model = toResultsWorkbenchModel(workspaceWithResults);

		expect(model.comparisons).toEqual({
			outputCount: 2,
			visibleOutputCount: 1,
			suppressedOutputCount: 1,
			groupRowCount: 2,
			visibleGroupRowCount: 1,
			waveRowCount: 2,
			comparableWaveCount: 2
		});
		expect(model.exports).toMatchObject({
			hasResultsMatrix: true,
			hasResponseDataset: false,
			downloadLabel: 'Download results matrix CSV',
			primaryGuidance:
				'Aggregate matrix is ready for dashboard, group, and measurement comparison. Create a response dataset only for row-level analysis.'
		});
	});

	it('can localize disclosure and range labels for rendered score cards', () => {
		const cards = toScoreCards(workspaceWithResults.resultsDashboard!.outputBars, {
			notAvailable: 'Nije dostupno',
			observedScale: 'Prikazani raspon',
			scoreRange: 'Raspon rezultata',
			suppressed: 'Skriveno'
		});

		expect(cards[0].rangeLabel).toBe('Raspon rezultata 0-100');
		expect(cards[1]).toMatchObject({
			valueLabel: 'Skriveno',
			countLabel: 'Skriveno',
			rangeLabel: 'Raspon rezultata 1-5'
		});
	});
});

const workspaceWithResults: CampaignSeriesReportsWorkspaceResponse = {
	series: {
		id: 'series-1',
		name: 'Work wellbeing',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-06-01T08:00:00Z',
		updatedAt: '2026-06-01T08:00:00Z'
	},
	summary: {
		campaignCount: 1,
		liveCampaignCount: 1,
		reportableCampaignCount: 1,
		submittedResponseCount: 24,
		scoreCount: 48,
		exportArtifactCount: 1,
		visibleScoreCount: 24,
		suppressedScoreCount: 24,
		missingPrerequisiteCount: 0
	},
	selectedCampaign: {
		id: 'campaign-1',
		name: 'June pulse',
		status: 'closed',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'launch-1',
		latestLaunchAt: '2026-06-01T08:00:00Z',
		scoringRuleId: 'rule-1',
		consentDocumentId: 'consent-1',
		retentionPolicyId: 'retention-1',
		disclosurePolicyId: 'disclosure-1',
		submittedResponseCount: 24,
		scoreCount: 48,
		exportArtifactCount: 1,
		visibleScoreCount: 24,
		suppressedScoreCount: 24,
		disclosureState: 'visible',
		disclosureKMin: 5,
		reportStatus: 'proof_only',
		interpretationStatus: 'not_validated_interpretation',
		latestExportArtifactId: 'artifact-1',
		latestExportArtifactFileName: 'campaign-series-results-matrix.csv',
		latestExportArtifactStatus: 'succeeded',
		latestExportArtifactCreatedAt: '2026-06-01T09:00:00Z',
		latestExportArtifactCompletedAt: '2026-06-01T09:00:01Z',
		latestExportArtifactStartedAt: null,
		latestExportArtifactFailedAt: null,
		latestExportArtifactExpiresAt: null,
		latestExportArtifactDeletedAt: null,
		latestExportArtifactFailureReasonCode: null,
		latestExportArtifactCanDownload: true,
		closedAt: '2026-06-01T09:00:00Z',
		dataFinality: 'closed_wave'
	},
	missingPrerequisites: [],
	exportArtifacts: [
		{
			id: 'artifact-1',
			targetKind: 'campaign_series',
			targetId: 'series-1',
			targetLabel: 'Work wellbeing',
			campaignId: null,
			campaignName: null,
			artifactType: 'campaign_series_results_matrix_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'campaign-series-results-matrix.csv',
			rowCount: 6,
			byteSize: 2048,
			checksumSha256: 'checksum',
			createdAt: '2026-06-01T09:00:00Z',
			completedAt: '2026-06-01T09:00:01Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true
		}
	],
	campaigns: [],
	resultsAnalytics: {
		selectedCampaignId: 'campaign-1',
		selectedCampaignName: 'June pulse',
		disclosureKMin: 5,
		disclosureState: 'visible',
		scoreOutputs: [
			{
				dimensionCode: 'workload',
				displayLabel: 'Workload pressure',
				disclosure: 'visible',
				submittedResponseCount: 24,
				scoreCount: 24,
				mean: 62.5,
				median: 61,
				standardDeviation: 8.1,
				min: 42,
				max: 78,
				nValidTotal: 72,
				nExpectedTotal: 72,
				missingPolicyStatusSummary: 'complete',
				suppressionReason: null,
				calculation: 'normalized_weighted_mean_0_100',
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			},
			{
				dimensionCode: 'recovery',
				displayLabel: 'Recovery capacity',
				disclosure: 'suppressed',
				submittedResponseCount: null,
				scoreCount: null,
				mean: null,
				median: null,
				standardDeviation: null,
				min: null,
				max: null,
				nValidTotal: null,
				nExpectedTotal: null,
				missingPolicyStatusSummary: null,
				suppressionReason: 'insufficient_responses',
				calculation: 'mean',
				calculationLabel: 'Average selected answers',
				scoreRangeMin: 1,
				scoreRangeMax: 5
			}
		],
		groupRows: [
			{
				groupType: 'department',
				groupName: 'Operations',
				dimensionCode: 'workload',
				displayLabel: 'Workload pressure',
				disclosure: 'visible',
				submittedResponseCount: 12,
				scoreCount: 12,
				mean: 60,
				median: 60,
				standardDeviation: 7,
				min: 40,
				max: 74,
				suppressionReason: null,
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			},
			{
				groupType: 'department',
				groupName: 'HR',
				dimensionCode: 'workload',
				displayLabel: 'Workload pressure',
				disclosure: 'suppressed',
				submittedResponseCount: null,
				scoreCount: null,
				mean: null,
				median: null,
				standardDeviation: null,
				min: null,
				max: null,
				suppressionReason: 'insufficient_responses',
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			}
		],
		waveRows: [
			{
				campaignId: 'campaign-0',
				campaignName: 'May pulse',
				campaignStatus: 'closed',
				dataFinality: 'closed_wave',
				closedAt: '2026-05-01T09:00:00Z',
				dimensionCode: 'workload',
				displayLabel: 'Workload pressure',
				disclosure: 'visible',
				submittedResponseCount: 22,
				scoreCount: 22,
				mean: 58,
				median: 58,
				standardDeviation: 6,
				min: 38,
				max: 70,
				suppressionReason: null,
				deltaFromPreviousMean: null,
				deltaFromFirstMean: 0,
				comparisonState: 'baseline',
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			},
			{
				campaignId: 'campaign-1',
				campaignName: 'June pulse',
				campaignStatus: 'closed',
				dataFinality: 'closed_wave',
				closedAt: '2026-06-01T09:00:00Z',
				dimensionCode: 'workload',
				displayLabel: 'Workload pressure',
				disclosure: 'visible',
				submittedResponseCount: 24,
				scoreCount: 24,
				mean: 62.5,
				median: 61,
				standardDeviation: 8.1,
				min: 42,
				max: 78,
				suppressionReason: null,
				deltaFromPreviousMean: 4.5,
				deltaFromFirstMean: 4.5,
				comparisonState: 'compared',
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			}
		],
		insights: []
	},
	resultsDashboard: {
		selectedCampaignId: 'campaign-1',
		selectedCampaignName: 'June pulse',
		disclosureKMin: 5,
		disclosureState: 'visible',
		metrics: [],
		outputBars: [
			{
				id: 'output:workload',
				label: 'workload',
				displayLabel: 'Workload pressure',
				dimensionCode: 'workload',
				disclosure: 'visible',
				value: 62.5,
				count: 24,
				detail: 'median 61, range 42-78',
				suppressionReason: null,
				calculation: 'normalized_weighted_mean_0_100',
				calculationLabel: 'Normalized 0-100 weighted average',
				scoreRangeMin: 0,
				scoreRangeMax: 100
			},
			{
				id: 'output:recovery',
				label: 'recovery',
				displayLabel: 'Recovery capacity',
				dimensionCode: 'recovery',
				disclosure: 'suppressed',
				value: null,
				count: null,
				detail: null,
				suppressionReason: 'insufficient_responses',
				calculation: 'mean',
				calculationLabel: 'Average selected answers',
				scoreRangeMin: 1,
				scoreRangeMax: 5
			}
		],
		groupBars: [],
		waveTrendPoints: [],
		notes: []
	}
};
