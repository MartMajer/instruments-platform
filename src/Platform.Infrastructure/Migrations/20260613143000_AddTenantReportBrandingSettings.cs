using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260613143000_AddTenantReportBrandingSettings")]
    public partial class AddTenantReportBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "report_branding_organization_label",
                table: "tenant",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report_branding_report_title",
                table: "tenant",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report_branding_accent_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report_branding_layout_variant",
                table: "tenant",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "report_branding_updated_at",
                table: "tenant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_report_branding_accent_color_hex",
                table: "tenant",
                sql: "report_branding_accent_color_hex IS NULL OR report_branding_accent_color_hex ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_report_branding_layout_variant",
                table: "tenant",
                sql: "report_branding_layout_variant IS NULL OR report_branding_layout_variant IN ('standard','compact','compliance')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_report_branding_layout_variant",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_report_branding_accent_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "report_branding_updated_at",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "report_branding_layout_variant",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "report_branding_accent_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "report_branding_report_title",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "report_branding_organization_label",
                table: "tenant");
        }
    }
}
