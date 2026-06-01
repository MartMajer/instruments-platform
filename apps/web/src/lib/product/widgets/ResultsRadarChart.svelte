<script lang="ts">
	import type { ResultsRadarProfile } from '$lib/product/results-workbench';
	import {
		formatCodeLabel,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';

	let {
		profile,
		copy
	}: {
		profile: ResultsRadarProfile;
		copy?: ReportWidgetFormatCopy;
	} = $props();

	const chartPoints = $derived(toRadarCoordinates(profile));
	const polygonPoints = $derived(
		chartPoints.map((point) => `${point.x.toFixed(2)},${point.y.toFixed(2)}`).join(' ')
	);
	const axisPoints = $derived(toAxisCoordinates(profile.points.length));
	const canDrawRadar = $derived(profile.canDrawRadar && profile.points.length >= 3);

	function toAxisCoordinates(count: number) {
		return Array.from({ length: count }, (_, index) => polarPoint(index, count, 42));
	}

	function toRadarCoordinates(currentProfile: ResultsRadarProfile) {
		return currentProfile.points.map((point, index) => {
			const label = polarPoint(index, currentProfile.points.length, 47);
			return {
				...polarPoint(index, currentProfile.points.length, point.positionPercent * 0.42),
				labelX: label.x,
				labelY: label.y,
				point
			};
		});
	}

	function polarPoint(index: number, count: number, radius: number) {
		const angle = -Math.PI / 2 + (index / Math.max(count, 1)) * Math.PI * 2;
		return {
			x: 50 + Math.cos(angle) * radius,
			y: 50 + Math.sin(angle) * radius
		};
	}

	function barStyle(positionPercent: number) {
		return `width: ${Math.max(4, Math.min(100, positionPercent))}%`;
	}
</script>

<div class="results-radar" data-mode={canDrawRadar ? 'radar' : 'compact'}>
	<div class="results-radar__scale" aria-label={formatWidgetLabel('profileScale', copy)}>
		<span>0% {formatWidgetLabel('lowerRange', copy)}</span>
		<span>50%</span>
		<span>100% {formatWidgetLabel('higherRange', copy)}</span>
	</div>

	{#if canDrawRadar}
		<div class="results-radar__frame">
			<svg
				viewBox="0 0 100 100"
				role="img"
				aria-label={`${formatWidgetLabel('profileRadar', copy)}: ${formatWidgetLabel('profileScale', copy)}`}
			>
				<circle class="results-radar__ring" cx="50" cy="50" r="42" />
				<circle class="results-radar__ring" cx="50" cy="50" r="28" />
				<circle class="results-radar__ring" cx="50" cy="50" r="14" />
				<text class="results-radar__scale-label" x="50" y="51">0%</text>
				<text class="results-radar__scale-label" x="50" y="22">50%</text>
				<text class="results-radar__scale-label" x="50" y="8">100%</text>
				{#each axisPoints as axis, index (`axis-${index}`)}
					<line class="results-radar__axis" x1="50" y1="50" x2={axis.x} y2={axis.y} />
				{/each}
				<polygon class="results-radar__shape" points={polygonPoints} />
				{#each chartPoints as chartPoint (chartPoint.point.id)}
					<g class="results-radar__point-marker">
						<title>
							{chartPoint.point.displayIndex}. {chartPoint.point.label}: {chartPoint.point
								.valueLabel}
						</title>
						<circle cx={chartPoint.x} cy={chartPoint.y} r="4" />
						<text x={chartPoint.x} y={chartPoint.y + 1.2}>{chartPoint.point.displayIndex}</text>
					</g>
					<text class="results-radar__axis-label" x={chartPoint.labelX} y={chartPoint.labelY + 1}>
						{chartPoint.point.displayIndex}
					</text>
				{/each}
			</svg>
		</div>
	{:else}
		<p class="results-radar__fallback text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('profileUnavailable', copy)}
		</p>
	{/if}

	<div class="results-radar__values" aria-label={formatWidgetLabel('resultProfile', copy)}>
		{#if profile.rangeLabel}
			<p class="record-field__label">{profile.rangeLabel}</p>
		{/if}
		{#if profile.points.length > 0}
			<ul>
				{#each profile.points as point (point.id)}
					<li>
						<small class="results-radar__legend-index">{point.displayIndex}</small>
						<span>{point.label}</span>
						<strong>{point.valueLabel}</strong>
						<small>{point.positionPercent.toFixed(0)}%</small>
						<span class="results-radar__bar" aria-hidden="true">
							<span style={barStyle(point.positionPercent)}></span>
						</span>
					</li>
				{/each}
			</ul>
		{:else}
			<p class="text-sm text-[var(--color-text-muted)]">
				{formatWidgetLabel('profileUnavailable', copy)}
			</p>
		{/if}
	</div>

	{#if profile.excluded.length > 0}
		<div class="results-radar__excluded">
			<p class="record-field__label">{formatWidgetLabel('excludedFromProfile', copy)}</p>
			<ul>
				{#each profile.excluded as item (item.id)}
					<li>
						<span>{item.label}</span>
						<small>{formatCodeLabel(item.reason, copy)}</small>
					</li>
				{/each}
			</ul>
		</div>
	{/if}
</div>
