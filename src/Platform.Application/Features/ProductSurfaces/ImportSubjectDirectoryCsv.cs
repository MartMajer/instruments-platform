using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record ImportSubjectDirectoryCsvCommand(SubjectDirectoryCsvImportRequest Request)
    : IRequest<Result<SubjectDirectoryCsvImportResponse>>;

public sealed class ImportSubjectDirectoryCsvValidator
    : AbstractValidator<ImportSubjectDirectoryCsvCommand>
{
    public ImportSubjectDirectoryCsvValidator()
    {
        RuleFor(command => command.Request.CsvContent)
            .NotEmpty()
            .MaximumLength(262_144);
    }
}

public sealed class ImportSubjectDirectoryCsvHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceWriteStore store)
    : IRequestHandler<ImportSubjectDirectoryCsvCommand, Result<SubjectDirectoryCsvImportResponse>>
{
    public Task<Result<SubjectDirectoryCsvImportResponse>> Handle(
        ImportSubjectDirectoryCsvCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<SubjectDirectoryCsvImportResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.ImportSubjectDirectoryCsvAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            command.Request,
            cancellationToken);
    }
}
