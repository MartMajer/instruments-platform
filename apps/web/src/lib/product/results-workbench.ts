import type {
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesResultsAnalyticsResponse,
	CampaignSeriesResultsDashboardResponse,
	CampaignSeriesResultsGroupMatrixRowResponse,
	CampaignSeriesResultsWaveMatrixRowResponse,
	ResultsDashboardPointResponse,
	ResultsDashboardBarResponse
} from '$lib/api/product';

export type ResultsWorkbenchScoreCard = {
	id: string;
	label: string;
	dimensionCode: string;
	disclosure: string;
	valueLabel: string;
	countLabel: string;
	methodLabel: string | null;
	rangeLabel: string;
	progressPercent: number | null;
	isSuppressed: boolean;
	suppressionReason: string | null;
};

export type ResultsScoreRange = {
	min: number;
	max: number;
};

export type ResultsWorkbenchComparisonSummary = {
	outputCount: number;
	visibleOutputCount: number;
	suppressedOutputCount: number;
	groupRowCount: number;
	visibleGroupRowCount: number;
	waveRowCount: number;
	comparableWaveCount: number;
};

export type ResultsWorkbenchExportSummary = {
	hasResultsMatrix: boolean;
	hasResponseDataset: boolean;
	downloadLabel: string;
	primaryGuidance: string;
};

export type ResultsWorkbenchModel = {
	selectedMeasurementLabel: string;
	scoreCards: ResultsWorkbenchScoreCard[];
	comparisons: ResultsWorkbenchComparisonSummary;
	exports: ResultsWorkbenchExportSummary;
};

export type ResultsInterpretationCockpit = {
	header: ResultsInterpretationHeader;
	attentionCards: ResultsAttentionCard[];
	radar: ResultsRadarProfile;
	heatmap: ResultsGroupHeatmap;
	trend: ResultsTrendSummary | null;
};

export type ResultsInterpretationHeader = {
	selectedMeasurementLabel: string;
	visibleResultCount: number;
	suppressedResultCount: number;
	sampleCount: number | null;
	disclosureState: string;
	dataFinality: string | null;
	interpretationStatus: string | null;
};

export type ResultsAttentionCard = {
	id: 'lowest_scale_position' | 'highest_scale_position' | 'latest_movement' | 'trust_constraint';
	title: string;
	label: string;
	valueLabel: string;
	tone: 'attention' | 'strong' | 'up' | 'down' | 'stable' | 'guarded' | 'neutral';
	detail: string;
	meta: string[];
};

export type ResultsRadarProfile = {
	points: ResultsRadarPoint[];
	excluded: ResultsExcludedResult[];
	rangeLabel: string | null;
};

export type ResultsRadarPoint = {
	id: string;
	dimensionCode: string;
	label: string;
	valueLabel: string;
	positionPercent: number;
	rawValue: number;
};

export type ResultsExcludedResult = {
	id: string;
	label: string;
	reason: 'suppressed' | 'not_numeric' | 'score_range_missing' | 'difference_range';
};

export type ResultsGroupHeatmap = {
	columns: ResultsHeatmapColumn[];
	rows: ResultsHeatmapRow[];
};

export type ResultsHeatmapColumn = {
	id: string;
	label: string;
};

export type ResultsHeatmapRow = {
	id: string;
	label: string;
	groupType: string;
	cells: ResultsHeatmapCell[];
};

export type ResultsHeatmapCell = {
	id: string;
	columnId: string;
	valueLabel: string;
	sampleLabel: string;
	positionPercent: number | null;
	disclosure: string;
	tone: 'low' | 'medium' | 'high' | 'suppressed' | 'empty';
	suppressionReason: string | null;
};

export type ResultsTrendSummary = {
	dimensionCode: string;
	label: string;
	baselineLabel: string;
	latestLabel: string;
	deltaLabel: string;
	direction: 'up' | 'down' | 'stable' | 'unavailable';
	points: ResultsDashboardPointResponse[];
};

export type ResultsFilterOption = {
	value: string;
	label: string;
	count: number;
};

export type ResultsAnalyticsFilters = {
	outputCode?: string | null;
	groupKey?: string | null;
	campaignId?: string | null;
};

export type ResultsAnalyticsFilterModel = {
	outputOptions: ResultsFilterOption[];
	groupOptions: ResultsFilterOption[];
	measurementOptions: ResultsFilterOption[];
};

export type FilteredResultsAnalytics = CampaignSeriesResultsAnalyticsResponse & {
	filteredCounts: {
		scoreOutputs: number;
		scoreOutputsTotal: number;
		groupRows: number;
		groupRowsTotal: number;
		waveRows: number;
		waveRowsTotal: number;
	};
};

export type ResultsWorkbenchLabels = {
	notAvailable: string;
	observedScale: string;
	scoreRange: string;
	suppressed: string;
};

const defaultLabels: ResultsWorkbenchLabels = {
	notAvailable: 'Not available',
	observedScale: 'Observed scale',
	scoreRange: 'Score range',
	suppressed: 'Suppressed'
};

export function toResultsWorkbenchModel(
	workspace: CampaignSeriesReportsWorkspaceResponse
): ResultsWorkbenchModel {
	const dashboard = workspace.resultsDashboard ?? null;
	const analytics = workspace.resultsAnalytics ?? null;
	const bars = sortScoreBars(dashboard?.outputBars ?? []);

	return {
		selectedMeasurementLabel:
			dashboard?.selectedCampaignName ??
			analytics?.selectedCampaignName ??
			workspace.selectedCampaign?.name ??
			workspace.series.name,
		scoreCards: toScoreCards(bars),
		comparisons: {
			outputCount: analytics?.scoreOutputs.length ?? bars.length,
			visibleOutputCount:
				analytics?.scoreOutputs.filter((row) => row.disclosure === 'visible').length ??
				bars.filter((bar) => bar.disclosure === 'visible').length,
			suppressedOutputCount:
				analytics?.scoreOutputs.filter((row) => row.disclosure !== 'visible').length ??
				bars.filter((bar) => bar.disclosure !== 'visible').length,
			groupRowCount: analytics?.groupRows.length ?? 0,
			visibleGroupRowCount:
				analytics?.groupRows.filter((row) => row.disclosure === 'visible').length ?? 0,
			waveRowCount: analytics?.waveRows.length ?? 0,
			comparableWaveCount: analytics
				? new Set(
						analytics.waveRows
							.filter(
								(row) =>
									row.disclosure === 'visible' &&
									(row.comparisonState === 'baseline' || row.comparisonState === 'compared')
							)
							.map((row) => row.campaignId)
					).size
				: 0
		},
		exports: toExportSummary(workspace)
	};
}

export function toResultsInterpretationCockpit(
	dashboard: CampaignSeriesResultsDashboardResponse,
	analytics: CampaignSeriesResultsAnalyticsResponse | null | undefined,
	options: {
		selectedOutputCode?: string | null;
		labels?: ResultsWorkbenchLabels;
	} = {}
): ResultsInterpretationCockpit {
	const labels = options.labels ?? defaultLabels;
	const outputBars = dashboard.outputBars;
	const visibleBars = outputBars.filter((bar) => isVisibleNumeric(bar));
	const suppressedResultCount = outputBars.filter((bar) => bar.disclosure !== 'visible').length;
	const sampleCount = maxNullable(outputBars.map((bar) => bar.count));
	const trend = toResultsTrendSummary(
		dashboard.waveTrendPoints,
		outputBars,
		options.selectedOutputCode,
		labels
	);

	return {
		header: {
			selectedMeasurementLabel:
				dashboard.selectedCampaignName ?? analytics?.selectedCampaignName ?? 'Selected measurement',
			visibleResultCount: visibleBars.length,
			suppressedResultCount,
			sampleCount,
			disclosureState: dashboard.disclosureState,
			dataFinality: mostRecentFinality(dashboard.waveTrendPoints, analytics?.waveRows ?? []),
			interpretationStatus: null
		},
		attentionCards: toAttentionCards(outputBars, analytics, trend, labels),
		radar: toRadarProfile(outputBars, labels),
		heatmap: toGroupHeatmap(analytics?.groupRows ?? [], labels),
		trend
	};
}

export function toResultFocusOptions(
	bars: ResultsDashboardBarResponse[],
	allLabel = 'All result outputs'
): ResultsFilterOption[] {
	const byDimension = new Map<string, ResultsFilterOption>();
	for (const bar of bars) {
		const existing = byDimension.get(bar.dimensionCode);
		if (existing) {
			existing.count += 1;
			continue;
		}

		byDimension.set(bar.dimensionCode, {
			value: bar.dimensionCode,
			label: resultBarDisplayLabel(bar),
			count: 1
		});
	}

	return [
		{
			value: 'all',
			label: allLabel,
			count: bars.length
		},
		...byDimension.values()
	];
}

export function filterResultsDashboard(
	dashboard: CampaignSeriesResultsDashboardResponse,
	selectedOutputCode: string | null | undefined
): CampaignSeriesResultsDashboardResponse {
	if (!selectedOutputCode || selectedOutputCode === 'all') {
		return dashboard;
	}

	return {
		...dashboard,
		outputBars: dashboard.outputBars.filter((bar) => bar.dimensionCode === selectedOutputCode),
		groupBars: dashboard.groupBars.filter((bar) => bar.dimensionCode === selectedOutputCode),
		waveTrendPoints: dashboard.waveTrendPoints.filter(
			(point) => point.dimensionCode === selectedOutputCode
		)
	};
}

export function toAnalyticsFilterModel(
	analytics: CampaignSeriesResultsAnalyticsResponse,
	labels: {
		allOutputs?: string;
		allGroups?: string;
		allMeasurements?: string;
	} = {}
): ResultsAnalyticsFilterModel {
	return {
		outputOptions: toResultFocusOptionsFromRows(
			analytics.scoreOutputs,
			labels.allOutputs ?? 'All result outputs'
		),
		groupOptions: toGroupedOptions(
			analytics.groupRows.map((row) => ({
				value: groupFilterKey(row.groupType, row.groupName),
				label: row.groupName
			})),
			labels.allGroups ?? 'All groups'
		),
		measurementOptions: toGroupedOptions(
			analytics.waveRows.map((row) => ({
				value: row.campaignId,
				label: row.campaignName
			})),
			labels.allMeasurements ?? 'All measurements'
		)
	};
}

export function filterResultsAnalytics(
	analytics: CampaignSeriesResultsAnalyticsResponse,
	filters: ResultsAnalyticsFilters
): FilteredResultsAnalytics {
	const outputCode = activeFilterValue(filters.outputCode);
	const groupKey = activeFilterValue(filters.groupKey);
	const campaignId = activeFilterValue(filters.campaignId);
	const scoreOutputs = outputCode
		? analytics.scoreOutputs.filter((row) => row.dimensionCode === outputCode)
		: analytics.scoreOutputs;
	const groupRows = analytics.groupRows.filter((row) => {
		if (outputCode && row.dimensionCode !== outputCode) {
			return false;
		}

		if (groupKey && groupFilterKey(row.groupType, row.groupName) !== groupKey) {
			return false;
		}

		return true;
	});
	const waveRows = analytics.waveRows.filter((row) => {
		if (outputCode && row.dimensionCode !== outputCode) {
			return false;
		}

		if (campaignId && row.campaignId !== campaignId) {
			return false;
		}

		return true;
	});

	return {
		...analytics,
		scoreOutputs,
		groupRows,
		waveRows,
		filteredCounts: {
			scoreOutputs: scoreOutputs.length,
			scoreOutputsTotal: analytics.scoreOutputs.length,
			groupRows: groupRows.length,
			groupRowsTotal: analytics.groupRows.length,
			waveRows: waveRows.length,
			waveRowsTotal: analytics.waveRows.length
		}
	};
}

export function resultBarDisplayLabel(
	bar: ResultsDashboardBarResponse,
	mode: 'display' | 'full' = 'display'
) {
	if (mode === 'full') {
		return bar.label?.trim() || bar.displayLabel?.trim() || bar.dimensionCode;
	}

	return bar.displayLabel?.trim() || bar.label?.trim() || bar.dimensionCode;
}

export function groupFilterKey(groupType: string, groupName: string) {
	return `${groupType}\u0000${groupName}`;
}

export function toScoreCards(
	bars: ResultsDashboardBarResponse[],
	labels: ResultsWorkbenchLabels = defaultLabels
): ResultsWorkbenchScoreCard[] {
	const observedMax = Math.max(
		1,
		...bars
			.filter((bar) => bar.disclosure === 'visible' && bar.value !== null)
			.map((bar) => bar.value ?? 0)
	);

	return bars.map((bar) => {
		const configuredRange = configuredScoreRange(bar);
		const rangeLabel = configuredRange
			? `${labels.scoreRange} ${formatCompactNumber(configuredRange.min)}-${formatCompactNumber(configuredRange.max)}`
			: `${labels.observedScale} 0-${formatCompactNumber(observedMax)}`;
		const progressPercent =
			bar.disclosure === 'visible' && bar.value !== null
				? scoreProgressPercent(bar.value, configuredRange, observedMax)
				: null;

		return {
			id: bar.id,
			label: bar.displayLabel?.trim() || bar.label,
			dimensionCode: bar.dimensionCode,
			disclosure: bar.disclosure,
			valueLabel: formatVisibleNumber(bar.value, bar.disclosure, labels),
			countLabel: formatVisibleCount(bar.count, bar.disclosure, labels),
			methodLabel: bar.calculationLabel?.trim() || humanizeCalculation(bar.calculation),
			rangeLabel,
			progressPercent,
			isSuppressed: bar.disclosure !== 'visible',
			suppressionReason: bar.suppressionReason
		};
	});
}

function toAttentionCards(
	bars: ResultsDashboardBarResponse[],
	analytics: CampaignSeriesResultsAnalyticsResponse | null | undefined,
	trend: ResultsTrendSummary | null,
	labels: ResultsWorkbenchLabels
): ResultsAttentionCard[] {
	const visible = bars
		.map((bar) => toPositionedResult(bar))
		.filter((item) => item !== null)
		.sort((left, right) => left.positionPercent - right.positionPercent);
	const cards: ResultsAttentionCard[] = [];
	const lowest = visible[0];
	const highest = visible.at(-1);

	if (lowest) {
		cards.push({
			id: 'lowest_scale_position',
			title: 'Needs attention',
			label: lowest.label,
			valueLabel: formatScoreValue(lowest.rawValue),
			tone: 'attention',
			detail: `${formatCompactNumber(lowest.positionPercent)}% of configured score range`,
			meta: [lowest.rangeLabel]
		});
	}

	if (highest && highest.id !== lowest?.id) {
		cards.push({
			id: 'highest_scale_position',
			title: 'Strongest area',
			label: highest.label,
			valueLabel: formatScoreValue(highest.rawValue),
			tone: 'strong',
			detail: `${formatCompactNumber(highest.positionPercent)}% of configured score range`,
			meta: [highest.rangeLabel]
		});
	}

	const movement = largestWaveMovement(analytics?.waveRows ?? []);
	if (movement) {
		cards.push({
			id: 'latest_movement',
			title: 'Largest movement',
			label: movement.displayLabel?.trim() || movement.dimensionCode,
			valueLabel: formatSignedScoreValue(movement.deltaFromPreviousMean ?? 0),
			tone: movementTone(movement.deltaFromPreviousMean),
			detail: movement.campaignName,
			meta: [movement.dataFinality, movement.comparisonState].filter(Boolean)
		});
	} else if (trend && trend.direction !== 'unavailable') {
		cards.push({
			id: 'latest_movement',
			title: 'Latest movement',
			label: trend.label,
			valueLabel: trend.deltaLabel,
			tone: trend.direction,
			detail: `${trend.baselineLabel} to ${trend.latestLabel}`,
			meta: []
		});
	}

	const suppressedCount = bars.filter((bar) => bar.disclosure !== 'visible').length;
	if (suppressedCount > 0) {
		cards.push({
			id: 'trust_constraint',
			title: 'Trust constraint',
			label: `${suppressedCount} ${suppressedCount === 1 ? 'result hidden' : 'results hidden'}`,
			valueLabel: labels.suppressed,
			tone: 'guarded',
			detail: 'Disclosure guardrails are hiding low-sample values.',
			meta: []
		});
	}

	return cards;
}

function toRadarProfile(
	bars: ResultsDashboardBarResponse[],
	labels: ResultsWorkbenchLabels
): ResultsRadarProfile {
	const points: ResultsRadarPoint[] = [];
	const excluded: ResultsExcludedResult[] = [];
	let sharedRange: ResultsScoreRange | null = null;

	for (const bar of bars) {
		const positioned = toPositionedResult(bar);
		if (!positioned) {
			excluded.push({
				id: bar.id,
				label: resultBarDisplayLabel(bar),
				reason: exclusionReason(bar)
			});
			continue;
		}

		if (sharedRange === null) {
			sharedRange = positioned.range;
		}

		if (positioned.range.min !== sharedRange.min || positioned.range.max !== sharedRange.max) {
			excluded.push({
				id: bar.id,
				label: resultBarDisplayLabel(bar),
				reason: positioned.range.min < 0 ? 'difference_range' : 'score_range_missing'
			});
			continue;
		}

		points.push({
			id: bar.id,
			dimensionCode: bar.dimensionCode,
			label: positioned.label,
			valueLabel: formatScoreValue(positioned.rawValue),
			positionPercent: positioned.positionPercent,
			rawValue: positioned.rawValue
		});
	}

	return {
		points,
		excluded,
		rangeLabel: sharedRange
			? `${labels.scoreRange} ${formatCompactNumber(sharedRange.min)}-${formatCompactNumber(sharedRange.max)}`
			: null
	};
}

function toGroupHeatmap(
	groupRows: CampaignSeriesResultsGroupMatrixRowResponse[],
	labels: ResultsWorkbenchLabels
): ResultsGroupHeatmap {
	const columnMap = new Map<string, ResultsHeatmapColumn>();
	const rowMap = new Map<string, ResultsHeatmapRow>();

	for (const row of groupRows) {
		if (!columnMap.has(row.dimensionCode)) {
			columnMap.set(row.dimensionCode, {
				id: row.dimensionCode,
				label: row.displayLabel?.trim() || row.dimensionCode
			});
		}

		const rowKey = groupFilterKey(row.groupType, row.groupName);
		if (!rowMap.has(rowKey)) {
			rowMap.set(rowKey, {
				id: rowKey,
				label: row.groupName,
				groupType: row.groupType,
				cells: []
			});
		}
	}

	const columns = [...columnMap.values()];
	for (const row of groupRows) {
		const heatmapRow = rowMap.get(groupFilterKey(row.groupType, row.groupName));
		if (!heatmapRow) {
			continue;
		}

		heatmapRow.cells.push(toHeatmapCell(row, labels));
	}

	for (const row of rowMap.values()) {
		const existingColumns = new Set(row.cells.map((cell) => cell.columnId));
		for (const column of columns) {
			if (!existingColumns.has(column.id)) {
				row.cells.push({
					id: `${row.id}\u0000${column.id}`,
					columnId: column.id,
					valueLabel: labels.notAvailable,
					sampleLabel: labels.notAvailable,
					positionPercent: null,
					disclosure: 'missing',
					tone: 'empty',
					suppressionReason: null
				});
			}
		}
		row.cells.sort(
			(left, right) =>
				columns.findIndex((column) => column.id === left.columnId) -
				columns.findIndex((column) => column.id === right.columnId)
		);
	}

	return {
		columns,
		rows: [...rowMap.values()]
	};
}

function toHeatmapCell(
	row: CampaignSeriesResultsGroupMatrixRowResponse,
	labels: ResultsWorkbenchLabels
): ResultsHeatmapCell {
	const range = configuredScoreRange(row);
	const positionPercent =
		row.disclosure === 'visible' && row.mean !== null && range
			? scoreProgressPercent(row.mean, range, 100)
			: null;

	return {
		id: `${groupFilterKey(row.groupType, row.groupName)}\u0000${row.dimensionCode}`,
		columnId: row.dimensionCode,
		valueLabel:
			row.disclosure === 'visible' && row.mean !== null
				? formatScoreValue(row.mean)
				: labels.suppressed,
		sampleLabel:
			row.disclosure === 'visible' && row.scoreCount !== null
				? String(row.scoreCount)
				: labels.suppressed,
		positionPercent,
		disclosure: row.disclosure,
		tone: heatmapTone(row.disclosure, positionPercent),
		suppressionReason: row.suppressionReason
	};
}

function toResultsTrendSummary(
	points: ResultsDashboardPointResponse[],
	bars: ResultsDashboardBarResponse[],
	selectedOutputCode: string | null | undefined,
	labels: ResultsWorkbenchLabels
): ResultsTrendSummary | null {
	const activeOutput =
		activeFilterValue(selectedOutputCode) ??
		bars.find((bar) => bar.disclosure === 'visible' && bar.value !== null)?.dimensionCode ??
		bars[0]?.dimensionCode;
	if (!activeOutput) {
		return null;
	}

	const trendPoints = points.filter((point) => point.dimensionCode === activeOutput);
	if (trendPoints.length === 0) {
		return null;
	}

	const visiblePoints = trendPoints.filter(
		(point) => point.disclosure === 'visible' && point.value !== null
	);
	const baseline = visiblePoints[0] ?? null;
	const latest = visiblePoints.at(-1) ?? null;
	const delta =
		latest?.deltaFromPrevious ??
		(baseline && latest ? (latest.value ?? 0) - (baseline.value ?? 0) : null);

	return {
		dimensionCode: activeOutput,
		label:
			bars.find((bar) => bar.dimensionCode === activeOutput)?.displayLabel?.trim() ||
			trendPoints.find((point) => point.displayLabel?.trim())?.displayLabel?.trim() ||
			activeOutput,
		baselineLabel:
			baseline?.value === null || baseline?.value === undefined
				? labels.notAvailable
				: formatScoreValue(baseline.value),
		latestLabel:
			latest?.value === null || latest?.value === undefined
				? labels.notAvailable
				: formatScoreValue(latest.value),
		deltaLabel:
			delta === null || delta === undefined ? labels.notAvailable : formatSignedScoreValue(delta),
		direction: delta === null || delta === undefined ? 'unavailable' : trendDirection(delta),
		points: trendPoints
	};
}

type PositionedResult = {
	id: string;
	label: string;
	rawValue: number;
	positionPercent: number;
	range: ResultsScoreRange;
	rangeLabel: string;
};

function toPositionedResult(bar: ResultsDashboardBarResponse): PositionedResult | null {
	const range = configuredScoreRange(bar);
	if (bar.disclosure !== 'visible' || bar.value === null || !range || range.min < 0) {
		return null;
	}

	return {
		id: bar.id,
		label: resultBarDisplayLabel(bar),
		rawValue: bar.value,
		positionPercent: scoreProgressPercent(bar.value, range, 100),
		range,
		rangeLabel: `${formatCompactNumber(range.min)}-${formatCompactNumber(range.max)}`
	};
}

function isVisibleNumeric(bar: ResultsDashboardBarResponse) {
	return bar.disclosure === 'visible' && bar.value !== null;
}

function sortScoreBars(bars: ResultsDashboardBarResponse[]) {
	return [...bars].sort((left, right) => {
		const leftVisible = left.disclosure === 'visible';
		const rightVisible = right.disclosure === 'visible';
		if (leftVisible !== rightVisible) {
			return leftVisible ? -1 : 1;
		}

		const valueSort =
			(right.value ?? Number.NEGATIVE_INFINITY) - (left.value ?? Number.NEGATIVE_INFINITY);
		if (valueSort !== 0) {
			return valueSort;
		}

		return (left.displayLabel || left.label).localeCompare(right.displayLabel || right.label);
	});
}

export function configuredScoreRange(value: {
	scoreRangeMin?: number | null;
	scoreRangeMax?: number | null;
}): ResultsScoreRange | null {
	if (
		value.scoreRangeMin === null ||
		value.scoreRangeMin === undefined ||
		value.scoreRangeMax === null ||
		value.scoreRangeMax === undefined ||
		value.scoreRangeMax <= value.scoreRangeMin
	) {
		return null;
	}

	return { min: value.scoreRangeMin, max: value.scoreRangeMax };
}

export function scoreProgressPercent(
	value: number,
	range: ResultsScoreRange | null,
	observedMax: number
) {
	const rawPercent = range
		? ((value - range.min) / (range.max - range.min)) * 100
		: (value / observedMax) * 100;
	return roundPercent(Math.max(0, Math.min(100, rawPercent)));
}

function formatVisibleNumber(
	value: number | null,
	disclosure: string,
	labels: ResultsWorkbenchLabels
) {
	if (disclosure !== 'visible') {
		return labels.suppressed;
	}

	return value === null ? labels.notAvailable : value.toFixed(2);
}

function formatVisibleCount(
	value: number | null,
	disclosure: string,
	labels: ResultsWorkbenchLabels
) {
	if (disclosure !== 'visible') {
		return labels.suppressed;
	}

	return value === null ? labels.notAvailable : String(value);
}

function humanizeCalculation(value: string | null | undefined) {
	if (!value?.trim()) {
		return null;
	}

	return value
		.replaceAll('_', ' ')
		.replaceAll('-', ' ')
		.trim()
		.replace(/^./, (first) => first.toUpperCase());
}

function formatCompactNumber(value: number) {
	return Number.isInteger(value) ? String(value) : value.toFixed(2);
}

function formatScoreValue(value: number) {
	return value.toFixed(2);
}

function formatSignedNumber(value: number) {
	if (value > 0) {
		return `+${formatCompactNumber(value)}`;
	}

	return formatCompactNumber(value);
}

function formatSignedScoreValue(value: number) {
	return value > 0 ? `+${formatScoreValue(value)}` : formatScoreValue(value);
}

function roundPercent(value: number) {
	return Math.round(value * 100) / 100;
}

function maxNullable(values: Array<number | null | undefined>) {
	const numeric = values.filter((value): value is number => value !== null && value !== undefined);
	return numeric.length === 0 ? null : Math.max(...numeric);
}

function exclusionReason(bar: ResultsDashboardBarResponse): ResultsExcludedResult['reason'] {
	const range = configuredScoreRange(bar);
	if (bar.disclosure !== 'visible') {
		return 'suppressed';
	}

	if (bar.value === null) {
		return 'not_numeric';
	}

	if (!range) {
		return 'score_range_missing';
	}

	if (range.min < 0) {
		return 'difference_range';
	}

	return 'score_range_missing';
}

function heatmapTone(
	disclosure: string,
	positionPercent: number | null
): ResultsHeatmapCell['tone'] {
	if (disclosure !== 'visible') {
		return 'suppressed';
	}

	if (positionPercent === null) {
		return 'empty';
	}

	if (positionPercent < 45) {
		return 'low';
	}

	if (positionPercent < 70) {
		return 'medium';
	}

	return 'high';
}

function largestWaveMovement(rows: CampaignSeriesResultsWaveMatrixRowResponse[]) {
	return [...rows]
		.filter(
			(row) =>
				row.disclosure === 'visible' &&
				row.deltaFromPreviousMean !== null &&
				row.comparisonState === 'compared'
		)
		.sort(
			(left, right) =>
				Math.abs(right.deltaFromPreviousMean ?? 0) - Math.abs(left.deltaFromPreviousMean ?? 0)
		)[0];
}

function movementTone(value: number | null | undefined): ResultsAttentionCard['tone'] {
	if (value === null || value === undefined || Math.abs(value) < 0.01) {
		return 'stable';
	}

	return value > 0 ? 'up' : 'down';
}

function trendDirection(value: number): ResultsTrendSummary['direction'] {
	if (Math.abs(value) < 0.01) {
		return 'stable';
	}

	return value > 0 ? 'up' : 'down';
}

function mostRecentFinality(
	points: ResultsDashboardPointResponse[],
	waveRows: CampaignSeriesResultsWaveMatrixRowResponse[]
) {
	return (
		[...points].reverse().find((point) => point.dataFinality)?.dataFinality ??
		[...waveRows].reverse().find((row) => row.dataFinality)?.dataFinality ??
		null
	);
}

function toExportSummary(
	workspace: CampaignSeriesReportsWorkspaceResponse
): ResultsWorkbenchExportSummary {
	const hasResultsMatrix = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'campaign_series_results_matrix_csv_codebook'
	);
	const hasResponseDataset = workspace.exportArtifacts.some(
		(artifact) => artifact.artifactType === 'campaign_series_response_csv_codebook'
	);

	if (hasResponseDataset && !hasResultsMatrix) {
		return {
			hasResultsMatrix,
			hasResponseDataset,
			downloadLabel: 'Download response dataset CSV',
			primaryGuidance:
				'Response dataset is ready for row-level analysis. Create a results matrix when you need aggregate dashboard or comparison data.'
		};
	}

	if (hasResultsMatrix) {
		return {
			hasResultsMatrix,
			hasResponseDataset,
			downloadLabel: 'Download results matrix CSV',
			primaryGuidance: hasResponseDataset
				? 'Aggregate matrix and response dataset are ready. Use the matrix for dashboard comparisons and the response dataset for row-level analysis.'
				: 'Aggregate matrix is ready for dashboard, group, and measurement comparison. Create a response dataset only for row-level analysis.'
		};
	}

	return {
		hasResultsMatrix,
		hasResponseDataset,
		downloadLabel: 'Create export file',
		primaryGuidance:
			'Create a results matrix for aggregate review, or create a response dataset when row-level analysis is needed.'
	};
}

function toResultFocusOptionsFromRows(
	rows: { dimensionCode: string; displayLabel?: string | null }[],
	allLabel: string
): ResultsFilterOption[] {
	return [
		{
			value: 'all',
			label: allLabel,
			count: rows.length
		},
		...toGroupedOptions(
			rows.map((row) => ({
				value: row.dimensionCode,
				label: row.displayLabel?.trim() || row.dimensionCode
			})),
			allLabel
		).slice(1)
	];
}

function toGroupedOptions(values: { value: string; label: string }[], allLabel: string) {
	const byValue = new Map<string, ResultsFilterOption>();
	for (const item of values) {
		const existing = byValue.get(item.value);
		if (existing) {
			existing.count += 1;
			continue;
		}

		byValue.set(item.value, {
			value: item.value,
			label: item.label,
			count: 1
		});
	}

	return [
		{
			value: 'all',
			label: allLabel,
			count: values.length
		},
		...byValue.values()
	];
}

function activeFilterValue(value: string | null | undefined) {
	return !value || value === 'all' ? null : value;
}
