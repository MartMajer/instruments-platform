using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseSessionPublicHandle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_handle_hash",
                table: "response_session",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "public_handle_issued_at",
                table: "response_session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_response_session_public_handle_hash",
                table: "response_session",
                column: "public_handle_hash",
                unique: true,
                filter: "public_handle_hash IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_response_session_public_handle_hash",
                table: "response_session",
                sql: "public_handle_hash IS NULL OR public_handle_hash ~ '^[0-9a-f]{64}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_response_session_public_handle_hash",
                table: "response_session");

            migrationBuilder.DropCheckConstraint(
                name: "ck_response_session_public_handle_hash",
                table: "response_session");

            migrationBuilder.DropColumn(
                name: "public_handle_hash",
                table: "response_session");

            migrationBuilder.DropColumn(
                name: "public_handle_issued_at",
                table: "response_session");
        }
    }
}
