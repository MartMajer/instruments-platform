using System.Globalization;
using System.Text;

namespace Platform.Domain.Consent;

public static class RetentionDueBatchStatuses
{
    public const string Planned = "planned";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";

    public static bool IsKnown(string value) => value is Planned or Processing or Completed or Failed;
}

public sealed class RetentionDueBatch
{
    private RetentionDueBatch()
    {
    }

    private RetentionDueBatch(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        string anchor,
        string actionAfter,
        string status,
        DateTimeOffset asOf,
        DateTimeOffset dueBefore,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        int derivedArtifactCount,
        string idempotencyKey,
        DateTimeOffset createdAt)
    {
        if (dueBefore > asOf)
        {
            throw new ArgumentOutOfRangeException(nameof(dueBefore), "Retention due boundary cannot be after as-of time.");
        }

        Id = id;
        TenantId = tenantId;
        CampaignSeriesId = campaignSeriesId;
        RetentionPolicyId = retentionPolicyId;
        Anchor = NormalizeAnchor(anchor);
        ActionAfter = NormalizeAction(actionAfter);
        Status = NormalizeStatus(status);
        AsOf = asOf;
        DueBefore = dueBefore;
        ConsentRecordCount = EnsureNonNegative(consentRecordCount, nameof(consentRecordCount));
        ResponseSessionCount = EnsurePositive(responseSessionCount, nameof(responseSessionCount));
        AnswerCount = EnsureNonNegative(answerCount, nameof(answerCount));
        ScoreRunCount = EnsureNonNegative(scoreRunCount, nameof(scoreRunCount));
        ScoreCount = EnsureNonNegative(scoreCount, nameof(scoreCount));
        DerivedArtifactCount = EnsureNonNegative(derivedArtifactCount, nameof(derivedArtifactCount));
        IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public Guid RetentionPolicyId { get; private set; }

    public string Anchor { get; private set; } = RetentionPolicy.ResponseSubmittedAt;

    public string ActionAfter { get; private set; } = RetentionPolicy.Anonymize;

    public string Status { get; private set; } = RetentionDueBatchStatuses.Planned;

    public DateTimeOffset AsOf { get; private set; }

    public DateTimeOffset DueBefore { get; private set; }

    public int ConsentRecordCount { get; private set; }

    public int ResponseSessionCount { get; private set; }

    public int AnswerCount { get; private set; }

    public int ScoreRunCount { get; private set; }

    public int ScoreCount { get; private set; }

    public int DerivedArtifactCount { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureDetail { get; private set; }

    public string? ExecutionResult { get; private set; }

    public int? ArtifactInvalidatedCount { get; private set; }

    public int? NoticeScrubbedCount { get; private set; }

    public int? DeliveryAttemptScrubbedCount { get; private set; }

    public int? InviteCredentialScrubbedCount { get; private set; }

    public static RetentionDueBatch Plan(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        string anchor,
        string actionAfter,
        DateTimeOffset asOf,
        DateTimeOffset dueBefore,
        int consentRecordCount,
        int responseSessionCount,
        int answerCount,
        int scoreRunCount,
        int scoreCount,
        int derivedArtifactCount,
        string idempotencyKey,
        DateTimeOffset createdAt)
    {
        return new RetentionDueBatch(
            id,
            tenantId,
            campaignSeriesId,
            retentionPolicyId,
            anchor,
            actionAfter,
            RetentionDueBatchStatuses.Planned,
            asOf,
            dueBefore,
            consentRecordCount,
            responseSessionCount,
            answerCount,
            scoreRunCount,
            scoreCount,
            derivedArtifactCount,
            idempotencyKey,
            createdAt);
    }

    public void Claim(DateTimeOffset processingStartedAt)
    {
        if (Status != RetentionDueBatchStatuses.Planned)
        {
            throw new InvalidOperationException("Only planned retention due-batches can be claimed.");
        }

        Status = RetentionDueBatchStatuses.Processing;
        ProcessingStartedAt = processingStartedAt;
        CompletedAt = null;
        FailedAt = null;
        FailureCode = null;
        FailureDetail = null;
        ExecutionResult = null;
        ArtifactInvalidatedCount = null;
        NoticeScrubbedCount = null;
        DeliveryAttemptScrubbedCount = null;
        InviteCredentialScrubbedCount = null;
    }

    public void Complete(
        DateTimeOffset completedAt,
        string executionResult = "manual_completed",
        int artifactInvalidatedCount = 0,
        int noticeScrubbedCount = 0,
        int deliveryAttemptScrubbedCount = 0,
        int inviteCredentialScrubbedCount = 0)
    {
        if (Status != RetentionDueBatchStatuses.Processing)
        {
            throw new InvalidOperationException("Only processing retention due-batches can be completed.");
        }

        Status = RetentionDueBatchStatuses.Completed;
        CompletedAt = completedAt;
        FailedAt = null;
        FailureCode = null;
        FailureDetail = null;
        ExecutionResult = NormalizeFailureCode(executionResult);
        ArtifactInvalidatedCount = EnsureNonNegative(artifactInvalidatedCount, nameof(artifactInvalidatedCount));
        NoticeScrubbedCount = EnsureNonNegative(noticeScrubbedCount, nameof(noticeScrubbedCount));
        DeliveryAttemptScrubbedCount = EnsureNonNegative(deliveryAttemptScrubbedCount, nameof(deliveryAttemptScrubbedCount));
        InviteCredentialScrubbedCount = EnsureNonNegative(inviteCredentialScrubbedCount, nameof(inviteCredentialScrubbedCount));
    }

    public void Fail(string failureCode, string? failureDetail, DateTimeOffset failedAt)
    {
        if (Status is RetentionDueBatchStatuses.Completed or RetentionDueBatchStatuses.Failed)
        {
            throw new InvalidOperationException("Completed or failed retention due-batches are terminal.");
        }

        Status = RetentionDueBatchStatuses.Failed;
        FailedAt = failedAt;
        CompletedAt = null;
        FailureCode = NormalizeFailureCode(failureCode);
        FailureDetail = NormalizeFailureDetail(failureDetail);
        ExecutionResult = null;
        ArtifactInvalidatedCount = null;
        NoticeScrubbedCount = null;
        DeliveryAttemptScrubbedCount = null;
        InviteCredentialScrubbedCount = null;
    }

    public static string CreateIdempotencyKey(
        Guid campaignSeriesId,
        Guid retentionPolicyId,
        string anchor,
        string actionAfter,
        DateTimeOffset asOf,
        DateTimeOffset dueBefore)
    {
        return $"retention_due_batch:{campaignSeriesId:N}:{retentionPolicyId:N}:{NormalizeAnchor(anchor)}:{NormalizeAction(actionAfter)}:{NormalizeTime(asOf)}:{NormalizeTime(dueBefore)}";
    }

    private static string NormalizeTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfffffff'Z'", CultureInfo.InvariantCulture);
    }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Retention due-batch counts must not be negative.");
        }

        return value;
    }

    private static int EnsurePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Retention due-batch response session count must be positive.");
        }

        return value;
    }

    private static string NormalizeAnchor(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (normalized is not RetentionPolicy.ResponseSubmittedAt)
        {
            throw new ArgumentException("Unknown retention due-batch anchor.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeAction(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (normalized is not (RetentionPolicy.Delete or RetentionPolicy.Anonymize))
        {
            throw new ArgumentException("Unknown retention due-batch action.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (!RetentionDueBatchStatuses.IsKnown(normalized))
        {
            throw new ArgumentException("Unknown retention due-batch status.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeIdempotencyKey(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (normalized.Length > 256)
        {
            throw new ArgumentException("Retention due-batch idempotency key is too long.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeFailureCode(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value)).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'
                ? character
                : '_');
        }

        var code = builder.ToString().Trim('_');
        if (code.Length == 0)
        {
            code = "retention_due_batch.failed";
        }

        return code.Length <= 128 ? code : code[..128];
    }

    private static string? NormalizeFailureDetail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (ContainsSensitiveFailureDetail(normalized))
        {
            return "[redacted]";
        }

        return normalized.Length <= 512 ? normalized : normalized[..512];
    }

    private static bool ContainsSensitiveFailureDetail(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower.Contains('@') ||
            lower.Contains("token", StringComparison.Ordinal) ||
            lower.Contains("recipient", StringComparison.Ordinal) ||
            lower.Contains("provider", StringComparison.Ordinal) ||
            lower.Contains("participant", StringComparison.Ordinal) ||
            lower.Contains("subject", StringComparison.Ordinal) ||
            lower.Contains("answer", StringComparison.Ordinal) ||
            lower.Contains("salt", StringComparison.Ordinal) ||
            lower.Contains("ip", StringComparison.Ordinal) ||
            lower.Contains("user-agent", StringComparison.Ordinal) ||
            lower.Contains("public-handle", StringComparison.Ordinal) ||
            lower.Contains("hash", StringComparison.Ordinal);
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
