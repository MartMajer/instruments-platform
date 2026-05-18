using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParentTenantGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subject_group_subject_group_parent_group_id",
                table: "subject_group");

            migrationBuilder.DropIndex(
                name: "ix_subject_group_parent_group_id",
                table: "subject_group");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_subject_group_id_tenant_id",
                table: "subject_group",
                columns: new[] { "id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_subject_group_parent_group_id_tenant_id",
                table: "subject_group",
                columns: new[] { "parent_group_id", "tenant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_subject_group_subject_group_parent_group_id",
                table: "subject_group",
                columns: new[] { "parent_group_id", "tenant_id" },
                principalTable: "subject_group",
                principalColumns: new[] { "id", "tenant_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION instrument_parent_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NEW.parent_instrument_id IS NULL THEN
                        RETURN NEW;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM instrument AS parent
                        WHERE parent.id = NEW.parent_instrument_id
                          AND (
                              parent.is_global = TRUE
                              OR parent.tenant_id = NEW.tenant_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'instrument parent must be global or owned by the same tenant';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER instrument_parent_tenant_guard
                    BEFORE INSERT OR UPDATE OF parent_instrument_id, tenant_id, is_global
                    ON instrument
                    FOR EACH ROW
                    EXECUTE FUNCTION instrument_parent_tenant_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS instrument_parent_tenant_guard ON instrument;
                DROP FUNCTION IF EXISTS instrument_parent_tenant_guard();
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_subject_group_subject_group_parent_group_id",
                table: "subject_group");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_subject_group_id_tenant_id",
                table: "subject_group");

            migrationBuilder.DropIndex(
                name: "ix_subject_group_parent_group_id_tenant_id",
                table: "subject_group");

            migrationBuilder.CreateIndex(
                name: "ix_subject_group_parent_group_id",
                table: "subject_group",
                column: "parent_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_subject_group_subject_group_parent_group_id",
                table: "subject_group",
                column: "parent_group_id",
                principalTable: "subject_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
