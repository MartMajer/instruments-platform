using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Auth;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Integrations;
using Platform.Domain.Reports;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Campaigns.RespondentRules;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.ProductSurfaces;

public sealed class ProductSurfaceReadStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    RespondentRuleResolver? respondentRuleResolver = null) : IProductSurfaceReadStore
{
    private const string DirectoryImportStaleAttribute = "directory_import_stale";
    private const string DirectoryImportStaleAtAttribute = "directory_import_stale_at";
    private const string PreliminaryLiveDataFinality = "preliminary_live";
    private const string ClosedWaveDataFinality = "closed_wave";
    private const string NotReportableDataFinality = "not_reportable";
    private const int RespondentRulePreviewMaxRows = 200;
    private const int WorkspaceCommandCenterMaxItems = 8;
    private readonly RespondentRuleResolver _respondentRuleResolver =
        respondentRuleResolver ?? new RespondentRuleResolver(db);

    public async Task<WorkspaceOverviewResponse> GetWorkspaceOverviewAsync(
        Guid tenantId,
        bool canManageSetup,
        bool canManageTeam,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var allItems = await LoadSeriesListItemsAsync(
            take: null,
            new CampaignSeriesPortfolioQuery(),
            cancellationToken);
        var items = allItems.Take(5).ToArray();
        var totals = await LoadWorkspaceTotalsAsync(cancellationToken);
        var commandCenter = await LoadWorkspaceCommandCenterAsync(
            tenantId,
            totals,
            allItems,
            canManageSetup,
            canManageTeam,
            cancellationToken);
        var studyCollections = new WorkspaceStudyCollectionsResponse(
            allItems.Where(item => item.IsSample).Take(4).ToArray(),
            allItems.Where(item => !item.IsSample).Take(4).ToArray());
        var response = new WorkspaceOverviewResponse(tenantId, totals, items, commandCenter, studyCollections);

        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<Result<TenantSettingsWorkspaceResponse>> GetTenantSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var tenantSettings = await db.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId && tenant.DeletedAt == null)
            .Select(tenant => new TenantSettingsTenantRow(
                new TenantSettingsProfileResponse(
                    tenant.Id,
                    tenant.Slug,
                    tenant.Name,
                    tenant.Region,
                    tenant.DefaultLocale,
                    tenant.Status,
                    tenant.CreatedAt,
                    tenant.UpdatedAt),
                tenant.Name,
                tenant.ReportBrandingOrganizationLabel,
                tenant.ReportBrandingReportTitle,
                tenant.ReportBrandingAccentColorHex,
                tenant.ReportBrandingLayoutVariant))
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantSettings is null)
        {
            return Result.Failure<TenantSettingsWorkspaceResponse>(
                Error.NotFound("tenant.not_found", "Tenant was not found."));
        }

        var campaignSeriesCount = await db.CampaignSeries
            .AsNoTracking()
            .CountAsync(series => series.TenantId == tenantId, cancellationToken);
        var campaignTotals = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && campaign.CampaignSeriesId.HasValue)
            .GroupBy(_ => 1)
            .Select(group => new CampaignTotalsRow(
                group.Count(),
                group.Count(campaign => campaign.Status == CampaignStatuses.Live)))
            .SingleOrDefaultAsync(cancellationToken);
        var submittedResponseCount = await (
                from campaign in db.Campaigns.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on campaign.Id equals assignment.CampaignId
                join session in db.ResponseSessions.AsNoTracking()
                    on assignment.Id equals session.AssignmentId
                where campaign.TenantId == tenantId &&
                    assignment.TenantId == tenantId &&
                    session.TenantId == tenantId &&
                    campaign.CampaignSeriesId.HasValue &&
                    session.SubmittedAt.HasValue
                select session.Id)
            .CountAsync(cancellationToken);
        var subjectCount = await db.Subjects
            .AsNoTracking()
            .CountAsync(subject => subject.TenantId == tenantId && subject.DeletedAt == null, cancellationToken);
        var subjectGroupCount = await db.SubjectGroups
            .AsNoTracking()
            .CountAsync(group => group.TenantId == tenantId && group.DeletedAt == null, cancellationToken);
        var tenantMemberCount = await db.UserAccounts
            .AsNoTracking()
            .CountAsync(user => user.TenantId == tenantId && user.DeletedAt == null, cancellationToken);
        var tenantRoleCount = await db.Roles
            .AsNoTracking()
            .CountAsync(role => role.TenantId == tenantId, cancellationToken);
        var exportArtifactCount = await db.ExportArtifacts
            .AsNoTracking()
            .CountAsync(
                artifact => artifact.TenantId == tenantId && artifact.DeletedAt == null,
                cancellationToken);

        var customEmailTemplateRows = await db.EmailTemplates
            .AsNoTracking()
            .Where(template =>
                template.TenantId == tenantId &&
                template.Status == EmailTemplateStatuses.Active)
            .Select(template => new
            {
                template.TemplateCode,
                template.Locale,
                template.Subject,
                template.BodyText
            })
            .ToListAsync(cancellationToken);
        var customEmailTemplates = customEmailTemplateRows.ToDictionary(
            template => (template.TemplateCode, EmailTemplateLocales.Normalize(template.Locale)),
            template => new EmailTemplateContent(
                template.TemplateCode,
                EmailTemplateLocales.Normalize(template.Locale),
                template.Subject,
                template.BodyText));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new TenantSettingsWorkspaceResponse(
            tenantSettings.Profile,
            new TenantSettingsWorkspaceCountsResponse(
                campaignSeriesCount,
                campaignTotals?.CampaignCount ?? 0,
                campaignTotals?.LiveCampaignCount ?? 0,
                submittedResponseCount,
                subjectCount,
                subjectGroupCount,
                tenantMemberCount,
                tenantRoleCount,
                exportArtifactCount),
            CreateTenantSettingsReportBranding(tenantSettings),
            TenantEmailTemplateSettingsFactory.Create(customEmailTemplates),
            CreateTenantSettingsManagementLinks()));
    }

    public async Task<ExportArtifactLibraryResponse> ListExportArtifactsAsync(
        Guid tenantId,
        bool canManageSetup,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var artifactQuery = db.ExportArtifacts
            .AsNoTracking()
            .Where(artifact =>
                artifact.TenantId == tenantId &&
                artifact.DeletedAt == null &&
                (artifact.ArtifactType == ExportArtifactTypes.ReportProofCsvCodebook ||
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook ||
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook ||
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportHtml ||
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf));

        var summary = await artifactQuery
            .GroupBy(_ => 1)
            .Select(group => new ExportArtifactLibrarySummaryResponse(
                group.Count(),
                group.Count(artifact =>
                    artifact.Status == ExportArtifactStatuses.Succeeded &&
                    artifact.ChecksumSha256 != null &&
                    (artifact.StorageKind == ExportArtifactStorageKinds.InlineText ||
                        (artifact.StorageKind == ExportArtifactStorageKinds.ExternalObject && artifact.StorageKey != null))),
                group.Count(artifact => artifact.Status == ExportArtifactStatuses.Failed),
                group.Count(artifact =>
                    artifact.Status == ExportArtifactStatuses.Queued ||
                    artifact.Status == ExportArtifactStatuses.Rendering),
                0))
            .SingleOrDefaultAsync(cancellationToken) ??
            new ExportArtifactLibrarySummaryResponse(0, 0, 0, 0);
        var retryableCount = canManageSetup
            ? await artifactQuery.CountAsync(
                artifact =>
                    artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                    artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
                    artifact.Format == ExportArtifactFormats.Pdf &&
                    artifact.Status == ExportArtifactStatuses.Failed &&
                    artifact.CampaignSeriesId != null,
                cancellationToken)
            : 0;
        summary = summary with { RetryableCount = retryableCount };

        var artifactRows = await artifactQuery
            .OrderByDescending(artifact => artifact.CreatedAt)
            .ThenBy(artifact => artifact.Id)
            .Select(artifact => new ExportArtifactLibraryRow(
                artifact.Id,
                artifact.TargetKind,
                artifact.CampaignId,
                artifact.CampaignSeriesId,
                artifact.ArtifactType,
                artifact.Status,
                artifact.Format,
                artifact.FileName,
                artifact.RowCount,
                artifact.ByteSize,
                artifact.ChecksumSha256,
                artifact.CreatedAt,
                artifact.CompletedAt,
                artifact.StartedAt,
                artifact.FailedAt,
                artifact.ExpiresAt,
                artifact.DeletedAt,
                artifact.FailureReasonCode,
                artifact.Status == ExportArtifactStatuses.Succeeded &&
                    artifact.ChecksumSha256 != null &&
                    (artifact.StorageKind == ExportArtifactStorageKinds.InlineText ||
                        (artifact.StorageKind == ExportArtifactStorageKinds.ExternalObject && artifact.StorageKey != null))))
            .Take(25)
            .ToListAsync(cancellationToken);

        var campaignIds = artifactRows
            .Where(artifact => artifact.CampaignId.HasValue)
            .Select(artifact => artifact.CampaignId!.Value)
            .Distinct()
            .ToArray();
        var campaignRows = campaignIds.Length == 0
            ? new Dictionary<Guid, ExportArtifactCampaignTargetRow>()
            : await db.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.TenantId == tenantId && campaignIds.Contains(campaign.Id))
                .Select(campaign => new ExportArtifactCampaignTargetRow(
                    campaign.Id,
                    campaign.Name,
                    campaign.Status,
                    campaign.ClosedAt))
                .ToDictionaryAsync(campaign => campaign.Id, cancellationToken);

        var campaignSeriesIds = artifactRows
            .Where(artifact => artifact.CampaignSeriesId.HasValue)
            .Select(artifact => artifact.CampaignSeriesId!.Value)
            .Distinct()
            .ToArray();
        var seriesRows = campaignSeriesIds.Length == 0
            ? new Dictionary<Guid, ExportArtifactSeriesTargetRow>()
            : await db.CampaignSeries
                .AsNoTracking()
                .Where(series => series.TenantId == tenantId && campaignSeriesIds.Contains(series.Id))
                .Select(series => new ExportArtifactSeriesTargetRow(
                    series.Id,
                    series.Name))
                .ToDictionaryAsync(series => series.Id, cancellationToken);

        var artifacts = artifactRows
            .Select(artifact =>
            {
                var targetIsCampaign = artifact.TargetKind == ExportArtifactTargetKinds.Campaign;
                var campaign = targetIsCampaign && artifact.CampaignId.HasValue
                    ? campaignRows.GetValueOrDefault(artifact.CampaignId.Value)
                    : null;
                var series = artifact.CampaignSeriesId.HasValue
                    ? seriesRows.GetValueOrDefault(artifact.CampaignSeriesId.Value)
                    : null;
                var targetId = targetIsCampaign
                    ? artifact.CampaignId!.Value
                    : artifact.CampaignSeriesId!.Value;
                var targetLabel = targetIsCampaign
                    ? campaign?.Name ?? "Unknown campaign"
                    : series?.Name ?? "Unknown campaign series";
                var dataFinality = targetIsCampaign && campaign is not null
                    ? DetermineReportDataFinality(campaign.Status, "proof_only")
                    : null;
                var response = new CampaignSeriesReportsExportArtifactResponse(
                    artifact.Id,
                    artifact.TargetKind,
                    targetId,
                    targetLabel,
                    artifact.CampaignId,
                    targetIsCampaign ? targetLabel : null,
                    artifact.ArtifactType,
                    artifact.Status,
                    artifact.Format,
                    artifact.FileName,
                    artifact.RowCount,
                    artifact.ByteSize,
                    artifact.ChecksumSha256,
                    artifact.CreatedAt,
                    artifact.CompletedAt,
                    artifact.StartedAt,
                    artifact.FailedAt,
                    artifact.ExpiresAt,
                    artifact.DeletedAt,
                    artifact.FailureReasonCode,
                    artifact.CanDownload,
                    campaign?.Status,
                    campaign?.ClosedAt,
                    dataFinality);

                return response with
                {
                    CanDownload = CanAdvertiseExportArtifactDownload(response, canManageSetup),
                    CanRetry = CanAdvertiseExportArtifactRetry(response, canManageSetup)
                };
            })
            .ToArray();

        await transaction.CommitAsync(cancellationToken);

        return new ExportArtifactLibraryResponse(
            tenantId,
            summary,
            artifacts);
    }

    public async Task<TenantMemberRosterResponse> ListTenantMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var assignmentRows = await (
                from user in db.UserAccounts.AsNoTracking()
                join assignment in db.RoleAssignments.AsNoTracking()
                    on user.Id equals assignment.UserId
                join role in db.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                where user.TenantId == tenantId &&
                    user.DeletedAt == null &&
                    assignment.TenantId == tenantId &&
                    assignment.ScopeType == RoleAssignmentScopes.Tenant
                orderby user.Email, role.Code
                select new TenantMemberAssignmentRow(
                    user.Id,
                    user.Email,
                    user.Locale,
                    user.CreatedAt,
                    user.LastLoginAt,
                    role.Id,
                    role.Code,
                    role.Name,
                    assignment.ScopeType,
                    assignment.ScopeId,
                    assignment.GrantedAt))
            .ToListAsync(cancellationToken);
        var userIds = assignmentRows
            .Select(row => row.UserId)
            .Distinct()
            .ToArray();
        var activeIdentityUserIds = userIds.Length == 0
            ? []
            : await db.ExternalAuthIdentities
                .AsNoTracking()
                .Where(identity =>
                    identity.TenantId == tenantId &&
                    userIds.Contains(identity.UserId) &&
                    identity.DisabledAt == null)
                .Select(identity => identity.UserId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
        var activeIdentityUsers = activeIdentityUserIds.ToHashSet();
        var roleIds = assignmentRows
            .Select(row => row.RoleId)
            .Distinct()
            .ToArray();
        var permissionRows = roleIds.Length == 0
            ? []
            : await (
                    from rolePermission in db.RolePermissions.AsNoTracking()
                    join permission in db.Permissions.AsNoTracking()
                        on rolePermission.PermissionId equals permission.Id
                    where roleIds.Contains(rolePermission.RoleId)
                    select new TenantMemberRolePermissionRow(
                        rolePermission.RoleId,
                        permission.Code))
                .ToListAsync(cancellationToken);
        var permissionsByRole = permissionRows
            .GroupBy(row => row.RoleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.PermissionCode)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray());
        var members = assignmentRows
            .GroupBy(row => new
            {
                row.UserId,
                row.Email,
                row.Locale,
                row.CreatedAt,
                row.LastLoginAt
            })
            .Select(group =>
            {
                var roles = group
                    .GroupBy(row => new
                    {
                        row.RoleId,
                        row.RoleCode,
                        row.RoleName,
                        row.ScopeType,
                        row.ScopeId,
                        row.GrantedAt
                    })
                    .OrderBy(role => role.Key.RoleCode, StringComparer.Ordinal)
                    .Select(role => new TenantMemberRoleResponse(
                        role.Key.RoleId,
                        role.Key.RoleCode,
                        role.Key.RoleName,
                        role.Key.ScopeType,
                        role.Key.ScopeId,
                        role.Key.GrantedAt))
                    .ToArray();
                var permissions = group
                    .SelectMany(row => permissionsByRole.GetValueOrDefault(row.RoleId) ?? [])
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray();

                return new TenantMemberResponse(
                    group.Key.UserId,
                    group.Key.Email,
                    group.Key.Locale,
                    group.Key.CreatedAt,
                    group.Key.LastLoginAt,
                    roles,
                    permissions,
                    activeIdentityUsers.Contains(group.Key.UserId)
                        ? TenantMemberIdentityStatuses.Active
                        : TenantMemberIdentityStatuses.PendingProviderLink);
            })
            .OrderBy(member => member.Email, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var response = new TenantMemberRosterResponse(tenantId, members);

        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<TenantRoleListResponse> ListTenantRolesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId)
            .OrderBy(role => role.Code)
            .Select(role => new
            {
                role.Id,
                role.Code,
                role.Name
            })
            .ToListAsync(cancellationToken);
        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissionRows = roleIds.Length == 0
            ? []
            : await (
                    from rolePermission in db.RolePermissions.AsNoTracking()
                    join permission in db.Permissions.AsNoTracking()
                        on rolePermission.PermissionId equals permission.Id
                    where roleIds.Contains(rolePermission.RoleId)
                    select new TenantMemberRolePermissionRow(
                        rolePermission.RoleId,
                        permission.Code))
                .ToListAsync(cancellationToken);
        var permissionsByRole = permissionRows
            .GroupBy(row => row.RoleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.PermissionCode)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray());
        var response = new TenantRoleListResponse(roles
            .Select(role => new TenantRoleResponse(
                role.Id,
                role.Code,
                role.Name,
                permissionsByRole.GetValueOrDefault(role.Id) ?? []))
            .ToArray());

        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<SubjectDirectoryResponse> ListSubjectsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(subject => subject.TenantId == tenantId && subject.DeletedAt == null)
            .OrderBy(subject => subject.DisplayName ?? subject.Email ?? subject.ExternalId ?? string.Empty)
            .ThenBy(subject => subject.Id)
            .Select(subject => new SubjectDirectorySubjectRow(
                subject.Id,
                subject.DisplayName,
                subject.Email,
                subject.ExternalId,
                subject.Locale,
                subject.Attributes))
            .ToListAsync(cancellationToken);
        var subjectIds = subjects.Select(subject => subject.Id).ToArray();

        var memberships = subjectIds.Length == 0
            ? []
            : await (
                    from membership in db.SubjectMemberships.AsNoTracking()
                    join subjectGroup in db.SubjectGroups.AsNoTracking()
                        on membership.GroupId equals subjectGroup.Id
                    join subject in db.Subjects.AsNoTracking()
                        on membership.SubjectId equals subject.Id
                    where subject.TenantId == tenantId &&
                        subject.DeletedAt == null &&
                        subjectGroup.TenantId == tenantId &&
                        subjectGroup.DeletedAt == null &&
                        subjectIds.Contains(subject.Id)
                    orderby subjectGroup.Name
                    select new SubjectDirectoryMembershipRow(
                        subject.Id,
                        subjectGroup.Id,
                        subjectGroup.Type,
                        subjectGroup.Name,
                        membership.RoleInGroup,
                        membership.ValidFrom,
                        membership.ValidTo))
                .ToListAsync(cancellationToken);
        var membershipsBySubject = memberships
            .GroupBy(membership => membership.SubjectId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(membership => new SubjectGroupMembershipResponse(
                        membership.GroupId,
                        membership.GroupType,
                        membership.GroupName,
                        membership.RoleInGroup,
                        membership.ValidFrom,
                        membership.ValidTo))
                    .ToArray());

        var activeManagerRows = subjectIds.Length == 0
            ? []
            : await (
                    from relationship in db.SubjectRelationships.AsNoTracking()
                    join manager in db.Subjects.AsNoTracking()
                        on relationship.SubjectId equals manager.Id
                    where relationship.TenantId == tenantId &&
                        relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                        relationship.ValidTo == null &&
                        subjectIds.Contains(relationship.RelatedSubjectId) &&
                        manager.TenantId == tenantId &&
                        manager.DeletedAt == null
                    select new SubjectManagerRow(
                        relationship.RelatedSubjectId,
                        manager.Id,
                        manager.DisplayName))
                .ToListAsync(cancellationToken);
        var managerBySubject = activeManagerRows
            .GroupBy(row => row.SubjectId)
            .ToDictionary(group => group.Key, group => group.First());
        var directReportCounts = subjectIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await db.SubjectRelationships
                .AsNoTracking()
                .Where(relationship =>
                    relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.ValidTo == null &&
                    subjectIds.Contains(relationship.SubjectId))
                .GroupBy(relationship => relationship.SubjectId)
                .Select(group => new
                {
                    SubjectId = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(row => row.SubjectId, row => row.Count, cancellationToken);
        var groupCount = await db.SubjectGroups
            .AsNoTracking()
            .CountAsync(group => group.TenantId == tenantId && group.DeletedAt == null, cancellationToken);
        var managerRelationshipCount = await db.SubjectRelationships
            .AsNoTracking()
            .CountAsync(
                relationship =>
                    relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.ValidTo == null,
                cancellationToken);
        var items = subjects
            .Select(subject =>
            {
                managerBySubject.TryGetValue(subject.Id, out var manager);
                var staleState = ReadDirectoryImportStaleState(subject.Attributes);

                return new SubjectDirectoryItemResponse(
                    subject.Id,
                    subject.DisplayName,
                    subject.Email,
                    subject.ExternalId,
                    subject.Locale,
                    subject.Attributes,
                    manager?.ManagerSubjectId,
                    manager?.ManagerDisplayName,
                    directReportCounts.GetValueOrDefault(subject.Id),
                    membershipsBySubject.GetValueOrDefault(subject.Id) ?? [],
                    staleState.IsStale,
                    staleState.StaleAt);
            })
            .ToArray();

        await transaction.CommitAsync(cancellationToken);

        return new SubjectDirectoryResponse(
            tenantId,
            new SubjectDirectorySummaryResponse(
                items.Length,
                groupCount,
                managerRelationshipCount),
            items);
    }

    public async Task<DirectoryConnectionStateResponse> GetMicrosoftGraphDirectoryConnectionStateAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var connection = await db.DirectoryConnections
            .AsNoTracking()
            .Where(row =>
                row.TenantId == tenantId &&
                row.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                row.DeletedAt == null)
            .OrderByDescending(row => row.UpdatedAt)
            .Select(row => new DirectoryConnectionStateRow(
                row.Provider,
                row.Status,
                row.DisplayName,
                row.PrimaryDomain,
                row.GrantedScopes,
                row.LastConsentAt,
                row.LastSuccessfulImportAt,
                row.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (connection is null)
        {
            return new DirectoryConnectionStateResponse(
                tenantId,
                DirectoryConnectionProviders.MicrosoftGraph,
                DirectoryConnectionStatuses.Disconnected,
                "Microsoft Graph",
                null,
                [],
                null,
                null,
                null,
                Connected: false);
        }

        return new DirectoryConnectionStateResponse(
            tenantId,
            connection.Provider,
            connection.Status,
            connection.DisplayName,
            connection.PrimaryDomain,
            ReadStringArray(connection.GrantedScopes),
            connection.LastConsentAt,
            connection.LastSuccessfulImportAt,
            connection.UpdatedAt,
            Connected: connection.Status == DirectoryConnectionStatuses.Active);
    }

    public async Task<DirectoryImportRunHistoryResponse> ListMicrosoftGraphDirectoryImportRunsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var rows = await (
                from run in db.DirectoryImportRuns.AsNoTracking()
                join connection in db.DirectoryConnections.AsNoTracking()
                    on new { Id = run.DirectoryConnectionId, run.TenantId }
                    equals new { connection.Id, connection.TenantId }
                where run.TenantId == tenantId &&
                    connection.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    connection.DeletedAt == null
                orderby run.CreatedAt descending
                select new DirectoryImportRunRow(
                    run.Id,
                    run.DirectoryConnectionId,
                    run.DirectoryImportRuleId,
                    run.PreviewRunId,
                    connection.Provider,
                    run.Mode,
                    run.Status,
                    run.Counts,
                    run.WarningCategories,
                    run.CreatedAt,
                    run.StartedAt,
                    run.CompletedAt))
            .Take(8)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DirectoryImportRunHistoryResponse(
            tenantId,
            rows.Select(row =>
            {
                var warningCategories = ReadStringArray(row.WarningCategories);
                return new DirectoryImportRunListItemResponse(
                    row.Id,
                    row.DirectoryConnectionId,
                    row.DirectoryImportRuleId,
                    row.PreviewRunId,
                    row.Provider,
                    row.Mode,
                    row.Status,
                    ReadJsonInt(row.Counts, "row_count"),
                    ReadJsonInt(row.Counts, "imported_row_count"),
                    ReadJsonInt(row.Counts, "failed_row_count"),
                    warningCategories.Count,
                    warningCategories,
                    row.CreatedAt,
                    row.StartedAt,
                    row.CompletedAt);
            }).ToArray());
    }

    public async Task<DirectoryImportRuleListResponse> ListMicrosoftGraphDirectoryImportRulesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var rows = await (
                from rule in db.DirectoryImportRules.AsNoTracking()
                join connection in db.DirectoryConnections.AsNoTracking()
                    on new { Id = rule.DirectoryConnectionId, rule.TenantId }
                    equals new { connection.Id, connection.TenantId }
                where rule.TenantId == tenantId &&
                    rule.DeletedAt == null &&
                    connection.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    connection.DeletedAt == null
                orderby rule.UpdatedAt descending
                select new DirectoryImportRuleRow(
                    rule.Id,
                    rule.DirectoryConnectionId,
                    rule.Name,
                    rule.Status,
                    rule.StalePolicy,
                    rule.RetainedFields,
                    rule.CreatedAt,
                    rule.UpdatedAt))
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DirectoryImportRuleListResponse(
            tenantId,
            rows.Select(row => new DirectoryImportRuleResponse(
                row.Id,
                row.DirectoryConnectionId,
                row.Name,
                row.Status,
                row.StalePolicy,
                ReadStringArray(row.RetainedFields),
                row.CreatedAt,
                row.UpdatedAt)).ToArray());
    }

    public async Task<Result<MicrosoftGraphImportRuleExecutionContext>> GetMicrosoftGraphDirectoryImportRuleExecutionContextAsync(
        Guid tenantId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var row = await (
                from rule in db.DirectoryImportRules.AsNoTracking()
                join connection in db.DirectoryConnections.AsNoTracking()
                    on new { Id = rule.DirectoryConnectionId, rule.TenantId }
                    equals new { connection.Id, connection.TenantId }
                where rule.Id == ruleId &&
                    rule.TenantId == tenantId &&
                    rule.Status == DirectoryImportRuleStatuses.Active &&
                    rule.DeletedAt == null &&
                    connection.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    connection.DeletedAt == null
                select new
                {
                    RuleId = rule.Id,
                    rule.DirectoryConnectionId,
                    rule.StalePolicy,
                    connection.ExternalTenantId
                })
            .SingleOrDefaultAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (row is null)
        {
            return Result.Failure<MicrosoftGraphImportRuleExecutionContext>(
                Error.NotFound("directory_import_rule.not_found", "Graph import rule was not found."));
        }

        if (string.IsNullOrWhiteSpace(row.ExternalTenantId))
        {
            return Result.Failure<MicrosoftGraphImportRuleExecutionContext>(
                Error.Conflict("directory_connection.not_connected", "Microsoft Graph connection is not active."));
        }

        return Result.Success(new MicrosoftGraphImportRuleExecutionContext(
            row.RuleId,
            row.DirectoryConnectionId,
            row.StalePolicy,
            row.ExternalTenantId));
    }

    public async Task<SubjectGroupListResponse> ListSubjectGroupsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var groups = await db.SubjectGroups
            .AsNoTracking()
            .Where(group => group.TenantId == tenantId && group.DeletedAt == null)
            .OrderBy(group => group.Name)
            .ThenBy(group => group.Id)
            .Select(group => new SubjectGroupRow(
                group.Id,
                group.Type,
                group.Name,
                group.ParentGroupId,
                group.Attributes))
            .ToListAsync(cancellationToken);
        var groupIds = groups.Select(group => group.Id).ToArray();
        var memberCounts = groupIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await (
                    from membership in db.SubjectMemberships.AsNoTracking()
                    join subject in db.Subjects.AsNoTracking()
                        on membership.SubjectId equals subject.Id
                    where groupIds.Contains(membership.GroupId) &&
                        subject.TenantId == tenantId &&
                        subject.DeletedAt == null
                    group membership by membership.GroupId into membershipGroup
                    select new
                    {
                        GroupId = membershipGroup.Key,
                        Count = membershipGroup.Count()
                    })
                .ToDictionaryAsync(row => row.GroupId, row => row.Count, cancellationToken);
        var response = new SubjectGroupListResponse(
            tenantId,
            groups
                .Select(group => new SubjectGroupResponse(
                    group.Id,
                    group.Type,
                    group.Name,
                    group.ParentGroupId,
                    group.Attributes,
                    memberCounts.GetValueOrDefault(group.Id)))
                .ToArray());

        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<Result<RespondentRulePreviewResponse>> PreviewRespondentRuleAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid campaignId,
        RespondentRulePreviewRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var resolution = await _respondentRuleResolver.ResolveAsync(
            new RespondentRuleResolutionRequest(
                tenantId,
                campaignId,
                campaignSeriesId,
                request.Rule,
                request.TargetSubjectId,
                request.GroupId,
                request.MaxRows),
            cancellationToken);
        if (resolution.IsFailure)
        {
            return Result.Failure<RespondentRulePreviewResponse>(resolution.Error);
        }

        var resolved = resolution.Value;
        var maxRows = Math.Clamp(request.MaxRows, 1, RespondentRulePreviewMaxRows);
        var truncated = resolved.Candidates.Count > maxRows;
        var rows = resolved.Candidates
            .Take(maxRows)
            .Select((candidate, index) => new RespondentRulePreviewRowResponse(
                index + 1,
                resolved.RuleKind,
                resolved.Role,
                candidate.Target is null ? null : ToPreviewSubject(candidate.Target),
                ToPreviewSubject(candidate.Respondent)))
            .ToArray();
        var warnings = resolved.Issues
            .Where(issue => string.Equals(issue.Severity, "warning", StringComparison.OrdinalIgnoreCase))
            .Select(issue => new RespondentRulePreviewWarningResponse(
                issue.Code,
                issue.Message,
                issue.SubjectId,
                issue.GroupId))
            .ToArray();
        var summary = new RespondentRulePreviewSummaryResponse(
            TargetCount: CountDistinctResolvedSubjects(resolved.Candidates.Select(candidate => candidate.Target)),
            RespondentCount: CountDistinctResolvedSubjects(
                resolved.Candidates.Select(candidate => (RespondentRuleSubject?)candidate.Respondent)),
            AssignmentPairCount: resolved.Candidates.Count,
            SkippedCount: 0,
            WarningCount: warnings.Length,
            Truncated: truncated);
        var response = new RespondentRulePreviewResponse(
            campaignSeriesId,
            campaignId,
            resolved.RuleKind,
            resolved.Role,
            summary,
            rows,
            warnings);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<CampaignSeriesListResponse> ListCampaignSeriesAsync(
        Guid tenantId,
        CampaignSeriesPortfolioQuery query,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var items = await LoadSeriesListItemsAsync(take: null, query, cancellationToken);
        var response = new CampaignSeriesListResponse(items);

        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task<Result<CampaignSeriesHubResponse>> GetCampaignSeriesHubAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesHubResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId)
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new CampaignRow(
                entity.Id,
                entity.CampaignSeriesId,
                entity.TemplateVersionId,
                entity.Name,
                entity.Status,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.StartAt,
                entity.EndAt,
                entity.UpdatedAt,
                entity.ClosedAt,
                entity.ClosedByUserId,
                entity.CloseReason))
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(entity => entity.Id).ToArray();
        var submittedCounts = await LoadSubmittedResponseCountsByCampaignAsync(campaignIds, cancellationToken);
        var launchAggregates = await LoadLaunchAggregatesByCampaignAsync(campaignIds, cancellationToken);
        var scoreCounts = await LoadScoreCountsByCampaignAsync(campaignIds, cancellationToken);
        var exportArtifactCounts = await LoadExportArtifactCountsByCampaignAsync(campaignIds, cancellationToken);
        var trajectoryRows = await LoadWaveSubmittedTrajectoriesAsync(campaignIds, cancellationToken);
        var governance = new CampaignSeriesGovernanceSummaryResponse(
            DetermineConfiguredStatus(launchAggregates.Values.Any(aggregate => aggregate.HasConsentDocument)),
            DetermineConfiguredStatus(launchAggregates.Values.Any(aggregate => aggregate.HasRetentionPolicy)),
            DetermineConfiguredStatus(launchAggregates.Values.Any(aggregate => aggregate.HasDisclosurePolicy)),
            DetermineConfiguredStatus(launchAggregates.Count > 0));

        var response = new CampaignSeriesHubResponse(
            series.Id,
            series.Name,
            series.CreatedAt,
            series.UpdatedAt,
            new CampaignSeriesHubTotalsResponse(
                campaigns.Count,
                campaigns.Count(campaign => campaign.Status == CampaignStatuses.Live),
                submittedCounts.Values.Sum(),
                scoreCounts.Values.Sum(),
                exportArtifactCounts.Values.Sum()),
            governance,
            CreateCampaignSeriesLifecycle(
                campaigns,
                governance,
                submittedCounts,
                exportArtifactCounts,
                trajectoryRows),
            campaigns
                .Select(campaign => new CampaignSeriesHubCampaignResponse(
                    campaign.Id,
                    campaign.Name,
                    campaign.Status,
                    campaign.ResponseIdentityMode,
                    campaign.DefaultLocale,
                    campaign.StartAt,
                    campaign.EndAt,
                    launchAggregates.GetValueOrDefault(campaign.Id)?.LatestLaunchAt,
                    submittedCounts.GetValueOrDefault(campaign.Id),
                    scoreCounts.GetValueOrDefault(campaign.Id),
                    exportArtifactCounts.GetValueOrDefault(campaign.Id)))
                .ToArray(),
            series.ArchivedAt.HasValue,
            series.ArchivedAt,
            series.ArchivedByUserId,
            series.ArchiveReason,
            series.StudyKind,
            IsSampleSeries(series),
            series.SampleScenario,
            GetReadOnlyReason(series),
            CreateStudyBriefResponse(series));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CampaignSeriesSetupWorkspaceResponse>> GetCampaignSeriesSetupWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesSetupWorkspaceResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId)
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new CampaignRow(
                entity.Id,
                entity.CampaignSeriesId,
                entity.TemplateVersionId,
                entity.Name,
                entity.Status,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.StartAt,
                entity.EndAt,
                entity.UpdatedAt,
                entity.ClosedAt,
                entity.ClosedByUserId,
                entity.CloseReason))
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(entity => entity.Id).ToArray();
        var launchAggregates = await LoadLaunchAggregatesByCampaignAsync(campaignIds, cancellationToken);
        var selectedSetupCampaign = campaigns
            .Where(IsEditableSetupCampaign)
            .OrderByDescending(campaign => campaign.Status == CampaignStatuses.Draft)
            .ThenByDescending(campaign => campaign.UpdatedAt)
            .FirstOrDefault();
        var selectedCampaign = selectedSetupCampaign ?? campaigns
            .OrderByDescending(campaign => campaign.UpdatedAt)
            .FirstOrDefault();
        var selectedSetupTemplateVersionId =
            selectedSetupCampaign?.TemplateVersionId ??
            series.SetupTemplateVersionId ??
            selectedCampaign?.TemplateVersionId;
        var template = selectedSetupTemplateVersionId.HasValue
            ? await LoadSetupTemplateAsync(selectedSetupTemplateVersionId.Value, cancellationToken)
            : null;
        var scoring = selectedSetupCampaign is not null
            ? await LoadSetupScoringAsync(
                selectedSetupCampaign.Id,
                selectedSetupCampaign.TemplateVersionId,
                cancellationToken)
            : series.SetupTemplateVersionId.HasValue
                ? await LoadSetupTemplateVersionScoringAsync(
                    series.SetupTemplateVersionId.Value,
                    cancellationToken)
                : selectedCampaign is not null
                    ? await LoadSetupScoringAsync(
                        selectedCampaign.Id,
                        selectedCampaign.TemplateVersionId,
                        cancellationToken)
                    : null;
        var consent = await LoadLatestConsentPolicyAsync(campaignSeriesId, cancellationToken);
        var retention = await LoadLatestRetentionPolicyAsync(campaignSeriesId, cancellationToken);
        var disclosure = await LoadLatestDisclosurePolicyAsync(campaignSeriesId, cancellationToken);
        var policies = new CampaignSeriesSetupPolicySummaryResponse(
            CreateConsentPolicyResponse(consent),
            CreateRetentionPolicyResponse(retention),
            CreateDisclosurePolicyResponse(disclosure));
        var missingPrerequisites = CreateMissingPrerequisites(
            selectedSetupCampaign,
            template,
            scoring,
            policies);

        var response = new CampaignSeriesSetupWorkspaceResponse(
            new CampaignSeriesSetupSeriesResponse(
                series.Id,
                series.Name,
                series.CreatedAt,
                series.UpdatedAt,
                series.StudyKind,
                IsSampleSeries(series),
                series.SampleScenario,
                GetReadOnlyReason(series),
                CreateStudyBriefResponse(series)),
            new CampaignSeriesSetupSummaryResponse(
                campaigns.Count,
                campaigns.Count(campaign => campaign.Status == CampaignStatuses.Live),
                missingPrerequisites.Count),
            selectedCampaign is null
                ? null
                : CreateSetupCampaignResponse(
                    selectedCampaign,
                    launchAggregates.GetValueOrDefault(selectedCampaign.Id)?.LatestLaunchAt),
            template is null
                ? null
                : new CampaignSeriesSetupTemplateResponse(
                    template.TemplateId,
                    template.TemplateVersionId,
                    template.TemplateName,
                    template.Semver,
                    template.Status,
                    template.DefaultLocale,
                    template.InstrumentId,
                    template.QuestionCount),
            scoring is null
                ? null
                : new CampaignSeriesSetupScoringResponse(
                    scoring.Id,
                    scoring.TemplateVersionId,
                    scoring.RuleKey,
                    scoring.RuleVersion,
                    scoring.Status,
                    scoring.Source),
            policies,
            CreateSetupReadiness(selectedSetupCampaign, missingPrerequisites.Count),
            missingPrerequisites,
            campaigns
                .Select(campaign => CreateSetupCampaignResponse(
                    campaign,
                    launchAggregates.GetValueOrDefault(campaign.Id)?.LatestLaunchAt))
                .ToArray());

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CampaignSeriesOperationsWorkspaceResponse>> GetCampaignSeriesOperationsWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesOperationsWorkspaceResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId)
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new CampaignRow(
                entity.Id,
                entity.CampaignSeriesId,
                entity.TemplateVersionId,
                entity.Name,
                entity.Status,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.StartAt,
                entity.EndAt,
                entity.UpdatedAt,
                entity.ClosedAt,
                entity.ClosedByUserId,
                entity.CloseReason))
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(entity => entity.Id).ToArray();
        var launchDetails = await LoadLatestLaunchDetailsByCampaignAsync(campaignIds, cancellationToken);
        var collectionAggregates = await LoadCollectionAggregatesByCampaignAsync(campaignIds, cancellationToken);
        var scoringRuleIdsByTemplate = await LoadCurrentScoringRuleIdsByTemplateVersionAsync(
            campaigns
                .Select(campaign => campaign.TemplateVersionId)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var scoreCoverageAggregates = await LoadScoreCoverageAggregatesByCampaignAsync(campaignIds, cancellationToken);
        var scoreCoverageInputs = campaigns.ToDictionary(
            campaign => campaign.Id,
            campaign => CreateScoreCoverageInput(
                campaign,
                launchDetails.GetValueOrDefault(campaign.Id),
                scoringRuleIdsByTemplate,
                collectionAggregates.GetValueOrDefault(campaign.Id)?.SubmittedResponseCount ?? 0,
                scoreCoverageAggregates.GetValueOrDefault(campaign.Id)));
        var disclosurePolicies = await LoadReportDisclosurePoliciesAsync(
            launchDetails.Values
                .Select(detail => detail.DisclosurePolicyId)
                .OfType<Guid>()
                .Distinct()
                .ToArray(),
            cancellationToken);
        var openLinkCounts = await LoadOpenLinkAssignmentCountsByCampaignAsync(tenantId, campaignIds, cancellationToken);
        var targetAwareAssignmentCounts = await LoadTargetAwareAssignmentCountsByCampaignAsync(
            tenantId,
            campaignIds,
            cancellationToken);
        var notificationCounts = await LoadInvitationNotificationCountsByCampaignAsync(tenantId, campaignIds, cancellationToken);
        var deliveryAggregates = await LoadDeliveryAttemptAggregatesByCampaignAsync(tenantId, campaignIds, cancellationToken);
        var providerEventAggregates = await LoadProviderDeliveryEventAggregatesByCampaignAsync(
            tenantId,
            campaignIds,
            cancellationToken);
        var campaignResponses = campaigns
            .Select(campaign =>
            {
                var launchDetail = launchDetails.GetValueOrDefault(campaign.Id);
                var disclosurePolicy = launchDetail?.DisclosurePolicyId is Guid disclosurePolicyId
                    ? disclosurePolicies.GetValueOrDefault(disclosurePolicyId)
                    : null;

                return CreateOperationsCampaignResponse(
                    campaign,
                    launchDetail,
                    collectionAggregates.GetValueOrDefault(campaign.Id),
                    disclosurePolicy,
                    scoreCoverageInputs[campaign.Id],
                    openLinkCounts.GetValueOrDefault(campaign.Id),
                    targetAwareAssignmentCounts.GetValueOrDefault(campaign.Id),
                    notificationCounts.GetValueOrDefault(campaign.Id),
                    deliveryAggregates.GetValueOrDefault(campaign.Id),
                    providerEventAggregates.GetValueOrDefault(campaign.Id));
            })
            .ToArray();
        var selectedCampaign = SelectOperationsCampaign(campaigns);
        var groupCoverage = selectedCampaign is null
            ? null
            : await LoadOperationsGroupCoverageAsync(campaignSeriesId, selectedCampaign.Id, cancellationToken);
        var missingPrerequisites = CreateOperationsMissingPrerequisites(campaignResponses);
        var summaryCollectionStatus = DetermineSeriesCollectionStatus(campaignResponses);
        var summaryReportVisibilityStatus = DetermineSeriesReportVisibilityStatus(campaignResponses);
        var response = new CampaignSeriesOperationsWorkspaceResponse(
            new CampaignSeriesOperationsSeriesResponse(
                series.Id,
                series.Name,
                series.CreatedAt,
                series.UpdatedAt,
                series.StudyKind,
                IsSampleSeries(series),
                series.SampleScenario,
                GetReadOnlyReason(series),
                CreateStudyBriefResponse(series)),
            new CampaignSeriesOperationsSummaryResponse(
                campaignResponses.Length,
                campaignResponses.Count(campaign => campaign.Status == CampaignStatuses.Live),
                campaignResponses.Sum(campaign => campaign.OpenLinkAssignmentCount),
                campaignResponses.Sum(campaign => campaign.QueuedInvitationCount),
                campaignResponses.Sum(campaign => campaign.SentInvitationCount),
                campaignResponses.Sum(campaign => campaign.FailedInvitationCount),
                campaignResponses.Sum(campaign => campaign.DeliveryAttemptCount),
                campaignResponses.Sum(campaign => campaign.SubmittedResponseCount),
                campaignResponses.Sum(campaign => campaign.StartedResponseCount),
                campaignResponses.Sum(campaign => campaign.DraftResponseCount),
                MaxNullableDateTimeOffset(campaignResponses.Select(campaign => campaign.LatestResponseStartedAt)),
                MaxNullableDateTimeOffset(campaignResponses.Select(campaign => campaign.LatestResponseSubmittedAt)),
                summaryCollectionStatus,
                summaryReportVisibilityStatus,
                CreateCollectionGuidance(summaryCollectionStatus, summaryReportVisibilityStatus),
                missingPrerequisites.Count,
                campaignResponses.Sum(campaign => campaign.BouncedInvitationCount),
                campaignResponses.Sum(campaign => campaign.ProviderAcceptedEventCount),
                campaignResponses.Sum(campaign => campaign.ProviderDeliveredEventCount),
                campaignResponses.Sum(campaign => campaign.ProviderBouncedEventCount),
                campaignResponses.Sum(campaign => campaign.ProviderComplainedEventCount),
                MaxNullableDateTimeOffset(campaignResponses.Select(campaign => campaign.LatestProviderEventAt))),
            selectedCampaign is null
                ? null
                : campaignResponses.Single(campaign => campaign.Id == selectedCampaign.Id),
            missingPrerequisites,
            campaignResponses,
            ScoreCoverageSummary.Create(scoreCoverageInputs.Values.ToArray()),
            groupCoverage);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    private async Task<CampaignSeriesOperationsGroupCoverageSummaryResponse> LoadOperationsGroupCoverageAsync(
        Guid campaignSeriesId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var kMin = await db.DisclosurePolicies
            .AsNoTracking()
            .Where(policy => policy.CampaignSeriesId == campaignSeriesId && policy.RetiredAt == null)
            .OrderByDescending(policy => policy.CreatedAt)
            .Select(policy => (int?)policy.KMin)
            .FirstOrDefaultAsync(cancellationToken) ?? DisclosurePolicy.MinimumKMin;

        var assignmentRows = await db.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.CampaignId == campaignId)
            .Select(assignment => new
            {
                assignment.Id,
                assignment.RespondentSubjectId,
                Submitted = db.ResponseSessions.Any(session =>
                    session.AssignmentId == assignment.Id && session.SubmittedAt != null)
            })
            .ToArrayAsync(cancellationToken);

        var subjectIds = assignmentRows
            .Where(row => row.RespondentSubjectId.HasValue)
            .Select(row => row.RespondentSubjectId!.Value)
            .Distinct()
            .ToArray();

        var memberships = subjectIds.Length == 0
            ? []
            : await db.SubjectMemberships
                .AsNoTracking()
                .Where(membership => subjectIds.Contains(membership.SubjectId))
                .Join(
                    db.SubjectGroups.AsNoTracking(),
                    membership => membership.GroupId,
                    group => group.Id,
                    (membership, group) => new { membership.SubjectId, group.Id, group.Name })
                .ToArrayAsync(cancellationToken);

        var groupsBySubject = memberships
            .GroupBy(row => row.SubjectId)
            .ToDictionary(rows => rows.Key, rows => rows.Select(row => (row.Id, row.Name)).ToArray());

        var invitedByGroup = new Dictionary<Guid, (string Name, int Invited, int Submitted)>();
        var unattributedInvited = 0;
        var unattributedSubmitted = 0;

        foreach (var row in assignmentRows)
        {
            var groups = row.RespondentSubjectId.HasValue
                ? groupsBySubject.GetValueOrDefault(row.RespondentSubjectId.Value)
                : null;

            if (groups is null || groups.Length == 0)
            {
                unattributedInvited++;
                if (row.Submitted)
                {
                    unattributedSubmitted++;
                }

                continue;
            }

            foreach (var (groupId, groupName) in groups)
            {
                if (!invitedByGroup.TryGetValue(groupId, out var entry))
                {
                    entry = (groupName, 0, 0);
                }

                invitedByGroup[groupId] = (
                    entry.Name,
                    entry.Invited + 1,
                    entry.Submitted + (row.Submitted ? 1 : 0));
            }
        }

        var groupRows = invitedByGroup
            .Select(pair => new CampaignSeriesOperationsGroupCoverageResponse(
                pair.Key,
                pair.Value.Name,
                pair.Value.Invited,
                pair.Value.Submitted,
                pair.Value.Submitted >= kMin))
            .OrderByDescending(row => row.SubmittedCount)
            .ThenBy(row => row.GroupName, StringComparer.Ordinal)
            .ToArray();

        return new CampaignSeriesOperationsGroupCoverageSummaryResponse(
            kMin,
            unattributedInvited,
            unattributedSubmitted,
            groupRows);
    }

    public async Task<Result<CampaignSeriesReportsWorkspaceResponse>> GetCampaignSeriesReportsWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesReportsWorkspaceResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId)
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new CampaignRow(
                entity.Id,
                entity.CampaignSeriesId,
                entity.TemplateVersionId,
                entity.Name,
                entity.Status,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.StartAt,
                entity.EndAt,
                entity.UpdatedAt,
                entity.ClosedAt,
                entity.ClosedByUserId,
                entity.CloseReason))
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(entity => entity.Id).ToArray();
        var launchDetails = await LoadReportLaunchDetailsByCampaignAsync(campaignIds, cancellationToken);
        var scoringRuleIdsByTemplate = await LoadCurrentScoringRuleIdsByTemplateVersionAsync(
            campaigns
                .Select(campaign => campaign.TemplateVersionId)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var disclosurePolicies = await LoadReportDisclosurePoliciesAsync(
            launchDetails.Values
                .Select(detail => detail.DisclosurePolicyId)
                .OfType<Guid>()
                .Distinct()
                .ToArray(),
            cancellationToken);
        var submittedCounts = await LoadSubmittedResponseCountsByCampaignAsync(campaignIds, cancellationToken);
        var scoreCoverageAggregates = await LoadScoreCoverageAggregatesByCampaignAsync(campaignIds, cancellationToken);
        var scoreCoverageInputs = campaigns.ToDictionary(
            campaign => campaign.Id,
            campaign => CreateScoreCoverageInput(
                campaign,
                launchDetails.GetValueOrDefault(campaign.Id),
                scoringRuleIdsByTemplate,
                submittedCounts.GetValueOrDefault(campaign.Id),
                scoreCoverageAggregates.GetValueOrDefault(campaign.Id)));
        var scoreOutputCounts = await LoadScoreOutputCountsByCampaignAsync(campaignIds, cancellationToken);
        var exportArtifactCounts = await LoadExportArtifactCountsByCampaignAsync(campaignIds, cancellationToken);
        var latestExportArtifacts = await LoadLatestExportArtifactsByCampaignAsync(campaignIds, cancellationToken);
        var reportExportArtifactCount = await LoadReportExportArtifactCountAsync(
            series.Id,
            campaignIds,
            cancellationToken);
        var campaignResponses = campaigns
            .Select(campaign => CreateReportsCampaignResponse(
                campaign,
                launchDetails.GetValueOrDefault(campaign.Id),
                disclosurePolicies,
                submittedCounts.GetValueOrDefault(campaign.Id),
                scoreOutputCounts.GetValueOrDefault(campaign.Id),
                exportArtifactCounts.GetValueOrDefault(campaign.Id),
                latestExportArtifacts.GetValueOrDefault(campaign.Id)))
            .ToArray();
        var exportArtifactRegistry = await LoadReportExportArtifactRegistryAsync(
            series.Id,
            series.Name,
            campaignResponses.ToDictionary(campaign => campaign.Id),
            campaignIds,
            cancellationToken);
        var selectedCampaign = SelectReportsCampaign(campaignResponses);
        var missingPrerequisites = CreateReportsMissingPrerequisites(campaignResponses);
        var resultsAnalytics = await LoadCampaignSeriesResultsAnalyticsAsync(
            tenantId,
            series.Id,
            campaignResponses,
            selectedCampaign,
            cancellationToken);
        var resultsDashboard = CreateResultsDashboard(resultsAnalytics);
        var response = new CampaignSeriesReportsWorkspaceResponse(
            new CampaignSeriesReportsSeriesResponse(
                series.Id,
                series.Name,
                series.CreatedAt,
                series.UpdatedAt,
                series.StudyKind,
                IsSampleSeries(series),
                series.SampleScenario,
                GetReadOnlyReason(series),
                CreateStudyBriefResponse(series)),
            new CampaignSeriesReportsSummaryResponse(
                campaignResponses.Length,
                campaignResponses.Count(campaign => campaign.Status == CampaignStatuses.Live),
                campaignResponses.Count(IsReportableCampaign),
                campaignResponses.Sum(campaign => campaign.SubmittedResponseCount),
                campaignResponses.Sum(campaign => campaign.ScoreCount),
                reportExportArtifactCount,
                campaignResponses.Sum(campaign => campaign.VisibleScoreCount),
                campaignResponses.Sum(campaign => campaign.SuppressedScoreCount),
                missingPrerequisites.Count,
                campaignResponses.Count(campaign => campaign.DataFinality == PreliminaryLiveDataFinality),
                campaignResponses.Count(campaign => campaign.DataFinality == ClosedWaveDataFinality)),
            selectedCampaign,
            missingPrerequisites,
            exportArtifactRegistry,
            campaignResponses,
            ScoreCoverageSummary.Create(scoreCoverageInputs.Values.ToArray()),
            resultsAnalytics,
            resultsDashboard);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CampaignSeriesReportsWidgetManifestResponse>> GetCampaignSeriesReportsWidgetManifestAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        bool canManageSetup,
        CancellationToken cancellationToken)
    {
        var workspace = await GetCampaignSeriesReportsWorkspaceAsync(tenantId, campaignSeriesId, cancellationToken);

        return workspace.IsSuccess
            ? Result.Success(ToReportsWidgetManifest(workspace.Value, canManageSetup))
            : Result.Failure<CampaignSeriesReportsWidgetManifestResponse>(workspace.Error);
    }

    private async Task<CampaignSeriesResultsAnalyticsResponse?> LoadCampaignSeriesResultsAnalyticsAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        IReadOnlyList<CampaignSeriesReportsCampaignResponse> campaigns,
        CampaignSeriesReportsCampaignResponse? selectedCampaign,
        CancellationToken cancellationToken)
    {
        if (selectedCampaign is null || campaigns.Count == 0)
        {
            return null;
        }

        var campaignById = campaigns.ToDictionary(campaign => campaign.Id);
        var campaignIds = campaignById.Keys.ToArray();
        var rawScores = await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                join campaign in db.Campaigns.AsNoTracking()
                    on score.CampaignId equals campaign.Id
                where score.TenantId == tenantId &&
                    campaign.CampaignSeriesId == campaignSeriesId &&
                    campaignIds.Contains(score.CampaignId) &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                select new ResultsScoreObservationRow(
                    score.Id,
                    score.CampaignId,
                    score.ResponseSessionId,
                    score.DimensionCode,
                    score.Value,
                    score.NValid,
                    score.NExpected,
                    score.MissingPolicyStatus,
                    score.ComputedAt,
                    session.SubmittedAt!.Value,
                    assignment.TargetSubjectId,
                    assignment.RespondentSubjectId))
            .ToListAsync(cancellationToken);

        var latestScores = rawScores
            .GroupBy(score => new { score.ResponseSessionId, score.DimensionCode })
            .Select(group => group
                .OrderByDescending(score => score.ComputedAt)
                .ThenByDescending(score => score.ScoreId)
                .First())
            .ToArray();
        var selectedScores = latestScores
            .Where(score => score.CampaignId == selectedCampaign.Id)
            .ToArray();
        var selectedOutputRows = selectedScores
            .GroupBy(score => score.DimensionCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => CreateResultsScoreOutputResponse(
                group.Key,
                group.ToArray(),
                selectedCampaign.SubmittedResponseCount,
                IsCampaignResultVisible(selectedCampaign, group.Count()),
                DetermineResultsSuppressionReason(selectedCampaign, group.Count())))
            .ToArray();
        var groupRows = await CreateResultsGroupMatrixRowsAsync(
            tenantId,
            selectedCampaign,
            selectedScores,
            cancellationToken);
        var waveRows = AddResultsWaveComparisons(latestScores
            .GroupBy(score => new { score.CampaignId, score.DimensionCode })
            .Where(group => campaignById.ContainsKey(group.Key.CampaignId))
            .OrderBy(group => campaignById[group.Key.CampaignId].LatestLaunchAt ?? DateTimeOffset.MaxValue)
            .ThenBy(group => campaignById[group.Key.CampaignId].Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.DimensionCode, StringComparer.Ordinal)
            .Select(group =>
            {
                var campaign = campaignById[group.Key.CampaignId];
                return CreateResultsWaveMatrixRowResponse(
                    campaign,
                    group.Key.DimensionCode,
                    group.ToArray());
            })
            .ToArray());

        return new CampaignSeriesResultsAnalyticsResponse(
            selectedCampaign.Id,
            selectedCampaign.Name,
            selectedCampaign.DisclosureKMin ?? 0,
            selectedCampaign.DisclosureState,
            selectedOutputRows,
            groupRows,
            waveRows,
            CreateResultsInsights(selectedCampaign, selectedOutputRows, groupRows, waveRows));
    }

    private async Task<CampaignSeriesResultsGroupMatrixRowResponse[]> CreateResultsGroupMatrixRowsAsync(
        Guid tenantId,
        CampaignSeriesReportsCampaignResponse selectedCampaign,
        IReadOnlyList<ResultsScoreObservationRow> selectedScores,
        CancellationToken cancellationToken)
    {
        var subjectIds = selectedScores
            .Select(score => score.TargetSubjectId ?? score.RespondentSubjectId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        if (subjectIds.Length == 0)
        {
            return [];
        }

        var memberships = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subject in db.Subjects.AsNoTracking()
                    on membership.SubjectId equals subject.Id
                join subjectGroup in db.SubjectGroups.AsNoTracking()
                    on membership.GroupId equals subjectGroup.Id
                where subjectIds.Contains(membership.SubjectId) &&
                    subject.TenantId == tenantId &&
                    subjectGroup.TenantId == tenantId &&
                    subject.DeletedAt == null &&
                    subjectGroup.DeletedAt == null
                select new ResultsSubjectGroupMembershipRow(
                    membership.SubjectId,
                    subjectGroup.Type,
                    subjectGroup.Name,
                    membership.ValidFrom,
                    membership.ValidTo))
            .ToListAsync(cancellationToken);

        var membershipsBySubject = memberships
            .GroupBy(membership => membership.SubjectId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var joinedRows = new List<ResultsGroupedScoreObservationRow>();

        foreach (var score in selectedScores)
        {
            var subjectId = score.TargetSubjectId ?? score.RespondentSubjectId;
            if (!subjectId.HasValue ||
                !membershipsBySubject.TryGetValue(subjectId.Value, out var subjectMemberships))
            {
                continue;
            }

            var submittedDate = DateOnly.FromDateTime(score.SubmittedAt.UtcDateTime);
            var applicableMemberships = subjectMemberships
                .Where(membership => MembershipAppliesOn(membership, submittedDate))
                .GroupBy(membership => new { membership.GroupType, membership.GroupName })
                .Select(group => group.First());
            foreach (var membership in applicableMemberships)
            {
                joinedRows.Add(new ResultsGroupedScoreObservationRow(
                    membership.GroupType,
                    membership.GroupName,
                    score.DimensionCode,
                    score.Value,
                    score.NValid,
                    score.NExpected,
                    score.MissingPolicyStatus));
            }
        }

        return joinedRows
            .GroupBy(row => new { row.GroupType, row.GroupName, row.DimensionCode })
            .OrderBy(group => group.Key.GroupType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.GroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.DimensionCode, StringComparer.Ordinal)
            .Select(group => CreateResultsGroupMatrixRowResponse(
                selectedCampaign,
                group.Key.GroupType,
                group.Key.GroupName,
                group.Key.DimensionCode,
                group.ToArray()))
            .ToArray();
    }

    private static CampaignSeriesResultsScoreOutputResponse CreateResultsScoreOutputResponse(
        string dimensionCode,
        IReadOnlyList<ResultsScoreObservationRow> scores,
        int submittedResponseCount,
        bool visible,
        string? suppressionReason)
    {
        var values = scores.Select(score => score.Value).ToArray();
        if (!visible || values.Length == 0)
        {
            return new CampaignSeriesResultsScoreOutputResponse(
                dimensionCode,
                "suppressed",
                SubmittedResponseCount: null,
                ScoreCount: null,
                Mean: null,
                Median: null,
                StandardDeviation: null,
                Min: null,
                Max: null,
                NValidTotal: null,
                NExpectedTotal: null,
                MissingPolicyStatusSummary: null,
                suppressionReason ?? "not_reportable");
        }

        return new CampaignSeriesResultsScoreOutputResponse(
            dimensionCode,
            "visible",
            submittedResponseCount,
            values.Length,
            CalculateResultsMean(values),
            CalculateResultsMedian(values),
            CalculateResultsStandardDeviation(values),
            values.Min(),
            values.Max(),
            scores.Sum(score => score.NValid),
            scores.Sum(score => score.NExpected),
            SummarizeResultMissingPolicyStatuses(scores.Select(score => score.MissingPolicyStatus)),
            SuppressionReason: null);
    }

    private static CampaignSeriesResultsGroupMatrixRowResponse CreateResultsGroupMatrixRowResponse(
        CampaignSeriesReportsCampaignResponse selectedCampaign,
        string groupType,
        string groupName,
        string dimensionCode,
        IReadOnlyList<ResultsGroupedScoreObservationRow> scores)
    {
        var values = scores.Select(score => score.Value).ToArray();
        var visible = selectedCampaign.DisclosureKMin.HasValue &&
            values.Length >= selectedCampaign.DisclosureKMin.Value;

        if (!visible || values.Length == 0)
        {
            return new CampaignSeriesResultsGroupMatrixRowResponse(
                groupType,
                groupName,
                dimensionCode,
                "suppressed",
                SubmittedResponseCount: null,
                ScoreCount: null,
                Mean: null,
                Median: null,
                StandardDeviation: null,
                Min: null,
                Max: null,
                SuppressionReason: selectedCampaign.DisclosureKMin.HasValue
                    ? "insufficient_responses"
                    : "disclosure_policy_missing");
        }

        return new CampaignSeriesResultsGroupMatrixRowResponse(
            groupType,
            groupName,
            dimensionCode,
            "visible",
            values.Length,
            values.Length,
            CalculateResultsMean(values),
            CalculateResultsMedian(values),
            CalculateResultsStandardDeviation(values),
            values.Min(),
            values.Max(),
            SuppressionReason: null);
    }

    private static CampaignSeriesResultsWaveMatrixRowResponse CreateResultsWaveMatrixRowResponse(
        CampaignSeriesReportsCampaignResponse campaign,
        string dimensionCode,
        IReadOnlyList<ResultsScoreObservationRow> scores)
    {
        var values = scores.Select(score => score.Value).ToArray();
        var visible = IsCampaignResultVisible(campaign, values.Length);

        if (!visible)
        {
            return new CampaignSeriesResultsWaveMatrixRowResponse(
                campaign.Id,
                campaign.Name,
                campaign.Status,
                campaign.DataFinality,
                campaign.ClosedAt,
                dimensionCode,
                "suppressed",
                SubmittedResponseCount: null,
                ScoreCount: null,
                Mean: null,
                Median: null,
                StandardDeviation: null,
                Min: null,
                Max: null,
                SuppressionReason: DetermineResultsSuppressionReason(campaign, values.Length));
        }

        return new CampaignSeriesResultsWaveMatrixRowResponse(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.DataFinality,
            campaign.ClosedAt,
            dimensionCode,
            "visible",
            campaign.SubmittedResponseCount,
            values.Length,
            CalculateResultsMean(values),
            CalculateResultsMedian(values),
            CalculateResultsStandardDeviation(values),
            values.Min(),
            values.Max(),
            SuppressionReason: null);
    }

    private static CampaignSeriesResultsWaveMatrixRowResponse[] AddResultsWaveComparisons(
        IReadOnlyList<CampaignSeriesResultsWaveMatrixRowResponse> rows)
    {
        var comparedRows = new List<CampaignSeriesResultsWaveMatrixRowResponse>();

        foreach (var dimensionRows in rows.GroupBy(row => row.DimensionCode, StringComparer.Ordinal))
        {
            CampaignSeriesResultsWaveMatrixRowResponse? firstVisible = null;
            CampaignSeriesResultsWaveMatrixRowResponse? previousVisible = null;

            foreach (var row in dimensionRows)
            {
                if (row.Disclosure != "visible" || !row.Mean.HasValue)
                {
                    comparedRows.Add(row with { ComparisonState = "not_comparable" });
                    continue;
                }

                if (firstVisible is null)
                {
                    firstVisible = row;
                    previousVisible = row;
                    comparedRows.Add(row with
                    {
                        DeltaFromPreviousMean = null,
                        DeltaFromFirstMean = 0,
                        ComparisonState = "baseline"
                    });
                    continue;
                }

                var previousVisibleMean = previousVisible?.Mean;
                var firstVisibleMean = firstVisible.Mean;

                comparedRows.Add(row with
                {
                    DeltaFromPreviousMean = previousVisibleMean.HasValue
                        ? RoundResultsDelta(row.Mean.Value - previousVisibleMean.Value)
                        : null,
                    DeltaFromFirstMean = firstVisibleMean.HasValue
                        ? RoundResultsDelta(row.Mean.Value - firstVisibleMean.Value)
                        : null,
                    ComparisonState = "compared"
                });
                previousVisible = row;
            }
        }

        return comparedRows.ToArray();
    }

    private static decimal RoundResultsDelta(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    private static CampaignSeriesResultsInsightResponse[] CreateResultsInsights(
        CampaignSeriesReportsCampaignResponse selectedCampaign,
        IReadOnlyList<CampaignSeriesResultsScoreOutputResponse> outputRows,
        IReadOnlyList<CampaignSeriesResultsGroupMatrixRowResponse> groupRows,
        IReadOnlyList<CampaignSeriesResultsWaveMatrixRowResponse> waveRows)
    {
        var insights = new List<CampaignSeriesResultsInsightResponse>();

        var visibleOutputCount = outputRows.Count(row =>
            row.Disclosure == "visible" &&
            row.Mean.HasValue &&
            row.ScoreCount.HasValue &&
            row.ScoreCount.Value > 0);

        if (outputRows.Count == 0)
        {
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "score_outputs",
                "blocked",
                "No result outputs yet",
                "Run scoring for submitted responses before interpreting results."));
        }
        else if (visibleOutputCount == 0)
        {
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "score_outputs",
                "pending",
                "Results hidden by disclosure",
                "Submitted responses may exist, but aggregate result values are hidden until disclosure and scoring requirements are met."));
        }
        else
        {
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "score_outputs",
                "ready",
                $"{visibleOutputCount} visible result output{PluralSuffix(visibleOutputCount)} ready",
                "Review mean, median, spread, range, and missing-answer coverage before sharing conclusions."));
        }

        if (groupRows.Count == 0)
        {
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "groups",
                "pending",
                "No group comparison yet",
                "Group comparisons need recipient subjects with directory group membership on the selected wave."));
        }
        else if (groupRows.Any(row => row.Disclosure == "suppressed"))
        {
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "groups",
                "pending",
                "Some groups are hidden",
                $"Rows under the disclosure minimum of {selectedCampaign.DisclosureKMin ?? 0} responses are hidden."));
        }
        else
        {
            var visibleGroupCount = groupRows
                .Where(row => row.Disclosure == "visible")
                .Select(row => new { row.GroupType, row.GroupName })
                .Distinct()
                .Count();
            insights.Add(new CampaignSeriesResultsInsightResponse(
                "groups",
                "ready",
                $"{visibleGroupCount} group comparison{PluralSuffix(visibleGroupCount)} ready",
                "Review group rows as aggregate comparisons only; do not use them to identify respondents."));
        }

        var comparableWaveCount = waveRows
            .Where(row => row.Disclosure == "visible" && row.ComparisonState is "baseline" or "compared")
            .GroupBy(row => row.DimensionCode, StringComparer.Ordinal)
            .Where(group => group.Any(row => row.ComparisonState == "compared"))
            .Select(group => group.Select(row => row.CampaignId).Distinct().Count())
            .DefaultIfEmpty(0)
            .Max();
        insights.Add(comparableWaveCount >= 2
            ? new CampaignSeriesResultsInsightResponse(
                "waves",
                "ready",
                $"{comparableWaveCount} measurements can be compared",
                "Use wave rows for change-over-time review. Treat live measurements as preliminary.")
            : new CampaignSeriesResultsInsightResponse(
                "waves",
                "pending",
                "Wave comparison not ready",
                "At least two measurements need visible score rows before change-over-time comparisons are useful."));

        return insights.ToArray();
    }

    private static CampaignSeriesResultsDashboardResponse? CreateResultsDashboard(
        CampaignSeriesResultsAnalyticsResponse? analytics)
    {
        if (analytics is null)
        {
            return null;
        }

        var outputBars = analytics.ScoreOutputs
            .OrderByDescending(row => row.Disclosure == "visible")
            .ThenByDescending(row => row.Mean ?? decimal.MinValue)
            .ThenBy(row => row.DimensionCode, StringComparer.Ordinal)
            .Select(row => new ResultsDashboardBarResponse(
                $"output:{row.DimensionCode}",
                row.DimensionCode,
                row.DimensionCode,
                row.Disclosure,
                row.Disclosure == "visible" ? row.Mean : null,
                row.Disclosure == "visible" ? row.ScoreCount : null,
                row.Disclosure == "visible"
                    ? $"median {FormatResultsDashboardDecimal(row.Median)}, range {FormatResultsDashboardDecimal(row.Min)}-{FormatResultsDashboardDecimal(row.Max)}"
                    : null,
                row.Disclosure == "visible" ? null : row.SuppressionReason))
            .ToArray();

        var groupBars = analytics.GroupRows
            .OrderByDescending(row => row.Disclosure == "visible")
            .ThenBy(row => row.GroupType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.GroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.DimensionCode, StringComparer.Ordinal)
            .Take(24)
            .Select(row => new ResultsDashboardBarResponse(
                $"group:{row.GroupType}:{row.GroupName}:{row.DimensionCode}",
                row.GroupName,
                row.DimensionCode,
                row.Disclosure,
                row.Disclosure == "visible" ? row.Mean : null,
                row.Disclosure == "visible" ? row.ScoreCount : null,
                row.GroupType,
                row.Disclosure == "visible" ? null : row.SuppressionReason))
            .ToArray();

        var waveTrendPoints = analytics.WaveRows
            .OrderBy(row => row.CampaignName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.DimensionCode, StringComparer.Ordinal)
            .Take(48)
            .Select(row => new ResultsDashboardPointResponse(
                $"wave:{row.CampaignId}:{row.DimensionCode}",
                row.CampaignId,
                row.CampaignName,
                row.DimensionCode,
                row.Disclosure,
                row.Disclosure == "visible" ? row.Mean : null,
                row.Disclosure == "visible" ? row.DeltaFromPreviousMean : null,
                row.ComparisonState,
                row.DataFinality,
                row.Disclosure == "visible" ? row.ScoreCount : null,
                row.Disclosure == "visible" ? null : row.SuppressionReason))
            .ToArray();

        var visibleOutputCount = analytics.ScoreOutputs.Count(row =>
            row.Disclosure == "visible" &&
            row.Mean.HasValue);
        var suppressedOutputCount = analytics.ScoreOutputs.Count(row => row.Disclosure != "visible");
        var visibleGroupRowCount = analytics.GroupRows.Count(row =>
            row.Disclosure == "visible" &&
            row.Mean.HasValue);
        var comparableWaveCount = analytics.WaveRows
            .Where(row => row.Disclosure == "visible" && row.ComparisonState is "baseline" or "compared")
            .Select(row => row.CampaignId)
            .Distinct()
            .Count();

        return new CampaignSeriesResultsDashboardResponse(
            analytics.SelectedCampaignId,
            analytics.SelectedCampaignName,
            analytics.DisclosureKMin,
            analytics.DisclosureState,
            [
                new ResultsDashboardMetricResponse(
                    "visible_outputs",
                    visibleOutputCount,
                    "count",
                    visibleOutputCount == 0 ? "No visible aggregate result values yet." : null,
                    visibleOutputCount > 0 ? "ready" : "pending"),
                new ResultsDashboardMetricResponse(
                    "hidden_outputs",
                    suppressedOutputCount,
                    "count",
                    suppressedOutputCount > 0 ? "Hidden by disclosure or missing score requirements." : null,
                    suppressedOutputCount > 0 ? "attention" : "ready"),
                new ResultsDashboardMetricResponse(
                    "group_rows",
                    visibleGroupRowCount,
                    "count",
                    visibleGroupRowCount == 0 ? "Add directory groups or more responses for group comparison." : null,
                    visibleGroupRowCount > 0 ? "ready" : "pending"),
                new ResultsDashboardMetricResponse(
                    "compared_measurements",
                    comparableWaveCount,
                    "count",
                    comparableWaveCount < 2 ? "At least two visible measurements are needed for change over time." : null,
                    comparableWaveCount >= 2 ? "ready" : "pending")
            ],
            outputBars,
            groupBars,
            waveTrendPoints,
            analytics.Insights
                .Select(row => new ResultsDashboardNoteResponse(row.Kind, row.Severity, row.Title, row.Detail))
                .ToArray());
    }

    private static string FormatResultsDashboardDecimal(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "n/a";
    }

    private static bool IsCampaignResultVisible(CampaignSeriesReportsCampaignResponse campaign, int scoreCount)
    {
        if (scoreCount <= 0)
        {
            return false;
        }

        if (campaign.ResponseIdentityMode == ResponseIdentityModes.Identified)
        {
            return true;
        }

        return campaign.DisclosureKMin.HasValue &&
            scoreCount >= campaign.DisclosureKMin.Value;
    }

    private static string? DetermineResultsSuppressionReason(
        CampaignSeriesReportsCampaignResponse campaign,
        int scoreCount)
    {
        if (scoreCount == 0)
        {
            return "no_scores";
        }

        if (!campaign.DisclosureKMin.HasValue)
        {
            return "disclosure_policy_missing";
        }

        if (campaign.ResponseIdentityMode == ResponseIdentityModes.Identified)
        {
            return null;
        }

        return scoreCount < campaign.DisclosureKMin.Value
            ? "insufficient_responses"
            : null;
    }

    private static bool MembershipAppliesOn(
        ResultsSubjectGroupMembershipRow membership,
        DateOnly submittedDate)
    {
        return (!membership.ValidFrom.HasValue || membership.ValidFrom.Value <= submittedDate) &&
            (!membership.ValidTo.HasValue || membership.ValidTo.Value >= submittedDate);
    }

    private static decimal CalculateResultsMean(IReadOnlyCollection<decimal> values)
    {
        return Math.Round(values.Average(), 4, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateResultsMedian(IReadOnlyCollection<decimal> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            return 0;
        }

        var middle = ordered.Length / 2;
        var median = ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2;

        return Math.Round(median, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateResultsStandardDeviation(IReadOnlyCollection<decimal> values)
    {
        if (values.Count <= 1)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values
            .Select(value => Math.Pow((double)(value - mean), 2))
            .Average();

        return Math.Round((decimal)Math.Sqrt(variance), 4, MidpointRounding.AwayFromZero);
    }

    private static string SummarizeResultMissingPolicyStatuses(IEnumerable<string> statuses)
    {
        var distinctStatuses = statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToArray();

        return distinctStatuses.Length switch
        {
            0 => "not_available",
            1 => distinctStatuses[0],
            _ => "mixed"
        };
    }

    public async Task<Result<CampaignSeriesWavesWorkspaceResponse>> GetCampaignSeriesWavesWorkspaceAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .Where(entity => entity.Id == campaignSeriesId)
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .SingleOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesWavesWorkspaceResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId)
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new CampaignRow(
                entity.Id,
                entity.CampaignSeriesId,
                entity.TemplateVersionId,
                entity.Name,
                entity.Status,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.StartAt,
                entity.EndAt,
                entity.UpdatedAt,
                entity.ClosedAt,
                entity.ClosedByUserId,
                entity.CloseReason))
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(entity => entity.Id).ToArray();
        var launchDetails = await LoadWaveLaunchDetailsByCampaignAsync(campaignIds, cancellationToken);
        var disclosurePolicies = await LoadReportDisclosurePoliciesAsync(
            launchDetails.Values
                .Select(detail => detail.DisclosurePolicyId)
                .OfType<Guid>()
                .Distinct()
                .ToArray(),
            cancellationToken);
        var scoringRules = await LoadWaveScoringRulesAsync(
            launchDetails.Values
                .Select(detail => detail.ScoringRuleId)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var submittedCounts = await LoadSubmittedResponseCountsByCampaignAsync(campaignIds, cancellationToken);
        var scoreCounts = await LoadScoreCountsByCampaignAsync(campaignIds, cancellationToken);
        var trajectoryRows = await LoadWaveSubmittedTrajectoriesAsync(campaignIds, cancellationToken);
        var scoreDimensionRows = await LoadWaveScoreDimensionsAsync(campaignIds, cancellationToken);
        var waves = campaigns
            .Select(campaign => CreateWavesWaveResponse(
                campaign,
                launchDetails.GetValueOrDefault(campaign.Id),
                disclosurePolicies,
                scoringRules,
                submittedCounts.GetValueOrDefault(campaign.Id),
                scoreCounts.GetValueOrDefault(campaign.Id),
                trajectoryRows.Count(row => row.CampaignId == campaign.Id)))
            .OrderBy(wave => wave.LatestLaunchAt ?? DateTimeOffset.MaxValue)
            .ThenBy(wave => wave.Name)
            .ToArray();
        var selectedWaves = waves
            .Where(wave => wave.WaveState == "wave")
            .Take(2)
            .ToArray();
        var selectedBaselineWave = selectedWaves.ElementAtOrDefault(0);
        var selectedComparisonWave = selectedWaves.ElementAtOrDefault(1);
        var comparison = CreateWavesComparison(
            selectedBaselineWave,
            selectedComparisonWave,
            trajectoryRows,
            scoreDimensionRows);
        var missingPrerequisites = CreateWavesMissingPrerequisites(
            waves,
            trajectoryRows,
            comparison);
        var response = new CampaignSeriesWavesWorkspaceResponse(
            new CampaignSeriesWavesSeriesResponse(
                series.Id,
                series.Name,
                series.CreatedAt,
                series.UpdatedAt,
                series.StudyKind,
                IsSampleSeries(series),
                series.SampleScenario,
                GetReadOnlyReason(series),
                CreateStudyBriefResponse(series)),
            new CampaignSeriesWavesSummaryResponse(
                waves.Length,
                waves.Count(wave => wave.Status == CampaignStatuses.Live),
                waves.Count(wave => wave.WaveState == "wave"),
                waves.Count(wave => wave.SubmittedResponseCount > 0),
                trajectoryRows
                    .Select(row => row.ParticipantCodeId)
                    .Distinct()
                    .Count(),
                CountCompleteTrajectories(trajectoryRows),
                comparison.VisibleScoreCount +
                    comparison.SuppressedScoreCount +
                    comparison.BlockedScoreCount,
                comparison.VisibleScoreCount,
                comparison.SuppressedScoreCount,
                comparison.BlockedScoreCount,
                missingPrerequisites.Count,
                waves.Count(wave => wave.DataFinality == PreliminaryLiveDataFinality),
                waves.Count(wave => wave.DataFinality == ClosedWaveDataFinality)),
            selectedBaselineWave,
            selectedComparisonWave,
            comparison,
            missingPrerequisites,
            waves);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    private async Task<WorkspaceCommandCenterResponse> LoadWorkspaceCommandCenterAsync(
        Guid tenantId,
        WorkspaceOverviewTotalsResponse totals,
        IReadOnlyCollection<CampaignSeriesListItemResponse> seriesItems,
        bool canManageSetup,
        bool canManageTeam,
        CancellationToken cancellationToken)
    {
        var commands = new List<WorkspaceCommandCenterItemResponse>();
        var activeSeries = seriesItems
            .Where(item => !item.Archived)
            .ToArray();

        if (activeSeries.Length == 0)
        {
            commands.Add(new WorkspaceCommandCenterItemResponse(
                "campaign_series.create",
                canManageSetup ? "Create a campaign series" : "No campaign series yet",
                canManageSetup
                    ? "Start by creating a campaign series for the tenant workspace."
                    : "This tenant does not have active campaign series yet.",
                "empty",
                "campaign_series",
                "/app/campaign-series",
                canManageSetup ? "Open campaign series" : "View campaign series",
                Priority: 10,
                RequiredPermission: canManageSetup ? PlatformPermissions.SetupManage : null));
        }

        if (canManageSetup)
        {
            var directory = await LoadWorkspaceDirectorySummaryAsync(tenantId, cancellationToken);
            if (directory.SubjectCount == 0 || directory.GroupCount == 0)
            {
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    "directory.setup",
                    "Set up Directory",
                    $"Directory has {directory.SubjectCount} subjects and {directory.GroupCount} groups. Add the roster before assignment rules depend on it.",
                    "blocked",
                    "directory",
                    "/app/directory",
                    "Open Directory",
                    Priority: 20,
                    RequiredPermission: PlatformPermissions.SetupManage));
            }
        }

        if (canManageSetup)
        {
            var setupSeries = activeSeries.FirstOrDefault(item =>
                item.ReadinessStatus == CampaignSeriesPortfolioStatuses.NotConfigured);
            if (setupSeries is not null)
            {
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    $"series.{setupSeries.Id:N}.setup",
                    $"Finish setup for {setupSeries.Name}",
                    "This series does not have a campaign configured yet.",
                    "blocked",
                    "setup",
                    $"/app/campaign-series/{setupSeries.Id}/setup",
                    "Open setup",
                    Priority: 30,
                    CampaignSeriesId: setupSeries.Id,
                    RequiredPermission: PlatformPermissions.SetupManage));
            }
        }

        var operationsSeries = activeSeries.FirstOrDefault(item => item.LiveCampaignCount > 0);
        if (operationsSeries is not null)
        {
            commands.Add(new WorkspaceCommandCenterItemResponse(
                $"series.{operationsSeries.Id:N}.operations",
                $"Monitor live collection for {operationsSeries.Name}",
                $"{operationsSeries.LiveCampaignCount} live campaign{PluralSuffix(operationsSeries.LiveCampaignCount)} can be reviewed in Operations.",
                "ready",
                "operations",
                $"/app/campaign-series/{operationsSeries.Id}/operations",
                "Open operations",
                Priority: 40,
                CampaignSeriesId: operationsSeries.Id));
        }

        var reportSeries = activeSeries.FirstOrDefault(item => item.SubmittedResponseCount > 0);
        if (reportSeries is not null)
        {
            commands.Add(new WorkspaceCommandCenterItemResponse(
                $"series.{reportSeries.Id:N}.reports",
                $"Review report data for {reportSeries.Name}",
                $"{reportSeries.SubmittedResponseCount} submitted response{PluralSuffix(reportSeries.SubmittedResponseCount)} can be reviewed in Reports.",
                "ready",
                "reports",
                $"/app/campaign-series/{reportSeries.Id}/reports",
                "Open reports",
                Priority: 50,
                CampaignSeriesId: reportSeries.Id));
        }

        if (canManageSetup && activeSeries.Length > 0)
        {
            var seriesIds = activeSeries.Select(item => item.Id).ToArray();
            var unscoredCounts = await LoadSeriesUnscoredSubmittedResponseCountsAsync(
                seriesIds,
                cancellationToken);
            var scoreSeries = activeSeries.FirstOrDefault(item => unscoredCounts.GetValueOrDefault(item.Id) > 0);
            if (scoreSeries is not null)
            {
                var unscoredCount = unscoredCounts[scoreSeries.Id];
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    $"series.{scoreSeries.Id:N}.score_remediation",
                    $"Complete scoring for {scoreSeries.Name}",
                    $"{unscoredCount} submitted response{PluralSuffix(unscoredCount)} do not have a successful score run yet.",
                    "blocked",
                    "reports",
                    $"/app/campaign-series/{scoreSeries.Id}/reports",
                    "Open reports",
                    Priority: 55,
                    CampaignSeriesId: scoreSeries.Id,
                    RequiredPermission: PlatformPermissions.SetupManage));
            }
        }

        if (canManageSetup && activeSeries.Length > 0)
        {
            var seriesIds = activeSeries.Select(item => item.Id).ToArray();
            var exportCounts = await LoadSeriesExportArtifactCountsAsync(seriesIds, cancellationToken);
            var exportSeries = activeSeries.FirstOrDefault(item => exportCounts.GetValueOrDefault(item.Id) > 0);
            if (exportSeries is not null)
            {
                var exportCount = exportCounts[exportSeries.Id];
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    $"series.{exportSeries.Id:N}.exports",
                    $"Inspect exports for {exportSeries.Name}",
                    $"{exportCount} export artifact{PluralSuffix(exportCount)} are available from the Reports surface.",
                    "ready",
                    "reports",
                    $"/app/campaign-series/{exportSeries.Id}/reports",
                    "Open exports",
                    Priority: 60,
                    CampaignSeriesId: exportSeries.Id,
                    RequiredPermission: PlatformPermissions.SetupManage));
            }
        }

        if (activeSeries.Length > 0)
        {
            var seriesIds = activeSeries.Select(item => item.Id).ToArray();
            var longitudinalCounts = await LoadSeriesLongitudinalCampaignCountsAsync(seriesIds, cancellationToken);
            var wavesSeries = activeSeries.FirstOrDefault(item => longitudinalCounts.GetValueOrDefault(item.Id) >= 2);
            if (wavesSeries is not null)
            {
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    $"series.{wavesSeries.Id:N}.waves",
                    $"Review waves for {wavesSeries.Name}",
                    "This series has at least two anonymous longitudinal campaigns available for wave review.",
                    wavesSeries.SubmittedResponseCount > 0 ? "ready" : "pending",
                    "waves",
                    $"/app/campaign-series/{wavesSeries.Id}/waves",
                    "Open waves",
                    Priority: 70,
                    CampaignSeriesId: wavesSeries.Id));
            }
        }

        if (canManageTeam)
        {
            var pendingProviderLinkCount = await LoadPendingProviderLinkCountAsync(tenantId, cancellationToken);
            if (pendingProviderLinkCount > 0)
            {
                commands.Add(new WorkspaceCommandCenterItemResponse(
                    "team.pending_provider_links",
                    "Review pending team access",
                    $"{pendingProviderLinkCount} tenant member{PluralSuffix(pendingProviderLinkCount)} still need an identity-provider link.",
                    "pending",
                    "team",
                    "/app/team",
                    "Open Team",
                    Priority: 80,
                    RequiredPermission: PlatformPermissions.TeamManage));
            }
        }

        if (commands.Count == 0)
        {
            commands.Add(new WorkspaceCommandCenterItemResponse(
                "workspace.review",
                "Review campaign series",
                $"Workspace has {totals.CampaignSeriesCount} campaign series and {totals.SubmittedResponseCount} submitted responses.",
                "ready",
                "campaign_series",
                "/app/campaign-series",
                "Open campaign series",
                Priority: 100));
        }

        return new WorkspaceCommandCenterResponse(
            commands
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .Take(WorkspaceCommandCenterMaxItems)
                .ToArray());
    }

    private async Task<WorkspaceDirectorySummaryRow> LoadWorkspaceDirectorySummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var subjectCount = await db.Subjects
            .AsNoTracking()
            .CountAsync(
                subject => subject.TenantId == tenantId && subject.DeletedAt == null,
                cancellationToken);
        var groupCount = await db.SubjectGroups
            .AsNoTracking()
            .CountAsync(
                group => group.TenantId == tenantId && group.DeletedAt == null,
                cancellationToken);

        return new WorkspaceDirectorySummaryRow(subjectCount, groupCount);
    }

    private async Task<Dictionary<Guid, int>> LoadSeriesExportArtifactCountsAsync(
        IReadOnlyCollection<Guid> seriesIds,
        CancellationToken cancellationToken)
    {
        if (seriesIds.Count == 0)
        {
            return [];
        }

        var rows = await db.ExportArtifacts
            .AsNoTracking()
            .Where(artifact =>
                artifact.CampaignSeriesId.HasValue &&
                seriesIds.Contains(artifact.CampaignSeriesId.Value) &&
                artifact.DeletedAt == null)
            .GroupBy(artifact => artifact.CampaignSeriesId!.Value)
            .Select(group => new WorkspaceSeriesCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.CampaignSeriesId, row => row.Count);
    }

    private async Task<Dictionary<Guid, int>> LoadSeriesUnscoredSubmittedResponseCountsAsync(
        IReadOnlyCollection<Guid> seriesIds,
        CancellationToken cancellationToken)
    {
        if (seriesIds.Count == 0)
        {
            return [];
        }

        var rows = await (
                from campaign in db.Campaigns.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on campaign.Id equals assignment.CampaignId
                join session in db.ResponseSessions.AsNoTracking()
                    on assignment.Id equals session.AssignmentId
                join scoreRun in db.ScoreRuns.AsNoTracking()
                    on session.Id equals scoreRun.ResponseSessionId into scoreRuns
                where campaign.CampaignSeriesId.HasValue &&
                    seriesIds.Contains(campaign.CampaignSeriesId.Value) &&
                    session.SubmittedAt.HasValue &&
                    !scoreRuns.Any(scoreRun => scoreRun.Status == ScoreRunStatuses.Success)
                group session by campaign.CampaignSeriesId!.Value into grouping
                select new WorkspaceSeriesCountRow(grouping.Key, grouping.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.CampaignSeriesId, row => row.Count);
    }

    private async Task<Dictionary<Guid, int>> LoadSeriesLongitudinalCampaignCountsAsync(
        IReadOnlyCollection<Guid> seriesIds,
        CancellationToken cancellationToken)
    {
        if (seriesIds.Count == 0)
        {
            return [];
        }

        var rows = await db.Campaigns
            .AsNoTracking()
            .Where(campaign =>
                campaign.CampaignSeriesId.HasValue &&
                seriesIds.Contains(campaign.CampaignSeriesId.Value) &&
                campaign.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal)
            .GroupBy(campaign => campaign.CampaignSeriesId!.Value)
            .Select(group => new WorkspaceSeriesCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.CampaignSeriesId, row => row.Count);
    }

    private async Task<int> LoadPendingProviderLinkCountAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var assignedUserIds = await (
                from user in db.UserAccounts.AsNoTracking()
                join assignment in db.RoleAssignments.AsNoTracking()
                    on user.Id equals assignment.UserId
                where user.TenantId == tenantId &&
                    user.DeletedAt == null &&
                    assignment.TenantId == tenantId &&
                    assignment.ScopeType == RoleAssignmentScopes.Tenant
                select user.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (assignedUserIds.Count == 0)
        {
            return 0;
        }

        var activeIdentityUserIds = await db.ExternalAuthIdentities
            .AsNoTracking()
            .Where(identity =>
                identity.TenantId == tenantId &&
                assignedUserIds.Contains(identity.UserId) &&
                identity.DisabledAt == null)
            .Select(identity => identity.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var activeIdentityUsers = activeIdentityUserIds.ToHashSet();

        return assignedUserIds.Count(userId => !activeIdentityUsers.Contains(userId));
    }

    private static string PluralSuffix(int count)
    {
        return count == 1 ? string.Empty : "s";
    }

    private async Task<WorkspaceOverviewTotalsResponse> LoadWorkspaceTotalsAsync(
        CancellationToken cancellationToken)
    {
        var seriesCount = await db.CampaignSeries
            .AsNoTracking()
            .CountAsync(cancellationToken);
        var campaignTotals = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId.HasValue)
            .GroupBy(_ => 1)
            .Select(group => new CampaignTotalsRow(
                group.Count(),
                group.Count(campaign => campaign.Status == CampaignStatuses.Live)))
            .SingleOrDefaultAsync(cancellationToken);
        var submittedResponseCount = await (
                from campaign in db.Campaigns.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on campaign.Id equals assignment.CampaignId
                join session in db.ResponseSessions.AsNoTracking()
                    on assignment.Id equals session.AssignmentId
                where campaign.CampaignSeriesId.HasValue && session.SubmittedAt.HasValue
                select session.Id)
            .CountAsync(cancellationToken);
        var exportArtifactCount = await db.ExportArtifacts
            .AsNoTracking()
            .CountAsync(entity => entity.CampaignSeriesId.HasValue, cancellationToken);

        return new WorkspaceOverviewTotalsResponse(
            seriesCount,
            campaignTotals?.CampaignCount ?? 0,
            campaignTotals?.LiveCampaignCount ?? 0,
            submittedResponseCount,
            exportArtifactCount);
    }

    private static TenantSettingsManagementLinkResponse[] CreateTenantSettingsManagementLinks()
    {
        return
        [
            new TenantSettingsManagementLinkResponse(
                "campaign-series",
                "Campaign series",
                "Create and select tenant campaign series.",
                "/app/campaign-series"),
            new TenantSettingsManagementLinkResponse(
                "team",
                "Team",
                "Review tenant members and app-owned roles.",
                "/app/team"),
            new TenantSettingsManagementLinkResponse(
                "directory",
                "Directory",
                "Review subjects, groups, and hierarchy.",
                "/app/directory")
        ];
    }

    private static TenantSettingsReportBrandingResponse CreateTenantSettingsReportBranding(TenantSettingsTenantRow tenant)
    {
        var hasSettings =
            !string.IsNullOrWhiteSpace(tenant.ReportBrandingOrganizationLabel) ||
            !string.IsNullOrWhiteSpace(tenant.ReportBrandingReportTitle) ||
            !string.IsNullOrWhiteSpace(tenant.ReportBrandingAccentColorHex) ||
            !string.IsNullOrWhiteSpace(tenant.ReportBrandingLayoutVariant);

        return new TenantSettingsReportBrandingResponse(
            SafeTextOrDefault(tenant.ReportBrandingOrganizationLabel, tenant.TenantName),
            SafeTextOrDefault(tenant.ReportBrandingReportTitle, "Campaign series report"),
            hasSettings ? "tenant_settings" : "tenant_profile",
            "none",
            SafeAccentColorOrDefault(tenant.ReportBrandingAccentColorHex),
            SafeLayoutVariantOrDefault(tenant.ReportBrandingLayoutVariant),
            [
                "logo_upload",
                "custom_fonts",
                "product_shell_theming"
            ]);
    }

    private static string SafeTextOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string SafeAccentColorOrDefault(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return Tenant.IsReportBrandingAccentColorHex(normalized)
            ? normalized!
            : Tenant.DefaultReportBrandingAccentColorHex;
    }

    private static string SafeLayoutVariantOrDefault(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return Tenant.IsReportBrandingLayoutVariantKnown(normalized)
            ? normalized!
            : Tenant.DefaultReportBrandingLayoutVariant;
    }

    private sealed record TenantSettingsTenantRow(
        TenantSettingsProfileResponse Profile,
        string TenantName,
        string? ReportBrandingOrganizationLabel,
        string? ReportBrandingReportTitle,
        string? ReportBrandingAccentColorHex,
        string? ReportBrandingLayoutVariant);

    private async Task<CampaignSeriesListItemResponse[]> LoadSeriesListItemsAsync(
        int? take,
        CampaignSeriesPortfolioQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeSearch(query.Search);
        var seriesQuery = db.CampaignSeries.AsNoTracking();
        seriesQuery = query.Visibility switch
        {
            CampaignSeriesPortfolioVisibilities.Archived => seriesQuery
                .Where(entity => entity.ArchivedAt.HasValue),
            CampaignSeriesPortfolioVisibilities.All => seriesQuery,
            _ => seriesQuery.Where(entity => !entity.ArchivedAt.HasValue)
        };

        if (normalizedSearch is not null)
        {
            seriesQuery = seriesQuery.Where(entity => EF.Functions.ILike(entity.Name, $"%{normalizedSearch}%"));
        }

        var series = await seriesQuery
            .Select(entity => new CampaignSeriesRow(
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.StudyKind,
                entity.SampleScenario,
                entity.ArchivedAt,
                entity.ArchivedByUserId,
                entity.ArchiveReason,
                entity.StudyPurpose,
                entity.StudyAudience,
                entity.StudyDesignType,
                entity.StudyIntendedUse,
                entity.StudyInterpretationBoundary,
                entity.StudyOwnerNotes,
                entity.SetupTemplateVersionId))
            .ToListAsync(cancellationToken);

        if (series.Count == 0)
        {
            return Array.Empty<CampaignSeriesListItemResponse>();
        }

        var seriesIds = series.Select(entity => entity.Id).ToArray();
        var campaignAggregates = await db.Campaigns
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId.HasValue && seriesIds.Contains(entity.CampaignSeriesId.Value))
            .GroupBy(entity => entity.CampaignSeriesId!.Value)
            .Select(group => new SeriesCampaignAggregateRow(
                group.Key,
                group.Count(),
                group.Count(campaign => campaign.Status == CampaignStatuses.Live)))
            .ToListAsync(cancellationToken);
        var submittedAggregates = await (
                from campaign in db.Campaigns.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on campaign.Id equals assignment.CampaignId
                join session in db.ResponseSessions.AsNoTracking()
                    on assignment.Id equals session.AssignmentId
                where campaign.CampaignSeriesId.HasValue &&
                    seriesIds.Contains(campaign.CampaignSeriesId.Value) &&
                    session.SubmittedAt.HasValue
                group session by campaign.CampaignSeriesId!.Value into grouping
                select new SeriesSubmittedAggregateRow(
                    grouping.Key,
                    grouping.Count(),
                    grouping.Max(session => session.SubmittedAt)))
            .ToListAsync(cancellationToken);
        var launchAggregates = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId.HasValue && seriesIds.Contains(entity.CampaignSeriesId.Value))
            .GroupBy(entity => entity.CampaignSeriesId!.Value)
            .Select(group => new SeriesLaunchAggregateRow(
                group.Key,
                group.Max(snapshot => snapshot.LaunchedAt)))
            .ToListAsync(cancellationToken);

        var campaignsBySeries = campaignAggregates.ToDictionary(row => row.CampaignSeriesId);
        var submittedBySeries = submittedAggregates.ToDictionary(row => row.CampaignSeriesId);
        var launchesBySeries = launchAggregates.ToDictionary(row => row.CampaignSeriesId);
        var filteredItems = series
            .Select(row => CreateSeriesListItem(row, campaignsBySeries, submittedBySeries, launchesBySeries));

        if (query.Status != CampaignSeriesPortfolioStatuses.All)
        {
            filteredItems = filteredItems.Where(item => item.ReadinessStatus == query.Status);
        }

        var orderedItems = SortSeriesItems(filteredItems, query.Sort);

        var items = take.HasValue ? orderedItems.Take(take.Value) : orderedItems;

        return items.ToArray();
    }

    private static CampaignSeriesReportsWidgetManifestResponse ToReportsWidgetManifest(
        CampaignSeriesReportsWorkspaceResponse workspace,
        bool canManageSetup)
    {
        var selectedCampaignIsReportable = workspace.SelectedCampaign is not null &&
            IsReportableCampaign(workspace.SelectedCampaign);
        var widgets = new List<ReportWidgetResponse>
        {
            new(
                "results-dashboard",
                "results-dashboard/v1",
                "Results dashboard",
                "full",
                workspace.ResultsDashboard is null || !selectedCampaignIsReportable
                    ? "blocked"
                    : workspace.ResultsDashboard.OutputBars.Any(row => row.Value.HasValue)
                        ? "ready"
                        : "empty",
                workspace.ResultsDashboard is null || !selectedCampaignIsReportable
                    ? "Select a reportable measurement before reviewing the Results dashboard."
                    : workspace.ResultsDashboard.OutputBars.Any(row => row.Value.HasValue)
                        ? null
                        : "Results exist, but chart values are not visible yet.",
                workspace.ResultsDashboard is null
                    ? null
                    : new ResultsDashboardWidgetDataResponse(workspace.ResultsDashboard),
                DataSource: null,
                Actions: []),
            new(
                "report-readiness-summary",
                "report-readiness-summary/v1",
                "Report readiness",
                "half",
                workspace.Summary.MissingPrerequisiteCount == 0 ? "ready" : "blocked",
                workspace.Summary.MissingPrerequisiteCount == 0
                    ? null
                    : "Some report prerequisites still need attention.",
                new ReportReadinessWidgetDataResponse(
                    workspace.Summary.CampaignCount,
                    workspace.Summary.LiveCampaignCount,
                    workspace.Summary.ReportableCampaignCount,
                    workspace.Summary.SubmittedResponseCount,
                    workspace.Summary.ScoreCount,
                    workspace.Summary.VisibleScoreCount,
                    workspace.Summary.SuppressedScoreCount,
                    workspace.Summary.MissingPrerequisiteCount,
                    workspace.MissingPrerequisites.Select(prerequisite => new ReportWidgetPrerequisiteResponse(
                        prerequisite.Code,
                        prerequisite.Label,
                        prerequisite.Message,
                        prerequisite.Severity)).ToArray()),
                DataSource: null,
                Actions: []),
        };

        if (workspace.ScoreCoverage is not null)
        {
            widgets.Add(new ReportWidgetResponse(
                "score-coverage-summary",
                "score-coverage-summary/v1",
                "Score coverage",
                "half",
                workspace.ScoreCoverage.Status == "complete" ? "ready" : "empty",
                workspace.ScoreCoverage.Guidance,
                new ScoreCoverageWidgetDataResponse(
                    workspace.ScoreCoverage.SubmittedResponseCount,
                    workspace.ScoreCoverage.ScoredSubmittedResponseCount,
                    workspace.ScoreCoverage.UnscoredSubmittedResponseCount,
                    workspace.ScoreCoverage.NotConfiguredSubmittedResponseCount,
                    workspace.ScoreCoverage.CampaignsWithScoringRuleCount,
                    workspace.ScoreCoverage.CampaignsWithoutScoringRuleCount,
                    workspace.ScoreCoverage.LatestScoringActivityAt,
                    workspace.ScoreCoverage.Status,
                    workspace.ScoreCoverage.Guidance),
                DataSource: null,
                Actions: []));
        }

        if (workspace.SelectedCampaign is not null)
        {
            widgets.Add(new ReportWidgetResponse(
                "selected-campaign-report-state",
                "selected-campaign-report-state/v1",
                "Selected campaign report state",
                "half",
                selectedCampaignIsReportable ? "ready" : "blocked",
                selectedCampaignIsReportable
                    ? null
                    : "The selected campaign is not reportable yet.",
                new SelectedCampaignReportStateWidgetDataResponse(
                    workspace.SelectedCampaign.Id,
                    workspace.SelectedCampaign.Name,
                    workspace.SelectedCampaign.Status,
                    workspace.SelectedCampaign.ResponseIdentityMode,
                    workspace.SelectedCampaign.DefaultLocale,
                    workspace.SelectedCampaign.LatestLaunchAt,
                    workspace.SelectedCampaign.SubmittedResponseCount,
                    workspace.SelectedCampaign.ScoreCount,
                    workspace.SelectedCampaign.VisibleScoreCount,
                    workspace.SelectedCampaign.SuppressedScoreCount,
                    workspace.SelectedCampaign.DisclosureState,
                    workspace.SelectedCampaign.DisclosureKMin,
                    workspace.SelectedCampaign.ReportStatus,
                    workspace.SelectedCampaign.InterpretationStatus,
                    workspace.SelectedCampaign.LatestExportArtifactId,
                    workspace.SelectedCampaign.LatestExportArtifactFileName,
                    workspace.SelectedCampaign.LatestExportArtifactStatus,
                    workspace.SelectedCampaign.LatestExportArtifactCreatedAt,
                    workspace.SelectedCampaign.LatestExportArtifactCompletedAt,
                    workspace.SelectedCampaign.LatestExportArtifactFailedAt,
                    workspace.SelectedCampaign.LatestExportArtifactFailureReasonCode,
                    canManageSetup &&
                        selectedCampaignIsReportable &&
                        workspace.SelectedCampaign.LatestExportArtifactCanDownload,
                    workspace.SelectedCampaign.ClosedAt,
                    workspace.SelectedCampaign.DataFinality),
                selectedCampaignIsReportable
                    ? new ReportWidgetDataSourceResponse(
                        $"/campaigns/{workspace.SelectedCampaign.Id}/report-proof",
                        "GET")
                    : null,
                Actions: []));
        }

        widgets.Add(new ReportWidgetResponse(
            "export-artifact-registry",
            "export-artifact-registry/v1",
            "Export artifact registry",
            "full",
            workspace.ExportArtifacts.Count > 0 ? "ready" : "empty",
            workspace.ExportArtifacts.Count > 0 ? null : "No report export artifacts have been created yet.",
            new ExportArtifactRegistryWidgetDataResponse(
                workspace.Summary.ExportArtifactCount,
                workspace.ExportArtifacts.Select(artifact => new ExportArtifactRegistryItemResponse(
                    artifact.Id,
                    artifact.TargetKind,
                    artifact.TargetId,
                    artifact.TargetLabel,
                    artifact.CampaignId,
                    artifact.CampaignName,
                    artifact.ArtifactType,
                    artifact.Status,
                    artifact.Format,
                    artifact.FileName,
                    artifact.RowCount,
                    artifact.ByteSize,
                    artifact.ChecksumSha256,
                    artifact.CreatedAt,
                    artifact.CompletedAt,
                    artifact.StartedAt,
                    artifact.FailedAt,
                    artifact.ExpiresAt,
                    artifact.DeletedAt,
                    artifact.FailureReasonCode,
                    CanAdvertiseExportArtifactDownload(artifact, canManageSetup),
                    artifact.CampaignStatus,
                    artifact.CampaignClosedAt,
                    artifact.DataFinality,
                    CanAdvertiseExportArtifactRetry(artifact, canManageSetup))).ToArray()),
            DataSource: null,
            CreateExportActions(workspace, canManageSetup, selectedCampaignIsReportable)));

        widgets.Add(new ReportWidgetResponse(
            "visual-analytics-entry",
            "visual-analytics-entry/v1",
            "Visual analytics",
            "full",
            selectedCampaignIsReportable ? "ready" : "blocked",
            selectedCampaignIsReportable ? null : "Select a reportable campaign before opening visual analytics.",
            new VisualAnalyticsEntryWidgetDataResponse(
                workspace.SelectedCampaign?.Id,
                workspace.Summary.VisibleScoreCount,
                workspace.Summary.SuppressedScoreCount,
                workspace.Summary.ReportableCampaignCount,
                workspace.ResultsAnalytics),
            selectedCampaignIsReportable
                ? new ReportWidgetDataSourceResponse($"/campaigns/{workspace.SelectedCampaign!.Id}/report-proof", "GET")
                : null,
            Actions: []));

        widgets.Add(new ReportWidgetResponse(
            "finality-provenance-summary",
            "finality-provenance-summary/v1",
            "Finality and provenance",
            "half",
            workspace.Summary.ReportableCampaignCount > 0 ? "ready" : "empty",
            null,
            new FinalityProvenanceWidgetDataResponse(
                workspace.Summary.PreliminaryLiveReportCount,
                workspace.Summary.ClosedWaveReportCount,
                workspace.SelectedCampaign?.Id,
                workspace.SelectedCampaign?.Status,
                workspace.SelectedCampaign?.DataFinality,
                workspace.SelectedCampaign?.ClosedAt,
                workspace.SelectedCampaign?.LatestLaunchAt),
            DataSource: null,
            Actions: []));

        return new CampaignSeriesReportsWidgetManifestResponse(
            workspace.Series.Id,
            "reports",
            "reports-widget-manifest/v1",
            new ReportWidgetLayoutResponse("dashboard-grid/v1", "standard"),
            widgets);
    }

    private static bool IsReportableCampaign(CampaignSeriesReportsCampaignResponse campaign)
    {
        return campaign.ReportStatus == "proof_only" &&
            IsReportableDataFinality(campaign.DataFinality);
    }

    private static bool IsReportableDataFinality(string? dataFinality)
    {
        return dataFinality is PreliminaryLiveDataFinality or ClosedWaveDataFinality;
    }

    private static bool CanAdvertiseExportArtifactDownload(
        CampaignSeriesReportsExportArtifactResponse artifact,
        bool canManageSetup)
    {
        if (!canManageSetup || !artifact.CanDownload)
        {
            return false;
        }

        return artifact.ArtifactType switch
        {
            ExportArtifactTypes.ReportProofCsvCodebook => IsReportableDataFinality(artifact.DataFinality),
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook => true,
            ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook => true,
            ExportArtifactTypes.CampaignSeriesReportHtml => true,
            ExportArtifactTypes.CampaignSeriesReportPdf => true,
            _ => false
        };
    }

    private static bool CanAdvertiseExportArtifactRetry(
        CampaignSeriesReportsExportArtifactResponse artifact,
        bool canManageSetup)
    {
        return canManageSetup &&
            artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
            artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf &&
            artifact.Format == ExportArtifactFormats.Pdf &&
            artifact.Status == ExportArtifactStatuses.Failed;
    }

    private static IReadOnlyList<ReportWidgetActionResponse> CreateExportActions(
        CampaignSeriesReportsWorkspaceResponse workspace,
        bool canManageSetup,
        bool selectedCampaignIsReportable)
    {
        if (!canManageSetup || !selectedCampaignIsReportable || workspace.SelectedCampaign is null)
        {
            return [];
        }

        return
        [
            new ReportWidgetActionResponse(
                "create-aggregate-export",
                "Create aggregate export",
                "api-command/v1",
                $"/campaigns/{workspace.SelectedCampaign.Id}/report-proof/exports",
                "POST",
                Enabled: true,
                DisabledReason: null)
        ];
    }

    private async Task<Dictionary<Guid, int>> LoadSubmittedResponseCountsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var counts = await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where session.SubmittedAt.HasValue && campaignIds.Contains(assignment.CampaignId)
                group session by assignment.CampaignId into grouping
                select new CampaignCountRow(
                    grouping.Key,
                    grouping.Count()))
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(row => row.CampaignId, row => row.Count);
    }

    private async Task<Dictionary<Guid, CampaignLaunchAggregateRow>> LoadLaunchAggregatesByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignLaunchAggregateRow>();
        }

        var aggregates = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => campaignIds.Contains(entity.CampaignId))
            .GroupBy(entity => entity.CampaignId)
            .Select(group => new CampaignLaunchAggregateRow(
                group.Key,
                group.Max(snapshot => snapshot.LaunchedAt),
                group.Any(snapshot => snapshot.ConsentDocumentId.HasValue),
                group.Any(snapshot => snapshot.RetentionPolicyId.HasValue),
                group.Any(snapshot => snapshot.DisclosurePolicyId.HasValue)))
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(row => row.CampaignId);
    }

    private async Task<Dictionary<Guid, CampaignLaunchDetailRow>> LoadLatestLaunchDetailsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignLaunchDetailRow>();
        }

        var launches = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => campaignIds.Contains(entity.CampaignId))
            .Select(entity => new CampaignLaunchDetailRow(
                entity.CampaignId,
                entity.Id,
                entity.LaunchedAt,
                entity.TemplateVersionId,
                entity.ScoringRuleId,
                entity.ConsentDocumentId,
                entity.RetentionPolicyId,
                entity.DisclosurePolicyId,
                entity.ResponseIdentityMode,
                entity.DefaultLocale,
                entity.TemplateQuestionCount,
                entity.LaunchedBy,
                entity.LaunchPacket))
            .ToListAsync(cancellationToken);

        return launches
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.LaunchedAt).First());
    }

    private async Task<Dictionary<Guid, int>> LoadOpenLinkAssignmentCountsByCampaignAsync(
        Guid tenantId,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var counts = await (
                from assignment in db.Assignments.AsNoTracking()
                join token in db.InvitationTokens.AsNoTracking()
                    on assignment.InviteTokenId equals token.Id
                where assignment.TenantId == tenantId &&
                    token.TenantId == tenantId &&
                    campaignIds.Contains(assignment.CampaignId) &&
                    token.Channel == InvitationTokenChannels.OpenLink
                group assignment by assignment.CampaignId into grouping
                select new CampaignCountRow(
                    grouping.Key,
                    grouping.Count()))
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(row => row.CampaignId, row => row.Count);
    }

    private async Task<Dictionary<Guid, CampaignNotificationCountsRow>> LoadInvitationNotificationCountsByCampaignAsync(
        Guid tenantId,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignNotificationCountsRow>();
        }

        var counts = await db.Notifications
            .AsNoTracking()
            .Where(entity =>
                entity.TenantId == tenantId &&
                campaignIds.Contains(entity.CampaignId) &&
                entity.Channel == NotificationChannels.Email &&
                entity.TemplateCode == Notification.InvitationTemplateCode)
            .GroupBy(entity => new { entity.CampaignId, entity.Status })
            .Select(group => new CampaignStatusCountRow(
                group.Key.CampaignId,
                group.Key.Status,
                group.Count()))
            .ToListAsync(cancellationToken);

        return counts
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => new CampaignNotificationCountsRow(
                    group.Where(row => row.Status == NotificationStatuses.Queued).Sum(row => row.Count),
                    group.Where(row => row.Status == NotificationStatuses.Sent).Sum(row => row.Count),
                    group.Where(row => row.Status == NotificationStatuses.Failed).Sum(row => row.Count),
                    group.Where(row => row.Status == NotificationStatuses.Bounced).Sum(row => row.Count)));
    }

    private async Task<Dictionary<Guid, CampaignDeliveryAggregateRow>> LoadDeliveryAttemptAggregatesByCampaignAsync(
        Guid tenantId,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignDeliveryAggregateRow>();
        }

        var aggregates = await (
                from attempt in db.NotificationDeliveryAttempts.AsNoTracking()
                join notification in db.Notifications.AsNoTracking()
                    on attempt.NotificationId equals notification.Id
                where attempt.TenantId == tenantId &&
                    notification.TenantId == tenantId &&
                    campaignIds.Contains(notification.CampaignId)
                group attempt by notification.CampaignId into grouping
                select new CampaignDeliveryAggregateRow(
                    grouping.Key,
                    grouping.Count(),
                    grouping.Max(attempt => attempt.CreatedAt)))
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(row => row.CampaignId);
    }

    private async Task<Dictionary<Guid, CampaignProviderDeliveryEventAggregateRow>> LoadProviderDeliveryEventAggregatesByCampaignAsync(
        Guid tenantId,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignProviderDeliveryEventAggregateRow>();
        }

        var aggregates = await (
                from deliveryEvent in db.NotificationDeliveryEvents.AsNoTracking()
                join notification in db.Notifications.AsNoTracking()
                    on deliveryEvent.NotificationId equals notification.Id
                where deliveryEvent.TenantId == tenantId &&
                    notification.TenantId == tenantId &&
                    campaignIds.Contains(notification.CampaignId) &&
                    notification.Channel == NotificationChannels.Email &&
                    notification.TemplateCode == Notification.InvitationTemplateCode
                group deliveryEvent by notification.CampaignId into grouping
                select new CampaignProviderDeliveryEventAggregateRow(
                    grouping.Key,
                    grouping.Count(deliveryEvent => deliveryEvent.EventType == NotificationDeliveryEventTypes.Accepted),
                    grouping.Count(deliveryEvent => deliveryEvent.EventType == NotificationDeliveryEventTypes.Delivered),
                    grouping.Count(deliveryEvent => deliveryEvent.EventType == NotificationDeliveryEventTypes.Bounced),
                    grouping.Count(deliveryEvent => deliveryEvent.EventType == NotificationDeliveryEventTypes.Complained),
                    grouping.Max(deliveryEvent => deliveryEvent.ReceivedAt)))
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(row => row.CampaignId);
    }

    private async Task<Dictionary<Guid, CampaignCollectionAggregateRow>> LoadCollectionAggregatesByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignCollectionAggregateRow>();
        }

        var aggregates = await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where campaignIds.Contains(assignment.CampaignId)
                group session by assignment.CampaignId into grouping
                select new CampaignCollectionAggregateRow(
                    grouping.Key,
                    grouping.Count(),
                    grouping.Count(session => session.SubmittedAt.HasValue),
                    grouping.Max(session => session.StartedAt),
                    grouping.Max(session => session.SubmittedAt)))
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(row => row.CampaignId);
    }

    private async Task<Dictionary<Guid, Guid>> LoadCurrentScoringRuleIdsByTemplateVersionAsync(
        Guid[] templateVersionIds,
        CancellationToken cancellationToken)
    {
        if (templateVersionIds.Length == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var rules = await db.ScoringRules
            .AsNoTracking()
            .Where(rule =>
                templateVersionIds.Contains(rule.TemplateVersionId) &&
                (rule.Status == ScoringRuleStatuses.Draft ||
                    rule.Status == ScoringRuleStatuses.Published))
            .Select(rule => new TemplateScoringRuleRow(
                rule.TemplateVersionId,
                rule.Id,
                rule.Status,
                rule.PublishedAt,
                rule.UpdatedAt))
            .ToListAsync(cancellationToken);

        return rules
            .GroupBy(rule => rule.TemplateVersionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(rule => rule.Status == ScoringRuleStatuses.Published)
                    .ThenByDescending(rule => rule.PublishedAt ?? rule.UpdatedAt)
                    .First()
                    .ScoringRuleId);
    }

    private async Task<Dictionary<Guid, CampaignScoreCoverageAggregateRow>> LoadScoreCoverageAggregatesByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignScoreCoverageAggregateRow>();
        }

        var scoredSessions = await (
                from run in db.ScoreRuns.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on run.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where run.Status == ScoreRunStatuses.Success &&
                    run.CampaignId == assignment.CampaignId &&
                    session.SubmittedAt.HasValue &&
                    campaignIds.Contains(assignment.CampaignId)
                select new CampaignScoredSessionRow(
                    assignment.CampaignId,
                    session.Id,
                    run.RanAt))
            .ToListAsync(cancellationToken);

        return scoredSessions
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => new CampaignScoreCoverageAggregateRow(
                    group.Key,
                    group.Select(row => row.ResponseSessionId).Distinct().Count(),
                    group.Max(row => row.RanAt)));
    }

    private async Task<Dictionary<Guid, int>> LoadScoreCountsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var counts = await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where campaignIds.Contains(score.CampaignId) &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                select new CampaignScoreOutputObservationRow(
                    score.Id,
                    score.CampaignId,
                    score.ResponseSessionId,
                    score.DimensionCode,
                    score.ComputedAt))
            .ToListAsync(cancellationToken);

        var latestCounts = counts
            .GroupBy(row => new { row.CampaignId, row.ResponseSessionId, row.DimensionCode })
            .Select(group => group
                .OrderByDescending(row => row.ComputedAt)
                .ThenByDescending(row => row.ScoreId)
                .First())
            .GroupBy(score => score.CampaignId)
            .Select(group => new CampaignCountRow(
                group.Key,
                group.Count()));

        return latestCounts.ToDictionary(row => row.CampaignId, row => row.Count);
    }

    private async Task<Dictionary<Guid, CampaignScoreOutputCountsRow>> LoadScoreOutputCountsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignScoreOutputCountsRow>();
        }

        var scoreRows = await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where campaignIds.Contains(score.CampaignId) &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                select new CampaignScoreOutputObservationRow(
                    score.Id,
                    score.CampaignId,
                    score.ResponseSessionId,
                    score.DimensionCode,
                    score.ComputedAt))
            .ToListAsync(cancellationToken);

        var latestScoreRows = scoreRows
            .GroupBy(row => new { row.CampaignId, row.ResponseSessionId, row.DimensionCode })
            .Select(group => group
                .OrderByDescending(row => row.ComputedAt)
                .ThenByDescending(row => row.ScoreId)
                .First())
            .GroupBy(row => new { row.CampaignId, row.DimensionCode })
            .Select(group => new CampaignScoreOutputCountRow(
                group.Key.CampaignId,
                group.Key.DimensionCode,
                group.Count()));

        return latestScoreRows
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => new CampaignScoreOutputCountsRow(
                    group.Key,
                    group.Sum(row => row.ScoreCount),
                    group.Select(row => row.ScoreCount).ToArray()));
    }

    private async Task<Dictionary<Guid, int>> LoadExportArtifactCountsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var counts = await db.ExportArtifacts
            .AsNoTracking()
            .Where(entity => entity.CampaignId.HasValue && campaignIds.Contains(entity.CampaignId.Value))
            .GroupBy(entity => entity.CampaignId!.Value)
            .Select(group => new CampaignCountRow(
                group.Key,
                group.Count()))
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(row => row.CampaignId, row => row.Count);
    }

    private async Task<Dictionary<Guid, CampaignReportLaunchDetailRow>> LoadReportLaunchDetailsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignReportLaunchDetailRow>();
        }

        var launches = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => campaignIds.Contains(entity.CampaignId))
            .Select(entity => new CampaignReportLaunchDetailRow(
                entity.CampaignId,
                entity.Id,
                entity.LaunchedAt,
                entity.ScoringRuleId,
                entity.ConsentDocumentId,
                entity.RetentionPolicyId,
                entity.DisclosurePolicyId,
                entity.ResponseIdentityMode,
                entity.LaunchPacket))
            .ToListAsync(cancellationToken);

        return launches
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.LaunchedAt).First());
    }

    private async Task<Dictionary<Guid, CampaignReportDisclosurePolicyRow>> LoadReportDisclosurePoliciesAsync(
        Guid[] disclosurePolicyIds,
        CancellationToken cancellationToken)
    {
        if (disclosurePolicyIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignReportDisclosurePolicyRow>();
        }

        var policies = await db.DisclosurePolicies
            .AsNoTracking()
            .Where(entity => disclosurePolicyIds.Contains(entity.Id))
            .Select(entity => new CampaignReportDisclosurePolicyRow(
                entity.Id,
                entity.KMin))
            .ToListAsync(cancellationToken);

        return policies.ToDictionary(row => row.Id);
    }

    private async Task<Dictionary<Guid, CampaignWaveLaunchDetailRow>> LoadWaveLaunchDetailsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignWaveLaunchDetailRow>();
        }

        var launches = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => campaignIds.Contains(entity.CampaignId))
            .Select(entity => new CampaignWaveLaunchDetailRow(
                entity.CampaignId,
                entity.Id,
                entity.LaunchedAt,
                entity.ScoringRuleId,
                entity.DisclosurePolicyId,
                entity.ResponseIdentityMode,
                entity.LaunchPacket))
            .ToListAsync(cancellationToken);

        return launches
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.LaunchedAt).First());
    }

    private async Task<Dictionary<Guid, CampaignWaveScoringRuleRow>> LoadWaveScoringRulesAsync(
        Guid[] scoringRuleIds,
        CancellationToken cancellationToken)
    {
        if (scoringRuleIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignWaveScoringRuleRow>();
        }

        var rules = await db.ScoringRules
            .AsNoTracking()
            .Where(entity => scoringRuleIds.Contains(entity.Id))
            .Select(entity => new CampaignWaveScoringRuleRow(
                entity.Id,
                entity.RuleKey,
                entity.RuleVersion))
            .ToListAsync(cancellationToken);

        return rules.ToDictionary(row => row.Id);
    }

    private async Task<WaveSubmittedTrajectoryRow[]> LoadWaveSubmittedTrajectoriesAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return Array.Empty<WaveSubmittedTrajectoryRow>();
        }

        return await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where session.SubmittedAt.HasValue &&
                    session.ParticipantCodeId.HasValue &&
                    campaignIds.Contains(assignment.CampaignId)
                select new WaveSubmittedTrajectoryRow(
                    session.Id,
                    assignment.CampaignId,
                    session.ParticipantCodeId!.Value))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<WaveScoreDimensionRow[]> LoadWaveScoreDimensionsAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return Array.Empty<WaveScoreDimensionRow>();
        }

        return await (
                from score in db.Scores.AsNoTracking()
                join session in db.ResponseSessions.AsNoTracking()
                    on score.ResponseSessionId equals session.Id
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                where campaignIds.Contains(score.CampaignId) &&
                    assignment.CampaignId == score.CampaignId &&
                    session.SubmittedAt.HasValue
                select new WaveScoreDimensionRow(
                    score.ResponseSessionId,
                    score.CampaignId,
                    score.DimensionCode))
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, CampaignReportExportArtifactRow>> LoadLatestExportArtifactsByCampaignAsync(
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return new Dictionary<Guid, CampaignReportExportArtifactRow>();
        }

        var artifacts = await db.ExportArtifacts
            .AsNoTracking()
            .Where(entity =>
                entity.CampaignId.HasValue &&
                campaignIds.Contains(entity.CampaignId.Value) &&
                (entity.ArtifactType == ExportArtifactTypes.ReportProofCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesReportHtml))
            .Select(entity => new CampaignReportExportArtifactRow(
                entity.CampaignId!.Value,
                entity.Id,
                entity.FileName,
                entity.Status,
                entity.CreatedAt,
                entity.CompletedAt,
                entity.StartedAt,
                entity.FailedAt,
                entity.ExpiresAt,
                entity.DeletedAt,
                entity.FailureReasonCode,
                entity.Status == ExportArtifactStatuses.Succeeded))
            .ToListAsync(cancellationToken);

        return artifacts
            .GroupBy(row => row.CampaignId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.CreatedAt).First());
    }

    private async Task<int> LoadReportExportArtifactCountAsync(
        Guid campaignSeriesId,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return 0;
        }

        return await db.ExportArtifacts
            .AsNoTracking()
            .CountAsync(entity =>
                ((entity.TargetKind == ExportArtifactTargetKinds.Campaign &&
                    entity.CampaignId.HasValue &&
                    campaignIds.Contains(entity.CampaignId.Value)) ||
                    (entity.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                        entity.CampaignSeriesId == campaignSeriesId)) &&
                (entity.ArtifactType == ExportArtifactTypes.ReportProofCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesReportHtml ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf),
                cancellationToken);
    }

    private async Task<CampaignSeriesReportsExportArtifactResponse[]> LoadReportExportArtifactRegistryAsync(
        Guid campaignSeriesId,
        string campaignSeriesName,
        IReadOnlyDictionary<Guid, CampaignSeriesReportsCampaignResponse> campaignsById,
        Guid[] campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Length == 0)
        {
            return Array.Empty<CampaignSeriesReportsExportArtifactResponse>();
        }

        var artifacts = await db.ExportArtifacts
            .AsNoTracking()
            .Where(entity =>
                ((entity.TargetKind == ExportArtifactTargetKinds.Campaign &&
                    entity.CampaignId.HasValue &&
                    campaignIds.Contains(entity.CampaignId.Value)) ||
                    (entity.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                        entity.CampaignSeriesId == campaignSeriesId)) &&
                (entity.ArtifactType == ExportArtifactTypes.ReportProofCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesReportHtml ||
                    entity.ArtifactType == ExportArtifactTypes.CampaignSeriesReportPdf))
            .OrderByDescending(entity => entity.CreatedAt)
            .ThenBy(entity => entity.Id)
            .Select(entity => new CampaignReportExportArtifactRegistryRow(
                entity.Id,
                entity.TargetKind,
                entity.CampaignId,
                entity.CampaignSeriesId,
                entity.ArtifactType,
                entity.Status,
                entity.Format,
                entity.FileName,
                entity.RowCount,
                entity.ByteSize,
                entity.ChecksumSha256,
                entity.CreatedAt,
                entity.CompletedAt,
                entity.StartedAt,
                entity.FailedAt,
                entity.ExpiresAt,
                entity.DeletedAt,
                entity.FailureReasonCode,
                entity.Status == ExportArtifactStatuses.Succeeded &&
                    entity.ChecksumSha256 != null &&
                    (entity.StorageKind == ExportArtifactStorageKinds.InlineText ||
                        (entity.StorageKind == ExportArtifactStorageKinds.ExternalObject && entity.StorageKey != null))))
            .ToListAsync(cancellationToken);

        return artifacts
            .Select(artifact =>
            {
                var targetIsCampaign = artifact.TargetKind == ExportArtifactTargetKinds.Campaign;
                var targetId = targetIsCampaign
                    ? artifact.CampaignId!.Value
                    : artifact.CampaignSeriesId!.Value;
                var campaign = targetIsCampaign
                    ? campaignsById.GetValueOrDefault(artifact.CampaignId!.Value)
                    : null;
                var targetLabel = targetIsCampaign
                    ? campaign?.Name ?? "Unknown campaign"
                    : campaignSeriesName;

                return new CampaignSeriesReportsExportArtifactResponse(
                    artifact.Id,
                    artifact.TargetKind,
                    targetId,
                    targetLabel,
                    artifact.CampaignId,
                    targetIsCampaign ? targetLabel : null,
                    artifact.ArtifactType,
                    artifact.Status,
                    artifact.Format,
                    artifact.FileName,
                    artifact.RowCount,
                    artifact.ByteSize,
                    artifact.ChecksumSha256,
                    artifact.CreatedAt,
                    artifact.CompletedAt,
                    artifact.StartedAt,
                    artifact.FailedAt,
                    artifact.ExpiresAt,
                    artifact.DeletedAt,
                    artifact.FailureReasonCode,
                    artifact.CanDownload,
                    campaign?.Status,
                    campaign?.ClosedAt,
                    campaign?.DataFinality);
            })
            .ToArray();
    }

    private async Task<TemplateSetupRow?> LoadSetupTemplateAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        return await (
                from version in db.TemplateVersions.AsNoTracking()
                join template in db.SurveyTemplates.AsNoTracking()
                    on version.TemplateId equals template.Id
                where version.Id == templateVersionId
                select new TemplateSetupRow(
                    template.Id,
                    version.Id,
                    template.Name,
                    version.Semver,
                    version.Status,
                    version.DefaultLocale,
                    version.InstrumentId,
                    db.TemplateQuestions
                        .AsNoTracking()
                        .Count(question => question.TemplateVersionId == version.Id)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ScoringSetupRow?> LoadSetupScoringAsync(
        Guid campaignId,
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        var templateVersionRule = await LoadSetupTemplateVersionScoringAsync(
            templateVersionId,
            cancellationToken);

        if (templateVersionRule is not null)
        {
            return templateVersionRule;
        }

        return await (
                from snapshot in db.CampaignLaunchSnapshots.AsNoTracking()
                join rule in db.ScoringRules.AsNoTracking()
                    on snapshot.ScoringRuleId equals rule.Id
                where snapshot.CampaignId == campaignId
                orderby snapshot.LaunchedAt descending
                select new ScoringSetupRow(
                    rule.Id,
                    rule.TemplateVersionId,
                    rule.RuleKey,
                    rule.RuleVersion,
                    rule.Status,
                    "launch_snapshot"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<ScoringSetupRow?> LoadSetupTemplateVersionScoringAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        return db.ScoringRules
            .AsNoTracking()
            .Where(entity =>
                entity.TemplateVersionId == templateVersionId &&
                entity.Status != ScoringRuleStatuses.Retired)
            .OrderByDescending(entity => entity.Status == ScoringRuleStatuses.Published)
            .ThenByDescending(entity => entity.UpdatedAt)
            .Select(entity => new ScoringSetupRow(
                entity.Id,
                entity.TemplateVersionId,
                entity.RuleKey,
                entity.RuleVersion,
                entity.Status,
                "template_version"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ConsentPolicySetupRow?> LoadLatestConsentPolicyAsync(
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return await db.ConsentDocuments
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId && entity.RetiredAt == null)
            .OrderByDescending(entity => entity.PublishedAt)
            .Select(entity => new ConsentPolicySetupRow(
                entity.Id,
                entity.Version,
                entity.Locale,
                entity.Title,
                entity.RequiredGrants,
                entity.OptionalGrants,
                entity.PublishedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<RetentionPolicySetupRow?> LoadLatestRetentionPolicyAsync(
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return await db.RetentionPolicies
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId && entity.RetiredAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new RetentionPolicySetupRow(
                entity.Id,
                entity.Version,
                entity.RetainForYears,
                entity.RetentionStartEvent,
                entity.ActionAfter,
                entity.NextReviewAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<DisclosurePolicySetupRow?> LoadLatestDisclosurePolicyAsync(
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        return await db.DisclosurePolicies
            .AsNoTracking()
            .Where(entity => entity.CampaignSeriesId == campaignSeriesId && entity.RetiredAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new DisclosurePolicySetupRow(
                entity.Id,
                entity.Version,
                entity.KMin,
                entity.SuppressionStrategy,
                entity.AppliesToDimensions))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static CampaignSeriesListItemResponse CreateSeriesListItem(
        CampaignSeriesRow series,
        IReadOnlyDictionary<Guid, SeriesCampaignAggregateRow> campaignsBySeries,
        IReadOnlyDictionary<Guid, SeriesSubmittedAggregateRow> submittedBySeries,
        IReadOnlyDictionary<Guid, SeriesLaunchAggregateRow> launchesBySeries)
    {
        campaignsBySeries.TryGetValue(series.Id, out var campaignAggregate);
        submittedBySeries.TryGetValue(series.Id, out var submittedAggregate);
        launchesBySeries.TryGetValue(series.Id, out var launchAggregate);

        var campaignCount = campaignAggregate?.CampaignCount ?? 0;
        var submittedResponseCount = submittedAggregate?.SubmittedResponseCount ?? 0;

        return new CampaignSeriesListItemResponse(
            series.Id,
            series.Name,
            series.CreatedAt,
            series.UpdatedAt,
            campaignCount,
            campaignAggregate?.LiveCampaignCount ?? 0,
            submittedResponseCount,
            launchAggregate?.LatestLaunchAt,
            submittedAggregate?.LatestSubmissionAt,
            DetermineReadiness(campaignCount, submittedResponseCount),
            series.ArchivedAt.HasValue,
            series.ArchivedAt,
            series.ArchivedByUserId,
            series.ArchiveReason,
            series.StudyKind,
            IsSampleSeries(series),
            series.SampleScenario,
            GetReadOnlyReason(series),
            CreateStudyBriefResponse(series));
    }

    private static DateTimeOffset? LatestActivity(CampaignSeriesListItemResponse item)
    {
        return Max(item.LatestSubmissionAt, item.LatestLaunchAt);
    }

    private static bool IsSampleSeries(CampaignSeriesRow series)
    {
        return series.StudyKind == CampaignSeriesStudyKinds.Sample;
    }

    private static string? GetReadOnlyReason(CampaignSeriesRow series)
    {
        return IsSampleSeries(series)
            ? CampaignSeriesReadOnlyReasons.SampleStudy
            : null;
    }

    private static CampaignSeriesStudyBriefResponse? CreateStudyBriefResponse(CampaignSeriesRow series)
    {
        if (string.IsNullOrWhiteSpace(series.StudyPurpose) &&
            string.IsNullOrWhiteSpace(series.StudyAudience) &&
            string.IsNullOrWhiteSpace(series.StudyDesignType) &&
            string.IsNullOrWhiteSpace(series.StudyIntendedUse) &&
            string.IsNullOrWhiteSpace(series.StudyInterpretationBoundary) &&
            string.IsNullOrWhiteSpace(series.StudyOwnerNotes))
        {
            return null;
        }

        return new CampaignSeriesStudyBriefResponse(
            series.StudyPurpose,
            series.StudyAudience,
            series.StudyDesignType,
            series.StudyIntendedUse,
            series.StudyInterpretationBoundary,
            series.StudyOwnerNotes);
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static IOrderedEnumerable<CampaignSeriesListItemResponse> SortSeriesItems(
        IEnumerable<CampaignSeriesListItemResponse> items,
        string sort)
    {
        return sort switch
        {
            CampaignSeriesPortfolioSorts.UpdatedDesc => items
                .OrderByDescending(item => item.UpdatedAt)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            CampaignSeriesPortfolioSorts.CreatedDesc => items
                .OrderByDescending(item => item.CreatedAt)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            CampaignSeriesPortfolioSorts.NameAsc => items
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => LatestActivity(item)),
            _ => items
                .OrderByDescending(item => LatestActivity(item))
                .ThenByDescending(item => item.UpdatedAt)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static DateTimeOffset? Max(DateTimeOffset? first, DateTimeOffset? second)
    {
        if (!first.HasValue)
        {
            return second;
        }

        if (!second.HasValue)
        {
            return first;
        }

        return first.Value >= second.Value ? first : second;
    }

    private static string DetermineReadiness(int campaignCount, int submittedResponseCount)
    {
        if (campaignCount == 0)
        {
            return "not_configured";
        }

        return submittedResponseCount == 0 ? "pending" : "proof_only";
    }

    private static string DetermineConfiguredStatus(bool configured)
    {
        return configured ? "proof_only" : "not_configured";
    }

    private static CampaignSeriesLifecycleItemResponse[] CreateCampaignSeriesLifecycle(
        IReadOnlyCollection<CampaignRow> campaigns,
        CampaignSeriesGovernanceSummaryResponse governance,
        IReadOnlyDictionary<Guid, int> submittedCounts,
        IReadOnlyDictionary<Guid, int> exportArtifactCounts,
        IReadOnlyCollection<WaveSubmittedTrajectoryRow> trajectoryRows)
    {
        var hasLiveCampaign = campaigns.Any(campaign => campaign.Status == CampaignStatuses.Live);
        var submittedResponseCount = submittedCounts.Values.Sum();
        var exportArtifactCount = exportArtifactCounts.Values.Sum();
        var hasAnyLongitudinalWave = campaigns.Any(campaign =>
            campaign.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal);
        var liveLongitudinalWaveCount = campaigns.Count(campaign =>
            campaign.Status == CampaignStatuses.Live &&
            campaign.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal);
        var hasCompleteTrajectories = CountCompleteTrajectories(trajectoryRows) > 0;
        var setupReady = IsSetupReady(governance);

        return
        [
            CreateLifecycleItem(
                "setup",
                "Setup",
                setupReady ? "ready" : "blocked",
                setupReady
                    ? "Governance prerequisites are configured for this series."
                    : "Finish consent, retention, disclosure, and scoring setup before collection.",
                "setup",
                "Review setup"),
            CreateLifecycleItem(
                "operations",
                "Operations",
                !hasLiveCampaign ? "blocked" : submittedResponseCount > 0 ? "ready" : "pending",
                !hasLiveCampaign
                    ? "Launch a campaign before monitoring collection."
                    : submittedResponseCount > 0
                        ? "Collection has submitted responses to monitor."
                        : "Collection is live, but no submitted responses are available yet.",
                "operations",
                "Open operations"),
            CreateLifecycleItem(
                "reports",
                "Reports",
                submittedResponseCount == 0 ? "pending" : exportArtifactCount > 0 ? "ready" : "proof_only",
                submittedResponseCount == 0
                    ? "Collect submitted responses before reviewing report proof."
                    : exportArtifactCount > 0
                        ? "Report proof and export artifacts are available for review."
                        : "Report proof can be reviewed; create an export artifact before handoff.",
                "reports",
                "Open reports"),
            CreateLifecycleItem(
                "waves",
                "Waves",
                !hasAnyLongitudinalWave
                    ? "not_available"
                    : liveLongitudinalWaveCount >= 2 && hasCompleteTrajectories
                        ? "ready"
                        : "pending",
                !hasAnyLongitudinalWave
                    ? "Use anonymous longitudinal campaign identity when this series needs wave comparison."
                    : liveLongitudinalWaveCount >= 2 && hasCompleteTrajectories
                        ? "Linked trajectories exist across live longitudinal waves."
                        : "Collect at least two live longitudinal waves with linked submitted trajectories.",
                "waves",
                "Review waves")
        ];
    }

    private static bool IsSetupReady(CampaignSeriesGovernanceSummaryResponse governance)
    {
        return governance.ConsentStatus == "proof_only" &&
            governance.RetentionStatus == "proof_only" &&
            governance.DisclosureStatus == "proof_only" &&
            governance.ScoringStatus == "proof_only";
    }

    private static CampaignSeriesLifecycleItemResponse CreateLifecycleItem(
        string id,
        string label,
        string status,
        string guidance,
        string route,
        string actionLabel)
    {
        return new CampaignSeriesLifecycleItemResponse(
            id,
            label,
            status,
            guidance,
            route,
            actionLabel);
    }

    private static CampaignSeriesSetupCampaignResponse CreateSetupCampaignResponse(
        CampaignRow campaign,
        DateTimeOffset? latestLaunchAt)
    {
        return new CampaignSeriesSetupCampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            campaign.TemplateVersionId,
            latestLaunchAt);
    }

    private static ScoreCoverageCampaignInput CreateScoreCoverageInput(
        CampaignRow campaign,
        CampaignLaunchDetailRow? launchDetail,
        IReadOnlyDictionary<Guid, Guid> scoringRuleIdsByTemplate,
        int submittedResponseCount,
        CampaignScoreCoverageAggregateRow? scoreCoverageAggregate)
    {
        return CreateScoreCoverageInput(
            campaign,
            ResolveScoringRuleId(campaign, launchDetail?.ScoringRuleId, scoringRuleIdsByTemplate),
            submittedResponseCount,
            scoreCoverageAggregate);
    }

    private static ScoreCoverageCampaignInput CreateScoreCoverageInput(
        CampaignRow campaign,
        CampaignReportLaunchDetailRow? launchDetail,
        IReadOnlyDictionary<Guid, Guid> scoringRuleIdsByTemplate,
        int submittedResponseCount,
        CampaignScoreCoverageAggregateRow? scoreCoverageAggregate)
    {
        return CreateScoreCoverageInput(
            campaign,
            ResolveScoringRuleId(campaign, launchDetail?.ScoringRuleId, scoringRuleIdsByTemplate),
            submittedResponseCount,
            scoreCoverageAggregate);
    }

    private static ScoreCoverageCampaignInput CreateScoreCoverageInput(
        CampaignRow campaign,
        Guid? scoringRuleId,
        int submittedResponseCount,
        CampaignScoreCoverageAggregateRow? scoreCoverageAggregate)
    {
        return new ScoreCoverageCampaignInput(
            campaign.Id,
            scoringRuleId,
            submittedResponseCount,
            scoreCoverageAggregate?.ScoredSubmittedResponseCount ?? 0,
            scoreCoverageAggregate?.LatestScoringActivityAt);
    }

    private static Guid? ResolveScoringRuleId(
        CampaignRow campaign,
        Guid? launchScoringRuleId,
        IReadOnlyDictionary<Guid, Guid> scoringRuleIdsByTemplate)
    {
        if (launchScoringRuleId.HasValue)
        {
            return launchScoringRuleId.Value;
        }

        return scoringRuleIdsByTemplate.TryGetValue(campaign.TemplateVersionId, out var scoringRuleId)
            ? scoringRuleId
            : null;
    }

    private async Task<IReadOnlyDictionary<Guid, int>> LoadTargetAwareAssignmentCountsByCampaignAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> campaignIds,
        CancellationToken cancellationToken)
    {
        if (campaignIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await db.Assignments
            .AsNoTracking()
            .Where(entity =>
                entity.TenantId == tenantId &&
                campaignIds.Contains(entity.CampaignId) &&
                !entity.Anonymous &&
                entity.RespondentSubjectId != null &&
                entity.TargetSubjectId != null)
            .GroupBy(entity => entity.CampaignId)
            .Select(group => new
            {
                CampaignId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                entity => entity.CampaignId,
                entity => entity.Count,
                cancellationToken);
    }

    private static CampaignSeriesOperationsCampaignResponse CreateOperationsCampaignResponse(
        CampaignRow campaign,
        CampaignLaunchDetailRow? launchDetail,
        CampaignCollectionAggregateRow? collectionAggregate,
        CampaignReportDisclosurePolicyRow? disclosurePolicy,
        ScoreCoverageCampaignInput scoreCoverageInput,
        int openLinkAssignmentCount,
        int targetAwareAssignmentCount,
        CampaignNotificationCountsRow? notificationCounts,
        CampaignDeliveryAggregateRow? deliveryAggregate,
        CampaignProviderDeliveryEventAggregateRow? providerEventAggregate)
    {
        var startedResponseCount = collectionAggregate?.StartedResponseCount ?? 0;
        var submittedResponseCount = collectionAggregate?.SubmittedResponseCount ?? 0;
        var draftResponseCount = Math.Max(0, startedResponseCount - submittedResponseCount);
        var collectionStatus = DetermineCollectionStatus(
            campaign.Status,
            startedResponseCount,
            draftResponseCount,
            submittedResponseCount);
        var reportVisibilityStatus = DetermineReportVisibilityStatus(submittedResponseCount, disclosurePolicy);
        var scoreCoverage = ScoreCoverageSummary.Create([scoreCoverageInput]);

        return new CampaignSeriesOperationsCampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            launchDetail?.LaunchSnapshotId,
            launchDetail?.LaunchedAt,
            submittedResponseCount,
            startedResponseCount,
            draftResponseCount,
            collectionAggregate?.LatestStartedAt,
            collectionAggregate?.LatestSubmittedAt,
            collectionStatus,
            reportVisibilityStatus,
            CreateCollectionGuidance(collectionStatus, reportVisibilityStatus),
            openLinkAssignmentCount,
            notificationCounts?.QueuedCount ?? 0,
            notificationCounts?.SentCount ?? 0,
            notificationCounts?.FailedCount ?? 0,
            deliveryAggregate?.AttemptCount ?? 0,
            deliveryAggregate?.LatestAttemptAt,
            campaign.ClosedAt,
            campaign.ClosedByUserId,
            campaign.CloseReason,
            scoreCoverageInput.ScoringRuleId,
            scoreCoverage.ScoredSubmittedResponseCount,
            scoreCoverage.UnscoredSubmittedResponseCount,
            scoreCoverage.NotConfiguredSubmittedResponseCount,
            scoreCoverage.LatestScoringActivityAt,
            scoreCoverage.Status,
            CreateOperationsLaunchSnapshotResponse(launchDetail),
            notificationCounts?.BouncedCount ?? 0,
            providerEventAggregate?.AcceptedCount ?? 0,
            providerEventAggregate?.DeliveredCount ?? 0,
            providerEventAggregate?.BouncedCount ?? 0,
            providerEventAggregate?.ComplainedCount ?? 0,
            providerEventAggregate?.LatestEventAt,
            targetAwareAssignmentCount);
    }

    private static CampaignSeriesOperationsLaunchSnapshotResponse? CreateOperationsLaunchSnapshotResponse(
        CampaignLaunchDetailRow? launchDetail)
    {
        var launchPacket = launchDetail is null
            ? null
            : Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection.FromJson(launchDetail.LaunchPacket);
        return launchDetail is null
            ? null
            : new CampaignSeriesOperationsLaunchSnapshotResponse(
                launchDetail.LaunchSnapshotId,
                launchDetail.TemplateVersionId,
                launchDetail.ScoringRuleId,
                launchDetail.ConsentDocumentId,
                launchDetail.RetentionPolicyId,
                launchDetail.DisclosurePolicyId,
                launchDetail.ResponseIdentityMode,
                launchDetail.DefaultLocale,
                launchDetail.TemplateQuestionCount,
                launchDetail.LaunchedAt,
                launchDetail.LaunchedByUserId,
                new ProductSurfaceLaunchPacketProvenanceResponse(
                    launchPacket!.SchemaVersion,
                    launchPacket.Sections,
                    launchPacket.Source));
    }

    private static string DetermineCollectionStatus(
        string campaignStatus,
        int startedResponseCount,
        int draftResponseCount,
        int submittedResponseCount)
    {
        if (startedResponseCount == 0)
        {
            return campaignStatus == CampaignStatuses.Live ? "not_started" : "closed_or_inactive";
        }

        if (submittedResponseCount > 0)
        {
            return "has_submissions";
        }

        return draftResponseCount > 0 ? "collecting" : "not_started";
    }

    private static string DetermineReportVisibilityStatus(
        int submittedResponseCount,
        CampaignReportDisclosurePolicyRow? disclosurePolicy)
    {
        if (disclosurePolicy is null)
        {
            return "unknown_policy";
        }

        if (submittedResponseCount == 0)
        {
            return "not_ready";
        }

        return submittedResponseCount >= disclosurePolicy.KMin
            ? "ready_for_aggregate_report"
            : "below_disclosure_minimum";
    }

    private static string CreateCollectionGuidance(
        string collectionStatus,
        string reportVisibilityStatus)
    {
        if (collectionStatus == "closed_or_inactive")
        {
            return "Collection is closed or inactive; existing submitted data remains reportable when disclosure policy allows.";
        }

        return reportVisibilityStatus switch
        {
            "unknown_policy" => "Report visibility readiness is unknown because disclosure policy is missing.",
            "ready_for_aggregate_report" => "Enough submitted responses exist for aggregate report visibility.",
            "below_disclosure_minimum" => "Collect more responses before aggregate report values can show.",
            "not_ready" when collectionStatus == "collecting" => "Responses have started but none are submitted yet.",
            _ => "Share the public link or send invitations."
        };
    }

    private static string DetermineSeriesCollectionStatus(
        IReadOnlyCollection<CampaignSeriesOperationsCampaignResponse> campaigns)
    {
        if (campaigns.Any(campaign => campaign.SubmittedResponseCount > 0))
        {
            return "has_submissions";
        }

        if (campaigns.Any(campaign => campaign.DraftResponseCount > 0))
        {
            return "collecting";
        }

        return campaigns.Any(campaign => campaign.Status == CampaignStatuses.Live)
            ? "not_started"
            : "closed_or_inactive";
    }

    private static string DetermineSeriesReportVisibilityStatus(
        IReadOnlyCollection<CampaignSeriesOperationsCampaignResponse> campaigns)
    {
        if (campaigns.Any(campaign => campaign.ReportVisibilityStatus == "ready_for_aggregate_report"))
        {
            return "ready_for_aggregate_report";
        }

        if (campaigns.Any(campaign => campaign.ReportVisibilityStatus == "below_disclosure_minimum"))
        {
            return "below_disclosure_minimum";
        }

        if (campaigns.Any(campaign => campaign.ReportVisibilityStatus == "not_ready"))
        {
            return "not_ready";
        }

        return "unknown_policy";
    }

    private static DateTimeOffset? MaxNullableDateTimeOffset(IEnumerable<DateTimeOffset?> values)
    {
        var concreteValues = values
            .OfType<DateTimeOffset>()
            .ToArray();

        return concreteValues.Length == 0 ? null : concreteValues.Max();
    }

    private static CampaignRow? SelectOperationsCampaign(IReadOnlyList<CampaignRow> campaigns)
    {
        return campaigns
            .OrderByDescending(campaign => campaign.Status == CampaignStatuses.Live)
            .ThenByDescending(campaign => campaign.Status == CampaignStatuses.Scheduled)
            .ThenByDescending(campaign => campaign.Status == CampaignStatuses.Draft)
            .ThenByDescending(campaign => campaign.UpdatedAt)
            .FirstOrDefault();
    }

    private static CampaignSeriesSetupPolicyResponse CreateMissingPolicyResponse()
    {
        return new CampaignSeriesSetupPolicyResponse(null, null, "not_configured");
    }

    private static CampaignSeriesSetupPolicyResponse CreateConsentPolicyResponse(
        ConsentPolicySetupRow? policy)
    {
        if (policy is null)
        {
            return CreateMissingPolicyResponse();
        }

        return new CampaignSeriesSetupPolicyResponse(policy.Id, policy.Version, "configured")
        {
            Details =
            [
                new CampaignSeriesSetupPolicyDetailResponse("Title", policy.Title),
                new CampaignSeriesSetupPolicyDetailResponse("Locale", policy.Locale),
                new CampaignSeriesSetupPolicyDetailResponse(
                    "Required grants",
                    CountJsonArrayEntries(policy.RequiredGrants).ToString(CultureInfo.InvariantCulture)),
                new CampaignSeriesSetupPolicyDetailResponse(
                    "Optional grants",
                    CountJsonArrayEntries(policy.OptionalGrants).ToString(CultureInfo.InvariantCulture)),
                new CampaignSeriesSetupPolicyDetailResponse("Published", FormatDate(policy.PublishedAt))
            ]
        };
    }

    private static CampaignSeriesSetupPolicyResponse CreateRetentionPolicyResponse(
        RetentionPolicySetupRow? policy)
    {
        if (policy is null)
        {
            return CreateMissingPolicyResponse();
        }

        return new CampaignSeriesSetupPolicyResponse(policy.Id, policy.Version, "configured")
        {
            Details =
            [
                new CampaignSeriesSetupPolicyDetailResponse("Retain for", FormatYears(policy.RetainForYears)),
                new CampaignSeriesSetupPolicyDetailResponse("Starts from", FormatCodeValue(policy.RetentionStartEvent)),
                new CampaignSeriesSetupPolicyDetailResponse("Action after retention", FormatCodeValue(policy.ActionAfter)),
                new CampaignSeriesSetupPolicyDetailResponse("Next review", FormatDate(policy.NextReviewAt))
            ]
        };
    }

    private static CampaignSeriesSetupPolicyResponse CreateDisclosurePolicyResponse(
        DisclosurePolicySetupRow? policy)
    {
        if (policy is null)
        {
            return CreateMissingPolicyResponse();
        }

        return new CampaignSeriesSetupPolicyResponse(policy.Id, policy.Version, "configured")
        {
            Details =
            [
                new CampaignSeriesSetupPolicyDetailResponse(
                    "Minimum group size",
                    policy.KMin.ToString(CultureInfo.InvariantCulture)),
                new CampaignSeriesSetupPolicyDetailResponse(
                    "Suppression",
                    FormatCodeValue(policy.SuppressionStrategy)),
                new CampaignSeriesSetupPolicyDetailResponse(
                    "Applies to",
                    FormatJsonStringArray(policy.AppliesToDimensions))
            ]
        };
    }

    private static int CountJsonArrayEntries(string value)
    {
        using var document = JsonDocument.Parse(value);

        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Count()
            : 0;
    }

    private static string FormatJsonStringArray(string value)
    {
        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return "Not configured";
        }

        var values = document.RootElement
            .EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.String)
            .Select(entry => entry.GetString())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Select(entry => FormatCodeValue(entry!))
            .ToArray();

        return values.Length == 0
            ? "All configured outputs"
            : string.Join(", ", values);
    }

    private static string FormatCodeValue(string value)
    {
        return value.Trim().Replace('_', ' ');
    }

    private static string FormatYears(int value)
    {
        return value == 1
            ? "1 year"
            : string.Concat(value.ToString(CultureInfo.InvariantCulture), " years");
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static CampaignSeriesSetupReadinessResponse CreateSetupReadiness(
        CampaignRow? selectedCampaign,
        int missingPrerequisiteCount)
    {
        if (selectedCampaign is null)
        {
            return new CampaignSeriesSetupReadinessResponse(null, "not_available", Ready: false);
        }

        if (selectedCampaign.Status is not (CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            return new CampaignSeriesSetupReadinessResponse(selectedCampaign.Id, "proof_only", Ready: false);
        }

        return missingPrerequisiteCount == 0
            ? new CampaignSeriesSetupReadinessResponse(selectedCampaign.Id, "ready", Ready: true)
            : new CampaignSeriesSetupReadinessResponse(selectedCampaign.Id, "blocked", Ready: false);
    }

    private static bool IsEditableSetupCampaign(CampaignRow campaign)
    {
        return campaign.Status is CampaignStatuses.Draft or CampaignStatuses.Scheduled;
    }

    private static List<CampaignSeriesSetupMissingPrerequisiteResponse> CreateMissingPrerequisites(
        CampaignRow? selectedCampaign,
        TemplateSetupRow? template,
        ScoringSetupRow? scoring,
        CampaignSeriesSetupPolicySummaryResponse policies)
    {
        var missing = new List<CampaignSeriesSetupMissingPrerequisiteResponse>();

        if (selectedCampaign is null)
        {
            missing.Add(CreateMissingPrerequisite(
                "campaign.missing",
                "Campaign",
                "Add a campaign to this series."));
        }

        if (template is null)
        {
            missing.Add(CreateMissingPrerequisite(
                "template.missing",
                "Template",
                "Attach a survey template version to the selected campaign."));
        }

        if (scoring is null)
        {
            missing.Add(CreateMissingPrerequisite(
                "scoring_rule.missing",
                "Scoring rule",
                "Add a scoring rule for the selected template version."));
        }

        if (policies.Consent.Status != "configured")
        {
            missing.Add(CreateMissingPrerequisite(
                "consent_document.missing",
                "Consent document",
                "Add a consent document for this series."));
        }

        if (policies.Retention.Status != "configured")
        {
            missing.Add(CreateMissingPrerequisite(
                "retention_policy.missing",
                "Retention policy",
                "Add a retention policy for this series."));
        }

        if (policies.Disclosure.Status != "configured")
        {
            missing.Add(CreateMissingPrerequisite(
                "disclosure_policy.missing",
                "Disclosure policy",
                "Add a disclosure policy for this series."));
        }

        return missing;
    }

    private static CampaignSeriesSetupMissingPrerequisiteResponse CreateMissingPrerequisite(
        string code,
        string label,
        string message)
    {
        return new CampaignSeriesSetupMissingPrerequisiteResponse(
            code,
            label,
            message,
            "blocking");
    }

    private static List<CampaignSeriesOperationsMissingPrerequisiteResponse> CreateOperationsMissingPrerequisites(
        IReadOnlyCollection<CampaignSeriesOperationsCampaignResponse> campaigns)
    {
        var missing = new List<CampaignSeriesOperationsMissingPrerequisiteResponse>();

        if (campaigns.Count == 0)
        {
            missing.Add(CreateOperationsMissingPrerequisite(
                "campaign.missing",
                "Campaign",
                "Add a campaign to this series."));
        }

        if (!campaigns.Any(campaign => campaign.Status is CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            missing.Add(CreateOperationsMissingPrerequisite(
                "launchable_campaign.missing",
                "Launchable campaign",
                "Prepare a draft or scheduled campaign before operating the series."));
        }

        if (!campaigns.Any(campaign => campaign.Status == CampaignStatuses.Live))
        {
            missing.Add(CreateOperationsMissingPrerequisite(
                "live_campaign.missing",
                "Live campaign",
                "Launch a campaign before monitoring live operations."));
        }

        if (campaigns.Sum(campaign => campaign.OpenLinkAssignmentCount) == 0)
        {
            missing.Add(CreateOperationsMissingPrerequisite(
                "public_entry.missing",
                "Public entry",
                "Create an open-link entry point before collecting anonymous responses."));
        }

        if (campaigns.Sum(campaign =>
                campaign.QueuedInvitationCount +
                campaign.SentInvitationCount +
                campaign.FailedInvitationCount +
                campaign.BouncedInvitationCount) == 0)
        {
            missing.Add(CreateOperationsMissingPrerequisite(
                "invitations.missing",
                "Invitations",
                "Queue invitations before monitoring delivery."));
        }

        return missing;
    }

    private static CampaignSeriesOperationsMissingPrerequisiteResponse CreateOperationsMissingPrerequisite(
        string code,
        string label,
        string message)
    {
        return new CampaignSeriesOperationsMissingPrerequisiteResponse(
            code,
            label,
            message,
            "blocking");
    }

    private static CampaignSeriesReportsCampaignResponse CreateReportsCampaignResponse(
        CampaignRow campaign,
        CampaignReportLaunchDetailRow? launchDetail,
        IReadOnlyDictionary<Guid, CampaignReportDisclosurePolicyRow> disclosurePolicies,
        int submittedResponseCount,
        CampaignScoreOutputCountsRow? scoreOutputCounts,
        int exportArtifactCount,
        CampaignReportExportArtifactRow? latestExportArtifact)
    {
        var scoreCount = scoreOutputCounts?.ScoreCount ?? 0;
        var disclosurePolicy = launchDetail?.DisclosurePolicyId is Guid disclosurePolicyId
            ? disclosurePolicies.GetValueOrDefault(disclosurePolicyId)
            : null;
        var disclosureState = DetermineReportsDisclosureState(
            launchDetail,
            disclosurePolicy,
            submittedResponseCount,
            scoreOutputCounts);
        var visibleScoreCount = DetermineVisibleReportScoreCount(
            launchDetail,
            disclosurePolicy,
            scoreOutputCounts);
        var suppressedScoreCount = DetermineSuppressedReportScoreCount(
            launchDetail,
            disclosurePolicy,
            scoreOutputCounts);
        var reportStatus = (disclosureState is "visible" or "suppressed") && scoreCount > 0
            ? "proof_only"
            : "blocked";
        var dataFinality = DetermineReportDataFinality(campaign.Status, reportStatus);
        var launchPacket = launchDetail is null
            ? null
            : Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection.FromJson(launchDetail.LaunchPacket);

        return new CampaignSeriesReportsCampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            launchDetail?.LaunchSnapshotId,
            launchDetail?.LaunchedAt,
            launchDetail?.ScoringRuleId,
            launchDetail?.ConsentDocumentId,
            launchDetail?.RetentionPolicyId,
            launchDetail?.DisclosurePolicyId,
            submittedResponseCount,
            scoreCount,
            exportArtifactCount,
            visibleScoreCount,
            suppressedScoreCount,
            disclosureState,
            disclosurePolicy?.KMin,
            reportStatus,
            launchDetail is null ? "not_available" : "not_validated_interpretation",
            latestExportArtifact?.Id,
            latestExportArtifact?.FileName,
            latestExportArtifact?.Status,
            latestExportArtifact?.CreatedAt,
            latestExportArtifact?.CompletedAt,
            latestExportArtifact?.StartedAt,
            latestExportArtifact?.FailedAt,
            latestExportArtifact?.ExpiresAt,
            latestExportArtifact?.DeletedAt,
            latestExportArtifact?.FailureReasonCode,
            latestExportArtifact?.CanDownload ?? false,
            campaign.ClosedAt,
            campaign.ClosedByUserId,
            campaign.CloseReason,
            dataFinality,
            launchPacket is null
                ? null
                : new ProductSurfaceLaunchPacketProvenanceResponse(
                    launchPacket.SchemaVersion,
                    launchPacket.Sections,
                    launchPacket.Source));
    }

    private static string DetermineReportDataFinality(
        string campaignStatus,
        string reportStatus)
    {
        if (reportStatus != "proof_only")
        {
            return NotReportableDataFinality;
        }

        return campaignStatus switch
        {
            CampaignStatuses.Closed => ClosedWaveDataFinality,
            CampaignStatuses.Live => PreliminaryLiveDataFinality,
            _ => NotReportableDataFinality
        };
    }

    private static string DetermineReportsDisclosureState(
        CampaignReportLaunchDetailRow? launchDetail,
        CampaignReportDisclosurePolicyRow? disclosurePolicy,
        int submittedResponseCount,
        CampaignScoreOutputCountsRow? scoreOutputCounts)
    {
        if (launchDetail is null)
        {
            return "not_available";
        }

        if (!launchDetail.DisclosurePolicyId.HasValue || disclosurePolicy is null)
        {
            return "not_configured";
        }

        var scoreCount = scoreOutputCounts?.ScoreCount ?? 0;
        if (submittedResponseCount == 0 || scoreCount == 0)
        {
            return "pending";
        }

        if (launchDetail.ResponseIdentityMode == ResponseIdentityModes.Identified)
        {
            return "visible";
        }

        return scoreOutputCounts!.DimensionScoreCounts.Any(count => count >= disclosurePolicy.KMin)
            ? "visible"
            : "suppressed";
    }

    private static int DetermineVisibleReportScoreCount(
        CampaignReportLaunchDetailRow? launchDetail,
        CampaignReportDisclosurePolicyRow? disclosurePolicy,
        CampaignScoreOutputCountsRow? scoreOutputCounts)
    {
        if (launchDetail is null ||
            scoreOutputCounts is null ||
            scoreOutputCounts.ScoreCount == 0 ||
            !launchDetail.DisclosurePolicyId.HasValue ||
            disclosurePolicy is null)
        {
            return 0;
        }

        if (launchDetail.ResponseIdentityMode == ResponseIdentityModes.Identified)
        {
            return scoreOutputCounts.ScoreCount;
        }

        return scoreOutputCounts.DimensionScoreCounts
            .Where(count => count >= disclosurePolicy.KMin)
            .Sum();
    }

    private static int DetermineSuppressedReportScoreCount(
        CampaignReportLaunchDetailRow? launchDetail,
        CampaignReportDisclosurePolicyRow? disclosurePolicy,
        CampaignScoreOutputCountsRow? scoreOutputCounts)
    {
        if (launchDetail is null ||
            scoreOutputCounts is null ||
            scoreOutputCounts.ScoreCount == 0 ||
            !launchDetail.DisclosurePolicyId.HasValue ||
            disclosurePolicy is null ||
            launchDetail.ResponseIdentityMode == ResponseIdentityModes.Identified)
        {
            return 0;
        }

        return scoreOutputCounts.DimensionScoreCounts
            .Where(count => count < disclosurePolicy.KMin)
            .Sum();
    }

    private static CampaignSeriesReportsCampaignResponse? SelectReportsCampaign(
        IReadOnlyCollection<CampaignSeriesReportsCampaignResponse> campaigns)
    {
        return campaigns
            .OrderByDescending(IsReportableCampaign)
            .ThenByDescending(campaign => campaign.Status == CampaignStatuses.Live)
            .ThenByDescending(campaign => campaign.LatestLaunchSnapshotId.HasValue)
            .ThenByDescending(campaign => campaign.SubmittedResponseCount)
            .ThenByDescending(campaign => campaign.ScoreCount)
            .FirstOrDefault();
    }

    private static List<CampaignSeriesReportsMissingPrerequisiteResponse> CreateReportsMissingPrerequisites(
        IReadOnlyCollection<CampaignSeriesReportsCampaignResponse> campaigns)
    {
        var missing = new List<CampaignSeriesReportsMissingPrerequisiteResponse>();

        if (campaigns.Count == 0)
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "campaign.missing",
                "Campaign",
                "Add a campaign to this series."));
        }

        if (!campaigns.Any(campaign => campaign.LatestLaunchSnapshotId.HasValue))
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "launched_campaign.missing",
                "Launched campaign",
                "Launch a campaign before reviewing report state."));
        }

        if (campaigns.Sum(campaign => campaign.SubmittedResponseCount) == 0)
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "submitted_responses.missing",
                "Submitted responses",
                "Collect submitted responses before reviewing report state."));
        }

        if (campaigns.Sum(campaign => campaign.ScoreCount) == 0)
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "scores.missing",
                "Scores",
                "Run scoring before reviewing report state."));
        }

        if (!campaigns.Any(campaign => campaign.DisclosureKMin.HasValue))
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "disclosure_policy.missing",
                "Disclosure policy",
                "Attach a disclosure policy to the launch snapshot before reviewing report state."));
        }

        if (campaigns.Any(IsReportableCampaign) &&
            campaigns.Sum(campaign => campaign.ExportArtifactCount) == 0)
        {
            missing.Add(CreateReportsMissingPrerequisite(
                "export_artifact.missing",
                "Export artifact",
                "Create a report proof export before handoff.",
                "advisory"));
        }

        return missing;
    }

    private static CampaignSeriesReportsMissingPrerequisiteResponse CreateReportsMissingPrerequisite(
        string code,
        string label,
        string message,
        string severity = "blocking")
    {
        return new CampaignSeriesReportsMissingPrerequisiteResponse(
            code,
            label,
            message,
            severity);
    }

    private static CampaignSeriesWavesWaveResponse CreateWavesWaveResponse(
        CampaignRow campaign,
        CampaignWaveLaunchDetailRow? launchDetail,
        IReadOnlyDictionary<Guid, CampaignReportDisclosurePolicyRow> disclosurePolicies,
        IReadOnlyDictionary<Guid, CampaignWaveScoringRuleRow> scoringRules,
        int submittedResponseCount,
        int scoreCount,
        int linkedTrajectoryCount)
    {
        var disclosurePolicy = launchDetail?.DisclosurePolicyId is Guid disclosurePolicyId
            ? disclosurePolicies.GetValueOrDefault(disclosurePolicyId)
            : null;
        var scoringRule = launchDetail is null
            ? null
            : scoringRules.GetValueOrDefault(launchDetail.ScoringRuleId);
        var waveState = DetermineWaveState(launchDetail);
        var dataFinality = DetermineWaveDataFinality(
            campaign.Status,
            waveState,
            submittedResponseCount,
            scoreCount);
        var launchPacket = launchDetail is null
            ? null
            : Platform.Infrastructure.Reports.LaunchPacketProvenanceProjection.FromJson(launchDetail.LaunchPacket);

        return new CampaignSeriesWavesWaveResponse(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            launchDetail?.LaunchSnapshotId,
            launchDetail?.LaunchedAt,
            launchDetail?.ScoringRuleId,
            scoringRule?.RuleKey,
            scoringRule?.RuleVersion,
            launchDetail?.DisclosurePolicyId,
            disclosurePolicy?.KMin,
            submittedResponseCount,
            scoreCount,
            linkedTrajectoryCount,
            waveState,
            campaign.ClosedAt,
            campaign.ClosedByUserId,
            campaign.CloseReason,
            dataFinality,
            launchPacket is null
                ? null
                : new ProductSurfaceLaunchPacketProvenanceResponse(
                    launchPacket.SchemaVersion,
                    launchPacket.Sections,
                    launchPacket.Source));
    }

    private static string DetermineWaveState(CampaignWaveLaunchDetailRow? launchDetail)
    {
        if (launchDetail is null)
        {
            return "not_launched";
        }

        return launchDetail.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal
            ? "wave"
            : "not_longitudinal";
    }

    private static string DetermineWaveDataFinality(
        string campaignStatus,
        string waveState,
        int submittedResponseCount,
        int scoreCount)
    {
        if (waveState != "wave" || submittedResponseCount == 0 || scoreCount == 0)
        {
            return NotReportableDataFinality;
        }

        return campaignStatus switch
        {
            CampaignStatuses.Closed => ClosedWaveDataFinality,
            CampaignStatuses.Live => PreliminaryLiveDataFinality,
            _ => NotReportableDataFinality
        };
    }

    private static CampaignSeriesWavesComparisonResponse CreateWavesComparison(
        CampaignSeriesWavesWaveResponse? baselineWave,
        CampaignSeriesWavesWaveResponse? comparisonWave,
        IReadOnlyCollection<WaveSubmittedTrajectoryRow> trajectoryRows,
        IReadOnlyCollection<WaveScoreDimensionRow> scoreRows)
    {
        if (baselineWave is null || comparisonWave is null)
        {
            return new CampaignSeriesWavesComparisonResponse(
                "blocked",
                "not_available",
                "not_available",
                "not_available",
                null,
                0,
                0,
                0,
                0);
        }

        var selectedCampaignIds = new[] { baselineWave.Id, comparisonWave.Id };
        var selectedTrajectories = trajectoryRows
            .Where(row => selectedCampaignIds.Contains(row.CampaignId))
            .ToArray();
        var completeParticipantCodeIds = selectedTrajectories
            .GroupBy(row => row.ParticipantCodeId)
            .Where(group => group.Select(row => row.CampaignId).Distinct().Count() == 2)
            .Select(group => group.Key)
            .ToHashSet();
        var linkedPairCount = completeParticipantCodeIds.Count;
        var baselineSessionIds = selectedTrajectories
            .Where(row => row.CampaignId == baselineWave.Id &&
                completeParticipantCodeIds.Contains(row.ParticipantCodeId))
            .Select(row => row.ResponseSessionId)
            .ToHashSet();
        var comparisonSessionIds = selectedTrajectories
            .Where(row => row.CampaignId == comparisonWave.Id &&
                completeParticipantCodeIds.Contains(row.ParticipantCodeId))
            .Select(row => row.ResponseSessionId)
            .ToHashSet();
        var baselineDimensions = scoreRows
            .Where(row => row.CampaignId == baselineWave.Id &&
                baselineSessionIds.Contains(row.ResponseSessionId))
            .Select(row => row.DimensionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var comparisonDimensions = scoreRows
            .Where(row => row.CampaignId == comparisonWave.Id &&
                comparisonSessionIds.Contains(row.ResponseSessionId))
            .Select(row => row.DimensionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var comparableDimensionCount = baselineDimensions
            .Intersect(comparisonDimensions, StringComparer.OrdinalIgnoreCase)
            .Count();
        var compatibilityState = DetermineWavesCompatibilityState(
            baselineWave,
            comparisonWave,
            comparableDimensionCount);
        var disclosureKMin = DetermineWavesDisclosureKMin(baselineWave, comparisonWave);
        var disclosureState = DetermineWavesDisclosureState(
            disclosureKMin,
            linkedPairCount,
            comparableDimensionCount);
        var status = compatibilityState == "compatible" &&
            disclosureState is "visible" or "suppressed" &&
            comparableDimensionCount > 0
                ? "proof_only"
                : "blocked";
        var visibleScoreCount = status == "proof_only" && disclosureState == "visible"
            ? comparableDimensionCount
            : 0;
        var suppressedScoreCount = status == "proof_only" && disclosureState == "suppressed"
            ? comparableDimensionCount
            : 0;
        var blockedScoreCount = comparableDimensionCount - visibleScoreCount - suppressedScoreCount;

        return new CampaignSeriesWavesComparisonResponse(
            status,
            disclosureState,
            compatibilityState,
            "not_validated_interpretation",
            disclosureKMin,
            linkedPairCount,
            visibleScoreCount,
            suppressedScoreCount,
            blockedScoreCount);
    }

    private static string DetermineWavesCompatibilityState(
        CampaignSeriesWavesWaveResponse baselineWave,
        CampaignSeriesWavesWaveResponse comparisonWave,
        int comparableDimensionCount)
    {
        if (comparableDimensionCount == 0)
        {
            return "not_available";
        }

        if (!baselineWave.ScoringRuleId.HasValue || !comparisonWave.ScoringRuleId.HasValue)
        {
            return "missing";
        }

        if (baselineWave.ScoringRuleId == comparisonWave.ScoringRuleId)
        {
            return "compatible";
        }

        return string.Equals(baselineWave.ScoringRuleKey, comparisonWave.ScoringRuleKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(baselineWave.ScoringRuleVersion, comparisonWave.ScoringRuleVersion, StringComparison.OrdinalIgnoreCase)
                ? "compatible"
                : "incompatible";
    }

    private static int? DetermineWavesDisclosureKMin(
        CampaignSeriesWavesWaveResponse baselineWave,
        CampaignSeriesWavesWaveResponse comparisonWave)
    {
        if (!baselineWave.DisclosureKMin.HasValue || !comparisonWave.DisclosureKMin.HasValue)
        {
            return null;
        }

        return Math.Max(baselineWave.DisclosureKMin.Value, comparisonWave.DisclosureKMin.Value);
    }

    private static string DetermineWavesDisclosureState(
        int? disclosureKMin,
        int linkedPairCount,
        int comparableDimensionCount)
    {
        if (!disclosureKMin.HasValue)
        {
            return "not_configured";
        }

        if (linkedPairCount == 0 || comparableDimensionCount == 0)
        {
            return "pending";
        }

        return linkedPairCount >= disclosureKMin.Value
            ? "visible"
            : "suppressed";
    }

    private static int CountCompleteTrajectories(IReadOnlyCollection<WaveSubmittedTrajectoryRow> trajectoryRows)
    {
        return trajectoryRows
            .GroupBy(row => row.ParticipantCodeId)
            .Count(group => group.Select(row => row.CampaignId).Distinct().Count() >= 2);
    }

    private static List<CampaignSeriesWavesMissingPrerequisiteResponse> CreateWavesMissingPrerequisites(
        IReadOnlyCollection<CampaignSeriesWavesWaveResponse> waves,
        IReadOnlyCollection<WaveSubmittedTrajectoryRow> trajectoryRows,
        CampaignSeriesWavesComparisonResponse comparison)
    {
        var missing = new List<CampaignSeriesWavesMissingPrerequisiteResponse>();

        if (waves.Count == 0)
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "campaign.missing",
                "Campaign",
                "Add campaigns to this series before reviewing wave state."));
        }

        if (waves.Count(wave => wave.WaveState == "wave") == 0)
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "longitudinal_waves.missing",
                "Longitudinal waves",
                "Launch campaigns with anonymous longitudinal identity before reviewing wave state."));
        }

        if (waves.Count(wave => wave.WaveState == "wave") < 2)
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "two_waves.missing",
                "Two waves",
                "Create at least two longitudinal waves before comparing movement."));
        }

        if (CountCompleteTrajectories(trajectoryRows) == 0)
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "linked_trajectories.missing",
                "Linked trajectories",
                "Collect submitted responses with the same participant code in at least two waves."));
        }

        if (comparison.VisibleScoreCount + comparison.SuppressedScoreCount + comparison.BlockedScoreCount == 0)
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "scores.missing",
                "Scores",
                "Run scoring for matched wave responses before reviewing comparison state."));
        }

        if (waves.Count != 0 && comparison.DisclosureState == "not_configured")
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "disclosure_policy.missing",
                "Disclosure policy",
                "Attach disclosure policies to both selected wave launches before reviewing comparison state."));
        }

        if (comparison.CompatibilityState is "missing" or "incompatible")
        {
            missing.Add(CreateWavesMissingPrerequisite(
                "scoring_compatibility.missing",
                "Scoring compatibility",
                "Use compatible scoring rules before comparing selected waves."));
        }

        return missing;
    }

    private static CampaignSeriesWavesMissingPrerequisiteResponse CreateWavesMissingPrerequisite(
        string code,
        string label,
        string message,
        string severity = "blocking")
    {
        return new CampaignSeriesWavesMissingPrerequisiteResponse(
            code,
            label,
            message,
            severity);
    }

    private async Task<Result<IReadOnlyList<RespondentRulePreviewCandidate>>> BuildSelfPreviewRowsAsync(
        Guid tenantId,
        Guid campaignId,
        List<RespondentRulePreviewWarningResponse> warnings,
        CancellationToken cancellationToken)
    {
        var audienceSubjectIds = await (
                from audience in db.Audiences.AsNoTracking()
                join member in db.AudienceMembers.AsNoTracking()
                    on audience.Id equals member.AudienceId
                join subject in db.Subjects.AsNoTracking()
                    on member.SubjectId equals subject.Id
                where audience.CampaignId == campaignId &&
                    member.RemovedAt == null &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null
                select subject.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (audienceSubjectIds.Count == 0)
        {
            warnings.Add(new RespondentRulePreviewWarningResponse(
                "respondent_rule_preview.audience_missing",
                "Campaign audience has no active members; preview uses all active tenant subjects."));
        }

        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(subject =>
                subject.TenantId == tenantId &&
                subject.DeletedAt == null &&
                (audienceSubjectIds.Count == 0 || audienceSubjectIds.Contains(subject.Id)))
            .Select(subject => new RespondentRulePreviewSubjectRow(
                subject.Id,
                subject.DisplayName,
                subject.Email,
                subject.ExternalId))
            .ToListAsync(cancellationToken);
        var candidates = subjects
            .Select(subject =>
            {
                var previewSubject = CreatePreviewSubject(subject);

                return new RespondentRulePreviewCandidate(previewSubject, previewSubject);
            })
            .ToArray();

        return Result.Success<IReadOnlyList<RespondentRulePreviewCandidate>>(candidates);
    }

    private async Task<Result<IReadOnlyList<RespondentRulePreviewCandidate>>> BuildAllInGroupPreviewRowsAsync(
        Guid tenantId,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        if (!groupId.HasValue)
        {
            return Result.Failure<IReadOnlyList<RespondentRulePreviewCandidate>>(
                Error.Validation(
                    "respondent_rule_preview.group_required",
                    "A subject group is required for this preview rule."));
        }

        var groupExists = await db.SubjectGroups
            .AsNoTracking()
            .AnyAsync(
                group =>
                    group.TenantId == tenantId &&
                    group.Id == groupId.Value &&
                    group.DeletedAt == null,
                cancellationToken);
        if (!groupExists)
        {
            return Result.Failure<IReadOnlyList<RespondentRulePreviewCandidate>>(
                Error.NotFound("subject_group.not_found", "Subject group was not found."));
        }

        var subjects = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subject in db.Subjects.AsNoTracking()
                    on membership.SubjectId equals subject.Id
                where membership.GroupId == groupId.Value &&
                    membership.ValidTo == null &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null
                select new RespondentRulePreviewSubjectRow(
                    subject.Id,
                    subject.DisplayName,
                    subject.Email,
                    subject.ExternalId))
            .Distinct()
            .ToListAsync(cancellationToken);
        var candidates = subjects
            .Select(subject => new RespondentRulePreviewCandidate(null, CreatePreviewSubject(subject)))
            .ToArray();

        return Result.Success<IReadOnlyList<RespondentRulePreviewCandidate>>(candidates);
    }

    private async Task<Result<IReadOnlyList<RespondentRulePreviewCandidate>>> BuildManagerOfTargetPreviewRowsAsync(
        Guid tenantId,
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        var targetResult = await LoadTargetPreviewSubjectAsync(tenantId, targetSubjectId, cancellationToken);
        if (targetResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRulePreviewCandidate>>(targetResult.Error);
        }

        var target = targetResult.Value;
        var managers = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join manager in db.Subjects.AsNoTracking()
                    on relationship.SubjectId equals manager.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.RelatedSubjectId == target.Id &&
                    relationship.ValidTo == null &&
                    manager.TenantId == tenantId &&
                    manager.DeletedAt == null
                select new RespondentRulePreviewSubjectRow(
                    manager.Id,
                    manager.DisplayName,
                    manager.Email,
                    manager.ExternalId))
            .ToListAsync(cancellationToken);
        var candidates = managers
            .Select(manager => new RespondentRulePreviewCandidate(target, CreatePreviewSubject(manager)))
            .ToArray();

        return Result.Success<IReadOnlyList<RespondentRulePreviewCandidate>>(candidates);
    }

    private async Task<Result<IReadOnlyList<RespondentRulePreviewCandidate>>> BuildReportsOfTargetPreviewRowsAsync(
        Guid tenantId,
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        var targetResult = await LoadTargetPreviewSubjectAsync(tenantId, targetSubjectId, cancellationToken);
        if (targetResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RespondentRulePreviewCandidate>>(targetResult.Error);
        }

        var target = targetResult.Value;
        var reports = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join report in db.Subjects.AsNoTracking()
                    on relationship.RelatedSubjectId equals report.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.SubjectId == target.Id &&
                    relationship.ValidTo == null &&
                    report.TenantId == tenantId &&
                    report.DeletedAt == null
                select new RespondentRulePreviewSubjectRow(
                    report.Id,
                    report.DisplayName,
                    report.Email,
                    report.ExternalId))
            .ToListAsync(cancellationToken);
        var candidates = reports
            .Select(report => new RespondentRulePreviewCandidate(target, CreatePreviewSubject(report)))
            .ToArray();

        return Result.Success<IReadOnlyList<RespondentRulePreviewCandidate>>(candidates);
    }

    private async Task<Result<RespondentRulePreviewSubjectResponse>> LoadTargetPreviewSubjectAsync(
        Guid tenantId,
        Guid? targetSubjectId,
        CancellationToken cancellationToken)
    {
        if (!targetSubjectId.HasValue)
        {
            return Result.Failure<RespondentRulePreviewSubjectResponse>(
                Error.Validation(
                    "respondent_rule_preview.target_required",
                    "A target subject is required for this preview rule."));
        }

        var subject = await db.Subjects
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.Id == targetSubjectId.Value &&
                item.DeletedAt == null)
            .Select(item => new RespondentRulePreviewSubjectRow(
                item.Id,
                item.DisplayName,
                item.Email,
                item.ExternalId))
            .SingleOrDefaultAsync(cancellationToken);
        if (subject is null)
        {
            return Result.Failure<RespondentRulePreviewSubjectResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        return Result.Success(CreatePreviewSubject(subject));
    }

    private static Result<RespondentRulePreviewRule> ParseRespondentRulePreviewRule(string rule)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            return InvalidRespondentRulePreviewRule();
        }

        try
        {
            using var document = JsonDocument.Parse(rule);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return InvalidRespondentRulePreviewRule();
            }

            if (!document.RootElement.TryGetProperty("kind", out var kindElement) ||
                kindElement.ValueKind != JsonValueKind.String)
            {
                return Result.Failure<RespondentRulePreviewRule>(
                    Error.Validation(
                        "respondent_rule_preview.kind_required",
                        "Respondent rule kind is required."));
            }

            var kind = NormalizePreviewText(kindElement.GetString())?.ToLowerInvariant();
            var role = TryGetPreviewRole(document.RootElement);
            if (kind is not
                RespondentRulePreviewRuleKinds.Self and not
                RespondentRulePreviewRuleKinds.AllInGroup and not
                RespondentRulePreviewRuleKinds.ManagerOfTarget and not
                RespondentRulePreviewRuleKinds.ReportsOfTarget)
            {
                return Result.Failure<RespondentRulePreviewRule>(
                    Error.Validation(
                        "respondent_rule_preview.unsupported_kind",
                        "Respondent rule kind is not supported for preview."));
            }

            return Result.Success(new RespondentRulePreviewRule(kind, role ?? DefaultPreviewRole(kind)));
        }
        catch (JsonException)
        {
            return InvalidRespondentRulePreviewRule();
        }
    }

    private static Result<RespondentRulePreviewRule> InvalidRespondentRulePreviewRule()
    {
        return Result.Failure<RespondentRulePreviewRule>(
            Error.Validation(
                "respondent_rule_preview.rule_invalid",
                "Respondent rule must be a JSON object."));
    }

    private static string? TryGetPreviewRole(JsonElement root)
    {
        if (!root.TryGetProperty("role", out var roleElement) ||
            roleElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return NormalizePreviewText(roleElement.GetString());
    }

    private static string DefaultPreviewRole(string kind)
    {
        return kind switch
        {
            RespondentRulePreviewRuleKinds.AllInGroup => "group_member",
            RespondentRulePreviewRuleKinds.ManagerOfTarget => "manager",
            RespondentRulePreviewRuleKinds.ReportsOfTarget => "direct_report",
            _ => "self"
        };
    }

    private static string? NormalizePreviewText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DirectoryImportStaleState ReadDirectoryImportStaleState(string attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
        {
            return new DirectoryImportStaleState(false, null);
        }

        try
        {
            using var document = JsonDocument.Parse(attributes);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(DirectoryImportStaleAttribute, out var staleElement) ||
                staleElement.ValueKind != JsonValueKind.True)
            {
                return new DirectoryImportStaleState(false, null);
            }

            DateTimeOffset? staleAt = null;
            if (root.TryGetProperty(DirectoryImportStaleAtAttribute, out var staleAtElement) &&
                staleAtElement.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(
                    staleAtElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedStaleAt))
            {
                staleAt = parsedStaleAt;
            }

            return new DirectoryImportStaleState(true, staleAt);
        }
        catch (JsonException)
        {
            return new DirectoryImportStaleState(false, null);
        }
    }

    private static IReadOnlyList<string> ReadStringArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement
                .EnumerateArray()
                .Where(element => element.ValueKind == JsonValueKind.String)
                .Select(element => element.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int ReadJsonInt(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.Number ||
                !property.TryGetInt32(out var value))
            {
                return 0;
            }

            return value;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static RespondentRulePreviewSubjectResponse CreatePreviewSubject(RespondentRulePreviewSubjectRow subject)
    {
        return new RespondentRulePreviewSubjectResponse(
            subject.Id,
            CreatePreviewSubjectLabel(subject),
            subject.DisplayName,
            subject.Email,
            subject.ExternalId);
    }

    private static RespondentRulePreviewSubjectResponse ToPreviewSubject(RespondentRuleSubject subject)
    {
        return new RespondentRulePreviewSubjectResponse(
            subject.Id,
            subject.Label,
            subject.DisplayName,
            subject.Email,
            subject.ExternalId);
    }

    private static string CreatePreviewSubjectLabel(RespondentRulePreviewSubjectRow subject)
    {
        return NormalizePreviewText(subject.DisplayName) ??
            NormalizePreviewText(subject.Email) ??
            NormalizePreviewText(subject.ExternalId) ??
            subject.Id.ToString("D");
    }

    private static int CountDistinctSubjects(IEnumerable<RespondentRulePreviewSubjectResponse?> subjects)
    {
        return subjects
            .Where(subject => subject is not null)
            .Select(subject => subject!.Id)
            .Distinct()
            .Count();
    }

    private static int CountDistinctResolvedSubjects(IEnumerable<RespondentRuleSubject?> subjects)
    {
        return subjects
            .Where(subject => subject is not null)
            .Select(subject => subject!.Id)
            .Distinct()
            .Count();
    }

    private static RespondentRulePreviewWarningResponse CreateEmptyPreviewWarning(
        string kind,
        Guid? subjectId,
        Guid? groupId)
    {
        return kind switch
        {
            RespondentRulePreviewRuleKinds.AllInGroup => new RespondentRulePreviewWarningResponse(
                "respondent_rule_preview.group_empty",
                "Selected subject group has no active members.",
                GroupId: groupId),
            RespondentRulePreviewRuleKinds.ManagerOfTarget => new RespondentRulePreviewWarningResponse(
                "respondent_rule_preview.target_has_no_manager",
                "Selected target subject has no active manager relationship.",
                SubjectId: subjectId),
            RespondentRulePreviewRuleKinds.ReportsOfTarget => new RespondentRulePreviewWarningResponse(
                "respondent_rule_preview.target_has_no_reports",
                "Selected target subject has no active direct reports.",
                SubjectId: subjectId),
            _ => new RespondentRulePreviewWarningResponse(
                "respondent_rule_preview.empty",
                "Preview did not resolve any respondents.")
        };
    }

    private static class RespondentRulePreviewRuleKinds
    {
        public const string Self = "self";
        public const string AllInGroup = "all_in_group";
        public const string ManagerOfTarget = "manager_of_target";
        public const string ReportsOfTarget = "reports_of_target";
    }

    private sealed record RespondentRulePreviewRule(
        string Kind,
        string Role);

    private sealed record RespondentRulePreviewSubjectRow(
        Guid Id,
        string? DisplayName,
        string? Email,
        string? ExternalId);

    private sealed record RespondentRulePreviewCandidate(
        RespondentRulePreviewSubjectResponse? Target,
        RespondentRulePreviewSubjectResponse? Respondent);

    private sealed record CampaignTotalsRow(
        int CampaignCount,
        int LiveCampaignCount);

    private sealed record WorkspaceDirectorySummaryRow(
        int SubjectCount,
        int GroupCount);

    private sealed record WorkspaceSeriesCountRow(
        Guid CampaignSeriesId,
        int Count);

    private sealed record CampaignCountRow(
        Guid CampaignId,
        int Count);

    private sealed record CampaignStatusCountRow(
        Guid CampaignId,
        string Status,
        int Count);

    private sealed record CampaignNotificationCountsRow(
        int QueuedCount,
        int SentCount,
        int FailedCount,
        int BouncedCount);

    private sealed record CampaignDeliveryAggregateRow(
        Guid CampaignId,
        int AttemptCount,
        DateTimeOffset? LatestAttemptAt);

    private sealed record CampaignProviderDeliveryEventAggregateRow(
        Guid CampaignId,
        int AcceptedCount,
        int DeliveredCount,
        int BouncedCount,
        int ComplainedCount,
        DateTimeOffset? LatestEventAt);

    private sealed record CampaignCollectionAggregateRow(
        Guid CampaignId,
        int StartedResponseCount,
        int SubmittedResponseCount,
        DateTimeOffset? LatestStartedAt,
        DateTimeOffset? LatestSubmittedAt);

    private sealed record TemplateScoringRuleRow(
        Guid TemplateVersionId,
        Guid ScoringRuleId,
        string Status,
        DateTimeOffset? PublishedAt,
        DateTimeOffset UpdatedAt);

    private sealed record CampaignScoredSessionRow(
        Guid CampaignId,
        Guid ResponseSessionId,
        DateTimeOffset RanAt);

    private sealed record CampaignScoreCoverageAggregateRow(
        Guid CampaignId,
        int ScoredSubmittedResponseCount,
        DateTimeOffset? LatestScoringActivityAt);

    private sealed record CampaignScoreOutputObservationRow(
        Guid ScoreId,
        Guid CampaignId,
        Guid ResponseSessionId,
        string DimensionCode,
        DateTimeOffset ComputedAt);

    private sealed record CampaignScoreOutputCountRow(
        Guid CampaignId,
        string DimensionCode,
        int ScoreCount);

    private sealed record CampaignScoreOutputCountsRow(
        Guid CampaignId,
        int ScoreCount,
        IReadOnlyList<int> DimensionScoreCounts);

    private sealed record SeriesCampaignAggregateRow(
        Guid CampaignSeriesId,
        int CampaignCount,
        int LiveCampaignCount);

    private sealed record SeriesSubmittedAggregateRow(
        Guid CampaignSeriesId,
        int SubmittedResponseCount,
        DateTimeOffset? LatestSubmissionAt);

    private sealed record SeriesLaunchAggregateRow(
        Guid CampaignSeriesId,
        DateTimeOffset? LatestLaunchAt);

    private sealed record CampaignLaunchAggregateRow(
        Guid CampaignId,
        DateTimeOffset? LatestLaunchAt,
        bool HasConsentDocument,
        bool HasRetentionPolicy,
        bool HasDisclosurePolicy);

    private sealed record CampaignLaunchDetailRow(
        Guid CampaignId,
        Guid LaunchSnapshotId,
        DateTimeOffset LaunchedAt,
        Guid TemplateVersionId,
        Guid ScoringRuleId,
        Guid? ConsentDocumentId,
        Guid? RetentionPolicyId,
        Guid? DisclosurePolicyId,
        string ResponseIdentityMode,
        string DefaultLocale,
        int TemplateQuestionCount,
        Guid? LaunchedByUserId,
        string LaunchPacket);

    private sealed record CampaignReportLaunchDetailRow(
        Guid CampaignId,
        Guid LaunchSnapshotId,
        DateTimeOffset LaunchedAt,
        Guid ScoringRuleId,
        Guid? ConsentDocumentId,
        Guid? RetentionPolicyId,
        Guid? DisclosurePolicyId,
        string ResponseIdentityMode,
        string LaunchPacket);

    private sealed record CampaignReportDisclosurePolicyRow(
        Guid Id,
        int KMin);

    private sealed record CampaignReportExportArtifactRow(
        Guid CampaignId,
        Guid Id,
        string FileName,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FailedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? DeletedAt,
        string? FailureReasonCode,
        bool CanDownload);

    private sealed record CampaignReportExportArtifactRegistryRow(
        Guid Id,
        string TargetKind,
        Guid? CampaignId,
        Guid? CampaignSeriesId,
        string ArtifactType,
        string Status,
        string Format,
        string FileName,
        int RowCount,
        long ByteSize,
        string? ChecksumSha256,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FailedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? DeletedAt,
        string? FailureReasonCode,
        bool CanDownload);

    private sealed record ExportArtifactLibraryRow(
        Guid Id,
        string TargetKind,
        Guid? CampaignId,
        Guid? CampaignSeriesId,
        string ArtifactType,
        string Status,
        string Format,
        string FileName,
        int RowCount,
        long ByteSize,
        string? ChecksumSha256,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FailedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? DeletedAt,
        string? FailureReasonCode,
        bool CanDownload);

    private sealed record ExportArtifactCampaignTargetRow(
        Guid Id,
        string Name,
        string Status,
        DateTimeOffset? ClosedAt);

    private sealed record ExportArtifactSeriesTargetRow(
        Guid Id,
        string Name);

    private sealed record CampaignWaveLaunchDetailRow(
        Guid CampaignId,
        Guid LaunchSnapshotId,
        DateTimeOffset LaunchedAt,
        Guid ScoringRuleId,
        Guid? DisclosurePolicyId,
        string ResponseIdentityMode,
        string LaunchPacket);

    private sealed record CampaignWaveScoringRuleRow(
        Guid Id,
        string RuleKey,
        string RuleVersion);

    private sealed record WaveSubmittedTrajectoryRow(
        Guid ResponseSessionId,
        Guid CampaignId,
        Guid ParticipantCodeId);

    private sealed record WaveScoreDimensionRow(
        Guid ResponseSessionId,
        Guid CampaignId,
        string DimensionCode);

    private sealed record ResultsScoreObservationRow(
        Guid ScoreId,
        Guid CampaignId,
        Guid ResponseSessionId,
        string DimensionCode,
        decimal Value,
        int NValid,
        int NExpected,
        string MissingPolicyStatus,
        DateTimeOffset ComputedAt,
        DateTimeOffset SubmittedAt,
        Guid? TargetSubjectId,
        Guid? RespondentSubjectId);

    private sealed record ResultsSubjectGroupMembershipRow(
        Guid SubjectId,
        string GroupType,
        string GroupName,
        DateOnly? ValidFrom,
        DateOnly? ValidTo);

    private sealed record ResultsGroupedScoreObservationRow(
        string GroupType,
        string GroupName,
        string DimensionCode,
        decimal Value,
        int NValid,
        int NExpected,
        string MissingPolicyStatus);

    private sealed record CampaignSeriesRow(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string StudyKind,
        string? SampleScenario,
        DateTimeOffset? ArchivedAt,
        Guid? ArchivedByUserId,
        string? ArchiveReason,
        string? StudyPurpose = null,
        string? StudyAudience = null,
        string? StudyDesignType = null,
        string? StudyIntendedUse = null,
        string? StudyInterpretationBoundary = null,
        string? StudyOwnerNotes = null,
        Guid? SetupTemplateVersionId = null);

    private sealed record TenantMemberAssignmentRow(
        Guid UserId,
        string Email,
        string Locale,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastLoginAt,
        Guid RoleId,
        string RoleCode,
        string RoleName,
        string ScopeType,
        Guid? ScopeId,
        DateTimeOffset GrantedAt);

    private sealed record TenantMemberRolePermissionRow(
        Guid RoleId,
        string PermissionCode);

    private sealed record SubjectDirectorySubjectRow(
        Guid Id,
        string? DisplayName,
        string? Email,
        string? ExternalId,
        string Locale,
        string Attributes);

    private sealed record SubjectDirectoryMembershipRow(
        Guid SubjectId,
        Guid GroupId,
        string GroupType,
        string GroupName,
        string? RoleInGroup,
        DateOnly? ValidFrom,
        DateOnly? ValidTo);

    private sealed record SubjectManagerRow(
        Guid SubjectId,
        Guid ManagerSubjectId,
        string? ManagerDisplayName);

    private sealed record DirectoryImportStaleState(
        bool IsStale,
        DateTimeOffset? StaleAt);

    private sealed record DirectoryConnectionStateRow(
        string Provider,
        string Status,
        string DisplayName,
        string? PrimaryDomain,
        string GrantedScopes,
        DateTimeOffset? LastConsentAt,
        DateTimeOffset? LastSuccessfulImportAt,
        DateTimeOffset UpdatedAt);

    private sealed record DirectoryImportRunRow(
        Guid Id,
        Guid DirectoryConnectionId,
        Guid? DirectoryImportRuleId,
        Guid? PreviewRunId,
        string Provider,
        string Mode,
        string Status,
        string Counts,
        string WarningCategories,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);

    private sealed record DirectoryImportRuleRow(
        Guid Id,
        Guid DirectoryConnectionId,
        string Name,
        string Status,
        string StalePolicy,
        string RetainedFields,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record SubjectGroupRow(
        Guid Id,
        string Type,
        string Name,
        Guid? ParentGroupId,
        string Attributes);

    private sealed record CampaignRow(
        Guid Id,
        Guid? CampaignSeriesId,
        Guid TemplateVersionId,
        string Name,
        string Status,
        string ResponseIdentityMode,
        string DefaultLocale,
        DateTimeOffset? StartAt,
        DateTimeOffset? EndAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ClosedAt = null,
        Guid? ClosedByUserId = null,
        string? CloseReason = null);

    private sealed record TemplateSetupRow(
        Guid TemplateId,
        Guid TemplateVersionId,
        string TemplateName,
        string Semver,
        string Status,
        string DefaultLocale,
        Guid? InstrumentId,
        int QuestionCount);

    private sealed record ScoringSetupRow(
        Guid Id,
        Guid TemplateVersionId,
        string RuleKey,
        string RuleVersion,
        string Status,
        string Source);

    private sealed record ConsentPolicySetupRow(
        Guid Id,
        string Version,
        string Locale,
        string Title,
        string RequiredGrants,
        string OptionalGrants,
        DateTimeOffset PublishedAt);

    private sealed record RetentionPolicySetupRow(
        Guid Id,
        string Version,
        int RetainForYears,
        string RetentionStartEvent,
        string ActionAfter,
        DateOnly NextReviewAt);

    private sealed record DisclosurePolicySetupRow(
        Guid Id,
        string Version,
        int KMin,
        string SuppressionStrategy,
        string AppliesToDimensions);
}
