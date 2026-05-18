namespace Platform.Domain.Templates;

public sealed class TemplateSection
{
    private TemplateSection()
    {
    }

    public TemplateSection(
        Guid id,
        Guid templateVersionId,
        int ordinal,
        string code,
        string titleDefault,
        Guid? parentSectionId = null)
    {
        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Section ordinal must be positive.");
        }

        Id = id;
        TemplateVersionId = templateVersionId;
        ParentSectionId = parentSectionId;
        Ordinal = ordinal;
        Code = NormalizeOptionalCode(code);
        TitleDefault = NormalizeRequired(titleDefault, nameof(titleDefault));
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public Guid? ParentSectionId { get; private set; }

    public int Ordinal { get; private set; }

    public string? Code { get; private set; }

    public string TitleDefault { get; private set; } = string.Empty;

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
