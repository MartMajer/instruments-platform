using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeEmailSuppressionToStudy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_email_suppression_tenant_id_recipient_active",
                table: "email_suppression");

            migrationBuilder.AddColumn<Guid>(
                name: "campaign_series_id",
                table: "email_suppression",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_email_suppression_tenant_id_recipient_series_active",
                table: "email_suppression",
                columns: new[] { "tenant_id", "recipient", "campaign_series_id" },
                unique: true,
                filter: "released_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_email_suppression_tenant_id_recipient_series_active",
                table: "email_suppression");

            migrationBuilder.DropColumn(
                name: "campaign_series_id",
                table: "email_suppression");

            migrationBuilder.CreateIndex(
                name: "ux_email_suppression_tenant_id_recipient_active",
                table: "email_suppression",
                columns: new[] { "tenant_id", "recipient" },
                unique: true,
                filter: "released_at IS NULL");
        }
    }
}
