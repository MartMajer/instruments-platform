using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public interface IProductSurfaceWriteStore
{
    Task<Result<CampaignSeriesRenameResponse>> RenameCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        RenameCampaignSeriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesDuplicateResponse>> DuplicateCampaignSeriesAsync(
        Guid tenantId,
        Guid sourceCampaignSeriesId,
        Guid actorUserId,
        DuplicateCampaignSeriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesArchiveStateResponse>> ArchiveCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        ArchiveCampaignSeriesRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesArchiveStateResponse>> RestoreCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<CampaignCloseStateResponse>> CloseCampaignAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid campaignId,
        Guid actorUserId,
        CloseCampaignRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesScoreRemediationResponse>> RemediateCampaignSeriesScoresAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<TenantMemberMutationResponse>> CreateTenantMemberAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateTenantMemberRequest request,
        CancellationToken cancellationToken);

    Task<Result<TenantMemberMutationResponse>> ChangeTenantMemberRoleAsync(
        Guid tenantId,
        Guid targetUserId,
        Guid actorUserId,
        ChangeTenantMemberRoleRequest request,
        CancellationToken cancellationToken);

    Task<Result<TenantMemberMutationResponse>> SuspendTenantMemberAsync(
        Guid tenantId,
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<TenantMemberMutationResponse>> ReactivateTenantMemberAsync(
        Guid tenantId,
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<TenantMemberRemovalResponse>> RemoveTenantMemberAsync(
        Guid tenantId,
        Guid targetUserId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<TenantLanguageResponse>> UpdateTenantLanguageAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantLanguageRequest request,
        CancellationToken cancellationToken);

    Task<Result<TenantEmailTemplateSettingsResponse>> UpdateTenantEmailTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        string templateCode,
        string locale,
        UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken);

    Task<Result<ResetEmailTemplateResponse>> ResetTenantEmailTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        string templateCode,
        string locale,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryItemResponse>> CreateSubjectAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateSubjectRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryItemResponse>> UpdateSubjectAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryItemResponse>> DeactivateSubjectAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        DeactivateSubjectRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryItemResponse>> SetSubjectDirectoryStatusAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        SetSubjectDirectoryStatusRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryCsvImportResponse>> ImportSubjectDirectoryCsvAsync(
        Guid tenantId,
        Guid actorUserId,
        SubjectDirectoryCsvImportRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectGroupResponse>> CreateSubjectGroupAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateSubjectGroupRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectGroupMembershipResponse>> AddSubjectGroupMemberAsync(
        Guid tenantId,
        Guid groupId,
        Guid actorUserId,
        AddSubjectGroupMemberRequest request,
        CancellationToken cancellationToken);

    Task<Result<SubjectGroupMembershipRemovalResponse>> RemoveSubjectGroupMemberAsync(
        Guid tenantId,
        Guid groupId,
        Guid subjectId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<Result<SubjectDirectoryItemResponse>> SetSubjectManagerAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        SetSubjectManagerRequest request,
        CancellationToken cancellationToken);
}
