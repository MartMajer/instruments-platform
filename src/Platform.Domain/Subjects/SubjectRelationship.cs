namespace Platform.Domain.Subjects;

public sealed class SubjectRelationship
{
    private SubjectRelationship()
    {
    }

    public SubjectRelationship(
        Guid id,
        Guid tenantId,
        Guid subjectId,
        Guid relatedSubjectId,
        string relationshipType,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipType);
        ValidateSelfRelationship(subjectId, relatedSubjectId, relationshipType);
        ValidateRange(validFrom, validTo);

        Id = id;
        TenantId = tenantId;
        SubjectId = subjectId;
        RelatedSubjectId = relatedSubjectId;
        RelationshipType = relationshipType;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid RelatedSubjectId { get; private set; }

    public string RelationshipType { get; private set; } = string.Empty;

    public DateOnly? ValidFrom { get; private set; }

    public DateOnly? ValidTo { get; private set; }

    public void End(DateOnly validTo)
    {
        if (ValidTo.HasValue)
        {
            return;
        }

        ValidateRange(ValidFrom, validTo);
        ValidTo = validTo;
    }

    private static void ValidateSelfRelationship(
        Guid subjectId,
        Guid relatedSubjectId,
        string relationshipType)
    {
        var isSelfType = relationshipType == SubjectRelationshipTypes.Self;
        var pointsToSelf = subjectId == relatedSubjectId;

        if (pointsToSelf != isSelfType)
        {
            throw new ArgumentException(
                "Only self relationships may point a subject at itself, and self relationships must point at itself.",
                nameof(relationshipType));
        }
    }

    private static void ValidateRange(DateOnly? validFrom, DateOnly? validTo)
    {
        if (validFrom.HasValue && validTo.HasValue && validTo.Value < validFrom.Value)
        {
            throw new ArgumentException("Relationship valid-to date cannot be before valid-from date.", nameof(validTo));
        }
    }
}
