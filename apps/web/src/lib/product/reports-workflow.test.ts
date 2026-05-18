import { describe, expect, it } from 'vitest';
import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import {
	toSelectedSeriesReportsPath,
	toSelectedSeriesReportsWorkflowActions
} from './reports-workflow';

describe('selected-series reports workflow model', () => {
	it('blocks reports actions when no campaign is selected', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'reportProof',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select a campaign before reviewing the report preview.'
			}),
			expect.objectContaining({
				id: 'exportArtifact',
				status: 'not_available',
				available: false,
				disabledReason: 'View report preview before creating an export artifact.'
			}),
			expect.objectContaining({
				id: 'responseExport',
				status: 'not_available',
				available: false,
				disabledReason: 'View report preview before creating a response export artifact.'
			}),
			expect.objectContaining({
				id: 'fetchArtifact',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select an export artifact before fetching it.'
			}),
			expect.objectContaining({
				id: 'downloadCsv',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select an export artifact before downloading CSV.'
			})
		]);
	});

	it('blocks report preview for a non-reportable selected campaign', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(blockedWorkspace);

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Resolve report prerequisites before reviewing the report preview.'
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'blocked',
			available: false
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Resolve report prerequisites before creating a response export artifact.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'blocked',
			available: false
		});
	});

	it('allows report preview for a reportable selected campaign and blocks export until preview is viewed', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(reportableWorkspace);

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'View report preview before creating an export artifact.'
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'View report preview before creating a response export artifact.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'blocked',
			available: false
		});
	});

	it('uses existing export artifact state to allow stored fetch and CSV download', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(reportableWorkspaceWithExport);

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'pending',
			available: true
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'View report preview before creating an export artifact.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'View report preview before creating a response export artifact.'
		});
		expect(actions.find((action) => action.id === 'downloadCsv')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
	});

	it('blocks CSV download when the existing export artifact is not downloadable', () => {
		const workspace: CampaignSeriesReportsWorkspaceResponse = {
			...reportableWorkspaceWithExport,
			selectedCampaign: {
				...reportableWorkspaceWithExport.selectedCampaign!,
				latestExportArtifactStatus: 'failed',
				latestExportArtifactCompletedAt: null,
				latestExportArtifactFailedAt: '2026-05-05T09:00:02Z',
				latestExportArtifactFailureReasonCode: 'renderer.failed',
				latestExportArtifactCanDownload: false
			}
		};
		const actions = toSelectedSeriesReportsWorkflowActions(workspace);

		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'downloadCsv')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Select a downloadable export artifact before downloading CSV.'
		});
	});

	it('uses local action results to advance export, fetch, and download state', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(reportableWorkspace, {
			reportProofViewed: true,
			exportCreated: true,
			responseExportCreated: true,
			artifactFetched: true,
			csvDownloaded: true
		});

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Export artifact was created in this session.'
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Response export artifact was created in this session.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'downloadCsv')).toMatchObject({
			status: 'ready',
			available: true
		});
	});

	it('uses existing response export registry state to advance stored artifact actions', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(reportableWorkspaceWithResponseExport);

		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Response export artifact already exists for this series.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'downloadCsv')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
	});

	it('selects report preview as the current reports task for a reportable campaign', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace);

		expect(path.currentActionId).toBe('reportProof');
		expect(path.currentAction.title).toBe('Report preview');
		expect(path.completedCount).toBe(0);
		expect(path.steps.find((step) => step.id === 'reportProof')).toMatchObject({
			pathState: 'current'
		});
	});

	it('advances the reports path to aggregate export after report preview is viewed', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace, {
			reportProofViewed: true
		});

		expect(path.currentActionId).toBe('exportArtifact');
		expect(path.completedCount).toBe(1);
		expect(path.steps.find((step) => step.id === 'reportProof')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'exportArtifact')).toMatchObject({
			pathState: 'current'
		});
	});

	it('advances the reports path to response export after aggregate export is created', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace, {
			reportProofViewed: true,
			exportCreated: true
		});

		expect(path.currentActionId).toBe('responseExport');
		expect(path.completedCount).toBe(2);
		expect(path.steps.find((step) => step.id === 'exportArtifact')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'responseExport')).toMatchObject({
			pathState: 'current'
		});
	});

	it('advances the reports path to stored artifact after response export is created', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace, {
			reportProofViewed: true,
			exportCreated: true,
			responseExportCreated: true
		});

		expect(path.currentActionId).toBe('fetchArtifact');
		expect(path.completedCount).toBe(3);
		expect(path.steps.find((step) => step.id === 'responseExport')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'fetchArtifact')).toMatchObject({
			pathState: 'current'
		});
	});

	it('advances the reports path to CSV download after stored artifact fetch', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace, {
			reportProofViewed: true,
			exportCreated: true,
			responseExportCreated: true,
			artifactFetched: true
		});

		expect(path.currentActionId).toBe('downloadCsv');
		expect(path.completedCount).toBe(4);
		expect(path.steps.find((step) => step.id === 'fetchArtifact')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'downloadCsv')).toMatchObject({
			pathState: 'current'
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
		exportArtifactCount: 1
	},
	selectedCampaign: {
		...reportableWorkspace.selectedCampaign!,
		exportArtifactCount: 1,
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
	}
};

const reportableWorkspaceWithResponseExport: CampaignSeriesReportsWorkspaceResponse = {
	...reportableWorkspace,
	summary: {
		...reportableWorkspace.summary,
		exportArtifactCount: 1
	},
	selectedCampaign: {
		...reportableWorkspace.selectedCampaign!,
		exportArtifactCount: 1,
		latestExportArtifactId: 'response-export-artifact-id',
		latestExportArtifactFileName: 'campaign-series-responses.csv',
		latestExportArtifactStatus: 'succeeded',
		latestExportArtifactCreatedAt: '2026-05-05T09:10:00Z',
		latestExportArtifactCompletedAt: '2026-05-05T09:10:03Z',
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
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'campaign-series-responses.csv',
			rowCount: 12,
			byteSize: 1024,
			checksumSha256: 'response-checksum-sha256',
			createdAt: '2026-05-05T09:10:00Z',
			completedAt: '2026-05-05T09:10:03Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true
		}
	]
};
