using System.Reflection;
using Platform.SharedKernel;

namespace Platform.UnitTests.Architecture;

public sealed class IdentifierGenerationTests
{
    [Fact]
    public void Shared_kernel_exposes_uuid_v7_identifier_generator()
    {
        var sharedKernelAssembly = typeof(Result).Assembly;
        var generatorType = sharedKernelAssembly.GetType("Platform.SharedKernel.PlatformIds");

        Assert.NotNull(generatorType);
        var method = generatorType.GetMethod(
            "NewId",
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        var id = Assert.IsType<Guid>(method.Invoke(null, null));

        Assert.Equal('7', id.ToString("D")[14]);
    }

    [Fact]
    public void Production_source_uses_platform_identifier_generator()
    {
        var root = FindRepoRoot();
        var src = Path.Combine(root, "src");
        var offenders = Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => new
            {
                Path = Path.GetRelativePath(root, path),
                Content = File.ReadAllText(path)
            })
            .Where(file =>
                file.Content.Contains("Guid.NewGuid(", StringComparison.Ordinal) ||
                (!file.Path.EndsWith(
                    Path.Combine("src", "Platform.SharedKernel", "PlatformIds.cs"),
                    StringComparison.OrdinalIgnoreCase) &&
                 file.Content.Contains("Guid.CreateVersion7(", StringComparison.Ordinal)))
            .Select(file => file.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Use PlatformIds.NewId() for production identifiers instead of direct Guid creation: "
            + string.Join(", ", offenders));
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
