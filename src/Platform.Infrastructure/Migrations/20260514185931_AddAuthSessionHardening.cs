using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSessionHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_user_account_id_tenant_id",
                table: "user_account",
                columns: new[] { "id", "tenant_id" });

            migrationBuilder.CreateTable(
                name: "external_auth_identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_subject_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email_at_binding = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_auth_identity", x => x.id);
                    table.UniqueConstraint("ak_external_auth_identity_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_external_auth_identity_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_auth_identity_user_account_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalTable: "user_account",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_auth_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_session_external_auth_identity_external_auth_identity_id_tenant_id",
                        columns: x => new { x.external_auth_identity_id, x.tenant_id },
                        principalTable: "external_auth_identity",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auth_session_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auth_session_user_account_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalTable: "user_account",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_external_auth_identity_id_tenant_id",
                table: "auth_session",
                columns: new[] { "external_auth_identity_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_tenant_id_expires_at",
                table: "auth_session",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_tenant_id_user_id",
                table: "auth_session",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_user_id_tenant_id",
                table: "auth_session",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_provider_provider_subject_hash",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "provider", "provider_subject_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_user_id",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_user_id_provider",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_user_id_tenant_id",
                table: "external_auth_identity",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.Sql(
                """
                ALTER TABLE external_auth_identity ENABLE ROW LEVEL SECURITY;
                ALTER TABLE external_auth_identity FORCE ROW LEVEL SECURITY;

                CREATE POLICY external_auth_identity_tenant_isolation ON external_auth_identity
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE auth_session ENABLE ROW LEVEL SECURITY;
                ALTER TABLE auth_session FORCE ROW LEVEL SECURITY;

                CREATE POLICY auth_session_tenant_isolation ON auth_session
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS auth_session_tenant_isolation ON auth_session;
                DROP POLICY IF EXISTS external_auth_identity_tenant_isolation ON external_auth_identity;
                """);

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "external_auth_identity");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_user_account_id_tenant_id",
                table: "user_account");
        }
    }
}
