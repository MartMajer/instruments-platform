namespace Platform.Domain.Subjects;

public sealed class Subject
{
    private Subject()
    {
    }

    public Subject(
        Guid id,
        Guid tenantId,
        Guid? workspaceId = null,
        string? externalId = null,
        Guid? userAccountId = null,
        string? email = null,
        string? displayName = null,
        string locale = "en",
        string attributes = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        Id = id;
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        ExternalId = NormalizeOptional(externalId);
        UserAccountId = userAccountId;
        Email = NormalizeOptional(email);
        DisplayName = NormalizeOptional(displayName);
        Locale = locale;
        Attributes = SubjectJson.RequireObject(attributes, nameof(attributes));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? WorkspaceId { get; private set; }

    public string? ExternalId { get; private set; }

    public Guid? UserAccountId { get; private set; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public string Locale { get; private set; } = "en";

    public string Attributes { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void ReplaceAttributes(string attributes)
    {
        Attributes = SubjectJson.RequireObject(attributes, nameof(attributes));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeProfile(string? displayName, string? email, string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        DisplayName = NormalizeOptional(displayName);
        Email = NormalizeOptional(email);
        Locale = locale;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeDirectoryProfile(
        string? displayName,
        string? email,
        string? externalId,
        string locale,
        string attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        DisplayName = NormalizeOptional(displayName);
        Email = NormalizeOptional(email);
        ExternalId = NormalizeOptional(externalId);
        Locale = locale;
        Attributes = SubjectJson.RequireObject(attributes, nameof(attributes));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
