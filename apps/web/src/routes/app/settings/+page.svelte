<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { onMount } from 'svelte';
	import type { TenantSettingsWorkspaceResponse } from '$lib/api/product';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toProductApiErrorMessage, toTenantSettingsView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let errorMessage = $state<string | null>(null);

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
			errorMessage = toProductApiErrorMessage(error, 'Tenant settings could not be loaded.');
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow="Workspace"
	title="Workspace settings"
	description="Manage workspace access, people, data, and study defaults from one place."
/>

<section class="product-panel" data-priority="primary" aria-label="Workspace settings">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading tenant settings">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Workspace settings unavailable"
				message={errorMessage}
				retryLabel="Retry settings"
				onRetry={loadTenantSettings}
			/>
		{:else if settingsView}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">Settings hub</p>
						<h2 class="product-title">What can you manage here?</h2>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
							Workspace-level settings are being assembled here. For now, use these
							shortcuts to manage the active areas of the product.
						</p>
					</div>
					<StatusBadge status={settingsView.status} />
				</div>

				<div class="record-list" aria-label="Workspace setting shortcuts">
					<a class="record-row" href="/app/team">
						<span class="record-row__header">
							<span class="record-row__title">Team access</span>
							<span class="secondary-button">Open</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							Invite workspace members, review pending access, and manage workspace roles.
						</span>
					</a>
					<a class="record-row" href="/app/directory">
						<span class="record-row__header">
							<span class="record-row__title">Directory</span>
							<span class="secondary-button">Open</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							Manage people, groups, and hierarchy data used for study audiences.
						</span>
					</a>
					<a class="record-row" href="/app/campaign-series">
						<span class="record-row__header">
							<span class="record-row__title">Study setup</span>
							<span class="secondary-button">Open</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							Create or continue studies, questionnaires, collection waves, and results setup.
						</span>
					</a>
					<a class="record-row" href="/app/exports">
						<span class="record-row__header">
							<span class="record-row__title">Exports</span>
							<span class="secondary-button">Open</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							Review generated export files and download analysis-ready outputs.
						</span>
					</a>
				</div>

				<details class="record-row" aria-label="Workspace details">
					<summary class="record-row__title">Workspace details</summary>
					<div class="grid gap-4 pt-4">
						<div>
							<p class="product-kicker">Workspace profile</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{settingsView.title}
							</h3>
						</div>

						<dl class="record-grid" role="group" aria-label="Workspace profile details">
							{#each settingsView.profileRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>

						<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
							<div>
								<p class="product-kicker">Workspace scale</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									Current footprint
								</h3>
							</div>

							<dl class="settings-count-list" role="group" aria-label="Workspace counts">
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
