<script lang="ts">
	import type { CampaignSeriesReportsWidgetManifestResponse } from '$lib/api/product';
	import { getReportWidgetComponent } from './report-widget-registry';

	let {
		manifest,
		warning
	}: {
		manifest: CampaignSeriesReportsWidgetManifestResponse | null;
		warning?: string | null;
	} = $props();
</script>

<section class="product-panel" data-priority="trust" aria-label="Report widgets">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Report dashboard</p>
			<h3 class="product-title">Configured result widgets</h3>
		</div>
		{#if manifest}
			<span class="status-badge" data-status="ready">{manifest.layout.density}</span>
		{/if}
	</div>

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
			<strong class="record-row__title">Widget manifest unavailable</strong>
			<span>Existing report panels remain available.</span>
		</p>
	{/if}
</section>
