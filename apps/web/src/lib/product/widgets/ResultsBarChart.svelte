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

	const visibleMax = $derived(
		Math.max(
			1,
			...bars
				.filter((bar) => bar.disclosure === 'visible' && bar.value !== null)
				.map((bar) => bar.value ?? 0)
		)
	);

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

		return value === null ? copy?.notAvailable ?? 'Not available' : String(value);
	}

	function barWidth(bar: ResultsDashboardBarResponse) {
		if (bar.disclosure !== 'visible' || bar.value === null) {
			return '0%';
		}

		return `${Math.max(5, Math.min(100, (bar.value / visibleMax) * 100))}%`;
	}
</script>

{#if bars.length > 0}
	<div class="results-bar-chart">
		{#each bars as bar (bar.id)}
			<div class="results-bar-chart__row" data-disclosure={bar.disclosure}>
				<div class="results-bar-chart__label">
					<span>{bar.label}</span>
					<small>{formatCodeLabel(bar.dimensionCode, copy)}</small>
				</div>
				<div class="results-bar-chart__track" aria-hidden="true">
					<span class="results-bar-chart__fill" style={`width: ${barWidth(bar)}`}></span>
				</div>
				<div class="results-bar-chart__value">
					<strong>{formatValue(bar.value, bar.disclosure)}</strong>
					<small>{formatWidgetLabel('sample', copy)} {formatCount(bar.count, bar.disclosure)}</small>
				</div>
				{#if bar.detail || bar.suppressionReason}
					<p class="results-bar-chart__detail">
						{bar.detail ?? formatCodeLabel(bar.suppressionReason, copy)}
					</p>
				{/if}
			</div>
		{/each}
	</div>
{:else}
	<p class="text-sm text-[var(--color-text-muted)]">{formatWidgetLabel(emptyLabelKey, copy)}</p>
{/if}
