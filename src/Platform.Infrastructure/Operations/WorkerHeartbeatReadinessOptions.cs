namespace Platform.Infrastructure.Operations;

public sealed class WorkerHeartbeatReadinessOptions
{
    public const string SectionName = "WorkerHeartbeatReadiness";

    public bool Enabled { get; set; }

    public string ExpectedWorkerName { get; set; } = "platform-workers";

    public int StaleAfterSeconds { get; set; } = 120;

    public TimeSpan StaleAfter => TimeSpan.FromSeconds(StaleAfterSeconds);
}
