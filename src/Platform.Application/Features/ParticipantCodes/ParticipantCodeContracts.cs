namespace Platform.Application.Features.ParticipantCodes;

public sealed record ParticipantCodeHashingParameters(
    int MemoryKiB,
    int Iterations,
    int Parallelism,
    int OutputBytes);

public sealed record ParticipantCodeHashResult(
    byte[] Hash,
    ParticipantCodeHashingParameters Parameters);
