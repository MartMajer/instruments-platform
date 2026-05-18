using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518191000_AllowExternalObjectFailedExportArtifactAttempts")]
    public partial class AllowExternalObjectFailedExportArtifactAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact",
                sql: "(storage_kind = 'inline_text' AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND content IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact",
                sql: "(storage_kind = 'inline_text' AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND storage_key IS NOT NULL AND content IS NULL)");
        }
    }
}
