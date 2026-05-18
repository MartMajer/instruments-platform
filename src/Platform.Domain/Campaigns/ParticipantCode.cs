namespace Platform.Domain.Campaigns;

public sealed class ParticipantCode
{
    public const int MinimumArgon2MemoryKiB = 65_536;
    public const int MinimumArgon2Iterations = 3;
    public const int MinimumArgon2Parallelism = 4;
    public const int MinimumArgon2OutputBytes = 32;

    private ParticipantCode()
    {
    }

    public ParticipantCode(
        Guid id,
        Guid tenantId,
        Guid campaignSeriesId,
        byte[] hash,
        int argon2MemoryKiB,
        int argon2Iterations,
        int argon2Parallelism,
        int argon2OutputBytes,
        DateTimeOffset firstSeenAt)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (hash.Length != MinimumArgon2OutputBytes)
        {
            throw new ArgumentException("Participant-code hash must be exactly 32 bytes.", nameof(hash));
        }

        EnsureMinimumParameters(
            argon2MemoryKiB,
            argon2Iterations,
            argon2Parallelism,
            argon2OutputBytes);

        Id = id;
        TenantId = tenantId;
        CampaignSeriesId = campaignSeriesId;
        Hash = [.. hash];
        Argon2MemoryKiB = argon2MemoryKiB;
        Argon2Iterations = argon2Iterations;
        Argon2Parallelism = argon2Parallelism;
        Argon2OutputBytes = argon2OutputBytes;
        FirstSeenAt = firstSeenAt;
        LastSeenAt = firstSeenAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CampaignSeriesId { get; private set; }

    public byte[] Hash { get; private set; } = [];

    public int Argon2MemoryKiB { get; private set; }

    public int Argon2Iterations { get; private set; }

    public int Argon2Parallelism { get; private set; }

    public int Argon2OutputBytes { get; private set; }

    public DateTimeOffset FirstSeenAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public void SeenAgain(DateTimeOffset seenAt)
    {
        if (seenAt < FirstSeenAt)
        {
            throw new ArgumentOutOfRangeException(nameof(seenAt), "Last seen cannot be before first seen.");
        }

        LastSeenAt = seenAt;
    }

    private static void EnsureMinimumParameters(
        int memoryKiB,
        int iterations,
        int parallelism,
        int outputBytes)
    {
        if (memoryKiB < MinimumArgon2MemoryKiB)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryKiB), "Argon2id memory must not be below ADR-0005 defaults.");
        }

        if (iterations < MinimumArgon2Iterations)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Argon2id iterations must not be below ADR-0005 defaults.");
        }

        if (parallelism < MinimumArgon2Parallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(parallelism), "Argon2id parallelism must not be below ADR-0005 defaults.");
        }

        if (outputBytes < MinimumArgon2OutputBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(outputBytes), "Argon2id output length must not be below ADR-0005 defaults.");
        }
    }
}
