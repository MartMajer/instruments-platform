using Platform.Application.Features.ProductSurfaces;

namespace Platform.UnitTests.Application;

public sealed class ScoreCoverageSummaryTests
{
    [Fact]
    public void Complete_when_every_submitted_response_has_successful_scoring_activity()
    {
        var latestActivity = DateTimeOffset.Parse("2026-05-13T10:15:00+00:00");

        var coverage = ScoreCoverageSummary.Create(
        [
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: Guid.NewGuid(),
                SubmittedResponseCount: 3,
                ScoredSubmittedResponseCount: 3,
                LatestScoringActivityAt: latestActivity)
        ]);

        Assert.Equal(3, coverage.SubmittedResponseCount);
        Assert.Equal(3, coverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, coverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, coverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(1, coverage.CampaignsWithScoringRuleCount);
        Assert.Equal(0, coverage.CampaignsWithoutScoringRuleCount);
        Assert.Equal(latestActivity, coverage.LatestScoringActivityAt);
        Assert.Equal("complete", coverage.Status);
        Assert.Equal("All submitted responses have successful scoring activity.", coverage.Guidance);
    }

    [Fact]
    public void Partial_when_scoring_rule_exists_but_submitted_response_has_no_successful_run()
    {
        var coverage = ScoreCoverageSummary.Create(
        [
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: Guid.NewGuid(),
                SubmittedResponseCount: 4,
                ScoredSubmittedResponseCount: 3,
                LatestScoringActivityAt: DateTimeOffset.Parse("2026-05-13T10:15:00+00:00"))
        ]);

        Assert.Equal(4, coverage.SubmittedResponseCount);
        Assert.Equal(3, coverage.ScoredSubmittedResponseCount);
        Assert.Equal(1, coverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, coverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal("partial", coverage.Status);
        Assert.Equal(
            "Some submitted responses still need scoring activity before score-dependent reports are complete.",
            coverage.Guidance);
    }

    [Fact]
    public void Not_configured_when_submitted_responses_exist_without_scoring_rule()
    {
        var coverage = ScoreCoverageSummary.Create(
        [
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: null,
                SubmittedResponseCount: 2,
                ScoredSubmittedResponseCount: 0,
                LatestScoringActivityAt: null)
        ]);

        Assert.Equal(2, coverage.SubmittedResponseCount);
        Assert.Equal(0, coverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, coverage.UnscoredSubmittedResponseCount);
        Assert.Equal(2, coverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(0, coverage.CampaignsWithScoringRuleCount);
        Assert.Equal(1, coverage.CampaignsWithoutScoringRuleCount);
        Assert.Null(coverage.LatestScoringActivityAt);
        Assert.Equal("not_configured", coverage.Status);
        Assert.Equal(
            "Submitted responses exist, but scoring is not configured for those campaigns.",
            coverage.Guidance);
    }

    [Fact]
    public void No_submissions_when_campaigns_have_no_submitted_responses()
    {
        var coverage = ScoreCoverageSummary.Create(
        [
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: Guid.NewGuid(),
                SubmittedResponseCount: 0,
                ScoredSubmittedResponseCount: 0,
                LatestScoringActivityAt: null),
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: null,
                SubmittedResponseCount: 0,
                ScoredSubmittedResponseCount: 0,
                LatestScoringActivityAt: null)
        ]);

        Assert.Equal(0, coverage.SubmittedResponseCount);
        Assert.Equal(0, coverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, coverage.UnscoredSubmittedResponseCount);
        Assert.Equal(0, coverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(1, coverage.CampaignsWithScoringRuleCount);
        Assert.Equal(1, coverage.CampaignsWithoutScoringRuleCount);
        Assert.Null(coverage.LatestScoringActivityAt);
        Assert.Equal("no_submissions", coverage.Status);
        Assert.Equal("No submitted responses are available for score coverage yet.", coverage.Guidance);
    }

    [Fact]
    public void Mixed_not_configured_when_scored_and_not_configured_submissions_coexist()
    {
        var latestActivity = DateTimeOffset.Parse("2026-05-13T10:15:00+00:00");

        var coverage = ScoreCoverageSummary.Create(
        [
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: Guid.NewGuid(),
                SubmittedResponseCount: 3,
                ScoredSubmittedResponseCount: 3,
                LatestScoringActivityAt: latestActivity),
            new ScoreCoverageCampaignInput(
                CampaignId: Guid.NewGuid(),
                ScoringRuleId: null,
                SubmittedResponseCount: 2,
                ScoredSubmittedResponseCount: 0,
                LatestScoringActivityAt: null)
        ]);

        Assert.Equal(5, coverage.SubmittedResponseCount);
        Assert.Equal(3, coverage.ScoredSubmittedResponseCount);
        Assert.Equal(0, coverage.UnscoredSubmittedResponseCount);
        Assert.Equal(2, coverage.NotConfiguredSubmittedResponseCount);
        Assert.Equal(latestActivity, coverage.LatestScoringActivityAt);
        Assert.Equal("mixed_not_configured", coverage.Status);
        Assert.Equal(
            "Some submitted responses are scored while others are intentionally unscored because scoring is not configured.",
            coverage.Guidance);
    }
}
