using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportArtifactTargetScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_export_artifact_campaign_id_created_at",
                table: "export_artifact");

            migrationBuilder.DropIndex(
                name: "ix_export_artifact_tenant_id_campaign_id_created_at",
                table: "export_artifact");

            migrationBuilder.AlterColumn<Guid>(
                name: "campaign_id",
                table: "export_artifact",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "target_kind",
                table: "export_artifact",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE export_artifact
                SET target_kind = CASE
                    WHEN artifact_type = 'campaign_series_response_csv_codebook' THEN 'campaign_series'
                    WHEN artifact_type = 'report_proof_csv_codebook' THEN 'campaign'
                    ELSE 'campaign'
                END;

                UPDATE export_artifact
                SET campaign_id = NULL
                WHERE artifact_type = 'campaign_series_response_csv_codebook';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "target_kind",
                table: "export_artifact",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_export_artifact_campaign_id",
                table: "export_artifact",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "target_kind", "campaign_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_series_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "target_kind", "campaign_series_id", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_target_kind",
                table: "export_artifact",
                sql: "target_kind IN ('campaign','campaign_series')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_export_artifact_target_scope",
                table: "export_artifact",
                sql: "(target_kind = 'campaign' AND campaign_id IS NOT NULL)\nOR (target_kind = 'campaign_series' AND campaign_id IS NULL AND campaign_series_id IS NOT NULL)");

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS export_artifact_tenant_guard ON export_artifact;
                DROP FUNCTION IF EXISTS export_artifact_tenant_guard();
                DROP POLICY IF EXISTS export_artifact_tenant_isolation ON export_artifact;

                CREATE POLICY export_artifact_tenant_isolation ON export_artifact
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND (
                            (
                                export_artifact.target_kind = 'campaign'
                                AND export_artifact.campaign_id IS NOT NULL
                                AND EXISTS (
                                    SELECT 1
                                    FROM campaign AS c
                                    WHERE c.id = export_artifact.campaign_id
                                      AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                                )
                            )
                            OR (
                                export_artifact.target_kind = 'campaign_series'
                                AND export_artifact.campaign_series_id IS NOT NULL
                                AND EXISTS (
                                    SELECT 1
                                    FROM campaign_series AS cs
                                    WHERE cs.id = export_artifact.campaign_series_id
                                      AND cs.tenant_id = current_setting('app.current_tenant_id')::uuid
                                )
                            )
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND (
                            (
                                export_artifact.target_kind = 'campaign'
                                AND export_artifact.campaign_id IS NOT NULL
                                AND EXISTS (
                                    SELECT 1
                                    FROM campaign AS c
                                    WHERE c.id = export_artifact.campaign_id
                                      AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                                )
                            )
                            OR (
                                export_artifact.target_kind = 'campaign_series'
                                AND export_artifact.campaign_series_id IS NOT NULL
                                AND EXISTS (
                                    SELECT 1
                                    FROM campaign_series AS cs
                                    WHERE cs.id = export_artifact.campaign_series_id
                                      AND cs.tenant_id = current_setting('app.current_tenant_id')::uuid
                                )
                            )
                        )
                    );

                CREATE OR REPLACE FUNCTION export_artifact_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NEW.target_kind = 'campaign' THEN
                        IF NEW.campaign_id IS NULL OR NOT EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = NEW.campaign_id
                              AND c.tenant_id = NEW.tenant_id
                              AND (
                                  NEW.campaign_series_id IS NULL
                                  OR c.campaign_series_id = NEW.campaign_series_id
                              )
                        ) THEN
                            RAISE EXCEPTION 'campaign-targeted export artifact must belong to the same tenant and campaign series';
                        END IF;
                    ELSIF NEW.target_kind = 'campaign_series' THEN
                        IF NEW.campaign_id IS NOT NULL
                           OR NEW.campaign_series_id IS NULL
                           OR NOT EXISTS (
                               SELECT 1
                               FROM campaign_series AS cs
                               WHERE cs.id = NEW.campaign_series_id
                                 AND cs.tenant_id = NEW.tenant_id
                           ) THEN
                            RAISE EXCEPTION 'campaign-series-targeted export artifact must belong to the same tenant';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'export artifact target kind is unknown';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER export_artifact_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, target_kind, campaign_id, campaign_series_id
                    ON export_artifact
                    FOR EACH ROW
                    EXECUTE FUNCTION export_artifact_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_export_artifact_campaign_id",
                table: "export_artifact");

            migrationBuilder.DropIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_id_created_at",
                table: "export_artifact");

            migrationBuilder.DropIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_series_id_created_at",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_target_kind",
                table: "export_artifact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_export_artifact_target_scope",
                table: "export_artifact");

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS export_artifact_tenant_guard ON export_artifact;
                DROP FUNCTION IF EXISTS export_artifact_tenant_guard();
                DROP POLICY IF EXISTS export_artifact_tenant_isolation ON export_artifact;

                UPDATE export_artifact AS ea
                SET campaign_id = (
                    SELECT c.id
                    FROM campaign AS c
                    WHERE c.campaign_series_id = ea.campaign_series_id
                      AND c.tenant_id = ea.tenant_id
                    ORDER BY c.created_at DESC, c.id
                    LIMIT 1
                )
                WHERE ea.campaign_id IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "target_kind",
                table: "export_artifact");

            migrationBuilder.AlterColumn<Guid>(
                name: "campaign_id",
                table: "export_artifact",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "campaign_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "campaign_id", "created_at" });

            migrationBuilder.Sql(
                """
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
    }
}
