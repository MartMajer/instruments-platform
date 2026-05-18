using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518160000_AddExportArtifactStorageLocation")]
    public partial class AddExportArtifactStorageLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact");

            migrationBuilder.AddColumn<string>(
                name: "storage_kind",
                table: "export_artifact",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "inline_text");

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "export_artifact",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE export_artifact
                SET storage_kind = 'inline_text'
                WHERE storage_kind IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_storage_kind",
                table: "export_artifact",
                sql: "storage_kind IN ('inline_text','external_object')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact",
                sql: "(storage_kind = 'inline_text' AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND storage_key IS NOT NULL AND content IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact",
                sql: "(status = 'succeeded' AND completed_at IS NOT NULL AND checksum_sha256 IS NOT NULL AND ((storage_kind = 'inline_text' AND content IS NOT NULL AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND content IS NULL AND storage_key IS NOT NULL)))\nOR (status <> 'succeeded' AND checksum_sha256 IS NULL AND content IS NULL AND storage_key IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_storage_shape",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_storage_kind",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "storage_kind",
                table: "export_artifact");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact",
                sql: "(status = 'succeeded' AND completed_at IS NOT NULL AND checksum_sha256 IS NOT NULL AND content IS NOT NULL)\nOR (status <> 'succeeded' AND checksum_sha256 IS NULL AND content IS NULL)");
        }
    }
}
