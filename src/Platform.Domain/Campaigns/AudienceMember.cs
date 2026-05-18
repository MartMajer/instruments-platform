namespace Platform.Domain.Campaigns;

public sealed class AudienceMember
{
    private AudienceMember()
    {
    }

    public AudienceMember(
        Guid audienceId,
        Guid subjectId,
        DateTimeOffset? addedAt = null)
    {
        AudienceId = audienceId;
        SubjectId = subjectId;
        AddedAt = addedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid AudienceId { get; private set; }

    public Guid SubjectId { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public void Remove(DateTimeOffset removedAt)
    {
        if (removedAt < AddedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(removedAt), "Audience member removal cannot predate addition.");
        }

        RemovedAt = removedAt;
    }
}
