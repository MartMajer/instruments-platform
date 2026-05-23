<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import type { TenantSettingsWorkspaceResponse } from '$lib/api/product';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toProductApiErrorMessage, toTenantSettingsView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let errorMessage = $state<string | null>(null);

	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const settingsView = $derived(settings ? toTenantSettingsView(settings) : null);

	onMount(() => {
		void loadTenantSettings();
	});

	async function loadTenantSettings() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextSettings = await productApi.getTenantSettings();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			settings = nextSettings;
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			settings = null;
			errorMessage = toProductApiErrorMessage(error, text.settings.errorTitle);
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow={text.settings.eyebrow}
	title={text.settings.title}
	description={text.settings.description}
/>

<section class="product-panel" data-priority="primary" aria-label={text.settings.title}>
	<LoadingBoundary loading={loadState === 'loading'} label={text.settings.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.settings.errorTitle}
				message={errorMessage}
				retryLabel={text.settings.retry}
				onRetry={loadTenantSettings}
			/>
		{:else if settingsView}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{text.settings.hub}</p>
						<h2 class="product-title">{text.settings.whatManage}</h2>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.whatManageBody}
						</p>
					</div>
					<StatusBadge status={settingsView.status} />
				</div>

				<div class="record-list" aria-label={text.settings.shortcutsAria}>
					<a class="record-row" href="/app/team">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.teamAccess}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.teamBody}
						</span>
					</a>
					<a class="record-row" href="/app/directory">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.directoryShortcut}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.directoryBody}
						</span>
					</a>
					<a class="record-row" href="/app/campaign-series">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.studySetup}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.studySetupBody}
						</span>
					</a>
					<a class="record-row" href="/app/exports">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.exportsShortcut}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.exportsBody}
						</span>
					</a>
				</div>

				<details class="record-row" aria-label={text.settings.workspaceDetailsAria}>
					<summary class="record-row__title">{text.settings.workspaceDetails}</summary>
					<div class="grid gap-4 pt-4">
						<div>
							<p class="product-kicker">{text.settings.profile}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{settingsView.title}
							</h3>
						</div>

						<dl class="record-grid" role="group" aria-label={text.settings.profileDetailsAria}>
							{#each settingsView.profileRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>

						<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
							<div>
								<p class="product-kicker">{text.settings.scale}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{text.settings.footprint}
								</h3>
							</div>

							<dl class="settings-count-list" role="group" aria-label={text.settings.countsAria}>
								{#each settingsView.metricRows as row}
									<div class="settings-count-row">
										<dt class="settings-count-row__label">{row.label}</dt>
										<dd class="settings-count-row__value">{row.value}</dd>
									</div>
								{/each}
							</dl>
						</div>
					</div>
				</details>
			</div>
		{/if}
	</LoadingBoundary>
</section>
