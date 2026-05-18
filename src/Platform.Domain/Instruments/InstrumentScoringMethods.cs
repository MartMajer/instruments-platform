namespace Platform.Domain.Instruments;

public static class InstrumentScoringMethods
{
    public const string Mean = "mean";
    public const string Sum = "sum";
    public const string Weighted = "weighted";

    private static readonly HashSet<string> Known =
    [
        Mean,
        Sum,
        Weighted
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
