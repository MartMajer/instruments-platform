using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreatePrivateInstrumentImportCommand(
    CreatePrivateInstrumentImportRequest Request) : IRequest<Result<InstrumentSummaryResponse>>;

public sealed class CreatePrivateInstrumentImportValidator
    : AbstractValidator<CreatePrivateInstrumentImportCommand>
{
    public CreatePrivateInstrumentImportValidator()
    {
        RuleFor(command => command.Request.Code).NotEmpty();
        RuleFor(command => command.Request.Version).NotEmpty();
        RuleFor(command => command.Request.FullName).NotEmpty();
        RuleFor(command => command.Request.Domain).NotEmpty();
        RuleFor(command => command.Request.ProvenanceNote).NotEmpty();
        RuleFor(command => command.Request.RightsStatus).NotEmpty();
        RuleFor(command => command.Request.ValidityLabel).NotEmpty();
        RuleFor(command => command.Request.LicenseType).NotEmpty();
    }
}

public sealed class CreatePrivateInstrumentImportHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<CreatePrivateInstrumentImportCommand, Result<InstrumentSummaryResponse>>
{
    public Task<Result<InstrumentSummaryResponse>> Handle(
        CreatePrivateInstrumentImportCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreatePrivateInstrumentImportAsync(
            currentTenant.TenantId,
            command.Request,
            cancellationToken);
    }
}
