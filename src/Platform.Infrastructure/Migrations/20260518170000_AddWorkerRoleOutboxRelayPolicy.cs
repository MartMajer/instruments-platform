using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260518170000_AddWorkerRoleOutboxRelayPolicy")]
public partial class AddWorkerRoleOutboxRelayPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_roles
                    WHERE rolname = 'platform_worker'
                ) THEN
                    CREATE ROLE platform_worker;
                END IF;
            END
            $$;

            GRANT SELECT, UPDATE ON TABLE outbox_event TO platform_worker;

            DROP POLICY IF EXISTS outbox_event_tenant_isolation ON outbox_event;
            CREATE POLICY outbox_event_tenant_isolation ON outbox_event
                USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);

            DROP POLICY IF EXISTS outbox_event_worker_relay_select ON outbox_event;
            CREATE POLICY outbox_event_worker_relay_select ON outbox_event
                FOR SELECT
                TO platform_worker
                USING (
                    published_at IS NULL
                    AND (next_retry_at IS NULL OR next_retry_at <= now())
                    AND (last_error IS NULL OR left(last_error, 12) <> 'DEAD_LETTER:')
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS outbox_event_worker_relay_select ON outbox_event;
            DROP POLICY IF EXISTS outbox_event_tenant_isolation ON outbox_event;
            CREATE POLICY outbox_event_tenant_isolation ON outbox_event
                USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
            REVOKE SELECT, UPDATE ON TABLE outbox_event FROM platform_worker;
            """);
    }
}
