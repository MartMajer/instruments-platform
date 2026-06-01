using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class RlsMigrationScriptTests
{
    private static readonly string[] TenantScopedTables =
    [
        "tenant",
        "user_account",
        "external_auth_identity",
        "auth_session",
        "role",
        "role_permission",
        "role_assignment",
        "audit_event",
        "outbox_event",
        "subject",
        "subject_group",
        "subject_membership",
        "subject_relationship",
        "instrument",
        "instrument_subscale",
        "instrument_item",
        "instrument_norm",
        "translation",
        "survey_template",
        "template_version",
        "scoring_rule",
        "score_run",
        "score",
        "campaign_series",
        "campaign",
        "campaign_launch_snapshot",
        "audience",
        "audience_member",
        "respondent_rule",
        "assignment",
        "invitation_token",
        "notification",
        "email_template",
        "notification_delivery_attempt",
        "notification_delivery_event",
        "email_suppression",
        "participant_code",
        "section",
        "scale",
        "question",
        "choice_option",
        "consent_document",
        "retention_policy",
        "retention_due_batch",
        "withdrawal_event",
        "withdrawal_request_token",
        "disclosure_policy",
        "consent_record",
        "response_session",
        "answer"
    ];

    [Fact]
    public void Initial_migration_enables_and_forces_rls_for_tenant_scoped_tables()
    {
        var script = GenerateMigrationScript();

        foreach (var table in TenantScopedTables)
        {
            Assert.Contains($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;", script);
            Assert.Contains($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;", script);
        }
    }

    [Fact]
    public void Initial_migration_policies_use_transaction_local_current_tenant_setting()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("current_setting('app.current_tenant_id')::uuid", script);
        Assert.Contains("CREATE POLICY tenant_isolation ON tenant", script);
        Assert.Contains("CREATE POLICY user_account_tenant_isolation ON user_account", script);
        Assert.Contains("CREATE POLICY external_auth_identity_tenant_isolation ON external_auth_identity", script);
        Assert.Contains("CREATE POLICY auth_session_tenant_isolation ON auth_session", script);
        Assert.Contains("CREATE POLICY role_assignment_tenant_isolation ON role_assignment", script);
        Assert.Contains("CREATE POLICY role_tenant_read ON role", script);
        Assert.Contains("CREATE POLICY role_permission_tenant_read ON role_permission", script);
    }

    [Fact]
    public void Initial_migration_uses_filtered_role_indexes_for_tenant_and_global_roles()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE UNIQUE INDEX ix_role_global_code ON role (code) WHERE tenant_id IS NULL;", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_role_tenant_id_code ON role (tenant_id, code) WHERE tenant_id IS NOT NULL;", script);
    }

    [Fact]
    public void Initial_migration_enforces_role_assignment_scope_shape()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CONSTRAINT ck_role_assignment_scope CHECK", script);
        Assert.Contains("scope_type = 'tenant' AND scope_id IS NULL", script);
        Assert.Contains("scope_type IN ('workspace', 'campaign', 'campaign_series') AND scope_id IS NOT NULL", script);
    }

    [Fact]
    public void Initial_migration_creates_append_only_partitioned_audit_event_table()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE audit_event", script);
        Assert.Contains("PARTITION BY RANGE (occurred_at)", script);
        Assert.Contains("CREATE TABLE audit_event_default PARTITION OF audit_event DEFAULT;", script);
        Assert.Contains("CREATE TRIGGER audit_event_prevent_update_delete", script);
    }

    [Fact]
    public void Migrations_create_outbox_event_table_with_relay_indexes()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE outbox_event", script);
        Assert.Contains("payload jsonb NOT NULL", script);
        Assert.Contains("CREATE INDEX ix_outbox_event_unpublished_next_retry_at", script);
        Assert.Contains("WHERE published_at IS NULL", script);
        Assert.Contains("CREATE INDEX ix_outbox_event_aggregate_id_created_at", script);
    }

    [Fact]
    public void Migrations_create_worker_outbox_relay_policy_without_broad_runtime_bypass()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE ROLE platform_worker", script);
        Assert.Contains("CREATE POLICY outbox_event_worker_relay_select ON outbox_event", script);
        Assert.Contains("FOR SELECT", script);
        Assert.Contains("TO platform_worker", script);
        Assert.Contains("NULLIF(current_setting('app.current_tenant_id', true), '')::uuid", script);
        Assert.Contains("published_at IS NULL", script);
        Assert.Contains("left(last_error, 12) <> 'DEAD_LETTER:'", script);
        Assert.DoesNotContain("BYPASSRLS", script);
    }

    [Fact]
    public void Migrations_create_subject_tables_with_json_attributes_indexes_and_rls()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE subject", script);
        Assert.Contains("attributes jsonb NOT NULL", script);
        Assert.Contains("CREATE INDEX ix_subject_attributes_gin ON subject USING gin (attributes);", script);
        Assert.Contains("CREATE TABLE subject_group", script);
        Assert.Contains("CREATE TABLE subject_membership", script);
        Assert.Contains("CREATE TABLE subject_relationship", script);
        Assert.Contains("CREATE POLICY subject_tenant_isolation ON subject", script);
        Assert.Contains("CREATE POLICY subject_group_tenant_isolation ON subject_group", script);
        Assert.Contains("CREATE POLICY subject_membership_tenant_isolation ON subject_membership", script);
        Assert.Contains("CREATE POLICY subject_relationship_tenant_isolation ON subject_relationship", script);
        Assert.Contains("related.tenant_id = current_setting('app.current_tenant_id')::uuid", script);
    }

    [Fact]
    public void Migrations_create_instrument_tables_with_lock_constraints_and_global_rls()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE instrument", script);
        Assert.Contains("version character varying(64) NOT NULL", script);
        Assert.Contains("developers text[] NOT NULL", script);
        Assert.Contains("rights_scope character varying(64) NOT NULL", script);
        Assert.Contains("rights_status character varying(64) NOT NULL", script);
        Assert.Contains("validity_label character varying(64) NOT NULL", script);
        Assert.Contains("provenance_note text", script);
        Assert.Contains("CONSTRAINT ck_instrument_global_tenant_shape CHECK", script);
        Assert.Contains("CONSTRAINT ck_instrument_rights_scope CHECK", script);
        Assert.Contains("CONSTRAINT ck_instrument_rights_status CHECK", script);
        Assert.Contains("CONSTRAINT ck_instrument_validity_label CHECK", script);
        Assert.Contains("CONSTRAINT ck_instrument_private_import_shape CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_instrument_global_code_version ON instrument (code, version) WHERE tenant_id IS NULL;", script);
        Assert.Contains("CREATE TABLE instrument_subscale", script);
        Assert.Contains("CREATE TABLE instrument_item", script);
        Assert.Contains("CREATE TABLE instrument_norm", script);
        Assert.Contains("percentiles jsonb NOT NULL", script);
        Assert.Contains("CREATE TABLE translation", script);
        Assert.Contains("CONSTRAINT ck_translation_exactly_one_instrument_target CHECK", script);
        Assert.Contains("CREATE POLICY instrument_tenant_or_global_read ON instrument", script);
        Assert.Contains("CREATE POLICY instrument_tenant_write ON instrument", script);
        Assert.Contains("CREATE POLICY instrument_subscale_tenant_or_global_read ON instrument_subscale", script);
        Assert.Contains("CREATE POLICY translation_tenant_write ON translation", script);
    }

    [Fact]
    public void Migrations_guard_cross_tenant_parent_links()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("ak_subject_group_id_tenant_id", script);
        Assert.Contains("fk_subject_group_subject_group_parent_group_id", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION instrument_parent_tenant_guard()", script);
        Assert.Contains("instrument parent must be global or owned by the same tenant", script);
    }

    [Fact]
    public void Migrations_create_template_tables_with_global_read_and_tenant_write_rls()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE survey_template", script);
        Assert.Contains("CREATE TABLE template_version", script);
        Assert.Contains("CREATE TABLE section", script);
        Assert.Contains("CREATE TABLE scale", script);
        Assert.Contains("CREATE TABLE question", script);
        Assert.Contains("CREATE TABLE choice_option", script);
        Assert.Contains("CREATE POLICY survey_template_tenant_or_global_read ON survey_template", script);
        Assert.Contains("CREATE POLICY template_version_tenant_or_global_read ON template_version", script);
        Assert.Contains("CREATE POLICY question_tenant_write ON question", script);
        Assert.Contains("fk_instrument_item_question_question_id", script);
        Assert.Contains("fk_instrument_template_version_canonical_template_version_id", script);
        Assert.Contains("CONSTRAINT ck_translation_exactly_one_target CHECK", script);
    }

    [Fact]
    public void Migrations_create_scoring_rule_table_with_template_version_rls()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE scoring_rule", script);
        Assert.Contains("document jsonb NOT NULL", script);
        Assert.Contains("produces jsonb NOT NULL", script);
        Assert.Contains("compatibility jsonb NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_scoring_rule_status CHECK", script);
        Assert.Contains("CONSTRAINT ck_scoring_rule_document_hash CHECK", script);
        Assert.Contains("CONSTRAINT ck_scoring_rule_publish_shape CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_scoring_rule_template_version_id_rule_key_rule_version", script);
        Assert.Contains("CREATE POLICY scoring_rule_tenant_or_global_read ON scoring_rule", script);
        Assert.Contains("CREATE POLICY scoring_rule_tenant_write ON scoring_rule", script);
    }

    [Fact]
    public void Migrations_create_score_tables_with_rls_and_tenant_guards()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE score_run", script);
        Assert.Contains("CREATE TABLE score", script);
        Assert.Contains("CONSTRAINT ck_score_run_status CHECK", script);
        Assert.Contains("CONSTRAINT ck_score_n_non_negative CHECK", script);
        Assert.Contains("n_expected integer NOT NULL DEFAULT 0", script);
        Assert.Contains("missing_policy_status character varying(64) NOT NULL DEFAULT 'ok'", script);
        Assert.Contains("UPDATE score SET n_expected = n;", script);
        Assert.Contains("CONSTRAINT ck_score_n_expected_non_negative CHECK", script);
        Assert.Contains("CONSTRAINT ck_score_n_valid_not_above_expected CHECK", script);
        Assert.Contains("CONSTRAINT ck_score_missing_policy_status_shape CHECK", script);
        Assert.Contains("CREATE POLICY score_run_tenant_isolation ON score_run", script);
        Assert.Contains("CREATE POLICY score_tenant_isolation ON score", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION score_run_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION score_tenant_guard()", script);
        Assert.Contains("score run session, campaign, and scoring rule must belong to the same tenant template", script);
    }

    [Fact]
    public void Migrations_create_campaign_shell_with_identity_mode_rls_and_tenant_guards()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE campaign_series", script);
        Assert.Contains("CREATE TABLE campaign", script);
        Assert.Contains("response_identity_mode character varying(64) NOT NULL", script);
        Assert.DoesNotContain("anonymous_default", script);
        Assert.Contains("CREATE TABLE audience", script);
        Assert.Contains("CREATE TABLE audience_member", script);
        Assert.Contains("CREATE TABLE respondent_rule", script);
        Assert.Contains("CREATE TABLE assignment", script);
        Assert.Contains("CREATE TABLE invitation_token", script);
        Assert.Contains("CREATE TABLE notification", script);
        Assert.Contains("CREATE TABLE notification_delivery_attempt", script);
        Assert.Contains("CREATE TABLE notification_delivery_event", script);
        Assert.Contains("CREATE TABLE email_suppression", script);
        Assert.Contains("CREATE TABLE participant_code", script);
        Assert.Contains("CONSTRAINT ck_campaign_series_code_salt_length CHECK", script);
        Assert.Contains("CONSTRAINT ck_campaign_response_identity_mode CHECK", script);
        Assert.Contains("CONSTRAINT ck_assignment_identity_shape CHECK", script);
        Assert.Contains("CONSTRAINT ck_notification_channel CHECK", script);
        Assert.Contains("CONSTRAINT ck_notification_status CHECK", script);
        Assert.Contains("CONSTRAINT ck_notification_delivery_attempt_status CHECK", script);
        Assert.Contains("CONSTRAINT ck_participant_code_hash_length CHECK", script);
        Assert.Contains("CONSTRAINT ck_participant_code_argon2_parameters CHECK", script);
        Assert.Contains("CREATE POLICY campaign_series_tenant_isolation ON campaign_series", script);
        Assert.Contains("CREATE POLICY campaign_tenant_isolation ON campaign", script);
        Assert.Contains("CREATE POLICY audience_tenant_isolation ON audience", script);
        Assert.Contains("CREATE POLICY audience_member_tenant_isolation ON audience_member", script);
        Assert.Contains("CREATE POLICY respondent_rule_tenant_isolation ON respondent_rule", script);
        Assert.Contains("CREATE POLICY assignment_tenant_isolation ON assignment", script);
        Assert.Contains("CREATE POLICY invitation_token_tenant_isolation ON invitation_token", script);
        Assert.Contains("CREATE POLICY notification_tenant_isolation ON notification", script);
        Assert.Contains(
            "CREATE POLICY notification_delivery_attempt_tenant_isolation ON notification_delivery_attempt",
            script);
        Assert.Contains(
            "CREATE POLICY notification_delivery_event_tenant_isolation ON notification_delivery_event",
            script);
        Assert.Contains("CREATE POLICY email_suppression_tenant_isolation ON email_suppression", script);
        Assert.Contains("CREATE POLICY participant_code_tenant_isolation ON participant_code", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION campaign_template_version_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION audience_member_subject_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION assignment_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION notification_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION notification_delivery_attempt_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION notification_delivery_event_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION participant_code_tenant_guard()", script);
        Assert.Contains("assignment shape does not match campaign response identity mode", script);
        Assert.Contains("notification campaign and assignment must belong to the same tenant campaign", script);
        Assert.Contains("notification delivery attempt must belong to the same tenant notification", script);
        Assert.Contains("notification delivery event must belong to the same tenant notification", script);
        Assert.Contains("notification delivery event must belong to the same tenant delivery attempt", script);
        Assert.Contains("participant code campaign series must belong to the same tenant", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION invitation_token_assignment_guard()", script);
        Assert.Contains("invitation token assignment must belong to the same tenant campaign", script);
    }

    [Fact]
    public void Migrations_guard_identified_queue_invitation_token_respondent_subject_tenant()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("ux_invitation_token_identified_queue_respondent", script);
        Assert.Contains("channel = 'identified_queue' AND respondent_subject_id IS NOT NULL", script);
        Assert.Contains("respondent_subject_id IS NOT NULL AND assignment_id IS NULL", script);
        Assert.Contains("respondent_subject_id IS NULL", script);
        Assert.Contains("subject.id = invitation_token.respondent_subject_id", script);
        Assert.Contains("subject.tenant_id = current_setting('app.current_tenant_id')::uuid", script);
        Assert.Contains("WHERE s.id = NEW.respondent_subject_id", script);
        Assert.Contains("AND s.tenant_id = NEW.tenant_id", script);
        Assert.Contains("invitation token respondent subject must belong to the same tenant", script);
        Assert.Contains("BEFORE INSERT OR UPDATE OF tenant_id, campaign_id, assignment_id, respondent_subject_id", script);
    }

    [Fact]
    public void Migrations_create_provider_message_id_lookup_for_acs_email_events()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE UNIQUE INDEX ux_notification_delivery_attempt_provider_message_id", script);
        Assert.Contains("ON notification_delivery_attempt (provider, provider_message_id)", script);
        Assert.Contains("WHERE provider_message_id IS NOT NULL", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION resolve_notification_delivery_attempt_by_provider_message_id", script);
        Assert.Contains("RETURNS TABLE(tenant_id uuid, notification_id uuid, delivery_attempt_id uuid)", script);
        Assert.Contains("SECURITY DEFINER", script);
        Assert.Contains("SET search_path = public", script);
    }

    [Fact]
    public void Migrations_create_campaign_launch_snapshot_with_rls_and_tenant_guard()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE campaign_launch_snapshot", script);
        Assert.Contains("launch_readiness jsonb NOT NULL", script);
        Assert.Contains("launch_packet jsonb NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_campaign_launch_snapshot_readiness_object CHECK", script);
        Assert.Contains("CONSTRAINT ck_campaign_launch_snapshot_packet_object CHECK", script);
        Assert.Contains("CONSTRAINT ck_campaign_launch_snapshot_question_count_positive CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_campaign_launch_snapshot_campaign_id", script);
        Assert.Contains("CREATE POLICY campaign_launch_snapshot_tenant_isolation ON campaign_launch_snapshot", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION campaign_launch_snapshot_tenant_guard()", script);
        Assert.Contains("campaign launch snapshot must match campaign, template, and scoring rule tenant", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION campaign_launch_snapshot_prevent_update_delete()", script);
    }

    [Fact]
    public void Migrations_create_response_tables_with_rls_and_tenant_guards()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE response_session", script);
        Assert.Contains("CREATE TABLE answer", script);
        Assert.Contains("value jsonb", script);
        Assert.Contains("CONSTRAINT ck_response_session_submitted_after_started CHECK", script);
        Assert.Contains("CONSTRAINT ck_response_session_time_taken_non_negative CHECK", script);
        Assert.Contains("CONSTRAINT ck_answer_not_skipped_and_na CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_answer_session_id_question_id", script);
        Assert.Contains("CREATE POLICY response_session_tenant_isolation ON response_session", script);
        Assert.Contains("CREATE POLICY answer_tenant_isolation ON answer", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION response_session_tenant_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION response_session_participant_code_guard()", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION answer_tenant_guard()", script);
        Assert.Contains("answer session and question must belong to the same tenant campaign template", script);
        Assert.Contains(
            "response session participant code must belong to the same tenant campaign series",
            script);
    }

    [Fact]
    public void Migrations_create_consent_tables_with_rls()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE consent_document", script);
        Assert.Contains("required_grants jsonb NOT NULL", script);
        Assert.Contains("optional_grants jsonb NOT NULL", script);
        Assert.Contains("CREATE TABLE consent_record", script);
        Assert.Contains("accepted_grants jsonb NOT NULL", script);
        Assert.Contains("consent_document_id uuid", script);
        Assert.Contains("CREATE POLICY consent_document_tenant_isolation ON consent_document", script);
        Assert.Contains("CREATE POLICY consent_record_tenant_isolation ON consent_record", script);
        Assert.Contains("fk_response_session_consent_record_consent_record_id", script);
        Assert.Contains("fk_campaign_launch_snapshot_consent_document_consent_document_id", script);
        Assert.Contains("subject_id uuid", script);
        Assert.Contains("fk_consent_record_subject_subject_id", script);
        Assert.Contains("consent record subject must match the assignment respondent subject", script);
    }

    [Fact]
    public void Migrations_create_policy_tables_with_rls_and_tenant_guards()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("CREATE TABLE retention_policy", script);
        Assert.Contains("publication_limits jsonb NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_retention_policy_publication_limits_object CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_retention_policy_campaign_series_id_version", script);
        Assert.Contains("CREATE POLICY retention_policy_tenant_isolation ON retention_policy", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION retention_policy_tenant_guard()", script);
        Assert.Contains("retention policy campaign series must belong to the same tenant", script);

        Assert.Contains("CREATE TABLE retention_due_batch", script);
        Assert.Contains("idempotency_key character varying(256) NOT NULL", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_retention_due_batch_tenant_id_idempotency_key", script);
        Assert.Contains("CREATE POLICY retention_due_batch_tenant_isolation ON retention_due_batch", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION retention_due_batch_tenant_guard()", script);
        Assert.Contains("retention due batch retention policy must belong to the same tenant campaign series", script);

        Assert.Contains("CREATE TABLE withdrawal_event", script);
        Assert.Contains("target_kind character varying(64) NOT NULL", script);
        Assert.Contains("response_session_id uuid", script);
        Assert.Contains("metadata_json jsonb NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_withdrawal_event_target_shape CHECK", script);
        Assert.Contains(
            "target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched', 'response_session')",
            script);
        Assert.Contains("status IN ('requested', 'planned', 'processing', 'completed', 'failed', 'denied')", script);
        Assert.Contains("CREATE INDEX ix_withdrawal_event_response_session_id", script);
        Assert.Contains("fk_withdrawal_event_response_session", script);
        Assert.Contains("action_after = 'delete'", script);
        Assert.Contains("status = 'completed'", script);
        Assert.Contains("CONSTRAINT ck_withdrawal_event_metadata_object CHECK", script);
        Assert.Contains("CREATE POLICY withdrawal_event_tenant_isolation ON withdrawal_event", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION withdrawal_event_tenant_guard()", script);
        Assert.Contains("IF NEW.response_session_id IS NOT NULL AND NOT EXISTS", script);
        Assert.Contains("FROM response_session rs", script);
        Assert.Contains("JOIN assignment a ON a.id = rs.assignment_id", script);
        Assert.Contains("JOIN campaign c ON c.id = a.campaign_id", script);
        Assert.Contains("withdrawal event target must belong to the same tenant campaign series", script);

        Assert.Contains("CREATE TABLE withdrawal_request_token", script);
        Assert.Contains("token_hash character varying(128) NOT NULL", script);
        Assert.Contains("requested_action character varying(64) NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_withdrawal_request_token_action CHECK", script);
        Assert.Contains("CONSTRAINT ck_withdrawal_request_token_expiry CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_withdrawal_request_token_token_hash", script);
        Assert.Contains("CREATE POLICY withdrawal_request_token_tenant_isolation ON withdrawal_request_token", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION withdrawal_request_token_tenant_guard()", script);
        Assert.Contains("withdrawal request token target must belong to the same tenant", script);

        Assert.Contains("CREATE TABLE disclosure_policy", script);
        Assert.Contains("applies_to_dimensions jsonb NOT NULL", script);
        Assert.Contains("CONSTRAINT ck_disclosure_policy_applies_to_dimensions_array CHECK", script);
        Assert.Contains("CREATE UNIQUE INDEX ix_disclosure_policy_campaign_series_id_version", script);
        Assert.Contains("CREATE POLICY disclosure_policy_tenant_isolation ON disclosure_policy", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION disclosure_policy_tenant_guard()", script);
        Assert.Contains("disclosure policy campaign series must belong to the same tenant", script);

        Assert.Contains("retention_policy_id uuid", script);
        Assert.Contains("disclosure_policy_id uuid", script);
        Assert.Contains("fk_campaign_launch_snapshot_retention_policy_retention_policy_id", script);
        Assert.Contains("fk_campaign_launch_snapshot_disclosure_policy_disclosure_policy_id", script);
        Assert.Contains("campaign launch snapshot policy ids must match tenant and campaign series", script);
    }

    [Fact]
    public void Migrations_update_export_artifact_target_scope()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("ALTER TABLE export_artifact ADD target_kind", script);
        Assert.Contains("artifact_type = 'campaign_series_response_csv_codebook'", script);
        Assert.Contains("target_kind = 'campaign_series'", script);
        Assert.Contains("artifact_type = 'report_proof_csv_codebook'", script);
        Assert.Contains("target_kind = 'campaign'", script);
        Assert.Contains("ALTER TABLE export_artifact ALTER COLUMN campaign_id DROP NOT NULL", script);
        Assert.Contains("ck_export_artifact_target_kind", script);
        Assert.Contains("ck_export_artifact_target_scope", script);
        Assert.Contains("DROP POLICY IF EXISTS export_artifact_tenant_isolation ON export_artifact", script);
        Assert.Contains("CREATE POLICY export_artifact_tenant_isolation ON export_artifact", script);
        Assert.Contains("export_artifact.target_kind = 'campaign'", script);
        Assert.Contains("export_artifact.target_kind = 'campaign_series'", script);
        Assert.Contains("FROM campaign_series AS cs", script);
        Assert.Contains("CREATE OR REPLACE FUNCTION export_artifact_tenant_guard()", script);
        Assert.Contains("campaign-series-targeted export artifact must belong to the same tenant", script);
    }

    [Fact]
    public void Migrations_update_export_artifact_lifecycle()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("ALTER TABLE export_artifact ADD started_at", script);
        Assert.Contains("ALTER TABLE export_artifact ADD failed_at", script);
        Assert.Contains("ALTER TABLE export_artifact ADD expires_at", script);
        Assert.Contains("ALTER TABLE export_artifact ADD deleted_at", script);
        Assert.Contains("ALTER TABLE export_artifact ADD failure_reason_code", script);
        Assert.Contains("ALTER TABLE export_artifact ALTER COLUMN checksum_sha256 DROP NOT NULL", script);
        Assert.Contains("ALTER TABLE export_artifact ALTER COLUMN content DROP NOT NULL", script);
        Assert.Contains("status IN ('queued','rendering','succeeded','failed','expired','deleted')", script);
        Assert.Contains("ck_export_artifact_materialization_shape", script);
        Assert.Contains("ck_export_artifact_lifecycle_shape", script);
        Assert.Contains("ck_export_artifact_failure_reason_shape", script);
        Assert.Contains("UPDATE export_artifact", script);
        Assert.Contains("SET status = 'succeeded'", script);
    }

    [Fact]
    public void Migrations_update_export_artifact_report_artifact_types_and_formats()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("format IN ('csv_codebook','html','pdf')", script);
        Assert.Contains(
            "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook','campaign_series_report_html','campaign_series_report_pdf')",
            script);
        Assert.Contains("ck_export_artifact_format", script);
        Assert.Contains("ck_export_artifact_type", script);
    }

    [Fact]
    public void Migrations_update_export_artifact_storage_location_contract()
    {
        var script = GenerateMigrationScript();

        Assert.Contains("ALTER TABLE export_artifact ADD storage_kind", script);
        Assert.Contains("ALTER TABLE export_artifact ADD storage_key", script);
        Assert.Contains("UPDATE export_artifact", script);
        Assert.Contains("SET storage_kind = 'inline_text'", script);
        Assert.Contains("storage_kind IN ('inline_text','external_object')", script);
        Assert.Contains("ck_export_artifact_storage_kind", script);
        Assert.Contains("ck_export_artifact_storage_shape", script);
        Assert.Contains("storage_kind = 'inline_text'", script);
        Assert.Contains("storage_kind = 'external_object'", script);
        Assert.Contains("storage_key IS NOT NULL", script);
    }

    private static string GenerateMigrationScript()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        return db.Database.GetService<IMigrator>()
            .GenerateScript(options: MigrationsSqlGenerationOptions.NoTransactions);
    }
}
