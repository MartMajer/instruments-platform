using Platform.Application.Features.Setup;
using Platform.Domain.Templates;

namespace Platform.UnitTests.Application;

public sealed class CreateTemplateVersionCommandTests
{
    [Fact]
    public void Validator_accepts_constrained_display_rule()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "has_barrier",
                QuestionTypes.SingleChoice,
                payload: ChoicePayload("o01", "o02")),
            Question(
                2,
                "barrier_detail",
                QuestionTypes.Text,
                payload:
                """{"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o01","requiredWhenVisible":true}}"""));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void Validator_rejects_display_rule_with_unknown_source_option()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "has_barrier",
                QuestionTypes.SingleChoice,
                payload: ChoicePayload("o01", "o02")),
            Question(
                2,
                "barrier_detail",
                QuestionTypes.Text,
                payload:
                """{"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o99","requiredWhenVisible":true}}"""));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "Request.Questions" &&
                failure.ErrorMessage.Contains("display rule needs one source answer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_display_rule_with_later_source_question()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "barrier_detail",
                QuestionTypes.Text,
                payload:
                """{"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o01","requiredWhenVisible":true}}"""),
            Question(
                2,
                "has_barrier",
                QuestionTypes.SingleChoice,
                payload: ChoicePayload("o01", "o02")));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "Request.Questions" &&
                failure.ErrorMessage.Contains("display rule needs an earlier source question", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_matrix_question_without_enough_column_options()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "body_discomfort",
                QuestionTypes.Matrix,
                payload:
                """{"matrix":{"mode":"single","rows":[{"code":"r01","label":"Neck"}],"columns":[{"code":"c01","label":"None"}]}}"""));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "Request.Questions" &&
                failure.ErrorMessage.Contains("matrix needs at least two column options", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_choice_question_without_enough_options()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "department",
                QuestionTypes.SingleChoice,
                payload: ChoicePayload("o01")));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "Request.Questions" &&
                failure.ErrorMessage.Contains("needs at least two answer options", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_ranking_top_n_above_option_count()
    {
        var validator = new CreateTemplateVersionValidator();
        var request = Request(
            Question(
                1,
                "priorities",
                QuestionTypes.Ranking,
                payload:
                """{"options":[{"code":"o01","label":"First"},{"code":"o02","label":"Second"}],"ranking":{"mode":"top_n","topN":3}}"""));

        var result = validator.Validate(new CreateTemplateVersionCommand(request));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "Request.Questions" &&
                failure.ErrorMessage.Contains("top-N ranking must be between 1", StringComparison.OrdinalIgnoreCase));
    }

    private static CreateTemplateVersionRequest Request(params CreateTemplateQuestionRequest[] questions)
    {
        return new CreateTemplateVersionRequest(
            "Private pulse",
            "1.0.0",
            "en",
            InstrumentId: null,
            Sections:
            [
                new CreateTemplateSectionRequest(1, "core", "Core")
            ],
            Scales: [],
            Questions: questions);
    }

    private static CreateTemplateQuestionRequest Question(
        int ordinal,
        string code,
        string type,
        string payload = "{}")
    {
        return new CreateTemplateQuestionRequest(
            ordinal,
            code,
            type,
            $"Question {ordinal}",
            SectionCode: "core",
            Payload: payload);
    }

    private static string ChoicePayload(params string[] optionCodes)
    {
        var options = string.Join(
            ",",
            optionCodes.Select(code => $$"""{"code":"{{code}}","label":"{{code}}"}"""));
        return $$"""{"options":[{{options}}]}""";
    }
}
