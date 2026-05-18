using System.Text.Json;

namespace Platform.Domain.Scoring;

public sealed record ScoringRuleCompatibilityReference(
    string RuleKey,
    string RuleVersion,
    string DocumentHash,
    string Compatibility);

public sealed record ScoringRuleCompatibilityResolution(
    string Status,
    string? Reason);

public static class ScoringRuleCompatibilityResolver
{
    public const string Compatible = "compatible";
    public const string DescriptiveOnly = "descriptive_only";
    public const string Incompatible = "incompatible";
    public const string Missing = "compatibility_missing";

    private const string MissingReason = "No mixed-version compatibility declaration matched this score.";

    public static ScoringRuleCompatibilityResolution Resolve(
        ScoringRuleCompatibilityReference baseline,
        ScoringRuleCompatibilityReference comparison,
        string dimensionCode)
    {
        if (SameRule(baseline, comparison))
        {
            return new ScoringRuleCompatibilityResolution(Compatible, Reason: null);
        }

        using var document = ParseCompatibility(comparison.Compatibility);
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return MissingResolution();
        }

        var dimension = Normalize(dimensionCode);
        foreach (var declaration in Declarations())
        {
            var match = FindMatch(
                document.RootElement,
                declaration.PropertyName,
                baseline,
                dimension);
            if (match is not null)
            {
                return new ScoringRuleCompatibilityResolution(declaration.Status, match);
            }
        }

        return MissingResolution();
    }

    private static bool SameRule(
        ScoringRuleCompatibilityReference baseline,
        ScoringRuleCompatibilityReference comparison)
    {
        return string.Equals(Normalize(baseline.RuleKey), Normalize(comparison.RuleKey), StringComparison.Ordinal) &&
            string.Equals(NormalizeVersion(baseline.RuleVersion), NormalizeVersion(comparison.RuleVersion), StringComparison.Ordinal) &&
            string.Equals(Normalize(baseline.DocumentHash), Normalize(comparison.DocumentHash), StringComparison.Ordinal);
    }

    private static JsonDocument? ParseCompatibility(string compatibility)
    {
        if (string.IsNullOrWhiteSpace(compatibility))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(compatibility);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindMatch(
        JsonElement root,
        string propertyName,
        ScoringRuleCompatibilityReference baseline,
        string dimension)
    {
        if (!root.TryGetProperty(propertyName, out var declarations) ||
            declarations.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var declaration in declarations.EnumerateArray())
        {
            if (Matches(declaration, baseline, dimension))
            {
                return ReadReason(declaration);
            }
        }

        return null;
    }

    private static bool Matches(
        JsonElement declaration,
        ScoringRuleCompatibilityReference baseline,
        string dimension)
    {
        if (declaration.ValueKind != JsonValueKind.Object ||
            !TryReadString(declaration, "rule_id", out var ruleId) ||
            !TryReadString(declaration, "rule_version_range", out var ruleVersionRange) ||
            !declaration.TryGetProperty("scope", out var scope))
        {
            return false;
        }

        return string.Equals(Normalize(ruleId), Normalize(baseline.RuleKey), StringComparison.Ordinal) &&
            VersionRangeMatches(NormalizeVersion(baseline.RuleVersion), ruleVersionRange) &&
            ScopeMatches(scope, dimension);
    }

    private static bool ScopeMatches(JsonElement scope, string dimension)
    {
        if (scope.ValueKind == JsonValueKind.String)
        {
            return string.Equals(Normalize(scope.GetString()), "all_outputs", StringComparison.Ordinal);
        }

        if (scope.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in scope.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String &&
                string.Equals(Normalize(entry.GetString()), dimension, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VersionRangeMatches(string version, string range)
    {
        if (!SemanticVersion.TryParse(version, out var actual))
        {
            return false;
        }

        var clauses = range.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (clauses.Length == 0)
        {
            return false;
        }

        foreach (var clause in clauses)
        {
            if (!VersionClauseMatches(actual, clause))
            {
                return false;
            }
        }

        return true;
    }

    private static bool VersionClauseMatches(SemanticVersion actual, string clause)
    {
        var operation = "==";
        var expectedText = clause;

        if (clause.StartsWith(">=", StringComparison.Ordinal) ||
            clause.StartsWith("<=", StringComparison.Ordinal))
        {
            operation = clause[..2];
            expectedText = clause[2..];
        }
        else if (clause.StartsWith('>') || clause.StartsWith('<'))
        {
            operation = clause[..1];
            expectedText = clause[1..];
        }
        else if (clause.StartsWith('='))
        {
            expectedText = clause[1..];
        }

        if (!SemanticVersion.TryParse(expectedText, out var expected))
        {
            return false;
        }

        var comparison = actual.CompareTo(expected);
        return operation switch
        {
            "==" => comparison == 0,
            ">=" => comparison >= 0,
            ">" => comparison > 0,
            "<=" => comparison <= 0,
            "<" => comparison < 0,
            _ => false
        };
    }

    private static string? ReadReason(JsonElement declaration)
    {
        if (TryReadString(declaration, "evidence", out var evidence))
        {
            return evidence.Trim();
        }

        return TryReadString(declaration, "rationale", out var rationale)
            ? rationale.Trim()
            : null;
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static ScoringRuleCompatibilityResolution MissingResolution()
    {
        return new ScoringRuleCompatibilityResolution(Missing, MissingReason);
    }

    private static IReadOnlyList<(string PropertyName, string Status)> Declarations()
    {
        return
        [
            ("output_equivalent_with", Compatible),
            ("descriptive_only_with", DescriptiveOnly),
            ("incompatible_with", Incompatible)
        ];
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeVersion(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private readonly record struct SemanticVersion(int Major, int Minor, int Patch)
        : IComparable<SemanticVersion>
    {
        public int CompareTo(SemanticVersion other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
            {
                return major;
            }

            var minor = Minor.CompareTo(other.Minor);
            return minor != 0 ? minor : Patch.CompareTo(other.Patch);
        }

        public static bool TryParse(string? value, out SemanticVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Trim().Split('.');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out var major) ||
                !int.TryParse(parts[1], out var minor) ||
                !int.TryParse(parts[2], out var patch))
            {
                return false;
            }

            version = new SemanticVersion(major, minor, patch);
            return true;
        }
    }
}
