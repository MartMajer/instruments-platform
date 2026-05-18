namespace Platform.Workers.Reports;

public sealed class ReportPdfArtifactWorkerOptions
{
    public const string SectionName = "ReportPdfArtifacts";
    public const int MaxAllowedArtifactsPerTenant = 100;
    public const int MaxAllowedTenantsPerTick = 500;

    public bool Enabled { get; set; } = false;

    public int InitialDelaySeconds { get; set; } = 30;

    public int IntervalSeconds { get; set; } = 60;

    public int RenderingTimeoutMinutes { get; set; } = 30;

    public int MaxArtifactsPerTenant { get; set; } = 10;

    public int MaxTenantsPerTick { get; set; } = 50;

    public TimeSpan InitialDelay => TimeSpan.FromSeconds(InitialDelaySeconds);

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);

    public TimeSpan RenderingTimeout => TimeSpan.FromMinutes(RenderingTimeoutMinutes);
}
