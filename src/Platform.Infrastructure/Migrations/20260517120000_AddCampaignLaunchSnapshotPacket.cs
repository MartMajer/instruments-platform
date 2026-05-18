using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260517120000_AddCampaignLaunchSnapshotPacket")]
public partial class AddCampaignLaunchSnapshotPacket : Migration
{
    private const string BackfillLaunchPacket =
        """{"schema_version":1,"template":{"status":"unknown"},"instrument":{"status":"unknown"},"scoring":{"status":"unknown"},"policies":{"consent":"unknown","retention":"unknown","disclosure":"unknown"},"identity":{"response_identity_mode":"unknown"},"respondent_rules":{"materialization":"unknown","materialized_assignment_count":0},"launch_readiness":{"status":"unknown"},"provenance":{"source":"migration_backfill"}}""";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "launch_packet",
            table: "campaign_launch_snapshot",
            type: "jsonb",
            nullable: false,
            defaultValue: BackfillLaunchPacket);

        migrationBuilder.AddCheckConstraint(
            name: "ck_campaign_launch_snapshot_packet_object",
            table: "campaign_launch_snapshot",
            sql: "jsonb_typeof(launch_packet) = 'object'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_campaign_launch_snapshot_packet_object",
            table: "campaign_launch_snapshot");

        migrationBuilder.DropColumn(
            name: "launch_packet",
            table: "campaign_launch_snapshot");
    }
}
