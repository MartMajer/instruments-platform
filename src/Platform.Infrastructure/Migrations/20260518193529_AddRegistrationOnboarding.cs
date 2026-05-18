using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "audit_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    change_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    before = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    after = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_event", x => new { x.id, x.occurred_at });
                });

            migrationBuilder.CreateTable(
                name: "outbox_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_heartbeat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    instance_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_worker_heartbeat", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ethics_approval_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retention_until = table.Column<DateOnly>(type: "date", nullable: true),
                    study_kind = table.Column<string>(type: "text", nullable: false, defaultValue: "own"),
                    sample_scenario = table.Column<string>(type: "text", nullable: true),
                    code_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archive_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_series", x => x.id);
                    table.CheckConstraint("ck_campaign_series_code_salt_length", "octet_length(code_salt) = 32");
                    table.CheckConstraint("ck_campaign_series_sample_consistency", "(study_kind = 'own' AND sample_scenario IS NULL) OR (study_kind = 'sample' AND sample_scenario IS NOT NULL)");
                    table.CheckConstraint("ck_campaign_series_sample_scenario", "sample_scenario IS NULL OR sample_scenario IN ('mixed_lifecycle', 'longitudinal', 'setup', 'in_collection', 'completed', 'blocked')");
                    table.CheckConstraint("ck_campaign_series_study_kind", "study_kind IN ('own', 'sample')");
                    table.ForeignKey(
                        name: "fk_campaign_series_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_aggregate_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operational_notification", x => x.id);
                    table.CheckConstraint("ck_operational_notification_payload_object", "jsonb_typeof(payload_json) = 'object'");
                    table.CheckConstraint("ck_operational_notification_severity", "severity IN ('info','warning')");
                    table.CheckConstraint("ck_operational_notification_status", "status IN ('unread','read')");
                    table.ForeignKey(
                        name: "fk_operational_notification_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_intent",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    organization_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_intent", x => x.id);
                    table.CheckConstraint("ck_registration_intent_consumed_shape", "(status = 'pending' AND consumed_at IS NULL AND consumed_tenant_id IS NULL) OR (status = 'consumed' AND consumed_at IS NOT NULL AND consumed_tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_registration_intent_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_registration_intent_status", "status IN ('pending','consumed')");
                    table.ForeignKey(
                        name: "fk_registration_intent_tenant_consumed_tenant_id",
                        column: x => x.consumed_tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
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
                    table.UniqueConstraint("ak_subject_group_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_subject_group_subject_group_parent_group_id",
                        columns: x => new { x.parent_group_id, x.tenant_id },
                        principalTable: "subject_group",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subject_group_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    mfa_secret = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account", x => x.id);
                    table.UniqueConstraint("ak_user_account_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_user_account_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    body_markdown = table.Column<string>(type: "text", nullable: false),
                    required_grants = table.Column<string>(type: "jsonb", nullable: false),
                    optional_grants = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_document", x => x.id);
                    table.CheckConstraint("ck_consent_document_optional_grants_array", "jsonb_typeof(optional_grants) = 'array'");
                    table.CheckConstraint("ck_consent_document_required_grants_array", "jsonb_typeof(required_grants) = 'array'");
                    table.CheckConstraint("ck_consent_document_retired_after_published", "retired_at IS NULL OR retired_at > published_at");
                    table.ForeignKey(
                        name: "fk_consent_document_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_document_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disclosure_policy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    k_min = table.Column<int>(type: "integer", nullable: false),
                    suppression_strategy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    applies_to_dimensions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disclosure_policy", x => x.id);
                    table.CheckConstraint("ck_disclosure_policy_applies_to_dimensions_array", "jsonb_typeof(applies_to_dimensions) = 'array'");
                    table.CheckConstraint("ck_disclosure_policy_k_min", "k_min >= 5");
                    table.CheckConstraint("ck_disclosure_policy_retired_after_created", "retired_at IS NULL OR retired_at > created_at");
                    table.CheckConstraint("ck_disclosure_policy_suppression_strategy", "suppression_strategy IN ('hide_cell','aggregate_up','round_to_n')");
                    table.ForeignKey(
                        name: "fk_disclosure_policy_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_disclosure_policy_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participant_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    argon2_memory_kib = table.Column<int>(type: "integer", nullable: false),
                    argon2_iterations = table.Column<int>(type: "integer", nullable: false),
                    argon2_parallelism = table.Column<int>(type: "integer", nullable: false),
                    argon2_output_bytes = table.Column<int>(type: "integer", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participant_code", x => x.id);
                    table.CheckConstraint("ck_participant_code_argon2_parameters", "argon2_memory_kib >= 65536 AND argon2_iterations >= 3 AND argon2_parallelism >= 4 AND argon2_output_bytes >= 32");
                    table.CheckConstraint("ck_participant_code_hash_length", "octet_length(hash) = 32");
                    table.ForeignKey(
                        name: "fk_participant_code_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_participant_code_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retention_policy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    retain_for_years = table.Column<int>(type: "integer", nullable: false),
                    retention_start_event = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    next_review_at = table.Column<DateOnly>(type: "date", nullable: false),
                    publication_limits = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policy", x => x.id);
                    table.CheckConstraint("ck_retention_policy_action_after", "action_after IN ('delete','anonymize')");
                    table.CheckConstraint("ck_retention_policy_publication_limits_object", "jsonb_typeof(publication_limits) = 'object'");
                    table.CheckConstraint("ck_retention_policy_retain_for_years_positive", "retain_for_years > 0");
                    table.CheckConstraint("ck_retention_policy_retention_start_event", "retention_start_event IN ('consent_accepted_at','response_submitted_at','wave_closed_at','series_closed_at','last_response_submitted_at')");
                    table.CheckConstraint("ck_retention_policy_retired_after_created", "retired_at IS NULL OR retired_at > created_at");
                    table.ForeignKey(
                        name: "fk_retention_policy_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_policy_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_auth_identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_subject_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email_at_binding = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_auth_identity", x => x.id);
                    table.UniqueConstraint("ak_external_auth_identity_id_tenant_id", x => new { x.id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_external_auth_identity_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_auth_identity_user_account_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalTable: "user_account",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignment", x => x.id);
                    table.CheckConstraint("ck_role_assignment_scope", "(scope_type = 'tenant' AND scope_id IS NULL)\nOR (scope_type IN ('workspace', 'campaign', 'campaign_series') AND scope_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_role_assignment_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_user_account_granted_by",
                        column: x => x.granted_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_assignment_user_account_user_id",
                        column: x => x.user_id,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "retention_due_batch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anchor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    as_of = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consent_record_count = table.Column<int>(type: "integer", nullable: false),
                    response_session_count = table.Column<int>(type: "integer", nullable: false),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    score_run_count = table.Column<int>(type: "integer", nullable: false),
                    score_count = table.Column<int>(type: "integer", nullable: false),
                    derived_artifact_count = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    failure_detail = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    execution_result = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    artifact_invalidated_count = table.Column<int>(type: "integer", nullable: true),
                    notice_scrubbed_count = table.Column<int>(type: "integer", nullable: true),
                    delivery_attempt_scrubbed_count = table.Column<int>(type: "integer", nullable: true),
                    invite_credential_scrubbed_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_due_batch", x => x.id);
                    table.CheckConstraint("ck_retention_due_batch_action_after", "action_after IN ('delete','anonymize')");
                    table.CheckConstraint("ck_retention_due_batch_anchor", "anchor IN ('response_submitted_at')");
                    table.CheckConstraint("ck_retention_due_batch_counts_non_negative", "consent_record_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0 AND derived_artifact_count >= 0");
                    table.CheckConstraint("ck_retention_due_batch_due_before_as_of", "due_before <= as_of");
                    table.CheckConstraint("ck_retention_due_batch_lifecycle", "((status = 'planned' AND processing_started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'processing' AND processing_started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL) OR (status = 'completed' AND processing_started_at IS NOT NULL AND completed_at IS NOT NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'failed' AND completed_at IS NULL AND failed_at IS NOT NULL AND failure_code IS NOT NULL))");
                    table.CheckConstraint("ck_retention_due_batch_response_session_count_positive", "response_session_count > 0");
                    table.CheckConstraint("ck_retention_due_batch_status", "status IN ('planned','processing','completed','failed')");
                    table.ForeignKey(
                        name: "fk_retention_due_batch_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_due_batch_retention_policy_retention_policy_id",
                        column: x => x.retention_policy_id,
                        principalTable: "retention_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retention_due_batch_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_auth_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_session_external_auth_identity_external_auth_identity_id_tenant_id",
                        columns: x => new { x.external_auth_identity_id, x.tenant_id },
                        principalTable: "external_auth_identity",
                        principalColumns: new[] { "id", "tenant_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auth_session_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auth_session_user_account_user_id_tenant_id",
                        columns: x => new { x.user_id, x.tenant_id },
                        principalTable: "user_account",
                        principalColumns: new[] { "id", "tenant_id" },
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
                name: "answer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    is_na = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answer", x => x.id);
                    table.CheckConstraint("ck_answer_not_skipped_and_na", "NOT (is_skipped = TRUE AND is_na = TRUE)");
                    table.CheckConstraint("ck_answer_value_json", "value IS NULL OR jsonb_typeof(value) IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_answer_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    respondent_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invite_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment", x => x.id);
                    table.CheckConstraint("ck_assignment_identity_shape", "(anonymized_at IS NULL AND anonymous = FALSE AND respondent_subject_id IS NOT NULL AND invite_token_id IS NULL) OR (anonymized_at IS NULL AND anonymous = TRUE AND respondent_subject_id IS NULL AND invite_token_id IS NOT NULL) OR (anonymized_at IS NOT NULL AND target_subject_id IS NULL AND respondent_subject_id IS NULL AND invite_token_id IS NULL)");
                    table.CheckConstraint("ck_assignment_status", "status IN ('pending','started','submitted','cancelled','expired')");
                    table.ForeignKey(
                        name: "fk_assignment_subject_respondent_subject_id",
                        column: x => x.respondent_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_subject_target_subject_id",
                        column: x => x.target_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assignment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audience",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selector = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audience_member",
                columns: table => new
                {
                    audience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_member", x => new { x.audience_id, x.subject_id });
                    table.CheckConstraint("ck_audience_member_removed_after_added", "removed_at IS NULL OR removed_at >= added_at");
                    table.ForeignKey(
                        name: "fk_audience_member_audience_audience_id",
                        column: x => x.audience_id,
                        principalTable: "audience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audience_member_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_identity_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule = table.Column<string>(type: "jsonb", nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    close_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign", x => x.id);
                    table.CheckConstraint("ck_campaign_date_range", "start_at IS NULL OR end_at IS NULL OR end_at > start_at");
                    table.CheckConstraint("ck_campaign_response_identity_mode", "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                    table.CheckConstraint("ck_campaign_schedule_object", "jsonb_typeof(schedule) = 'object'");
                    table.CheckConstraint("ck_campaign_status", "status IN ('draft','scheduled','live','closed','cancelled')");
                    table.ForeignKey(
                        name: "fk_campaign_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_user_account_created_by",
                        column: x => x.created_by,
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    accepted_grants = table.Column<string>(type: "jsonb", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_record", x => x.id);
                    table.CheckConstraint("ck_consent_record_accepted_grants_array", "jsonb_typeof(accepted_grants) = 'array'");
                    table.ForeignKey(
                        name: "fk_consent_record_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_consent_document_consent_document_id",
                        column: x => x.consent_document_id,
                        principalTable: "consent_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consent_record_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "export_artifact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artifact_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false),
                    byte_size = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    codebook_json = table.Column<string>(type: "jsonb", nullable: false),
                    storage_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_artifact", x => x.id);
                    table.CheckConstraint("ck_export_artifact_byte_size_non_negative", "byte_size >= 0");
                    table.CheckConstraint("ck_export_artifact_checksum_sha256", "checksum_sha256 IS NULL OR checksum_sha256 ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_export_artifact_codebook_object", "codebook_json IS NOT NULL AND jsonb_typeof(codebook_json) = 'object'");
                    table.CheckConstraint("ck_export_artifact_failure_reason_shape", "failure_reason_code IS NULL OR failure_reason_code ~ '^[a-z0-9_.-]{1,128}$'");
                    table.CheckConstraint("ck_export_artifact_format", "format IN ('csv_codebook','html','pdf')");
                    table.CheckConstraint("ck_export_artifact_lifecycle_shape", "(status = 'queued' AND started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND expires_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'rendering' AND started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'succeeded' AND completed_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'failed' AND failed_at IS NOT NULL AND failure_reason_code IS NOT NULL AND completed_at IS NULL AND deleted_at IS NULL)\nOR (status = 'expired' AND expires_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\nOR (status = 'deleted' AND deleted_at IS NOT NULL AND failed_at IS NULL AND failure_reason_code IS NULL)");
                    table.CheckConstraint("ck_export_artifact_materialization_shape", "(status = 'succeeded' AND completed_at IS NOT NULL AND checksum_sha256 IS NOT NULL AND ((storage_kind = 'inline_text' AND content IS NOT NULL AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND content IS NULL AND storage_key IS NOT NULL)))\nOR (status <> 'succeeded' AND checksum_sha256 IS NULL AND content IS NULL AND storage_key IS NULL)");
                    table.CheckConstraint("ck_export_artifact_metadata_object", "metadata_json IS NOT NULL AND jsonb_typeof(metadata_json) = 'object'");
                    table.CheckConstraint("ck_export_artifact_row_count_non_negative", "row_count >= 0");
                    table.CheckConstraint("ck_export_artifact_status", "status IN ('queued','rendering','succeeded','failed','expired','deleted')");
                    table.CheckConstraint("ck_export_artifact_storage_kind", "storage_kind IN ('inline_text','external_object')");
                    table.CheckConstraint("ck_export_artifact_storage_shape", "(storage_kind = 'inline_text' AND storage_key IS NULL)\nOR (storage_kind = 'external_object' AND content IS NULL)");
                    table.CheckConstraint("ck_export_artifact_target_kind", "target_kind IN ('campaign','campaign_series')");
                    table.CheckConstraint("ck_export_artifact_target_scope", "(target_kind = 'campaign' AND campaign_id IS NOT NULL)\nOR (target_kind = 'campaign_series' AND campaign_id IS NULL AND campaign_series_id IS NOT NULL)");
                    table.CheckConstraint("ck_export_artifact_type", "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook','campaign_series_report_html','campaign_series_report_pdf')");
                    table.ForeignKey(
                        name: "fk_export_artifact_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_artifact_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_artifact_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_token", x => x.id);
                    table.CheckConstraint("ck_invitation_token_channel", "channel IN ('email','sms','open_link','identified_entry')");
                    table.ForeignKey(
                        name: "fk_invitation_token_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitation_token_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitation_token_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    template_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.CheckConstraint("ck_notification_channel", "channel IN ('email','sms')");
                    table.CheckConstraint("ck_notification_status", "status IN ('queued','sent','failed','bounced')");
                    table.ForeignKey(
                        name: "fk_notification_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "respondent_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    rule = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_respondent_rule", x => x.id);
                    table.CheckConstraint("ck_respondent_rule_ordinal_positive", "ordinal > 0");
                    table.CheckConstraint("ck_respondent_rule_rule_object", "jsonb_typeof(rule) = 'object'");
                    table.ForeignKey(
                        name: "fk_respondent_rule_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "response_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_taken_ms = table.Column<int>(type: "integer", nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    public_handle_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    public_handle_issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ip_hash = table.Column<string>(type: "text", nullable: true),
                    user_agent_hash = table.Column<string>(type: "text", nullable: true),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_response_session", x => x.id);
                    table.CheckConstraint("ck_response_session_public_handle_hash", "public_handle_hash IS NULL OR public_handle_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_response_session_submitted_after_started", "started_at IS NULL OR submitted_at IS NULL OR submitted_at >= started_at");
                    table.CheckConstraint("ck_response_session_time_taken_non_negative", "time_taken_ms IS NULL OR time_taken_ms >= 0");
                    table.ForeignKey(
                        name: "fk_response_session_assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_response_session_consent_record_consent_record_id",
                        column: x => x.consent_record_id,
                        principalTable: "consent_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_response_session_participant_code_participant_code_id",
                        column: x => x.participant_code_id,
                        principalTable: "participant_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_response_session_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery_attempt", x => x.id);
                    table.CheckConstraint("ck_notification_delivery_attempt_status", "status IN ('sent','failed')");
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempt_notification_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempt_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "withdrawal_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_after = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_record_count = table.Column<int>(type: "integer", nullable: false),
                    response_session_count = table.Column<int>(type: "integer", nullable: false),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    score_run_count = table.Column<int>(type: "integer", nullable: false),
                    score_count = table.Column<int>(type: "integer", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_event", x => x.id);
                    table.CheckConstraint("ck_withdrawal_event_action_after", "action_after IN ('delete', 'anonymize')");
                    table.CheckConstraint("ck_withdrawal_event_counts_non_negative", "consent_record_count >= 0 AND response_session_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0");
                    table.CheckConstraint("ck_withdrawal_event_metadata_object", "jsonb_typeof(metadata_json) = 'object'");
                    table.CheckConstraint("ck_withdrawal_event_processed_after_requested", "processed_at IS NULL OR processed_at >= requested_at");
                    table.CheckConstraint("ck_withdrawal_event_scope", "scope IN ('campaign_series')");
                    table.CheckConstraint("ck_withdrawal_event_status", "status IN ('requested', 'planned', 'processing', 'completed', 'failed', 'denied')");
                    table.CheckConstraint("ck_withdrawal_event_target_kind", "target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched', 'response_session')");
                    table.CheckConstraint("ck_withdrawal_event_target_shape", "((target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'response_session' AND subject_id IS NULL AND participant_code_id IS NULL AND (response_session_id IS NOT NULL OR (action_after = 'delete' AND status = 'completed' AND response_session_id IS NULL))))");
                    table.ForeignKey(
                        name: "fk_withdrawal_event_campaign_series",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_participant_code",
                        column: x => x.participant_code_id,
                        principalTable: "participant_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_response_session",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_retention_policy",
                        column: x => x.retention_policy_id,
                        principalTable: "retention_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_subject",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_event_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "withdrawal_request_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_request_token", x => x.id);
                    table.CheckConstraint("ck_withdrawal_request_token_action", "requested_action IN ('delete', 'anonymize')");
                    table.CheckConstraint("ck_withdrawal_request_token_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_withdrawal_request_token_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_request_token_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_launch_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scoring_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retention_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disclosure_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    response_identity_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    template_question_count = table.Column<int>(type: "integer", nullable: false),
                    scoring_rule_document_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    launch_readiness = table.Column<string>(type: "jsonb", nullable: false),
                    launch_packet = table.Column<string>(type: "jsonb", nullable: false),
                    launched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    launched_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_launch_snapshot", x => x.id);
                    table.CheckConstraint("ck_campaign_launch_snapshot_packet_object", "jsonb_typeof(launch_packet) = 'object'");
                    table.CheckConstraint("ck_campaign_launch_snapshot_question_count_positive", "template_question_count > 0");
                    table.CheckConstraint("ck_campaign_launch_snapshot_readiness_object", "jsonb_typeof(launch_readiness) = 'object'");
                    table.CheckConstraint("ck_campaign_launch_snapshot_response_identity_mode", "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_campaign_series_campaign_series_id",
                        column: x => x.campaign_series_id,
                        principalTable: "campaign_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_consent_document_consent_document_id",
                        column: x => x.consent_document_id,
                        principalTable: "consent_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_disclosure_policy_disclosure_policy_id",
                        column: x => x.disclosure_policy_id,
                        principalTable: "disclosure_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_retention_policy_retention_policy_id",
                        column: x => x.retention_policy_id,
                        principalTable: "retention_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaign_launch_snapshot_user_account_launched_by",
                        column: x => x.launched_by,
                        principalTable: "user_account",
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
                });

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
                    rights_scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rights_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    validity_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provenance_note = table.Column<string>(type: "text", nullable: true),
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
                    table.CheckConstraint("ck_instrument_private_import_shape", "validity_status <> 'private_import' OR (tenant_id IS NOT NULL AND parent_instrument_id IS NULL AND is_global = FALSE)");
                    table.CheckConstraint("ck_instrument_rights_scope", "rights_scope IN ('platform_granted','tenant_provided')");
                    table.CheckConstraint("ck_instrument_rights_status", "rights_status IN ('verified','attested_by_tenant','unverified_internal_demo','expired')");
                    table.CheckConstraint("ck_instrument_validity_label", "validity_label IN ('official','tenant_provided','adapted','experimental','rights_unverified')");
                    table.CheckConstraint("ck_instrument_validity_status", "validity_status IN ('canonical','derived','private_import','draft','retired')");
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
                name: "scoring_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    rule_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    engine_min_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    produces = table.Column<string>(type: "jsonb", nullable: false),
                    compatibility = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scoring_rule", x => x.id);
                    table.CheckConstraint("ck_scoring_rule_compatibility_object", "jsonb_typeof(compatibility) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_document_hash", "document_hash ~ '^[a-f0-9]{64}$'");
                    table.CheckConstraint("ck_scoring_rule_document_object", "jsonb_typeof(document) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_produces_object", "jsonb_typeof(produces) = 'object'");
                    table.CheckConstraint("ck_scoring_rule_publish_shape", "(status = 'published' AND published_at IS NOT NULL AND is_locked = TRUE) OR (status <> 'published')");
                    table.CheckConstraint("ck_scoring_rule_status", "status IN ('draft','published','retired')");
                    table.ForeignKey(
                        name: "fk_scoring_rule_template_version_template_version_id",
                        column: x => x.template_version_id,
                        principalTable: "template_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scoring_rule_user_account_published_by",
                        column: x => x.published_by,
                        principalTable: "user_account",
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
                name: "score_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scoring_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ran_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score_run", x => x.id);
                    table.CheckConstraint("ck_score_run_status", "status IN ('running','success','failed')");
                    table.ForeignKey(
                        name: "fk_score_run_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_scoring_rule_scoring_rule_id",
                        column: x => x.scoring_rule_id,
                        principalTable: "scoring_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_run_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
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
                name: "score",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dimension_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    n = table.Column<int>(type: "integer", nullable: false),
                    n_expected = table.Column<int>(type: "integer", nullable: false),
                    missing_policy_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score", x => x.id);
                    table.CheckConstraint("ck_score_missing_policy_status_shape", "missing_policy_status ~ '^[a-z0-9_.-]{1,64}$'");
                    table.CheckConstraint("ck_score_n_expected_non_negative", "n_expected >= 0");
                    table.CheckConstraint("ck_score_n_non_negative", "n >= 0");
                    table.CheckConstraint("ck_score_n_valid_not_above_expected", "n <= n_expected");
                    table.ForeignKey(
                        name: "fk_score_campaign_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_response_session_response_session_id",
                        column: x => x.response_session_id,
                        principalTable: "response_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_score_run_score_run_id",
                        column: x => x.score_run_id,
                        principalTable: "score_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_score_tenant_tenant_id",
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
                    table.ForeignKey(
                        name: "fk_instrument_item_question_question_id",
                        column: x => x.question_id,
                        principalTable: "question",
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
                    survey_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    choice_option_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.CheckConstraint("ck_translation_exactly_one_target", "((instrument_id IS NOT NULL)::int\n+ (instrument_subscale_id IS NOT NULL)::int\n+ (instrument_item_id IS NOT NULL)::int\n+ (survey_template_id IS NOT NULL)::int\n+ (template_section_id IS NOT NULL)::int\n+ (template_question_id IS NOT NULL)::int\n+ (choice_option_id IS NOT NULL)::int) = 1");
                    table.CheckConstraint("ck_translation_status", "status IN ('draft_translation','back_translated','reconciled','approved_canonical_equivalent','approved_derivative','rejected')");
                    table.ForeignKey(
                        name: "fk_translation_choice_option_choice_option_id",
                        column: x => x.choice_option_id,
                        principalTable: "choice_option",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "fk_translation_question_template_question_id",
                        column: x => x.template_question_id,
                        principalTable: "question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_translation_section_template_section_id",
                        column: x => x.template_section_id,
                        principalTable: "section",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_translation_survey_template_survey_template_id",
                        column: x => x.survey_template_id,
                        principalTable: "survey_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_answer_question_id_answered_at",
                table: "answer",
                columns: new[] { "question_id", "answered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_answer_session_id",
                table: "answer",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_answer_session_id_question_id",
                table: "answer",
                columns: new[] { "session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_answer_tenant_id",
                table: "answer",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_campaign_id_status_due_at",
                table: "assignment",
                columns: new[] { "campaign_id", "status", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assignment_invite_token_id",
                table: "assignment",
                column: "invite_token_id",
                unique: true,
                filter: "invite_token_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_respondent_subject_id",
                table: "assignment",
                column: "respondent_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_target_subject_id",
                table: "assignment",
                column: "target_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_tenant_id",
                table: "assignment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_unique_identified",
                table: "assignment",
                columns: new[] { "campaign_id", "target_subject_id", "respondent_subject_id" },
                unique: true,
                filter: "respondent_subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audience_campaign_id",
                table: "audience",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_audience_member_subject_id",
                table: "audience_member",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_entity_type_entity_id_occurred_at",
                table: "audit_event",
                columns: new[] { "entity_type", "entity_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_tenant_id_occurred_at",
                table: "audit_event",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_external_auth_identity_id_tenant_id",
                table: "auth_session",
                columns: new[] { "external_auth_identity_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_tenant_id_expires_at",
                table: "auth_session",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_tenant_id_user_id",
                table: "auth_session",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_user_id_tenant_id",
                table: "auth_session",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_campaign_series_id",
                table: "campaign",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_created_by",
                table: "campaign",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_template_version_id",
                table: "campaign",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_tenant_id",
                table: "campaign",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_tenant_id_status",
                table: "campaign",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_campaign_id",
                table: "campaign_launch_snapshot",
                column: "campaign_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_campaign_series_id",
                table: "campaign_launch_snapshot",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_consent_document_id",
                table: "campaign_launch_snapshot",
                column: "consent_document_id",
                filter: "consent_document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_disclosure_policy_id",
                table: "campaign_launch_snapshot",
                column: "disclosure_policy_id",
                filter: "disclosure_policy_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_launched_by",
                table: "campaign_launch_snapshot",
                column: "launched_by");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_retention_policy_id",
                table: "campaign_launch_snapshot",
                column: "retention_policy_id",
                filter: "retention_policy_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_scoring_rule_id",
                table: "campaign_launch_snapshot",
                column: "scoring_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_template_version_id",
                table: "campaign_launch_snapshot",
                column: "template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_tenant_id",
                table: "campaign_launch_snapshot",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_launch_snapshot_tenant_id_launched_at",
                table: "campaign_launch_snapshot",
                columns: new[] { "tenant_id", "launched_at" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_series_tenant_id",
                table: "campaign_series",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_series_tenant_id_name",
                table: "campaign_series",
                columns: new[] { "tenant_id", "name" });

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
                name: "ix_consent_document_campaign_series_id_locale_version",
                table: "consent_document",
                columns: new[] { "campaign_series_id", "locale", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consent_document_tenant_id",
                table: "consent_document",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_assignment_id",
                table: "consent_record",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_campaign_id",
                table: "consent_record",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_consent_document_id",
                table: "consent_record",
                column: "consent_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_subject_id",
                table: "consent_record",
                column: "subject_id",
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_consent_record_tenant_id",
                table: "consent_record",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_campaign_series_id",
                table: "disclosure_policy",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_campaign_series_id_version",
                table: "disclosure_policy",
                columns: new[] { "campaign_series_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_disclosure_policy_tenant_id",
                table: "disclosure_policy",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_artifact_campaign_id",
                table: "export_artifact",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_artifact_campaign_series_id",
                table: "export_artifact",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "target_kind", "campaign_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_export_artifact_tenant_id_target_kind_campaign_series_id_created_at",
                table: "export_artifact",
                columns: new[] { "tenant_id", "target_kind", "campaign_series_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_provider_provider_subject_hash",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "provider", "provider_subject_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_user_id",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_tenant_id_user_id_provider",
                table: "external_auth_identity",
                columns: new[] { "tenant_id", "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_auth_identity_user_id_tenant_id",
                table: "external_auth_identity",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_canonical_template_version_id",
                table: "instrument",
                column: "canonical_template_version_id");

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
                name: "ix_instrument_item_question_id",
                table: "instrument_item",
                column: "question_id");

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
                name: "ix_invitation_token_assignment_id",
                table: "invitation_token",
                column: "assignment_id",
                filter: "assignment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_campaign_id",
                table: "invitation_token",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_tenant_id",
                table: "invitation_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_token_hash",
                table: "invitation_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_assignment_id",
                table: "notification",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_campaign_id",
                table: "notification",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_status_scheduled_for",
                table: "notification",
                columns: new[] { "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id_campaign_id",
                table: "notification",
                columns: new[] { "tenant_id", "campaign_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_attempt_notification_id_created_at",
                table: "notification_delivery_attempt",
                columns: new[] { "notification_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_delivery_attempt_tenant_id_notification_id",
                table: "notification_delivery_attempt",
                columns: new[] { "tenant_id", "notification_id" });

            migrationBuilder.CreateIndex(
                name: "ix_operational_notification_tenant_id_status_created_at",
                table: "operational_notification",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_operational_notification_source",
                table: "operational_notification",
                columns: new[] { "tenant_id", "source_aggregate_id", "source_event_type", "notification_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_event_aggregate_id_created_at",
                table: "outbox_event",
                columns: new[] { "aggregate_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_event_tenant_id_created_at",
                table: "outbox_event",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_event_unpublished_next_retry_at",
                table: "outbox_event",
                column: "next_retry_at",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_participant_code_campaign_series_id_hash",
                table: "participant_code",
                columns: new[] { "campaign_series_id", "hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participant_code_tenant_id",
                table: "participant_code",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_code",
                table: "permission",
                column: "code",
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
                name: "ix_registration_intent_consumed_tenant_id",
                table: "registration_intent",
                column: "consumed_tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_email",
                table: "registration_intent",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_registration_token_hash",
                table: "registration_intent",
                column: "registration_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_intent_status_expires_at",
                table: "registration_intent",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_respondent_rule_campaign_id_ordinal",
                table: "respondent_rule",
                columns: new[] { "campaign_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_response_session_assignment_id",
                table: "response_session",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_consent_record_id",
                table: "response_session",
                column: "consent_record_id",
                filter: "consent_record_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_participant_code_id",
                table: "response_session",
                column: "participant_code_id",
                filter: "participant_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_public_handle_hash",
                table: "response_session",
                column: "public_handle_hash",
                unique: true,
                filter: "public_handle_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_submitted_at",
                table: "response_session",
                column: "submitted_at",
                filter: "submitted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_response_session_tenant_id",
                table: "response_session",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_retention_due_batch_campaign_series_id",
                table: "retention_due_batch",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "IX_retention_due_batch_retention_policy_id",
                table: "retention_due_batch",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_id",
                table: "retention_due_batch",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_id_idempotency_key",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_series_due_before",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "campaign_series_id", "due_before" });

            migrationBuilder.CreateIndex(
                name: "ix_retention_due_batch_tenant_status_created_at",
                table: "retention_due_batch",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_campaign_series_id",
                table: "retention_policy",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_campaign_series_id_version",
                table: "retention_policy",
                columns: new[] { "campaign_series_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_policy_tenant_id",
                table: "retention_policy",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_global_code",
                table: "role",
                column: "code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_role_tenant_id_code",
                table: "role",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_granted_by",
                table: "role_assignment",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_role_id",
                table: "role_assignment",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_tenant_id",
                table: "role_assignment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignment_user_id",
                table: "role_assignment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_scale_template_version_id_code",
                table: "scale",
                columns: new[] { "template_version_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_score_campaign_id_dimension_code",
                table: "score",
                columns: new[] { "campaign_id", "dimension_code" });

            migrationBuilder.CreateIndex(
                name: "ix_score_response_session_id",
                table: "score",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_score_run_id",
                table: "score",
                column: "score_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_tenant_id",
                table: "score",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_campaign_id",
                table: "score_run",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_response_session_id",
                table: "score_run",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_scoring_rule_id",
                table: "score_run",
                column: "scoring_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_run_tenant_id",
                table: "score_run",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_document_hash",
                table: "scoring_rule",
                column: "document_hash");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_published_by",
                table: "scoring_rule",
                column: "published_by");

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_rule_key_rule_version",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "rule_key", "rule_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scoring_rule_template_version_id_status",
                table: "scoring_rule",
                columns: new[] { "template_version_id", "status" });

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
                name: "ix_subject_group_parent_group_id_tenant_id",
                table: "subject_group",
                columns: new[] { "parent_group_id", "tenant_id" });

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

            migrationBuilder.CreateIndex(
                name: "ix_tenant_slug",
                table: "tenant",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_translation_unique_choice_option",
                table: "translation",
                columns: new[] { "choice_option_id", "field", "locale" },
                unique: true,
                filter: "choice_option_id IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "ix_user_account_tenant_id_email",
                table: "user_account",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_event_campaign_series_id",
                table: "withdrawal_event",
                column: "campaign_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_participant_code_id",
                table: "withdrawal_event",
                column: "participant_code_id",
                filter: "participant_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_response_session_id",
                table: "withdrawal_event",
                column: "response_session_id",
                filter: "response_session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_event_retention_policy_id",
                table: "withdrawal_event",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_subject_id",
                table: "withdrawal_event",
                column: "subject_id",
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_tenant_id",
                table: "withdrawal_event",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_event_tenant_series_requested",
                table: "withdrawal_event",
                columns: new[] { "tenant_id", "campaign_series_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_response_session_id",
                table: "withdrawal_request_token",
                column: "response_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_tenant_id",
                table: "withdrawal_request_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_tenant_id_expires_at",
                table: "withdrawal_request_token",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_withdrawal_request_token_token_hash",
                table: "withdrawal_request_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_worker_heartbeat_last_seen_at",
                table: "worker_heartbeat",
                column: "last_seen_at");

            migrationBuilder.CreateIndex(
                name: "ix_worker_heartbeat_worker_name_instance_id",
                table: "worker_heartbeat",
                columns: new[] { "worker_name", "instance_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_answer_question_question_id",
                table: "answer",
                column: "question_id",
                principalTable: "question",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_answer_response_session_session_id",
                table: "answer",
                column: "session_id",
                principalTable: "response_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_assignment_campaign_campaign_id",
                table: "assignment",
                column: "campaign_id",
                principalTable: "campaign",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_assignment_invitation_token_invite_token_id",
                table: "assignment",
                column: "invite_token_id",
                principalTable: "invitation_token",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_audience_campaign_campaign_id",
                table: "audience",
                column: "campaign_id",
                principalTable: "campaign",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_template_version_template_version_id",
                table: "campaign",
                column: "template_version_id",
                principalTable: "template_version",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_launch_snapshot_scoring_rule_scoring_rule_id",
                table: "campaign_launch_snapshot",
                column: "scoring_rule_id",
                principalTable: "scoring_rule",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_launch_snapshot_template_version_template_version_id",
                table: "campaign_launch_snapshot",
                column: "template_version_id",
                principalTable: "template_version",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_question_question_id",
                table: "choice_option",
                column: "question_id",
                principalTable: "question",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_instrument_template_version_canonical_template_version_id",
                table: "instrument",
                column: "canonical_template_version_id",
                principalTable: "template_version",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignment_tenant_tenant_id",
                table: "assignment");

            migrationBuilder.DropForeignKey(
                name: "fk_campaign_tenant_tenant_id",
                table: "campaign");

            migrationBuilder.DropForeignKey(
                name: "fk_campaign_series_tenant_tenant_id",
                table: "campaign_series");

            migrationBuilder.DropForeignKey(
                name: "fk_instrument_tenant_tenant_id",
                table: "instrument");

            migrationBuilder.DropForeignKey(
                name: "fk_invitation_token_tenant_tenant_id",
                table: "invitation_token");

            migrationBuilder.DropForeignKey(
                name: "fk_subject_tenant_tenant_id",
                table: "subject");

            migrationBuilder.DropForeignKey(
                name: "fk_survey_template_tenant_tenant_id",
                table: "survey_template");

            migrationBuilder.DropForeignKey(
                name: "fk_user_account_tenant_tenant_id",
                table: "user_account");

            migrationBuilder.DropForeignKey(
                name: "fk_assignment_campaign_campaign_id",
                table: "assignment");

            migrationBuilder.DropForeignKey(
                name: "fk_invitation_token_campaign_campaign_id",
                table: "invitation_token");

            migrationBuilder.DropForeignKey(
                name: "fk_assignment_invitation_token_invite_token_id",
                table: "assignment");

            migrationBuilder.DropForeignKey(
                name: "fk_survey_template_user_account_created_by",
                table: "survey_template");

            migrationBuilder.DropForeignKey(
                name: "fk_template_version_user_account_published_by",
                table: "template_version");

            migrationBuilder.DropForeignKey(
                name: "fk_instrument_template_version_canonical_template_version_id",
                table: "instrument");

            migrationBuilder.DropTable(
                name: "answer");

            migrationBuilder.DropTable(
                name: "audience_member");

            migrationBuilder.DropTable(
                name: "audit_event");

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "campaign_launch_snapshot");

            migrationBuilder.DropTable(
                name: "export_artifact");

            migrationBuilder.DropTable(
                name: "instrument_norm");

            migrationBuilder.DropTable(
                name: "notification_delivery_attempt");

            migrationBuilder.DropTable(
                name: "operational_notification");

            migrationBuilder.DropTable(
                name: "outbox_event");

            migrationBuilder.DropTable(
                name: "registration_intent");

            migrationBuilder.DropTable(
                name: "respondent_rule");

            migrationBuilder.DropTable(
                name: "retention_due_batch");

            migrationBuilder.DropTable(
                name: "role_assignment");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "score");

            migrationBuilder.DropTable(
                name: "subject_membership");

            migrationBuilder.DropTable(
                name: "subject_relationship");

            migrationBuilder.DropTable(
                name: "translation");

            migrationBuilder.DropTable(
                name: "withdrawal_event");

            migrationBuilder.DropTable(
                name: "withdrawal_request_token");

            migrationBuilder.DropTable(
                name: "worker_heartbeat");

            migrationBuilder.DropTable(
                name: "audience");

            migrationBuilder.DropTable(
                name: "external_auth_identity");

            migrationBuilder.DropTable(
                name: "disclosure_policy");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "score_run");

            migrationBuilder.DropTable(
                name: "subject_group");

            migrationBuilder.DropTable(
                name: "choice_option");

            migrationBuilder.DropTable(
                name: "instrument_item");

            migrationBuilder.DropTable(
                name: "instrument_subscale");

            migrationBuilder.DropTable(
                name: "retention_policy");

            migrationBuilder.DropTable(
                name: "response_session");

            migrationBuilder.DropTable(
                name: "scoring_rule");

            migrationBuilder.DropTable(
                name: "question");

            migrationBuilder.DropTable(
                name: "consent_record");

            migrationBuilder.DropTable(
                name: "participant_code");

            migrationBuilder.DropTable(
                name: "scale");

            migrationBuilder.DropTable(
                name: "section");

            migrationBuilder.DropTable(
                name: "consent_document");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "campaign");

            migrationBuilder.DropTable(
                name: "campaign_series");

            migrationBuilder.DropTable(
                name: "invitation_token");

            migrationBuilder.DropTable(
                name: "assignment");

            migrationBuilder.DropTable(
                name: "subject");

            migrationBuilder.DropTable(
                name: "user_account");

            migrationBuilder.DropTable(
                name: "template_version");

            migrationBuilder.DropTable(
                name: "instrument");

            migrationBuilder.DropTable(
                name: "survey_template");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
