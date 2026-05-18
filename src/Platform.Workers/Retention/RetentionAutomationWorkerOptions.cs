namespace Platform.Workers.Retention;

public sealed class RetentionAutomationWorkerOptions
{
    public const string SectionName = "RetentionAutomation";
    public const int MaxAllowedBatchesPerTenant = 100;
    public const int MaxAllowedTenantsPerTick = 500;

    public bool Enabled { get; set; } = false;

    public int InitialDelaySeconds { get; set; } = 30;

    public int IntervalSeconds { get; set; } = 3600;

    public int MaxBatchesPerTenant { get; set; } = 10;

    public int MaxTenantsPerTick { get; set; } = 50;

    public TimeSpan InitialDelay => TimeSpan.FromSeconds(InitialDelaySeconds);

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);
}
