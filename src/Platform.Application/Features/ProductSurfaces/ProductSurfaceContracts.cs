namespace Platform.Application.Features.ProductSurfaces;

public sealed record WorkspaceOverviewResponse(
    Guid TenantId,
    WorkspaceOverviewTotalsResponse Totals,
    IReadOnlyList<CampaignSeriesListItemResponse> RecentSeries,
    WorkspaceCommandCenterResponse CommandCenter,
    WorkspaceStudyCollectionsResponse StudyCollections);

public sealed record WorkspaceStudyCollectionsResponse(
    IReadOnlyList<CampaignSeriesListItemResponse> SampleStudies,
    IReadOnlyList<CampaignSeriesListItemResponse> OwnStudies);

public sealed record EnsureSampleStudiesResponse(
    Guid TenantId,
    int ExistingSampleStudyCount,
    int CreatedSampleStudyCount,
    IReadOnlyList<Guid> CreatedCampaignSeriesIds);

public sealed record WorkspaceOverviewTotalsResponse(
    int CampaignSeriesCount,
    int CampaignCount,
    int LiveCampaignCount,
    int SubmittedResponseCount,
    int ExportArtifactCount);

public sealed record WorkspaceCommandCenterResponse(
    IReadOnlyList<WorkspaceCommandCenterItemResponse> Items);

public sealed record WorkspaceCommandCenterItemResponse(
    string Id,
    string Title,
    string Description,
    string State,
    string Surface,
    string Route,
    string ActionLabel,
    int Priority,
    Guid? CampaignSeriesId = null,
    Guid? CampaignId = null,
    string? RequiredPermission = null);

public sealed record TenantSettingsWorkspaceResponse(
    TenantSettingsProfileResponse Profile,
    TenantSettingsWorkspaceCountsResponse Counts,
    IReadOnlyList<TenantSettingsManagementLinkResponse> ManagementLinks);

public sealed record TenantSettingsProfileResponse(
    Guid TenantId,
    string Slug,
    string Name,
    string Region,
    string DefaultLocale,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantSettingsWorkspaceCountsResponse(
    int CampaignSeriesCount,
    int CampaignCount,
    int LiveCampaignCount,
    int SubmittedResponseCount,
    int SubjectCount,
    int SubjectGroupCount,
    int TenantMemberCount,
    int TenantRoleCount,
    int ExportArtifactCount);

public sealed record TenantSettingsManagementLinkResponse(
    string Id,
    string Label,
    string Description,
    string Route);

public sealed record ExportArtifactLibraryResponse(
    Guid TenantId,
    ExportArtifactLibrarySummaryResponse Summary,
    IReadOnlyList<CampaignSeriesReportsExportArtifactResponse> Artifacts);

public sealed record ExportArtifactLibrarySummaryResponse(
    int TotalCount,
    int DownloadableCount,
    int FailedCount,
    int PendingCount,
    int RetryableCount = 0);

public sealed record CampaignSeriesListResponse(
    IReadOnlyList<CampaignSeriesListItemResponse> Items);

public sealed record CampaignSeriesStudyBriefResponse(
    string? Purpose,
    string? Audience,
    string? DesignType,
    string? IntendedUse,
    string? InterpretationBoundary,
    string? OwnerNotes);

public sealed record CampaignSeriesPortfolioQuery(
    string? Search = null,
    string Status = CampaignSeriesPortfolioStatuses.All,
    string Sort = CampaignSeriesPortfolioSorts.ActivityDesc,
    string Visibility = CampaignSeriesPortfolioVisibilities.Active);

public static class CampaignSeriesPortfolioStatuses
{
    public const string All = "all";
    public const string NotConfigured = "not_configured";
    public const string Pending = "pending";
    public const string ProofOnly = "proof_only";

    public static bool IsKnown(string value)
    {
        return value is All or NotConfigured or Pending or ProofOnly;
    }
}

public static class CampaignSeriesPortfolioSorts
{
    public const string ActivityDesc = "activity_desc";
    public const string UpdatedDesc = "updated_desc";
    public const string CreatedDesc = "created_desc";
    public const string NameAsc = "name_asc";

    public static bool IsKnown(string value)
    {
        return value is ActivityDesc or UpdatedDesc or CreatedDesc or NameAsc;
    }
}

public static class CampaignSeriesPortfolioVisibilities
{
    public const string Active = "active";
    public const string Archived = "archived";
    public const string All = "all";

    public static bool IsKnown(string value)
    {
        return value is Active or Archived or All;
    }
}

public static class CampaignSeriesReadOnlyReasons
{
    public const string SampleStudy = "sample_study";
}

public sealed record CampaignSeriesListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int CampaignCount,
    int LiveCampaignCount,
    int SubmittedResponseCount,
    DateTimeOffset? LatestLaunchAt,
    DateTimeOffset? LatestSubmissionAt,
    string ReadinessStatus,
    bool Archived = false,
    DateTimeOffset? ArchivedAt = null,
    Guid? ArchivedByUserId = null,
    string? ArchiveReason = null,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record RenameCampaignSeriesRequest(string Name);

public sealed record CampaignSeriesRenameResponse(
    Guid Id,
    string Name,
    DateTimeOffset UpdatedAt);

public sealed record DuplicateCampaignSeriesRequest(string Name);

public sealed record CampaignSeriesDuplicateResponse(
    Guid Id,
    string Name,
    string StudyKind,
    bool IsSample,
    Guid SourceCampaignSeriesId);

public sealed record ArchiveCampaignSeriesRequest(string? Reason = null);

public sealed record CampaignSeriesArchiveStateResponse(
    Guid Id,
    bool Archived,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt = null,
    Guid? ArchivedByUserId = null,
    string? ArchiveReason = null);

public sealed record CloseCampaignRequest(string? Reason = null);

public sealed record CampaignCloseStateResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt = null,
    Guid? ClosedByUserId = null,
    string? CloseReason = null);

public sealed record CampaignSeriesHubResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    CampaignSeriesHubTotalsResponse Totals,
    CampaignSeriesGovernanceSummaryResponse Governance,
    IReadOnlyList<CampaignSeriesLifecycleItemResponse> Lifecycle,
    IReadOnlyList<CampaignSeriesHubCampaignResponse> Campaigns,
    bool Archived = false,
    DateTimeOffset? ArchivedAt = null,
    Guid? ArchivedByUserId = null,
    string? ArchiveReason = null,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record CampaignSeriesHubTotalsResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int SubmittedResponseCount,
    int ScoreCount,
    int ExportArtifactCount);

public sealed record CampaignSeriesGovernanceSummaryResponse(
    string ConsentStatus,
    string RetentionStatus,
    string DisclosureStatus,
    string ScoringStatus);

public sealed record CampaignSeriesLifecycleItemResponse(
    string Id,
    string Label,
    string Status,
    string Guidance,
    string Route,
    string ActionLabel);

public sealed record CampaignSeriesHubCampaignResponse(
    Guid Id,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    DateTimeOffset? LatestLaunchAt,
    int SubmittedResponseCount,
    int ScoreCount,
    int ExportArtifactCount);

public sealed record CampaignSeriesSetupWorkspaceResponse(
    CampaignSeriesSetupSeriesResponse Series,
    CampaignSeriesSetupSummaryResponse Summary,
    CampaignSeriesSetupCampaignResponse? SelectedCampaign,
    CampaignSeriesSetupTemplateResponse? Template,
    CampaignSeriesSetupScoringResponse? Scoring,
    CampaignSeriesSetupPolicySummaryResponse Policies,
    CampaignSeriesSetupReadinessResponse Readiness,
    IReadOnlyList<CampaignSeriesSetupMissingPrerequisiteResponse> MissingPrerequisites,
    IReadOnlyList<CampaignSeriesSetupCampaignResponse> Campaigns);

public sealed record CampaignSeriesSetupSeriesResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record CampaignSeriesSetupSummaryResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int MissingPrerequisiteCount);

public sealed record CampaignSeriesSetupCampaignResponse(
    Guid Id,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    Guid TemplateVersionId,
    DateTimeOffset? LatestLaunchAt);

public sealed record CampaignSeriesSetupTemplateResponse(
    Guid TemplateId,
    Guid TemplateVersionId,
    string TemplateName,
    string Semver,
    string Status,
    string DefaultLocale,
    Guid? InstrumentId,
    int QuestionCount);

public sealed record CampaignSeriesSetupScoringResponse(
    Guid Id,
    string RuleKey,
    string RuleVersion,
    string Status,
    string Source);

public sealed record CampaignSeriesSetupPolicySummaryResponse(
    CampaignSeriesSetupPolicyResponse Consent,
    CampaignSeriesSetupPolicyResponse Retention,
    CampaignSeriesSetupPolicyResponse Disclosure);

public sealed record CampaignSeriesSetupPolicyResponse(
    Guid? Id,
    string? Version,
    string Status)
{
    public IReadOnlyList<CampaignSeriesSetupPolicyDetailResponse> Details { get; init; } =
        Array.Empty<CampaignSeriesSetupPolicyDetailResponse>();
}

public sealed record CampaignSeriesSetupPolicyDetailResponse(
    string Label,
    string Value);

public sealed record CampaignSeriesSetupReadinessResponse(
    Guid? CampaignId,
    string Status,
    bool Ready);

public sealed record CampaignSeriesSetupMissingPrerequisiteResponse(
    string Code,
    string Label,
    string Message,
    string Severity);

public sealed record CampaignSeriesOperationsWorkspaceResponse(
    CampaignSeriesOperationsSeriesResponse Series,
    CampaignSeriesOperationsSummaryResponse Summary,
    CampaignSeriesOperationsCampaignResponse? SelectedCampaign,
    IReadOnlyList<CampaignSeriesOperationsMissingPrerequisiteResponse> MissingPrerequisites,
    IReadOnlyList<CampaignSeriesOperationsCampaignResponse> Campaigns,
    CampaignSeriesScoreCoverageResponse? ScoreCoverage = null);

public sealed record CampaignSeriesOperationsSeriesResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record CampaignSeriesOperationsSummaryResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int OpenLinkAssignmentCount,
    int QueuedInvitationCount,
    int SentInvitationCount,
    int FailedInvitationCount,
    int DeliveryAttemptCount,
    int SubmittedResponseCount,
    int StartedResponseCount,
    int DraftResponseCount,
    DateTimeOffset? LatestResponseStartedAt,
    DateTimeOffset? LatestResponseSubmittedAt,
    string CollectionStatus,
    string ReportVisibilityStatus,
    string CollectionGuidance,
    int MissingPrerequisiteCount,
    int BouncedInvitationCount = 0,
    int ProviderAcceptedEventCount = 0,
    int ProviderDeliveredEventCount = 0,
    int ProviderBouncedEventCount = 0,
    int ProviderComplainedEventCount = 0,
    DateTimeOffset? LatestProviderEventAt = null);

public sealed record CampaignSeriesOperationsCampaignResponse(
    Guid Id,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    Guid? LatestLaunchSnapshotId,
    DateTimeOffset? LatestLaunchAt,
    int SubmittedResponseCount,
    int StartedResponseCount,
    int DraftResponseCount,
    DateTimeOffset? LatestResponseStartedAt,
    DateTimeOffset? LatestResponseSubmittedAt,
    string CollectionStatus,
    string ReportVisibilityStatus,
    string CollectionGuidance,
    int OpenLinkAssignmentCount,
    int QueuedInvitationCount,
    int SentInvitationCount,
    int FailedInvitationCount,
    int DeliveryAttemptCount,
    DateTimeOffset? LatestDeliveryAttemptAt,
    DateTimeOffset? ClosedAt = null,
    Guid? ClosedByUserId = null,
    string? CloseReason = null,
    Guid? ScoringRuleId = null,
    int ScoredSubmittedResponseCount = 0,
    int UnscoredSubmittedResponseCount = 0,
    int NotConfiguredSubmittedResponseCount = 0,
    DateTimeOffset? LatestScoringActivityAt = null,
    string ScoreCoverageStatus = "no_submissions",
    CampaignSeriesOperationsLaunchSnapshotResponse? LaunchSnapshot = null,
    int BouncedInvitationCount = 0,
    int ProviderAcceptedEventCount = 0,
    int ProviderDeliveredEventCount = 0,
    int ProviderBouncedEventCount = 0,
    int ProviderComplainedEventCount = 0,
    DateTimeOffset? LatestProviderEventAt = null);

public sealed record CampaignSeriesOperationsLaunchSnapshotResponse(
    Guid Id,
    Guid TemplateVersionId,
    Guid ScoringRuleId,
    Guid? ConsentDocumentId,
    Guid? RetentionPolicyId,
    Guid? DisclosurePolicyId,
    string ResponseIdentityMode,
    string DefaultLocale,
    int TemplateQuestionCount,
    DateTimeOffset LaunchedAt,
    Guid? LaunchedByUserId,
    ProductSurfaceLaunchPacketProvenanceResponse? LaunchPacket = null);

public sealed record ProductSurfaceLaunchPacketProvenanceResponse(
    int SchemaVersion,
    IReadOnlyList<string> Sections,
    string Source = "unknown");

public sealed record CampaignSeriesOperationsMissingPrerequisiteResponse(
    string Code,
    string Label,
    string Message,
    string Severity);

public sealed record CampaignSeriesScoreCoverageResponse(
    int SubmittedResponseCount,
    int ScoredSubmittedResponseCount,
    int UnscoredSubmittedResponseCount,
    int NotConfiguredSubmittedResponseCount,
    int CampaignsWithScoringRuleCount,
    int CampaignsWithoutScoringRuleCount,
    DateTimeOffset? LatestScoringActivityAt,
    string Status,
    string Guidance);

public sealed record CampaignSeriesScoreRemediationResponse(
    Guid CampaignSeriesId,
    int SubmittedResponseCount,
    int EligibleSubmittedResponseCount,
    int AlreadyScoredSubmittedResponseCount,
    int RemediatedSubmittedResponseCount,
    int SkippedNotConfiguredSubmittedResponseCount,
    int FailedSubmittedResponseCount,
    DateTimeOffset? LatestScoringActivityAt);

public sealed record CampaignSeriesReportsWorkspaceResponse(
    CampaignSeriesReportsSeriesResponse Series,
    CampaignSeriesReportsSummaryResponse Summary,
    CampaignSeriesReportsCampaignResponse? SelectedCampaign,
    IReadOnlyList<CampaignSeriesReportsMissingPrerequisiteResponse> MissingPrerequisites,
    IReadOnlyList<CampaignSeriesReportsExportArtifactResponse> ExportArtifacts,
    IReadOnlyList<CampaignSeriesReportsCampaignResponse> Campaigns,
    CampaignSeriesScoreCoverageResponse? ScoreCoverage = null,
    CampaignSeriesResultsAnalyticsResponse? ResultsAnalytics = null,
    CampaignSeriesResultsDashboardResponse? ResultsDashboard = null);

public sealed record CampaignSeriesReportsSeriesResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record CampaignSeriesReportsSummaryResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int ReportableCampaignCount,
    int SubmittedResponseCount,
    int ScoreCount,
    int ExportArtifactCount,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    int MissingPrerequisiteCount,
    int PreliminaryLiveReportCount = 0,
    int ClosedWaveReportCount = 0);

public sealed record CampaignSeriesReportsCampaignResponse(
    Guid Id,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    Guid? LatestLaunchSnapshotId,
    DateTimeOffset? LatestLaunchAt,
    Guid? ScoringRuleId,
    Guid? ConsentDocumentId,
    Guid? RetentionPolicyId,
    Guid? DisclosurePolicyId,
    int SubmittedResponseCount,
    int ScoreCount,
    int ExportArtifactCount,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    string DisclosureState,
    int? DisclosureKMin,
    string ReportStatus,
    string InterpretationStatus,
    Guid? LatestExportArtifactId,
    string? LatestExportArtifactFileName,
    string? LatestExportArtifactStatus,
    DateTimeOffset? LatestExportArtifactCreatedAt,
    DateTimeOffset? LatestExportArtifactCompletedAt,
    DateTimeOffset? LatestExportArtifactStartedAt,
    DateTimeOffset? LatestExportArtifactFailedAt,
    DateTimeOffset? LatestExportArtifactExpiresAt,
    DateTimeOffset? LatestExportArtifactDeletedAt,
    string? LatestExportArtifactFailureReasonCode,
    bool LatestExportArtifactCanDownload,
    DateTimeOffset? ClosedAt = null,
    Guid? ClosedByUserId = null,
    string? CloseReason = null,
    string DataFinality = "not_reportable",
    ProductSurfaceLaunchPacketProvenanceResponse? LaunchPacket = null);

public sealed record CampaignSeriesReportsMissingPrerequisiteResponse(
    string Code,
    string Label,
    string Message,
    string Severity);

public sealed record CampaignSeriesReportsExportArtifactResponse(
    Guid Id,
    string TargetKind,
    Guid TargetId,
    string TargetLabel,
    Guid? CampaignId,
    string? CampaignName,
    string ArtifactType,
    string Status,
    string Format,
    string FileName,
    int RowCount,
    long ByteSize,
    string? ChecksumSha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? DeletedAt,
    string? FailureReasonCode,
    bool CanDownload,
    string? CampaignStatus = null,
    DateTimeOffset? CampaignClosedAt = null,
    string? DataFinality = null,
    bool CanRetry = false);

public sealed record CampaignSeriesReportsWidgetManifestResponse(
    Guid CampaignSeriesId,
    string Surface,
    string SurfaceVersion,
    ReportWidgetLayoutResponse Layout,
    IReadOnlyList<ReportWidgetResponse> Widgets);

public sealed record ReportWidgetLayoutResponse(
    string Kind,
    string Density);

public sealed record ReportWidgetResponse(
    string Id,
    string Kind,
    string Title,
    string Size,
    string State,
    string? Message,
    object? Data,
    ReportWidgetDataSourceResponse? DataSource,
    IReadOnlyList<ReportWidgetActionResponse> Actions);

public sealed record ReportWidgetDataSourceResponse(
    string Href,
    string Method);

public sealed record ReportWidgetActionResponse(
    string Id,
    string Label,
    string Kind,
    string Href,
    string Method,
    bool Enabled,
    string? DisabledReason);

public sealed record ReportReadinessWidgetDataResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int ReportableCampaignCount,
    int SubmittedResponseCount,
    int ScoreCount,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    int MissingPrerequisiteCount,
    IReadOnlyList<ReportWidgetPrerequisiteResponse> MissingPrerequisites);

public sealed record ReportWidgetPrerequisiteResponse(
    string Code,
    string Label,
    string Message,
    string Severity);

public sealed record ScoreCoverageWidgetDataResponse(
    int SubmittedResponseCount,
    int ScoredSubmittedResponseCount,
    int UnscoredSubmittedResponseCount,
    int NotConfiguredSubmittedResponseCount,
    int CampaignsWithScoringRuleCount,
    int CampaignsWithoutScoringRuleCount,
    DateTimeOffset? LatestScoringActivityAt,
    string Status,
    string Guidance);

public sealed record SelectedCampaignReportStateWidgetDataResponse(
    Guid CampaignId,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    DateTimeOffset? LatestLaunchAt,
    int SubmittedResponseCount,
    int ScoreCount,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    string DisclosureState,
    int? DisclosureKMin,
    string ReportStatus,
    string InterpretationStatus,
    Guid? LatestExportArtifactId,
    string? LatestExportArtifactFileName,
    string? LatestExportArtifactStatus,
    DateTimeOffset? LatestExportArtifactCreatedAt,
    DateTimeOffset? LatestExportArtifactCompletedAt,
    DateTimeOffset? LatestExportArtifactFailedAt,
    string? LatestExportArtifactFailureReasonCode,
    bool LatestExportArtifactCanDownload,
    DateTimeOffset? ClosedAt,
    string DataFinality);

public sealed record ExportArtifactRegistryWidgetDataResponse(
    int ExportArtifactCount,
    IReadOnlyList<ExportArtifactRegistryItemResponse> Artifacts);

public sealed record ExportArtifactRegistryItemResponse(
    Guid Id,
    string TargetKind,
    Guid TargetId,
    string TargetLabel,
    Guid? CampaignId,
    string? CampaignName,
    string ArtifactType,
    string Status,
    string Format,
    string FileName,
    int RowCount,
    long ByteSize,
    string? ChecksumSha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? DeletedAt,
    string? FailureReasonCode,
    bool CanDownload,
    string? CampaignStatus,
    DateTimeOffset? CampaignClosedAt,
    string? DataFinality,
    bool CanRetry = false);

public sealed record VisualAnalyticsEntryWidgetDataResponse(
    Guid? SelectedCampaignId,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    int ReportableCampaignCount,
    CampaignSeriesResultsAnalyticsResponse? Analytics = null);

public sealed record ResultsDashboardWidgetDataResponse(
    CampaignSeriesResultsDashboardResponse Dashboard);

public sealed record CampaignSeriesResultsDashboardResponse(
    Guid? SelectedCampaignId,
    string? SelectedCampaignName,
    int DisclosureKMin,
    string DisclosureState,
    IReadOnlyList<ResultsDashboardMetricResponse> Metrics,
    IReadOnlyList<ResultsDashboardBarResponse> OutputBars,
    IReadOnlyList<ResultsDashboardBarResponse> GroupBars,
    IReadOnlyList<ResultsDashboardPointResponse> WaveTrendPoints,
    IReadOnlyList<ResultsDashboardNoteResponse> Notes);

public sealed record ResultsDashboardMetricResponse(
    string Id,
    decimal? Value,
    string Unit,
    string? Detail,
    string Tone);

public sealed record ResultsDashboardBarResponse(
    string Id,
    string Label,
    string DimensionCode,
    string Disclosure,
    decimal? Value,
    int? Count,
    string? Detail,
    string? SuppressionReason);

public sealed record ResultsDashboardPointResponse(
    string Id,
    Guid CampaignId,
    string CampaignName,
    string DimensionCode,
    string Disclosure,
    decimal? Value,
    decimal? DeltaFromPrevious,
    string ComparisonState,
    string DataFinality,
    int? Count,
    string? SuppressionReason);

public sealed record ResultsDashboardNoteResponse(
    string Kind,
    string Severity,
    string Title,
    string Detail);

public sealed record CampaignSeriesResultsAnalyticsResponse(
    Guid? SelectedCampaignId,
    string? SelectedCampaignName,
    int DisclosureKMin,
    string DisclosureState,
    IReadOnlyList<CampaignSeriesResultsScoreOutputResponse> ScoreOutputs,
    IReadOnlyList<CampaignSeriesResultsGroupMatrixRowResponse> GroupRows,
    IReadOnlyList<CampaignSeriesResultsWaveMatrixRowResponse> WaveRows,
    IReadOnlyList<CampaignSeriesResultsInsightResponse> Insights);

public sealed record CampaignSeriesResultsScoreOutputResponse(
    string DimensionCode,
    string Disclosure,
    int? SubmittedResponseCount,
    int? ScoreCount,
    decimal? Mean,
    decimal? Median,
    decimal? StandardDeviation,
    decimal? Min,
    decimal? Max,
    int? NValidTotal,
    int? NExpectedTotal,
    string? MissingPolicyStatusSummary,
    string? SuppressionReason);

public sealed record CampaignSeriesResultsGroupMatrixRowResponse(
    string GroupType,
    string GroupName,
    string DimensionCode,
    string Disclosure,
    int? SubmittedResponseCount,
    int? ScoreCount,
    decimal? Mean,
    decimal? Median,
    decimal? StandardDeviation,
    decimal? Min,
    decimal? Max,
    string? SuppressionReason);

public sealed record CampaignSeriesResultsWaveMatrixRowResponse(
    Guid CampaignId,
    string CampaignName,
    string CampaignStatus,
    string DataFinality,
    DateTimeOffset? ClosedAt,
    string DimensionCode,
    string Disclosure,
    int? SubmittedResponseCount,
    int? ScoreCount,
    decimal? Mean,
    decimal? Median,
    decimal? StandardDeviation,
    decimal? Min,
    decimal? Max,
    string? SuppressionReason,
    decimal? DeltaFromPreviousMean = null,
    decimal? DeltaFromFirstMean = null,
    string ComparisonState = "not_comparable");

public sealed record CampaignSeriesResultsInsightResponse(
    string Kind,
    string Severity,
    string Title,
    string Detail);

public sealed record FinalityProvenanceWidgetDataResponse(
    int PreliminaryLiveReportCount,
    int ClosedWaveReportCount,
    Guid? SelectedCampaignId,
    string? SelectedCampaignStatus,
    string? SelectedDataFinality,
    DateTimeOffset? SelectedClosedAt,
    DateTimeOffset? SelectedLatestLaunchAt);

public sealed record CampaignSeriesWavesWorkspaceResponse(
    CampaignSeriesWavesSeriesResponse Series,
    CampaignSeriesWavesSummaryResponse Summary,
    CampaignSeriesWavesWaveResponse? SelectedBaselineWave,
    CampaignSeriesWavesWaveResponse? SelectedComparisonWave,
    CampaignSeriesWavesComparisonResponse Comparison,
    IReadOnlyList<CampaignSeriesWavesMissingPrerequisiteResponse> MissingPrerequisites,
    IReadOnlyList<CampaignSeriesWavesWaveResponse> Waves);

public sealed record CampaignSeriesWavesSeriesResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string StudyKind = "own",
    bool IsSample = false,
    string? SampleScenario = null,
    string? ReadOnlyReason = null,
    CampaignSeriesStudyBriefResponse? StudyBrief = null);

public sealed record CampaignSeriesWavesSummaryResponse(
    int CampaignCount,
    int LiveCampaignCount,
    int LongitudinalWaveCount,
    int SubmittedWaveCount,
    int LinkedTrajectoryCount,
    int CompleteTrajectoryCount,
    int ComparableScoreCount,
    int VisibleComparisonCount,
    int SuppressedComparisonCount,
    int BlockedComparisonCount,
    int MissingPrerequisiteCount,
    int PreliminaryLiveWaveCount = 0,
    int ClosedWaveCount = 0);

public sealed record CampaignSeriesWavesWaveResponse(
    Guid Id,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    Guid? LatestLaunchSnapshotId,
    DateTimeOffset? LatestLaunchAt,
    Guid? ScoringRuleId,
    string? ScoringRuleKey,
    string? ScoringRuleVersion,
    Guid? DisclosurePolicyId,
    int? DisclosureKMin,
    int SubmittedResponseCount,
    int ScoreCount,
    int LinkedTrajectoryCount,
    string WaveState,
    DateTimeOffset? ClosedAt = null,
    Guid? ClosedByUserId = null,
    string? CloseReason = null,
    string DataFinality = "not_reportable",
    ProductSurfaceLaunchPacketProvenanceResponse? LaunchPacket = null);

public sealed record CampaignSeriesWavesComparisonResponse(
    string Status,
    string DisclosureState,
    string CompatibilityState,
    string InterpretationStatus,
    int? DisclosureKMin,
    int LinkedPairCount,
    int VisibleScoreCount,
    int SuppressedScoreCount,
    int BlockedScoreCount);

public sealed record CampaignSeriesWavesMissingPrerequisiteResponse(
    string Code,
    string Label,
    string Message,
    string Severity);

public sealed record SubjectDirectoryResponse(
    Guid TenantId,
    SubjectDirectorySummaryResponse Summary,
    IReadOnlyList<SubjectDirectoryItemResponse> Subjects);

public sealed record SubjectDirectorySummaryResponse(
    int SubjectCount,
    int GroupCount,
    int ManagerRelationshipCount);

public sealed record SubjectDirectoryItemResponse(
    Guid Id,
    string? DisplayName,
    string? Email,
    string? ExternalId,
    string Locale,
    string Attributes,
    Guid? ManagerSubjectId,
    string? ManagerDisplayName,
    int DirectReportCount,
    IReadOnlyList<SubjectGroupMembershipResponse> Groups);

public sealed record SubjectGroupMembershipResponse(
    Guid GroupId,
    string GroupType,
    string GroupName,
    string? RoleInGroup,
    DateOnly? ValidFrom,
    DateOnly? ValidTo);

public sealed record SubjectGroupListResponse(
    Guid TenantId,
    IReadOnlyList<SubjectGroupResponse> Groups);

public sealed record SubjectGroupResponse(
    Guid Id,
    string Type,
    string Name,
    Guid? ParentGroupId,
    string Attributes,
    int MemberCount);

public sealed record RespondentRulePreviewRequest(
    string Rule,
    Guid? TargetSubjectId = null,
    Guid? GroupId = null,
    int MaxRows = 50);

public sealed record RespondentRulePreviewResponse(
    Guid CampaignSeriesId,
    Guid CampaignId,
    string RuleKind,
    string Role,
    RespondentRulePreviewSummaryResponse Summary,
    IReadOnlyList<RespondentRulePreviewRowResponse> Rows,
    IReadOnlyList<RespondentRulePreviewWarningResponse> Warnings);

public sealed record RespondentRulePreviewSummaryResponse(
    int TargetCount,
    int RespondentCount,
    int AssignmentPairCount,
    int SkippedCount,
    int WarningCount,
    bool Truncated);

public sealed record RespondentRulePreviewRowResponse(
    int Ordinal,
    string RuleKind,
    string Role,
    RespondentRulePreviewSubjectResponse? Target,
    RespondentRulePreviewSubjectResponse? Respondent);

public sealed record RespondentRulePreviewSubjectResponse(
    Guid Id,
    string Label,
    string? DisplayName,
    string? Email,
    string? ExternalId);

public sealed record RespondentRulePreviewWarningResponse(
    string Code,
    string Message,
    Guid? SubjectId = null,
    Guid? GroupId = null);

public sealed record CreateSubjectRequest(
    string? DisplayName,
    string? Email,
    string? ExternalId,
    string Locale = "en",
    string Attributes = "{}");

public sealed record UpdateSubjectRequest(
    string? DisplayName,
    string? Email,
    string? ExternalId,
    string Locale = "en",
    string Attributes = "{}");

public sealed record SubjectDirectoryCsvImportRequest(
    string CsvContent,
    bool DryRun = false);

public sealed record SubjectDirectoryCsvImportResponse(
    Guid TenantId,
    int RowCount,
    int ImportedRowCount,
    int CreatedSubjectCount,
    int UpdatedSubjectCount,
    int CreatedGroupCount,
    int AddedMembershipCount,
    int SkippedMembershipCount,
    IReadOnlyList<SubjectDirectoryCsvImportRowResponse> Rows,
    bool DryRun = false);

public sealed record SubjectDirectoryCsvImportRowResponse(
    int RowNumber,
    string Status,
    string? ExternalId,
    string? Email,
    string? DisplayName,
    string? GroupType,
    string? GroupName,
    string Action,
    IReadOnlyList<string> Issues);

public sealed record CreateSubjectGroupRequest(
    string Type,
    string Name,
    Guid? ParentGroupId = null,
    string Attributes = "{}");

public sealed record AddSubjectGroupMemberRequest(
    Guid SubjectId,
    string? RoleInGroup = null,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record SetSubjectManagerRequest(
    Guid? ManagerSubjectId,
    DateOnly? ValidFrom = null);
