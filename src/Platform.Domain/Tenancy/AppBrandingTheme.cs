namespace Platform.Domain.Tenancy;

/// <summary>The anchor colors a tenant may set. Any null falls back to the platform default.</summary>
public sealed record AppBrandingThemeTokens(
    string? Accent = null,
    string? Topbar = null,
    string? Background = null,
    string? Surface = null,
    string? Ink = null);

/// <summary>
/// A fully-resolved, legibility-guarded palette. Every field is a concrete
/// <c>#rrggbb</c> the frontend maps straight onto platform CSS custom properties
/// (no tenant CSS — only scalar values fill our own widget). Soft variants
/// (ink-2/3, washes) are derived from these in CSS via color-mix.
/// </summary>
public sealed record ResolvedAppBrandingTheme(
    string Accent,
    string OnAccent,
    string AccentOnTopbar,
    string Topbar,
    string TopbarInk,
    string Background,
    string Surface,
    string Ink);

/// <summary>
/// Resolves a tenant's anchor colors into a coherent, legible palette. Surfaces
/// are honored as chosen; foregrounds (accent, ink, topbar text, button text)
/// are contrast-guarded against the surface they land on. Pure and deterministic
/// — the backend resolves it once; the client mirrors it for the live preview.
/// </summary>
public static class AppBrandingTheme
{
    // Defaults mirror apps/validatedscale/src/app.css.
    public const string DefaultAccent = "#4530a6";
    public const string DefaultTopbar = "#151c25";
    public const string DefaultBackground = "#f2f4f8";
    public const string DefaultSurface = "#ffffff";
    public const string DefaultInk = "#151c25";

    public static bool HasAnyToken(AppBrandingThemeTokens tokens)
    {
        return !string.IsNullOrWhiteSpace(tokens.Accent) ||
            !string.IsNullOrWhiteSpace(tokens.Topbar) ||
            !string.IsNullOrWhiteSpace(tokens.Background) ||
            !string.IsNullOrWhiteSpace(tokens.Surface) ||
            !string.IsNullOrWhiteSpace(tokens.Ink);
    }

    public static ResolvedAppBrandingTheme Resolve(AppBrandingThemeTokens tokens)
    {
        var surface = Coalesce(tokens.Surface, DefaultSurface);
        var background = Coalesce(tokens.Background, DefaultBackground);
        var topbar = Coalesce(tokens.Topbar, DefaultTopbar);
        var accentRaw = Coalesce(tokens.Accent, DefaultAccent);
        var inkRaw = Coalesce(tokens.Ink, DefaultInk);

        var accent = AccentContrastGuard.EnsureLegible(accentRaw, surface);

        return new ResolvedAppBrandingTheme(
            Accent: accent,
            OnAccent: AccentContrastGuard.ReadableTextOn(accent),
            AccentOnTopbar: AccentContrastGuard.EnsureLegible(accentRaw, topbar),
            Topbar: topbar,
            TopbarInk: AccentContrastGuard.ReadableTextOn(topbar),
            Background: background,
            Surface: surface,
            Ink: AccentContrastGuard.EnsureLegible(inkRaw, surface));
    }

    private static string Coalesce(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
    }
}
