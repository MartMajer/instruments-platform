namespace Platform.Domain.Reports;

public static class ExportArtifactTargetKinds
{
    public const string Campaign = "campaign";
    public const string CampaignSeries = "campaign_series";

    public static bool IsKnown(string value)
    {
        return value is Campaign or CampaignSeries;
    }
}
