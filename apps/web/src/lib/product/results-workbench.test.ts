import { describe, expect, it } from 'vitest';
import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import {
	filterResultsAnalytics,
	filterResultsDashboard,
	resultBarDisplayLabel,
	toAnalyticsFilterModel,
	toResultFocusOptions,
	toResultsInterpretationCockpit,
	toResultsWorkbenchModel,
	toScoreCards
} from './results-workbench';

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

	it('builds stable result focus options from dashboard outputs', () => {
		const options = toResultFocusOptions(workspaceWithResults.resultsDashboard!.outputBars);

		expect(options).toEqual([
			{ value: 'all', label: 'All result outputs', count: 2 },
			{ value: 'workload', label: 'Workload pressure', count: 1 },
			{ value: 'recovery', label: 'Recovery capacity', count: 1 }
		]);
	});

	it('filters dashboard charts by result output while keeping disclosure-safe rows intact', () => {
		const dashboard = {
			...workspaceWithResults.resultsDashboard!,
			groupBars: [
				{
					id: 'group:ops:workload',
					label: 'Operations / Workload pressure',
					displayLabel: 'Workload pressure',
					dimensionCode: 'workload',
					disclosure: 'visible',
					value: 60,
					count: 12,
					detail: 'department',
					suppressionReason: null
				},
				{
					id: 'group:hr:recovery',
					label: 'HR / Recovery capacity',
					displayLabel: 'Recovery capacity',
					dimensionCode: 'recovery',
					disclosure: 'suppressed',
					value: null,
					count: null,
					detail: 'department',
					suppressionReason: 'insufficient_responses'
				}
			],
			waveTrendPoints: [
				{
					id: 'wave:may:workload',
					campaignId: 'campaign-0',
					campaignName: 'May pulse',
					displayLabel: 'Workload pressure',
					dimensionCode: 'workload',
					disclosure: 'visible',
					value: 58,
					count: 22,
					deltaFromPrevious: null,
					comparisonState: 'baseline',
					dataFinality: 'closed_wave',
					suppressionReason: null
				},
				{
					id: 'wave:june:recovery',
					campaignId: 'campaign-1',
					campaignName: 'June pulse',
					displayLabel: 'Recovery capacity',
					dimensionCode: 'recovery',
					disclosure: 'suppressed',
					value: null,
					count: null,
					deltaFromPrevious: null,
					comparisonState: 'not_comparable',
					dataFinality: 'closed_wave',
					suppressionReason: 'insufficient_responses'
				}
			]
		};

		const filtered = filterResultsDashboard(dashboard, 'recovery');

		expect(filtered.outputBars.map((bar) => bar.id)).toEqual(['output:recovery']);
		expect(filtered.groupBars.map((bar) => bar.id)).toEqual(['group:hr:recovery']);
		expect(filtered.groupBars[0].disclosure).toBe('suppressed');
		expect(filtered.waveTrendPoints.map((point) => point.id)).toEqual(['wave:june:recovery']);
	});

	it('keeps group bar labels distinct from output-only labels', () => {
		expect(
			resultBarDisplayLabel(
				{
					id: 'group:ops:workload',
					label: 'Operations / Workload pressure',
					displayLabel: 'Workload pressure',
					dimensionCode: 'workload',
					disclosure: 'visible',
					value: 60,
					count: 12,
					detail: null,
					suppressionReason: null
				},
				'full'
			)
		).toBe('Operations / Workload pressure');
	});

	it('builds and applies visual analytics filters for result, group, and measurement', () => {
		const analytics = workspaceWithResults.resultsAnalytics!;
		const filterModel = toAnalyticsFilterModel(analytics);

		expect(filterModel.outputOptions.map((option) => option.label)).toEqual([
			'All result outputs',
			'Workload pressure',
			'Recovery capacity'
		]);
		expect(filterModel.groupOptions.map((option) => option.label)).toEqual([
			'All groups',
			'Operations',
			'HR'
		]);
		expect(filterModel.measurementOptions.map((option) => option.label)).toEqual([
			'All measurements',
			'May pulse',
			'June pulse'
		]);

		const filtered = filterResultsAnalytics(analytics, {
			outputCode: 'workload',
			groupKey: 'department\u0000Operations',
			campaignId: 'campaign-1'
		});

		expect(filtered.scoreOutputs.map((row) => row.dimensionCode)).toEqual(['workload']);
		expect(filtered.groupRows).toHaveLength(1);
		expect(filtered.groupRows[0].groupName).toBe('Operations');
		expect(filtered.waveRows.map((row) => row.campaignName)).toEqual(['June pulse']);
		expect(filtered.filteredCounts).toEqual({
			scoreOutputs: 1,
			scoreOutputsTotal: 2,
			groupRows: 1,
			groupRowsTotal: 2,
			waveRows: 1,
			waveRowsTotal: 2
		});
	});

	it('builds a decision-oriented cockpit model without inventing official thresholds', () => {
		const cockpit = toResultsInterpretationCockpit(
			cockpitWorkspace.resultsDashboard!,
			cockpitWorkspace.resultsAnalytics!,
			{ selectedOutputCode: 'all' }
		);

		expect(cockpit.header).toMatchObject({
			selectedMeasurementLabel: 'Intervention review',
			visibleResultCount: 5,
			suppressedResultCount: 1,
			sampleCount: 24,
			disclosureState: 'visible'
		});
		expect(cockpit.attentionCards.map((card) => card.id)).toEqual([
			'lowest_scale_position',
			'highest_scale_position',
			'latest_movement',
			'trust_constraint'
		]);
		expect(cockpit.attentionCards[0]).toMatchObject({
			label: 'Recovery capacity',
			valueLabel: '54.00',
			tone: 'attention'
		});
		expect(cockpit.attentionCards[1]).toMatchObject({
			label: 'Readiness index',
			valueLabel: '68.20',
			tone: 'strong'
		});
		expect(cockpit.attentionCards[2]).toMatchObject({
			label: 'Readiness index',
			valueLabel: '+8.00',
			tone: 'up'
		});
		expect(cockpit.attentionCards[3]).toMatchObject({
			label: '1 result hidden',
			tone: 'guarded'
		});
	});

	it('creates a compatible radar profile and explains excluded outputs', () => {
		const cockpit = toResultsInterpretationCockpit(
			cockpitWorkspace.resultsDashboard!,
			cockpitWorkspace.resultsAnalytics!
		);

		expect(cockpit.radar.points.map((point) => point.label)).toEqual([
			'Focus stability',
			'Recovery capacity',
			'Support resources',
			'Readiness index'
		]);
		expect(cockpit.radar.canDrawRadar).toBe(true);
		expect(cockpit.radar.points.map((point) => point.displayIndex)).toEqual([1, 2, 3, 4]);
		expect(cockpit.radar.points.map((point) => point.positionPercent)).toEqual([64, 54, 58, 68.2]);
		expect(cockpit.radar.excluded).toEqual([
			{
				id: 'output:gap',
				label: 'Recovery minus focus gap',
				reason: 'difference_range'
			},
			{
				id: 'output:suppressed',
				label: 'Small advisory score',
				reason: 'suppressed'
			}
		]);
	});

	it('marks the radar as a compact profile when fewer than three compatible outputs remain', () => {
		const focusedDashboard = filterResultsDashboard(
			cockpitWorkspace.resultsDashboard!,
			'recovery_capacity'
		);
		const focusedAnalytics = filterResultsAnalytics(cockpitWorkspace.resultsAnalytics!, {
			outputCode: 'recovery_capacity'
		});
		const cockpit = toResultsInterpretationCockpit(focusedDashboard, focusedAnalytics, {
			selectedOutputCode: 'recovery_capacity'
		});

		expect(cockpit.radar.canDrawRadar).toBe(false);
		expect(cockpit.radar.points).toEqual([
			expect.objectContaining({
				displayIndex: 1,
				label: 'Recovery capacity',
				valueLabel: '54.00',
				positionPercent: 54
			})
		]);
	});

	it('creates a disclosure-safe group heatmap', () => {
		const cockpit = toResultsInterpretationCockpit(
			cockpitWorkspace.resultsDashboard!,
			cockpitWorkspace.resultsAnalytics!
		);

		expect(cockpit.heatmap.columns.map((column) => column.label)).toEqual([
			'Focus stability',
			'Recovery capacity',
			'Readiness index'
		]);
		expect(cockpit.heatmap.rows.map((row) => row.label)).toEqual([
			'Pilot cohort',
			'Control cohort',
			'Small advisory cell'
		]);
		expect(cockpit.heatmap.rows[0].cells[0]).toMatchObject({
			columnId: 'focus_stability',
			valueLabel: '66.00',
			sampleLabel: '9',
			positionPercent: 66,
			tone: 'medium'
		});
		expect(cockpit.heatmap.rows[2].cells[0]).toMatchObject({
			columnId: 'focus_stability',
			valueLabel: 'Suppressed',
			sampleLabel: 'Suppressed',
			disclosure: 'suppressed',
			positionPercent: null,
			tone: 'suppressed'
		});
	});

	it('summarizes the focused trend by baseline, latest, and direction', () => {
		const cockpit = toResultsInterpretationCockpit(
			cockpitWorkspace.resultsDashboard!,
			cockpitWorkspace.resultsAnalytics!,
			{ selectedOutputCode: 'readiness_index' }
		);

		expect(cockpit.trend).toMatchObject({
			dimensionCode: 'readiness_index',
			label: 'Readiness index',
			baselineLabel: '60.20',
			latestLabel: '68.20',
			deltaLabel: '+8.00',
			direction: 'up'
		});
		expect(cockpit.trend?.points.map((point) => point.campaignName)).toEqual([
			'Before intervention',
			'Intervention review'
		]);
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

const cockpitWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	...workspaceWithResults,
	selectedCampaign: {
		...workspaceWithResults.selectedCampaign!,
		name: 'Intervention review',
		submittedResponseCount: 24,
		visibleScoreCount: 96,
		suppressedScoreCount: 4,
		dataFinality: 'closed_wave',
		interpretationStatus: 'not_validated_interpretation'
	},
	resultsAnalytics: {
		selectedCampaignId: 'campaign-2',
		selectedCampaignName: 'Intervention review',
		disclosureKMin: 5,
		disclosureState: 'visible',
		scoreOutputs: [
			scoreOutput('focus_stability', 'Focus stability', 64, 24),
			scoreOutput('recovery_capacity', 'Recovery capacity', 54, 24),
			scoreOutput('support_resources', 'Support resources', 58, 24),
			scoreOutput('readiness_index', 'Readiness index', 68.2, 24),
			scoreOutput('recovery_focus_gap', 'Recovery minus focus gap', 8, 24, -100, 100),
			{
				...scoreOutput('small_advisory', 'Small advisory score', null, null),
				disclosure: 'suppressed',
				suppressionReason: 'insufficient_responses'
			}
		],
		groupRows: [
			groupRow('Pilot cohort', 'focus_stability', 'Focus stability', 66, 9),
			groupRow('Pilot cohort', 'recovery_capacity', 'Recovery capacity', 56, 9),
			groupRow('Pilot cohort', 'readiness_index', 'Readiness index', 70, 9),
			groupRow('Control cohort', 'focus_stability', 'Focus stability', 51, 8),
			groupRow('Control cohort', 'recovery_capacity', 'Recovery capacity', 46, 8),
			groupRow('Control cohort', 'readiness_index', 'Readiness index', 55, 8),
			{
				...groupRow('Small advisory cell', 'focus_stability', 'Focus stability', null, null),
				disclosure: 'suppressed',
				suppressionReason: 'insufficient_responses'
			}
		],
		waveRows: [
			waveRow(
				'campaign-0',
				'Before intervention',
				'readiness_index',
				'Readiness index',
				60.2,
				22,
				null
			),
			waveRow(
				'campaign-2',
				'Intervention review',
				'readiness_index',
				'Readiness index',
				68.2,
				24,
				8
			),
			waveRow(
				'campaign-0',
				'Before intervention',
				'recovery_capacity',
				'Recovery capacity',
				50,
				22,
				null
			),
			waveRow(
				'campaign-2',
				'Intervention review',
				'recovery_capacity',
				'Recovery capacity',
				54,
				24,
				4
			)
		],
		insights: []
	},
	resultsDashboard: {
		selectedCampaignId: 'campaign-2',
		selectedCampaignName: 'Intervention review',
		disclosureKMin: 5,
		disclosureState: 'visible',
		metrics: [],
		outputBars: [
			dashboardBar('focus_stability', 'Focus stability', 64, 24),
			dashboardBar('recovery_capacity', 'Recovery capacity', 54, 24),
			dashboardBar('support_resources', 'Support resources', 58, 24),
			dashboardBar('readiness_index', 'Readiness index', 68.2, 24),
			dashboardBar('recovery_focus_gap', 'Recovery minus focus gap', 8, 24, -100, 100),
			{
				...dashboardBar('small_advisory', 'Small advisory score', null, null),
				id: 'output:suppressed',
				disclosure: 'suppressed',
				suppressionReason: 'insufficient_responses'
			}
		],
		groupBars: [],
		waveTrendPoints: [
			trendPoint(
				'campaign-0',
				'Before intervention',
				'readiness_index',
				'Readiness index',
				60.2,
				22,
				null
			),
			trendPoint(
				'campaign-2',
				'Intervention review',
				'readiness_index',
				'Readiness index',
				68.2,
				24,
				8
			)
		],
		notes: []
	}
};

function scoreOutput(
	dimensionCode: string,
	displayLabel: string,
	mean: number | null,
	scoreCount: number | null,
	scoreRangeMin = 0,
	scoreRangeMax = 100
) {
	return {
		dimensionCode,
		displayLabel,
		disclosure: 'visible',
		submittedResponseCount: scoreCount,
		scoreCount,
		mean,
		median: mean,
		standardDeviation: 6,
		min: mean === null ? null : Math.max(scoreRangeMin, mean - 10),
		max: mean === null ? null : Math.min(scoreRangeMax, mean + 10),
		nValidTotal: scoreCount === null ? null : scoreCount * 3,
		nExpectedTotal: scoreCount === null ? null : scoreCount * 3,
		missingPolicyStatusSummary: scoreCount === null ? null : 'complete',
		suppressionReason: null,
		calculation: 'normalized_weighted_mean_0_100',
		calculationLabel: 'Normalized weighted mean',
		scoreRangeMin,
		scoreRangeMax
	};
}

function groupRow(
	groupName: string,
	dimensionCode: string,
	displayLabel: string,
	mean: number | null,
	scoreCount: number | null
) {
	return {
		groupType: 'team',
		groupName,
		dimensionCode,
		displayLabel,
		disclosure: 'visible',
		submittedResponseCount: scoreCount,
		scoreCount,
		mean,
		median: mean,
		standardDeviation: 5,
		min: mean === null ? null : Math.max(0, mean - 8),
		max: mean === null ? null : Math.min(100, mean + 8),
		suppressionReason: null,
		calculationLabel: 'Normalized weighted mean',
		scoreRangeMin: 0,
		scoreRangeMax: 100
	};
}

function waveRow(
	campaignId: string,
	campaignName: string,
	dimensionCode: string,
	displayLabel: string,
	mean: number,
	scoreCount: number,
	deltaFromPreviousMean: number | null
) {
	return {
		campaignId,
		campaignName,
		campaignStatus: 'closed',
		dataFinality: 'closed_wave',
		closedAt: '2026-06-01T09:00:00Z',
		dimensionCode,
		displayLabel,
		disclosure: 'visible',
		submittedResponseCount: scoreCount,
		scoreCount,
		mean,
		median: mean,
		standardDeviation: 6,
		min: Math.max(0, mean - 10),
		max: Math.min(100, mean + 10),
		suppressionReason: null,
		deltaFromPreviousMean,
		deltaFromFirstMean: deltaFromPreviousMean ?? 0,
		comparisonState: deltaFromPreviousMean === null ? 'baseline' : 'compared',
		calculationLabel: 'Normalized weighted mean',
		scoreRangeMin: 0,
		scoreRangeMax: 100
	};
}

function dashboardBar(
	dimensionCode: string,
	displayLabel: string,
	value: number | null,
	count: number | null,
	scoreRangeMin = 0,
	scoreRangeMax = 100
) {
	return {
		id: `output:${dimensionCode === 'recovery_focus_gap' ? 'gap' : dimensionCode}`,
		label: dimensionCode,
		displayLabel,
		dimensionCode,
		disclosure: 'visible',
		value,
		count,
		detail: null,
		suppressionReason: null,
		calculation: 'normalized_weighted_mean_0_100',
		calculationLabel: 'Normalized weighted mean',
		scoreRangeMin,
		scoreRangeMax
	};
}

function trendPoint(
	campaignId: string,
	campaignName: string,
	dimensionCode: string,
	displayLabel: string,
	value: number,
	count: number,
	deltaFromPrevious: number | null
) {
	return {
		id: `trend:${campaignId}:${dimensionCode}`,
		campaignId,
		campaignName,
		displayLabel,
		dimensionCode,
		disclosure: 'visible',
		value,
		deltaFromPrevious,
		comparisonState: deltaFromPrevious === null ? 'baseline' : 'compared',
		dataFinality: 'closed_wave',
		count,
		suppressionReason: null,
		calculationLabel: 'Normalized weighted mean',
		scoreRangeMin: 0,
		scoreRangeMax: 100
	};
}
