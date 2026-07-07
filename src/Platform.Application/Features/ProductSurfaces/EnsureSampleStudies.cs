using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

// Locale selects the language of the example content ("en" default, "hr"/"hr-HR"
// for Croatian); example studies should read believably in the researcher's language.
public sealed record EnsureSampleStudiesCommand(string? Locale = null)
    : IRequest<Result<EnsureSampleStudiesResponse>>;

public interface ISampleStudySeeder
{
    Task<Result<EnsureSampleStudiesResponse>> EnsureAsync(
        Guid tenantId,
        Guid actorUserId,
        string? locale,
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
            command.Locale,
            cancellationToken);
    }
}
