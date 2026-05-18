using Platform.Domain.Auditing;

namespace Platform.Application.Auditing;

public sealed class CurrentAuditContext : ICurrentAuditContext
{
    private string _actorType = AuditActorTypes.System;
    private Guid? _actorId;
    private Guid? _correlationId;
    private string? _reason;

    public string ActorType => _actorType;

    public Guid? ActorId => _actorId;

    public Guid? CorrelationId => _correlationId;

    public string? Reason => _reason;

    public void SetActor(Guid? actorId, string actorType)
    {
        if (!AuditActorTypes.IsKnown(actorType))
        {
            throw new ArgumentException("Unknown audit actor type.", nameof(actorType));
        }

        _actorId = actorId;
        _actorType = actorType;
    }

    public void SetCorrelationId(Guid correlationId)
    {
        _correlationId = correlationId;
    }

    public void SetReason(string? reason)
    {
        _reason = reason;
    }
}
