namespace Platform.Domain.Integrations;

public sealed class DirectoryImportRun
{
    public const int ModeMaxLength = 32;
    public const int StatusMaxLength = 32;
    public const int ErrorCategoryMaxLength = 128;

    private DirectoryImportRun()
    {
    }

    public DirectoryImportRun(
        Guid id,
        Guid tenantId,
        Guid directoryConnectionId,
        string mode,
        string ruleSnapshot,
        string retainedFields = "[]",
        string counts = "{}",
        string warningCategories = "[]",
        string checkpoint = "{}",
        string status = DirectoryImportRunStatuses.Queued,
        Guid? directoryImportRuleId = null,
        Guid? previewRunId = null,
        Guid? requestedByUserId = null,
        DateTimeOffset? observedAt = null)
    {
        if (!DirectoryImportRunModes.IsKnown(mode))
        {
            throw new ArgumentException("Unknown directory import run mode.", nameof(mode));
        }

        if (!DirectoryImportRunStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown directory import run status.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        DirectoryConnectionId = directoryConnectionId;
        DirectoryImportRuleId = directoryImportRuleId;
        PreviewRunId = previewRunId;
        Mode = mode;
        Status = status;
        RuleSnapshot = DirectoryIntegrationJson.RequireObject(ruleSnapshot, nameof(ruleSnapshot));
        RetainedFields = DirectoryIntegrationJson.RequireArray(retainedFields, nameof(retainedFields));
        Counts = DirectoryIntegrationJson.RequireObject(counts, nameof(counts));
        WarningCategories = DirectoryIntegrationJson.RequireArray(warningCategories, nameof(warningCategories));
        Checkpoint = DirectoryIntegrationJson.RequireObject(checkpoint, nameof(checkpoint));
        RequestedByUserId = requestedByUserId;
        CreatedAt = observedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid DirectoryConnectionId { get; private set; }

    public Guid? DirectoryImportRuleId { get; private set; }

    public Guid? PreviewRunId { get; private set; }

    public string Mode { get; private set; } = DirectoryImportRunModes.Preview;

    public string Status { get; private set; } = DirectoryImportRunStatuses.Queued;

    public string RuleSnapshot { get; private set; } = "{}";

    public string RetainedFields { get; private set; } = "[]";

    public string Counts { get; private set; } = "{}";

    public string WarningCategories { get; private set; } = "[]";

    public string? ErrorCategory { get; private set; }

    public string Checkpoint { get; private set; } = "{}";

    public Guid? RequestedByUserId { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Start(DateTimeOffset observedAt)
    {
        Status = DirectoryImportRunStatuses.Running;
        StartedAt = observedAt;
        UpdatedAt = observedAt;
    }

    public void Succeed(
        string counts,
        string warningCategories,
        string checkpoint,
        DateTimeOffset observedAt)
    {
        Counts = DirectoryIntegrationJson.RequireObject(counts, nameof(counts));
        WarningCategories = DirectoryIntegrationJson.RequireArray(warningCategories, nameof(warningCategories));
        Checkpoint = DirectoryIntegrationJson.RequireObject(checkpoint, nameof(checkpoint));
        Status = DirectoryImportRunStatuses.Succeeded;
        StartedAt ??= CreatedAt;
        CompletedAt = observedAt;
        ErrorCategory = null;
        UpdatedAt = observedAt;
    }

    public void Fail(string errorCategory, DateTimeOffset observedAt)
    {
        Status = DirectoryImportRunStatuses.Failed;
        ErrorCategory = NormalizeRequired(errorCategory, nameof(errorCategory), ErrorCategoryMaxLength);
        StartedAt ??= CreatedAt;
        CompletedAt = observedAt;
        UpdatedAt = observedAt;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);
        }

        return trimmed;
    }
}
