using System.Text.Json;

namespace Platform.Domain.Templates;

public static class TemplateJson
{
    public static string RequireObject(string value, string parameterName)
    {
        return RequireKind(value, parameterName, JsonValueKind.Object);
    }

    public static string RequireArray(string value, string parameterName)
    {
        return RequireKind(value, parameterName, JsonValueKind.Array);
    }

    private static string RequireKind(string value, string parameterName, JsonValueKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != expectedKind)
            {
                throw new ArgumentException($"JSON value must be a {expectedKind.ToString().ToLowerInvariant()}.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value is invalid.", parameterName, exception);
        }

        return normalized;
    }
}
