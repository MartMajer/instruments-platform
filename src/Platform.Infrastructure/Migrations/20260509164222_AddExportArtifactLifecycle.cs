using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportArtifactLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_checksum_sha256",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_status",
                table: "export_artifact");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "export_artifact",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "checksum_sha256",
                table: "export_artifact",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "export_artifact",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "export_artifact",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "export_artifact",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason_code",
                table: "export_artifact",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                table: "export_artifact",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE export_artifact
                SET status = 'succeeded',
                    completed_at = COALESCE(completed_at, created_at)
                WHERE status = 'succeeded';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_checksum_sha256",
                table: "export_artifact",
                sql: "checksum_sha256 IS NULL OR checksum_sha256 ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_failure_reason_shape",
                table: "export_artifact",
                sql: "failure_reason_code IS NULL OR failure_reason_code ~ '^[a-z0-9_.-]{1,128}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_lifecycle_shape",
                table: "export_artifact",
                sql: "(status = 'queued' AND started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND expires_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'rendering' AND started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'succeeded' AND completed_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'failed' AND failed_at IS NOT NULL AND failure_reason_code IS NOT NULL AND completed_at IS NULL AND deleted_at IS NULL)\nOR (status = 'expired' AND expires_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'deleted' AND deleted_at IS NOT NULL AND failed_at IS NULL AND failure_reason_code IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact",
                sql: "(status = 'succeeded' AND completed_at IS NOT NULL AND checksum_sha256 IS NOT NULL AND content IS NOT NULL)\nOR (status <> 'succeeded' AND checksum_sha256 IS NULL AND content IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_status",
                table: "export_artifact",
                sql: "status IN ('queued','rendering','succeeded','failed','expired','deleted')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_checksum_sha256",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_failure_reason_shape",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_lifecycle_shape",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_materialization_shape",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_status",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "failure_reason_code",
                table: "export_artifact");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "export_artifact");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "export_artifact",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "checksum_sha256",
                table: "export_artifact",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_checksum_sha256",
                table: "export_artifact",
                sql: "checksum_sha256 ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_status",
                table: "export_artifact",
                sql: "status IN ('succeeded')");
        }
    }
}
