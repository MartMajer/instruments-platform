namespace Platform.Domain.Integrations;

public sealed class DirectoryConnection
{
    public const int ProviderMaxLength = 64;
    public const int StatusMaxLength = 32;
    public const int ExternalTenantIdMaxLength = 128;
    public const int DisplayNameMaxLength = 256;
    public const int PrimaryDomainMaxLength = 256;

    private DirectoryConnection()
    {
    }

    public DirectoryConnection(
        Guid id,
        Guid tenantId,
        string provider,
        string? externalTenantId,
        string displayName,
        string? primaryDomain,
        string grantedScopes = "[]",
        string status = DirectoryConnectionStatuses.PendingConsent,
        Guid? createdByUserId = null,
        DateTimeOffset? observedAt = null)
    {
        if (!DirectoryConnectionProviders.IsKnown(provider))
        {
            throw new ArgumentException("Unknown directory connection provider.", nameof(provider));
        }

        if (!DirectoryConnectionStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown directory connection status.", nameof(status));
        }

        Id = id;
        TenantId = tenantId;
        Provider = provider;
        ExternalTenantId = NormalizeOptional(externalTenantId, nameof(externalTenantId), ExternalTenantIdMaxLength);
        DisplayName = NormalizeRequired(displayName, nameof(displayName), DisplayNameMaxLength);
        PrimaryDomain = NormalizeOptional(primaryDomain, nameof(primaryDomain), PrimaryDomainMaxLength);
        GrantedScopes = DirectoryIntegrationJson.RequireArray(grantedScopes, nameof(grantedScopes));
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAt = observedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Provider { get; private set; } = DirectoryConnectionProviders.MicrosoftGraph;

    public string? ExternalTenantId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? PrimaryDomain { get; private set; }

    public string GrantedScopes { get; private set; } = "[]";

    public string Status { get; private set; } = DirectoryConnectionStatuses.PendingConsent;

    public DateTimeOffset? LastConsentAt { get; private set; }

    public DateTimeOffset? LastSuccessfulImportAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkPendingConsent(DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionStatuses.PendingConsent;
        UpdatedAt = observedAt;
    }

    public void Activate(
        string externalTenantId,
        string displayName,
        string? primaryDomain,
        string grantedScopes,
        DateTimeOffset observedAt)
    {
        ExternalTenantId = NormalizeRequired(externalTenantId, nameof(externalTenantId), ExternalTenantIdMaxLength);
        DisplayName = NormalizeRequired(displayName, nameof(displayName), DisplayNameMaxLength);
        PrimaryDomain = NormalizeOptional(primaryDomain, nameof(primaryDomain), PrimaryDomainMaxLength);
        GrantedScopes = DirectoryIntegrationJson.RequireArray(grantedScopes, nameof(grantedScopes));
        Status = DirectoryConnectionStatuses.Active;
        LastConsentAt = observedAt;
        UpdatedAt = observedAt;
    }

    public void MarkConsentRequired(DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionStatuses.ConsentRequired;
        UpdatedAt = observedAt;
    }

    public void MarkFailed(DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionStatuses.Failed;
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

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);
        }

        return trimmed;
    }
}
