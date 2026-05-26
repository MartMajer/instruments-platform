using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Reports;

public sealed record CreateCampaignReportProofExportCommand(Guid CampaignId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class CreateCampaignReportProofExportValidator
    : AbstractValidator<CreateCampaignReportProofExportCommand>
{
    public CreateCampaignReportProofExportValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateCampaignReportProofExportHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<CreateCampaignReportProofExportCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        CreateCampaignReportProofExportCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignReportProofExportAsync(
            currentTenant.TenantId,
            command.CampaignId,
            cancellationToken);
    }
}

public sealed record CreateCampaignSeriesResponseExportCommand(Guid CampaignSeriesId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class CreateCampaignSeriesResponseExportValidator
    : AbstractValidator<CreateCampaignSeriesResponseExportCommand>
{
    public CreateCampaignSeriesResponseExportValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class CreateCampaignSeriesResponseExportHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<CreateCampaignSeriesResponseExportCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        CreateCampaignSeriesResponseExportCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignSeriesResponseExportAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            cancellationToken);
    }
}

public sealed record CreateCampaignSeriesResultsMatrixExportCommand(Guid CampaignSeriesId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class CreateCampaignSeriesResultsMatrixExportValidator
    : AbstractValidator<CreateCampaignSeriesResultsMatrixExportCommand>
{
    public CreateCampaignSeriesResultsMatrixExportValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class CreateCampaignSeriesResultsMatrixExportHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<CreateCampaignSeriesResultsMatrixExportCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        CreateCampaignSeriesResultsMatrixExportCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignSeriesResultsMatrixExportAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            cancellationToken);
    }
}

public sealed record CreateCampaignSeriesReportHtmlArtifactCommand(Guid CampaignSeriesId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class CreateCampaignSeriesReportHtmlArtifactValidator
    : AbstractValidator<CreateCampaignSeriesReportHtmlArtifactCommand>
{
    public CreateCampaignSeriesReportHtmlArtifactValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class CreateCampaignSeriesReportHtmlArtifactHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<CreateCampaignSeriesReportHtmlArtifactCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        CreateCampaignSeriesReportHtmlArtifactCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignSeriesReportHtmlArtifactAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            cancellationToken);
    }
}

public sealed record CreateCampaignSeriesReportPdfArtifactCommand(Guid CampaignSeriesId)
    : IRequest<Result<ReportProofExportArtifactResponse>>;

public sealed class CreateCampaignSeriesReportPdfArtifactValidator
    : AbstractValidator<CreateCampaignSeriesReportPdfArtifactCommand>
{
    public CreateCampaignSeriesReportPdfArtifactValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
    }
}

public sealed class CreateCampaignSeriesReportPdfArtifactHandler(
    ICurrentTenant currentTenant,
    IReportProofExportStore store)
    : IRequestHandler<CreateCampaignSeriesReportPdfArtifactCommand, Result<ReportProofExportArtifactResponse>>
{
    public Task<Result<ReportProofExportArtifactResponse>> Handle(
        CreateCampaignSeriesReportPdfArtifactCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignSeriesReportPdfArtifactAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            cancellationToken);
    }
}
