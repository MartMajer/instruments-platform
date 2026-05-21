using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521223000_AddEmailDeliveryRlsPolicies")]
    public partial class AddEmailDeliveryRlsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE email_suppression ENABLE ROW LEVEL SECURITY;
                ALTER TABLE email_suppression FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS email_suppression_tenant_isolation ON email_suppression;
                CREATE POLICY email_suppression_tenant_isolation ON email_suppression
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE notification_delivery_event ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notification_delivery_event FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS notification_delivery_event_tenant_isolation ON notification_delivery_event;
                CREATE POLICY notification_delivery_event_tenant_isolation ON notification_delivery_event
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM notification AS n
                            WHERE n.id = notification_delivery_event.notification_id
                              AND n.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM notification_delivery_attempt AS a
                            WHERE a.id = notification_delivery_event.delivery_attempt_id
                              AND a.notification_id = notification_delivery_event.notification_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM notification AS n
                            WHERE n.id = notification_delivery_event.notification_id
                              AND n.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM notification_delivery_attempt AS a
                            WHERE a.id = notification_delivery_event.delivery_attempt_id
                              AND a.notification_id = notification_delivery_event.notification_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE OR REPLACE FUNCTION notification_delivery_event_tenant_guard()
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
                        RAISE EXCEPTION 'notification delivery event must belong to the same tenant notification';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM notification_delivery_attempt AS a
                        WHERE a.id = NEW.delivery_attempt_id
                          AND a.notification_id = NEW.notification_id
                          AND a.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'notification delivery event must belong to the same tenant delivery attempt';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                DROP TRIGGER IF EXISTS notification_delivery_event_tenant_guard ON notification_delivery_event;
                CREATE TRIGGER notification_delivery_event_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, notification_id, delivery_attempt_id
                    ON notification_delivery_event
                    FOR EACH ROW
                    EXECUTE FUNCTION notification_delivery_event_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS notification_delivery_event_tenant_guard ON notification_delivery_event;
                DROP FUNCTION IF EXISTS notification_delivery_event_tenant_guard();
                DROP POLICY IF EXISTS notification_delivery_event_tenant_isolation ON notification_delivery_event;
                ALTER TABLE notification_delivery_event NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE notification_delivery_event DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS email_suppression_tenant_isolation ON email_suppression;
                ALTER TABLE email_suppression NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE email_suppression DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}
