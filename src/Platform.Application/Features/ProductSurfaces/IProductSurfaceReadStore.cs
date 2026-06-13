using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public interface IProductSurfaceReadStore
{
    Task<WorkspaceOverviewResponse> GetWorkspaceOverviewAsync(
        Guid tenantId,
        bool canManageSetup,
        bool canManageTeam,
        CancellationToken cancellationToken);

    Task<Result<TenantSettingsWorkspaceResponse>> GetTenantSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<ExportArtifactLibraryResponse> ListExportArtifactsAsync(
        Guid tenantId,
        bool canManageSetup,
        CancellationToken cancellationToken);

    Task<CampaignSeriesListResponse> ListCampaignSeriesAsync(
        Guid tenantId,
        CampaignSeriesPortfolioQuery query,
        CancellationToken cancellationToken);

    Task<TenantMemberRosterResponse> ListTenantMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<TenantRoleListResponse> ListTenantRolesAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<SubjectDirectoryResponse> ListSubjectsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<DirectoryConnectionStateResponse> GetMicrosoftGraphDirectoryConnectionStateAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<DirectoryImportRunHistoryResponse> ListMicrosoftGraphDirectoryImportRunsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<DirectoryImportRuleListResponse> ListMicrosoftGraphDirectoryImportRulesAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Result<MicrosoftGraphImportRuleExecutionContext>> GetMicrosoftGraphDirectoryImportRuleExecutionContextAsync(
        Guid tenantId,
        Guid ruleId,
        CancellationToken cancellationToken);

    Task<SubjectGroupListResponse> ListSubjectGroupsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Result<RespondentRulePreviewResponse>> PreviewRespondentRuleAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid campaignId,
        RespondentRulePreviewRequest request,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesHubResponse>> GetCampaignSeriesHubAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesSetupWorkspaceResponse>> GetCampaignSeriesSetupWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesOperationsWorkspaceResponse>> GetCampaignSeriesOperationsWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesReportsWorkspaceResponse>> GetCampaignSeriesReportsWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesReportsWidgetManifestResponse>> GetCampaignSeriesReportsWidgetManifestAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        bool canManageSetup,
        CancellationToken cancellationToken);

    Task<Result<CampaignSeriesWavesWorkspaceResponse>> GetCampaignSeriesWavesWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken);
}
