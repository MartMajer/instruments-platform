<script lang="ts">
	import type { ResultsDashboardPointResponse } from '$lib/api/product';
	import {
		formatCodeLabel,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';

	let {
		points,
		copy
	}: {
		points: ResultsDashboardPointResponse[];
		copy?: ReportWidgetFormatCopy;
	} = $props();

	type TrendGroup = {
		dimensionCode: string;
		points: ResultsDashboardPointResponse[];
	};

	const groups = $derived(groupTrendPoints(points));
	const visibleValues = $derived(
		points
			.filter((point) => point.disclosure === 'visible' && point.value !== null)
			.map((point) => point.value ?? 0)
	);
	const minValue = $derived(visibleValues.length > 0 ? Math.min(...visibleValues) : 0);
	const maxValue = $derived(visibleValues.length > 0 ? Math.max(...visibleValues) : 1);

	function groupTrendPoints(rows: ResultsDashboardPointResponse[]): TrendGroup[] {
		const byDimension = new Map<string, ResultsDashboardPointResponse[]>();
		for (const row of rows) {
			const existing = byDimension.get(row.dimensionCode) ?? [];
			existing.push(row);
			byDimension.set(row.dimensionCode, existing);
		}

		return [...byDimension.entries()].map(([dimensionCode, groupedPoints]) => ({
			dimensionCode,
			points: groupedPoints
		}));
	}

	function pointX(index: number, total: number) {
		return total <= 1 ? 50 : (index / (total - 1)) * 100;
	}

	function pointY(point: ResultsDashboardPointResponse) {
		if (point.disclosure !== 'visible' || point.value === null) {
			return 92;
		}

		const spread = maxValue - minValue;
		if (spread <= 0) {
			return 50;
		}

		return 91 - ((point.value - minValue) / spread) * 82;
	}

	function visiblePointCount(rows: ResultsDashboardPointResponse[]) {
		return rows.filter((point) => point.disclosure === 'visible' && point.value !== null).length;
	}

	function polylinePoints(rows: ResultsDashboardPointResponse[]) {
		return rows
			.map((point, index) =>
				point.disclosure === 'visible' && point.value !== null
					? `${pointX(index, rows.length)},${pointY(point)}`
					: null
			)
			.filter(Boolean)
			.join(' ');
	}

	function formatPointValue(point: ResultsDashboardPointResponse) {
		if (point.disclosure !== 'visible') {
			return formatCodeLabel('suppressed', copy);
		}

		return point.value === null ? (copy?.notAvailable ?? 'Not available') : point.value.toFixed(2);
	}

	function formatDelta(point: ResultsDashboardPointResponse) {
		if (point.disclosure !== 'visible') {
			return formatCodeLabel('suppressed', copy);
		}

		if (point.comparisonState === 'baseline') {
			return formatWidgetLabel('baseline', copy);
		}

		if (point.deltaFromPrevious === null) {
			return copy?.notAvailable ?? 'Not available';
		}

		return point.deltaFromPrevious > 0
			? `+${point.deltaFromPrevious.toFixed(2)}`
			: point.deltaFromPrevious.toFixed(2);
	}
</script>

{#if groups.length > 0}
	<div class="results-trend-chart">
		{#each groups as group (group.dimensionCode)}
			<section class="results-trend-chart__group" aria-label={formatCodeLabel(group.dimensionCode, copy)}>
				<div class="record-row__header">
					<p class="record-row__title">{formatCodeLabel(group.dimensionCode, copy)}</p>
					<span class="record-field__label">
						{formatWidgetLabel('measurement', copy)} {group.points.length}
					</span>
				</div>
				<div class="results-trend-chart__plot" aria-hidden="true">
					<svg class="results-trend-chart__svg" viewBox="0 0 100 100" preserveAspectRatio="none">
						{#if visiblePointCount(group.points) > 1}
							<polyline class="results-trend-chart__line" points={polylinePoints(group.points)} />
						{/if}
						{#each group.points as point, index (point.id)}
							{#if point.disclosure === 'visible' && point.value !== null}
								<circle
									class="results-trend-chart__point"
									cx={pointX(index, group.points.length)}
									cy={pointY(point)}
									r="2.8"
								>
									<title>{point.campaignName}: {formatPointValue(point)}</title>
								</circle>
							{:else}
								<line
									class="results-trend-chart__suppressed"
									x1={pointX(index, group.points.length)}
									x2={pointX(index, group.points.length)}
									y1="84"
									y2="94"
								>
									<title>{point.campaignName}: {formatPointValue(point)}</title>
								</line>
							{/if}
						{/each}
					</svg>
				</div>
				<div class="results-trend-chart__legend">
					{#each group.points as point (point.id)}
						<div class="results-trend-chart__legend-item" data-disclosure={point.disclosure}>
							<span>{point.campaignName}</span>
							<strong>{formatPointValue(point)}</strong>
							<small>{formatDelta(point)}</small>
						</div>
					{/each}
				</div>
			</section>
		{/each}
	</div>
{:else}
	<p class="text-sm text-[var(--color-text-muted)]">{formatWidgetLabel('noWaveTrendPoints', copy)}</p>
{/if}
