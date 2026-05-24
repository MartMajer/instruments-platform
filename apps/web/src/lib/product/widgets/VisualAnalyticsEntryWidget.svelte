<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isVisualAnalyticsEntryWidgetData } from './report-widget-data';
	import { formatWidgetLabel, type ReportWidgetFormatCopy } from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isVisualAnalyticsEntryWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('visibleScores', copy)}</dt>
				<dd class="metric-card__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('suppressed', copy)}</dt>
				<dd class="metric-card__value">{data.suppressedScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('reportable', copy)}</dt>
				<dd class="metric-card__value">{data.reportableCampaignCount}</dd>
			</div>
		</dl>

		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('selectedCampaign', copy)}</dt>
				<dd class="record-field__value">
					{data.selectedCampaignId
						? formatWidgetLabel('available', copy)
						: formatWidgetLabel('notSelected', copy)}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('previewSource', copy)}</dt>
				<dd class="record-field__value">
					{widget.dataSource
						? `${widget.dataSource.method} ${formatWidgetLabel('reportPreview', copy)}`
						: formatWidgetLabel('notConfiguredState', copy)}
				</dd>
			</div>
		</dl>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('visualAnalyticsDataUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
