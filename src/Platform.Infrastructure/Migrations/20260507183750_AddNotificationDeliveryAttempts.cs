using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_delivery_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery_attempt", x => x.id);
                    table.CheckConstraint("ck_notification_delivery_attempt_status", "status IN ('sent','failed')");
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempt_notification_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempt_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_attempt_notification_id_created_at",
                table: "notification_delivery_attempt",
                columns: new[] { "notification_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_attempt_tenant_id_notification_id",
                table: "notification_delivery_attempt",
                columns: new[] { "tenant_id", "notification_id" });

            migrationBuilder.Sql(
                """
                ALTER TABLE notification_delivery_attempt ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notification_delivery_attempt FORCE ROW LEVEL SECURITY;

                CREATE POLICY notification_delivery_attempt_tenant_isolation ON notification_delivery_attempt
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM notification AS n
                            WHERE n.id = notification_delivery_attempt.notification_id
                              AND n.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM notification AS n
                            WHERE n.id = notification_delivery_attempt.notification_id
                              AND n.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE OR REPLACE FUNCTION notification_delivery_attempt_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM notification AS n
                        WHERE n.id = NEW.notification_id
                          AND n.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'notification delivery attempt must belong to the same tenant notification';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER notification_delivery_attempt_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, notification_id
                    ON notification_delivery_attempt
                    FOR EACH ROW
                    EXECUTE FUNCTION notification_delivery_attempt_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS notification_delivery_attempt_tenant_guard ON notification_delivery_attempt;
                DROP FUNCTION IF EXISTS notification_delivery_attempt_tenant_guard();
                DROP POLICY IF EXISTS notification_delivery_attempt_tenant_isolation ON notification_delivery_attempt;
                """);

            migrationBuilder.DropTable(
                name: "notification_delivery_attempt");
        }
    }
}
