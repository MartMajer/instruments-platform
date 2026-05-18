namespace Platform.Domain.Scoring;

public sealed class ScoreRun
{
    private ScoreRun()
    {
    }

    public ScoreRun(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        Guid responseSessionId,
        Guid scoringRuleId,
        string status = ScoreRunStatuses.Success,
        DateTimeOffset? ranAt = null,
        string? errorMessage = null)
    {
        Id = id;
        TenantId = tenantId;
        CampaignId = campaignId;
        ResponseSessionId = responseSessionId;
        ScoringRuleId = scoringRuleId;
        Status = NormalizeStatus(status);
        RanAt = ranAt ?? DateTimeOffset.UtcNow;
        ErrorMessage = NormalizeOptional(errorMessage);
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid ResponseSessionId { get; private set; }

    public Guid ScoringRuleId { get; private set; }

    public DateTimeOffset RanAt { get; private set; }

    public string Status { get; private set; } = ScoreRunStatuses.Success;

    public string? ErrorMessage { get; private set; }

    private static string NormalizeStatus(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (!ScoreRunStatuses.IsKnown(normalized))
        {
            throw new ArgumentException("Score run status is unknown.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
