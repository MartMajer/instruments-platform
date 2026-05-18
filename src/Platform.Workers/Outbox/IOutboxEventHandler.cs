using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

public interface IOutboxEventHandler
{
    string EventType { get; }

    Task HandleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
