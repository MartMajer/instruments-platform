namespace Platform.Domain.Instruments;

public static class InstrumentValidityLabels
{
    public const string Official = "official";
    public const string TenantProvided = "tenant_provided";
    public const string Adapted = "adapted";
    public const string Experimental = "experimental";
    public const string RightsUnverified = "rights_unverified";

    private static readonly HashSet<string> Known =
    [
        Official,
        TenantProvided,
        Adapted,
        Experimental,
        RightsUnverified
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
