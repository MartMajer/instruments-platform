namespace Platform.Domain.Campaigns;

public sealed class Assignment
{
    private Assignment()
    {
    }

    private Assignment(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        Guid? targetSubjectId,
        Guid? respondentSubjectId,
        Guid? inviteTokenId,
        string role,
        bool anonymous,
        DateTimeOffset? dueAt)
    {
        if (anonymous)
        {
            if (!inviteTokenId.HasValue || respondentSubjectId.HasValue)
            {
                throw new ArgumentException("Anonymous assignments require an invitation token and no respondent subject.");
            }
        }
        else if (!respondentSubjectId.HasValue || inviteTokenId.HasValue)
        {
            throw new ArgumentException("Identified assignments require a respondent subject and no invitation token.");
        }

        Id = id;
        TenantId = tenantId;
        CampaignId = campaignId;
        TargetSubjectId = targetSubjectId;
        RespondentSubjectId = respondentSubjectId;
        InviteTokenId = inviteTokenId;
        Role = NormalizeRequired(role, nameof(role));
        Status = AssignmentStatuses.Pending;
        DueAt = dueAt;
        Anonymous = anonymous;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid? TargetSubjectId { get; private set; }

    public Guid? RespondentSubjectId { get; private set; }

    public Guid? InviteTokenId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public DateTimeOffset? DueAt { get; private set; }

    public bool Anonymous { get; private set; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Assignment CreateIdentified(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        string role,
        Guid respondentSubjectId,
        Guid? targetSubjectId = null,
        DateTimeOffset? dueAt = null)
    {
        return new Assignment(
            id,
            tenantId,
            campaignId,
            targetSubjectId,
            respondentSubjectId,
            inviteTokenId: null,
            role,
            anonymous: false,
            dueAt);
    }

    public static Assignment CreateAnonymous(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        string role,
        Guid inviteTokenId,
        Guid? targetSubjectId = null,
        DateTimeOffset? dueAt = null)
    {
        return new Assignment(
            id,
            tenantId,
            campaignId,
            targetSubjectId,
            respondentSubjectId: null,
            inviteTokenId,
            role,
            anonymous: true,
            dueAt);
    }

    public void Anonymize(DateTimeOffset anonymizedAt)
    {
        TargetSubjectId = null;
        RespondentSubjectId = null;
        InviteTokenId = null;
        Anonymous = true;
        AnonymizedAt = anonymizedAt;
        UpdatedAt = anonymizedAt;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
