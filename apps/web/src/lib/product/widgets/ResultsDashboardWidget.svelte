<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isResultsDashboardWidgetData } from './report-widget-data';
	import {
		formatCodeLabel,
		formatProductCopy,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';
	import ResultsBarChart from './ResultsBarChart.svelte';
	import ResultsTrendChart from './ResultsTrendChart.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isResultsDashboardWidgetData(widget.data) ? widget.data : null);
	const dashboard = $derived(data?.dashboard ?? null);

	function metricLabel(id: string) {
		return formatWidgetLabel(`metric_${id}`, copy);
	}

	function formatMetricValue(value: number | null) {
		return value === null ? (copy?.notAvailable ?? 'Not available') : String(value);
	}
</script>

<ReportWidgetShell {widget} {copy}>
	{#if dashboard}
		<div class="results-dashboard-widget">
			<dl class="record-grid">
				{#each dashboard.metrics as metric (metric.id)}
					<div class="metric-card" data-tone={metric.tone}>
						<dt class="metric-card__label">{metricLabel(metric.id)}</dt>
						<dd class="metric-card__value">{formatMetricValue(metric.value)}</dd>
						{#if metric.detail}
							<dd class="metric-card__label">{formatProductCopy(metric.detail)}</dd>
						{/if}
					</div>
				{/each}
			</dl>

			<section class="results-dashboard-widget__panel" aria-label={formatWidgetLabel('resultBarChart', copy)}>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('resultBarChart', copy)}</p>
						<p class="record-row__title">
							{dashboard.selectedCampaignName ?? formatWidgetLabel('selectedCampaign', copy)}
						</p>
					</div>
					<span class="status-badge" data-status={dashboard.disclosureState}>
						{formatCodeLabel(dashboard.disclosureState, copy)}
					</span>
				</div>
				<ResultsBarChart bars={dashboard.outputBars} {copy} />
			</section>

			<section class="results-dashboard-widget__panel" aria-label={formatWidgetLabel('groupBarChart', copy)}>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('groupBarChart', copy)}</p>
						<p class="record-row__title">{formatWidgetLabel('groupBreakdown', copy)}</p>
					</div>
					<span class="record-field__label">
						{formatWidgetLabel('sample', copy)} >= {dashboard.disclosureKMin}
					</span>
				</div>
				<ResultsBarChart
					bars={dashboard.groupBars}
					{copy}
					emptyLabelKey="noGroupBars"
				/>
			</section>

			<section class="results-dashboard-widget__panel" aria-label={formatWidgetLabel('measurementTrendChart', copy)}>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('measurementTrendChart', copy)}</p>
						<p class="record-row__title">{formatWidgetLabel('waveTrend', copy)}</p>
					</div>
					<span class="record-field__label">{formatWidgetLabel('mean', copy)}</span>
				</div>
				<ResultsTrendChart points={dashboard.waveTrendPoints} {copy} />
			</section>

			{#if dashboard.notes.length > 0}
				<section class="results-dashboard-widget__panel" aria-label={formatWidgetLabel('dashboardNotes', copy)}>
					<div class="record-row__header">
						<p class="record-row__title">{formatWidgetLabel('dashboardNotes', copy)}</p>
					</div>
					<div class="record-list">
						{#each dashboard.notes as note, index (`${note.kind}\u0000${index}`)}
							<article class="record-row" data-state={note.severity}>
								<div class="record-row__header">
									<p class="record-row__title">{formatProductCopy(note.title)}</p>
									<span class="status-badge" data-status={note.severity}>
										{formatCodeLabel(note.severity, copy)}
									</span>
								</div>
								<p class="text-sm text-[var(--color-text-muted)]">{formatProductCopy(note.detail)}</p>
							</article>
						{/each}
					</div>
				</section>
			{/if}
		</div>
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('resultsDashboardDataUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
