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
                Assert.Equal("comment", fourth.QuestionCode);
                Assert.Contains("simulated", fourth.Value, StringComparison.OrdinalIgnoreCase);
            });
    }
}
