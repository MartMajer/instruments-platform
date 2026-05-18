namespace Platform.Domain.Campaigns;

public static class ResponseIdentityModes
{
    public const string Identified = "identified";
    public const string Anonymous = "anonymous";
    public const string AnonymousLongitudinal = "anonymous_longitudinal";

    public static bool IsKnown(string mode)
    {
        return mode is Identified or Anonymous or AnonymousLongitudinal;
    }
}
