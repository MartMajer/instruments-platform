using Platform.Domain.Outbox;

namespace Platform.UnitTests.Domain;

public sealed class OutboxEventSafetyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_aggregate_type(string aggregateType)
    {
        Assert.Throws<ArgumentException>(() => OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            aggregateType,
            "InvitationEmailQueued",
            CreatePayload(),
            correlationId: null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_event_type(string eventType)
    {
        Assert.Throws<ArgumentException>(() => OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            eventType,
            CreatePayload(),
            correlationId: null));
    }

    [Fact]
    public void Create_rejects_unsafe_aggregate_type_values()
    {
        var exception = Assert.Throws<ArgumentException>(() => OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification:/r/inv_secret:ada@example.test",
            "InvitationEmailQueued",
            CreatePayload(),
            correlationId: null));

        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_rejects_unsafe_event_type_values()
    {
        var exception = Assert.Throws<ArgumentException>(() => OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            "InvitationEmailQueued\r\n/r/inv_secret",
            CreatePayload(),
            correlationId: null));

        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\r", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Mark_failed_uses_safe_fallback_for_blank_errors()
    {
        var outboxEvent = CreateEvent();

        outboxEvent.MarkFailed("   ", DateTimeOffset.UtcNow);

        Assert.Equal("OUTBOX_FAILURE", outboxEvent.LastError);
    }

    [Fact]
    public void Mark_failed_redacts_sensitive_error_values()
    {
        var outboxEvent = CreateEvent();

        outboxEvent.MarkFailed(
            "SMTP failed for ada@example.test at /r/inv_secret with password=secret.",
            DateTimeOffset.UtcNow);

        Assert.Equal("OUTBOX_FAILURE", outboxEvent.LastError);
        Assert.DoesNotContain("ada@example.test", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mark_failed_redacts_withdrawal_token_values()
    {
        var outboxEvent = CreateEvent();

        outboxEvent.MarkFailed(
            "Dispatch failed for wdr_11111111111141118111111111111111_sensitiveWDR.",
            DateTimeOffset.UtcNow);

        Assert.Equal("OUTBOX_FAILURE", outboxEvent.LastError);
        Assert.DoesNotContain("wdr_", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mark_failed_removes_control_characters_from_durable_errors()
    {
        var outboxEvent = CreateEvent();

        outboxEvent.MarkFailed(
            "DISPATCH_FAILED\r\nevent_type=InvitationEmailQueued\taggregate_type=notification",
            DateTimeOffset.UtcNow);

        Assert.NotNull(outboxEvent.LastError);
        Assert.DoesNotContain("\r", outboxEvent.LastError, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", outboxEvent.LastError, StringComparison.Ordinal);
        Assert.DoesNotContain("\t", outboxEvent.LastError, StringComparison.Ordinal);
        Assert.Contains("DISPATCH_FAILED", outboxEvent.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public void Mark_failed_redacts_sensitive_values_when_dead_lettering()
    {
        var outboxEvent = CreateEvent();
        var failedAt = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            outboxEvent.MarkFailed(
                "SMTP failed for ada@example.test at /r/inv_secret with password=secret.",
                failedAt.AddSeconds(attempt));
        }

        Assert.NotNull(outboxEvent.LastError);
        Assert.StartsWith(OutboxEvent.DeadLetterPrefix, outboxEvent.LastError, StringComparison.Ordinal);
        Assert.Contains("OUTBOX_FAILURE", outboxEvent.LastError, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@example.test", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", outboxEvent.LastError, StringComparison.OrdinalIgnoreCase);
    }

    private static OutboxEvent CreateEvent()
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            "InvitationEmailQueued",
            CreatePayload(),
            correlationId: null);
    }

    private static System.Text.Json.JsonDocument CreatePayload()
    {
        return OutboxPayload.Create(new Dictionary<string, object?>
        {
            ["schema_version"] = 1,
            ["notification_id"] = Guid.NewGuid()
        });
    }
}
