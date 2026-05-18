<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { onMount } from 'svelte';
	import type { InstrumentSummaryResponse } from '$lib/api/setup';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import RouteGuidancePanel from '$lib/components/RouteGuidancePanel.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { toProductRouteGuidance } from '$lib/product/route-guidance';
	import { createProductRequestGate, createSetupApiFromEnv } from '$lib/product/route-state';
	import { toInstrumentLibraryView, toProductApiErrorMessage } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const setupApi = createSetupApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let instruments = $state<InstrumentSummaryResponse[]>([]);
	let errorMessage = $state<string | null>(null);

	const libraryView = $derived(toInstrumentLibraryView(instruments));
	const routeGuidance = $derived(
		toProductRouteGuidance('instruments', {
			isEmpty: loadState === 'ready' && libraryView.cards.length === 0
		})
	);

	onMount(() => {
		void loadInstruments();
	});

	async function loadInstruments() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextInstruments = await setupApi.listInstruments();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			instruments = nextInstruments;
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			instruments = [];
			errorMessage = toProductApiErrorMessage(error, 'Instrument library could not be loaded.');
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow="Instrument library"
	title="Instruments"
	description="Tenant-visible instrument assets and launch eligibility."
/>

<RouteGuidancePanel guidance={routeGuidance} />

<section class="product-panel" data-priority="primary" aria-label="Instrument library">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading instrument library">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Instrument library unavailable"
				message={errorMessage}
				retryLabel="Retry instruments"
				onRetry={loadInstruments}
			/>
		{:else if loadState === 'ready'}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">Library summary</p>
						<h2 class="product-title">Visible instruments</h2>
					</div>
				</div>

				<dl class="instrument-count-list" role="group" aria-label="Instrument library counts">
					{#each libraryView.metricRows as row}
						<div class="instrument-count-row">
							<dt class="instrument-count-row__label">{row.label}</dt>
							<dd class="instrument-count-row__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				{#if libraryView.cards.length === 0}
					<EmptyState
						title="No instruments"
						description="No tenant-visible instruments are available yet."
					/>
				{:else}
					<div class="record-list" aria-label="Visible instruments">
						{#each libraryView.cards as card (card.id)}
							<article class="record-row" aria-label={card.title}>
								<span class="record-row__header">
									<span class="min-w-0">
										<span class="record-row__title">{card.title}</span>
										<span class="block text-sm leading-6 text-[var(--color-text-muted)]">
											{card.subtitle}
										</span>
									</span>
									<StatusBadge status={card.status} label={card.statusLabel} />
								</span>
								<dl class="record-grid" role="group" aria-label={`${card.title} metadata`}>
									{#each card.rows as row}
										<div class="record-field">
											<dt class="record-field__label">{row.label}</dt>
											<dd class="record-field__value">{row.value}</dd>
										</div>
									{/each}
								</dl>
							</article>
						{/each}
					</div>
				{/if}

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">Management</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Campaign setup surfaces
						</h3>
					</div>
					<div class="record-list" aria-label="Instrument management links">
						<a class="record-row" href="/app/campaign-series">
							<span class="record-row__header">
								<span class="record-row__title">Campaign series</span>
								<span class="secondary-button">Open</span>
							</span>
							<span class="text-sm leading-6 text-[var(--color-text-muted)]">
								Select a campaign series to configure templates, scoring, and launch state.
							</span>
						</a>
					</div>
				</div>
			</div>
		{/if}
	</LoadingBoundary>
</section>
