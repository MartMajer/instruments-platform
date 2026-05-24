using Platform.Application.Features.Responses;
using Platform.Domain.Templates;

namespace Platform.UnitTests.Application;

public sealed class ResponseAnswerValueValidatorTests
{
    [Fact]
    public void Single_choice_answer_with_unknown_option_is_rejected()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "q01",
                    QuestionTypes.SingleChoice,
                    """{"options":[{"code":"o01","label":"Yes"},{"code":"o02","label":"No"}]}""")
            ],
            [
                new SaveAnswerRequest(questionId, "\"o03\"")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("q01", result.Error.Message);
    }

    [Fact]
    public void Matrix_answer_with_unknown_column_is_rejected()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "body_discomfort",
                    QuestionTypes.Matrix,
                    """{"matrix":{"mode":"single","rows":[{"code":"r01","label":"Neck"}],"columns":[{"code":"c01","label":"None"},{"code":"c02","label":"Severe"}]}}""")
            ],
            [
                new SaveAnswerRequest(questionId, """{"r01":"c03"}""")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("body_discomfort", result.Error.Message);
    }

    [Fact]
    public void Scale_answer_outside_configured_bounds_is_rejected()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "workload",
                    QuestionTypes.Likert,
                    "{}",
                    ScaleMinValue: 1,
                    ScaleMaxValue: 5,
                    ScaleStep: 1)
            ],
            [
                new SaveAnswerRequest(questionId, "6")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("workload", result.Error.Message);
    }

    [Fact]
    public void Multi_choice_exclusive_option_cannot_be_combined_with_other_options()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "blockers",
                    QuestionTypes.MultiChoice,
                    """{"options":[{"code":"o01","label":"Workload"},{"code":"o02","label":"None","exclusive":true}]}""")
            ],
            [
                new SaveAnswerRequest(questionId, """["o01","o02"]""")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("exclusive", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Top_n_ranking_rejects_more_than_configured_choices()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "priorities",
                    QuestionTypes.Ranking,
                    """{"options":[{"code":"o01","label":"First"},{"code":"o02","label":"Second"},{"code":"o03","label":"Third"}],"ranking":{"mode":"top_n","topN":2}}""")
            ],
            [
                new SaveAnswerRequest(questionId, """["o01","o02","o03"]""")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("top 2", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Date_answer_requires_iso_calendar_date()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "start_date",
                    QuestionTypes.Date,
                    "{}")
            ],
            [
                new SaveAnswerRequest(questionId, "\"next Monday\"")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("YYYY-MM-DD", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Text_answer_rejects_blank_saved_string()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "context",
                    QuestionTypes.Text,
                    "{}")
            ],
            [
                new SaveAnswerRequest(questionId, "\"   \"")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("text", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Skipped_or_not_applicable_answer_cannot_carry_a_value()
    {
        var skippedId = Guid.NewGuid();
        var notApplicableId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    skippedId,
                    "optional_context",
                    QuestionTypes.Text,
                    "{}"),
                new ResponseAnswerQuestionContract(
                    notApplicableId,
                    "workload",
                    QuestionTypes.Likert,
                    "{}",
                    ScaleMinValue: 1,
                    ScaleMaxValue: 5,
                    ScaleStep: 1,
                    ScaleNaAllowed: true)
            ],
            [
                new SaveAnswerRequest(skippedId, "\"some text\"", IsSkipped: true),
                new SaveAnswerRequest(notApplicableId, "5", IsNa: true)
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("cannot carry a value", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Skipped_or_not_applicable_answer_cannot_carry_a_comment()
    {
        var skippedId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    skippedId,
                    "optional_context",
                    QuestionTypes.Text,
                    "{}")
            ],
            [
                new SaveAnswerRequest(skippedId, Value: null, Comment: "Skipped because this did not apply.", IsSkipped: true)
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
        Assert.Contains("comment", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Valid_supported_answer_values_are_accepted()
    {
        var singleId = Guid.NewGuid();
        var matrixId = Guid.NewGuid();
        var numberId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.Validate(
            [
                new ResponseAnswerQuestionContract(
                    singleId,
                    "q01",
                    QuestionTypes.SingleChoice,
                    """{"options":[{"code":"o01","label":"Yes"},{"code":"o02","label":"No"}]}"""),
                new ResponseAnswerQuestionContract(
                    matrixId,
                    "q02",
                    QuestionTypes.Matrix,
                    """{"matrix":{"mode":"single","rows":[{"code":"r01","label":"Neck"}],"columns":[{"code":"c01","label":"None"},{"code":"c02","label":"Severe"}]}}"""),
                new ResponseAnswerQuestionContract(
                    numberId,
                    "q03",
                    QuestionTypes.Number,
                    """{"validation":{"min":0,"max":10,"integerOnly":true}}""")
            ],
            [
                new SaveAnswerRequest(singleId, "\"o01\""),
                new SaveAnswerRequest(matrixId, """{"r01":"c02"}"""),
                new SaveAnswerRequest(numberId, "7")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
    }

    [Fact]
    public void Saved_answer_contract_rejects_stale_invalid_values_before_submit()
    {
        var questionId = Guid.NewGuid();

        var result = ResponseAnswerValueValidator.ValidateSaved(
            [
                new ResponseAnswerQuestionContract(
                    questionId,
                    "q01",
                    QuestionTypes.SingleChoice,
                    """{"options":[{"code":"o01","label":"Yes"},{"code":"o02","label":"No"}]}""")
            ],
            [
                new ResponseAnswerValueContract(questionId, "\"legacy_bad\"", Comment: null, IsSkipped: false, IsNa: false)
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
    }
}
