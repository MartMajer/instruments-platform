using Platform.Domain.Outbox;

namespace Platform.Workers.Outbox;

public interface IOutboxEventDispatcher
{
    Task DispatchAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
