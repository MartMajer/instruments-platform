using Platform.Domain.Tenancy;

namespace Platform.UnitTests.Domain;

public sealed class AppBrandingThemeTests
{
    private const double MinRatio = AccentContrastGuard.MinimumContrastRatio;

    [Fact]
    public void Unset_tokens_resolve_to_the_platform_defaults()
    {
        var theme = AppBrandingTheme.Resolve(new AppBrandingThemeTokens());

        Assert.Equal(AppBrandingTheme.DefaultAccent, theme.Accent);
        Assert.Equal(AppBrandingTheme.DefaultTopbar, theme.Topbar);
        Assert.Equal(AppBrandingTheme.DefaultBackground, theme.Background);
        Assert.Equal(AppBrandingTheme.DefaultSurface, theme.Surface);
        Assert.Equal(AppBrandingTheme.DefaultInk, theme.Ink);
        Assert.Equal("#ffffff", theme.TopbarInk); // white reads on the dark default topbar
    }

    [Fact]
    public void Has_any_token_detects_a_single_set_color()
    {
        Assert.False(AppBrandingTheme.HasAnyToken(new AppBrandingThemeTokens()));
        Assert.True(AppBrandingTheme.HasAnyToken(new AppBrandingThemeTokens(Background: "#101828")));
    }

    [Fact]
    public void A_dark_surface_theme_stays_legible_everywhere()
    {
        // A tenant goes dark: near-black surface, off-white ink, a light background,
        // a teal accent, a deep-blue topbar.
        var theme = AppBrandingTheme.Resolve(new AppBrandingThemeTokens(
            Accent: "#12b3a6",
            Topbar: "#0b1e3f",
            Background: "#0d1117",
            Surface: "#111827",
            Ink: "#e6edf3"));

        Assert.Equal("#111827", theme.Surface);
        Assert.Equal("#0d1117", theme.Background);
        Assert.Equal("#0b1e3f", theme.Topbar);

        // Ink reads on the surface; accent reads on the surface; topbar text and
        // the topbar accent read on the topbar; button text reads on the accent.
        Assert.True(AccentContrastGuard.ContrastRatio(theme.Ink, theme.Surface) >= MinRatio);
        Assert.True(AccentContrastGuard.ContrastRatio(theme.Accent, theme.Surface) >= MinRatio);
        Assert.True(AccentContrastGuard.ContrastRatio(theme.TopbarInk, theme.Topbar) >= MinRatio);
        Assert.True(AccentContrastGuard.ContrastRatio(theme.AccentOnTopbar, theme.Topbar) >= MinRatio);
        Assert.True(AccentContrastGuard.ContrastRatio(theme.OnAccent, theme.Accent) >= MinRatio);
    }

    [Fact]
    public void A_light_accent_on_white_surface_is_guarded()
    {
        var theme = AppBrandingTheme.Resolve(new AppBrandingThemeTokens(Accent: "#ffe100"));

        Assert.NotEqual("#ffe100", theme.Accent); // auto-corrected
        Assert.True(AccentContrastGuard.ContrastRatio(theme.Accent, theme.Surface) >= MinRatio);
        Assert.True(AccentContrastGuard.ContrastRatio(theme.OnAccent, theme.Accent) >= MinRatio);
    }
}
