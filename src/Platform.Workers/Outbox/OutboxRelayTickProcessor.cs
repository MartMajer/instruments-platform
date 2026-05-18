namespace Platform.Workers.Outbox;

public sealed class OutboxRelayTickProcessor(OutboxRelay relay) : IOutboxRelayTickProcessor
{
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return relay.ProcessDueAsync(batchSize, cancellationToken);
    }
}
