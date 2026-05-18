using System.Text.Json;

namespace Platform.Domain.Reports;

public static class ExportArtifactJson
{
    public static string RequireObject(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Value must be a JSON object.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }

        return normalized;
    }
}
