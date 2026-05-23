<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isScoreCoverageSummaryWidgetData } from './report-widget-data';
	import {
		formatCodeLabel,
		formatNullableDate,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isScoreCoverageSummaryWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">Submitted</dt>
				<dd class="metric-card__value">{data.submittedResponseCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Scored</dt>
				<dd class="metric-card__value">{data.scoredSubmittedResponseCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Unscored</dt>
				<dd class="metric-card__value">{data.unscoredSubmittedResponseCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Not configured</dt>
				<dd class="metric-card__value">{data.notConfiguredSubmittedResponseCount}</dd>
			</div>
		</dl>

		<div class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">Coverage status</dt>
				<dd class="record-field__value">{formatCodeLabel(data.status, copy)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Latest scoring activity</dt>
				<dd class="record-field__value">{formatNullableDate(data.latestScoringActivityAt, copy)}</dd>
			</div>
		</div>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">Score coverage data is unavailable.</p>
	{/if}
</ReportWidgetShell>
