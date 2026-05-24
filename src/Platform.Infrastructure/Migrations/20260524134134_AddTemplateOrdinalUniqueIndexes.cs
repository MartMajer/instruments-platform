using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateOrdinalUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_section_template_version_id_ordinal",
                table: "section",
                columns: new[] { "template_version_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_template_version_id_ordinal",
                table: "question",
                columns: new[] { "template_version_id", "ordinal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_section_template_version_id_ordinal",
                table: "section");

            migrationBuilder.DropIndex(
                name: "ix_question_template_version_id_ordinal",
                table: "question");
        }
    }
}
