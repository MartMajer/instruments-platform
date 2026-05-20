<script lang="ts">
	import { resolve } from '$app/paths';
	import { BarChart3, ClipboardList, RadioTower, Settings2, Waves } from 'lucide-svelte';

	export type SelectedSeriesWorkspaceSurface = 'hub' | 'setup' | 'operations' | 'reports' | 'waves';

	type WorkspaceBadge = {
		status: string;
		label: string;
	};

	let {
		seriesId,
		seriesTitle,
		seriesSubtitle,
		currentSurface,
		badges = []
	}: {
		seriesId: string;
		seriesTitle: string;
		seriesSubtitle?: string | null;
		currentSurface: SelectedSeriesWorkspaceSurface;
		badges?: WorkspaceBadge[];
	} = $props();

	const routeItems = $derived([
		{
			surface: 'hub' as const,
			label: 'Hub',
			description: 'Overview',
			href: resolve(`/app/campaign-series/${seriesId}`),
			icon: ClipboardList
		},
		{
			surface: 'setup' as const,
			label: 'Setup',
			description: 'Configuration',
			href: resolve(`/app/campaign-series/${seriesId}/setup`),
			icon: Settings2
		},
		{
			surface: 'operations' as const,
			label: 'Operations',
			description: 'Launch and delivery',
			href: resolve(`/app/campaign-series/${seriesId}/operations`),
			icon: RadioTower
		},
		{
			surface: 'reports' as const,
			label: 'Reports',
			description: 'Proof and exports',
			href: resolve(`/app/campaign-series/${seriesId}/reports`),
			icon: BarChart3
		},
		{
			surface: 'waves' as const,
			label: 'Waves',
			description: 'Linked trajectories',
			href: resolve(`/app/campaign-series/${seriesId}/waves`),
			icon: Waves
		}
	]);
	const currentRouteItem = $derived(
		routeItems.find((item) => item.surface === currentSurface) ?? routeItems[0]
	);
</script>

<div class="series-workspace" role="group" aria-label="Selected series workspace">
	<div class="series-workspace__summary">
		<div class="min-w-0">
			<p class="product-kicker">Selected series</p>
			<p class="series-workspace__title">{seriesTitle}</p>
			{#if seriesSubtitle}
				<p class="series-workspace__subtitle">{seriesSubtitle}</p>
			{/if}
		</div>

		{#if badges.length > 0}
			<div class="series-workspace__badges" aria-label="Selected series posture">
				{#each badges as badge}
					<span class="status-badge" data-status={badge.status}>{badge.label}</span>
				{/each}
			</div>
		{/if}

		<p class="result-line series-workspace__id">
			<span>Campaign series id</span>
			<code>{seriesId}</code>
		</p>
	</div>

	<nav class="series-workspace__nav" aria-label="Selected series routes">
		<ol class="series-workspace__nav-list">
			{#each routeItems as item (item.surface)}
				{@const Icon = item.icon}
				{@const isCurrent = item.surface === currentSurface}
				<li>
					<a
						class="series-workspace__link"
						data-current={isCurrent ? 'true' : undefined}
						href={item.href}
						aria-current={isCurrent ? 'page' : undefined}
						aria-label={item.label}
					>
						<span class="series-workspace__icon">
							<Icon size={17} strokeWidth={2.1} aria-hidden="true" />
						</span>
						<span class="series-workspace__link-copy">
							<span class="series-workspace__label">{item.label}</span>
							<span class="series-workspace__description">{item.description}</span>
						</span>
					</a>
				</li>
			{/each}
		</ol>
	</nav>

	<details class="series-workspace__mobile-switcher">
		<summary>
			<span>
				<span class="product-kicker">Current area</span>
				<strong>{currentRouteItem.label}</strong>
			</span>
			<span aria-hidden="true">Open</span>
		</summary>
		<ol class="series-workspace__mobile-list" aria-label="Selected series mobile routes">
			{#each routeItems as item (item.surface)}
				<li>
					<a
						href={item.href}
						aria-current={item.surface === currentSurface ? 'page' : undefined}
					>
						<span>{item.label}</span>
						<small>{item.description}</small>
					</a>
				</li>
			{/each}
		</ol>
	</details>
</div>
