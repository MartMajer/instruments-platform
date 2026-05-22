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

export type SelectedSeriesResultsHandoffLaneId =
	| 'operational'
	| 'interpretation'
	| 'export'
	| 'finality';

export type SelectedSeriesResultsHandoffLane = {
	id: SelectedSeriesResultsHandoffLaneId;
	label: string;
	title: string;
	status: ProductReadModelBadgeStatus;
	detail: string;
};

export type SelectedSeriesResultsHandoffStatus = {
	overallStatus: ProductReadModelBadgeStatus;
	overallLabel: string;
	headline: string;
	guidance: string;
	nextAction: string;
	lanes: SelectedSeriesResultsHandoffLane[];
};

export type SelectedSeriesResultsPacketReviewItemId =
	| 'results'
	| 'interpretation'
	| 'export_files'
	| 'sharing';

export type SelectedSeriesResultsPacketReviewItem = {
	id: SelectedSeriesResultsPacketReviewItemId;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	detail: string;
};

export type SelectedSeriesResultsPacketReview = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	primaryAction: string;
	items: SelectedSeriesResultsPacketReviewItem[];
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
	const downloadIsResponseDataset = hasResponseExport;
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
			title: 'Review results',
			description: 'Preview disclosure-safe result summaries for the selected wave.',
			status: toReportProofStatus(hasCampaign, reportable, reportProofViewed),
			available: reportable,
			disabledReason: toReportProofDisabledReason(hasCampaign, reportable)
		},
		{
			id: 'exportArtifact',
			step: 'Step 2',
			title: 'Create report-summary export',
			description:
				'Create the aggregate results CSV and codebook. Use it outside the team only after interpretation and finality are ready.',
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
			title: 'Create response export',
			description: 'Create analysis-ready response rows and a codebook for this study.',
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
			title: 'Review export file',
			description: 'Review the latest export file details before downloading.',
			status: toArtifactStatus(hasCampaign, hasExport, artifactFetched),
			available: hasCampaign && hasExport,
			disabledReason:
				hasCampaign && hasExport ? null : 'Create or select an export file before reviewing it.'
		},
		{
			id: 'downloadCsv',
			step: 'Step 5',
			title: downloadIsResponseDataset ? 'Download response dataset CSV' : 'Download report-summary CSV',
			description: downloadIsResponseDataset
				? 'Download the analysis-ready response dataset CSV and codebook when it is ready.'
				: 'Download the report-summary CSV for review packets only. This is not an analysis-ready response dataset.',
			status: toDownloadStatus(hasCampaign, hasDownloadableExport, csvDownloaded),
			available: hasCampaign && hasDownloadableExport,
			disabledReason: hasDownloadableExport
				? hasCampaign
					? null
					: 'Create or select an export file before downloading CSV.'
				: hasExport
					? 'Select a downloadable export file before downloading CSV.'
					: 'Create or select an export file before downloading CSV.'
		}
	];
}

export function toSelectedSeriesResultsHandoffStatus(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {}
): SelectedSeriesResultsHandoffStatus {
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		const lanes: SelectedSeriesResultsHandoffLane[] = [
			{
				id: 'operational',
				label: 'Operational status',
				title: 'No wave selected',
				status: 'not_available',
				detail: 'Create or select a wave before reviewing results.'
			},
			{
				id: 'interpretation',
				label: 'Interpretation status',
				title: 'Not available',
				status: 'not_available',
				detail: 'Interpretation can be reviewed after a wave has results.'
			},
			{
				id: 'export',
				label: 'Export status',
				title: 'No export available',
				status: 'not_available',
				detail: 'Create results before creating client export files.'
			},
			{
				id: 'finality',
				label: 'Finality status',
				title: 'Not available',
				status: 'not_available',
				detail: 'Collection finality appears after a wave is selected.'
			}
		];

		return {
			overallStatus: 'not_available',
			overallLabel: 'No results selected',
			headline: 'Select a wave to review results',
			guidance: 'Results, interpretation, exports, and finality depend on a selected wave.',
			nextAction: 'Create or select a wave with submitted responses.',
			lanes
		};
	}

	const reportable = selectedCampaign.reportStatus === 'proof_only';
	const submittedResponses = selectedCampaign.submittedResponseCount ?? 0;
	const visibleScores = selectedCampaign.visibleScoreCount ?? 0;
	const scoreCount = selectedCampaign.scoreCount ?? 0;
	const hasScoredResults = scoreCount > 0 || visibleScores > 0;
	const hasExport = hasAnyExport(workspace, localState);
	const hasDownloadableExport = hasAnyDownloadableExport(workspace, localState);
	const interpretationValidated = isInterpretationValidated(
		selectedCampaign.interpretationStatus
	);
	const collectionClosed = isCollectionClosed(selectedCampaign.status);
	const collectionLive = selectedCampaign.status === 'live';
	const scoreVisibilityGap = Math.max(0, submittedResponses - visibleScores);
	const exportClientReady = hasDownloadableExport && interpretationValidated && collectionClosed;

	const operationalLane: SelectedSeriesResultsHandoffLane = !reportable
		? {
				id: 'operational',
				label: 'Operational status',
				title: 'Results are not ready',
				status: 'blocked',
				detail: 'Finish setup, launch, submissions, disclosure, and scoring before reviewing results.'
			}
		: hasScoredResults
			? {
					id: 'operational',
					label: 'Operational status',
					title: 'Preview data is ready',
					status: 'ready',
					detail:
						scoreVisibilityGap > 0
							? `${submittedResponses} submitted response${submittedResponses === 1 ? '' : 's'} and ${visibleScores} visible score${visibleScores === 1 ? '' : 's'} are available for review. ${scoreVisibilityGap} submitted response${scoreVisibilityGap === 1 ? ' is' : 's are'} not visible as scores because scoring, missing-answer rules, or disclosure still exclude them. Resolve or document the gap before sharing outside the team.`
							: `${submittedResponses} submitted response${submittedResponses === 1 ? '' : 's'} and ${visibleScores} visible score${visibleScores === 1 ? '' : 's'} are available for review.`
				}
			: {
					id: 'operational',
					label: 'Operational status',
					title: 'Waiting for scored responses',
					status: 'pending',
					detail: 'The wave exists, but scored responses are not available yet.'
				};

	const interpretationLane: SelectedSeriesResultsHandoffLane = interpretationValidated
		? {
				id: 'interpretation',
				label: 'Interpretation status',
				title: 'Interpretation reviewed',
				status: 'ready',
				detail: 'The interpretation state is ready for external result claims.'
			}
		: selectedCampaign.interpretationStatus === 'not_available'
			? {
					id: 'interpretation',
					label: 'Interpretation status',
					title: 'Interpretation not available',
					status: 'not_available',
					detail: 'Interpretation can be reviewed after scoring and disclosure are available.'
				}
			: {
					id: 'interpretation',
					label: 'Interpretation status',
				title: 'Needs interpretation validation',
				status: 'blocked',
				detail:
					'Scoring is available, but the meaning, limits, and external claims have not been reviewed.'
				};

	const exportLane: SelectedSeriesResultsHandoffLane = hasDownloadableExport
		? {
				id: 'export',
				label: 'Export status',
				title: exportClientReady ? 'Share-ready export ready' : 'Internal preview export ready',
				status: exportClientReady ? 'ready' : 'pending',
				detail: exportClientReady
					? 'A downloadable export file is available for external sharing.'
					: 'A downloadable file exists, but use it internally until interpretation validation and collection finality are ready.'
			}
		: hasExport
			? {
					id: 'export',
					label: 'Export status',
					title: 'Export exists but is not downloadable',
					status: 'pending',
					detail: 'Review the export file and confirm it is downloadable before sharing.'
				}
			: reportable
				? {
						id: 'export',
						label: 'Export status',
						title: 'Share-ready export not created',
						status: 'pending',
						detail: 'Create a share-ready export before sending files outside the team.'
					}
				: {
						id: 'export',
						label: 'Export status',
						title: 'Export blocked',
						status: 'blocked',
						detail: 'Results must be ready before export files can be created.'
					};

	const finalityLane: SelectedSeriesResultsHandoffLane = collectionClosed
		? {
				id: 'finality',
				label: 'Finality status',
				title: 'Collection closed',
				status: 'ready',
				detail: 'The response window is closed, so the result set is stable for sharing.'
			}
		: collectionLive
			? {
					id: 'finality',
					label: 'Finality status',
					title: 'Preliminary live data',
					status: 'pending',
					detail: 'Collection is still live. Results can change until the wave is closed.'
				}
			: {
					id: 'finality',
					label: 'Finality status',
					title: 'Collection not finalized',
					status: 'pending',
					detail: 'Close collection when the response window is finished.'
				};

	const lanes = [operationalLane, interpretationLane, exportLane, finalityLane];
	const clientReady = lanes.every((lane) => lane.status === 'ready');
	const previewReady = operationalLane.status === 'ready';

	if (clientReady) {
		return {
			overallStatus: 'ready',
			overallLabel: 'Ready to share',
			headline: 'Results are ready to share',
			guidance: 'Operational data, interpretation, export, and finality are ready.',
			nextAction: 'Download the export file or review waves.',
			lanes
		};
	}

	if (previewReady) {
		return {
			overallStatus: 'blocked',
			overallLabel: 'Not share-ready',
			headline: 'Preview ready; not ready to share',
			guidance:
				'Use these results for internal review only. Review interpretation, create the export file, and resolve finality before sharing outside the team.',
			nextAction: toHandoffNextAction(interpretationLane, exportLane, finalityLane),
			lanes
		};
	}

	return {
		overallStatus: operationalLane.status,
		overallLabel: 'Results not ready',
		headline: 'Results are not ready for review',
		guidance: 'Finish the missing operational prerequisites before reviewing or exporting results.',
		nextAction: operationalLane.detail,
		lanes
	};
}

export function toSelectedSeriesResultsPacketReview(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {}
): SelectedSeriesResultsPacketReview {
	const handoffStatus = toSelectedSeriesResultsHandoffStatus(workspace, localState);
	const operationalLane = findHandoffLane(handoffStatus, 'operational');
	const hasCampaign = Boolean(workspace.selectedCampaign);
	const hasResponseDataset = hasResponseDatasetExport(workspace, localState);
	const items: SelectedSeriesResultsPacketReviewItem[] = [
		toResultsPacketItem(handoffStatus),
		toInterpretationPacketItem(handoffStatus),
		toExportFilesPacketItem(workspace, localState, operationalLane.status === 'ready'),
		toSharingPacketItem(handoffStatus)
	];

	return {
		title: 'Results packet',
		description:
			'Check what can be reviewed, which export file is appropriate, and whether anything is safe to share outside the team.',
		status: !hasCampaign
			? 'not_available'
			: handoffStatus.overallStatus === 'ready'
				? 'ready'
				: operationalLane.status === 'ready'
					? 'pending'
					: 'blocked',
		primaryAction:
			handoffStatus.overallStatus === 'ready' && hasResponseDataset
				? 'Download the response dataset or review waves.'
				: handoffStatus.nextAction,
		items
	};
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
	const hasDownloadableRegistryExport = workspace.exportArtifacts.some(
		(artifact) => artifact.canDownload
	);
	const latestExportDownloadable = Boolean(
		workspace.selectedCampaign?.latestExportArtifactId &&
			workspace.selectedCampaign.latestExportArtifactCanDownload
	);
	const hasPersistedExport = hasExistingReportExport || hasExistingResponseExport;
	const hasPersistedDownloadableExport = latestExportDownloadable || hasDownloadableRegistryExport;
	const doneByActionId: Record<SelectedSeriesReportsWorkflowActionId, boolean> = {
		reportProof: Boolean(localState.reportProofViewed || hasPersistedExport),
		exportArtifact: Boolean(localState.exportCreated || hasExistingReportExport),
		responseExport: Boolean(localState.responseExportCreated || hasExistingResponseExport),
		fetchArtifact: Boolean(localState.artifactFetched || hasPersistedDownloadableExport),
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
		return 'Review results before creating a response export.';
	}

	if (!reportable) {
		return 'Resolve report prerequisites before creating a response export.';
	}

	if (responseExportCreated) {
		return 'Response export was created in this session.';
	}

	if (hasExistingResponseExport) {
		return 'Response export already exists for this study.';
	}

	return reportProofViewed ? null : 'Review results before creating a response export.';
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
		return 'Create or select a wave before reviewing results.';
	}

	return reportable ? null : 'Resolve report prerequisites before reviewing results.';
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
		return 'Review results before creating a report export.';
	}

	if (!reportable) {
		return 'Resolve report prerequisites before creating a report export.';
	}

	if (exportCreated) {
		return 'Report export was created in this session.';
	}

	return reportProofViewed ? null : 'Review results before creating a report export.';
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

function hasAnyExport(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState
) {
	return Boolean(
		workspace.selectedCampaign?.latestExportArtifactId ||
			localState.exportCreated ||
			localState.responseExportCreated ||
			workspace.exportArtifacts.length > 0
	);
}

function hasAnyDownloadableExport(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState
) {
	return Boolean(
		localState.exportCreated ||
			localState.responseExportCreated ||
			workspace.selectedCampaign?.latestExportArtifactCanDownload ||
			workspace.exportArtifacts.some((artifact) => artifact.canDownload)
	);
}

function isInterpretationValidated(status: string | null | undefined) {
	const normalized = status ?? '';
	return (
		normalized === 'validated' ||
		normalized === 'validated_interpretation' ||
		normalized === 'official_validated'
	);
}

function isCollectionClosed(status: string | null | undefined) {
	return status === 'closed' || status === 'completed' || status === 'ended';
}

function toResultsPacketItem(
	handoffStatus: SelectedSeriesResultsHandoffStatus
): SelectedSeriesResultsPacketReviewItem {
	const lane = findHandoffLane(handoffStatus, 'operational');

	if (lane.status === 'ready') {
		return {
			id: 'results',
			label: 'Results',
			status: 'ready',
			summary: 'Results preview is ready',
			detail: lane.detail
		};
	}

	return {
		id: 'results',
		label: 'Results',
		status: lane.status,
		summary: lane.title,
		detail: lane.detail
	};
}

function toInterpretationPacketItem(
	handoffStatus: SelectedSeriesResultsHandoffStatus
): SelectedSeriesResultsPacketReviewItem {
	const lane = findHandoffLane(handoffStatus, 'interpretation');

	return {
		id: 'interpretation',
		label: 'Interpretation',
		status: lane.status,
		summary: lane.status === 'ready' ? 'Interpretation reviewed' : lane.title,
		detail: lane.detail
	};
}

function toExportFilesPacketItem(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState,
	resultsReady: boolean
): SelectedSeriesResultsPacketReviewItem {
	if (!workspace.selectedCampaign) {
		return {
			id: 'export_files',
			label: 'Export files',
			status: 'not_available',
			summary: 'No export file yet',
			detail: 'Select a wave before preparing files.'
		};
	}

	if (hasResponseDatasetExport(workspace, localState) && hasAnyDownloadableExport(workspace, localState)) {
		return {
			id: 'export_files',
			label: 'Export files',
			status: 'ready',
			summary: 'Response dataset ready',
			detail: 'Use this CSV and codebook for analysis. Keep interpretation and finality checks with the file.'
		};
	}

	if (hasAnyDownloadableExport(workspace, localState)) {
		return {
			id: 'export_files',
			label: 'Export files',
			status: 'pending',
			summary: 'Report-summary file ready for internal review',
			detail:
				'This file summarizes aggregate results. Create a response dataset when row-level analysis is needed.'
		};
	}

	if (hasAnyExport(workspace, localState)) {
		return {
			id: 'export_files',
			label: 'Export files',
			status: 'pending',
			summary: 'Export file needs review',
			detail: 'Review the generated file and confirm it is downloadable before relying on it.'
		};
	}

	return {
		id: 'export_files',
		label: 'Export files',
		status: resultsReady ? 'pending' : 'blocked',
		summary: resultsReady ? 'Create the response dataset when ready' : 'Export blocked',
		detail: resultsReady
			? 'Create the response dataset for analysis, or create the report-summary file for internal review.'
			: 'Results must be ready before export files can be created.'
	};
}

function toSharingPacketItem(
	handoffStatus: SelectedSeriesResultsHandoffStatus
): SelectedSeriesResultsPacketReviewItem {
	if (handoffStatus.overallStatus === 'ready') {
		return {
			id: 'sharing',
			label: 'Sharing',
			status: 'ready',
			summary: 'Ready to share',
			detail:
				'Results, interpretation, export file, and collection finality are ready. Keep disclosure limits with the shared packet.'
		};
	}

	const operationalLane = findHandoffLane(handoffStatus, 'operational');

	if (operationalLane.status === 'ready') {
		return {
			id: 'sharing',
			label: 'Sharing',
			status: 'blocked',
			summary: 'Do not share yet',
			detail: handoffStatus.guidance
		};
	}

	return {
		id: 'sharing',
		label: 'Sharing',
		status: operationalLane.status,
		summary: 'Nothing ready to share yet',
		detail: handoffStatus.guidance
	};
}

function findHandoffLane(
	handoffStatus: SelectedSeriesResultsHandoffStatus,
	id: SelectedSeriesResultsHandoffLaneId
) {
	return handoffStatus.lanes.find((lane) => lane.id === id) ?? handoffStatus.lanes[0];
}

function hasResponseDatasetExport(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState
) {
	return Boolean(
		localState.responseExportCreated ||
			workspace.exportArtifacts.some(
				(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
			)
	);
}

function toHandoffNextAction(
	interpretationLane: SelectedSeriesResultsHandoffLane,
	exportLane: SelectedSeriesResultsHandoffLane,
	finalityLane: SelectedSeriesResultsHandoffLane
) {
	const interpretationOpen = interpretationLane.status !== 'ready';
	const exportOpen = exportLane.status !== 'ready';
	const finalityOpen = finalityLane.status !== 'ready';

	if (interpretationOpen && exportOpen) {
		return 'Review interpretation limits before sharing; keep the current report-summary export internal.';
	}

	if (interpretationOpen) {
		return 'Validate interpretation limits before using results with a client.';
	}

	if (exportOpen) {
		return 'Generate a share-ready export only after the remaining gates pass.';
	}

	if (finalityOpen) {
		return 'Close collection or keep the results clearly marked as preliminary live data.';
	}

	return 'Review the share-ready export.';
}
