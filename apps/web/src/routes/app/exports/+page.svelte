<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { onMount } from 'svelte';
	import type { ExportArtifactLibraryResponse } from '$lib/api/product';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toExportArtifactLibraryView, toProductApiErrorMessage } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let library = $state<ExportArtifactLibraryResponse | null>(null);
	let errorMessage = $state<string | null>(null);

	const libraryView = $derived(library ? toExportArtifactLibraryView(library) : null);

	onMount(() => {
		void loadExportArtifacts();
	});

	async function loadExportArtifacts() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextLibrary = await productApi.listExportArtifacts();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			library = nextLibrary;
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			library = null;
			errorMessage = toProductApiErrorMessage(error, 'Export files could not be loaded.');
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow="Exports"
	title="Download files"
	description="Find CSV and codebook files created from study Results pages."
/>

<section class="product-panel" data-priority="primary" aria-label="Export workspace">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading export files">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Export files unavailable"
				message={errorMessage}
				retryLabel="Retry exports"
				onRetry={loadExportArtifacts}
			/>
		{:else if loadState === 'ready' && libraryView}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">Files</p>
						<h2 class="product-title">Downloadable files and next use</h2>
					</div>
				</div>

				<div class="export-overview-list" role="group" aria-label="Export overview">
					{#each libraryView.exportOverview as item}
						<article class="export-overview-row" aria-label={item.label}>
							<div class="export-overview-row__body">
								<div class="export-overview-row__header">
									<h3 class="export-overview-row__title">{item.label}</h3>
									<StatusBadge status={item.status} label={item.badgeLabel} />
								</div>
								<p class="mt-3 text-sm leading-6 font-semibold text-[var(--color-text)]">
									{item.summary}
								</p>
								<p class="mt-2 text-sm leading-6 text-[var(--color-text-muted)]">
									{item.guidance}
								</p>
								<dl class="record-grid mt-4" role="group" aria-label={`${item.label} details`}>
									{#each item.detailRows as row}
										<div class="record-field">
											<dt class="record-field__label">{row.label}</dt>
											<dd class="record-field__value">{row.value}</dd>
										</div>
									{/each}
								</dl>
							</div>
						</article>
					{/each}
				</div>

				{#if libraryView.cards.length === 0}
					<EmptyState
						title="No export files"
						description="Create an export from a study results page after results are available."
					/>
				{:else}
					<div class="record-list" aria-label="Export files">
						{#each libraryView.cards as card (card.id)}
							<article class="record-row" aria-label={card.title}>
								<span class="record-row__header">
									<span class="min-w-0">
										<span class="product-kicker">{card.purposeLabel}</span>
										<span class="record-row__title">{card.title}</span>
										<span class="block text-sm leading-6 text-[var(--color-text-muted)]">
											{card.subtitle}
										</span>
										<span class="mt-2 block text-sm leading-6 text-[var(--color-text)]">
											{card.nextUse}
										</span>
										<span class="mt-1 block text-sm leading-6 text-[var(--color-text-muted)]">
											{card.finalityLabel}
										</span>
									</span>
									<span class="flex items-center gap-2">
										<StatusBadge status={card.status} label={card.statusLabel} />
										{#if card.href}
											<a class="secondary-button" href={card.href}>Reports</a>
										{/if}
									</span>
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

				<details
					class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
				>
					<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
						Export counts
					</summary>
					<p class="mt-2 text-sm leading-6 text-[var(--color-text-muted)]">
						Use these counts when checking whether files are ready, pending, or failed.
					</p>
					<dl class="export-count-list mt-4" role="group" aria-label="Export file counts">
						{#each libraryView.metricRows as row}
							<div class="export-count-row">
								<dt class="export-count-row__label">{row.label}</dt>
								<dd class="export-count-row__value">{row.value}</dd>
							</div>
						{/each}
					</dl>
				</details>
			</div>
		{/if}
	</LoadingBoundary>
</section>
