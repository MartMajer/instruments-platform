namespace Platform.Domain.Instruments;

public static class InstrumentRightsStatuses
{
    public const string Verified = "verified";
    public const string AttestedByTenant = "attested_by_tenant";
    public const string UnverifiedInternalDemo = "unverified_internal_demo";
    public const string Expired = "expired";

    private static readonly HashSet<string> Known =
    [
        Verified,
        AttestedByTenant,
        UnverifiedInternalDemo,
        Expired
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
