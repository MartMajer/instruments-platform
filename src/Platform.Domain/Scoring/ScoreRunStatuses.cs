namespace Platform.Domain.Scoring;

public static class ScoreRunStatuses
{
    public const string Running = "running";
    public const string Success = "success";
    public const string Failed = "failed";

    public static bool IsKnown(string status)
    {
        return status is Running or Success or Failed;
    }
}
