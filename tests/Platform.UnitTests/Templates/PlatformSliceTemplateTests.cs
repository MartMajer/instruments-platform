using System.Diagnostics;

namespace Platform.UnitTests.Templates;

public sealed class PlatformSliceTemplateTests
{
    [Fact]
    public async Task Template_generates_command_slice_in_feature_folder()
    {
        using var workspace = TemplateWorkspace.Create();

        AssertCommandSucceeded(await workspace.InstallTemplateAsync());

        var result = await workspace.RunDotnetAsync(
            "new",
            "platform-slice",
            "-n",
            "CreateCampaign",
            "--feature",
            "Campaigns");

        AssertCommandSucceeded(result);

        var commandPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignCommand.cs");
        var handlerPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignHandler.cs");
        var validatorPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignValidator.cs");
        var endpointPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignEndpoint.cs");
        var responsePath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignResponse.cs");
        var testPath = workspace.OutputPath(
            "tests",
            "Platform.UnitTests",
            "Application",
            "Campaigns",
            "CreateCampaign",
            "CreateCampaignSliceTests.cs");

        Assert.True(File.Exists(commandPath), $"Expected generated file: {commandPath}");
        Assert.True(File.Exists(handlerPath), $"Expected generated file: {handlerPath}");
        Assert.True(File.Exists(validatorPath), $"Expected generated file: {validatorPath}");
        Assert.True(File.Exists(endpointPath), $"Expected generated file: {endpointPath}");
        Assert.True(File.Exists(responsePath), $"Expected generated file: {responsePath}");
        Assert.True(File.Exists(testPath), $"Expected generated file: {testPath}");

        var command = await File.ReadAllTextAsync(commandPath);
        Assert.Contains("namespace Platform.Application.Features.Campaigns.CreateCampaign;", command);
        Assert.Contains("public sealed record CreateCampaignCommand : IRequest<Result<CreateCampaignResponse>>;", command);

        var handler = await File.ReadAllTextAsync(handlerPath);
        Assert.Contains("IRequestHandler<CreateCampaignCommand, Result<CreateCampaignResponse>>", handler);
        Assert.Contains("CreateCampaign.NotImplemented", handler);

        var validator = await File.ReadAllTextAsync(validatorPath);
        Assert.Contains("AbstractValidator<CreateCampaignCommand>", validator);

        var endpoint = await File.ReadAllTextAsync(endpointPath);
        Assert.Contains("public static IEndpointRouteBuilder MapCreateCampaignEndpoint", endpoint);
        Assert.Contains("app.MapPost(", endpoint);
        Assert.Contains("\"/api/Campaigns/CreateCampaign\"", endpoint);
        Assert.Contains("new CreateCampaignCommand()", endpoint);

        var test = await File.ReadAllTextAsync(testPath);
        Assert.Contains("namespace Platform.UnitTests.Application.Campaigns.CreateCampaign;", test);
        Assert.Contains("Application_registration_dispatches_CreateCampaign_Command", test);
        Assert.Contains("GetServices<IValidator<CreateCampaignCommand>>", test);
    }

    [Fact]
    public async Task Template_generates_query_slice_when_requested()
    {
        using var workspace = TemplateWorkspace.Create();

        AssertCommandSucceeded(await workspace.InstallTemplateAsync());

        var result = await workspace.RunDotnetAsync(
            "new",
            "platform-slice",
            "-n",
            "GetCampaign",
            "--feature",
            "Campaigns",
            "--requestKind",
            "Query",
            "--httpVerb",
            "Get");

        AssertCommandSucceeded(result);

        var queryPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "GetCampaign",
            "GetCampaignQuery.cs");
        var commandPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "GetCampaign",
            "GetCampaignCommand.cs");
        var endpointPath = workspace.OutputPath(
            "src",
            "Platform.Application",
            "Features",
            "Campaigns",
            "GetCampaign",
            "GetCampaignEndpoint.cs");

        Assert.True(File.Exists(queryPath), $"Expected generated file: {queryPath}");
        Assert.False(File.Exists(commandPath), $"Did not expect command file for query slice: {commandPath}");

        var query = await File.ReadAllTextAsync(queryPath);
        Assert.Contains("public sealed record GetCampaignQuery : IRequest<Result<GetCampaignResponse>>;", query);
        Assert.DoesNotContain("GetCampaignCommand", query);

        var endpoint = await File.ReadAllTextAsync(endpointPath);
        Assert.Contains("app.MapGet(", endpoint);
        Assert.Contains("new GetCampaignQuery()", endpoint);
    }

    private static void AssertCommandSucceeded(CommandResult result)
    {
        Assert.True(
            result.ExitCode == 0,
            $"""
            Expected dotnet command to succeed.
            Exit code: {result.ExitCode}
            STDOUT:
            {result.StandardOutput}
            STDERR:
            {result.StandardError}
            """);
    }

    private sealed class TemplateWorkspace : IDisposable
    {
        private TemplateWorkspace(string root, string cliHome, string outputRoot, string templateRoot)
        {
            Root = root;
            CliHome = cliHome;
            OutputRoot = outputRoot;
            TemplateRoot = templateRoot;
        }

        private string Root { get; }

        private string CliHome { get; }

        private string OutputRoot { get; }

        private string TemplateRoot { get; }

        public static TemplateWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "platform-slice-template-tests", Guid.NewGuid().ToString("N"));
            var cliHome = Path.Combine(root, "dotnet-home");
            var outputRoot = Path.Combine(root, "output");
            var templateRoot = Path.Combine(FindRepoRoot(), "templates", "platform-slice");

            Directory.CreateDirectory(cliHome);
            Directory.CreateDirectory(outputRoot);

            return new TemplateWorkspace(root, cliHome, outputRoot, templateRoot);
        }

        public string OutputPath(params string[] segments)
        {
            return Path.Combine([OutputRoot, .. segments]);
        }

        public Task<CommandResult> InstallTemplateAsync()
        {
            return RunDotnetAsync("new", "install", TemplateRoot);
        }

        public async Task<CommandResult> RunDotnetAsync(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = OutputRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.Environment["DOTNET_CLI_HOME"] = CliHome;
            startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            startInfo.Environment["DOTNET_NOLOGO"] = "1";

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start dotnet process.");
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(90));

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellation.Token);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellation.Token);

            try
            {
                await process.WaitForExitAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Process already exited.
                }

                throw;
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            return new CommandResult(process.ExitCode, standardOutput, standardError);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
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

            throw new InvalidOperationException("Could not locate repository root from test output directory.");
        }
    }

    private sealed record CommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
