namespace Platform.Application.Features.Setup;

public sealed record CampaignSeriesTwoWaveProofResponse(
    Guid CampaignSeriesId,
    string ProofStatus,
    int ExpectedWaveCount,
    int LaunchedWaveCount,
    int SubmittedWaveCount,
    int LinkedTrajectoryCount,
    int CompleteTrajectoryCount,
    IReadOnlyList<TwoWaveProofWaveResponse> Waves);

public sealed record TwoWaveProofWaveResponse(
    Guid CampaignId,
    string Name,
    string Status,
    string ResponseIdentityMode,
    int SubmittedResponseCount);
