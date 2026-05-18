namespace Platform.Workers.Operations;

public sealed class WorkerHeartbeatWorkerOptions
{
    public const string SectionName = "WorkerHeartbeat";

    public bool Enabled { get; set; } = true;

    public string WorkerName { get; set; } = "platform-workers";

    public int InitialDelaySeconds { get; set; } = 5;

    public int IntervalSeconds { get; set; } = 30;

    public TimeSpan InitialDelay => TimeSpan.FromSeconds(InitialDelaySeconds);

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);
}
