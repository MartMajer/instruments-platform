namespace Platform.Application.Features.Scoring;

public sealed record ComputeScoresResponse(
    Guid ScoreRunId,
    Guid SessionId,
    IReadOnlyList<ComputedScoreResponse> Scores);

public sealed record ComputedScoreResponse(
    string DimensionCode,
    decimal Value,
    int NValid,
    int NExpected,
    string MissingPolicyStatus);
