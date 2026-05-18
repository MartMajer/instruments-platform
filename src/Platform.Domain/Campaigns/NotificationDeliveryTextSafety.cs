namespace Platform.Domain.Campaigns;

internal static class NotificationDeliveryTextSafety
{
    private const int MaxProviderMessageIdLength = 200;
    private const string RedactedProviderMessageId = "redacted";
    private const string UnknownProvider = "unknown";

    public const string DeliveryFailed = "delivery_failed";

    public static string SanitizeProvider(string? provider)
    {
        if (string.Equals(provider, "local-dev", StringComparison.OrdinalIgnoreCase))
        {
            return "local-dev";
        }

        if (string.Equals(provider, "smtp", StringComparison.OrdinalIgnoreCase))
        {
            return "smtp";
        }

        return UnknownProvider;
    }

    public static string SanitizeFailureError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return DeliveryFailed;
        }

        var normalized = ReplaceControlCharacters(error).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || ContainsSensitiveValue(normalized))
        {
            return DeliveryFailed;
        }

        return normalized;
    }

    public static string SanitizeProviderMessageId(string? providerMessageId)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return RedactedProviderMessageId;
        }

        var normalized = ReplaceControlCharacters(providerMessageId).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || ContainsSensitiveValue(normalized))
        {
            return RedactedProviderMessageId;
        }

        return normalized.Length > MaxProviderMessageIdLength
            ? normalized[..MaxProviderMessageIdLength]
            : normalized;
    }

    private static bool ContainsSensitiveValue(string value)
    {
        return value.Contains("/r/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inv_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("opn_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wdr_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            value.Contains('@');
    }

    private static string ReplaceControlCharacters(string value)
    {
        var characters = value.ToCharArray();
        for (var index = 0; index < characters.Length; index++)
        {
            if (char.IsControl(characters[index]))
            {
                characters[index] = ' ';
            }
        }

        return new string(characters);
    }
}

