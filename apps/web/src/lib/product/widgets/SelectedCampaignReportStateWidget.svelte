<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isSelectedCampaignReportStateWidgetData } from './report-widget-data';
	import {
		formatBooleanLabel,
		formatCodeLabel,
		formatNullableDate,
		formatNullableNumber,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isSelectedCampaignReportStateWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('submitted', copy)}</dt>
				<dd class="metric-card__value">{data.submittedResponseCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('scores', copy)}</dt>
				<dd class="metric-card__value">{data.scoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('visible', copy)}</dt>
				<dd class="metric-card__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('suppressed', copy)}</dt>
				<dd class="metric-card__value">{data.suppressedScoreCount}</dd>
			</div>
		</dl>

		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('campaign', copy)}</dt>
				<dd class="record-field__value">{data.name}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('campaignStatus', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.status, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('reportStatus', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.reportStatus, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('interpretation', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.interpretationStatus, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('disclosure', copy)}</dt>
				<dd class="record-field__value">
					{formatCodeLabel(data.disclosureState, copy)} / k {formatNullableNumber(data.disclosureKMin, copy)}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('dataFinality', copy)}</dt>
				<dd class="record-field__value">{formatCodeLabel(data.dataFinality, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('latestLaunch', copy)}</dt>
				<dd class="record-field__value">{formatNullableDate(data.latestLaunchAt, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('closedAt', copy)}</dt>
				<dd class="record-field__value">{formatNullableDate(data.closedAt, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('latestExport', copy)}</dt>
				<dd class="record-field__value">
					{data.latestExportArtifactFileName ?? copy?.notAvailable ?? 'Not available'}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('exportState', copy)}</dt>
				<dd class="record-field__value">
					{formatCodeLabel(data.latestExportArtifactStatus, copy)} / {formatWidgetLabel('download', copy)}
					{formatBooleanLabel(
						data.latestExportArtifactCanDownload,
						copy
					)}
				</dd>
			</div>
		</dl>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('selectedCampaignReportStateUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
