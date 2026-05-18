namespace Platform.Domain.Auditing;

public static class AuditChangeKinds
{
    public const string Added = "added";

    public const string Modified = "modified";

    public const string Deleted = "deleted";

    public static bool IsKnown(string changeKind)
    {
        return changeKind is Added or Modified or Deleted;
    }
}
