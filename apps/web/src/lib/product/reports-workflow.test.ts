import { describe, expect, it } from 'vitest';
import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
import type { CampaignReportProofResponse, ReportProofExportArtifactResponse } from '$lib/api/setup';
import {
	toSelectedSeriesExportPreview,
	toSelectedSeriesScoreMethodReview,
	toSelectedSeriesResultsPacketReview,
	toSelectedSeriesResultsHandoffStatus,
	toSelectedSeriesReportsPath,
	toSelectedSeriesReportsWorkflowActions,
	type SelectedSeriesReportsWorkflowCopy
} from './reports-workflow';
import { routePageCopy } from '../i18n/route-copy';

describe('selected-series reports workflow model', () => {
	it('blocks reports actions when no campaign is selected', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'reportProof',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select a wave before reviewing results.'
			}),
			expect.objectContaining({
				id: 'exportArtifact',
				status: 'not_available',
				available: false,
				disabledReason: 'Review results before creating a report export.'
			}),
			expect.objectContaining({
				id: 'responseExport',
				status: 'not_available',
				available: false,
				disabledReason: 'Review results before creating a response export.'
			}),
			expect.objectContaining({
				id: 'fetchArtifact',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select an export file before reviewing it.'
			}),
			expect.objectContaining({
				id: 'downloadCsv',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select an export file before downloading CSV.'
			})
		]);
	});

	it('blocks report preview for a non-reportable selected campaign', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(blockedWorkspace);

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Resolve report prerequisites before reviewing results.'
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'blocked',
			available: false
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Resolve report prerequisites before creating a response export.'
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
			disabledReason: 'Review results before creating a report export.'
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Review results before creating a response export.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'blocked',
			available: false
		});
	});

	it('uses existing export artifact state to allow stored fetch and CSV download', () => {
		const actions = toSelectedSeriesReportsWorkflowActions(reportableWorkspaceWithExport);

		expect(actions.find((action) => action.id === 'reportProof')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Report-summary export already exists for this study.'
		});
		expect(actions.find((action) => action.id === 'fetchArtifact')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
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
			disabledReason: 'Select a downloadable export file before downloading CSV.'
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
			disabledReason: 'Report export was created in this session.'
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Response export was created in this session.'
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

		expect(actions.find((action) => action.id === 'exportArtifact')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Response dataset already exists; report-summary export is optional.'
		});
		expect(actions.find((action) => action.id === 'responseExport')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Response export already exists for this study.'
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

	it('selects response export before download when only a report-summary export exists', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspaceWithExport);

		expect(path.currentActionId).toBe('responseExport');
		expect(path.steps.find((step) => step.id === 'exportArtifact')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'responseExport')).toMatchObject({
			pathState: 'current'
		});
		expect(path.steps.find((step) => step.id === 'downloadCsv')).toMatchObject({
			pathState: 'blocked'
		});
	});

	it('skips report-summary export as optional when a response dataset already exists', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspaceWithResponseExport);

		expect(path.currentActionId).toBe('downloadCsv');
		expect(path.steps.find((step) => step.id === 'reportProof')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'exportArtifact')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'responseExport')).toMatchObject({
			pathState: 'done'
		});
		expect(path.currentAction).toMatchObject({
			id: 'downloadCsv',
			title: 'Download response dataset CSV',
			available: true
		});
	});

	it('selects report preview as the current reports task for a reportable campaign', () => {
		const path = toSelectedSeriesReportsPath(reportableWorkspace);

		expect(path.currentActionId).toBe('reportProof');
		expect(path.currentAction.title).toBe('Review results');
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
		expect(path.currentAction.title).toBe('Download response dataset CSV');
		expect(path.completedCount).toBe(4);
		expect(path.steps.find((step) => step.id === 'fetchArtifact')).toMatchObject({
			pathState: 'done'
		});
		expect(path.steps.find((step) => step.id === 'downloadCsv')).toMatchObject({
			pathState: 'current'
		});
	});

	it('separates preview readiness from share readiness for live unexported results', () => {
		const handoffStatus = toSelectedSeriesResultsHandoffStatus(reportableWorkspace);

		expect(handoffStatus).toMatchObject({
			overallStatus: 'blocked',
			overallLabel: 'Not share-ready',
			headline: 'Preview ready; not ready to share',
			guidance:
				'Use these results for internal review only. Review interpretation, create the export file, and resolve finality before sharing outside the team.',
			nextAction:
				'Review interpretation limits before sharing; keep the current report-summary export internal.'
		});
		expect(handoffStatus.lanes).toEqual([
			expect.objectContaining({
				id: 'operational',
				label: 'Operational status',
				title: 'Preview data is ready',
				status: 'ready',
				detail: '12 submitted responses and 12 visible scores are available for review.'
			}),
			expect.objectContaining({
				id: 'interpretation',
				label: 'Interpretation status',
				title: 'Needs interpretation validation',
				status: 'blocked',
				detail:
					'Scoring is available, but the meaning, limits, and external claims have not been reviewed.'
			}),
			expect.objectContaining({
				id: 'export',
				label: 'Export status',
				title: 'Share-ready export not created',
				status: 'pending',
				detail: 'Create a share-ready export before sending files outside the team.'
			}),
			expect.objectContaining({
				id: 'finality',
				label: 'Finality status',
				title: 'Preliminary live data',
				status: 'pending',
				detail: 'Collection is still live. Results can change until the wave is closed.'
			})
		]);
	});

	it('does not label a downloadable export share-ready before interpretation and finality are ready', () => {
		const handoffStatus = toSelectedSeriesResultsHandoffStatus(reportableWorkspaceWithExport);

		expect(handoffStatus.overallLabel).toBe('Not share-ready');
		expect(handoffStatus.lanes.find((lane) => lane.id === 'export')).toMatchObject({
			title: 'Internal preview export ready',
			status: 'pending',
			detail:
				'A downloadable file exists, but use it internally until interpretation validation and collection finality are ready.'
		});
	});

	it('labels report-summary downloads as not analysis-ready when no response export exists', () => {
		const downloadAction = toSelectedSeriesReportsWorkflowActions(reportableWorkspaceWithExport).find(
			(action) => action.id === 'downloadCsv'
		);

		expect(downloadAction).toMatchObject({
			title: 'Download report-summary CSV',
			description:
				'Download the report-summary CSV for review packets only. This is not an analysis-ready response dataset.'
		});
	});

	it('explains score coverage gaps before external sharing', () => {
		const workspace: CampaignSeriesReportsWorkspaceResponse = {
			...reportableWorkspace,
			selectedCampaign: {
				...reportableWorkspace.selectedCampaign!,
				submittedResponseCount: 12,
				visibleScoreCount: 10
			}
		};
		const handoffStatus = toSelectedSeriesResultsHandoffStatus(workspace);

		expect(handoffStatus.lanes.find((lane) => lane.id === 'operational')?.detail).toContain(
			'2 submitted responses are not visible as scores because scoring, missing-answer rules, or disclosure still exclude them.'
		);
	});

	it('summarizes a live results packet as internal review only', () => {
		const packet = toSelectedSeriesResultsPacketReview(reportableWorkspace);

		expect(packet).toMatchObject({
			title: 'Can these results be used?',
			status: 'pending',
			primaryAction:
				'Create a response export for analysis, or create a report-summary file for internal review.'
		});
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'responses',
				status: 'ready',
				summary: '12 responses collected'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'scores',
				status: 'ready',
				summary: '12 scores visible'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'export_files',
				status: 'pending',
				summary: 'Create response export'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'use_status',
				status: 'pending',
				summary: 'Internal review only'
			})
		);
	});

	it('does not call results ready when responses exist but no scores are visible', () => {
		const packet = toSelectedSeriesResultsPacketReview({
			...reportableWorkspace,
			summary: {
				...reportableWorkspace.summary,
				submittedResponseCount: 1,
				scoreCount: 1,
				visibleScoreCount: 0
			},
			selectedCampaign: {
				...reportableWorkspace.selectedCampaign!,
				submittedResponseCount: 1,
				scoreCount: 1,
				visibleScoreCount: 0
			}
		});

		expect(packet).toMatchObject({
			title: 'Can these results be used?',
			status: 'blocked',
			primaryAction:
				'Use raw response export for internal analysis, or review result-output scoring, missing-answer rules, and disclosure.'
		});
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'responses',
				status: 'ready',
				summary: '1 response collected'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'scores',
				status: 'blocked',
				summary: 'No scores visible',
				detail:
					'1 response exists, but no scored result is visible. Check scoring setup, missing-answer rules, and disclosure before treating this as scored results.'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'use_status',
				status: 'blocked',
				summary: 'Raw responses only'
			})
		);
	});

	it('summarizes a closed validated response export as ready to share', () => {
		const packet = toSelectedSeriesResultsPacketReview(shareReadyWorkspace);

		expect(packet).toMatchObject({
			title: 'Can these results be used?',
			status: 'ready',
			primaryAction: 'Download the response dataset for analysis.'
		});
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'scores',
				status: 'ready',
				summary: '12 scores visible'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'export_files',
				status: 'ready',
				summary: 'Response dataset ready'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'use_status',
				status: 'ready',
				summary: 'Ready for controlled sharing'
			})
		);
	});

	it('explains score method limits before result preview exposes output rows', () => {
		const method = toSelectedSeriesScoreMethodReview(reportableWorkspace);

		expect(method).toMatchObject({
			title: 'How were these scores produced?',
			status: 'pending',
			description:
				'Review score outputs, coverage, missing-answer handling, and interpretation limits before using results.'
		});
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'outputs',
				status: 'pending',
				summary: 'Output names available after reviewing results'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'coverage',
				status: 'ready',
				summary: '12 submitted responses, 12 visible score rows'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'direction_scale',
				status: 'pending',
				summary: 'Direction and scale family need setup context'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'interpretation_boundary',
				status: 'pending',
				summary: 'Custom-study interpretation, not externally validated'
			})
		);
	});

	it('summarizes proof score outputs and missing-answer metadata after result preview', () => {
		const method = toSelectedSeriesScoreMethodReview(reportableWorkspace, reportProof);

		expect(method).toMatchObject({
			title: 'How were these scores produced?',
			status: 'pending'
		});
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'outputs',
				status: 'ready',
				summary: '2 score outputs: posture_strain, recovery_control'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'missingness',
				status: 'pending',
				summary: 'Some score inputs were incomplete'
			})
		);
		expect(method.items.find((item) => item.id === 'missingness')?.detail).toContain(
			'posture_strain used 10 of 12 expected answer contributions'
		);
	});

	it('previews a response dataset export before download', () => {
		const preview = toSelectedSeriesExportPreview(
			reportableWorkspaceWithResponseExport,
			responseExportArtifact
		);

		expect(preview).toMatchObject({
			title: 'What is in this export?',
			status: 'ready',
			downloadLabel: 'Download response dataset CSV'
		});
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'file_purpose',
				status: 'ready',
				summary: 'Response dataset CSV and codebook'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'row_shape',
				status: 'ready',
				summary: '12 rows; one row per submitted response'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'wave_fields',
				status: 'ready',
				summary: 'Wave fields included for 2 waves'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'trajectory_keys',
				status: 'ready',
				summary: 'Artifact-local repeat-response keys included'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'variables_values',
				status: 'ready',
				summary: '2 answer variables, 2 score metadata fields, 1 answer metadata field, 7 columns total'
			})
		);
		expect(preview.items.find((item) => item.id === 'variables_values')?.detail).toContain(
			'value labels and answer constraints'
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'missingness',
				status: 'ready',
				summary: 'Missing-answer codes documented'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'score_outputs',
				status: 'ready',
				summary: 'Score metadata for posture_strain'
			})
		);
	});

	it('previews a report-summary export as aggregate-only and not analysis-ready', () => {
		const preview = toSelectedSeriesExportPreview(
			reportableWorkspaceWithExport,
			reportSummaryExportArtifact
		);

		expect(preview).toMatchObject({
			title: 'What is in this export?',
			status: 'pending',
			downloadLabel: 'Download report-summary CSV'
		});
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'file_purpose',
				status: 'pending',
				summary: 'Report-summary CSV, not row-level response data'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'row_shape',
				status: 'ready',
				summary: '2 rows; one row per visible or suppressed score output'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'trajectory_keys',
				status: 'not_available',
				summary: 'No repeat-response keys in report-summary export'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'score_outputs',
				status: 'ready',
				summary: 'Score outputs listed in dimension_code'
			})
		);
	});

	it('explains export preview is pending before artifact content is reviewed', () => {
		const preview = toSelectedSeriesExportPreview(reportableWorkspaceWithResponseExport);

		expect(preview).toMatchObject({
			status: 'pending',
			downloadLabel: 'Review export file first'
		});
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'file_purpose',
				status: 'pending',
				summary: 'Review export file to inspect contents'
			})
		);
	});
	it('localizes the results workflow model for Croatian route copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy
			.reportsWorkflow as SelectedSeriesReportsWorkflowCopy;
		const path = toSelectedSeriesReportsPath(reportableWorkspaceWithResponseExport, {}, copy);
		const packet = toSelectedSeriesResultsPacketReview(reportableWorkspace, {}, copy);
		const preview = toSelectedSeriesExportPreview(
			reportableWorkspaceWithResponseExport,
			responseExportArtifact,
			copy
		);
		const pendingPreview = toSelectedSeriesExportPreview(
			reportableWorkspaceWithResponseExport,
			null,
			copy
		);

		expect(path.steps[0]).toMatchObject({
			step: '1',
			title: 'Pregled rezultata',
			description: 'Pregledajte sažetke rezultata za odabrano mjerenje bez narušavanja pravila prikaza.'
		});
		expect(path.steps.find((step) => step.id === 'downloadCsv')).toMatchObject({
			title: 'Preuzmi CSV skupa odgovora'
		});
		expect(packet).toMatchObject({
			title: 'Mogu li se ovi rezultati koristiti?',
			primaryAction: 'Izradite izvoz odgovora za analizu ili datoteku sažetka izvještaja za interni pregled.'
		});
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'responses',
				label: 'Odgovori',
				summary: '12 prikupljenih odgovora'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'scores',
				label: 'Rezultati',
				summary: '12 vidljivih rezultata'
			})
		);
		expect(packet.items).toContainEqual(
			expect.objectContaining({
				id: 'use_status',
				label: 'Status korištenja',
				summary: 'Samo interni pregled'
			})
		);
		expect(pendingPreview.items).toContainEqual(
			expect.objectContaining({
				id: 'file_purpose',
				label: 'Namjena datoteke',
				summary: 'Pregledajte datoteku izvoza za provjeru sadržaja'
			})
		);
		expect(pendingPreview.items).toContainEqual(
			expect.objectContaining({
				id: 'trajectory_keys',
				label: 'Ključevi praćenja',
				summary: 'Pravila ključeva praćenja dostupna su nakon pregleda datoteke'
			})
		);
		expect(preview).toMatchObject({
			title: 'Što je u ovom izvozu?',
			downloadLabel: 'Preuzmi CSV skupa odgovora'
		});
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'file_purpose',
				label: 'Namjena datoteke',
				summary: 'CSV skup odgovora i opis podataka'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'row_shape',
				label: 'Oblik redaka',
				summary: '12 redaka; jedan redak po predanom odgovoru'
			})
		);
		expect(preview.items).toContainEqual(
			expect.objectContaining({
				id: 'wave_fields',
				label: 'Polja mjerenja',
				summary: 'Polja mjerenja uključena su za 2 mjerenja'
			})
		);

		const method = toSelectedSeriesScoreMethodReview(reportableWorkspace, null, copy);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'coverage',
				label: 'Pokrivenost odgovora'
			})
		);
		expect(method.items).toContainEqual(
			expect.objectContaining({
				id: 'direction_scale',
				label: 'Smjer i ljestvica'
			})
		);
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

const shareReadyWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	...reportableWorkspaceWithResponseExport,
	selectedCampaign: {
		...reportableWorkspaceWithResponseExport.selectedCampaign!,
		status: 'closed',
		interpretationStatus: 'validated_interpretation'
	}
};

const reportProof: CampaignReportProofResponse = {
	campaignId: 'campaign-id',
	campaignSeriesId: 'series-id',
	campaignName: 'Wave 1',
	campaignStatus: 'live',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: 'launch-snapshot-id',
		templateVersionId: 'template-version-id',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleDocumentHash: 'hash',
		consentDocumentId: 'consent-id',
		retentionPolicyId: 'retention-id',
		disclosurePolicyId: 'disclosure-id',
		responseIdentityMode: 'anonymous',
		launchedAt: '2026-05-05T08:30:00Z'
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_small_groups'
	},
	scores: [
		{
			dimensionCode: 'posture_strain',
			disclosure: 'visible',
			submittedResponseCount: 12,
			scoreCount: 10,
			nValidTotal: 10,
			nExpectedTotal: 12,
			missingPolicyStatusSummary: 'partial',
			mean: 6.7,
			min: 4,
			max: 9,
			suppressionReason: null
		},
		{
			dimensionCode: 'recovery_control',
			disclosure: 'visible',
			submittedResponseCount: 12,
			scoreCount: 12,
			nValidTotal: 12,
			nExpectedTotal: 12,
			missingPolicyStatusSummary: 'complete',
			mean: 3.2,
			min: 1,
			max: 5,
			suppressionReason: null
		}
	]
};

const responseExportArtifact: ReportProofExportArtifactResponse = {
	id: 'response-export-artifact-id',
	targetKind: 'campaign_series',
	targetId: 'series-id',
	targetLabel: 'Quarterly burnout pulse',
	campaignId: null,
	campaignSeriesId: 'series-id',
	artifactType: 'campaign_series_response_csv_codebook',
	status: 'succeeded',
	format: 'csv_codebook',
	fileName: 'campaign-series-responses.csv',
	contentType: 'text/csv',
	rowCount: 12,
	byteSize: 2048,
	checksumSha256: 'checksum',
	createdAt: '2026-05-05T09:10:00Z',
	completedAt: '2026-05-05T09:10:03Z',
	startedAt: null,
	failedAt: null,
	expiresAt: null,
	deletedAt: null,
	failureReasonCode: null,
	canDownload: true,
	csvContent:
		'response_row_id,trajectory_id,wave_label,answer_posture,answer_recovery,posture_strain_n_valid,posture_strain_missing_policy_status\n1,t1,Wave 1,4,2,2,complete',
	codebookJson: JSON.stringify({
		artifactType: 'campaign_series_response_csv_codebook',
		format: 'csv_codebook',
		rowCount: 12,
		campaignCount: 2,
		trajectoryCount: 6,
		trajectoryIdPolicy: 'per_artifact',
		missingTreatment: {
			blank: 'question_not_answered_or_not_present_in_session_template',
			skipped: '__skipped',
			notApplicable: '__not_applicable'
		},
		columns: [
			{ name: 'response_row_id', source: 'artifact_local_row_id' },
			{ name: 'trajectory_id', source: 'artifact_local_identity' },
			{ name: 'wave_label', source: 'response_export_projection' },
			{
				name: 'answer_posture',
				source: 'answer',
				questionCode: 'posture',
				missingCodes: { skipped: '__skipped' },
				scale: { code: 'agreement_1_5', minValue: 1, maxValue: 5 }
			},
			{
				name: 'answer_recovery',
				source: 'answer',
				questionCode: 'recovery',
				missingCodes: { skipped: '__skipped' },
				valueLabels: { o01: 'Low recovery', o02: 'High recovery' },
				answerMetadata: {
					choice: {
						allowOther: true,
						otherLabel: 'Other recovery factor'
					}
				}
			},
			{
				name: 'posture_strain_n_valid',
				source: 'score_output_metadata',
				dimensionCode: 'posture_strain',
				metadataKind: 'n_valid'
			},
			{
				name: 'posture_strain_missing_policy_status',
				source: 'score_output_metadata',
				dimensionCode: 'posture_strain',
				metadataKind: 'missing_policy_status'
			}
		]
	})
};

const reportSummaryExportArtifact: ReportProofExportArtifactResponse = {
	...responseExportArtifact,
	id: 'report-export-artifact-id',
	targetKind: 'campaign',
	targetId: 'campaign-id',
	targetLabel: 'Wave 1',
	campaignId: 'campaign-id',
	campaignSeriesId: 'series-id',
	artifactType: 'report_proof_csv_codebook',
	fileName: 'report-proof.csv',
	rowCount: 2,
	csvContent:
		'dimension_code,score_count,n_valid_total,n_expected_total,missing_policy_status_summary,mean\nposture_strain,10,10,12,partial,6.7\nrecovery_control,12,12,12,complete,3.2',
	codebookJson: JSON.stringify({
		artifactType: 'report_proof_csv_codebook',
		format: 'csv_codebook',
		rowCount: 2,
		dataFinality: 'preliminary_live',
		excludedIdentifiers: ['tenant_id'],
		columns: [
			{ name: 'dimension_code', source: 'report_proof_score_summary' },
			{ name: 'score_count', source: 'report_proof_score_summary' },
			{ name: 'n_valid_total', source: 'score_output_metadata' },
			{ name: 'n_expected_total', source: 'score_output_metadata' },
			{ name: 'missing_policy_status_summary', source: 'score_output_metadata' },
			{ name: 'mean', source: 'report_proof_score_summary' }
		]
	})
};

