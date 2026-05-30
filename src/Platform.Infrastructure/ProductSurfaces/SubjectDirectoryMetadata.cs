using System.Text.Json;
using System.Text.Json.Nodes;
using Platform.Application.Features.ProductSurfaces;

namespace Platform.Infrastructure.ProductSurfaces;

internal static class SubjectDirectoryMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SubjectDirectoryMetadataValues From(string? externalId, string attributesJson)
    {
        using var document = JsonDocument.Parse(attributesJson);
        var root = document.RootElement;
        var source = NormalizeSource(ReadString(root, "directory_source"), externalId);
        var status = NormalizeStatus(ReadString(root, "directory_status"));

        return new SubjectDirectoryMetadataValues(
            source,
            SourceLabel(source),
            status,
            StatusLabel(status),
            ReadString(root, "department"),
            ReadString(root, "job_title"),
            ReadString(root, "employee_type"),
            ReadString(root, "office_location"));
    }

    public static string MarkDeactivated(
        string attributesJson,
        Guid actorUserId,
        DateTimeOffset changedAt,
        string? reason)
    {
        return MarkStatus(
            attributesJson,
            SubjectDirectoryStatuses.Deactivated,
            actorUserId,
            changedAt,
            reason);
    }

    public static string MarkStatus(
        string attributesJson,
        string status,
        Guid actorUserId,
        DateTimeOffset changedAt,
        string? reason)
    {
        var attributes = ParseObject(attributesJson);
        attributes["directory_status"] = status;
        attributes["directory_status_changed_at"] = changedAt.ToString("O");
        attributes["directory_status_changed_by"] = actorUserId.ToString("D");
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedReason is null)
        {
            attributes.Remove("directory_status_reason");
        }
        else
        {
            attributes["directory_status_reason"] = normalizedReason;
        }

        return attributes.ToJsonString(JsonOptions);
    }

    public static string EnsureSource(string attributesJson, string source)
    {
        var attributes = ParseObject(attributesJson);
        attributes["directory_source"] = source;

        return attributes.ToJsonString(JsonOptions);
    }

    public static string MergeMicrosoftGraphSource(
        string attributesJson,
        string externalTenantId)
    {
        var attributes = ParseObject(attributesJson);
        attributes["directory_source"] = SubjectDirectorySources.MicrosoftGraph;
        attributes["directory_source_tenant_id"] = externalTenantId;

        return attributes.ToJsonString(JsonOptions);
    }

    private static JsonObject ParseObject(string attributesJson)
    {
        var node = JsonNode.Parse(attributesJson);
        if (node is not JsonObject attributes)
        {
            throw new ArgumentException("Subject attributes must be a JSON object.", nameof(attributesJson));
        }

        return attributes;
    }

    private static string NormalizeSource(string? value, string? externalId)
    {
        if (string.Equals(value, SubjectDirectorySources.MicrosoftGraph, StringComparison.OrdinalIgnoreCase) ||
            IsMicrosoftGraphExternalId(externalId))
        {
            return SubjectDirectorySources.MicrosoftGraph;
        }

        if (string.Equals(value, SubjectDirectorySources.Csv, StringComparison.OrdinalIgnoreCase))
        {
            return SubjectDirectorySources.Csv;
        }

        return SubjectDirectorySources.Manual;
    }

    private static string NormalizeStatus(string? value)
    {
        if (string.Equals(value, SubjectDirectoryStatuses.Deactivated, StringComparison.OrdinalIgnoreCase))
        {
            return SubjectDirectoryStatuses.Deactivated;
        }

        if (string.Equals(value, SubjectDirectoryStatuses.Excluded, StringComparison.OrdinalIgnoreCase))
        {
            return SubjectDirectoryStatuses.Excluded;
        }

        return SubjectDirectoryStatuses.Active;
    }

    private static bool IsMicrosoftGraphExternalId(string? externalId)
    {
        return externalId is not null &&
            externalId.StartsWith("msgraph:", StringComparison.OrdinalIgnoreCase);
    }

    private static string SourceLabel(string source)
    {
        return source switch
        {
            SubjectDirectorySources.MicrosoftGraph => "Microsoft 365",
            SubjectDirectorySources.Csv => "CSV",
            _ => "Manual"
        };
    }

    private static string StatusLabel(string status)
    {
        return status switch
        {
            SubjectDirectoryStatuses.Deactivated => "Deactivated",
            SubjectDirectoryStatuses.Excluded => "Excluded",
            _ => "Active"
        };
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }
}

internal sealed record SubjectDirectoryMetadataValues(
    string Source,
    string SourceLabel,
    string Status,
    string StatusLabel,
    string? Department,
    string? JobTitle,
    string? EmployeeType,
    string? OfficeLocation);
