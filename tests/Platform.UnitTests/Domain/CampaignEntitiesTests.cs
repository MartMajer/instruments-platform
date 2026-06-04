using Platform.Domain.Campaigns;

namespace Platform.UnitTests.Domain;

public sealed class CampaignEntitiesTests
{
    [Fact]
    public void Campaign_series_requires_32_byte_code_salt()
    {
        Assert.Throws<ArgumentException>(() => new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            [1, 2, 3]));
    }

    [Fact]
    public void Campaign_series_defaults_to_own_study_metadata()
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            new byte[32]);

        Assert.Equal(CampaignSeriesStudyKinds.Own, series.StudyKind);
        Assert.False(series.IsSample);
        Assert.Null(series.SampleScenario);
    }

    [Fact]
    public void Campaign_series_accepts_known_sample_scenario_metadata()
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Starter sample",
            new byte[32],
            studyKind: CampaignSeriesStudyKinds.Sample,
            sampleScenario: CampaignSeriesSampleScenarios.MixedLifecycle);

        Assert.Equal(CampaignSeriesStudyKinds.Sample, series.StudyKind);
        Assert.True(series.IsSample);
        Assert.Equal(CampaignSeriesSampleScenarios.MixedLifecycle, series.SampleScenario);
    }

    [Theory]
    [InlineData("unknown", null)]
    [InlineData(CampaignSeriesStudyKinds.Own, CampaignSeriesSampleScenarios.MixedLifecycle)]
    [InlineData(CampaignSeriesStudyKinds.Sample, null)]
    [InlineData(CampaignSeriesStudyKinds.Sample, "unknown")]
    public void Campaign_series_rejects_invalid_study_metadata(string studyKind, string? sampleScenario)
    {
        Assert.Throws<ArgumentException>(() => new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            new byte[32],
            studyKind: studyKind,
            sampleScenario: sampleScenario));
    }

    [Fact]
    public void Campaign_series_rename_normalizes_name_and_updates_timestamp()
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            new byte[32]);
        var renamedAt = DateTimeOffset.Parse("2026-05-09T12:00:00+00:00");

        series.Rename("  Renamed pulse  ", renamedAt);

        Assert.Equal("Renamed pulse", series.Name);
        Assert.Equal(renamedAt, series.UpdatedAt);
        Assert.Throws<ArgumentException>(() => series.Rename("   ", renamedAt));
        Assert.Throws<ArgumentException>(() => series.Rename(new string('x', 257), renamedAt));
    }

    [Fact]
    public void Campaign_series_archive_sets_metadata_and_updates_timestamp()
    {
        var actorUserId = Guid.NewGuid();
        var archivedAt = DateTimeOffset.Parse("2026-05-11T12:00:00+00:00");
        var series = new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            new byte[32]);

        series.Archive("  Smoke run complete  ", actorUserId, archivedAt);

        Assert.True(series.Archived);
        Assert.Equal(archivedAt, series.ArchivedAt);
        Assert.Equal(actorUserId, series.ArchivedByUserId);
        Assert.Equal("Smoke run complete", series.ArchiveReason);
        Assert.Equal(archivedAt, series.UpdatedAt);
    }

    [Fact]
    public void Campaign_series_restore_clears_archive_metadata_and_is_idempotent()
    {
        var series = new CampaignSeries(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pulse 2026",
            new byte[32]);
        var archivedAt = DateTimeOffset.Parse("2026-05-11T12:00:00+00:00");
        var restoredAt = DateTimeOffset.Parse("2026-05-11T13:00:00+00:00");

        series.Restore(restoredAt);
        Assert.False(series.Archived);

        series.Archive(new string('x', 257), Guid.NewGuid(), archivedAt);
        Assert.Equal(new string('x', 256), series.ArchiveReason);

        series.Restore(restoredAt);

        Assert.False(series.Archived);
        Assert.Null(series.ArchivedAt);
        Assert.Null(series.ArchivedByUserId);
        Assert.Null(series.ArchiveReason);
        Assert.Equal(restoredAt, series.UpdatedAt);
    }

    [Fact]
    public void Campaign_uses_explicit_response_identity_mode()
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.AnonymousLongitudinal,
            schedule: """{"kind":"one_shot"}""");

        Assert.Equal(CampaignStatuses.Draft, campaign.Status);
        Assert.Equal(ResponseIdentityModes.AnonymousLongitudinal, campaign.ResponseIdentityMode);
        Assert.Equal("""{"kind":"one_shot"}""", campaign.Schedule);
    }

    [Fact]
    public void Campaign_rejects_unknown_identity_mode()
    {
        Assert.Throws<ArgumentException>(() => new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            "anonymous-ish"));
    }

    [Fact]
    public void Campaign_rejects_end_at_before_start_at()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.Identified,
            startAt: DateTimeOffset.Parse("2026-05-07T00:00:00+00:00"),
            endAt: DateTimeOffset.Parse("2026-05-06T00:00:00+00:00")));
    }

    [Fact]
    public void Campaign_launch_moves_draft_campaign_live_and_sets_start_when_missing()
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.Anonymous);
        var launchedAt = DateTimeOffset.Parse("2026-05-07T10:15:00+00:00");

        campaign.Launch(launchedAt);

        Assert.Equal(CampaignStatuses.Live, campaign.Status);
        Assert.Equal(launchedAt, campaign.StartAt);
        Assert.Equal(launchedAt, campaign.UpdatedAt);
    }

    [Fact]
    public void Campaign_launch_rejects_non_launchable_campaign()
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.Anonymous,
            status: CampaignStatuses.Live,
            startAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"));

        Assert.Throws<InvalidOperationException>(() =>
            campaign.Launch(DateTimeOffset.Parse("2026-05-07T10:20:00+00:00")));
    }

    [Fact]
    public void Campaign_close_moves_live_campaign_closed_and_sets_provenance()
    {
        var actorUserId = Guid.NewGuid();
        var closedAt = DateTimeOffset.Parse("2026-05-11T14:30:00+00:00");
        var campaign = new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.Anonymous,
            status: CampaignStatuses.Live,
            startAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"));

        campaign.Close("  Collection complete  ", actorUserId, closedAt);

        Assert.Equal(CampaignStatuses.Closed, campaign.Status);
        Assert.Equal(closedAt, campaign.ClosedAt);
        Assert.Equal(actorUserId, campaign.ClosedByUserId);
        Assert.Equal("Collection complete", campaign.CloseReason);
        Assert.Equal(closedAt, campaign.UpdatedAt);
    }

    [Fact]
    public void Campaign_close_rejects_non_live_campaigns()
    {
        var campaign = new Campaign(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "May pulse",
            ResponseIdentityModes.Anonymous);

        Assert.Throws<InvalidOperationException>(() =>
            campaign.Close("Done", Guid.NewGuid(), DateTimeOffset.Parse("2026-05-11T14:30:00+00:00")));
    }

    [Fact]
    public void Campaign_launch_snapshot_requires_valid_freeze_data()
    {
        Assert.Throws<ArgumentException>(() => new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            campaignSeriesId: null,
            templateVersionId: Guid.NewGuid(),
            scoringRuleId: Guid.NewGuid(),
            responseIdentityMode: "anonymous-ish",
            defaultLocale: "en",
            templateQuestionCount: 3,
            scoringRuleDocumentHash: "hash",
            launchReadiness: "{}",
            launchedAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00")));

        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            campaignSeriesId: null,
            templateVersionId: Guid.NewGuid(),
            scoringRuleId: Guid.NewGuid(),
            responseIdentityMode: ResponseIdentityModes.Anonymous,
            defaultLocale: "en",
            templateQuestionCount: 0,
            scoringRuleDocumentHash: "hash",
            launchReadiness: "{}",
            launchedAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00")));

        Assert.Throws<ArgumentException>(() => new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            campaignSeriesId: null,
            templateVersionId: Guid.NewGuid(),
            scoringRuleId: Guid.NewGuid(),
            responseIdentityMode: ResponseIdentityModes.Anonymous,
            defaultLocale: "en",
            templateQuestionCount: 3,
            scoringRuleDocumentHash: "hash",
            launchReadiness: "[]",
            launchedAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00")));

        Assert.Throws<ArgumentException>(() => new CampaignLaunchSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            campaignSeriesId: null,
            templateVersionId: Guid.NewGuid(),
            scoringRuleId: Guid.NewGuid(),
            responseIdentityMode: ResponseIdentityModes.Anonymous,
            defaultLocale: "en",
            templateQuestionCount: 3,
            scoringRuleDocumentHash: "hash",
            launchReadiness: "{}",
            launchedAt: DateTimeOffset.Parse("2026-05-07T10:15:00+00:00"),
            launchPacket: "[]"));
    }

    [Fact]
    public void Campaign_launch_snapshot_stores_freeze_data()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var templateVersionId = Guid.NewGuid();
        var scoringRuleId = Guid.NewGuid();
        var launchedBy = Guid.NewGuid();
        var launchedAt = DateTimeOffset.Parse("2026-05-07T10:15:00+00:00");

        var snapshot = new CampaignLaunchSnapshot(
            id,
            tenantId,
            campaignId,
            seriesId,
            templateVersionId,
            scoringRuleId,
            ResponseIdentityModes.Anonymous,
            "en",
            templateQuestionCount: 3,
            scoringRuleDocumentHash: "hash",
            launchReadiness: """{"ready":true}""",
            launchedAt,
            launchedBy,
            launchPacket: """{"schema_version":1,"identity":{"mode":"anonymous"}}""");

        Assert.Equal(id, snapshot.Id);
        Assert.Equal(tenantId, snapshot.TenantId);
        Assert.Equal(campaignId, snapshot.CampaignId);
        Assert.Equal(seriesId, snapshot.CampaignSeriesId);
        Assert.Equal(templateVersionId, snapshot.TemplateVersionId);
        Assert.Equal(scoringRuleId, snapshot.ScoringRuleId);
        Assert.Equal(ResponseIdentityModes.Anonymous, snapshot.ResponseIdentityMode);
        Assert.Equal("en", snapshot.DefaultLocale);
        Assert.Equal(3, snapshot.TemplateQuestionCount);
        Assert.Equal("hash", snapshot.ScoringRuleDocumentHash);
        Assert.Equal("""{"ready":true}""", snapshot.LaunchReadiness);
        Assert.Equal("""{"schema_version":1,"identity":{"mode":"anonymous"}}""", snapshot.LaunchPacket);
        Assert.Equal(launchedAt, snapshot.LaunchedAt);
        Assert.Equal(launchedBy, snapshot.LaunchedBy);
    }

    [Fact]
    public void Respondent_rule_requires_positive_ordinal_and_object_rule()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RespondentRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            """{"kind":"self"}"""));

        Assert.Throws<ArgumentException>(() => new RespondentRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "[]"));
    }

    [Fact]
    public void Assignment_shapes_match_identity_mode()
    {
        var identified = Assignment.CreateIdentified(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "self",
            respondentSubjectId: Guid.NewGuid(),
            targetSubjectId: Guid.NewGuid());

        var anonymous = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employee",
            inviteTokenId: Guid.NewGuid());

        Assert.False(identified.Anonymous);
        Assert.NotNull(identified.RespondentSubjectId);
        Assert.Null(identified.InviteTokenId);
        Assert.True(anonymous.Anonymous);
        Assert.Null(anonymous.RespondentSubjectId);
        Assert.NotNull(anonymous.InviteTokenId);
    }

    [Fact]
    public void Campaign_identity_mode_accepts_only_compatible_assignments()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var identifiedCampaign = new Campaign(
            campaignId,
            tenantId,
            Guid.NewGuid(),
            "Identified pulse",
            ResponseIdentityModes.Identified);
        var anonymousCampaign = new Campaign(
            campaignId,
            tenantId,
            Guid.NewGuid(),
            "Anonymous pulse",
            ResponseIdentityModes.Anonymous);

        var identifiedAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "self",
            Guid.NewGuid());
        var anonymousAssignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "self",
            Guid.NewGuid());

        Assert.True(identifiedCampaign.CanAcceptAssignment(identifiedAssignment));
        Assert.False(identifiedCampaign.CanAcceptAssignment(anonymousAssignment));
        Assert.True(anonymousCampaign.CanAcceptAssignment(anonymousAssignment));
        Assert.False(anonymousCampaign.CanAcceptAssignment(identifiedAssignment));
    }

    [Fact]
    public void Anonymous_longitudinal_requires_anonymous_assignment_and_later_participant_code()
    {
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign(
            campaignId,
            tenantId,
            Guid.NewGuid(),
            "Longitudinal pulse",
            ResponseIdentityModes.AnonymousLongitudinal);
        var anonymousAssignment = Assignment.CreateAnonymous(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "self",
            Guid.NewGuid());
        var identifiedAssignment = Assignment.CreateIdentified(
            Guid.NewGuid(),
            tenantId,
            campaignId,
            "self",
            Guid.NewGuid());

        Assert.True(campaign.CanAcceptAssignment(anonymousAssignment));
        Assert.False(campaign.CanAcceptAssignment(identifiedAssignment));
        Assert.True(campaign.RequiresParticipantCodeAtSession);
    }

    [Fact]
    public void Invitation_token_stores_hash_and_channel_only()
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            InvitationTokenChannels.Email,
            recipient: "researcher@example.com");

        Assert.Equal("token-hash", token.TokenHash);
        Assert.Equal(InvitationTokenChannels.Email, token.Channel);
        Assert.Equal("researcher@example.com", token.Recipient);
    }

    [Fact]
    public void Identified_queue_is_a_known_invitation_token_channel()
    {
        Assert.True(InvitationTokenChannels.IsKnown(InvitationTokenChannels.IdentifiedQueue));
    }

    [Fact]
    public void Identified_queue_invitation_token_requires_respondent_subject_id()
    {
        Assert.Throws<ArgumentException>(() => new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            InvitationTokenChannels.IdentifiedQueue));
    }

    [Fact]
    public void Identified_queue_invitation_token_stores_respondent_subject_id()
    {
        var respondentSubjectId = Guid.NewGuid();

        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            InvitationTokenChannels.IdentifiedQueue,
            respondentSubjectId: respondentSubjectId);

        Assert.Equal(respondentSubjectId, token.RespondentSubjectId);
        Assert.Equal(InvitationTokenChannels.IdentifiedQueue, token.Channel);
    }

    [Fact]
    public void Identified_queue_invitation_token_can_attach_recipient_and_reissue_email_hash()
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "old-token-hash",
            InvitationTokenChannels.IdentifiedQueue,
            respondentSubjectId: Guid.NewGuid());

        token.SetIdentifiedQueueRecipient("  ANA@Example.Test  ");
        token.MarkUsed(DateTimeOffset.Parse("2026-06-04T12:00:00+00:00"));
        token.ReissueIdentifiedQueueEmailHash("new-token-hash", "BO@Example.Test");

        Assert.Equal("new-token-hash", token.TokenHash);
        Assert.Equal("bo@example.test", token.Recipient);
        Assert.Null(token.UsedAt);
    }

    [Fact]
    public void Non_queue_invitation_token_rejects_identified_queue_email_mutation()
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            InvitationTokenChannels.Email,
            recipient: "researcher@example.test");

        Assert.Throws<InvalidOperationException>(() =>
            token.SetIdentifiedQueueRecipient("researcher@example.test"));
        Assert.Throws<InvalidOperationException>(() =>
            token.ReissueIdentifiedQueueEmailHash("new-hash", "researcher@example.test"));
    }

    [Fact]
    public void Identified_queue_invitation_token_rejects_assignment_id()
    {
        Assert.Throws<ArgumentException>(() => new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            InvitationTokenChannels.IdentifiedQueue,
            assignmentId: Guid.NewGuid(),
            respondentSubjectId: Guid.NewGuid()));
    }

    [Theory]
    [InlineData(InvitationTokenChannels.Email)]
    [InlineData(InvitationTokenChannels.Sms)]
    [InlineData(InvitationTokenChannels.OpenLink)]
    [InlineData(InvitationTokenChannels.IdentifiedEntry)]
    public void Non_queue_invitation_tokens_reject_respondent_subject_id(string channel)
    {
        Assert.Throws<ArgumentException>(() => new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            channel,
            respondentSubjectId: Guid.NewGuid()));
    }

    [Theory]
    [InlineData(InvitationTokenChannels.Email)]
    [InlineData(InvitationTokenChannels.Sms)]
    [InlineData(InvitationTokenChannels.OpenLink)]
    [InlineData(InvitationTokenChannels.IdentifiedEntry)]
    public void Existing_invitation_token_channels_leave_respondent_subject_id_null(string channel)
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            channel);

        Assert.Null(token.RespondentSubjectId);
    }

    [Fact]
    public void Notification_creates_queued_email_delivery_intent()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  RESEARCHER@example.com  ",
            scheduledFor: DateTimeOffset.Parse("2026-05-07T12:00:00+00:00"));

        Assert.Equal(NotificationChannels.Email, notification.Channel);
        Assert.Equal(NotificationStatuses.Queued, notification.Status);
        Assert.Equal("researcher@example.com", notification.Recipient);
        Assert.Equal("invitation", notification.TemplateCode);
        Assert.Equal("en", notification.Locale);
        Assert.Equal(DateTimeOffset.Parse("2026-05-07T12:00:00+00:00"), notification.ScheduledFor);
        Assert.Null(notification.SentAt);
        Assert.Null(notification.Error);
    }

    [Fact]
    public void Notification_creates_queued_email_delivery_intent_with_resolved_locale()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "researcher@example.com",
            locale: "hr-HR");

        Assert.Equal("hr-HR", notification.Locale);
    }

    [Fact]
    public void Notification_marks_email_delivery_sent()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "researcher@example.com");
        var sentAt = DateTimeOffset.Parse("2026-05-07T14:15:00+00:00");

        notification.MarkSent(sentAt);

        Assert.Equal(NotificationStatuses.Sent, notification.Status);
        Assert.Equal(sentAt, notification.SentAt);
        Assert.Null(notification.Error);
        Assert.Equal(sentAt, notification.UpdatedAt);
    }

    [Fact]
    public void Notification_marks_email_delivery_failed()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "researcher@example.com");
        var failedAt = DateTimeOffset.Parse("2026-05-07T14:20:00+00:00");

        notification.MarkFailed("  Provider unavailable  ", failedAt);

        Assert.Equal(NotificationStatuses.Failed, notification.Status);
        Assert.Null(notification.SentAt);
        Assert.Equal("Provider unavailable", notification.Error);
        Assert.Equal(failedAt, notification.UpdatedAt);
    }

    [Fact]
    public void Notification_requeues_failed_email_delivery_for_retry()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "researcher@example.com");
        var failedAt = DateTimeOffset.Parse("2026-05-07T14:20:00+00:00");
        var retryAt = DateTimeOffset.Parse("2026-05-07T14:25:00+00:00");
        notification.MarkFailed("Provider unavailable", failedAt);

        notification.RequeueForRetry(retryAt);

        Assert.Equal(NotificationStatuses.Queued, notification.Status);
        Assert.Null(notification.SentAt);
        Assert.Null(notification.Error);
        Assert.Equal(retryAt, notification.ScheduledFor);
        Assert.Equal(retryAt, notification.UpdatedAt);
    }

    [Fact]
    public void Notification_requeue_for_retry_rejects_non_failed_and_withdrawal_scrubbed_notifications()
    {
        var retryAt = DateTimeOffset.Parse("2026-05-07T14:25:00+00:00");
        var queued = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "queued@example.com");
        var scrubbed = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "scrubbed@example.com");
        scrubbed.ScrubForWithdrawal(DateTimeOffset.Parse("2026-05-07T14:20:00+00:00"));

        Assert.Throws<InvalidOperationException>(() => queued.RequeueForRetry(retryAt));
        Assert.Throws<InvalidOperationException>(() => scrubbed.RequeueForRetry(retryAt));
    }

    [Fact]
    public void Notification_scrub_for_withdrawal_makes_queued_email_non_deliverable()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "subject@example.com");
        var scrubbedAt = DateTimeOffset.Parse("2026-05-17T15:00:00+00:00");

        notification.ScrubForWithdrawal(scrubbedAt);

        Assert.Equal("withdrawn@example.invalid", notification.Recipient);
        Assert.Equal(NotificationStatuses.Failed, notification.Status);
        Assert.Null(notification.SentAt);
        Assert.Equal("withdrawal_scrubbed", notification.Error);
        Assert.Equal(scrubbedAt, notification.UpdatedAt);
    }

    [Fact]
    public void Notification_scrub_for_withdrawal_preserves_sent_lifecycle_without_recipient()
    {
        var notification = Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "subject@example.com");
        var sentAt = DateTimeOffset.Parse("2026-05-17T14:00:00+00:00");
        var scrubbedAt = DateTimeOffset.Parse("2026-05-17T15:00:00+00:00");
        notification.MarkSent(sentAt);

        notification.ScrubForWithdrawal(scrubbedAt);

        Assert.Equal("withdrawn@example.invalid", notification.Recipient);
        Assert.Equal(NotificationStatuses.Sent, notification.Status);
        Assert.Equal(sentAt, notification.SentAt);
        Assert.Null(notification.Error);
        Assert.Equal(scrubbedAt, notification.UpdatedAt);
    }

    [Fact]
    public void Delivery_attempt_scrub_for_withdrawal_clears_recipient_and_provider_message_id()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "subject@example.com",
            "provider-message-123",
            DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"));

        attempt.ScrubForWithdrawal();

        Assert.Equal("withdrawn@example.invalid", attempt.Recipient);
        Assert.Null(attempt.ProviderMessageId);
        Assert.Equal(NotificationStatuses.Sent, attempt.Status);
        Assert.Null(attempt.Error);
    }

    [Fact]
    public void Notification_does_not_expose_raw_respondent_path_storage()
    {
        Assert.DoesNotContain(
            typeof(Notification).GetProperties(),
            property => property.Name == "RespondentPath");
    }

    [Fact]
    public void Invitation_token_reissues_hash_without_raw_token_storage()
    {
        var token = new InvitationToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "old-hash",
            InvitationTokenChannels.Email,
            recipient: "researcher@example.com");

        token.ReissueHash("  new-hash  ");

        Assert.Equal("new-hash", token.TokenHash);
        Assert.DoesNotContain(
            typeof(InvitationToken).GetProperties(),
            property => property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Invitation_token_scrub_for_withdrawal_severs_assignment_and_revokes_hash()
    {
        var tokenId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var scrubbedAt = DateTimeOffset.Parse("2026-05-17T15:00:00+00:00");
        var token = new InvitationToken(
            tokenId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "linked-delivery-token-hash",
            InvitationTokenChannels.Email,
            recipient: "subject@example.com",
            assignmentId: assignmentId);

        token.ScrubForWithdrawal(scrubbedAt);

        Assert.Null(token.AssignmentId);
        Assert.Null(token.Recipient);
        Assert.Equal($"withdrawn:{tokenId:N}", token.TokenHash);
        Assert.Equal(scrubbedAt, token.ExpiresAt);
        Assert.Equal(scrubbedAt, token.UsedAt);
    }

    [Fact]
    public void Identified_queue_invitation_token_scrub_for_withdrawal_severs_respondent_subject()
    {
        var tokenId = Guid.NewGuid();
        var scrubbedAt = DateTimeOffset.Parse("2026-05-17T15:00:00+00:00");
        var token = new InvitationToken(
            tokenId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "queue-token-hash",
            InvitationTokenChannels.IdentifiedQueue,
            recipient: "subject@example.com",
            respondentSubjectId: Guid.NewGuid());

        token.ScrubForWithdrawal(scrubbedAt);

        Assert.Null(token.RespondentSubjectId);
        Assert.Null(token.AssignmentId);
        Assert.Null(token.Recipient);
        Assert.Equal($"withdrawn:{tokenId:N}", token.TokenHash);
        Assert.Equal(scrubbedAt, token.ExpiresAt);
        Assert.Equal(scrubbedAt, token.UsedAt);
        Assert.DoesNotContain(
            typeof(InvitationToken).GetProperties(),
            property => property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Notification_rejects_unknown_channel_or_status()
    {
        Assert.Throws<ArgumentException>(() => new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fax",
            "invitation",
            "queued",
            "researcher@example.com"));

        Assert.Throws<ArgumentException>(() => new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationChannels.Email,
            "invitation",
            "half_sent",
            "researcher@example.com"));
    }

    [Fact]
    public void Participant_code_requires_32_byte_hash()
    {
        Assert.Throws<ArgumentException>(() => new ParticipantCode(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new byte[31],
            65_536,
            3,
            4,
            32,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Participant_code_rejects_parameters_below_adr_0005_defaults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParticipantCode(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new byte[32],
            1024,
            1,
            1,
            16,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Participant_code_seen_again_updates_last_seen_only()
    {
        var firstSeen = DateTimeOffset.Parse("2026-05-07T11:00:00+00:00");
        var lastSeen = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        var code = new ParticipantCode(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new byte[32],
            65_536,
            3,
            4,
            32,
            firstSeen);

        code.SeenAgain(lastSeen);

        Assert.Equal(firstSeen, code.FirstSeenAt);
        Assert.Equal(lastSeen, code.LastSeenAt);
    }

    [Fact]
    public void Participant_code_exposes_no_raw_or_normalized_code_storage()
    {
        Assert.DoesNotContain(
            typeof(ParticipantCode).GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase) ||
                property.Name == "Code");
    }
}
