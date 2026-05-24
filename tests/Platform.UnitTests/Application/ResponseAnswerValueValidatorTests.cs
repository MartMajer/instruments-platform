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
                new ResponseAnswerValueContract(questionId, "\"legacy_bad\"", IsSkipped: false, IsNa: false)
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("answer.value_invalid", result.Error.Code);
    }
}
