using Platform.Domain.Consent;

namespace Platform.UnitTests.Domain;

public sealed class WithdrawalEventTests
{
    [Fact]
    public void Identified_withdrawal_requires_subject_target_only()
    {
        var tenantId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.Parse("2026-05-17T14:00:00+00:00");

        var withdrawal = WithdrawalEvent.PlanIdentified(
            Guid.NewGuid(),
            tenantId,
            seriesId,
            policyId,
            subjectId,
            RetentionPolicy.Anonymize,
            requestedAt,
            consentRecordCount: 1,
            responseSessionCount: 2,
            answerCount: 3,
            scoreRunCount: 4,
            scoreCount: 4);

        Assert.Equal(WithdrawalTargetKinds.IdentifiedSubject, withdrawal.TargetKind);
        Assert.Equal(subjectId, withdrawal.SubjectId);
        Assert.Null(withdrawal.ParticipantCodeId);
        Assert.Equal(WithdrawalEventStatuses.Planned, withdrawal.Status);
        Assert.Equal(WithdrawalScopes.CampaignSeries, withdrawal.Scope);
        Assert.Equal(RetentionPolicy.Anonymize, withdrawal.ActionAfter);
        Assert.Equal(1, withdrawal.ConsentRecordCount);
        Assert.Equal(2, withdrawal.ResponseSessionCount);
        Assert.Equal(3, withdrawal.AnswerCount);
        Assert.Equal(4, withdrawal.ScoreRunCount);
        Assert.Equal(4, withdrawal.ScoreCount);
    }

    [Fact]
    public void Anonymous_longitudinal_withdrawal_requires_participant_code_target_only()
    {
        var participantCodeId = Guid.NewGuid();

        var withdrawal = WithdrawalEvent.PlanAnonymousLongitudinal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            participantCodeId,
            RetentionPolicy.Delete,
            DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"),
            consentRecordCount: 0,
            responseSessionCount: 1,
            answerCount: 2,
            scoreRunCount: 3,
            scoreCount: 3);

        Assert.Equal(WithdrawalTargetKinds.AnonymousLongitudinalCode, withdrawal.TargetKind);
        Assert.Null(withdrawal.SubjectId);
        Assert.Equal(participantCodeId, withdrawal.ParticipantCodeId);
        Assert.Equal(RetentionPolicy.Delete, withdrawal.ActionAfter);
    }

    [Fact]
    public void Anonymous_longitudinal_unmatched_withdrawal_records_neutral_safe_event()
    {
        var withdrawal = WithdrawalEvent.PlanAnonymousLongitudinalUnmatched(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RetentionPolicy.Anonymize,
            DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"));

        Assert.Equal(WithdrawalTargetKinds.AnonymousLongitudinalUnmatched, withdrawal.TargetKind);
        Assert.Null(withdrawal.SubjectId);
        Assert.Null(withdrawal.ParticipantCodeId);
        Assert.Equal(0, withdrawal.ConsentRecordCount);
        Assert.Equal(0, withdrawal.ResponseSessionCount);
        Assert.Equal(0, withdrawal.AnswerCount);
        Assert.Equal(0, withdrawal.ScoreRunCount);
        Assert.Equal(0, withdrawal.ScoreCount);
    }

    [Fact]
    public void Withdrawal_event_rejects_negative_counts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WithdrawalEvent.PlanIdentified(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RetentionPolicy.Anonymize,
            DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"),
            consentRecordCount: -1,
            responseSessionCount: 0,
            answerCount: 0,
            scoreRunCount: 0,
            scoreCount: 0));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""{"rawParticipantCode":"alpha-001"}""")]
    [InlineData("""{"answer":"yes"}""")]
    [InlineData("""{"token":"secret"}""")]
    [InlineData("""{"recipient":"person@example.com"}""")]
    [InlineData("""{"providerMessageId":"smtp-123"}""")]
    [InlineData("""{"ipHash":"abc"}""")]
    [InlineData("""{"userAgentHash":"abc"}""")]
    public void Withdrawal_event_rejects_unsafe_metadata(string metadataJson)
    {
        Assert.Throws<ArgumentException>(() => WithdrawalEvent.PlanIdentified(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RetentionPolicy.Anonymize,
            DateTimeOffset.Parse("2026-05-17T14:00:00+00:00"),
            consentRecordCount: 0,
            responseSessionCount: 0,
            answerCount: 0,
            scoreRunCount: 0,
            scoreCount: 0,
            metadataJson: metadataJson));
    }

    [Fact]
    public void Withdrawal_event_exposes_no_raw_participant_code_or_answer_storage()
    {
        Assert.DoesNotContain(
            typeof(WithdrawalEvent).GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase) ||
                property.Name == "ParticipantCode" ||
                property.Name.Contains("AnswerValue", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("AnswerContent", StringComparison.OrdinalIgnoreCase));
    }
}
