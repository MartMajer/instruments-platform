using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Scoring;

public sealed record ComputeResponseScoresCommand(Guid SessionId)
    : IRequest<Result<ComputeScoresResponse>>;

public sealed class ComputeResponseScoresValidator : AbstractValidator<ComputeResponseScoresCommand>
{
    public ComputeResponseScoresValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty();
    }
}

public sealed class ComputeResponseScoresHandler(
    ICurrentTenant currentTenant,
    IScoreComputationStore store)
    : IRequestHandler<ComputeResponseScoresCommand, Result<ComputeScoresResponse>>
{
    public Task<Result<ComputeScoresResponse>> Handle(
        ComputeResponseScoresCommand command,
        CancellationToken cancellationToken)
    {
        return store.ComputeResponseScoresAsync(
            currentTenant.TenantId,
            command.SessionId,
            cancellationToken);
    }
}
