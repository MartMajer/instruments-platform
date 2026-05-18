<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { ClipboardCheck, FileStack, Gauge, LibraryBig, PanelLeft, Send } from 'lucide-svelte';
	import SurfaceNav from '$lib/components/SurfaceNav.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { setupStages } from '$lib/setup/stages';

	let { children } = $props();

	const stageIcons = [LibraryBig, FileStack, Gauge, Send, ClipboardCheck];
	const isProductShell = $derived(page.url.pathname.startsWith('/app'));
	const isProductEntry = $derived(page.url.pathname === '/');
	const shellLabel = $derived(
		isProductShell ? 'Product workspace' : isProductEntry ? 'Private beta' : 'Tenant setup'
	);
	const headerKicker = $derived(
		isProductShell ? 'Tenant workspace' : isProductEntry ? 'Product entry' : 'Tenant setup path'
	);
	const headerTitle = $derived(
		isProductShell
			? 'Tenant command workspace'
			: isProductEntry
				? 'Authenticated workspace gateway'
				: 'Setup APIs and launch readiness'
	);
	const mainLabel = $derived(
		isProductShell
			? 'Product workspace'
			: isProductEntry
				? 'Product entry'
				: 'Tenant setup workspace'
	);
</script>

<div class="app-shell">
	<div class="app-shell__grid">
		<aside class="app-sidebar">
			<div class="app-brand">
				<div class="app-brand__mark" aria-hidden="true">
					<PanelLeft size={18} strokeWidth={2.1} />
				</div>
				<div class="min-w-0">
					<p class="app-brand__name">Instruments Platform</p>
					<p class="app-brand__context">{shellLabel}</p>
				</div>
			</div>

			{#if isProductShell}
				<SurfaceNav />
			{:else if !isProductEntry}
				<nav class="mt-5" aria-label="Setup stages">
					<ol class="grid gap-2">
						{#each setupStages as stage, index (stage.href)}
							{@const Icon = stageIcons[index]}
							<li>
								<a
									href={resolve(stage.href)}
									aria-current={stage.status === 'next' ? 'step' : undefined}
									class="group grid grid-cols-[2.125rem_1fr] gap-3 rounded border border-transparent px-2 py-2 transition hover:border-[var(--color-border)] hover:bg-[var(--color-surface-muted)]"
								>
									<span
										class="flex size-8 items-center justify-center rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-muted)] group-hover:text-[var(--color-text)]"
									>
										<Icon size={17} strokeWidth={2.1} aria-hidden="true" />
									</span>
									<span class="min-w-0">
										<span class="flex items-center justify-between gap-2">
											<span class="truncate text-sm font-semibold">{stage.label}</span>
											<StatusBadge status={stage.status} />
										</span>
										<span class="mt-1 line-clamp-2 block text-xs text-[var(--color-text-muted)]">
											{stage.description}
										</span>
									</span>
								</a>
							</li>
						{/each}
					</ol>
				</nav>
			{/if}
		</aside>

		<div class="min-w-0">
			{#if !isProductShell}
				<header class="app-topbar">
					<div class="app-topbar__inner">
						<div>
							<p class="app-topbar__kicker">{headerKicker}</p>
							<p class="app-topbar__title">{headerTitle}</p>
						</div>
						<div class="app-topbar__meta" aria-label="Workspace posture">
							<span class="app-chip">
								{isProductEntry ? 'App-first route' : 'Rights-attested content'}
							</span>
							<span class="app-chip">
								{isProductEntry ? 'Tenant data in /app' : 'Local dev auth'}
							</span>
						</div>
					</div>
				</header>
			{/if}

			<main class="app-main" aria-label={mainLabel}>
				<div class="app-main__inner">
					{@render children()}
				</div>
			</main>
		</div>
	</div>
</div>
