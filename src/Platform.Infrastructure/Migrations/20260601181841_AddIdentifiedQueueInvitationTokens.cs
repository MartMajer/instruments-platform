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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "respondent_subject_id",
                table: "invitation_token");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invitation_token_channel",
                table: "invitation_token",
                sql: "channel IN ('email','sms','open_link','identified_entry')");
        }
    }
}
