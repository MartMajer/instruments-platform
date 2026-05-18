namespace Platform.Domain.Subjects;

public sealed class SubjectMembership
{
    private SubjectMembership()
    {
    }

    public SubjectMembership(
        Guid subjectId,
        Guid groupId,
        string? roleInGroup = null,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        ValidateRange(validFrom, validTo);

        SubjectId = subjectId;
        GroupId = groupId;
        RoleInGroup = NormalizeOptional(roleInGroup);
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public Guid SubjectId { get; private set; }

    public Guid GroupId { get; private set; }

    public string? RoleInGroup { get; private set; }

    public DateOnly? ValidFrom { get; private set; }

    public DateOnly? ValidTo { get; private set; }

    private static void ValidateRange(DateOnly? validFrom, DateOnly? validTo)
    {
        if (validFrom.HasValue && validTo.HasValue && validTo.Value < validFrom.Value)
        {
            throw new ArgumentException("Membership valid-to date cannot be before valid-from date.", nameof(validTo));
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
