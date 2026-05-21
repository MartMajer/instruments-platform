using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public interface IResponseCaptureStore
{
    Task<Result<RespondentCampaignResponse>> GetCampaignAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<LabAssignmentResponse>> CreateLabAssignmentAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<ResponseSessionResponse>> CreateSessionAsync(
        Guid tenantId,
        CreateResponseSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<SaveAnswersResponse>> SaveAnswersAsync(
        Guid tenantId,
        Guid sessionId,
        SaveAnswersRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubmitResponseSessionResponse>> SubmitSessionAsync(
        Guid tenantId,
        Guid sessionId,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<OpenLinkEntryResponse>> GetOpenLinkEntryAsync(
        string token,
        CancellationToken cancellationToken);

    Task<Result<EmailInvitationUnsubscribeResponse>> UnsubscribeEmailInvitationAsync(
        string token,
        CancellationToken cancellationToken);

    Task<Result<ResponseSessionResponse>> CreateOpenLinkSessionAsync(
        string token,
        CreateOpenLinkSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<OpenLinkEntryResponse>> GetIdentifiedEntryAsync(
        string token,
        CancellationToken cancellationToken);

    Task<Result<ResponseSessionResponse>> CreateIdentifiedEntrySessionAsync(
        string token,
        CreateOpenLinkSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<OpenLinkSessionDraftResponse>> GetOpenLinkSessionDraftAsync(
        string token,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<Result<SaveAnswersResponse>> SaveOpenLinkAnswersAsync(
        string token,
        Guid sessionId,
        SaveAnswersRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubmitResponseSessionResponse>> SubmitOpenLinkSessionAsync(
        string token,
        Guid sessionId,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<OpenLinkSessionDraftResponse>> GetPublicSessionDraftAsync(
        string handle,
        CancellationToken cancellationToken);

    Task<Result<SaveAnswersResponse>> SavePublicSessionAnswersAsync(
        string handle,
        SaveAnswersRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubmitResponseSessionResponse>> SubmitPublicSessionAsync(
        string handle,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken);
}
