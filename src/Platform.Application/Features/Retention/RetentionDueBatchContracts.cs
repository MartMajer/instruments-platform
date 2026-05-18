namespace Platform.Application.Features.Retention;

public sealed record RetentionDueBatchResponse(
    Guid Id,
    Guid CampaignSeriesId,
    Guid RetentionPolicyId,
    string Anchor,
    string ActionAfter,
    string Status,
    DateTimeOffset AsOf,
    DateTimeOffset DueBefore,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount,
    int DerivedArtifactCount,
    string IdempotencyKey,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt,
    string? FailureCode,
    string? FailureDetail,
    string? ExecutionResult,
    int? ArtifactInvalidatedCount,
    int? NoticeScrubbedCount,
    int? DeliveryAttemptScrubbedCount,
    int? InviteCredentialScrubbedCount);

public sealed record RetentionDueBatchDryRunResponse(
    RetentionDueBatchResponse Batch,
    bool ParityMatched,
    IReadOnlyList<RetentionDueBatchParityMismatch> Mismatches);

public sealed record RetentionDueBatchParityMismatch(
    string Field,
    string Planned,
    string Current);

public sealed record RetentionDueBatchExecutionResponse(
    RetentionDueBatchResponse Batch,
    string Result,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount,
    int DerivedArtifactCount,
    int ArtifactInvalidatedCount,
    int NoticeScrubbedCount,
    int DeliveryAttemptScrubbedCount,
    int InviteCredentialScrubbedCount);

public sealed record RetentionDueBatchAutomationRunResponse(
    Guid TenantId,
    DateTimeOffset AsOf,
    int MaxBatches,
    int SeriesScannedCount,
    int DueBatchCount,
    int ClaimedBatchCount,
    int CompletedBatchCount,
    int FailedBatchCount,
    int NoCandidateSeriesCount,
    int SkippedBatchCount,
    IReadOnlyList<RetentionDueBatchAutomationItemResponse> Items);

public sealed record RetentionDueBatchAutomationItemResponse(
    Guid CampaignSeriesId,
    Guid? DueBatchId,
    string Stage,
    string Status,
    string? Result,
    string? ErrorCode);
