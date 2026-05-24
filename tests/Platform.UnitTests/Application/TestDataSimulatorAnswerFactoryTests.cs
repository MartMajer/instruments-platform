using Platform.Application.Features.TestData;

namespace Platform.UnitTests.Application;

public sealed class TestDataSimulatorAnswerFactoryTests
{
    [Fact]
    public void CreatesTargetedAnswersForSupportedQuestionFormats()
    {
        var questions = new[]
        {
            new TestDataSimulatorQuestion(
                Guid.Parse("018f9d3d-7415-7000-9000-000000000001"),
                "strain",
                "likert",
                Required: true,
                ScaleMinValue: 1,
                ScaleMaxValue: 5,
                Payload: "{}"),
            new TestDataSimulatorQuestion(
                Guid.Parse("018f9d3d-7415-7000-9000-000000000002"),
                "pain",
                "nps",
                Required: true,
                ScaleMinValue: 0,
                ScaleMaxValue: 10,
                Payload: "{}"),
            new TestDataSimulatorQuestion(
                Guid.Parse("018f9d3d-7415-7000-9000-000000000003"),
                "shift",
                "single",
                Required: true,
                ScaleMinValue: null,
                ScaleMaxValue: null,
                Payload: """
                    {"options":[{"code":"morning","label":"Morning"},{"code":"afternoon","label":"Afternoon"},{"code":"night","label":"Night"}]}
                    """),
            new TestDataSimulatorQuestion(
                Guid.Parse("018f9d3d-7415-7000-9000-000000000004"),
                "body_discomfort",
                "matrix",
                Required: true,
                ScaleMinValue: null,
                ScaleMaxValue: null,
                Payload: """
                    {"matrix":{"mode":"single","rows":[{"code":"r01","label":"Neck / shoulders"},{"code":"r02","label":"Lower back"}],"columns":[{"code":"c01","label":"None"},{"code":"c02","label":"Mild"},{"code":"c03","label":"Severe"}]}}
                    """),
            new TestDataSimulatorQuestion(
                Guid.Parse("018f9d3d-7415-7000-9000-000000000005"),
                "comment",
                "text",
                Required: false,
                ScaleMinValue: null,
                ScaleMaxValue: null,
                Payload: "{}")
        };

        var answers = TestDataSimulatorAnswerFactory.CreateAnswers(
            questions,
            new CreateCampaignTestResponsesRequest(
                ResponseCount: 12,
                TargetOutcome: 7,
                Variation: "tight",
                IncludeComments: true),
            respondentIndex: 2);

        Assert.Collection(
            answers,
            first =>
            {
                Assert.Equal("strain", first.QuestionCode);
                Assert.Equal("4", first.Value);
            },
            second =>
            {
                Assert.Equal("pain", second.QuestionCode);
                Assert.Equal("7", second.Value);
            },
            third =>
            {
                Assert.Equal("shift", third.QuestionCode);
                Assert.Equal("\"night\"", third.Value);
            },
            fourth =>
            {
                Assert.Equal("body_discomfort", fourth.QuestionCode);
                Assert.Equal("""{"r01":"c03","r02":"c03"}""", fourth.Value);
            },
            fifth =>
            {
                Assert.Equal("comment", fifth.QuestionCode);
                Assert.Contains("simulated", fifth.Value, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void Hidden_display_logic_follow_up_is_saved_as_skipped()
    {
        var sourceId = Guid.Parse("018f9d3d-7415-7000-9000-000000000101");
        var followUpId = Guid.Parse("018f9d3d-7415-7000-9000-000000000102");
        var questions = new[]
        {
            new TestDataSimulatorQuestion(
                sourceId,
                "has_barrier",
                "single",
                Required: true,
                ScaleMinValue: null,
                ScaleMaxValue: null,
                Payload: """
                    {"options":[{"code":"o01","label":"No"},{"code":"o02","label":"Yes"}]}
                    """),
            new TestDataSimulatorQuestion(
                followUpId,
                "barrier_detail",
                "likert",
                Required: true,
                ScaleMinValue: 1,
                ScaleMaxValue: 5,
                Payload: """
                    {"displayLogic":{"mode":"show_when","sourceQuestionCode":"has_barrier","operator":"equals","value":"o02","requiredWhenVisible":true}}
                    """)
        };

        var answers = TestDataSimulatorAnswerFactory.CreateAnswers(
            questions,
            new CreateCampaignTestResponsesRequest(
                ResponseCount: 1,
                TargetOutcome: 0,
                Variation: "tight",
                IncludeComments: true),
            respondentIndex: 0);

        var followUpAnswer = Assert.Single(answers, answer => answer.QuestionId == followUpId);
        Assert.True(followUpAnswer.IsSkipped);
        Assert.Null(followUpAnswer.Value);
    }
}
