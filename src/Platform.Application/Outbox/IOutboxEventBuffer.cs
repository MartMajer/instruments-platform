using Platform.Domain.Outbox;

namespace Platform.Application.Outbox;

public interface IOutboxEventBuffer
{
    IReadOnlyList<OutboxMessage> PendingMessages { get; }

    void Enqueue(OutboxMessage message);

    void Clear();
}
