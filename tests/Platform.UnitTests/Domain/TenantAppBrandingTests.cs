using Platform.Domain.Tenancy;

namespace Platform.UnitTests.Domain;

public sealed class TenantAppBrandingTests
{
    private static readonly DateTimeOffset FixedNow =
        DateTimeOffset.Parse("2026-07-08T10:00:00+00:00");

    private static Tenant NewTenant()
    {
        return new Tenant(Guid.NewGuid(), "algebra-research", "Algebra Research");
    }

    [Fact]
    public void Update_app_branding_stores_normalized_accent_and_logo()
    {
        var tenant = NewTenant();
        var actor = Guid.NewGuid();

        tenant.UpdateAppBranding("#2B5FD9", "tenant-branding/x/logo.png", "IMAGE/PNG", actor, FixedNow);

        Assert.Equal("#2b5fd9", tenant.AppBrandingAccentColorHex);
        Assert.Equal("tenant-branding/x/logo.png", tenant.AppBrandingLogoObjectKey);
        Assert.Equal("image/png", tenant.AppBrandingLogoContentType);
        Assert.Equal(actor, tenant.AppBrandingUpdatedBy);
        Assert.Equal(FixedNow, tenant.AppBrandingUpdatedAt);
        Assert.Equal(FixedNow, tenant.UpdatedAt);
    }

    [Fact]
    public void Update_app_branding_with_blank_logo_key_clears_the_logo()
    {
        var tenant = NewTenant();
        tenant.UpdateAppBranding("#2b5fd9", "tenant-branding/x/logo.png", "image/png", Guid.NewGuid(), FixedNow);

        tenant.UpdateAppBranding("#2b5fd9", null, null, Guid.NewGuid(), FixedNow);

        Assert.Null(tenant.AppBrandingLogoObjectKey);
        Assert.Null(tenant.AppBrandingLogoContentType);
        Assert.Equal("#2b5fd9", tenant.AppBrandingAccentColorHex);
    }

    [Theory]
    [InlineData("2b5fd9")]     // missing hash
    [InlineData("#2b5fd")]     // too short
    [InlineData("#gggggg")]    // non-hex
    [InlineData("")]
    public void Update_app_branding_rejects_bad_accent(string accent)
    {
        var tenant = NewTenant();

        Assert.Throws<ArgumentException>(() =>
            tenant.UpdateAppBranding(accent, null, null, Guid.NewGuid(), FixedNow));
    }

    [Theory]
    [InlineData("image/svg+xml")] // SVG excluded in v1
    [InlineData("image/gif")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    public void Update_app_branding_rejects_disallowed_logo_content_type(string contentType)
    {
        var tenant = NewTenant();

        Assert.Throws<ArgumentException>(() =>
            tenant.UpdateAppBranding("#2b5fd9", "tenant-branding/x/logo", contentType, Guid.NewGuid(), FixedNow));
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("image/webp", true)]
    [InlineData("image/svg+xml", false)]
    [InlineData(null, false)]
    public void Logo_content_type_allow_list_is_enforced(string? contentType, bool allowed)
    {
        Assert.Equal(allowed, Tenant.IsAppBrandingLogoContentType(contentType));
    }
}
