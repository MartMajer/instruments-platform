using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ParticipantCodes;

public sealed record ResolveParticipantCodeCommand(
    Guid CampaignSeriesId,
    ResolveParticipantCodeRequest Request)
    : IRequest<Result<ParticipantCodeResponse>>;

public sealed class ResolveParticipantCodeValidator : AbstractValidator<ResolveParticipantCodeCommand>
{
    public ResolveParticipantCodeValidator()
    {
        RuleFor(command => command.CampaignSeriesId).NotEmpty();
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.RawCode)
            .NotEmpty()
            .When(command => command.Request is not null);
    }
}

public sealed class ResolveParticipantCodeHandler(
    ICurrentTenant currentTenant,
    IParticipantCodeStore store)
    : IRequestHandler<ResolveParticipantCodeCommand, Result<ParticipantCodeResponse>>
{
    public Task<Result<ParticipantCodeResponse>> Handle(
        ResolveParticipantCodeCommand command,
        CancellationToken cancellationToken)
    {
        return store.ResolveAsync(
            currentTenant.TenantId,
            command.CampaignSeriesId,
            command.Request.RawCode,
            cancellationToken);
    }
}
