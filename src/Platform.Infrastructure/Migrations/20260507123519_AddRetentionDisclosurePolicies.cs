using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionDisclosurePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "disclosure_policy_id",
                table: "campaign_launch_snapshot",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "retention_policy_id",
                table: "campaign_launch_snapshot",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "disclosure_policy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    k_min = table.Column<int>(type: "integer", nullable: false),
                    suppression_strategy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    applies_to_dimensions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disclosure_policy", x => x.id);
                    table.CheckConstraint("ck_disclosure_policy_applies_to_dimensions_array", "jsonb_typeof(applies_to_dimensions) = 'array'");
                    table.CheckConstraint("ck_disclosure_policy_k_min", "k_min >= 5");
                    table.CheckConstraint("ck_disclosure_policy_retired_after_created", "retired_at IS NULL OR retired_at > created_at");
                    table.CheckConstraint("ck_disclosure_policy_suppression_strategy", "suppression_strategy IN ('hide_cell','aggregate_up','round_to_n')");
                    table.ForeignKey(
                        name: "fk_disclosure_policy_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_disclosure_policy_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retention_policy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    retain_for_years = table.Column<int>(type: "integer", nullable: false),
                    retention_start_event = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    next_review_at = table.Column<DateOnly>(type: "date", nullable: false),
                    publication_limits = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policy", x => x.id);
                    table.CheckConstraint("ck_retention_policy_action_after", "action_after IN ('delete','anonymize')");
                    table.CheckConstraint("ck_retention_policy_publication_limits_object", "jsonb_typeof(publication_limits) = 'object'");
                    table.CheckConstraint("ck_retention_policy_retain_for_years_positive", "retain_for_years > 0");
                    table.CheckConstraint("ck_retention_policy_retention_start_event", "retention_start_event IN ('consent_accepted_at','response_submitted_at','wave_closed_at','series_closed_at','last_response_submitted_at')");
                    table.CheckConstraint("ck_retention_policy_retired_after_created", "retired_at IS NULL OR retired_at > created_at");
                    table.ForeignKey(
                        name: "fk_retention_policy_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_policy_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_disclosure_policy_id",
                table: "campaign_launch_snapshot",
                column: "disclosure_policy_id",
                filter: "disclosure_policy_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_retention_policy_id",
                table: "campaign_launch_snapshot",
                column: "retention_policy_id",
                filter: "retention_policy_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_campaign_series_id",
                table: "disclosure_policy",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_campaign_series_id_version",
                table: "disclosure_policy",
                columns: new[] { "campaign_series_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_tenant_id",
                table: "disclosure_policy",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_campaign_series_id",
                table: "retention_policy",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_campaign_series_id_version",
                table: "retention_policy",
                columns: new[] { "campaign_series_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_tenant_id",
                table: "retention_policy",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_launch_snapshot_disclosure_policy_disclosure_policy_id",
                table: "campaign_launch_snapshot",
                column: "disclosure_policy_id",
                principalTable: "disclosure_policy",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_launch_snapshot_retention_policy_retention_policy_id",
                table: "campaign_launch_snapshot",
                column: "retention_policy_id",
                principalTable: "retention_policy",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE retention_policy ENABLE ROW LEVEL SECURITY;
                ALTER TABLE retention_policy FORCE ROW LEVEL SECURITY;

                CREATE POLICY retention_policy_tenant_isolation ON retention_policy
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE disclosure_policy ENABLE ROW LEVEL SECURITY;
                ALTER TABLE disclosure_policy FORCE ROW LEVEL SECURITY;

                CREATE POLICY disclosure_policy_tenant_isolation ON disclosure_policy
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION retention_policy_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign_series AS cs
                        WHERE cs.id = NEW.campaign_series_id
                          AND cs.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'retention policy campaign series must belong to the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER retention_policy_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id
                    ON retention_policy
                    FOR EACH ROW
                    EXECUTE FUNCTION retention_policy_tenant_guard();

                CREATE OR REPLACE FUNCTION disclosure_policy_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign_series AS cs
                        WHERE cs.id = NEW.campaign_series_id
                          AND cs.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'disclosure policy campaign series must belong to the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER disclosure_policy_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id
                    ON disclosure_policy
                    FOR EACH ROW
                    EXECUTE FUNCTION disclosure_policy_tenant_guard();

                DROP TRIGGER IF EXISTS campaign_launch_snapshot_tenant_guard ON campaign_launch_snapshot;

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

                    IF NEW.consent_document_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM consent_document AS cd
                        WHERE cd.id = NEW.consent_document_id
                          AND cd.tenant_id = NEW.tenant_id
                          AND cd.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot consent document must match tenant and campaign series';
                    END IF;

                    IF NEW.retention_policy_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM retention_policy AS rp
                        WHERE rp.id = NEW.retention_policy_id
                          AND rp.tenant_id = NEW.tenant_id
                          AND rp.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot policy ids must match tenant and campaign series';
                    END IF;

                    IF NEW.disclosure_policy_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM disclosure_policy AS dp
                        WHERE dp.id = NEW.disclosure_policy_id
                          AND dp.tenant_id = NEW.tenant_id
                          AND dp.campaign_series_id = NEW.campaign_series_id
                    ) THEN
                        RAISE EXCEPTION 'campaign launch snapshot policy ids must match tenant and campaign series';
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
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, campaign_series_id, template_version_id, scoring_rule_id, consent_document_id, retention_policy_id, disclosure_policy_id, launched_by
                    ON campaign_launch_snapshot
                    FOR EACH ROW
                    EXECUTE FUNCTION campaign_launch_snapshot_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS campaign_launch_snapshot_tenant_guard ON campaign_launch_snapshot;

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

                DROP TRIGGER IF EXISTS disclosure_policy_tenant_guard ON disclosure_policy;
                DROP FUNCTION IF EXISTS disclosure_policy_tenant_guard();
                DROP TRIGGER IF EXISTS retention_policy_tenant_guard ON retention_policy;
                DROP FUNCTION IF EXISTS retention_policy_tenant_guard();
                DROP POLICY IF EXISTS disclosure_policy_tenant_isolation ON disclosure_policy;
                DROP POLICY IF EXISTS retention_policy_tenant_isolation ON retention_policy;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_campaign_launch_snapshot_disclosure_policy_disclosure_policy_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropForeignKey(
                name: "fk_campaign_launch_snapshot_retention_policy_retention_policy_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropTable(
                name: "disclosure_policy");

            migrationBuilder.DropTable(
                name: "retention_policy");

            migrationBuilder.DropIndex(
                name: "ix_campaign_launch_snapshot_disclosure_policy_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropIndex(
                name: "ix_campaign_launch_snapshot_retention_policy_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropColumn(
                name: "disclosure_policy_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropColumn(
                name: "retention_policy_id",
                table: "campaign_launch_snapshot");
        }
    }
}
