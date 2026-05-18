namespace Platform.Domain.Campaigns;

public sealed class CampaignLaunchSnapshot
{
    private const string DefaultLaunchPacket =
        """{"schema_version":1,"template":{"status":"unknown"},"instrument":{"status":"unknown"},"scoring":{"status":"unknown"},"policies":{"consent":"unknown","retention":"unknown","disclosure":"unknown"},"identity":{"response_identity_mode":"unknown"},"respondent_rules":{"materialization":"unknown","materialized_assignment_count":0},"launch_readiness":{"status":"unknown"},"provenance":{"source":"legacy_constructor_default"}}""";

    private CampaignLaunchSnapshot()
    {
    }

    public CampaignLaunchSnapshot(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        Guid? campaignSeriesId,
        Guid templateVersionId,
        Guid scoringRuleId,
        string responseIdentityMode,
        string defaultLocale,
        int templateQuestionCount,
        string scoringRuleDocumentHash,
        string launchReadiness,
        DateTimeOffset launchedAt,
        Guid? launchedBy = null,
        Guid? consentDocumentId = null,
        Guid? retentionPolicyId = null,
        Guid? disclosurePolicyId = null,
        string launchPacket = DefaultLaunchPacket)
    {
        if (!ResponseIdentityModes.IsKnown(responseIdentityMode))
        {
            throw new ArgumentException("Unknown response identity mode.", nameof(responseIdentityMode));
        }

        if (templateQuestionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(templateQuestionCount),
                "Template question count must be positive.");
        }

        Id = id;
        TenantId = tenantId;
        CampaignId = campaignId;
        CampaignSeriesId = campaignSeriesId;
        TemplateVersionId = templateVersionId;
        ScoringRuleId = scoringRuleId;
        ConsentDocumentId = consentDocumentId;
        RetentionPolicyId = retentionPolicyId;
        DisclosurePolicyId = disclosurePolicyId;
        ResponseIdentityMode = responseIdentityMode;
        DefaultLocale = NormalizeRequired(defaultLocale, nameof(defaultLocale));
        TemplateQuestionCount = templateQuestionCount;
        ScoringRuleDocumentHash = NormalizeRequired(scoringRuleDocumentHash, nameof(scoringRuleDocumentHash));
        LaunchReadiness = CampaignJson.RequireObject(launchReadiness, nameof(launchReadiness));
        LaunchPacket = CampaignJson.RequireObject(launchPacket, nameof(launchPacket));
        LaunchedAt = launchedAt;
        LaunchedBy = launchedBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid? CampaignSeriesId { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public Guid ScoringRuleId { get; private set; }

    public Guid? ConsentDocumentId { get; private set; }

    public Guid? RetentionPolicyId { get; private set; }

    public Guid? DisclosurePolicyId { get; private set; }

    public string ResponseIdentityMode { get; private set; } = string.Empty;

    public string DefaultLocale { get; private set; } = "en";

    public int TemplateQuestionCount { get; private set; }

    public string ScoringRuleDocumentHash { get; private set; } = string.Empty;

    public string LaunchReadiness { get; private set; } = "{}";

    public string LaunchPacket { get; private set; } = DefaultLaunchPacket;

    public DateTimeOffset LaunchedAt { get; private set; }

    public Guid? LaunchedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
