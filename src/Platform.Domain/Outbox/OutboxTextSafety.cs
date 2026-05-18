namespace Platform.Domain.Outbox;

public static class OutboxTextSafety
{
    public const int MaxIdentifierLength = 128;
    public const string UnsafeEventType = "unsafe_event_type";
    public const string FailureFallback = "OUTBOX_FAILURE";

    public static string EnsureSafeIdentifier(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"Outbox {parameterName} must be a non-empty safe identifier.",
                parameterName);
        }

        var normalized = value.Trim();
        if (!IsSafeIdentifier(normalized))
        {
            throw new ArgumentException(
                $"Outbox {parameterName} contains unsafe identifier characters.",
                parameterName);
        }

        return normalized;
    }

    public static string SafeIdentifierForDiagnostics(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || !IsSafeIdentifier(value.Trim())
            ? UnsafeEventType
            : value.Trim();
    }

    public static string SanitizeFailureText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FailureFallback;
        }

        var normalized = ReplaceControlCharacters(value).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || ContainsSensitiveValue(normalized))
        {
            return FailureFallback;
        }

        return normalized;
    }

    public static bool ContainsSensitiveValue(string value)
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

    private static bool IsSafeIdentifier(string value)
    {
        if (value.Length is 0 or > MaxIdentifierLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '-' and not '.')
            {
                return false;
            }
        }

        return true;
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

