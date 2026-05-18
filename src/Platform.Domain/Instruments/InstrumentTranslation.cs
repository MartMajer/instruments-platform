namespace Platform.Domain.Instruments;

public sealed class InstrumentTranslation
{
    private InstrumentTranslation()
    {
    }

    public InstrumentTranslation(
        Guid id,
        Guid? instrumentId,
        Guid? instrumentSubscaleId,
        Guid? instrumentItemId,
        string field,
        string locale,
        string text,
        string status,
        Guid? workflowId = null)
        : this(
            id,
            instrumentId,
            instrumentSubscaleId,
            instrumentItemId,
            null,
            null,
            null,
            null,
            field,
            locale,
            text,
            status,
            workflowId)
    {
    }

    public InstrumentTranslation(
        Guid id,
        Guid? instrumentId,
        Guid? instrumentSubscaleId,
        Guid? instrumentItemId,
        Guid? surveyTemplateId,
        Guid? templateSectionId,
        Guid? templateQuestionId,
        Guid? choiceOptionId,
        string field,
        string locale,
        string text,
        string status,
        Guid? workflowId = null)
    {
        ValidateTarget(
            instrumentId,
            instrumentSubscaleId,
            instrumentItemId,
            surveyTemplateId,
            templateSectionId,
            templateQuestionId,
            choiceOptionId);

        if (!TranslationStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown translation status.", nameof(status));
        }

        Id = id;
        InstrumentId = instrumentId;
        InstrumentSubscaleId = instrumentSubscaleId;
        InstrumentItemId = instrumentItemId;
        SurveyTemplateId = surveyTemplateId;
        TemplateSectionId = templateSectionId;
        TemplateQuestionId = templateQuestionId;
        ChoiceOptionId = choiceOptionId;
        Field = NormalizeRequired(field, nameof(field));
        Locale = NormalizeRequired(locale, nameof(locale));
        Text = NormalizeRequired(text, nameof(text));
        Status = status;
        WorkflowId = workflowId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid? InstrumentId { get; private set; }

    public Guid? InstrumentSubscaleId { get; private set; }

    public Guid? InstrumentItemId { get; private set; }

    public Guid? SurveyTemplateId { get; private set; }

    public Guid? TemplateSectionId { get; private set; }

    public Guid? TemplateQuestionId { get; private set; }

    public Guid? ChoiceOptionId { get; private set; }

    public string Field { get; private set; } = string.Empty;

    public string Locale { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public Guid? WorkflowId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static InstrumentTranslation ForInstrument(
        Guid id,
        Guid instrumentId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            instrumentId,
            null,
            null,
            null,
            null,
            null,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForSubscale(
        Guid id,
        Guid instrumentSubscaleId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            instrumentSubscaleId,
            null,
            null,
            null,
            null,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForItem(
        Guid id,
        Guid instrumentItemId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            null,
            instrumentItemId,
            null,
            null,
            null,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForTemplate(
        Guid id,
        Guid surveyTemplateId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            null,
            null,
            surveyTemplateId,
            null,
            null,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForSection(
        Guid id,
        Guid templateSectionId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            null,
            null,
            null,
            templateSectionId,
            null,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForQuestion(
        Guid id,
        Guid templateQuestionId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            null,
            null,
            null,
            null,
            templateQuestionId,
            null,
            field,
            locale,
            text,
            status);
    }

    public static InstrumentTranslation ForChoiceOption(
        Guid id,
        Guid choiceOptionId,
        string field,
        string locale,
        string text,
        string status)
    {
        return new InstrumentTranslation(
            id,
            null,
            null,
            null,
            null,
            null,
            null,
            choiceOptionId,
            field,
            locale,
            text,
            status);
    }

    private static void ValidateTarget(
        Guid? instrumentId,
        Guid? instrumentSubscaleId,
        Guid? instrumentItemId,
        Guid? surveyTemplateId,
        Guid? templateSectionId,
        Guid? templateQuestionId,
        Guid? choiceOptionId)
    {
        var targetCount =
            (instrumentId.HasValue ? 1 : 0) +
            (instrumentSubscaleId.HasValue ? 1 : 0) +
            (instrumentItemId.HasValue ? 1 : 0) +
            (surveyTemplateId.HasValue ? 1 : 0) +
            (templateSectionId.HasValue ? 1 : 0) +
            (templateQuestionId.HasValue ? 1 : 0) +
            (choiceOptionId.HasValue ? 1 : 0);

        if (targetCount != 1)
        {
            throw new ArgumentException("Translation must target exactly one entity.");
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
