using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518123000_AddWithdrawalRequestTokens")]
    public partial class AddWithdrawalRequestTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "withdrawal_request_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_request_token", x => x.id);
                    table.CheckConstraint("ck_withdrawal_request_token_action", "requested_action IN ('delete', 'anonymize')");
                    table.CheckConstraint("ck_withdrawal_request_token_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_withdrawal_request_token_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_request_token_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_response_session_id",
                table: "withdrawal_request_token",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_tenant_id",
                table: "withdrawal_request_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_tenant_id_expires_at",
                table: "withdrawal_request_token",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_token_hash",
                table: "withdrawal_request_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_request_token ENABLE ROW LEVEL SECURITY;
                ALTER TABLE withdrawal_request_token FORCE ROW LEVEL SECURITY;

                CREATE POLICY withdrawal_request_token_tenant_isolation ON withdrawal_request_token
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION withdrawal_request_token_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM response_session rs
                        WHERE rs.id = NEW.response_session_id
                          AND rs.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal request token target must belong to the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER withdrawal_request_token_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, response_session_id
                    ON withdrawal_request_token
                    FOR EACH ROW
                    EXECUTE FUNCTION withdrawal_request_token_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS withdrawal_request_token_tenant_guard ON withdrawal_request_token;
                DROP FUNCTION IF EXISTS withdrawal_request_token_tenant_guard();
                DROP POLICY IF EXISTS withdrawal_request_token_tenant_isolation ON withdrawal_request_token;
                """);

            migrationBuilder.DropTable(
                name: "withdrawal_request_token");
        }
    }
}
