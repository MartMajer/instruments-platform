using System.Text.Json;

namespace Platform.Application.Features.DirectoryImports;

public static class DirectoryImportRulePlanner
{
    private static readonly string[] ApprovedUserSelectFields =
    [
        "id",
        "displayName",
        "mail",
        "userPrincipalName",
        "department",
        "jobTitle",
        "employeeType",
        "officeLocation",
        "preferredLanguage",
        "accountEnabled",
        "userType"
    ];

    public static DirectoryImportPlan Plan(
        string criteriaJson,
        bool mirrorMode = false,
        DateTimeOffset? mirrorConfirmedAt = null)
    {
        if (mirrorMode && !mirrorConfirmedAt.HasValue)
        {
            throw new InvalidOperationException("Mirror-mode imports require explicit confirmation.");
        }

        using var document = ParseCriteria(criteriaJson);
        var root = document.RootElement;
        var graphFilters = new List<string>();
        var groupFetches = new List<DirectoryImportGroupMemberFetch>();
        var localPostFilters = new List<DirectoryImportLocalPostFilter>();
        var warnings = new List<DirectoryImportPlanWarning>();

        if (TryReadBoolean(root, "accountEnabled", out var accountEnabled))
        {
            graphFilters.Add($"accountEnabled eq {accountEnabled.ToString().ToLowerInvariant()}");
        }

        var userTypes = ReadStringArray(root, "userTypes");
        var excludeGuests = TryReadBoolean(root, "excludeGuests", out var excludeGuestsValue) && excludeGuestsValue;
        if (userTypes.Count > 0)
        {
            graphFilters.Add(BuildStringFilter("userType", userTypes));
        }
        else if (excludeGuests)
        {
            graphFilters.Add("userType eq 'Member'");
        }

        var departments = ReadStringArray(root, "departments");
        if (departments.Count > 0)
        {
            graphFilters.Add(BuildStringFilter("department", departments));
        }

        foreach (var groupId in ReadStringArray(root, "groupIds"))
        {
            groupFetches.Add(new DirectoryImportGroupMemberFetch(groupId));
        }

        if (TryReadString(root, "jobTitleContains", out var jobTitleContains))
        {
            localPostFilters.Add(new DirectoryImportLocalPostFilter(
                DirectoryImportLocalPostFilterKinds.JobTitleContains,
                jobTitleContains));
            warnings.Add(new DirectoryImportPlanWarning(
                "job_title_contains_local_filter",
                "Microsoft Graph does not support the requested job-title contains predicate in the safe filter set; it will be applied after candidate fetch."));
        }

        var managerFetchMode =
            TryReadBoolean(root, "includeManagerChain", out var includeManagerChain) && includeManagerChain
                ? DirectoryImportManagerFetchModes.ManagerChain
                : DirectoryImportManagerFetchModes.None;

        return new DirectoryImportPlan(
            ApprovedUserSelectFields,
            graphFilters.Count == 0 ? null : string.Join(" and ", graphFilters),
            RequiresAdvancedQuery: false,
            groupFetches,
            managerFetchMode,
            localPostFilters,
            warnings,
            mirrorMode);
    }

    private static JsonDocument ParseCriteria(string criteriaJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criteriaJson);

        try
        {
            var document = JsonDocument.Parse(criteriaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new ArgumentException("Criteria JSON must be an object.", nameof(criteriaJson));
            }

            return document;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Criteria JSON must be valid JSON.", nameof(criteriaJson), exception);
        }
    }

    private static bool TryReadBoolean(JsonElement root, string propertyName, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var normalized = property.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        value = normalized;
        return true;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
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

            var value = item.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static string BuildStringFilter(string fieldName, IReadOnlyList<string> values)
    {
        if (values.Count == 1)
        {
            return $"{fieldName} eq '{EscapeODataString(values[0])}'";
        }

        return "(" + string.Join(" or ", values.Select(value => $"{fieldName} eq '{EscapeODataString(value)}'")) + ")";
    }

    private static string EscapeODataString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
