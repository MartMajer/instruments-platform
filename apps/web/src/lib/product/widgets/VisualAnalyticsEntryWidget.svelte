<script lang="ts">
	import type {
		CampaignSeriesResultsGroupMatrixRowResponse,
		CampaignSeriesResultsInsightResponse,
		CampaignSeriesResultsScoreOutputResponse,
		CampaignSeriesResultsWaveMatrixRowResponse,
		ReportWidget
	} from '$lib/api/product';
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
	type SortDirection = 'asc' | 'desc';
	type OutputSortKey = 'result' | 'sample' | 'mean' | 'median' | 'range' | 'missingness';
	type GroupSortKey = 'group' | 'result' | 'sample' | 'mean' | 'median' | 'range';
	type WaveSortKey = 'campaign' | 'result' | 'sample' | 'mean' | 'median' | 'previous' | 'first';
	type SortState<T extends string> = {
		key: T;
		direction: SortDirection;
	};

	let outputSort = $state<SortState<OutputSortKey>>({ key: 'mean', direction: 'desc' });
	let groupSort = $state<SortState<GroupSortKey>>({ key: 'mean', direction: 'desc' });
	let waveSort = $state<SortState<WaveSortKey>>({ key: 'campaign', direction: 'asc' });
	const sortedScoreOutputs = $derived(sortScoreOutputs(analytics?.scoreOutputs ?? [], outputSort));
	const sortedGroupRows = $derived(sortGroupRows(analytics?.groupRows ?? [], groupSort));
	const sortedWaveRows = $derived(sortWaveRows(analytics?.waveRows ?? [], waveSort));

	function nextSort<T extends string>(current: SortState<T>, key: T): SortState<T> {
		if (current.key !== key) {
			return { key, direction: key === 'result' || key === 'group' || key === 'campaign' ? 'asc' : 'desc' };
		}

		return { key, direction: current.direction === 'asc' ? 'desc' : 'asc' };
	}

	function sortIndicator<T extends string>(current: SortState<T>, key: T) {
		if (current.key !== key) {
			return '';
		}

		return current.direction === 'asc' ? ' ↑' : ' ↓';
	}

	function toggleOutputSort(key: OutputSortKey) {
		outputSort = nextSort(outputSort, key);
	}

	function toggleGroupSort(key: GroupSortKey) {
		groupSort = nextSort(groupSort, key);
	}

	function toggleWaveSort(key: WaveSortKey) {
		waveSort = nextSort(waveSort, key);
	}

	function disclosureRank(disclosure: string) {
		return disclosure === 'visible' ? 0 : 1;
	}

	function compareText(left: string, right: string, direction: SortDirection) {
		return direction === 'asc' ? left.localeCompare(right) : right.localeCompare(left);
	}

	function compareNullableNumber(
		left: number | null | undefined,
		right: number | null | undefined,
		direction: SortDirection
	) {
		const leftMissing = left === null || left === undefined;
		const rightMissing = right === null || right === undefined;

		if (leftMissing && rightMissing) {
			return 0;
		}

		if (leftMissing) {
			return 1;
		}

		if (rightMissing) {
			return -1;
		}

		return direction === 'asc' ? left - right : right - left;
	}

	function compareDisclosure(left: string, right: string) {
		return disclosureRank(left) - disclosureRank(right);
	}

	function rangeWidth(min: number | null | undefined, max: number | null | undefined) {
		if (min === null || min === undefined || max === null || max === undefined) {
			return null;
		}

		return max - min;
	}

	function sortScoreOutputs(
		rows: CampaignSeriesResultsScoreOutputResponse[],
		sort: SortState<OutputSortKey>
	) {
		return [...rows].sort((left, right) => {
			const disclosure = compareDisclosure(left.disclosure, right.disclosure);
			if (disclosure !== 0) {
				return disclosure;
			}

			switch (sort.key) {
				case 'result':
					return compareText(formatResultName(left.dimensionCode), formatResultName(right.dimensionCode), sort.direction);
				case 'sample':
					return compareNullableNumber(left.scoreCount, right.scoreCount, sort.direction);
				case 'mean':
					return compareNullableNumber(left.mean, right.mean, sort.direction);
				case 'median':
					return compareNullableNumber(left.median, right.median, sort.direction);
				case 'range':
					return compareNullableNumber(rangeWidth(left.min, left.max), rangeWidth(right.min, right.max), sort.direction);
				case 'missingness':
					return compareText(left.missingPolicyStatusSummary ?? '', right.missingPolicyStatusSummary ?? '', sort.direction);
			}
		});
	}

	function sortGroupRows(rows: CampaignSeriesResultsGroupMatrixRowResponse[], sort: SortState<GroupSortKey>) {
		return [...rows].sort((left, right) => {
			const disclosure = compareDisclosure(left.disclosure, right.disclosure);
			if (disclosure !== 0) {
				return disclosure;
			}

			switch (sort.key) {
				case 'group':
					return compareText(`${left.groupType} ${left.groupName}`, `${right.groupType} ${right.groupName}`, sort.direction);
				case 'result':
					return compareText(formatResultName(left.dimensionCode), formatResultName(right.dimensionCode), sort.direction);
				case 'sample':
					return compareNullableNumber(left.scoreCount, right.scoreCount, sort.direction);
				case 'mean':
					return compareNullableNumber(left.mean, right.mean, sort.direction);
				case 'median':
					return compareNullableNumber(left.median, right.median, sort.direction);
				case 'range':
					return compareNullableNumber(rangeWidth(left.min, left.max), rangeWidth(right.min, right.max), sort.direction);
			}
		});
	}

	function sortWaveRows(rows: CampaignSeriesResultsWaveMatrixRowResponse[], sort: SortState<WaveSortKey>) {
		return [...rows].sort((left, right) => {
			const disclosure = compareDisclosure(left.disclosure, right.disclosure);
			if (disclosure !== 0) {
				return disclosure;
			}

			switch (sort.key) {
				case 'campaign':
					return compareText(left.campaignName, right.campaignName, sort.direction);
				case 'result':
					return compareText(formatResultName(left.dimensionCode), formatResultName(right.dimensionCode), sort.direction);
				case 'sample':
					return compareNullableNumber(left.scoreCount, right.scoreCount, sort.direction);
				case 'mean':
					return compareNullableNumber(left.mean, right.mean, sort.direction);
				case 'median':
					return compareNullableNumber(left.median, right.median, sort.direction);
				case 'previous':
					return compareNullableNumber(left.deltaFromPreviousMean, right.deltaFromPreviousMean, sort.direction);
				case 'first':
					return compareNullableNumber(left.deltaFromFirstMean, right.deltaFromFirstMean, sort.direction);
			}
		});
	}

	function insightEvidence(insight: CampaignSeriesResultsInsightResponse) {
		if (insight.kind === 'score_outputs') {
			const output = highestOutput ?? lowestOutput;
			return {
				source: formatWidgetLabel('resultOutputs', copy),
				value: output ? `${formatResultName(output.dimensionCode)} / ${formatScore(output.mean)}` : copy?.notAvailable ?? 'Not available',
				target: formatWidgetLabel('inspectResultMatrix', copy)
			};
		}

		if (insight.kind === 'groups') {
			const visibleRows = analytics?.groupRows.filter((row) => row.disclosure === 'visible').length ?? 0;
			const suppressedRows = analytics?.groupRows.filter((row) => row.disclosure !== 'visible').length ?? 0;
			return {
				source: formatWidgetLabel('groupMatrix', copy),
				value: `${visibleRows} ${formatWidgetLabel('visible', copy)} / ${suppressedRows} ${formatWidgetLabel('suppressed', copy)}`,
				target: formatWidgetLabel('inspectGroupMatrix', copy)
			};
		}

		if (insight.kind === 'waves') {
			return {
				source: formatWidgetLabel('waveTrend', copy),
				value: largestWaveChange
					? `${largestWaveChange.campaignName} / ${formatDelta(
							largestWaveChange.deltaFromPreviousMean,
							largestWaveChange.comparisonState
						)}`
					: copy?.notAvailable ?? 'Not available',
				target: formatWidgetLabel('inspectWaveMatrix', copy)
			};
		}

		return {
			source: formatWidgetLabel('resultsPreview', copy),
			value: formatCodeLabel(insight.kind, copy),
			target: formatWidgetLabel('inspectMatrix', copy)
		};
	}

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

			<div class="results-insight-list" aria-label={formatWidgetLabel('insights', copy)}>
				{#each analytics.insights as insight, index (`${insight.kind}\u0000${index}`)}
					{@const evidence = insightEvidence(insight)}
					<article class="results-insight-card" data-state={insight.severity}>
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{insight.title}</p>
								<p class="text-sm leading-6 text-[var(--color-text-muted)]">{insight.detail}</p>
							</div>
							<span class="status-badge" data-status={insight.severity}>
								{formatCodeLabel(insight.severity, copy)}
							</span>
						</div>
						<dl class="results-insight-card__evidence">
							<div>
								<dt>{formatWidgetLabel('evidenceSource', copy)}</dt>
								<dd>{evidence.source}</dd>
							</div>
							<div>
								<dt>{formatWidgetLabel('currentSignal', copy)}</dt>
								<dd>{evidence.value}</dd>
							</div>
							<div>
								<dt>{formatWidgetLabel('whereToInspect', copy)}</dt>
								<dd>{evidence.target}</dd>
							</div>
						</dl>
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
					<table class="results-matrix-table">
						<thead class="text-xs uppercase text-[var(--color-text-muted)]">
							<tr>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('result')}>
										{formatWidgetLabel('resultName', copy)}{sortIndicator(outputSort, 'result')}
									</button>
								</th>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('sample')}>
										{formatWidgetLabel('sample', copy)}{sortIndicator(outputSort, 'sample')}
									</button>
								</th>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('mean')}>
										{formatWidgetLabel('mean', copy)}{sortIndicator(outputSort, 'mean')}
									</button>
								</th>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('median')}>
										{formatWidgetLabel('median', copy)}{sortIndicator(outputSort, 'median')}
									</button>
								</th>
								<th>{formatWidgetLabel('standardDeviation', copy)}</th>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('range')}>
										{formatWidgetLabel('range', copy)}{sortIndicator(outputSort, 'range')}
									</button>
								</th>
								<th>
									<button type="button" class="matrix-sort-button" onclick={() => toggleOutputSort('missingness')}>
										{formatWidgetLabel('missingness', copy)}{sortIndicator(outputSort, 'missingness')}
									</button>
								</th>
							</tr>
						</thead>
						<tbody>
							{#each sortedScoreOutputs as row (row.dimensionCode)}
								<tr data-disclosure={row.disclosure}>
									<td>{formatResultName(row.dimensionCode)}</td>
									<td>{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
									<td>{formatVisibleScore(row.mean, row.disclosure)}</td>
									<td>{formatVisibleScore(row.median, row.disclosure)}</td>
									<td>{formatVisibleScore(row.standardDeviation, row.disclosure)}</td>
									<td>{formatVisibleRange(row.min, row.max, row.disclosure)}</td>
									<td>{formatVisibleMissingness(row.missingPolicyStatusSummary, row.disclosure)}</td>
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
						<table class="results-matrix-table">
							<thead class="text-xs uppercase text-[var(--color-text-muted)]">
								<tr>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('group')}>
											{formatWidgetLabel('group', copy)}{sortIndicator(groupSort, 'group')}
										</button>
									</th>
									<th>{formatWidgetLabel('groupType', copy)}</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('result')}>
											{formatWidgetLabel('resultName', copy)}{sortIndicator(groupSort, 'result')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('sample')}>
											{formatWidgetLabel('sample', copy)}{sortIndicator(groupSort, 'sample')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('mean')}>
											{formatWidgetLabel('mean', copy)}{sortIndicator(groupSort, 'mean')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('median')}>
											{formatWidgetLabel('median', copy)}{sortIndicator(groupSort, 'median')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleGroupSort('range')}>
											{formatWidgetLabel('range', copy)}{sortIndicator(groupSort, 'range')}
										</button>
									</th>
								</tr>
							</thead>
							<tbody>
								{#each sortedGroupRows as row (`${row.groupType}\u0000${row.groupName}\u0000${row.dimensionCode}`)}
									<tr data-disclosure={row.disclosure}>
										<td>{row.groupName}</td>
										<td>{formatCodeLabel(row.groupType, copy)}</td>
										<td>{formatResultName(row.dimensionCode)}</td>
										<td>{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
										<td>{formatVisibleScore(row.mean, row.disclosure)}</td>
										<td>{formatVisibleScore(row.median, row.disclosure)}</td>
										<td>{formatVisibleRange(row.min, row.max, row.disclosure)}</td>
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
						<table class="results-matrix-table">
							<thead class="text-xs uppercase text-[var(--color-text-muted)]">
								<tr>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('campaign')}>
											{formatWidgetLabel('campaign', copy)}{sortIndicator(waveSort, 'campaign')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('result')}>
											{formatWidgetLabel('resultName', copy)}{sortIndicator(waveSort, 'result')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('sample')}>
											{formatWidgetLabel('sample', copy)}{sortIndicator(waveSort, 'sample')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('mean')}>
											{formatWidgetLabel('mean', copy)}{sortIndicator(waveSort, 'mean')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('median')}>
											{formatWidgetLabel('median', copy)}{sortIndicator(waveSort, 'median')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('previous')}>
											{formatWidgetLabel('fromPrevious', copy)}{sortIndicator(waveSort, 'previous')}
										</button>
									</th>
									<th>
										<button type="button" class="matrix-sort-button" onclick={() => toggleWaveSort('first')}>
											{formatWidgetLabel('fromFirst', copy)}{sortIndicator(waveSort, 'first')}
										</button>
									</th>
									<th>{formatWidgetLabel('dataFinality', copy)}</th>
								</tr>
							</thead>
							<tbody>
								{#each sortedWaveRows as row (`${row.campaignId}\u0000${row.dimensionCode}`)}
									<tr data-disclosure={row.disclosure}>
										<td>{row.campaignName}</td>
										<td>{formatResultName(row.dimensionCode)}</td>
										<td>{formatVisibleCount(row.scoreCount, row.disclosure)}</td>
										<td>{formatVisibleScore(row.mean, row.disclosure)}</td>
										<td>{formatVisibleScore(row.median, row.disclosure)}</td>
										<td>
											{formatVisibleDelta(row.deltaFromPreviousMean, row.comparisonState, row.disclosure)}
										</td>
										<td>
											{formatVisibleDelta(row.deltaFromFirstMean, row.comparisonState, row.disclosure)}
										</td>
										<td>{formatCodeLabel(row.dataFinality, copy)}</td>
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
