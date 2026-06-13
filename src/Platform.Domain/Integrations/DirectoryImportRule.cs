namespace Platform.Domain.Integrations;

public sealed class DirectoryImportRule
{
    public const int NameMaxLength = 256;
    public const int StatusMaxLength = 32;
    public const int StalePolicyMaxLength = 32;

    private DirectoryImportRule()
    {
    }

    public DirectoryImportRule(
        Guid id,
        Guid tenantId,
        Guid directoryConnectionId,
        string name,
        string ruleDocument,
        string retainedFields = "[]",
        string stalePolicy = DirectoryImportStalePolicies.None,
        string status = DirectoryImportRuleStatuses.Draft,
        Guid? createdByUserId = null,
        DateTimeOffset? observedAt = null)
    {
        if (!DirectoryImportStalePolicies.IsKnown(stalePolicy))
        {
            throw new ArgumentException("Unknown directory import stale policy.", nameof(stalePolicy));
        }

        if (!DirectoryImportRuleStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown directory import rule status.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        DirectoryConnectionId = directoryConnectionId;
        Name = NormalizeRequired(name, nameof(name), NameMaxLength);
        RuleDocument = DirectoryIntegrationJson.RequireObject(ruleDocument, nameof(ruleDocument));
        RetainedFields = DirectoryIntegrationJson.RequireArray(retainedFields, nameof(retainedFields));
        StalePolicy = stalePolicy;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = observedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid DirectoryConnectionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string RuleDocument { get; private set; } = "{}";

    public string RetainedFields { get; private set; } = "[]";

    public string StalePolicy { get; private set; } = DirectoryImportStalePolicies.None;

    public string Status { get; private set; } = DirectoryImportRuleStatuses.Draft;

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void Update(
        string name,
        string ruleDocument,
        string retainedFields,
        string stalePolicy,
        DateTimeOffset observedAt)
    {
        if (!DirectoryImportStalePolicies.IsKnown(stalePolicy))
        {
            throw new ArgumentException("Unknown directory import stale policy.", nameof(stalePolicy));
        }

        Name = NormalizeRequired(name, nameof(name), NameMaxLength);
        RuleDocument = DirectoryIntegrationJson.RequireObject(ruleDocument, nameof(ruleDocument));
        RetainedFields = DirectoryIntegrationJson.RequireArray(retainedFields, nameof(retainedFields));
        StalePolicy = stalePolicy;
        Status = DirectoryImportRuleStatuses.Active;
        UpdatedAt = observedAt;
    }

    public void Archive(DateTimeOffset observedAt)
    {
        Status = DirectoryImportRuleStatuses.Archived;
        DeletedAt = observedAt;
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
