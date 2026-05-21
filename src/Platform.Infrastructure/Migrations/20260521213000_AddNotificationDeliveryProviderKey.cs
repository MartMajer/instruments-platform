using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryProviderKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider_delivery_key",
                table: "notification_delivery_attempt",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_notification_delivery_attempt_tenant_provider_delivery_key
                ON notification_delivery_attempt (tenant_id, provider_delivery_key)
                WHERE provider_delivery_key IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_notification_delivery_attempt_tenant_provider_delivery_key;
                """);

            migrationBuilder.DropColumn(
                name: "provider_delivery_key",
                table: "notification_delivery_attempt");
        }
    }
}
