using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAppBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "app_branding_accent_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "app_branding_logo_content_type",
                table: "tenant",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "app_branding_logo_object_key",
                table: "tenant",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "app_branding_updated_at",
                table: "tenant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "app_branding_updated_by",
                table: "tenant",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_accent_color_hex",
                table: "tenant",
                sql: "app_branding_accent_color_hex IS NULL OR app_branding_accent_color_hex ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_logo_content_type",
                table: "tenant",
                sql: "app_branding_logo_content_type IS NULL OR app_branding_logo_content_type IN ('image/png','image/jpeg','image/webp')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_accent_color_hex",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_logo_content_type",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_accent_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_logo_content_type",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_logo_object_key",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_updated_at",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_updated_by",
                table: "tenant");
        }
    }
}
