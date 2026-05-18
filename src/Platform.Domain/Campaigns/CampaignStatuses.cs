namespace Platform.Domain.Campaigns;

public static class CampaignStatuses
{
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Live = "live";
    public const string Closed = "closed";
    public const string Cancelled = "cancelled";

    public static bool IsKnown(string status)
    {
        return status is Draft or Scheduled or Live or Closed or Cancelled;
    }
}
