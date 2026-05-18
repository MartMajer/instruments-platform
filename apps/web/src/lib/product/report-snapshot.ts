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
		badgeLabel: loadedForSelectedCampaign ? 'Proof/local' : 'Proof-only',
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
		{ label: 'Latest launch', value: campaign.latestLaunchAt ?? 'Not available' },
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
		{ label: 'Export artifacts', value: formatCount(campaign.exportArtifactCount) },
		idRow('Latest export artifact', campaign.latestExportArtifactId),
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
			value: campaign.latestExportArtifactCreatedAt ?? 'Not available'
		},
		{
			label: 'Latest export completed',
			value: campaign.latestExportArtifactCompletedAt ?? 'Not available'
		},
		{
			label: 'Latest export started',
			value: campaign.latestExportArtifactStartedAt ?? 'Not available'
		},
		{
			label: 'Latest export failed',
			value: campaign.latestExportArtifactFailedAt ?? 'Not available'
		},
		{
			label: 'Latest export expires',
			value: campaign.latestExportArtifactExpiresAt ?? 'Not available'
		},
		{
			label: 'Latest export deleted',
			value: campaign.latestExportArtifactDeletedAt ?? 'Not available'
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
			title: artifact.fileName.trim() || 'Untitled export artifact',
			badgeLabel: artifact.status,
			meta: [
				formatCodeLabel(artifact.artifactType),
				formatCodeLabel(artifact.format),
				`${formatCount(artifact.rowCount)} rows`,
				`${formatCount(artifact.byteSize)} bytes`
			],
			rows: [
				idRow('Artifact', artifact.id),
				{ label: 'Target', value: artifact.targetLabel.trim() || 'Untitled target' },
				{ label: 'Target scope', value: formatCodeLabel(artifact.targetKind) },
				{ label: 'Created', value: artifact.createdAt },
				{ label: 'Completed', value: artifact.completedAt ?? 'Not available' },
				{ label: 'Started', value: artifact.startedAt ?? 'Not available' },
				{ label: 'Failed', value: artifact.failedAt ?? 'Not available' },
				{ label: 'Expires', value: artifact.expiresAt ?? 'Not available' },
				{ label: 'Deleted', value: artifact.deletedAt ?? 'Not available' },
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
	return value.replaceAll('_', ' ');
}
