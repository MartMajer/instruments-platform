using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

public sealed class OutboxEventHandlerNotFoundException(string eventType)
    : InvalidOperationException(
        $"No outbox event handler registered for event type '{OutboxTextSafety.SafeIdentifierForDiagnostics(eventType)}'.")
{
    public string EventType { get; } = eventType;
}
