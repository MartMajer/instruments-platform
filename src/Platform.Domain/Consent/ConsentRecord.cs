namespace Platform.Domain.Consent;

public sealed class ConsentRecord
{
    private ConsentRecord()
    {
    }

    public ConsentRecord(
        Guid id,
        Guid tenantId,
        Guid consentDocumentId,
        Guid campaignId,
        Guid assignmentId,
        string locale,
        string acceptedGrants,
        DateTimeOffset acceptedAt,
        Guid? subjectId = null)
    {
        Id = id;
        TenantId = tenantId;
        ConsentDocumentId = consentDocumentId;
        CampaignId = campaignId;
        AssignmentId = assignmentId;
        SubjectId = subjectId;
        Locale = NormalizeRequired(locale, nameof(locale));
        AcceptedGrants = ConsentDocument.NormalizeGrantArray(acceptedGrants, nameof(acceptedGrants));
        AcceptedAt = acceptedAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ConsentDocumentId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid? SubjectId { get; private set; }

    public string Locale { get; private set; } = "en";

    public string AcceptedGrants { get; private set; } = "[]";

    public DateTimeOffset AcceptedAt { get; private set; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void Anonymize(DateTimeOffset anonymizedAt)
    {
        SubjectId = null;
        AnonymizedAt = anonymizedAt;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
