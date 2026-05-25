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
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import ReportWidgetsSection from '$lib/product/widgets/ReportWidgetsSection.svelte';
	import {
		toSelectedSeriesExportPreview,
		toSelectedSeriesScoreMethodReview,
		toSelectedSeriesResultsPacketReview,
		toSelectedSeriesReportsPath,
		type SelectedSeriesReportsPathStepState,
		type SelectedSeriesReportsWorkflowActionId
	} from './reports-workflow';
	import { createSetupApiFromEnv } from './route-state';
	import { formatScoreOutputMetadata, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';

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
	const selectedCampaign = $derived(workspace.selectedCampaign);
	const latestResponseExportArtifact = $derived(
		workspace.exportArtifacts.find(
			(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
		) ?? null
	);
	const latestReportExportArtifact = $derived(
		workspace.exportArtifacts.find((artifact) => artifact.artifactType === 'report_proof_csv_codebook') ??
			null
	);
	const latestDownloadableExportArtifact = $derived(
		workspace.exportArtifacts.find((artifact) => artifact.canDownload) ?? null
	);
	const latestResponseExportArtifactId = $derived(latestResponseExportArtifact?.id ?? null);
	const currentExportArtifactId = $derived(
		storedExportResult?.id ??
			responseExportResult?.id ??
			exportResult?.id ??
			latestResponseExportArtifactId ??
			latestReportExportArtifact?.id ??
			selectedCampaign?.latestExportArtifactId ??
			latestDownloadableExportArtifact?.id ??
			null
	);
	const currentDownloadableExportArtifactId = $derived(
		(storedExportResult?.canDownload ? storedExportResult.id : null) ??
			(responseExportResult?.canDownload ? responseExportResult.id : null) ??
			(exportResult?.canDownload ? exportResult.id : null) ??
			latestDownloadableExportArtifact?.id ??
			(selectedCampaign?.latestExportArtifactCanDownload
				? selectedCampaign.latestExportArtifactId
				: null) ??
			null
	);
	const currentExportFileName = $derived(
		storedExportResult?.fileName ??
			responseExportResult?.fileName ??
			exportResult?.fileName ??
			latestResponseExportArtifact?.fileName ??
			latestReportExportArtifact?.fileName ??
			selectedCampaign?.latestExportArtifactFileName ??
			latestDownloadableExportArtifact?.fileName ??
			null
	);
	const currentExportPurpose = $derived(
		currentExportArtifactId === latestResponseExportArtifact?.id || responseExportResult?.id === currentExportArtifactId
			? 'Response dataset CSV and codebook'
			: 'Report-summary CSV, not analysis-ready response dataset'
	);
	const localState = $derived({
		reportProofViewed: Boolean(reportProofResult),
		exportCreated: Boolean(exportResult),
		responseExportCreated: Boolean(responseExportResult),
		artifactFetched: Boolean(storedExportResult),
		csvDownloaded: Boolean(downloadResult)
	});
	const reportsPath = $derived(toSelectedSeriesReportsPath(workspace, localState, reportsWorkflowCopy));
	const packetReview = $derived(
		toSelectedSeriesResultsPacketReview(workspace, localState, reportsWorkflowCopy)
	);
	const methodReview = $derived(
		toSelectedSeriesScoreMethodReview(workspace, reportProofResult, reportsWorkflowCopy)
	);
	const exportPreviewArtifact = $derived(storedExportResult ?? responseExportResult ?? exportResult ?? null);
	const exportPreview = $derived(
		toSelectedSeriesExportPreview(workspace, exportPreviewArtifact, reportsWorkflowCopy)
	);
	const workflowActions = $derived(reportsPath.steps);
	const currentAction = $derived(reportsPath.currentAction);
	const wavesHref = $derived(`/app/campaign-series/${workspace.series.id}/waves`);

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
			interpretation.isValidated ? 'reviewed' : 'not reviewed',
			interpretation.isOfficial ? 'official' : 'not official'
		].join(' / ');
	}

	async function viewReportProof() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				reportProof: 'Create or select a wave before reviewing results.'
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
				exportArtifact: 'Create or select a wave before creating a report export.'
			};
			return;
		}

		const result = await runAction(
			'exportArtifact',
			() => setupApi.createCampaignReportProofExport(selectedCampaign.id),
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
				responseExport:
					'Create or select a study before creating a response export.'
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
				fetchArtifact: 'Create or select an export file before reviewing it.'
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
					? 'Select a downloadable export file before downloading CSV.'
					: 'Create or select an export file before downloading CSV.'
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
					refreshWarning = 'Reports action saved, but the reports workspace refresh failed.';
				}
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, 'Reports action failed.')
			};
			return null;
		}
	}

	function workflowAction(id: SelectedSeriesReportsWorkflowActionId) {
		return workflowActions.find((action) => action.id === id) ?? workflowActions[0];
	}

	function isActionDisabled(id: SelectedSeriesReportsWorkflowActionId) {
		const action = workflowAction(id);
		return !action.available || actionStates[id] === 'submitting';
	}

	function stepLabel(state: StepState) {
		if (state === 'submitting') {
			return 'Working';
		}

		if (state === 'succeeded') {
			return 'Saved';
		}

		if (state === 'failed') {
			return 'Failed';
		}

		return 'Ready';
	}

	function pathStateLabel(state: SelectedSeriesReportsPathStepState) {
		if (state === 'done') {
			return 'Done';
		}

		if (state === 'current') {
			return 'Current';
		}

		return 'Blocked';
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? 'suppressed' : value.toFixed(2);
	}

	function humanize(value: string | null | undefined) {
		return value ? value.replaceAll('_', ' ') : 'Not available';
	}

	function csvPreview(content: string | null | undefined) {
		return (content ?? '').trim().split(/\r?\n/).slice(0, 6).join('\n');
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

<section class="product-panel" role="group" aria-label={reportsWorkflowCopy.surface.reviewActionsAria}>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Study flow · Results</p>
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

	<article class="questionnaire-blueprint-review questionnaire-blueprint-review--section" role="region" aria-label={reportsWorkflowCopy.surface.resultsUseReviewAria}>
		<div class="questionnaire-blueprint-review__header">
			<div>
				<p class="product-kicker">{reportsWorkflowCopy.surface.useDecisionLabel}</p>
				<h4 class="setup-current-task__title">{packetReview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{packetReview.description}</p>
			</div>
			<StatusBadge status={packetReview.status} />
		</div>
		<div class="questionnaire-blueprint-review__grid">
			{#each packetReview.items as item (item.id)}
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
			<span>{reportsWorkflowCopy.surface.nextActionLabel}</span>
			<span>{packetReview.primaryAction}</span>
		</p>
	</article>

	<article class="questionnaire-blueprint-review questionnaire-blueprint-review--section" role="region" aria-label={reportsWorkflowCopy.surface.scoreMethodReviewAria}>
		<div class="questionnaire-blueprint-review__header">
			<div>
				<p class="product-kicker">{reportsWorkflowCopy.surface.scoreMethodLabel}</p>
				<h4 class="setup-current-task__title">{methodReview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{methodReview.description}</p>
			</div>
			<StatusBadge status={methodReview.status} />
		</div>
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
		</div>
	</article>

	<article class="questionnaire-blueprint-review questionnaire-blueprint-review--section" role="region" aria-label={reportsWorkflowCopy.surface.exportPreviewAria}>
		<div class="questionnaire-blueprint-review__header">
			<div>
				<p class="product-kicker">{reportsWorkflowCopy.surface.exportPreviewLabel}</p>
				<h4 class="setup-current-task__title">{exportPreview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{exportPreview.description}</p>
			</div>
			<StatusBadge status={exportPreview.status} />
		</div>
		<div class="questionnaire-blueprint-review__grid">
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
			<span>Download action</span>
			<span>{exportPreview.downloadLabel}</span>
		</p>
	</article>

	<div class="setup-path" role="list" aria-label="Review and export path">
		{#each reportsPath.steps as action, index (action.id)}
			<div
				class="setup-path__item"
				data-state={action.pathState}
				role="listitem"
				aria-current={action.pathState === 'current' ? 'step' : undefined}
			>
				<span class="setup-path__marker" aria-hidden="true">{index + 1}</span>
				<div class="setup-path__content">
					<p class="setup-path__title">{action.title}</p>
					<p class="setup-path__description">{action.description}</p>
				</div>
				<span class="setup-path__state">{pathStateLabel(action.pathState)}</span>
			</div>
		{/each}
	</div>

	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">Read-only access</strong>
			<span>Review and export actions require workspace management access.</span>
		</p>
	{:else}
		<article class="record-row setup-current-task" role="region" aria-label="Current review task">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">
						{reportsPath.completedCount} of {reportsPath.totalCount} results tasks done
					</p>
					<h4 class="setup-current-task__title">Current results task</h4>
					<p class="record-row__title">{currentAction.title}</p>
					<p class="text-sm text-[var(--color-text-muted)]">{currentAction.description}</p>
				</div>
				<StatusBadge status={currentAction.status} />
			</div>
			{#if currentAction.disabledReason}
				<p class="text-sm text-[var(--color-text-muted)]">{currentAction.disabledReason}</p>
			{/if}

			<div class="setup-current-task__body">
				{#if currentAction.id === 'reportProof'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Selected wave</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Preview status</dt>
							<dd class="record-field__value">
								{selectedCampaign?.reportStatus === 'proof_only'
									? 'Ready for review'
									: 'Finish setup first'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Interpretation</dt>
							<dd class="record-field__value">
								{humanize(
									reportProofResult?.interpretationStatus ?? selectedCampaign?.interpretationStatus
								)}
							</dd>
						</div>
					</dl>
					<ReportWidgetsSection
						manifest={widgetManifest}
						warning={widgetWarning}
						embedded={true}
					/>
					{#if reportProofResult}
						{@render ReportProofResult()}
					{/if}
					{@render ActionFooter({
						id: 'reportProof',
						label: 'Review results',
						resultLabel: 'Preview',
						resultValue: reportProofResult ? 'Ready' : null,
						onclick: viewReportProof
					})}
				{:else if currentAction.id === 'exportArtifact'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Latest export</dt>
							<dd class="record-field__value">
								{exportResult?.fileName ??
									selectedCampaign?.latestExportArtifactFileName ??
									'Not available'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Export count</dt>
							<dd class="record-field__value">{selectedCampaign?.exportArtifactCount ?? 0}</dd>
						</div>
					</dl>
					{#if exportResult}
						{@render ExportArtifactResult({
							result: exportResult,
							ariaLabel: 'Report export result',
							kicker: 'Report export',
							title: 'Report-summary CSV and codebook'
						})}
					{/if}
					{@render ActionFooter({
						id: 'exportArtifact',
						label: 'Create report-summary export',
						resultLabel: 'Export file',
						resultValue:
							exportResult?.fileName ?? selectedCampaign?.latestExportArtifactFileName ?? null,
						onclick: createExportArtifact
					})}
				{:else if currentAction.id === 'responseExport'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Series</dt>
							<dd class="record-field__value">{workspace.series.name}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Latest response export</dt>
							<dd class="record-field__value">
								{responseExportResult?.fileName ??
									latestResponseExportArtifact?.fileName ??
									'Not available'}
							</dd>
						</div>
					</dl>
					{#if responseExportResult}
						{@render ExportArtifactResult({
							result: responseExportResult,
							ariaLabel: 'Response export result',
							kicker: 'Response export',
							title: 'Response CSV and codebook'
						})}
					{/if}
					{@render ActionFooter({
						id: 'responseExport',
						label: 'Create response export',
						resultLabel: 'Response file',
						resultValue:
							responseExportResult?.fileName ?? latestResponseExportArtifact?.fileName ?? null,
						onclick: createResponseExportArtifact
					})}
				{:else if currentAction.id === 'fetchArtifact'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Export file</dt>
							<dd class="record-field__value">{currentExportFileName ?? 'Not available'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Download status</dt>
							<dd class="record-field__value">
								{currentDownloadableExportArtifactId ? 'Downloadable' : 'Not ready'}
							</dd>
						</div>
					</dl>
					{#if storedExportResult}
						{@render StoredArtifactResult()}
					{/if}
					{@render ActionFooter({
						id: 'fetchArtifact',
						label: 'Review export file',
						resultLabel: 'Reviewed file',
						resultValue: storedExportResult?.fileName ?? currentExportFileName,
						onclick: fetchStoredArtifact
					})}
				{:else}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Download status</dt>
							<dd class="record-field__value">
								{currentDownloadableExportArtifactId ? 'Downloadable' : 'Not ready'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Latest file</dt>
							<dd class="record-field__value">
								{downloadResult?.fileName ??
									latestDownloadableExportArtifact?.fileName ??
									selectedCampaign?.latestExportArtifactFileName ??
									'Not available'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">File purpose</dt>
							<dd class="record-field__value">{currentExportPurpose}</dd>
						</div>
					</dl>
					{#if downloadResult}
						{@render CsvDownloadResult()}
					{/if}
					{@render ActionFooter({
						id: 'downloadCsv',
						label: currentAction.title,
						resultLabel: 'Downloaded file',
						resultValue:
							downloadResult?.fileName ??
							latestDownloadableExportArtifact?.fileName ??
							selectedCampaign?.latestExportArtifactFileName ??
							null,
						onclick: downloadCsv
					})}
				{/if}
			</div>
		</article>
	{/if}
</section>

{#snippet ReportProofResult()}
	{#if reportProofResult}
		<section class="score-result-panel report-proof-panel" aria-label="Report preview">
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">Results preview</p>
					<h4 class="record-row__title">Aggregate result preview</h4>
				</div>
				<StatusBadge status="ready" label="Internal preview" />
			</div>
			<div class="response-lab__meta">
				<span>Internal preview</span>
				<span>{humanize(reportProofResult.launchSnapshot.responseIdentityMode)} responses</span>
				<span>Minimum group {reportProofResult.disclosurePolicy.kMin}</span>
			</div>
			<div class="score-card-list" aria-label="Report preview scores">
				{#each reportProofResult.scores as score (score.dimensionCode)}
					{@const scoreMetadata =
						score.disclosure === 'visible'
							? formatScoreOutputMetadata(
									score.nValidTotal,
									score.nExpectedTotal,
									score.missingPolicyStatusSummary
								)
							: null}
					<article class="score-card" aria-label={`Report score ${score.dimensionCode}`}>
						<div>
							<p class="score-card__label">{score.dimensionCode}</p>
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
							scores={score.scoreCount ?? 'suppressed'}
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
				label={result.canDownload ? 'Downloadable' : 'Preparing'}
			/>
		</div>
		<div class="response-lab__meta">
			<span>Export file</span>
			<span>{result.canDownload ? 'Downloadable' : humanize(result.status)}</span>
			<span>{result.rowCount} rows</span>
		</div>
		<div class="score-result-panel__footer">
			{@render ResultLine({ label: 'File', value: result.fileName })}
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
				<dt class="record-field__label">Export file</dt>
				<dd class="record-field__value">{storedExportResult.fileName}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Rows</dt>
				<dd class="record-field__value">{storedExportResult.rowCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Download status</dt>
				<dd class="record-field__value">
					{storedExportResult.canDownload ? 'Downloadable' : humanize(storedExportResult.status)}
				</dd>
			</div>
		</dl>
	{/if}
{/snippet}

{#snippet CsvDownloadResult()}
	{#if downloadResult}
		<div class="response-lab__meta">
			<span>Downloaded CSV</span>
			<span>{downloadResult.contentType}</span>
			<span>{downloadResult.byteSize} bytes</span>
		</div>
		{@render ResultLine({ label: 'Downloaded file', value: downloadResult.fileName })}
		{#if csvPreview(downloadResult.content)}
			<pre class="csv-preview">{csvPreview(downloadResult.content)}</pre>
		{/if}
	{/if}
{/snippet}

{#snippet ActionFooter({
	id,
	label,
	resultLabel,
	resultValue,
	onclick
}: {
	id: SelectedSeriesReportsWorkflowActionId;
	label: string;
	resultLabel: string;
	resultValue: string | null | undefined;
	onclick: () => void | Promise<void>;
})}
	<div class="action-row">
		<button
			type="button"
			class="primary-button"
			disabled={isActionDisabled(id)}
			title={workflowAction(id).disabledReason ?? undefined}
			{onclick}
		>
			{#if actionStates[id] === 'submitting'}
				<LoaderCircle size={17} aria-hidden="true" />
			{:else if id === 'reportProof' || id === 'fetchArtifact'}
				<FileSearch size={17} aria-hidden="true" />
			{:else if id === 'downloadCsv'}
				<Download size={17} aria-hidden="true" />
			{:else}
				<Send size={17} aria-hidden="true" />
			{/if}
			<span>{label}</span>
		</button>
		<p class="step-pill" data-state={actionStates[id]}>{stepLabel(actionStates[id])}</p>
		{@render ResultLine({ label: resultLabel, value: resultValue })}
		{#if id === 'downloadCsv'}
			<a class="secondary-button" href={wavesHref}>Go to waves</a>
		{/if}
	</div>
	{#if actionErrors[id]}
		<p class="error-line">{actionErrors[id]}</p>
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
