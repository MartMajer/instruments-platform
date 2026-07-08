using Platform.Domain.Tenancy;

namespace Platform.UnitTests.Domain;

public sealed class AccentContrastGuardTests
{
    [Theory]
    [InlineData("#000000")] // black — max contrast
    [InlineData("#4530a6")] // stain violet (the platform default) — legible
    [InlineData("#2563eb")] // report default blue — legible
    [InlineData("#b3323e")] // danger red — legible
    public void Legible_accents_are_returned_unchanged(string accent)
    {
        Assert.True(AccentContrastGuard.IsLegibleOnWhite(accent));
        Assert.Equal(accent, AccentContrastGuard.EnsureLegibleOnWhite(accent));
    }

    [Theory]
    [InlineData("#ffff00")] // pure yellow — far too light for white text
    [InlineData("#ffffff")] // white on white — the worst case
    [InlineData("#7fff00")] // chartreuse
    [InlineData("#00ffff")] // cyan
    public void Illegible_accents_are_darkened_until_they_pass(string accent)
    {
        Assert.False(AccentContrastGuard.IsLegibleOnWhite(accent));

        var corrected = AccentContrastGuard.EnsureLegibleOnWhite(accent);

        Assert.NotEqual(accent.ToLowerInvariant(), corrected);
        Assert.True(
            AccentContrastGuard.IsLegibleOnWhite(corrected),
            $"corrected accent {corrected} for {accent} still fails contrast");
        Assert.Matches("^#[0-9a-f]{6}$", corrected);
    }

    [Fact]
    public void Correction_preserves_the_dominant_hue_when_darkening()
    {
        // A light yellow darkens toward olive: red and green stay high relative
        // to blue rather than collapsing to a grey.
        var corrected = AccentContrastGuard.EnsureLegibleOnWhite("#ffee00");

        var r = Convert.ToInt32(corrected.Substring(1, 2), 16);
        var g = Convert.ToInt32(corrected.Substring(3, 2), 16);
        var b = Convert.ToInt32(corrected.Substring(5, 2), 16);

        Assert.True(r > b, $"expected red to dominate blue in {corrected}");
        Assert.True(g > b, $"expected green to dominate blue in {corrected}");
    }

    [Fact]
    public void Contrast_against_white_matches_known_wcag_values()
    {
        // Black on white is the canonical 21:1.
        Assert.Equal(21.0, AccentContrastGuard.ContrastAgainstWhite("#000000"), 1);
        // White on white is 1:1.
        Assert.Equal(1.0, AccentContrastGuard.ContrastAgainstWhite("#ffffff"), 3);
    }

    [Fact]
    public void Unparseable_input_is_returned_untouched()
    {
        Assert.Equal("not-a-color", AccentContrastGuard.EnsureLegibleOnWhite("not-a-color"));
    }

    [Theory]
    [InlineData("#333333", "#111111")] // dark on dark — must lighten
    [InlineData("#eeeeee", "#ffffff")] // light on white — must darken
    [InlineData("#1f4fd1", "#151c25")] // mid accent on the dark topbar
    public void Ensure_legible_raises_contrast_against_any_background(string fg, string bg)
    {
        var corrected = AccentContrastGuard.EnsureLegible(fg, bg);

        Assert.True(
            AccentContrastGuard.ContrastRatio(corrected, bg) >= AccentContrastGuard.MinimumContrastRatio,
            $"{corrected} on {bg} still fails contrast");
    }

    [Fact]
    public void Ensure_legible_leaves_a_passing_pair_untouched()
    {
        Assert.Equal("#000000", AccentContrastGuard.EnsureLegible("#000000", "#ffffff"));
        Assert.Equal("#ffffff", AccentContrastGuard.EnsureLegible("#ffffff", "#000000"));
    }

    [Theory]
    [InlineData("#111111", "#ffffff")] // dark surface wants white text
    [InlineData("#0b1220", "#ffffff")]
    [InlineData("#ffffff", "#141c25")] // light surface wants dark ink
    [InlineData("#f2f4f8", "#141c25")]
    public void Readable_text_on_picks_the_higher_contrast_option(string background, string expected)
    {
        Assert.Equal(expected, AccentContrastGuard.ReadableTextOn(background));
    }
}
