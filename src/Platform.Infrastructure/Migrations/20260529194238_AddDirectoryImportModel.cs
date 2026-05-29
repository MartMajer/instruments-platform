using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryImportModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "directory_connection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    primary_domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    granted_scopes_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_successful_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_connection", x => x.id);
                    table.UniqueConstraint("ak_directory_connection_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.CheckConstraint("ck_directory_connection_provider", "provider IN ('microsoft_graph')");
                    table.CheckConstraint("ck_directory_connection_status", "status IN ('active','revoked','failed')");
                    table.ForeignKey(
                        name: "fk_directory_connection_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_import_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    criteria_json = table.Column<string>(type: "jsonb", nullable: false),
                    field_selection_json = table.Column<string>(type: "jsonb", nullable: false),
                    mirror_mode = table.Column<bool>(type: "boolean", nullable: false),
                    mirror_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_import_rule", x => x.id);
                    table.UniqueConstraint("ak_directory_import_rule_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.CheckConstraint("ck_directory_import_rule_mirror_confirmed", "mirror_mode = FALSE OR mirror_confirmed_at IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_directory_import_rule_directory_connection_connection_id_tenant_id",
                        columns: x => new { x.connection_id, x.tenant_id },
                        principalTable: "directory_connection",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_rule_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_import_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    summary_json = table.Column<string>(type: "jsonb", nullable: false),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_import_run", x => x.id);
                    table.UniqueConstraint("ak_directory_import_run_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.CheckConstraint("ck_directory_import_run_finished_range", "finished_at IS NULL OR finished_at >= started_at");
                    table.CheckConstraint("ck_directory_import_run_mode", "mode IN ('preview','apply')");
                    table.CheckConstraint("ck_directory_import_run_status", "status IN ('planned','previewed','applying','applied','failed')");
                    table.ForeignKey(
                        name: "fk_directory_import_run_directory_import_rule_rule_id_tenant_id",
                        columns: x => new { x.rule_id, x.tenant_id },
                        principalTable: "directory_import_rule",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_run_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_run_user_account_created_by_user_id_tenant_id",
                        columns: x => new { x.created_by_user_id, x.tenant_id },
                        principalTable: "user_account",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_import_run_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_object_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_object_id_hash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    issue_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    safe_summary_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_import_run_item", x => x.id);
                    table.CheckConstraint("ck_directory_import_run_item_action", "action IN ('create_subject','update_subject','create_group','add_membership','set_manager','deactivate_subject','no_change','warning')");
                    table.CheckConstraint("ck_directory_import_run_item_status", "status IN ('planned','applied','skipped','warning','failed')");
                    table.ForeignKey(
                        name: "fk_directory_import_run_item_directory_import_run_run_id_tenant_id",
                        columns: x => new { x.run_id, x.tenant_id },
                        principalTable: "directory_import_run",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_run_item_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_tenant_id",
                table: "directory_connection",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_tenant_id_provider_external_tenant_id",
                table: "directory_connection",
                columns: new[] { "tenant_id", "provider", "external_tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_rule_connection_id_tenant_id",
                table: "directory_import_rule",
                columns: new[] { "connection_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_rule_tenant_id",
                table: "directory_import_rule",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_rule_tenant_id_connection_id_name",
                table: "directory_import_rule",
                columns: new[] { "tenant_id", "connection_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_created_by_user_id_tenant_id",
                table: "directory_import_run",
                columns: new[] { "created_by_user_id", "tenant_id" },
                filter: "created_by_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_rule_id_tenant_id_started_at",
                table: "directory_import_run",
                columns: new[] { "rule_id", "tenant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_tenant_id",
                table: "directory_import_run",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_item_run_id_tenant_id",
                table: "directory_import_run_item",
                columns: new[] { "run_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_item_tenant_id",
                table: "directory_import_run_item",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_item_tenant_id_action",
                table: "directory_import_run_item",
                columns: new[] { "tenant_id", "action" });

            migrationBuilder.Sql(
                """
                ALTER TABLE directory_connection ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_connection FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_connection_tenant_isolation ON directory_connection
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE directory_import_rule ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_rule FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_import_rule_tenant_isolation ON directory_import_rule
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE directory_import_run ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_run FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_import_run_tenant_isolation ON directory_import_run
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE directory_import_run_item ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_run_item FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_import_run_item_tenant_isolation ON directory_import_run_item
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS directory_import_run_item_tenant_isolation ON directory_import_run_item;
                ALTER TABLE directory_import_run_item NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_run_item DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS directory_import_run_tenant_isolation ON directory_import_run;
                ALTER TABLE directory_import_run NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_run DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS directory_import_rule_tenant_isolation ON directory_import_rule;
                ALTER TABLE directory_import_rule NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE directory_import_rule DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS directory_connection_tenant_isolation ON directory_connection;
                ALTER TABLE directory_connection NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE directory_connection DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.DropTable(
                name: "directory_import_run_item");

            migrationBuilder.DropTable(
                name: "directory_import_run");

            migrationBuilder.DropTable(
                name: "directory_import_rule");

            migrationBuilder.DropTable(
                name: "directory_connection");
        }
    }
}
