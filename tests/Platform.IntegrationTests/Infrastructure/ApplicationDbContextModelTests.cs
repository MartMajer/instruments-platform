using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Platform.Domain.Auditing;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Instruments;
using Platform.Domain.Operations;
using Platform.Domain.Outbox;
using Platform.Domain.Responses;
using Platform.Domain.Reports;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ApplicationDbContextModelTests
{
    [Fact]
    public void Design_time_factory_creates_npgsql_context()
    {
        var factory = new ApplicationDbContextFactory();

        using var db = factory.CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
    }

    [Fact]
    public void Tenant_entity_maps_to_expected_table_and_key()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var entity = db.Model.FindEntityType(typeof(Tenant));

        Assert.NotNull(entity);
        Assert.Equal("tenant", entity.GetTableName());
        Assert.Equal(nameof(Tenant.Id), entity.FindPrimaryKey()!.Properties.Single().Name);
    }

    [Fact]
    public void Email_template_and_notification_locale_model_maps_expected_columns_and_indexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var emailTemplate = model.FindEntityType(typeof(EmailTemplate));
        var notification = model.FindEntityType(typeof(Notification));

        Assert.NotNull(emailTemplate);
        Assert.Equal("email_template", emailTemplate.GetTableName());
        Assert.Equal("template_code", emailTemplate.FindProperty(nameof(EmailTemplate.TemplateCode))!.GetColumnName());
        Assert.Equal("body_text", emailTemplate.FindProperty(nameof(EmailTemplate.BodyText))!.GetColumnName());
        Assert.Equal(128, emailTemplate.FindProperty(nameof(EmailTemplate.TemplateCode))!.GetMaxLength());
        Assert.Equal(16, emailTemplate.FindProperty(nameof(EmailTemplate.Locale))!.GetMaxLength());
        Assert.Equal(160, emailTemplate.FindProperty(nameof(EmailTemplate.Subject))!.GetMaxLength());
        Assert.Contains(emailTemplate.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(EmailTemplate.TenantId),
                    nameof(EmailTemplate.TemplateCode),
                    nameof(EmailTemplate.Locale)
                ]));
        Assert.Contains(emailTemplate.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(EmailTemplate.TenantId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Tenant));

        Assert.NotNull(notification);
        Assert.Equal("locale", notification.FindProperty(nameof(Notification.Locale))!.GetColumnName());
        Assert.Equal(16, notification.FindProperty(nameof(Notification.Locale))!.GetMaxLength());
    }

    [Fact]
    public void Score_entity_maps_output_metadata_columns_and_constraints()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var score = model.FindEntityType(typeof(Score));

        Assert.NotNull(score);
        Assert.Equal("score", score.GetTableName());
        Assert.Equal("n", score.FindProperty(nameof(Score.NValid))!.GetColumnName());
        Assert.Equal("n_expected", score.FindProperty(nameof(Score.NExpected))!.GetColumnName());
        Assert.Equal(
            "missing_policy_status",
            score.FindProperty(nameof(Score.MissingPolicyStatus))!.GetColumnName());
        Assert.Equal(64, score.FindProperty(nameof(Score.MissingPolicyStatus))!.GetMaxLength());
        Assert.Contains(score.GetCheckConstraints(), check => check.Name == "ck_score_n_non_negative");
        Assert.Contains(score.GetCheckConstraints(), check => check.Name == "ck_score_n_expected_non_negative");
        Assert.Contains(score.GetCheckConstraints(), check => check.Name == "ck_score_n_valid_not_above_expected");
        Assert.Contains(score.GetCheckConstraints(), check => check.Name == "ck_score_missing_policy_status_shape");
    }

    [Fact]
    public void Auth_entities_map_to_expected_tables_keys_and_indexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var userAccount = model.FindEntityType(typeof(UserAccount));
        var role = model.FindEntityType(typeof(Role));
        var permission = model.FindEntityType(typeof(Permission));
        var rolePermission = model.FindEntityType(typeof(RolePermission));
        var roleAssignment = model.FindEntityType(typeof(RoleAssignment));
        var externalAuthIdentity = model.FindEntityType(typeof(ExternalAuthIdentity));
        var authSession = model.FindEntityType(typeof(AuthSession));
        var auditEvent = model.FindEntityType(typeof(AuditEvent));
        var outboxEvent = model.FindEntityType(typeof(OutboxEvent));

        Assert.NotNull(userAccount);
        Assert.NotNull(role);
        Assert.NotNull(permission);
        Assert.NotNull(rolePermission);
        Assert.NotNull(roleAssignment);
        Assert.NotNull(externalAuthIdentity);
        Assert.NotNull(authSession);
        Assert.NotNull(auditEvent);
        Assert.NotNull(outboxEvent);

        Assert.Equal("user_account", userAccount.GetTableName());
        Assert.Equal("role", role.GetTableName());
        Assert.Equal("permission", permission.GetTableName());
        Assert.Equal("role_permission", rolePermission.GetTableName());
        Assert.Equal("role_assignment", roleAssignment.GetTableName());
        Assert.Equal("external_auth_identity", externalAuthIdentity.GetTableName());
        Assert.Equal("auth_session", authSession.GetTableName());
        Assert.Equal("audit_event", auditEvent.GetTableName());
        Assert.Equal("outbox_event", outboxEvent.GetTableName());

        Assert.Equal("citext", userAccount.FindProperty(nameof(UserAccount.Email))!.GetColumnType());
        Assert.Contains(userAccount.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(UserAccount.TenantId), nameof(UserAccount.Email)]));

        Assert.Contains(role.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "tenant_id IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Role.TenantId), nameof(Role.Code)]));

        Assert.Contains(role.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "tenant_id IS NULL" &&
            index.Properties.Single().Name == nameof(Role.Code));

        Assert.Contains(permission.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(Permission.Code));

        Assert.Equal(
            [nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId)],
            rolePermission.FindPrimaryKey()!.Properties.Select(property => property.Name));

        Assert.Contains(roleAssignment.GetCheckConstraints(), check =>
            check.Name == "ck_role_assignment_scope");

        Assert.Equal(
            "provider_subject_hash",
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.ProviderSubjectHash))!
                .GetColumnName());
        Assert.Equal(
            "email_verified_at",
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerifiedAt))!
                .GetColumnName());
        Assert.Equal(
            "timestamp with time zone",
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerifiedAt))!
                .GetColumnType());
        Assert.True(externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerifiedAt))!.IsNullable);
        Assert.Equal(
            "email_verification_grace_used_at",
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerificationGraceUsedAt))!
                .GetColumnName());
        Assert.Equal(
            "timestamp with time zone",
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerificationGraceUsedAt))!
                .GetColumnType());
        Assert.True(
            externalAuthIdentity.FindProperty(nameof(ExternalAuthIdentity.EmailVerificationGraceUsedAt))!.IsNullable);
        Assert.Null(externalAuthIdentity.FindProperty("ProviderSubject"));
        Assert.Contains(externalAuthIdentity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ExternalAuthIdentity.TenantId),
                    nameof(ExternalAuthIdentity.Provider),
                    nameof(ExternalAuthIdentity.ProviderSubjectHash)
                ]));
        Assert.Contains(externalAuthIdentity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ExternalAuthIdentity.TenantId),
                    nameof(ExternalAuthIdentity.UserId),
                    nameof(ExternalAuthIdentity.Provider)
                ]));
        Assert.Equal(
            64,
            authSession.FindProperty(nameof(AuthSession.RevokedReason))!.GetMaxLength());

        Assert.Equal(
            [nameof(AuditEvent.Id), nameof(AuditEvent.OccurredAt)],
            auditEvent.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal("jsonb", auditEvent.FindProperty(nameof(AuditEvent.Before))!.GetColumnType());
        Assert.Equal("jsonb", auditEvent.FindProperty(nameof(AuditEvent.After))!.GetColumnType());

        Assert.Equal(nameof(OutboxEvent.Id), outboxEvent.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal("jsonb", outboxEvent.FindProperty(nameof(OutboxEvent.Payload))!.GetColumnType());
        Assert.Contains(outboxEvent.GetIndexes(), index =>
            index.GetFilter() == "published_at IS NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(OutboxEvent.NextRetryAt)]));
    }

    [Fact]
    public void Subject_entities_map_to_expected_tables_keys_and_indexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var subject = model.FindEntityType(typeof(Subject));
        var subjectGroup = model.FindEntityType(typeof(SubjectGroup));
        var subjectMembership = model.FindEntityType(typeof(SubjectMembership));
        var subjectRelationship = model.FindEntityType(typeof(SubjectRelationship));

        Assert.NotNull(subject);
        Assert.NotNull(subjectGroup);
        Assert.NotNull(subjectMembership);
        Assert.NotNull(subjectRelationship);

        Assert.Equal("subject", subject.GetTableName());
        Assert.Equal("subject_group", subjectGroup.GetTableName());
        Assert.Equal("subject_membership", subjectMembership.GetTableName());
        Assert.Equal("subject_relationship", subjectRelationship.GetTableName());

        Assert.Equal("citext", subject.FindProperty(nameof(Subject.Email))!.GetColumnType());
        Assert.Equal("jsonb", subject.FindProperty(nameof(Subject.Attributes))!.GetColumnType());
        Assert.Equal("jsonb", subjectGroup.FindProperty(nameof(SubjectGroup.Attributes))!.GetColumnType());

        Assert.Contains(subject.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "external_id IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Subject.TenantId), nameof(Subject.ExternalId)]));

        Assert.Contains(subject.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "email IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Subject.TenantId), nameof(Subject.Email)]));

        Assert.Equal(
            [nameof(SubjectMembership.SubjectId), nameof(SubjectMembership.GroupId)],
            subjectMembership.FindPrimaryKey()!.Properties.Select(property => property.Name));

        Assert.Contains(subjectMembership.GetCheckConstraints(), check =>
            check.Name == "ck_subject_membership_valid_range");

        Assert.Contains(subjectRelationship.GetCheckConstraints(), check =>
            check.Name == "ck_subject_relationship_not_self_unless_self_type");
        Assert.Contains(subjectRelationship.GetCheckConstraints(), check =>
            check.Name == "ck_subject_relationship_valid_range");
    }

    [Fact]
    public void Instrument_entities_map_to_expected_tables_keys_indexes_and_constraints()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var instrument = model.FindEntityType(typeof(Instrument));
        var subscale = model.FindEntityType(typeof(InstrumentSubscale));
        var item = model.FindEntityType(typeof(InstrumentItem));
        var norm = model.FindEntityType(typeof(InstrumentNorm));
        var translation = model.FindEntityType(typeof(InstrumentTranslation));

        Assert.NotNull(instrument);
        Assert.NotNull(subscale);
        Assert.NotNull(item);
        Assert.NotNull(norm);
        Assert.NotNull(translation);

        Assert.Equal("instrument", instrument.GetTableName());
        Assert.Equal("instrument_subscale", subscale.GetTableName());
        Assert.Equal("instrument_item", item.GetTableName());
        Assert.Equal("instrument_norm", norm.GetTableName());
        Assert.Equal("translation", translation.GetTableName());

        Assert.Equal("text[]", instrument.FindProperty(nameof(Instrument.Developers))!.GetColumnType());
        Assert.Equal("rights_scope", instrument.FindProperty(nameof(Instrument.RightsScope))!.GetColumnName());
        Assert.Equal("rights_status", instrument.FindProperty(nameof(Instrument.RightsStatus))!.GetColumnName());
        Assert.Equal("validity_label", instrument.FindProperty(nameof(Instrument.ValidityLabel))!.GetColumnName());
        Assert.Equal("provenance_note", instrument.FindProperty(nameof(Instrument.ProvenanceNote))!.GetColumnName());
        Assert.Equal("jsonb", norm.FindProperty(nameof(InstrumentNorm.Percentiles))!.GetColumnType());

        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_domain");
        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_global_tenant_shape");
        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_rights_scope");
        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_rights_status");
        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_validity_label");
        Assert.Contains(instrument.GetCheckConstraints(), check =>
            check.Name == "ck_instrument_private_import_shape");
        Assert.Contains(instrument.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "tenant_id IS NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Instrument.Code), nameof(Instrument.Version)]));
        Assert.Contains(instrument.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "tenant_id IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Instrument.TenantId), nameof(Instrument.Code), nameof(Instrument.Version)]));

        Assert.Contains(subscale.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InstrumentSubscale.InstrumentId), nameof(InstrumentSubscale.Code)]));
        Assert.Contains(item.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InstrumentItem.InstrumentId), nameof(InstrumentItem.Ordinal)]));
        Assert.Contains(translation.GetCheckConstraints(), check =>
            check.Name == "ck_translation_exactly_one_target");
    }

    [Fact]
    public void Template_entities_map_to_expected_tables_keys_indexes_and_constraints()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var template = model.FindEntityType(typeof(SurveyTemplate));
        var version = model.FindEntityType(typeof(TemplateVersion));
        var section = model.FindEntityType(typeof(TemplateSection));
        var scale = model.FindEntityType(typeof(QuestionScale));
        var question = model.FindEntityType(typeof(TemplateQuestion));
        var choice = model.FindEntityType(typeof(ChoiceOption));
        var item = model.FindEntityType(typeof(InstrumentItem));
        var translation = model.FindEntityType(typeof(InstrumentTranslation));

        Assert.NotNull(template);
        Assert.NotNull(version);
        Assert.NotNull(section);
        Assert.NotNull(scale);
        Assert.NotNull(question);
        Assert.NotNull(choice);
        Assert.NotNull(item);
        Assert.NotNull(translation);

        Assert.Equal("survey_template", template.GetTableName());
        Assert.Equal("template_version", version.GetTableName());
        Assert.Equal("section", section.GetTableName());
        Assert.Equal("scale", scale.GetTableName());
        Assert.Equal("question", question.GetTableName());
        Assert.Equal("choice_option", choice.GetTableName());

        Assert.Equal("jsonb", scale.FindProperty(nameof(QuestionScale.Anchors))!.GetColumnType());
        Assert.Equal("jsonb", question.FindProperty(nameof(TemplateQuestion.Payload))!.GetColumnType());
        Assert.Equal("jsonb", question.FindProperty(nameof(TemplateQuestion.MissingCodes))!.GetColumnType());

        Assert.Contains(version.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TemplateVersion.TemplateId), nameof(TemplateVersion.Semver)]));
        Assert.Contains(scale.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(QuestionScale.TemplateVersionId), nameof(QuestionScale.Code)]));
        Assert.Contains(question.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "code IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TemplateQuestion.TemplateVersionId), nameof(TemplateQuestion.Code)]));
        Assert.Contains(section.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TemplateSection.TemplateVersionId), nameof(TemplateSection.Ordinal)]));
        Assert.Contains(question.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TemplateQuestion.TemplateVersionId), nameof(TemplateQuestion.Ordinal)]));
        Assert.Contains(choice.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ChoiceOption.QuestionId), nameof(ChoiceOption.Value)]));
        Assert.Contains(question.GetCheckConstraints(), check =>
            check.Name == "ck_question_scale_only_for_scale_backed");

        Assert.Contains(section.GetKeys(), key =>
            key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TemplateSection.Id), nameof(TemplateSection.TemplateVersionId)]));
        Assert.Contains(scale.GetKeys(), key =>
            key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(QuestionScale.Id), nameof(QuestionScale.TemplateVersionId)]));

        Assert.Contains(item.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(InstrumentItem.QuestionId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(TemplateQuestion));

        Assert.Contains(translation.GetCheckConstraints(), check =>
            check.Name == "ck_translation_exactly_one_target");
    }

    [Fact]
    public void Scoring_rule_maps_to_template_version_with_jsonb_metadata_and_constraints()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var scoringRule = model.FindEntityType(typeof(ScoringRule));

        Assert.NotNull(scoringRule);
        Assert.Equal("scoring_rule", scoringRule.GetTableName());
        Assert.Equal("jsonb", scoringRule.FindProperty(nameof(ScoringRule.Document))!.GetColumnType());
        Assert.Equal("jsonb", scoringRule.FindProperty(nameof(ScoringRule.Produces))!.GetColumnType());
        Assert.Equal("jsonb", scoringRule.FindProperty(nameof(ScoringRule.Compatibility))!.GetColumnType());

        Assert.Contains(scoringRule.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ScoringRule.TemplateVersionId),
                    nameof(ScoringRule.RuleKey),
                    nameof(ScoringRule.RuleVersion)
                ]));

        Assert.Contains(scoringRule.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ScoringRule.TemplateVersionId),
                    nameof(ScoringRule.Status)
                ]));

        Assert.Contains(scoringRule.GetCheckConstraints(), check =>
            check.Name == "ck_scoring_rule_status");
        Assert.Contains(scoringRule.GetCheckConstraints(), check =>
            check.Name == "ck_scoring_rule_document_hash");
        Assert.Contains(scoringRule.GetCheckConstraints(), check =>
            check.Name == "ck_scoring_rule_publish_shape");

        Assert.Contains(scoringRule.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ScoringRule.TemplateVersionId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(TemplateVersion));
    }

    [Fact]
    public void Campaign_model_maps_to_expected_tables_indexes_constraints_and_foreign_keys()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var series = model.FindEntityType(typeof(CampaignSeries));
        var campaign = model.FindEntityType(typeof(Campaign));
        var audience = model.FindEntityType(typeof(Audience));
        var audienceMember = model.FindEntityType(typeof(AudienceMember));
        var respondentRule = model.FindEntityType(typeof(RespondentRule));
        var assignment = model.FindEntityType(typeof(Assignment));
        var invitationToken = model.FindEntityType(typeof(InvitationToken));
        var notification = model.FindEntityType(typeof(Notification));
        var notificationDeliveryAttempt = model.FindEntityType(typeof(NotificationDeliveryAttempt));
        var operationalNotification = model.FindEntityType(typeof(OperationalNotification));
        var participantCode = model.FindEntityType(typeof(ParticipantCode));
        var launchSnapshot = model.FindEntityType(typeof(CampaignLaunchSnapshot));

        Assert.NotNull(series);
        Assert.NotNull(campaign);
        Assert.NotNull(audience);
        Assert.NotNull(audienceMember);
        Assert.NotNull(respondentRule);
        Assert.NotNull(assignment);
        Assert.NotNull(invitationToken);
        Assert.NotNull(notification);
        Assert.NotNull(notificationDeliveryAttempt);
        Assert.NotNull(operationalNotification);
        Assert.NotNull(participantCode);
        Assert.NotNull(launchSnapshot);

        Assert.Equal("campaign_series", series.GetTableName());
        Assert.Equal("campaign", campaign.GetTableName());
        Assert.Equal("audience", audience.GetTableName());
        Assert.Equal("audience_member", audienceMember.GetTableName());
        Assert.Equal("respondent_rule", respondentRule.GetTableName());
        Assert.Equal("assignment", assignment.GetTableName());
        Assert.Equal("invitation_token", invitationToken.GetTableName());
        Assert.Equal("notification", notification.GetTableName());
        Assert.Equal("notification_delivery_attempt", notificationDeliveryAttempt.GetTableName());
        Assert.Equal("operational_notification", operationalNotification.GetTableName());
        Assert.Equal("participant_code", participantCode.GetTableName());
        Assert.Equal("campaign_launch_snapshot", launchSnapshot.GetTableName());

        Assert.Equal("jsonb", campaign.FindProperty(nameof(Campaign.Schedule))!.GetColumnType());
        Assert.Equal("jsonb", launchSnapshot.FindProperty(nameof(CampaignLaunchSnapshot.LaunchReadiness))!.GetColumnType());
        Assert.Equal("jsonb", launchSnapshot.FindProperty(nameof(CampaignLaunchSnapshot.LaunchPacket))!.GetColumnType());
        Assert.Equal("jsonb", audience.FindProperty(nameof(Audience.Selector))!.GetColumnType());
        Assert.Equal("jsonb", respondentRule.FindProperty(nameof(RespondentRule.Rule))!.GetColumnType());
        Assert.Equal("jsonb", operationalNotification.FindProperty(nameof(OperationalNotification.PayloadJson))!.GetColumnType());

        Assert.Contains(series.GetCheckConstraints(), check => check.Name == "ck_campaign_series_code_salt_length");
        Assert.Contains(campaign.GetCheckConstraints(), check => check.Name == "ck_campaign_status");
        Assert.Contains(campaign.GetCheckConstraints(), check => check.Name == "ck_campaign_response_identity_mode");
        Assert.Contains(campaign.GetCheckConstraints(), check => check.Name == "ck_campaign_schedule_object");
        Assert.Contains(campaign.GetCheckConstraints(), check => check.Name == "ck_campaign_date_range");
        Assert.Contains(respondentRule.GetCheckConstraints(), check => check.Name == "ck_respondent_rule_ordinal_positive");
        Assert.Contains(assignment.GetCheckConstraints(), check => check.Name == "ck_assignment_status");
        Assert.Contains(assignment.GetCheckConstraints(), check => check.Name == "ck_assignment_identity_shape");
        Assert.Contains(invitationToken.GetCheckConstraints(), check => check.Name == "ck_invitation_token_channel");
        Assert.Contains(notification.GetCheckConstraints(), check => check.Name == "ck_notification_channel");
        Assert.Contains(notification.GetCheckConstraints(), check => check.Name == "ck_notification_status");
        Assert.Contains(notificationDeliveryAttempt.GetCheckConstraints(), check =>
            check.Name == "ck_notification_delivery_attempt_status");
        Assert.Contains(operationalNotification.GetCheckConstraints(), check =>
            check.Name == "ck_operational_notification_status");
        Assert.Contains(operationalNotification.GetCheckConstraints(), check =>
            check.Name == "ck_operational_notification_severity");
        Assert.Contains(participantCode.GetCheckConstraints(), check =>
            check.Name == "ck_participant_code_hash_length");
        Assert.Contains(participantCode.GetCheckConstraints(), check =>
            check.Name == "ck_participant_code_argon2_parameters");

        Assert.Contains(respondentRule.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RespondentRule.CampaignId), nameof(RespondentRule.Ordinal)]));
        Assert.Contains(assignment.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "respondent_subject_id IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(Assignment.CampaignId),
                    nameof(Assignment.TargetSubjectId),
                    nameof(Assignment.RespondentSubjectId)
                ]));
        Assert.Contains(assignment.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "invite_token_id IS NOT NULL" &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Assignment.InviteTokenId)]));
        Assert.Contains(invitationToken.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InvitationToken.TokenHash)]));
        Assert.Contains(invitationToken.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InvitationToken.AssignmentId)]));
        Assert.Contains(notification.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Notification.TenantId), nameof(Notification.CampaignId)]));
        Assert.Contains(notification.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Notification.AssignmentId)]));
        Assert.Contains(notification.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Notification.Status), nameof(Notification.ScheduledFor)]));
        Assert.Contains(notificationDeliveryAttempt.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(NotificationDeliveryAttempt.TenantId),
                    nameof(NotificationDeliveryAttempt.NotificationId)
                ]));
        Assert.Contains(notificationDeliveryAttempt.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(NotificationDeliveryAttempt.NotificationId),
                    nameof(NotificationDeliveryAttempt.CreatedAt)
                ]));
        Assert.Contains(operationalNotification.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(OperationalNotification.TenantId),
                    nameof(OperationalNotification.SourceAggregateId),
                    nameof(OperationalNotification.SourceEventType),
                    nameof(OperationalNotification.NotificationType)
                ]));
        Assert.Contains(operationalNotification.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(OperationalNotification.TenantId),
                    nameof(OperationalNotification.Status),
                    nameof(OperationalNotification.CreatedAt)
                ]));
        Assert.DoesNotContain(
            notificationDeliveryAttempt.GetProperties(),
            property => property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(participantCode.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ParticipantCode.CampaignSeriesId), nameof(ParticipantCode.Hash)]));
        Assert.DoesNotContain(
            participantCode.GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase) ||
                property.Name == "Code");
        Assert.Contains(launchSnapshot.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(CampaignLaunchSnapshot.CampaignId));
        Assert.Contains(launchSnapshot.GetCheckConstraints(), check =>
            check.Name == "ck_campaign_launch_snapshot_readiness_object");
        Assert.Contains(launchSnapshot.GetCheckConstraints(), check =>
            check.Name == "ck_campaign_launch_snapshot_packet_object");
        Assert.Contains(launchSnapshot.GetCheckConstraints(), check =>
            check.Name == "ck_campaign_launch_snapshot_question_count_positive");

        Assert.Contains(audienceMember.GetKeys(), key =>
            key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AudienceMember.AudienceId), nameof(AudienceMember.SubjectId)]));

        Assert.Contains(campaign.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Campaign.TemplateVersionId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(TemplateVersion));
        Assert.Contains(audience.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Audience.CampaignId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Campaign));
        Assert.Contains(audienceMember.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(AudienceMember.SubjectId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Subject));
        Assert.Contains(assignment.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Assignment.InviteTokenId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(InvitationToken));
        Assert.Contains(invitationToken.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(InvitationToken.AssignmentId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Assignment));
        Assert.Contains(notification.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Notification.CampaignId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Campaign));
        Assert.Contains(notification.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Notification.AssignmentId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Assignment));
        Assert.Contains(notificationDeliveryAttempt.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(NotificationDeliveryAttempt.NotificationId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Notification));
        Assert.Contains(participantCode.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ParticipantCode.CampaignSeriesId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CampaignSeries));
        Assert.Contains(launchSnapshot.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(CampaignLaunchSnapshot.CampaignId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Campaign));
        Assert.Contains(launchSnapshot.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(CampaignLaunchSnapshot.ScoringRuleId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ScoringRule));
    }

    [Fact]
    public void Response_session_assignment_index_is_not_unique_for_open_link_sessions()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var responseSession = model.FindEntityType(typeof(ResponseSession));
        var answer = model.FindEntityType(typeof(Answer));

        Assert.NotNull(responseSession);
        Assert.NotNull(answer);
        Assert.Equal(
            "public_handle_hash",
            responseSession.FindProperty(nameof(ResponseSession.PublicHandleHash))!.GetColumnName());
        Assert.Equal(
            "public_handle_issued_at",
            responseSession.FindProperty(nameof(ResponseSession.PublicHandleIssuedAt))!.GetColumnName());
        Assert.Contains(responseSession.GetIndexes(), index =>
            !index.IsUnique &&
            index.Properties.Single().Name == nameof(ResponseSession.AssignmentId));
        Assert.Contains(responseSession.GetIndexes(), index =>
            index.IsUnique &&
            index.GetFilter() == "public_handle_hash IS NOT NULL" &&
            index.Properties.Single().Name == nameof(ResponseSession.PublicHandleHash));
        Assert.Contains(responseSession.GetCheckConstraints(), check =>
            check.Name == "ck_response_session_public_handle_hash");
        Assert.Contains(answer.GetCheckConstraints(), check =>
            check.Name == "ck_answer_skipped_na_payload_shape");
        Assert.Contains(responseSession.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ResponseSession.ParticipantCodeId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ParticipantCode));
    }

    [Fact]
    public void Consent_entities_map_to_expected_tables_indexes_and_foreign_keys()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var consentDocument = model.FindEntityType(typeof(ConsentDocument));
        var consentRecord = model.FindEntityType(typeof(ConsentRecord));
        var retentionPolicy = model.FindEntityType(typeof(RetentionPolicy));
        var withdrawalEvent = model.FindEntityType(typeof(WithdrawalEvent));
        var withdrawalRequestToken = model.FindEntityType(typeof(WithdrawalRequestToken));
        var disclosurePolicy = model.FindEntityType(typeof(DisclosurePolicy));
        var launchSnapshot = model.FindEntityType(typeof(CampaignLaunchSnapshot));
        var responseSession = model.FindEntityType(typeof(ResponseSession));

        Assert.NotNull(consentDocument);
        Assert.NotNull(consentRecord);
        Assert.NotNull(retentionPolicy);
        Assert.NotNull(withdrawalEvent);
        Assert.NotNull(withdrawalRequestToken);
        Assert.NotNull(disclosurePolicy);
        Assert.NotNull(launchSnapshot);
        Assert.NotNull(responseSession);

        Assert.Equal("consent_document", consentDocument.GetTableName());
        Assert.Equal("consent_record", consentRecord.GetTableName());
        Assert.Equal("retention_policy", retentionPolicy.GetTableName());
        Assert.Equal("withdrawal_event", withdrawalEvent.GetTableName());
        Assert.Equal("withdrawal_request_token", withdrawalRequestToken.GetTableName());
        Assert.Equal("disclosure_policy", disclosurePolicy.GetTableName());
        Assert.Equal("jsonb", consentDocument.FindProperty(nameof(ConsentDocument.RequiredGrants))!.GetColumnType());
        Assert.Equal("jsonb", consentDocument.FindProperty(nameof(ConsentDocument.OptionalGrants))!.GetColumnType());
        Assert.Equal("jsonb", consentRecord.FindProperty(nameof(ConsentRecord.AcceptedGrants))!.GetColumnType());
        Assert.Equal("jsonb", retentionPolicy.FindProperty(nameof(RetentionPolicy.PublicationLimits))!.GetColumnType());
        Assert.Equal("jsonb", withdrawalEvent.FindProperty(nameof(WithdrawalEvent.MetadataJson))!.GetColumnType());
        Assert.True(withdrawalEvent.FindProperty(nameof(WithdrawalEvent.ResponseSessionId))!.IsNullable);
        Assert.Equal(
            "response_session_id",
            withdrawalEvent.FindProperty(nameof(WithdrawalEvent.ResponseSessionId))!.GetColumnName());
        Assert.Equal("jsonb", disclosurePolicy.FindProperty(nameof(DisclosurePolicy.AppliesToDimensions))!.GetColumnType());
        Assert.Equal("consent_document_id", launchSnapshot.FindProperty(nameof(CampaignLaunchSnapshot.ConsentDocumentId))!.GetColumnName());
        Assert.Equal("retention_policy_id", launchSnapshot.FindProperty(nameof(CampaignLaunchSnapshot.RetentionPolicyId))!.GetColumnName());
        Assert.Equal("disclosure_policy_id", launchSnapshot.FindProperty(nameof(CampaignLaunchSnapshot.DisclosurePolicyId))!.GetColumnName());

        Assert.Contains(consentDocument.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ConsentDocument.CampaignSeriesId),
                    nameof(ConsentDocument.Locale),
                    nameof(ConsentDocument.Version)
                ]));
        Assert.Contains(retentionPolicy.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RetentionPolicy.CampaignSeriesId), nameof(RetentionPolicy.Version)]));
        Assert.Contains(withdrawalEvent.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(WithdrawalEvent.TenantId), nameof(WithdrawalEvent.CampaignSeriesId), nameof(WithdrawalEvent.RequestedAt)]));
        Assert.Contains(withdrawalEvent.GetIndexes(), index =>
            index.GetFilter() == "subject_id IS NOT NULL" &&
            index.Properties.Single().Name == nameof(WithdrawalEvent.SubjectId));
        Assert.Contains(withdrawalEvent.GetIndexes(), index =>
            index.GetFilter() == "participant_code_id IS NOT NULL" &&
            index.Properties.Single().Name == nameof(WithdrawalEvent.ParticipantCodeId));
        Assert.Contains(withdrawalEvent.GetIndexes(), index =>
            index.GetFilter() == "response_session_id IS NOT NULL" &&
            index.Properties.Single().Name == nameof(WithdrawalEvent.ResponseSessionId));
        Assert.Equal(
            "token_hash",
            withdrawalRequestToken.FindProperty(nameof(WithdrawalRequestToken.TokenHash))!.GetColumnName());
        Assert.Equal(
            "requested_action",
            withdrawalRequestToken.FindProperty(nameof(WithdrawalRequestToken.RequestedAction))!.GetColumnName());
        Assert.Contains(withdrawalRequestToken.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(WithdrawalRequestToken.TokenHash));
        Assert.Contains(withdrawalRequestToken.GetIndexes(), index =>
            index.Properties.Single().Name == nameof(WithdrawalRequestToken.ResponseSessionId));
        Assert.Contains(withdrawalRequestToken.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_request_token_action");
        Assert.Contains(withdrawalRequestToken.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_request_token_expiry");
        Assert.DoesNotContain(
            withdrawalRequestToken.GetProperties(),
            property => property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(disclosurePolicy.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(DisclosurePolicy.CampaignSeriesId), nameof(DisclosurePolicy.Version)]));
        Assert.Contains(consentRecord.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ConsentRecord.AssignmentId)]));
        Assert.Contains(consentRecord.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ConsentRecord.SubjectId)]));
        Assert.Contains(retentionPolicy.GetCheckConstraints(), check =>
            check.Name == "ck_retention_policy_publication_limits_object");
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_target_shape");
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_status" &&
            check.Sql.Contains("requested", StringComparison.Ordinal) &&
            check.Sql.Contains("denied", StringComparison.Ordinal));
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_target_kind" &&
            check.Sql.Contains("response_session", StringComparison.Ordinal));
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_target_shape" &&
            check.Sql.Contains("response_session_id IS NOT NULL", StringComparison.Ordinal) &&
            check.Sql.Contains("action_after = 'delete'", StringComparison.Ordinal) &&
            check.Sql.Contains("status = 'completed'", StringComparison.Ordinal));
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_counts_non_negative");
        Assert.Contains(withdrawalEvent.GetCheckConstraints(), check =>
            check.Name == "ck_withdrawal_event_metadata_object");
        Assert.DoesNotContain(
            withdrawalEvent.GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("AnswerValue", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(disclosurePolicy.GetCheckConstraints(), check =>
            check.Name == "ck_disclosure_policy_applies_to_dimensions_array");

        Assert.Contains(consentRecord.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ConsentRecord.ConsentDocumentId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ConsentDocument));
        Assert.Contains(consentRecord.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ConsentRecord.SubjectId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Subject));
        Assert.Contains(retentionPolicy.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(RetentionPolicy.CampaignSeriesId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CampaignSeries));
        Assert.Contains(withdrawalEvent.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalEvent.CampaignSeriesId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CampaignSeries));
        Assert.Contains(withdrawalEvent.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalEvent.RetentionPolicyId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(RetentionPolicy));
        Assert.Contains(withdrawalEvent.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalEvent.SubjectId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Subject));
        Assert.Contains(withdrawalEvent.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalEvent.ParticipantCodeId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ParticipantCode));
        Assert.Contains(withdrawalEvent.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalEvent.ResponseSessionId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ResponseSession));
        Assert.Contains(withdrawalRequestToken.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(WithdrawalRequestToken.ResponseSessionId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ResponseSession));
        Assert.Contains(disclosurePolicy.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(DisclosurePolicy.CampaignSeriesId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CampaignSeries));
        Assert.Contains(launchSnapshot.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(CampaignLaunchSnapshot.ConsentDocumentId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ConsentDocument));
        Assert.Contains(launchSnapshot.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(CampaignLaunchSnapshot.RetentionPolicyId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(RetentionPolicy));
        Assert.Contains(launchSnapshot.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(CampaignLaunchSnapshot.DisclosurePolicyId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(DisclosurePolicy));
        Assert.Contains(responseSession.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ResponseSession.ConsentRecordId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(ConsentRecord));
    }

    [Fact]
    public void Export_artifact_model_maps_to_expected_table_indexes_constraints_and_foreign_keys()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=instruments_platform;Username=platform_app;Password=not-used")
            .Options;

        using var db = new ApplicationDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var exportArtifact = model.FindEntityType(typeof(ExportArtifact));

        Assert.NotNull(exportArtifact);
        Assert.Equal("export_artifact", exportArtifact.GetTableName());
        Assert.Equal("target_kind", exportArtifact.FindProperty(nameof(ExportArtifact.TargetKind))!.GetColumnName());
        Assert.True(exportArtifact.FindProperty(nameof(ExportArtifact.CampaignId))!.IsNullable);
        Assert.Equal("started_at", exportArtifact.FindProperty(nameof(ExportArtifact.StartedAt))!.GetColumnName());
        Assert.Equal("failed_at", exportArtifact.FindProperty(nameof(ExportArtifact.FailedAt))!.GetColumnName());
        Assert.Equal("expires_at", exportArtifact.FindProperty(nameof(ExportArtifact.ExpiresAt))!.GetColumnName());
        Assert.Equal("deleted_at", exportArtifact.FindProperty(nameof(ExportArtifact.DeletedAt))!.GetColumnName());
        Assert.Equal("storage_kind", exportArtifact.FindProperty(nameof(ExportArtifact.StorageKind))!.GetColumnName());
        Assert.Equal("storage_key", exportArtifact.FindProperty(nameof(ExportArtifact.StorageKey))!.GetColumnName());
        Assert.False(exportArtifact.FindProperty(nameof(ExportArtifact.StorageKind))!.IsNullable);
        Assert.True(exportArtifact.FindProperty(nameof(ExportArtifact.StorageKey))!.IsNullable);
        Assert.Equal(
            "failure_reason_code",
            exportArtifact.FindProperty(nameof(ExportArtifact.FailureReasonCode))!.GetColumnName());
        Assert.True(exportArtifact.FindProperty(nameof(ExportArtifact.ChecksumSha256))!.IsNullable);
        Assert.True(exportArtifact.FindProperty(nameof(ExportArtifact.Content))!.IsNullable);
        Assert.Equal("jsonb", exportArtifact.FindProperty(nameof(ExportArtifact.MetadataJson))!.GetColumnType());
        Assert.Equal("jsonb", exportArtifact.FindProperty(nameof(ExportArtifact.CodebookJson))!.GetColumnType());

        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_status"
            && check.Sql.Contains("queued", StringComparison.Ordinal)
            && check.Sql.Contains("rendering", StringComparison.Ordinal)
            && check.Sql.Contains("succeeded", StringComparison.Ordinal)
            && check.Sql.Contains("failed", StringComparison.Ordinal)
            && check.Sql.Contains("expired", StringComparison.Ordinal)
            && check.Sql.Contains("deleted", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_format"
            && check.Sql.Contains("csv_codebook", StringComparison.Ordinal)
            && check.Sql.Contains("html", StringComparison.Ordinal)
            && check.Sql.Contains("pdf", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_type"
            && check.Sql.Contains("report_proof_csv_codebook", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_series_response_csv_codebook", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_series_report_html", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_series_report_pdf", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_target_kind"
            && check.Sql.Contains("campaign", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_series", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_target_scope"
            && check.Sql.Contains("target_kind = 'campaign'", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_id IS NOT NULL", StringComparison.Ordinal)
            && check.Sql.Contains("target_kind = 'campaign_series'", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_id IS NULL", StringComparison.Ordinal)
            && check.Sql.Contains("campaign_series_id IS NOT NULL", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_storage_kind"
            && check.Sql.Contains("inline_text", StringComparison.Ordinal)
            && check.Sql.Contains("external_object", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_storage_shape"
            && check.Sql.Contains("storage_kind = 'inline_text'", StringComparison.Ordinal)
            && check.Sql.Contains("storage_kind = 'external_object'", StringComparison.Ordinal)
            && check.Sql.Contains("content IS NULL", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_row_count_non_negative");
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_byte_size_non_negative");
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_checksum_sha256");
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_materialization_shape"
            && check.Sql.Contains("status = 'succeeded'", StringComparison.Ordinal)
            && check.Sql.Contains("checksum_sha256 IS NOT NULL", StringComparison.Ordinal)
            && check.Sql.Contains("storage_kind = 'inline_text'", StringComparison.Ordinal)
            && check.Sql.Contains("storage_kind = 'external_object'", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_lifecycle_shape"
            && check.Sql.Contains("status = 'queued'", StringComparison.Ordinal)
            && check.Sql.Contains("status = 'rendering'", StringComparison.Ordinal)
            && check.Sql.Contains("status = 'failed'", StringComparison.Ordinal)
            && check.Sql.Contains("status = 'expired'", StringComparison.Ordinal)
            && check.Sql.Contains("status = 'deleted'", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_failure_reason_shape"
            && check.Sql.Contains("failure_reason_code", StringComparison.Ordinal));
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_metadata_object");
        Assert.Contains(exportArtifact.GetCheckConstraints(), check =>
            check.Name == "ck_export_artifact_codebook_object");

        Assert.Contains(exportArtifact.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ExportArtifact.TenantId),
                    nameof(ExportArtifact.TargetKind),
                    nameof(ExportArtifact.CampaignId),
                    nameof(ExportArtifact.CreatedAt)
                ]));
        Assert.Contains(exportArtifact.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(ExportArtifact.TenantId),
                    nameof(ExportArtifact.TargetKind),
                    nameof(ExportArtifact.CampaignSeriesId),
                    nameof(ExportArtifact.CreatedAt)
                ]));

        Assert.Contains(exportArtifact.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ExportArtifact.TenantId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Tenant));
        Assert.Contains(exportArtifact.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ExportArtifact.CampaignId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(Campaign));
        Assert.Contains(exportArtifact.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Single().Name == nameof(ExportArtifact.CampaignSeriesId) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CampaignSeries));
    }
}
