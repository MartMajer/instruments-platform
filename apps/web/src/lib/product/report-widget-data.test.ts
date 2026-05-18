import { describe, expect, it } from 'vitest';
import {
	isExportArtifactRegistryWidgetData,
	isFinalityProvenanceWidgetData,
	isReportReadinessSummaryWidgetData,
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
