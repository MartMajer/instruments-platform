namespace Platform.Domain.Scoring;

public sealed record SimpleScoreInput(
    string QuestionCode,
    string? Value,
    bool IsSkipped = false,
    bool IsNa = false);
