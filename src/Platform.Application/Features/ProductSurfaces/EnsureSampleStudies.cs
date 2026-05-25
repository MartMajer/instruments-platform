using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record EnsureSampleStudiesCommand : IRequest<Result<EnsureSampleStudiesResponse>>;

public interface ISampleStudySeeder
{
    Task<Result<EnsureSampleStudiesResponse>> EnsureAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

public sealed class EnsureSampleStudiesHandler(
    ICurrentTenant currentTenant,
    ICurrentActor currentActor,
    ISampleStudySeeder seeder)
    : IRequestHandler<EnsureSampleStudiesCommand, Result<EnsureSampleStudiesResponse>>
{
    public Task<Result<EnsureSampleStudiesResponse>> Handle(
        EnsureSampleStudiesCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentActor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<EnsureSampleStudiesResponse>(
                Error.Forbidden("actor.required", "A signed-in workspace member is required.")));
        }

        return seeder.EnsureAsync(
            currentTenant.TenantId,
            currentActor.UserId.Value,
            cancellationToken);
    }
}
