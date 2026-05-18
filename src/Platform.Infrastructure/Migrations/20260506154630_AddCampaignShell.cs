using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignShell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaign_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ethics_approval_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retention_until = table.Column<DateOnly>(type: "date", nullable: true),
                    code_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_series", x => x.id);
                    table.CheckConstraint("ck_campaign_series_code_salt_length", "octet_length(code_salt) = 32");
                    table.ForeignKey(
                        name: "fk_campaign_series_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_identity_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule = table.Column<string>(type: "jsonb", nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign", x => x.id);
                    table.CheckConstraint("ck_campaign_date_range", "start_at IS NULL OR end_at IS NULL OR end_at > start_at");
                    table.CheckConstraint("ck_campaign_response_identity_mode", "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                    table.CheckConstraint("ck_campaign_schedule_object", "jsonb_typeof(schedule) = 'object'");
                    table.CheckConstraint("ck_campaign_status", "status IN ('draft','scheduled','live','closed','cancelled')");
                    table.ForeignKey(
                        name: "fk_campaign_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_user_account_created_by",
                        column: x => x.created_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audience",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selector = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience", x => x.id);
                    table.ForeignKey(
                        name: "fk_audience_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_token", x => x.id);
                    table.CheckConstraint("ck_invitation_token_channel", "channel IN ('email','sms','open_link')");
                    table.ForeignKey(
                        name: "fk_invitation_token_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitation_token_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "respondent_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    rule = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_respondent_rule", x => x.id);
                    table.CheckConstraint("ck_respondent_rule_ordinal_positive", "ordinal > 0");
                    table.CheckConstraint("ck_respondent_rule_rule_object", "jsonb_typeof(rule) = 'object'");
                    table.ForeignKey(
                        name: "fk_respondent_rule_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audience_member",
                columns: table => new
                {
                    audience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_member", x => new { x.audience_id, x.subject_id });
                    table.CheckConstraint("ck_audience_member_removed_after_added", "removed_at IS NULL OR removed_at >= added_at");
                    table.ForeignKey(
                        name: "fk_audience_member_audience_audience_id",
                        column: x => x.audience_id,
                        principalTable: "audience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audience_member_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    respondent_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invite_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment", x => x.id);
                    table.CheckConstraint("ck_assignment_identity_shape", "(anonymous = FALSE AND respondent_subject_id IS NOT NULL AND invite_token_id IS NULL) OR (anonymous = TRUE AND respondent_subject_id IS NULL AND invite_token_id IS NOT NULL)");
                    table.CheckConstraint("ck_assignment_status", "status IN ('pending','started','submitted','cancelled','expired')");
                    table.ForeignKey(
                        name: "fk_assignment_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_invitation_token_invite_token_id",
                        column: x => x.invite_token_id,
                        principalTable: "invitation_token",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_subject_respondent_subject_id",
                        column: x => x.respondent_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_subject_target_subject_id",
                        column: x => x.target_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignment_campaign_id_status_due_at",
                table: "assignment",
                columns: new[] { "campaign_id", "status", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assignment_invite_token_id",
                table: "assignment",
                column: "invite_token_id",
                unique: true,
                filter: "invite_token_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_respondent_subject_id",
                table: "assignment",
                column: "respondent_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_target_subject_id",
                table: "assignment",
                column: "target_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_tenant_id",
                table: "assignment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_unique_identified",
                table: "assignment",
                columns: new[] { "campaign_id", "target_subject_id", "respondent_subject_id" },
                unique: true,
                filter: "respondent_subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audience_campaign_id",
                table: "audience",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_audience_member_subject_id",
                table: "audience_member",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_campaign_series_id",
                table: "campaign",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_created_by",
                table: "campaign",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_template_version_id",
                table: "campaign",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_tenant_id",
                table: "campaign",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_tenant_id_status",
                table: "campaign",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_series_tenant_id",
                table: "campaign_series",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_series_tenant_id_name",
                table: "campaign_series",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_campaign_id",
                table: "invitation_token",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_tenant_id",
                table: "invitation_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_token_hash",
                table: "invitation_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_respondent_rule_campaign_id_ordinal",
                table: "respondent_rule",
                columns: new[] { "campaign_id", "ordinal" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION campaign_template_version_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM template_version AS tv
                        JOIN survey_template AS st ON st.id = tv.template_id
                        WHERE tv.id = NEW.template_version_id
                          AND (
                              tv.is_global = TRUE
                              OR st.tenant_id = NEW.tenant_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'campaign template version must be global or owned by the same tenant';
                    END IF;

                    IF NEW.campaign_series_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM campaign_series AS cs
                        WHERE cs.id = NEW.campaign_series_id
                          AND cs.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'campaign series must be owned by the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER campaign_template_version_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_series_id, template_version_id
                    ON campaign
                    FOR EACH ROW
                    EXECUTE FUNCTION campaign_template_version_tenant_guard();

                CREATE OR REPLACE FUNCTION audience_member_subject_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM audience AS a
                        JOIN campaign AS c ON c.id = a.campaign_id
                        JOIN subject AS s ON s.id = NEW.subject_id
                        WHERE a.id = NEW.audience_id
                          AND s.tenant_id = c.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'audience member subject must be owned by the campaign tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER audience_member_subject_tenant_guard
                    BEFORE INSERT OR UPDATE OF audience_id, subject_id
                    ON audience_member
                    FOR EACH ROW
                    EXECUTE FUNCTION audience_member_subject_tenant_guard();

                CREATE OR REPLACE FUNCTION assignment_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment campaign must be owned by the same tenant';
                    END IF;

                    IF NEW.target_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.target_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment target subject must be owned by the same tenant';
                    END IF;

                    IF NEW.respondent_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.respondent_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment respondent subject must be owned by the same tenant';
                    END IF;

                    IF NEW.invite_token_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM invitation_token AS token
                        WHERE token.id = NEW.invite_token_id
                          AND token.tenant_id = NEW.tenant_id
                          AND token.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'assignment invitation token must be owned by the same tenant and campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER assignment_tenant_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, target_subject_id, respondent_subject_id, invite_token_id
                    ON assignment
                    FOR EACH ROW
                    EXECUTE FUNCTION assignment_tenant_guard();

                ALTER TABLE campaign_series ENABLE ROW LEVEL SECURITY;
                ALTER TABLE campaign_series FORCE ROW LEVEL SECURITY;

                CREATE POLICY campaign_series_tenant_isolation ON campaign_series
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE campaign ENABLE ROW LEVEL SECURITY;
                ALTER TABLE campaign FORCE ROW LEVEL SECURITY;

                CREATE POLICY campaign_tenant_isolation ON campaign
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE audience ENABLE ROW LEVEL SECURITY;
                ALTER TABLE audience FORCE ROW LEVEL SECURITY;

                CREATE POLICY audience_tenant_isolation ON audience
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = audience.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = audience.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE audience_member ENABLE ROW LEVEL SECURITY;
                ALTER TABLE audience_member FORCE ROW LEVEL SECURITY;

                CREATE POLICY audience_member_tenant_isolation ON audience_member
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM audience AS a
                            JOIN campaign AS c ON c.id = a.campaign_id
                            WHERE a.id = audience_member.audience_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS s
                            WHERE s.id = audience_member.subject_id
                              AND s.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM audience AS a
                            JOIN campaign AS c ON c.id = a.campaign_id
                            WHERE a.id = audience_member.audience_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS s
                            WHERE s.id = audience_member.subject_id
                              AND s.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE respondent_rule ENABLE ROW LEVEL SECURITY;
                ALTER TABLE respondent_rule FORCE ROW LEVEL SECURITY;

                CREATE POLICY respondent_rule_tenant_isolation ON respondent_rule
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = respondent_rule.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = respondent_rule.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE invitation_token ENABLE ROW LEVEL SECURITY;
                ALTER TABLE invitation_token FORCE ROW LEVEL SECURITY;

                CREATE POLICY invitation_token_tenant_isolation ON invitation_token
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = invitation_token.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = invitation_token.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE assignment ENABLE ROW LEVEL SECURITY;
                ALTER TABLE assignment FORCE ROW LEVEL SECURITY;

                CREATE POLICY assignment_tenant_isolation ON assignment
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = assignment.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND (
                            target_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS target_subject
                                WHERE target_subject.id = assignment.target_subject_id
                                  AND target_subject.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        AND (
                            respondent_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS respondent_subject
                                WHERE respondent_subject.id = assignment.respondent_subject_id
                                  AND respondent_subject.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        AND (
                            invite_token_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM invitation_token AS token
                                WHERE token.id = assignment.invite_token_id
                                  AND token.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND token.campaign_id = assignment.campaign_id
                            )
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = assignment.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND (
                            target_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS target_subject
                                WHERE target_subject.id = assignment.target_subject_id
                                  AND target_subject.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        AND (
                            respondent_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS respondent_subject
                                WHERE respondent_subject.id = assignment.respondent_subject_id
                                  AND respondent_subject.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        AND (
                            invite_token_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM invitation_token AS token
                                WHERE token.id = assignment.invite_token_id
                                  AND token.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND token.campaign_id = assignment.campaign_id
                            )
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS assignment_tenant_guard ON assignment;
                DROP FUNCTION IF EXISTS assignment_tenant_guard();
                DROP TRIGGER IF EXISTS audience_member_subject_tenant_guard ON audience_member;
                DROP FUNCTION IF EXISTS audience_member_subject_tenant_guard();
                DROP TRIGGER IF EXISTS campaign_template_version_tenant_guard ON campaign;
                DROP FUNCTION IF EXISTS campaign_template_version_tenant_guard();

                DROP POLICY IF EXISTS assignment_tenant_isolation ON assignment;
                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;
                DROP POLICY IF EXISTS respondent_rule_tenant_isolation ON respondent_rule;
                DROP POLICY IF EXISTS audience_member_tenant_isolation ON audience_member;
                DROP POLICY IF EXISTS audience_tenant_isolation ON audience;
                DROP POLICY IF EXISTS campaign_tenant_isolation ON campaign;
                DROP POLICY IF EXISTS campaign_series_tenant_isolation ON campaign_series;
                """);

            migrationBuilder.DropTable(
                name: "assignment");

            migrationBuilder.DropTable(
                name: "audience_member");

            migrationBuilder.DropTable(
                name: "respondent_rule");

            migrationBuilder.DropTable(
                name: "invitation_token");

            migrationBuilder.DropTable(
                name: "audience");

            migrationBuilder.DropTable(
                name: "campaign");

            migrationBuilder.DropTable(
                name: "campaign_series");
        }
    }
}
