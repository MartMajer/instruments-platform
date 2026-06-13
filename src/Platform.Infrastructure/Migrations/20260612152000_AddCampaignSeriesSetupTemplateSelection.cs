using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260612152000_AddCampaignSeriesSetupTemplateSelection")]
    public partial class AddCampaignSeriesSetupTemplateSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "setup_template_version_id",
                table: "campaign_series",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_series_setup_template_version_id",
                table: "campaign_series",
                column: "setup_template_version_id",
                filter: "setup_template_version_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_series_template_version_setup_template_version_id",
                table: "campaign_series",
                column: "setup_template_version_id",
                principalTable: "template_version",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_campaign_series_template_version_setup_template_version_id",
                table: "campaign_series");

            migrationBuilder.DropIndex(
                name: "ix_campaign_series_setup_template_version_id",
                table: "campaign_series");

            migrationBuilder.DropColumn(
                name: "setup_template_version_id",
                table: "campaign_series");
        }
    }
}
