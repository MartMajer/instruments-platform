using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521193000_AddPreparedNotificationDeliveryAttempts")]
    public partial class AddPreparedNotificationDeliveryAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE notification_delivery_attempt
                DROP CONSTRAINT IF EXISTS ck_notification_delivery_attempt_status;

                ALTER TABLE notification_delivery_attempt
                ADD CONSTRAINT ck_notification_delivery_attempt_status
                CHECK (status IN ('prepared','sent','failed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE notification_delivery_attempt
                SET status = 'failed',
                    error = COALESCE(error, 'delivery_ambiguous')
                WHERE status = 'prepared';

                ALTER TABLE notification_delivery_attempt
                DROP CONSTRAINT IF EXISTS ck_notification_delivery_attempt_status;

                ALTER TABLE notification_delivery_attempt
                ADD CONSTRAINT ck_notification_delivery_attempt_status
                CHECK (status IN ('sent','failed'));
                """);
        }
    }
}
