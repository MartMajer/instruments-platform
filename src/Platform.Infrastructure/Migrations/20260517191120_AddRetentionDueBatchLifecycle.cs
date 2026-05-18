using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionDueBatchLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "retention_due_batch",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "retention_due_batch",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "retention_due_batch",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_detail",
                table: "retention_due_batch",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_started_at",
                table: "retention_due_batch",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_status_created_at",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_retention_due_batch_lifecycle",
                table: "retention_due_batch",
                sql: "((status = 'planned' AND processing_started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'processing' AND processing_started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL) OR (status = 'completed' AND processing_started_at IS NOT NULL AND completed_at IS NOT NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'failed' AND completed_at IS NULL AND failed_at IS NOT NULL AND failure_code IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_retention_due_batch_tenant_status_created_at",
                table: "retention_due_batch");

            migrationBuilder.DropCheckConstraint(
                name: "ck_retention_due_batch_lifecycle",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "failure_detail",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                table: "retention_due_batch");
        }
    }
}
