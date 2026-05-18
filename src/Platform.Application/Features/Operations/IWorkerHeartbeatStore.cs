namespace Platform.Application.Features.Operations;

public interface IWorkerHeartbeatStore
{
    Task RecordHeartbeatAsync(
        WorkerHeartbeatRecordRequest request,
        CancellationToken cancellationToken);

    Task<WorkerHeartbeatSnapshotResponse?> GetLatestHeartbeatAsync(
        string workerName,
        CancellationToken cancellationToken);
}

public sealed record WorkerHeartbeatRecordRequest(
    string WorkerName,
    string InstanceId,
    DateTimeOffset ObservedAt);

public sealed record WorkerHeartbeatSnapshotResponse(
    string WorkerName,
    string InstanceId,
    DateTimeOffset LastSeenAt);
