namespace Platform.Domain.Instruments;

public static class InstrumentNormTypes
{
    public const string PublishedInstrument = "published_instrument";
    public const string PlatformBenchmark = "platform_benchmark";
    public const string TenantBenchmark = "tenant_benchmark";

    private static readonly HashSet<string> Known =
    [
        PublishedInstrument,
        PlatformBenchmark,
        TenantBenchmark
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
