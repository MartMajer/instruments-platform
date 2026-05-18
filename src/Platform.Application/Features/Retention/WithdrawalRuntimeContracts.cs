namespace Platform.Application.Features.Retention;

public sealed record CreateWithdrawalRequestCommand(
    string TargetKind,
    Guid TargetId,
    string RequestedAction,
    Guid? ActorUserId,
    string? ReasonCode);

public sealed record IssueWithdrawalRequestTokenCommand(
    Guid ResponseSessionId,
    string RequestedAction,
    DateTimeOffset ExpiresAt,
    string? ReasonCode);

public sealed record CreateAnonymousWithdrawalRequestCommand(
    string Token,
    string RequestedAction,
    string? ReasonCode);

public sealed record CreateWithdrawalRequestRequest(
    string TargetKind,
    Guid TargetId,
    string RequestedAction,
    string? ReasonCode = null);

public sealed record CreateAnonymousWithdrawalRequestRequest(
    string Token,
    string RequestedAction,
    string? ReasonCode = null);

public sealed record IssueWithdrawalRequestTokenRequest(
    Guid ResponseSessionId,
    string RequestedAction,
    DateTimeOffset ExpiresAt,
    string? ReasonCode = null);

public sealed record WithdrawalRequestDecisionCommand(
    Guid ActorUserId,
    string? ReasonCode);

public sealed record WithdrawalRequestDecisionRequest(
    string? ReasonCode = null);

public sealed record WithdrawalRequestResponse(
    Guid RequestId,
    string TargetKind,
    Guid TargetId,
    string RequestedAction,
    string Status,
    DateTimeOffset RequestedAt,
    bool Idempotent,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount);

public sealed record WithdrawalRequestTokenIssueResponse(
    Guid TokenId,
    Guid ResponseSessionId,
    string RequestedAction,
    DateTimeOffset ExpiresAt,
    string RawToken);

public sealed record WithdrawalRequestReviewResponse(
    Guid RequestId,
    string TargetKind,
    Guid TargetId,
    string RequestedAction,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ProcessedAt,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount,
    bool CanApprove = false,
    bool CanDeny = false,
    bool CanExecute = false);

public sealed record WithdrawalEventResponse(
    Guid Id,
    Guid CampaignSeriesId,
    string TargetKind,
    string Scope,
    string ActionAfter,
    string Status,
    bool TargetMatched,
    DateTimeOffset RequestedAt,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount);

public static class WithdrawalDryRunDependencyEntities
{
    public const string ConsentRecord = "consent_record";
    public const string ResponseSession = "response_session";
    public const string Answer = "answer";
    public const string ScoreRun = "score_run";
    public const string Score = "score";
}

public sealed record WithdrawalDryRunDependency(
    string Entity,
    int Count);

public sealed record WithdrawalDryRunResponse(
    Guid WithdrawalEventId,
    Guid CampaignSeriesId,
    string TargetKind,
    string Scope,
    string ActionAfter,
    string Status,
    bool TargetMatched,
    int ConsentRecordCount,
    int ResponseSessionCount,
    int AnswerCount,
    int ScoreRunCount,
    int ScoreCount,
    IReadOnlyList<WithdrawalDryRunDependency> Dependencies);

public sealed record WithdrawalExecutionStateResponse(
    Guid WithdrawalEventId,
    string Status,
    DateTimeOffset? ProcessedAt,
    WithdrawalDryRunResponse DryRun);
