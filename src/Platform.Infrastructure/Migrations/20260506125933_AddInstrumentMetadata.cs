using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "instrument",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    domain = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    construct_category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    developers = table.Column<string[]>(type: "text[]", nullable: false),
                    year_first_published = table.Column<int>(type: "integer", nullable: true),
                    citation_apa = table.Column<string>(type: "text", nullable: false),
                    doi = table.Column<string>(type: "text", nullable: true),
                    license_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    license_terms_url = table.Column<string>(type: "text", nullable: true),
                    license_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    vendor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    validity_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    parent_instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    canonical_template_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument", x => x.id);
                    table.CheckConstraint("ck_instrument_canonical_parent_shape", "validity_status <> 'canonical' OR parent_instrument_id IS NULL");
                    table.CheckConstraint("ck_instrument_derived_parent_shape", "validity_status <> 'derived' OR (tenant_id IS NOT NULL AND parent_instrument_id IS NOT NULL AND is_global = FALSE)");
                    table.CheckConstraint("ck_instrument_domain", "domain IN ('psychometric','ergonomic','medical','educational','regulatory','other')");
                    table.CheckConstraint("ck_instrument_global_tenant_shape", "(is_global = TRUE AND tenant_id IS NULL) OR (is_global = FALSE AND tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_instrument_license_type", "license_type IN ('free','free_academic','paid','unknown')");
                    table.CheckConstraint("ck_instrument_validity_status", "validity_status IN ('canonical','derived','draft','retired')");
                    table.ForeignKey(
                        name: "fk_instrument_instrument_parent_instrument_id",
                        column: x => x.parent_instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instrument_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instrument_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subscale_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reverse_coded = table.Column<bool>(type: "boolean", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_item", x => x.id);
                    table.CheckConstraint("ck_instrument_item_ordinal_positive", "ordinal > 0");
                    table.ForeignKey(
                        name: "fk_instrument_item_instrument_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instrument_norm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscale_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    norm_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    population = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sample_size = table.Column<int>(type: "integer", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    mean = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    sd = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    percentiles = table.Column<string>(type: "jsonb", nullable: false),
                    source_citation = table.Column<string>(type: "text", nullable: true),
                    source_year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_norm", x => x.id);
                    table.CheckConstraint("ck_instrument_norm_sample_size_positive", "sample_size > 0");
                    table.CheckConstraint("ck_instrument_norm_type", "norm_type IN ('published_instrument','platform_benchmark','tenant_benchmark')");
                    table.ForeignKey(
                        name: "fk_instrument_norm_instrument_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instrument_subscale",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    reliability_alpha_published = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    scoring_method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_subscale", x => x.id);
                    table.CheckConstraint("ck_instrument_subscale_item_count_positive", "item_count > 0");
                    table.CheckConstraint("ck_instrument_subscale_reliability_alpha_range", "reliability_alpha_published IS NULL OR (reliability_alpha_published >= 0 AND reliability_alpha_published <= 1)");
                    table.CheckConstraint("ck_instrument_subscale_scoring_method", "scoring_method IN ('mean','sum','weighted')");
                    table.ForeignKey(
                        name: "fk_instrument_subscale_instrument_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "translation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_subscale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    field = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_translation", x => x.id);
                    table.CheckConstraint("ck_translation_exactly_one_instrument_target", "((instrument_id IS NOT NULL)::int\n+ (instrument_subscale_id IS NOT NULL)::int\n+ (instrument_item_id IS NOT NULL)::int) = 1");
                    table.CheckConstraint("ck_translation_status", "status IN ('draft_translation','back_translated','reconciled','approved_canonical_equivalent','approved_derivative','rejected')");
                    table.ForeignKey(
                        name: "fk_translation_instrument_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_translation_instrument_item_instrument_item_id",
                        column: x => x.instrument_item_id,
                        principalTable: "instrument_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_translation_instrument_subscale_instrument_subscale_id",
                        column: x => x.instrument_subscale_id,
                        principalTable: "instrument_subscale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_global_code_version",
                table: "instrument",
                columns: new[] { "code", "version" },
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_parent_instrument_id",
                table: "instrument",
                column: "parent_instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_tenant_id",
                table: "instrument",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_tenant_id_code_version",
                table: "instrument",
                columns: new[] { "tenant_id", "code", "version" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_item_instrument_id_code",
                table: "instrument_item",
                columns: new[] { "instrument_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instrument_item_instrument_id_ordinal",
                table: "instrument_item",
                columns: new[] { "instrument_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instrument_norm_lookup",
                table: "instrument_norm",
                columns: new[] { "instrument_id", "subscale_code", "locale", "norm_type" });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_subscale_instrument_id_code",
                table: "instrument_subscale",
                columns: new[] { "instrument_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_instrument",
                table: "translation",
                columns: new[] { "instrument_id", "field", "locale" },
                unique: true,
                filter: "instrument_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_instrument_item",
                table: "translation",
                columns: new[] { "instrument_item_id", "field", "locale" },
                unique: true,
                filter: "instrument_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_instrument_subscale",
                table: "translation",
                columns: new[] { "instrument_subscale_id", "field", "locale" },
                unique: true,
                filter: "instrument_subscale_id IS NOT NULL");

            migrationBuilder.Sql(
                """
                ALTER TABLE instrument ENABLE ROW LEVEL SECURITY;
                ALTER TABLE instrument FORCE ROW LEVEL SECURITY;

                CREATE POLICY instrument_tenant_or_global_read ON instrument
                    FOR SELECT
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        OR is_global = TRUE
                    );

                CREATE POLICY instrument_tenant_write ON instrument
                    FOR INSERT
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND is_global = FALSE
                    );

                CREATE POLICY instrument_tenant_update ON instrument
                    FOR UPDATE
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND is_global = FALSE
                    )
                    WITH CHECK (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND is_global = FALSE
                    );

                CREATE POLICY instrument_tenant_delete ON instrument
                    FOR DELETE
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        AND is_global = FALSE
                    );

                ALTER TABLE instrument_subscale ENABLE ROW LEVEL SECURITY;
                ALTER TABLE instrument_subscale FORCE ROW LEVEL SECURITY;

                CREATE POLICY instrument_subscale_tenant_or_global_read ON instrument_subscale
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_subscale.instrument_id
                              AND (
                                  i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  OR i.is_global = TRUE
                              )
                        )
                    );

                CREATE POLICY instrument_subscale_tenant_write ON instrument_subscale
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_subscale.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_subscale.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    );

                ALTER TABLE instrument_item ENABLE ROW LEVEL SECURITY;
                ALTER TABLE instrument_item FORCE ROW LEVEL SECURITY;

                CREATE POLICY instrument_item_tenant_or_global_read ON instrument_item
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_item.instrument_id
                              AND (
                                  i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  OR i.is_global = TRUE
                              )
                        )
                    );

                CREATE POLICY instrument_item_tenant_write ON instrument_item
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_item.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_item.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    );

                ALTER TABLE instrument_norm ENABLE ROW LEVEL SECURITY;
                ALTER TABLE instrument_norm FORCE ROW LEVEL SECURITY;

                CREATE POLICY instrument_norm_tenant_or_global_read ON instrument_norm
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_norm.instrument_id
                              AND (
                                  i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  OR i.is_global = TRUE
                              )
                        )
                    );

                CREATE POLICY instrument_norm_tenant_write ON instrument_norm
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_norm.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM instrument AS i
                            WHERE i.id = instrument_norm.instrument_id
                              AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                              AND i.is_global = FALSE
                        )
                    );

                ALTER TABLE translation ENABLE ROW LEVEL SECURITY;
                ALTER TABLE translation FORCE ROW LEVEL SECURITY;

                CREATE POLICY translation_tenant_or_global_read ON translation
                    FOR SELECT
                    USING (
                        (
                            instrument_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument AS i
                                WHERE i.id = translation.instrument_id
                                  AND (
                                      i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                      OR i.is_global = TRUE
                                  )
                            )
                        )
                        OR (
                            instrument_subscale_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_subscale AS s
                                JOIN instrument AS i ON i.id = s.instrument_id
                                WHERE s.id = translation.instrument_subscale_id
                                  AND (
                                      i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                      OR i.is_global = TRUE
                                  )
                            )
                        )
                        OR (
                            instrument_item_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_item AS item
                                JOIN instrument AS i ON i.id = item.instrument_id
                                WHERE item.id = translation.instrument_item_id
                                  AND (
                                      i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                      OR i.is_global = TRUE
                                  )
                            )
                        )
                    );

                CREATE POLICY translation_tenant_write ON translation
                    FOR ALL
                    USING (
                        (
                            instrument_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument AS i
                                WHERE i.id = translation.instrument_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                        OR (
                            instrument_subscale_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_subscale AS s
                                JOIN instrument AS i ON i.id = s.instrument_id
                                WHERE s.id = translation.instrument_subscale_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                        OR (
                            instrument_item_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_item AS item
                                JOIN instrument AS i ON i.id = item.instrument_id
                                WHERE item.id = translation.instrument_item_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                    )
                    WITH CHECK (
                        (
                            instrument_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument AS i
                                WHERE i.id = translation.instrument_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                        OR (
                            instrument_subscale_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_subscale AS s
                                JOIN instrument AS i ON i.id = s.instrument_id
                                WHERE s.id = translation.instrument_subscale_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                        OR (
                            instrument_item_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM instrument_item AS item
                                JOIN instrument AS i ON i.id = item.instrument_id
                                WHERE item.id = translation.instrument_item_id
                                  AND i.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  AND i.is_global = FALSE
                            )
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instrument_norm");

            migrationBuilder.DropTable(
                name: "translation");

            migrationBuilder.DropTable(
                name: "instrument_item");

            migrationBuilder.DropTable(
                name: "instrument_subscale");

            migrationBuilder.DropTable(
                name: "instrument");
        }
    }
}
