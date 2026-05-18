using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public sealed record GetExportArtifactQuery(Guid ArtifactId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class GetExportArtifactValidator : AbstractValidator<GetExportArtifactQuery>
{
    public GetExportArtifactValidator()
    {
        RuleFor(query => query.ArtifactId).NotEmpty();
    }
}

public sealed class GetExportArtifactHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<GetExportArtifactQuery, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        GetExportArtifactQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetExportArtifactAsync(
            currentTenant.TenantId,
            query.ArtifactId,
            cancellationToken);
    }
}

public sealed record GetExportArtifactDownloadQuery(Guid ArtifactId)
    : IRequest<Result<ExportArtifactDownloadResponse>>;

public sealed class GetExportArtifactDownloadValidator
    : AbstractValidator<GetExportArtifactDownloadQuery>
{
    public GetExportArtifactDownloadValidator()
    {
        RuleFor(query => query.ArtifactId).NotEmpty();
    }
}

public sealed class GetExportArtifactDownloadHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<GetExportArtifactDownloadQuery, Result<ExportArtifactDownloadResponse>>
{
    public Task<Result<ExportArtifactDownloadResponse>> Handle(
        GetExportArtifactDownloadQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetExportArtifactDownloadAsync(
            currentTenant.TenantId,
            query.ArtifactId,
            cancellationToken);
    }
}

public sealed record GetExportArtifactSignedDownloadUrlQuery(Guid ArtifactId)
    : IRequest<Result<ExportArtifactSignedDownloadUrlResponse>>;

public sealed class GetExportArtifactSignedDownloadUrlValidator
    : AbstractValidator<GetExportArtifactSignedDownloadUrlQuery>
{
    public GetExportArtifactSignedDownloadUrlValidator()
    {
        RuleFor(query => query.ArtifactId).NotEmpty();
    }
}

public sealed class GetExportArtifactSignedDownloadUrlHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<GetExportArtifactSignedDownloadUrlQuery, Result<ExportArtifactSignedDownloadUrlResponse>>
{
    private static readonly TimeSpan DefaultExpiresIn = TimeSpan.FromMinutes(15);

    public Task<Result<ExportArtifactSignedDownloadUrlResponse>> Handle(
        GetExportArtifactSignedDownloadUrlQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetExportArtifactSignedDownloadUrlAsync(
            currentTenant.TenantId,
            query.ArtifactId,
            DefaultExpiresIn,
            cancellationToken);
    }
}

public sealed record RetryCampaignSeriesReportPdfArtifactCommand(Guid ArtifactId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class RetryCampaignSeriesReportPdfArtifactValidator
    : AbstractValidator<RetryCampaignSeriesReportPdfArtifactCommand>
{
    public RetryCampaignSeriesReportPdfArtifactValidator()
    {
        RuleFor(command => command.ArtifactId).NotEmpty();
    }
}

public sealed class RetryCampaignSeriesReportPdfArtifactHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<RetryCampaignSeriesReportPdfArtifactCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        RetryCampaignSeriesReportPdfArtifactCommand command,
        CancellationToken cancellationToken)
    {
        return store.RetryCampaignSeriesReportPdfArtifactAsync(
            currentTenant.TenantId,
            command.ArtifactId,
            cancellationToken);
    }
}
