using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260517162000_AddWithdrawalEventScoreRunCount")]
    public partial class AddWithdrawalEventScoreRunCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "score_run_count",
                table: "withdrawal_event",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_counts_non_negative;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_counts_non_negative
                    CHECK (
                        consent_record_count >= 0
                        AND response_session_count >= 0
                        AND answer_count >= 0
                        AND score_run_count >= 0
                        AND score_count >= 0
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_counts_non_negative;
                """);

            migrationBuilder.DropColumn(
                name: "score_run_count",
                table: "withdrawal_event");

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_counts_non_negative
                    CHECK (
                        consent_record_count >= 0
                        AND response_session_count >= 0
                        AND answer_count >= 0
                        AND score_count >= 0
                    );
                """);
        }
    }
}
