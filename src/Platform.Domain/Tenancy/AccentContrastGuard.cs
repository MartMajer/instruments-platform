namespace Platform.Domain.Tenancy;

/// <summary>
/// The contrast guard for tenant accent colors. A tenant's chosen accent lands
/// on the respondent surface as a button background carrying white label text,
/// and as accent-colored marks on white surfaces. Both roles reduce to the same
/// requirement: the accent must have enough WCAG contrast against white. If the
/// tenant picks a color too light to be legible there, this derives a legible,
/// hue-preserving darker variant rather than rejecting the choice (owner
/// decision, 2026-07-08: auto-adjust, don't reject). Pure and deterministic so
/// it can be unit-tested and pre-computed server-side.
/// </summary>
public static class AccentContrastGuard
{
    /// <summary>WCAG 2.1 AA contrast ratio for normal-size text.</summary>
    public const double MinimumContrastRatio = 4.5;

    private const double DarkenStep = 0.96;
    private const int MaxDarkenIterations = 128;

    private static readonly Rgb White = new(255, 255, 255);

    /// <summary>
    /// Returns an accent hex (lowercase <c>#rrggbb</c>) guaranteed to meet the
    /// AA contrast ratio against white, darkening the input toward black in
    /// hue-preserving steps only if it falls short. A color that already passes
    /// is returned unchanged (normalized). An unparseable input is returned as
    /// given — callers pass values already validated by <see cref="Tenant"/>.
    /// </summary>
    public static string EnsureLegibleOnWhite(string accentHex)
    {
        if (!TryParseHex(accentHex, out var rgb))
        {
            return accentHex;
        }

        if (Contrast(rgb, White) >= MinimumContrastRatio)
        {
            return ToHex(rgb);
        }

        var current = rgb;
        for (var iteration = 0; iteration < MaxDarkenIterations; iteration++)
        {
            current = new Rgb(
                (int)Math.Round(current.R * DarkenStep),
                (int)Math.Round(current.G * DarkenStep),
                (int)Math.Round(current.B * DarkenStep));

            if (Contrast(current, White) >= MinimumContrastRatio)
            {
                return ToHex(current);
            }

            if (current is { R: 0, G: 0, B: 0 })
            {
                break;
            }
        }

        // Black is the highest-contrast fallback against white and always passes.
        return ToHex(new Rgb(0, 0, 0));
    }

    /// <summary>The WCAG contrast ratio of the accent against white (1.0–21.0).</summary>
    public static double ContrastAgainstWhite(string accentHex)
    {
        return TryParseHex(accentHex, out var rgb) ? Contrast(rgb, White) : 1.0;
    }

    /// <summary>Whether the accent already meets AA contrast against white.</summary>
    public static bool IsLegibleOnWhite(string accentHex)
    {
        return ContrastAgainstWhite(accentHex) >= MinimumContrastRatio;
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
