using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518103000_AddWithdrawalRequestDeniedStatus")]
    public partial class AddWithdrawalRequestDeniedStatus : Migration
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
                    CHECK (status IN ('requested', 'planned', 'processing', 'completed', 'failed', 'denied'));
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
                    CHECK (status IN ('requested', 'planned', 'processing', 'completed', 'failed'));
                """);
        }
    }
}
