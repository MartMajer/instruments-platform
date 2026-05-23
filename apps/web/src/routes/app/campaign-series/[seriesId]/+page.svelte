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
	const hubView = $derived(campaignSeriesHub ? toCampaignSeriesHubView(campaignSeriesHub) : null);

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
	description={copy.description}
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

				<div
					role="group"
					aria-label={hubView.lifecycleMap.title}
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">{copy.lifecycle}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							{hubView.lifecycleMap.title}
						</h3>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{hubView.lifecycleMap.description}
						</p>
					</div>

					<div class="selected-lifecycle-list">
						{#each hubView.lifecycleMap.items as item}
							<article aria-label={item.label} class="selected-lifecycle-row">
								<div class="selected-lifecycle-row__body">
									<div class="record-row__header">
										<h4 class="record-row__title">{item.label}</h4>
										<StatusBadge status={item.status} />
									</div>
									<p class="text-sm leading-6 text-[var(--color-text)]">{item.description}</p>
									<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.guidance}</p>
								</div>
								<div class="selected-lifecycle-row__action">
									<a
										class="secondary-button"
										href={resolve(`/app/campaign-series/${seriesId}/${item.route}`)}
									>
										{item.actionLabel}
									</a>
								</div>
							</article>
						{/each}
					</div>
				</div>
			</section>

			<section class="product-panel" aria-label={hubView.referenceTitle}>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{copy.studyDetails}</p>
						<h2 class="product-title">{copy.statusRecords}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{copy.statusDescription}
						</p>
					</div>
				</div>

				<dl class="selected-reference-total-list" role="group" aria-label={copy.statusRecords}>
					{#each hubView.totalRows as row}
						<div class="selected-reference-total-row">
							<dt class="selected-reference-total-row__label">{row.label}</dt>
							<dd class="selected-reference-total-row__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

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

				<div
					role="group"
					aria-label={copy.governanceAria}
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">{copy.governance}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">{copy.policyScoring}</h3>
					</div>
					<div class="record-list">
						{#each hubView.governanceRows as row}
							<article class="record-row" aria-label={row.label}>
								<div class="record-row__header">
									<div>
										<p class="record-field__label">{row.label}</p>
										<p class="mt-2 text-sm text-[var(--color-text)]">{row.value}</p>
									</div>
									<StatusBadge status={row.status} />
								</div>
							</article>
						{/each}
					</div>
				</div>

				<div
					role="group"
					aria-label={copy.campaignsAria}
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">{copy.campaigns}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">{copy.campaignsInStudy}</h3>
					</div>

					{#if hubView.campaignRows.length === 0}
						<p class="text-sm text-[var(--color-text-muted)]">
							{copy.noCampaigns}
						</p>
					{:else}
						<div class="record-list">
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
					{/if}
				</div>
			</section>
		{/if}
	</LoadingBoundary>
</section>
