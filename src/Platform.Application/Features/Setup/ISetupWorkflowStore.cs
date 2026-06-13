using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public interface ISetupWorkflowStore
{
    Task<Result<InstrumentSummaryResponse>> CreatePrivateInstrumentImportAsync(
        Guid tenantId,
        CreatePrivateInstrumentImportRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InstrumentSummaryResponse>> ListInstrumentsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionAsync(
        Guid tenantId,
        Guid? actorId,
        CreateTemplateVersionRequest request,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionDetailResponse>> GetTemplateVersionAsync(
        Guid tenantId,
        Guid templateVersionId,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionListResponse>> ListTemplateVersionsAsync(
        Guid tenantId,
        Guid anchorTemplateVersionId,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionDetailResponse>> PublishTemplateVersionAsync(
        Guid tenantId,
        Guid? actorId,
        Guid templateVersionId,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionDraftAsync(
        Guid tenantId,
        Guid? actorId,
        Guid sourceTemplateVersionId,
        CreateTemplateVersionDraftRequest request,
        CancellationToken cancellationToken);

    Task<Result<TemplateVersionDetailResponse>> UpdateTemplateVersionDraftContentAsync(
        Guid tenantId,
        Guid? actorId,
        Guid templateVersionId,
        UpdateTemplateVersionDraftContentRequest request,
        CancellationToken cancellationToken);

    Task<Result<SetupIdResponse>> CreateScoringRuleAsync(
        Guid tenantId,
        CreateScoringRuleRequest request,
        CancellationToken cancellationToken);

    Task<Result<RetireTemplateVersionDraftScoringResponse>> RetireTemplateVersionDraftScoringAsync(
        Guid tenantId,
        Guid? actorId,
        Guid templateVersionId,
        CancellationToken cancellationToken);

    Task<Result<SetupIdResponse>> CreateCampaignSeriesAsync(
        Guid tenantId,
        CreateCampaignSeriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<SelectCampaignSeriesSetupTemplateResponse>> SelectCampaignSeriesSetupTemplateAsync(
        Guid tenantId,
        Guid? actorId,
        Guid campaignSeriesId,
        SelectCampaignSeriesSetupTemplateRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignDraftResponse>> CreateCampaignAsync(
        Guid tenantId,
        Guid? actorId,
        CreateCampaignRequest request,
        CancellationToken cancellationToken);

    Task<Result<LaunchReadinessResponse>> GetLaunchReadinessAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignRespondentRuleListResponse>> ListCampaignRespondentRulesAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignRespondentRuleListResponse>> UpdateCampaignRespondentRulesAsync(
        Guid tenantId,
        Guid campaignId,
        UpdateCampaignRespondentRulesRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignAssignmentListResponse>> ListCampaignAssignmentsAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<LaunchCampaignResponse>> LaunchCampaignAsync(
        Guid tenantId,
        Guid? actorId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignOpenLinkResponse>> CreateCampaignOpenLinkAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignOpenLinkResponse>> ReplaceCampaignOpenLinkAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignIdentifiedEntryResponse>> CreateCampaignIdentifiedEntryAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignIdentifiedQueueAccessResponse>> CreateCampaignIdentifiedQueueAccessAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<Result<CampaignInvitationBatchResponse>> CreateCampaignInvitationBatchAsync(
        Guid tenantId,
        Guid campaignId,
        CreateCampaignInvitationBatchRequest request,
        CancellationToken cancellationToken);
}
