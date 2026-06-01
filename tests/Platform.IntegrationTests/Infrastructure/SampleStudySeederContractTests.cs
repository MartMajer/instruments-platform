using System.Reflection;
using Platform.Domain.Campaigns;
using Platform.Domain.Scoring;
using Platform.Infrastructure.ProductSurfaces;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class SampleStudySeederContractTests
{
    [Fact]
    public void Target_aware_360_sample_study_is_declared_as_identified_assignment_sample()
    {
        var specsType = typeof(SampleStudySeeder).Assembly.GetType(
            "Platform.Infrastructure.ProductSurfaces.SampleStudySpecs");
        Assert.NotNull(specsType);
        var all = Assert.IsAssignableFrom<IEnumerable<object>>(
            specsType.GetProperty("All", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
        var feedback360Spec = all.Single(spec =>
            string.Equals(
                spec.GetType().GetProperty("Key")?.GetValue(spec)?.ToString(),
                "leadership-360-feedback",
                StringComparison.Ordinal));

        Assert.Equal(
            ResponseIdentityModes.Identified,
            feedback360Spec.GetType().GetProperty("ResponseIdentityMode")?.GetValue(feedback360Spec));
        Assert.Equal(
            "target_aware_360",
            feedback360Spec.GetType().GetProperty("AssignmentScenario")?.GetValue(feedback360Spec));
    }

    [Fact]
    public void Complex_sample_study_scoring_document_evaluates_advanced_outputs()
    {
        var specsType = typeof(SampleStudySeeder).Assembly.GetType(
            "Platform.Infrastructure.ProductSurfaces.SampleStudySpecs");
        Assert.NotNull(specsType);
        var all = Assert.IsAssignableFrom<IEnumerable<object>>(
            specsType.GetProperty("All", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
        var complexSpec = all.Single(spec =>
            string.Equals(
                spec.GetType().GetProperty("Key")?.GetValue(spec)?.ToString(),
                "complex-scoring-showcase",
                StringComparison.Ordinal));
        var builder = typeof(SampleStudySeeder).GetMethod(
            "BuildSampleScoringDocument",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(builder);

        var document = Assert.IsType<string>(builder.Invoke(null, [complexSpec]));
        var result = SimpleScoringEngine.Evaluate(
            document,
            [
                new SimpleScoreInput("q01", "5"),
                new SimpleScoreInput("q02", "4"),
                new SimpleScoreInput("q03", "6"),
                new SimpleScoreInput("q04", "5"),
                new SimpleScoreInput("q05", "4"),
                new SimpleScoreInput("q06", "4"),
                new SimpleScoreInput("q07", "5"),
                new SimpleScoreInput("q08", "4")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Collection(
            result.Value,
            focus =>
            {
                Assert.Equal("focus_stability", focus.DimensionCode);
                Assert.InRange(focus.Value, 60m, 70m);
            },
            recovery =>
            {
                Assert.Equal("recovery_capacity", recovery.DimensionCode);
                Assert.Equal(4.5m, recovery.Value);
            },
            support =>
            {
                Assert.Equal("support_resource_total", support.DimensionCode);
                Assert.Equal(18m, support.Value);
            },
            readiness =>
            {
                Assert.Equal("readiness_index", readiness.DimensionCode);
                Assert.InRange(readiness.Value, 60m, 65m);
            },
            gap =>
            {
                Assert.Equal("recovery_focus_gap", gap.DimensionCode);
                Assert.InRange(gap.Value, -10m, -5m);
            });
    }
}
