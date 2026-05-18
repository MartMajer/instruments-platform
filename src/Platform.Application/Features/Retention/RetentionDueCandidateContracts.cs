namespace Platform.Application.Features.Retention;

public static class RetentionDueCandidateStatuses
{
    public const string Ready = "ready";
    public const string Unsupported = "unsupported";
}

public static class RetentionDueCandidateEntities
{
    public const string ConsentRecord = "consent_record";
    public const string ResponseSession = "response_session";
    public const string Answer = "answer";
    public const string ScoreRun = "score_run";
    public const string Score = "score";
    public const string DerivedArtifact = "derived_artifact";
}

public static class RetentionDueCandidateDiagnosticCodes
{
    public const string UnsupportedAnchor = "retention_policy.anchor_unsupported";
    public const string UnsupportedAction = "retention_policy.action_unsupported";
}

public sealed record RetentionDueCandidatePlanResponse(
    Guid CampaignSeriesId,
    DateTimeOffset AsOf,
    IReadOnlyList<RetentionDueCandidateBatch> Batches);

public sealed record RetentionDueCandidateBatch(
    Guid RetentionPolicyId,
    Guid CampaignSeriesId,
    string Anchor,
    string ActionAfter,
    string Status,
    DateTimeOffset? DueBefore,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount,
    int DerivedArtifactCount,
    IReadOnlyList<RetentionDueCandidateDependency> Dependencies,
    IReadOnlyList<RetentionDueCandidateDiagnostic> Diagnostics);

public sealed record RetentionDueCandidateDependency(
    string Entity,
    int Count);

public sealed record RetentionDueCandidateDiagnostic(
    string Code,
    string Message);
