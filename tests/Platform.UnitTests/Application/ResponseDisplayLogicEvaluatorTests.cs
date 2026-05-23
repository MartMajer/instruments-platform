using Platform.Application.Features.Responses;

namespace Platform.UnitTests.Application;

public sealed class ResponseDisplayLogicEvaluatorTests
{
    [Fact]
    public void Hidden_required_follow_up_is_not_required()
    {
        var sourceId = Guid.NewGuid();
        var followUpId = Guid.NewGuid();

        var evaluation = ResponseDisplayLogicEvaluator.Evaluate(
            [
                Question(sourceId, 1, "has_barrier", required: true),
                Question(
                    followUpId,
                    2,
                    "barrier_detail",
                    required: true,
                    """{"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o01","requiredWhenVisible":true}}""")
            ],
            [
                new ResponseDisplayLogicAnswer(sourceId, "\"o02\"", IsSkipped: false, IsNa: false)
            ]);

        Assert.Contains(sourceId, evaluation.RequiredVisibleQuestionIds);
        Assert.DoesNotContain(followUpId, evaluation.RequiredVisibleQuestionIds);
        Assert.Contains(followUpId, evaluation.HiddenQuestionIds);
    }

    [Fact]
    public void Matching_source_answer_makes_follow_up_visible_and_required()
    {
        var sourceId = Guid.NewGuid();
        var followUpId = Guid.NewGuid();

        var evaluation = ResponseDisplayLogicEvaluator.Evaluate(
            [
                Question(sourceId, 1, "has_barrier", required: true),
                Question(
                    followUpId,
                    2,
                    "barrier_detail",
                    required: true,
                    """{"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o01","requiredWhenVisible":true}}""")
            ],
            [
                new ResponseDisplayLogicAnswer(sourceId, "\"o01\"", IsSkipped: false, IsNa: false)
            ]);

        Assert.Contains(followUpId, evaluation.VisibleQuestionIds);
        Assert.Contains(followUpId, evaluation.RequiredVisibleQuestionIds);
        Assert.DoesNotContain(followUpId, evaluation.HiddenQuestionIds);
    }

    [Fact]
    public void Malformed_rules_fail_visible_to_avoid_bypassing_required_questions()
    {
        var questionId = Guid.NewGuid();

        var evaluation = ResponseDisplayLogicEvaluator.Evaluate(
            [Question(questionId, 1, "q01", required: true, """{"displayLogic":{"mode":"show_when"}}""")],
            []);

        Assert.Contains(questionId, evaluation.VisibleQuestionIds);
        Assert.Contains(questionId, evaluation.RequiredVisibleQuestionIds);
    }

    private static ResponseDisplayLogicQuestion Question(
        Guid id,
        int ordinal,
        string code,
        bool required,
        string payload = "{}")
    {
        return new ResponseDisplayLogicQuestion(id, ordinal, code, required, payload);
    }
}
