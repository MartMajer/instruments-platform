<script lang="ts">
	import { page } from '$app/state';
	import { env } from '$env/dynamic/public';
	import { GitCompareArrows, LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
	import type { CampaignSeriesWaveComparisonProofResponse } from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import VisualAnalyticsChart from './VisualAnalyticsChart.svelte';
	import {
		toSelectedSeriesWaveComparisonSnapshotState,
		toSelectedSeriesWaveDashboardView
	} from './wave-comparison-snapshot';
	import { createSetupApiFromEnv } from './route-state';
	import { toWaveVisualAnalyticsView } from './visual-analytics';
	import { toProductApiErrorMessage, toWaveComparisonView } from './view-models';

	type LoadState = 'idle' | 'loading' | 'ready' | 'error';

	let {
		workspace,
		embedded = false
	}: { workspace: CampaignSeriesWavesWorkspaceResponse; embedded?: boolean } = $props();

	const setupApi = createSetupApiFromEnv(env);
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const waveSnapshotCopy = $derived(routePageCopy(appLocale).selectedStudy.waveSnapshot);

	let snapshotResult = $state<CampaignSeriesWaveComparisonProofResponse | null>(null);
	let loadedSeriesId = $state<string | null>(null);
	let loadingSeriesId = $state<string | null>(null);
	let lastAttemptedSeriesId = $state<string | null>(null);
	let loadState = $state<LoadState>('idle');
	let errorMessage = $state<string | null>(null);
	let requestSequence = 0;

	const snapshotLoadedForSelectedSeries = $derived(
		Boolean(snapshotResult) &&
			loadedSeriesId === workspace.series.id &&
			snapshotResult?.campaignSeriesId === workspace.series.id
	);
	const snapshotState = $derived(
		toSelectedSeriesWaveComparisonSnapshotState(
			workspace,
			{
				loadedSeriesId: snapshotLoadedForSelectedSeries ? loadedSeriesId : null
			},
			waveSnapshotCopy
		)
	);
	const dashboardView = $derived(
		toSelectedSeriesWaveDashboardView(
			workspace,
			{
				loadedSeriesId: snapshotLoadedForSelectedSeries ? loadedSeriesId : null
			},
			waveSnapshotCopy
		)
	);
	const snapshotView = $derived(
		snapshotLoadedForSelectedSeries && snapshotResult ? toWaveComparisonView(snapshotResult) : null
	);
	const visualAnalyticsView = $derived(
		snapshotLoadedForSelectedSeries ? toWaveVisualAnalyticsView(snapshotResult, appLocale) : null
	);
	const snapshotBadgeStatus = $derived(loadState === 'error' ? 'failed' : snapshotState.status);
	const snapshotBadgeLabel = $derived(
		loadState === 'error' ? waveSnapshotCopy.status.failed : snapshotState.badgeLabel
	);
	const snapshotStepState = $derived(loadState === 'error' ? 'failed' : loadState);
	const snapshotStepLabel = $derived(
		loadState === 'loading'
			? waveSnapshotCopy.status.loading
			: loadState === 'ready'
				? waveSnapshotCopy.status.ready
				: loadState === 'error'
					? waveSnapshotCopy.status.failed
					: waveSnapshotCopy.status.ready
	);

	$effect(() => {
		const seriesId = workspace.series.id;

		if (!snapshotState.available) {
			snapshotResult = null;
			loadedSeriesId = null;
			loadingSeriesId = null;
			lastAttemptedSeriesId = null;
			errorMessage = null;
			loadState = 'idle';
			return;
		}

		if (
			loadedSeriesId !== seriesId &&
			loadingSeriesId !== seriesId &&
			lastAttemptedSeriesId !== seriesId
		) {
			void loadWaveComparisonSnapshot(seriesId);
		}
	});

	async function loadWaveComparisonSnapshot(seriesId: string = workspace.series.id) {
		if (!snapshotState.available) {
			errorMessage =
				snapshotState.disabledReason ??
				waveSnapshotCopy.chrome.resolvePrerequisites;
			loadState = 'error';
			return;
		}

		const requestId = ++requestSequence;
		loadingSeriesId = seriesId;
		lastAttemptedSeriesId = seriesId;
		errorMessage = null;
		loadState = 'loading';

		try {
			const result = await setupApi.getCampaignSeriesWaveComparisonProof(seriesId);
			if (requestId !== requestSequence || workspace.series.id !== seriesId) {
				return;
			}

			snapshotResult = result;
			loadedSeriesId = seriesId;
			loadState = 'ready';
		} catch (error) {
			if (requestId !== requestSequence) {
				return;
			}

			snapshotResult = null;
			loadedSeriesId = null;
			errorMessage = toProductApiErrorMessage(
				error,
				waveSnapshotCopy.chrome.loadFailed
			);
			loadState = 'error';
		} finally {
			if (requestId === requestSequence) {
				loadingSeriesId = null;
			}
		}
	}
</script>

<section
	class={embedded ? 'score-result-panel report-proof-panel' : 'product-panel'}
	role="group"
	aria-label={waveSnapshotCopy.chrome.sectionAria}
>
	<div class={embedded ? 'score-result-panel__header' : 'product-panel__header'}>
		<div>
			<p class="product-kicker">{waveSnapshotCopy.chrome.kicker}</p>
			<h3 class={embedded ? 'record-row__title' : 'product-title'}>{waveSnapshotCopy.chrome.title}</h3>
			{#if !embedded}
				<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
					{waveSnapshotCopy.chrome.description}
				</p>
			{/if}
		</div>
		<StatusBadge status={snapshotBadgeStatus} label={snapshotBadgeLabel} />
	</div>

	<section
		role="group"
		aria-label={waveSnapshotCopy.chrome.summaryAria}
		class="grid gap-4 border-t border-[var(--color-border)] pt-4"
	>
		<div>
			<p class="product-kicker">{waveSnapshotCopy.chrome.readinessKicker}</p>
			<h4 class="record-row__title">{waveSnapshotCopy.chrome.readinessTitle}</h4>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				{waveSnapshotCopy.chrome.readinessDescription}
			</p>
		</div>

		{#if dashboardView.emptyMessage}
			<p class="record-row text-sm text-[var(--color-text-muted)]">
				<strong class="record-row__title">{dashboardView.title}</strong>
				<span>{dashboardView.emptyMessage}</span>
			</p>
		{/if}

		<div role="group" aria-label={waveSnapshotCopy.chrome.waveReadinessAria} class="grid gap-3">
			<div>
				<p class="product-kicker">{waveSnapshotCopy.chrome.waveReadinessKicker}</p>
				<h5 class="text-base font-semibold text-[var(--color-text)]">
					{waveSnapshotCopy.chrome.waveReadinessTitle}
				</h5>
			</div>
			<dl class="record-grid">
				{#each dashboardView.readinessRows.filter((row) => !row.mono) as row}
					<div class="record-field">
						<dt class="record-field__label">{row.label}</dt>
						<dd class="record-field__value">{row.value}</dd>
					</div>
				{/each}
			</dl>
		</div>

		{#if dashboardView.comparisonRows.length > 0}
			<div
				role="group"
				aria-label={waveSnapshotCopy.chrome.comparisonAria}
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">{waveSnapshotCopy.chrome.comparisonKicker}</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">
						{waveSnapshotCopy.chrome.comparisonTitle}
					</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.comparisonRows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</div>
		{/if}

		{#if dashboardView.guardrailRows.length > 0}
			<div
				role="group"
				aria-label={waveSnapshotCopy.chrome.guardrailsAria}
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">{waveSnapshotCopy.chrome.guardrailsKicker}</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">
						{waveSnapshotCopy.chrome.guardrailsTitle}
					</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.guardrailRows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</div>
		{/if}

		{#if dashboardView.provenanceRows.length > 0}
			<div
				role="group"
				aria-label={waveSnapshotCopy.chrome.sourceAria}
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">{waveSnapshotCopy.chrome.sourceKicker}</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">
						{waveSnapshotCopy.chrome.sourceTitle}
					</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.provenanceRows.filter((row) => !row.mono) as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</div>
		{/if}

		{#if snapshotState.disabledReason}
			<p class="text-sm text-[var(--color-text-muted)]">{snapshotState.disabledReason}</p>
		{/if}

		{#if !embedded}
			<div class="action-row">
				<button
					type="button"
					class="primary-button"
					disabled={!snapshotState.available || loadState === 'loading'}
					title={snapshotState.disabledReason ?? undefined}
					onclick={() => loadWaveComparisonSnapshot()}
				>
					{#if loadState === 'loading'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else if snapshotView}
						<RefreshCw size={17} aria-hidden="true" />
					{:else}
						<GitCompareArrows size={17} aria-hidden="true" />
					{/if}
					<span
						>{loadState === 'loading'
							? waveSnapshotCopy.chrome.loadingComparison
							: waveSnapshotCopy.chrome.refreshComparison}</span
					>
				</button>
				<p class="step-pill" data-state={snapshotStepState}>{snapshotStepLabel}</p>
				<p class="result-line">
					<span>{waveSnapshotCopy.chrome.study}</span>
					<span>{workspace.series.name}</span>
				</p>
			</div>
		{/if}

		{#if errorMessage}
			<p class="error-line">{errorMessage}</p>
		{/if}

		{#if snapshotView}
			{#if visualAnalyticsView}
				<VisualAnalyticsChart view={visualAnalyticsView} testId="wave-visual-analytics-chart" />
			{/if}

			<section
				class="score-result-panel report-proof-panel"
				aria-label={waveSnapshotCopy.chrome.aggregateSnapshotAria}
			>
				<div class="score-result-panel__header">
					<div>
						<p class="product-kicker">{waveSnapshotCopy.chrome.kicker}</p>
						<h4 class="record-row__title">{waveSnapshotCopy.chrome.changeOverTimeTitle}</h4>
					</div>
					<StatusBadge status="ready" label={waveSnapshotCopy.chrome.comparisonReady} />
				</div>

				<div class="response-lab__meta">
					<span>{snapshotResult?.interpretationStatus}</span>
					<span>{waveSnapshotCopy.chrome.completeTrajectories(workspace.summary.completeTrajectoryCount)}</span>
					<span>{waveSnapshotCopy.chrome.linkedPairs(workspace.comparison.linkedPairCount)}</span>
				</div>

				<dl class="record-grid">
					{#each snapshotView.summaryRows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				<div class="score-card-list" role="region" aria-label={waveSnapshotCopy.chrome.waveComparisonRowsAria}>
					{#each snapshotView.scoreRows as score (score.dimensionCode)}
						<article class="score-card" aria-label={waveSnapshotCopy.chrome.waveComparisonScoreAria(score.dimensionCode)}>
							<div>
								<p class="score-card__label">{score.dimensionCode}</p>
								<p
									class={score.disclosureState === 'visible'
										? 'score-card__value'
										: 'score-card__interpretation'}
								>
									{score.aggregateDelta}
								</p>
							</div>
							<p class="score-card__meta">{score.compatibilityStatus}</p>
							<p class="score-card__interpretation">{score.disclosureState}</p>
							<p class="score-card__interpretation">{waveSnapshotCopy.chrome.baselineMean(score.baselineMean)}</p>
							<p class="score-card__interpretation">{waveSnapshotCopy.chrome.comparisonMean(score.comparisonMean)}</p>
							<p class="score-card__interpretation">{waveSnapshotCopy.chrome.linkedPairs(score.linkedPairCount)}</p>
							{#if score.baselineScoreMetadata}
								<p class="score-card__interpretation">{waveSnapshotCopy.chrome.baselineMeta(score.baselineScoreMetadata)}</p>
							{/if}
							{#if score.comparisonScoreMetadata}
								<p class="score-card__interpretation">{waveSnapshotCopy.chrome.comparisonMeta(score.comparisonScoreMetadata)}</p>
							{/if}
							<p class="score-card__interpretation">{waveSnapshotCopy.chrome.aggregateDelta(score.aggregateDelta)}</p>
							<p class="score-card__interpretation">{waveSnapshotCopy.chrome.pairedDelta(score.pairedDeltaMean)}</p>
							{#if score.baselineInterpretationLabel}
								<p class="score-card__interpretation">
									{waveSnapshotCopy.chrome.baselineBand(score.baselineInterpretationLabel)}
								</p>
							{/if}
							{#if score.comparisonInterpretationLabel}
								<p class="score-card__interpretation">
									{waveSnapshotCopy.chrome.comparisonBand(score.comparisonInterpretationLabel)}
								</p>
							{/if}
							{#if score.interpretationMeta}
								<p class="score-card__interpretation">{score.interpretationMeta}</p>
							{/if}
							{#if score.note}
								<p class="score-card__interpretation">{score.note}</p>
							{/if}
						</article>
					{/each}
				</div>
			</section>
		{/if}
	</section>
</section>
