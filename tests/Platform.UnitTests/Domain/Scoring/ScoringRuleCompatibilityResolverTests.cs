using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain.Scoring;

public sealed class ScoringRuleCompatibilityResolverTests
{
    [Fact]
    public void Same_rule_key_version_and_hash_returns_compatible()
    {
        var baseline = Rule("tenant-burnout.total", "1.0.0", Hash('a'));
        var comparison = Rule("tenant-burnout.total", "1.0.0", Hash('a'));

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("compatible", result.Status);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Output_equivalent_all_outputs_returns_compatible()
    {
        var baseline = Rule("tenant-burnout.total", "1.1.0", Hash('a'));
        var comparison = Rule(
            "tenant-burnout.total",
            "2.0.0",
            Hash('b'),
            """
            {
              "output_equivalent_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": "all_outputs",
                  "evidence": "Backfilled parity replay passed."
                }
              ]
            }
            """);

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("compatible", result.Status);
        Assert.Contains("Backfilled parity", result.Reason);
    }

    [Fact]
    public void Descriptive_only_scope_match_returns_descriptive_only()
    {
        var baseline = Rule("tenant-burnout.total", "1.0.0", Hash('a'));
        var comparison = Rule(
            "tenant-burnout.total",
            "2.0.0",
            Hash('b'),
            """
            {
              "descriptive_only_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": ["total"],
                  "rationale": "Formula changed; descriptive only."
                }
              ]
            }
            """);

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("descriptive_only", result.Status);
        Assert.Contains("Formula changed", result.Reason);
    }

    [Fact]
    public void Incompatible_entry_returns_incompatible()
    {
        var baseline = Rule("tenant-burnout.total", "1.0.0", Hash('a'));
        var comparison = Rule(
            "tenant-burnout.total",
            "2.0.0",
            Hash('b'),
            """
            {
              "incompatible_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": "1.0.0",
                  "scope": ["total"],
                  "rationale": "Scale direction changed."
                }
              ]
            }
            """);

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("incompatible", result.Status);
        Assert.Contains("Scale direction", result.Reason);
    }

    [Fact]
    public void No_matching_declaration_returns_compatibility_missing()
    {
        var baseline = Rule("tenant-burnout.total", "1.0.0", Hash('a'));
        var comparison = Rule(
            "tenant-burnout.total",
            "2.0.0",
            Hash('b'),
            """
            {
              "output_equivalent_with": [
                {
                  "rule_id": "other-rule",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": "all_outputs",
                  "evidence": "Other rule only."
                }
              ]
            }
            """);

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("compatibility_missing", result.Status);
        Assert.Equal("No mixed-version compatibility declaration matched this score.", result.Reason);
    }

    [Fact]
    public void Unsupported_range_syntax_returns_compatibility_missing()
    {
        var baseline = Rule("tenant-burnout.total", "1.0.0", Hash('a'));
        var comparison = Rule(
            "tenant-burnout.total",
            "2.0.0",
            Hash('b'),
            """
            {
              "output_equivalent_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": "^1.0.0",
                  "scope": "all_outputs",
                  "evidence": "Unsupported npm-style range."
                }
              ]
            }
            """);

        var result = ScoringRuleCompatibilityResolver.Resolve(baseline, comparison, "total");

        Assert.Equal("compatibility_missing", result.Status);
    }

    private static ScoringRuleCompatibilityReference Rule(
        string key,
        string version,
        string hash,
        string compatibility = "{}")
    {
        return new ScoringRuleCompatibilityReference(key, version, hash, compatibility);
    }

    private static string Hash(char value)
    {
        return new string(value, 64);
    }
}
