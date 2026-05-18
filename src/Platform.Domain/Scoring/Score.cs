namespace Platform.Domain.Scoring;

public sealed class Score
{
    private Score()
    {
    }

    public Score(
        Guid id,
        Guid tenantId,
        Guid scoreRunId,
        Guid campaignId,
        Guid responseSessionId,
        string dimensionCode,
        decimal value,
        int nValid,
        int? nExpected = null,
        string? missingPolicyStatus = null,
        DateTimeOffset? computedAt = null)
    {
        if (nValid < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nValid), "Score valid sample count must not be negative.");
        }

        var resolvedNExpected = nExpected ?? nValid;
        if (resolvedNExpected < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nExpected),
                "Score expected sample count must not be negative.");
        }

        if (nValid > resolvedNExpected)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nValid),
                "Score valid sample count must not exceed expected sample count.");
        }

        Id = id;
        TenantId = tenantId;
        ScoreRunId = scoreRunId;
        CampaignId = campaignId;
        ResponseSessionId = responseSessionId;
        DimensionCode = NormalizeRequired(dimensionCode, nameof(dimensionCode)).ToLowerInvariant();
        Value = Math.Round(value, 4, MidpointRounding.AwayFromZero);
        NValid = nValid;
        NExpected = resolvedNExpected;
        MissingPolicyStatus = NormalizeRequired(
                missingPolicyStatus ?? ScoreMissingPolicyStatuses.Ok,
                nameof(missingPolicyStatus))
            .ToLowerInvariant();
        ComputedAt = computedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ScoreRunId { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid ResponseSessionId { get; private set; }

    public string DimensionCode { get; private set; } = string.Empty;

    public decimal Value { get; private set; }

    public int NValid { get; private set; }

    public int NExpected { get; private set; }

    public string MissingPolicyStatus { get; private set; } = ScoreMissingPolicyStatuses.Ok;

    public DateTimeOffset ComputedAt { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
