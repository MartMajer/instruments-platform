namespace Platform.Domain.Campaigns;

public sealed class InvitationToken
{
    private InvitationToken()
    {
    }

    public InvitationToken(
        Guid id,
        Guid tenantId,
        Guid campaignId,
        string tokenHash,
        string channel,
        string? recipient = null,
        DateTimeOffset? expiresAt = null,
        Guid? assignmentId = null,
        Guid? respondentSubjectId = null)
    {
        if (!InvitationTokenChannels.IsKnown(channel))
        {
            throw new ArgumentException("Unknown invitation token channel.", nameof(channel));
        }

        if (channel == InvitationTokenChannels.IdentifiedQueue && !respondentSubjectId.HasValue)
        {
            throw new ArgumentException(
                "Identified queue tokens require a respondent subject.",
                nameof(respondentSubjectId));
        }

        if (channel != InvitationTokenChannels.IdentifiedQueue && respondentSubjectId.HasValue)
        {
            throw new ArgumentException(
                "Respondent subject is only valid for identified queue tokens.",
                nameof(respondentSubjectId));
        }

        Id = id;
        TenantId = tenantId;
        CampaignId = campaignId;
        TokenHash = NormalizeRequired(tokenHash, nameof(tokenHash));
        Channel = channel;
        Recipient = NormalizeOptional(recipient);
        ExpiresAt = expiresAt;
        AssignmentId = assignmentId;
        RespondentSubjectId = respondentSubjectId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid? AssignmentId { get; private set; }

    public Guid? RespondentSubjectId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string Channel { get; private set; } = string.Empty;

    public string? Recipient { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void ReissueHash(string tokenHash, DateTimeOffset? expiresAt = null)
    {
        TokenHash = NormalizeRequired(tokenHash, nameof(tokenHash));
        ExpiresAt = expiresAt;
        UsedAt = null;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        if (UsedAt.HasValue)
        {
            throw new InvalidOperationException("Invitation token has already been used.");
        }

        UsedAt = usedAt;
    }

    public void ScrubForWithdrawal(DateTimeOffset scrubbedAt)
    {
        AssignmentId = null;
        RespondentSubjectId = null;
        Recipient = null;
        TokenHash = $"withdrawn:{Id:N}";
        ExpiresAt = scrubbedAt;
        UsedAt ??= scrubbedAt;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
