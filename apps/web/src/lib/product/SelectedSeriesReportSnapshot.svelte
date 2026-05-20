<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { FileSearch, LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type { CampaignSeriesReportsWorkspaceResponse } from '$lib/api/product';
	import type { CampaignReportProofResponse } from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import VisualAnalyticsChart from './VisualAnalyticsChart.svelte';
	import {
		toSelectedSeriesReportDashboardView,
		toSelectedSeriesReportSnapshotState
	} from './report-snapshot';
	import { createSetupApiFromEnv } from './route-state';
	import { toReportVisualAnalyticsView } from './visual-analytics';
	import { toProductApiErrorMessage, toReportProofView } from './view-models';

	type LoadState = 'idle' | 'loading' | 'ready' | 'error';

	let { workspace }: { workspace: CampaignSeriesReportsWorkspaceResponse } = $props();

	const setupApi = createSetupApiFromEnv(env);

	let snapshotResult = $state<CampaignReportProofResponse | null>(null);
	let loadedCampaignId = $state<string | null>(null);
	let loadingCampaignId = $state<string | null>(null);
	let lastAttemptedCampaignId = $state<string | null>(null);
	let loadState = $state<LoadState>('idle');
	let errorMessage = $state<string | null>(null);
	let requestSequence = 0;

	const selectedCampaign = $derived(workspace.selectedCampaign);
	const snapshotLoadedForSelectedCampaign = $derived(
		Boolean(snapshotResult) &&
			Boolean(selectedCampaign) &&
			loadedCampaignId === selectedCampaign?.id &&
			snapshotResult?.campaignId === selectedCampaign?.id
	);
	const snapshotState = $derived(
		toSelectedSeriesReportSnapshotState(workspace, {
			loadedCampaignId: snapshotLoadedForSelectedCampaign ? loadedCampaignId : null
		})
	);
	const dashboardView = $derived(
		toSelectedSeriesReportDashboardView(workspace, {
			loadedCampaignId: snapshotLoadedForSelectedCampaign ? loadedCampaignId : null
		})
	);
	const snapshotView = $derived(
		snapshotLoadedForSelectedCampaign && snapshotResult ? toReportProofView(snapshotResult) : null
	);
	const visualAnalyticsView = $derived(
		snapshotLoadedForSelectedCampaign ? toReportVisualAnalyticsView(snapshotResult) : null
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
					: 'Local'
	);

	$effect(() => {
		const campaignId = selectedCampaign?.id ?? null;
		const reportable = selectedCampaign?.reportStatus === 'proof_only';

		if (!campaignId || !reportable) {
			snapshotResult = null;
			loadedCampaignId = null;
			loadingCampaignId = null;
			lastAttemptedCampaignId = null;
			errorMessage = null;
			loadState = 'idle';
			return;
		}

		if (
			loadedCampaignId !== campaignId &&
			loadingCampaignId !== campaignId &&
			lastAttemptedCampaignId !== campaignId
		) {
			void loadReportSnapshot(campaignId);
		}
	});

	async function loadReportSnapshot(campaignId: string | null = selectedCampaign?.id ?? null) {
		if (!campaignId) {
			errorMessage = 'Create or select a campaign before loading the report snapshot.';
			loadState = 'error';
			return;
		}

		if (!snapshotState.available) {
			errorMessage =
				snapshotState.disabledReason ??
				'Resolve report prerequisites before loading the report snapshot.';
			loadState = 'error';
			return;
		}

		const requestId = ++requestSequence;
		loadingCampaignId = campaignId;
		lastAttemptedCampaignId = campaignId;
		errorMessage = null;
		loadState = 'loading';

		try {
			const result = await setupApi.getCampaignReportProof(campaignId);
			if (requestId !== requestSequence || selectedCampaign?.id !== campaignId) {
				return;
			}

			snapshotResult = result;
			loadedCampaignId = campaignId;
			loadState = 'ready';
		} catch (error) {
			if (requestId !== requestSequence) {
				return;
			}

			snapshotResult = null;
			loadedCampaignId = null;
			errorMessage = toProductApiErrorMessage(error, 'Report snapshot could not be loaded.');
			loadState = 'error';
		} finally {
			if (requestId === requestSequence) {
				loadingCampaignId = null;
			}
		}
	}
</script>

<section class="product-panel" role="group" aria-label="Report snapshot">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Report snapshot</p>
			<h3 class="product-title">Selected-series report snapshot</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Aggregate preview for the selected report campaign.
			</p>
		</div>
		<StatusBadge status={snapshotBadgeStatus} label={snapshotBadgeLabel} />
	</div>

	<section
		role="group"
		aria-label="Report dashboard"
		class="grid gap-4 border-t border-[var(--color-border)] pt-4"
	>
		<div>
			<p class="product-kicker">Report dashboard</p>
			<h4 class="record-row__title">Selected-series report dashboard</h4>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Latest report preview for the selected campaign.
			</p>
		</div>

		{#if dashboardView.emptyMessage}
			<p class="record-row text-sm text-[var(--color-text-muted)]">
				<strong class="record-row__title">{dashboardView.title}</strong>
				<span>{dashboardView.emptyMessage}</span>
			</p>
		{/if}

		<div role="group" aria-label="Report readiness" class="grid gap-3">
			<div>
				<p class="product-kicker">Readiness</p>
				<h5 class="text-base font-semibold text-[var(--color-text)]">Report readiness</h5>
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

		{#if dashboardView.disclosureRows.length > 0}
			<div
				role="group"
				aria-label="Disclosure guardrails"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Disclosure</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">
						Disclosure guardrails
					</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.disclosureRows as row}
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
				aria-label="Report provenance"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Provenance</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">Report provenance</h5>
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

		{#if dashboardView.exportRows.length > 0}
			<div
				role="group"
				aria-label="Export readiness"
				class="grid gap-3 border-t border-[var(--color-border)] pt-4"
			>
				<div>
					<p class="product-kicker">Exports</p>
					<h5 class="text-base font-semibold text-[var(--color-text)]">Export readiness</h5>
				</div>
				<dl class="record-grid">
					{#each dashboardView.exportRows as row}
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

		<div
			role="group"
			aria-label="Export files"
			class="grid gap-3 border-t border-[var(--color-border)] pt-4"
		>
			<div>
				<p class="product-kicker">Exports</p>
				<h5 class="text-base font-semibold text-[var(--color-text)]">Export files</h5>
			</div>
			{#if dashboardView.artifactRegistry.length > 0}
				<div class="record-list">
					{#each dashboardView.artifactRegistry as artifact (artifact.id)}
						<article class="record-row" aria-label={`Export file ${artifact.title}`}>
							<div class="record-row__header">
								<div>
									<p class="record-field__label">{artifact.targetLabel}</p>
									<h6 class="record-row__title">{artifact.title}</h6>
								</div>
								<StatusBadge
									status={artifact.badgeLabel === 'succeeded' ? 'ready' : 'pending'}
									label={artifact.badgeLabel}
								/>
							</div>
							<div class="response-lab__meta">
								{#each artifact.meta as item}
									<span>{item}</span>
								{/each}
							</div>
							<dl class="record-grid">
								{#each artifact.rows as row}
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
						</article>
					{/each}
				</div>
			{:else if dashboardView.available}
				<p class="record-row text-sm text-[var(--color-text-muted)]">
					<strong class="record-row__title">No stored export files yet</strong>
					<span>Create a report export before downloading a file.</span>
				</p>
			{/if}
		</div>

		{#if snapshotState.disabledReason}
			<p class="text-sm text-[var(--color-text-muted)]">{snapshotState.disabledReason}</p>
		{/if}

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!snapshotState.available || loadState === 'loading'}
				title={snapshotState.disabledReason ?? undefined}
				onclick={() => loadReportSnapshot()}
			>
				{#if loadState === 'loading'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else if snapshotView}
					<RefreshCw size={17} aria-hidden="true" />
				{:else}
					<FileSearch size={17} aria-hidden="true" />
				{/if}
				<span>{loadState === 'loading' ? 'Loading snapshot' : 'Refresh report snapshot'}</span>
			</button>
			<p class="step-pill" data-state={snapshotStepState}>{snapshotStepLabel}</p>
			{#if snapshotState.campaignId}
				<p class="result-line">
					<span>Campaign</span>
					<code>{snapshotState.campaignId}</code>
				</p>
			{/if}
		</div>

		{#if errorMessage}
			<p class="error-line">{errorMessage}</p>
		{/if}

		{#if snapshotView}
			{#if visualAnalyticsView}
				<VisualAnalyticsChart
					view={visualAnalyticsView}
					testId="report-visual-analytics-chart"
				/>
			{/if}

			<section class="score-result-panel report-proof-panel" aria-label="Aggregate report snapshot">
				<div class="score-result-panel__header">
					<div>
						<p class="product-kicker">Report preview</p>
						<h4 class="record-row__title">{snapshotView.campaignName}</h4>
					</div>
					<StatusBadge status="proof_only" label="Preview" />
				</div>

				<div class="response-lab__meta">
					<span>{snapshotView.proofStatus}</span>
					<span>{snapshotView.campaignId}</span>
					<span>{snapshotResult?.interpretationStatus}</span>
				</div>

				<dl class="record-grid">
					{#each snapshotView.provenance as row}
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

				<section
					class="record-row border-t border-[var(--color-border)] pt-4"
					aria-label="Preliminary report summary"
				>
					<div>
						<p class="product-kicker">Preliminary summary</p>
						<h5 class="record-row__title">{snapshotView.summary.title}</h5>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text)]">
							{snapshotView.summary.headline}
						</p>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
							{snapshotView.summary.detail}
						</p>
					</div>

					<dl class="record-grid">
						{#each snapshotView.summary.metrics as metric}
							<div class="record-field">
								<dt class="record-field__label">{metric.label}</dt>
								<dd class="record-field__value">{metric.value}</dd>
							</div>
						{/each}
					</dl>

					<ul class="grid gap-2 text-sm leading-6 text-[var(--color-text-muted)]">
						{#each snapshotView.summary.guardrails as guardrail}
							<li>{guardrail}</li>
						{/each}
					</ul>
				</section>

				<div class="score-card-list" role="region" aria-label="Report score rows">
					{#each snapshotView.scoreRows as score (score.dimensionCode)}
						<article class="score-card" aria-label={`Report score ${score.dimensionCode}`}>
							<div>
								<p class="score-card__label">{score.dimensionCode}</p>
								<p
									class={score.disclosureState === 'visible'
										? 'score-card__value'
										: 'score-card__interpretation'}
								>
									{score.mean}
								</p>
							</div>
							<p class="score-card__meta">{score.disclosureState}</p>
							<p class="score-card__interpretation">mean {score.mean}</p>
							<p class="score-card__interpretation">scores {score.scoreCount}</p>
							{#if score.scoreMetadata}
								<p class="score-card__interpretation">{score.scoreMetadata}</p>
							{/if}
							<p class="score-card__interpretation">range {score.range}</p>
							{#if score.interpretationLabel}
								<p class="score-card__interpretation">{score.interpretationLabel}</p>
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
