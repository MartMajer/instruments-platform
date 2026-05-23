<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isVisualAnalyticsEntryWidgetData } from './report-widget-data';
	import type { ReportWidgetFormatCopy } from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isVisualAnalyticsEntryWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">Visible scores</dt>
				<dd class="metric-card__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Suppressed</dt>
				<dd class="metric-card__value">{data.suppressedScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Reportable</dt>
				<dd class="metric-card__value">{data.reportableCampaignCount}</dd>
			</div>
		</dl>

		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">Selected campaign</dt>
				<dd class="record-field__value">
					{data.selectedCampaignId ? 'Available' : 'Not selected'}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Preview source</dt>
				<dd class="record-field__value">
					{widget.dataSource ? `${widget.dataSource.method} report preview` : 'Not configured'}
				</dd>
			</div>
		</dl>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			Visual analytics entry data is unavailable.
		</p>
	{/if}
</ReportWidgetShell>
