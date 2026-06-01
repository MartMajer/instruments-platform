<script lang="ts">
	import type { ResultsGroupHeatmap } from '$lib/product/results-workbench';
	import {
		formatCodeLabel,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';

	let {
		heatmap,
		copy
	}: {
		heatmap: ResultsGroupHeatmap;
		copy?: ReportWidgetFormatCopy;
	} = $props();

	function cellStyle(positionPercent: number | null) {
		const alpha = positionPercent === null ? 0 : 0.16 + (positionPercent / 100) * 0.58;
		return `--heat-alpha: ${(alpha * 100).toFixed(0)}%`;
	}
</script>

{#if heatmap.rows.length > 0 && heatmap.columns.length > 0}
	<div
		class="results-heatmap"
		role="table"
		aria-label={formatWidgetLabel('groupHeatmap', copy)}
		style={`--heatmap-columns: ${heatmap.columns.length}`}
	>
		<div class="results-heatmap__header" role="row">
			<div class="results-heatmap__corner" role="columnheader">
				{formatWidgetLabel('group', copy)}
			</div>
			{#each heatmap.columns as column (column.id)}
				<div class="results-heatmap__column" role="columnheader">{column.label}</div>
			{/each}
		</div>
		{#each heatmap.rows as row (row.id)}
			<div class="results-heatmap__row" role="row">
				<div class="results-heatmap__row-label" role="rowheader">
					<strong>{row.label}</strong>
					<small>{formatCodeLabel(row.groupType, copy)}</small>
				</div>
				{#each row.cells as cell (cell.id)}
					<div
						class="results-heatmap__cell"
						data-tone={cell.tone}
						data-disclosure={cell.disclosure}
						style={cellStyle(cell.positionPercent)}
						role="cell"
						aria-label={`${row.label}, ${cell.columnId}: ${cell.valueLabel}`}
					>
						<strong>{cell.valueLabel}</strong>
						<small>{formatWidgetLabel('sample', copy)} {cell.sampleLabel}</small>
						{#if cell.suppressionReason}
							<small>{formatCodeLabel(cell.suppressionReason, copy)}</small>
						{/if}
					</div>
				{/each}
			</div>
		{/each}
	</div>
{:else}
	<p class="record-row text-sm text-[var(--color-text-muted)]">
		<strong class="record-row__title">{formatWidgetLabel('heatmapUnavailable', copy)}</strong>
		<span>{formatWidgetLabel('noGroupBreakdown', copy)}</span>
	</p>
{/if}
