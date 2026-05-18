namespace Platform.Domain.Auth;

public static class RoleAssignmentScopes
{
    public const string Tenant = "tenant";

    public const string Workspace = "workspace";

    public const string Campaign = "campaign";

    public const string CampaignSeries = "campaign_series";

    public static bool IsKnown(string scopeType)
    {
        return scopeType is Tenant or Workspace or Campaign or CampaignSeries;
    }
}
