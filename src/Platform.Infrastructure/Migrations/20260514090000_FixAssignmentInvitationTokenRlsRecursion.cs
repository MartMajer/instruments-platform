using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260514090000_FixAssignmentInvitationTokenRlsRecursion")]
    public partial class FixAssignmentInvitationTokenRlsRecursion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS assignment_tenant_isolation ON assignment;
                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;

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
                    );

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
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS assignment_tenant_isolation ON assignment;
                DROP POLICY IF EXISTS invitation_token_tenant_isolation ON invitation_token;

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
                """);
        }
    }
}
