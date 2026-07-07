using System.Text.Json;

namespace Platform.Domain.Consent;

public static class WithdrawalTargetKinds
{
    public const string IdentifiedSubject = "identified_subject";
    public const string AnonymousLongitudinalCode = "anonymous_longitudinal_code";
    public const string AnonymousLongitudinalUnmatched = "anonymous_longitudinal_unmatched";
    public const string ResponseSession = "response_session";

    public static bool IsKnown(string value) =>
        value is IdentifiedSubject or AnonymousLongitudinalCode or AnonymousLongitudinalUnmatched or ResponseSession;
}

public static class WithdrawalScopes
{
    public const string CampaignSeries = "campaign_series";

    public static bool IsKnown(string value) => value is CampaignSeries;
}

public static class WithdrawalEventStatuses
{
    public const string Requested = "requested";
    public const string Planned = "planned";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Denied = "denied";

    public static bool IsKnown(string value) => value is Requested or Planned or Processing or Completed or Failed or Denied;
}

public sealed class WithdrawalEvent
{
    private static readonly string[] SensitiveMetadataMarkers =
    [
        "raw",
        "answer",
        "token",
        "recipient",
        "providermessage",
        "iphash",
        "useragenthash",
        "participantcode",
        "publichandle",
        "salt"
    ];

    private WithdrawalEvent()
    {
    }

    private WithdrawalEvent(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        string targetKind,
        string scope,
        string actionAfter,
        string status,
        DateTimeOffset requestedAt,
        Guid? subjectId,
        Guid? participantCodeId,
        Guid? responseSessionId,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        string metadataJson,
        DateTimeOffset? processedAt = null)
    {
        Id = id;
        TenantId = tenantId;
        CampaignSeriesId = campaignSeriesId;
        RetentionPolicyId = retentionPolicyId;
        TargetKind = NormalizeTargetKind(targetKind);
        Scope = NormalizeScope(scope);
        ActionAfter = NormalizeAction(actionAfter);
        Status = NormalizeStatus(status);
        RequestedAt = requestedAt;
        ProcessedAt = processedAt;
        SubjectId = subjectId;
        ParticipantCodeId = participantCodeId;
        ResponseSessionId = responseSessionId;
        ConsentRecordCount = EnsureNonNegative(consentRecordCount, nameof(consentRecordCount));
        ResponseSessionCount = EnsureNonNegative(responseSessionCount, nameof(responseSessionCount));
        AnswerCount = EnsureNonNegative(answerCount, nameof(answerCount));
        ScoreRunCount = EnsureNonNegative(scoreRunCount, nameof(scoreRunCount));
        ScoreCount = EnsureNonNegative(scoreCount, nameof(scoreCount));
        MetadataJson = NormalizeMetadata(metadataJson);
        CreatedAt = DateTimeOffset.UtcNow;

        EnsureTargetShape();
        if (ProcessedAt.HasValue && ProcessedAt.Value < RequestedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(processedAt), "Processed time cannot be before request time.");
        }
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public Guid RetentionPolicyId { get; private set; }

    public string TargetKind { get; private set; } = string.Empty;

    public string Scope { get; private set; } = WithdrawalScopes.CampaignSeries;

    public string ActionAfter { get; private set; } = RetentionPolicy.Anonymize;

    public string Status { get; private set; } = WithdrawalEventStatuses.Planned;

    public Guid? SubjectId { get; private set; }

    public Guid? ParticipantCodeId { get; private set; }

    public Guid? ResponseSessionId { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int ConsentRecordCount { get; private set; }

    public int ResponseSessionCount { get; private set; }

    public int AnswerCount { get; private set; }

    public int ScoreRunCount { get; private set; }

    public int ScoreCount { get; private set; }

    public string MetadataJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public static WithdrawalEvent PlanIdentified(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        Guid subjectId,
        string actionAfter,
        DateTimeOffset requestedAt,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        string metadataJson = "{}")
    {
        return new WithdrawalEvent(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            WithdrawalTargetKinds.IdentifiedSubject,
            WithdrawalScopes.CampaignSeries,
            actionAfter,
            WithdrawalEventStatuses.Planned,
            requestedAt,
            subjectId,
            participantCodeId: null,
            responseSessionId: null,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            metadataJson);
    }

    public static WithdrawalEvent PlanAnonymousLongitudinal(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        Guid participantCodeId,
        string actionAfter,
        DateTimeOffset requestedAt,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        string metadataJson = "{}")
    {
        return new WithdrawalEvent(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            WithdrawalTargetKinds.AnonymousLongitudinalCode,
            WithdrawalScopes.CampaignSeries,
            actionAfter,
            WithdrawalEventStatuses.Planned,
            requestedAt,
            subjectId: null,
            participantCodeId,
            responseSessionId: null,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            metadataJson);
    }

    public static WithdrawalEvent PlanAnonymousLongitudinalUnmatched(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        string actionAfter,
        DateTimeOffset requestedAt,
        string metadataJson = "{}")
    {
        return new WithdrawalEvent(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            WithdrawalTargetKinds.AnonymousLongitudinalUnmatched,
            WithdrawalScopes.CampaignSeries,
            actionAfter,
            WithdrawalEventStatuses.Planned,
            requestedAt,
            subjectId: null,
            participantCodeId: null,
            responseSessionId: null,
            consentRecordCount: 0,
            responseSessionCount: 0,
            answerCount: 0,
            scoreRunCount: 0,
            scoreCount: 0,
            metadataJson);
    }

    public static WithdrawalEvent RequestResponseSession(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        Guid responseSessionId,
        string actionAfter,
        DateTimeOffset requestedAt,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        string metadataJson = "{}")
    {
        return new WithdrawalEvent(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            WithdrawalTargetKinds.ResponseSession,
            WithdrawalScopes.CampaignSeries,
            actionAfter,
            WithdrawalEventStatuses.Requested,
            requestedAt,
            subjectId: null,
            participantCodeId: null,
            responseSessionId,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            metadataJson);
    }

    public static WithdrawalEvent RequestIdentifiedSubject(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        Guid subjectId,
        string actionAfter,
        DateTimeOffset requestedAt,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        string metadataJson = "{}")
    {
        return new WithdrawalEvent(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            WithdrawalTargetKinds.IdentifiedSubject,
            WithdrawalScopes.CampaignSeries,
            actionAfter,
            WithdrawalEventStatuses.Requested,
            requestedAt,
            subjectId,
            participantCodeId: null,
            responseSessionId: null,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            metadataJson);
    }

    public void ApproveRequest(string metadataJson)
    {
        if (Status != WithdrawalEventStatuses.Requested)
        {
            throw new InvalidOperationException("Only requested withdrawal events can be approved.");
        }

        MetadataJson = NormalizeMetadata(metadataJson);
        ProcessedAt = null;
        Status = WithdrawalEventStatuses.Planned;
    }

    public void DenyRequest(DateTimeOffset processedAt, string metadataJson)
    {
        if (Status != WithdrawalEventStatuses.Requested)
        {
            throw new InvalidOperationException("Only requested withdrawal events can be denied.");
        }

        EnsureProcessedAt(processedAt);
        MetadataJson = NormalizeMetadata(metadataJson);
        ProcessedAt = processedAt;
        Status = WithdrawalEventStatuses.Denied;
    }

    public void MarkProcessing()
    {
        if (Status != WithdrawalEventStatuses.Planned)
        {
            throw new InvalidOperationException("Only planned withdrawal events can be claimed for processing.");
        }

        Status = WithdrawalEventStatuses.Processing;
    }

    public void MarkCompleted(DateTimeOffset processedAt, string metadataJson)
    {
        if (Status != WithdrawalEventStatuses.Processing)
        {
            throw new InvalidOperationException("Only processing withdrawal events can be completed.");
        }

        EnsureProcessedAt(processedAt);
        MetadataJson = NormalizeMetadata(metadataJson);
        ProcessedAt = processedAt;
        Status = WithdrawalEventStatuses.Completed;
    }

    public void DetachDeletedResponseSessionTarget()
    {
        if (TargetKind != WithdrawalTargetKinds.ResponseSession || ActionAfter != RetentionPolicy.Delete)
        {
            throw new InvalidOperationException("Only delete response-session withdrawal events can detach their target.");
        }

        if (Status != WithdrawalEventStatuses.Processing)
        {
            throw new InvalidOperationException("Only processing withdrawal events can detach their target.");
        }

        ResponseSessionId = null;
    }

    public void MarkFailed(DateTimeOffset processedAt, string metadataJson)
    {
        if (Status is WithdrawalEventStatuses.Completed or WithdrawalEventStatuses.Failed or WithdrawalEventStatuses.Denied)
        {
            throw new InvalidOperationException("Completed, failed, or denied withdrawal events cannot be failed again.");
        }

        EnsureProcessedAt(processedAt);
        MetadataJson = NormalizeMetadata(metadataJson);
        ProcessedAt = processedAt;
        Status = WithdrawalEventStatuses.Failed;
    }

    private void EnsureTargetShape()
    {
        var valid = TargetKind switch
        {
            WithdrawalTargetKinds.IdentifiedSubject =>
                SubjectId.HasValue && !ParticipantCodeId.HasValue && !ResponseSessionId.HasValue,
            WithdrawalTargetKinds.AnonymousLongitudinalCode =>
                !SubjectId.HasValue && ParticipantCodeId.HasValue && !ResponseSessionId.HasValue,
            WithdrawalTargetKinds.AnonymousLongitudinalUnmatched =>
                !SubjectId.HasValue && !ParticipantCodeId.HasValue && !ResponseSessionId.HasValue,
            WithdrawalTargetKinds.ResponseSession =>
                !SubjectId.HasValue && !ParticipantCodeId.HasValue && ResponseSessionId.HasValue,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException("Withdrawal target shape does not match target kind.");
        }
    }

    private void EnsureProcessedAt(DateTimeOffset processedAt)
    {
        if (processedAt < RequestedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(processedAt), "Processed time cannot be before request time.");
        }
    }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Withdrawal event counts must not be negative.");
        }

        return value;
    }

    private static string NormalizeTargetKind(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (!WithdrawalTargetKinds.IsKnown(normalized))
        {
            throw new ArgumentException("Unknown withdrawal target kind.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeScope(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (!WithdrawalScopes.IsKnown(normalized))
        {
            throw new ArgumentException("Unknown withdrawal scope.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeAction(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (normalized is not (RetentionPolicy.Anonymize or RetentionPolicy.Delete))
        {
            throw new ArgumentException("Unknown withdrawal retention action.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (!WithdrawalEventStatuses.IsKnown(normalized))
        {
            throw new ArgumentException("Unknown withdrawal event status.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeMetadata(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Withdrawal metadata must be a JSON object.", nameof(value));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Withdrawal metadata must be valid JSON.", nameof(value), exception);
        }

        var comparable = normalized.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        if (SensitiveMetadataMarkers.Any(comparable.Contains))
        {
            throw new ArgumentException("Withdrawal metadata contains sensitive data markers.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
