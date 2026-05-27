<script lang="ts">
	import type { ResultsDashboardBarResponse } from '$lib/api/product';
	import {
		formatCodeLabel,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';

	let {
		bars,
		copy,
		emptyLabelKey = 'noResultBars'
	}: {
		bars: ResultsDashboardBarResponse[];
		copy?: ReportWidgetFormatCopy;
		emptyLabelKey?: string;
	} = $props();

	let selectedBarId = $state<string | null>(null);

	const visibleMax = $derived(
		Math.max(
			1,
			...bars
				.filter((bar) => bar.disclosure === 'visible' && bar.value !== null)
				.map((bar) => bar.value ?? 0)
		)
	);
	const selectedBar = $derived(
		bars.find((bar) => bar.id === selectedBarId) ??
			bars.find((bar) => bar.disclosure === 'visible' && bar.value !== null) ??
			bars[0] ??
			null
	);

	function selectBar(bar: ResultsDashboardBarResponse) {
		selectedBarId = bar.id;
	}

	function formatValue(value: number | null, disclosure: string) {
		if (disclosure !== 'visible') {
			return formatCodeLabel('suppressed', copy);
		}

		return value === null ? (copy?.notAvailable ?? 'Not available') : value.toFixed(2);
	}

	function formatCount(value: number | null, disclosure: string) {
		if (disclosure !== 'visible') {
			return formatCodeLabel('suppressed', copy);
		}

		return value === null ? (copy?.notAvailable ?? 'Not available') : String(value);
	}

	function barWidth(bar: ResultsDashboardBarResponse) {
		if (bar.disclosure !== 'visible' || bar.value === null) {
			return '0%';
		}

		return `${Math.max(5, Math.min(100, (bar.value / visibleMax) * 100))}%`;
	}

	function barButtonLabel(bar: ResultsDashboardBarResponse) {
		return `${bar.label}: ${formatValue(bar.value, bar.disclosure)}, ${formatWidgetLabel(
			'sample',
			copy
		)} ${formatCount(bar.count, bar.disclosure)}`;
	}
</script>

{#if bars.length > 0}
	<div class="results-bar-chart">
		<div class="results-bar-chart__scale" aria-hidden="true">
			<span>0</span>
			<span>{formatWidgetLabel('chartScale', copy)} {visibleMax.toFixed(2)}</span>
		</div>

		<div class="results-bar-chart__rows">
			{#each bars as bar (bar.id)}
				<button
					type="button"
					class="results-bar-chart__row"
					data-disclosure={bar.disclosure}
					data-selected={selectedBar?.id === bar.id}
					aria-label={barButtonLabel(bar)}
					onclick={() => selectBar(bar)}
				>
					<span class="results-bar-chart__label">
						<span>{bar.label}</span>
						<small>{formatCodeLabel(bar.dimensionCode, copy)}</small>
					</span>
					<span class="results-bar-chart__track" aria-hidden="true">
						<span class="results-bar-chart__fill" style={`width: ${barWidth(bar)}`}></span>
					</span>
					<span class="results-bar-chart__value">
						<strong>{formatValue(bar.value, bar.disclosure)}</strong>
						<small>{formatWidgetLabel('sample', copy)} {formatCount(bar.count, bar.disclosure)}</small>
					</span>
					{#if bar.detail || bar.suppressionReason}
						<span class="results-bar-chart__detail">
							{bar.detail ?? formatCodeLabel(bar.suppressionReason, copy)}
						</span>
					{/if}
				</button>
			{/each}
		</div>

		{#if selectedBar}
			<div class="results-bar-chart__selected" aria-live="polite">
				<div>
					<p class="record-field__label">{formatWidgetLabel('selectedChartItem', copy)}</p>
					<p class="record-row__title">{selectedBar.label}</p>
					<p class="record-field__label">{formatCodeLabel(selectedBar.dimensionCode, copy)}</p>
				</div>
				<dl class="results-bar-chart__selected-grid">
					<div>
						<dt>{formatWidgetLabel('mean', copy)}</dt>
						<dd>{formatValue(selectedBar.value, selectedBar.disclosure)}</dd>
					</div>
					<div>
						<dt>{formatWidgetLabel('sample', copy)}</dt>
						<dd>{formatCount(selectedBar.count, selectedBar.disclosure)}</dd>
					</div>
					<div>
						<dt>{formatWidgetLabel('disclosure', copy)}</dt>
						<dd>{formatCodeLabel(selectedBar.disclosure, copy)}</dd>
					</div>
				</dl>
				{#if selectedBar.detail || selectedBar.suppressionReason}
					<p class="text-sm text-[var(--color-text-muted)]">
						{selectedBar.detail ?? formatCodeLabel(selectedBar.suppressionReason, copy)}
					</p>
				{/if}
			</div>
		{/if}

		<div class="results-bar-chart__table" aria-label={formatWidgetLabel('chartDataTable', copy)}>
			<div class="results-bar-chart__table-row">
				<strong>{formatWidgetLabel('resultName', copy)}</strong>
				<strong>{formatWidgetLabel('mean', copy)}</strong>
				<strong>{formatWidgetLabel('sample', copy)}</strong>
				<strong>{formatWidgetLabel('disclosure', copy)}</strong>
			</div>
			{#each bars as bar (bar.id)}
				<button
					type="button"
					class="results-bar-chart__table-row"
					data-selected={selectedBar?.id === bar.id}
					onclick={() => selectBar(bar)}
				>
					<span>{bar.label}</span>
					<span>{formatValue(bar.value, bar.disclosure)}</span>
					<span>{formatCount(bar.count, bar.disclosure)}</span>
					<span>{formatCodeLabel(bar.disclosure, copy)}</span>
				</button>
			{/each}
		</div>
	</div>
{:else}
	<p class="text-sm text-[var(--color-text-muted)]">{formatWidgetLabel(emptyLabelKey, copy)}</p>
{/if}
