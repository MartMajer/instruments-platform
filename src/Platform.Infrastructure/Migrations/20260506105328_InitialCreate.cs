using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    mfa_secret = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_account_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignment", x => x.id);
                    table.CheckConstraint("ck_role_assignment_scope", "(scope_type = 'tenant' AND scope_id IS NULL)\nOR (scope_type IN ('workspace', 'campaign', 'campaign_series') AND scope_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_role_assignment_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_user_account_granted_by",
                        column: x => x.granted_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_user_account_user_id",
                        column: x => x.user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_permission_code",
                table: "permission",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_global_code",
                table: "role",
                column: "code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_role_tenant_id_code",
                table: "role",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_granted_by",
                table: "role_assignment",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_role_id",
                table: "role_assignment",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_tenant_id",
                table: "role_assignment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_user_id",
                table: "role_assignment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_slug",
                table: "tenant",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_account_tenant_id_email",
                table: "user_account",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE tenant ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation ON tenant
                    USING (id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE user_account ENABLE ROW LEVEL SECURITY;
                ALTER TABLE user_account FORCE ROW LEVEL SECURITY;

                CREATE POLICY user_account_tenant_isolation ON user_account
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE role ENABLE ROW LEVEL SECURITY;
                ALTER TABLE role FORCE ROW LEVEL SECURITY;

                CREATE POLICY role_tenant_read ON role
                    FOR SELECT
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        OR tenant_id IS NULL
                    );

                CREATE POLICY role_tenant_insert ON role
                    FOR INSERT
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE POLICY role_tenant_update ON role
                    FOR UPDATE
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE POLICY role_tenant_delete ON role
                    FOR DELETE
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE role_permission ENABLE ROW LEVEL SECURITY;
                ALTER TABLE role_permission FORCE ROW LEVEL SECURITY;

                CREATE POLICY role_permission_tenant_read ON role_permission
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_permission.role_id
                              AND (
                                  r.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  OR r.tenant_id IS NULL
                              )
                        )
                    );

                CREATE POLICY role_permission_tenant_insert ON role_permission
                    FOR INSERT
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_permission.role_id
                              AND r.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE POLICY role_permission_tenant_update ON role_permission
                    FOR UPDATE
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_permission.role_id
                              AND r.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_permission.role_id
                              AND r.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE POLICY role_permission_tenant_delete ON role_permission
                    FOR DELETE
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_permission.role_id
                              AND r.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE role_assignment ENABLE ROW LEVEL SECURITY;
                ALTER TABLE role_assignment FORCE ROW LEVEL SECURITY;

                CREATE POLICY role_assignment_tenant_isolation ON role_assignment
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (
                        role_assignment.tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM user_account AS u
                            WHERE u.id = role_assignment.user_id
                              AND u.tenant_id = role_assignment.tenant_id
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM role AS r
                            WHERE r.id = role_assignment.role_id
                              AND (
                                  r.tenant_id = role_assignment.tenant_id
                                  OR r.tenant_id IS NULL
                              )
                        )
                        AND (
                            role_assignment.granted_by IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM user_account AS grantor
                                WHERE grantor.id = role_assignment.granted_by
                                  AND grantor.tenant_id = role_assignment.tenant_id
                            )
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_assignment");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "user_account");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "tenant");
        }
    }
}
