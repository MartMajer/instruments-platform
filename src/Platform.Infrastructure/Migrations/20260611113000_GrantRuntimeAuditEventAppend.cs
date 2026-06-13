using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260611113000_GrantRuntimeAuditEventAppend")]
    public partial class GrantRuntimeAuditEventAppend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_roles
                        WHERE rolname = 'platform_app_runtime'
                    ) THEN
                        GRANT SELECT, INSERT ON TABLE audit_event TO platform_app_runtime;
                        GRANT SELECT, INSERT ON TABLE audit_event_default TO platform_app_runtime;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_roles
                        WHERE rolname = 'platform_app_runtime'
                    ) THEN
                        REVOKE SELECT, INSERT ON TABLE audit_event_default FROM platform_app_runtime;
                        REVOKE SELECT, INSERT ON TABLE audit_event FROM platform_app_runtime;
                    END IF;
                END
                $$;
                """);
        }
    }
}
