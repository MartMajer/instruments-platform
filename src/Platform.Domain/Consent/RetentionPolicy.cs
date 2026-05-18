using System.Text.Json;

namespace Platform.Domain.Consent;

public sealed class RetentionPolicy
{
    public const string ConsentAcceptedAt = "consent_accepted_at";
    public const string ResponseSubmittedAt = "response_submitted_at";
    public const string WaveClosedAt = "wave_closed_at";
    public const string SeriesClosedAt = "series_closed_at";
    public const string LastResponseSubmittedAt = "last_response_submitted_at";

    public const string Delete = "delete";
    public const string Anonymize = "anonymize";

    private static readonly HashSet<string> AllowedRetentionStartEvents =
    [
        ConsentAcceptedAt,
        ResponseSubmittedAt,
        WaveClosedAt,
        SeriesClosedAt,
        LastResponseSubmittedAt
    ];

    private static readonly HashSet<string> AllowedActions =
    [
        Delete,
        Anonymize
    ];

    private RetentionPolicy()
    {
    }

    public RetentionPolicy(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        string version,
        int retainForYears,
        string retentionStartEvent,
        string actionAfter,
        DateOnly nextReviewAt,
        string publicationLimits,
        DateTimeOffset createdAt,
        DateTimeOffset? retiredAt = null)
    {
        if (retainForYears <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retainForYears),
                "Retention period must be positive.");
        }

        if (retiredAt.HasValue && retiredAt.Value <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retiredAt),
                "Retirement time must be after creation time.");
        }

        Id = id;
        TenantId = tenantId;
        CampaignSeriesId = campaignSeriesId;
        Version = NormalizeRequired(version, nameof(version));
        RetainForYears = retainForYears;
        RetentionStartEvent = NormalizeAllowed(
            retentionStartEvent,
            AllowedRetentionStartEvents,
            nameof(retentionStartEvent),
            "Unknown retention start event.");
        ActionAfter = NormalizeAllowed(
            actionAfter,
            AllowedActions,
            nameof(actionAfter),
            "Unknown retention action.");
        NextReviewAt = nextReviewAt;
        PublicationLimits = NormalizeJsonObject(publicationLimits, nameof(publicationLimits));
        CreatedAt = createdAt;
        RetiredAt = retiredAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public string Version { get; private set; } = string.Empty;

    public int RetainForYears { get; private set; }

    public string RetentionStartEvent { get; private set; } = ResponseSubmittedAt;

    public string ActionAfter { get; private set; } = Anonymize;

    public DateOnly NextReviewAt { get; private set; }

    public string PublicationLimits { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public bool IsUsableAt(DateTimeOffset at)
    {
        return CreatedAt <= at && (!RetiredAt.HasValue || RetiredAt.Value > at);
    }

    internal static string NormalizeJsonObject(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName);

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Value must be a JSON object.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }

        return normalized;
    }

    private static string NormalizeAllowed(
        string value,
        IReadOnlySet<string> allowedValues,
        string parameterName,
        string message)
    {
        var normalized = NormalizeRequired(value, parameterName);
        if (!allowedValues.Contains(normalized))
        {
            throw new ArgumentException(message, parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
