using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "response_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_taken_ms = table.Column<int>(type: "integer", nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ip_hash = table.Column<string>(type: "text", nullable: true),
                    user_agent_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_response_session", x => x.id);
                    table.CheckConstraint("ck_response_session_submitted_after_started", "started_at IS NULL OR submitted_at IS NULL OR submitted_at >= started_at");
                    table.CheckConstraint("ck_response_session_time_taken_non_negative", "time_taken_ms IS NULL OR time_taken_ms >= 0");
                    table.ForeignKey(
                        name: "fk_response_session_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_response_session_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "answer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    is_na = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answer", x => x.id);
                    table.CheckConstraint("ck_answer_not_skipped_and_na", "NOT (is_skipped = TRUE AND is_na = TRUE)");
                    table.CheckConstraint("ck_answer_value_json", "value IS NULL OR jsonb_typeof(value) IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_answer_question_question_id",
                        column: x => x.question_id,
                        principalTable: "question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_answer_response_session_session_id",
                        column: x => x.session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_answer_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_answer_question_id_answered_at",
                table: "answer",
                columns: new[] { "question_id", "answered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_answer_session_id",
                table: "answer",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_answer_session_id_question_id",
                table: "answer",
                columns: new[] { "session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_answer_tenant_id",
                table: "answer",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session",
                column: "assignment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_response_session_participant_code_id",
                table: "response_session",
                column: "participant_code_id",
                filter: "participant_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_submitted_at",
                table: "response_session",
                column: "submitted_at",
                filter: "submitted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_tenant_id",
                table: "response_session",
                column: "tenant_id");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION response_session_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM assignment AS a
                        WHERE a.id = NEW.assignment_id
                          AND a.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'response session assignment must be owned by the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER response_session_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, assignment_id
                    ON response_session
                    FOR EACH ROW
                    EXECUTE FUNCTION response_session_tenant_guard();

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

                CREATE TRIGGER answer_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, session_id, question_id
                    ON answer
                    FOR EACH ROW
                    EXECUTE FUNCTION answer_tenant_guard();

                ALTER TABLE response_session ENABLE ROW LEVEL SECURITY;
                ALTER TABLE response_session FORCE ROW LEVEL SECURITY;

                CREATE POLICY response_session_tenant_isolation ON response_session
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM assignment AS a
                            WHERE a.id = response_session.assignment_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM assignment AS a
                            WHERE a.id = response_session.assignment_id
                              AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE answer ENABLE ROW LEVEL SECURITY;
                ALTER TABLE answer FORCE ROW LEVEL SECURITY;

                CREATE POLICY answer_tenant_isolation ON answer
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM response_session AS rs
                            WHERE rs.id = answer.session_id
                              AND rs.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM response_session AS rs
                            WHERE rs.id = answer.session_id
                              AND rs.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS answer_tenant_isolation ON answer;
                DROP POLICY IF EXISTS response_session_tenant_isolation ON response_session;
                DROP TRIGGER IF EXISTS answer_tenant_guard ON answer;
                DROP FUNCTION IF EXISTS answer_tenant_guard();
                DROP TRIGGER IF EXISTS response_session_tenant_guard ON response_session;
                DROP FUNCTION IF EXISTS response_session_tenant_guard();
                """);

            migrationBuilder.DropTable(
                name: "answer");

            migrationBuilder.DropTable(
                name: "response_session");
        }
    }
}
