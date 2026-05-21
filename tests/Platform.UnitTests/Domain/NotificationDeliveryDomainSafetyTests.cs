using Platform.Domain.Campaigns;

namespace Platform.UnitTests.Domain;

public sealed class NotificationDeliveryDomainSafetyTests
{
    [Fact]
    public void Notification_mark_failed_uses_stable_fallback_for_blank_errors()
    {
        var notification = CreateNotification();

        notification.MarkFailed("   ", DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", notification.Error);
    }

    [Fact]
    public void Notification_mark_failed_redacts_sensitive_error_values()
    {
        var notification = CreateNotification();

        notification.MarkFailed(
            "SMTP failed for ada@example.test at /r/inv_secret with password=secret.",
            DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", notification.Error);
    }

    [Fact]
    public void Notification_mark_failed_redacts_withdrawal_token_values()
    {
        var notification = CreateNotification();

        notification.MarkFailed(
            "Delivery failed for wdr_11111111111141118111111111111111_sensitiveWDR.",
            DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", notification.Error);
    }

    [Fact]
    public void Notification_mark_failed_removes_control_characters_from_safe_errors()
    {
        var notification = CreateNotification();

        notification.MarkFailed("smtp timeout\r\nretryable\tlater", DateTimeOffset.UtcNow);

        Assert.NotNull(notification.Error);
        Assert.DoesNotContain("\r", notification.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", notification.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("\t", notification.Error, StringComparison.Ordinal);
        Assert.Contains("smtp timeout", notification.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Delivery_attempt_create_failed_uses_stable_fallback_for_blank_errors()
    {
        var attempt = NotificationDeliveryAttempt.CreateFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "   ",
            DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", attempt.Error);
    }

    [Fact]
    public void Delivery_attempt_create_failed_redacts_sensitive_error_values()
    {
        var attempt = NotificationDeliveryAttempt.CreateFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "SMTP failed for ada@example.test at /r/inv_secret with password=secret.",
            DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", attempt.Error);
    }

    [Fact]
    public void Delivery_attempt_create_failed_redacts_withdrawal_token_values()
    {
        var attempt = NotificationDeliveryAttempt.CreateFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "Delivery failed for wdr_11111111111141118111111111111111_sensitiveWDR.",
            DateTimeOffset.UtcNow);

        Assert.Equal("delivery_failed", attempt.Error);
    }

    [Fact]
    public void Delivery_attempt_create_failed_removes_control_characters_from_safe_errors()
    {
        var attempt = NotificationDeliveryAttempt.CreateFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "smtp timeout\r\nretryable\tlater",
            DateTimeOffset.UtcNow);

        Assert.NotNull(attempt.Error);
        Assert.DoesNotContain("\r", attempt.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", attempt.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("\t", attempt.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Delivery_attempt_create_sent_redacts_sensitive_provider_message_ids()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "smtp:/r/inv_secret:ada@example.test",
            DateTimeOffset.UtcNow);

        Assert.Equal("redacted", attempt.ProviderMessageId);
    }

    [Fact]
    public void Delivery_attempt_create_sent_keeps_missing_provider_message_id_missing()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            providerMessageId: null,
            DateTimeOffset.UtcNow);

        Assert.Null(attempt.ProviderMessageId);
    }

    [Fact]
    public void Delivery_attempt_create_sent_redacts_withdrawal_token_provider_message_ids()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "smtp:wdr_11111111111141118111111111111111_sensitiveWDR",
            DateTimeOffset.UtcNow);

        Assert.Equal("redacted", attempt.ProviderMessageId);
    }

    [Fact]
    public void Delivery_attempt_create_sent_redacts_platform_delivery_key_provider_message_ids()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            "smtp:campaign-email:tenant:notification:attempt",
            DateTimeOffset.UtcNow);

        Assert.Equal("redacted", attempt.ProviderMessageId);
    }

    [Fact]
    public void Delivery_attempt_create_sent_bounds_provider_message_ids()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp",
            "ada@example.test",
            new string('a', 500),
            DateTimeOffset.UtcNow);

        Assert.NotNull(attempt.ProviderMessageId);
        Assert.True(attempt.ProviderMessageId.Length <= 200);
    }

    [Fact]
    public void Delivery_attempt_create_sent_sanitizes_provider_name()
    {
        var attempt = NotificationDeliveryAttempt.CreateSent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp:/r/inv_secret",
            "ada@example.test",
            "smtp:message",
            DateTimeOffset.UtcNow);

        Assert.Equal("unknown", attempt.Provider);
    }

    [Fact]
    public void Delivery_attempt_create_failed_sanitizes_provider_name()
    {
        var attempt = NotificationDeliveryAttempt.CreateFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "smtp:/r/inv_secret",
            "ada@example.test",
            "smtp timeout",
            DateTimeOffset.UtcNow);

        Assert.Equal("unknown", attempt.Provider);
    }

    private static Notification CreateNotification()
    {
        return Notification.QueueEmailInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ada@example.test",
            scheduledFor: null);
    }
}
