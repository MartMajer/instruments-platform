namespace Platform.Domain.Responses;

public sealed class ResponseSession
{
    private ResponseSession()
    {
    }

    public ResponseSession(
        Guid id,
        Guid tenantId,
        Guid assignmentId,
        string locale,
        Guid? participantCodeId = null,
        Guid? consentRecordId = null,
        DateTimeOffset? startedAt = null,
        string? publicHandleHash = null,
        DateTimeOffset? publicHandleIssuedAt = null,
        string? ipHash = null,
        string? userAgentHash = null)
    {
        Id = id;
        TenantId = tenantId;
        AssignmentId = assignmentId;
        ParticipantCodeId = participantCodeId;
        ConsentRecordId = consentRecordId;
        Locale = NormalizeRequired(locale, nameof(locale));
        StartedAt = startedAt ?? DateTimeOffset.UtcNow;
        PublicHandleHash = NormalizeOptional(publicHandleHash);
        PublicHandleIssuedAt = publicHandleIssuedAt;
        IpHash = NormalizeOptional(ipHash);
        UserAgentHash = NormalizeOptional(userAgentHash);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid? ParticipantCodeId { get; private set; }

    public Guid? ConsentRecordId { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public int? TimeTakenMs { get; private set; }

    public string Locale { get; private set; } = "en";

    public string? PublicHandleHash { get; private set; }

    public DateTimeOffset? PublicHandleIssuedAt { get; private set; }

    public string? IpHash { get; private set; }

    public string? UserAgentHash { get; private set; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void EnsureCanAcceptAnswers()
    {
        if (SubmittedAt.HasValue)
        {
            throw new InvalidOperationException("Submitted response sessions cannot accept answer changes.");
        }
    }

    public void Submit(DateTimeOffset submittedAt, int? timeTakenMs = null)
    {
        EnsureCanAcceptAnswers();

        if (timeTakenMs is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeTakenMs), "Time taken must not be negative.");
        }

        SubmittedAt = submittedAt;
        TimeTakenMs = timeTakenMs;
        UpdatedAt = submittedAt;
    }

    public void Anonymize(DateTimeOffset anonymizedAt)
    {
        ParticipantCodeId = null;
        ConsentRecordId = null;
        PublicHandleHash = null;
        PublicHandleIssuedAt = null;
        IpHash = null;
        UserAgentHash = null;
        AnonymizedAt = anonymizedAt;
        UpdatedAt = anonymizedAt;
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
