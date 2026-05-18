namespace Platform.Application.Auditing;

public interface ICurrentAuditContext
{
    string ActorType { get; }

    Guid? ActorId { get; }

    Guid? CorrelationId { get; }

    string? Reason { get; }

    void SetActor(Guid? actorId, string actorType);

    void SetCorrelationId(Guid correlationId);

    void SetReason(string? reason);
}
