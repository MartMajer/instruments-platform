using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    template_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.CheckConstraint("ck_notification_channel", "channel IN ('email','sms')");
                    table.CheckConstraint("ck_notification_status", "status IN ('queued','sent','failed','bounced')");
                    table.ForeignKey(
                        name: "fk_notification_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_assignment_id",
                table: "notification",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_campaign_id",
                table: "notification",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_status_scheduled_for",
                table: "notification",
                columns: new[] { "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id_campaign_id",
                table: "notification",
                columns: new[] { "tenant_id", "campaign_id" });

            migrationBuilder.Sql(
                """
                ALTER TABLE notification ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notification FORCE ROW LEVEL SECURITY;

                CREATE POLICY notification_tenant_isolation ON notification
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = notification.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM assignment AS a
                            WHERE a.id = notification.assignment_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND a.campaign_id = notification.campaign_id
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = notification.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM assignment AS a
                            WHERE a.id = notification.assignment_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND a.campaign_id = notification.campaign_id
                        )
                    );

                CREATE OR REPLACE FUNCTION notification_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        JOIN assignment AS a ON a.id = NEW.assignment_id
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                          AND a.tenant_id = NEW.tenant_id
                          AND a.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'notification campaign and assignment must belong to the same tenant campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER notification_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, assignment_id
                    ON notification
                    FOR EACH ROW
                    EXECUTE FUNCTION notification_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS notification_tenant_guard ON notification;
                DROP FUNCTION IF EXISTS notification_tenant_guard();
                DROP POLICY IF EXISTS notification_tenant_isolation ON notification;
                """);

            migrationBuilder.DropTable(
                name: "notification");
        }
    }
}
