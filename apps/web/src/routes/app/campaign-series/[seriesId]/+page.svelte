<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import type { CampaignSeriesHubResponse } from '$lib/api/product';
	import type { AuthSessionResponse } from '$lib/api/setup';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { Copy, LoaderCircle, RotateCcw } from 'lucide-svelte';
	import { onDestroy } from 'svelte';
	import {
		createProductApiFromEnv,
		createProductRequestGate,
		toSelectedSeriesErrorMessage
	} from '$lib/product/route-state';
	import {
		getProductAuthContext,
		hasProductPermission,
		setupManagePermission
	} from '$lib/product/auth-context';
	import { toCampaignSeriesHubView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();

	const seriesId = $derived(page.params.seriesId ?? '');
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(routePageCopy(appLocale).selectedStudy.overview);

	let loadState = $state<LoadState>('loading');
	let authSession = $state<AuthSessionResponse | null>(null);
	let campaignSeriesHub = $state<CampaignSeriesHubResponse | null>(null);
	let errorMessage = $state<string | null>(null);
	let duplicateActionInFlight = $state(false);
	let duplicateActionError = $state<string | null>(null);
	let restoringSeries = $state(false);
	let restoreError = $state<string | null>(null);

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const canManageSetup = $derived(hasProductPermission(authSession, setupManagePermission));
	const hubView = $derived(campaignSeriesHub ? toCampaignSeriesHubView(campaignSeriesHub, appLocale) : null);

	$effect(() => {
		void loadCampaignSeriesHub(seriesId);
	});

	async function loadCampaignSeriesHub(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId) {
			campaignSeriesHub = null;
			errorMessage = copy.missingId;
			loadState = 'error';
			return;
		}

		loadState = 'loading';
		campaignSeriesHub = null;
		errorMessage = null;

		try {
			const nextCampaignSeriesHub = await productApi.getCampaignSeriesHub(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			campaignSeriesHub = nextCampaignSeriesHub;
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			campaignSeriesHub = null;
			errorMessage = toSelectedSeriesErrorMessage(
				error,
				copy.unavailableFallback
			);
			loadState = 'error';
		}
	}

	async function restoreCampaignSeries() {
		if (!seriesId || restoringSeries) {
			return;
		}

		restoringSeries = true;
		restoreError = null;

		try {
			await productApi.restoreCampaignSeries(seriesId);
			await loadCampaignSeriesHub(seriesId);
		} catch (error) {
			restoreError = toSelectedSeriesErrorMessage(error, copy.restoreFailed);
		} finally {
			restoringSeries = false;
		}
	}

	async function duplicateCampaignSeries() {
		if (!seriesId || !hubView?.ownership.isSample || !canManageSetup || duplicateActionInFlight) {
			return;
		}

		duplicateActionInFlight = true;
		duplicateActionError = null;

		try {
			const created = await productApi.duplicateCampaignSeries(seriesId, {
				name: `Copy of ${hubView.title}`
			});
			await goto(resolve(`/app/campaign-series/${created.id}/setup`));
		} catch (error) {
			duplicateActionError = toSelectedSeriesErrorMessage(
				error,
				copy.duplicateFailed
			);
		} finally {
			duplicateActionInFlight = false;
		}
	}
</script>

<SurfaceHeader
	eyebrow={copy.eyebrow}
	title={copy.title}
	description={hubView?.overviewCommand.summary ?? copy.description}
	statusLabel={hubView?.overviewCommand.badgeLabel ?? 'Study workspace'}
	status={hubView?.overviewCommand.status ?? 'neutral'}
/>

<section class="product-stack" aria-label={copy.ariaLabel}>
	<LoadingBoundary loading={loadState === 'loading'} label={copy.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={copy.errorTitle}
				message={errorMessage}
				retryLabel={copy.retry}
				onRetry={() => loadCampaignSeriesHub()}
			/>
		{:else if hubView}
			<section class="product-panel" data-priority="primary" aria-label="Selected study summary">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{copy.selectedStudy}</p>
						<h2 class="product-title">{hubView.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">{hubView.subtitle}</p>
					</div>
					<div class="action-row">
						<StatusBadge status={hubView.ownership.badgeStatus} label={hubView.ownership.label} />
						<StatusBadge status={hubView.archiveState.status} label={hubView.archiveState.label} />
						{#if hubView.archiveState.archived && hubView.canMutate}
							<button
								type="button"
								class="secondary-button"
								disabled={restoringSeries}
								onclick={restoreCampaignSeries}
							>
								{#if restoringSeries}
									<LoaderCircle size={17} aria-hidden="true" />
								{:else}
									<RotateCcw size={17} aria-hidden="true" />
								{/if}
								<span>{restoringSeries ? copy.restoring : copy.restore}</span>
							</button>
						{/if}
					</div>
				</div>

				{#if hubView.ownership.readOnlyMessage}
					<div class="record-row" role="note" aria-label="Sample study read-only state">
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{hubView.ownership.label}</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									{hubView.ownership.readOnlyMessage}
								</p>
							</div>
							<div class="action-row">
								<StatusBadge status={hubView.ownership.badgeStatus} label={copy.readOnly} />
								{#if canManageSetup && hubView.ownership.isSample}
									<button
										type="button"
										class="secondary-button"
										aria-label={copy.duplicateAria(hubView.title)}
										disabled={duplicateActionInFlight}
										onclick={duplicateCampaignSeries}
									>
										{#if duplicateActionInFlight}
											<LoaderCircle size={17} aria-hidden="true" />
										{:else}
											<Copy size={17} aria-hidden="true" />
										{/if}
										<span>{duplicateActionInFlight ? copy.duplicating : copy.duplicateAsStudy}</span>
									</button>
								{/if}
							</div>
						</div>
					</div>
				{/if}

				{#if duplicateActionError}
					<p class="error-line" role="alert">{duplicateActionError}</p>
				{/if}

				{#if restoreError}
					<p class="error-line" role="alert">{restoreError}</p>
				{/if}

				<div class="overview-command-card" aria-label={hubView.overviewCommand.title}>
					<div class="overview-command-card__body">
						<div class="overview-command-card__header">
							<StatusBadge
								status={hubView.overviewCommand.status}
								label={hubView.overviewCommand.badgeLabel}
							/>
							<h3 class="overview-command-card__title">{hubView.overviewCommand.title}</h3>
						</div>
						<p class="overview-command-card__summary">{hubView.overviewCommand.summary}</p>
					</div>
					{#if hubView.overviewCommand.href && hubView.overviewCommand.actionLabel}
						<a class="primary-button" href={hubView.overviewCommand.href}>
							{hubView.overviewCommand.actionLabel}
						</a>
					{/if}
				</div>

				<dl class="overview-metric-grid" role="group" aria-label={copy.statusRecords}>
					{#each hubView.overviewMetrics as row}
						<div class="overview-metric-card">
							<dt class="overview-metric-card__label">{row.label}</dt>
							<dd class="overview-metric-card__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				{#if hubView.overviewAttentionItems.length > 0}
					<section class="overview-attention-list" aria-label={hubView.overviewAttentionTitle}>
						<p class="product-kicker">{hubView.overviewAttentionTitle}</p>
						{#each hubView.overviewAttentionItems as item}
							<article class="overview-attention-row" aria-label={item.label}>
								<div>
									<div class="overview-attention-row__header">
										<h4 class="record-row__title">{item.label}</h4>
										<StatusBadge status={item.status} label={item.badgeLabel} />
									</div>
									<p class="mt-1 text-sm text-[var(--color-text-muted)]">{item.summary}</p>
								</div>
								<a class="secondary-button" href={item.href}>{item.actionLabel}</a>
							</article>
						{/each}
					</section>
				{/if}

				{#if hubView.campaignRows.length > 0}
					<details class="record-row" open={hubView.campaignRows.length <= 2}>
						<summary class="record-row__title">
							{copy.campaigns} ({hubView.campaignRows.length})
						</summary>
						<div class="record-list mt-3">
							{#each hubView.campaignRows as campaign (campaign.id)}
								<article aria-label={campaign.title} class="record-row">
									<div class="record-row__header">
										<h4 class="record-row__title">{campaign.title}</h4>
										<StatusBadge status={campaign.status} />
									</div>
									<span class="record-grid">
										{#each campaign.rows as row}
											<span class="record-field">
												<span class="record-field__label">{row.label}</span>
												<span class="record-field__value">{row.value}</span>
											</span>
										{/each}
									</span>
								</article>
							{/each}
						</div>
					</details>
				{/if}

				{#if hubView.rows.length > 0}
					<details class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
						<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
							{copy.dates}
						</summary>
						<dl
							class="record-grid mt-3"
							role="group"
							aria-label={copy.datesAria}
						>
							{#each hubView.rows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value" class:font-mono={row.mono}>
										{row.value}
									</dd>
								</div>
							{/each}
						</dl>
					</details>
				{/if}
			</section>
		{/if}
	</LoadingBoundary>
</section>
