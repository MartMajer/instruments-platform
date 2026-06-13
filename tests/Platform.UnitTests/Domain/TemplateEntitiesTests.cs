using Platform.Domain.Templates;

namespace Platform.UnitTests.Domain;

public sealed class TemplateEntitiesTests
{
    [Fact]
    public void Global_canonical_template_version_is_locked_and_global()
    {
        var template = SurveyTemplate.CreateGlobal(Guid.NewGuid(), "OLBI");
        var version = TemplateVersion.CreateCanonicalDraft(
            Guid.NewGuid(),
            template.Id,
            "1.0.0",
            "en");

        Assert.Null(template.TenantId);
        Assert.True(version.IsGlobal);
        Assert.True(version.IsLocked);
        Assert.Equal(TemplateVersionStatuses.Draft, version.Status);
    }

    [Fact]
    public void Tenant_template_version_is_tenant_owned_and_editable_draft()
    {
        var tenantId = Guid.NewGuid();
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), tenantId, "Custom burnout pulse");
        var version = TemplateVersion.CreateTenantDraft(
            Guid.NewGuid(),
            template.Id,
            "0.1.0",
            "en");

        Assert.Equal(tenantId, template.TenantId);
        Assert.False(version.IsGlobal);
        Assert.False(version.IsLocked);
        Assert.Equal(TemplateVersionStatuses.Draft, version.Status);
    }

    [Fact]
    public void Publish_records_template_version_publisher()
    {
        var publisherId = Guid.NewGuid();
        var publishedAt = DateTimeOffset.Parse("2026-06-12T12:00:00+00:00");
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), Guid.NewGuid(), "Custom burnout pulse");
        var version = TemplateVersion.CreateTenantDraft(
            Guid.NewGuid(),
            template.Id,
            "0.1.0",
            "en");

        version.Publish(publisherId, publishedAt);

        Assert.Equal(TemplateVersionStatuses.Published, version.Status);
        Assert.True(version.IsLocked);
        Assert.Equal(publishedAt, version.PublishedAt);
        Assert.Equal(publisherId, version.PublishedBy);
    }

    [Fact]
    public void Published_template_version_cannot_be_published_again()
    {
        var template = SurveyTemplate.CreateTenant(Guid.NewGuid(), Guid.NewGuid(), "Custom burnout pulse");
        var version = TemplateVersion.CreateTenantDraft(
            Guid.NewGuid(),
            template.Id,
            "0.1.0",
            "en");

        version.Publish(null, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => version.Publish(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Section_rejects_non_positive_ordinal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemplateSection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ordinal: 0,
            code: "intro",
            titleDefault: "Intro"));
    }

    [Fact]
    public void Scale_rejects_invalid_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QuestionScale(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "likert_1_4",
            ScaleTypes.Likert,
            minValue: 4,
            maxValue: 1,
            step: 1,
            naAllowed: false,
            anchors: """[{"value":1,"label_default":"Strongly agree"}]"""));
    }

    [Fact]
    public void Likert_question_requires_scale()
    {
        Assert.Throws<ArgumentException>(() => new TemplateQuestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ordinal: 1,
            code: "olbi_01",
            type: QuestionTypes.Likert,
            scaleId: null,
            textDefault: "I always find new and interesting aspects in my work."));
    }

    [Fact]
    public void Choice_option_rejects_empty_value()
    {
        Assert.Throws<ArgumentException>(() => new ChoiceOption(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ordinal: 1,
            value: " ",
            labelDefault: "Other"));
    }
}
