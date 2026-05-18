using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleResponseSessionsPerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session",
                column: "assignment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session",
                column: "assignment_id",
                unique: true);
        }
    }
}
