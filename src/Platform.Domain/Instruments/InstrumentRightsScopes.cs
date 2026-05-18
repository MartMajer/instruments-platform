namespace Platform.Domain.Instruments;

public static class InstrumentRightsScopes
{
    public const string PlatformGranted = "platform_granted";
    public const string TenantProvided = "tenant_provided";

    private static readonly HashSet<string> Known =
    [
        PlatformGranted,
        TenantProvided
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
