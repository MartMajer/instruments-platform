using System.Text.Json;
using Platform.SharedKernel;

namespace Platform.Domain.Outbox;

public sealed class OutboxEvent
{
    public const int MaxLastErrorLength = 2_000;
    public const string DeadLetterPrefix = "DEAD_LETTER:";

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6)
    ];

    private OutboxEvent()
    {
    }

    private OutboxEvent(
        Guid id,
        Guid tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventType,
        JsonDocument payload,
        DateTimeOffset createdAt,
        Guid? correlationId)
    {
        Id = id;
        TenantId = tenantId;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        EventType = eventType;
        Payload = payload;
        CreatedAt = createdAt;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AggregateId { get; private set; }

    public string AggregateType { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid? CorrelationId { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? NextRetryAt { get; private set; }

    public bool IsDeadLetter => LastError?.StartsWith(DeadLetterPrefix, StringComparison.Ordinal) == true;

    public static OutboxEvent Create(
        Guid tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventType,
        JsonDocument payload,
        Guid? correlationId)
    {
        OutboxPayload.EnsureWithinLimit(payload, nameof(payload));
        var safeAggregateType = OutboxTextSafety.EnsureSafeIdentifier(aggregateType, nameof(aggregateType));
        var safeEventType = OutboxTextSafety.EnsureSafeIdentifier(eventType, nameof(eventType));

        return new OutboxEvent(
            PlatformIds.NewId(),
            tenantId,
            aggregateId,
            safeAggregateType,
            safeEventType,
            payload,
            DateTimeOffset.UtcNow,
            correlationId);
    }

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        PublishedAt = publishedAt;
        NextRetryAt = null;
        LastError = null;
    }

    public void MarkFailed(string error, DateTimeOffset failedAt)
    {
        RetryCount++;
        var safeError = OutboxTextSafety.SanitizeFailureText(error);

        var shouldDeadLetter = RetryCount >= 8 || failedAt - CreatedAt > TimeSpan.FromHours(24);
        if (shouldDeadLetter)
        {
            LastError = Truncate($"{DeadLetterPrefix} {safeError}");
            NextRetryAt = null;
            return;
        }

        LastError = Truncate(safeError);
        var delayIndex = Math.Min(RetryCount - 1, RetryDelays.Length - 1);
        NextRetryAt = failedAt.Add(RetryDelays[delayIndex]);
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxLastErrorLength
            ? value
            : value[..MaxLastErrorLength];
    }
}
