using Platform.Application.Features.Notifications;
using Platform.Domain.Campaigns;

namespace Platform.UnitTests.Notifications;

public sealed class EmailTemplateRenderingTests
{
    [Fact]
    public void Built_in_invitation_default_renders_required_links()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en");

        var result = EmailTemplateRenderer.Render(template, CreateContext("en"));

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("Study invitation", result.Value.Subject);
        Assert.Contains("https://example.test/r/inv_123", result.Value.BodyText, StringComparison.Ordinal);
        Assert.Contains("https://example.test/unsubscribe/wdr_123", result.Value.BodyText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", result.Value.BodyText, StringComparison.Ordinal);
    }

    [Fact]
    public void Croatian_built_in_invitation_renders_croatian_copy()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "hr-HR");

        var result = EmailTemplateRenderer.Render(template, CreateContext("hr-HR"));

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("Poziv na istraživanje", result.Value.Subject);
        Assert.Contains("Pozvani ste", result.Value.BodyText, StringComparison.Ordinal);
        Assert.Contains("https://example.test/r/inv_123", result.Value.BodyText, StringComparison.Ordinal);
        Assert.Contains("https://example.test/unsubscribe/wdr_123", result.Value.BodyText, StringComparison.Ordinal);
    }

    [Fact]
    public void Invitation_without_respondent_link_variable_is_invalid()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en");
        var edited = template with
        {
            BodyText = template.BodyText.Replace("{{respondent_link}}", "https://example.test/r/manual", StringComparison.Ordinal)
        };

        var result = EmailTemplateValidator.Validate(edited);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "email_template.respondent_link_required");
    }

    [Fact]
    public void Invitation_without_unsubscribe_link_variable_is_invalid()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en");
        var edited = template with
        {
            BodyText = template.BodyText.Replace("{{unsubscribe_link}}", "https://example.test/unsubscribe/manual", StringComparison.Ordinal)
        };

        var result = EmailTemplateValidator.Validate(edited);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "email_template.unsubscribe_link_required");
    }

    [Fact]
    public void Unknown_template_variable_is_invalid()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en");
        var edited = template with
        {
            BodyText = template.BodyText + "\n\nInternal owner: {{owner_email}}"
        };

        var result = EmailTemplateValidator.Validate(edited);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "email_template.variable_unknown");
    }

    [Fact]
    public void Unsupported_template_locale_is_invalid()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en") with
        {
            Locale = "de-DE"
        };

        var result = EmailTemplateValidator.Validate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "email_template.locale_invalid");
    }

    [Fact]
    public void Rendered_output_has_no_unresolved_variables()
    {
        var template = EmailTemplateDefaults.Get(EmailTemplateCodes.Invitation, "en") with
        {
            Subject = "Invitation from {{workspace_name}}"
        };

        var result = EmailTemplateRenderer.Render(template, CreateContext("en"));

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("Invitation from ValidatedScale", result.Value.Subject);
        Assert.DoesNotContain("{{", result.Value.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", result.Value.BodyText, StringComparison.Ordinal);
    }

    private static EmailTemplateRenderContext CreateContext(string locale)
    {
        return new EmailTemplateRenderContext(
            EmailTemplateCodes.Invitation,
            locale,
            "ValidatedScale",
            "https://example.test/r/inv_123",
            "https://example.test/unsubscribe/wdr_123");
    }
}
