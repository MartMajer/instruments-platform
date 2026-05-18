using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "score_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scoring_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ran_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score_run", x => x.id);
                    table.CheckConstraint("ck_score_run_status", "status IN ('running','success','failed')");
                    table.ForeignKey(
                        name: "fk_score_run_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_scoring_rule_scoring_rule_id",
                        column: x => x.scoring_rule_id,
                        principalTable: "scoring_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "score",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dimension_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    n = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score", x => x.id);
                    table.CheckConstraint("ck_score_n_non_negative", "n >= 0");
                    table.ForeignKey(
                        name: "fk_score_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_score_run_score_run_id",
                        column: x => x.score_run_id,
                        principalTable: "score_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_score_campaign_id_dimension_code",
                table: "score",
                columns: new[] { "campaign_id", "dimension_code" });

            migrationBuilder.CreateIndex(
                name: "ix_score_response_session_id",
                table: "score",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_score_run_id",
                table: "score",
                column: "score_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_tenant_id",
                table: "score",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_campaign_id",
                table: "score_run",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_response_session_id",
                table: "score_run",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_scoring_rule_id",
                table: "score_run",
                column: "scoring_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_tenant_id",
                table: "score_run",
                column: "tenant_id");

            migrationBuilder.Sql(
                """
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

                CREATE TRIGGER score_run_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, response_session_id, scoring_rule_id
                    ON score_run
                    FOR EACH ROW
                    EXECUTE FUNCTION score_run_tenant_guard();

                CREATE OR REPLACE FUNCTION score_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM score_run AS sr
                        WHERE sr.id = NEW.score_run_id
                          AND sr.tenant_id = NEW.tenant_id
                          AND sr.campaign_id = NEW.campaign_id
                          AND sr.response_session_id = NEW.response_session_id
                    ) THEN
                        RAISE EXCEPTION 'score must belong to the same tenant, campaign, and response session as its score run';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER score_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, score_run_id, campaign_id, response_session_id
                    ON score
                    FOR EACH ROW
                    EXECUTE FUNCTION score_tenant_guard();

                ALTER TABLE score_run ENABLE ROW LEVEL SECURITY;
                ALTER TABLE score_run FORCE ROW LEVEL SECURITY;

                CREATE POLICY score_run_tenant_isolation ON score_run
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM response_session AS rs
                            JOIN assignment AS a ON a.id = rs.assignment_id
                            WHERE rs.id = score_run.response_session_id
                              AND rs.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND a.campaign_id = score_run.campaign_id
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM response_session AS rs
                            JOIN assignment AS a ON a.id = rs.assignment_id
                            WHERE rs.id = score_run.response_session_id
                              AND rs.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND a.campaign_id = score_run.campaign_id
                        )
                    );

                ALTER TABLE score ENABLE ROW LEVEL SECURITY;
                ALTER TABLE score FORCE ROW LEVEL SECURITY;

                CREATE POLICY score_tenant_isolation ON score
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM score_run AS sr
                            WHERE sr.id = score.score_run_id
                              AND sr.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM score_run AS sr
                            WHERE sr.id = score.score_run_id
                              AND sr.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS score_tenant_isolation ON score;
                DROP POLICY IF EXISTS score_run_tenant_isolation ON score_run;
                DROP TRIGGER IF EXISTS score_tenant_guard ON score;
                DROP FUNCTION IF EXISTS score_tenant_guard();
                DROP TRIGGER IF EXISTS score_run_tenant_guard ON score_run;
                DROP FUNCTION IF EXISTS score_run_tenant_guard();
                """);

            migrationBuilder.DropTable(
                name: "score");

            migrationBuilder.DropTable(
                name: "score_run");
        }
    }
}
