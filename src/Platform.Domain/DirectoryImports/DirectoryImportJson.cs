using System.Text.Json;

namespace Platform.Domain.DirectoryImports;

internal static class DirectoryImportJson
{
    public static string RequireObject(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        try
        {
            using var document = JsonDocument.Parse(value);
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
