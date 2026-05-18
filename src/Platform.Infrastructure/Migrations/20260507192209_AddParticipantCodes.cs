using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participant_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    argon2_memory_kib = table.Column<int>(type: "integer", nullable: false),
                    argon2_iterations = table.Column<int>(type: "integer", nullable: false),
                    argon2_parallelism = table.Column<int>(type: "integer", nullable: false),
                    argon2_output_bytes = table.Column<int>(type: "integer", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participant_code", x => x.id);
                    table.CheckConstraint("ck_participant_code_argon2_parameters", "argon2_memory_kib >= 65536 AND argon2_iterations >= 3 AND argon2_parallelism >= 4 AND argon2_output_bytes >= 32");
                    table.CheckConstraint("ck_participant_code_hash_length", "octet_length(hash) = 32");
                    table.ForeignKey(
                        name: "fk_participant_code_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_participant_code_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_participant_code_campaign_series_id_hash",
                table: "participant_code",
                columns: new[] { "campaign_series_id", "hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participant_code_tenant_id",
                table: "participant_code",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_response_session_participant_code_participant_code_id",
                table: "response_session",
                column: "participant_code_id",
                principalTable: "participant_code",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE participant_code ENABLE ROW LEVEL SECURITY;
                ALTER TABLE participant_code FORCE ROW LEVEL SECURITY;

                CREATE POLICY participant_code_tenant_isolation ON participant_code
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION participant_code_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign_series cs
                        WHERE cs.id = NEW.campaign_series_id
                          AND cs.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'participant code campaign series must belong to the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER participant_code_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id
                    ON participant_code
                    FOR EACH ROW
                    EXECUTE FUNCTION participant_code_tenant_guard();

                CREATE OR REPLACE FUNCTION response_session_participant_code_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NEW.participant_code_id IS NULL THEN
                        RETURN NEW;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM participant_code pc
                        JOIN assignment a ON a.id = NEW.assignment_id
                        JOIN campaign c ON c.id = a.campaign_id
                        WHERE pc.id = NEW.participant_code_id
                          AND pc.tenant_id = NEW.tenant_id
                          AND a.tenant_id = NEW.tenant_id
                          AND c.tenant_id = NEW.tenant_id
                          AND c.campaign_series_id = pc.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'response session participant code must belong to the same tenant campaign series';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER response_session_participant_code_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, assignment_id, participant_code_id
                    ON response_session
                    FOR EACH ROW
                    EXECUTE FUNCTION response_session_participant_code_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS response_session_participant_code_guard ON response_session;
                DROP FUNCTION IF EXISTS response_session_participant_code_guard();
                DROP TRIGGER IF EXISTS participant_code_tenant_guard ON participant_code;
                DROP FUNCTION IF EXISTS participant_code_tenant_guard();
                DROP POLICY IF EXISTS participant_code_tenant_isolation ON participant_code;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_response_session_participant_code_participant_code_id",
                table: "response_session");

            migrationBuilder.DropTable(
                name: "participant_code");
        }
    }
}
