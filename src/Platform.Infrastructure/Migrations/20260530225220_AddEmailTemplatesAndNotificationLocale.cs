using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplatesAndNotificationLocale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "notification",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "email_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subject = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body_text = table.Column<string>(type: "text", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_template", x => x.id);
                    table.CheckConstraint("ck_email_template_body_text_length", "length(trim(body_text)) BETWEEN 80 AND 4000");
                    table.CheckConstraint("ck_email_template_code", "template_code IN ('invitation','reminder')");
                    table.CheckConstraint("ck_email_template_locale", "locale IN ('en','hr-HR')");
                    table.CheckConstraint("ck_email_template_status", "status IN ('active')");
                    table.CheckConstraint("ck_email_template_subject_length", "length(trim(subject)) BETWEEN 1 AND 160");
                    table.ForeignKey(
                        name: "fk_email_template_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_email_template_tenant_code_locale",
                table: "email_template",
                columns: new[] { "tenant_id", "template_code", "locale" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE email_template ENABLE ROW LEVEL SECURITY;
                ALTER TABLE email_template FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS email_template_tenant_isolation ON email_template;
                CREATE POLICY email_template_tenant_isolation ON email_template
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS email_template_tenant_isolation ON email_template;
                ALTER TABLE email_template NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE email_template DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.DropTable(
                name: "email_template");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "notification");
        }
    }
}
