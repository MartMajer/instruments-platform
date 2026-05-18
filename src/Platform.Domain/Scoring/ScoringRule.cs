namespace Platform.Domain.Scoring;

public sealed class ScoringRule
{
    private ScoringRule()
    {
    }

    private ScoringRule(
        Guid id,
        Guid templateVersionId,
        string ruleKey,
        string ruleVersion,
        string schemaVersion,
        string engineMinVersion,
        string documentHash,
        string document,
        string produces,
        string compatibility)
    {
        Id = id;
        TemplateVersionId = templateVersionId;
        RuleKey = NormalizeKey(ruleKey, nameof(ruleKey));
        RuleVersion = NormalizeRequired(ruleVersion, nameof(ruleVersion));
        SchemaVersion = NormalizeRequired(schemaVersion, nameof(schemaVersion));
        EngineMinVersion = NormalizeRequired(engineMinVersion, nameof(engineMinVersion));
        DocumentHash = NormalizeSha256(documentHash, nameof(documentHash));
        Document = ScoringRuleJson.RequireObject(document, nameof(document));
        Produces = ScoringRuleJson.RequireObject(produces, nameof(produces));
        Compatibility = ScoringRuleJson.RequireObject(compatibility, nameof(compatibility));
        Status = ScoringRuleStatuses.Draft;
        IsLocked = false;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public string RuleKey { get; private set; } = string.Empty;

    public string RuleVersion { get; private set; } = string.Empty;

    public string SchemaVersion { get; private set; } = string.Empty;

    public string EngineMinVersion { get; private set; } = string.Empty;

    public string DocumentHash { get; private set; } = string.Empty;

    public string Document { get; private set; } = "{}";

    public string Produces { get; private set; } = "{}";

    public string Compatibility { get; private set; } = "{}";

    public string Status { get; private set; } = string.Empty;

    public bool IsLocked { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid? PublishedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ScoringRule CreateDraft(
        Guid id,
        Guid templateVersionId,
        string ruleKey,
        string ruleVersion,
        string schemaVersion,
        string engineMinVersion,
        string documentHash,
        string document,
        string produces,
        string compatibility = "{}")
    {
        return new ScoringRule(
            id,
            templateVersionId,
            ruleKey,
            ruleVersion,
            schemaVersion,
            engineMinVersion,
            documentHash,
            document,
            produces,
            compatibility);
    }

    public void Publish(Guid? publishedBy, DateTimeOffset publishedAt)
    {
        if (Status != ScoringRuleStatuses.Draft)
        {
            throw new InvalidOperationException("Only draft scoring rules can be published.");
        }

        Status = ScoringRuleStatuses.Published;
        IsLocked = true;
        PublishedAt = publishedAt;
        PublishedBy = publishedBy;
        UpdatedAt = publishedAt;
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        if (Status != ScoringRuleStatuses.Published)
        {
            throw new InvalidOperationException("Only published scoring rules can be retired.");
        }

        Status = ScoringRuleStatuses.Retired;
        IsLocked = true;
        UpdatedAt = retiredAt;
    }

    private static string NormalizeKey(string value, string parameterName)
    {
        return NormalizeRequired(value, parameterName).ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string NormalizeSha256(string value, string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !char.IsAsciiHexDigitLower(character)))
        {
            throw new ArgumentException("Document hash must be a SHA-256 hex string.", parameterName);
        }

        return normalized;
    }
}
