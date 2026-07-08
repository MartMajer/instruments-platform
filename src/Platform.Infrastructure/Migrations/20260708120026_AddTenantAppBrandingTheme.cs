using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAppBrandingTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "app_branding_background_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "app_branding_ink_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "app_branding_surface_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "app_branding_topbar_color_hex",
                table: "tenant",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_background_color_hex",
                table: "tenant",
                sql: "app_branding_background_color_hex IS NULL OR app_branding_background_color_hex ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_ink_color_hex",
                table: "tenant",
                sql: "app_branding_ink_color_hex IS NULL OR app_branding_ink_color_hex ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_surface_color_hex",
                table: "tenant",
                sql: "app_branding_surface_color_hex IS NULL OR app_branding_surface_color_hex ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_app_branding_topbar_color_hex",
                table: "tenant",
                sql: "app_branding_topbar_color_hex IS NULL OR app_branding_topbar_color_hex ~ '^#[0-9A-Fa-f]{6}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_background_color_hex",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_ink_color_hex",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_surface_color_hex",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_app_branding_topbar_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_background_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_ink_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_surface_color_hex",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "app_branding_topbar_color_hex",
                table: "tenant");
        }
    }
}
