using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Setup;
using Platform.Domain.Campaigns;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Setup;

public sealed class CampaignSeriesProofStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope) : ICampaignSeriesProofStore
{
    private const int ExpectedWaveCount = 2;
    private const string Ready = "ready";
    private const string NotReady = "not_ready";

    public async Task<Result<CampaignSeriesTwoWaveProofResponse>> GetTwoWaveProofAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var seriesExists = await db.CampaignSeries
            .AsNoTracking()
            .AnyAsync(series => series.Id == campaignSeriesId, cancellationToken);

        if (!seriesExists)
        {
            return Result.Failure<CampaignSeriesTwoWaveProofResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var waves = await (
            from campaign in db.Campaigns.AsNoTracking()
            join snapshot in db.CampaignLaunchSnapshots.AsNoTracking()
                on campaign.Id equals snapshot.CampaignId
            where campaign.CampaignSeriesId == campaignSeriesId &&
                snapshot.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal
            orderby snapshot.LaunchedAt, campaign.Name
            select new
            {
                campaign.Id,
                campaign.Name,
                campaign.Status,
                snapshot.ResponseIdentityMode
            })
            .ToListAsync(cancellationToken);

        var waveIds = waves.Select(wave => wave.Id).ToArray();
        var submittedRows = await (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking()
                on session.AssignmentId equals assignment.Id
            where waveIds.Contains(assignment.CampaignId) &&
                session.SubmittedAt != null &&
                session.ParticipantCodeId != null
            select new
            {
                assignment.CampaignId,
                ParticipantCodeId = session.ParticipantCodeId!.Value
            })
            .ToListAsync(cancellationToken);

        var submittedByWave = submittedRows
            .GroupBy(row => row.CampaignId)
            .ToDictionary(group => group.Key, group => group.Count());
        var linkedTrajectoryCount = submittedRows
            .Select(row => row.ParticipantCodeId)
            .Distinct()
            .Count();
        var completeTrajectoryCount = submittedRows
            .GroupBy(row => row.ParticipantCodeId)
            .Count(group => group.Select(row => row.CampaignId).Distinct().Count() >= ExpectedWaveCount);
        var submittedWaveCount = submittedByWave.Count;
        var proofStatus =
            waves.Count >= ExpectedWaveCount &&
            submittedWaveCount >= ExpectedWaveCount &&
            completeTrajectoryCount > 0
                ? Ready
                : NotReady;

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignSeriesTwoWaveProofResponse(
            campaignSeriesId,
            proofStatus,
            ExpectedWaveCount,
            waves.Count,
            submittedWaveCount,
            linkedTrajectoryCount,
            completeTrajectoryCount,
            waves
                .Select(wave => new TwoWaveProofWaveResponse(
                    wave.Id,
                    wave.Name,
                    wave.Status,
                    wave.ResponseIdentityMode,
                    submittedByWave.GetValueOrDefault(wave.Id)))
                .ToArray()));
    }
}
