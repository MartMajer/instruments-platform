using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Operations;
using Platform.Domain.Operations;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Operations;

public sealed class WorkerHeartbeatStore(ApplicationDbContext dbContext) : IWorkerHeartbeatStore
{
    public async Task RecordHeartbeatAsync(
        WorkerHeartbeatRecordRequest request,
        CancellationToken cancellationToken)
    {
        var heartbeat = await dbContext.WorkerHeartbeats
            .SingleOrDefaultAsync(
                row => row.WorkerName == request.WorkerName && row.InstanceId == request.InstanceId,
                cancellationToken);

        if (heartbeat is null)
        {
            dbContext.WorkerHeartbeats.Add(new WorkerHeartbeat(
                Guid.NewGuid(),
                request.WorkerName,
                request.InstanceId,
                request.ObservedAt));
        }
        else
        {
            heartbeat.RecordSeen(request.ObservedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkerHeartbeatSnapshotResponse?> GetLatestHeartbeatAsync(
        string workerName,
        CancellationToken cancellationToken)
    {
        return await dbContext.WorkerHeartbeats
            .AsNoTracking()
            .Where(heartbeat => heartbeat.WorkerName == workerName)
            .OrderByDescending(heartbeat => heartbeat.LastSeenAt)
            .Select(heartbeat => new WorkerHeartbeatSnapshotResponse(
                heartbeat.WorkerName,
                heartbeat.InstanceId,
                heartbeat.LastSeenAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
