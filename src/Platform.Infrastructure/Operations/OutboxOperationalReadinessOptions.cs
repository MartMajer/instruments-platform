namespace Platform.Infrastructure.Operations;

public sealed class OutboxOperationalReadinessOptions
{
    public const string SectionName = "OutboxOperations";

    public int DueBacklogUnreadyAfterMinutes { get; set; } = 15;

    public TimeSpan DueBacklogUnreadyAfter => TimeSpan.FromMinutes(DueBacklogUnreadyAfterMinutes);
}
