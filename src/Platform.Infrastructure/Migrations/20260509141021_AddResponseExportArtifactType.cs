using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseExportArtifactType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact",
                sql: "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_type",
                table: "export_artifact",
                sql: "artifact_type IN ('report_proof_csv_codebook')");
        }
    }
}
