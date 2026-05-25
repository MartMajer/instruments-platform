import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import type {
	CampaignReportProofResponse,
	ReportProofExportArtifactResponse,
	ReportScoreSummaryResponse
} from '$lib/api/setup';
import type { AppLocale } from '$lib/i18n/localization';
import { appMessage, type AppMessageId } from '$lib/i18n/messages';
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
	| 'responses'
	| 'scores'
	| 'export_files'
	| 'use_status';

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

export type SelectedSeriesScoreMethodReviewItemId =
	| 'outputs'
	| 'coverage'
	| 'direction_scale'
	| 'missingness'
	| 'interpretation_boundary';

export type SelectedSeriesScoreMethodReviewItem = {
	id: SelectedSeriesScoreMethodReviewItemId;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	detail: string;
};

export type SelectedSeriesScoreMethodReview = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	items: SelectedSeriesScoreMethodReviewItem[];
};

export type SelectedSeriesExportPreviewItemId =
	| 'file_purpose'
	| 'row_shape'
	| 'wave_fields'
	| 'trajectory_keys'
	| 'variables_values'
	| 'missingness'
	| 'score_outputs';

export type SelectedSeriesExportPreviewItem = {
	id: SelectedSeriesExportPreviewItemId;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	detail: string;
};

export type SelectedSeriesExportPreview = {
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	downloadLabel: string;
	items: SelectedSeriesExportPreviewItem[];
};

type ExportCodebookColumn = {
	name: string;
	source: string | null;
	questionCode: string | null;
	dimensionCode: string | null;
	metadataKind: string | null;
	hasMissingCodes: boolean;
	hasScale: boolean;
	hasValueLabels: boolean;
	hasAnswerMetadata: boolean;
};

type ExportCodebookSummary = {
	artifactType: string | null;
	rowCount: number | null;
	campaignCount: number | null;
	trajectoryCount: number | null;
	trajectoryIdPolicy: string | null;
	hasMissingTreatment: boolean;
	columns: ExportCodebookColumn[];
};

type ReportsWorkflowActionCopy = {
	title: string;
	description: string;
};

export type SelectedSeriesReportsWorkflowCopy = {
	locale?: string;
	stepNumber: (number: number) => string;
	surface: {
		reviewActionsAria: string;
		flowKicker: string;
		title: string;
		description: string;
		useDecisionLabel: string;
		resultsUseReviewAria: string;
		nextActionLabel: string;
		scoreMethodLabel: string;
		scoreMethodReviewAria: string;
		exportPreviewLabel: string;
		exportPreviewAria: string;
	};
	actions: {
		reportProof: ReportsWorkflowActionCopy;
		exportArtifact: ReportsWorkflowActionCopy & {
			optionalTitle: string;
			optionalDescription: string;
		};
		responseExport: ReportsWorkflowActionCopy;
		fetchArtifact: ReportsWorkflowActionCopy;
		downloadCsv: {
			responseDatasetTitle: string;
			responseDatasetDescription: string;
			reportSummaryTitle: string;
			reportSummaryDescription: string;
		};
	};
	disabled: {
		createOrSelectWaveBeforeReviewingResults: string;
		resolveReportPrerequisitesBeforeReviewingResults: string;
		reviewResultsBeforeCreatingReportExport: string;
		resolveReportPrerequisitesBeforeCreatingReportExport: string;
		reportExportCreatedThisSession: string;
		responseDatasetAlreadyExistsReportOptional: string;
		reportSummaryExportAlreadyExists: string;
		reviewResultsBeforeCreatingResponseExport: string;
		resolveReportPrerequisitesBeforeCreatingResponseExport: string;
		responseExportCreatedThisSession: string;
		responseExportAlreadyExists: string;
		createOrSelectExportBeforeReview: string;
		createOrSelectExportBeforeDownload: string;
		selectDownloadableExportBeforeDownload: string;
	};
	packetReview: {
		title: string;
		description: string;
		primaryAction: {
			noCampaign: string;
			noResponses: string;
			noVisibleScores: string;
			createExport: string;
			downloadDataset: string;
			documentInterpretation: string;
			preliminary: string;
		};
	};
	scoreMethodReview: {
		title: string;
		description: string;
	};
	exportPreview: {
		title: string;
		description: string;
		createOrSelectWaveFirst: string;
		reviewExportFileFirst: string;
		selectWavePendingDetail: string;
		reviewFilePendingDetail: string;
		downloadResponseDatasetCsv: string;
		downloadReportSummaryCsv: string;
	};
};

export const defaultSelectedSeriesReportsWorkflowCopy: SelectedSeriesReportsWorkflowCopy = {
	locale: 'en',
	stepNumber: (number) => `Step ${number}`,
	surface: {
		reviewActionsAria: 'Review and export actions',
		flowKicker: 'Study flow · Results',
		title: 'Review and export results',
		description:
			'Review aggregate results, check whether they are ready to share, and create export files when ready.',
		useDecisionLabel: 'Use decision',
		resultsUseReviewAria: 'Results use review',
		nextActionLabel: 'Next action',
		scoreMethodLabel: 'Score method',
		scoreMethodReviewAria: 'Score method review',
		exportPreviewLabel: 'Export preview',
		exportPreviewAria: 'Export preview'
	},
	actions: {
		reportProof: {
			title: 'Review results',
			description: 'Preview disclosure-safe result summaries for the selected wave.'
		},
		exportArtifact: {
			title: 'Create report-summary export',
			optionalTitle: 'Report-summary export optional',
			description:
				'Create the aggregate results CSV and codebook. Use it outside the team only after interpretation and finality are ready.',
			optionalDescription:
				'A response dataset already exists. A report-summary export is optional and not required before download.'
		},
		responseExport: {
			title: 'Create response export',
			description: 'Create analysis-ready response rows and a codebook for this study.'
		},
		fetchArtifact: {
			title: 'Review export file',
			description: 'Review the latest export file details before downloading.'
		},
		downloadCsv: {
			responseDatasetTitle: 'Download response dataset CSV',
			responseDatasetDescription:
				'Download the analysis-ready response dataset CSV and codebook when it is ready.',
			reportSummaryTitle: 'Download report-summary CSV',
			reportSummaryDescription:
				'Download the report-summary CSV for review packets only. This is not an analysis-ready response dataset.'
		}
	},
	disabled: {
		createOrSelectWaveBeforeReviewingResults: 'Create or select a wave before reviewing results.',
		resolveReportPrerequisitesBeforeReviewingResults:
			'Resolve report prerequisites before reviewing results.',
		reviewResultsBeforeCreatingReportExport: 'Review results before creating a report export.',
		resolveReportPrerequisitesBeforeCreatingReportExport:
			'Resolve report prerequisites before creating a report export.',
		reportExportCreatedThisSession: 'Report export was created in this session.',
		responseDatasetAlreadyExistsReportOptional:
			'Response dataset already exists; report-summary export is optional.',
		reportSummaryExportAlreadyExists: 'Report-summary export already exists for this study.',
		reviewResultsBeforeCreatingResponseExport: 'Review results before creating a response export.',
		resolveReportPrerequisitesBeforeCreatingResponseExport:
			'Resolve report prerequisites before creating a response export.',
		responseExportCreatedThisSession: 'Response export was created in this session.',
		responseExportAlreadyExists: 'Response export already exists for this study.',
		createOrSelectExportBeforeReview: 'Create or select an export file before reviewing it.',
		createOrSelectExportBeforeDownload: 'Create or select an export file before downloading CSV.',
		selectDownloadableExportBeforeDownload:
			'Select a downloadable export file before downloading CSV.'
	},
	packetReview: {
		title: 'Can these results be used?',
		description:
			'Check whether you have responses, visible scores, an export file, and a clear use limit.',
		primaryAction: {
			noCampaign: 'Create or select a wave before reviewing results.',
			noResponses: 'Collect responses before reviewing results.',
			noVisibleScores:
				'Use raw response export for internal analysis, or review result-output scoring, missing-answer rules, and disclosure.',
			createExport:
				'Create a response export for analysis, or create a report-summary file for internal review.',
			downloadDataset: 'Download the response dataset for analysis.',
			documentInterpretation:
				'Use the response dataset internally; document score meaning before sharing conclusions.',
			preliminary: 'Use as preliminary internal data until collection is closed.'
		}
	},
	scoreMethodReview: {
		title: 'How were these scores produced?',
		description:
			'Review score outputs, coverage, missing-answer handling, and interpretation limits before using results.'
	},
	exportPreview: {
		title: 'What is in this export?',
		description:
			'Review file purpose, row shape, wave fields, trajectory keys, variables, missingness, and score outputs before downloading.',
		createOrSelectWaveFirst: 'Create or select a wave first',
		reviewExportFileFirst: 'Review export file first',
		selectWavePendingDetail: 'Select a wave before preparing export files.',
		reviewFilePendingDetail: 'Review the export file to inspect its CSV and codebook contents.',
		downloadResponseDatasetCsv: 'Download response dataset CSV',
		downloadReportSummaryCsv: 'Download report-summary CSV'
	}
};

export function toSelectedSeriesReportsWorkflowActions(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {},
	copy: SelectedSeriesReportsWorkflowCopy = defaultSelectedSeriesReportsWorkflowCopy
): SelectedSeriesReportsWorkflowAction[] {
	const selectedCampaign = workspace.selectedCampaign;
	const actionCopy = copy.actions;
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
	const hasDownloadableResponseExport = workspace.exportArtifacts.some(
		(artifact) =>
			artifact.artifactType === 'campaign_series_response_csv_codebook' && artifact.canDownload
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
	const resultsReviewed =
		reportProofViewed || hasExistingReportExport || exportCreated || hasResponseExport;
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
			step: copy.stepNumber(1),
			title: actionCopy.reportProof.title,
			description: actionCopy.reportProof.description,
			status: toReportProofStatus(hasCampaign, reportable, resultsReviewed),
			available: reportable,
			disabledReason: toReportProofDisabledReason(hasCampaign, reportable, copy)
		},
		{
			id: 'exportArtifact',
			step: copy.stepNumber(2),
			title: hasResponseExport
				? actionCopy.exportArtifact.optionalTitle
				: actionCopy.exportArtifact.title,
			description: hasResponseExport
				? actionCopy.exportArtifact.optionalDescription
				: actionCopy.exportArtifact.description,
			status: toExportStatus(
				hasCampaign,
				reportable,
				resultsReviewed,
				hasExistingReportExport || exportCreated || hasResponseExport
			),
			available:
				reportable && resultsReviewed && !exportCreated && !hasExistingReportExport && !hasResponseExport,
			disabledReason: toExportDisabledReason(
				hasCampaign,
				reportable,
				resultsReviewed,
				exportCreated,
				hasExistingReportExport,
				hasResponseExport,
				copy
			)
		},
		{
			id: 'responseExport',
			step: copy.stepNumber(3),
			title: actionCopy.responseExport.title,
			description: actionCopy.responseExport.description,
			status: toResponseExportStatus(hasCampaign, reportable, resultsReviewed, hasResponseExport),
			available:
				reportable && resultsReviewed && !responseExportCreated && !hasExistingResponseExport,
			disabledReason: toResponseExportDisabledReason(
				hasCampaign,
				reportable,
				resultsReviewed,
				responseExportCreated,
				hasExistingResponseExport,
				copy
			)
		},
		{
			id: 'fetchArtifact',
			step: copy.stepNumber(4),
			title: actionCopy.fetchArtifact.title,
			description: actionCopy.fetchArtifact.description,
			status: toArtifactStatus(hasCampaign, hasExport, artifactFetched),
			available: hasCampaign && hasExport,
			disabledReason:
				hasCampaign && hasExport ? null : copy.disabled.createOrSelectExportBeforeReview
		},
		{
			id: 'downloadCsv',
			step: copy.stepNumber(5),
			title: downloadIsResponseDataset
				? actionCopy.downloadCsv.responseDatasetTitle
				: actionCopy.downloadCsv.reportSummaryTitle,
			description: downloadIsResponseDataset
				? actionCopy.downloadCsv.responseDatasetDescription
				: actionCopy.downloadCsv.reportSummaryDescription,
			status: toDownloadStatus(hasCampaign, hasDownloadableExport, csvDownloaded),
			available: hasCampaign && hasDownloadableExport,
			disabledReason: hasDownloadableExport
				? hasCampaign
					? null
					: copy.disabled.createOrSelectExportBeforeDownload
				: hasExport
					? copy.disabled.selectDownloadableExportBeforeDownload
					: copy.disabled.createOrSelectExportBeforeDownload
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
	localState: SelectedSeriesReportsWorkflowLocalState = {},
	copy: SelectedSeriesReportsWorkflowCopy = defaultSelectedSeriesReportsWorkflowCopy
): SelectedSeriesResultsPacketReview {
	const campaign = workspace.selectedCampaign;
	const hasCampaign = Boolean(campaign);
	const hasResponseDataset = hasResponseDatasetExport(workspace, localState);
	const hasDownloadableExport = hasAnyDownloadableExport(workspace, localState);
	const submittedResponses = campaign?.submittedResponseCount ?? 0;
	const visibleScores = campaign?.visibleScoreCount ?? 0;
	const hasResponses = submittedResponses > 0;
	const hasVisibleScores = visibleScores > 0;
	const interpretationReviewed = isInterpretationValidated(campaign?.interpretationStatus);
	const collectionClosed = isCollectionClosed(campaign?.status);
	const responseDatasetReady = hasResponseDataset && hasDownloadableExport;
	const controlledSharingReady =
		hasVisibleScores && responseDatasetReady && interpretationReviewed && collectionClosed;
	const items: SelectedSeriesResultsPacketReviewItem[] = [
		toResponsesPacketItem(campaign?.submittedResponseCount ?? null),
		toScoresPacketItem(campaign?.submittedResponseCount ?? null, campaign?.visibleScoreCount ?? null),
		toExportFilesPacketItem(workspace, localState, hasVisibleScores),
		toUseStatusPacketItem({
			hasCampaign,
			hasResponses,
			hasVisibleScores,
			responseDatasetReady,
			interpretationReviewed,
			collectionClosed
		})
	];

	return {
		title: copy.packetReview.title,
		description: copy.packetReview.description,
		status: !hasCampaign
			? 'not_available'
			: controlledSharingReady
				? 'ready'
				: hasResponses && hasVisibleScores
					? 'pending'
					: 'blocked',
		primaryAction: toResultsPacketPrimaryAction({
			hasCampaign,
			hasResponses,
			hasVisibleScores,
			responseDatasetReady,
			controlledSharingReady,
			interpretationReviewed,
			collectionClosed
		}, copy),
		items: items.map((item) => localizeReportsWorkflowItem(item, copy))
	};
}

export function toSelectedSeriesScoreMethodReview(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	reportProof: CampaignReportProofResponse | null = null,
	copy: SelectedSeriesReportsWorkflowCopy = defaultSelectedSeriesReportsWorkflowCopy
): SelectedSeriesScoreMethodReview {
	const campaign = workspace.selectedCampaign;
	const hasCampaign = Boolean(campaign);
	const hasScoringRule = Boolean(campaign?.scoringRuleId);
	const proofScores = reportProof?.scores ?? [];
	const interpretationReviewed = isInterpretationValidated(
		reportProof?.interpretationStatus ?? campaign?.interpretationStatus
	);
	const hasIncompleteInputs = proofScores.some(hasIncompleteScoreInputs);
	const items: SelectedSeriesScoreMethodReviewItem[] = [
		toScoreMethodOutputsItem(hasCampaign, campaign?.visibleScoreCount ?? 0, proofScores),
		toScoreMethodCoverageItem(workspace),
		toScoreMethodDirectionScaleItem(hasCampaign, hasScoringRule),
		toScoreMethodMissingnessItem(hasCampaign, hasScoringRule, proofScores),
		toScoreMethodInterpretationBoundaryItem(hasCampaign, hasScoringRule, interpretationReviewed)
	];

	return {
		title: copy.scoreMethodReview.title,
		description: copy.scoreMethodReview.description,
		status: !hasCampaign
			? 'not_available'
			: !hasScoringRule
				? 'blocked'
				: interpretationReviewed && proofScores.length > 0 && !hasIncompleteInputs
					? 'ready'
					: 'pending',
		items: items.map((item) => localizeReportsWorkflowItem(item, copy))
	};
}

export function toSelectedSeriesExportPreview(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	artifact: ReportProofExportArtifactResponse | null = null,
	copy: SelectedSeriesReportsWorkflowCopy = defaultSelectedSeriesReportsWorkflowCopy
): SelectedSeriesExportPreview {
	if (!workspace.selectedCampaign) {
		return {
			title: copy.exportPreview.title,
			description: copy.exportPreview.description,
			status: 'not_available',
			downloadLabel: copy.exportPreview.createOrSelectWaveFirst,
			items: toPendingExportPreviewItems(copy.exportPreview.selectWavePendingDetail)
				.map((item) => localizeReportsWorkflowItem(item, copy))
		};
	}

	if (!artifact) {
		return {
			title: copy.exportPreview.title,
			description: copy.exportPreview.description,
			status: 'pending',
			downloadLabel: copy.exportPreview.reviewExportFileFirst,
			items: toPendingExportPreviewItems(copy.exportPreview.reviewFilePendingDetail)
				.map((item) => localizeReportsWorkflowItem(item, copy))
		};
	}

	const codebook = parseExportCodebook(artifact.codebookJson);
	const artifactType = codebook.artifactType ?? artifact.artifactType;
	const responseDataset = artifactType === 'campaign_series_response_csv_codebook';
	const reportSummary = artifactType === 'report_proof_csv_codebook';
	const items: SelectedSeriesExportPreviewItem[] = [
		toExportFilePurposeItem(artifact, responseDataset, reportSummary),
		toExportRowShapeItem(artifact, codebook, responseDataset, reportSummary),
		toExportWaveFieldsItem(codebook, responseDataset, reportSummary),
		toExportTrajectoryKeysItem(codebook, responseDataset, reportSummary),
		toExportVariablesValuesItem(artifact, codebook, responseDataset, reportSummary),
		toExportMissingnessItem(codebook, responseDataset, reportSummary),
		toExportScoreOutputsItem(artifact, codebook, responseDataset, reportSummary)
	];

	return {
		title: copy.exportPreview.title,
		description: copy.exportPreview.description,
		status: responseDataset && artifact.canDownload ? 'ready' : artifact.canDownload ? 'pending' : 'blocked',
		downloadLabel: responseDataset
			? copy.exportPreview.downloadResponseDatasetCsv
			: copy.exportPreview.downloadReportSummaryCsv,
		items: items.map((item) => localizeReportsWorkflowItem(item, copy))
	};
}

export function toSelectedSeriesReportsPath(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportsWorkflowLocalState = {},
	copy: SelectedSeriesReportsWorkflowCopy = defaultSelectedSeriesReportsWorkflowCopy
): SelectedSeriesReportsPath {
	const actions = toSelectedSeriesReportsWorkflowActions(workspace, localState, copy);
	const hasRegistryReportExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'report_proof_csv_codebook'
	);
	const hasExistingResponseExport = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
	);
	const hasDownloadableRegistryExport = workspace.exportArtifacts.some(
		(artifact) => artifact.canDownload
	);
	const hasDownloadableResponseExport = workspace.exportArtifacts.some(
		(artifact) =>
			artifact.artifactType === 'campaign_series_response_csv_codebook' && artifact.canDownload
	);
	const latestExportDownloadable = Boolean(
		workspace.selectedCampaign?.latestExportArtifactId &&
			workspace.selectedCampaign.latestExportArtifactCanDownload
	);
	const hasExistingReportExport =
		hasRegistryReportExport ||
		(Boolean(workspace.selectedCampaign?.latestExportArtifactId) && !hasExistingResponseExport);
	const hasPersistedExport = hasExistingReportExport || hasExistingResponseExport;
	const hasPersistedDownloadableExport = latestExportDownloadable || hasDownloadableRegistryExport;
	const hasPersistedDownloadableResponseExport =
		hasDownloadableResponseExport ||
		(hasExistingResponseExport && latestExportDownloadable);
	const resultsReviewed = Boolean(localState.reportProofViewed || hasPersistedExport);
	const doneByActionId: Record<SelectedSeriesReportsWorkflowActionId, boolean> = {
		reportProof: resultsReviewed,
		exportArtifact: Boolean(
			localState.exportCreated || hasExistingReportExport || hasExistingResponseExport
		),
		responseExport: Boolean(localState.responseExportCreated || hasExistingResponseExport),
		fetchArtifact: Boolean(localState.artifactFetched || hasPersistedDownloadableResponseExport),
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
	hasExistingResponseExport: boolean,
	copy: SelectedSeriesReportsWorkflowCopy
) {
	if (!hasCampaign) {
		return copy.disabled.reviewResultsBeforeCreatingResponseExport;
	}

	if (!reportable) {
		return copy.disabled.resolveReportPrerequisitesBeforeCreatingResponseExport;
	}

	if (responseExportCreated) {
		return copy.disabled.responseExportCreatedThisSession;
	}

	if (hasExistingResponseExport) {
		return copy.disabled.responseExportAlreadyExists;
	}

	return reportProofViewed ? null : copy.disabled.reviewResultsBeforeCreatingResponseExport;
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

function toReportProofDisabledReason(
	hasCampaign: boolean,
	reportable: boolean,
	copy: SelectedSeriesReportsWorkflowCopy
) {
	if (!hasCampaign) {
		return copy.disabled.createOrSelectWaveBeforeReviewingResults;
	}

	return reportable ? null : copy.disabled.resolveReportPrerequisitesBeforeReviewingResults;
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
	exportCreated: boolean,
	hasExistingReportExport: boolean,
	hasResponseExport: boolean,
	copy: SelectedSeriesReportsWorkflowCopy
) {
	if (!hasCampaign) {
		return copy.disabled.reviewResultsBeforeCreatingReportExport;
	}

	if (!reportable) {
		return copy.disabled.resolveReportPrerequisitesBeforeCreatingReportExport;
	}

	if (exportCreated) {
		return copy.disabled.reportExportCreatedThisSession;
	}

	if (hasResponseExport) {
		return copy.disabled.responseDatasetAlreadyExistsReportOptional;
	}

	if (hasExistingReportExport) {
		return copy.disabled.reportSummaryExportAlreadyExists;
	}

	return reportProofViewed ? null : copy.disabled.reviewResultsBeforeCreatingReportExport;
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

function toResponsesPacketItem(
	submittedResponseCount: number | null
): SelectedSeriesResultsPacketReviewItem {
	const submittedResponses = submittedResponseCount ?? 0;

	if (submittedResponses > 0) {
		return {
			id: 'responses',
			label: 'Responses',
			status: 'ready',
			summary: `${submittedResponses} response${submittedResponses === 1 ? '' : 's'} collected`,
			detail:
				'Raw submitted responses exist. They can be exported for internal analysis when an export file is created.'
		};
	}

	return {
		id: 'responses',
		label: 'Responses',
		status: 'blocked',
		summary: 'No responses yet',
		detail: 'Collect at least one response before reviewing or exporting results.'
	};
}

function toScoresPacketItem(
	submittedResponseCount: number | null,
	visibleScoreCount: number | null
): SelectedSeriesResultsPacketReviewItem {
	const submittedResponses = submittedResponseCount ?? 0;
	const visibleScores = visibleScoreCount ?? 0;

	if (visibleScores > 0) {
		return {
			id: 'scores',
			label: 'Scores',
			status: 'ready',
			summary: `${visibleScores} score${visibleScores === 1 ? '' : 's'} visible`,
			detail:
				'Scored results are visible for internal review. Keep score meaning and method notes with any exported analysis.'
		};
	}

	if (submittedResponses > 0) {
		return {
			id: 'scores',
			label: 'Scores',
			status: 'blocked',
			summary: 'No scores visible',
			detail: `${submittedResponses} response${submittedResponses === 1 ? '' : 's'} exist${submittedResponses === 1 ? 's' : ''}, but no scored result is visible. Check scoring setup, missing-answer rules, and disclosure before treating this as scored results.`
		};
	}

	return {
		id: 'scores',
		label: 'Scores',
		status: 'blocked',
		summary: 'No scores yet',
		detail: 'Scores appear after responses can be scored and disclosure allows them to be shown.'
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
			detail: 'Use this CSV and codebook for analysis. Keep method and interpretation notes with the file.'
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
		summary: resultsReady ? 'Create response export' : 'Export blocked',
		detail: resultsReady
			? 'Create the response dataset for analysis, or create the report-summary file for internal review.'
			: 'Results must be ready before export files can be created.'
	};
}

function toUseStatusPacketItem(options: {
	hasCampaign: boolean;
	hasResponses: boolean;
	hasVisibleScores: boolean;
	responseDatasetReady: boolean;
	interpretationReviewed: boolean;
	collectionClosed: boolean;
}): SelectedSeriesResultsPacketReviewItem {
	if (!options.hasCampaign) {
		return {
			id: 'use_status',
			label: 'Use status',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before deciding how results can be used.'
		};
	}

	if (!options.hasResponses) {
		return {
			id: 'use_status',
			label: 'Use status',
			status: 'blocked',
			summary: 'No result data yet',
			detail: 'Collect responses before using or exporting results.'
		};
	}

	if (!options.hasVisibleScores) {
		return {
			id: 'use_status',
			label: 'Use status',
			status: 'blocked',
			summary: 'Raw responses only',
			detail:
				'Do not present scored results yet. Use raw responses internally or fix scoring, missing-answer rules, and disclosure.'
		};
	}

	if (
		options.responseDatasetReady &&
		options.interpretationReviewed &&
		options.collectionClosed
	) {
		return {
			id: 'use_status',
			label: 'Use status',
			status: 'ready',
			summary: 'Ready for controlled sharing',
			detail:
				'Response dataset, visible scores, reviewed interpretation, and closed collection are in place. Keep disclosure and study-method notes with anything shared.'
		};
	}

	return {
		id: 'use_status',
		label: 'Use status',
		status: 'pending',
		summary: 'Internal review only',
		detail:
			'Use these results inside the workspace while export, interpretation, or collection status still needs review.'
	};
}

function toScoreMethodOutputsItem(
	hasCampaign: boolean,
	visibleScoreCount: number,
	proofScores: ReportScoreSummaryResponse[]
): SelectedSeriesScoreMethodReviewItem {
	if (!hasCampaign) {
		return {
			id: 'outputs',
			label: 'Score outputs',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before reviewing score outputs.'
		};
	}

	if (proofScores.length > 0) {
		return {
			id: 'outputs',
			label: 'Score outputs',
			status: 'ready',
			summary: `${proofScores.length} ${pluralize(proofScores.length, 'score output', 'score outputs')}: ${proofScores.map((score) => score.dimensionCode).join(', ')}`,
			detail:
				'These are the score output codes returned by the selected wave result preview. Treat them as tenant-defined custom outputs unless a separate canonical approval exists.'
		};
	}

	return {
		id: 'outputs',
		label: 'Score outputs',
		status: visibleScoreCount > 0 ? 'pending' : 'blocked',
		summary:
			visibleScoreCount > 0
				? 'Output names available after reviewing results'
				: 'No visible score outputs yet',
		detail:
			'Use Review results to load the score output rows. Results does not infer score meaning from hidden setup data.'
	};
}

function toScoreMethodCoverageItem(
	workspace: CampaignSeriesReportsWorkspaceResponse
): SelectedSeriesScoreMethodReviewItem {
	const campaign = workspace.selectedCampaign;

	if (!campaign) {
		return {
			id: 'coverage',
			label: 'Response coverage',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before reviewing response coverage.'
		};
	}

	const coverage = workspace.scoreCoverage;

	if (coverage) {
		const status: ProductReadModelBadgeStatus =
			coverage.unscoredSubmittedResponseCount > 0 ||
			coverage.notConfiguredSubmittedResponseCount > 0
				? 'pending'
				: coverage.scoredSubmittedResponseCount > 0
					? 'ready'
					: 'blocked';

		return {
			id: 'coverage',
			label: 'Response coverage',
			status,
			summary: `${coverage.scoredSubmittedResponseCount} of ${coverage.submittedResponseCount} submitted responses scored`,
			detail: coverage.guidance
		};
	}

	return {
		id: 'coverage',
		label: 'Response coverage',
		status: campaign.visibleScoreCount > 0 ? 'ready' : 'blocked',
		summary: `${campaign.submittedResponseCount} submitted ${pluralize(
			campaign.submittedResponseCount,
			'response',
			'responses'
		)}, ${campaign.visibleScoreCount} visible score ${pluralize(
			campaign.visibleScoreCount,
			'row',
			'rows'
		)}`,
		detail:
			campaign.suppressedScoreCount > 0
				? `${campaign.suppressedScoreCount} score ${pluralize(
						campaign.suppressedScoreCount,
						'row is',
						'rows are'
					)} suppressed by disclosure or scoring rules.`
				: 'Visible score rows are available for internal review.'
	};
}

function toScoreMethodDirectionScaleItem(
	hasCampaign: boolean,
	hasScoringRule: boolean
): SelectedSeriesScoreMethodReviewItem {
	if (!hasCampaign) {
		return {
			id: 'direction_scale',
			label: 'Direction and scale',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before reviewing score direction and scale family.'
		};
	}

	if (!hasScoringRule) {
		return {
			id: 'direction_scale',
			label: 'Direction and scale',
			status: 'blocked',
			summary: 'No scoring rule selected',
			detail: 'Create or attach a scoring rule before score direction can be reviewed.'
		};
	}

	return {
		id: 'direction_scale',
		label: 'Direction and scale',
		status: 'pending',
		summary: 'Direction and scale family need setup context',
		detail:
			'Results can show output rows and missingness, but question-to-output coverage, reverse scoring, and answer scale family still need the Setup scoring plan. Do not infer better/worse meaning from output codes alone.'
	};
}

function toScoreMethodMissingnessItem(
	hasCampaign: boolean,
	hasScoringRule: boolean,
	proofScores: ReportScoreSummaryResponse[]
): SelectedSeriesScoreMethodReviewItem {
	if (!hasCampaign) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before reviewing missing-answer handling.'
		};
	}

	if (!hasScoringRule) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'blocked',
			summary: 'No scoring rule selected',
			detail: 'Missing-answer handling applies after a scoring rule exists.'
		};
	}

	if (proofScores.length === 0) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'pending',
			summary: 'Missing-answer metadata available after reviewing results',
			detail: 'Use Review results to load valid/expected answer contribution counts where available.'
		};
	}

	const incomplete = proofScores.filter(hasIncompleteScoreInputs);

	if (incomplete.length === 0) {
		return {
			id: 'missingness',
			label: 'Missing answers',
			status: 'ready',
			summary: 'No missing-score input gap in preview',
			detail:
				'The visible score outputs reported complete valid/expected answer contribution counts.'
		};
	}

	return {
		id: 'missingness',
		label: 'Missing answers',
		status: 'pending',
		summary: 'Some score inputs were incomplete',
		detail: incomplete.map(toIncompleteScoreInputSummary).join('; ')
	};
}

function toScoreMethodInterpretationBoundaryItem(
	hasCampaign: boolean,
	hasScoringRule: boolean,
	interpretationReviewed: boolean
): SelectedSeriesScoreMethodReviewItem {
	if (!hasCampaign) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'not_available',
			summary: 'No wave selected',
			detail: 'Select a wave before reviewing interpretation boundaries.'
		};
	}

	if (!hasScoringRule) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'blocked',
			summary: 'No score interpretation yet',
			detail: 'Add scoring before writing result interpretation.'
		};
	}

	if (interpretationReviewed) {
		return {
			id: 'interpretation_boundary',
			label: 'Interpretation boundary',
			status: 'ready',
			summary: 'Interpretation reviewed for this workspace',
			detail:
				'Keep the study method notes and disclosure limits with any export or shared result packet.'
		};
	}

	return {
		id: 'interpretation_boundary',
		label: 'Interpretation boundary',
		status: 'pending',
		summary: 'Custom-study interpretation, not externally validated',
		detail:
			'Use these scores as tenant-defined custom-study calculations. Do not present them as official norms, benchmarks, clinical thresholds, or externally validated claims.'
	};
}

function toPendingExportPreviewItems(detail: string): SelectedSeriesExportPreviewItem[] {
	return [
		{
			id: 'file_purpose',
			label: 'File purpose',
			status: 'pending',
			summary: 'Review export file to inspect contents',
			detail
		},
		{
			id: 'row_shape',
			label: 'Row shape',
			status: 'pending',
			summary: 'Row shape available after file review',
			detail
		},
		{
			id: 'wave_fields',
			label: 'Wave fields',
			status: 'pending',
			summary: 'Wave fields available after file review',
			detail
		},
		{
			id: 'trajectory_keys',
			label: 'Trajectory keys',
			status: 'pending',
			summary: 'Trajectory key policy available after file review',
			detail
		},
		{
			id: 'variables_values',
			label: 'Variables and values',
			status: 'pending',
			summary: 'Variables and values available after file review',
			detail
		},
		{
			id: 'missingness',
			label: 'Missingness',
			status: 'pending',
			summary: 'Missingness policy available after file review',
			detail
		},
		{
			id: 'score_outputs',
			label: 'Score outputs',
			status: 'pending',
			summary: 'Score outputs available after file review',
			detail
		}
	];
}

function toExportFilePurposeItem(
	artifact: ReportProofExportArtifactResponse,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	if (responseDataset) {
		return {
			id: 'file_purpose',
			label: 'File purpose',
			status: 'ready',
			summary: 'Response dataset CSV and codebook',
			detail:
				'Use this for row-level analysis. Keep method, disclosure, and interpretation notes with the file.'
		};
	}

	if (reportSummary) {
		return {
			id: 'file_purpose',
			label: 'File purpose',
			status: 'pending',
			summary: 'Report-summary CSV, not row-level response data',
			detail:
				'Use this for internal aggregate review. Create a response dataset when you need submitted-response rows.'
		};
	}

	return {
		id: 'file_purpose',
		label: 'File purpose',
		status: 'pending',
		summary: artifact.artifactType.replaceAll('_', ' '),
		detail: 'Review this file type before using it outside the workspace.'
	};
}

function toExportRowShapeItem(
	artifact: ReportProofExportArtifactResponse,
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	const rowCount = codebook.rowCount ?? artifact.rowCount;

	if (responseDataset) {
		return {
			id: 'row_shape',
			label: 'Row shape',
			status: 'ready',
			summary: `${rowCount} ${pluralize(rowCount, 'row', 'rows')}; one row per submitted response`,
			detail:
				'Each row represents one submitted response session in this study export.'
		};
	}

	if (reportSummary) {
		return {
			id: 'row_shape',
			label: 'Row shape',
			status: 'ready',
			summary: `${rowCount} ${pluralize(
				rowCount,
				'row',
				'rows'
			)}; one row per visible or suppressed score output`,
			detail:
				'Each row summarizes one score output for the selected wave, not one respondent.'
		};
	}

	return {
		id: 'row_shape',
		label: 'Row shape',
		status: 'pending',
		summary: `${rowCount} ${pluralize(rowCount, 'row', 'rows')}`,
		detail: 'Review the codebook before interpreting row meaning.'
	};
}

function toExportWaveFieldsItem(
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	const hasWaveFields = codebook.columns.some((column) =>
		['wave_label', 'campaign_id', 'campaign_status', 'campaign_data_finality'].includes(column.name)
	);

	if (responseDataset) {
		const campaignCount = codebook.campaignCount ?? 0;
		return {
			id: 'wave_fields',
			label: 'Wave fields',
			status: hasWaveFields ? 'ready' : 'pending',
			summary:
				campaignCount > 0
					? `Wave fields included for ${campaignCount} ${pluralize(campaignCount, 'wave', 'waves')}`
					: 'Wave fields included',
			detail:
				'Use wave and campaign fields to separate baseline, follow-up, live, and closed-wave rows.'
		};
	}

	if (reportSummary) {
		return {
			id: 'wave_fields',
			label: 'Wave fields',
			status: 'ready',
			summary: 'Selected-wave lifecycle fields included',
			detail:
				'The report-summary file describes the selected wave and its finality, not every wave in the study.'
		};
	}

	return {
		id: 'wave_fields',
		label: 'Wave fields',
		status: hasWaveFields ? 'ready' : 'pending',
		summary: hasWaveFields ? 'Wave fields included' : 'Wave fields not detected',
		detail: 'Review the codebook column list before using wave-level grouping.'
	};
}

function toExportTrajectoryKeysItem(
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	if (reportSummary) {
		return {
			id: 'trajectory_keys',
			label: 'Trajectory keys',
			status: 'not_available',
			summary: 'No trajectory keys in report-summary export',
			detail:
				'Report-summary exports are aggregate files and do not include respondent trajectory rows.'
		};
	}

	const hasTrajectory = codebook.columns.some((column) => column.name === 'trajectory_id');

	if (responseDataset && hasTrajectory) {
		return {
			id: 'trajectory_keys',
			label: 'Trajectory keys',
			status: 'ready',
			summary: 'Artifact-local trajectory keys included',
			detail: `Trajectory ids are ${codebook.trajectoryIdPolicy ?? 'artifact-local'} and should not be treated as raw participant codes or reusable identifiers.`
		};
	}

	return {
		id: 'trajectory_keys',
		label: 'Trajectory keys',
		status: responseDataset ? 'pending' : 'not_available',
		summary: responseDataset ? 'No trajectory key column detected' : 'No trajectory keys',
		detail:
			'Use trajectory fields only when anonymous longitudinal linking is present and disclosure allows it.'
	};
}

function toExportVariablesValuesItem(
	artifact: ReportProofExportArtifactResponse,
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	const columns = columnsOrCsvHeaders(codebook, artifact);
	const answerCount = codebook.columns.filter((column) => column.source === 'answer').length;
	const scoreMetadataCount = codebook.columns.filter(
		(column) => column.source === 'score_output_metadata'
	).length;
	const answerMetadataCount = codebook.columns.filter(
		(column) => column.hasValueLabels || column.hasAnswerMetadata
	).length;
	const answerMetadataSummary =
		answerMetadataCount > 0
			? `, ${answerMetadataCount} answer metadata ${pluralize(answerMetadataCount, 'field', 'fields')}`
			: '';

	if (responseDataset) {
		return {
			id: 'variables_values',
			label: 'Variables and values',
			status: 'ready',
			summary: `${answerCount} answer ${pluralize(
				answerCount,
				'variable',
				'variables'
			)}, ${scoreMetadataCount} score metadata ${pluralize(
				scoreMetadataCount,
				'field',
				'fields'
			)}${answerMetadataSummary}, ${columns.length} columns total`,
			detail:
				'Question columns include codebook metadata such as question type, missing codes, scale anchors, value labels and answer constraints when available.'
		};
	}

	if (reportSummary) {
		return {
			id: 'variables_values',
			label: 'Variables and values',
			status: 'ready',
			summary: `${columns.length} report-summary columns`,
			detail:
				'Columns describe aggregate score output, disclosure, finality, score metadata, and tenant-defined interpretation fields.'
		};
	}

	return {
		id: 'variables_values',
		label: 'Variables and values',
		status: columns.length > 0 ? 'ready' : 'pending',
		summary: `${columns.length} columns detected`,
		detail: 'Review the codebook before using column values.'
	};
}

function toExportMissingnessItem(
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	const hasMissingColumns = codebook.columns.some((column) =>
		column.name.includes('missing') || column.metadataKind?.includes('missing')
	);
	const hasQuestionMissingCodes = codebook.columns.some((column) => column.hasMissingCodes);

	if (responseDataset) {
		return {
			id: 'missingness',
			label: 'Missingness',
			status: codebook.hasMissingTreatment || hasQuestionMissingCodes ? 'ready' : 'pending',
			summary:
				codebook.hasMissingTreatment || hasQuestionMissingCodes
					? 'Missing-answer codes documented'
					: 'Missing-answer codes not detected',
			detail:
				'Use the codebook missing-treatment fields and question missing codes before treating blanks as real answers.'
		};
	}

	if (reportSummary) {
		return {
			id: 'missingness',
			label: 'Missingness',
			status: hasMissingColumns ? 'ready' : 'pending',
			summary: hasMissingColumns
				? 'Score missingness fields included'
				: 'Score missingness fields not detected',
			detail:
				'Report-summary missingness describes valid/expected score contributions, not respondent-level skipped answers.'
		};
	}

	return {
		id: 'missingness',
		label: 'Missingness',
		status: hasMissingColumns ? 'ready' : 'pending',
		summary: hasMissingColumns ? 'Missingness fields included' : 'Missingness fields not detected',
		detail: 'Review missingness fields before analysis.'
	};
}

function toExportScoreOutputsItem(
	artifact: ReportProofExportArtifactResponse,
	codebook: ExportCodebookSummary,
	responseDataset: boolean,
	reportSummary: boolean
): SelectedSeriesExportPreviewItem {
	const dimensionCodes = uniqueStrings(
		codebook.columns
			.map((column) => column.dimensionCode)
			.filter((value): value is string => Boolean(value))
	);

	if (responseDataset) {
		return {
			id: 'score_outputs',
			label: 'Score outputs',
			status: dimensionCodes.length > 0 ? 'ready' : 'pending',
			summary:
				dimensionCodes.length > 0
					? `Score metadata for ${dimensionCodes.join(', ')}`
					: 'No score metadata columns detected',
			detail:
				'Response datasets include score metadata fields when scores exist; keep score method notes with analysis.'
		};
	}

	if (reportSummary) {
		const csvHeaders = csvHeadersFromContent(artifact.csvContent);
		const hasDimensionCode = codebook.columns.some((column) => column.name === 'dimension_code') || csvHeaders.includes('dimension_code');
		return {
			id: 'score_outputs',
			label: 'Score outputs',
			status: hasDimensionCode ? 'ready' : 'pending',
			summary: hasDimensionCode
				? 'Score outputs listed in dimension_code'
				: 'Score output column not detected',
			detail:
				'Use dimension_code with score_count and disclosure fields to understand aggregate score rows.'
		};
	}

	return {
		id: 'score_outputs',
		label: 'Score outputs',
		status: dimensionCodes.length > 0 ? 'ready' : 'pending',
		summary:
			dimensionCodes.length > 0
				? `Score metadata for ${dimensionCodes.join(', ')}`
				: 'Score outputs not detected',
		detail: 'Review score output fields before interpretation.'
	};
}

const reportsWorkflowMessageIds: Record<string, AppMessageId> = {
	Responses: 'results.packet.responses.label',
	Scores: 'results.packet.scores.label',
	'Export files': 'results.packet.exportFiles.label',
	'Use status': 'results.packet.useStatus.label',
	'Raw submitted responses exist. They can be exported for internal analysis when an export file is created.':
		'results.packet.rawResponses.detail',
	'Scored results are visible for internal review. Keep score meaning and method notes with any exported analysis.':
		'results.packet.scoredResults.detail',
	'Response dataset ready': 'results.packet.responseDatasetReady.summary',
	'Use this CSV and codebook for analysis. Keep method and interpretation notes with the file.':
		'results.packet.responseDatasetReady.detail',
	'Internal review only': 'results.packet.internalReviewOnly.summary',
	'Use these results inside the workspace while export, interpretation, or collection status still needs review.':
		'results.packet.internalReviewOnly.detail',
	'No wave selected': 'results.packet.noWaveSelected.summary',
	'Select a wave before deciding how results can be used.': 'results.packet.noWaveSelected.detail',
	'No result data yet': 'results.packet.noResultData.summary',
	'Collect responses before using or exporting results.': 'results.packet.noResultData.detail',
	'Raw responses only': 'results.packet.rawResponsesOnly.summary',
	'Do not present scored results yet. Use raw responses internally or fix scoring, missing-answer rules, and disclosure.':
		'results.packet.rawResponsesOnly.detail',
	'Ready for controlled sharing': 'results.packet.controlledSharing.summary',
	'Response dataset, visible scores, reviewed interpretation, and closed collection are in place. Keep disclosure and study-method notes with anything shared.':
		'results.packet.controlledSharing.detail',

	'Score outputs': 'results.method.outputs.label',
	'Response coverage': 'results.method.coverage.label',
	'Direction and scale': 'results.method.directionScale.label',
	'Missing answers': 'results.method.missingAnswers.label',
	'Interpretation boundary': 'results.method.interpretationBoundary.label',
	'Output names available after reviewing results': 'results.method.outputs.pending.summary',
	'Use Review results to load the score output rows. Results does not infer score meaning from hidden setup data.':
		'results.method.outputs.pending.detail',
	'All submitted responses have successful scoring activity.':
		'results.method.coverage.allScored.detail',
	'Direction and scale family need setup context': 'results.method.directionScale.pending.summary',
	'Results can show output rows and missingness, but question-to-output coverage, reverse scoring, and answer scale family still need the Setup scoring plan. Do not infer better/worse meaning from output codes alone.':
		'results.method.directionScale.pending.detail',
	'Missing-answer metadata available after reviewing results':
		'results.method.missingAnswers.pending.summary',
	'Use Review results to load valid/expected answer contribution counts where available.':
		'results.method.missingAnswers.pending.detail',
	'Some score inputs were incomplete': 'results.method.missingAnswers.incomplete.summary',
	'Custom-study interpretation, not externally validated':
		'results.method.interpretation.custom.summary',
	'Use these scores as tenant-defined custom-study calculations. Do not present them as official norms, benchmarks, clinical thresholds, or externally validated claims.':
		'results.method.interpretation.custom.detail',

	'File purpose': 'results.export.filePurpose.label',
	'Row shape': 'results.export.rowShape.label',
	'Wave fields': 'results.export.waveFields.label',
	'Trajectory keys': 'results.export.trajectoryKeys.label',
	'Variables and values': 'results.export.variablesValues.label',
	Missingness: 'results.export.missingness.label',
	'Review export file to inspect contents': 'results.export.pending.filePurpose.summary',
	'Row shape available after file review': 'results.export.pending.rowShape.summary',
	'Wave fields available after file review': 'results.export.pending.waveFields.summary',
	'Trajectory key policy available after file review':
		'results.export.pending.trajectoryKeys.summary',
	'Variables and values available after file review':
		'results.export.pending.variablesValues.summary',
	'Missingness policy available after file review': 'results.export.pending.missingness.summary',
	'Score outputs available after file review': 'results.export.pending.scoreOutputs.summary',
	'Response dataset CSV and codebook': 'results.export.responseDataset.summary',
	'Use this for row-level analysis. Keep method, disclosure, and interpretation notes with the file.':
		'results.export.responseDataset.detail',
	'Report-summary CSV, not row-level response data': 'results.export.reportSummary.summary',
	'Use this for internal aggregate review. Create a response dataset when you need submitted-response rows.':
		'results.export.reportSummary.detail',
	'Review this file type before using it outside the workspace.': 'results.export.unknownFile.detail',
	'Each row represents one submitted response session in this study export.':
		'results.export.rowShape.responseRows.detail',
	'Each row summarizes one score output for the selected wave, not one respondent.':
		'results.export.rowShape.scoreRows.detail',
	'Review the codebook before interpreting row meaning.':
		'results.export.rowShape.generic.detail',
	'Wave fields included': 'results.export.waveFields.included.summary',
	'Selected-wave lifecycle fields included': 'results.export.waveFields.reportSummary.summary',
	'The report-summary file describes the selected wave and its finality, not every wave in the study.':
		'results.export.waveFields.reportSummary.detail',
	'Wave fields not detected': 'results.export.waveFields.notDetected.summary',
	'Review the codebook column list before using wave-level grouping.':
		'results.export.waveFields.grouping.detail',
	'No trajectory keys in report-summary export':
		'results.export.trajectoryKeys.reportSummary.summary',
	'Report-summary exports are aggregate files and do not include respondent trajectory rows.':
		'results.export.trajectoryKeys.reportSummary.detail',
	'Artifact-local trajectory keys included': 'results.export.trajectoryKeys.included.summary',
	'No trajectory key column detected': 'results.export.trajectoryKeys.noColumn.summary',
	'No trajectory keys': 'results.export.trajectoryKeys.none.summary',
	'Use trajectory fields only when anonymous longitudinal linking is present and disclosure allows it.':
		'results.export.trajectoryKeys.usage.detail',
	'Question columns include codebook metadata such as question type, missing codes, scale anchors, value labels and answer constraints when available.':
		'results.export.variables.responseDataset.detail',
	'Columns describe aggregate score output, disclosure, finality, score metadata, and tenant-defined interpretation fields.':
		'results.export.variables.reportSummary.detail',
	'Review the codebook before using column values.': 'results.export.variables.codebook.detail',
	'Missing-answer codes documented': 'results.export.missingness.codesDocumented.summary',
	'Missing-answer codes not detected': 'results.export.missingness.codesNotDetected.summary',
	'Use the codebook missing-treatment fields and question missing codes before treating blanks as real answers.':
		'results.export.missingness.responseDataset.detail',
	'Score missingness fields included': 'results.export.missingness.scoreFields.summary',
	'Score missingness fields not detected':
		'results.export.missingness.scoreFieldsNotDetected.summary',
	'Report-summary missingness describes valid/expected score contributions, not respondent-level skipped answers.':
		'results.export.missingness.reportSummary.detail',
	'Missingness fields included': 'results.export.missingness.fields.summary',
	'Missingness fields not detected': 'results.export.missingness.fieldsNotDetected.summary',
	'Review missingness fields before analysis.': 'results.export.missingness.review.detail',
	'No score metadata columns detected': 'results.export.scoreOutputs.noMetadata.summary',
	'Response datasets include score metadata fields when scores exist; keep score method notes with analysis.':
		'results.export.scoreOutputs.responseDataset.detail',
	'Score outputs listed in dimension_code': 'results.export.scoreOutputs.dimensionCode.summary',
	'Score output column not detected': 'results.export.scoreOutputs.noColumn.summary',
	'Use dimension_code with score_count and disclosure fields to understand aggregate score rows.':
		'results.export.scoreOutputs.dimensionCode.detail',
	'Score outputs not detected': 'results.export.scoreOutputs.notDetected.summary',
	'Review score output fields before interpretation.': 'results.export.scoreOutputs.review.detail'
};

function localizeReportsWorkflowItem<
	TItem extends { label: string; summary: string; detail: string }
>(item: TItem, copy: SelectedSeriesReportsWorkflowCopy): TItem {
	return {
		...item,
		label: localizeReportsWorkflowText(item.label, copy),
		summary: localizeReportsWorkflowText(item.summary, copy),
		detail: localizeReportsWorkflowText(item.detail, copy)
	};
}

function localizeReportsWorkflowText(value: string, copy: SelectedSeriesReportsWorkflowCopy) {
	const locale = reportsWorkflowLocale(copy);
	const messageId = reportsWorkflowMessageIds[value];
	if (messageId) {
		return appMessage(locale, messageId);
	}

	if (locale !== 'hr-HR') {
		return value;
	}

	const responseCollectedMatch = value.match(/^(\d+) responses? collected$/);
	if (responseCollectedMatch) {
		return appMessage(locale, 'results.packet.responses.collected', {
			count: Number(responseCollectedMatch[1])
		});
	}

	const scoreVisibleMatch = value.match(/^(\d+) scores? visible$/);
	if (scoreVisibleMatch) {
		return appMessage(locale, 'results.packet.scores.visible', {
			count: Number(scoreVisibleMatch[1])
		});
	}

	const coverageMatch = value.match(/^(\d+) of (\d+) submitted responses scored$/);
	if (coverageMatch) {
		return appMessage(locale, 'results.method.coverage.scored', {
			scored: Number(coverageMatch[1]),
			total: Number(coverageMatch[2])
		});
	}

	const submittedVisibleMatch = value.match(/^(\d+) submitted responses?, (\d+) visible score rows?$/);
	if (submittedVisibleMatch) {
		return appMessage(locale, 'results.method.coverage.submittedVisible', {
			submitted: Number(submittedVisibleMatch[1]),
			visible: Number(submittedVisibleMatch[2])
		});
	}

	const rowShapeResponseMatch = value.match(/^(\d+) rows?; one row per submitted response$/);
	if (rowShapeResponseMatch) {
		return appMessage(locale, 'results.export.rowShape.responseRows', {
			count: Number(rowShapeResponseMatch[1])
		});
	}

	const rowShapeScoreMatch = value.match(
		/^(\d+) rows?; one row per visible or suppressed score output$/
	);
	if (rowShapeScoreMatch) {
		return appMessage(locale, 'results.export.rowShape.scoreRows', {
			count: Number(rowShapeScoreMatch[1])
		});
	}

	const rowCountMatch = value.match(/^(\d+) rows?$/);
	if (rowCountMatch) {
		return appMessage(locale, 'results.export.rowShape.rows', {
			count: Number(rowCountMatch[1])
		});
	}

	const waveFieldsMatch = value.match(/^Wave fields included for (\d+) waves?$/);
	if (waveFieldsMatch) {
		return appMessage(locale, 'results.export.waveFields.includedForWaves', {
			count: Number(waveFieldsMatch[1])
		});
	}

	const answerVariableMatch = value.match(
		/^(\d+) answer variables?, (\d+) score metadata fields?(?:, (\d+) answer metadata fields?)?, (\d+) columns total$/
	);
	if (answerVariableMatch) {
		return appMessage(locale, 'results.export.variables.responseDatasetSummary', {
			answerCount: Number(answerVariableMatch[1]),
			scoreMetadataCount: Number(answerVariableMatch[2]),
			answerMetadataCount: answerVariableMatch[3] ? Number(answerVariableMatch[3]) : 0,
			columnCount: Number(answerVariableMatch[4])
		});
	}

	const reportSummaryColumnsMatch = value.match(/^(\d+) report-summary columns$/);
	if (reportSummaryColumnsMatch) {
		return appMessage(locale, 'results.export.variables.reportSummaryColumns', {
			count: Number(reportSummaryColumnsMatch[1])
		});
	}

	const columnsDetectedMatch = value.match(/^(\d+) columns detected$/);
	if (columnsDetectedMatch) {
		return appMessage(locale, 'results.export.variables.columnsDetected', {
			count: Number(columnsDetectedMatch[1])
		});
	}

	const scoreMetadataMatch = value.match(/^Score metadata for (.+)$/);
	if (scoreMetadataMatch) {
		return appMessage(locale, 'results.export.scoreOutputs.metadataFor', {
			dimensions: scoreMetadataMatch[1]
		});
	}

	const scoreOutputsMatch = value.match(/^(\d+) score outputs?: (.+)$/);
	if (scoreOutputsMatch) {
		return appMessage(locale, 'results.method.outputs.countList', {
			count: Number(scoreOutputsMatch[1]),
			outputs: scoreOutputsMatch[2]
		});
	}

	const trajectoryPolicyMatch = value.match(
		/^Trajectory ids are (.+) and should not be treated as raw participant codes or reusable identifiers\.$/
	);
	if (trajectoryPolicyMatch) {
		return appMessage(locale, 'results.export.trajectoryKeys.policy.detail', {
			policy: trajectoryPolicyMatch[1]
		});
	}

	const incompleteScoreInputMatch = value.match(
		/^(.+) used (\d+) of (\d+) expected answer contributions$/
	);
	if (incompleteScoreInputMatch) {
		return appMessage(locale, 'results.method.missingAnswers.incomplete.detail', {
			dimension: incompleteScoreInputMatch[1],
			valid: Number(incompleteScoreInputMatch[2]),
			expected: Number(incompleteScoreInputMatch[3])
		});
	}

	return value;
}

function reportsWorkflowLocale(copy: SelectedSeriesReportsWorkflowCopy): AppLocale {
	return copy.locale === 'hr-HR' ? 'hr-HR' : 'en';
}

function parseExportCodebook(value: string | null | undefined): ExportCodebookSummary {
	if (!value) {
		return emptyExportCodebookSummary();
	}

	try {
		const parsed = JSON.parse(value) as unknown;
		if (!isRecord(parsed)) {
			return emptyExportCodebookSummary();
		}

		const columnsValue = parsed.columns;
		const columns = Array.isArray(columnsValue)
			? columnsValue.filter(isRecord).map(toExportCodebookColumn)
			: [];

		return {
			artifactType: stringValue(parsed.artifactType),
			rowCount: numberValue(parsed.rowCount),
			campaignCount: numberValue(parsed.campaignCount),
			trajectoryCount: numberValue(parsed.trajectoryCount),
			trajectoryIdPolicy: stringValue(parsed.trajectoryIdPolicy),
			hasMissingTreatment: isRecord(parsed.missingTreatment),
			columns
		};
	} catch {
		return emptyExportCodebookSummary();
	}
}

function emptyExportCodebookSummary(): ExportCodebookSummary {
	return {
		artifactType: null,
		rowCount: null,
		campaignCount: null,
		trajectoryCount: null,
		trajectoryIdPolicy: null,
		hasMissingTreatment: false,
		columns: []
	};
}

function toExportCodebookColumn(value: Record<string, unknown>): ExportCodebookColumn {
	return {
		name: stringValue(value.name) ?? '',
		source: stringValue(value.source),
		questionCode: stringValue(value.questionCode),
		dimensionCode: stringValue(value.dimensionCode),
		metadataKind: stringValue(value.metadataKind),
		hasMissingCodes: isRecord(value.missingCodes),
		hasScale: isRecord(value.scale),
		hasValueLabels: isRecord(value.valueLabels),
		hasAnswerMetadata: isRecord(value.answerMetadata)
	};
}

function columnsOrCsvHeaders(
	codebook: ExportCodebookSummary,
	artifact: ReportProofExportArtifactResponse
) {
	if (codebook.columns.length > 0) {
		return codebook.columns.map((column) => column.name).filter(Boolean);
	}

	return csvHeadersFromContent(artifact.csvContent);
}

function csvHeadersFromContent(content: string | null | undefined) {
	return (content ?? '').trim().split(/\r?\n/)[0]?.split(',').map((header) => header.trim()).filter(Boolean) ?? [];
}

function isRecord(value: unknown): value is Record<string, unknown> {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function stringValue(value: unknown) {
	return typeof value === 'string' ? value : null;
}

function numberValue(value: unknown) {
	return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function uniqueStrings(values: string[]) {
	return [...new Set(values)];
}

function hasIncompleteScoreInputs(score: ReportScoreSummaryResponse) {
	return (
		typeof score.nValidTotal === 'number' &&
		typeof score.nExpectedTotal === 'number' &&
		score.nValidTotal < score.nExpectedTotal
	);
}

function toIncompleteScoreInputSummary(score: ReportScoreSummaryResponse) {
	return `${score.dimensionCode} used ${score.nValidTotal ?? 0} of ${
		score.nExpectedTotal ?? 0
	} expected answer contributions`;
}

function pluralize(value: number, singular: string, plural: string) {
	return value === 1 ? singular : plural;
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

function toResultsPacketPrimaryAction(options: {
	hasCampaign: boolean;
	hasResponses: boolean;
	hasVisibleScores: boolean;
	responseDatasetReady: boolean;
	controlledSharingReady: boolean;
	interpretationReviewed: boolean;
	collectionClosed: boolean;
}, copy: SelectedSeriesReportsWorkflowCopy) {
	if (!options.hasCampaign) {
		return copy.packetReview.primaryAction.noCampaign;
	}

	if (!options.hasResponses) {
		return copy.packetReview.primaryAction.noResponses;
	}

	if (!options.hasVisibleScores) {
		return copy.packetReview.primaryAction.noVisibleScores;
	}

	if (!options.responseDatasetReady) {
		return copy.packetReview.primaryAction.createExport;
	}

	if (options.controlledSharingReady) {
		return copy.packetReview.primaryAction.downloadDataset;
	}

	if (!options.interpretationReviewed) {
		return copy.packetReview.primaryAction.documentInterpretation;
	}

	if (!options.collectionClosed) {
		return copy.packetReview.primaryAction.preliminary;
	}

	return copy.packetReview.primaryAction.downloadDataset;
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
