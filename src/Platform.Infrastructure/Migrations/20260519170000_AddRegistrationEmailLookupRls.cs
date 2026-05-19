using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260519170000_AddRegistrationEmailLookupRls")]
public partial class AddRegistrationEmailLookupRls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS tenant_isolation ON tenant;
            CREATE POLICY tenant_isolation ON tenant
                USING (id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);

            DROP POLICY IF EXISTS user_account_tenant_isolation ON user_account;
            CREATE POLICY user_account_tenant_isolation ON user_account
                USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);

            DROP POLICY IF EXISTS tenant_registration_email_lookup_select ON tenant;
            CREATE POLICY tenant_registration_email_lookup_select ON tenant
                FOR SELECT
                USING (current_setting('app.registration_email_lookup', true) = 'on');

            DROP POLICY IF EXISTS user_account_registration_email_lookup_select ON user_account;
            CREATE POLICY user_account_registration_email_lookup_select ON user_account
                FOR SELECT
                USING (current_setting('app.registration_email_lookup', true) = 'on');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS user_account_registration_email_lookup_select ON user_account;
            DROP POLICY IF EXISTS tenant_registration_email_lookup_select ON tenant;

            DROP POLICY IF EXISTS user_account_tenant_isolation ON user_account;
            CREATE POLICY user_account_tenant_isolation ON user_account
                USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

            DROP POLICY IF EXISTS tenant_isolation ON tenant;
            CREATE POLICY tenant_isolation ON tenant
                USING (id = current_setting('app.current_tenant_id')::uuid)
                WITH CHECK (id = current_setting('app.current_tenant_id')::uuid);
            """);
    }
}
