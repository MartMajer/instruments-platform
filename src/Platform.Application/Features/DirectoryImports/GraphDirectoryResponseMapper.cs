using System.Text.Json;

namespace Platform.Application.Features.DirectoryImports;

public static class GraphDirectoryResponseMapper
{
    public static IReadOnlyList<GraphDirectoryUserCandidate> MapUsers(string json)
    {
        return MapUserPage(json).Users;
    }

    public static GraphDirectoryUserPage MapUserPage(string json)
    {
        using var document = JsonDocument.Parse(json);
        var users = new List<GraphDirectoryUserCandidate>();

        if (!document.RootElement.TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return new GraphDirectoryUserPage(users, ReadOptionalString(document.RootElement, "@odata.nextLink"));
        }

        foreach (var userElement in value.EnumerateArray())
        {
            var id = ReadOptionalString(userElement, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            users.Add(MapUser(id, userElement));
        }

        return new GraphDirectoryUserPage(users, ReadOptionalString(document.RootElement, "@odata.nextLink"));
    }

    public static GraphDirectoryGroupPage MapGroupPage(string json)
    {
        using var document = JsonDocument.Parse(json);
        var groups = new List<GraphDirectoryGroupCandidate>();

        if (!document.RootElement.TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return new GraphDirectoryGroupPage(groups, ReadOptionalString(document.RootElement, "@odata.nextLink"));
        }

        foreach (var groupElement in value.EnumerateArray())
        {
            var id = ReadOptionalString(groupElement, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            groups.Add(new GraphDirectoryGroupCandidate(
                id,
                ReadOptionalString(groupElement, "displayName"),
                ReadOptionalBoolean(groupElement, "mailEnabled"),
                ReadOptionalBoolean(groupElement, "securityEnabled"),
                ReadStringArray(groupElement, "groupTypes")));
        }

        return new GraphDirectoryGroupPage(groups, ReadOptionalString(document.RootElement, "@odata.nextLink"));
    }

    public static GraphDirectoryManagerCandidate? MapManager(string userGraphId, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var managerId = ReadOptionalString(root, "id");
        if (string.IsNullOrWhiteSpace(managerId))
        {
            return null;
        }

        var mail = ReadOptionalString(root, "mail");
        var userPrincipalName = ReadOptionalString(root, "userPrincipalName");
        var email = ChooseEmail(mail, userPrincipalName);
        var warnings = BuildEmailWarnings(email);

        return new GraphDirectoryManagerCandidate(
            userGraphId,
            managerId,
            ReadOptionalString(root, "displayName"),
            email,
            userPrincipalName,
            warnings);
    }

    private static GraphDirectoryUserCandidate MapUser(string id, JsonElement userElement)
    {
        var mail = ReadOptionalString(userElement, "mail");
        var userPrincipalName = ReadOptionalString(userElement, "userPrincipalName");
        var email = ChooseEmail(mail, userPrincipalName);

        return new GraphDirectoryUserCandidate(
            id,
            email,
            userPrincipalName,
            ReadOptionalString(userElement, "displayName"),
            ReadOptionalString(userElement, "department"),
            ReadOptionalString(userElement, "jobTitle"),
            ReadOptionalString(userElement, "employeeType"),
            ReadOptionalString(userElement, "officeLocation"),
            ReadOptionalString(userElement, "preferredLanguage"),
            ReadOptionalBoolean(userElement, "accountEnabled"),
            ReadOptionalString(userElement, "userType"),
            BuildEmailWarnings(email));
    }

    private static IReadOnlyList<GraphDirectoryCandidateWarning> BuildEmailWarnings(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return [];
        }

        return
        [
            new GraphDirectoryCandidateWarning(
                GraphDirectoryCandidateWarningCodes.MissingEmail,
                "Microsoft Graph user has neither mail nor userPrincipalName available for import.")
        ];
    }

    private static string? ChooseEmail(string? mail, string? userPrincipalName)
    {
        return !string.IsNullOrWhiteSpace(mail) ? mail : userPrincipalName;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null ||
            property.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? NullIfWhiteSpace(property.GetString())
            : null;
    }

    private static bool? ReadOptionalBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = NullIfWhiteSpace(item.GetString());
            if (value is not null)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
