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
		displayLabel: string;
		points: ResultsDashboardPointResponse[];
	};

	type ChartDomain = {
		min: number;
		max: number;
	};

	const selectedPointIds = $state<Record<string, string>>({});
	const groups = $derived(groupTrendPoints(points));
	const visibleValues = $derived(
		points
			.filter((point) => point.disclosure === 'visible' && point.value !== null)
			.map((point) => point.value ?? 0)
	);
	const chartDomain = $derived(createChartDomain(visibleValues));
	const axisTicks = $derived(createAxisTicks(chartDomain));

	function groupTrendPoints(rows: ResultsDashboardPointResponse[]): TrendGroup[] {
		const byDimension = new Map<string, ResultsDashboardPointResponse[]>();
		for (const row of rows) {
			const existing = byDimension.get(row.dimensionCode) ?? [];
			existing.push(row);
			byDimension.set(row.dimensionCode, existing);
		}

		return [...byDimension.entries()].map(([dimensionCode, groupedPoints]) => ({
			dimensionCode,
			displayLabel:
				groupedPoints.find((point) => point.displayLabel?.trim())?.displayLabel?.trim() ??
				formatCodeLabel(dimensionCode, copy),
			points: groupedPoints
		}));
	}

	function createChartDomain(values: number[]): ChartDomain {
		if (values.length === 0) {
			return { min: 0, max: 1 };
		}

		const rawMin = Math.min(...values);
		const rawMax = Math.max(...values);
		const rawSpread = rawMax - rawMin;
		const padding = rawSpread <= 0 ? Math.max(Math.abs(rawMax) * 0.1, 0.5) : rawSpread * 0.12;
		const min = rawMin >= 0 ? Math.max(0, rawMin - padding) : rawMin - padding;
		const max = rawMax + padding;

		return max <= min ? { min, max: min + 1 } : { min, max };
	}

	function createAxisTicks(domain: ChartDomain) {
		const middle = domain.min + (domain.max - domain.min) / 2;
		return [domain.max, middle, domain.min].map((value) => ({
			value,
			label: formatAxisValue(value),
			y: axisY(value, domain)
		}));
	}

	function axisY(value: number, domain: ChartDomain) {
		const spread = domain.max - domain.min;
		if (spread <= 0) {
			return 50;
		}

		return 88 - ((value - domain.min) / spread) * 76;
	}

	function pointX(index: number, total: number) {
		return total <= 1 ? 50 : 8 + (index / (total - 1)) * 84;
	}

	function pointY(point: ResultsDashboardPointResponse) {
		if (point.disclosure !== 'visible' || point.value === null) {
			return 92;
		}

		return axisY(point.value, chartDomain);
	}

	function visiblePointCount(rows: ResultsDashboardPointResponse[]) {
		return rows.filter((point) => point.disclosure === 'visible' && point.value !== null).length;
	}

	function polylineSegments(rows: ResultsDashboardPointResponse[]) {
		const segments: string[] = [];
		let current: string[] = [];

		rows.forEach((point, index) => {
			if (point.disclosure === 'visible' && point.value !== null) {
				current.push(`${pointX(index, rows.length)},${pointY(point)}`);
				return;
			}

			if (current.length > 1) {
				segments.push(current.join(' '));
			}
			current = [];
		});

		if (current.length > 1) {
			segments.push(current.join(' '));
		}

		return segments;
	}

	function selectedPointForGroup(group: TrendGroup) {
		const selectedId = selectedPointIds[group.dimensionCode];
		return (
			group.points.find((point) => point.id === selectedId) ??
			group.points.find((point) => point.disclosure === 'visible' && point.value !== null) ??
			group.points[0] ??
			null
		);
	}

	function selectPoint(group: TrendGroup, point: ResultsDashboardPointResponse) {
		selectedPointIds[group.dimensionCode] = point.id;
	}

	function formatAxisValue(value: number) {
		return Number.isInteger(value) ? String(value) : value.toFixed(1);
	}

	function formatPointValue(point: ResultsDashboardPointResponse) {
		if (point.disclosure !== 'visible') {
			return formatCodeLabel('suppressed', copy);
		}

		return point.value === null ? (copy?.notAvailable ?? 'Not available') : point.value.toFixed(2);
	}

	function formatCountValue(point: ResultsDashboardPointResponse) {
		return point.count === null ? (copy?.notAvailable ?? 'Not available') : String(point.count);
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

	function pointButtonLabel(point: ResultsDashboardPointResponse) {
		return `${point.campaignName}: ${formatPointValue(point)}, ${formatWidgetLabel(
			'fromPrevious',
			copy
		)} ${formatDelta(point)}`;
	}
</script>

{#if groups.length > 0}
	<div class="results-trend-chart">
		{#each groups as group (group.dimensionCode)}
			{@const selectedPoint = selectedPointForGroup(group)}
			<section class="results-trend-chart__group" aria-label={group.displayLabel}>
				<div class="record-row__header">
					<div>
						<p class="record-row__title">{group.displayLabel}</p>
						<p class="record-field__label">
							{formatWidgetLabel('observedRange', copy)} {formatAxisValue(chartDomain.min)} - {formatAxisValue(
								chartDomain.max
							)}
						</p>
					</div>
					<span class="record-field__label">
						{formatWidgetLabel('measurement', copy)} {group.points.length}
					</span>
				</div>

				<div class="results-trend-chart__frame">
					<div class="results-trend-chart__y-axis" aria-hidden="true">
						{#each axisTicks as tick (tick.value)}
							<span style={`top: ${tick.y}%`}>{tick.label}</span>
						{/each}
					</div>

					<div class="results-trend-chart__plot">
						<svg class="results-trend-chart__svg" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
							{#each axisTicks as tick (tick.value)}
								<line class="results-trend-chart__gridline" x1="0" x2="100" y1={tick.y} y2={tick.y} />
							{/each}
							{#if visiblePointCount(group.points) > 1}
								{#each polylineSegments(group.points) as segment (segment)}
									<polyline class="results-trend-chart__line" points={segment} />
								{/each}
							{/if}
						</svg>

						<div class="results-trend-chart__point-layer">
							{#each group.points as point, index (point.id)}
								<button
									type="button"
									class="results-trend-chart__point-button"
									data-disclosure={point.disclosure}
									data-selected={selectedPoint?.id === point.id}
									style={`left: ${pointX(index, group.points.length)}%; top: ${pointY(point)}%;`}
									aria-label={pointButtonLabel(point)}
									onclick={() => selectPoint(group, point)}
								>
									<span class="results-trend-chart__value-label">{formatPointValue(point)}</span>
									<span class="results-trend-chart__marker" aria-hidden="true"></span>
								</button>
							{/each}
						</div>
					</div>
				</div>

				<div class="results-trend-chart__x-axis" aria-hidden="true">
					{#each group.points as point (point.id)}
						<span>{point.campaignName}</span>
					{/each}
				</div>

				{#if selectedPoint}
					<div class="results-trend-chart__selected" aria-live="polite">
						<div>
							<p class="record-field__label">{formatWidgetLabel('selectedMeasurement', copy)}</p>
							<p class="record-row__title">{selectedPoint.campaignName}</p>
						</div>
						<dl class="results-trend-chart__selected-grid">
							<div>
								<dt>{formatWidgetLabel('mean', copy)}</dt>
								<dd>{formatPointValue(selectedPoint)}</dd>
							</div>
							<div>
								<dt>{formatWidgetLabel('fromPrevious', copy)}</dt>
								<dd>{formatDelta(selectedPoint)}</dd>
							</div>
							<div>
								<dt>{formatWidgetLabel('sample', copy)}</dt>
								<dd>{formatCountValue(selectedPoint)}</dd>
							</div>
							<div>
								<dt>{formatWidgetLabel('dataFinality', copy)}</dt>
								<dd>{formatCodeLabel(selectedPoint.dataFinality, copy)}</dd>
							</div>
						</dl>
						{#if selectedPoint.suppressionReason}
							<p class="text-sm text-[var(--color-text-muted)]">
								{formatCodeLabel(selectedPoint.suppressionReason, copy)}
							</p>
						{/if}
					</div>
				{/if}

				<div class="results-trend-chart__table" aria-label={formatWidgetLabel('chartDataTable', copy)}>
					<div class="results-trend-chart__table-row">
						<strong>{formatWidgetLabel('measurement', copy)}</strong>
						<strong>{formatWidgetLabel('mean', copy)}</strong>
						<strong>{formatWidgetLabel('fromPrevious', copy)}</strong>
						<strong>{formatWidgetLabel('sample', copy)}</strong>
					</div>
					{#each group.points as point (point.id)}
						<button
							type="button"
							class="results-trend-chart__table-row"
							data-selected={selectedPoint?.id === point.id}
							onclick={() => selectPoint(group, point)}
						>
							<span>{point.campaignName}</span>
							<span>{formatPointValue(point)}</span>
							<span>{formatDelta(point)}</span>
							<span>{formatCountValue(point)}</span>
						</button>
					{/each}
				</div>
			</section>
		{/each}
	</div>
{:else}
	<p class="text-sm text-[var(--color-text-muted)]">{formatWidgetLabel('noWaveTrendPoints', copy)}</p>
{/if}
