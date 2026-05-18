namespace Platform.Domain.Scoring;

public sealed record SimpleScoreOutput(
    string DimensionCode,
    decimal Value,
    int NValid,
    int? NExpected = null,
    string? MissingPolicyStatus = null);
