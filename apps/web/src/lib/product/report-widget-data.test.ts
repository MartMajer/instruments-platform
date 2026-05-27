import { describe, expect, it } from 'vitest';
import {
	isExportArtifactRegistryWidgetData,
	isFinalityProvenanceWidgetData,
	isReportReadinessSummaryWidgetData,
	isResultsDashboardWidgetData,
	isSelectedCampaignReportStateWidgetData,
	isVisualAnalyticsEntryWidgetData
} from './widgets/report-widget-data';

describe('report widget data guards', () => {
	it('rejects readiness data missing rendered count fields', () => {
		expect(
			isReportReadinessSummaryWidgetData({
				campaignCount: 1,
				liveCampaignCount: 1,
				reportableCampaignCount: 0,
				missingPrerequisiteCount: 0,
				missingPrerequisites: []
			})
		).toBe(false);
	});

	it('rejects readiness data with malformed prerequisite rows', () => {
		expect(
			isReportReadinessSummaryWidgetData({
				campaignCount: 1,
				liveCampaignCount: 1,
				reportableCampaignCount: 0,
				submittedResponseCount: 0,
				scoreCount: 0,
				visibleScoreCount: 0,
				suppressedScoreCount: 0,
				missingPrerequisiteCount: 1,
				missingPrerequisites: [{ code: 'scores.missing' }]
			})
		).toBe(false);
	});

	it('accepts selected campaign report state data with lifecycle and finality fields', () => {
		expect(
			isSelectedCampaignReportStateWidgetData({
				campaignId: 'campaign-1',
				name: 'Pulse wave 1',
				status: 'live',
				responseIdentityMode: 'anonymous',
				defaultLocale: 'en',
				latestLaunchAt: '2026-05-05T10:15:00Z',
				submittedResponseCount: 128,
				scoreCount: 120,
				visibleScoreCount: 115,
				suppressedScoreCount: 5,
				disclosureState: 'visible',
				disclosureKMin: 5,
				reportStatus: 'proof_only',
				interpretationStatus: 'not_validated_interpretation',
				latestExportArtifactId: 'artifact-1',
				latestExportArtifactFileName: 'report-proof.csv',
				latestExportArtifactStatus: 'succeeded',
				latestExportArtifactCreatedAt: '2026-05-05T11:00:00Z',
				latestExportArtifactCompletedAt: '2026-05-05T11:00:03Z',
				latestExportArtifactFailedAt: null,
				latestExportArtifactFailureReasonCode: null,
				latestExportArtifactCanDownload: true,
				closedAt: null,
				dataFinality: 'preliminary_live'
			})
		).toBe(true);
	});

	it('rejects selected campaign report state data with unsafe missing count fields', () => {
		expect(
			isSelectedCampaignReportStateWidgetData({
				campaignId: 'campaign-1',
				name: 'Pulse wave 1'
			})
		).toBe(false);
	});

	it('accepts export artifact registry data with safe artifact rows', () => {
		expect(
			isExportArtifactRegistryWidgetData({
				exportArtifactCount: 1,
				artifacts: [
					{
						id: 'artifact-1',
						targetKind: 'campaign',
						targetId: 'campaign-1',
						targetLabel: 'Pulse wave 1',
						campaignId: 'campaign-1',
						campaignName: 'Pulse wave 1',
						artifactType: 'report_proof_csv_codebook',
						status: 'succeeded',
						format: 'csv_codebook',
						fileName: 'report-proof.csv',
						rowCount: 120,
						byteSize: 2048,
						checksumSha256: 'checksum-sha256',
						createdAt: '2026-05-05T11:00:00Z',
						completedAt: '2026-05-05T11:00:03Z',
						startedAt: null,
						failedAt: null,
						expiresAt: null,
						deletedAt: null,
						failureReasonCode: null,
						canDownload: true
					}
				]
			})
		).toBe(true);
	});

	it('rejects export artifact registry data with malformed artifact rows', () => {
		expect(
			isExportArtifactRegistryWidgetData({
				exportArtifactCount: 1,
				artifacts: [{ id: 'artifact-1', fileName: 'report-proof.csv' }]
			})
		).toBe(false);
	});

	it('accepts visual analytics entry data', () => {
		expect(
			isVisualAnalyticsEntryWidgetData({
				selectedCampaignId: 'campaign-1',
				visibleScoreCount: 115,
				suppressedScoreCount: 5,
				reportableCampaignCount: 1
			})
		).toBe(true);
	});

	it('accepts disclosure-safe results dashboard widget data', () => {
		expect(
			isResultsDashboardWidgetData({
				dashboard: {
					selectedCampaignId: 'campaign-1',
					selectedCampaignName: 'Wave 1',
					disclosureKMin: 5,
					disclosureState: 'visible',
					metrics: [
						{
							id: 'visible_outputs',
							value: 2,
							unit: 'count',
							detail: null,
							tone: 'ready'
						}
					],
					outputBars: [
						{
							id: 'output:workload',
							label: 'workload',
							dimensionCode: 'workload',
							disclosure: 'visible',
							value: 4.2,
							count: 42,
							detail: 'median 4.1',
							suppressionReason: null
						},
						{
							id: 'output:recovery',
							label: 'recovery',
							dimensionCode: 'recovery',
							disclosure: 'suppressed',
							value: null,
							count: null,
							detail: null,
							suppressionReason: 'insufficient_responses'
						}
					],
					groupBars: [],
					waveTrendPoints: [
						{
							id: 'wave:campaign-1:workload',
							campaignId: 'campaign-1',
							campaignName: 'Wave 1',
							dimensionCode: 'workload',
							disclosure: 'visible',
							value: 4.2,
							deltaFromPrevious: null,
							comparisonState: 'baseline',
							dataFinality: 'closed_wave',
							count: 42,
							suppressionReason: null
						}
					],
					notes: [
						{
							kind: 'score_outputs',
							severity: 'ready',
							title: '2 visible outputs',
							detail: 'Review before sharing.'
						}
					]
				}
			})
		).toBe(true);
	});

	it('rejects dashboard widget data that exposes suppressed values', () => {
		expect(
			isResultsDashboardWidgetData({
				dashboard: {
					selectedCampaignId: 'campaign-1',
					selectedCampaignName: 'Wave 1',
					disclosureKMin: 5,
					disclosureState: 'suppressed',
					metrics: [],
					outputBars: [
						{
							id: 'output:workload',
							label: 'workload',
							dimensionCode: 'workload',
							disclosure: 'suppressed',
							value: 4.2,
							count: 1,
							detail: null,
							suppressionReason: 'insufficient_responses'
						}
					],
					groupBars: [],
					waveTrendPoints: [],
					notes: []
				}
			})
		).toBe(false);
	});

	it('accepts visual analytics entry data with aggregate matrices', () => {
		expect(
			isVisualAnalyticsEntryWidgetData({
				selectedCampaignId: 'campaign-1',
				visibleScoreCount: 115,
				suppressedScoreCount: 5,
				reportableCampaignCount: 2,
				analytics: {
					selectedCampaignId: 'campaign-1',
					selectedCampaignName: 'Wave 1',
					disclosureKMin: 5,
					disclosureState: 'visible',
					scoreOutputs: [
						{
							dimensionCode: 'workload',
							disclosure: 'visible',
							submittedResponseCount: 42,
							scoreCount: 42,
							mean: 4.2,
							median: 4,
							standardDeviation: 1.1,
							min: 1,
							max: 7,
							nValidTotal: 126,
							nExpectedTotal: 126,
							missingPolicyStatusSummary: 'ok',
							suppressionReason: null
						}
					],
					groupRows: [
						{
							groupType: 'department',
							groupName: 'Operations',
							dimensionCode: 'workload',
							disclosure: 'visible',
							submittedResponseCount: 12,
							scoreCount: 12,
							mean: 4.5,
							median: 4.5,
							standardDeviation: 0.8,
							min: 3,
							max: 6,
							suppressionReason: null
						}
					],
					waveRows: [
						{
							campaignId: 'campaign-1',
							campaignName: 'Wave 1',
							campaignStatus: 'closed',
							dataFinality: 'closed_wave',
							closedAt: '2026-05-26T08:00:00Z',
							dimensionCode: 'workload',
							disclosure: 'visible',
							submittedResponseCount: 42,
							scoreCount: 42,
							mean: 4.2,
							median: 4,
							standardDeviation: 1.1,
							min: 1,
							max: 7,
							suppressionReason: null,
							deltaFromPreviousMean: null,
							deltaFromFirstMean: 0,
							comparisonState: 'baseline'
						}
					],
					insights: [
						{
							kind: 'score_outputs',
							severity: 'ready',
							title: '1 result output ready',
							detail: 'Review aggregate statistics before sharing conclusions.'
						}
					]
				}
			})
		).toBe(true);
	});

	it('rejects visual analytics entry data with malformed matrix rows', () => {
		expect(
			isVisualAnalyticsEntryWidgetData({
				selectedCampaignId: 'campaign-1',
				visibleScoreCount: 115,
				suppressedScoreCount: 5,
				reportableCampaignCount: 2,
				analytics: {
					selectedCampaignId: 'campaign-1',
					selectedCampaignName: 'Wave 1',
					disclosureKMin: 5,
					disclosureState: 'visible',
					scoreOutputs: [{ dimensionCode: 'workload' }],
					groupRows: [],
					waveRows: [],
					insights: []
				}
			})
		).toBe(false);
	});

	it('accepts suppressed visual analytics rows with null counts and score metadata', () => {
		expect(
			isVisualAnalyticsEntryWidgetData({
				selectedCampaignId: 'campaign-1',
				visibleScoreCount: 0,
				suppressedScoreCount: 3,
				reportableCampaignCount: 1,
				analytics: {
					selectedCampaignId: 'campaign-1',
					selectedCampaignName: 'Wave 1',
					disclosureKMin: 5,
					disclosureState: 'suppressed',
					scoreOutputs: [
						{
							dimensionCode: 'workload',
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
							suppressionReason: 'insufficient_responses'
						}
					],
					groupRows: [
						{
							groupType: 'department',
							groupName: 'Operations',
							dimensionCode: 'workload',
							disclosure: 'suppressed',
							submittedResponseCount: null,
							scoreCount: null,
							mean: null,
							median: null,
							standardDeviation: null,
							min: null,
							max: null,
							suppressionReason: 'insufficient_responses'
						}
					],
					waveRows: [],
					insights: []
				}
			})
		).toBe(true);
	});

	it('accepts finality provenance summary data', () => {
		expect(
			isFinalityProvenanceWidgetData({
				preliminaryLiveReportCount: 1,
				closedWaveReportCount: 0,
				selectedCampaignId: 'campaign-1',
				selectedCampaignStatus: 'live',
				selectedDataFinality: 'preliminary_live',
				selectedClosedAt: null,
				selectedLatestLaunchAt: '2026-05-05T10:15:00Z'
			})
		).toBe(true);
	});
});
