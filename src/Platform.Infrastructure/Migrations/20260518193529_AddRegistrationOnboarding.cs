using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_intent",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    organization_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_intent", x => x.id);
                    table.CheckConstraint("ck_registration_intent_consumed_shape", "(status = 'pending' AND consumed_at IS NULL AND consumed_tenant_id IS NULL) OR (status = 'consumed' AND consumed_at IS NOT NULL AND consumed_tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_registration_intent_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_registration_intent_status", "status IN ('pending','consumed')");
                    table.ForeignKey(
                        name: "fk_registration_intent_tenant_consumed_tenant_id",
                        column: x => x.consumed_tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_consumed_tenant_id",
                table: "registration_intent",
                column: "consumed_tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_email",
                table: "registration_intent",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_registration_token_hash",
                table: "registration_intent",
                column: "registration_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_status_expires_at",
                table: "registration_intent",
                columns: new[] { "status", "expires_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_intent");
        }
    }
}
