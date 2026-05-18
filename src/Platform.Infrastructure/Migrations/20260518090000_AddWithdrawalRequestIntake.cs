using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518090000_AddWithdrawalRequestIntake")]
    public partial class AddWithdrawalRequestIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "response_session_id",
                table: "withdrawal_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_shape;

                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_kind;

                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_status;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_kind
                    CHECK (target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched', 'response_session'));

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_status
                    CHECK (status IN ('requested', 'planned', 'processing', 'completed', 'failed'));

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_shape
                    CHECK (
                        (target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (target_kind = 'response_session' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NOT NULL)
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_response_session_id",
                table: "withdrawal_event",
                column: "response_session_id",
                filter: "response_session_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_withdrawal_event_response_session",
                table: "withdrawal_event",
                column: "response_session_id",
                principalTable: "response_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION withdrawal_event_tenant_guard()
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
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM retention_policy rp
                        WHERE rp.id = NEW.retention_policy_id
                          AND rp.tenant_id = NEW.tenant_id
                          AND rp.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event retention policy must belong to the same tenant campaign series';
                    END IF;

                    IF NEW.subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject s
                        WHERE s.id = NEW.subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    IF NEW.participant_code_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM participant_code pc
                        WHERE pc.id = NEW.participant_code_id
                          AND pc.tenant_id = NEW.tenant_id
                          AND pc.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    IF NEW.response_session_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM response_session rs
                        JOIN assignment a ON a.id = rs.assignment_id
                        JOIN campaign c ON c.id = a.campaign_id
                        WHERE rs.id = NEW.response_session_id
                          AND rs.tenant_id = NEW.tenant_id
                          AND c.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                DROP TRIGGER IF EXISTS withdrawal_event_tenant_guard ON withdrawal_event;

                CREATE TRIGGER withdrawal_event_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id, retention_policy_id, subject_id, participant_code_id, response_session_id
                    ON withdrawal_event
                    FOR EACH ROW
                    EXECUTE FUNCTION withdrawal_event_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS withdrawal_event_tenant_guard ON withdrawal_event;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_withdrawal_event_response_session",
                table: "withdrawal_event");

            migrationBuilder.DropIndex(
                name: "ix_withdrawal_event_response_session_id",
                table: "withdrawal_event");

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_shape;

                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_kind;

                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_status;
                """);

            migrationBuilder.DropColumn(
                name: "response_session_id",
                table: "withdrawal_event");

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_kind
                    CHECK (target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched'));

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_status
                    CHECK (status IN ('planned', 'processing', 'completed', 'failed'));

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_shape
                    CHECK (
                        (target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL)
                        OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL)
                    );

                CREATE OR REPLACE FUNCTION withdrawal_event_tenant_guard()
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
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM retention_policy rp
                        WHERE rp.id = NEW.retention_policy_id
                          AND rp.tenant_id = NEW.tenant_id
                          AND rp.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event retention policy must belong to the same tenant campaign series';
                    END IF;

                    IF NEW.subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject s
                        WHERE s.id = NEW.subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    IF NEW.participant_code_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM participant_code pc
                        WHERE pc.id = NEW.participant_code_id
                          AND pc.tenant_id = NEW.tenant_id
                          AND pc.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'withdrawal event target must belong to the same tenant campaign series';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER withdrawal_event_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id, retention_policy_id, subject_id, participant_code_id
                    ON withdrawal_event
                    FOR EACH ROW
                    EXECUTE FUNCTION withdrawal_event_tenant_guard();
                """);
        }
    }
}
