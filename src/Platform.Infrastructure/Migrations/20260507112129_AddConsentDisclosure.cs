using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentDisclosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "consent_document_id",
                table: "campaign_launch_snapshot",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "consent_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    body_markdown = table.Column<string>(type: "text", nullable: false),
                    required_grants = table.Column<string>(type: "jsonb", nullable: false),
                    optional_grants = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_document", x => x.id);
                    table.CheckConstraint("ck_consent_document_optional_grants_array", "jsonb_typeof(optional_grants) = 'array'");
                    table.CheckConstraint("ck_consent_document_required_grants_array", "jsonb_typeof(required_grants) = 'array'");
                    table.CheckConstraint("ck_consent_document_retired_after_published", "retired_at IS NULL OR retired_at > published_at");
                    table.ForeignKey(
                        name: "fk_consent_document_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_document_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    accepted_grants = table.Column<string>(type: "jsonb", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_record", x => x.id);
                    table.CheckConstraint("ck_consent_record_accepted_grants_array", "jsonb_typeof(accepted_grants) = 'array'");
                    table.ForeignKey(
                        name: "fk_consent_record_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_consent_document_consent_document_id",
                        column: x => x.consent_document_id,
                        principalTable: "consent_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_response_session_consent_record_id",
                table: "response_session",
                column: "consent_record_id",
                filter: "consent_record_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_consent_document_id",
                table: "campaign_launch_snapshot",
                column: "consent_document_id",
                filter: "consent_document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_consent_document_campaign_series_id_locale_version",
                table: "consent_document",
                columns: new[] { "campaign_series_id", "locale", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consent_document_tenant_id",
                table: "consent_document",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_assignment_id",
                table: "consent_record",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_campaign_id",
                table: "consent_record",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_consent_document_id",
                table: "consent_record",
                column: "consent_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_tenant_id",
                table: "consent_record",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_launch_snapshot_consent_document_consent_document_id",
                table: "campaign_launch_snapshot",
                column: "consent_document_id",
                principalTable: "consent_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_response_session_consent_record_consent_record_id",
                table: "response_session",
                column: "consent_record_id",
                principalTable: "consent_record",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE consent_document ENABLE ROW LEVEL SECURITY;
                ALTER TABLE consent_document FORCE ROW LEVEL SECURITY;

                CREATE POLICY consent_document_tenant_isolation ON consent_document
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE consent_record ENABLE ROW LEVEL SECURITY;
                ALTER TABLE consent_record FORCE ROW LEVEL SECURITY;

                CREATE POLICY consent_record_tenant_isolation ON consent_record
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                CREATE OR REPLACE FUNCTION consent_document_tenant_guard()
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
                        RAISE EXCEPTION 'consent document campaign series must be owned by the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER consent_document_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id
                    ON consent_document
                    FOR EACH ROW
                    EXECUTE FUNCTION consent_document_tenant_guard();

                CREATE OR REPLACE FUNCTION consent_record_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM consent_document AS cd
                        WHERE cd.id = NEW.consent_document_id
                          AND cd.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'consent record document must be owned by the same tenant';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'consent record campaign must be owned by the same tenant';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM assignment AS a
                        WHERE a.id = NEW.assignment_id
                          AND a.tenant_id = NEW.tenant_id
                          AND a.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'consent record assignment must be owned by the same tenant campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER consent_record_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, consent_document_id, campaign_id, assignment_id
                    ON consent_record
                    FOR EACH ROW
                    EXECUTE FUNCTION consent_record_tenant_guard();

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

                    IF NEW.consent_record_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM consent_record AS cr
                        WHERE cr.id = NEW.consent_record_id
                          AND cr.tenant_id = NEW.tenant_id
                          AND cr.assignment_id = NEW.assignment_id
                    ) THEN
                        RAISE EXCEPTION 'response session consent record must be owned by the same tenant assignment';
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

                DROP TRIGGER IF EXISTS consent_record_tenant_guard ON consent_record;
                DROP FUNCTION IF EXISTS consent_record_tenant_guard();
                DROP TRIGGER IF EXISTS consent_document_tenant_guard ON consent_document;
                DROP FUNCTION IF EXISTS consent_document_tenant_guard();
                DROP POLICY IF EXISTS consent_record_tenant_isolation ON consent_record;
                DROP POLICY IF EXISTS consent_document_tenant_isolation ON consent_document;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_campaign_launch_snapshot_consent_document_consent_document_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropForeignKey(
                name: "fk_response_session_consent_record_consent_record_id",
                table: "response_session");

            migrationBuilder.DropTable(
                name: "consent_record");

            migrationBuilder.DropTable(
                name: "consent_document");

            migrationBuilder.DropIndex(
                name: "ix_response_session_consent_record_id",
                table: "response_session");

            migrationBuilder.DropIndex(
                name: "ix_campaign_launch_snapshot_consent_document_id",
                table: "campaign_launch_snapshot");

            migrationBuilder.DropColumn(
                name: "consent_document_id",
                table: "campaign_launch_snapshot");
        }
    }
}
