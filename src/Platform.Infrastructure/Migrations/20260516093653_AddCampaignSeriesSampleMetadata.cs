using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignSeriesSampleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sample_scenario",
                table: "campaign_series",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_kind",
                table: "campaign_series",
                type: "text",
                nullable: false,
                defaultValue: "own");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campaign_series_sample_consistency",
                table: "campaign_series",
                sql: "(study_kind = 'own' AND sample_scenario IS NULL) OR (study_kind = 'sample' AND sample_scenario IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campaign_series_sample_scenario",
                table: "campaign_series",
                sql: "sample_scenario IS NULL OR sample_scenario IN ('mixed_lifecycle', 'longitudinal', 'setup', 'in_collection', 'completed', 'blocked')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campaign_series_study_kind",
                table: "campaign_series",
                sql: "study_kind IN ('own', 'sample')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_campaign_series_sample_consistency",
                table: "campaign_series");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campaign_series_sample_scenario",
                table: "campaign_series");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campaign_series_study_kind",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "sample_scenario",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_kind",
                table: "campaign_series");
        }
    }
}
