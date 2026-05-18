using Platform.Application.Features.Reports;
using Platform.Application.Features.ProductSurfaces;
using Platform.Infrastructure.Reports;
using Platform.Infrastructure.ProductSurfaces;
using System.Reflection;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class LaunchPacketProvenanceContractTests
{
    [Fact]
    public void Report_and_wave_contracts_expose_launch_packet_provenance()
    {
        Assert.Contains(
            typeof(ReportLaunchSnapshotResponse).GetProperties(),
            property => property.Name == "LaunchPacket");
        Assert.Contains(
            typeof(WaveComparisonWaveResponse).GetProperties(),
            property => property.Name == "LaunchPacket");
        Assert.Contains(
            typeof(LaunchPacketProvenanceResponse).GetProperties(),
            property => property.Name == "Source");

        var reportSnapshot = new ReportLaunchSnapshotResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hash",
            null,
            null,
            null,
            "anonymous",
            DateTimeOffset.Parse("2026-05-17T12:00:00+00:00"),
            new LaunchPacketProvenanceResponse(1, ["scoring", "policies"], "runtime_launch"));
        var reportJson = System.Text.Json.JsonSerializer.Serialize(
            reportSnapshot,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"launchPacket\"", reportJson);
        Assert.Contains("\"schemaVersion\":1", reportJson);
        Assert.Contains("\"source\":\"runtime_launch\"", reportJson);
        Assert.Contains("scoring", reportJson);
        Assert.Contains("policies", reportJson);
    }

    [Fact]
    public void Response_export_contract_includes_launch_packet_provenance_columns()
    {
        var columnsField = typeof(ReportProofExportStore).GetField(
            "ResponseExportBaseColumns",
            BindingFlags.NonPublic | BindingFlags.Static);
        var columns = Assert.IsType<string[]>(columnsField?.GetValue(null));

        Assert.Contains("launch_packet_schema_version", columns);
        Assert.Contains("launch_packet_sections", columns);
        Assert.Contains("launch_packet_source", columns);

        var reportColumnsField = typeof(ReportProofExportStore).GetField(
            "CsvColumns",
            BindingFlags.NonPublic | BindingFlags.Static);
        var reportColumns = Assert.IsType<string[]>(reportColumnsField?.GetValue(null));
        Assert.Contains("launch_packet_source", reportColumns);

        var columnSourceMethod = typeof(ReportProofExportStore).GetMethod(
            "ResponseExportColumnSource",
            BindingFlags.NonPublic | BindingFlags.Static);
        var reportColumnSourceMethod = typeof(ReportProofExportStore).GetMethod(
            "ColumnSource",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Equal("launch_packet", columnSourceMethod?.Invoke(null, ["launch_packet_schema_version"]));
        Assert.Equal("launch_packet", columnSourceMethod?.Invoke(null, ["launch_packet_sections"]));
        Assert.Equal("launch_packet", columnSourceMethod?.Invoke(null, ["launch_packet_source"]));
        Assert.Equal("launch_packet", reportColumnSourceMethod?.Invoke(null, ["launch_packet_source"]));
    }

    [Fact]
    public void Product_surface_launch_snapshot_contract_exposes_launch_packet_provenance()
    {
        Assert.Contains(
            typeof(CampaignSeriesOperationsLaunchSnapshotResponse).GetProperties(),
            property => property.Name == "LaunchPacket");
        Assert.Contains(
            typeof(ProductSurfaceLaunchPacketProvenanceResponse).GetProperties(),
            property => property.Name == "Source");

        var snapshot = new CampaignSeriesOperationsLaunchSnapshotResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            "anonymous",
            "en",
            3,
            DateTimeOffset.Parse("2026-05-17T12:00:00+00:00"),
            null,
            new ProductSurfaceLaunchPacketProvenanceResponse(1, ["scoring", "policies"], "runtime_launch"));
        var json = System.Text.Json.JsonSerializer.Serialize(
            snapshot,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"launchPacket\"", json);
        Assert.Contains("\"schemaVersion\":1", json);
        Assert.Contains("\"source\":\"runtime_launch\"", json);
        Assert.Contains("scoring", json);
    }

    [Fact]
    public void Product_surface_wave_contract_exposes_launch_packet_provenance()
    {
        Assert.Contains(
            typeof(CampaignSeriesWavesWaveResponse).GetProperties(),
            property => property.Name == "LaunchPacket");
    }

    [Fact]
    public void Product_surface_reports_launch_detail_projection_carries_launch_packet()
    {
        var launchDetailRow = typeof(ProductSurfaceReadStore).GetNestedType(
            "CampaignReportLaunchDetailRow",
            BindingFlags.NonPublic);

        Assert.NotNull(launchDetailRow);
        Assert.Contains(
            launchDetailRow.GetProperties(),
            property => property.Name == "LaunchPacket");
    }

    [Fact]
    public void Launch_packet_projection_accepts_camel_case_schema_version()
    {
        var projectionType = typeof(ReportProofExportStore).Assembly.GetType(
            "Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection");
        var fromJson = projectionType?.GetMethod(
            "FromJson",
            BindingFlags.Public | BindingFlags.Static);
        var result = fromJson?.Invoke(
            null,
            ["""{"schemaVersion":2,"scoring":{},"policies":{},"tenant_id":"blocked"}"""]);
        var schemaVersion = result?.GetType().GetProperty("SchemaVersion")?.GetValue(result);
        var sections = Assert.IsAssignableFrom<IReadOnlyList<string>>(
            result?.GetType().GetProperty("Sections")?.GetValue(result));

        Assert.Equal(2, schemaVersion);
        Assert.Contains("scoring", sections);
        Assert.Contains("policies", sections);
        Assert.DoesNotContain("tenant_id", sections);
    }

    [Fact]
    public void Launch_packet_projection_exposes_provenance_source()
    {
        var projectionType = typeof(ReportProofExportStore).Assembly.GetType(
            "Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection");
        var fromJson = projectionType?.GetMethod(
            "FromJson",
            BindingFlags.Public | BindingFlags.Static);
        var result = fromJson?.Invoke(
            null,
            ["""{"schema_version":1,"scoring":{},"provenance":{"source":"migration_backfill","campaign_id":"blocked"}}"""]);

        Assert.Equal(
            "migration_backfill",
            result?.GetType().GetProperty("Source")?.GetValue(result));
    }

    [Fact]
    public void Launch_packet_projection_allowlists_provenance_source()
    {
        var projectionType = typeof(ReportProofExportStore).Assembly.GetType(
            "Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection");
        var fromJson = projectionType?.GetMethod(
            "FromJson",
            BindingFlags.Public | BindingFlags.Static);

        var unsafeResult = fromJson?.Invoke(
            null,
            ["""{"schema_version":1,"provenance":{"source":"tenant_id:secret-token"}}"""]);
        var safeResult = fromJson?.Invoke(
            null,
            ["""{"schema_version":1,"provenance":{"source":"runtime_launch"}}"""]);

        Assert.Equal("unknown", unsafeResult?.GetType().GetProperty("Source")?.GetValue(unsafeResult));
        Assert.Equal("runtime_launch", safeResult?.GetType().GetProperty("Source")?.GetValue(safeResult));
    }

    [Fact]
    public void Launch_packet_projection_without_source_is_unknown()
    {
        var projectionType = typeof(ReportProofExportStore).Assembly.GetType(
            "Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection");
        var fromJson = projectionType?.GetMethod(
            "FromJson",
            BindingFlags.Public | BindingFlags.Static);
        var result = fromJson?.Invoke(
            null,
            ["""{"schema_version":1,"scoring":{},"provenance":{"launched_at":"2026-05-17T12:00:00Z"}}"""]);

        Assert.Equal("unknown", result?.GetType().GetProperty("Source")?.GetValue(result));
    }

    [Fact]
    public void Response_export_codebook_declares_excluded_launch_packet_identifiers()
    {
        var excludedIdentifiersField = typeof(ReportProofExportStore).GetField(
            "ResponseExportExcludedIdentifiers",
            BindingFlags.NonPublic | BindingFlags.Static);
        var excludedIdentifiers = Assert.IsType<string[]>(excludedIdentifiersField?.GetValue(null));

        Assert.Contains("tenant_id", excludedIdentifiers);
        Assert.Contains("launch_packet_raw_json", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.campaign_id", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.campaign_series_id", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.launched_by", excludedIdentifiers);
    }

    [Fact]
    public void Report_proof_codebook_declares_excluded_launch_packet_identifiers()
    {
        var excludedIdentifiersField = typeof(ReportProofExportStore).GetField(
            "ReportProofExcludedIdentifiers",
            BindingFlags.NonPublic | BindingFlags.Static);
        var excludedIdentifiers = Assert.IsType<string[]>(excludedIdentifiersField?.GetValue(null));

        Assert.Contains("tenant_id", excludedIdentifiers);
        Assert.Contains("launch_packet_raw_json", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.campaign_id", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.campaign_series_id", excludedIdentifiers);
        Assert.Contains("launch_packet.provenance.launched_by", excludedIdentifiers);
    }

    [Fact]
    public void Launch_packet_codebook_columns_have_specific_disclosure_treatment()
    {
        var responseDisclosureMethod = typeof(ReportProofExportStore).GetMethod(
            "ResponseExportColumnDisclosureTreatment",
            BindingFlags.NonPublic | BindingFlags.Static);
        var reportDisclosureMethod = typeof(ReportProofExportStore).GetMethod(
            "ColumnDisclosureTreatment",
            BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var column in new[] { "launch_packet_schema_version", "launch_packet_sections", "launch_packet_source" })
        {
            Assert.Equal("launch_packet_provenance", responseDisclosureMethod?.Invoke(null, [column]));
            Assert.Equal("launch_packet_provenance", reportDisclosureMethod?.Invoke(null, [column]));
        }
    }
}
