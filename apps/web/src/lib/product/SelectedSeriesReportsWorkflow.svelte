<script lang="ts">
	import { page } from '$app/state';
	import { env } from '$env/dynamic/public';
	import { Download, FileSearch, LoaderCircle, Send } from 'lucide-svelte';
	import type {
		CampaignSeriesReportsWidgetManifestResponse,
		CampaignSeriesReportsWorkspaceResponse
	} from '$lib/api/product';
	import type {
		CampaignReportProofResponse,
		ExportArtifactDownloadResponse,
		ReportProofExportArtifactResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import type { ProductStatus } from '$lib/components/status-badge-labels';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import ReportWidgetsSection from '$lib/product/widgets/ReportWidgetsSection.svelte';
	import { formatProductCopy } from '$lib/product/widgets/report-widget-format';
	import {
		toSelectedSeriesExportPreview,
		toSelectedSeriesScoreMethodReview,
		toSelectedSeriesResultsPacketReview,
		type SelectedSeriesReportsWorkflowActionId
	} from './reports-workflow';
	import { createSetupApiFromEnv } from './route-state';
	import { formatScoreOutputMetadata, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	const primaryReportWidgetKinds = ['results-dashboard/v1', 'visual-analytics-entry/v1'];
	const detailReportWidgetKinds = [
		'report-readiness-summary/v1',
		'score-coverage-summary/v1',
		'selected-campaign-report-state/v1',
		'export-artifact-registry/v1',
		'finality-provenance-summary/v1'
	];

	let {
		workspace,
		widgetManifest = null,
		widgetWarning = null,
		canManageSetup = true,
		onWorkspaceRefresh
	}: {
		workspace: CampaignSeriesReportsWorkspaceResponse;
		widgetManifest?: CampaignSeriesReportsWidgetManifestResponse | null;
		widgetWarning?: string | null;
		canManageSetup?: boolean;
		onWorkspaceRefresh?: () => Promise<boolean>;
	} = $props();

	const setupApi = createSetupApiFromEnv(env);

	let reportProofResult = $state<CampaignReportProofResponse | null>(null);
	let exportResult = $state<ReportProofExportArtifactResponse | null>(null);
	let responseExportResult = $state<ReportProofExportArtifactResponse | null>(null);
	let storedExportResult = $state<ReportProofExportArtifactResponse | null>(null);
	let downloadResult = $state<ExportArtifactDownloadResponse | null>(null);
	let refreshWarning = $state<string | null>(null);
	let actionStates = $state<Record<SelectedSeriesReportsWorkflowActionId, StepState>>({
		reportProof: 'idle',
		exportArtifact: 'idle',
		responseExport: 'idle',
		fetchArtifact: 'idle',
		downloadCsv: 'idle'
	});
	let actionErrors = $state<Record<SelectedSeriesReportsWorkflowActionId, string | null>>({
		reportProof: null,
		exportArtifact: null,
		responseExport: null,
		fetchArtifact: null,
		downloadCsv: null
	});

	const appLocale = $derived(appLocaleFromPageData(page.data));
	const reportsWorkflowCopy = $derived(routePageCopy(appLocale).selectedStudy.reportsWorkflow);
	const reportsUi = $derived(reportsWorkflowCopy.component);
	const selectedCampaign = $derived(workspace.selectedCampaign);
	const latestResponseExportArtifact = $derived(
		workspace.exportArtifacts.find(
			(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
		) ?? null
	);
	const latestResultsMatrixExportArtifact = $derived(
		workspace.exportArtifacts.find(
			(artifact) => artifact.artifactType === 'campaign_series_results_matrix_csv_codebook'
		) ?? null
	);
	const latestLegacyReportExportArtifact = $derived(
		workspace.exportArtifacts.find(
			(artifact) => artifact.artifactType === 'report_proof_csv_codebook'
		) ?? null
	);
	const latestReportExportArtifact = $derived(
		latestResultsMatrixExportArtifact ?? latestLegacyReportExportArtifact
	);
	const latestDownloadableExportArtifact = $derived(
		workspace.exportArtifacts.find((artifact) => artifact.canDownload) ?? null
	);
	const preferredExportArtifact = $derived(
		storedExportResult ??
			responseExportResult ??
			exportResult ??
			latestReportExportArtifact ??
			latestResponseExportArtifact ??
			latestDownloadableExportArtifact ??
			null
	);
	const preferredDownloadableExportArtifact = $derived(
		(storedExportResult?.canDownload ? storedExportResult : null) ??
			(responseExportResult?.canDownload ? responseExportResult : null) ??
			(exportResult?.canDownload ? exportResult : null) ??
			(latestResultsMatrixExportArtifact?.canDownload ? latestResultsMatrixExportArtifact : null) ??
			(latestResponseExportArtifact?.canDownload ? latestResponseExportArtifact : null) ??
			(latestLegacyReportExportArtifact?.canDownload ? latestLegacyReportExportArtifact : null) ??
			latestDownloadableExportArtifact ??
			null
	);
	const currentExportArtifactId = $derived(
		preferredExportArtifact?.id ?? selectedCampaign?.latestExportArtifactId ?? null
	);
	const currentDownloadableExportArtifactId = $derived(
		preferredDownloadableExportArtifact?.id ??
			(selectedCampaign?.latestExportArtifactCanDownload
				? selectedCampaign.latestExportArtifactId
				: null) ??
			null
	);
	const currentExportFileName = $derived(
		preferredExportArtifact?.fileName ?? selectedCampaign?.latestExportArtifactFileName ?? null
	);
	const currentExportPurpose = $derived(
		preferredExportArtifact?.artifactType === 'campaign_series_response_csv_codebook'
			? reportsUi.currentPurpose.responseDataset
			: reportsUi.currentPurpose.reportSummary
	);
	const localState = $derived({
		reportProofViewed: Boolean(reportProofResult),
		exportCreated: Boolean(exportResult),
		responseExportCreated: Boolean(responseExportResult),
		artifactFetched: Boolean(storedExportResult),
		csvDownloaded: Boolean(downloadResult)
	});
	const packetReview = $derived(
		toSelectedSeriesResultsPacketReview(workspace, localState, reportsWorkflowCopy)
	);
	const methodReview = $derived(
		toSelectedSeriesScoreMethodReview(workspace, reportProofResult, reportsWorkflowCopy)
	);
	const exportPreviewArtifact = $derived(
		storedExportResult ?? responseExportResult ?? exportResult ?? null
	);
	const exportPreview = $derived(
		toSelectedSeriesExportPreview(workspace, exportPreviewArtifact, reportsWorkflowCopy)
	);
	const canReviewResults = $derived(
		canManageSetup && Boolean(selectedCampaign) && actionStates.reportProof !== 'submitting'
	);
	const canCreateResultsMatrixExport = $derived(
		canManageSetup && Boolean(selectedCampaign) && actionStates.exportArtifact !== 'submitting'
	);
	const canCreateResponseExport = $derived(
		canManageSetup && Boolean(workspace.series.id) && actionStates.responseExport !== 'submitting'
	);
	const canReviewExportFile = $derived(
		canManageSetup &&
			Boolean(currentExportArtifactId) &&
			actionStates.fetchArtifact !== 'submitting'
	);
	const canDownloadCsv = $derived(
		canManageSetup &&
			Boolean(currentDownloadableExportArtifactId) &&
			actionStates.downloadCsv !== 'submitting'
	);
	const hasDetailReportWidgets = $derived(
		Boolean(widgetManifest?.widgets.some((widget) => detailReportWidgetKinds.includes(widget.kind)))
	);
	const downloadCsvTitle = $derived(
		preferredDownloadableExportArtifact?.artifactType === 'campaign_series_response_csv_codebook'
			? reportsWorkflowCopy.actions.downloadCsv.responseDatasetTitle
			: reportsWorkflowCopy.actions.downloadCsv.reportSummaryTitle
	);
	const resultsMatrixChoice = $derived(
		toExportChoice(
			reportsUi.reportSummaryCsvCodebook,
			reportsUi.resultsMatrixUse,
			exportResult ?? latestResultsMatrixExportArtifact
		)
	);
	const responseDatasetChoice = $derived(
		toExportChoice(
			reportsUi.responseCsvCodebook,
			reportsUi.responseDatasetUse,
			responseExportResult ?? latestResponseExportArtifact
		)
	);

	function scoreInterpretationMeta(
		interpretation:
			| {
					status: string;
					source: string;
					isValidated: boolean;
					isOfficial: boolean;
			  }
			| null
			| undefined
	) {
		if (!interpretation) {
			return null;
		}

		return [
			humanize(interpretation.status),
			humanize(interpretation.source),
			interpretation.isValidated ? reportsUi.reviewed : reportsUi.notReviewed,
			interpretation.isOfficial ? reportsUi.official : reportsUi.notOfficial
		].join(' / ');
	}

	async function viewReportProof() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				reportProof: reportsUi.errors.createWaveBeforeResults
			};
			return;
		}

		const result = await runAction('reportProof', () =>
			setupApi.getCampaignReportProof(selectedCampaign.id)
		);

		if (result) {
			reportProofResult = result;
			exportResult = null;
			responseExportResult = null;
			storedExportResult = null;
			downloadResult = null;
		}
	}

	async function createExportArtifact() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				exportArtifact: reportsUi.errors.createWaveBeforeReportExport
			};
			return;
		}

		const result = await runAction(
			'exportArtifact',
			() => setupApi.createCampaignSeriesResultsMatrixExport(workspace.series.id),
			{ refreshAfter: true }
		);

		if (result) {
			exportResult = result;
			responseExportResult = null;
			storedExportResult = null;
			downloadResult = null;
		}
	}

	async function createResponseExportArtifact() {
		if (!workspace.series.id) {
			actionErrors = {
				...actionErrors,
				responseExport: reportsUi.errors.createStudyBeforeResponseExport
			};
			return;
		}

		const result = await runAction(
			'responseExport',
			() => setupApi.createCampaignSeriesResponseExport(workspace.series.id),
			{ refreshAfter: true }
		);

		if (result) {
			responseExportResult = result;
			storedExportResult = null;
			downloadResult = null;
		}
	}

	async function fetchStoredArtifact() {
		if (!currentExportArtifactId) {
			actionErrors = {
				...actionErrors,
				fetchArtifact: reportsUi.errors.createExportBeforeReview
			};
			return;
		}

		const result = await runAction('fetchArtifact', () =>
			setupApi.getExportArtifact(currentExportArtifactId)
		);

		if (result) {
			storedExportResult = result;
			downloadResult = null;
		}
	}

	async function downloadCsv() {
		if (!currentDownloadableExportArtifactId) {
			actionErrors = {
				...actionErrors,
				downloadCsv: currentExportArtifactId
					? reportsUi.errors.selectDownloadableExport
					: reportsUi.errors.createExportBeforeDownload
			};
			return;
		}

		const result = await runAction('downloadCsv', () =>
			setupApi.downloadExportArtifactCsv(currentDownloadableExportArtifactId)
		);

		if (result) {
			downloadResult = result;
			triggerCsvDownload(result);
		}
	}

	async function runAction<T>(
		actionId: SelectedSeriesReportsWorkflowActionId,
		action: () => Promise<T>,
		options: { refreshAfter?: boolean } = {}
	) {
		actionStates = { ...actionStates, [actionId]: 'submitting' };
		actionErrors = { ...actionErrors, [actionId]: null };
		refreshWarning = null;

		try {
			const result = await action();
			actionStates = { ...actionStates, [actionId]: 'succeeded' };
			if (options.refreshAfter) {
				const refreshed = await onWorkspaceRefresh?.();
				if (refreshed === false) {
					refreshWarning = reportsUi.errors.refreshFailed;
				}
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, reportsUi.errors.actionFailed)
			};
			return null;
		}
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? reportsUi.suppressed : value.toFixed(2);
	}

	function humanize(value: string | null | undefined) {
		return value ? formatProductCopy(value.replaceAll('_', ' ')) : reportsUi.notAvailable;
	}

	function csvPreview(content: string | null | undefined) {
		return (content ?? '').trim().split(/\r?\n/).slice(0, 6).join('\n');
	}

	function toExportChoice(
		title: string,
		detail: string,
		artifact:
			| {
					fileName: string | null;
					rowCount: number;
					canDownload: boolean;
			  }
			| null
			| undefined
	) {
		const status: ProductStatus = artifact
			? artifact.canDownload
				? 'ready'
				: 'pending'
			: 'not_available';
		return {
			title,
			detail,
			status,
			fileName: artifact?.fileName ?? reportsUi.notAvailable,
			rows: artifact ? reportsUi.rows(artifact.rowCount) : reportsUi.notAvailable
		};
	}

	function triggerCsvDownload(result: ExportArtifactDownloadResponse) {
		if (typeof document === 'undefined') {
			return;
		}

		const blob = new Blob([result.content ?? ''], {
			type: result.contentType || 'text/csv;charset=utf-8'
		});
		const url = URL.createObjectURL(blob);
		const anchor = document.createElement('a');
		anchor.href = url;
		anchor.download = result.fileName || 'results.csv';
		anchor.rel = 'noopener';
		document.body.append(anchor);
		anchor.click();
		anchor.remove();
		window.setTimeout(() => URL.revokeObjectURL(url), 0);
	}
</script>

<section
	class="product-panel"
	role="group"
	aria-label={reportsWorkflowCopy.surface.reviewActionsAria}
>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">{reportsWorkflowCopy.surface.flowKicker}</p>
			<h3 class="product-title">{reportsWorkflowCopy.surface.title}</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				{reportsWorkflowCopy.surface.description}
			</p>
		</div>
		<StatusBadge status={packetReview.status} />
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<article class="record-row" role="group" aria-label={reportsUi.resultsPreview}>
		<div class="record-row__header">
			<div>
				<span>{reportsWorkflowCopy.surface.flowKicker}</span>
				<strong>{selectedCampaign?.name ?? workspace.series.name}</strong>
			</div>
			<StatusBadge status={packetReview.status} />
		</div>
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.responsesLabel}</dt>
				<dd class="record-field__value">{workspace.summary.submittedResponseCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.scoresLabel}</dt>
				<dd class="record-field__value">{workspace.summary.visibleScoreCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.exportsLabel}</dt>
				<dd class="record-field__value">{workspace.summary.exportArtifactCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.statusLabel}</dt>
				<dd class="record-field__value">
					{selectedCampaign ? humanize(selectedCampaign.reportStatus) : packetReview.title}
				</dd>
			</div>
		</dl>
	</article>

	<ReportWidgetsSection
		manifest={widgetManifest}
		warning={widgetWarning}
		embedded={true}
		includeKinds={primaryReportWidgetKinds}
		hideWhenEmpty={true}
		showChrome={false}
	/>
	<article
		class="overview-command-card"
		role="region"
		aria-label={reportsWorkflowCopy.surface.resultsUseReviewAria}
	>
		<div>
			<p class="product-kicker">{reportsWorkflowCopy.surface.useDecisionLabel}</p>
			<h4 class="setup-current-task__title">{packetReview.title}</h4>
			<p class="text-sm text-[var(--color-text-muted)]">{packetReview.primaryAction}</p>
		</div>
		<div class="action-row">
			<StatusBadge status={packetReview.status} />
			{#if canManageSetup}
				<button
					type="button"
					class="primary-button"
					disabled={!canReviewResults}
					onclick={viewReportProof}
				>
					{#if actionStates.reportProof === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<FileSearch size={17} aria-hidden="true" />
					{/if}
					<span>{reportsWorkflowCopy.actions.reportProof.title}</span>
				</button>
			{/if}
		</div>
	</article>

	{#if actionErrors.reportProof}
		<p class="error-line">{actionErrors.reportProof}</p>
	{/if}
	{#if reportProofResult}
		{@render ReportProofResult()}
	{/if}

	<article
		class="record-row"
		role="region"
		aria-label={reportsWorkflowCopy.surface.exportPreviewAria}
	>
		<div class="record-row__header">
			<div>
				<p class="product-kicker">{reportsWorkflowCopy.surface.exportPreviewLabel}</p>
				<h4 class="setup-current-task__title">{exportPreview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{exportPreview.description}</p>
				<p class="record-field__label">{currentExportPurpose}</p>
			</div>
			<StatusBadge
				status={preferredDownloadableExportArtifact
					? 'ready'
					: preferredExportArtifact
						? 'pending'
						: 'not_available'}
				label={preferredDownloadableExportArtifact
					? reportsUi.downloadable
					: preferredExportArtifact
						? reportsUi.notReady
						: reportsUi.notAvailable}
			/>
		</div>
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.reportExportResult}</dt>
				<dd class="record-field__value">
					{exportResult?.fileName ??
						latestResultsMatrixExportArtifact?.fileName ??
						reportsUi.notAvailable}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.responseExportResult}</dt>
				<dd class="record-field__value">
					{responseExportResult?.fileName ??
						latestResponseExportArtifact?.fileName ??
						reportsUi.notAvailable}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.downloadStatus}</dt>
				<dd class="record-field__value">
					{currentDownloadableExportArtifactId
						? reportsUi.downloadable
						: exportPreview.downloadLabel}
				</dd>
			</div>
		</dl>

		<div class="export-choice-grid" aria-label={reportsUi.exportChoicesAria}>
			{#each [resultsMatrixChoice, responseDatasetChoice] as choice (choice.title)}
				<article class="export-choice-card" data-status={choice.status}>
					<div class="record-row__header">
						<div>
							<p class="record-row__title">{choice.title}</p>
							<p class="text-sm text-[var(--color-text-muted)]">{choice.detail}</p>
						</div>
						<StatusBadge status={choice.status} />
					</div>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">{reportsUi.file}</dt>
							<dd class="record-field__value">{choice.fileName}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{reportsUi.rowsLabel}</dt>
							<dd class="record-field__value">{choice.rows}</dd>
						</div>
					</dl>
				</article>
			{/each}
		</div>

		{#if exportPreview.fileProfile}
			{@const profile = exportPreview.fileProfile}
			<section class="export-file-profile" aria-label={reportsUi.exportProfileAria}>
				<div class="record-row__header">
					<div>
						<p class="product-kicker">{reportsUi.reviewedFile}</p>
						<h4 class="record-row__title">{profile.title}</h4>
						<p class="text-sm text-[var(--color-text-muted)]">{profile.fileName}</p>
					</div>
					<StatusBadge status={profile.status} label={profile.downloadSummary} />
				</div>
				<dl class="record-grid">
					<div class="record-field">
						<dt class="record-field__label">{reportsUi.rowShapeLabel}</dt>
						<dd class="record-field__value">{profile.rowShape}</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">{reportsUi.columnsLabel}</dt>
						<dd class="record-field__value">{profile.columnSummary}</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">{reportsUi.downloadStatus}</dt>
						<dd class="record-field__value">{profile.downloadSummary}</dd>
					</div>
				</dl>
				<div class="export-file-profile__columns">
					<p class="record-field__label">{reportsUi.sampleColumnsLabel}</p>
					{#if profile.sampleColumns.length > 0}
						<div class="export-column-chip-list">
							{#each profile.sampleColumns as column (column)}
								<span>{column}</span>
							{/each}
						</div>
					{:else}
						<p class="text-sm text-[var(--color-text-muted)]">{reportsUi.noSampleColumns}</p>
					{/if}
				</div>
				<div class="export-readiness-list" aria-label={reportsUi.readinessChecks}>
					{#each profile.readinessItems as item (item.id)}
						<article class="export-readiness-item" data-state={item.status}>
							<div class="questionnaire-blueprint-review__item-header">
								<p class="record-field__label">{item.label}</p>
								<StatusBadge status={item.status} />
							</div>
							<p class="record-row__title">{item.summary}</p>
							<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.detail}</p>
						</article>
					{/each}
				</div>
			</section>
		{/if}

		{#if canManageSetup}
			<div class="action-row">
				<button
					type="button"
					class="secondary-button"
					disabled={!canCreateResultsMatrixExport}
					onclick={createExportArtifact}
				>
					{#if actionStates.exportArtifact === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<Send size={17} aria-hidden="true" />
					{/if}
					<span>{reportsUi.createReportSummaryExport}</span>
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!canCreateResponseExport}
					onclick={createResponseExportArtifact}
				>
					{#if actionStates.responseExport === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<Send size={17} aria-hidden="true" />
					{/if}
					<span>{reportsUi.createResponseExport}</span>
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!canReviewExportFile}
					onclick={fetchStoredArtifact}
				>
					{#if actionStates.fetchArtifact === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<FileSearch size={17} aria-hidden="true" />
					{/if}
					<span>{reportsWorkflowCopy.actions.fetchArtifact.title}</span>
				</button>
				<button
					type="button"
					class={canDownloadCsv ? 'primary-button' : 'secondary-button'}
					disabled={!canDownloadCsv}
					onclick={downloadCsv}
				>
					{#if actionStates.downloadCsv === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<Download size={17} aria-hidden="true" />
					{/if}
					<span>{downloadCsvTitle}</span>
				</button>
			</div>
			{#if actionErrors.exportArtifact}
				<p class="error-line">{actionErrors.exportArtifact}</p>
			{/if}
			{#if actionErrors.responseExport}
				<p class="error-line">{actionErrors.responseExport}</p>
			{/if}
			{#if actionErrors.fetchArtifact}
				<p class="error-line">{actionErrors.fetchArtifact}</p>
			{/if}
			{#if actionErrors.downloadCsv}
				<p class="error-line">{actionErrors.downloadCsv}</p>
			{/if}
		{/if}

		{#if exportResult}
			{@render ExportArtifactResult({
				result: exportResult,
				ariaLabel: reportsUi.reportExportResult,
				kicker: reportsUi.reportExport,
				title: reportsUi.reportSummaryCsvCodebook
			})}
		{/if}
		{#if responseExportResult}
			{@render ExportArtifactResult({
				result: responseExportResult,
				ariaLabel: reportsUi.responseExportResult,
				kicker: reportsUi.responseExport,
				title: reportsUi.responseCsvCodebook
			})}
		{/if}
		{#if storedExportResult}
			{@render StoredArtifactResult()}
		{/if}
		{#if downloadResult}
			{@render CsvDownloadResult()}
		{/if}
	</article>

	<details class="record-row">
		<summary class="record-row__header">
			<span>{reportsUi.detailsDrawerTitle}</span>
			<StatusBadge status={exportPreview.status} />
		</summary>

		{#if hasDetailReportWidgets || widgetWarning}
			<ReportWidgetsSection
				manifest={widgetManifest}
				warning={widgetWarning}
				embedded={true}
				includeKinds={detailReportWidgetKinds}
				hideWhenEmpty={true}
				showChrome={false}
			/>
		{/if}

		<div class="questionnaire-blueprint-review__grid">
			{#each methodReview.items as item (item.id)}
				<section
					class="questionnaire-blueprint-review__item"
					data-state={item.status}
					aria-label={item.label}
				>
					<div class="questionnaire-blueprint-review__item-header">
						<p class="record-field__label">{item.label}</p>
						<StatusBadge status={item.status} />
					</div>
					<p class="record-row__title">{item.summary}</p>
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.detail}</p>
				</section>
			{/each}
			{#each exportPreview.items as item (item.id)}
				<section
					class="questionnaire-blueprint-review__item"
					data-state={item.status}
					aria-label={item.label}
				>
					<div class="questionnaire-blueprint-review__item-header">
						<p class="record-field__label">{item.label}</p>
						<StatusBadge status={item.status} />
					</div>
					<p class="record-row__title">{item.summary}</p>
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.detail}</p>
				</section>
			{/each}
		</div>
		<p class="result-line">
			<span>{reportsUi.downloadAction}</span>
			<span>{exportPreview.downloadLabel}</span>
		</p>
	</details>
	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">{reportsUi.readOnlyTitle}</strong>
			<span>{reportsUi.readOnlyBody}</span>
		</p>
	{/if}
</section>

{#snippet ReportProofResult()}
	{#if reportProofResult}
		<section class="score-result-panel report-proof-panel" aria-label={reportsUi.reportPreviewAria}>
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">{reportsUi.resultsPreview}</p>
					<h4 class="record-row__title">{reportsUi.aggregateResultPreview}</h4>
				</div>
				<StatusBadge status="ready" label={reportsUi.internalPreview} />
			</div>
			<div class="response-lab__meta">
				<span>{reportsUi.internalPreview}</span>
				<span
					>{humanize(reportProofResult.launchSnapshot.responseIdentityMode)}
					{reportsUi.responsesSuffix}</span
				>
				<span>{reportsUi.minimumGroup(reportProofResult.disclosurePolicy.kMin)}</span>
			</div>
			<div class="score-card-list" aria-label={reportsUi.reportPreviewScoresAria}>
				{#each reportProofResult.scores as score (score.dimensionCode)}
					{@const scoreMetadata = formatScoreOutputMetadata(
						score.disclosure === 'visible' ? score.nValidTotal : null,
						score.disclosure === 'visible' ? score.nExpectedTotal : null,
						score.disclosure === 'visible' ? score.missingPolicyStatusSummary : null,
						{
							calculationLabel: score.calculationLabel,
							scoreRangeMin: score.scoreRangeMin,
							scoreRangeMax: score.scoreRangeMax
						}
					)}
					<article class="score-card" aria-label={reportsUi.reportScoreAria(score.dimensionCode)}>
						<div>
							<p class="score-card__label">{score.displayLabel?.trim() || score.dimensionCode}</p>
							<p
								class={score.disclosure === 'visible'
									? 'score-card__value'
									: 'score-card__interpretation'}
							>
								{formatNullableScoreValue(score.mean)}
							</p>
						</div>
						<p class="score-card__meta">{humanize(score.disclosure)}</p>
						<p class="score-card__interpretation">
							{reportsUi.scoreCount(score.scoreCount ?? reportsUi.suppressed)}
						</p>
						{#if scoreMetadata}
							<p class="score-card__interpretation">{scoreMetadata}</p>
						{/if}
						{#if score.interpretation}
							<p class="score-card__interpretation">{score.interpretation.label}</p>
							<p class="score-card__interpretation">
								{scoreInterpretationMeta(score.interpretation)}
							</p>
						{/if}
					</article>
				{/each}
			</div>
		</section>
	{/if}
{/snippet}

{#snippet ExportArtifactResult({
	result,
	ariaLabel,
	kicker,
	title
}: {
	result: ReportProofExportArtifactResponse;
	ariaLabel: string;
	kicker: string;
	title: string;
})}
	<section class="score-result-panel report-proof-panel" aria-label={ariaLabel}>
		<div class="score-result-panel__header">
			<div>
				<p class="product-kicker">{kicker}</p>
				<h4 class="record-row__title">{title}</h4>
			</div>
			<StatusBadge
				status={result.canDownload ? 'ready' : 'pending'}
				label={result.canDownload ? reportsUi.downloadable : reportsUi.exportPreparing}
			/>
		</div>
		<div class="response-lab__meta">
			<span>{reportsUi.exportFile}</span>
			<span>{result.canDownload ? reportsUi.downloadable : humanize(result.status)}</span>
			<span>{reportsUi.rows(result.rowCount)}</span>
		</div>
		<div class="score-result-panel__footer">
			{@render ResultLine({ label: reportsUi.file, value: result.fileName })}
		</div>
		{#if csvPreview(result.csvContent)}
			<pre class="csv-preview">{csvPreview(result.csvContent)}</pre>
		{/if}
	</section>
{/snippet}

{#snippet StoredArtifactResult()}
	{#if storedExportResult}
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.exportFile}</dt>
				<dd class="record-field__value">{storedExportResult.fileName}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.rowsLabel}</dt>
				<dd class="record-field__value">{storedExportResult.rowCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{reportsUi.downloadStatus}</dt>
				<dd class="record-field__value">
					{storedExportResult.canDownload
						? reportsUi.downloadable
						: humanize(storedExportResult.status)}
				</dd>
			</div>
		</dl>
	{/if}
{/snippet}

{#snippet CsvDownloadResult()}
	{#if downloadResult}
		<div class="response-lab__meta">
			<span>{reportsUi.downloadedCsv}</span>
			<span>{downloadResult.contentType}</span>
			<span>{reportsUi.bytes(downloadResult.byteSize)}</span>
		</div>
		{@render ResultLine({ label: reportsUi.downloadedFile, value: downloadResult.fileName })}
		{#if csvPreview(downloadResult.content)}
			<pre class="csv-preview">{csvPreview(downloadResult.content)}</pre>
		{/if}
	{/if}
{/snippet}

{#snippet ResultLine({ label, value }: { label: string; value: string | null | undefined })}
	{#if value}
		<p class="result-line">
			<span>{label}</span>
			<span>{value}</span>
		</p>
	{/if}
{/snippet}
