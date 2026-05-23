<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isSelectedCampaignReportStateWidgetData } from './report-widget-data';
	import {
		formatBooleanLabel,
		formatCodeLabel,
		formatNullableDate,
		formatNullableNumber,
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
				<dt class="metric-card__label">Submitted</dt>
				<dd class="metric-card__value">{data.submittedResponseCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Scores</dt>
				<dd class="metric-card__value">{data.scoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Visible</dt>
				<dd class="metric-card__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Suppressed</dt>
				<dd class="metric-card__value">{data.suppressedScoreCount}</dd>
			</div>
		</dl>

		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">Campaign</dt>
				<dd class="record-field__value">{data.name}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Campaign status</dt>
				<dd class="record-field__value">{formatCodeLabel(data.status, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Report status</dt>
				<dd class="record-field__value">{formatCodeLabel(data.reportStatus, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Interpretation</dt>
				<dd class="record-field__value">{formatCodeLabel(data.interpretationStatus, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Disclosure</dt>
				<dd class="record-field__value">
					{formatCodeLabel(data.disclosureState, copy)} / k {formatNullableNumber(data.disclosureKMin, copy)}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Data finality</dt>
				<dd class="record-field__value">{formatCodeLabel(data.dataFinality, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Latest launch</dt>
				<dd class="record-field__value">{formatNullableDate(data.latestLaunchAt, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Closed at</dt>
				<dd class="record-field__value">{formatNullableDate(data.closedAt, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Latest export</dt>
				<dd class="record-field__value">
					{data.latestExportArtifactFileName ?? copy?.notAvailable ?? 'Not available'}
				</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Export state</dt>
				<dd class="record-field__value">
					{formatCodeLabel(data.latestExportArtifactStatus, copy)} / download {formatBooleanLabel(
						data.latestExportArtifactCanDownload,
						copy
					)}
				</dd>
			</div>
		</dl>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			Selected campaign report state is unavailable.
		</p>
	{/if}
</ReportWidgetShell>
