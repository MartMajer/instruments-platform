using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Reports;

public static class ReportProofEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapReportProofEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/campaigns/{campaignId:guid}/report-proof", GetCampaignReportProof)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignReportProof")
            .WithTags("Reports");

        app.MapPost("/campaigns/{campaignId:guid}/report-proof/exports", CreateCampaignReportProofExport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignReportProofExport")
            .WithTags("Reports");

        app.MapPost("/campaign-series/{campaignSeriesId:guid}/response-exports", CreateCampaignSeriesResponseExport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignSeriesResponseExport")
            .WithTags("Reports");

        app.MapPost("/campaign-series/{campaignSeriesId:guid}/results-matrix-exports", CreateCampaignSeriesResultsMatrixExport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignSeriesResultsMatrixExport")
            .WithTags("Reports");

        app.MapPost("/campaign-series/{campaignSeriesId:guid}/report-html-artifacts", CreateCampaignSeriesReportHtmlArtifact)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignSeriesReportHtmlArtifact")
            .WithTags("Reports");

        app.MapPost("/campaign-series/{campaignSeriesId:guid}/report-pdf-artifacts", CreateCampaignSeriesReportPdfArtifact)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignSeriesReportPdfArtifact")
            .WithTags("Reports");

        app.MapGet("/export-artifacts/{artifactId:guid}", GetExportArtifact)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetExportArtifact")
            .WithTags("Reports");

        app.MapGet("/export-artifacts/{artifactId:guid}/download", DownloadExportArtifact)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("DownloadExportArtifact")
            .WithTags("Reports");

        app.MapGet("/export-artifacts/{artifactId:guid}/codebook", DownloadExportArtifactCodebook)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("DownloadExportArtifactCodebook")
            .WithTags("Reports");

        app.MapGet("/export-artifacts/{artifactId:guid}/signed-download-url", GetExportArtifactSignedDownloadUrl)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetExportArtifactSignedDownloadUrl")
            .WithTags("Reports");

        app.MapPost("/export-artifacts/{artifactId:guid}/retry", RetryExportArtifact)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RetryExportArtifact")
            .WithTags("Reports");

        app.MapGet("/campaign-series/{campaignSeriesId:guid}/wave-comparison-proof", GetCampaignSeriesWaveComparisonProof)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesWaveComparisonProof")
            .WithTags("Reports");

        return app;
    }

    private static async Task<IResult> GetCampaignReportProof(
        Guid campaignId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignReportProofQuery(campaignId), cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesWaveComparisonProof(
        Guid campaignSeriesId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCampaignSeriesWaveComparisonProofQuery(campaignSeriesId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetExportArtifact(
        Guid artifactId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportArtifactQuery(artifactId), cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> DownloadExportArtifact(
        Guid artifactId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportArtifactDownloadQuery(artifactId), cancellationToken);

        return ReportProofHttpResults.ToFile(result);
    }

    private static async Task<IResult> DownloadExportArtifactCodebook(
        Guid artifactId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportArtifactCodebookQuery(artifactId), cancellationToken);

        return ReportProofHttpResults.ToFile(result);
    }

    private static async Task<IResult> GetExportArtifactSignedDownloadUrl(
        Guid artifactId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportArtifactSignedDownloadUrlQuery(artifactId), cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> RetryExportArtifact(
        Guid artifactId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RetryCampaignSeriesReportPdfArtifactCommand(artifactId), cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignReportProofExport(
        Guid campaignId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignReportProofExportCommand(campaignId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignSeriesResponseExport(
        Guid campaignSeriesId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignSeriesResponseExportCommand(campaignSeriesId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignSeriesResultsMatrixExport(
        Guid campaignSeriesId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignSeriesResultsMatrixExportCommand(campaignSeriesId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignSeriesReportHtmlArtifact(
        Guid campaignSeriesId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignSeriesReportHtmlArtifactCommand(campaignSeriesId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignSeriesReportPdfArtifact(
        Guid campaignSeriesId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignSeriesReportPdfArtifactCommand(campaignSeriesId),
            cancellationToken);

        return ReportProofHttpResults.ToOk(result);
    }
}
