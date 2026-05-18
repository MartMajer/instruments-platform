using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subject",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "citext", nullable: true),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject", x => x.id);
                    table.ForeignKey(
                        name: "fk_subject_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_user_account_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subject_group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    parent_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_group", x => x.id);
                    table.ForeignKey(
                        name: "fk_subject_group_subject_group_parent_group_id",
                        column: x => x.parent_group_id,
                        principalTable: "subject_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_group_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subject_relationship",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rel_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_relationship", x => x.id);
                    table.CheckConstraint("ck_subject_relationship_not_self_unless_self_type", "(subject_id <> related_subject_id AND rel_type <> 'self') OR (subject_id = related_subject_id AND rel_type = 'self')");
                    table.CheckConstraint("ck_subject_relationship_valid_range", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_subject_relationship_subject_related_subject_id",
                        column: x => x.related_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_relationship_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_relationship_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subject_membership",
                columns: table => new
                {
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_in_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subject_membership", x => new { x.subject_id, x.group_id });
                    table.CheckConstraint("ck_subject_membership_valid_range", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_subject_membership_subject_group_group_id",
                        column: x => x.group_id,
                        principalTable: "subject_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_membership_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subject_attributes_gin",
                table: "subject",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_subject_tenant_id",
                table: "subject",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_tenant_id_email",
                table: "subject",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subject_tenant_id_external_id",
                table: "subject",
                columns: new[] { "tenant_id", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subject_user_account_id",
                table: "subject",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_group_parent_group_id",
                table: "subject_group",
                column: "parent_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_group_tenant_id",
                table: "subject_group",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_membership_group_id",
                table: "subject_membership",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_relationship_related_subject_id_rel_type",
                table: "subject_relationship",
                columns: new[] { "related_subject_id", "rel_type" });

            migrationBuilder.CreateIndex(
                name: "ix_subject_relationship_subject_id_rel_type",
                table: "subject_relationship",
                columns: new[] { "subject_id", "rel_type" });

            migrationBuilder.CreateIndex(
                name: "ix_subject_relationship_tenant_id",
                table: "subject_relationship",
                column: "tenant_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE subject ENABLE ROW LEVEL SECURITY;
                ALTER TABLE subject FORCE ROW LEVEL SECURITY;

                CREATE POLICY subject_tenant_isolation ON subject
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND (
                            user_account_id IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM user_account AS u
                                WHERE u.id = subject.user_account_id
                                  AND u.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                    );

                ALTER TABLE subject_group ENABLE ROW LEVEL SECURITY;
                ALTER TABLE subject_group FORCE ROW LEVEL SECURITY;

                CREATE POLICY subject_group_tenant_isolation ON subject_group
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE subject_membership ENABLE ROW LEVEL SECURITY;
                ALTER TABLE subject_membership FORCE ROW LEVEL SECURITY;

                CREATE POLICY subject_membership_tenant_isolation ON subject_membership
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM subject AS s
                            WHERE s.id = subject_membership.subject_id
                              AND s.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject_group AS g
                            WHERE g.id = subject_membership.group_id
                              AND g.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM subject AS s
                            WHERE s.id = subject_membership.subject_id
                              AND s.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject_group AS g
                            WHERE g.id = subject_membership.group_id
                              AND g.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE subject_relationship ENABLE ROW LEVEL SECURITY;
                ALTER TABLE subject_relationship FORCE ROW LEVEL SECURITY;

                CREATE POLICY subject_relationship_tenant_isolation ON subject_relationship
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS subject_row
                            WHERE subject_row.id = subject_relationship.subject_id
                              AND subject_row.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS related
                            WHERE related.id = subject_relationship.related_subject_id
                              AND related.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS subject_row
                            WHERE subject_row.id = subject_relationship.subject_id
                              AND subject_row.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                        AND EXISTS (
                            SELECT 1
                            FROM subject AS related
                            WHERE related.id = subject_relationship.related_subject_id
                              AND related.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subject_membership");

            migrationBuilder.DropTable(
                name: "subject_relationship");

            migrationBuilder.DropTable(
                name: "subject_group");

            migrationBuilder.DropTable(
                name: "subject");
        }
    }
}
