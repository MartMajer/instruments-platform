using Microsoft.EntityFrameworkCore;
using Platform.Domain.Auditing;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Instruments;
using Platform.Domain.Outbox;
using Platform.Domain.Operations;
using Platform.Domain.Responses;
using Platform.Domain.Reports;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Domain.Templates;

namespace Platform.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<ExternalAuthIdentity> ExternalAuthIdentities => Set<ExternalAuthIdentity>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<RegistrationIntent> RegistrationIntents => Set<RegistrationIntent>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    public DbSet<OperationalNotification> OperationalNotifications => Set<OperationalNotification>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<SubjectGroup> SubjectGroups => Set<SubjectGroup>();

    public DbSet<SubjectMembership> SubjectMemberships => Set<SubjectMembership>();

    public DbSet<SubjectRelationship> SubjectRelationships => Set<SubjectRelationship>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<InstrumentSubscale> InstrumentSubscales => Set<InstrumentSubscale>();

    public DbSet<InstrumentItem> InstrumentItems => Set<InstrumentItem>();

    public DbSet<InstrumentNorm> InstrumentNorms => Set<InstrumentNorm>();

    public DbSet<InstrumentTranslation> InstrumentTranslations => Set<InstrumentTranslation>();

    public DbSet<SurveyTemplate> SurveyTemplates => Set<SurveyTemplate>();

    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();

    public DbSet<ScoringRule> ScoringRules => Set<ScoringRule>();

    public DbSet<ScoreRun> ScoreRuns => Set<ScoreRun>();

    public DbSet<Score> Scores => Set<Score>();

    public DbSet<ExportArtifact> ExportArtifacts => Set<ExportArtifact>();

    public DbSet<CampaignSeries> CampaignSeries => Set<CampaignSeries>();

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<CampaignLaunchSnapshot> CampaignLaunchSnapshots => Set<CampaignLaunchSnapshot>();

    public DbSet<ConsentDocument> ConsentDocuments => Set<ConsentDocument>();

    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();

    public DbSet<RetentionDueBatch> RetentionDueBatches => Set<RetentionDueBatch>();

    public DbSet<WithdrawalEvent> WithdrawalEvents => Set<WithdrawalEvent>();

    public DbSet<WithdrawalRequestToken> WithdrawalRequestTokens => Set<WithdrawalRequestToken>();

    public DbSet<DisclosurePolicy> DisclosurePolicies => Set<DisclosurePolicy>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<Audience> Audiences => Set<Audience>();

    public DbSet<AudienceMember> AudienceMembers => Set<AudienceMember>();

    public DbSet<RespondentRule> RespondentRules => Set<RespondentRule>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<EmailSuppression> EmailSuppressions => Set<EmailSuppression>();

    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();

    public DbSet<NotificationDeliveryEvent> NotificationDeliveryEvents => Set<NotificationDeliveryEvent>();

    public DbSet<ParticipantCode> ParticipantCodes => Set<ParticipantCode>();

    public DbSet<ResponseSession> ResponseSessions => Set<ResponseSession>();

    public DbSet<Answer> Answers => Set<Answer>();

    public DbSet<TemplateSection> TemplateSections => Set<TemplateSection>();

    public DbSet<QuestionScale> QuestionScales => Set<QuestionScale>();

    public DbSet<TemplateQuestion> TemplateQuestions => Set<TemplateQuestion>();

    public DbSet<ChoiceOption> ChoiceOptions => Set<ChoiceOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<WorkerHeartbeat>(builder =>
        {
            builder.ToTable("worker_heartbeat");
            builder.HasKey(heartbeat => heartbeat.Id).HasName("pk_worker_heartbeat");

            builder.Property(heartbeat => heartbeat.Id).HasColumnName("id");
            builder.Property(heartbeat => heartbeat.WorkerName)
                .HasColumnName("worker_name")
                .HasMaxLength(WorkerHeartbeat.WorkerNameMaxLength)
                .IsRequired();
            builder.Property(heartbeat => heartbeat.InstanceId)
                .HasColumnName("instance_id")
                .HasMaxLength(WorkerHeartbeat.InstanceIdMaxLength)
                .IsRequired();
            builder.Property(heartbeat => heartbeat.StartedAt).HasColumnName("started_at").IsRequired();
            builder.Property(heartbeat => heartbeat.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
            builder.Property(heartbeat => heartbeat.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(heartbeat => heartbeat.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(heartbeat => new { heartbeat.WorkerName, heartbeat.InstanceId })
                .HasDatabaseName("ix_worker_heartbeat_worker_name_instance_id")
                .IsUnique();
            builder.HasIndex(heartbeat => heartbeat.LastSeenAt)
                .HasDatabaseName("ix_worker_heartbeat_last_seen_at");
        });

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenant");
            builder.HasKey(tenant => tenant.Id).HasName("pk_tenant");

            builder.Property(tenant => tenant.Id).HasColumnName("id");
            builder.Property(tenant => tenant.Slug).HasColumnName("slug").HasMaxLength(128).IsRequired();
            builder.Property(tenant => tenant.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(tenant => tenant.Region).HasColumnName("region").HasMaxLength(32).IsRequired();
            builder.Property(tenant => tenant.DefaultLocale).HasColumnName("default_locale").HasMaxLength(16).IsRequired();
            builder.Property(tenant => tenant.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            builder.Property(tenant => tenant.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(tenant => tenant.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(tenant => tenant.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(tenant => tenant.Slug).HasDatabaseName("ix_tenant_slug").IsUnique();
        });

        modelBuilder.Entity<UserAccount>(builder =>
        {
            builder.ToTable("user_account");
            builder.HasKey(user => user.Id).HasName("pk_user_account");
            builder.HasAlternateKey(user => new { user.Id, user.TenantId })
                .HasName("ak_user_account_id_tenant_id");

            builder.Property(user => user.Id).HasColumnName("id");
            builder.Property(user => user.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(user => user.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
            builder.Property(user => user.PasswordHash).HasColumnName("password_hash");
            builder.Property(user => user.MfaSecret).HasColumnName("mfa_secret");
            builder.Property(user => user.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(user => user.EmailVerifiedAt).HasColumnName("email_verified_at");
            builder.Property(user => user.LastLoginAt).HasColumnName("last_login_at");
            builder.Property(user => user.FailedLoginAttempts).HasColumnName("failed_login_attempts").IsRequired();
            builder.Property(user => user.LockedUntil).HasColumnName("locked_until");
            builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(user => user.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(user => user.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(user => new { user.TenantId, user.Email })
                .HasDatabaseName("ix_user_account_tenant_id_email")
                .IsUnique();

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(user => user.TenantId)
                .HasConstraintName("fk_user_account_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalAuthIdentity>(builder =>
        {
            builder.ToTable("external_auth_identity");
            builder.HasKey(identity => identity.Id).HasName("pk_external_auth_identity");
            builder.HasAlternateKey(identity => new { identity.Id, identity.TenantId })
                .HasName("ak_external_auth_identity_id_tenant_id");

            builder.Property(identity => identity.Id).HasColumnName("id");
            builder.Property(identity => identity.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(identity => identity.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(identity => identity.Provider)
                .HasColumnName("provider")
                .HasMaxLength(ExternalAuthIdentity.ProviderMaxLength)
                .IsRequired();
            builder.Property(identity => identity.ProviderSubjectHash)
                .HasColumnName("provider_subject_hash")
                .HasMaxLength(ExternalAuthIdentity.ProviderSubjectHashMaxLength)
                .IsRequired();
            builder.Property(identity => identity.EmailAtBinding)
                .HasColumnName("email_at_binding")
                .HasColumnType("citext")
                .HasMaxLength(ExternalAuthIdentity.EmailAtBindingMaxLength)
                .IsRequired();
            builder.Property(identity => identity.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(identity => identity.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
            builder.Property(identity => identity.EmailVerifiedAt)
                .HasColumnName("email_verified_at")
                .HasColumnType("timestamp with time zone");
            builder.Property(identity => identity.EmailVerificationGraceUsedAt)
                .HasColumnName("email_verification_grace_used_at")
                .HasColumnType("timestamp with time zone");
            builder.Property(identity => identity.DisabledAt).HasColumnName("disabled_at");

            builder.HasIndex(identity => new { identity.TenantId, identity.Provider, identity.ProviderSubjectHash })
                .HasDatabaseName("ix_external_auth_identity_tenant_id_provider_provider_subject_hash")
                .IsUnique();
            builder.HasIndex(identity => new { identity.TenantId, identity.UserId, identity.Provider })
                .HasDatabaseName("ix_external_auth_identity_tenant_id_user_id_provider")
                .IsUnique();
            builder.HasIndex(identity => new { identity.TenantId, identity.UserId })
                .HasDatabaseName("ix_external_auth_identity_tenant_id_user_id");
            builder.HasIndex(identity => new { identity.UserId, identity.TenantId })
                .HasDatabaseName("ix_external_auth_identity_user_id_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(identity => identity.TenantId)
                .HasConstraintName("fk_external_auth_identity_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(identity => new { identity.UserId, identity.TenantId })
                .HasPrincipalKey(user => new { user.Id, user.TenantId })
                .HasConstraintName("fk_external_auth_identity_user_account_user_id_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuthSession>(builder =>
        {
            builder.ToTable("auth_session");
            builder.HasKey(session => session.Id).HasName("pk_auth_session");

            builder.Property(session => session.Id).HasColumnName("id");
            builder.Property(session => session.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(session => session.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(session => session.ExternalAuthIdentityId)
                .HasColumnName("external_auth_identity_id")
                .IsRequired();
            builder.Property(session => session.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(session => session.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(session => session.RevokedAt).HasColumnName("revoked_at");
            builder.Property(session => session.RevokedReason)
                .HasColumnName("revoked_reason")
                .HasMaxLength(AuthSession.RevokedReasonMaxLength);

            builder.HasIndex(session => new { session.TenantId, session.UserId })
                .HasDatabaseName("ix_auth_session_tenant_id_user_id");
            builder.HasIndex(session => new { session.TenantId, session.ExpiresAt })
                .HasDatabaseName("ix_auth_session_tenant_id_expires_at");
            builder.HasIndex(session => new { session.ExternalAuthIdentityId, session.TenantId })
                .HasDatabaseName("ix_auth_session_external_auth_identity_id_tenant_id");
            builder.HasIndex(session => new { session.UserId, session.TenantId })
                .HasDatabaseName("ix_auth_session_user_id_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(session => session.TenantId)
                .HasConstraintName("fk_auth_session_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(session => new { session.UserId, session.TenantId })
                .HasPrincipalKey(user => new { user.Id, user.TenantId })
                .HasConstraintName("fk_auth_session_user_account_user_id_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ExternalAuthIdentity>()
                .WithMany()
                .HasForeignKey(session => new { session.ExternalAuthIdentityId, session.TenantId })
                .HasPrincipalKey(identity => new { identity.Id, identity.TenantId })
                .HasConstraintName("fk_auth_session_external_auth_identity_external_auth_identity_id_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistrationIntent>(builder =>
        {
            builder.ToTable("registration_intent", table =>
            {
                table.HasCheckConstraint(
                    "ck_registration_intent_status",
                    "status IN ('pending','consumed')");
                table.HasCheckConstraint(
                    "ck_registration_intent_expiry",
                    "expires_at > created_at");
                table.HasCheckConstraint(
                    "ck_registration_intent_consumed_shape",
                    "(status = 'pending' AND consumed_at IS NULL AND consumed_tenant_id IS NULL) OR (status = 'consumed' AND consumed_at IS NOT NULL AND consumed_tenant_id IS NOT NULL)");
            });
            builder.HasKey(intent => intent.Id).HasName("pk_registration_intent");

            builder.Property(intent => intent.Id).HasColumnName("id");
            builder.Property(intent => intent.RegistrationTokenHash)
                .HasColumnName("registration_token_hash")
                .HasMaxLength(RegistrationIntent.TokenHashMaxLength)
                .IsRequired();
            builder.Property(intent => intent.Email)
                .HasColumnName("email")
                .HasColumnType("citext")
                .HasMaxLength(RegistrationIntent.EmailMaxLength)
                .IsRequired();
            builder.Property(intent => intent.OrganizationName)
                .HasColumnName("organization_name")
                .HasMaxLength(RegistrationIntent.OrganizationNameMaxLength)
                .IsRequired();
            builder.Property(intent => intent.Slug)
                .HasColumnName("slug")
                .HasMaxLength(RegistrationIntent.SlugMaxLength)
                .IsRequired();
            builder.Property(intent => intent.Status)
                .HasColumnName("status")
                .HasMaxLength(32)
                .IsRequired();
            builder.Property(intent => intent.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(intent => intent.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(intent => intent.ConsumedAt).HasColumnName("consumed_at");
            builder.Property(intent => intent.ConsumedTenantId).HasColumnName("consumed_tenant_id");

            builder.HasIndex(intent => intent.RegistrationTokenHash)
                .HasDatabaseName("ix_registration_intent_registration_token_hash")
                .IsUnique();
            builder.HasIndex(intent => intent.Email)
                .HasDatabaseName("ix_registration_intent_email");
            builder.HasIndex(intent => new { intent.Status, intent.ExpiresAt })
                .HasDatabaseName("ix_registration_intent_status_expires_at");
            builder.HasIndex(intent => intent.ConsumedTenantId)
                .HasDatabaseName("ix_registration_intent_consumed_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(intent => intent.ConsumedTenantId)
                .HasConstraintName("fk_registration_intent_tenant_consumed_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("role");
            builder.HasKey(role => role.Id).HasName("pk_role");

            builder.Property(role => role.Id).HasColumnName("id");
            builder.Property(role => role.TenantId).HasColumnName("tenant_id");
            builder.Property(role => role.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            builder.Property(role => role.Name).HasColumnName("name").HasMaxLength(256).IsRequired();

            builder.HasIndex(role => new { role.TenantId, role.Code })
                .HasDatabaseName("ix_role_tenant_id_code")
                .HasFilter("tenant_id IS NOT NULL")
                .IsUnique();

            builder.HasIndex(role => role.Code)
                .HasDatabaseName("ix_role_global_code")
                .HasFilter("tenant_id IS NULL")
                .IsUnique();

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(role => role.TenantId)
                .HasConstraintName("fk_role_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("permission");
            builder.HasKey(permission => permission.Id).HasName("pk_permission");

            builder.Property(permission => permission.Id).HasColumnName("id");
            builder.Property(permission => permission.Code).HasColumnName("code").HasMaxLength(128).IsRequired();

            builder.HasIndex(permission => permission.Code)
                .HasDatabaseName("ix_permission_code")
                .IsUnique();
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("role_permission");
            builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
                .HasName("pk_role_permission");

            builder.Property(rolePermission => rolePermission.RoleId).HasColumnName("role_id");
            builder.Property(rolePermission => rolePermission.PermissionId).HasColumnName("permission_id");

            builder.HasIndex(rolePermission => rolePermission.PermissionId)
                .HasDatabaseName("ix_role_permission_permission_id");

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .HasConstraintName("fk_role_permission_role_role_id")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .HasConstraintName("fk_role_permission_permission_permission_id")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleAssignment>(builder =>
        {
            builder.ToTable("role_assignment", table =>
            {
                table.HasCheckConstraint(
                    "ck_role_assignment_scope",
                    "(scope_type = 'tenant' AND scope_id IS NULL)\n"
                    + "OR (scope_type IN ('workspace', 'campaign', 'campaign_series') AND scope_id IS NOT NULL)");
            });
            builder.HasKey(roleAssignment => roleAssignment.Id).HasName("pk_role_assignment");

            builder.Property(roleAssignment => roleAssignment.Id).HasColumnName("id");
            builder.Property(roleAssignment => roleAssignment.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(roleAssignment => roleAssignment.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(roleAssignment => roleAssignment.RoleId).HasColumnName("role_id").IsRequired();
            builder.Property(roleAssignment => roleAssignment.ScopeType).HasColumnName("scope_type").HasMaxLength(64).IsRequired();
            builder.Property(roleAssignment => roleAssignment.ScopeId).HasColumnName("scope_id");
            builder.Property(roleAssignment => roleAssignment.GrantedAt).HasColumnName("granted_at").IsRequired();
            builder.Property(roleAssignment => roleAssignment.GrantedBy).HasColumnName("granted_by");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(roleAssignment => roleAssignment.TenantId)
                .HasConstraintName("fk_role_assignment_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(roleAssignment => roleAssignment.UserId)
                .HasConstraintName("fk_role_assignment_user_account_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey(roleAssignment => roleAssignment.RoleId)
                .HasConstraintName("fk_role_assignment_role_role_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(roleAssignment => roleAssignment.GrantedBy)
                .HasConstraintName("fk_role_assignment_user_account_granted_by")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(roleAssignment => roleAssignment.TenantId)
                .HasDatabaseName("ix_role_assignment_tenant_id");
            builder.HasIndex(roleAssignment => roleAssignment.UserId)
                .HasDatabaseName("ix_role_assignment_user_id");
            builder.HasIndex(roleAssignment => roleAssignment.RoleId)
                .HasDatabaseName("ix_role_assignment_role_id");
            builder.HasIndex(roleAssignment => roleAssignment.GrantedBy)
                .HasDatabaseName("ix_role_assignment_granted_by");
        });

        modelBuilder.Entity<AuditEvent>(builder =>
        {
            builder.ToTable("audit_event");
            builder.HasKey(auditEvent => new { auditEvent.Id, auditEvent.OccurredAt })
                .HasName("pk_audit_event");

            builder.Property(auditEvent => auditEvent.Id).HasColumnName("id");
            builder.Property(auditEvent => auditEvent.OccurredAt).HasColumnName("occurred_at").IsRequired();
            builder.Property(auditEvent => auditEvent.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(auditEvent => auditEvent.ActorId).HasColumnName("actor_id");
            builder.Property(auditEvent => auditEvent.ActorType).HasColumnName("actor_type").HasMaxLength(32).IsRequired();
            builder.Property(auditEvent => auditEvent.CorrelationId).HasColumnName("correlation_id");
            builder.Property(auditEvent => auditEvent.EntityType).HasColumnName("entity_type").HasMaxLength(256).IsRequired();
            builder.Property(auditEvent => auditEvent.EntityId).HasColumnName("entity_id").IsRequired();
            builder.Property(auditEvent => auditEvent.ChangeKind).HasColumnName("change_kind").HasMaxLength(32).IsRequired();
            builder.Property(auditEvent => auditEvent.Before).HasColumnName("before").HasColumnType("jsonb");
            builder.Property(auditEvent => auditEvent.After).HasColumnName("after").HasColumnType("jsonb");
            builder.Property(auditEvent => auditEvent.Reason).HasColumnName("reason");

            builder.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.OccurredAt })
                .HasDatabaseName("ix_audit_event_tenant_id_occurred_at");
            builder.HasIndex(auditEvent => new { auditEvent.EntityType, auditEvent.EntityId, auditEvent.OccurredAt })
                .HasDatabaseName("ix_audit_event_entity_type_entity_id_occurred_at");
        });

        modelBuilder.Entity<OutboxEvent>(builder =>
        {
            builder.ToTable("outbox_event");
            builder.HasKey(outboxEvent => outboxEvent.Id).HasName("pk_outbox_event");

            builder.Property(outboxEvent => outboxEvent.Id).HasColumnName("id");
            builder.Property(outboxEvent => outboxEvent.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(outboxEvent => outboxEvent.AggregateId).HasColumnName("aggregate_id").IsRequired();
            builder.Property(outboxEvent => outboxEvent.AggregateType).HasColumnName("aggregate_type").HasMaxLength(128).IsRequired();
            builder.Property(outboxEvent => outboxEvent.EventType).HasColumnName("event_type").HasMaxLength(256).IsRequired();
            builder.Property(outboxEvent => outboxEvent.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            builder.Property(outboxEvent => outboxEvent.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(outboxEvent => outboxEvent.PublishedAt).HasColumnName("published_at");
            builder.Property(outboxEvent => outboxEvent.CorrelationId).HasColumnName("correlation_id");
            builder.Property(outboxEvent => outboxEvent.RetryCount).HasColumnName("retry_count").IsRequired();
            builder.Property(outboxEvent => outboxEvent.LastError).HasColumnName("last_error");
            builder.Property(outboxEvent => outboxEvent.NextRetryAt).HasColumnName("next_retry_at");

            builder.HasIndex(outboxEvent => outboxEvent.NextRetryAt)
                .HasDatabaseName("ix_outbox_event_unpublished_next_retry_at")
                .HasFilter("published_at IS NULL");
            builder.HasIndex(outboxEvent => new { outboxEvent.AggregateId, outboxEvent.CreatedAt })
                .HasDatabaseName("ix_outbox_event_aggregate_id_created_at");
            builder.HasIndex(outboxEvent => new { outboxEvent.TenantId, outboxEvent.CreatedAt })
                .HasDatabaseName("ix_outbox_event_tenant_id_created_at");
        });

        modelBuilder.Entity<Subject>(builder =>
        {
            builder.ToTable("subject");
            builder.HasKey(subject => subject.Id).HasName("pk_subject");

            builder.Property(subject => subject.Id).HasColumnName("id");
            builder.Property(subject => subject.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(subject => subject.WorkspaceId).HasColumnName("workspace_id");
            builder.Property(subject => subject.ExternalId).HasColumnName("external_id").HasMaxLength(256);
            builder.Property(subject => subject.UserAccountId).HasColumnName("user_account_id");
            builder.Property(subject => subject.Email).HasColumnName("email").HasColumnType("citext");
            builder.Property(subject => subject.DisplayName).HasColumnName("display_name").HasMaxLength(256);
            builder.Property(subject => subject.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(subject => subject.Attributes)
                .HasColumnName("attributes")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(subject => subject.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(subject => subject.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(subject => subject.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(subject => new { subject.TenantId, subject.ExternalId })
                .HasDatabaseName("ix_subject_tenant_id_external_id")
                .HasFilter("external_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(subject => new { subject.TenantId, subject.Email })
                .HasDatabaseName("ix_subject_tenant_id_email")
                .HasFilter("email IS NOT NULL")
                .IsUnique();
            builder.HasIndex(subject => subject.Attributes)
                .HasDatabaseName("ix_subject_attributes_gin")
                .HasMethod("gin");
            builder.HasIndex(subject => subject.TenantId)
                .HasDatabaseName("ix_subject_tenant_id");
            builder.HasIndex(subject => subject.UserAccountId)
                .HasDatabaseName("ix_subject_user_account_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(subject => subject.TenantId)
                .HasConstraintName("fk_subject_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(subject => subject.UserAccountId)
                .HasConstraintName("fk_subject_user_account_user_account_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubjectGroup>(builder =>
        {
            builder.ToTable("subject_group");
            builder.HasKey(group => group.Id).HasName("pk_subject_group");
            builder.HasAlternateKey(group => new { group.Id, group.TenantId })
                .HasName("ak_subject_group_id_tenant_id");

            builder.Property(group => group.Id).HasColumnName("id");
            builder.Property(group => group.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(group => group.WorkspaceId).HasColumnName("workspace_id");
            builder.Property(group => group.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
            builder.Property(group => group.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(group => group.ParentGroupId).HasColumnName("parent_group_id");
            builder.Property(group => group.Attributes)
                .HasColumnName("attributes")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(group => group.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(group => group.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(group => group.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(group => group.TenantId)
                .HasDatabaseName("ix_subject_group_tenant_id");
            builder.HasIndex(group => new { group.ParentGroupId, group.TenantId })
                .HasDatabaseName("ix_subject_group_parent_group_id_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(group => group.TenantId)
                .HasConstraintName("fk_subject_group_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<SubjectGroup>()
                .WithMany()
                .HasForeignKey(group => new { group.ParentGroupId, group.TenantId })
                .HasPrincipalKey(group => new { group.Id, group.TenantId })
                .HasConstraintName("fk_subject_group_subject_group_parent_group_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubjectMembership>(builder =>
        {
            builder.ToTable("subject_membership", table =>
            {
                table.HasCheckConstraint(
                    "ck_subject_membership_valid_range",
                    "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
            });
            builder.HasKey(membership => new { membership.SubjectId, membership.GroupId })
                .HasName("pk_subject_membership");

            builder.Property(membership => membership.SubjectId).HasColumnName("subject_id");
            builder.Property(membership => membership.GroupId).HasColumnName("group_id");
            builder.Property(membership => membership.RoleInGroup).HasColumnName("role_in_group").HasMaxLength(64);
            builder.Property(membership => membership.ValidFrom).HasColumnName("valid_from");
            builder.Property(membership => membership.ValidTo).HasColumnName("valid_to");

            builder.HasIndex(membership => membership.GroupId)
                .HasDatabaseName("ix_subject_membership_group_id");

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(membership => membership.SubjectId)
                .HasConstraintName("fk_subject_membership_subject_subject_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<SubjectGroup>()
                .WithMany()
                .HasForeignKey(membership => membership.GroupId)
                .HasConstraintName("fk_subject_membership_subject_group_group_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubjectRelationship>(builder =>
        {
            builder.ToTable("subject_relationship", table =>
            {
                table.HasCheckConstraint(
                    "ck_subject_relationship_not_self_unless_self_type",
                    "(subject_id <> related_subject_id AND rel_type <> 'self') OR (subject_id = related_subject_id AND rel_type = 'self')");
                table.HasCheckConstraint(
                    "ck_subject_relationship_valid_range",
                    "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
            });
            builder.HasKey(relationship => relationship.Id).HasName("pk_subject_relationship");

            builder.Property(relationship => relationship.Id).HasColumnName("id");
            builder.Property(relationship => relationship.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(relationship => relationship.SubjectId).HasColumnName("subject_id").IsRequired();
            builder.Property(relationship => relationship.RelatedSubjectId).HasColumnName("related_subject_id").IsRequired();
            builder.Property(relationship => relationship.RelationshipType)
                .HasColumnName("rel_type")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(relationship => relationship.ValidFrom).HasColumnName("valid_from");
            builder.Property(relationship => relationship.ValidTo).HasColumnName("valid_to");

            builder.HasIndex(relationship => new { relationship.SubjectId, relationship.RelationshipType })
                .HasDatabaseName("ix_subject_relationship_subject_id_rel_type");
            builder.HasIndex(relationship => new { relationship.RelatedSubjectId, relationship.RelationshipType })
                .HasDatabaseName("ix_subject_relationship_related_subject_id_rel_type");
            builder.HasIndex(relationship => relationship.TenantId)
                .HasDatabaseName("ix_subject_relationship_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(relationship => relationship.TenantId)
                .HasConstraintName("fk_subject_relationship_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(relationship => relationship.SubjectId)
                .HasConstraintName("fk_subject_relationship_subject_subject_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(relationship => relationship.RelatedSubjectId)
                .HasConstraintName("fk_subject_relationship_subject_related_subject_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Instrument>(builder =>
        {
            builder.ToTable("instrument", table =>
            {
                table.HasCheckConstraint(
                    "ck_instrument_domain",
                    "domain IN ('psychometric','ergonomic','medical','educational','regulatory','other')");
                table.HasCheckConstraint(
                    "ck_instrument_license_type",
                    "license_type IN ('free','free_academic','paid','unknown')");
                table.HasCheckConstraint(
                    "ck_instrument_validity_status",
                    "validity_status IN ('canonical','derived','private_import','draft','retired')");
                table.HasCheckConstraint(
                    "ck_instrument_rights_scope",
                    "rights_scope IN ('platform_granted','tenant_provided')");
                table.HasCheckConstraint(
                    "ck_instrument_rights_status",
                    "rights_status IN ('verified','attested_by_tenant','unverified_internal_demo','expired')");
                table.HasCheckConstraint(
                    "ck_instrument_validity_label",
                    "validity_label IN ('official','tenant_provided','adapted','experimental','rights_unverified')");
                table.HasCheckConstraint(
                    "ck_instrument_global_tenant_shape",
                    "(is_global = TRUE AND tenant_id IS NULL) OR (is_global = FALSE AND tenant_id IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_instrument_derived_parent_shape",
                    "validity_status <> 'derived' OR (tenant_id IS NOT NULL AND parent_instrument_id IS NOT NULL AND is_global = FALSE)");
                table.HasCheckConstraint(
                    "ck_instrument_canonical_parent_shape",
                    "validity_status <> 'canonical' OR parent_instrument_id IS NULL");
                table.HasCheckConstraint(
                    "ck_instrument_private_import_shape",
                    "validity_status <> 'private_import' OR (tenant_id IS NOT NULL AND parent_instrument_id IS NULL AND is_global = FALSE)");
            });
            builder.HasKey(instrument => instrument.Id).HasName("pk_instrument");

            builder.Property(instrument => instrument.Id).HasColumnName("id");
            builder.Property(instrument => instrument.TenantId).HasColumnName("tenant_id");
            builder.Property(instrument => instrument.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            builder.Property(instrument => instrument.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.FullName).HasColumnName("full_name").HasMaxLength(512).IsRequired();
            builder.Property(instrument => instrument.Domain).HasColumnName("domain").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.ConstructCategory).HasColumnName("construct_category").HasMaxLength(128);
            builder.Property(instrument => instrument.Developers)
                .HasColumnName("developers")
                .HasColumnType("text[]")
                .IsRequired();
            builder.Property(instrument => instrument.YearFirstPublished).HasColumnName("year_first_published");
            builder.Property(instrument => instrument.CitationApa).HasColumnName("citation_apa").IsRequired();
            builder.Property(instrument => instrument.Doi).HasColumnName("doi");
            builder.Property(instrument => instrument.LicenseType).HasColumnName("license_type").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.LicenseTermsUrl).HasColumnName("license_terms_url");
            builder.Property(instrument => instrument.LicenseExpiresAt).HasColumnName("license_expires_at");
            builder.Property(instrument => instrument.RightsScope).HasColumnName("rights_scope").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.RightsStatus).HasColumnName("rights_status").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.ValidityLabel).HasColumnName("validity_label").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.ProvenanceNote).HasColumnName("provenance_note");
            builder.Property(instrument => instrument.Vendor).HasColumnName("vendor").HasMaxLength(256);
            builder.Property(instrument => instrument.IsLocked).HasColumnName("is_locked").IsRequired();
            builder.Property(instrument => instrument.IsGlobal).HasColumnName("is_global").IsRequired();
            builder.Property(instrument => instrument.ValidityStatus).HasColumnName("validity_status").HasMaxLength(64).IsRequired();
            builder.Property(instrument => instrument.ParentInstrumentId).HasColumnName("parent_instrument_id");
            builder.Property(instrument => instrument.CanonicalTemplateVersionId).HasColumnName("canonical_template_version_id");
            builder.Property(instrument => instrument.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(instrument => instrument.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(instrument => new { instrument.Code, instrument.Version })
                .HasDatabaseName("ix_instrument_global_code_version")
                .HasFilter("tenant_id IS NULL")
                .IsUnique();
            builder.HasIndex(instrument => new { instrument.TenantId, instrument.Code, instrument.Version })
                .HasDatabaseName("ix_instrument_tenant_id_code_version")
                .HasFilter("tenant_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(instrument => instrument.TenantId)
                .HasDatabaseName("ix_instrument_tenant_id");
            builder.HasIndex(instrument => instrument.ParentInstrumentId)
                .HasDatabaseName("ix_instrument_parent_instrument_id");
            builder.HasIndex(instrument => instrument.CanonicalTemplateVersionId)
                .HasDatabaseName("ix_instrument_canonical_template_version_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(instrument => instrument.TenantId)
                .HasConstraintName("fk_instrument_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(instrument => instrument.ParentInstrumentId)
                .HasConstraintName("fk_instrument_instrument_parent_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(instrument => instrument.CanonicalTemplateVersionId)
                .HasConstraintName("fk_instrument_template_version_canonical_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstrumentSubscale>(builder =>
        {
            builder.ToTable("instrument_subscale", table =>
            {
                table.HasCheckConstraint(
                    "ck_instrument_subscale_item_count_positive",
                    "item_count > 0");
                table.HasCheckConstraint(
                    "ck_instrument_subscale_reliability_alpha_range",
                    "reliability_alpha_published IS NULL OR (reliability_alpha_published >= 0 AND reliability_alpha_published <= 1)");
                table.HasCheckConstraint(
                    "ck_instrument_subscale_scoring_method",
                    "scoring_method IN ('mean','sum','weighted')");
            });
            builder.HasKey(subscale => subscale.Id).HasName("pk_instrument_subscale");

            builder.Property(subscale => subscale.Id).HasColumnName("id");
            builder.Property(subscale => subscale.InstrumentId).HasColumnName("instrument_id").IsRequired();
            builder.Property(subscale => subscale.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            builder.Property(subscale => subscale.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(subscale => subscale.ItemCount).HasColumnName("item_count").IsRequired();
            builder.Property(subscale => subscale.ReliabilityAlphaPublished)
                .HasColumnName("reliability_alpha_published")
                .HasPrecision(4, 3);
            builder.Property(subscale => subscale.ScoringMethod)
                .HasColumnName("scoring_method")
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(subscale => new { subscale.InstrumentId, subscale.Code })
                .HasDatabaseName("ix_instrument_subscale_instrument_id_code")
                .IsUnique();

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(subscale => subscale.InstrumentId)
                .HasConstraintName("fk_instrument_subscale_instrument_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstrumentItem>(builder =>
        {
            builder.ToTable("instrument_item", table =>
            {
                table.HasCheckConstraint(
                    "ck_instrument_item_ordinal_positive",
                    "ordinal > 0");
            });
            builder.HasKey(item => item.Id).HasName("pk_instrument_item");

            builder.Property(item => item.Id).HasColumnName("id");
            builder.Property(item => item.InstrumentId).HasColumnName("instrument_id").IsRequired();
            builder.Property(item => item.Ordinal).HasColumnName("ordinal").IsRequired();
            builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            builder.Property(item => item.SubscaleCode).HasColumnName("subscale_code").HasMaxLength(64).IsRequired();
            builder.Property(item => item.ReverseCoded).HasColumnName("reverse_coded").IsRequired();
            builder.Property(item => item.QuestionId).HasColumnName("question_id");

            builder.HasIndex(item => new { item.InstrumentId, item.Ordinal })
                .HasDatabaseName("ix_instrument_item_instrument_id_ordinal")
                .IsUnique();
            builder.HasIndex(item => new { item.InstrumentId, item.Code })
                .HasDatabaseName("ix_instrument_item_instrument_id_code")
                .IsUnique();
            builder.HasIndex(item => item.QuestionId)
                .HasDatabaseName("ix_instrument_item_question_id");

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(item => item.InstrumentId)
                .HasConstraintName("fk_instrument_item_instrument_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateQuestion>()
                .WithMany()
                .HasForeignKey(item => item.QuestionId)
                .HasConstraintName("fk_instrument_item_question_question_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstrumentNorm>(builder =>
        {
            builder.ToTable("instrument_norm", table =>
            {
                table.HasCheckConstraint(
                    "ck_instrument_norm_sample_size_positive",
                    "sample_size > 0");
                table.HasCheckConstraint(
                    "ck_instrument_norm_type",
                    "norm_type IN ('published_instrument','platform_benchmark','tenant_benchmark')");
            });
            builder.HasKey(norm => norm.Id).HasName("pk_instrument_norm");

            builder.Property(norm => norm.Id).HasColumnName("id");
            builder.Property(norm => norm.InstrumentId).HasColumnName("instrument_id").IsRequired();
            builder.Property(norm => norm.SubscaleCode).HasColumnName("subscale_code").HasMaxLength(64).IsRequired();
            builder.Property(norm => norm.NormType).HasColumnName("norm_type").HasMaxLength(64).IsRequired();
            builder.Property(norm => norm.Population).HasColumnName("population").HasMaxLength(256).IsRequired();
            builder.Property(norm => norm.SampleSize).HasColumnName("sample_size").IsRequired();
            builder.Property(norm => norm.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(norm => norm.Mean).HasColumnName("mean").HasPrecision(8, 3);
            builder.Property(norm => norm.StandardDeviation).HasColumnName("sd").HasPrecision(8, 3);
            builder.Property(norm => norm.Percentiles)
                .HasColumnName("percentiles")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(norm => norm.SourceCitation).HasColumnName("source_citation");
            builder.Property(norm => norm.SourceYear).HasColumnName("source_year");

            builder.HasIndex(norm => new { norm.InstrumentId, norm.SubscaleCode, norm.Locale, norm.NormType })
                .HasDatabaseName("ix_instrument_norm_lookup");

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(norm => norm.InstrumentId)
                .HasConstraintName("fk_instrument_norm_instrument_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SurveyTemplate>(builder =>
        {
            builder.ToTable("survey_template");
            builder.HasKey(template => template.Id).HasName("pk_survey_template");

            builder.Property(template => template.Id).HasColumnName("id");
            builder.Property(template => template.TenantId).HasColumnName("tenant_id");
            builder.Property(template => template.WorkspaceId).HasColumnName("workspace_id");
            builder.Property(template => template.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(template => template.Description).HasColumnName("description");
            builder.Property(template => template.CreatedBy).HasColumnName("created_by");
            builder.Property(template => template.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(template => template.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(template => template.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(template => template.TenantId)
                .HasDatabaseName("ix_survey_template_tenant_id");
            builder.HasIndex(template => new { template.TenantId, template.Name })
                .HasDatabaseName("ix_survey_template_tenant_id_name");
            builder.HasIndex(template => template.CreatedBy)
                .HasDatabaseName("ix_survey_template_created_by");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(template => template.TenantId)
                .HasConstraintName("fk_survey_template_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(template => template.CreatedBy)
                .HasConstraintName("fk_survey_template_user_account_created_by")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateVersion>(builder =>
        {
            builder.ToTable("template_version", table =>
            {
                table.HasCheckConstraint(
                    "ck_template_version_status",
                    "status IN ('draft','published','retired')");
                table.HasCheckConstraint(
                    "ck_template_version_global_locked",
                    "is_global = FALSE OR is_locked = TRUE");
                table.HasCheckConstraint(
                    "ck_template_version_publish_shape",
                    "(status = 'published' AND published_at IS NOT NULL) OR (status <> 'published')");
            });
            builder.HasKey(version => version.Id).HasName("pk_template_version");

            builder.Property(version => version.Id).HasColumnName("id");
            builder.Property(version => version.TemplateId).HasColumnName("template_id").IsRequired();
            builder.Property(version => version.InstrumentId).HasColumnName("instrument_id");
            builder.Property(version => version.Semver).HasColumnName("semver").HasMaxLength(64).IsRequired();
            builder.Property(version => version.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(version => version.PublishedAt).HasColumnName("published_at");
            builder.Property(version => version.PublishedBy).HasColumnName("published_by");
            builder.Property(version => version.IsLocked).HasColumnName("is_locked").IsRequired();
            builder.Property(version => version.IsGlobal).HasColumnName("is_global").IsRequired();
            builder.Property(version => version.DefaultLocale).HasColumnName("default_locale").HasMaxLength(16).IsRequired();
            builder.Property(version => version.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(version => new { version.TemplateId, version.Semver })
                .HasDatabaseName("ix_template_version_template_id_semver")
                .IsUnique();
            builder.HasIndex(version => version.InstrumentId)
                .HasDatabaseName("ix_template_version_instrument_id");
            builder.HasIndex(version => version.PublishedBy)
                .HasDatabaseName("ix_template_version_published_by");

            builder.HasOne<SurveyTemplate>()
                .WithMany()
                .HasForeignKey(version => version.TemplateId)
                .HasConstraintName("fk_template_version_survey_template_template_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(version => version.InstrumentId)
                .HasConstraintName("fk_template_version_instrument_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(version => version.PublishedBy)
                .HasConstraintName("fk_template_version_user_account_published_by")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScoringRule>(builder =>
        {
            builder.ToTable("scoring_rule", table =>
            {
                table.HasCheckConstraint(
                    "ck_scoring_rule_status",
                    "status IN ('draft','published','retired')");
                table.HasCheckConstraint(
                    "ck_scoring_rule_document_hash",
                    "document_hash ~ '^[a-f0-9]{64}$'");
                table.HasCheckConstraint(
                    "ck_scoring_rule_document_object",
                    "jsonb_typeof(document) = 'object'");
                table.HasCheckConstraint(
                    "ck_scoring_rule_produces_object",
                    "jsonb_typeof(produces) = 'object'");
                table.HasCheckConstraint(
                    "ck_scoring_rule_compatibility_object",
                    "jsonb_typeof(compatibility) = 'object'");
                table.HasCheckConstraint(
                    "ck_scoring_rule_publish_shape",
                    "(status = 'published' AND published_at IS NOT NULL AND is_locked = TRUE) OR (status <> 'published')");
            });
            builder.HasKey(rule => rule.Id).HasName("pk_scoring_rule");

            builder.Property(rule => rule.Id).HasColumnName("id");
            builder.Property(rule => rule.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(rule => rule.RuleKey).HasColumnName("rule_key").HasMaxLength(128).IsRequired();
            builder.Property(rule => rule.RuleVersion).HasColumnName("rule_version").HasMaxLength(64).IsRequired();
            builder.Property(rule => rule.SchemaVersion).HasColumnName("schema_version").HasMaxLength(64).IsRequired();
            builder.Property(rule => rule.EngineMinVersion).HasColumnName("engine_min_version").HasMaxLength(64).IsRequired();
            builder.Property(rule => rule.DocumentHash).HasColumnName("document_hash").HasMaxLength(64).IsRequired();
            builder.Property(rule => rule.Document)
                .HasColumnName("document")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(rule => rule.Produces)
                .HasColumnName("produces")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(rule => rule.Compatibility)
                .HasColumnName("compatibility")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(rule => rule.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(rule => rule.IsLocked).HasColumnName("is_locked").IsRequired();
            builder.Property(rule => rule.PublishedAt).HasColumnName("published_at");
            builder.Property(rule => rule.PublishedBy).HasColumnName("published_by");
            builder.Property(rule => rule.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(rule => rule.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(rule => new { rule.TemplateVersionId, rule.RuleKey, rule.RuleVersion })
                .HasDatabaseName("ix_scoring_rule_template_version_id_rule_key_rule_version")
                .IsUnique();
            builder.HasIndex(rule => new { rule.TemplateVersionId, rule.Status })
                .HasDatabaseName("ix_scoring_rule_template_version_id_status");
            builder.HasIndex(rule => rule.DocumentHash)
                .HasDatabaseName("ix_scoring_rule_document_hash");
            builder.HasIndex(rule => rule.PublishedBy)
                .HasDatabaseName("ix_scoring_rule_published_by");

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(rule => rule.TemplateVersionId)
                .HasConstraintName("fk_scoring_rule_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(rule => rule.PublishedBy)
                .HasConstraintName("fk_scoring_rule_user_account_published_by")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScoreRun>(builder =>
        {
            builder.ToTable("score_run", table =>
            {
                table.HasCheckConstraint(
                    "ck_score_run_status",
                    "status IN ('running','success','failed')");
            });
            builder.HasKey(run => run.Id).HasName("pk_score_run");

            builder.Property(run => run.Id).HasColumnName("id");
            builder.Property(run => run.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(run => run.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(run => run.ResponseSessionId).HasColumnName("response_session_id").IsRequired();
            builder.Property(run => run.ScoringRuleId).HasColumnName("scoring_rule_id").IsRequired();
            builder.Property(run => run.RanAt).HasColumnName("ran_at").IsRequired();
            builder.Property(run => run.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(run => run.ErrorMessage).HasColumnName("error_message");

            builder.HasIndex(run => run.TenantId)
                .HasDatabaseName("ix_score_run_tenant_id");
            builder.HasIndex(run => run.CampaignId)
                .HasDatabaseName("ix_score_run_campaign_id");
            builder.HasIndex(run => run.ResponseSessionId)
                .HasDatabaseName("ix_score_run_response_session_id");
            builder.HasIndex(run => run.ScoringRuleId)
                .HasDatabaseName("ix_score_run_scoring_rule_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(run => run.TenantId)
                .HasConstraintName("fk_score_run_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(run => run.CampaignId)
                .HasConstraintName("fk_score_run_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(run => run.ResponseSessionId)
                .HasConstraintName("fk_score_run_response_session_response_session_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ScoringRule>()
                .WithMany()
                .HasForeignKey(run => run.ScoringRuleId)
                .HasConstraintName("fk_score_run_scoring_rule_scoring_rule_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Score>(builder =>
        {
            builder.ToTable("score", table =>
            {
                table.HasCheckConstraint(
                    "ck_score_n_non_negative",
                    "n >= 0");
                table.HasCheckConstraint(
                    "ck_score_n_expected_non_negative",
                    "n_expected >= 0");
                table.HasCheckConstraint(
                    "ck_score_n_valid_not_above_expected",
                    "n <= n_expected");
                table.HasCheckConstraint(
                    "ck_score_missing_policy_status_shape",
                    "missing_policy_status ~ '^[a-z0-9_.-]{1,64}$'");
            });
            builder.HasKey(score => score.Id).HasName("pk_score");

            builder.Property(score => score.Id).HasColumnName("id");
            builder.Property(score => score.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(score => score.ScoreRunId).HasColumnName("score_run_id").IsRequired();
            builder.Property(score => score.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(score => score.ResponseSessionId).HasColumnName("response_session_id").IsRequired();
            builder.Property(score => score.DimensionCode).HasColumnName("dimension_code").HasMaxLength(128).IsRequired();
            builder.Property(score => score.Value).HasColumnName("value").HasColumnType("numeric(10,4)").IsRequired();
            builder.Property(score => score.NValid).HasColumnName("n").IsRequired();
            builder.Property(score => score.NExpected).HasColumnName("n_expected").IsRequired();
            builder.Property(score => score.MissingPolicyStatus)
                .HasColumnName("missing_policy_status")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(score => score.ComputedAt).HasColumnName("computed_at").IsRequired();

            builder.HasIndex(score => score.TenantId)
                .HasDatabaseName("ix_score_tenant_id");
            builder.HasIndex(score => score.ScoreRunId)
                .HasDatabaseName("ix_score_score_run_id");
            builder.HasIndex(score => score.ResponseSessionId)
                .HasDatabaseName("ix_score_response_session_id");
            builder.HasIndex(score => new { score.CampaignId, score.DimensionCode })
                .HasDatabaseName("ix_score_campaign_id_dimension_code");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(score => score.TenantId)
                .HasConstraintName("fk_score_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ScoreRun>()
                .WithMany()
                .HasForeignKey(score => score.ScoreRunId)
                .HasConstraintName("fk_score_score_run_score_run_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(score => score.CampaignId)
                .HasConstraintName("fk_score_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(score => score.ResponseSessionId)
                .HasConstraintName("fk_score_response_session_response_session_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExportArtifact>(builder =>
        {
            builder.ToTable("export_artifact", table =>
            {
                table.HasCheckConstraint(
                    "ck_export_artifact_status",
                    "status IN ('queued','rendering','succeeded','failed','expired','deleted')");
                table.HasCheckConstraint(
                    "ck_export_artifact_format",
                    "format IN ('csv_codebook','html','pdf')");
                table.HasCheckConstraint(
                    "ck_export_artifact_type",
                    "artifact_type IN ('report_proof_csv_codebook','campaign_series_response_csv_codebook','campaign_series_results_matrix_csv_codebook','campaign_series_report_html','campaign_series_report_pdf')");
                table.HasCheckConstraint(
                    "ck_export_artifact_target_kind",
                    "target_kind IN ('campaign','campaign_series')");
                table.HasCheckConstraint(
                    "ck_export_artifact_target_scope",
                    "(target_kind = 'campaign' AND campaign_id IS NOT NULL)\n" +
                    "OR (target_kind = 'campaign_series' AND campaign_id IS NULL AND campaign_series_id IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_export_artifact_storage_kind",
                    "storage_kind IN ('inline_text','external_object')");
                table.HasCheckConstraint(
                    "ck_export_artifact_storage_shape",
                    "(storage_kind = 'inline_text' AND storage_key IS NULL)\n" +
                    "OR (storage_kind = 'external_object' AND content IS NULL)");
                table.HasCheckConstraint(
                    "ck_export_artifact_row_count_non_negative",
                    "row_count >= 0");
                table.HasCheckConstraint(
                    "ck_export_artifact_byte_size_non_negative",
                    "byte_size >= 0");
                table.HasCheckConstraint(
                    "ck_export_artifact_checksum_sha256",
                    "checksum_sha256 IS NULL OR checksum_sha256 ~ '^[0-9a-f]{64}$'");
                table.HasCheckConstraint(
                    "ck_export_artifact_materialization_shape",
                    "(status = 'succeeded' AND completed_at IS NOT NULL AND checksum_sha256 IS NOT NULL AND " +
                    "((storage_kind = 'inline_text' AND content IS NOT NULL AND storage_key IS NULL)\n" +
                    "OR (storage_kind = 'external_object' AND content IS NULL AND storage_key IS NOT NULL)))\n" +
                    "OR (status <> 'succeeded' AND checksum_sha256 IS NULL AND content IS NULL AND storage_key IS NULL)");
                table.HasCheckConstraint(
                    "ck_export_artifact_lifecycle_shape",
                    "(status = 'queued' AND started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND expires_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\n" +
                    "OR (status = 'rendering' AND started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\n" +
                    "OR (status = 'succeeded' AND completed_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\n" +
                    "OR (status = 'failed' AND failed_at IS NOT NULL AND failure_reason_code IS NOT NULL AND completed_at IS NULL AND deleted_at IS NULL)\n" +
                    "OR (status = 'expired' AND expires_at IS NOT NULL AND failed_at IS NULL AND deleted_at IS NULL AND failure_reason_code IS NULL)\n" +
                    "OR (status = 'deleted' AND deleted_at IS NOT NULL AND failed_at IS NULL AND failure_reason_code IS NULL)");
                table.HasCheckConstraint(
                    "ck_export_artifact_failure_reason_shape",
                    "failure_reason_code IS NULL OR failure_reason_code ~ '^[a-z0-9_.-]{1,128}$'");
                table.HasCheckConstraint(
                    "ck_export_artifact_metadata_object",
                    "metadata_json IS NOT NULL AND jsonb_typeof(metadata_json) = 'object'");
                table.HasCheckConstraint(
                    "ck_export_artifact_codebook_object",
                    "codebook_json IS NOT NULL AND jsonb_typeof(codebook_json) = 'object'");
            });
            builder.HasKey(artifact => artifact.Id).HasName("pk_export_artifact");

            builder.Property(artifact => artifact.Id).HasColumnName("id");
            builder.Property(artifact => artifact.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(artifact => artifact.TargetKind).HasColumnName("target_kind").HasMaxLength(64).IsRequired();
            builder.Property(artifact => artifact.CampaignId).HasColumnName("campaign_id");
            builder.Property(artifact => artifact.CampaignSeriesId).HasColumnName("campaign_series_id");
            builder.Property(artifact => artifact.ArtifactType).HasColumnName("artifact_type").HasMaxLength(128).IsRequired();
            builder.Property(artifact => artifact.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(artifact => artifact.Format).HasColumnName("format").HasMaxLength(64).IsRequired();
            builder.Property(artifact => artifact.FileName).HasColumnName("file_name").HasMaxLength(256).IsRequired();
            builder.Property(artifact => artifact.ContentType).HasColumnName("content_type").HasMaxLength(128).IsRequired();
            builder.Property(artifact => artifact.RowCount).HasColumnName("row_count").IsRequired();
            builder.Property(artifact => artifact.ByteSize).HasColumnName("byte_size").IsRequired();
            builder.Property(artifact => artifact.ChecksumSha256)
                .HasColumnName("checksum_sha256")
                .HasMaxLength(64)
                .IsFixedLength();
            builder.Property(artifact => artifact.MetadataJson)
                .HasColumnName("metadata_json")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(artifact => artifact.Content).HasColumnName("content");
            builder.Property(artifact => artifact.CodebookJson)
                .HasColumnName("codebook_json")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(artifact => artifact.StorageKind)
                .HasColumnName("storage_kind")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(artifact => artifact.StorageKey)
                .HasColumnName("storage_key")
                .HasMaxLength(1024);
            builder.Property(artifact => artifact.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(artifact => artifact.CompletedAt).HasColumnName("completed_at");
            builder.Property(artifact => artifact.StartedAt).HasColumnName("started_at");
            builder.Property(artifact => artifact.FailedAt).HasColumnName("failed_at");
            builder.Property(artifact => artifact.ExpiresAt).HasColumnName("expires_at");
            builder.Property(artifact => artifact.DeletedAt).HasColumnName("deleted_at");
            builder.Property(artifact => artifact.FailureReasonCode)
                .HasColumnName("failure_reason_code")
                .HasMaxLength(128);

            builder.HasIndex(artifact => new { artifact.TenantId, artifact.TargetKind, artifact.CampaignId, artifact.CreatedAt })
                .HasDatabaseName("ix_export_artifact_tenant_id_target_kind_campaign_id_created_at");
            builder.HasIndex(artifact => new { artifact.TenantId, artifact.TargetKind, artifact.CampaignSeriesId, artifact.CreatedAt })
                .HasDatabaseName("ix_export_artifact_tenant_id_target_kind_campaign_series_id_created_at");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(artifact => artifact.TenantId)
                .HasConstraintName("fk_export_artifact_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(artifact => artifact.CampaignId)
                .HasConstraintName("fk_export_artifact_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(artifact => artifact.CampaignSeriesId)
                .HasConstraintName("fk_export_artifact_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CampaignSeries>(builder =>
        {
            builder.ToTable("campaign_series", table =>
            {
                table.HasCheckConstraint(
                    "ck_campaign_series_code_salt_length",
                    "octet_length(code_salt) = 32");
                table.HasCheckConstraint(
                    "ck_campaign_series_study_kind",
                    "study_kind IN ('own', 'sample')");
                table.HasCheckConstraint(
                    "ck_campaign_series_sample_scenario",
                    "sample_scenario IS NULL OR sample_scenario IN ('mixed_lifecycle', 'longitudinal', 'setup', 'in_collection', 'completed', 'blocked')");
                table.HasCheckConstraint(
                    "ck_campaign_series_study_design_type",
                    "study_design_type IS NULL OR study_design_type IN ('single_wave', 'repeated_group_trend', 'repeated_linked_change')");
                table.HasCheckConstraint(
                    "ck_campaign_series_study_intended_use",
                    "study_intended_use IS NULL OR study_intended_use IN ('internal_review', 'research_analysis', 'client_report')");
                table.HasCheckConstraint(
                    "ck_campaign_series_sample_consistency",
                    "(study_kind = 'own' AND sample_scenario IS NULL) OR (study_kind = 'sample' AND sample_scenario IS NOT NULL)");
            });
            builder.HasKey(series => series.Id).HasName("pk_campaign_series");

            builder.Property(series => series.Id).HasColumnName("id");
            builder.Property(series => series.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(series => series.WorkspaceId).HasColumnName("workspace_id");
            builder.Property(series => series.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(series => series.EthicsApprovalId).HasColumnName("ethics_approval_id");
            builder.Property(series => series.RetentionUntil).HasColumnName("retention_until").HasColumnType("date");
            builder.Property(series => series.StudyKind)
                .HasColumnName("study_kind")
                .HasDefaultValue(CampaignSeriesStudyKinds.Own)
                .IsRequired();
            builder.Property(series => series.SampleScenario).HasColumnName("sample_scenario");
            builder.Property(series => series.StudyPurpose)
                .HasColumnName("study_purpose")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyPurposeMaxLength);
            builder.Property(series => series.StudyAudience)
                .HasColumnName("study_audience")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyAudienceMaxLength);
            builder.Property(series => series.StudyDesignType)
                .HasColumnName("study_design_type")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyDesignTypeMaxLength);
            builder.Property(series => series.StudyIntendedUse)
                .HasColumnName("study_intended_use")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyIntendedUseMaxLength);
            builder.Property(series => series.StudyInterpretationBoundary)
                .HasColumnName("study_interpretation_boundary")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyInterpretationBoundaryMaxLength);
            builder.Property(series => series.StudyOwnerNotes)
                .HasColumnName("study_owner_notes")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.StudyOwnerNotesMaxLength);
            builder.Property(series => series.CodeSalt)
                .HasColumnName("code_salt")
                .HasColumnType("bytea")
                .IsRequired();
            builder.Property(series => series.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(series => series.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(series => series.ArchivedAt).HasColumnName("archived_at");
            builder.Property(series => series.ArchivedByUserId).HasColumnName("archived_by_user_id");
            builder.Property(series => series.ArchiveReason)
                .HasColumnName("archive_reason")
                .HasMaxLength(Platform.Domain.Campaigns.CampaignSeries.ArchiveReasonMaxLength);
            builder.Ignore(series => series.Archived);
            builder.Ignore(series => series.IsSample);

            builder.HasIndex(series => series.TenantId)
                .HasDatabaseName("ix_campaign_series_tenant_id");
            builder.HasIndex(series => new { series.TenantId, series.Name })
                .HasDatabaseName("ix_campaign_series_tenant_id_name");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(series => series.TenantId)
                .HasConstraintName("fk_campaign_series_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParticipantCode>(builder =>
        {
            builder.ToTable("participant_code", table =>
            {
                table.HasCheckConstraint(
                    "ck_participant_code_hash_length",
                    "octet_length(hash) = 32");
                table.HasCheckConstraint(
                    "ck_participant_code_argon2_parameters",
                    "argon2_memory_kib >= 65536 AND argon2_iterations >= 3 AND argon2_parallelism >= 4 AND argon2_output_bytes >= 32");
            });
            builder.HasKey(code => code.Id).HasName("pk_participant_code");

            builder.Property(code => code.Id).HasColumnName("id");
            builder.Property(code => code.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(code => code.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(code => code.Hash).HasColumnName("hash").HasColumnType("bytea").IsRequired();
            builder.Property(code => code.Argon2MemoryKiB).HasColumnName("argon2_memory_kib").IsRequired();
            builder.Property(code => code.Argon2Iterations).HasColumnName("argon2_iterations").IsRequired();
            builder.Property(code => code.Argon2Parallelism).HasColumnName("argon2_parallelism").IsRequired();
            builder.Property(code => code.Argon2OutputBytes).HasColumnName("argon2_output_bytes").IsRequired();
            builder.Property(code => code.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
            builder.Property(code => code.LastSeenAt).HasColumnName("last_seen_at").IsRequired();

            builder.HasIndex(code => code.TenantId)
                .HasDatabaseName("ix_participant_code_tenant_id");
            builder.HasIndex(code => new { code.CampaignSeriesId, code.Hash })
                .HasDatabaseName("ix_participant_code_campaign_series_id_hash")
                .IsUnique();

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(code => code.TenantId)
                .HasConstraintName("fk_participant_code_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(code => code.CampaignSeriesId)
                .HasConstraintName("fk_participant_code_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Campaign>(builder =>
        {
            builder.ToTable("campaign", table =>
            {
                table.HasCheckConstraint(
                    "ck_campaign_status",
                    "status IN ('draft','scheduled','live','closed','cancelled')");
                table.HasCheckConstraint(
                    "ck_campaign_response_identity_mode",
                    "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                table.HasCheckConstraint(
                    "ck_campaign_schedule_object",
                    "jsonb_typeof(schedule) = 'object'");
                table.HasCheckConstraint(
                    "ck_campaign_date_range",
                    "start_at IS NULL OR end_at IS NULL OR end_at > start_at");
            });
            builder.HasKey(campaign => campaign.Id).HasName("pk_campaign");

            builder.Property(campaign => campaign.Id).HasColumnName("id");
            builder.Property(campaign => campaign.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(campaign => campaign.WorkspaceId).HasColumnName("workspace_id");
            builder.Property(campaign => campaign.CampaignSeriesId).HasColumnName("campaign_series_id");
            builder.Property(campaign => campaign.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(campaign => campaign.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            builder.Property(campaign => campaign.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(campaign => campaign.ResponseIdentityMode)
                .HasColumnName("response_identity_mode")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(campaign => campaign.StartAt).HasColumnName("start_at");
            builder.Property(campaign => campaign.EndAt).HasColumnName("end_at");
            builder.Property(campaign => campaign.Schedule)
                .HasColumnName("schedule")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(campaign => campaign.DefaultLocale)
                .HasColumnName("default_locale")
                .HasMaxLength(16)
                .IsRequired();
            builder.Property(campaign => campaign.CreatedBy).HasColumnName("created_by");
            builder.Property(campaign => campaign.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(campaign => campaign.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(campaign => campaign.ClosedAt).HasColumnName("closed_at");
            builder.Property(campaign => campaign.ClosedByUserId).HasColumnName("closed_by_user_id");
            builder.Property(campaign => campaign.CloseReason)
                .HasColumnName("close_reason")
                .HasMaxLength(Campaign.CloseReasonMaxLength);

            builder.HasIndex(campaign => campaign.TenantId)
                .HasDatabaseName("ix_campaign_tenant_id");
            builder.HasIndex(campaign => new { campaign.TenantId, campaign.Status })
                .HasDatabaseName("ix_campaign_tenant_id_status");
            builder.HasIndex(campaign => campaign.CampaignSeriesId)
                .HasDatabaseName("ix_campaign_campaign_series_id");
            builder.HasIndex(campaign => campaign.TemplateVersionId)
                .HasDatabaseName("ix_campaign_template_version_id");
            builder.HasIndex(campaign => campaign.CreatedBy)
                .HasDatabaseName("ix_campaign_created_by");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(campaign => campaign.TenantId)
                .HasConstraintName("fk_campaign_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(campaign => campaign.CampaignSeriesId)
                .HasConstraintName("fk_campaign_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(campaign => campaign.TemplateVersionId)
                .HasConstraintName("fk_campaign_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(campaign => campaign.CreatedBy)
                .HasConstraintName("fk_campaign_user_account_created_by")
                .OnDelete(DeleteBehavior.Restrict);

        });

        modelBuilder.Entity<CampaignLaunchSnapshot>(builder =>
        {
            builder.ToTable("campaign_launch_snapshot", table =>
            {
                table.HasCheckConstraint(
                    "ck_campaign_launch_snapshot_response_identity_mode",
                    "response_identity_mode IN ('identified','anonymous','anonymous_longitudinal')");
                table.HasCheckConstraint(
                    "ck_campaign_launch_snapshot_question_count_positive",
                    "template_question_count > 0");
                table.HasCheckConstraint(
                    "ck_campaign_launch_snapshot_readiness_object",
                    "jsonb_typeof(launch_readiness) = 'object'");
                table.HasCheckConstraint(
                    "ck_campaign_launch_snapshot_packet_object",
                    "jsonb_typeof(launch_packet) = 'object'");
            });
            builder.HasKey(snapshot => snapshot.Id).HasName("pk_campaign_launch_snapshot");

            builder.Property(snapshot => snapshot.Id).HasColumnName("id");
            builder.Property(snapshot => snapshot.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(snapshot => snapshot.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(snapshot => snapshot.CampaignSeriesId).HasColumnName("campaign_series_id");
            builder.Property(snapshot => snapshot.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(snapshot => snapshot.ScoringRuleId).HasColumnName("scoring_rule_id").IsRequired();
            builder.Property(snapshot => snapshot.ConsentDocumentId).HasColumnName("consent_document_id");
            builder.Property(snapshot => snapshot.RetentionPolicyId).HasColumnName("retention_policy_id");
            builder.Property(snapshot => snapshot.DisclosurePolicyId).HasColumnName("disclosure_policy_id");
            builder.Property(snapshot => snapshot.ResponseIdentityMode)
                .HasColumnName("response_identity_mode")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(snapshot => snapshot.DefaultLocale)
                .HasColumnName("default_locale")
                .HasMaxLength(16)
                .IsRequired();
            builder.Property(snapshot => snapshot.TemplateQuestionCount)
                .HasColumnName("template_question_count")
                .IsRequired();
            builder.Property(snapshot => snapshot.ScoringRuleDocumentHash)
                .HasColumnName("scoring_rule_document_hash")
                .HasMaxLength(128)
                .IsRequired();
            builder.Property(snapshot => snapshot.LaunchReadiness)
                .HasColumnName("launch_readiness")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(snapshot => snapshot.LaunchPacket)
                .HasColumnName("launch_packet")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(snapshot => snapshot.LaunchedAt).HasColumnName("launched_at").IsRequired();
            builder.Property(snapshot => snapshot.LaunchedBy).HasColumnName("launched_by");
            builder.Property(snapshot => snapshot.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(snapshot => snapshot.CampaignId)
                .HasDatabaseName("ix_campaign_launch_snapshot_campaign_id")
                .IsUnique();
            builder.HasIndex(snapshot => snapshot.TenantId)
                .HasDatabaseName("ix_campaign_launch_snapshot_tenant_id");
            builder.HasIndex(snapshot => new { snapshot.TenantId, snapshot.LaunchedAt })
                .HasDatabaseName("ix_campaign_launch_snapshot_tenant_id_launched_at");
            builder.HasIndex(snapshot => snapshot.CampaignSeriesId)
                .HasDatabaseName("ix_campaign_launch_snapshot_campaign_series_id");
            builder.HasIndex(snapshot => snapshot.TemplateVersionId)
                .HasDatabaseName("ix_campaign_launch_snapshot_template_version_id");
            builder.HasIndex(snapshot => snapshot.ScoringRuleId)
                .HasDatabaseName("ix_campaign_launch_snapshot_scoring_rule_id");
            builder.HasIndex(snapshot => snapshot.ConsentDocumentId)
                .HasDatabaseName("ix_campaign_launch_snapshot_consent_document_id")
                .HasFilter("consent_document_id IS NOT NULL");
            builder.HasIndex(snapshot => snapshot.RetentionPolicyId)
                .HasDatabaseName("ix_campaign_launch_snapshot_retention_policy_id")
                .HasFilter("retention_policy_id IS NOT NULL");
            builder.HasIndex(snapshot => snapshot.DisclosurePolicyId)
                .HasDatabaseName("ix_campaign_launch_snapshot_disclosure_policy_id")
                .HasFilter("disclosure_policy_id IS NOT NULL");
            builder.HasIndex(snapshot => snapshot.LaunchedBy)
                .HasDatabaseName("ix_campaign_launch_snapshot_launched_by");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TenantId)
                .HasConstraintName("fk_campaign_launch_snapshot_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.CampaignId)
                .HasConstraintName("fk_campaign_launch_snapshot_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.CampaignSeriesId)
                .HasConstraintName("fk_campaign_launch_snapshot_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TemplateVersionId)
                .HasConstraintName("fk_campaign_launch_snapshot_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ScoringRule>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.ScoringRuleId)
                .HasConstraintName("fk_campaign_launch_snapshot_scoring_rule_scoring_rule_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ConsentDocument>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.ConsentDocumentId)
                .HasConstraintName("fk_campaign_launch_snapshot_consent_document_consent_document_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<RetentionPolicy>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.RetentionPolicyId)
                .HasConstraintName("fk_campaign_launch_snapshot_retention_policy_retention_policy_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<DisclosurePolicy>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.DisclosurePolicyId)
                .HasConstraintName("fk_campaign_launch_snapshot_disclosure_policy_disclosure_policy_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.LaunchedBy)
                .HasConstraintName("fk_campaign_launch_snapshot_user_account_launched_by")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConsentDocument>(builder =>
        {
            builder.ToTable("consent_document", table =>
            {
                table.HasCheckConstraint(
                    "ck_consent_document_required_grants_array",
                    "jsonb_typeof(required_grants) = 'array'");
                table.HasCheckConstraint(
                    "ck_consent_document_optional_grants_array",
                    "jsonb_typeof(optional_grants) = 'array'");
                table.HasCheckConstraint(
                    "ck_consent_document_retired_after_published",
                    "retired_at IS NULL OR retired_at > published_at");
            });
            builder.HasKey(document => document.Id).HasName("pk_consent_document");

            builder.Property(document => document.Id).HasColumnName("id");
            builder.Property(document => document.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(document => document.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(document => document.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(document => document.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
            builder.Property(document => document.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
            builder.Property(document => document.BodyMarkdown).HasColumnName("body_markdown").IsRequired();
            builder.Property(document => document.RequiredGrants)
                .HasColumnName("required_grants")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(document => document.OptionalGrants)
                .HasColumnName("optional_grants")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(document => document.PublishedAt).HasColumnName("published_at").IsRequired();
            builder.Property(document => document.RetiredAt).HasColumnName("retired_at");
            builder.Property(document => document.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(document => new { document.CampaignSeriesId, document.Locale, document.Version })
                .HasDatabaseName("ix_consent_document_campaign_series_id_locale_version")
                .IsUnique();
            builder.HasIndex(document => document.TenantId)
                .HasDatabaseName("ix_consent_document_tenant_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(document => document.TenantId)
                .HasConstraintName("fk_consent_document_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(document => document.CampaignSeriesId)
                .HasConstraintName("fk_consent_document_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RetentionPolicy>(builder =>
        {
            builder.ToTable("retention_policy", table =>
            {
                table.HasCheckConstraint(
                    "ck_retention_policy_retain_for_years_positive",
                    "retain_for_years > 0");
                table.HasCheckConstraint(
                    "ck_retention_policy_retention_start_event",
                    "retention_start_event IN ('consent_accepted_at','response_submitted_at','wave_closed_at','series_closed_at','last_response_submitted_at')");
                table.HasCheckConstraint(
                    "ck_retention_policy_action_after",
                    "action_after IN ('delete','anonymize')");
                table.HasCheckConstraint(
                    "ck_retention_policy_publication_limits_object",
                    "jsonb_typeof(publication_limits) = 'object'");
                table.HasCheckConstraint(
                    "ck_retention_policy_retired_after_created",
                    "retired_at IS NULL OR retired_at > created_at");
            });
            builder.HasKey(policy => policy.Id).HasName("pk_retention_policy");

            builder.Property(policy => policy.Id).HasColumnName("id");
            builder.Property(policy => policy.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(policy => policy.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(policy => policy.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
            builder.Property(policy => policy.RetainForYears).HasColumnName("retain_for_years").IsRequired();
            builder.Property(policy => policy.RetentionStartEvent)
                .HasColumnName("retention_start_event")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(policy => policy.ActionAfter)
                .HasColumnName("action_after")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(policy => policy.NextReviewAt)
                .HasColumnName("next_review_at")
                .HasColumnType("date")
                .IsRequired();
            builder.Property(policy => policy.PublicationLimits)
                .HasColumnName("publication_limits")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(policy => policy.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(policy => policy.RetiredAt).HasColumnName("retired_at");

            builder.HasIndex(policy => new { policy.CampaignSeriesId, policy.Version })
                .HasDatabaseName("ix_retention_policy_campaign_series_id_version")
                .IsUnique();
            builder.HasIndex(policy => policy.TenantId)
                .HasDatabaseName("ix_retention_policy_tenant_id");
            builder.HasIndex(policy => policy.CampaignSeriesId)
                .HasDatabaseName("ix_retention_policy_campaign_series_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(policy => policy.TenantId)
                .HasConstraintName("fk_retention_policy_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(policy => policy.CampaignSeriesId)
                .HasConstraintName("fk_retention_policy_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WithdrawalEvent>(builder =>
        {
            builder.ToTable("withdrawal_event", table =>
            {
                table.HasCheckConstraint(
                    "ck_withdrawal_event_target_kind",
                    "target_kind IN ('identified_subject','anonymous_longitudinal_code','anonymous_longitudinal_unmatched','response_session')");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_scope",
                    "scope IN ('campaign_series')");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_action_after",
                    "action_after IN ('delete','anonymize')");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_status",
                    "status IN ('requested', 'planned', 'processing', 'completed', 'failed', 'denied')");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_target_shape",
                    "(target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'response_session' AND subject_id IS NULL AND participant_code_id IS NULL AND (response_session_id IS NOT NULL OR (action_after = 'delete' AND status = 'completed' AND response_session_id IS NULL)))");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_counts_non_negative",
                    "consent_record_count >= 0 AND response_session_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_metadata_object",
                    "jsonb_typeof(metadata_json) = 'object'");
                table.HasCheckConstraint(
                    "ck_withdrawal_event_processed_after_requested",
                    "processed_at IS NULL OR processed_at >= requested_at");
            });
            builder.HasKey(withdrawal => withdrawal.Id).HasName("pk_withdrawal_event");

            builder.Property(withdrawal => withdrawal.Id).HasColumnName("id");
            builder.Property(withdrawal => withdrawal.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(withdrawal => withdrawal.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(withdrawal => withdrawal.RetentionPolicyId).HasColumnName("retention_policy_id").IsRequired();
            builder.Property(withdrawal => withdrawal.TargetKind)
                .HasColumnName("target_kind")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(withdrawal => withdrawal.Scope)
                .HasColumnName("scope")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(withdrawal => withdrawal.ActionAfter)
                .HasColumnName("action_after")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(withdrawal => withdrawal.Status)
                .HasColumnName("status")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(withdrawal => withdrawal.SubjectId).HasColumnName("subject_id");
            builder.Property(withdrawal => withdrawal.ParticipantCodeId).HasColumnName("participant_code_id");
            builder.Property(withdrawal => withdrawal.ResponseSessionId).HasColumnName("response_session_id");
            builder.Property(withdrawal => withdrawal.RequestedAt).HasColumnName("requested_at").IsRequired();
            builder.Property(withdrawal => withdrawal.ProcessedAt).HasColumnName("processed_at");
            builder.Property(withdrawal => withdrawal.ConsentRecordCount)
                .HasColumnName("consent_record_count")
                .IsRequired();
            builder.Property(withdrawal => withdrawal.ResponseSessionCount)
                .HasColumnName("response_session_count")
                .IsRequired();
            builder.Property(withdrawal => withdrawal.AnswerCount).HasColumnName("answer_count").IsRequired();
            builder.Property(withdrawal => withdrawal.ScoreRunCount).HasColumnName("score_run_count").IsRequired();
            builder.Property(withdrawal => withdrawal.ScoreCount).HasColumnName("score_count").IsRequired();
            builder.Property(withdrawal => withdrawal.MetadataJson)
                .HasColumnName("metadata_json")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(withdrawal => withdrawal.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(withdrawal => withdrawal.TenantId)
                .HasDatabaseName("ix_withdrawal_event_tenant_id");
            builder.HasIndex(withdrawal => new { withdrawal.TenantId, withdrawal.CampaignSeriesId, withdrawal.RequestedAt })
                .HasDatabaseName("ix_withdrawal_event_tenant_id_campaign_series_id_requested_at");
            builder.HasIndex(withdrawal => withdrawal.SubjectId)
                .HasDatabaseName("ix_withdrawal_event_subject_id")
                .HasFilter("subject_id IS NOT NULL");
            builder.HasIndex(withdrawal => withdrawal.ParticipantCodeId)
                .HasDatabaseName("ix_withdrawal_event_participant_code_id")
                .HasFilter("participant_code_id IS NOT NULL");
            builder.HasIndex(withdrawal => withdrawal.ResponseSessionId)
                .HasDatabaseName("ix_withdrawal_event_response_session_id")
                .HasFilter("response_session_id IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.TenantId)
                .HasConstraintName("fk_withdrawal_event_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.CampaignSeriesId)
                .HasConstraintName("fk_withdrawal_event_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<RetentionPolicy>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.RetentionPolicyId)
                .HasConstraintName("fk_withdrawal_event_retention_policy_retention_policy_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.SubjectId)
                .HasConstraintName("fk_withdrawal_event_subject_subject_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ParticipantCode>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.ParticipantCodeId)
                .HasConstraintName("fk_withdrawal_event_participant_code_participant_code_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(withdrawal => withdrawal.ResponseSessionId)
                .HasConstraintName("fk_withdrawal_event_response_session_response_session_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WithdrawalEvent>(builder =>
        {
            builder.ToTable("withdrawal_event", table =>
            {
                table.HasCheckConstraint("ck_withdrawal_event_target_kind", "target_kind IN ('identified_subject', 'anonymous_longitudinal_code', 'anonymous_longitudinal_unmatched', 'response_session')");
                table.HasCheckConstraint("ck_withdrawal_event_scope", "scope IN ('campaign_series')");
                table.HasCheckConstraint("ck_withdrawal_event_action_after", "action_after IN ('delete', 'anonymize')");
                table.HasCheckConstraint("ck_withdrawal_event_status", "status IN ('requested', 'planned', 'processing', 'completed', 'failed', 'denied')");
                table.HasCheckConstraint("ck_withdrawal_event_target_shape", "((target_kind = 'identified_subject' AND subject_id IS NOT NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_code' AND subject_id IS NULL AND participant_code_id IS NOT NULL AND response_session_id IS NULL) OR (target_kind = 'anonymous_longitudinal_unmatched' AND subject_id IS NULL AND participant_code_id IS NULL AND response_session_id IS NULL) OR (target_kind = 'response_session' AND subject_id IS NULL AND participant_code_id IS NULL AND (response_session_id IS NOT NULL OR (action_after = 'delete' AND status = 'completed' AND response_session_id IS NULL))))");
                table.HasCheckConstraint("ck_withdrawal_event_counts_non_negative", "consent_record_count >= 0 AND response_session_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0");
                table.HasCheckConstraint("ck_withdrawal_event_metadata_object", "jsonb_typeof(metadata_json) = 'object'");
                table.HasCheckConstraint("ck_withdrawal_event_processed_after_requested", "processed_at IS NULL OR processed_at >= requested_at");
            });

            builder.HasKey(e => e.Id).HasName("pk_withdrawal_event");

            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.TenantId).HasColumnName("tenant_id");
            builder.Property(e => e.CampaignSeriesId).HasColumnName("campaign_series_id");
            builder.Property(e => e.RetentionPolicyId).HasColumnName("retention_policy_id");
            builder.Property(e => e.TargetKind).HasColumnName("target_kind").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(64).IsRequired();
            builder.Property(e => e.ActionAfter).HasColumnName("action_after").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(e => e.SubjectId).HasColumnName("subject_id");
            builder.Property(e => e.ParticipantCodeId).HasColumnName("participant_code_id");
            builder.Property(e => e.ResponseSessionId).HasColumnName("response_session_id");
            builder.Property(e => e.RequestedAt).HasColumnName("requested_at");
            builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            builder.Property(e => e.ConsentRecordCount).HasColumnName("consent_record_count");
            builder.Property(e => e.ResponseSessionCount).HasColumnName("response_session_count");
            builder.Property(e => e.AnswerCount).HasColumnName("answer_count");
            builder.Property(e => e.ScoreRunCount).HasColumnName("score_run_count");
            builder.Property(e => e.ScoreCount).HasColumnName("score_count");
            builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired();
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_withdrawal_event_tenant_id");
            builder.HasIndex(e => new { e.TenantId, e.CampaignSeriesId, e.RequestedAt }).HasDatabaseName("ix_withdrawal_event_tenant_series_requested");
            builder.HasIndex(e => e.SubjectId).HasDatabaseName("ix_withdrawal_event_subject_id").HasFilter("subject_id IS NOT NULL");
            builder.HasIndex(e => e.ParticipantCodeId).HasDatabaseName("ix_withdrawal_event_participant_code_id").HasFilter("participant_code_id IS NOT NULL");
            builder.HasIndex(e => e.ResponseSessionId).HasDatabaseName("ix_withdrawal_event_response_session_id").HasFilter("response_session_id IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_tenant");

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(e => e.CampaignSeriesId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_campaign_series");

            builder.HasOne<RetentionPolicy>()
                .WithMany()
                .HasForeignKey(e => e.RetentionPolicyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_retention_policy");

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_subject");

            builder.HasOne<ParticipantCode>()
                .WithMany()
                .HasForeignKey(e => e.ParticipantCodeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_participant_code");

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(e => e.ResponseSessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_withdrawal_event_response_session");
        });

        modelBuilder.Entity<WithdrawalRequestToken>(builder =>
        {
            builder.ToTable("withdrawal_request_token", table =>
            {
                table.HasCheckConstraint(
                    "ck_withdrawal_request_token_action",
                    "requested_action IN ('delete', 'anonymize')");
                table.HasCheckConstraint(
                    "ck_withdrawal_request_token_expiry",
                    "expires_at > created_at");
            });
            builder.HasKey(token => token.Id).HasName("pk_withdrawal_request_token");

            builder.Property(token => token.Id).HasColumnName("id");
            builder.Property(token => token.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(token => token.ResponseSessionId).HasColumnName("response_session_id").IsRequired();
            builder.Property(token => token.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(128)
                .IsRequired();
            builder.Property(token => token.RequestedAction)
                .HasColumnName("requested_action")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(token => token.ConsumedAt).HasColumnName("consumed_at");
            builder.Property(token => token.CreatedReason)
                .HasColumnName("created_reason")
                .HasMaxLength(64);
            builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(token => token.TokenHash)
                .HasDatabaseName("ix_withdrawal_request_token_token_hash")
                .IsUnique();
            builder.HasIndex(token => token.TenantId)
                .HasDatabaseName("ix_withdrawal_request_token_tenant_id");
            builder.HasIndex(token => token.ResponseSessionId)
                .HasDatabaseName("ix_withdrawal_request_token_response_session_id");
            builder.HasIndex(token => new { token.TenantId, token.ExpiresAt })
                .HasDatabaseName("ix_withdrawal_request_token_tenant_id_expires_at");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(token => token.TenantId)
                .HasConstraintName("fk_withdrawal_request_token_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(token => token.ResponseSessionId)
                .HasConstraintName("fk_withdrawal_request_token_response_session_response_session_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RetentionDueBatch>(builder =>
        {
            builder.ToTable("retention_due_batch", table =>
            {
                table.HasCheckConstraint(
                    "ck_retention_due_batch_anchor",
                    "anchor IN ('response_submitted_at')");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_action_after",
                    "action_after IN ('delete','anonymize')");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_status",
                    "status IN ('planned','processing','completed','failed')");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_due_before_as_of",
                    "due_before <= as_of");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_response_session_count_positive",
                    "response_session_count > 0");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_counts_non_negative",
                    "consent_record_count >= 0 AND answer_count >= 0 AND score_run_count >= 0 AND score_count >= 0 AND derived_artifact_count >= 0");
                table.HasCheckConstraint(
                    "ck_retention_due_batch_lifecycle",
                    "((status = 'planned' AND processing_started_at IS NULL AND completed_at IS NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'processing' AND processing_started_at IS NOT NULL AND completed_at IS NULL AND failed_at IS NULL) OR (status = 'completed' AND processing_started_at IS NOT NULL AND completed_at IS NOT NULL AND failed_at IS NULL AND failure_code IS NULL AND failure_detail IS NULL) OR (status = 'failed' AND completed_at IS NULL AND failed_at IS NOT NULL AND failure_code IS NOT NULL))");
            });

            builder.HasKey(batch => batch.Id).HasName("pk_retention_due_batch");

            builder.Property(batch => batch.Id).HasColumnName("id");
            builder.Property(batch => batch.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(batch => batch.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(batch => batch.RetentionPolicyId).HasColumnName("retention_policy_id").IsRequired();
            builder.Property(batch => batch.Anchor).HasColumnName("anchor").HasMaxLength(64).IsRequired();
            builder.Property(batch => batch.ActionAfter).HasColumnName("action_after").HasMaxLength(64).IsRequired();
            builder.Property(batch => batch.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(batch => batch.AsOf).HasColumnName("as_of").IsRequired();
            builder.Property(batch => batch.DueBefore).HasColumnName("due_before").IsRequired();
            builder.Property(batch => batch.ConsentRecordCount).HasColumnName("consent_record_count").IsRequired();
            builder.Property(batch => batch.ResponseSessionCount).HasColumnName("response_session_count").IsRequired();
            builder.Property(batch => batch.AnswerCount).HasColumnName("answer_count").IsRequired();
            builder.Property(batch => batch.ScoreRunCount).HasColumnName("score_run_count").IsRequired();
            builder.Property(batch => batch.ScoreCount).HasColumnName("score_count").IsRequired();
            builder.Property(batch => batch.DerivedArtifactCount).HasColumnName("derived_artifact_count").IsRequired();
            builder.Property(batch => batch.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(batch => batch.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(batch => batch.ProcessingStartedAt).HasColumnName("processing_started_at");
            builder.Property(batch => batch.CompletedAt).HasColumnName("completed_at");
            builder.Property(batch => batch.FailedAt).HasColumnName("failed_at");
            builder.Property(batch => batch.FailureCode)
                .HasColumnName("failure_code")
                .HasMaxLength(128);
            builder.Property(batch => batch.FailureDetail)
                .HasColumnName("failure_detail")
                .HasMaxLength(512);
            builder.Property(batch => batch.ExecutionResult)
                .HasColumnName("execution_result")
                .HasMaxLength(128);
            builder.Property(batch => batch.ArtifactInvalidatedCount).HasColumnName("artifact_invalidated_count");
            builder.Property(batch => batch.NoticeScrubbedCount).HasColumnName("notice_scrubbed_count");
            builder.Property(batch => batch.DeliveryAttemptScrubbedCount).HasColumnName("delivery_attempt_scrubbed_count");
            builder.Property(batch => batch.InviteCredentialScrubbedCount).HasColumnName("invite_credential_scrubbed_count");

            builder.HasIndex(batch => batch.TenantId)
                .HasDatabaseName("ix_retention_due_batch_tenant_id");
            builder.HasIndex(batch => new { batch.TenantId, batch.Status, batch.CreatedAt })
                .HasDatabaseName("ix_retention_due_batch_tenant_status_created_at");
            builder.HasIndex(batch => new { batch.TenantId, batch.IdempotencyKey })
                .HasDatabaseName("ix_retention_due_batch_tenant_id_idempotency_key")
                .IsUnique();
            builder.HasIndex(batch => new { batch.TenantId, batch.CampaignSeriesId, batch.DueBefore })
                .HasDatabaseName("ix_retention_due_batch_tenant_series_due_before");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(batch => batch.TenantId)
                .HasConstraintName("fk_retention_due_batch_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(batch => batch.CampaignSeriesId)
                .HasConstraintName("fk_retention_due_batch_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<RetentionPolicy>()
                .WithMany()
                .HasForeignKey(batch => batch.RetentionPolicyId)
                .HasConstraintName("fk_retention_due_batch_retention_policy_retention_policy_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DisclosurePolicy>(builder =>
        {
            builder.ToTable("disclosure_policy", table =>
            {
                table.HasCheckConstraint(
                    "ck_disclosure_policy_k_min",
                    "k_min >= 5");
                table.HasCheckConstraint(
                    "ck_disclosure_policy_suppression_strategy",
                    "suppression_strategy IN ('hide_cell','aggregate_up','round_to_n')");
                table.HasCheckConstraint(
                    "ck_disclosure_policy_applies_to_dimensions_array",
                    "jsonb_typeof(applies_to_dimensions) = 'array'");
                table.HasCheckConstraint(
                    "ck_disclosure_policy_retired_after_created",
                    "retired_at IS NULL OR retired_at > created_at");
            });
            builder.HasKey(policy => policy.Id).HasName("pk_disclosure_policy");

            builder.Property(policy => policy.Id).HasColumnName("id");
            builder.Property(policy => policy.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(policy => policy.CampaignSeriesId).HasColumnName("campaign_series_id").IsRequired();
            builder.Property(policy => policy.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
            builder.Property(policy => policy.KMin).HasColumnName("k_min").IsRequired();
            builder.Property(policy => policy.SuppressionStrategy)
                .HasColumnName("suppression_strategy")
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(policy => policy.AppliesToDimensions)
                .HasColumnName("applies_to_dimensions")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(policy => policy.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(policy => policy.RetiredAt).HasColumnName("retired_at");

            builder.HasIndex(policy => new { policy.CampaignSeriesId, policy.Version })
                .HasDatabaseName("ix_disclosure_policy_campaign_series_id_version")
                .IsUnique();
            builder.HasIndex(policy => policy.TenantId)
                .HasDatabaseName("ix_disclosure_policy_tenant_id");
            builder.HasIndex(policy => policy.CampaignSeriesId)
                .HasDatabaseName("ix_disclosure_policy_campaign_series_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(policy => policy.TenantId)
                .HasConstraintName("fk_disclosure_policy_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CampaignSeries>()
                .WithMany()
                .HasForeignKey(policy => policy.CampaignSeriesId)
                .HasConstraintName("fk_disclosure_policy_campaign_series_campaign_series_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConsentRecord>(builder =>
        {
            builder.ToTable("consent_record", table =>
            {
                table.HasCheckConstraint(
                    "ck_consent_record_accepted_grants_array",
                    "jsonb_typeof(accepted_grants) = 'array'");
            });
            builder.HasKey(record => record.Id).HasName("pk_consent_record");

            builder.Property(record => record.Id).HasColumnName("id");
            builder.Property(record => record.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(record => record.ConsentDocumentId).HasColumnName("consent_document_id").IsRequired();
            builder.Property(record => record.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(record => record.AssignmentId).HasColumnName("assignment_id").IsRequired();
            builder.Property(record => record.SubjectId).HasColumnName("subject_id");
            builder.Property(record => record.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(record => record.AcceptedGrants)
                .HasColumnName("accepted_grants")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(record => record.AcceptedAt).HasColumnName("accepted_at").IsRequired();
            builder.Property(record => record.AnonymizedAt).HasColumnName("anonymized_at");
            builder.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(record => record.TenantId)
                .HasDatabaseName("ix_consent_record_tenant_id");
            builder.HasIndex(record => record.ConsentDocumentId)
                .HasDatabaseName("ix_consent_record_consent_document_id");
            builder.HasIndex(record => record.CampaignId)
                .HasDatabaseName("ix_consent_record_campaign_id");
            builder.HasIndex(record => record.AssignmentId)
                .HasDatabaseName("ix_consent_record_assignment_id");
            builder.HasIndex(record => record.SubjectId)
                .HasDatabaseName("ix_consent_record_subject_id")
                .HasFilter("subject_id IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(record => record.TenantId)
                .HasConstraintName("fk_consent_record_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ConsentDocument>()
                .WithMany()
                .HasForeignKey(record => record.ConsentDocumentId)
                .HasConstraintName("fk_consent_record_consent_document_consent_document_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(record => record.CampaignId)
                .HasConstraintName("fk_consent_record_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(record => record.AssignmentId)
                .HasConstraintName("fk_consent_record_assignment_assignment_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(record => record.SubjectId)
                .HasConstraintName("fk_consent_record_subject_subject_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Audience>(builder =>
        {
            builder.ToTable("audience");
            builder.HasKey(audience => audience.Id).HasName("pk_audience");

            builder.Property(audience => audience.Id).HasColumnName("id");
            builder.Property(audience => audience.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(audience => audience.Selector)
                .HasColumnName("selector")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(audience => audience.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(audience => audience.CampaignId)
                .HasDatabaseName("ix_audience_campaign_id");

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(audience => audience.CampaignId)
                .HasConstraintName("fk_audience_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AudienceMember>(builder =>
        {
            builder.ToTable("audience_member", table =>
            {
                table.HasCheckConstraint(
                    "ck_audience_member_removed_after_added",
                    "removed_at IS NULL OR removed_at >= added_at");
            });
            builder.HasKey(member => new { member.AudienceId, member.SubjectId })
                .HasName("pk_audience_member");

            builder.Property(member => member.AudienceId).HasColumnName("audience_id");
            builder.Property(member => member.SubjectId).HasColumnName("subject_id");
            builder.Property(member => member.AddedAt).HasColumnName("added_at").IsRequired();
            builder.Property(member => member.RemovedAt).HasColumnName("removed_at");

            builder.HasIndex(member => member.SubjectId)
                .HasDatabaseName("ix_audience_member_subject_id");

            builder.HasOne<Audience>()
                .WithMany()
                .HasForeignKey(member => member.AudienceId)
                .HasConstraintName("fk_audience_member_audience_audience_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(member => member.SubjectId)
                .HasConstraintName("fk_audience_member_subject_subject_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RespondentRule>(builder =>
        {
            builder.ToTable("respondent_rule", table =>
            {
                table.HasCheckConstraint(
                    "ck_respondent_rule_ordinal_positive",
                    "ordinal > 0");
                table.HasCheckConstraint(
                    "ck_respondent_rule_rule_object",
                    "jsonb_typeof(rule) = 'object'");
            });
            builder.HasKey(rule => rule.Id).HasName("pk_respondent_rule");

            builder.Property(rule => rule.Id).HasColumnName("id");
            builder.Property(rule => rule.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(rule => rule.Ordinal).HasColumnName("ordinal").IsRequired();
            builder.Property(rule => rule.Rule)
                .HasColumnName("rule")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(rule => rule.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(rule => new { rule.CampaignId, rule.Ordinal })
                .HasDatabaseName("ix_respondent_rule_campaign_id_ordinal")
                .IsUnique();

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(rule => rule.CampaignId)
                .HasConstraintName("fk_respondent_rule_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvitationToken>(builder =>
        {
            builder.ToTable("invitation_token", table =>
            {
                table.HasCheckConstraint(
                    "ck_invitation_token_channel",
                    "channel IN ('email','sms','open_link','identified_entry')");
            });
            builder.HasKey(token => token.Id).HasName("pk_invitation_token");

            builder.Property(token => token.Id).HasColumnName("id");
            builder.Property(token => token.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(token => token.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(token => token.AssignmentId).HasColumnName("assignment_id");
            builder.Property(token => token.TokenHash).HasColumnName("token_hash").IsRequired();
            builder.Property(token => token.Channel).HasColumnName("channel").HasMaxLength(64).IsRequired();
            builder.Property(token => token.Recipient).HasColumnName("recipient");
            builder.Property(token => token.ExpiresAt).HasColumnName("expires_at");
            builder.Property(token => token.UsedAt).HasColumnName("used_at");
            builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(token => token.TokenHash)
                .HasDatabaseName("ix_invitation_token_token_hash")
                .IsUnique();
            builder.HasIndex(token => token.TenantId)
                .HasDatabaseName("ix_invitation_token_tenant_id");
            builder.HasIndex(token => token.CampaignId)
                .HasDatabaseName("ix_invitation_token_campaign_id");
            builder.HasIndex(token => token.AssignmentId)
                .HasDatabaseName("ix_invitation_token_assignment_id")
                .HasFilter("assignment_id IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(token => token.TenantId)
                .HasConstraintName("fk_invitation_token_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(token => token.CampaignId)
                .HasConstraintName("fk_invitation_token_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(token => token.AssignmentId)
                .HasConstraintName("fk_invitation_token_assignment_assignment_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Assignment>(builder =>
        {
            builder.ToTable("assignment", table =>
            {
                table.HasCheckConstraint(
                    "ck_assignment_status",
                    "status IN ('pending','started','submitted','cancelled','expired')");
                table.HasCheckConstraint(
                    "ck_assignment_identity_shape",
                    "(anonymized_at IS NULL AND anonymous = FALSE AND respondent_subject_id IS NOT NULL AND invite_token_id IS NULL) OR (anonymized_at IS NULL AND anonymous = TRUE AND respondent_subject_id IS NULL AND invite_token_id IS NOT NULL) OR (anonymized_at IS NOT NULL AND target_subject_id IS NULL AND respondent_subject_id IS NULL AND invite_token_id IS NULL)");
            });
            builder.HasKey(assignment => assignment.Id).HasName("pk_assignment");

            builder.Property(assignment => assignment.Id).HasColumnName("id");
            builder.Property(assignment => assignment.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(assignment => assignment.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(assignment => assignment.TargetSubjectId).HasColumnName("target_subject_id");
            builder.Property(assignment => assignment.RespondentSubjectId).HasColumnName("respondent_subject_id");
            builder.Property(assignment => assignment.InviteTokenId).HasColumnName("invite_token_id");
            builder.Property(assignment => assignment.Role).HasColumnName("role").HasMaxLength(64).IsRequired();
            builder.Property(assignment => assignment.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(assignment => assignment.DueAt).HasColumnName("due_at");
            builder.Property(assignment => assignment.Anonymous).HasColumnName("anonymous").IsRequired();
            builder.Property(assignment => assignment.AnonymizedAt).HasColumnName("anonymized_at");
            builder.Property(assignment => assignment.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(assignment => assignment.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(assignment => assignment.TenantId)
                .HasDatabaseName("ix_assignment_tenant_id");
            builder.HasIndex(assignment => new { assignment.CampaignId, assignment.Status, assignment.DueAt })
                .HasDatabaseName("ix_assignment_campaign_id_status_due_at");
            builder.HasIndex(assignment => new
                {
                    assignment.CampaignId,
                    assignment.TargetSubjectId,
                    assignment.RespondentSubjectId
                })
                .HasDatabaseName("ix_assignment_unique_identified")
                .HasFilter("respondent_subject_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(assignment => assignment.InviteTokenId)
                .HasDatabaseName("ix_assignment_invite_token_id")
                .HasFilter("invite_token_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(assignment => assignment.TargetSubjectId)
                .HasDatabaseName("ix_assignment_target_subject_id");
            builder.HasIndex(assignment => assignment.RespondentSubjectId)
                .HasDatabaseName("ix_assignment_respondent_subject_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(assignment => assignment.TenantId)
                .HasConstraintName("fk_assignment_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(assignment => assignment.CampaignId)
                .HasConstraintName("fk_assignment_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(assignment => assignment.TargetSubjectId)
                .HasConstraintName("fk_assignment_subject_target_subject_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(assignment => assignment.RespondentSubjectId)
                .HasConstraintName("fk_assignment_subject_respondent_subject_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<InvitationToken>()
                .WithMany()
                .HasForeignKey(assignment => assignment.InviteTokenId)
                .HasConstraintName("fk_assignment_invitation_token_invite_token_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notification", table =>
            {
                table.HasCheckConstraint(
                    "ck_notification_channel",
                    "channel IN ('email','sms')");
                table.HasCheckConstraint(
                    "ck_notification_status",
                    "status IN ('queued','sent','failed','bounced')");
            });
            builder.HasKey(notification => notification.Id).HasName("pk_notification");

            builder.Property(notification => notification.Id).HasColumnName("id");
            builder.Property(notification => notification.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(notification => notification.CampaignId).HasColumnName("campaign_id").IsRequired();
            builder.Property(notification => notification.AssignmentId).HasColumnName("assignment_id").IsRequired();
            builder.Property(notification => notification.Channel).HasColumnName("channel").HasMaxLength(64).IsRequired();
            builder.Property(notification => notification.TemplateCode).HasColumnName("template_code").HasMaxLength(128).IsRequired();
            builder.Property(notification => notification.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(notification => notification.Recipient).HasColumnName("recipient").IsRequired();
            builder.Property(notification => notification.ScheduledFor).HasColumnName("scheduled_for");
            builder.Property(notification => notification.SentAt).HasColumnName("sent_at");
            builder.Property(notification => notification.Error).HasColumnName("error");
            builder.Property(notification => notification.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(notification => notification.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(notification => new { notification.TenantId, notification.CampaignId })
                .HasDatabaseName("ix_notification_tenant_id_campaign_id");
            builder.HasIndex(notification => notification.AssignmentId)
                .HasDatabaseName("ix_notification_assignment_id");
            builder.HasIndex(notification => new { notification.Status, notification.ScheduledFor })
                .HasDatabaseName("ix_notification_status_scheduled_for");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(notification => notification.TenantId)
                .HasConstraintName("fk_notification_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(notification => notification.CampaignId)
                .HasConstraintName("fk_notification_campaign_campaign_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(notification => notification.AssignmentId)
                .HasConstraintName("fk_notification_assignment_assignment_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmailSuppression>(builder =>
        {
            builder.ToTable("email_suppression");
            builder.HasKey(suppression => suppression.Id).HasName("pk_email_suppression");

            builder.Property(suppression => suppression.Id).HasColumnName("id");
            builder.Property(suppression => suppression.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(suppression => suppression.Recipient).HasColumnName("recipient").IsRequired();
            builder.Property(suppression => suppression.Reason)
                .HasColumnName("reason")
                .HasMaxLength(EmailSuppression.ReasonMaxLength)
                .IsRequired();
            builder.Property(suppression => suppression.Source)
                .HasColumnName("source")
                .HasMaxLength(EmailSuppression.SourceMaxLength)
                .IsRequired();
            builder.Property(suppression => suppression.Note)
                .HasColumnName("note")
                .HasMaxLength(EmailSuppression.NoteMaxLength);
            builder.Property(suppression => suppression.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(suppression => suppression.ReleasedAt).HasColumnName("released_at");
            builder.Property(suppression => suppression.ReleaseReason)
                .HasColumnName("release_reason")
                .HasMaxLength(EmailSuppression.ReleaseReasonMaxLength);

            builder.Ignore(suppression => suppression.Active);

            builder.HasIndex(suppression => new { suppression.TenantId, suppression.Recipient })
                .IsUnique()
                .HasFilter("released_at IS NULL")
                .HasDatabaseName("ux_email_suppression_tenant_id_recipient_active");
            builder.HasIndex(suppression => new { suppression.TenantId, suppression.CreatedAt })
                .HasDatabaseName("ix_email_suppression_tenant_id_created_at");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(suppression => suppression.TenantId)
                .HasConstraintName("fk_email_suppression_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationDeliveryAttempt>(builder =>
        {
            builder.ToTable("notification_delivery_attempt", table =>
            {
                table.HasCheckConstraint(
                    "ck_notification_delivery_attempt_status",
                    "status IN ('prepared','sent','failed')");
            });
            builder.HasKey(attempt => attempt.Id).HasName("pk_notification_delivery_attempt");

            builder.Property(attempt => attempt.Id).HasColumnName("id");
            builder.Property(attempt => attempt.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(attempt => attempt.NotificationId).HasColumnName("notification_id").IsRequired();
            builder.Property(attempt => attempt.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
            builder.Property(attempt => attempt.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(attempt => attempt.Recipient).HasColumnName("recipient").IsRequired();
            builder.Property(attempt => attempt.ProviderMessageId)
                .HasColumnName("provider_message_id")
                .HasMaxLength(256);
            builder.Property(attempt => attempt.ProviderDeliveryKey)
                .HasColumnName("provider_delivery_key")
                .HasMaxLength(128);
            builder.Property(attempt => attempt.Error).HasColumnName("error");
            builder.Property(attempt => attempt.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasIndex(attempt => new { attempt.TenantId, attempt.NotificationId })
                .HasDatabaseName("ix_notification_delivery_attempt_tenant_id_notification_id");
            builder.HasIndex(attempt => new { attempt.NotificationId, attempt.CreatedAt })
                .HasDatabaseName("ix_notification_delivery_attempt_notification_id_created_at");
            builder.HasIndex(attempt => new { attempt.TenantId, attempt.ProviderDeliveryKey })
                .HasDatabaseName("ux_notification_delivery_attempt_tenant_provider_delivery_key")
                .IsUnique()
                .HasFilter("provider_delivery_key IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(attempt => attempt.TenantId)
                .HasConstraintName("fk_notification_delivery_attempt_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Notification>()
                .WithMany()
                .HasForeignKey(attempt => attempt.NotificationId)
                .HasConstraintName("fk_notification_delivery_attempt_notification_notification_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationDeliveryEvent>(builder =>
        {
            builder.ToTable("notification_delivery_event", table =>
            {
                table.HasCheckConstraint(
                    "ck_notification_delivery_event_type",
                    "event_type IN ('accepted','delivered','bounced','complained')");
            });
            builder.HasKey(deliveryEvent => deliveryEvent.Id).HasName("pk_notification_delivery_event");

            builder.Property(deliveryEvent => deliveryEvent.Id).HasColumnName("id");
            builder.Property(deliveryEvent => deliveryEvent.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.NotificationId).HasColumnName("notification_id").IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.DeliveryAttemptId).HasColumnName("delivery_attempt_id").IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.EventType).HasColumnName("event_type").HasMaxLength(NotificationDeliveryEvent.EventTypeMaxLength).IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.ProviderEventId)
                .HasColumnName("provider_event_id")
                .HasMaxLength(NotificationDeliveryEvent.ProviderEventIdMaxLength);
            builder.Property(deliveryEvent => deliveryEvent.ProviderMessageId)
                .HasColumnName("provider_message_id")
                .HasMaxLength(256);
            builder.Property(deliveryEvent => deliveryEvent.Reason)
                .HasColumnName("reason")
                .HasMaxLength(NotificationDeliveryEvent.ReasonMaxLength);
            builder.Property(deliveryEvent => deliveryEvent.OccurredAt).HasColumnName("occurred_at").IsRequired();
            builder.Property(deliveryEvent => deliveryEvent.ReceivedAt).HasColumnName("received_at").IsRequired();

            builder.HasIndex(deliveryEvent => new { deliveryEvent.TenantId, deliveryEvent.NotificationId })
                .HasDatabaseName("ix_notification_delivery_event_tenant_id_notification_id");
            builder.HasIndex(deliveryEvent => new { deliveryEvent.TenantId, deliveryEvent.DeliveryAttemptId })
                .HasDatabaseName("ix_notification_delivery_event_tenant_id_delivery_attempt_id");
            builder.HasIndex(deliveryEvent => new { deliveryEvent.TenantId, deliveryEvent.Provider, deliveryEvent.ProviderEventId })
                .IsUnique()
                .HasFilter("provider_event_id IS NOT NULL")
                .HasDatabaseName("ux_notification_delivery_event_tenant_provider_event_id");
            builder.HasIndex(deliveryEvent => new { deliveryEvent.TenantId, deliveryEvent.DeliveryAttemptId, deliveryEvent.EventType })
                .IsUnique()
                .HasFilter("provider_event_id IS NULL")
                .HasDatabaseName("ux_notification_delivery_event_tenant_attempt_type_without_provider_id");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(deliveryEvent => deliveryEvent.TenantId)
                .HasConstraintName("fk_notification_delivery_event_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Notification>()
                .WithMany()
                .HasForeignKey(deliveryEvent => deliveryEvent.NotificationId)
                .HasConstraintName("fk_notification_delivery_event_notification_notification_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<NotificationDeliveryAttempt>()
                .WithMany()
                .HasForeignKey(deliveryEvent => deliveryEvent.DeliveryAttemptId)
                .HasConstraintName("fk_notification_delivery_event_attempt_delivery_attempt_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationalNotification>(builder =>
        {
            builder.ToTable("operational_notification", table =>
            {
                table.HasCheckConstraint(
                    "ck_operational_notification_status",
                    "status IN ('unread','read')");
                table.HasCheckConstraint(
                    "ck_operational_notification_severity",
                    "severity IN ('info','warning')");
                table.HasCheckConstraint(
                    "ck_operational_notification_payload_object",
                    "jsonb_typeof(payload_json) = 'object'");
            });
            builder.HasKey(notification => notification.Id).HasName("pk_operational_notification");

            builder.Property(notification => notification.Id).HasColumnName("id");
            builder.Property(notification => notification.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(notification => notification.NotificationType)
                .HasColumnName("notification_type")
                .HasMaxLength(128)
                .IsRequired();
            builder.Property(notification => notification.Severity).HasColumnName("severity").HasMaxLength(32).IsRequired();
            builder.Property(notification => notification.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            builder.Property(notification => notification.SourceAggregateId)
                .HasColumnName("source_aggregate_id")
                .IsRequired();
            builder.Property(notification => notification.SourceAggregateType)
                .HasColumnName("source_aggregate_type")
                .HasMaxLength(128)
                .IsRequired();
            builder.Property(notification => notification.SourceEventType)
                .HasColumnName("source_event_type")
                .HasMaxLength(128)
                .IsRequired();
            builder.Property(notification => notification.PayloadJson)
                .HasColumnName("payload_json")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(notification => notification.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(notification => notification.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(notification => notification.ReadAt).HasColumnName("read_at");

            builder.HasIndex(notification => new
                {
                    notification.TenantId,
                    notification.SourceAggregateId,
                    notification.SourceEventType,
                    notification.NotificationType
                })
                .IsUnique()
                .HasDatabaseName("ux_operational_notification_source");
            builder.HasIndex(notification => new
                {
                    notification.TenantId,
                    notification.Status,
                    notification.CreatedAt
                })
                .HasDatabaseName("ix_operational_notification_tenant_id_status_created_at");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(notification => notification.TenantId)
                .HasConstraintName("fk_operational_notification_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ResponseSession>(builder =>
        {
            builder.ToTable("response_session", table =>
            {
                table.HasCheckConstraint(
                    "ck_response_session_time_taken_non_negative",
                    "time_taken_ms IS NULL OR time_taken_ms >= 0");
                table.HasCheckConstraint(
                    "ck_response_session_submitted_after_started",
                    "started_at IS NULL OR submitted_at IS NULL OR submitted_at >= started_at");
                table.HasCheckConstraint(
                    "ck_response_session_public_handle_hash",
                    "public_handle_hash IS NULL OR public_handle_hash ~ '^[0-9a-f]{64}$'");
            });
            builder.HasKey(session => session.Id).HasName("pk_response_session");

            builder.Property(session => session.Id).HasColumnName("id");
            builder.Property(session => session.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(session => session.AssignmentId).HasColumnName("assignment_id").IsRequired();
            builder.Property(session => session.ParticipantCodeId).HasColumnName("participant_code_id");
            builder.Property(session => session.ConsentRecordId).HasColumnName("consent_record_id");
            builder.Property(session => session.StartedAt).HasColumnName("started_at");
            builder.Property(session => session.SubmittedAt).HasColumnName("submitted_at");
            builder.Property(session => session.TimeTakenMs).HasColumnName("time_taken_ms");
            builder.Property(session => session.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(session => session.PublicHandleHash)
                .HasColumnName("public_handle_hash")
                .HasColumnType("character(64)")
                .HasMaxLength(64)
                .IsFixedLength();
            builder.Property(session => session.PublicHandleIssuedAt).HasColumnName("public_handle_issued_at");
            builder.Property(session => session.IpHash).HasColumnName("ip_hash");
            builder.Property(session => session.UserAgentHash).HasColumnName("user_agent_hash");
            builder.Property(session => session.AnonymizedAt).HasColumnName("anonymized_at");
            builder.Property(session => session.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(session => session.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(session => session.TenantId)
                .HasDatabaseName("ix_response_session_tenant_id");
            builder.HasIndex(session => session.AssignmentId)
                .HasDatabaseName("ix_response_session_assignment_id");
            builder.HasIndex(session => session.ParticipantCodeId)
                .HasDatabaseName("ix_response_session_participant_code_id")
                .HasFilter("participant_code_id IS NOT NULL");
            builder.HasIndex(session => session.ConsentRecordId)
                .HasDatabaseName("ix_response_session_consent_record_id")
                .HasFilter("consent_record_id IS NOT NULL");
            builder.HasIndex(session => session.PublicHandleHash)
                .HasDatabaseName("ix_response_session_public_handle_hash")
                .HasFilter("public_handle_hash IS NOT NULL")
                .IsUnique();
            builder.HasIndex(session => session.SubmittedAt)
                .HasDatabaseName("ix_response_session_submitted_at")
                .HasFilter("submitted_at IS NOT NULL");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(session => session.TenantId)
                .HasConstraintName("fk_response_session_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(session => session.AssignmentId)
                .HasConstraintName("fk_response_session_assignment_assignment_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ConsentRecord>()
                .WithMany()
                .HasForeignKey(session => session.ConsentRecordId)
                .HasConstraintName("fk_response_session_consent_record_consent_record_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ParticipantCode>()
                .WithMany()
                .HasForeignKey(session => session.ParticipantCodeId)
                .HasConstraintName("fk_response_session_participant_code_participant_code_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Answer>(builder =>
        {
            builder.ToTable("answer", table =>
            {
                table.HasCheckConstraint(
                    "ck_answer_not_skipped_and_na",
                    "NOT (is_skipped = TRUE AND is_na = TRUE)");
                table.HasCheckConstraint(
                    "ck_answer_skipped_na_payload_shape",
                    "NOT (is_skipped = TRUE OR is_na = TRUE) OR (value IS NULL AND NULLIF(BTRIM(COALESCE(comment, '')), '') IS NULL)");
                table.HasCheckConstraint(
                    "ck_answer_value_json",
                    "value IS NULL OR jsonb_typeof(value) IS NOT NULL");
            });
            builder.HasKey(answer => answer.Id).HasName("pk_answer");

            builder.Property(answer => answer.Id).HasColumnName("id");
            builder.Property(answer => answer.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(answer => answer.SessionId).HasColumnName("session_id").IsRequired();
            builder.Property(answer => answer.QuestionId).HasColumnName("question_id").IsRequired();
            builder.Property(answer => answer.Value)
                .HasColumnName("value")
                .HasColumnType("jsonb");
            builder.Property(answer => answer.Comment).HasColumnName("comment");
            builder.Property(answer => answer.IsSkipped).HasColumnName("is_skipped").IsRequired();
            builder.Property(answer => answer.IsNa).HasColumnName("is_na").IsRequired();
            builder.Property(answer => answer.AnsweredAt).HasColumnName("answered_at").IsRequired();

            builder.HasIndex(answer => answer.TenantId)
                .HasDatabaseName("ix_answer_tenant_id");
            builder.HasIndex(answer => answer.SessionId)
                .HasDatabaseName("ix_answer_session_id");
            builder.HasIndex(answer => new { answer.SessionId, answer.QuestionId })
                .HasDatabaseName("ix_answer_session_id_question_id")
                .IsUnique();
            builder.HasIndex(answer => new { answer.QuestionId, answer.AnsweredAt })
                .HasDatabaseName("ix_answer_question_id_answered_at");

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(answer => answer.TenantId)
                .HasConstraintName("fk_answer_tenant_tenant_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ResponseSession>()
                .WithMany()
                .HasForeignKey(answer => answer.SessionId)
                .HasConstraintName("fk_answer_response_session_session_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateQuestion>()
                .WithMany()
                .HasForeignKey(answer => answer.QuestionId)
                .HasConstraintName("fk_answer_question_question_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateSection>(builder =>
        {
            builder.ToTable("section", table =>
            {
                table.HasCheckConstraint(
                    "ck_section_ordinal_positive",
                    "ordinal > 0");
            });
            builder.HasKey(section => section.Id).HasName("pk_section");
            builder.HasAlternateKey(section => new { section.Id, section.TemplateVersionId })
                .HasName("ak_section_id_template_version_id");

            builder.Property(section => section.Id).HasColumnName("id");
            builder.Property(section => section.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(section => section.ParentSectionId).HasColumnName("parent_section_id");
            builder.Property(section => section.Ordinal).HasColumnName("ordinal").IsRequired();
            builder.Property(section => section.Code).HasColumnName("code").HasMaxLength(128);
            builder.Property(section => section.TitleDefault).HasColumnName("title_default").HasMaxLength(512).IsRequired();

            builder.HasIndex(section => new { section.TemplateVersionId, section.ParentSectionId, section.Ordinal })
                .HasDatabaseName("ix_section_template_version_id_parent_section_id_ordinal");
            builder.HasIndex(section => new { section.TemplateVersionId, section.Ordinal })
                .HasDatabaseName("ix_section_template_version_id_ordinal")
                .IsUnique();
            builder.HasIndex(section => new { section.TemplateVersionId, section.Code })
                .HasDatabaseName("ix_section_template_version_id_code")
                .HasFilter("code IS NOT NULL")
                .IsUnique();
            builder.HasIndex(section => new { section.ParentSectionId, section.TemplateVersionId })
                .HasDatabaseName("ix_section_parent_section_id_template_version_id");

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(section => section.TemplateVersionId)
                .HasConstraintName("fk_section_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateSection>()
                .WithMany()
                .HasForeignKey(section => new { section.ParentSectionId, section.TemplateVersionId })
                .HasPrincipalKey(section => new { section.Id, section.TemplateVersionId })
                .HasConstraintName("fk_section_section_parent_section_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionScale>(builder =>
        {
            builder.ToTable("scale", table =>
            {
                table.HasCheckConstraint(
                    "ck_scale_type",
                    "type IN ('likert','nps','binary','numeric')");
                table.HasCheckConstraint(
                    "ck_scale_range",
                    "max_value > min_value");
                table.HasCheckConstraint(
                    "ck_scale_step_positive",
                    "step > 0");
            });
            builder.HasKey(scale => scale.Id).HasName("pk_scale");
            builder.HasAlternateKey(scale => new { scale.Id, scale.TemplateVersionId })
                .HasName("ak_scale_id_template_version_id");

            builder.Property(scale => scale.Id).HasColumnName("id");
            builder.Property(scale => scale.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(scale => scale.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            builder.Property(scale => scale.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
            builder.Property(scale => scale.MinValue).HasColumnName("min_value").IsRequired();
            builder.Property(scale => scale.MaxValue).HasColumnName("max_value").IsRequired();
            builder.Property(scale => scale.Step).HasColumnName("step").IsRequired();
            builder.Property(scale => scale.NaAllowed).HasColumnName("na_allowed").IsRequired();
            builder.Property(scale => scale.Anchors)
                .HasColumnName("anchors")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.HasIndex(scale => new { scale.TemplateVersionId, scale.Code })
                .HasDatabaseName("ix_scale_template_version_id_code")
                .IsUnique();

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(scale => scale.TemplateVersionId)
                .HasConstraintName("fk_scale_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateQuestion>(builder =>
        {
            builder.ToTable("question", table =>
            {
                table.HasCheckConstraint(
                    "ck_question_ordinal_positive",
                    "ordinal > 0");
                table.HasCheckConstraint(
                    "ck_question_type",
                    "type IN ('likert','single','multi','text','number','date','matrix','nps','ranking','file','pairwise')");
                table.HasCheckConstraint(
                    "ck_question_scale_backed",
                    "type NOT IN ('likert','nps') OR scale_id IS NOT NULL");
                table.HasCheckConstraint(
                    "ck_question_scale_only_for_scale_backed",
                    "type IN ('likert','nps') OR scale_id IS NULL");
                table.HasCheckConstraint(
                    "ck_question_weight_positive",
                    "weight > 0");
                table.HasCheckConstraint(
                    "ck_question_measurement_level",
                    "measurement_level IS NULL OR measurement_level IN ('nominal','ordinal','scale')");
            });
            builder.HasKey(question => question.Id).HasName("pk_question");

            builder.Property(question => question.Id).HasColumnName("id");
            builder.Property(question => question.TemplateVersionId).HasColumnName("template_version_id").IsRequired();
            builder.Property(question => question.SectionId).HasColumnName("section_id").IsRequired();
            builder.Property(question => question.Ordinal).HasColumnName("ordinal").IsRequired();
            builder.Property(question => question.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            builder.Property(question => question.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
            builder.Property(question => question.ScaleId).HasColumnName("scale_id");
            builder.Property(question => question.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Property(question => question.TextDefault).HasColumnName("text_default").IsRequired();
            builder.Property(question => question.DescriptionDefault).HasColumnName("description_default");
            builder.Property(question => question.Required).HasColumnName("required").IsRequired();
            builder.Property(question => question.ReverseCoded).HasColumnName("reverse_coded").IsRequired();
            builder.Property(question => question.Weight).HasColumnName("weight").HasPrecision(6, 4).IsRequired();
            builder.Property(question => question.VariableLabel).HasColumnName("variable_label");
            builder.Property(question => question.MeasurementLevel).HasColumnName("measurement_level").HasMaxLength(64);
            builder.Property(question => question.MissingCodes)
                .HasColumnName("missing_codes")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.HasIndex(question => new { question.TemplateVersionId, question.SectionId, question.Ordinal })
                .HasDatabaseName("ix_question_template_version_id_section_id_ordinal");
            builder.HasIndex(question => new { question.TemplateVersionId, question.Ordinal })
                .HasDatabaseName("ix_question_template_version_id_ordinal")
                .IsUnique();
            builder.HasIndex(question => new { question.TemplateVersionId, question.Code })
                .HasDatabaseName("ix_question_template_version_id_code")
                .HasFilter("code IS NOT NULL")
                .IsUnique();
            builder.HasIndex(question => new { question.SectionId, question.TemplateVersionId })
                .HasDatabaseName("ix_question_section_id_template_version_id");
            builder.HasIndex(question => new { question.ScaleId, question.TemplateVersionId })
                .HasDatabaseName("ix_question_scale_id_template_version_id");

            builder.HasOne<TemplateVersion>()
                .WithMany()
                .HasForeignKey(question => question.TemplateVersionId)
                .HasConstraintName("fk_question_template_version_template_version_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateSection>()
                .WithMany()
                .HasForeignKey(question => new { question.SectionId, question.TemplateVersionId })
                .HasPrincipalKey(section => new { section.Id, section.TemplateVersionId })
                .HasConstraintName("fk_question_section_section_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<QuestionScale>()
                .WithMany()
                .HasForeignKey(question => new { question.ScaleId, question.TemplateVersionId })
                .HasPrincipalKey(scale => new { scale.Id, scale.TemplateVersionId })
                .HasConstraintName("fk_question_scale_scale_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChoiceOption>(builder =>
        {
            builder.ToTable("choice_option", table =>
            {
                table.HasCheckConstraint(
                    "ck_choice_option_ordinal_positive",
                    "ordinal > 0");
            });
            builder.HasKey(choice => choice.Id).HasName("pk_choice_option");

            builder.Property(choice => choice.Id).HasColumnName("id");
            builder.Property(choice => choice.QuestionId).HasColumnName("question_id").IsRequired();
            builder.Property(choice => choice.Ordinal).HasColumnName("ordinal").IsRequired();
            builder.Property(choice => choice.Value).HasColumnName("value").HasMaxLength(128).IsRequired();
            builder.Property(choice => choice.LabelDefault).HasColumnName("label_default").HasMaxLength(512).IsRequired();
            builder.Property(choice => choice.IsOther).HasColumnName("is_other").IsRequired();
            builder.Property(choice => choice.IsExclusive).HasColumnName("is_exclusive").IsRequired();

            builder.HasIndex(choice => new { choice.QuestionId, choice.Ordinal })
                .HasDatabaseName("ix_choice_option_question_id_ordinal")
                .IsUnique();
            builder.HasIndex(choice => new { choice.QuestionId, choice.Value })
                .HasDatabaseName("ix_choice_option_question_id_value")
                .IsUnique();

            builder.HasOne<TemplateQuestion>()
                .WithMany()
                .HasForeignKey(choice => choice.QuestionId)
                .HasConstraintName("fk_choice_option_question_question_id")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstrumentTranslation>(builder =>
        {
            builder.ToTable("translation", table =>
            {
                table.HasCheckConstraint(
                    "ck_translation_exactly_one_target",
                    "((instrument_id IS NOT NULL)::int\n"
                    + "+ (instrument_subscale_id IS NOT NULL)::int\n"
                    + "+ (instrument_item_id IS NOT NULL)::int\n"
                    + "+ (survey_template_id IS NOT NULL)::int\n"
                    + "+ (template_section_id IS NOT NULL)::int\n"
                    + "+ (template_question_id IS NOT NULL)::int\n"
                    + "+ (choice_option_id IS NOT NULL)::int) = 1");
                table.HasCheckConstraint(
                    "ck_translation_status",
                    "status IN ('draft_translation','back_translated','reconciled','approved_canonical_equivalent','approved_derivative','rejected')");
            });
            builder.HasKey(translation => translation.Id).HasName("pk_translation");

            builder.Property(translation => translation.Id).HasColumnName("id");
            builder.Property(translation => translation.InstrumentId).HasColumnName("instrument_id");
            builder.Property(translation => translation.InstrumentSubscaleId).HasColumnName("instrument_subscale_id");
            builder.Property(translation => translation.InstrumentItemId).HasColumnName("instrument_item_id");
            builder.Property(translation => translation.SurveyTemplateId).HasColumnName("survey_template_id");
            builder.Property(translation => translation.TemplateSectionId).HasColumnName("template_section_id");
            builder.Property(translation => translation.TemplateQuestionId).HasColumnName("template_question_id");
            builder.Property(translation => translation.ChoiceOptionId).HasColumnName("choice_option_id");
            builder.Property(translation => translation.Field).HasColumnName("field").HasMaxLength(128).IsRequired();
            builder.Property(translation => translation.Locale).HasColumnName("locale").HasMaxLength(16).IsRequired();
            builder.Property(translation => translation.Text).HasColumnName("text").IsRequired();
            builder.Property(translation => translation.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            builder.Property(translation => translation.WorkflowId).HasColumnName("translation_workflow_id");
            builder.Property(translation => translation.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(translation => translation.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(translation => new { translation.InstrumentId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_instrument")
                .HasFilter("instrument_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.InstrumentSubscaleId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_instrument_subscale")
                .HasFilter("instrument_subscale_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.InstrumentItemId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_instrument_item")
                .HasFilter("instrument_item_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.SurveyTemplateId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_survey_template")
                .HasFilter("survey_template_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.TemplateSectionId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_template_section")
                .HasFilter("template_section_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.TemplateQuestionId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_template_question")
                .HasFilter("template_question_id IS NOT NULL")
                .IsUnique();
            builder.HasIndex(translation => new { translation.ChoiceOptionId, translation.Field, translation.Locale })
                .HasDatabaseName("ix_translation_unique_choice_option")
                .HasFilter("choice_option_id IS NOT NULL")
                .IsUnique();

            builder.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(translation => translation.InstrumentId)
                .HasConstraintName("fk_translation_instrument_instrument_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<InstrumentSubscale>()
                .WithMany()
                .HasForeignKey(translation => translation.InstrumentSubscaleId)
                .HasConstraintName("fk_translation_instrument_subscale_instrument_subscale_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<InstrumentItem>()
                .WithMany()
                .HasForeignKey(translation => translation.InstrumentItemId)
                .HasConstraintName("fk_translation_instrument_item_instrument_item_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<SurveyTemplate>()
                .WithMany()
                .HasForeignKey(translation => translation.SurveyTemplateId)
                .HasConstraintName("fk_translation_survey_template_survey_template_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateSection>()
                .WithMany()
                .HasForeignKey(translation => translation.TemplateSectionId)
                .HasConstraintName("fk_translation_section_template_section_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<TemplateQuestion>()
                .WithMany()
                .HasForeignKey(translation => translation.TemplateQuestionId)
                .HasConstraintName("fk_translation_question_template_question_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ChoiceOption>()
                .WithMany()
                .HasForeignKey(translation => translation.ChoiceOptionId)
                .HasConstraintName("fk_translation_choice_option_choice_option_id")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
