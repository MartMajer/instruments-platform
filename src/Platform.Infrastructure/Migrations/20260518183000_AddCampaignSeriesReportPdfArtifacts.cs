using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518183000_AddCampaignSeriesReportPdfArtifacts")]
    public partial class AddCampaignSeriesReportPdfArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_format",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_format",
                table: "export_artifact",
                sql: "format IN ('csv_codebook','html','pdf')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact",
                sql: "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook','campaign_series_report_html','campaign_series_report_pdf')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_format",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_format",
                table: "export_artifact",
                sql: "format IN ('csv_codebook','html')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact",
                sql: "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook','campaign_series_report_html')");
        }
    }
}
