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
	kicker: string;
	primarySeriesLabel: string;
	secondarySeriesLabel: string | null;
	yAxisLabel: string;
	statusLabel: string;
	emptyMessage: string;
	noChartableValuesTitle: string;
	rendererFailedMessage: string;
	excludedFromChartLabel: string;
	chartValuesAria: (title: string) => string;
	excludedRowsAria: (title: string) => string;
	points: VisualAnalyticsChartPoint[];
	excludedRows: VisualAnalyticsExcludedRow[];
};

type VisualAnalyticsLocale = 'en' | 'hr-HR';

const visualAnalyticsCopy = {
	en: {
		kicker: 'Visual analytics',
		reportTitle: 'Report visual analytics',
		reportAria: 'Report visual analytics',
		waveTitle: 'Wave visual analytics',
		waveAria: 'Wave visual analytics',
		mean: 'Mean',
		scoreMean: 'Score mean',
		aggregateDelta: 'Aggregate delta',
		pairedDelta: 'Paired delta',
		delta: 'Delta',
		status: 'Preview / not validated',
		loadReportFirst: 'Load the selected report snapshot before reviewing visual analytics.',
		noReportScores: 'No visible numeric report scores are available for charting.',
		loadWaveFirst: 'Load the selected wave comparison snapshot before reviewing visual analytics.',
		noWaveValues: 'No visible compatible wave comparison values are available for charting.',
		noChartableValuesTitle: 'No chartable values',
		rendererFailedMessage: 'Chart renderer could not be loaded. Values remain available below.',
		excludedFromChartLabel: 'Excluded from chart',
		chartValuesAria: (title: string) => `${title} chart values`,
		excludedRowsAria: (title: string) => `${title} excluded rows`,
		scores: (count: string) => `scores ${count}`,
		submitted: (count: number) => `submitted ${count}`,
		range: (range: string) => `range ${range}`,
		baseline: (value: string) => `baseline ${value}`,
		comparison: (value: string) => `comparison ${value}`,
		linkedPairs: (count: number) => `linked pairs ${count}`,
		notAvailable: 'not available'
	},
	'hr-HR': {
		kicker: 'Vizualni pregled',
		reportTitle: 'Vizualni pregled izvještaja',
		reportAria: 'Vizualni pregled izvještaja',
		waveTitle: 'Vizualni pregled mjerenja',
		waveAria: 'Vizualni pregled mjerenja',
		mean: 'Prosjek',
		scoreMean: 'Prosjek rezultata',
		aggregateDelta: 'Agregirana promjena',
		pairedDelta: 'Promjena u paru',
		delta: 'Promjena',
		status: 'Pregled / nije validirano',
		loadReportFirst: 'Učitajte odabrani pregled izvještaja prije vizualnog pregleda.',
		noReportScores: 'Nema vidljivih numeričkih rezultata izvještaja za graf.',
		loadWaveFirst: 'Učitajte odabranu usporedbu mjerenja prije vizualnog pregleda.',
		noWaveValues: 'Nema vidljivih kompatibilnih vrijednosti usporedbe mjerenja za graf.',
		noChartableValuesTitle: 'Nema vrijednosti za graf',
		rendererFailedMessage: 'Prikaz grafa nije se mogao učitati. Vrijednosti ostaju dostupne ispod.',
		excludedFromChartLabel: 'Isključeno iz grafa',
		chartValuesAria: (title: string) => `${title} vrijednosti grafa`,
		excludedRowsAria: (title: string) => `${title} isključeni redci`,
		scores: (count: string) => `rezultati ${count}`,
		submitted: (count: number) => `predano ${count}`,
		range: (range: string) => `raspon ${range}`,
		baseline: (value: string) => `početno ${value}`,
		comparison: (value: string) => `usporedno ${value}`,
		linkedPairs: (count: number) => `povezanih parova ${count}`,
		notAvailable: 'nije dostupno'
	}
} satisfies Record<VisualAnalyticsLocale, {
	kicker: string;
	reportTitle: string;
	reportAria: string;
	waveTitle: string;
	waveAria: string;
	mean: string;
	scoreMean: string;
	aggregateDelta: string;
	pairedDelta: string;
	delta: string;
	status: string;
	loadReportFirst: string;
	noReportScores: string;
	loadWaveFirst: string;
	noWaveValues: string;
	noChartableValuesTitle: string;
	rendererFailedMessage: string;
	excludedFromChartLabel: string;
	chartValuesAria: (title: string) => string;
	excludedRowsAria: (title: string) => string;
	scores: (count: string) => string;
	submitted: (count: number) => string;
	range: (range: string) => string;
	baseline: (value: string) => string;
	comparison: (value: string) => string;
	linkedPairs: (count: number) => string;
	notAvailable: string;
}>;

export function toReportVisualAnalyticsView(
	report: CampaignReportProofResponse | null,
	locale: VisualAnalyticsLocale = 'en'
): VisualAnalyticsChartView {
	const copy = visualAnalyticsCopy[locale];
	if (!report) {
		return {
			title: copy.reportTitle,
			ariaLabel: copy.reportAria,
			kicker: copy.kicker,
			primarySeriesLabel: copy.mean,
			secondarySeriesLabel: null,
			yAxisLabel: copy.scoreMean,
			statusLabel: copy.status,
			emptyMessage: copy.loadReportFirst,
			noChartableValuesTitle: copy.noChartableValuesTitle,
			rendererFailedMessage: copy.rendererFailedMessage,
			excludedFromChartLabel: copy.excludedFromChartLabel,
			chartValuesAria: copy.chartValuesAria,
			excludedRowsAria: copy.excludedRowsAria,
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
					copy.scores(formatNullableCount(score.scoreCount, copy.notAvailable)),
					copy.submitted(score.submittedResponseCount),
					copy.range(formatNullableRange(score.min, score.max, copy.notAvailable))
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
		title: copy.reportTitle,
		ariaLabel: copy.reportAria,
		kicker: copy.kicker,
		primarySeriesLabel: copy.mean,
		secondarySeriesLabel: null,
		yAxisLabel: copy.scoreMean,
		statusLabel: copy.status,
		emptyMessage: copy.noReportScores,
		noChartableValuesTitle: copy.noChartableValuesTitle,
		rendererFailedMessage: copy.rendererFailedMessage,
		excludedFromChartLabel: copy.excludedFromChartLabel,
		chartValuesAria: copy.chartValuesAria,
		excludedRowsAria: copy.excludedRowsAria,
		points,
		excludedRows
	};
}

export function toWaveVisualAnalyticsView(
	comparison: CampaignSeriesWaveComparisonProofResponse | null,
	locale: VisualAnalyticsLocale = 'en'
): VisualAnalyticsChartView {
	const copy = visualAnalyticsCopy[locale];
	if (!comparison) {
		return {
			title: copy.waveTitle,
			ariaLabel: copy.waveAria,
			kicker: copy.kicker,
			primarySeriesLabel: copy.aggregateDelta,
			secondarySeriesLabel: copy.pairedDelta,
			yAxisLabel: copy.delta,
			statusLabel: copy.status,
			emptyMessage: copy.loadWaveFirst,
			noChartableValuesTitle: copy.noChartableValuesTitle,
			rendererFailedMessage: copy.rendererFailedMessage,
			excludedFromChartLabel: copy.excludedFromChartLabel,
			chartValuesAria: copy.chartValuesAria,
			excludedRowsAria: copy.excludedRowsAria,
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
					copy.baseline(formatNullableNumber(score.baselineMean, copy.notAvailable)),
					copy.comparison(formatNullableNumber(score.comparisonMean, copy.notAvailable)),
					copy.linkedPairs(score.linkedPairCount)
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
		title: copy.waveTitle,
		ariaLabel: copy.waveAria,
		kicker: copy.kicker,
		primarySeriesLabel: copy.aggregateDelta,
		secondarySeriesLabel: copy.pairedDelta,
		yAxisLabel: copy.delta,
		statusLabel: copy.status,
		emptyMessage: copy.noWaveValues,
		noChartableValuesTitle: copy.noChartableValuesTitle,
		rendererFailedMessage: copy.rendererFailedMessage,
		excludedFromChartLabel: copy.excludedFromChartLabel,
		chartValuesAria: copy.chartValuesAria,
		excludedRowsAria: copy.excludedRowsAria,
		points,
		excludedRows
	};
}

function formatNullableRange(min: number | null, max: number | null, fallback: string) {
	if (min === null || max === null) {
		return fallback;
	}

	return `${formatNumber(min)}-${formatNumber(max)}`;
}

function formatNullableNumber(value: number | null, fallback: string) {
	return value === null ? fallback : formatNumber(value);
}

function formatNullableCount(value: number | null | undefined, fallback: string) {
	return value === null || value === undefined ? fallback : value.toString();
}

function formatNumber(value: number) {
	return value.toFixed(2);
}

function formatSignedNumber(value: number) {
	return value.toFixed(2);
}
