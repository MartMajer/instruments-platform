using Platform.Domain.Outbox;

namespace Platform.Application.Outbox;

public sealed class OutboxEventBuffer : IOutboxEventBuffer
{
    private readonly List<OutboxMessage> _pendingMessages = [];

    public IReadOnlyList<OutboxMessage> PendingMessages => _pendingMessages;

    public void Enqueue(OutboxMessage message)
    {
        _pendingMessages.Add(message);
    }

    public void Clear()
    {
        _pendingMessages.Clear();
    }
}
