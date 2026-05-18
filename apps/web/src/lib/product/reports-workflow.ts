import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesReportsWorkflowActionId =
	| 'reportProof'
	| 'exportArtifact'
	| 'responseExport'
	| 'fetchArtifact'
	| 'downloadCsv';

export type SelectedSeriesReportsWorkflowLocalState = {
	reportProofViewed?: boolean;
	exportCreated?: boolean;
	responseExportCreated?: boolean;
	artifactFetched?: boolean;
	csvDownloaded?: boolean;
};

export type SelectedSeriesReportsWorkflowAction = {
	id: SelectedSeriesReportsWorkflowActionId;
	step: string;
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	disabledReason: string | null;
};

export type SelectedSeriesReportsPathStepState = 'done' | 'current' | 'blocked';

export type SelectedSeriesReportsPathStep = SelectedSeriesReportsWorkflowAction & {
	pathState: SelectedSeriesReportsPathStepState;
};

export type SelectedSeriesReportsPath = {
	steps: SelectedSeriesReportsPathStep[];
	currentActionId: SelectedSeriesReportsWorkflowActionId;
	currentAction: SelectedSeriesReportsWorkflowAction;
	completedCount: number;
	totalCount: number;
};

export function toSelectedSeriesReportsWorkflowActions(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {}
): SelectedSeriesReportsWorkflowAction[] {
	const selectedCampaign = workspace.selectedCampaign;
	const hasCampaign = Boolean(selectedCampaign);
	const reportable = selectedCampaign?.reportStatus === 'proof_only';
	const reportProofViewed = Boolean(localState.reportProofViewed);
	const hasRegistryReportExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'report_proof_csv_codebook'
	);
	const hasExistingResponseExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
	);
	const hasDownloadableRegistryExport = workspace.exportArtifacts.some(
		(artifact) => artifact.canDownload
	);
	const latestExportDownloadable = Boolean(
		selectedCampaign?.latestExportArtifactId && selectedCampaign.latestExportArtifactCanDownload
	);
	const hasExistingReportExport =
		hasRegistryReportExport ||
		(Boolean(selectedCampaign?.latestExportArtifactId) && !hasExistingResponseExport);
	const exportCreated = Boolean(localState.exportCreated);
	const responseExportCreated = Boolean(localState.responseExportCreated);
	const hasResponseExport = hasExistingResponseExport || responseExportCreated;
	const hasExport =
		Boolean(selectedCampaign?.latestExportArtifactId) || exportCreated || hasResponseExport;
	const hasDownloadableExport =
		latestExportDownloadable ||
		hasDownloadableRegistryExport ||
		exportCreated ||
		responseExportCreated;
	const artifactFetched = Boolean(localState.artifactFetched);
	const csvDownloaded = Boolean(localState.csvDownloaded);

	return [
		{
			id: 'reportProof',
			step: 'Step 1',
			title: 'Report preview',
			description: 'Review the disclosure-safe aggregate report preview for the selected campaign.',
			status: toReportProofStatus(hasCampaign, reportable, reportProofViewed),
			available: reportable,
			disabledReason: toReportProofDisabledReason(hasCampaign, reportable)
		},
		{
			id: 'exportArtifact',
			step: 'Step 2',
			title: 'Export artifact',
			description: 'Create a governed CSV/codebook export artifact for the selected campaign.',
			status: toExportStatus(
				hasCampaign,
				reportable,
				reportProofViewed,
				hasExistingReportExport || exportCreated
			),
			available: reportable && reportProofViewed && !exportCreated,
			disabledReason: toExportDisabledReason(
				hasCampaign,
				reportable,
				reportProofViewed,
				exportCreated
			)
		},
		{
			id: 'responseExport',
			step: 'Step 3',
			title: 'Response export',
			description: 'Create a governed campaign-series response CSV/codebook artifact.',
			status: toResponseExportStatus(hasCampaign, reportable, reportProofViewed, hasResponseExport),
			available:
				reportable && reportProofViewed && !responseExportCreated && !hasExistingResponseExport,
			disabledReason: toResponseExportDisabledReason(
				hasCampaign,
				reportable,
				reportProofViewed,
				responseExportCreated,
				hasExistingResponseExport
			)
		},
		{
			id: 'fetchArtifact',
			step: 'Step 4',
			title: 'Stored artifact',
			description: 'Fetch the stored export artifact metadata.',
			status: toArtifactStatus(hasCampaign, hasExport, artifactFetched),
			available: hasCampaign && hasExport,
			disabledReason:
				hasCampaign && hasExport ? null : 'Create or select an export artifact before fetching it.'
		},
		{
			id: 'downloadCsv',
			step: 'Step 5',
			title: 'CSV download',
			description: 'Download the CSV for local review.',
			status: toDownloadStatus(hasCampaign, hasDownloadableExport, csvDownloaded),
			available: hasCampaign && hasDownloadableExport,
			disabledReason: hasDownloadableExport
				? hasCampaign
					? null
					: 'Create or select an export artifact before downloading CSV.'
				: hasExport
					? 'Select a downloadable export artifact before downloading CSV.'
					: 'Create or select an export artifact before downloading CSV.'
		}
	];
}

export function toSelectedSeriesReportsPath(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {}
): SelectedSeriesReportsPath {
	const actions = toSelectedSeriesReportsWorkflowActions(workspace, localState);
	const hasExistingReportExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'report_proof_csv_codebook'
	);
	const hasExistingResponseExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
	);
	const doneByActionId: Record<SelectedSeriesReportsWorkflowActionId, boolean> = {
		reportProof: Boolean(localState.reportProofViewed),
		exportArtifact: Boolean(localState.exportCreated || hasExistingReportExport),
		responseExport: Boolean(localState.responseExportCreated || hasExistingResponseExport),
		fetchArtifact: Boolean(localState.artifactFetched),
		downloadCsv: Boolean(localState.csvDownloaded)
	};
	const currentAction =
		actions.find((action) => !doneByActionId[action.id] && action.available) ??
		actions.find((action) => !doneByActionId[action.id]) ??
		actions.at(-1) ??
		actions[0];
	const steps = actions.map((action) => ({
		...action,
		pathState: toPathStepState(action.id, currentAction.id, doneByActionId)
	}));

	return {
		steps,
		currentActionId: currentAction.id,
		currentAction,
		completedCount: steps.filter((step) => step.pathState === 'done').length,
		totalCount: steps.length
	};
}

function toResponseExportStatus(
	hasCampaign: boolean,
	reportable: boolean,
	reportProofViewed: boolean,
	hasResponseExport: boolean
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (hasResponseExport) {
		return 'ready';
	}

	if (!reportable || !reportProofViewed) {
		return 'blocked';
	}

	return 'pending';
}

function toResponseExportDisabledReason(
	hasCampaign: boolean,
	reportable: boolean,
	reportProofViewed: boolean,
	responseExportCreated: boolean,
	hasExistingResponseExport: boolean
) {
	if (!hasCampaign) {
		return 'View report preview before creating a response export artifact.';
	}

	if (!reportable) {
		return 'Resolve report prerequisites before creating a response export artifact.';
	}

	if (responseExportCreated) {
		return 'Response export artifact was created in this session.';
	}

	if (hasExistingResponseExport) {
		return 'Response export artifact already exists for this series.';
	}

	return reportProofViewed ? null : 'View report preview before creating a response export artifact.';
}

function toReportProofStatus(
	hasCampaign: boolean,
	reportable: boolean,
	reportProofViewed: boolean
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (reportProofViewed) {
		return 'ready';
	}

	return reportable ? 'pending' : 'blocked';
}

function toReportProofDisabledReason(hasCampaign: boolean, reportable: boolean) {
	if (!hasCampaign) {
		return 'Create or select a campaign before reviewing the report preview.';
	}

	return reportable ? null : 'Resolve report prerequisites before reviewing the report preview.';
}

function toExportStatus(
	hasCampaign: boolean,
	reportable: boolean,
	reportProofViewed: boolean,
	hasExport: boolean
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (hasExport) {
		return 'ready';
	}

	if (!reportable || !reportProofViewed) {
		return 'blocked';
	}

	return 'pending';
}

function toExportDisabledReason(
	hasCampaign: boolean,
	reportable: boolean,
	reportProofViewed: boolean,
	exportCreated: boolean
) {
	if (!hasCampaign) {
		return 'View report preview before creating an export artifact.';
	}

	if (!reportable) {
		return 'Resolve report prerequisites before creating an export artifact.';
	}

	if (exportCreated) {
		return 'Export artifact was created in this session.';
	}

	return reportProofViewed ? null : 'View report preview before creating an export artifact.';
}

function toArtifactStatus(
	hasCampaign: boolean,
	hasExport: boolean,
	artifactFetched: boolean
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (artifactFetched) {
		return 'ready';
	}

	return hasExport ? 'pending' : 'blocked';
}

function toPathStepState(
	actionId: SelectedSeriesReportsWorkflowActionId,
	currentActionId: SelectedSeriesReportsWorkflowActionId,
	doneByActionId: Record<SelectedSeriesReportsWorkflowActionId, boolean>
): SelectedSeriesReportsPathStepState {
	if (doneByActionId[actionId]) {
		return 'done';
	}

	if (actionId === currentActionId) {
		return 'current';
	}

	return 'blocked';
}

function toDownloadStatus(
	hasCampaign: boolean,
	hasExport: boolean,
	csvDownloaded: boolean
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (csvDownloaded) {
		return 'ready';
	}

	return hasExport ? 'pending' : 'blocked';
}
