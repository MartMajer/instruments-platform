using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_delivery_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery_event", x => x.id);
                    table.CheckConstraint("ck_notification_delivery_event_type", "event_type IN ('accepted','delivered','bounced','complained')");
                    table.ForeignKey(
                        name: "fk_notification_delivery_event_attempt_delivery_attempt_id",
                        column: x => x.delivery_attempt_id,
                        principalTable: "notification_delivery_attempt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_delivery_event_notification_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_delivery_event_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_event_tenant_id_notification_id",
                table: "notification_delivery_event",
                columns: new[] { "tenant_id", "notification_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_event_tenant_id_delivery_attempt_id",
                table: "notification_delivery_event",
                columns: new[] { "tenant_id", "delivery_attempt_id" });

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_notification_delivery_event_tenant_provider_event_id
                ON notification_delivery_event (tenant_id, provider, provider_event_id)
                WHERE provider_event_id IS NOT NULL;
                """);
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_notification_delivery_event_tenant_attempt_type_without_provider_id
                ON notification_delivery_event (tenant_id, delivery_attempt_id, event_type)
                WHERE provider_event_id IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_delivery_event");
        }
    }
}
