namespace Platform.UnitTests.Architecture;

public sealed class SpecCodeInvariantTests
{
    [Fact]
    public void Production_source_does_not_use_ef_bulk_operations()
    {
        var root = FindRepoRoot();
        var offenders = ProductionSourceFiles(root)
            .Where(file =>
                file.Content.Contains("ExecuteUpdate", StringComparison.Ordinal) ||
                file.Content.Contains("ExecuteDelete", StringComparison.Ordinal))
            .Select(file => file.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "EF ExecuteUpdate/ExecuteDelete bypass ChangeTracker audit and tenant-scope review; offenders: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Production_source_does_not_write_to_console()
    {
        var root = FindRepoRoot();
        var offenders = ProductionSourceFiles(root)
            .Where(file =>
                file.Content.Contains("Console.Write", StringComparison.Ordinal) ||
                file.Content.Contains("Console.Out", StringComparison.Ordinal) ||
                file.Content.Contains("Console.Error", StringComparison.Ordinal))
            .Select(file => file.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Production source must not ship placeholder/direct console writes; offenders: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Invariant_ledger_covers_current_guardrail_set()
    {
        var root = FindRepoRoot();
        var ledgerPath = Path.Combine(root, "docs", "v2", "20-architecture", "spec-code-invariants.md");

        Assert.True(File.Exists(ledgerPath), "Missing spec-code invariant ledger.");

        var ledger = File.ReadAllText(ledgerPath);
        var requiredFragments = new[]
        {
            "INV-001",
            "UUIDv7",
            "INV-002",
            "ExecuteUpdate",
            "ExecuteDelete",
            "INV-003",
            "Console.Write",
            "INV-004",
            "Sensitive values",
            "raw tokens",
            "participant codes",
            "INV-005",
            "Tenant scope",
            "INV-006",
            "Worker/outbox bootstrap",
            "INV-010",
            "Outbox event payloads",
            "64 KiB"
        };

        var missing = requiredFragments
            .Where(fragment => !ledger.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Invariant ledger is missing required fragments: " + string.Join(", ", missing));
    }

    private static IEnumerable<(string Path, string Content)> ProductionSourceFiles(string root)
    {
        var src = Path.Combine(root, "src");
        return Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => (
                Path: Path.GetRelativePath(root, path),
                Content: File.ReadAllText(path)));
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

        throw new InvalidOperationException("Could not find repository root.");
    }
}
