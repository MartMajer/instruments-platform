using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_artifact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artifact_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false),
                    byte_size = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    codebook_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_artifact", x => x.id);
                    table.CheckConstraint("ck_export_artifact_byte_size_non_negative", "byte_size >= 0");
                    table.CheckConstraint("ck_export_artifact_checksum_sha256", "checksum_sha256 ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_export_artifact_codebook_object", "codebook_json IS NOT NULL AND jsonb_typeof(codebook_json) = 'object'");
                    table.CheckConstraint("ck_export_artifact_format", "format IN ('csv_codebook')");
                    table.CheckConstraint("ck_export_artifact_metadata_object", "metadata_json IS NOT NULL AND jsonb_typeof(metadata_json) = 'object'");
                    table.CheckConstraint("ck_export_artifact_row_count_non_negative", "row_count >= 0");
                    table.CheckConstraint("ck_export_artifact_status", "status IN ('succeeded')");
                    table.CheckConstraint("ck_export_artifact_type", "artifact_type IN ('report_proof_csv_codebook')");
                    table.ForeignKey(
                        name: "fk_export_artifact_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_artifact_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_artifact_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "campaign_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_export_artifact_campaign_series_id",
                table: "export_artifact",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "campaign_id", "created_at" });

            migrationBuilder.Sql(
                """
                ALTER TABLE export_artifact ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_artifact FORCE ROW LEVEL SECURITY;

                CREATE POLICY export_artifact_tenant_isolation ON export_artifact
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = export_artifact.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = export_artifact.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE OR REPLACE FUNCTION export_artifact_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                          AND (
                              NEW.campaign_series_id IS NULL
                              OR c.campaign_series_id = NEW.campaign_series_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'export artifact campaign must belong to the same tenant and campaign series';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER export_artifact_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, campaign_series_id
                    ON export_artifact
                    FOR EACH ROW
                    EXECUTE FUNCTION export_artifact_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS export_artifact_tenant_guard ON export_artifact;
                DROP FUNCTION IF EXISTS export_artifact_tenant_guard();
                DROP POLICY IF EXISTS export_artifact_tenant_isolation ON export_artifact;
                """);

            migrationBuilder.DropTable(
                name: "export_artifact");
        }
    }
}
