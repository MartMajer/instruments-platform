\set ON_ERROR_STOP on

SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'runtime_user', :'runtime_password')
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'runtime_user'
)
\gexec

SELECT format('ALTER ROLE %I LOGIN PASSWORD %L', :'runtime_user', :'runtime_password')
\gexec

SELECT format('CREATE ROLE %I', :'worker_user')
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'worker_user'
)
\gexec

SELECT format('ALTER ROLE %I LOGIN PASSWORD %L', :'worker_user', :'worker_password')
\gexec

GRANT USAGE ON SCHEMA public TO :"runtime_user";
GRANT USAGE ON SCHEMA public TO :"worker_user";

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    tenant,
    user_account,
    external_auth_identity,
    auth_session,
    role,
    permission,
    role_permission,
    role_assignment,
    subject,
    subject_group,
    subject_membership,
    subject_relationship,
    instrument,
    instrument_subscale,
    instrument_item,
    instrument_norm,
    translation,
    survey_template,
    template_version,
    scoring_rule,
    score_run,
    score,
    export_artifact,
    campaign_series,
    campaign,
    campaign_launch_snapshot,
    consent_document,
    retention_policy,
    retention_due_batch,
    withdrawal_event,
    withdrawal_request_token,
    disclosure_policy,
    consent_record,
    audience,
    audience_member,
    respondent_rule,
    assignment,
    invitation_token,
    notification,
    notification_delivery_attempt,
    notification_delivery_event,
    email_suppression,
    operational_notification,
    registration_intent,
    participant_code,
    response_session,
    answer,
    section,
    scale,
    question,
    choice_option
TO :"runtime_user";

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    tenant,
    user_account,
    external_auth_identity,
    auth_session,
    role,
    permission,
    role_permission,
    role_assignment,
    subject,
    subject_group,
    subject_membership,
    subject_relationship,
    instrument,
    instrument_subscale,
    instrument_item,
    instrument_norm,
    translation,
    survey_template,
    template_version,
    scoring_rule,
    score_run,
    score,
    export_artifact,
    campaign_series,
    campaign,
    campaign_launch_snapshot,
    consent_document,
    retention_policy,
    retention_due_batch,
    withdrawal_event,
    withdrawal_request_token,
    disclosure_policy,
    consent_record,
    audience,
    audience_member,
    respondent_rule,
    assignment,
    invitation_token,
    notification,
    notification_delivery_attempt,
    notification_delivery_event,
    email_suppression,
    operational_notification,
    registration_intent,
    participant_code,
    response_session,
    answer,
    section,
    scale,
    question,
    choice_option
TO :"worker_user";

GRANT SELECT, INSERT ON TABLE
    audit_event,
    outbox_event
TO :"runtime_user";

GRANT SELECT, INSERT ON TABLE
    audit_event
TO :"worker_user";

GRANT SELECT, INSERT, UPDATE ON TABLE
    outbox_event,
    worker_heartbeat
TO :"worker_user";

GRANT SELECT ON TABLE
    worker_heartbeat
TO :"runtime_user";
