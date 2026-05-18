using Platform.SharedKernel;

namespace Platform.Application.Features.ParticipantCodes;

public interface IParticipantCodeStore
{
    Task<Result<ParticipantCodeResponse>> ResolveAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        string rawCode,
        CancellationToken cancellationToken);
}
