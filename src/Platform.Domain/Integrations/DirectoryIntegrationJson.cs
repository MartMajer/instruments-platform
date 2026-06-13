using System.Text.Json;

namespace Platform.Domain.Integrations;

internal static class DirectoryIntegrationJson
{
    public static string RequireObject(string json, string parameterName)
    {
        return RequireKind(json, parameterName, JsonValueKind.Object);
    }

    public static string RequireArray(string json, string parameterName)
    {
        return RequireKind(json, parameterName, JsonValueKind.Array);
    }

    private static string RequireKind(string json, string parameterName, JsonValueKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, parameterName);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != expectedKind)
            {
                throw new ArgumentException($"JSON value must be a {expectedKind.ToString().ToLowerInvariant()}.", parameterName);
            }

            return document.RootElement.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value must be valid JSON.", parameterName, exception);
        }
    }
}
