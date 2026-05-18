namespace Platform.Domain.Instruments;

public sealed class InstrumentNorm
{
    private InstrumentNorm()
    {
    }

    public InstrumentNorm(
        Guid id,
        Guid instrumentId,
        string subscaleCode,
        string normType,
        string population,
        int sampleSize,
        string locale,
        decimal? mean = null,
        decimal? sd = null,
        string percentiles = "{}",
        string? sourceCitation = null,
        int? sourceYear = null)
    {
        if (!InstrumentNormTypes.IsKnown(normType))
        {
            throw new ArgumentException("Unknown instrument norm type.", nameof(normType));
        }

        if (sampleSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleSize), "Norm sample size must be positive.");
        }

        Id = id;
        InstrumentId = instrumentId;
        SubscaleCode = NormalizeRequired(subscaleCode, nameof(subscaleCode));
        NormType = normType;
        Population = NormalizeRequired(population, nameof(population));
        SampleSize = sampleSize;
        Locale = NormalizeRequired(locale, nameof(locale));
        Mean = mean;
        StandardDeviation = sd;
        Percentiles = InstrumentJson.RequireObject(percentiles, nameof(percentiles));
        SourceCitation = NormalizeOptional(sourceCitation);
        SourceYear = sourceYear;
    }

    public Guid Id { get; private set; }

    public Guid InstrumentId { get; private set; }

    public string SubscaleCode { get; private set; } = string.Empty;

    public string NormType { get; private set; } = string.Empty;

    public string Population { get; private set; } = string.Empty;

    public int SampleSize { get; private set; }

    public string Locale { get; private set; } = "en";

    public decimal? Mean { get; private set; }

    public decimal? StandardDeviation { get; private set; }

    public string Percentiles { get; private set; } = "{}";

    public string? SourceCitation { get; private set; }

    public int? SourceYear { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
