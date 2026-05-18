using System.Text.Json;

namespace Platform.Domain.Campaigns;

internal static class CampaignJson
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
                throw new ArgumentException("JSON value must be an object.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value is invalid.", parameterName, exception);
        }

        return normalized;
    }
}
