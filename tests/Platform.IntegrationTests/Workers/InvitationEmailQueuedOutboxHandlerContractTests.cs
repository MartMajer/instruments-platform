using Platform.Domain.Outbox;
using Platform.Workers.Outbox;

namespace Platform.IntegrationTests.Workers;

public sealed class InvitationEmailQueuedOutboxHandlerContractTests
{
    [Fact]
    public async Task Handler_rejects_direct_calls_for_other_event_types()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent("OtherEvent", ValidPayload());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("InvitationEmailQueued", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_schema_version()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent(
            InvitationEmailQueuedOutboxHandler.EventTypeName,
            new Dictionary<string, object?>
            {
                ["notification_id"] = Guid.NewGuid()
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("schema_version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_rejects_unsupported_schema_version()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent(
            InvitationEmailQueuedOutboxHandler.EventTypeName,
            new Dictionary<string, object?>
            {
                ["schema_version"] = 2,
                ["notification_id"] = Guid.NewGuid()
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("schema_version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_notification_id()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent(
            InvitationEmailQueuedOutboxHandler.EventTypeName,
            new Dictionary<string, object?>
            {
                ["schema_version"] = 1
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("notification_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_requires_notification_id_to_be_guid()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent(
            InvitationEmailQueuedOutboxHandler.EventTypeName,
            new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["notification_id"] = "not-a-guid"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("notification_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_rejects_sensitive_invitation_payload_values()
    {
        var handler = new InvitationEmailQueuedOutboxHandler();
        var outboxEvent = CreateEvent(
            InvitationEmailQueuedOutboxHandler.EventTypeName,
            new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["notification_id"] = Guid.NewGuid(),
                ["recipient"] = "ada@example.test",
                ["path"] = "/r/inv_secret"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(outboxEvent));

        Assert.Contains("payload", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ada@example.test", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/r/", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OutboxEvent CreateEvent(
        string eventType,
        IReadOnlyDictionary<string, object?> payload)
    {
        return OutboxEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notification",
            eventType,
            OutboxPayload.Create(payload),
            correlationId: null);
    }

    private static IReadOnlyDictionary<string, object?> ValidPayload()
    {
        return new Dictionary<string, object?>
        {
            ["schema_version"] = 1,
            ["notification_id"] = Guid.NewGuid()
        };
    }
}
