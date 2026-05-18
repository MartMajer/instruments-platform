using System.Text.Json;

namespace Platform.Domain.Auditing;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        Guid id,
        DateTimeOffset occurredAt,
        Guid tenantId,
        string actorType,
        Guid? actorId,
        Guid? correlationId,
        string entityType,
        string entityId,
        string changeKind,
        JsonDocument? before,
        JsonDocument? after,
        string? reason)
    {
        if (!AuditActorTypes.IsKnown(actorType))
        {
            throw new ArgumentException("Unknown audit actor type.", nameof(actorType));
        }

        if (!AuditChangeKinds.IsKnown(changeKind))
        {
            throw new ArgumentException("Unknown audit change kind.", nameof(changeKind));
        }

        Id = id;
        OccurredAt = occurredAt;
        TenantId = tenantId;
        ActorType = actorType;
        ActorId = actorId;
        CorrelationId = correlationId;
        EntityType = entityType;
        EntityId = entityId;
        ChangeKind = changeKind;
        Before = before;
        After = after;
        Reason = reason;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? ActorId { get; private set; }

    public string ActorType { get; private set; } = AuditActorTypes.System;

    public Guid? CorrelationId { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string ChangeKind { get; private set; } = string.Empty;

    public JsonDocument? Before { get; private set; }

    public JsonDocument? After { get; private set; }

    public string? Reason { get; private set; }
}
