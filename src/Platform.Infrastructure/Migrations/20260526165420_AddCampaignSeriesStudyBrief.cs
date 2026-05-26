using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignSeriesStudyBrief : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "study_audience",
                table: "campaign_series",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_design_type",
                table: "campaign_series",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_intended_use",
                table: "campaign_series",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_interpretation_boundary",
                table: "campaign_series",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_owner_notes",
                table: "campaign_series",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_purpose",
                table: "campaign_series",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_campaign_series_study_design_type",
                table: "campaign_series",
                sql: "study_design_type IS NULL OR study_design_type IN ('single_wave', 'repeated_group_trend', 'repeated_linked_change')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campaign_series_study_intended_use",
                table: "campaign_series",
                sql: "study_intended_use IS NULL OR study_intended_use IN ('internal_review', 'research_analysis', 'client_report')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_campaign_series_study_design_type",
                table: "campaign_series");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campaign_series_study_intended_use",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_audience",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_design_type",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_intended_use",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_interpretation_boundary",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_owner_notes",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "study_purpose",
                table: "campaign_series");
        }
    }
}
