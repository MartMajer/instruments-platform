import { describe, expect, it } from 'vitest';
import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import {
	selectedSeriesReportSnapshotCopy,
	toSelectedSeriesReportDashboardView,
	toSelectedSeriesReportSnapshotState
} from './report-snapshot';

describe('selected-series report snapshot model', () => {
	it('marks the snapshot not available when no campaign is selected', () => {
		const state = toSelectedSeriesReportSnapshotState(emptyWorkspace);

		expect(state).toEqual({
			status: 'not_available',
			available: false,
			campaignId: null,
			campaignName: null,
			badgeLabel: 'Not available',
			disabledReason: 'Create or select a campaign before loading the report snapshot.'
		});
	});

	it('blocks the snapshot when the selected campaign is not reportable', () => {
		const state = toSelectedSeriesReportSnapshotState(blockedWorkspace);

		expect(state).toEqual({
			status: 'blocked',
			available: false,
			campaignId: 'campaign-id',
			campaignName: 'Draft wave',
			badgeLabel: 'Blocked',
			disabledReason: 'Resolve report prerequisites before loading the report snapshot.'
		});
	});

	it('allows a reportable selected campaign before the snapshot is loaded', () => {
		const state = toSelectedSeriesReportSnapshotState(reportableWorkspace);

		expect(state).toEqual({
			status: 'pending',
			available: true,
			campaignId: 'campaign-id',
			campaignName: 'Live wave',
			badgeLabel: 'Preview available',
			disabledReason: null
		});
	});

	it('marks the selected campaign snapshot ready after it loads', () => {
		const state = toSelectedSeriesReportSnapshotState(reportableWorkspace, {
			loadedCampaignId: 'campaign-id'
		});

		expect(state).toMatchObject({
			status: 'ready',
			available: true,
			campaignId: 'campaign-id',
			campaignName: 'Live wave',
			badgeLabel: 'Preview ready',
			disabledReason: null
		});
	});

	it('keeps a reportable campaign pending when the loaded snapshot belongs to another campaign', () => {
		const state = toSelectedSeriesReportSnapshotState(reportableWorkspace, {
			loadedCampaignId: 'previous-campaign-id'
		});

		expect(state).toMatchObject({
			status: 'pending',
			available: true,
			campaignId: 'campaign-id',
			badgeLabel: 'Preview available',
			disabledReason: null
		});
	});

	it('maps selected campaign state into report dashboard sections', () => {
		const dashboard = toSelectedSeriesReportDashboardView(reportableWorkspaceWithExport, {
			loadedCampaignId: 'campaign-id'
		});

		expect(dashboard).toMatchObject({
			title: 'Live wave report dashboard',
			status: 'ready',
			badgeLabel: 'Preview ready',
			available: true,
			emptyMessage: null
		});
		expect(dashboard.readinessRows).toEqual([
			{ label: 'Selected campaign', value: 'Live wave' },
			{ label: 'Campaign status', value: 'live' },
			{ label: 'Report status', value: 'preview' },
			{ label: 'Interpretation', value: 'not validated interpretation' },
			{ label: 'Submitted responses', value: '12' },
			{ label: 'Scores', value: '12' }
		]);
		expect(dashboard.disclosureRows).toEqual([
			{ label: 'Disclosure', value: 'visible' },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Visible scores', value: '12' },
			{ label: 'Suppressed scores', value: '0' }
		]);
		expect(dashboard.provenanceRows).toEqual([
			{ label: 'Launch snapshot', value: 'launch-snapshot-id', mono: true },
			{ label: 'Latest launch', value: '05. 05. 2026. 10:30' },
			{ label: 'Scoring rule', value: 'scoring-rule-id', mono: true },
			{ label: 'Consent document', value: 'consent-id', mono: true },
			{ label: 'Retention policy', value: 'retention-id', mono: true },
			{ label: 'Disclosure policy', value: 'disclosure-id', mono: true }
		]);
		expect(dashboard.exportRows).toEqual([
			{ label: 'Export files', value: '2' },
			{ label: 'Latest export record', value: 'export-artifact-id', mono: true },
			{ label: 'Latest export file', value: 'report-proof.csv' },
			{ label: 'Latest export status', value: 'succeeded' },
			{ label: 'Latest export created', value: '05. 05. 2026. 11:00' },
			{ label: 'Latest export completed', value: '05. 05. 2026. 11:00' },
			{ label: 'Latest export started', value: 'Not available' },
			{ label: 'Latest export failed', value: 'Not available' },
			{ label: 'Latest export expires', value: 'Not available' },
			{ label: 'Latest export deleted', value: 'Not available' },
			{ label: 'Latest export failure reason', value: 'Not available' },
			{ label: 'Latest export downloadable', value: 'Yes' }
		]);
		expect(dashboard.artifactRegistry).toEqual([
			{
				id: 'response-export-artifact-id',
				targetKind: 'campaign_series',
				targetId: 'series-id',
				targetLabel: 'Quarterly burnout pulse',
				campaignId: null,
				campaignName: null,
				title: 'campaign-series-responses.csv',
				badgeStatus: 'failed',
				badgeLabel: 'failed',
				meta: ['response dataset CSV and codebook', 'csv codebook', '0 rows', '0 bytes'],
				rows: [
					{ label: 'Export record', value: 'response-export-artifact-id', mono: true },
					{ label: 'Study context', value: 'Quarterly burnout pulse' },
					{ label: 'Context type', value: 'campaign series' },
					{ label: 'Created', value: '05. 05. 2026. 11:10' },
					{ label: 'Completed', value: 'Not available' },
					{ label: 'Started', value: 'Not available' },
					{ label: 'Failed', value: '05. 05. 2026. 11:10' },
					{ label: 'Expires', value: 'Not available' },
					{ label: 'Deleted', value: 'Not available' },
					{ label: 'Failure reason', value: 'renderer.failed' },
					{ label: 'Downloadable', value: 'No' },
					{ label: 'Checksum', value: 'Not available' }
				]
			},
			{
				id: 'export-artifact-id',
				targetKind: 'campaign',
				targetId: 'campaign-id',
				targetLabel: 'Live wave',
				campaignId: 'campaign-id',
				campaignName: 'Live wave',
				title: 'report-proof.csv',
				badgeStatus: 'ready',
				badgeLabel: 'succeeded',
				meta: ['report summary CSV and codebook', 'csv codebook', '12 rows', '512 bytes'],
				rows: [
					{ label: 'Export record', value: 'export-artifact-id', mono: true },
					{ label: 'Study context', value: 'Live wave' },
					{ label: 'Context type', value: 'campaign' },
					{ label: 'Created', value: '05. 05. 2026. 11:00' },
					{ label: 'Completed', value: '05. 05. 2026. 11:00' },
					{ label: 'Started', value: 'Not available' },
					{ label: 'Failed', value: 'Not available' },
					{ label: 'Expires', value: 'Not available' },
					{ label: 'Deleted', value: 'Not available' },
					{ label: 'Failure reason', value: 'Not available' },
					{ label: 'Downloadable', value: 'Yes' },
					{ label: 'Checksum', value: 'checksum-sha256', mono: true }
				]
			}
		]);
	});

	it('maps missing selected campaign into an unavailable report dashboard', () => {
		const dashboard = toSelectedSeriesReportDashboardView(emptyWorkspace);

		expect(dashboard).toMatchObject({
			title: 'Report dashboard unavailable',
			status: 'not_available',
			badgeLabel: 'Not available',
			available: false,
			emptyMessage: 'Create or select a campaign before reviewing the report dashboard.'
		});
		expect(dashboard.readinessRows).toEqual([
			{ label: 'Campaigns', value: '0' },
			{ label: 'Reportable campaigns', value: '0' },
			{ label: 'Missing prerequisites', value: '1' }
		]);
		expect(dashboard.disclosureRows).toEqual([]);
		expect(dashboard.provenanceRows).toEqual([]);
		expect(dashboard.exportRows).toEqual([]);
		expect(dashboard.artifactRegistry).toEqual([]);
	});

	it('localizes Croatian report dashboard rows and artifact status labels', () => {
		const copy = selectedSeriesReportSnapshotCopy('hr-HR');
		const state = toSelectedSeriesReportSnapshotState(emptyWorkspace, {}, copy);
		const dashboard = toSelectedSeriesReportDashboardView(
			reportableWorkspaceWithExport,
			{ loadedCampaignId: 'campaign-id' },
			copy
		);

		expect(state).toMatchObject({
			badgeLabel: 'Nije dostupno',
			disabledReason: 'Izradite ili odaberite mjerenje prije učitavanja pregleda izvještaja.'
		});
		expect(dashboard).toMatchObject({
			title: 'Nadzorna ploča izvještaja za Live wave',
			badgeLabel: 'Pregled spreman'
		});
		expect(dashboard.readinessRows).toEqual([
			{ label: 'Odabrano mjerenje', value: 'Live wave' },
			{ label: 'Status mjerenja', value: 'u tijeku' },
			{ label: 'Status izvještaja', value: 'pregled' },
			{ label: 'Tumačenje', value: 'tumačenje nije potvrđeno' },
			{ label: 'Predani odgovori', value: '12' },
			{ label: 'Rezultati', value: '12' }
		]);
		expect(dashboard.exportRows).toContainEqual({
			label: 'Zadnji izvoz dostupan za preuzimanje',
			value: 'Da'
		});
		expect(dashboard.artifactRegistry[0]).toMatchObject({
			badgeStatus: 'failed',
			badgeLabel: 'Neuspjelo',
			meta: ['CSV i opis podataka s odgovorima', 'CSV opis podataka', '0 redaka', '0 bajtova']
		});
		expect(dashboard.artifactRegistry[0].rows).toContainEqual({
			label: 'Vrsta konteksta',
			value: 'studija'
		});
	});
});

const emptyWorkspace: CampaignSeriesReportsWorkspaceResponse = {
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
		reportableCampaignCount: 0,
		submittedResponseCount: 0,
		scoreCount: 0,
		exportArtifactCount: 0,
		visibleScoreCount: 0,
		suppressedScoreCount: 0,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: null,
	missingPrerequisites: [],
	exportArtifacts: [],
	campaigns: []
};

const blockedWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		...emptyWorkspace.summary,
		campaignCount: 1,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Draft wave',
		status: 'draft',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		latestLaunchSnapshotId: null,
		latestLaunchAt: null,
		scoringRuleId: null,
		consentDocumentId: null,
		retentionPolicyId: null,
		disclosurePolicyId: null,
		submittedResponseCount: 0,
		scoreCount: 0,
		exportArtifactCount: 0,
		visibleScoreCount: 0,
		suppressedScoreCount: 0,
		disclosureState: 'not_available',
		disclosureKMin: null,
		reportStatus: 'blocked',
		interpretationStatus: 'not_available',
		latestExportArtifactId: null,
		latestExportArtifactFileName: null,
		latestExportArtifactStatus: null,
		latestExportArtifactCreatedAt: null,
		latestExportArtifactCompletedAt: null,
		latestExportArtifactStartedAt: null,
		latestExportArtifactFailedAt: null,
		latestExportArtifactExpiresAt: null,
		latestExportArtifactDeletedAt: null,
		latestExportArtifactFailureReasonCode: null,
		latestExportArtifactCanDownload: false
	},
	campaigns: []
};

const reportableWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	...blockedWorkspace,
	summary: {
		...blockedWorkspace.summary,
		liveCampaignCount: 1,
		reportableCampaignCount: 1,
		submittedResponseCount: 12,
		scoreCount: 12,
		visibleScoreCount: 12,
		missingPrerequisiteCount: 0
	},
	selectedCampaign: {
		...blockedWorkspace.selectedCampaign!,
		name: 'Live wave',
		status: 'live',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		consentDocumentId: 'consent-id',
		retentionPolicyId: 'retention-id',
		disclosurePolicyId: 'disclosure-id',
		submittedResponseCount: 12,
		scoreCount: 12,
		visibleScoreCount: 12,
		disclosureState: 'visible',
		disclosureKMin: 5,
		reportStatus: 'proof_only',
		interpretationStatus: 'not_validated_interpretation'
	}
};

const reportableWorkspaceWithExport: CampaignSeriesReportsWorkspaceResponse = {
	...reportableWorkspace,
	summary: {
		...reportableWorkspace.summary,
		exportArtifactCount: 2
	},
	selectedCampaign: {
		...reportableWorkspace.selectedCampaign!,
		exportArtifactCount: 2,
		latestExportArtifactId: 'export-artifact-id',
		latestExportArtifactFileName: 'report-proof.csv',
		latestExportArtifactStatus: 'succeeded',
		latestExportArtifactCreatedAt: '2026-05-05T09:00:00Z',
		latestExportArtifactCompletedAt: '2026-05-05T09:00:03Z',
		latestExportArtifactStartedAt: null,
		latestExportArtifactFailedAt: null,
		latestExportArtifactExpiresAt: null,
		latestExportArtifactDeletedAt: null,
		latestExportArtifactFailureReasonCode: null,
		latestExportArtifactCanDownload: true
	},
	exportArtifacts: [
		{
			id: 'response-export-artifact-id',
			targetKind: 'campaign_series',
			targetId: 'series-id',
			targetLabel: 'Quarterly burnout pulse',
			campaignId: null,
			campaignName: null,
			artifactType: 'campaign_series_response_csv_codebook',
			status: 'failed',
			format: 'csv_codebook',
			fileName: 'campaign-series-responses.csv',
			rowCount: 0,
			byteSize: 0,
			checksumSha256: null,
			createdAt: '2026-05-05T09:10:00Z',
			completedAt: null,
			startedAt: null,
			failedAt: '2026-05-05T09:10:02Z',
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: 'renderer.failed',
			canDownload: false
		},
		{
			id: 'export-artifact-id',
			targetKind: 'campaign',
			targetId: 'campaign-id',
			targetLabel: 'Live wave',
			campaignId: 'campaign-id',
			campaignName: 'Live wave',
			artifactType: 'report_proof_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'report-proof.csv',
			rowCount: 12,
			byteSize: 512,
			checksumSha256: 'checksum-sha256',
			createdAt: '2026-05-05T09:00:00Z',
			completedAt: '2026-05-05T09:00:03Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true
		}
	]
};
