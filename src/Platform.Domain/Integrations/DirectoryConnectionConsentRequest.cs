namespace Platform.Domain.Integrations;

public sealed class DirectoryConnectionConsentRequest
{
    public const int ProviderMaxLength = DirectoryConnection.ProviderMaxLength;
    public const int StatusMaxLength = 32;
    public const int StateHashMaxLength = 128;
    public const int NonceHashMaxLength = 128;
    public const int FailureCategoryMaxLength = 128;

    private DirectoryConnectionConsentRequest()
    {
    }

    public DirectoryConnectionConsentRequest(
        Guid id,
        Guid tenantId,
        string provider,
        string stateHash,
        string nonceHash,
        DateTimeOffset expiresAt,
        string requestedScopes = "[]",
        Guid? directoryConnectionId = null,
        Guid? createdByUserId = null,
        DateTimeOffset? observedAt = null)
    {
        if (!DirectoryConnectionProviders.IsKnown(provider))
        {
            throw new ArgumentException("Unknown directory connection provider.", nameof(provider));
        }

        Id = id;
        TenantId = tenantId;
        DirectoryConnectionId = directoryConnectionId;
        Provider = provider;
        StateHash = NormalizeRequired(stateHash, nameof(stateHash), StateHashMaxLength);
        NonceHash = NormalizeRequired(nonceHash, nameof(nonceHash), NonceHashMaxLength);
        RequestedScopes = DirectoryIntegrationJson.RequireArray(requestedScopes, nameof(requestedScopes));
        Status = DirectoryConnectionConsentRequestStatuses.Pending;
        ExpiresAt = expiresAt;
        CreatedByUserId = createdByUserId;
        CreatedAt = observedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? DirectoryConnectionId { get; private set; }

    public string Provider { get; private set; } = DirectoryConnectionProviders.MicrosoftGraph;

    public string StateHash { get; private set; } = string.Empty;

    public string NonceHash { get; private set; } = string.Empty;

    public string RequestedScopes { get; private set; } = "[]";

    public string Status { get; private set; } = DirectoryConnectionConsentRequestStatuses.Pending;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureCategory { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Complete(DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionConsentRequestStatuses.Completed;
        CompletedAt = observedAt;
        FailureCategory = null;
        UpdatedAt = observedAt;
    }

    public void Fail(string failureCategory, DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionConsentRequestStatuses.Failed;
        CompletedAt = observedAt;
        FailureCategory = NormalizeRequired(failureCategory, nameof(failureCategory), FailureCategoryMaxLength);
        UpdatedAt = observedAt;
    }

    public void Expire(DateTimeOffset observedAt)
    {
        Status = DirectoryConnectionConsentRequestStatuses.Expired;
        CompletedAt = observedAt;
        FailureCategory = "expired";
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
