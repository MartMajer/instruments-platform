namespace Platform.Domain.Instruments;

public sealed class InstrumentSubscale
{
    private InstrumentSubscale()
    {
    }

    public InstrumentSubscale(
        Guid id,
        Guid instrumentId,
        string code,
        string name,
        int itemCount,
        string scoringMethod,
        decimal? reliabilityAlphaPublished = null)
    {
        if (!InstrumentScoringMethods.IsKnown(scoringMethod))
        {
            throw new ArgumentException("Unknown instrument scoring method.", nameof(scoringMethod));
        }

        if (itemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "Subscale item count must be positive.");
        }

        if (reliabilityAlphaPublished is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reliabilityAlphaPublished),
                "Reliability alpha must be between 0 and 1.");
        }

        Id = id;
        InstrumentId = instrumentId;
        Code = NormalizeRequired(code, nameof(code));
        Name = NormalizeRequired(name, nameof(name));
        ItemCount = itemCount;
        ScoringMethod = scoringMethod;
        ReliabilityAlphaPublished = reliabilityAlphaPublished;
    }

    public Guid Id { get; private set; }

    public Guid InstrumentId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int ItemCount { get; private set; }

    public decimal? ReliabilityAlphaPublished { get; private set; }

    public string ScoringMethod { get; private set; } = string.Empty;

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
