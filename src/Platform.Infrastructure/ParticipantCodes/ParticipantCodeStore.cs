using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.ParticipantCodes;

public sealed class ParticipantCodeStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IParticipantCodeHasher hasher)
    : IParticipantCodeStore
{
    public async Task<Result<ParticipantCodeResponse>> ResolveAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        string rawCode,
        CancellationToken cancellationToken)
    {
        ParticipantCodeHashResult hash;
        DateTimeOffset now;

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
                         tenantId,
                         cancellationToken: cancellationToken))
        {
            var series = await db.CampaignSeries
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

            if (series is null)
            {
                return Result.Failure<ParticipantCodeResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
            }

            hash = await hasher.HashAsync(rawCode, series.CodeSalt, cancellationToken);
            now = DateTimeOffset.UtcNow;
            var existing = await db.ParticipantCodes
                .SingleOrDefaultAsync(
                    code => code.CampaignSeriesId == campaignSeriesId && code.Hash == hash.Hash,
                    cancellationToken);

            if (existing is not null)
            {
                existing.SeenAgain(now);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result.Success(new ParticipantCodeResponse(existing.Id, existing.CampaignSeriesId));
            }

            var participantCode = new ParticipantCode(
                PlatformIds.NewId(),
                tenantId,
                campaignSeriesId,
                hash.Hash,
                hash.Parameters.MemoryKiB,
                hash.Parameters.Iterations,
                hash.Parameters.Parallelism,
                hash.Parameters.OutputBytes,
                now);

            db.ParticipantCodes.Add(participantCode);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result.Success(new ParticipantCodeResponse(participantCode.Id, participantCode.CampaignSeriesId));
            }
            catch (DbUpdateException exception) when (IsUniqueParticipantCodeViolation(exception))
            {
                db.Entry(participantCode).State = EntityState.Detached;
                await transaction.RollbackAsync(cancellationToken);
            }
        }

        await using var retryTransaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var raced = await db.ParticipantCodes
            .SingleAsync(
                code => code.CampaignSeriesId == campaignSeriesId && code.Hash == hash.Hash,
                cancellationToken);
        raced.SeenAgain(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await retryTransaction.CommitAsync(cancellationToken);

        return Result.Success(new ParticipantCodeResponse(raced.Id, raced.CampaignSeriesId));
    }

    private static bool IsUniqueParticipantCodeViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgresException.ConstraintName == "ix_participant_code_campaign_series_id_hash";
    }
}
