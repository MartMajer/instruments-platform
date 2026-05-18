using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260517153000_ExpandWithdrawalEventStatuses")]
    public partial class ExpandWithdrawalEventStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_status;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_status
                    CHECK (status IN ('planned', 'processing', 'completed', 'failed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_status;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_status
                    CHECK (status IN ('planned'));
                """);
        }
    }
}
