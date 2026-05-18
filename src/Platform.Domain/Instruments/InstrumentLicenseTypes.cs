namespace Platform.Domain.Instruments;

public static class InstrumentLicenseTypes
{
    public const string Free = "free";
    public const string FreeAcademic = "free_academic";
    public const string Paid = "paid";
    public const string Unknown = "unknown";

    private static readonly HashSet<string> Known =
    [
        Free,
        FreeAcademic,
        Paid,
        Unknown
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
