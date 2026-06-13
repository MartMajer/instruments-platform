namespace Platform.Domain.Integrations;

public static class DirectoryConnectionProviders
{
    public const string MicrosoftGraph = "microsoft_graph";

    public static bool IsKnown(string value)
    {
        return value is MicrosoftGraph;
    }
}

public static class DirectoryConnectionStatuses
{
    public const string PendingConsent = "pending_consent";
    public const string Active = "active";
    public const string ConsentRequired = "consent_required";
    public const string Revoked = "revoked";
    public const string Failed = "failed";
    public const string Disconnected = "disconnected";

    public static bool IsKnown(string value)
    {
        return value is PendingConsent or Active or ConsentRequired or Revoked or Failed or Disconnected;
    }
}

public static class DirectoryConnectionConsentRequestStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Expired = "expired";

    public static bool IsKnown(string value)
    {
        return value is Pending or Completed or Failed or Expired;
    }
}

public static class DirectoryImportRuleStatuses
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Archived = "archived";

    public static bool IsKnown(string value)
    {
        return value is Draft or Active or Archived;
    }
}

public static class DirectoryImportStalePolicies
{
    public const string None = "none";
    public const string MarkStale = "mark_stale";

    public static bool IsKnown(string value)
    {
        return value is None or MarkStale;
    }
}

public static class DirectoryImportRunModes
{
    public const string Preview = "preview";
    public const string Apply = "apply";

    public static bool IsKnown(string value)
    {
        return value is Preview or Apply;
    }
}

public static class DirectoryImportRunStatuses
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";

    public static bool IsKnown(string value)
    {
        return value is Queued or Running or Succeeded or Failed or Canceled;
    }
}
