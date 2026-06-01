using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifiedQueueInvitationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token");

            migrationBuilder.AddColumn<Guid>(
                name: "respondent_subject_id",
                table: "invitation_token",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_respondent_subject_id",
                table: "invitation_token",
                column: "respondent_subject_id",
                filter: "respondent_subject_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token",
                sql: "channel IN ('email','sms','open_link','identified_entry','identified_queue')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token",
                sql: "(channel = 'identified_queue' AND respondent_subject_id IS NOT NULL) OR (channel <> 'identified_queue' AND respondent_subject_id IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_invitation_token_subject_respondent_subject_id",
                table: "invitation_token",
                column: "respondent_subject_id",
                principalTable: "subject",
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
                        AND (
                            respondent_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS subject
                                WHERE subject.id = invitation_token.respondent_subject_id
                                  AND subject.tenant_id = current_setting('app.current_tenant_id')::uuid
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
                        AND (
                            respondent_subject_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM subject AS subject
                                WHERE subject.id = invitation_token.respondent_subject_id
                                  AND subject.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                    );

                DROP TRIGGER IF EXISTS invitation_token_assignment_guard ON invitation_token;

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

                    IF NEW.respondent_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.respondent_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'invitation token respondent subject must belong to the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER invitation_token_assignment_guard
                    BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, assignment_id, respondent_subject_id
                    ON invitation_token
                    FOR EACH ROW
                    EXECUTE FUNCTION invitation_token_assignment_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;
                DROP TRIGGER IF EXISTS invitation_token_assignment_guard ON invitation_token;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_invitation_token_subject_respondent_subject_id",
                table: "invitation_token");

            migrationBuilder.DropIndex(
                name: "ix_invitation_token_respondent_subject_id",
                table: "invitation_token");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token");

            migrationBuilder.Sql(
                """
                DELETE FROM invitation_token
                WHERE channel = 'identified_queue';
                """);

            migrationBuilder.DropColumn(
                name: "respondent_subject_id",
                table: "invitation_token");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token",
                sql: "channel IN ('email','sms','open_link','identified_entry')");

            migrationBuilder.Sql(
                """
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
                """);
        }
    }
}
