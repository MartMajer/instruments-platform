namespace Platform.Application.Features.ParticipantCodes;

public sealed record ResolveParticipantCodeRequest(string RawCode);

public sealed record ParticipantCodeResponse(Guid Id, Guid CampaignSeriesId);
