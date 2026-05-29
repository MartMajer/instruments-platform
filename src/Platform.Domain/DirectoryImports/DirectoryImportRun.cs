namespace Platform.Domain.DirectoryImports;

public sealed class DirectoryImportRun
{
    private DirectoryImportRun()
    {
    }

    public DirectoryImportRun(
        Guid id,
        Guid tenantId,
        Guid ruleId,
        string mode,
        Guid? createdByUserId = null,
        DateTimeOffset? startedAt = null)
    {
        Id = id;
        TenantId = RequireNonEmpty(tenantId, nameof(tenantId));
        RuleId = RequireNonEmpty(ruleId, nameof(ruleId));
        Mode = NormalizeKnown(mode, DirectoryImportRunModes.IsKnown, nameof(mode));
        Status = DirectoryImportRunStatuses.Planned;
        StartedAt = startedAt ?? DateTimeOffset.UtcNow;
        CreatedAt = StartedAt;
        UpdatedAt = CreatedAt;
        CreatedByUserId = createdByUserId;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid RuleId { get; private set; }

    public string Mode { get; private set; } = DirectoryImportRunModes.Preview;

    public string Status { get; private set; } = DirectoryImportRunStatuses.Planned;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public string SummaryJson { get; private set; } = "{}";

    public string? ErrorCode { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkPreviewed(string summaryJson, DateTimeOffset previewedAt)
    {
        EnsureStatus(DirectoryImportRunStatuses.Planned, "Only planned directory import runs can be marked previewed.");

        Status = DirectoryImportRunStatuses.Previewed;
        SummaryJson = DirectoryImportJson.RequireObject(summaryJson, nameof(summaryJson));
        UpdatedAt = previewedAt;
    }

    public void StartApplying(DateTimeOffset applyingAt)
    {
        EnsureStatus(DirectoryImportRunStatuses.Previewed, "Only previewed directory import runs can start applying.");

        Status = DirectoryImportRunStatuses.Applying;
        UpdatedAt = applyingAt;
    }

    public void MarkApplied(string summaryJson, DateTimeOffset appliedAt)
    {
        EnsureStatus(DirectoryImportRunStatuses.Applying, "Only applying directory import runs can be marked applied.");

        Status = DirectoryImportRunStatuses.Applied;
        SummaryJson = DirectoryImportJson.RequireObject(summaryJson, nameof(summaryJson));
        FinishedAt = appliedAt;
        UpdatedAt = appliedAt;
    }

    public void MarkFailed(string errorCode, string summaryJson, DateTimeOffset failedAt)
    {
        if (Status is DirectoryImportRunStatuses.Applied or DirectoryImportRunStatuses.Failed)
        {
            throw new InvalidOperationException("Completed directory import runs cannot be marked failed.");
        }

        Status = DirectoryImportRunStatuses.Failed;
        ErrorCode = NormalizeRequired(errorCode, nameof(errorCode));
        SummaryJson = DirectoryImportJson.RequireObject(summaryJson, nameof(summaryJson));
        FinishedAt = failedAt;
        UpdatedAt = failedAt;
    }

    private void EnsureStatus(string expected, string message)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static Guid RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value must not be an empty GUID.", parameterName);
        }

        return value;
    }

    private static string NormalizeKnown(string value, Func<string, bool> isKnown, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();
        if (!isKnown(normalized))
        {
            throw new ArgumentException("Value is not supported.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
