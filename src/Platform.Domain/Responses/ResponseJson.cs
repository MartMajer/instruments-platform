using System.Text.Json;

namespace Platform.Domain.Responses;

internal static class ResponseJson
{
    public static string RequireValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value is invalid.", parameterName, exception);
        }

        return normalized;
    }
}
