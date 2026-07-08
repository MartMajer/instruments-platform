namespace Platform.Domain.Tenancy;

/// <summary>
/// The contrast engine for tenant branding. Tenant-chosen colors must stay
/// legible wherever they land: accent as a button background and as marks on a
/// surface, primary text on a card, foreground on the topbar. Rather than
/// rejecting a poor pick, this derives a legible variant (owner decision,
/// 2026-07-08: auto-adjust). Pure and deterministic so it can be unit-tested and
/// pre-computed server-side, then mirrored client-side for the live preview.
/// </summary>
public static class AccentContrastGuard
{
    /// <summary>WCAG 2.1 AA contrast ratio for normal-size text.</summary>
    public const double MinimumContrastRatio = 4.5;

    private const double NudgeStep = 0.04;
    private const int MaxIterations = 160;

    private static readonly Rgb White = new(255, 255, 255);
    private static readonly Rgb Black = new(0, 0, 0);

    // The app's near-black ink (#141c25), used as the dark option for readable text.
    private static readonly Rgb DarkInk = new(20, 28, 37);

    /// <summary>
    /// Returns <paramref name="foregroundHex"/> nudged toward black or white
    /// (whichever raises contrast against <paramref name="backgroundHex"/>) until
    /// it meets AA, or the input unchanged if it already passes. Unparseable
    /// input is returned as given — callers pass validated hex.
    /// </summary>
    public static string EnsureLegible(
        string foregroundHex,
        string backgroundHex,
        double minimumRatio = MinimumContrastRatio)
    {
        if (!TryParseHex(foregroundHex, out var fg) || !TryParseHex(backgroundHex, out var bg))
        {
            return foregroundHex;
        }

        if (Contrast(fg, bg) >= minimumRatio)
        {
            return ToHex(fg);
        }

        var target = Contrast(Black, bg) >= Contrast(White, bg) ? Black : White;
        var current = fg;
        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            current = Nudge(current, target, NudgeStep);
            if (Contrast(current, bg) >= minimumRatio)
            {
                return ToHex(current);
            }

            if (current.Equals(target))
            {
                break;
            }
        }

        return ToHex(target);
    }

    /// <summary>The higher-contrast of white or the app's dark ink against the given surface.</summary>
    public static string ReadableTextOn(string backgroundHex)
    {
        if (!TryParseHex(backgroundHex, out var bg))
        {
            return "#ffffff";
        }

        return Contrast(White, bg) >= Contrast(DarkInk, bg) ? "#ffffff" : ToHex(DarkInk);
    }

    /// <summary>The WCAG contrast ratio between two colors (1.0–21.0).</summary>
    public static double ContrastRatio(string aHex, string bHex)
    {
        return TryParseHex(aHex, out var a) && TryParseHex(bHex, out var b) ? Contrast(a, b) : 1.0;
    }

    /// <summary>Convenience for accent-on-white (button text is white; marks sit on white).</summary>
    public static string EnsureLegibleOnWhite(string accentHex)
    {
        return EnsureLegible(accentHex, "#ffffff");
    }

    /// <summary>The contrast ratio of the accent against white (1.0–21.0).</summary>
    public static double ContrastAgainstWhite(string accentHex)
    {
        return ContrastRatio(accentHex, "#ffffff");
    }

    /// <summary>Whether the accent already meets AA contrast against white.</summary>
    public static bool IsLegibleOnWhite(string accentHex)
    {
        return ContrastAgainstWhite(accentHex) >= MinimumContrastRatio;
    }

    private static Rgb Nudge(Rgb from, Rgb target, double fraction)
    {
        return new Rgb(
            (int)Math.Round(from.R + ((target.R - from.R) * fraction)),
            (int)Math.Round(from.G + ((target.G - from.G) * fraction)),
            (int)Math.Round(from.B + ((target.B - from.B) * fraction)));
    }

    private static double Contrast(Rgb a, Rgb b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        var lighter = Math.Max(la, lb);
        var darker = Math.Min(la, lb);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Rgb rgb)
    {
        var r = Linearize(rgb.R / 255.0);
        var g = Linearize(rgb.G / 255.0);
        var b = Linearize(rgb.B / 255.0);

        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static double Linearize(double channel)
    {
        return channel <= 0.03928
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static bool TryParseHex(string? value, out Rgb rgb)
    {
        rgb = default;
        var candidate = value?.Trim();
        if (candidate is null || candidate.Length != 7 || candidate[0] != '#')
        {
            return false;
        }

        if (!TryParseByte(candidate, 1, out var r) ||
            !TryParseByte(candidate, 3, out var g) ||
            !TryParseByte(candidate, 5, out var b))
        {
            return false;
        }

        rgb = new Rgb(r, g, b);
        return true;
    }

    private static bool TryParseByte(string value, int offset, out int result)
    {
        var high = HexValue(value[offset]);
        var low = HexValue(value[offset + 1]);
        if (high < 0 || low < 0)
        {
            result = 0;
            return false;
        }

        result = (high * 16) + low;
        return true;
    }

    private static int HexValue(char character)
    {
        return character switch
        {
            >= '0' and <= '9' => character - '0',
            >= 'a' and <= 'f' => character - 'a' + 10,
            >= 'A' and <= 'F' => character - 'A' + 10,
            _ => -1
        };
    }

    private static string ToHex(Rgb rgb)
    {
        return $"#{Clamp(rgb.R):x2}{Clamp(rgb.G):x2}{Clamp(rgb.B):x2}";
    }

    private static int Clamp(int channel)
    {
        return Math.Clamp(channel, 0, 255);
    }

    private readonly record struct Rgb(int R, int G, int B);
}
