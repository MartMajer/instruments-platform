namespace Platform.Domain.Instruments;

public static class InstrumentValidityStatuses
{
    public const string Canonical = "canonical";
    public const string Derived = "derived";
    public const string PrivateImport = "private_import";
    public const string Draft = "draft";
    public const string Retired = "retired";

    private static readonly HashSet<string> Known =
    [
        Canonical,
        Derived,
        PrivateImport,
        Draft,
        Retired
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
