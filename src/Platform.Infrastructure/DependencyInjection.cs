using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.Application.Features.System.GetHealth;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.Operations;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.Reports;
using Platform.Application.Features.Responses;
using Platform.Application.Features.Retention;
using Platform.Application.Features.Scoring;
using Platform.Application.Features.Setup;
using Platform.Application.Features.TestData;
using Platform.Infrastructure.Campaigns.RespondentRules;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Interceptors;
using Platform.Infrastructure.Notifications;
using Platform.Infrastructure.Operations;
using Platform.Infrastructure.ParticipantCodes;
using Platform.Infrastructure.ProductSurfaces;
using Platform.Infrastructure.Reports;
using Platform.Infrastructure.Responses;
using Platform.Infrastructure.Retention;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Setup;
using Platform.Infrastructure.Health;
using Platform.Infrastructure.Tenancy;
using Platform.Infrastructure.TestData;

namespace Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PlatformDb")
            ?? throw new InvalidOperationException("Connection string 'PlatformDb' is required.");

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<OutboxSaveChangesInterceptor>();
        services.Configure<EmailDeliveryOptions>(configuration.GetSection(EmailDeliveryOptions.SectionName));
        services
            .AddOptions<ExportArtifactObjectStoreOptions>()
            .Bind(configuration.GetSection(ExportArtifactObjectStoreOptions.SectionName))
            .Validate(
                options => options.UsesLocalProvider() || options.UsesS3CompatibleProvider(),
                "ExportArtifacts:ObjectStore:Provider must be 'local' or 's3_compatible'.");
        services.Configure<ReportPdfRendererOptions>(
            configuration.GetSection(ReportPdfRendererOptions.SectionName));
        services.Configure<ParticipantCodeHashingOptions>(
            configuration.GetSection(ParticipantCodeHashingOptions.SectionName));
        services
            .AddOptions<OutboxOperationalReadinessOptions>()
            .Bind(configuration.GetSection(OutboxOperationalReadinessOptions.SectionName))
            .Validate(
                options => options.DueBacklogUnreadyAfterMinutes > 0,
                "OutboxOperations:DueBacklogUnreadyAfterMinutes must be greater than zero.");
        services
            .AddOptions<WorkerHeartbeatReadinessOptions>()
            .Bind(configuration.GetSection(WorkerHeartbeatReadinessOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExpectedWorkerName),
                "WorkerHeartbeatReadiness:ExpectedWorkerName must not be empty.")
            .Validate(
                options => options.ExpectedWorkerName.Length <= 128,
                "WorkerHeartbeatReadiness:ExpectedWorkerName must be 128 characters or fewer.")
            .Validate(
                options => options.ExpectedWorkerName.All(character =>
                    char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'),
                "WorkerHeartbeatReadiness:ExpectedWorkerName contains unsupported characters.")
            .Validate(
                options => options.StaleAfterSeconds > 0,
                "WorkerHeartbeatReadiness:StaleAfterSeconds must be greater than zero.");

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<OutboxSaveChangesInterceptor>(),
                    serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<ITenantDbScope, TenantDbScope>();
        services.AddScoped<IPlatformHealthCheck, DatabaseConnectivityHealthCheck>();
        services.AddScoped<IOutboxOperationalSnapshotStore, OutboxOperationalSnapshotStore>();
        services.AddScoped<IWorkerHeartbeatStore, WorkerHeartbeatStore>();
        services.AddScoped<IPlatformHealthCheck, WorkerHeartbeatHealthCheck>();
        services.AddScoped<IPlatformHealthCheck, OutboxDeadLetterHealthCheck>();
        services.AddScoped<IPlatformHealthCheck, OutboxDueBacklogHealthCheck>();
        services.AddScoped<IPlatformHealthCheck, EmailDeliveryConfigurationHealthCheck>();
        services.AddScoped<RespondentRuleResolver>();
        services.AddScoped<ISetupWorkflowStore, SetupWorkflowStore>();
        services.AddScoped<ITestDataSimulatorStore, TestDataSimulatorStore>();
        services.AddScoped<ICampaignSeriesProofStore, CampaignSeriesProofStore>();
        services.AddScoped<IProductSurfaceReadStore, ProductSurfaceReadStore>();
        services.AddScoped<IProductSurfaceWriteStore, ProductSurfaceWriteStore>();
        services.AddScoped<ISampleStudySeeder, SampleStudySeeder>();
        services.AddScoped<INotificationDeliveryStore, NotificationDeliveryStore>();
        services.AddScoped<IOperationalNotificationStore, OperationalNotificationStore>();
        services.AddScoped<SubmittedResponseScoreMaterializer>();
        services.AddScoped<IResponseCaptureStore, ResponseCaptureStore>();
        services.AddScoped<IScoreComputationStore, ScoreComputationStore>();
        services.AddScoped<IReportProofStore, ReportProofStore>();
        services.AddScoped<ICampaignSeriesReportHtmlRenderer, CampaignSeriesReportHtmlRenderer>();
        services.AddScoped<LocalExportArtifactObjectStore>();
        services.AddScoped<S3CompatibleExportArtifactObjectStore>();
        services.AddScoped<UnsupportedExportArtifactSignedUrlProvider>();
        services.AddScoped<S3CompatibleExportArtifactSignedUrlProvider>();
        services.AddScoped<IExportArtifactObjectStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExportArtifactObjectStoreOptions>>().Value;

            return options.UsesS3CompatibleProvider()
                ? serviceProvider.GetRequiredService<S3CompatibleExportArtifactObjectStore>()
                : serviceProvider.GetRequiredService<LocalExportArtifactObjectStore>();
        });
        services.AddScoped<IExportArtifactSignedUrlProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExportArtifactObjectStoreOptions>>().Value;

            return options.UsesS3CompatibleProvider()
                ? serviceProvider.GetRequiredService<S3CompatibleExportArtifactSignedUrlProvider>()
                : serviceProvider.GetRequiredService<UnsupportedExportArtifactSignedUrlProvider>();
        });
        services.AddScoped<IPlatformHealthCheck, ExportArtifactObjectStoreHealthCheck>();
        services.AddScoped<IPlatformHealthCheck, ReportPdfRendererHealthCheck>();
        services.AddScoped<IReportPdfRenderer, PuppeteerSharpReportPdfRenderer>();
        services.AddScoped<IReportProofExportStore, ReportProofExportStore>();
        services.AddScoped<IReportPdfArtifactWorkerTenantSource, ReportPdfArtifactWorkerTenantSource>();
        services.AddScoped<IWaveComparisonProofStore, WaveComparisonProofStore>();
        services.AddScoped<IRetentionDueCandidateStore, RetentionDueCandidateStore>();
        services.AddScoped<IRetentionDueBatchStore, RetentionDueBatchStore>();
        services.AddScoped<IRetentionAutomationTenantSource, RetentionAutomationTenantSource>();
        services.AddScoped<IWithdrawalRuntimeStore, WithdrawalRuntimeStore>();
        services.AddScoped<IParticipantCodeStore, ParticipantCodeStore>();
        services.AddScoped<IParticipantCodeHasher>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ParticipantCodeHashingOptions>>().Value;
            options.EnsureValid();

            return new Argon2idParticipantCodeHasher(options);
        });
        services.AddScoped<LocalDevEmailDeliveryProvider>();
        services.AddScoped<SmtpEmailDeliveryProvider>();
        services
            .AddHttpClient<IAwsSnsSignatureVerifier, AwsSnsSignatureVerifier>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        services
            .AddHttpClient<IAwsSnsSubscriptionConfirmer, AwsSnsSubscriptionConfirmer>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        services.AddScoped<IEmailDeliveryProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmailDeliveryOptions>>().Value;
            options.EnsureValidProviderConfiguration();

            return string.Equals(options.Provider, EmailDeliveryProviderNames.Smtp, StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<SmtpEmailDeliveryProvider>()
                : serviceProvider.GetRequiredService<LocalDevEmailDeliveryProvider>();
        });

        return services;
    }
}
