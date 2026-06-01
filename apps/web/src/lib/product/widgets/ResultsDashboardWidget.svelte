<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import {
		filterResultsDashboard,
		toResultFocusOptions,
		toScoreCards
	} from '$lib/product/results-workbench';
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
	let selectedResultCode = $state('all');

	const data = $derived(isResultsDashboardWidgetData(widget.data) ? widget.data : null);
	const dashboard = $derived(data?.dashboard ?? null);
	const resultFocusOptions = $derived(
		toResultFocusOptions(dashboard?.outputBars ?? [], formatWidgetLabel('allResultOutputs', copy))
	);
	const focusedDashboard = $derived(
		dashboard ? filterResultsDashboard(dashboard, selectedResultCode) : null
	);
	const scoreCards = $derived(
		toScoreCards(focusedDashboard?.outputBars ?? [], {
			notAvailable: copy?.notAvailable ?? 'Not available',
			observedScale: formatWidgetLabel('observedRange', copy),
			scoreRange: formatWidgetLabel('scoreRange', copy),
			suppressed: formatWidgetLabel('suppressed', copy)
		})
	);

	function metricLabel(id: string) {
		return formatWidgetLabel(`metric_${id}`, copy);
	}

	function formatMetricValue(value: number | null) {
		return value === null ? (copy?.notAvailable ?? 'Not available') : String(value);
	}

	function selectResultFocus(value: string) {
		selectedResultCode = value;
	}

	$effect(() => {
		if (
			selectedResultCode !== 'all' &&
			!resultFocusOptions.some((option) => option.value === selectedResultCode)
		) {
			selectedResultCode = 'all';
		}
	});
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

			{#if resultFocusOptions.length > 2}
				<div class="results-filter-rail" aria-label={formatWidgetLabel('resultFocus', copy)}>
					<span class="record-field__label">{formatWidgetLabel('resultFocus', copy)}</span>
					<div class="results-filter-rail__options">
						{#each resultFocusOptions as option (option.value)}
							<button
								type="button"
								class="results-filter-chip"
								data-selected={selectedResultCode === option.value}
								aria-pressed={selectedResultCode === option.value}
								onclick={() => selectResultFocus(option.value)}
							>
								<span>{option.label}</span>
								<small>{option.count}</small>
							</button>
						{/each}
					</div>
				</div>
			{/if}

			{#if scoreCards.length > 0}
				<section
					class="results-dashboard-widget__panel"
					aria-label={formatWidgetLabel('scoreCards', copy)}
				>
					<div class="record-row__header">
						<div>
							<p class="record-field__label">{formatWidgetLabel('scoreCards', copy)}</p>
							<p class="record-row__title">{formatWidgetLabel('resultOutputs', copy)}</p>
						</div>
						<span class="record-field__label">
							{formatWidgetLabel('disclosure', copy)}: {formatCodeLabel(
								focusedDashboard?.disclosureState,
								copy
							)}
						</span>
					</div>
					<div class="results-score-card-grid">
						{#each scoreCards as card (card.id)}
							<article class="results-score-card" data-disclosure={card.disclosure}>
								<div class="results-score-card__header">
									<div>
										<p class="results-score-card__label">{card.label}</p>
										<p class="record-field__label">{formatCodeLabel(card.dimensionCode, copy)}</p>
									</div>
									<span class="status-badge" data-status={card.disclosure}>
										{formatCodeLabel(card.disclosure, copy)}
									</span>
								</div>
								<p class="results-score-card__value">{card.valueLabel}</p>
								<div class="results-score-card__meter" aria-hidden="true">
									<span
										class="results-score-card__meter-fill"
										style={`width: ${card.progressPercent === null ? 0 : Math.max(4, card.progressPercent)}%`}
									></span>
								</div>
								<dl class="results-score-card__meta">
									<div>
										<dt>{formatWidgetLabel('sample', copy)}</dt>
										<dd>{card.countLabel}</dd>
									</div>
									<div>
										<dt>{formatWidgetLabel('scoreRange', copy)}</dt>
										<dd>{card.rangeLabel}</dd>
									</div>
									{#if card.methodLabel}
										<div>
											<dt>{formatWidgetLabel('calculation', copy)}</dt>
											<dd>{card.methodLabel}</dd>
										</div>
									{/if}
								</dl>
								{#if card.suppressionReason}
									<p class="record-field__label">{formatCodeLabel(card.suppressionReason, copy)}</p>
								{/if}
							</article>
						{/each}
					</div>
				</section>
			{/if}

			<section
				class="results-dashboard-widget__panel"
				aria-label={formatWidgetLabel('resultBarChart', copy)}
			>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('resultBarChart', copy)}</p>
						<p class="record-row__title">
							{focusedDashboard?.selectedCampaignName ??
								formatWidgetLabel('selectedCampaign', copy)}
						</p>
					</div>
					<span class="status-badge" data-status={focusedDashboard?.disclosureState}>
						{formatCodeLabel(focusedDashboard?.disclosureState, copy)}
					</span>
				</div>
				<ResultsBarChart bars={focusedDashboard?.outputBars ?? []} {copy} />
			</section>

			<section
				class="results-dashboard-widget__panel"
				aria-label={formatWidgetLabel('groupBarChart', copy)}
			>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('groupBarChart', copy)}</p>
						<p class="record-row__title">{formatWidgetLabel('groupBreakdown', copy)}</p>
					</div>
					<span class="record-field__label">
						{formatWidgetLabel('sample', copy)} >= {focusedDashboard?.disclosureKMin}
					</span>
				</div>
				<ResultsBarChart
					bars={focusedDashboard?.groupBars ?? []}
					{copy}
					emptyLabelKey="noGroupBars"
					labelMode="full"
				/>
			</section>

			<section
				class="results-dashboard-widget__panel"
				aria-label={formatWidgetLabel('measurementTrendChart', copy)}
			>
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{formatWidgetLabel('measurementTrendChart', copy)}</p>
						<p class="record-row__title">{formatWidgetLabel('waveTrend', copy)}</p>
					</div>
					<span class="record-field__label">{formatWidgetLabel('mean', copy)}</span>
				</div>
				<ResultsTrendChart points={focusedDashboard?.waveTrendPoints ?? []} {copy} />
			</section>

			{#if dashboard.notes.length > 0}
				<section
					class="results-dashboard-widget__panel"
					aria-label={formatWidgetLabel('dashboardNotes', copy)}
				>
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
								<p class="text-sm text-[var(--color-text-muted)]">
									{formatProductCopy(note.detail)}
								</p>
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
