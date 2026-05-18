namespace Platform.Domain.Campaigns;

public static class NotificationStatuses
{
    public const string Queued = "queued";
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string Bounced = "bounced";

    public static bool IsKnown(string status)
    {
        return status is Queued or Sent or Failed or Bounced;
    }
}
