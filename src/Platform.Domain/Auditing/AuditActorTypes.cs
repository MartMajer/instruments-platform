namespace Platform.Domain.Auditing;

public static class AuditActorTypes
{
    public const string User = "user";

    public const string System = "system";

    public const string Service = "service";

    public static bool IsKnown(string actorType)
    {
        return actorType is User or System or Service;
    }
}
