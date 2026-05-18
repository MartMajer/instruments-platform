namespace Platform.Domain.Campaigns;

public sealed class Audience
{
    private Audience()
    {
    }

    public Audience(Guid id, Guid campaignId, string selector = "{}")
    {
        Id = id;
        CampaignId = campaignId;
        Selector = CampaignJson.RequireObject(selector, nameof(selector));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CampaignId { get; private set; }

    public string Selector { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }
}
