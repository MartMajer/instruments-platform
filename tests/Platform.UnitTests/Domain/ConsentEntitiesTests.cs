using Platform.Domain.Consent;

namespace Platform.UnitTests.Domain;

public sealed class ConsentEntitiesTests
{
    private const string RequiredGrants = """["data_processing","research_participation"]""";
    private const string OptionalGrants = """["recontact"]""";

    [Fact]
    public void Consent_document_accepts_required_fields()
    {
        var publishedAt = DateTimeOffset.Parse("2026-05-07T10:00:00+00:00");

        var document = new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " en ",
            " 1.0.0 ",
            " Participant disclosure ",
            " Consent body ",
            RequiredGrants,
            OptionalGrants,
            publishedAt);

        Assert.Equal("en", document.Locale);
        Assert.Equal("1.0.0", document.Version);
        Assert.Equal("Participant disclosure", document.Title);
        Assert.Equal("Consent body", document.BodyMarkdown);
        Assert.Equal(RequiredGrants, document.RequiredGrants);
        Assert.Equal(OptionalGrants, document.OptionalGrants);
        Assert.True(document.IsUsableAt(publishedAt.AddMinutes(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Consent_document_rejects_missing_required_text(string invalid)
    {
        Assert.Throws<ArgumentException>(() => new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalid,
            "1.0.0",
            "Participant disclosure",
            "Consent body",
            RequiredGrants,
            "[]",
            DateTimeOffset.UtcNow));

        Assert.Throws<ArgumentException>(() => new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            invalid,
            "Consent body",
            RequiredGrants,
            "[]",
            DateTimeOffset.UtcNow));

        Assert.Throws<ArgumentException>(() => new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            "Participant disclosure",
            invalid,
            RequiredGrants,
            "[]",
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""[""]""")]
    [InlineData("""["data_processing", 123]""")]
    public void Consent_document_rejects_invalid_grant_json(string invalidGrantJson)
    {
        Assert.Throws<ArgumentException>(() => new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            "Participant disclosure",
            "Consent body",
            invalidGrantJson,
            "[]",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Consent_record_accepts_required_fields()
    {
        var acceptedAt = DateTimeOffset.Parse("2026-05-07T10:10:00+00:00");
        var subjectId = Guid.NewGuid();

        var record = new ConsentRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " en ",
            RequiredGrants,
            acceptedAt,
            subjectId);

        Assert.Equal("en", record.Locale);
        Assert.Equal(RequiredGrants, record.AcceptedGrants);
        Assert.Equal(acceptedAt, record.AcceptedAt);
        Assert.Equal(subjectId, record.SubjectId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Consent_record_rejects_missing_locale(string invalidLocale)
    {
        Assert.Throws<ArgumentException>(() => new ConsentRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalidLocale,
            RequiredGrants,
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""[""]""")]
    [InlineData("""["data_processing", 123]""")]
    public void Consent_record_rejects_invalid_accepted_grants(string invalidGrantJson)
    {
        Assert.Throws<ArgumentException>(() => new ConsentRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            invalidGrantJson,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Retire_SetsRetirementAfterPublication()
    {
        var publishedAt = DateTimeOffset.UtcNow;
        var document = new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            "Participant disclosure",
            "Consent body",
            RequiredGrants,
            OptionalGrants,
            publishedAt);

        var retiredAt = publishedAt.AddMinutes(5);
        document.Retire(retiredAt);

        Assert.Equal(retiredAt, document.RetiredAt);
        Assert.False(document.IsUsableAt(retiredAt.AddMinutes(1)));
        Assert.True(document.IsUsableAt(publishedAt.AddMinutes(1)));
    }

    [Fact]
    public void Retire_RejectsSecondRetirement()
    {
        var publishedAt = DateTimeOffset.UtcNow;
        var document = new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            "Participant disclosure",
            "Consent body",
            RequiredGrants,
            OptionalGrants,
            publishedAt);

        document.Retire(publishedAt.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => document.Retire(publishedAt.AddMinutes(10)));
    }

    [Fact]
    public void Retire_RejectsRetirementBeforePublication()
    {
        var publishedAt = DateTimeOffset.UtcNow;
        var document = new ConsentDocument(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en",
            "1.0.0",
            "Participant disclosure",
            "Consent body",
            RequiredGrants,
            OptionalGrants,
            publishedAt);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.Retire(publishedAt.AddMinutes(-1)));
    }

}
