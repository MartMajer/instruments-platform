using System.Text.Json;

namespace Platform.Domain.Consent;

public sealed class DisclosurePolicy
{
    public const int MinimumKMin = 5;

    public const string HideCell = "hide_cell";
    public const string AggregateUp = "aggregate_up";
    public const string RoundToN = "round_to_n";

    private static readonly HashSet<string> AllowedSuppressionStrategies =
    [
        HideCell,
        AggregateUp,
        RoundToN
    ];

    private DisclosurePolicy()
    {
    }

    public DisclosurePolicy(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        string version,
        int kMin,
        string suppressionStrategy,
        string appliesToDimensions,
        DateTimeOffset createdAt,
        DateTimeOffset? retiredAt = null)
    {
        if (kMin < MinimumKMin)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kMin),
                $"Disclosure k-min must be at least {MinimumKMin}.");
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
        KMin = kMin;
        SuppressionStrategy = NormalizeSuppressionStrategy(suppressionStrategy);
        AppliesToDimensions = NormalizeStringArray(appliesToDimensions, nameof(appliesToDimensions));
        CreatedAt = createdAt;
        RetiredAt = retiredAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public string Version { get; private set; } = string.Empty;

    public int KMin { get; private set; } = MinimumKMin;

    public string SuppressionStrategy { get; private set; } = HideCell;

    public string AppliesToDimensions { get; private set; } = "[]";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public bool IsUsableAt(DateTimeOffset at)
    {
        return CreatedAt <= at && (!RetiredAt.HasValue || RetiredAt.Value > at);
    }

    private static string NormalizeSuppressionStrategy(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (!AllowedSuppressionStrategies.Contains(normalized))
        {
            throw new ArgumentException("Unknown disclosure suppression strategy.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeStringArray(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName);

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Value must be a JSON array.", parameterName);
            }

            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(entry.GetString()))
                {
                    throw new ArgumentException(
                        "Array entries must be non-empty strings.",
                        parameterName);
                }
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
