using System.Text.Json;
using Platform.Application.Features.Reports;

namespace Platform.Infrastructure.Reports;

internal static class LaunchPacketProvenanceProjection
{
    private static readonly HashSet<string> KnownSources = new(StringComparer.Ordinal)
    {
        "runtime_launch",
        "migration_backfill",
        "missing",
        "invalid",
        "unknown"
    };

    private static readonly string[] KnownSections =
    [
        "template",
        "instrument",
        "scoring",
        "policies",
        "identity",
        "respondent_rules",
        "launch_readiness",
        "provenance"
    ];

    public static LaunchPacketProvenanceResponse FromJson(string? launchPacket)
    {
        if (string.IsNullOrWhiteSpace(launchPacket))
        {
            return new LaunchPacketProvenanceResponse(0, [], "missing");
        }

        try
        {
            using var document = JsonDocument.Parse(launchPacket);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new LaunchPacketProvenanceResponse(0, [], "invalid");
            }

            var schemaVersion = ReadSchemaVersion(root);
            var sections = KnownSections
                .Where(section => root.TryGetProperty(section, out _))
                .ToArray();
            var source = ReadSource(root, schemaVersion);

            return new LaunchPacketProvenanceResponse(schemaVersion, sections, source);
        }
        catch (JsonException)
        {
            return new LaunchPacketProvenanceResponse(0, [], "invalid");
        }
    }

    private static int ReadSchemaVersion(JsonElement root)
    {
        return TryReadSchemaVersion(root, "schema_version", out var snakeCaseVersion)
            ? snakeCaseVersion
            : TryReadSchemaVersion(root, "schemaVersion", out var camelCaseVersion)
                ? camelCaseVersion
                : 0;
    }

    private static bool TryReadSchemaVersion(
        JsonElement root,
        string propertyName,
        out int schemaVersion)
    {
        if (root.TryGetProperty(propertyName, out var schemaVersionElement) &&
            schemaVersionElement.ValueKind == JsonValueKind.Number &&
            schemaVersionElement.TryGetInt32(out schemaVersion))
        {
            return true;
        }

        schemaVersion = 0;
        return false;
    }

    private static string ReadSource(JsonElement root, int schemaVersion)
    {
        if (root.TryGetProperty("provenance", out var provenanceElement) &&
            provenanceElement.ValueKind == JsonValueKind.Object &&
            provenanceElement.TryGetProperty("source", out var sourceElement) &&
            sourceElement.ValueKind == JsonValueKind.String)
        {
            var source = sourceElement.GetString();
            if (!string.IsNullOrWhiteSpace(source) && KnownSources.Contains(source))
            {
                return source;
            }
        }

        return "unknown";
    }

    public static string FormatSections(LaunchPacketProvenanceResponse launchPacket)
    {
        return string.Join(';', launchPacket.Sections);
    }
}
