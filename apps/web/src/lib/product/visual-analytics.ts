import type {
	CampaignReportProofResponse,
	CampaignSeriesWaveComparisonProofResponse
} from '$lib/api/setup';

export type VisualAnalyticsChartPoint = {
	id: string;
	label: string;
	primaryValue: number;
	primaryDisplay: string;
	secondaryValue: number | null;
	secondaryDisplay: string | null;
	meta: string[];
};

export type VisualAnalyticsExcludedRow = {
	id: string;
	label: string;
	state: string;
	reason: string;
};

export type VisualAnalyticsChartView = {
	title: string;
	ariaLabel: string;
	primarySeriesLabel: string;
	secondarySeriesLabel: string | null;
	yAxisLabel: string;
	statusLabel: string;
	emptyMessage: string;
	points: VisualAnalyticsChartPoint[];
	excludedRows: VisualAnalyticsExcludedRow[];
};

export function toReportVisualAnalyticsView(
	report: CampaignReportProofResponse | null
): VisualAnalyticsChartView {
	if (!report) {
		return {
			title: 'Report visual analytics',
			ariaLabel: 'Report visual analytics',
			primarySeriesLabel: 'Mean',
			secondarySeriesLabel: null,
			yAxisLabel: 'Score mean',
			statusLabel: 'Preview / not validated',
			emptyMessage: 'Load the selected report snapshot before reviewing visual analytics.',
			points: [],
			excludedRows: []
		};
	}

	const points: VisualAnalyticsChartPoint[] = [];
	const excludedRows: VisualAnalyticsExcludedRow[] = [];

	for (const score of report.scores) {
		const visible = score.disclosure === 'visible';

		if (visible && score.mean !== null) {
			points.push({
				id: score.dimensionCode,
				label: score.dimensionCode,
				primaryValue: score.mean,
				primaryDisplay: formatNumber(score.mean),
				secondaryValue: null,
				secondaryDisplay: null,
				meta: [
					`scores ${score.scoreCount ?? 0}`,
					`submitted ${score.submittedResponseCount}`,
					`range ${formatNullableRange(score.min, score.max)}`
				]
			});
			continue;
		}

		excludedRows.push({
			id: score.dimensionCode,
			label: score.dimensionCode,
			state: visible ? 'not_numeric' : score.disclosure,
			reason: score.suppressionReason ?? (visible ? 'mean_not_available' : score.disclosure)
		});
	}

	return {
		title: 'Report visual analytics',
		ariaLabel: 'Report visual analytics',
		primarySeriesLabel: 'Mean',
		secondarySeriesLabel: null,
		yAxisLabel: 'Score mean',
		statusLabel: 'Preview / not validated',
		emptyMessage: 'No visible numeric report scores are available for charting.',
		points,
		excludedRows
	};
}

export function toWaveVisualAnalyticsView(
	comparison: CampaignSeriesWaveComparisonProofResponse | null
): VisualAnalyticsChartView {
	if (!comparison) {
		return {
			title: 'Wave visual analytics',
			ariaLabel: 'Wave visual analytics',
			primarySeriesLabel: 'Aggregate delta',
			secondarySeriesLabel: 'Paired delta',
			yAxisLabel: 'Delta',
			statusLabel: 'Preview / not validated',
			emptyMessage: 'Load the selected wave comparison snapshot before reviewing visual analytics.',
			points: [],
			excludedRows: []
		};
	}

	const points: VisualAnalyticsChartPoint[] = [];
	const excludedRows: VisualAnalyticsExcludedRow[] = [];

	for (const score of comparison.scores) {
		const visible = score.disclosure === 'visible';
		const compatible = score.compatibilityStatus === 'compatible';

		if (visible && compatible && score.aggregateDelta !== null) {
			points.push({
				id: score.dimensionCode,
				label: score.dimensionCode,
				primaryValue: score.aggregateDelta,
				primaryDisplay: formatSignedNumber(score.aggregateDelta),
				secondaryValue: score.pairedDeltaMean,
				secondaryDisplay:
					score.pairedDeltaMean === null ? null : formatSignedNumber(score.pairedDeltaMean),
				meta: [
					`baseline ${formatNullableNumber(score.baselineMean)}`,
					`comparison ${formatNullableNumber(score.comparisonMean)}`,
					`linked pairs ${score.linkedPairCount}`
				]
			});
			continue;
		}

		excludedRows.push({
			id: score.dimensionCode,
			label: score.dimensionCode,
			state: !visible
				? score.disclosure
				: compatible
					? 'not_numeric'
					: score.compatibilityStatus,
			reason:
				score.compatibilityReason ??
				score.suppressionReason ??
				(!visible ? score.disclosure : 'delta_not_available')
		});
	}

	return {
		title: 'Wave visual analytics',
		ariaLabel: 'Wave visual analytics',
		primarySeriesLabel: 'Aggregate delta',
		secondarySeriesLabel: 'Paired delta',
		yAxisLabel: 'Delta',
		statusLabel: 'Preview / not validated',
		emptyMessage: 'No visible compatible wave comparison values are available for charting.',
		points,
		excludedRows
	};
}

function formatNullableRange(min: number | null, max: number | null) {
	if (min === null || max === null) {
		return 'not available';
	}

	return `${formatNumber(min)}-${formatNumber(max)}`;
}

function formatNullableNumber(value: number | null) {
	return value === null ? 'not available' : formatNumber(value);
}

function formatNumber(value: number) {
	return value.toFixed(2);
}

function formatSignedNumber(value: number) {
	return value.toFixed(2);
}
