import type {
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesResultsAnalyticsResponse,
	CampaignSeriesResultsDashboardResponse,
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

function roundPercent(value: number) {
	return Math.round(value * 100) / 100;
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
