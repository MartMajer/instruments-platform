using System.Text.Json;
using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class ScoringFixtureReplayTests
{
    private static readonly string[] CurrentGraphOperations =
    [
        "select_answers",
        "map_choice_scores",
        "reverse_code",
        "mean",
        "sum",
        "count_valid",
        "subscale_aggregate"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static IEnumerable<object[]> FixtureFiles()
    {
        var root = FindRepoRoot();
        foreach (var path in Directory.EnumerateFiles(
            Path.Combine(root, "fixtures", "instruments"),
            "*.json",
            SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}noncanonical{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                yield return [path];
            }
        }
    }

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public async Task Noncanonical_scoring_fixture_replays(string path)
    {
        var fixture = JsonSerializer.Deserialize<ScoringFixture>(
            await File.ReadAllTextAsync(path),
            JsonOptions);

        Assert.NotNull(fixture);
        ValidateFixture(fixture);
        AssertScoringRuleMetadataValid(fixture);

        var inputs = fixture.Answers.Select(pair =>
            new SimpleScoreInput(pair.Key, pair.Value.GetRawText())).ToArray();

        var result = SimpleScoringEngine.Evaluate(fixture.ScoringDocument.GetRawText(), inputs);
        if (!string.IsNullOrWhiteSpace(fixture.ExpectedErrorCode))
        {
            Assert.True(result.IsFailure, "Expected scoring to fail for this fixture.");
            Assert.Equal(fixture.ExpectedErrorCode, result.Error.Code);
            return;
        }

        Assert.True(result.IsSuccess, result.Error.ToString());
        AssertExpectedOutputs(fixture, result.Value);
    }

    [Fact]
    public async Task Noncanonical_fixture_suite_covers_current_graph_subset()
    {
        var fixtures = await LoadFixturesAsync();
        var graphFixtures = fixtures
            .Where(fixture => IsGraphRule(fixture.ScoringDocument))
            .ToArray();

        Assert.NotEmpty(graphFixtures);

        var coveredOperations = graphFixtures
            .SelectMany(fixture => ReadNodeOperations(fixture.ScoringDocument))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var operation in CurrentGraphOperations)
        {
            Assert.Contains(operation, coveredOperations);
        }

        Assert.Contains(
            graphFixtures,
            fixture => fixture.ExpectedOutputs.Length > 1 &&
                string.IsNullOrWhiteSpace(fixture.ExpectedErrorCode));
        Assert.Contains(
            graphFixtures,
            fixture => fixture.ExpectedErrorCode == "score.answer_missing" &&
                UsesMinValidCount(fixture.ScoringDocument));
    }

    private static void ValidateFixture(ScoringFixture fixture)
    {
        Assert.Equal("noncanonical_synthetic", fixture.Kind);
        Assert.False(
            fixture.InstrumentCode.Equals("olbi", StringComparison.OrdinalIgnoreCase),
            "A49b fixtures must stay non-canonical and must not use OLBI.");
        Assert.DoesNotContain("olbi", fixture.FixtureId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("olbi", fixture.ScoringRuleId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("olbi", fixture.ScoringDocument.GetRawText(), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(JsonValueKind.Object, fixture.ScoringDocument.ValueKind);
        Assert.Equal(JsonValueKind.Object, fixture.Produces.ValueKind);
        Assert.Equal(JsonValueKind.Object, fixture.Compatibility.ValueKind);
        Assert.NotEmpty(fixture.Answers);

        var hasExpectedOutputs = fixture.ExpectedOutputs.Length > 0;
        var hasExpectedError = !string.IsNullOrWhiteSpace(fixture.ExpectedErrorCode);

        Assert.False(
            hasExpectedOutputs && hasExpectedError,
            "A fixture must not declare both expected outputs and an expected error.");
        Assert.True(
            hasExpectedOutputs || hasExpectedError,
            "A fixture must declare expected outputs or an expected error.");

        foreach (var output in fixture.ExpectedOutputs)
        {
            Assert.False(string.IsNullOrWhiteSpace(output.DimensionCode));
            Assert.True(output.NValid.HasValue, "A49b expected outputs must declare n_valid.");
            Assert.True(output.NExpected.HasValue, "A49b expected outputs must declare n_expected.");
            Assert.Equal("ok", output.MissingPolicyStatus);
        }
    }

    private static void AssertScoringRuleMetadataValid(ScoringFixture fixture)
    {
        var request = new ScoringRuleValidationRequest(
            fixture.ScoringRuleId,
            ReadDocumentString(fixture.ScoringDocument, "rule_version") ??
                fixture.ScoringRuleVersion ??
                "1.0.0",
            ReadDocumentString(fixture.ScoringDocument, "schema_version") ?? "1.0.0",
            ReadDocumentString(fixture.ScoringDocument, "engine_min_version") ?? "1.0.0",
            fixture.ScoringDocument.GetRawText(),
            fixture.Produces.GetRawText(),
            fixture.Compatibility.GetRawText());

        var validation = ScoringRuleValidator.Validate(request);

        Assert.True(validation.IsSuccess, validation.Error.ToString());
    }

    private static void AssertExpectedOutputs(
        ScoringFixture fixture,
        IReadOnlyList<SimpleScoreOutput> actualOutputs)
    {
        var expectedOutputs = fixture.ExpectedOutputs
            .OrderBy(output => output.DimensionCode, StringComparer.Ordinal)
            .ToArray();
        var actual = actualOutputs
            .OrderBy(output => output.DimensionCode, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedOutputs.Length, actual.Length);

        var tolerance = fixture.Tolerance?.NumericAbs ?? 0.0001m;
        for (var index = 0; index < expectedOutputs.Length; index++)
        {
            var expected = expectedOutputs[index];
            var observed = actual[index];

            Assert.Equal(expected.DimensionCode, observed.DimensionCode);
            Assert.Equal(expected.NValid!.Value, observed.NValid);
            Assert.Equal(expected.NExpected!.Value, observed.NExpected);
            Assert.Equal(expected.MissingPolicyStatus, observed.MissingPolicyStatus);
            Assert.InRange(Math.Abs(observed.Value - expected.Value), 0m, tolerance);
        }
    }

    private static async Task<ScoringFixture[]> LoadFixturesAsync()
    {
        var fixtures = new List<ScoringFixture>();
        foreach (var values in FixtureFiles())
        {
            var path = Assert.IsType<string>(Assert.Single(values));
            var fixture = JsonSerializer.Deserialize<ScoringFixture>(
                await File.ReadAllTextAsync(path),
                JsonOptions);

            Assert.NotNull(fixture);
            fixtures.Add(fixture);
        }

        return fixtures.ToArray();
    }

    private static bool IsGraphRule(JsonElement scoringDocument)
    {
        return scoringDocument.TryGetProperty("nodes", out _) ||
            scoringDocument.TryGetProperty("inputs", out _) ||
            scoringDocument.TryGetProperty("outputs", out _);
    }

    private static IEnumerable<string> ReadNodeOperations(JsonElement scoringDocument)
    {
        if (!scoringDocument.TryGetProperty("nodes", out var nodes) ||
            nodes.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var node in nodes.EnumerateArray())
        {
            if (node.TryGetProperty("op", out var op) &&
                op.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(op.GetString()))
            {
                yield return op.GetString()!.Trim().ToLowerInvariant();
            }
        }
    }

    private static bool UsesMinValidCount(JsonElement scoringDocument)
    {
        return scoringDocument.TryGetProperty("missing_data", out var missingData) &&
            missingData.TryGetProperty("defaults", out var defaults) &&
            defaults.TryGetProperty("strategy", out var strategy) &&
            strategy.ValueKind == JsonValueKind.String &&
            string.Equals(strategy.GetString(), "min_valid_count", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadDocumentString(JsonElement scoringDocument, string propertyName)
    {
        return scoringDocument.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!.Trim()
            : null;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Platform.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed record ScoringFixture
    {
        public string FixtureId { get; init; } = string.Empty;

        public string SchemaVersion { get; init; } = string.Empty;

        public string Kind { get; init; } = string.Empty;

        public string InstrumentCode { get; init; } = string.Empty;

        public string ScoringRuleId { get; init; } = string.Empty;

        public string? ScoringRuleVersion { get; init; }

        public JsonElement ScoringDocument { get; init; }

        public JsonElement Produces { get; init; }

        public JsonElement Compatibility { get; init; }

        public Dictionary<string, JsonElement> Answers { get; init; } = [];

        public FixtureExpectedOutput[] ExpectedOutputs { get; init; } = [];

        public string? ExpectedErrorCode { get; init; }

        public FixtureTolerance? Tolerance { get; init; }
    }

    private sealed record FixtureExpectedOutput
    {
        public string DimensionCode { get; init; } = string.Empty;

        public decimal Value { get; init; }

        public int? N { get; init; }

        public int? NValid { get; init; }

        public int? NExpected { get; init; }

        public string? MissingPolicyStatus { get; init; }
    }

    private sealed record FixtureTolerance
    {
        public decimal NumericAbs { get; init; }
    }
}
