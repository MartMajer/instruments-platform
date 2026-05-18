<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { onMount } from 'svelte';
	import type { TenantSettingsWorkspaceResponse } from '$lib/api/product';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import RouteGuidancePanel from '$lib/components/RouteGuidancePanel.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { toProductRouteGuidance } from '$lib/product/route-guidance';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toProductApiErrorMessage, toTenantSettingsView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let errorMessage = $state<string | null>(null);

	const settingsView = $derived(settings ? toTenantSettingsView(settings) : null);
	const routeGuidance = toProductRouteGuidance('settings');

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
	eyebrow="Tenant administration"
	title="Settings"
	description="Read-only tenant profile, workspace scale, and management destinations."
/>

<RouteGuidancePanel guidance={routeGuidance} />

<section class="product-panel" data-priority="primary" aria-label="Tenant settings">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading tenant settings">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Tenant settings unavailable"
				message={errorMessage}
				retryLabel="Retry settings"
				onRetry={loadTenantSettings}
			/>
		{:else if settingsView}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">Tenant profile</p>
						<h2 class="product-title">{settingsView.title}</h2>
					</div>
					<StatusBadge status={settingsView.status} />
				</div>

				<dl class="record-grid" role="group" aria-label="Tenant profile details">
					{#each settingsView.profileRows as row}
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

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">Workspace scale</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Current tenant footprint
						</h3>
					</div>

					<dl class="settings-count-list" role="group" aria-label="Tenant workspace counts">
						{#each settingsView.metricRows as row}
							<div class="settings-count-row">
								<dt class="settings-count-row__label">{row.label}</dt>
								<dd class="settings-count-row__value">{row.value}</dd>
							</div>
						{/each}
					</dl>
				</div>

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">Management</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Tenant management surfaces
						</h3>
					</div>

					{#if settingsView.managementLinks.length === 0}
						<EmptyState
							title="No management links"
							description="Management destinations are not available for this tenant."
						/>
					{:else}
						<div class="record-list" aria-label="Tenant management links">
							{#each settingsView.managementLinks as link (link.id)}
								<a class="record-row" href={link.href}>
									<span class="record-row__header">
										<span class="record-row__title">{link.label}</span>
										<span class="secondary-button">Open</span>
									</span>
									<span class="text-sm leading-6 text-[var(--color-text-muted)]">
										{link.description}
									</span>
								</a>
							{/each}
						</div>
					{/if}
				</div>
			</div>
		{/if}
	</LoadingBoundary>
</section>
