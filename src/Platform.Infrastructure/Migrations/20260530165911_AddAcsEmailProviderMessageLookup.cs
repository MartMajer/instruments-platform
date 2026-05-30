using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcsEmailProviderMessageLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_notification_delivery_attempt_provider_message_id",
                table: "notification_delivery_attempt",
                columns: new[] { "provider", "provider_message_id" },
                unique: true,
                filter: "provider_message_id IS NOT NULL");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION resolve_notification_delivery_attempt_by_provider_message_id(
                    p_provider text,
                    p_provider_message_id text)
                RETURNS TABLE(tenant_id uuid, notification_id uuid, delivery_attempt_id uuid)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = public
                SET row_security = off
                AS $$
                    SELECT attempt.tenant_id, attempt.notification_id, attempt.id
                    FROM notification_delivery_attempt AS attempt
                    WHERE attempt.provider = p_provider
                      AND attempt.provider_message_id = p_provider_message_id
                    LIMIT 1;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS resolve_notification_delivery_attempt_by_provider_message_id(text, text);
                """);

            migrationBuilder.DropIndex(
                name: "ux_notification_delivery_attempt_provider_message_id",
                table: "notification_delivery_attempt");
        }
    }
}
