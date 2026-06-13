using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260612143000_FilterRetiredScoringRuleUniqueness")]
    public partial class FilterRetiredScoringRuleUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "rule_key", "rule_version" },
                unique: true,
                filter: "status <> 'retired'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "rule_key", "rule_version" },
                unique: true);
        }
    }
}
