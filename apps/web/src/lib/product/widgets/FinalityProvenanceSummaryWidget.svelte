<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isFinalityProvenanceWidgetData } from './report-widget-data';
	import {
		formatCodeLabel,
		formatNullableDate,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isFinalityProvenanceWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('preliminaryLive', copy)}</dt>
				<dd class="metric-card__value">{data.preliminaryLiveReportCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('closedWave', copy)}</dt>
				<dd class="metric-card__value">{data.closedWaveReportCount}</dd>
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
				<dt class="record-field__label">{formatWidgetLabel('campaignStatus', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.selectedCampaignStatus, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('dataFinality', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.selectedDataFinality, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('latestLaunch', copy)}</dt>
				<dd class="record-field__value">{formatNullableDate(data.selectedLatestLaunchAt, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('closedAt', copy)}</dt>
				<dd class="record-field__value">{formatNullableDate(data.selectedClosedAt, copy)}</dd>
			</div>
		</dl>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('finalityDataUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
