using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionDueBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "retention_due_batch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anchor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    as_of = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consent_record_count = table.Column<int>(type: "integer", nullable: false),
                    response_session_count = table.Column<int>(type: "integer", nullable: false),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    score_run_count = table.Column<int>(type: "integer", nullable: false),
                    score_count = table.Column<int>(type: "integer", nullable: false),
                    derived_artifact_count = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_due_batch", x => x.id);
                    table.CheckConstraint("ck_retention_due_batch_action_after", "action_after IN ('delete','anonymize')");
                    table.CheckConstraint("ck_retention_due_batch_anchor", "anchor IN ('response_submitted_at')");
                    table.CheckConstraint("ck_retention_due_batch_counts_non_negative", "consent_record_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0 AND derived_artifact_count >= 0");
                    table.CheckConstraint("ck_retention_due_batch_due_before_as_of", "due_before <= as_of");
                    table.CheckConstraint("ck_retention_due_batch_response_session_count_positive", "response_session_count > 0");
                    table.CheckConstraint("ck_retention_due_batch_status", "status IN ('planned','processing','completed','failed')");
                    table.ForeignKey(
                        name: "fk_retention_due_batch_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_due_batch_retention_policy_retention_policy_id",
                        column: x => x.retention_policy_id,
                        principalTable: "retention_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_due_batch_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_retention_due_batch_campaign_series_id",
                table: "retention_due_batch",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "IX_retention_due_batch_retention_policy_id",
                table: "retention_due_batch",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_id",
                table: "retention_due_batch",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_id_idempotency_key",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_series_due_before",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "campaign_series_id", "due_before" });

            migrationBuilder.Sql(
                """
                ALTER TABLE retention_due_batch ENABLE ROW LEVEL SECURITY;
                ALTER TABLE retention_due_batch FORCE ROW LEVEL SECURITY;

                CREATE POLICY retention_due_batch_tenant_isolation ON retention_due_batch
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION retention_due_batch_tenant_guard()
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
                        RAISE EXCEPTION 'retention due batch campaign series must belong to the same tenant';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM retention_policy rp
                        WHERE rp.id = NEW.retention_policy_id
                          AND rp.tenant_id = NEW.tenant_id
                          AND rp.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'retention due batch retention policy must belong to the same tenant campaign series';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER retention_due_batch_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id, retention_policy_id
                    ON retention_due_batch
                    FOR EACH ROW
                    EXECUTE FUNCTION retention_due_batch_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS retention_due_batch_tenant_guard ON retention_due_batch;
                DROP FUNCTION IF EXISTS retention_due_batch_tenant_guard();
                DROP POLICY IF EXISTS retention_due_batch_tenant_isolation ON retention_due_batch;
                """);

            migrationBuilder.DropTable(
                name: "retention_due_batch");
        }
    }
}
