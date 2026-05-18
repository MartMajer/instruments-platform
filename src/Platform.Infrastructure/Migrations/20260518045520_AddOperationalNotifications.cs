using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operational_notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_aggregate_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operational_notification", x => x.id);
                    table.CheckConstraint("ck_operational_notification_payload_object", "jsonb_typeof(payload_json) = 'object'");
                    table.CheckConstraint("ck_operational_notification_severity", "severity IN ('info','warning')");
                    table.CheckConstraint("ck_operational_notification_status", "status IN ('unread')");
                    table.ForeignKey(
                        name: "fk_operational_notification_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operational_notification_tenant_id_status_created_at",
                table: "operational_notification",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_operational_notification_source",
                table: "operational_notification",
                columns: new[] { "tenant_id", "source_aggregate_id", "source_event_type", "notification_type" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE operational_notification ENABLE ROW LEVEL SECURITY;

                CREATE POLICY operational_notification_tenant_isolation ON operational_notification
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS operational_notification_tenant_isolation ON operational_notification;
                """);

            migrationBuilder.DropTable(
                name: "operational_notification");
        }
    }
}
