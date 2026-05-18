using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifiedRespondentEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token");

            migrationBuilder.AddColumn<Guid>(
                name: "assignment_id",
                table: "invitation_token",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subject_id",
                table: "consent_record",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_assignment_id",
                table: "invitation_token",
                column: "assignment_id",
                filter: "assignment_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token",
                sql: "channel IN ('email','sms','open_link','identified_entry')");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_subject_id",
                table: "consent_record",
                column: "subject_id",
                filter: "subject_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_consent_record_subject_subject_id",
                table: "consent_record",
                column: "subject_id",
                principalTable: "subject",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invitation_token_assignment_assignment_id",
                table: "invitation_token",
                column: "assignment_id",
                principalTable: "assignment",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;

                CREATE POLICY invitation_token_tenant_isolation ON invitation_token
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM campaign AS c
                            WHERE c.id = invitation_token.campaign_id
                              AND c.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND (
                            assignment_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM assignment AS a
                                WHERE a.id = invitation_token.assignment_id
                                  AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND a.campaign_id = invitation_token.campaign_id
                            )
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
                        AND (
                            assignment_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM assignment AS a
                                WHERE a.id = invitation_token.assignment_id
                                  AND a.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND a.campaign_id = invitation_token.campaign_id
                            )
                        )
                    );

                CREATE OR REPLACE FUNCTION invitation_token_assignment_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NEW.assignment_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM assignment AS a
                        WHERE a.id = NEW.assignment_id
                          AND a.tenant_id = NEW.tenant_id
                          AND a.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'invitation token assignment must belong to the same tenant campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER invitation_token_assignment_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, assignment_id
                    ON invitation_token
                    FOR EACH ROW
                    EXECUTE FUNCTION invitation_token_assignment_guard();

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

                    IF NEW.subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'consent record subject must be owned by the same tenant';
                    END IF;

                    IF NEW.subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM assignment AS a
                        WHERE a.id = NEW.assignment_id
                          AND a.respondent_subject_id = NEW.subject_id
                    ) THEN
                        RAISE EXCEPTION 'consent record subject must match the assignment respondent subject';
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
                DROP TRIGGER IF EXISTS invitation_token_assignment_guard ON invitation_token;
                DROP FUNCTION IF EXISTS invitation_token_assignment_guard();

                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;

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
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_consent_record_subject_subject_id",
                table: "consent_record");

            migrationBuilder.DropForeignKey(
                name: "fk_invitation_token_assignment_assignment_id",
                table: "invitation_token");

            migrationBuilder.DropIndex(
                name: "ix_invitation_token_assignment_id",
                table: "invitation_token");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token");

            migrationBuilder.DropIndex(
                name: "ix_consent_record_subject_id",
                table: "consent_record");

            migrationBuilder.DropColumn(
                name: "assignment_id",
                table: "invitation_token");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "consent_record");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token",
                sql: "channel IN ('email','sms','open_link')");
        }
    }
}
