using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignLaunchSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaign_launch_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scoring_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_identity_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    template_question_count = table.Column<int>(type: "integer", nullable: false),
                    scoring_rule_document_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    launch_readiness = table.Column<string>(type: "jsonb", nullable: false),
                    launched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    launched_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_launch_snapshot", x => x.id);
                    table.CheckConstraint("ck_campaign_launch_snapshot_question_count_positive", "template_question_count > 0");
                    table.CheckConstraint("ck_campaign_launch_snapshot_readiness_object", "jsonb_typeof(launch_readiness) = 'object'");
                    table.CheckConstraint("ck_campaign_launch_snapshot_response_identity_mode", "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_scoring_rule_scoring_rule_id",
                        column: x => x.scoring_rule_id,
                        principalTable: "scoring_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_user_account_launched_by",
                        column: x => x.launched_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_campaign_id",
                table: "campaign_launch_snapshot",
                column: "campaign_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_campaign_series_id",
                table: "campaign_launch_snapshot",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_launched_by",
                table: "campaign_launch_snapshot",
                column: "launched_by");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_scoring_rule_id",
                table: "campaign_launch_snapshot",
                column: "scoring_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_template_version_id",
                table: "campaign_launch_snapshot",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_tenant_id",
                table: "campaign_launch_snapshot",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_tenant_id_launched_at",
                table: "campaign_launch_snapshot",
                columns: new[] { "tenant_id", "launched_at" });

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION campaign_launch_snapshot_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                          AND c.template_version_id = NEW.template_version_id
                          AND (
                              NEW.campaign_series_id IS NULL
                              OR c.campaign_series_id = NEW.campaign_series_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot must match campaign, template, and scoring rule tenant';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM scoring_rule AS sr
                        WHERE sr.id = NEW.scoring_rule_id
                          AND sr.template_version_id = NEW.template_version_id
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot scoring rule must match template version';
                    END IF;

                    IF NEW.launched_by IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM user_account AS u
                        WHERE u.id = NEW.launched_by
                          AND u.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot actor must be owned by the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER campaign_launch_snapshot_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, campaign_series_id, template_version_id, scoring_rule_id, launched_by
                    ON campaign_launch_snapshot
                    FOR EACH ROW
                    EXECUTE FUNCTION campaign_launch_snapshot_tenant_guard();

                CREATE OR REPLACE FUNCTION campaign_launch_snapshot_prevent_update_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'campaign launch snapshots are immutable';
                END;
                $$;

                CREATE TRIGGER campaign_launch_snapshot_prevent_update
                    BEFORE UPDATE ON campaign_launch_snapshot
                    FOR EACH ROW
                    EXECUTE FUNCTION campaign_launch_snapshot_prevent_update_delete();

                CREATE TRIGGER campaign_launch_snapshot_prevent_delete
                    BEFORE DELETE ON campaign_launch_snapshot
                    FOR EACH ROW
                    EXECUTE FUNCTION campaign_launch_snapshot_prevent_update_delete();

                ALTER TABLE campaign_launch_snapshot ENABLE ROW LEVEL SECURITY;
                ALTER TABLE campaign_launch_snapshot FORCE ROW LEVEL SECURITY;

                CREATE POLICY campaign_launch_snapshot_tenant_isolation ON campaign_launch_snapshot
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = campaign_launch_snapshot.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = campaign_launch_snapshot.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE OR REPLACE FUNCTION answer_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM response_session AS rs
                        JOIN assignment AS a ON a.id = rs.assignment_id
                        JOIN campaign AS c ON c.id = a.campaign_id
                        LEFT JOIN campaign_launch_snapshot AS cls ON cls.campaign_id = c.id
                        JOIN question AS q ON q.id = NEW.question_id
                        WHERE rs.id = NEW.session_id
                          AND rs.tenant_id = NEW.tenant_id
                          AND q.template_version_id = COALESCE(cls.template_version_id, c.template_version_id)
                    ) THEN
                        RAISE EXCEPTION 'answer session and question must belong to the same tenant campaign template';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION score_run_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM response_session AS rs
                        JOIN assignment AS a ON a.id = rs.assignment_id
                        JOIN campaign AS c ON c.id = a.campaign_id
                        LEFT JOIN campaign_launch_snapshot AS cls ON cls.campaign_id = c.id
                        JOIN scoring_rule AS sr ON sr.id = NEW.scoring_rule_id
                        WHERE rs.id = NEW.response_session_id
                          AND rs.tenant_id = NEW.tenant_id
                          AND c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                          AND sr.template_version_id = COALESCE(cls.template_version_id, c.template_version_id)
                          AND (
                              cls.id IS NULL
                              OR cls.scoring_rule_id = NEW.scoring_rule_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'score run session, campaign, and scoring rule must belong to the same tenant template';
                    END IF;

                    RETURN NEW;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS campaign_launch_snapshot_tenant_isolation ON campaign_launch_snapshot;
                DROP TRIGGER IF EXISTS campaign_launch_snapshot_prevent_delete ON campaign_launch_snapshot;
                DROP TRIGGER IF EXISTS campaign_launch_snapshot_prevent_update ON campaign_launch_snapshot;
                DROP FUNCTION IF EXISTS campaign_launch_snapshot_prevent_update_delete();
                DROP TRIGGER IF EXISTS campaign_launch_snapshot_tenant_guard ON campaign_launch_snapshot;
                DROP FUNCTION IF EXISTS campaign_launch_snapshot_tenant_guard();
                """);

            migrationBuilder.DropTable(
                name: "campaign_launch_snapshot");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION answer_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM response_session AS rs
                        JOIN assignment AS a ON a.id = rs.assignment_id
                        JOIN campaign AS c ON c.id = a.campaign_id
                        JOIN question AS q ON q.id = NEW.question_id
                        WHERE rs.id = NEW.session_id
                          AND rs.tenant_id = NEW.tenant_id
                          AND q.template_version_id = c.template_version_id
                    ) THEN
                        RAISE EXCEPTION 'answer session and question must belong to the same tenant campaign template';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION score_run_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM response_session AS rs
                        JOIN assignment AS a ON a.id = rs.assignment_id
                        JOIN campaign AS c ON c.id = a.campaign_id
                        JOIN scoring_rule AS sr ON sr.id = NEW.scoring_rule_id
                        WHERE rs.id = NEW.response_session_id
                          AND rs.tenant_id = NEW.tenant_id
                          AND c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                          AND sr.template_version_id = c.template_version_id
                    ) THEN
                        RAISE EXCEPTION 'score run session, campaign, and scoring rule must belong to the same tenant template';
                    END IF;

                    RETURN NEW;
                END;
                $$;
                """);
        }
    }
}
