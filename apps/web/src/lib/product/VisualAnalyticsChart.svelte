<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { EChartsOption } from 'echarts';
	import type { EChartsType } from 'echarts/core';
	import type { VisualAnalyticsChartView } from './visual-analytics';

	let { view, testId }: { view: VisualAnalyticsChartView; testId: string } = $props();

	let chartElement = $state<HTMLDivElement | null>(null);
	let chart: EChartsType | null = null;
	let mounted = $state(false);
	let loadState = $state<'idle' | 'loading' | 'ready' | 'failed'>('idle');

	onMount(() => {
		mounted = true;

		const resize = () => chart?.resize();
		window.addEventListener('resize', resize);

		return () => {
			window.removeEventListener('resize', resize);
		};
	});

	onDestroy(() => {
		chart?.dispose();
		chart = null;
	});

	$effect(() => {
		if (!mounted || !chartElement || view.points.length === 0) {
			return;
		}

		void renderChart();
	});

	async function renderChart() {
		if (!chartElement || view.points.length === 0) {
			return;
		}

		loadState = chart ? 'ready' : 'loading';

		try {
			const [{ init, use }, { BarChart }, { GridComponent, LegendComponent, TooltipComponent }, { CanvasRenderer }] =
				await Promise.all([
					import('echarts/core'),
					import('echarts/charts'),
					import('echarts/components'),
					import('echarts/renderers')
				]);

			use([BarChart, GridComponent, LegendComponent, TooltipComponent, CanvasRenderer]);
			chart ??= init(chartElement, undefined, { renderer: 'canvas' });
			chart.setOption(toChartOption(view), true);
			loadState = 'ready';
		} catch {
			loadState = 'failed';
		}
	}

	// Validated Spectra chart palette (ADR-0018): series-1 indigo, series-2 teal (CVD-checked pair).
	// Status colors are never used as series colors; gray is reserved for reference annotations.
	const chartSeriesColors = ['#08569a', '#008065'];
	const chartInk = '#14181c';
	const chartMutedInk = '#636a6f';
	const chartHairline = '#dce2e8';

	function toChartOption(chartView: VisualAnalyticsChartView): EChartsOption {
		const secondaryValues = chartView.points.map((point) => point.secondaryValue);
		const hasSecondarySeries = secondaryValues.some((value) => value !== null);
		const series: EChartsOption['series'] = [
			{
				type: 'bar',
				name: chartView.primarySeriesLabel,
				data: chartView.points.map((point) => point.primaryValue),
				barMaxWidth: 32,
				itemStyle: { color: chartSeriesColors[0], borderRadius: [2, 2, 0, 0] }
			}
		];

		if (hasSecondarySeries && chartView.secondarySeriesLabel) {
			series.push({
				type: 'bar',
				name: chartView.secondarySeriesLabel,
				data: secondaryValues.map((value) => value ?? null),
				barMaxWidth: 32,
				itemStyle: { color: chartSeriesColors[1], borderRadius: [2, 2, 0, 0] }
			});
		}

		return {
			animation: false,
			textStyle: { color: chartMutedInk },
			grid: { left: 48, right: 16, top: 32, bottom: 48 },
			legend: { show: hasSecondarySeries, bottom: 0, textStyle: { color: chartMutedInk } },
			tooltip: {
				trigger: 'axis',
				backgroundColor: '#ffffff',
				borderColor: chartHairline,
				textStyle: { color: chartInk, fontSize: 12 }
			},
			xAxis: {
				type: 'category',
				data: chartView.points.map((point) => point.label),
				axisLabel: { interval: 0, color: chartMutedInk },
				axisLine: { lineStyle: { color: chartHairline } },
				axisTick: { show: false }
			},
			yAxis: {
				type: 'value',
				name: chartView.yAxisLabel,
				nameGap: 28,
				nameTextStyle: { color: chartMutedInk },
				axisLabel: { color: chartMutedInk },
				splitLine: { lineStyle: { color: chartHairline } }
			},
			series
		};
	}
</script>

<section class="visual-analytics" role="group" aria-label={view.ariaLabel}>
	<div class="visual-analytics__header">
		<div>
			<p class="product-kicker">{view.kicker}</p>
			<h5 class="text-base font-semibold text-[var(--color-text)]">{view.title}</h5>
		</div>
		<p class="step-pill" data-state="proof_only">{view.statusLabel}</p>
	</div>

	{#if view.points.length > 0}
		<div
			bind:this={chartElement}
			class="visual-analytics__chart"
			data-chart-state={loadState}
			data-testid={testId}
			aria-hidden="true"
		></div>

		<ul class="visual-analytics__values" aria-label={view.chartValuesAria(view.title)}>
			{#each view.points as point (point.id)}
				<li class="visual-analytics__value-row">
					<div>
						<p class="score-card__label">{point.label}</p>
						<p class="score-card__value">{view.primarySeriesLabel} {point.primaryDisplay}</p>
						{#if point.secondaryDisplay && view.secondarySeriesLabel}
							<p class="score-card__interpretation">
								{view.secondarySeriesLabel} {point.secondaryDisplay}
							</p>
						{/if}
					</div>
					<div class="response-lab__meta">
						{#each point.meta as item}
							<span>{item}</span>
						{/each}
					</div>
				</li>
			{/each}
		</ul>
	{:else}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">{view.noChartableValuesTitle}</strong>
			<span>{view.emptyMessage}</span>
		</p>
	{/if}

	{#if loadState === 'failed'}
		<p class="error-line">{view.rendererFailedMessage}</p>
	{/if}

	{#if view.excludedRows.length > 0}
		<div class="visual-analytics__excluded">
			<p class="record-field__label">{view.excludedFromChartLabel}</p>
			<ul class="visual-analytics__excluded-list" aria-label={view.excludedRowsAria(view.title)}>
				{#each view.excludedRows as row (row.id)}
					<li>
						<span>{row.label}</span>
						<span>{row.state}</span>
						<span>{row.reason}</span>
					</li>
				{/each}
			</ul>
		</div>
	{/if}
</section>
