using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE audit_event (
                    id UUID NOT NULL,
                    occurred_at TIMESTAMPTZ NOT NULL,
                    tenant_id UUID NOT NULL,
                    actor_id UUID NULL,
                    actor_type VARCHAR(32) NOT NULL,
                    correlation_id UUID NULL,
                    entity_type VARCHAR(256) NOT NULL,
                    entity_id TEXT NOT NULL,
                    change_kind VARCHAR(32) NOT NULL,
                    before JSONB NULL,
                    after JSONB NULL,
                    reason TEXT NULL,
                    CONSTRAINT pk_audit_event PRIMARY KEY (id, occurred_at)
                ) PARTITION BY RANGE (occurred_at);

                CREATE TABLE audit_event_default PARTITION OF audit_event DEFAULT;

                CREATE INDEX ix_audit_event_tenant_id_occurred_at
                    ON audit_event (tenant_id, occurred_at);

                CREATE INDEX ix_audit_event_entity_type_entity_id_occurred_at
                    ON audit_event (entity_type, entity_id, occurred_at);

                ALTER TABLE audit_event ENABLE ROW LEVEL SECURITY;
                ALTER TABLE audit_event FORCE ROW LEVEL SECURITY;

                CREATE POLICY audit_event_tenant_isolation ON audit_event
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION prevent_audit_event_update_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_event is append-only';
                END;
                $$;

                CREATE TRIGGER audit_event_prevent_update_delete
                    BEFORE UPDATE OR DELETE ON audit_event
                    FOR EACH ROW
                    EXECUTE FUNCTION prevent_audit_event_update_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS audit_event_prevent_update_delete ON audit_event;
                DROP FUNCTION IF EXISTS prevent_audit_event_update_delete();
                DROP TABLE IF EXISTS audit_event_default;
                DROP TABLE IF EXISTS audit_event;
                """);
        }
    }
}
