using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifiedQueueAccessUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token");

            migrationBuilder.CreateIndex(
                name: "ux_invitation_token_identified_queue_respondent",
                table: "invitation_token",
                columns: new[] { "campaign_id", "respondent_subject_id" },
                unique: true,
                filter: "channel = 'identified_queue' AND respondent_subject_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token",
                sql: "(channel = 'identified_queue' AND respondent_subject_id IS NOT NULL AND assignment_id IS NULL) OR (channel <> 'identified_queue' AND respondent_subject_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invitation_token_identified_queue_respondent",
                table: "invitation_token");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_respondent_subject_shape",
                table: "invitation_token",
                sql: "(channel = 'identified_queue' AND respondent_subject_id IS NOT NULL) OR (channel <> 'identified_queue' AND respondent_subject_id IS NULL)");
        }
    }
}
