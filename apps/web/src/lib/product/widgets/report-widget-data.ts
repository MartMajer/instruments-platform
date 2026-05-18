import type {
	CampaignSeriesReportsExportArtifactResponse,
	ExportArtifactRegistryWidgetData,
	FinalityProvenanceWidgetData,
	ReportReadinessSummaryWidgetData,
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
		typeof record.reportableCampaignCount === 'number'
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
