<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import { isVisualAnalyticsEntryWidgetData } from './report-widget-data';
	import {
		formatCodeLabel,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isVisualAnalyticsEntryWidgetData(widget.data) ? widget.data : null);
	const analytics = $derived(data?.analytics ?? null);
	const groupCount = $derived(
		new Set(analytics?.groupRows.map((row) => `${row.groupType}\u0000${row.groupName}`) ?? []).size
	);
	const waveCount = $derived(new Set(analytics?.waveRows.map((row) => row.campaignId) ?? []).size);
	const visibleOutputRows = $derived(
		analytics?.scoreOutputs.filter((row) => row.disclosure === 'visible' && row.mean !== null) ?? []
	);
	const highestOutput = $derived(
		[...visibleOutputRows].sort((left, right) => (right.mean ?? 0) - (left.mean ?? 0))[0] ?? null
	);
	const lowestOutput = $derived(
		[...visibleOutputRows].sort((left, right) => (left.mean ?? 0) - (right.mean ?? 0))[0] ?? null
	);
	const largestWaveChange = $derived(
		[
			...(analytics?.waveRows.filter(
				(row) =>
					row.disclosure === 'visible' &&
					row.comparisonState === 'compared' &&
					row.deltaFromPreviousMean !== null
			) ?? [])
		].sort(
			(left, right) =>
				Math.abs(right.deltaFromPreviousMean ?? 0) - Math.abs(left.deltaFromPreviousMean ?? 0)
		)[0] ?? null
	);

	function formatScore(value: number | null | undefined) {
		return value === null || value === undefined ? (copy?.notAvailable ?? 'Not available') : value.toFixed(2);
	}

	function isVisible(disclosure: string) {
		return disclosure === 'visible';
	}

	function formatSuppressed() {
		return formatCodeLabel('suppressed', copy);
	}

	function formatVisibleCount(value: number | null | undefined, disclosure: string) {
		if (!isVisible(disclosure)) {
			return formatSuppressed();
		}

		return value === null || value === undefined ? (copy?.notAvailable ?? 'Not available') : String(value);
	}

	function formatVisibleScore(value: number | null | undefined, disclosure: string) {
		return isVisible(disclosure) ? formatScore(value) : formatSuppressed();
	}

	function formatRange(min: number | null | undefined, max: number | null | undefined) {
		if (min === null || min === undefined || max === null || max === undefined) {
			return copy?.notAvailable ?? 'Not available';
		}

		return `${min.toFixed(2)}-${max.toFixed(2)}`;
	}

	function formatVisibleRange(
		min: number | null | undefined,
		max: number | null | undefined,
		disclosure: string
	) {
		return isVisible(disclosure) ? formatRange(min, max) : formatSuppressed();
	}

	function formatDelta(value: number | null | undefined, state: string) {
		if (state === 'baseline') {
			return formatWidgetLabel('baseline', copy);
		}

		if (value === null || value === undefined) {
			return copy?.notAvailable ?? 'Not available';
		}

		return value > 0 ? `+${value.toFixed(2)}` : value.toFixed(2);
	}

	function formatVisibleDelta(value: number | null | undefined, state: string, disclosure: string) {
		return isVisible(disclosure) ? formatDelta(value, state) : formatSuppressed();
	}

	function formatResultName(value: string) {
		return formatCodeLabel(value, copy);
	}

	function formatVisibleMissingness(value: string | null | undefined, disclosure: string) {
		if (!isVisible(disclosure)) {
			return formatSuppressed();
		}

		return value ? formatCodeLabel(value, copy) : (copy?.notAvailable ?? 'Not available');
	}
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('resultOutputs', copy)}</dt>
				<dd class="metric-card__value">{analytics?.scoreOutputs.length ?? data.visibleScoreCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('groupBreakdown', copy)}</dt>
				<dd class="metric-card__value">{analytics ? groupCount : 0}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">{formatWidgetLabel('waveBreakdown', copy)}</dt>
				<dd class="metric-card__value">{analytics ? waveCount : data.reportableCampaignCount}</dd>
			</div>
		</dl>

		{#if analytics}
			<dl class="record-grid" aria-label={formatWidgetLabel('highlights', copy)}>
				<div class="metric-card">
					<dt class="metric-card__label">{formatWidgetLabel('highestMean', copy)}</dt>
					<dd class="metric-card__value">{highestOutput ? formatScore(highestOutput.mean) : copy?.notAvailable ?? 'Not available'}</dd>
					{#if highestOutput}
						<dd class="metric-card__label">{formatResultName(highestOutput.dimensionCode)}</dd>
					{/if}
				</div>
				<div class="metric-card">
					<dt class="metric-card__label">{formatWidgetLabel('lowestMean', copy)}</dt>
					<dd class="metric-card__value">{lowestOutput ? formatScore(lowestOutput.mean) : copy?.notAvailable ?? 'Not available'}</dd>
					{#if lowestOutput}
						<dd class="metric-card__label">{formatResultName(lowestOutput.dimensionCode)}</dd>
					{/if}
				</div>
				<div class="metric-card">
					<dt class="metric-card__label">{formatWidgetLabel('largestWaveChange', copy)}</dt>
					<dd class="metric-card__value">
						{largestWaveChange
							? formatDelta(largestWaveChange.deltaFromPreviousMean, largestWaveChange.comparisonState)
							: copy?.notAvailable ?? 'Not available'}
					</dd>
					{#if largestWaveChange}
						<dd class="metric-card__label">
							{largestWaveChange.campaignName} / {formatResultName(largestWaveChange.dimensionCode)}
						</dd>
					{/if}
				</div>
			</dl>

			<div class="record-list" aria-label={formatWidgetLabel('insights', copy)}>
				{#each analytics.insights as insight, index (`${insight.kind}\u0000${index}`)}
					<article class="record-row" data-state={insight.severity}>
						<div class="record-row__header">
							<p class="record-row__title">{insight.title}</p>
							<span class="status-badge" data-status={insight.severity}>
								{formatCodeLabel(insight.severity, copy)}
							</span>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">{insight.detail}</p>
					</article>
				{/each}
			</div>

			<section class="record-row" aria-label={formatWidgetLabel('resultMatrix', copy)}>
				<div class="record-row__header">
					<p class="record-row__title">{formatWidgetLabel('resultOutputs', copy)}</p>
					<span class="record-field__label">
						{formatWidgetLabel('disclosure', copy)}: {formatCodeLabel(analytics.disclosureState, copy)}
					</span>
				</div>
				<div class="overflow-x-auto">
					<table class="min-w-full text-left text-sm">
						<thead class="text-xs uppercase text-[var(--color-text-muted)]">
							<tr>
								<th class="py-2 pr-4">{formatWidgetLabel('resultName', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('sample', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('mean', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('median', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('standardDeviation', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('range', copy)}</th>
								<th class="py-2 pr-4">{formatWidgetLabel('missingness', copy)}</th>
							</tr>
						</thead>
						<tbody>
							{#each analytics.scoreOutputs as row (row.dimensionCode)}
								<tr class="border-t border-[var(--color-border-muted)]">
									<td class="py-2 pr-4 font-medium">{formatResultName(row.dimensionCode)}</td>
									<td class="py-2 pr-4">{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
									<td class="py-2 pr-4">{formatVisibleScore(row.mean, row.disclosure)}</td>
									<td class="py-2 pr-4">{formatVisibleScore(row.median, row.disclosure)}</td>
									<td class="py-2 pr-4">{formatVisibleScore(row.standardDeviation, row.disclosure)}</td>
									<td class="py-2 pr-4">{formatVisibleRange(row.min, row.max, row.disclosure)}</td>
									<td class="py-2 pr-4">{formatVisibleMissingness(row.missingPolicyStatusSummary, row.disclosure)}</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			</section>

			<section class="record-row" aria-label={formatWidgetLabel('groupMatrix', copy)}>
				<div class="record-row__header">
					<p class="record-row__title">{formatWidgetLabel('groupMatrix', copy)}</p>
					<span class="record-field__label">
						{formatWidgetLabel('sample', copy)} >= {analytics.disclosureKMin}
					</span>
				</div>
				{#if analytics.groupRows.length > 0}
					<div class="overflow-x-auto">
						<table class="min-w-full text-left text-sm">
							<thead class="text-xs uppercase text-[var(--color-text-muted)]">
								<tr>
									<th class="py-2 pr-4">{formatWidgetLabel('group', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('groupType', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('resultName', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('sample', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('mean', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('median', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('range', copy)}</th>
								</tr>
							</thead>
							<tbody>
								{#each analytics.groupRows as row (`${row.groupType}\u0000${row.groupName}\u0000${row.dimensionCode}`)}
									<tr class="border-t border-[var(--color-border-muted)]">
										<td class="py-2 pr-4 font-medium">{row.groupName}</td>
										<td class="py-2 pr-4">{formatCodeLabel(row.groupType, copy)}</td>
										<td class="py-2 pr-4">{formatResultName(row.dimensionCode)}</td>
										<td class="py-2 pr-4">{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
										<td class="py-2 pr-4">{formatVisibleScore(row.mean, row.disclosure)}</td>
										<td class="py-2 pr-4">{formatVisibleScore(row.median, row.disclosure)}</td>
										<td class="py-2 pr-4">{formatVisibleRange(row.min, row.max, row.disclosure)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{:else}
					<p class="text-sm text-[var(--color-text-muted)]">
						{formatWidgetLabel('noGroupBreakdown', copy)}
					</p>
				{/if}
			</section>

			<section class="record-row" aria-label={formatWidgetLabel('waveTrend', copy)}>
				<div class="record-row__header">
					<p class="record-row__title">{formatWidgetLabel('waveTrend', copy)}</p>
					<span class="record-field__label">{formatWidgetLabel('waveBreakdown', copy)}</span>
				</div>
				{#if analytics.waveRows.length > 0}
					<div class="overflow-x-auto">
						<table class="min-w-full text-left text-sm">
							<thead class="text-xs uppercase text-[var(--color-text-muted)]">
								<tr>
									<th class="py-2 pr-4">{formatWidgetLabel('campaign', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('resultName', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('sample', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('mean', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('median', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('fromPrevious', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('fromFirst', copy)}</th>
									<th class="py-2 pr-4">{formatWidgetLabel('dataFinality', copy)}</th>
								</tr>
							</thead>
							<tbody>
								{#each analytics.waveRows as row (`${row.campaignId}\u0000${row.dimensionCode}`)}
									<tr class="border-t border-[var(--color-border-muted)]">
										<td class="py-2 pr-4 font-medium">{row.campaignName}</td>
										<td class="py-2 pr-4">{formatResultName(row.dimensionCode)}</td>
										<td class="py-2 pr-4">{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
										<td class="py-2 pr-4">{formatVisibleScore(row.mean, row.disclosure)}</td>
										<td class="py-2 pr-4">{formatVisibleScore(row.median, row.disclosure)}</td>
										<td class="py-2 pr-4">
											{formatVisibleDelta(row.deltaFromPreviousMean, row.comparisonState, row.disclosure)}
										</td>
										<td class="py-2 pr-4">
											{formatVisibleDelta(row.deltaFromFirstMean, row.comparisonState, row.disclosure)}
										</td>
										<td class="py-2 pr-4">{formatCodeLabel(row.dataFinality, copy)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{:else}
					<p class="text-sm text-[var(--color-text-muted)]">
						{formatWidgetLabel('noWaveBreakdown', copy)}
					</p>
				{/if}
			</section>
		{:else}
			<dl class="record-grid">
				<div class="record-field">
					<dt class="record-field__label">{formatWidgetLabel('selectedCampaign', copy)}</dt>
					<dd class="record-field__value">
						{data.selectedCampaignId
							? formatWidgetLabel('available', copy)
							: formatWidgetLabel('notSelected', copy)}
					</dd>
				</div>
				<div class="record-field">
					<dt class="record-field__label">{formatWidgetLabel('previewSource', copy)}</dt>
					<dd class="record-field__value">
						{widget.dataSource
							? `${widget.dataSource.method} ${formatWidgetLabel('reportPreview', copy)}`
							: formatWidgetLabel('notConfiguredState', copy)}
					</dd>
				</div>
			</dl>
		{/if}
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('visualAnalyticsDataUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
