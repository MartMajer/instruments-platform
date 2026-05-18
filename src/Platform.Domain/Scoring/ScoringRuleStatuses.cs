namespace Platform.Domain.Scoring;

public static class ScoringRuleStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Retired = "retired";

    public static bool IsKnown(string status)
    {
        return status is Draft or Published or Retired;
    }
}
