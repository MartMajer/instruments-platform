namespace Platform.Domain.Campaigns;

public sealed class RespondentRule
{
    private RespondentRule()
    {
    }

    public RespondentRule(Guid id, Guid campaignId, int ordinal, string rule)
    {
        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Respondent rule ordinal must be positive.");
        }

        Id = id;
        CampaignId = campaignId;
        Ordinal = ordinal;
        Rule = CampaignJson.RequireObject(rule, nameof(rule));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CampaignId { get; private set; }

    public int Ordinal { get; private set; }

    public string Rule { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }
}
