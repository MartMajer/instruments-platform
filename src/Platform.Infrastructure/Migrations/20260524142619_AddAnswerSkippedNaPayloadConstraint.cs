using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerSkippedNaPayloadConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_answer_skipped_na_payload_shape",
                table: "answer",
                sql: "NOT (is_skipped = TRUE OR is_na = TRUE) OR (value IS NULL AND NULLIF(BTRIM(COALESCE(comment, '')), '') IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_answer_skipped_na_payload_shape",
                table: "answer");
        }
    }
}
