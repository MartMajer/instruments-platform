using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentRightsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_validity_status",
                table: "instrument");

            migrationBuilder.AddColumn<string>(
                name: "provenance_note",
                table: "instrument",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rights_scope",
                table: "instrument",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "platform_granted");

            migrationBuilder.AddColumn<string>(
                name: "rights_status",
                table: "instrument",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "verified");

            migrationBuilder.AddColumn<string>(
                name: "validity_label",
                table: "instrument",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "official");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_private_import_shape",
                table: "instrument",
                sql: "validity_status <> 'private_import' OR (tenant_id IS NOT NULL AND parent_instrument_id IS NULL AND is_global = FALSE)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_rights_scope",
                table: "instrument",
                sql: "rights_scope IN ('platform_granted','tenant_provided')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_rights_status",
                table: "instrument",
                sql: "rights_status IN ('verified','attested_by_tenant','unverified_internal_demo','expired')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_validity_label",
                table: "instrument",
                sql: "validity_label IN ('official','tenant_provided','adapted','experimental','rights_unverified')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_validity_status",
                table: "instrument",
                sql: "validity_status IN ('canonical','derived','private_import','draft','retired')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_private_import_shape",
                table: "instrument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_rights_scope",
                table: "instrument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_rights_status",
                table: "instrument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_validity_label",
                table: "instrument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instrument_validity_status",
                table: "instrument");

            migrationBuilder.DropColumn(
                name: "provenance_note",
                table: "instrument");

            migrationBuilder.DropColumn(
                name: "rights_scope",
                table: "instrument");

            migrationBuilder.DropColumn(
                name: "rights_status",
                table: "instrument");

            migrationBuilder.DropColumn(
                name: "validity_label",
                table: "instrument");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instrument_validity_status",
                table: "instrument",
                sql: "validity_status IN ('canonical','derived','draft','retired')");
        }
    }
}
