namespace Platform.Domain.Instruments;

public static class InstrumentDomains
{
    public const string Psychometric = "psychometric";
    public const string Ergonomic = "ergonomic";
    public const string Medical = "medical";
    public const string Educational = "educational";
    public const string Regulatory = "regulatory";
    public const string Other = "other";

    private static readonly HashSet<string> Known =
    [
        Psychometric,
        Ergonomic,
        Medical,
        Educational,
        Regulatory,
        Other
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
