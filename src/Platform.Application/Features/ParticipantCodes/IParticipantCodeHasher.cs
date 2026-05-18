namespace Platform.Application.Features.ParticipantCodes;

public interface IParticipantCodeHasher
{
    Task<ParticipantCodeHashResult> HashAsync(
        string rawCode,
        byte[] seriesSalt,
        CancellationToken cancellationToken);
}
