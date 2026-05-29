namespace Platform.Domain.DirectoryImports;

public sealed class DirectoryImportRunItem
{
    private DirectoryImportRunItem()
    {
    }

    public DirectoryImportRunItem(
        Guid id,
        Guid tenantId,
        Guid runId,
        string sourceObjectType,
        string sourceObjectIdHash,
        string action,
        string status,
        string? issueCode = null,
        string safeSummaryJson = "{}")
    {
        Id = id;
        TenantId = RequireNonEmpty(tenantId, nameof(tenantId));
        RunId = RequireNonEmpty(runId, nameof(runId));
        SourceObjectType = NormalizeRequired(sourceObjectType, nameof(sourceObjectType));
        SourceObjectIdHash = NormalizeRequired(sourceObjectIdHash, nameof(sourceObjectIdHash));
        Action = NormalizeKnown(action, DirectoryImportRunItemActions.IsKnown, nameof(action));
        Status = NormalizeKnown(status, DirectoryImportRunItemStatuses.IsKnown, nameof(status));
        IssueCode = NormalizeOptional(issueCode);
        SafeSummaryJson = DirectoryImportJson.RequireObject(safeSummaryJson, nameof(safeSummaryJson));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid RunId { get; private set; }

    public string SourceObjectType { get; private set; } = string.Empty;

    public string SourceObjectIdHash { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string Status { get; private set; } = DirectoryImportRunItemStatuses.Planned;

    public string? IssueCode { get; private set; }

    public string SafeSummaryJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
