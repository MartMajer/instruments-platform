using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record CreateLabAssignmentCommand(Guid CampaignId)
    : IRequest<Result<LabAssignmentResponse>>;

public sealed class CreateLabAssignmentValidator : AbstractValidator<CreateLabAssignmentCommand>
{
    public CreateLabAssignmentValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
    }
}

public sealed class CreateLabAssignmentHandler(
    ICurrentTenant currentTenant,
    IResponseCaptureStore store)
    : IRequestHandler<CreateLabAssignmentCommand, Result<LabAssignmentResponse>>
{
    public Task<Result<LabAssignmentResponse>> Handle(
        CreateLabAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateLabAssignmentAsync(currentTenant.TenantId, command.CampaignId, cancellationToken);
    }
}
