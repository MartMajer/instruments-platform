namespace Platform.Domain.Campaigns;

public static class InvitationTokenChannels
{
    public const string Email = "email";
    public const string Sms = "sms";
    public const string OpenLink = "open_link";
    public const string IdentifiedEntry = "identified_entry";
    public const string IdentifiedQueue = "identified_queue";

    public static bool IsKnown(string channel)
    {
        return channel is Email or Sms or OpenLink or IdentifiedEntry or IdentifiedQueue;
    }
}
