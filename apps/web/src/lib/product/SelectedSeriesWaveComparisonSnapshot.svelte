<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { GitCompareArrows, LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
	import type { CampaignSeriesWaveComparisonProofResponse } from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import VisualAnalyticsChart from './VisualAnalyticsChart.svelte';
	import {
		toSelectedSeriesWaveComparisonSnapshotState,
		toSelectedSeriesWaveDashboardView
	} from './wave-comparison-snapshot';
	import { createSetupApiFromEnv } from './route-state';
	import { toWaveVisualAnalyticsView } from './visual-analytics';
	import { toProductApiErrorMessage, toWaveComparisonView } from './view-models';

	type LoadState = 'idle' | 'loading' | 'ready' | 'error';

	let { workspace }: { workspace: CampaignSeriesWavesWorkspaceResponse } = $props();

	const setupApi = createSetupApiFromEnv(env);

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
		toSelectedSeriesWaveComparisonSnapshotState(workspace, {
			loadedSeriesId: snapshotLoadedForSelectedSeries ? loadedSeriesId : null
		})
	);
	const dashboardView = $derived(
		toSelectedSeriesWaveDashboardView(workspace, {
			loadedSeriesId: snapshotLoadedForSelectedSeries ? loadedSeriesId : null
		})
	);
	const snapshotView = $derived(
		snapshotLoadedForSelectedSeries && snapshotResult ? toWaveComparisonView(snapshotResult) : null
	);
	const visualAnalyticsView = $derived(
		snapshotLoadedForSelectedSeries ? toWaveVisualAnalyticsView(snapshotResult) : null
	);
	const snapshotBadgeStatus = $derived(loadState === 'error' ? 'failed' : snapshotState.status);
	const snapshotBadgeLabel = $derived(loadState === 'error' ? 'Failed' : snapshotState.badgeLabel);
	const snapshotStepState = $derived(loadState === 'error' ? 'failed' : loadState);
	const snapshotStepLabel = $derived(
		loadState === 'loading'
			? 'Loading'
			: loadState === 'ready'
				? 'Ready'
				: loadState === 'error'
					? 'Failed'
					: 'Ready'
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
				'Resolve wave comparison prerequisites before loading the snapshot.';
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
				'Wave comparison snapshot could not be loaded.'
			);
			loadState = 'error';
		} finally {
			if (requestId === requestSequence) {
				loadingSeriesId = null;
			}
		}
	}
</script>

<section class="product-panel" role="group" aria-label="Wave comparison snapshot">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Wave dashboard</p>
			<h3 class="product-title">Wave comparison snapshot</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Disclosure-safe comparison for the selected baseline and comparison waves.
			</p>
		</div>
		<StatusBadge status={snapshotBadgeStatus} label={snapshotBadgeLabel} />
	</div>

	<section
		role="group"
		aria-label="Wave dashboard"
		class="grid gap-4 border-t border-[var(--color-border)] pt-4"
	>
		<div>
			<p class="product-kicker">Wave dashboard</p>
			<h4 class="record-row__title">Selected-series wave dashboard</h4>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Latest governed wave comparison preview for the selected series.
			</p>
		</div>

		{#if dashboardView.emptyMessage}
			<p class="record-row text-sm text-[var(--color-text-muted)]">
				<strong class="record-row__title">{dashboardView.title}</strong>
				<span>{dashboardView.emptyMessage}</span>
			</p>
		{/if}

		<div role="group" aria-label="Wave readiness" class="grid gap-3">
			<div>
				<p class="product-kicker">Readiness</p>
				<h5 class="text-base font-semibold text-[var(--color-text)]">Wave readiness</h5>
			</div>
			<dl class="record-grid">
				{#each dashboardView.readinessRows as row}
					<div class="record-field">
						<dt class="record-field__label">{row.label}</dt>
						<dd class="record-field__value">
							{#if row.mono}
								<code>{row.value}</code>
							{:else}
								{row.value}
							{/if}
						</dd>
					</div>
				{/each}
			</dl>
		</div>

		{#if dashboardView.comparisonRows.length > 0}
			<div
				role="group"
				aria-label="Comparison status"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Comparison</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">Comparison status</h5>
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
				aria-label="Disclosure and compatibility"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Guardrails</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">
						Disclosure and compatibility
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
				aria-label="Wave provenance"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Provenance</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">Wave provenance</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.provenanceRows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">
								{#if row.mono}
									<code>{row.value}</code>
								{:else}
									{row.value}
								{/if}
							</dd>
						</div>
					{/each}
				</dl>
			</div>
		{/if}

		{#if snapshotState.disabledReason}
			<p class="text-sm text-[var(--color-text-muted)]">{snapshotState.disabledReason}</p>
		{/if}

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
						? 'Loading snapshot'
						: 'Refresh wave comparison snapshot'}</span
				>
			</button>
			<p class="step-pill" data-state={snapshotStepState}>{snapshotStepLabel}</p>
			<p class="result-line">
				<span>Study</span>
				<span>{workspace.series.name}</span>
			</p>
		</div>

		{#if errorMessage}
			<p class="error-line">{errorMessage}</p>
		{/if}

		{#if snapshotView}
			{#if visualAnalyticsView}
				<VisualAnalyticsChart view={visualAnalyticsView} testId="wave-visual-analytics-chart" />
			{/if}

			<section
				class="score-result-panel report-proof-panel"
				aria-label="Aggregate wave comparison snapshot"
			>
				<div class="score-result-panel__header">
					<div>
						<p class="product-kicker">Wave comparison</p>
						<h4 class="record-row__title">Disclosure-gated wave comparison</h4>
					</div>
					<StatusBadge status="ready" label="Ready" />
				</div>

				<div class="response-lab__meta">
					<span>{snapshotResult?.interpretationStatus}</span>
					<span>complete trajectories {workspace.summary.completeTrajectoryCount}</span>
					<span>linked pairs {workspace.comparison.linkedPairCount}</span>
				</div>

				<dl class="record-grid">
					{#each snapshotView.summaryRows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				<div class="score-card-list" role="region" aria-label="Wave comparison rows">
					{#each snapshotView.scoreRows as score (score.dimensionCode)}
						<article class="score-card" aria-label={`Wave comparison ${score.dimensionCode}`}>
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
							<p class="score-card__interpretation">baseline mean {score.baselineMean}</p>
							<p class="score-card__interpretation">comparison mean {score.comparisonMean}</p>
							<p class="score-card__interpretation">linked pairs {score.linkedPairCount}</p>
							{#if score.baselineScoreMetadata}
								<p class="score-card__interpretation">baseline {score.baselineScoreMetadata}</p>
							{/if}
							{#if score.comparisonScoreMetadata}
								<p class="score-card__interpretation">comparison {score.comparisonScoreMetadata}</p>
							{/if}
							<p class="score-card__interpretation">aggregate delta {score.aggregateDelta}</p>
							<p class="score-card__interpretation">paired delta {score.pairedDeltaMean}</p>
							{#if score.baselineInterpretationLabel}
								<p class="score-card__interpretation">
									baseline band {score.baselineInterpretationLabel}
								</p>
							{/if}
							{#if score.comparisonInterpretationLabel}
								<p class="score-card__interpretation">
									comparison band {score.comparisonInterpretationLabel}
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
