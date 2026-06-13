using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryIntegrationPersistence : Migration
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
                    external_tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    primary_domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    granted_scopes = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_consent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_successful_import_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_connection", x => x.id);
                    table.UniqueConstraint("ak_directory_connection_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.CheckConstraint("ck_directory_connection_active_shape", "status <> 'active' OR external_tenant_id IS NOT NULL");
                    table.CheckConstraint("ck_directory_connection_provider", "provider IN ('microsoft_graph')");
                    table.CheckConstraint("ck_directory_connection_status", "status IN ('pending_consent','active','consent_required','revoked','failed','disconnected')");
                    table.ForeignKey(
                        name: "fk_directory_connection_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_connection_user_account_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_connection_consent_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    directory_connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    nonce_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_scopes = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_connection_consent_request", x => x.id);
                    table.CheckConstraint("ck_directory_connection_consent_request_completed_at", "completed_at IS NULL OR completed_at >= created_at");
                    table.CheckConstraint("ck_directory_connection_consent_request_expires_at", "expires_at > created_at");
                    table.CheckConstraint("ck_directory_connection_consent_request_provider", "provider IN ('microsoft_graph')");
                    table.CheckConstraint("ck_directory_connection_consent_request_status", "status IN ('pending','completed','failed','expired')");
                    table.ForeignKey(
                        name: "fk_directory_connection_consent_request_connection_id_tenant_id",
                        columns: x => new { x.directory_connection_id, x.tenant_id },
                        principalTable: "directory_connection",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_connection_consent_request_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_connection_consent_request_user_account_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_import_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    directory_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    rule_document = table.Column<string>(type: "jsonb", nullable: false),
                    retained_fields = table.Column<string>(type: "jsonb", nullable: false),
                    stale_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_import_rule", x => x.id);
                    table.UniqueConstraint("ak_directory_import_rule_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.CheckConstraint("ck_directory_import_rule_stale_policy", "stale_policy IN ('none','mark_stale')");
                    table.CheckConstraint("ck_directory_import_rule_status", "status IN ('draft','active','archived')");
                    table.ForeignKey(
                        name: "fk_directory_import_rule_connection_id_tenant_id",
                        columns: x => new { x.directory_connection_id, x.tenant_id },
                        principalTable: "directory_connection",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_rule_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_rule_user_account_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "directory_import_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    directory_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    directory_import_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    preview_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rule_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    retained_fields = table.Column<string>(type: "jsonb", nullable: false),
                    counts = table.Column<string>(type: "jsonb", nullable: false),
                    warning_categories = table.Column<string>(type: "jsonb", nullable: false),
                    error_category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    checkpoint = table.Column<string>(type: "jsonb", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directory_import_run", x => x.id);
                    table.CheckConstraint("ck_directory_import_run_apply_preview", "mode <> 'apply' OR preview_run_id IS NOT NULL");
                    table.CheckConstraint("ck_directory_import_run_completed_at", "completed_at IS NULL OR completed_at >= created_at");
                    table.CheckConstraint("ck_directory_import_run_mode", "mode IN ('preview','apply')");
                    table.CheckConstraint("ck_directory_import_run_status", "status IN ('queued','running','succeeded','failed','canceled')");
                    table.ForeignKey(
                        name: "fk_directory_import_run_connection_id_tenant_id",
                        columns: x => new { x.directory_connection_id, x.tenant_id },
                        principalTable: "directory_connection",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_run_preview_run_id",
                        column: x => x.preview_run_id,
                        principalTable: "directory_import_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_directory_import_run_rule_id_tenant_id",
                        columns: x => new { x.directory_import_rule_id, x.tenant_id },
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
                        name: "fk_directory_import_run_user_account_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_directory_connection_created_by_user_id",
                table: "directory_connection",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_tenant_id_provider_active",
                table: "directory_connection",
                columns: new[] { "tenant_id", "provider" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_tenant_id_status",
                table: "directory_connection",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_consent_request_connection_id_tenant_id",
                table: "directory_connection_consent_request",
                columns: new[] { "directory_connection_id", "tenant_id" },
                filter: "directory_connection_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_directory_connection_consent_request_created_by_user_id",
                table: "directory_connection_consent_request",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_consent_request_state_hash",
                table: "directory_connection_consent_request",
                column: "state_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_directory_connection_consent_request_tenant_id_status_expires_at",
                table: "directory_connection_consent_request",
                columns: new[] { "tenant_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_directory_import_rule_created_by_user_id",
                table: "directory_import_rule",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_directory_import_rule_directory_connection_id_tenant_id",
                table: "directory_import_rule",
                columns: new[] { "directory_connection_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_rule_tenant_id_connection_id_status",
                table: "directory_import_rule",
                columns: new[] { "tenant_id", "directory_connection_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_rule_tenant_id_name",
                table: "directory_import_rule",
                columns: new[] { "tenant_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_directory_import_run_directory_connection_id_tenant_id",
                table: "directory_import_run",
                columns: new[] { "directory_connection_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_directory_import_run_directory_import_rule_id_tenant_id",
                table: "directory_import_run",
                columns: new[] { "directory_import_rule_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_preview_run_id",
                table: "directory_import_run",
                column: "preview_run_id",
                filter: "preview_run_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_directory_import_run_requested_by_user_id",
                table: "directory_import_run",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_tenant_id_connection_id_created_at",
                table: "directory_import_run",
                columns: new[] { "tenant_id", "directory_connection_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_tenant_id_rule_id_created_at",
                table: "directory_import_run",
                columns: new[] { "tenant_id", "directory_import_rule_id", "created_at" },
                filter: "directory_import_rule_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_directory_import_run_tenant_id_status_created_at",
                table: "directory_import_run",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.Sql(
                """
                ALTER TABLE directory_connection ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_connection FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_connection_tenant_isolation ON directory_connection
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE directory_connection_consent_request ENABLE ROW LEVEL SECURITY;
                ALTER TABLE directory_connection_consent_request FORCE ROW LEVEL SECURITY;

                CREATE POLICY directory_connection_consent_request_tenant_isolation ON directory_connection_consent_request
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
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_roles
                        WHERE rolname = 'platform_app_runtime'
                    ) THEN
                        GRANT SELECT, INSERT, UPDATE ON TABLE directory_connection TO platform_app_runtime;
                        GRANT SELECT, INSERT, UPDATE ON TABLE directory_connection_consent_request TO platform_app_runtime;
                        GRANT SELECT, INSERT, UPDATE ON TABLE directory_import_rule TO platform_app_runtime;
                        GRANT SELECT, INSERT, UPDATE ON TABLE directory_import_run TO platform_app_runtime;
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
                        REVOKE SELECT, INSERT, UPDATE ON TABLE directory_import_run FROM platform_app_runtime;
                        REVOKE SELECT, INSERT, UPDATE ON TABLE directory_import_rule FROM platform_app_runtime;
                        REVOKE SELECT, INSERT, UPDATE ON TABLE directory_connection_consent_request FROM platform_app_runtime;
                        REVOKE SELECT, INSERT, UPDATE ON TABLE directory_connection FROM platform_app_runtime;
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropTable(
                name: "directory_connection_consent_request");

            migrationBuilder.DropTable(
                name: "directory_import_run");

            migrationBuilder.DropTable(
                name: "directory_import_rule");

            migrationBuilder.DropTable(
                name: "directory_connection");
        }
    }
}
