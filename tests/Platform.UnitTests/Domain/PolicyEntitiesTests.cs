using Platform.Domain.Consent;

namespace Platform.UnitTests.Domain;

public sealed class PolicyEntitiesTests
{
    [Fact]
    public void Retention_policy_accepts_valid_fields()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var policy = new RetentionPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " 1.0.0 ",
            retainForYears: 1,
            retentionStartEvent: " response_submitted_at ",
            actionAfter: " anonymize ",
            nextReviewAt: DateOnly.FromDateTime(createdAt.UtcDateTime).AddYears(1),
            publicationLimits: """{"status":"proof_default_not_legal_advice"}""",
            createdAt);

        Assert.Equal("1.0.0", policy.Version);
        Assert.Equal(1, policy.RetainForYears);
        Assert.Equal("response_submitted_at", policy.RetentionStartEvent);
        Assert.Equal("anonymize", policy.ActionAfter);
        Assert.Equal("""{"status":"proof_default_not_legal_advice"}""", policy.PublicationLimits);
        Assert.True(policy.IsUsableAt(createdAt));
    }

    [Theory]
    [InlineData(0, "response_submitted_at", "anonymize", """{}""")]
    [InlineData(1, "unknown_anchor", "anonymize", """{}""")]
    [InlineData(1, "response_submitted_at", "archive", """{}""")]
    [InlineData(1, "response_submitted_at", "anonymize", """[]""")]
    public void Retention_policy_rejects_invalid_fields(
        int retainForYears,
        string retentionStartEvent,
        string actionAfter,
        string publicationLimits)
    {
        Assert.ThrowsAny<ArgumentException>(() => new RetentionPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.0.0",
            retainForYears,
            retentionStartEvent,
            actionAfter,
            DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            publicationLimits,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Retention_policy_is_not_usable_after_retirement()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var retiredAt = createdAt.AddDays(1);
        var policy = new RetentionPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.0.0",
            retainForYears: 1,
            retentionStartEvent: "response_submitted_at",
            actionAfter: "anonymize",
            nextReviewAt: DateOnly.FromDateTime(createdAt.UtcDateTime).AddYears(1),
            publicationLimits: "{}",
            createdAt,
            retiredAt);

        Assert.True(policy.IsUsableAt(createdAt));
        Assert.False(policy.IsUsableAt(retiredAt));
    }

    [Fact]
    public void Retention_policy_rejects_retirement_before_or_at_creation()
    {
        var createdAt = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentOutOfRangeException>(() => new RetentionPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.0.0",
            retainForYears: 1,
            retentionStartEvent: "response_submitted_at",
            actionAfter: "anonymize",
            nextReviewAt: DateOnly.FromDateTime(createdAt.UtcDateTime).AddYears(1),
            publicationLimits: "{}",
            createdAt,
            createdAt));
    }

    [Fact]
    public void Disclosure_policy_accepts_valid_fields()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var policy = new DisclosurePolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " 1.0.0 ",
            kMin: 5,
            suppressionStrategy: " hide_cell ",
            appliesToDimensions: """["score","subscale"]""",
            createdAt);

        Assert.Equal("1.0.0", policy.Version);
        Assert.Equal(5, policy.KMin);
        Assert.Equal("hide_cell", policy.SuppressionStrategy);
        Assert.Equal("""["score","subscale"]""", policy.AppliesToDimensions);
        Assert.True(policy.IsUsableAt(createdAt));
    }

    [Theory]
    [InlineData(4, "hide_cell", """["score"]""")]
    [InlineData(5, "show_cell", """["score"]""")]
    [InlineData(5, "hide_cell", """{}""")]
    [InlineData(5, "hide_cell", """[""]""")]
    public void Disclosure_policy_rejects_invalid_fields(
        int kMin,
        string suppressionStrategy,
        string appliesToDimensions)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DisclosurePolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.0.0",
            kMin,
            suppressionStrategy,
            appliesToDimensions,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Disclosure_policy_rejects_retirement_before_or_at_creation()
    {
        var createdAt = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentOutOfRangeException>(() => new DisclosurePolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.0.0",
            kMin: 5,
            suppressionStrategy: "hide_cell",
            appliesToDimensions: """["score"]""",
            createdAt,
            createdAt));
    }
}
