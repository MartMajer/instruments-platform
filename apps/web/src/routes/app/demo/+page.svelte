<script lang="ts">
	import { env } from '$env/dynamic/public';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import { productFixtureCatalog } from '$lib/product/fixtures';

	const demoEnabled = env.PUBLIC_DEMO_SURFACES_ENABLED === 'true';
</script>

<SurfaceHeader
	eyebrow="Local design aid"
	title="Demo fixtures"
	description="Fixture-backed product states for frontend planning without live tenant database choreography."
	statusLabel={demoEnabled ? 'Demo data' : 'Unavailable'}
/>

{#if demoEnabled}
	<section class="setup-panel" aria-label="Demo fixture scenarios">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Demo data</p>
				<h2 class="setup-panel__title">Fixture scenarios</h2>
			</div>
		</div>
		<p class="text-sm leading-6 text-[var(--color-text-muted)]">
			Demo data is local/dev only and must stay visibly labelled. Scenario links use the same
			route model as real product surfaces where practical.
		</p>
		<div class="grid gap-4">
			{#each productFixtureCatalog as group (group.id)}
				<section class="grid gap-3 rounded border border-[var(--color-border)] p-3" aria-label={group.title}>
					<div>
						<h3 class="text-base font-semibold text-[var(--color-text)]">{group.title}</h3>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">{group.description}</p>
					</div>
					<div class="grid gap-2 md:grid-cols-2">
						{#each group.scenarios as scenario (scenario.id)}
							<a class="setup-callout" href={scenario.href}>
								<span class="setup-callout__key">{scenario.labels.join(' / ')}</span>
								<span class="setup-callout__value">{scenario.title}</span>
								<span class="setup-callout__note">{scenario.description}</span>
							</a>
						{/each}
					</div>
				</section>
			{/each}
		</div>
	</section>
{:else}
	<section class="setup-panel" aria-label="Demo fixture unavailable state">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Gate closed</p>
				<h2 class="setup-panel__title">Demo fixture surfaces are unavailable</h2>
			</div>
		</div>
		<p class="text-sm leading-6 text-[var(--color-text-muted)]">
			Set PUBLIC_DEMO_SURFACES_ENABLED=true in local development before showing fixture-backed
			surfaces.
		</p>
	</section>
{/if}
