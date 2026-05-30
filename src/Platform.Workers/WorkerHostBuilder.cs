using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Application;
using Platform.Infrastructure;
using Platform.Workers.Operations;
using Platform.Workers.Outbox;
using Platform.Workers.Reports;
using Platform.Workers.Retention;

namespace Platform.Workers;

public static class WorkerHostBuilder
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddPlatformApplication();
        builder.Services.AddDataProtection();
        builder.Services.AddRouting();
        builder.Services.AddPlatformInfrastructure(builder.Configuration);
        builder.Services.AddOutboxDispatching();
        builder.Services.AddWorkerHeartbeat(builder.Configuration);
        builder.Services.AddOutboxRelayWorker(builder.Configuration);
        builder.Services.AddRetentionAutomationWorker(builder.Configuration);
        builder.Services.AddReportPdfArtifactWorker(builder.Configuration);

        return builder.Build();
    }
}
