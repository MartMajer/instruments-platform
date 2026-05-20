<script lang="ts">
	import type { CampaignSeriesReportsWidgetManifestResponse } from '$lib/api/product';
	import { getReportWidgetComponent } from './report-widget-registry';

	let {
		manifest,
		warning,
		embedded = false
	}: {
		manifest: CampaignSeriesReportsWidgetManifestResponse | null;
		warning?: string | null;
		embedded?: boolean;
	} = $props();
</script>

{#if embedded}
	<div
		class="score-result-panel report-proof-panel"
		data-priority="trust"
		role="group"
		aria-label="Results preview widgets"
	>
		<div class="score-result-panel__header">
			<div>
				<p class="product-kicker">Results preview</p>
				<h4 class="record-row__title">Current result summary</h4>
			</div>
			{#if manifest}
				<span class="status-badge" data-status="ready">Preview ready</span>
			{/if}
		</div>
		{@render WidgetBody()}
	</div>
{:else}
	<section class="product-panel" data-priority="trust" aria-label="Results summary">
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">Results preview</p>
				<h3 class="product-title">Results summary</h3>
			</div>
			{#if manifest}
				<span class="status-badge" data-status="ready">Ready</span>
			{/if}
		</div>
		{@render WidgetBody()}
	</section>
{/if}

{#snippet WidgetBody()}
	{#if warning}
		<p class="error-line">{warning}</p>
	{/if}

	{#if manifest}
		<div class="report-widget-grid">
			{#each manifest.widgets as widget (widget.id)}
				{@const WidgetComponent = getReportWidgetComponent(widget.kind)}
				<WidgetComponent {widget} />
			{/each}
		</div>
	{:else}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">Results preview unavailable</strong>
			<span>
				{warning
					? 'The export workflow can still be used while the preview is unavailable.'
					: 'The results preview is loading.'}
			</span>
		</p>
	{/if}
{/snippet}
