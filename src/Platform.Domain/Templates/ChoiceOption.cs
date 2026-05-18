namespace Platform.Domain.Templates;

public sealed class ChoiceOption
{
    private ChoiceOption()
    {
    }

    public ChoiceOption(
        Guid id,
        Guid questionId,
        int ordinal,
        string value,
        string labelDefault,
        bool isOther = false,
        bool isExclusive = false)
    {
        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Choice option ordinal must be positive.");
        }

        Id = id;
        QuestionId = questionId;
        Ordinal = ordinal;
        Value = NormalizeRequired(value, nameof(value));
        LabelDefault = NormalizeRequired(labelDefault, nameof(labelDefault));
        IsOther = isOther;
        IsExclusive = isExclusive;
    }

    public Guid Id { get; private set; }

    public Guid QuestionId { get; private set; }

    public int Ordinal { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string LabelDefault { get; private set; } = string.Empty;

    public bool IsOther { get; private set; }

    public bool IsExclusive { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
