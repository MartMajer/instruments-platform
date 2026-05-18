namespace Platform.Domain.Responses;

public sealed class Answer
{
    private Answer()
    {
    }

    public Answer(
        Guid id,
        Guid tenantId,
        Guid sessionId,
        Guid questionId,
        string? value,
        string? comment = null,
        bool isSkipped = false,
        bool isNa = false,
        DateTimeOffset? answeredAt = null)
    {
        if (isSkipped && isNa)
        {
            throw new ArgumentException("Answer cannot be both skipped and not applicable.");
        }

        Id = id;
        TenantId = tenantId;
        SessionId = sessionId;
        QuestionId = questionId;
        Value = value is null ? null : ResponseJson.RequireValue(value, nameof(value));
        Comment = NormalizeOptional(comment);
        IsSkipped = isSkipped;
        IsNa = isNa;
        AnsweredAt = answeredAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid SessionId { get; private set; }

    public Guid QuestionId { get; private set; }

    public string? Value { get; private set; }

    public string? Comment { get; private set; }

    public bool IsSkipped { get; private set; }

    public bool IsNa { get; private set; }

    public DateTimeOffset AnsweredAt { get; private set; }

    public void UpdateValue(
        ResponseSession session,
        string? value,
        string? comment,
        bool isSkipped,
        bool isNa,
        DateTimeOffset? answeredAt = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Id != SessionId || session.TenantId != TenantId)
        {
            throw new ArgumentException("Answer does not belong to the supplied response session.", nameof(session));
        }

        session.EnsureCanAcceptAnswers();

        if (isSkipped && isNa)
        {
            throw new ArgumentException("Answer cannot be both skipped and not applicable.");
        }

        Value = value is null ? null : ResponseJson.RequireValue(value, nameof(value));
        Comment = NormalizeOptional(comment);
        IsSkipped = isSkipped;
        IsNa = isNa;
        AnsweredAt = answeredAt ?? DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
