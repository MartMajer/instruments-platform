using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreOutputMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "missing_policy_status",
                table: "score",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "ok");

            migrationBuilder.AddColumn<int>(
                name: "n_expected",
                table: "score",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE score SET n_expected = n;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_score_missing_policy_status_shape",
                table: "score",
                sql: "missing_policy_status ~ '^[a-z0-9_.-]{1,64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_score_n_expected_non_negative",
                table: "score",
                sql: "n_expected >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_score_n_valid_not_above_expected",
                table: "score",
                sql: "n <= n_expected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_score_missing_policy_status_shape",
                table: "score");

            migrationBuilder.DropCheckConstraint(
                name: "ck_score_n_expected_non_negative",
                table: "score");

            migrationBuilder.DropCheckConstraint(
                name: "ck_score_n_valid_not_above_expected",
                table: "score");

            migrationBuilder.DropColumn(
                name: "missing_policy_status",
                table: "score");

            migrationBuilder.DropColumn(
                name: "n_expected",
                table: "score");
        }
    }
}
