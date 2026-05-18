using Platform.Domain.Subjects;

namespace Platform.UnitTests.Domain;

public sealed class SubjectEntitiesTests
{
    [Fact]
    public void Subject_defaults_to_empty_attribute_object()
    {
        var tenantId = Guid.NewGuid();
        var subject = new Subject(
            Guid.NewGuid(),
            tenantId,
            externalId: "emp-001",
            displayName: "Ana Horvat");

        Assert.Equal(tenantId, subject.TenantId);
        Assert.Equal("emp-001", subject.ExternalId);
        Assert.Equal("Ana Horvat", subject.DisplayName);
        Assert.Equal("en", subject.Locale);
        Assert.Equal("{}", subject.Attributes);
        Assert.Null(subject.UserAccountId);
    }

    [Fact]
    public void Subject_rejects_non_object_attribute_json()
    {
        Assert.Throws<ArgumentException>(() => new Subject(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attributes: """["not-an-object"]"""));
    }

    [Fact]
    public void Subject_membership_rejects_date_range_ending_before_it_starts()
    {
        Assert.Throws<ArgumentException>(() => new SubjectMembership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubjectGroupRoles.Member,
            new DateOnly(2026, 5, 6),
            new DateOnly(2026, 5, 5)));
    }

    [Fact]
    public void Subject_relationship_rejects_subject_pointing_to_itself_unless_type_is_self()
    {
        var subjectId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new SubjectRelationship(
            Guid.NewGuid(),
            Guid.NewGuid(),
            subjectId,
            subjectId,
            SubjectRelationshipTypes.ManagerOf));
    }

    [Fact]
    public void Subject_relationship_allows_self_relationship_for_self_type()
    {
        var subjectId = Guid.NewGuid();
        var relationship = new SubjectRelationship(
            Guid.NewGuid(),
            Guid.NewGuid(),
            subjectId,
            subjectId,
            SubjectRelationshipTypes.Self);

        Assert.Equal(subjectId, relationship.SubjectId);
        Assert.Equal(subjectId, relationship.RelatedSubjectId);
        Assert.Equal(SubjectRelationshipTypes.Self, relationship.RelationshipType);
    }

    [Fact]
    public void Subject_relationship_can_be_ended_once()
    {
        var relationship = new SubjectRelationship(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubjectRelationshipTypes.ManagerOf,
            validFrom: new DateOnly(2026, 5, 1));

        relationship.End(new DateOnly(2026, 5, 15));
        relationship.End(new DateOnly(2026, 5, 16));

        Assert.Equal(new DateOnly(2026, 5, 15), relationship.ValidTo);
    }
}
