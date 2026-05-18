namespace Platform.Domain.Templates;

public sealed class TemplateQuestion
{
    private TemplateQuestion()
    {
    }

    public TemplateQuestion(
        Guid id,
        Guid templateVersionId,
        Guid sectionId,
        int ordinal,
        string code,
        string type,
        Guid? scaleId,
        string textDefault,
        string? descriptionDefault = null,
        bool required = false,
        bool reverseCoded = false,
        decimal weight = 1.0m,
        string? variableLabel = null,
        string? measurementLevel = null,
        string payload = "{}",
        string missingCodes = "[]")
    {
        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Question ordinal must be positive.");
        }

        if (!QuestionTypes.IsKnown(type))
        {
            throw new ArgumentException("Unknown question type.", nameof(type));
        }

        if (QuestionTypes.RequiresScale(type) && !scaleId.HasValue)
        {
            throw new ArgumentException("Scale-backed questions require a scale.", nameof(scaleId));
        }

        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Question weight must be positive.");
        }

        if (measurementLevel is not null && !MeasurementLevels.IsKnown(measurementLevel))
        {
            throw new ArgumentException("Unknown measurement level.", nameof(measurementLevel));
        }

        Id = id;
        TemplateVersionId = templateVersionId;
        SectionId = sectionId;
        Ordinal = ordinal;
        Code = NormalizeCode(code);
        Type = type;
        ScaleId = scaleId;
        Payload = TemplateJson.RequireObject(payload, nameof(payload));
        TextDefault = NormalizeRequired(textDefault, nameof(textDefault));
        DescriptionDefault = NormalizeOptional(descriptionDefault);
        Required = required;
        ReverseCoded = reverseCoded;
        Weight = weight;
        VariableLabel = NormalizeOptional(variableLabel);
        MeasurementLevel = measurementLevel;
        MissingCodes = TemplateJson.RequireArray(missingCodes, nameof(missingCodes));
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public Guid SectionId { get; private set; }

    public int Ordinal { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public Guid? ScaleId { get; private set; }

    public string Payload { get; private set; } = "{}";

    public string TextDefault { get; private set; } = string.Empty;

    public string? DescriptionDefault { get; private set; }

    public bool Required { get; private set; }

    public bool ReverseCoded { get; private set; }

    public decimal Weight { get; private set; } = 1.0m;

    public string? VariableLabel { get; private set; }

    public string? MeasurementLevel { get; private set; }

    public string MissingCodes { get; private set; } = "[]";

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        return code.Trim().ToLowerInvariant();
    }

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
