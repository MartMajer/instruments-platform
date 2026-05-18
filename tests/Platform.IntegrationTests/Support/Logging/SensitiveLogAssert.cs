namespace Platform.IntegrationTests.Support.Logging;

public static class SensitiveLogAssert
{
    public static readonly string[] DefaultSentinels =
    [
        "alpha-raw-participant-code-2026",
        "inv_11111111111141118111111111111111_sensitiveINV",
        "opn_11111111111141118111111111111111_sensitiveOPN",
        "idn_11111111111141118111111111111111_sensitiveIDN",
        "rsh_11111111111141118111111111111111_sensitiveRSH",
        "raw free-text answer with identifiable detail",
        "Host=db.example;Username=platform_app;Password=super-secret",
        "smtp-provider-token-secret",
        "series-salt-raw-value",
        "/r/inv_11111111111141118111111111111111_sensitiveINV"
    ];

    public static string JoinSentinels()
    {
        return string.Join(" | ", DefaultSentinels);
    }

    public static void DoesNotContain(CapturedLoggerProvider provider, params string[] sensitiveValues)
    {
        var captured = Flatten(provider).ToArray();

        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var leak = captured.FirstOrDefault(
                value => value.Contains(sensitiveValue, StringComparison.OrdinalIgnoreCase));

            Assert.True(
                leak is null,
                $"Captured logs contained sensitive value '{sensitiveValue}' in '{leak}'.");
        }
    }

    public static void Contains(CapturedLoggerProvider provider, string expected)
    {
        Assert.Contains(
            Flatten(provider),
            value => value.Contains(expected, StringComparison.Ordinal));
    }

    private static IEnumerable<string> Flatten(CapturedLoggerProvider provider)
    {
        foreach (var entry in provider.Entries)
        {
            yield return entry.CategoryName;
            yield return entry.Level.ToString();
            yield return entry.EventId.ToString();
            yield return entry.Message;
            yield return entry.MessageTemplate ?? string.Empty;
            yield return entry.ExceptionType ?? string.Empty;
            yield return entry.ExceptionMessage ?? string.Empty;

            foreach (var (key, value) in entry.State)
            {
                yield return key;
                yield return value ?? string.Empty;
            }

            foreach (var scope in entry.Scopes)
            {
                yield return scope;
            }
        }
    }
}
