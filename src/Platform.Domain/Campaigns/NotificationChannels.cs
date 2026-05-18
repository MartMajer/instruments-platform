namespace Platform.Domain.Campaigns;

public static class NotificationChannels
{
    public const string Email = "email";
    public const string Sms = "sms";

    public static bool IsKnown(string channel)
    {
        return channel is Email or Sms;
    }
}
