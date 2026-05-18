using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class ScoreEntitiesTests
{
    [Fact]
    public void Score_run_normalizes_status_and_timestamps()
    {
        var ranAt = DateTimeOffset.Parse("2026-05-07T10:00:00+00:00");
        var run = new ScoreRun(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Success ",
            ranAt);

        Assert.Equal(ScoreRunStatuses.Success, run.Status);
        Assert.Equal(ranAt, run.RanAt);
        Assert.Null(run.ErrorMessage);
    }

    [Fact]
    public void Score_requires_non_negative_n_valid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Score(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "total",
            4.25m,
            nValid: -1));
    }

    [Fact]
    public void Score_requires_non_negative_n_expected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Score(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "total",
            4.25m,
            nValid: 1,
            nExpected: -1));
    }

    [Fact]
    public void Score_normalizes_dimension_value_and_output_metadata()
    {
        var score = new Score(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Total ",
            4.25777m,
            nValid: 2,
            nExpected: 3,
            missingPolicyStatus: " OK ",
            computedAt: DateTimeOffset.Parse("2026-05-07T10:01:00+00:00"));

        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4.2578m, score.Value);
        Assert.Equal(2, score.NValid);
        Assert.Equal(3, score.NExpected);
        Assert.Equal("ok", score.MissingPolicyStatus);
    }
}
