using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionDueBatchExecutionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "artifact_invalidated_count",
                table: "retention_due_batch",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "delivery_attempt_scrubbed_count",
                table: "retention_due_batch",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "execution_result",
                table: "retention_due_batch",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "invite_credential_scrubbed_count",
                table: "retention_due_batch",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "notice_scrubbed_count",
                table: "retention_due_batch",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "artifact_invalidated_count",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "delivery_attempt_scrubbed_count",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "execution_result",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "invite_credential_scrubbed_count",
                table: "retention_due_batch");

            migrationBuilder.DropColumn(
                name: "notice_scrubbed_count",
                table: "retention_due_batch");
        }
    }
}
