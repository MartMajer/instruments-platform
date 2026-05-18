using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518113000_AllowCompletedResponseSessionWithdrawalProofWithoutTarget")]
    public partial class AllowCompletedResponseSessionWithdrawalProofWithoutTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_shape;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_shape
                    CHECK (
                        (target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (
                            target_kind = 'response_session'
                            AND subject_id IS NULL
                            AND participant_code_id IS NULL
                            AND (
                                response_session_id IS NOT NULL
                                OR (
                                    action_after = 'delete'
                                    AND status = 'completed'
                                    AND response_session_id IS NULL
                                )
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
                ALTER TABLE withdrawal_event
                    DROP CONSTRAINT IF EXISTS ck_withdrawal_event_target_shape;

                ALTER TABLE withdrawal_event
                    ADD CONSTRAINT ck_withdrawal_event_target_shape
                    CHECK (
                        (target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL)
                        OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL)
                        OR (target_kind = 'response_session' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NOT NULL)
                    );
                """);
        }
    }
}
