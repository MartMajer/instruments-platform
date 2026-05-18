namespace Platform.Domain.Campaigns;

public sealed class Campaign
{
    public const int CloseReasonMaxLength = 256;

    private Campaign()
    {
    }

    public Campaign(
        Guid id,
        Guid tenantId,
        Guid templateVersionId,
        string name,
        string responseIdentityMode,
        Guid? workspaceId = null,
        Guid? campaignSeriesId = null,
        string status = CampaignStatuses.Draft,
        DateTimeOffset? startAt = null,
        DateTimeOffset? endAt = null,
        string schedule = "{}",
        string defaultLocale = "en",
        Guid? createdBy = null)
    {
        if (!CampaignStatuses.IsKnown(status))
        {
            throw new ArgumentException("Unknown campaign status.", nameof(status));
        }

        if (!ResponseIdentityModes.IsKnown(responseIdentityMode))
        {
            throw new ArgumentException("Unknown response identity mode.", nameof(responseIdentityMode));
        }

        if (startAt.HasValue && endAt.HasValue && endAt.Value <= startAt.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(endAt), "Campaign end time must be after start time.");
        }

        Id = id;
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        CampaignSeriesId = campaignSeriesId;
        TemplateVersionId = templateVersionId;
        Name = NormalizeRequired(name, nameof(name));
        Status = status;
        ResponseIdentityMode = responseIdentityMode;
        StartAt = startAt;
        EndAt = endAt;
        Schedule = CampaignJson.RequireObject(schedule, nameof(schedule));
        DefaultLocale = NormalizeRequired(defaultLocale, nameof(defaultLocale));
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? WorkspaceId { get; private set; }

    public Guid? CampaignSeriesId { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string ResponseIdentityMode { get; private set; } = string.Empty;

    public DateTimeOffset? StartAt { get; private set; }

    public DateTimeOffset? EndAt { get; private set; }

    public string Schedule { get; private set; } = "{}";

    public string DefaultLocale { get; private set; } = "en";

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    public string? CloseReason { get; private set; }

    public bool RequiresIdentifiedAssignments => ResponseIdentityMode == ResponseIdentityModes.Identified;

    public bool RequiresAnonymousAssignments =>
        ResponseIdentityMode is ResponseIdentityModes.Anonymous or ResponseIdentityModes.AnonymousLongitudinal;

    public bool RequiresParticipantCodeAtSession => ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal;

    public void Launch(DateTimeOffset launchedAt)
    {
        if (Status is not (CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            throw new InvalidOperationException("Only draft or scheduled campaigns can be launched.");
        }

        Status = CampaignStatuses.Live;
        StartAt ??= launchedAt;
        UpdatedAt = launchedAt;
    }

    public void Close(string? reason, Guid closedByUserId, DateTimeOffset closedAt)
    {
        if (Status != CampaignStatuses.Live)
        {
            throw new InvalidOperationException("Only live campaigns can be closed.");
        }

        Status = CampaignStatuses.Closed;
        ClosedAt = closedAt;
        ClosedByUserId = closedByUserId;
        CloseReason = NormalizeOptional(reason, CloseReasonMaxLength);
        UpdatedAt = closedAt;
    }

    public bool CanAcceptAssignment(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (assignment.TenantId != TenantId || assignment.CampaignId != Id)
        {
            return false;
        }

        return RequiresIdentifiedAssignments
            ? !assignment.Anonymous
            : assignment.Anonymous;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
