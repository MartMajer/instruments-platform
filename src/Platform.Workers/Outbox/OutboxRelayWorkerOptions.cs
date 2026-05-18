namespace Platform.Workers.Outbox;

public sealed class OutboxRelayWorkerOptions
{
    public const string SectionName = "OutboxRelay";
    public const int MaxBatchSize = 500;

    public bool Enabled { get; set; } = true;

    public int BatchSize { get; set; } = 100;

    public int PollIntervalSeconds { get; set; } = 1;

    public bool ProcessOnStartup { get; set; } = true;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);
}
