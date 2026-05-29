namespace Platform.Domain.DirectoryImports;

public sealed class DirectoryConnection
{
    private DirectoryConnection()
    {
    }

    public DirectoryConnection(
        Guid id,
        Guid tenantId,
        string provider,
        string externalTenantId,
        string displayName,
        string primaryDomain,
        string grantedScopesJson,
        string status = DirectoryConnectionStatuses.Active,
        DateTimeOffset? lastSuccessfulSyncAt = null)
    {
        Id = id;
        TenantId = RequireNonEmpty(tenantId, nameof(tenantId));
        Provider = NormalizeKnown(provider, DirectoryConnectionProviders.IsKnown, nameof(provider));
        ExternalTenantId = NormalizeRequired(externalTenantId, nameof(externalTenantId));
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        PrimaryDomain = NormalizeRequired(primaryDomain, nameof(primaryDomain)).ToLowerInvariant();
        GrantedScopesJson = DirectoryImportJson.RequireObject(grantedScopesJson, nameof(grantedScopesJson));
        Status = NormalizeKnown(status, DirectoryConnectionStatuses.IsKnown, nameof(status));
        LastSuccessfulSyncAt = lastSuccessfulSyncAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Provider { get; private set; } = DirectoryConnectionProviders.MicrosoftGraph;

    public string ExternalTenantId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PrimaryDomain { get; private set; } = string.Empty;

    public string GrantedScopesJson { get; private set; } = "{}";

    public string Status { get; private set; } = DirectoryConnectionStatuses.Active;

    public DateTimeOffset? LastSuccessfulSyncAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkSuccessfulSync(DateTimeOffset syncedAt)
    {
        LastSuccessfulSyncAt = syncedAt;
        Status = DirectoryConnectionStatuses.Active;
        UpdatedAt = syncedAt;
    }

    public void MarkFailed(DateTimeOffset failedAt)
    {
        Status = DirectoryConnectionStatuses.Failed;
        UpdatedAt = failedAt;
    }

    public void MarkRevoked(DateTimeOffset revokedAt)
    {
        Status = DirectoryConnectionStatuses.Revoked;
        UpdatedAt = revokedAt;
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
