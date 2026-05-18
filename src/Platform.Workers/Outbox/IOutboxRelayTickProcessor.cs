namespace Platform.Workers.Outbox;

public interface IOutboxRelayTickProcessor
{
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
