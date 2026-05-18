namespace Platform.Domain.Operations;

public sealed class WorkerHeartbeat
{
    public const int WorkerNameMaxLength = 128;
    public const int InstanceIdMaxLength = 128;

    private WorkerHeartbeat()
    {
    }

    public WorkerHeartbeat(
        Guid id,
        string workerName,
        string instanceId,
        DateTimeOffset observedAt)
    {
        Id = id;
        WorkerName = EnsureSafeIdentifier(workerName, nameof(workerName), WorkerNameMaxLength);
        InstanceId = EnsureSafeIdentifier(instanceId, nameof(instanceId), InstanceIdMaxLength);
        StartedAt = observedAt;
        LastSeenAt = observedAt;
        CreatedAt = observedAt;
        UpdatedAt = observedAt;
    }

    public Guid Id { get; private set; }

    public string WorkerName { get; private set; } = string.Empty;

    public string InstanceId { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void RecordSeen(DateTimeOffset observedAt)
    {
        LastSeenAt = observedAt;
        UpdatedAt = observedAt;
    }

    private static string EnsureSafeIdentifier(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);
        }

        if (trimmed.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
        {
            throw new ArgumentException("Value contains unsupported characters.", parameterName);
        }

        return trimmed;
    }
}
