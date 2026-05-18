namespace Platform.Domain.Instruments;

public sealed class InstrumentItem
{
    private InstrumentItem()
    {
    }

    public InstrumentItem(
        Guid id,
        Guid instrumentId,
        int ordinal,
        string code,
        string subscaleCode,
        bool reverseCoded,
        Guid? questionId = null)
    {
        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Instrument item ordinal must be positive.");
        }

        Id = id;
        InstrumentId = instrumentId;
        Ordinal = ordinal;
        Code = NormalizeRequired(code, nameof(code));
        SubscaleCode = NormalizeRequired(subscaleCode, nameof(subscaleCode));
        ReverseCoded = reverseCoded;
        QuestionId = questionId;
    }

    public Guid Id { get; private set; }

    public Guid InstrumentId { get; private set; }

    public int Ordinal { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string SubscaleCode { get; private set; } = string.Empty;

    public bool ReverseCoded { get; private set; }

    public Guid? QuestionId { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
