using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

public sealed class OutboxEventDispatcher : IOutboxEventDispatcher
{
    private readonly IReadOnlyDictionary<string, IOutboxEventHandler> _handlers;

    public OutboxEventDispatcher(IEnumerable<IOutboxEventHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        var handlerMap = new Dictionary<string, IOutboxEventHandler>(StringComparer.Ordinal);
        foreach (var handler in handlers)
        {
            ArgumentNullException.ThrowIfNull(handler);

            string eventType;
            try
            {
                eventType = OutboxTextSafety.EnsureSafeIdentifier(handler.EventType, "eventType");
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"Outbox event handlers must declare a safe non-empty event type '{OutboxTextSafety.UnsafeEventType}'.",
                    exception);
            }

            if (!handlerMap.TryAdd(eventType, handler))
            {
                throw new InvalidOperationException(
                    $"Duplicate outbox event handler registered for event type '{OutboxTextSafety.SafeIdentifierForDiagnostics(eventType)}'.");
            }
        }

        _handlers = handlerMap;
    }

    public Task DispatchAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);

        if (!_handlers.TryGetValue(outboxEvent.EventType, out var handler))
        {
            throw new OutboxEventHandlerNotFoundException(outboxEvent.EventType);
        }

        return handler.HandleAsync(outboxEvent, cancellationToken);
    }
}
