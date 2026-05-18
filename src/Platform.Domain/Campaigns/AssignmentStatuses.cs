namespace Platform.Domain.Campaigns;

public static class AssignmentStatuses
{
    public const string Pending = "pending";
    public const string Started = "started";
    public const string Submitted = "submitted";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static bool IsKnown(string status)
    {
        return status is Pending or Started or Submitted or Cancelled or Expired;
    }
}
