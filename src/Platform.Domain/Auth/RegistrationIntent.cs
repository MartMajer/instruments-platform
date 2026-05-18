namespace Platform.Domain.Auth;

public static class RegistrationIntentStatuses
{
    public const string Pending = "pending";

    public const string Consumed = "consumed";
}

public sealed class RegistrationIntent
{
    public const int TokenHashMaxLength = 128;

    public const int EmailMaxLength = 320;

    public const int OrganizationNameMaxLength = 256;

    public const int SlugMaxLength = 128;

    private RegistrationIntent()
    {
    }

    public RegistrationIntent(
        Guid id,
        string registrationTokenHash,
        string email,
        string organizationName,
        string slug,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(registrationTokenHash))
        {
            throw new ArgumentException("Registration token hash is required.", nameof(registrationTokenHash));
        }

        if (registrationTokenHash.Length > TokenHashMaxLength)
        {
            throw new ArgumentException("Registration token hash is too long.", nameof(registrationTokenHash));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Registration email is required.", nameof(email));
        }

        if (email.Length > EmailMaxLength)
        {
            throw new ArgumentException("Registration email is too long.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(organizationName))
        {
            throw new ArgumentException("Organization name is required.", nameof(organizationName));
        }

        if (organizationName.Length > OrganizationNameMaxLength)
        {
            throw new ArgumentException("Organization name is too long.", nameof(organizationName));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Registration slug is required.", nameof(slug));
        }

        if (slug.Length > SlugMaxLength)
        {
            throw new ArgumentException("Registration slug is too long.", nameof(slug));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Registration intent expiry must be after creation.", nameof(expiresAt));
        }

        Id = id;
        RegistrationTokenHash = registrationTokenHash;
        Email = email;
        OrganizationName = organizationName;
        Slug = slug;
        Status = RegistrationIntentStatuses.Pending;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public string RegistrationTokenHash { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string OrganizationName { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Status { get; private set; } = RegistrationIntentStatuses.Pending;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public Guid? ConsumedTenantId { get; private set; }

    public bool IsPending(DateTimeOffset now)
    {
        return Status == RegistrationIntentStatuses.Pending &&
            ConsumedAt is null &&
            ConsumedTenantId is null &&
            now < ExpiresAt;
    }

    public void Consume(Guid tenantId, DateTimeOffset consumedAt)
    {
        if (!IsPending(consumedAt))
        {
            throw new InvalidOperationException("Registration intent is not pending.");
        }

        Status = RegistrationIntentStatuses.Consumed;
        ConsumedAt = consumedAt;
        ConsumedTenantId = tenantId;
    }
}