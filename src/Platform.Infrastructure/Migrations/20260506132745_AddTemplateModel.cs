using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_translation_exactly_one_instrument_target",
                table: "translation");

            migrationBuilder.AddColumn<Guid>(
                name: "choice_option_id",
                table: "translation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "survey_template_id",
                table: "translation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_question_id",
                table: "translation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_section_id",
                table: "translation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "survey_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_survey_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_survey_template_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_survey_template_user_account_created_by",
                        column: x => x.created_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_version",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    semver = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_version", x => x.id);
                    table.CheckConstraint("ck_template_version_global_locked", "is_global = FALSE OR is_locked = TRUE");
                    table.CheckConstraint("ck_template_version_publish_shape", "(status = 'published' AND published_at IS NOT NULL) OR (status <> 'published')");
                    table.CheckConstraint("ck_template_version_status", "status IN ('draft','published','retired')");
                    table.ForeignKey(
                        name: "fk_template_version_instrument_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instrument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_template_version_survey_template_template_id",
                        column: x => x.template_id,
                        principalTable: "survey_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_template_version_user_account_published_by",
                        column: x => x.published_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scale",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    min_value = table.Column<int>(type: "integer", nullable: false),
                    max_value = table.Column<int>(type: "integer", nullable: false),
                    step = table.Column<int>(type: "integer", nullable: false),
                    na_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    anchors = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scale", x => x.id);
                    table.UniqueConstraint("ak_scale_id_template_version_id", x => new { x.id, x.template_version_id });
                    table.CheckConstraint("ck_scale_range", "max_value > min_value");
                    table.CheckConstraint("ck_scale_step_positive", "step > 0");
                    table.CheckConstraint("ck_scale_type", "type IN ('likert','nps','binary','numeric')");
                    table.ForeignKey(
                        name: "fk_scale_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "section",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    title_default = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_section", x => x.id);
                    table.UniqueConstraint("ak_section_id_template_version_id", x => new { x.id, x.template_version_id });
                    table.CheckConstraint("ck_section_ordinal_positive", "ordinal > 0");
                    table.ForeignKey(
                        name: "fk_section_section_parent_section_id",
                        columns: x => new { x.parent_section_id, x.template_version_id },
                        principalTable: "section",
                        principalColumns: new[] { "id", "template_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_section_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "question",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    text_default = table.Column<string>(type: "text", nullable: false),
                    description_default = table.Column<string>(type: "text", nullable: true),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    reverse_coded = table.Column<bool>(type: "boolean", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    variable_label = table.Column<string>(type: "text", nullable: true),
                    measurement_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    missing_codes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question", x => x.id);
                    table.CheckConstraint("ck_question_measurement_level", "measurement_level IS NULL OR measurement_level IN ('nominal','ordinal','scale')");
                    table.CheckConstraint("ck_question_ordinal_positive", "ordinal > 0");
                    table.CheckConstraint("ck_question_scale_backed", "type NOT IN ('likert','nps') OR scale_id IS NOT NULL");
                    table.CheckConstraint("ck_question_type", "type IN ('likert','single','multi','text','number','date','matrix','nps','ranking','file','pairwise')");
                    table.CheckConstraint("ck_question_weight_positive", "weight > 0");
                    table.ForeignKey(
                        name: "fk_question_scale_scale_id",
                        columns: x => new { x.scale_id, x.template_version_id },
                        principalTable: "scale",
                        principalColumns: new[] { "id", "template_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_question_section_section_id",
                        columns: x => new { x.section_id, x.template_version_id },
                        principalTable: "section",
                        principalColumns: new[] { "id", "template_version_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_question_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "choice_option",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    label_default = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_other = table.Column<bool>(type: "boolean", nullable: false),
                    is_exclusive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_choice_option", x => x.id);
                    table.CheckConstraint("ck_choice_option_ordinal_positive", "ordinal > 0");
                    table.ForeignKey(
                        name: "fk_choice_option_question_question_id",
                        column: x => x.question_id,
                        principalTable: "question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_choice_option",
                table: "translation",
                columns: new[] { "choice_option_id", "field", "locale" },
                unique: true,
                filter: "choice_option_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_survey_template",
                table: "translation",
                columns: new[] { "survey_template_id", "field", "locale" },
                unique: true,
                filter: "survey_template_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_template_question",
                table: "translation",
                columns: new[] { "template_question_id", "field", "locale" },
                unique: true,
                filter: "template_question_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_template_section",
                table: "translation",
                columns: new[] { "template_section_id", "field", "locale" },
                unique: true,
                filter: "template_section_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_translation_exactly_one_target",
                table: "translation",
                sql: "((instrument_id IS NOT NULL)::int\n+ (instrument_subscale_id IS NOT NULL)::int\n+ (instrument_item_id IS NOT NULL)::int\n+ (survey_template_id IS NOT NULL)::int\n+ (template_section_id IS NOT NULL)::int\n+ (template_question_id IS NOT NULL)::int\n+ (choice_option_id IS NOT NULL)::int) = 1");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_item_question_id",
                table: "instrument_item",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_canonical_template_version_id",
                table: "instrument",
                column: "canonical_template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_choice_option_question_id_ordinal",
                table: "choice_option",
                columns: new[] { "question_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_choice_option_question_id_value",
                table: "choice_option",
                columns: new[] { "question_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_scale_id_template_version_id",
                table: "question",
                columns: new[] { "scale_id", "template_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_question_section_id_template_version_id",
                table: "question",
                columns: new[] { "section_id", "template_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_question_template_version_id_code",
                table: "question",
                columns: new[] { "template_version_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_question_template_version_id_section_id_ordinal",
                table: "question",
                columns: new[] { "template_version_id", "section_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ix_scale_template_version_id_code",
                table: "scale",
                columns: new[] { "template_version_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_section_parent_section_id_template_version_id",
                table: "section",
                columns: new[] { "parent_section_id", "template_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_section_template_version_id_code",
                table: "section",
                columns: new[] { "template_version_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_section_template_version_id_parent_section_id_ordinal",
                table: "section",
                columns: new[] { "template_version_id", "parent_section_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ix_survey_template_created_by",
                table: "survey_template",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_survey_template_tenant_id",
                table: "survey_template",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_survey_template_tenant_id_name",
                table: "survey_template",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_template_version_instrument_id",
                table: "template_version",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_template_version_published_by",
                table: "template_version",
                column: "published_by");

            migrationBuilder.CreateIndex(
                name: "ix_template_version_template_id_semver",
                table: "template_version",
                columns: new[] { "template_id", "semver" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_instrument_template_version_canonical_template_version_id",
                table: "instrument",
                column: "canonical_template_version_id",
                principalTable: "template_version",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_instrument_item_question_question_id",
                table: "instrument_item",
                column: "question_id",
                principalTable: "question",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_translation_choice_option_choice_option_id",
                table: "translation",
                column: "choice_option_id",
                principalTable: "choice_option",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_translation_question_template_question_id",
                table: "translation",
                column: "template_question_id",
                principalTable: "question",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_translation_section_template_section_id",
                table: "translation",
                column: "template_section_id",
                principalTable: "section",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_translation_survey_template_survey_template_id",
                table: "translation",
                column: "survey_template_id",
                principalTable: "survey_template",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE survey_template ENABLE ROW LEVEL SECURITY;
                ALTER TABLE survey_template FORCE ROW LEVEL SECURITY;

                CREATE POLICY survey_template_tenant_or_global_read ON survey_template
                    FOR SELECT
                    USING (
                        tenant_id = current_setting('app.current_tenant_id')::uuid
                        OR tenant_id IS NULL
                    );

                CREATE POLICY survey_template_tenant_write ON survey_template
                    FOR ALL
                    USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
                    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

                ALTER TABLE template_version ENABLE ROW LEVEL SECURITY;
                ALTER TABLE template_version FORCE ROW LEVEL SECURITY;

                CREATE POLICY template_version_tenant_or_global_read ON template_version
                    FOR SELECT
                    USING (
                        is_global = TRUE
                        OR EXISTS (
                            SELECT 1
                            FROM survey_template AS st
                            WHERE st.id = template_version.template_id
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                CREATE POLICY template_version_tenant_write ON template_version
                    FOR ALL
                    USING (
                        is_global = FALSE
                        AND EXISTS (
                            SELECT 1
                            FROM survey_template AS st
                            WHERE st.id = template_version.template_id
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        is_global = FALSE
                        AND EXISTS (
                            SELECT 1
                            FROM survey_template AS st
                            WHERE st.id = template_version.template_id
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE section ENABLE ROW LEVEL SECURITY;
                ALTER TABLE section FORCE ROW LEVEL SECURITY;

                CREATE POLICY section_tenant_or_global_read ON section
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = section.template_version_id
                              AND (
                                  tv.is_global = TRUE
                                  OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                              )
                        )
                    );

                CREATE POLICY section_tenant_write ON section
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = section.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = section.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE scale ENABLE ROW LEVEL SECURITY;
                ALTER TABLE scale FORCE ROW LEVEL SECURITY;

                CREATE POLICY scale_tenant_or_global_read ON scale
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scale.template_version_id
                              AND (
                                  tv.is_global = TRUE
                                  OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                              )
                        )
                    );

                CREATE POLICY scale_tenant_write ON scale
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scale.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = scale.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE question ENABLE ROW LEVEL SECURITY;
                ALTER TABLE question FORCE ROW LEVEL SECURITY;

                CREATE POLICY question_tenant_or_global_read ON question
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = question.template_version_id
                              AND (
                                  tv.is_global = TRUE
                                  OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                              )
                        )
                    );

                CREATE POLICY question_tenant_write ON question
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = question.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM template_version AS tv
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE tv.id = question.template_version_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                ALTER TABLE choice_option ENABLE ROW LEVEL SECURITY;
                ALTER TABLE choice_option FORCE ROW LEVEL SECURITY;

                CREATE POLICY choice_option_tenant_or_global_read ON choice_option
                    FOR SELECT
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM question AS q
                            JOIN template_version AS tv ON tv.id = q.template_version_id
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE q.id = choice_option.question_id
                              AND (
                                  tv.is_global = TRUE
                                  OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                              )
                        )
                    );

                CREATE POLICY choice_option_tenant_write ON choice_option
                    FOR ALL
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM question AS q
                            JOIN template_version AS tv ON tv.id = q.template_version_id
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE q.id = choice_option.question_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    )
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM question AS q
                            JOIN template_version AS tv ON tv.id = q.template_version_id
                            JOIN survey_template AS st ON st.id = tv.template_id
                            WHERE q.id = choice_option.question_id
                              AND tv.is_global = FALSE
                              AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                        )
                    );

                DROP POLICY IF EXISTS translation_tenant_or_global_read ON translation;
                DROP POLICY IF EXISTS translation_tenant_write ON translation;

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
                        OR (
                            survey_template_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM survey_template AS st
                                WHERE st.id = translation.survey_template_id
                                  AND (
                                      st.tenant_id = current_setting('app.current_tenant_id')::uuid
                                      OR st.tenant_id IS NULL
                                  )
                            )
                        )
                        OR (
                            template_section_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM section AS s
                                JOIN template_version AS tv ON tv.id = s.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE s.id = translation.template_section_id
                                  AND (
                                      tv.is_global = TRUE
                                      OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  )
                            )
                        )
                        OR (
                            template_question_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM question AS q
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE q.id = translation.template_question_id
                                  AND (
                                      tv.is_global = TRUE
                                      OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
                                  )
                            )
                        )
                        OR (
                            choice_option_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM choice_option AS c
                                JOIN question AS q ON q.id = c.question_id
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE c.id = translation.choice_option_id
                                  AND (
                                      tv.is_global = TRUE
                                      OR st.tenant_id = current_setting('app.current_tenant_id')::uuid
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
                        OR (
                            survey_template_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM survey_template AS st
                                WHERE st.id = translation.survey_template_id
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            template_section_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM section AS s
                                JOIN template_version AS tv ON tv.id = s.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE s.id = translation.template_section_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            template_question_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM question AS q
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE q.id = translation.template_question_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            choice_option_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM choice_option AS c
                                JOIN question AS q ON q.id = c.question_id
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE c.id = translation.choice_option_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
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
                        OR (
                            survey_template_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM survey_template AS st
                                WHERE st.id = translation.survey_template_id
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            template_section_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM section AS s
                                JOIN template_version AS tv ON tv.id = s.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE s.id = translation.template_section_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            template_question_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM question AS q
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE q.id = translation.template_question_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                        OR (
                            choice_option_id IS NOT NULL
                            AND EXISTS (
                                SELECT 1
                                FROM choice_option AS c
                                JOIN question AS q ON q.id = c.question_id
                                JOIN template_version AS tv ON tv.id = q.template_version_id
                                JOIN survey_template AS st ON st.id = tv.template_id
                                WHERE c.id = translation.choice_option_id
                                  AND tv.is_global = FALSE
                                  AND st.tenant_id = current_setting('app.current_tenant_id')::uuid
                            )
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS translation_tenant_or_global_read ON translation;
                DROP POLICY IF EXISTS translation_tenant_write ON translation;

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

            migrationBuilder.DropForeignKey(
                name: "fk_instrument_template_version_canonical_template_version_id",
                table: "instrument");

            migrationBuilder.DropForeignKey(
                name: "fk_instrument_item_question_question_id",
                table: "instrument_item");

            migrationBuilder.DropForeignKey(
                name: "fk_translation_choice_option_choice_option_id",
                table: "translation");

            migrationBuilder.DropForeignKey(
                name: "fk_translation_question_template_question_id",
                table: "translation");

            migrationBuilder.DropForeignKey(
                name: "fk_translation_section_template_section_id",
                table: "translation");

            migrationBuilder.DropForeignKey(
                name: "fk_translation_survey_template_survey_template_id",
                table: "translation");

            migrationBuilder.DropTable(
                name: "choice_option");

            migrationBuilder.DropTable(
                name: "question");

            migrationBuilder.DropTable(
                name: "scale");

            migrationBuilder.DropTable(
                name: "section");

            migrationBuilder.DropTable(
                name: "template_version");

            migrationBuilder.DropTable(
                name: "survey_template");

            migrationBuilder.DropIndex(
                name: "ix_translation_unique_choice_option",
                table: "translation");

            migrationBuilder.DropIndex(
                name: "ix_translation_unique_survey_template",
                table: "translation");

            migrationBuilder.DropIndex(
                name: "ix_translation_unique_template_question",
                table: "translation");

            migrationBuilder.DropIndex(
                name: "ix_translation_unique_template_section",
                table: "translation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_translation_exactly_one_target",
                table: "translation");

            migrationBuilder.DropIndex(
                name: "ix_instrument_item_question_id",
                table: "instrument_item");

            migrationBuilder.DropIndex(
                name: "ix_instrument_canonical_template_version_id",
                table: "instrument");

            migrationBuilder.DropColumn(
                name: "choice_option_id",
                table: "translation");

            migrationBuilder.DropColumn(
                name: "survey_template_id",
                table: "translation");

            migrationBuilder.DropColumn(
                name: "template_question_id",
                table: "translation");

            migrationBuilder.DropColumn(
                name: "template_section_id",
                table: "translation");

            migrationBuilder.AddCheckConstraint(
                name: "ck_translation_exactly_one_instrument_target",
                table: "translation",
                sql: "((instrument_id IS NOT NULL)::int\n+ (instrument_subscale_id IS NOT NULL)::int\n+ (instrument_item_id IS NOT NULL)::int) = 1");
        }
    }
}
