using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "withdrawal_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_record_count = table.Column<int>(type: "integer", nullable: false),
                    response_session_count = table.Column<int>(type: "integer", nullable: false),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    score_count = table.Column<int>(type: "integer", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_event", x => x.id);
                    table.CheckConstraint("ck_withdrawal_event_action_after", "action_after IN ('delete', 'anonymize')");
                    table.CheckConstraint("ck_withdrawal_event_counts_non_negative", "consent_record_count >= 0 AND response_session_count >= 0 AND answer_count >= 0 AND score_count >= 0");
                    table.CheckConstraint("ck_withdrawal_event_metadata_object", "jsonb_typeof(metadata_json) = 'object'");
                    table.CheckConstraint("ck_withdrawal_event_processed_after_requested", "processed_at IS NULL OR processed_at >= requested_at");
                    table.CheckConstraint("ck_withdrawal_event_scope", "scope IN ('campaign_series')");
                    table.CheckConstraint("ck_withdrawal_event_status", "status IN ('planned')");
                    table.CheckConstraint("ck_withdrawal_event_target_kind", "target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched')");
                    table.CheckConstraint("ck_withdrawal_event_target_shape", "((target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL) OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL) OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL))");
                    table.ForeignKey(
                        name: "fk_withdrawal_event_campaign_series",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_participant_code",
                        column: x => x.participant_code_id,
                        principalTable: "participant_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_retention_policy",
                        column: x => x.retention_policy_id,
                        principalTable: "retention_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_subject",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_event_campaign_series_id",
                table: "withdrawal_event",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_participant_code_id",
                table: "withdrawal_event",
                column: "participant_code_id",
                filter: "participant_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_event_retention_policy_id",
                table: "withdrawal_event",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_subject_id",
                table: "withdrawal_event",
                column: "subject_id",
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_tenant_id",
                table: "withdrawal_event",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_tenant_series_requested",
                table: "withdrawal_event",
                columns: new[] { "tenant_id", "campaign_series_id", "requested_at" });

            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event ENABLE ROW LEVEL SECURITY;
                ALTER TABLE withdrawal_event FORCE ROW LEVEL SECURITY;

                CREATE POLICY withdrawal_event_tenant_isolation ON withdrawal_event
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS withdrawal_event_tenant_guard ON withdrawal_event;
                DROP FUNCTION IF EXISTS withdrawal_event_tenant_guard();
                DROP POLICY IF EXISTS withdrawal_event_tenant_isolation ON withdrawal_event;
                """);

            migrationBuilder.DropTable(
                name: "withdrawal_event");
        }
    }
}
