namespace Platform.Domain.Templates;

public sealed class TemplateVersion
{
    private TemplateVersion()
    {
    }

    private TemplateVersion(
        Guid id,
        Guid templateId,
        Guid? instrumentId,
        string semver,
        string status,
        bool isLocked,
        bool isGlobal,
        string defaultLocale,
        DateTimeOffset? publishedAt = null,
        Guid? publishedBy = null)
    {
        if (!TemplateVersionStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown template version status.", nameof(status));
        }

        if (isGlobal && !isLocked)
        {
            throw new ArgumentException("Global canonical template versions must be locked.", nameof(isLocked));
        }

        Id = id;
        TemplateId = templateId;
        InstrumentId = instrumentId;
        Semver = NormalizeRequired(semver, nameof(semver));
        Status = status;
        PublishedAt = publishedAt;
        PublishedBy = publishedBy;
        IsLocked = isLocked;
        IsGlobal = isGlobal;
        DefaultLocale = NormalizeRequired(defaultLocale, nameof(defaultLocale));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    public Guid? InstrumentId { get; private set; }

    public string Semver { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid? PublishedBy { get; private set; }

    public bool IsLocked { get; private set; }

    public bool IsGlobal { get; private set; }

    public string DefaultLocale { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static TemplateVersion CreateCanonicalDraft(
        Guid id,
        Guid templateId,
        string semver,
        string defaultLocale,
        Guid? instrumentId = null)
    {
        return new TemplateVersion(
            id,
            templateId,
            instrumentId,
            semver,
            TemplateVersionStatuses.Draft,
            isLocked: true,
            isGlobal: true,
            defaultLocale);
    }

    public static TemplateVersion CreateTenantDraft(
        Guid id,
        Guid templateId,
        string semver,
        string defaultLocale,
        Guid? instrumentId = null)
    {
        return new TemplateVersion(
            id,
            templateId,
            instrumentId,
            semver,
            TemplateVersionStatuses.Draft,
            isLocked: false,
            isGlobal: false,
            defaultLocale);
    }

    public void LinkInstrument(Guid instrumentId)
    {
        InstrumentId = instrumentId;
    }

    public void Publish(Guid? publishedBy, DateTimeOffset publishedAt)
    {
        if (Status != TemplateVersionStatuses.Draft)
        {
            throw new InvalidOperationException("Only draft template versions can be published.");
        }

        Status = TemplateVersionStatuses.Published;
        PublishedAt = publishedAt;
        PublishedBy = publishedBy;
        IsLocked = true;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
