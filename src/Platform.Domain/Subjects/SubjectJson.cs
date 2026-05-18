using System.Text.Json;

namespace Platform.Domain.Subjects;

internal static class SubjectJson
{
    public static string RequireObject(string json, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, parameterName);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("JSON value must be an object.", parameterName);
            }

            return document.RootElement.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value must be valid JSON.", parameterName, exception);
        }
    }
}
