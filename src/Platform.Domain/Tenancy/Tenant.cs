namespace Platform.Domain.Tenancy;

public sealed class Tenant
{
    private Tenant()
    {
    }

    public Tenant(Guid id, string slug, string name, string region = "eu", string defaultLocale = "en")
    {
        Id = id;
        Slug = slug;
        Name = name;
        Region = region;
        DefaultLocale = defaultLocale;
        Status = "active";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Region { get; private set; } = "eu";

    public string DefaultLocale { get; private set; } = "en";

    public string Status { get; private set; } = "active";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void ChangeDefaultLocale(string defaultLocale, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLocale, nameof(defaultLocale));
        if (defaultLocale.Length > 16)
        {
            throw new ArgumentException("Default locale must be 16 characters or fewer.", nameof(defaultLocale));
        }

        DefaultLocale = defaultLocale.Trim();
        UpdatedAt = updatedAt;
    }
}
