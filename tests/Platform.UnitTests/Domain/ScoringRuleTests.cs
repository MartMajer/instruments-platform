using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class ScoringRuleTests
{
    private const string ValidHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Draft_normalizes_metadata_and_json_objects()
    {
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Burnout.Total ",
            " 1.0.0 ",
            " scoring-rule/v1 ",
            " engine/v1 ",
            ValidHash.ToUpperInvariant(),
            " { \"rule_id\": \"burnout.total\" } ",
            " { \"scores\": [\"total\"] } ");

        Assert.Equal("burnout.total", rule.RuleKey);
        Assert.Equal("1.0.0", rule.RuleVersion);
        Assert.Equal("scoring-rule/v1", rule.SchemaVersion);
        Assert.Equal("engine/v1", rule.EngineMinVersion);
        Assert.Equal(ValidHash, rule.DocumentHash);
        Assert.Equal(ScoringRuleStatuses.Draft, rule.Status);
        Assert.False(rule.IsLocked);
        Assert.Null(rule.PublishedAt);
        Assert.Null(rule.PublishedBy);
        Assert.Equal("{ \"rule_id\": \"burnout.total\" }", rule.Document);
        Assert.Equal("{ \"scores\": [\"total\"] }", rule.Produces);
        Assert.Equal("{}", rule.Compatibility);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"not-object\"")]
    public void Draft_requires_json_object_payloads(string invalidJson)
    {
        Assert.Throws<ArgumentException>(() => ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidHash,
            invalidJson,
            "{}"));
    }

    [Fact]
    public void Draft_rejects_invalid_document_hash()
    {
        Assert.Throws<ArgumentException>(() => ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            "not-a-sha256",
            "{}",
            "{}"));
    }

    [Fact]
    public void Publish_locks_rule_and_records_publisher()
    {
        var publisherId = Guid.NewGuid();
        var publishedAt = DateTimeOffset.Parse("2026-05-06T12:00:00+00:00");
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidHash,
            "{}",
            "{}");

        rule.Publish(publisherId, publishedAt);

        Assert.Equal(ScoringRuleStatuses.Published, rule.Status);
        Assert.True(rule.IsLocked);
        Assert.Equal(publishedAt, rule.PublishedAt);
        Assert.Equal(publisherId, rule.PublishedBy);
        Assert.Equal(publishedAt, rule.UpdatedAt);
    }

    [Fact]
    public void Published_rule_cannot_be_published_again()
    {
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidHash,
            "{}",
            "{}");

        rule.Publish(null, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => rule.Publish(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Draft_rule_can_be_retired_without_publishing()
    {
        var retiredAt = DateTimeOffset.Parse("2026-06-12T12:00:00+00:00");
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidHash,
            "{}",
            "{}");

        rule.RetireDraft(retiredAt);

        Assert.Equal(ScoringRuleStatuses.Retired, rule.Status);
        Assert.True(rule.IsLocked);
        Assert.Null(rule.PublishedAt);
        Assert.Null(rule.PublishedBy);
        Assert.Equal(retiredAt, rule.UpdatedAt);
    }

    [Fact]
    public void Published_rule_cannot_be_retired_as_draft()
    {
        var rule = ScoringRule.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "burnout.total",
            "1.0.0",
            "scoring-rule/v1",
            "engine/v1",
            ValidHash,
            "{}",
            "{}");
        rule.Publish(null, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => rule.RetireDraft(DateTimeOffset.UtcNow));
    }
}
