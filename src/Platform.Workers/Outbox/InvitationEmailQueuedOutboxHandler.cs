using System.Text.Json;
using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

public sealed class InvitationEmailQueuedOutboxHandler : IOutboxEventHandler
{
    public const string EventTypeName = "InvitationEmailQueued";

    public string EventType => EventTypeName;

    public Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);
        ValidateInvitationEmailQueuedIntent(outboxEvent);

        // Current invitation delivery is driven by notification rows; this only acknowledges a validated known outbox intent.
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    private static void ValidateInvitationEmailQueuedIntent(OutboxEvent outboxEvent)
    {
        if (outboxEvent.EventType != EventTypeName)
        {
            throw new InvalidOperationException(
                $"{EventTypeName} handler cannot process event type '{OutboxTextSafety.SafeIdentifierForDiagnostics(outboxEvent.EventType)}'.");
        }

        var root = outboxEvent.Payload.RootElement;
        if (OutboxTextSafety.ContainsSensitiveValue(root.GetRawText()))
        {
            throw new InvalidOperationException("InvitationEmailQueued payload contains unsafe delivery values.");
        }

        if (!root.TryGetProperty("schema_version", out var schemaVersion) ||
            schemaVersion.ValueKind != JsonValueKind.Number ||
            schemaVersion.GetInt32() != 1)
        {
            throw new InvalidOperationException("InvitationEmailQueued payload must declare schema_version 1.");
        }

        if (!root.TryGetProperty("notification_id", out var notificationId) ||
            notificationId.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(notificationId.GetString(), out _))
        {
            throw new InvalidOperationException("InvitationEmailQueued payload must declare notification_id as a GUID.");
        }
    }
}
