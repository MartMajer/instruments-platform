using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scoring_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    rule_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    engine_min_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    produces = table.Column<string>(type: "jsonb", nullable: false),
                    compatibility = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scoring_rule", x => x.id);
                    table.CheckConstraint("ck_scoring_rule_compatibility_object", "jsonb_typeof(compatibility) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_document_hash", "document_hash ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("ck_scoring_rule_document_object", "jsonb_typeof(document) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_produces_object", "jsonb_typeof(produces) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_publish_shape", "(status = 'published' AND published_at IS NOT NULL AND is_locked = TRUE) OR (status <> 'published')");
                    table.CheckConstraint("ck_scoring_rule_status", "status IN ('draft','published','retired')");
                    table.ForeignKey(
                        name: "fk_scoring_rule_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scoring_rule_user_account_published_by",
                        column: x => x.published_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_document_hash",
                table: "scoring_rule",
                column: "document_hash");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_published_by",
                table: "scoring_rule",
                column: "published_by");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "rule_key", "rule_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_status",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "status" });

            migrationBuilder.Sql(
                """
                ALTER TABLE scoring_rule ENABLE ROW LEVEL SECURITY;
                ALTER TABLE scoring_rule FORCE ROW LEVEL SECURITY;

                CREATE POLICY scoring_rule_tenant_or_global_read ON scoring_rule
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scoring_rule.template_version_id
                              AND (
                                  tv.is_global = TRUE
                                  OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                              )
                        )
                    );

                CREATE POLICY scoring_rule_tenant_write ON scoring_rule
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scoring_rule.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scoring_rule.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS scoring_rule_tenant_write ON scoring_rule;
                DROP POLICY IF EXISTS scoring_rule_tenant_or_global_read ON scoring_rule;
                """);

            migrationBuilder.DropTable(
                name: "scoring_rule");
        }
    }
}
