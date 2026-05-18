namespace Platform.Application.Features.ProductSurfaces;

public sealed record ScoreCoverageCampaignInput(
    Guid CampaignId,
    Guid? ScoringRuleId,
    int SubmittedResponseCount,
    int ScoredSubmittedResponseCount,
    DateTimeOffset? LatestScoringActivityAt);

public static class ScoreCoverageSummary
{
    public static CampaignSeriesScoreCoverageResponse Create(
        IReadOnlyCollection<ScoreCoverageCampaignInput> campaigns)
    {
        ArgumentNullException.ThrowIfNull(campaigns);

        var submitted = 0;
        var scored = 0;
        var unscored = 0;
        var notConfigured = 0;
        DateTimeOffset? latestScoringActivityAt = null;

        foreach (var campaign in campaigns)
        {
            var submittedCount = Math.Max(0, campaign.SubmittedResponseCount);
            var scoredCount = Math.Min(
                submittedCount,
                Math.Max(0, campaign.ScoredSubmittedResponseCount));

            submitted += submittedCount;
            scored += scoredCount;

            if (campaign.ScoringRuleId.HasValue)
            {
                unscored += Math.Max(0, submittedCount - scoredCount);
            }
            else
            {
                notConfigured += submittedCount;
            }

            if (campaign.LatestScoringActivityAt.HasValue &&
                (!latestScoringActivityAt.HasValue ||
                    campaign.LatestScoringActivityAt.Value > latestScoringActivityAt.Value))
            {
                latestScoringActivityAt = campaign.LatestScoringActivityAt;
            }
        }

        var campaignsWithScoringRule = campaigns.Count(campaign => campaign.ScoringRuleId.HasValue);
        var campaignsWithoutScoringRule = campaigns.Count - campaignsWithScoringRule;
        var status = DetermineStatus(submitted, unscored, notConfigured, scored);

        return new CampaignSeriesScoreCoverageResponse(
            submitted,
            scored,
            unscored,
            notConfigured,
            campaignsWithScoringRule,
            campaignsWithoutScoringRule,
            latestScoringActivityAt,
            status,
            CreateGuidance(status));
    }

    private static string DetermineStatus(
        int submitted,
        int unscored,
        int notConfigured,
        int scored)
    {
        if (submitted == 0)
        {
            return "no_submissions";
        }

        if (unscored > 0)
        {
            return "partial";
        }

        if (notConfigured == submitted)
        {
            return "not_configured";
        }

        if (notConfigured > 0 && scored > 0)
        {
            return "mixed_not_configured";
        }

        return "complete";
    }

    private static string CreateGuidance(string status)
    {
        return status switch
        {
            "complete" => "All submitted responses have successful scoring activity.",
            "partial" => "Some submitted responses still need scoring activity before score-dependent reports are complete.",
            "not_configured" => "Submitted responses exist, but scoring is not configured for those campaigns.",
            "mixed_not_configured" => "Some submitted responses are scored while others are intentionally unscored because scoring is not configured.",
            _ => "No submitted responses are available for score coverage yet."
        };
    }
}
