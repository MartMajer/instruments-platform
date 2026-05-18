namespace Platform.Domain.Templates;

public sealed class QuestionScale
{
    private QuestionScale()
    {
    }

    public QuestionScale(
        Guid id,
        Guid templateVersionId,
        string code,
        string type,
        int minValue,
        int maxValue,
        int step,
        bool naAllowed,
        string anchors)
    {
        if (!ScaleTypes.IsKnown(type))
        {
            throw new ArgumentException("Unknown scale type.", nameof(type));
        }

        if (maxValue <= minValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "Scale max value must be greater than min value.");
        }

        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Scale step must be positive.");
        }

        Id = id;
        TemplateVersionId = templateVersionId;
        Code = NormalizeCode(code);
        Type = type;
        MinValue = minValue;
        MaxValue = maxValue;
        Step = step;
        NaAllowed = naAllowed;
        Anchors = TemplateJson.RequireArray(anchors, nameof(anchors));
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public int MinValue { get; private set; }

    public int MaxValue { get; private set; }

    public int Step { get; private set; }

    public bool NaAllowed { get; private set; }

    public string Anchors { get; private set; } = "[]";

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        return code.Trim().ToLowerInvariant();
    }
}
