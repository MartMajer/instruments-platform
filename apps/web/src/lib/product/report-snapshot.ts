import type {
	CampaignSeriesReportsCampaignResponse,
	CampaignSeriesReportsExportArtifactResponse,
	CampaignSeriesReportsWorkspaceResponse
} from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesReportSnapshotLocalState = {
	loadedCampaignId?: string | null;
};

export type SelectedSeriesReportSnapshotState = {
	status: ProductReadModelBadgeStatus;
	available: boolean;
	campaignId: string | null;
	campaignName: string | null;
	badgeLabel: string;
	disabledReason: string | null;
};

export type SelectedSeriesReportDashboardRow = {
	label: string;
	value: string;
	mono?: boolean;
};

export type SelectedSeriesReportDashboardView = {
	title: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	badgeLabel: string;
	emptyMessage: string | null;
	readinessRows: SelectedSeriesReportDashboardRow[];
	disclosureRows: SelectedSeriesReportDashboardRow[];
	provenanceRows: SelectedSeriesReportDashboardRow[];
	exportRows: SelectedSeriesReportDashboardRow[];
	artifactRegistry: SelectedSeriesReportArtifactRegistryItem[];
};

export type SelectedSeriesReportArtifactRegistryItem = {
	id: string;
	targetKind: string;
	targetId: string;
	targetLabel: string;
	campaignId: string | null;
	campaignName: string | null;
	title: string;
	badgeLabel: string;
	meta: string[];
	rows: SelectedSeriesReportDashboardRow[];
};

export function toSelectedSeriesReportSnapshotState(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportSnapshotLocalState = {}
): SelectedSeriesReportSnapshotState {
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		return {
			status: 'not_available',
			available: false,
			campaignId: null,
			campaignName: null,
			badgeLabel: 'Not available',
			disabledReason: 'Create or select a campaign before loading the report snapshot.'
		};
	}

	const reportable = selectedCampaign.reportStatus === 'proof_only';

	if (!reportable) {
		return {
			status: 'blocked',
			available: false,
			campaignId: selectedCampaign.id,
			campaignName: selectedCampaign.name,
			badgeLabel: 'Blocked',
			disabledReason: 'Resolve report prerequisites before loading the report snapshot.'
		};
	}

	const loadedForSelectedCampaign = localState.loadedCampaignId === selectedCampaign.id;

	return {
		status: loadedForSelectedCampaign ? 'ready' : 'pending',
		available: true,
		campaignId: selectedCampaign.id,
		campaignName: selectedCampaign.name,
		badgeLabel: loadedForSelectedCampaign ? 'Preview ready' : 'Preview available',
		disabledReason: null
	};
}

export function toSelectedSeriesReportDashboardView(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportSnapshotLocalState = {}
): SelectedSeriesReportDashboardView {
	const snapshotState = toSelectedSeriesReportSnapshotState(workspace, localState);
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		return {
			title: 'Report dashboard unavailable',
			status: snapshotState.status,
			available: false,
			badgeLabel: snapshotState.badgeLabel,
			emptyMessage: 'Create or select a campaign before reviewing the report dashboard.',
			readinessRows: [
				{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
				{
					label: 'Reportable campaigns',
					value: formatCount(workspace.summary.reportableCampaignCount)
				},
				{
					label: 'Missing prerequisites',
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			],
			disclosureRows: [],
			provenanceRows: [],
			exportRows: [],
			artifactRegistry: []
		};
	}

	return {
		title: `${selectedCampaign.name.trim() || 'Untitled campaign'} report dashboard`,
		status: snapshotState.status,
		available: snapshotState.available,
		badgeLabel: snapshotState.badgeLabel,
		emptyMessage: null,
		readinessRows: toReportReadinessRows(selectedCampaign),
		disclosureRows: toReportDisclosureRows(selectedCampaign),
		provenanceRows: toReportProvenanceRows(selectedCampaign),
		exportRows: toReportExportRows(selectedCampaign),
		artifactRegistry: toReportArtifactRegistry(workspace.exportArtifacts)
	};
}

function toReportReadinessRows(
	campaign: CampaignSeriesReportsCampaignResponse
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
		{ label: 'Campaign status', value: formatCodeLabel(campaign.status) },
		{ label: 'Report status', value: formatCodeLabel(campaign.reportStatus) },
		{ label: 'Interpretation', value: formatCodeLabel(campaign.interpretationStatus) },
		{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
		{ label: 'Scores', value: formatCount(campaign.scoreCount) }
	];
}

function toReportDisclosureRows(
	campaign: CampaignSeriesReportsCampaignResponse
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: 'Disclosure', value: formatCodeLabel(campaign.disclosureState) },
		{
			label: 'Disclosure k',
			value: campaign.disclosureKMin === null ? 'Not available' : String(campaign.disclosureKMin)
		},
		{ label: 'Visible scores', value: formatCount(campaign.visibleScoreCount) },
		{ label: 'Suppressed scores', value: formatCount(campaign.suppressedScoreCount) }
	];
}

function toReportProvenanceRows(
	campaign: CampaignSeriesReportsCampaignResponse
): SelectedSeriesReportDashboardRow[] {
	return [
		idRow('Launch snapshot', campaign.latestLaunchSnapshotId),
		{ label: 'Latest launch', value: formatNullableDateTime(campaign.latestLaunchAt) },
		idRow('Scoring rule', campaign.scoringRuleId),
		idRow('Consent document', campaign.consentDocumentId),
		idRow('Retention policy', campaign.retentionPolicyId),
		idRow('Disclosure policy', campaign.disclosurePolicyId)
	];
}

function toReportExportRows(
	campaign: CampaignSeriesReportsCampaignResponse
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: 'Export files', value: formatCount(campaign.exportArtifactCount) },
		idRow('Latest export record', campaign.latestExportArtifactId),
		{
			label: 'Latest export file',
			value: campaign.latestExportArtifactFileName ?? 'Not available'
		},
		{
			label: 'Latest export status',
			value: campaign.latestExportArtifactStatus ?? 'Not available'
		},
		{
			label: 'Latest export created',
			value: formatNullableDateTime(campaign.latestExportArtifactCreatedAt)
		},
		{
			label: 'Latest export completed',
			value: formatNullableDateTime(campaign.latestExportArtifactCompletedAt)
		},
		{
			label: 'Latest export started',
			value: formatNullableDateTime(campaign.latestExportArtifactStartedAt)
		},
		{
			label: 'Latest export failed',
			value: formatNullableDateTime(campaign.latestExportArtifactFailedAt)
		},
		{
			label: 'Latest export expires',
			value: formatNullableDateTime(campaign.latestExportArtifactExpiresAt)
		},
		{
			label: 'Latest export deleted',
			value: formatNullableDateTime(campaign.latestExportArtifactDeletedAt)
		},
		{
			label: 'Latest export failure reason',
			value: campaign.latestExportArtifactFailureReasonCode ?? 'Not available'
		},
		{
			label: 'Latest export downloadable',
			value: formatBoolean(campaign.latestExportArtifactCanDownload)
		}
	];
}

function toReportArtifactRegistry(
	artifacts: CampaignSeriesReportsExportArtifactResponse[]
): SelectedSeriesReportArtifactRegistryItem[] {
	return [...artifacts]
		.sort((left, right) => {
			const createdOrder = Date.parse(right.createdAt) - Date.parse(left.createdAt);
			return createdOrder === 0 ? left.id.localeCompare(right.id) : createdOrder;
		})
		.map((artifact) => ({
			id: artifact.id,
			targetKind: artifact.targetKind,
			targetId: artifact.targetId,
			targetLabel: artifact.targetLabel.trim() || 'Untitled target',
			campaignId: artifact.campaignId,
			campaignName: artifact.campaignName?.trim() || null,
			title: artifact.fileName.trim() || 'Untitled export file',
			badgeLabel: artifact.status,
			meta: [
				formatExportFileTypeLabel(artifact.artifactType),
				formatCodeLabel(artifact.format),
				`${formatCount(artifact.rowCount)} rows`,
				`${formatCount(artifact.byteSize)} bytes`
			],
			rows: [
				idRow('Export record', artifact.id),
				{ label: 'Study context', value: artifact.targetLabel.trim() || 'Untitled target' },
				{ label: 'Context type', value: formatCodeLabel(artifact.targetKind) },
				{ label: 'Created', value: formatNullableDateTime(artifact.createdAt) },
				{ label: 'Completed', value: formatNullableDateTime(artifact.completedAt) },
				{ label: 'Started', value: formatNullableDateTime(artifact.startedAt) },
				{ label: 'Failed', value: formatNullableDateTime(artifact.failedAt) },
				{ label: 'Expires', value: formatNullableDateTime(artifact.expiresAt) },
				{ label: 'Deleted', value: formatNullableDateTime(artifact.deletedAt) },
				{ label: 'Failure reason', value: artifact.failureReasonCode ?? 'Not available' },
				{ label: 'Downloadable', value: formatBoolean(artifact.canDownload) },
				idRow('Checksum', artifact.checksumSha256)
			]
		}));
}

function idRow(label: string, value: string | null): SelectedSeriesReportDashboardRow {
	return value ? { label, value, mono: true } : { label, value: 'Not available' };
}

function formatCount(value: number) {
	return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
}

function formatBoolean(value: boolean) {
	return value ? 'Yes' : 'No';
}

function formatCodeLabel(value: string) {
	if (value === 'proof_only') {
		return 'preview';
	}

	return value.replaceAll('_', ' ');
}

function formatExportFileTypeLabel(value: string) {
	switch (value) {
		case 'report_proof_csv_codebook':
			return 'report summary CSV and codebook';
		case 'campaign_series_response_csv_codebook':
			return 'response dataset CSV and codebook';
		default:
			return formatCodeLabel(value);
	}
}

function formatNullableDateTime(value: string | null | undefined) {
	if (!value) {
		return 'Not available';
	}

	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return value;
	}

	return new Intl.DateTimeFormat('hr-HR', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	}).format(date);
}
