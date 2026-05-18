using System.Text.Json;

namespace Platform.Domain.Consent;

public sealed class ConsentDocument
{
    private ConsentDocument()
    {
    }

    public ConsentDocument(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        string locale,
        string version,
        string title,
        string bodyMarkdown,
        string requiredGrants,
        string optionalGrants,
        DateTimeOffset publishedAt,
        DateTimeOffset? retiredAt = null)
    {
        if (retiredAt.HasValue && retiredAt.Value <= publishedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retiredAt),
                "Retirement time must be after publication time.");
        }

        Id = id;
        TenantId = tenantId;
        CampaignSeriesId = campaignSeriesId;
        Locale = NormalizeRequired(locale, nameof(locale));
        Version = NormalizeRequired(version, nameof(version));
        Title = NormalizeRequired(title, nameof(title));
        BodyMarkdown = NormalizeRequired(bodyMarkdown, nameof(bodyMarkdown));
        RequiredGrants = NormalizeGrantArray(requiredGrants, nameof(requiredGrants));
        OptionalGrants = NormalizeGrantArray(optionalGrants, nameof(optionalGrants));
        PublishedAt = publishedAt;
        RetiredAt = retiredAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public string Locale { get; private set; } = "en";

    public string Version { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string BodyMarkdown { get; private set; } = string.Empty;

    public string RequiredGrants { get; private set; } = "[]";

    public string OptionalGrants { get; private set; } = "[]";

    public DateTimeOffset PublishedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsUsableAt(DateTimeOffset at)
    {
        return PublishedAt <= at && (!RetiredAt.HasValue || RetiredAt.Value > at);
    }

    internal static string NormalizeGrantArray(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName);

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Consent grants must be a JSON array.", parameterName);
            }

            foreach (var grant in document.RootElement.EnumerateArray())
            {
                if (grant.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(grant.GetString()))
                {
                    throw new ArgumentException("Consent grant entries must be non-empty strings.", parameterName);
                }
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Consent grants must be valid JSON.", parameterName, exception);
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
