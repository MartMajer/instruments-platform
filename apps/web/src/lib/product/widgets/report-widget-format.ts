export type ReportWidgetFormatCopy = {
	notAvailable: string;
	yes: string;
	no: string;
	labels?: Record<string, string>;
	codeLabels: Record<string, string>;
};

const defaultReportWidgetFormatCopy: ReportWidgetFormatCopy = {
	notAvailable: 'Not available',
	yes: 'Yes',
	no: 'No',
	labels: {
		available: 'Available',
		campaign: 'Campaign',
		campaignStatus: 'Campaign status',
		closedAt: 'Closed at',
		closedWave: 'Closed wave',
		completed: 'Completed',
		coverageStatus: 'Coverage status',
		created: 'Created',
		currentResultSummary: 'Current result summary',
		dataFinality: 'Data finality',
		disclosure: 'Disclosure',
		disabled: 'Disabled',
		download: 'Download',
		enabled: 'Enabled',
		exportActions: 'Export actions',
		exportFiles: 'Export files',
		exportFileDataUnavailable: 'Export file data is unavailable.',
		exportState: 'Export state',
		failureReason: 'Failure reason',
		finalityDataUnavailable: 'Finality and provenance data is unavailable.',
		interpretation: 'Interpretation',
		latestExport: 'Latest export',
		latestLaunch: 'Latest launch',
		latestScoringActivity: 'Latest scoring activity',
		listed: 'Listed',
		noExportFiles: 'No export files recorded.',
		bestUse: 'Best use',
		exportUseGuide: 'Which export should I use?',
		exportUseGuideDescription:
			'Use aggregate exports for the dashboard and group/measurement comparisons. Use response datasets only when you need row-level analysis.',
		dashboardMatrixExport: 'Dashboard and matrix export',
		dashboardMatrixExportGuidance: 'Aggregate data behind result charts, group comparison, and measurement comparison.',
		rowLevelResponseExport: 'Row-level response dataset',
		rowLevelResponseExportGuidance: 'Analysis-ready respondent rows with variables and score values.',
		singleMeasurementReportExport: 'Single measurement report export',
		singleMeasurementReportExportGuidance: 'One measurement summary for review or handoff.',
		reportPacketExport: 'Report packet export',
		reportPacketExportGuidance: 'Formatted report file for review or sharing after interpretation checks.',
		exportUseUnknownGuidance: 'Review file metadata before using this export.',
		notConfigured: 'Not configured',
		notConfiguredState: 'Not configured',
		notSelected: 'Not selected',
		previewReady: 'Preview ready',
		previewSource: 'Preview source',
		preliminaryLive: 'Preliminary live',
		readinessDataUnavailable: 'Readiness data is unavailable.',
		reportable: 'Reportable',
		reportableCampaigns: 'Reportable campaigns',
		reportReadinessPrerequisites: 'Report readiness prerequisites',
		reportPreview: 'report preview',
		reportStatus: 'Report status',
		resultOutput: 'Result output',
		resultOutputs: 'Result outputs',
		resultMatrix: 'Results overview',
		groupMatrix: 'Compare groups',
		waveTrend: 'Compare measurement rounds',
		groupBreakdown: 'Group breakdown',
		waveBreakdown: 'Wave breakdown',
		highlights: 'Result highlights',
		highestMean: 'Highest result',
		lowestMean: 'Lowest result',
		largestWaveChange: 'Largest measurement change',
		resultBarChart: 'Result score chart',
		groupBarChart: 'Group comparison chart',
		measurementTrendChart: 'Measurement trend chart',
		dashboardNotes: 'Dashboard notes',
		measurement: 'Measurement',
		observedRange: 'Observed range',
		selectedMeasurement: 'Selected measurement',
		selectedChartItem: 'Selected item',
		chartScale: 'Scale max',
		chartDataTable: 'Chart data table',
		metric_visible_outputs: 'Visible result outputs',
		metric_hidden_outputs: 'Hidden outputs',
		metric_group_rows: 'Visible group rows',
		metric_compared_measurements: 'Compared measurements',
		insights: 'What to notice',
		evidenceSource: 'Evidence source',
		currentSignal: 'Current signal',
		whereToInspect: 'Where to inspect',
		inspectResultMatrix: 'Inspect result output table',
		inspectGroupMatrix: 'Inspect group comparison table',
		inspectWaveMatrix: 'Inspect measurement comparison table',
		inspectMatrix: 'Inspect results tables',
		change: 'Change',
		fromPrevious: 'Change from previous',
		fromFirst: 'Change from first',
		baseline: 'Baseline',
		group: 'Group',
		groupType: 'Group type',
		mean: 'Mean',
		median: 'Median',
		standardDeviation: 'Std. dev.',
		range: 'Range',
		scoreOutput: 'Score output',
		resultName: 'Result',
		sample: 'Responses',
		missingness: 'Missingness',
		noGroupBreakdown: 'No group comparison is available for this measurement round.',
		noWaveBreakdown: 'No repeated measurement comparison is available yet.',
		noResultBars: 'No visible result chart values are available yet.',
		noGroupBars: 'No group comparison chart is available yet.',
		noWaveTrendPoints: 'No measurement trend chart is available yet.',
		ready: 'Ready',
		readyToRun: 'Ready to run',
		resultsPreview: 'Results preview',
		resultsPreviewLoading: 'The results preview is loading.',
		resultsPreviewUnavailable: 'Results preview unavailable',
		resultsPreviewUnavailableExport:
			'The export workflow can still be used while the preview is unavailable.',
		resultsPreviewWidgets: 'Results preview widgets',
		resultsSummary: 'Results summary',
		rows: 'Rows',
		scoreCoverageDataUnavailable: 'Score coverage data is unavailable.',
		scored: 'Scored',
		scores: 'Scores',
		selectedCampaign: 'Selected campaign',
		selectedCampaignReportStateUnavailable: 'Selected campaign report state is unavailable.',
		size: 'Size',
		submitted: 'Submitted',
		submittedResponses: 'Submitted responses',
		suppressed: 'Suppressed',
		suppressedScores: 'Suppressed scores',
		unavailable: 'Unavailable',
		unscored: 'Unscored',
		visible: 'Visible',
		visibleScores: 'Visible scores',
		visualAnalyticsDataUnavailable: 'Visual analytics entry data is unavailable.'
		,
		resultsDashboardDataUnavailable: 'Results dashboard data is unavailable.'
	},
	codeLabels: {
		proof_only: 'preview'
	}
};

export function formatCodeLabel(
	value: string | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	if (!value) {
		return copy.notAvailable;
	}

	const mapped = copy.codeLabels[value];
	if (mapped) {
		return mapped;
	}

	return value.replaceAll('_', ' ');
}

export function formatNullableDate(
	value: string | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	if (!value) {
		return copy.notAvailable;
	}

	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return value;
	}

	return new Intl.DateTimeFormat('hr-HR', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	}).format(date);
}

export function formatNullableNumber(
	value: number | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	return value === null || value === undefined ? copy.notAvailable : String(value);
}

export function formatBooleanLabel(
	value: boolean,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	return value ? copy.yes : copy.no;
}

export function formatWidgetLabel(
	key: string,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	return copy.labels?.[key] ?? defaultReportWidgetFormatCopy.labels?.[key] ?? key;
}

export function formatBytes(value: number) {
	if (value < 1000) {
		return `${value} B`;
	}

	if (value < 1_000_000) {
		return `${(value / 1000).toFixed(1)} KB`;
	}

	return `${(value / 1_000_000).toFixed(1)} MB`;
}

export function formatProductCopy(value: string | null | undefined) {
	if (!value) {
		return '';
	}

	return value
		.replace(/\bExport artifacts\b/g, 'Export files')
		.replace(/\bexport artifacts\b/g, 'export files')
		.replace(/\bExport artifact\b/g, 'Export file')
		.replace(/\bexport artifact\b/g, 'export file')
		.replace(/\breport proof export\b/g, 'results export')
		.replace(/\bReport proof export\b/g, 'Results export')
		.replace(/\breport proof\b/g, 'report preview')
		.replace(/\bReport proof\b/g, 'Report preview')
		.replace(/\bproof only\b/g, 'preview')
		.replace(/\bProof only\b/g, 'Preview');
}
