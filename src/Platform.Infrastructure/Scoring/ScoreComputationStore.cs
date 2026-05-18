using Platform.Application.Features.Scoring;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Scoring;

public sealed class ScoreComputationStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    SubmittedResponseScoreMaterializer? scoreMaterializer = null) : IScoreComputationStore
{
    private readonly SubmittedResponseScoreMaterializer submittedScoreMaterializer =
        scoreMaterializer ?? new SubmittedResponseScoreMaterializer(db);

    public async Task<Result<ComputeScoresResponse>> ComputeResponseScoresAsync(
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var materialized = await submittedScoreMaterializer.MaterializeAsync(
            tenantId,
            sessionId,
            requireScoringRule: true,
            cancellationToken);

        if (materialized.IsFailure)
        {
            return Result.Failure<ComputeScoresResponse>(materialized.Error);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ComputeScoresResponse(
            materialized.Value.ScoreRunId!.Value,
            materialized.Value.SessionId,
            materialized.Value.Scores));
    }
}
