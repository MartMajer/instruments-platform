namespace Platform.Infrastructure.ParticipantCodes;

public sealed class ParticipantCodeHashingOptions
{
    public const string SectionName = "ParticipantCodes:Hashing";
    public const int DefaultMemoryKiB = 65_536;
    public const int DefaultIterations = 3;
    public const int DefaultParallelism = 4;
    public const int DefaultOutputBytes = 32;

    public int MemoryKiB { get; set; } = DefaultMemoryKiB;

    public int Iterations { get; set; } = DefaultIterations;

    public int Parallelism { get; set; } = DefaultParallelism;

    public int OutputBytes { get; set; } = DefaultOutputBytes;

    public bool AllowUnsafeTestParameters { get; set; }

    public void EnsureValid()
    {
        if (MemoryKiB <= 0 || Iterations <= 0 || Parallelism <= 0 || OutputBytes <= 0)
        {
            throw new InvalidOperationException("Argon2id participant-code parameters must be positive.");
        }

        if (AllowUnsafeTestParameters)
        {
            return;
        }

        if (MemoryKiB < DefaultMemoryKiB ||
            Iterations < DefaultIterations ||
            Parallelism < DefaultParallelism ||
            OutputBytes < DefaultOutputBytes)
        {
            throw new InvalidOperationException(
                "Argon2id participant-code parameters must not be below ADR-0005 defaults: " +
                $"m={DefaultMemoryKiB} KiB, t={DefaultIterations}, p={DefaultParallelism}, " +
                $"out_len={DefaultOutputBytes}.");
        }
    }
}
