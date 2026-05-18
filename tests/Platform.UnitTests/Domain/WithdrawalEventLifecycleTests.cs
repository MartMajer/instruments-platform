using Platform.Domain.Consent;

namespace Platform.UnitTests.Domain;

public sealed class WithdrawalEventLifecycleTests
{
    [Fact]
    public void Planned_event_can_move_to_processing_then_completed()
    {
        var requestedAt = DateTimeOffset.Parse("2026-05-17T12:00:00+00:00");
        var withdrawal = CreateIdentified(requestedAt);

        withdrawal.MarkProcessing();
        withdrawal.MarkCompleted(
            requestedAt.AddMinutes(5),
            """{"result":"dry_run_guarded_noop"}""");

        Assert.Equal(WithdrawalEventStatuses.Completed, withdrawal.Status);
        Assert.Equal(requestedAt.AddMinutes(5), withdrawal.ProcessedAt);
        Assert.Contains("dry_run_guarded_noop", withdrawal.MetadataJson);
    }

    [Fact]
    public void Planned_event_can_move_to_failed_with_safe_metadata()
    {
        var requestedAt = DateTimeOffset.Parse("2026-05-17T12:00:00+00:00");
        var withdrawal = CreateIdentified(requestedAt);

        withdrawal.MarkFailed(
            requestedAt.AddMinutes(1),
            """{"failure_code":"graph_changed"}""");

        Assert.Equal(WithdrawalEventStatuses.Failed, withdrawal.Status);
        Assert.Equal(requestedAt.AddMinutes(1), withdrawal.ProcessedAt);
        Assert.Contains("graph_changed", withdrawal.MetadataJson);
    }

    [Fact]
    public void Processing_cannot_be_claimed_twice()
    {
        var withdrawal = CreateIdentified();
        withdrawal.MarkProcessing();

        Assert.Throws<InvalidOperationException>(() => withdrawal.MarkProcessing());
    }

    [Fact]
    public void Planned_event_cannot_complete_without_processing()
    {
        var withdrawal = CreateIdentified();

        Assert.Throws<InvalidOperationException>(() =>
            withdrawal.MarkCompleted(DateTimeOffset.UtcNow, """{"result":"completed"}"""));
    }

    [Fact]
    public void Completion_cannot_precede_requested_time()
    {
        var requestedAt = DateTimeOffset.Parse("2026-05-17T12:00:00+00:00");
        var withdrawal = CreateIdentified(requestedAt);
        withdrawal.MarkProcessing();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            withdrawal.MarkCompleted(
                requestedAt.AddSeconds(-1),
                """{"result":"completed"}"""));
    }

    [Fact]
    public void Failure_metadata_rejects_sensitive_markers()
    {
        var withdrawal = CreateIdentified();

        Assert.Throws<ArgumentException>(() =>
            withdrawal.MarkFailed(
                DateTimeOffset.UtcNow,
                """{"raw_participant_code":"alpha-001"}"""));
    }

    [Fact]
    public void Requested_response_session_request_can_be_approved_to_planned()
    {
        var requestedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+00:00");
        var withdrawal = CreateResponseSessionRequest(requestedAt);

        withdrawal.ApproveRequest("""{"decision":"approved"}""");

        Assert.Equal(WithdrawalEventStatuses.Planned, withdrawal.Status);
        Assert.Null(withdrawal.ProcessedAt);
        Assert.Contains("approved", withdrawal.MetadataJson);
    }

    [Fact]
    public void Requested_response_session_request_can_be_denied_to_denied()
    {
        var requestedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+00:00");
        var deniedAt = requestedAt.AddMinutes(3);
        var withdrawal = CreateResponseSessionRequest(requestedAt);

        withdrawal.DenyRequest(deniedAt, """{"decision":"denied"}""");

        Assert.Equal(WithdrawalEventStatuses.Denied, withdrawal.Status);
        Assert.Equal(deniedAt, withdrawal.ProcessedAt);
        Assert.Contains("denied", withdrawal.MetadataJson);
    }

    [Fact]
    public void Only_requested_events_can_be_approved_or_denied()
    {
        var planned = CreateIdentified();
        var denied = CreateResponseSessionRequest();
        denied.DenyRequest(DateTimeOffset.UtcNow.AddMinutes(1), """{"decision":"denied"}""");

        Assert.Throws<InvalidOperationException>(() => planned.ApproveRequest("""{"decision":"approved"}"""));
        Assert.Throws<InvalidOperationException>(() => planned.DenyRequest(DateTimeOffset.UtcNow, """{"decision":"denied"}"""));
        Assert.Throws<InvalidOperationException>(() => denied.ApproveRequest("""{"decision":"approved"}"""));
        Assert.Throws<InvalidOperationException>(() => denied.DenyRequest(DateTimeOffset.UtcNow.AddMinutes(2), """{"decision":"denied"}"""));
    }

    [Fact]
    public void Decision_metadata_rejects_sensitive_markers()
    {
        var approve = CreateResponseSessionRequest();
        var deny = CreateResponseSessionRequest();

        Assert.Throws<ArgumentException>(() =>
            approve.ApproveRequest("""{"raw_answer":"value"}"""));
        Assert.Throws<ArgumentException>(() =>
            deny.DenyRequest(DateTimeOffset.UtcNow.AddMinutes(1), """{"participant_code":"alpha"}"""));
    }

    private static WithdrawalEvent CreateIdentified(DateTimeOffset? requestedAt = null)
    {
        return WithdrawalEvent.PlanIdentified(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RetentionPolicy.Anonymize,
            requestedAt ?? DateTimeOffset.UtcNow,
            consentRecordCount: 1,
            responseSessionCount: 1,
            answerCount: 1,
            scoreRunCount: 1,
            scoreCount: 1);
    }

    private static WithdrawalEvent CreateResponseSessionRequest(DateTimeOffset? requestedAt = null)
    {
        return WithdrawalEvent.RequestResponseSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RetentionPolicy.Anonymize,
            requestedAt ?? DateTimeOffset.UtcNow,
            consentRecordCount: 1,
            responseSessionCount: 1,
            answerCount: 1,
            scoreRunCount: 1,
            scoreCount: 1);
    }
}
