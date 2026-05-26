namespace Platform.Domain.Campaigns;

public sealed class CampaignSeries
{
    public const int NameMaxLength = 256;
    public const int ArchiveReasonMaxLength = 256;
    public const int StudyPurposeMaxLength = 1000;
    public const int StudyAudienceMaxLength = 1000;
    public const int StudyDesignTypeMaxLength = 64;
    public const int StudyIntendedUseMaxLength = 64;
    public const int StudyInterpretationBoundaryMaxLength = 1000;
    public const int StudyOwnerNotesMaxLength = 2000;

    private CampaignSeries()
    {
    }

    public CampaignSeries(
        Guid id,
        Guid tenantId,
        string name,
        byte[] codeSalt,
        Guid? workspaceId = null,
        Guid? ethicsApprovalId = null,
        DateOnly? retentionUntil = null,
        string studyKind = CampaignSeriesStudyKinds.Own,
        string? sampleScenario = null,
        string? studyPurpose = null,
        string? studyAudience = null,
        string? studyDesignType = null,
        string? studyIntendedUse = null,
        string? studyInterpretationBoundary = null,
        string? studyOwnerNotes = null)
    {
        ArgumentNullException.ThrowIfNull(codeSalt);
        if (codeSalt.Length != 32)
        {
            throw new ArgumentException("Campaign series code salt must be exactly 32 bytes.", nameof(codeSalt));
        }

        Id = id;
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        Name = NormalizeRequired(name, nameof(name));
        EthicsApprovalId = ethicsApprovalId;
        RetentionUntil = retentionUntil;
        StudyKind = NormalizeStudyKind(studyKind);
        SampleScenario = NormalizeSampleScenario(StudyKind, sampleScenario);
        StudyPurpose = NormalizeOptional(studyPurpose, StudyPurposeMaxLength);
        StudyAudience = NormalizeOptional(studyAudience, StudyAudienceMaxLength);
        StudyDesignType = NormalizeStudyDesignType(studyDesignType);
        StudyIntendedUse = NormalizeStudyIntendedUse(studyIntendedUse);
        StudyInterpretationBoundary = NormalizeOptional(studyInterpretationBoundary, StudyInterpretationBoundaryMaxLength);
        StudyOwnerNotes = NormalizeOptional(studyOwnerNotes, StudyOwnerNotesMaxLength);
        CodeSalt = [.. codeSalt];
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid? EthicsApprovalId { get; private set; }

    public DateOnly? RetentionUntil { get; private set; }

    public string StudyKind { get; private set; } = CampaignSeriesStudyKinds.Own;

    public string? SampleScenario { get; private set; }

    public string? StudyPurpose { get; private set; }

    public string? StudyAudience { get; private set; }

    public string? StudyDesignType { get; private set; }

    public string? StudyIntendedUse { get; private set; }

    public string? StudyInterpretationBoundary { get; private set; }

    public string? StudyOwnerNotes { get; private set; }

    public byte[] CodeSalt { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public Guid? ArchivedByUserId { get; private set; }

    public string? ArchiveReason { get; private set; }

    public bool Archived => ArchivedAt.HasValue;

    public bool IsSample => StudyKind == CampaignSeriesStudyKinds.Sample;

    public void Rename(string name, DateTimeOffset renamedAt)
    {
        Name = NormalizeRequired(name, nameof(name));
        UpdatedAt = renamedAt;
    }

    public void Archive(string? reason, Guid archivedByUserId, DateTimeOffset archivedAt)
    {
        if (Archived)
        {
            return;
        }

        ArchivedAt = archivedAt;
        ArchivedByUserId = archivedByUserId;
        ArchiveReason = NormalizeOptional(reason, ArchiveReasonMaxLength);
        UpdatedAt = archivedAt;
    }

    public void Restore(DateTimeOffset restoredAt)
    {
        if (!Archived)
        {
            return;
        }

        ArchivedAt = null;
        ArchivedByUserId = null;
        ArchiveReason = null;
        UpdatedAt = restoredAt;
    }

    public void UpdateStudyBrief(
        string? purpose,
        string? audience,
        string? designType,
        string? intendedUse,
        string? interpretationBoundary,
        string? ownerNotes,
        DateTimeOffset updatedAt)
    {
        StudyPurpose = NormalizeOptional(purpose, StudyPurposeMaxLength);
        StudyAudience = NormalizeOptional(audience, StudyAudienceMaxLength);
        StudyDesignType = NormalizeStudyDesignType(designType);
        StudyIntendedUse = NormalizeStudyIntendedUse(intendedUse);
        StudyInterpretationBoundary = NormalizeOptional(
            interpretationBoundary,
            StudyInterpretationBoundaryMaxLength);
        StudyOwnerNotes = NormalizeOptional(ownerNotes, StudyOwnerNotesMaxLength);
        UpdatedAt = updatedAt;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Campaign series name must be at most {NameMaxLength} characters.",
                parameterName);
        }

        return normalized;
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

    private static string NormalizeStudyKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();
        if (!CampaignSeriesStudyKinds.IsKnown(normalized))
        {
            throw new ArgumentException("Campaign series study kind is not supported.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeSampleScenario(string studyKind, string? sampleScenario)
    {
        var normalized = sampleScenario?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (studyKind == CampaignSeriesStudyKinds.Sample)
            {
                throw new ArgumentException("Sample campaign series must specify a sample scenario.", nameof(sampleScenario));
            }

            return null;
        }

        if (studyKind == CampaignSeriesStudyKinds.Own)
        {
            throw new ArgumentException("Own campaign series must not specify a sample scenario.", nameof(sampleScenario));
        }

        if (!CampaignSeriesSampleScenarios.IsKnown(normalized))
        {
            throw new ArgumentException("Campaign series sample scenario is not supported.", nameof(sampleScenario));
        }

        return normalized;
    }

    private static string? NormalizeStudyDesignType(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (!CampaignSeriesStudyDesignTypes.IsKnown(normalized))
        {
            throw new ArgumentException("Campaign series study design type is not supported.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeStudyIntendedUse(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (!CampaignSeriesStudyIntendedUseTypes.IsKnown(normalized))
        {
            throw new ArgumentException("Campaign series study intended use is not supported.", nameof(value));
        }

        return normalized;
    }
}

public static class CampaignSeriesStudyKinds
{
    public const string Own = "own";
    public const string Sample = "sample";

    public static bool IsKnown(string value) =>
        value is Own or Sample;
}

public static class CampaignSeriesSampleScenarios
{
    public const string MixedLifecycle = "mixed_lifecycle";
    public const string Longitudinal = "longitudinal";
    public const string Setup = "setup";
    public const string InCollection = "in_collection";
    public const string Completed = "completed";
    public const string Blocked = "blocked";

    public static bool IsKnown(string value) =>
        value is MixedLifecycle or Longitudinal or Setup or InCollection or Completed or Blocked;
}

public static class CampaignSeriesStudyDesignTypes
{
    public const string SingleWave = "single_wave";
    public const string RepeatedGroupTrend = "repeated_group_trend";
    public const string RepeatedLinkedChange = "repeated_linked_change";

    public static bool IsKnown(string value) =>
        value is SingleWave or RepeatedGroupTrend or RepeatedLinkedChange;
}

public static class CampaignSeriesStudyIntendedUseTypes
{
    public const string InternalReview = "internal_review";
    public const string ResearchAnalysis = "research_analysis";
    public const string ClientReport = "client_report";

    public static bool IsKnown(string value) =>
        value is InternalReview or ResearchAnalysis or ClientReport;
}
