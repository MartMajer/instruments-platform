import type {
	CampaignSeriesReportsExportArtifactResponse,
	ExportArtifactRegistryWidgetData,
	FinalityProvenanceWidgetData,
	ReportReadinessSummaryWidgetData,
	ResultsDashboardWidgetData,
	ReportWidgetData,
	ScoreCoverageSummaryWidgetData,
	SelectedCampaignReportStateWidgetData,
	VisualAnalyticsEntryWidgetData
} from '$lib/api/product';

export function isReportReadinessSummaryWidgetData(
	data: ReportWidgetData | null
): data is ReportReadinessSummaryWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		typeof record.campaignCount === 'number' &&
		typeof record.liveCampaignCount === 'number' &&
		typeof record.reportableCampaignCount === 'number' &&
		typeof record.submittedResponseCount === 'number' &&
		typeof record.scoreCount === 'number' &&
		typeof record.visibleScoreCount === 'number' &&
		typeof record.suppressedScoreCount === 'number' &&
		typeof record.missingPrerequisiteCount === 'number' &&
		Array.isArray(record.missingPrerequisites) &&
		record.missingPrerequisites.every(isReportWidgetPrerequisite)
	);
}

export function isScoreCoverageSummaryWidgetData(
	data: ReportWidgetData | null
): data is ScoreCoverageSummaryWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		typeof record.submittedResponseCount === 'number' &&
		typeof record.scoredSubmittedResponseCount === 'number' &&
		typeof record.unscoredSubmittedResponseCount === 'number' &&
		typeof record.notConfiguredSubmittedResponseCount === 'number' &&
		typeof record.status === 'string' &&
		typeof record.guidance === 'string'
	);
}

export function isSelectedCampaignReportStateWidgetData(
	data: ReportWidgetData | null
): data is SelectedCampaignReportStateWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		typeof record.campaignId === 'string' &&
		typeof record.name === 'string' &&
		typeof record.status === 'string' &&
		typeof record.responseIdentityMode === 'string' &&
		typeof record.defaultLocale === 'string' &&
		isNullableString(record.latestLaunchAt) &&
		typeof record.submittedResponseCount === 'number' &&
		typeof record.scoreCount === 'number' &&
		typeof record.visibleScoreCount === 'number' &&
		typeof record.suppressedScoreCount === 'number' &&
		typeof record.disclosureState === 'string' &&
		isNullableNumber(record.disclosureKMin) &&
		typeof record.reportStatus === 'string' &&
		typeof record.interpretationStatus === 'string' &&
		isNullableString(record.latestExportArtifactId) &&
		isNullableString(record.latestExportArtifactFileName) &&
		isNullableString(record.latestExportArtifactStatus) &&
		isNullableString(record.latestExportArtifactCreatedAt) &&
		isNullableString(record.latestExportArtifactCompletedAt) &&
		isNullableString(record.latestExportArtifactFailedAt) &&
		isNullableString(record.latestExportArtifactFailureReasonCode) &&
		typeof record.latestExportArtifactCanDownload === 'boolean' &&
		isNullableString(record.closedAt) &&
		typeof record.dataFinality === 'string'
	);
}

export function isExportArtifactRegistryWidgetData(
	data: ReportWidgetData | null
): data is ExportArtifactRegistryWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		typeof record.exportArtifactCount === 'number' &&
		Array.isArray(record.artifacts) &&
		record.artifacts.every(isExportArtifact)
	);
}

export function isVisualAnalyticsEntryWidgetData(
	data: ReportWidgetData | null
): data is VisualAnalyticsEntryWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		isNullableString(record.selectedCampaignId) &&
		typeof record.visibleScoreCount === 'number' &&
		typeof record.suppressedScoreCount === 'number' &&
		typeof record.reportableCampaignCount === 'number' &&
		isOptionalResultsAnalytics(record.analytics)
	);
}

export function isResultsDashboardWidgetData(
	data: ReportWidgetData | null
): data is ResultsDashboardWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	if (!isRecord(record.dashboard) || !isOptionalResultsAnalytics(record.analytics)) {
		return false;
	}

	const dashboard = record.dashboard as Record<string, unknown>;
	return (
		isNullableString(dashboard.selectedCampaignId) &&
		isNullableString(dashboard.selectedCampaignName) &&
		typeof dashboard.disclosureKMin === 'number' &&
		typeof dashboard.disclosureState === 'string' &&
		Array.isArray(dashboard.metrics) &&
		dashboard.metrics.every(isResultsDashboardMetric) &&
		Array.isArray(dashboard.outputBars) &&
		dashboard.outputBars.every(isResultsDashboardBar) &&
		Array.isArray(dashboard.groupBars) &&
		dashboard.groupBars.every(isResultsDashboardBar) &&
		Array.isArray(dashboard.waveTrendPoints) &&
		dashboard.waveTrendPoints.every(isResultsDashboardPoint) &&
		Array.isArray(dashboard.notes) &&
		dashboard.notes.every(isResultsDashboardNote)
	);
}

export function isFinalityProvenanceWidgetData(
	data: ReportWidgetData | null
): data is FinalityProvenanceWidgetData {
	if (!isRecord(data)) {
		return false;
	}

	const record = data as Record<string, unknown>;
	return (
		typeof record.preliminaryLiveReportCount === 'number' &&
		typeof record.closedWaveReportCount === 'number' &&
		isNullableString(record.selectedCampaignId) &&
		isNullableString(record.selectedCampaignStatus) &&
		isNullableString(record.selectedDataFinality) &&
		isNullableString(record.selectedClosedAt) &&
		isNullableString(record.selectedLatestLaunchAt)
	);
}

function isRecord(data: unknown): data is Record<string, unknown> {
	return typeof data === 'object' && data !== null && !Array.isArray(data);
}

function isReportWidgetPrerequisite(data: unknown) {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.code === 'string' &&
		typeof data.label === 'string' &&
		typeof data.message === 'string' &&
		typeof data.severity === 'string'
	);
}

function isExportArtifact(data: unknown): data is CampaignSeriesReportsExportArtifactResponse {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.id === 'string' &&
		typeof data.targetKind === 'string' &&
		typeof data.targetId === 'string' &&
		typeof data.targetLabel === 'string' &&
		isNullableString(data.campaignId) &&
		isNullableString(data.campaignName) &&
		typeof data.artifactType === 'string' &&
		typeof data.status === 'string' &&
		typeof data.format === 'string' &&
		typeof data.fileName === 'string' &&
		typeof data.rowCount === 'number' &&
		typeof data.byteSize === 'number' &&
		isNullableString(data.checksumSha256) &&
		typeof data.createdAt === 'string' &&
		isNullableString(data.completedAt) &&
		isNullableString(data.startedAt) &&
		isNullableString(data.failedAt) &&
		isNullableString(data.expiresAt) &&
		isNullableString(data.deletedAt) &&
		isNullableString(data.failureReasonCode) &&
		typeof data.canDownload === 'boolean' &&
		isOptionalNullableString(data.campaignStatus) &&
		isOptionalNullableString(data.campaignClosedAt) &&
		isOptionalNullableString(data.dataFinality)
	);
}

function isNullableString(data: unknown): data is string | null {
	return data === null || typeof data === 'string';
}

function isOptionalNullableString(data: unknown): data is string | null | undefined {
	return data === undefined || isNullableString(data);
}

function isNullableNumber(data: unknown): data is number | null {
	return data === null || typeof data === 'number';
}

function isOptionalNullableNumber(data: unknown): data is number | null | undefined {
	return data === undefined || isNullableNumber(data);
}

function isOptionalResultsAnalytics(data: unknown): boolean {
	if (data === undefined || data === null) {
		return true;
	}

	if (!isRecord(data)) {
		return false;
	}

	return (
		isNullableString(data.selectedCampaignId) &&
		isNullableString(data.selectedCampaignName) &&
		typeof data.disclosureKMin === 'number' &&
		typeof data.disclosureState === 'string' &&
		Array.isArray(data.scoreOutputs) &&
		data.scoreOutputs.every(isResultsScoreOutputRow) &&
		Array.isArray(data.groupRows) &&
		data.groupRows.every(isResultsGroupMatrixRow) &&
		Array.isArray(data.waveRows) &&
		data.waveRows.every(isResultsWaveMatrixRow) &&
		Array.isArray(data.insights) &&
		data.insights.every(isResultsInsightRow)
	);
}

function isResultsScoreOutputRow(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.dimensionCode === 'string' &&
		isOptionalNullableString(data.displayLabel) &&
		isOptionalScoreOutputMethodMetadata(data) &&
		typeof data.disclosure === 'string' &&
		isNullableNumber(data.submittedResponseCount) &&
		isNullableNumber(data.scoreCount) &&
		isNullableNumber(data.mean) &&
		isNullableNumber(data.median) &&
		isNullableNumber(data.standardDeviation) &&
		isNullableNumber(data.min) &&
		isNullableNumber(data.max) &&
		isNullableNumber(data.nValidTotal) &&
		isNullableNumber(data.nExpectedTotal) &&
		isNullableString(data.missingPolicyStatusSummary) &&
		isNullableString(data.suppressionReason)
	);
}

function isResultsGroupMatrixRow(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.groupType === 'string' &&
		typeof data.groupName === 'string' &&
		typeof data.dimensionCode === 'string' &&
		isOptionalNullableString(data.displayLabel) &&
		isOptionalScoreOutputMethodMetadata(data) &&
		typeof data.disclosure === 'string' &&
		isNullableNumber(data.submittedResponseCount) &&
		isNullableNumber(data.scoreCount) &&
		isNullableNumber(data.mean) &&
		isNullableNumber(data.median) &&
		isNullableNumber(data.standardDeviation) &&
		isNullableNumber(data.min) &&
		isNullableNumber(data.max) &&
		isNullableString(data.suppressionReason)
	);
}

function isResultsWaveMatrixRow(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.campaignId === 'string' &&
		typeof data.campaignName === 'string' &&
		typeof data.campaignStatus === 'string' &&
		typeof data.dataFinality === 'string' &&
		isNullableString(data.closedAt) &&
		typeof data.dimensionCode === 'string' &&
		isOptionalNullableString(data.displayLabel) &&
		isOptionalScoreOutputMethodMetadata(data) &&
		typeof data.disclosure === 'string' &&
		isNullableNumber(data.submittedResponseCount) &&
		isNullableNumber(data.scoreCount) &&
		isNullableNumber(data.mean) &&
		isNullableNumber(data.median) &&
		isNullableNumber(data.standardDeviation) &&
		isNullableNumber(data.min) &&
		isNullableNumber(data.max) &&
		isNullableString(data.suppressionReason) &&
		isNullableNumber(data.deltaFromPreviousMean) &&
		isNullableNumber(data.deltaFromFirstMean) &&
		typeof data.comparisonState === 'string'
	);
}

function isResultsInsightRow(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.kind === 'string' &&
		typeof data.severity === 'string' &&
		typeof data.title === 'string' &&
		typeof data.detail === 'string'
	);
}

function isResultsDashboardMetric(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.id === 'string' &&
		isNullableNumber(data.value) &&
		typeof data.unit === 'string' &&
		isNullableString(data.detail) &&
		typeof data.tone === 'string'
	);
}

function isResultsDashboardBar(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	const suppressedHasHiddenFields =
		data.disclosure !== 'visible' && (data.value !== null || data.count !== null);

	return (
		typeof data.id === 'string' &&
		typeof data.label === 'string' &&
		isOptionalNullableString(data.displayLabel) &&
		typeof data.dimensionCode === 'string' &&
		isOptionalScoreOutputMethodMetadata(data) &&
		typeof data.disclosure === 'string' &&
		isNullableNumber(data.value) &&
		isNullableNumber(data.count) &&
		isNullableString(data.detail) &&
		isNullableString(data.suppressionReason) &&
		!suppressedHasHiddenFields
	);
}

function isResultsDashboardPoint(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	const suppressedHasHiddenFields =
		data.disclosure !== 'visible' &&
		(data.value !== null || data.deltaFromPrevious !== null || data.count !== null);

	return (
		typeof data.id === 'string' &&
		typeof data.campaignId === 'string' &&
		typeof data.campaignName === 'string' &&
		isOptionalNullableString(data.displayLabel) &&
		typeof data.dimensionCode === 'string' &&
		isOptionalScoreOutputMethodMetadata(data) &&
		typeof data.disclosure === 'string' &&
		isNullableNumber(data.value) &&
		isNullableNumber(data.deltaFromPrevious) &&
		typeof data.comparisonState === 'string' &&
		typeof data.dataFinality === 'string' &&
		isNullableNumber(data.count) &&
		isNullableString(data.suppressionReason) &&
		!suppressedHasHiddenFields
	);
}

function isResultsDashboardNote(data: unknown): boolean {
	if (!isRecord(data)) {
		return false;
	}

	return (
		typeof data.kind === 'string' &&
		typeof data.severity === 'string' &&
		typeof data.title === 'string' &&
		typeof data.detail === 'string'
	);
}

function isOptionalScoreOutputMethodMetadata(data: Record<string, unknown>) {
	return (
		isOptionalNullableString(data.calculation) &&
		isOptionalNullableString(data.calculationLabel) &&
		isOptionalNullableNumber(data.scoreRangeMin) &&
		isOptionalNullableNumber(data.scoreRangeMax)
	);
}
