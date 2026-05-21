using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryEventIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_event_delivery_attempt_id",
                table: "notification_delivery_event",
                column: "delivery_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_event_notification_id",
                table: "notification_delivery_event",
                column: "notification_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_event_delivery_attempt_id",
                table: "notification_delivery_event");

            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_event_notification_id",
                table: "notification_delivery_event");
        }
    }
}
