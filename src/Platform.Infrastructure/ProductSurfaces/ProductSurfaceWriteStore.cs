using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Auditing;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Integrations;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.ProductSurfaces;

public sealed class ProductSurfaceWriteStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    SubmittedResponseScoreMaterializer? scoreMaterializer = null) : IProductSurfaceWriteStore
{
    private readonly SubmittedResponseScoreMaterializer submittedScoreMaterializer =
        scoreMaterializer ?? new SubmittedResponseScoreMaterializer(db);
    private static readonly TimeSpan MicrosoftGraphConsentRequestLifetime = TimeSpan.FromMinutes(20);
    private const string MicrosoftGraphConsentCallbackPath =
        "/app/directory";

    public async Task<Result<TenantSettingsReportBrandingResponse>> UpdateTenantReportBrandingAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantReportBrandingRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var tenant = await db.Tenants
            .SingleOrDefaultAsync(entity => entity.Id == tenantId && entity.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure<TenantSettingsReportBrandingResponse>(
                Error.NotFound("tenant.not_found", "Tenant was not found."));
        }

        try
        {
            tenant.UpdateReportBranding(
                request.OrganizationLabel,
                request.ReportTitle,
                request.AccentColorHex,
                request.LayoutVariant,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<TenantSettingsReportBrandingResponse>(
                Error.Validation("tenant_report_branding.invalid", exception.Message));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new TenantSettingsReportBrandingResponse(
            tenant.ReportBrandingOrganizationLabel ?? tenant.Name,
            tenant.ReportBrandingReportTitle ?? "Campaign series report",
            "tenant_settings",
            "none",
            tenant.ReportBrandingAccentColorHex ?? Tenant.DefaultReportBrandingAccentColorHex,
            tenant.ReportBrandingLayoutVariant ?? Tenant.DefaultReportBrandingLayoutVariant,
            [
                "logo_upload",
                "custom_fonts",
                "product_shell_theming"
            ]));
    }

    public async Task<Result<CampaignSeriesRenameResponse>> RenameCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        RenameCampaignSeriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesRenameResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (series.IsSample)
        {
            return Result.Failure<CampaignSeriesRenameResponse>(CreateSampleReadOnlyError());
        }

        try
        {
            series.Rename(request.Name, DateTimeOffset.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<CampaignSeriesRenameResponse>(
                Error.Validation("campaign_series.invalid", exception.Message));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignSeriesRenameResponse(
            series.Id,
            series.Name,
            series.UpdatedAt));
    }

    public async Task<Result<CampaignSeriesDuplicateResponse>> DuplicateCampaignSeriesAsync(
        Guid tenantId,
        Guid sourceCampaignSeriesId,
        Guid actorUserId,
        DuplicateCampaignSeriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var source = await db.CampaignSeries
            .SingleOrDefaultAsync(series => series.Id == sourceCampaignSeriesId, cancellationToken);

        if (source is null)
        {
            return Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (!source.IsSample)
        {
            return Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.Conflict("campaign_series.not_sample", "Only sample studies can be duplicated."));
        }

        CampaignSeries copy;
        try
        {
            copy = new CampaignSeries(
                PlatformIds.NewId(),
                tenantId,
                request.Name,
                RandomNumberGenerator.GetBytes(32),
                workspaceId: source.WorkspaceId,
                ethicsApprovalId: source.EthicsApprovalId,
                retentionUntil: source.RetentionUntil,
                studyPurpose: source.StudyPurpose,
                studyAudience: source.StudyAudience,
                studyDesignType: source.StudyDesignType,
                studyIntendedUse: source.StudyIntendedUse,
                studyInterpretationBoundary: source.StudyInterpretationBoundary,
                studyOwnerNotes: source.StudyOwnerNotes);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<CampaignSeriesDuplicateResponse>(
                Error.Validation("campaign_series.invalid", exception.Message));
        }

        db.CampaignSeries.Add(copy);

        var now = DateTimeOffset.UtcNow;
        var sourceConsentDocuments = await db.ConsentDocuments
            .AsNoTracking()
            .Where(document =>
                document.CampaignSeriesId == source.Id &&
                document.PublishedAt <= now &&
                (!document.RetiredAt.HasValue || document.RetiredAt.Value > now))
            .ToListAsync(cancellationToken);
        foreach (var document in sourceConsentDocuments)
        {
            db.ConsentDocuments.Add(new ConsentDocument(
                PlatformIds.NewId(),
                tenantId,
                copy.Id,
                document.Locale,
                document.Version,
                document.Title,
                document.BodyMarkdown,
                document.RequiredGrants,
                document.OptionalGrants,
                document.PublishedAt,
                document.RetiredAt));
        }

        var sourceRetentionPolicies = await db.RetentionPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.CampaignSeriesId == source.Id &&
                policy.CreatedAt <= now &&
                (!policy.RetiredAt.HasValue || policy.RetiredAt.Value > now))
            .ToListAsync(cancellationToken);
        foreach (var policy in sourceRetentionPolicies)
        {
            db.RetentionPolicies.Add(new RetentionPolicy(
                PlatformIds.NewId(),
                tenantId,
                copy.Id,
                policy.Version,
                policy.RetainForYears,
                policy.RetentionStartEvent,
                policy.ActionAfter,
                policy.NextReviewAt,
                policy.PublicationLimits,
                policy.CreatedAt,
                policy.RetiredAt));
        }

        var sourceDisclosurePolicies = await db.DisclosurePolicies
            .AsNoTracking()
            .Where(policy =>
                policy.CampaignSeriesId == source.Id &&
                policy.CreatedAt <= now &&
                (!policy.RetiredAt.HasValue || policy.RetiredAt.Value > now))
            .ToListAsync(cancellationToken);
        foreach (var policy in sourceDisclosurePolicies)
        {
            db.DisclosurePolicies.Add(new DisclosurePolicy(
                PlatformIds.NewId(),
                tenantId,
                copy.Id,
                policy.Version,
                policy.KMin,
                policy.SuppressionStrategy,
                policy.AppliesToDimensions,
                policy.CreatedAt,
                policy.RetiredAt));
        }

        var sourceCampaigns = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.CampaignSeriesId == source.Id)
            .OrderBy(campaign => campaign.CreatedAt)
            .ThenBy(campaign => campaign.Id)
            .ToListAsync(cancellationToken);
        foreach (var campaign in sourceCampaigns)
        {
            db.Campaigns.Add(new Campaign(
                PlatformIds.NewId(),
                tenantId,
                campaign.TemplateVersionId,
                campaign.Name,
                campaign.ResponseIdentityMode,
                workspaceId: campaign.WorkspaceId,
                campaignSeriesId: copy.Id,
                status: CampaignStatuses.Draft,
                schedule: campaign.Schedule,
                defaultLocale: campaign.DefaultLocale));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignSeriesDuplicateResponse(
            copy.Id,
            copy.Name,
            copy.StudyKind,
            copy.IsSample,
            source.Id));
    }

    public async Task<Result<CampaignSeriesArchiveStateResponse>> ArchiveCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        ArchiveCampaignSeriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (series.IsSample)
        {
            return Result.Failure<CampaignSeriesArchiveStateResponse>(CreateSampleReadOnlyError());
        }

        series.Archive(request.Reason, actorUserId, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(CreateArchiveStateResponse(series));
    }

    public async Task<Result<CampaignSeriesArchiveStateResponse>> RestoreCampaignSeriesAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesArchiveStateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (series.IsSample)
        {
            return Result.Failure<CampaignSeriesArchiveStateResponse>(CreateSampleReadOnlyError());
        }

        series.Restore(DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(CreateArchiveStateResponse(series));
    }

    public async Task<Result<CampaignCloseStateResponse>> CloseCampaignAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid campaignId,
        Guid actorUserId,
        CloseCampaignRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignCloseStateResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (series.IsSample)
        {
            return Result.Failure<CampaignCloseStateResponse>(CreateSampleReadOnlyError());
        }

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(
                entity => entity.Id == campaignId && entity.CampaignSeriesId == campaignSeriesId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignCloseStateResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        try
        {
            campaign.Close(request.Reason, actorUserId, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<CampaignCloseStateResponse>(
                Error.Conflict("campaign.not_closeable", exception.Message));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(CreateCloseStateResponse(campaign));
    }

    public async Task<Result<CampaignSeriesScoreRemediationResponse>> RemediateCampaignSeriesScoresAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var series = await db.CampaignSeries
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Failure<CampaignSeriesScoreRemediationResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
        }

        if (series.IsSample)
        {
            return Result.Failure<CampaignSeriesScoreRemediationResponse>(CreateSampleReadOnlyError());
        }

        var submittedSessions = await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                join campaign in db.Campaigns.AsNoTracking()
                    on assignment.CampaignId equals campaign.Id
                where campaign.CampaignSeriesId == campaignSeriesId &&
                    session.SubmittedAt.HasValue
                select new SubmittedSessionForScoreRemediation(session.Id, campaign.Id))
            .ToListAsync(cancellationToken);

        if (submittedSessions.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new CampaignSeriesScoreRemediationResponse(
                campaignSeriesId,
                SubmittedResponseCount: 0,
                EligibleSubmittedResponseCount: 0,
                AlreadyScoredSubmittedResponseCount: 0,
                RemediatedSubmittedResponseCount: 0,
                SkippedNotConfiguredSubmittedResponseCount: 0,
                FailedSubmittedResponseCount: 0,
                LatestScoringActivityAt: null));
        }

        var sessionIds = submittedSessions
            .Select(session => session.SessionId)
            .ToArray();
        var alreadyScoredSessionIds = await db.ScoreRuns
            .AsNoTracking()
            .Where(run =>
                sessionIds.Contains(run.ResponseSessionId) &&
                run.Status == ScoreRunStatuses.Success)
            .Select(run => run.ResponseSessionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var alreadyScored = alreadyScoredSessionIds.ToHashSet();
        var remediatedSubmittedResponseCount = 0;
        var skippedNotConfiguredSubmittedResponseCount = 0;

        foreach (var session in submittedSessions)
        {
            if (alreadyScored.Contains(session.SessionId))
            {
                continue;
            }

            var materialized = await submittedScoreMaterializer.MaterializeAsync(
                tenantId,
                session.SessionId,
                requireScoringRule: false,
                cancellationToken);

            if (materialized.IsFailure)
            {
                db.ChangeTracker.Clear();

                return Result.Failure<CampaignSeriesScoreRemediationResponse>(
                    Error.Conflict(
                        "score_remediation.failed",
                        "Score remediation failed before changes were committed."));
            }

            if (materialized.Value.ScoreRunId.HasValue)
            {
                remediatedSubmittedResponseCount++;
            }
            else
            {
                skippedNotConfiguredSubmittedResponseCount++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var campaignIds = submittedSessions
            .Select(session => session.CampaignId)
            .Distinct()
            .ToArray();
        var latestScoringActivityAt = await db.ScoreRuns
            .AsNoTracking()
            .Where(run =>
                campaignIds.Contains(run.CampaignId) &&
                run.Status == ScoreRunStatuses.Success)
            .MaxAsync(run => (DateTimeOffset?)run.RanAt, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignSeriesScoreRemediationResponse(
            campaignSeriesId,
            submittedSessions.Count,
            alreadyScored.Count + remediatedSubmittedResponseCount,
            alreadyScored.Count,
            remediatedSubmittedResponseCount,
            skippedNotConfiguredSubmittedResponseCount,
            FailedSubmittedResponseCount: 0,
            latestScoringActivityAt));
    }

    public async Task<Result<TenantMemberMutationResponse>> CreateTenantMemberAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateTenantMemberRequest request,
        CancellationToken cancellationToken)
    {
        var emailResult = NormalizeEmail(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<TenantMemberMutationResponse>(emailResult.Error);
        }

        var roleCode = NormalizeRoleCode(request.RoleCode);
        var locale = NormalizeLocale(request.Locale);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var role = await db.Roles
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Code == roleCode,
                cancellationToken);

        if (role is null)
        {
            return Result.Failure<TenantMemberMutationResponse>(
                Error.Validation("tenant_role.unknown", "Tenant role is not assignable."));
        }

        var user = await db.UserAccounts
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.Email == emailResult.Value &&
                    entity.DeletedAt == null,
                cancellationToken);

        if (user is null)
        {
            user = new UserAccount(
                PlatformIds.NewId(),
                tenantId,
                emailResult.Value,
                locale);
            db.UserAccounts.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAssignment = await db.RoleAssignments.AnyAsync(
            assignment =>
                assignment.TenantId == tenantId &&
                assignment.UserId == user.Id &&
                assignment.RoleId == role.Id &&
                assignment.ScopeType == RoleAssignmentScopes.Tenant,
            cancellationToken);

        if (!hasAssignment)
        {
            db.RoleAssignments.Add(new RoleAssignment(
                PlatformIds.NewId(),
                tenantId,
                user.Id,
                role.Id,
                RoleAssignmentScopes.Tenant,
                grantedBy: actorUserId));
            await db.SaveChangesAsync(cancellationToken);
        }

        var member = await LoadTenantMemberAsync(tenantId, user.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new TenantMemberMutationResponse(member));
    }

    public async Task<Result<TenantMemberMutationResponse>> ChangeTenantMemberRoleAsync(
        Guid tenantId,
        Guid targetUserId,
        Guid actorUserId,
        ChangeTenantMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (targetUserId == actorUserId)
        {
            return Result.Failure<TenantMemberMutationResponse>(
                Error.Conflict("tenant_member.self_role_change", "You cannot change your own tenant role."));
        }

        var roleCode = NormalizeRoleCode(request.RoleCode);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var userExists = await db.UserAccounts.AnyAsync(
            user =>
                user.TenantId == tenantId &&
                user.Id == targetUserId &&
                user.DeletedAt == null,
            cancellationToken);

        if (!userExists)
        {
            return Result.Failure<TenantMemberMutationResponse>(
                Error.NotFound("tenant_member.not_found", "Tenant member was not found."));
        }

        var role = await db.Roles.SingleOrDefaultAsync(
            entity => entity.TenantId == tenantId && entity.Code == roleCode,
            cancellationToken);

        if (role is null)
        {
            return Result.Failure<TenantMemberMutationResponse>(
                Error.Validation("tenant_role.unknown", "Tenant role is not assignable."));
        }

        var tenantAssignments = await db.RoleAssignments
            .Where(assignment =>
                assignment.TenantId == tenantId &&
                assignment.UserId == targetUserId &&
                assignment.ScopeType == RoleAssignmentScopes.Tenant)
            .ToListAsync(cancellationToken);

        db.RoleAssignments.RemoveRange(tenantAssignments);
        db.RoleAssignments.Add(new RoleAssignment(
            PlatformIds.NewId(),
            tenantId,
            targetUserId,
            role.Id,
            RoleAssignmentScopes.Tenant,
            grantedBy: actorUserId));

        var now = DateTimeOffset.UtcNow;
        var activeSessions = await db.AuthSessions
            .Where(session =>
                session.TenantId == tenantId &&
                session.UserId == targetUserId &&
                session.RevokedAt == null &&
                session.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(now, "role_changed");
        }

        await db.SaveChangesAsync(cancellationToken);
        var member = await LoadTenantMemberAsync(tenantId, targetUserId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new TenantMemberMutationResponse(member));
    }

    public async Task<Result<SubjectDirectoryItemResponse>> CreateSubjectAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var emailResult = NormalizeOptionalSubjectEmail(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(emailResult.Error);
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        Subject subject;
        try
        {
            subject = new Subject(
                PlatformIds.NewId(),
                tenantId,
                externalId: request.ExternalId,
                email: emailResult.Value,
                displayName: request.DisplayName,
                locale: NormalizeLocale(request.Locale),
                attributes: request.Attributes);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(CreateSubjectValidationError(exception));
        }

        db.Subjects.Add(subject);
        await db.SaveChangesAsync(cancellationToken);
        var response = await LoadSubjectDirectoryItemAsync(tenantId, subject.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<SubjectDirectoryItemResponse>> UpdateSubjectAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var emailResult = NormalizeOptionalSubjectEmail(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(emailResult.Error);
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var subject = await db.Subjects.SingleOrDefaultAsync(
            entity => entity.TenantId == tenantId && entity.Id == subjectId && entity.DeletedAt == null,
            cancellationToken);

        if (subject is null)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        try
        {
            subject.ChangeDirectoryProfile(
                request.DisplayName,
                emailResult.Value,
                request.ExternalId,
                NormalizeLocale(request.Locale),
                request.Attributes);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(CreateSubjectValidationError(exception));
        }

        await db.SaveChangesAsync(cancellationToken);
        var response = await LoadSubjectDirectoryItemAsync(tenantId, subject.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<SubjectDirectoryCsvImportResponse>> ImportSubjectDirectoryCsvAsync(
        Guid tenantId,
        Guid actorUserId,
        SubjectDirectoryCsvImportRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = ParseSubjectDirectoryCsv(request.CsvContent);
        if (parsed.IsFailure)
        {
            return Result.Failure<SubjectDirectoryCsvImportResponse>(parsed.Error);
        }

        var sourceExternalIdPrefix = NormalizeOptionalSourceExternalIdPrefix(request.SourceExternalIdPrefix);
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);
        var dryRun = request.DryRun;
        var graphImportRunResult = await CreateGraphDirectoryImportRunAsync(
            tenantId,
            request,
            sourceExternalIdPrefix,
            dryRun,
            cancellationToken);
        if (graphImportRunResult.IsFailure)
        {
            return Result.Failure<SubjectDirectoryCsvImportResponse>(graphImportRunResult.Error);
        }

        var graphImportRun = graphImportRunResult.Value;

        var existingSubjects = await db.Subjects
            .Where(subject => subject.TenantId == tenantId && subject.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var subjectsByExternalId = existingSubjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject.ExternalId))
            .GroupBy(subject => subject.ExternalId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var subjectsByEmail = existingSubjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject.Email))
            .GroupBy(subject => subject.Email!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var groupsByKey = await db.SubjectGroups
            .Where(group => group.TenantId == tenantId && group.DeletedAt == null)
            .OrderBy(group => group.Name)
            .ThenBy(group => group.Id)
            .ToListAsync(cancellationToken);
        var groups = groupsByKey
            .GroupBy(group => SubjectGroupImportKey(group.Type, group.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var membershipKeys = await (
                from membership in db.SubjectMemberships
                join subjectGroup in db.SubjectGroups on membership.GroupId equals subjectGroup.Id
                where subjectGroup.TenantId == tenantId && subjectGroup.DeletedAt == null
                select new SubjectMembershipImportKey(membership.SubjectId, membership.GroupId))
            .ToListAsync(cancellationToken);
        var memberships = membershipKeys.ToHashSet();

        var rows = new List<SubjectDirectoryCsvImportRowResponse>();
        var createdSubjectCount = 0;
        var updatedSubjectCount = 0;
        var createdGroupCount = 0;
        var addedMembershipCount = 0;
        var skippedMembershipCount = 0;
        var setManagerRelationshipCount = 0;
        var skippedManagerRelationshipCount = 0;
        var missingManagerReferenceCount = 0;
        var markedStaleSubjectCount = 0;
        var clearedStaleSubjectCount = 0;
        var appliedManagerKeys = new HashSet<(Guid SubjectId, Guid ManagerId)>();
        var sourceExternalIdsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsed.Value)
        {
            var values = NormalizeImportRow(row.Values);
            var issues = ValidateImportRow(values);
            var actions = new List<string>();
            if (sourceExternalIdPrefix is not null &&
                values.ExternalId is not null &&
                values.ExternalId.StartsWith(sourceExternalIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                sourceExternalIdsSeen.Add(values.ExternalId);
            }

            Subject? subject = null;
            if (issues.Count == 0)
            {
                var externalMatch = values.ExternalId is not null &&
                    subjectsByExternalId.TryGetValue(values.ExternalId, out var externalSubject)
                        ? externalSubject
                        : null;
                var emailMatch = values.Email is not null &&
                    subjectsByEmail.TryGetValue(values.Email, out var emailSubject)
                        ? emailSubject
                        : null;

                if (externalMatch is not null && emailMatch is not null && externalMatch.Id != emailMatch.Id)
                {
                    issues.Add("External id and email match different people already in the directory.");
                }
                else
                {
                    subject = externalMatch ?? emailMatch;
                }
            }

            if (issues.Count > 0)
            {
                rows.Add(CreateImportRowResponse(row.RowNumber, "failed", values, "none", issues));
                continue;
            }

            if (subject is null)
            {
                subject = new Subject(
                    PlatformIds.NewId(),
                    tenantId,
                    externalId: values.ExternalId,
                    email: values.Email,
                    displayName: values.DisplayName,
                    locale: values.Locale,
                    attributes: "{}");
                if (!dryRun)
                {
                    db.Subjects.Add(subject);
                }

                createdSubjectCount++;
                actions.Add("created_subject");
            }
            else
            {
                var attributes = subject.Attributes;
                var clearedStale = false;
                if (sourceExternalIdPrefix is not null &&
                    values.ExternalId is not null &&
                    values.ExternalId.StartsWith(sourceExternalIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var clearResult = ClearDirectoryImportStaleMarker(attributes);
                    attributes = clearResult.Attributes;
                    clearedStale = clearResult.Changed;
                }

                if (!dryRun)
                {
                    subject.ChangeDirectoryProfile(
                        values.DisplayName ?? subject.DisplayName,
                        values.Email ?? subject.Email,
                        values.ExternalId ?? subject.ExternalId,
                        values.Locale,
                        attributes);
                }

                if (clearedStale)
                {
                    clearedStaleSubjectCount++;
                    actions.Add("cleared_stale");
                }

                updatedSubjectCount++;
                actions.Add("updated_subject");
            }

            if (values.ExternalId is not null)
            {
                subjectsByExternalId[values.ExternalId] = subject;
            }

            if (values.Email is not null)
            {
                subjectsByEmail[values.Email] = subject;
            }

            if (values.GroupName is not null)
            {
                var groupKey = SubjectGroupImportKey(values.GroupType, values.GroupName);
                if (!groups.TryGetValue(groupKey, out var group))
                {
                    group = new SubjectGroup(
                        PlatformIds.NewId(),
                        tenantId,
                        values.GroupType,
                        values.GroupName);
                    if (!dryRun)
                    {
                        db.SubjectGroups.Add(group);
                    }

                    groups[groupKey] = group;
                    createdGroupCount++;
                    actions.Add("created_group");
                }

                var membershipKey = new SubjectMembershipImportKey(subject.Id, group.Id);
                if (memberships.Contains(membershipKey))
                {
                    skippedMembershipCount++;
                    actions.Add("skipped_membership");
                }
                else
                {
                    if (!dryRun)
                    {
                        db.SubjectMemberships.Add(new SubjectMembership(
                            subject.Id,
                            group.Id,
                            values.RoleInGroup));
                    }

                    memberships.Add(membershipKey);
                    addedMembershipCount++;
                    actions.Add("added_membership");
                }
            }

            if (values.ManagerExternalId is not null)
            {
                if (!subjectsByExternalId.TryGetValue(values.ManagerExternalId, out var managerSubject))
                {
                    actions.Add("manager_not_imported");
                    issues.Add("manager_external_id did not match an imported or existing subject.");
                    missingManagerReferenceCount++;
                }
                else if (appliedManagerKeys.Contains((subject.Id, managerSubject.Id)))
                {
                    actions.Add("skipped_manager");
                    skippedManagerRelationshipCount++;
                }
                else
                {
                    var activeManagerRelationships = await db.SubjectRelationships
                        .Where(relationship =>
                            relationship.TenantId == tenantId &&
                            relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                            relationship.RelatedSubjectId == subject.Id &&
                            relationship.ValidTo == null)
                        .ToListAsync(cancellationToken);
                    var hasSameManager = activeManagerRelationships.Any(
                        relationship => relationship.SubjectId == managerSubject.Id);
                    var effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    foreach (var relationship in activeManagerRelationships)
                    {
                        if (relationship.SubjectId == managerSubject.Id)
                        {
                            continue;
                        }

                        relationship.End(ClampEndDate(relationship.ValidFrom, effectiveDate));
                    }

                    if (hasSameManager)
                    {
                        actions.Add("skipped_manager");
                        skippedManagerRelationshipCount++;
                    }
                    else
                    {
                        db.SubjectRelationships.Add(new SubjectRelationship(
                            PlatformIds.NewId(),
                            tenantId,
                            managerSubject.Id,
                            subject.Id,
                            SubjectRelationshipTypes.ManagerOf));
                        actions.Add("set_manager");
                        setManagerRelationshipCount++;
                    }

                    appliedManagerKeys.Add((subject.Id, managerSubject.Id));
                }
            }

            rows.Add(CreateImportRowResponse(row.RowNumber, "imported", values, string.Join(",", actions), issues));
        }

        if (request.MarkMissingSubjectsStale && sourceExternalIdPrefix is not null)
        {
            var staleCandidates = existingSubjects
                .Where(subject =>
                    subject.ExternalId is not null &&
                    subject.ExternalId.StartsWith(sourceExternalIdPrefix, StringComparison.OrdinalIgnoreCase) &&
                    !sourceExternalIdsSeen.Contains(subject.ExternalId))
                .ToArray();

            foreach (var subject in staleCandidates)
            {
                var staleResult = MarkDirectoryImportStale(subject.Attributes, sourceExternalIdPrefix);
                if (!staleResult.Changed)
                {
                    continue;
                }

                markedStaleSubjectCount++;
                if (!dryRun)
                {
                    subject.ReplaceAttributes(staleResult.Attributes);
                }
            }
        }

        var importAuditEventId = PlatformIds.NewId();
        db.AuditEvents.Add(CreateSubjectDirectoryImportAuditEvent(
            importAuditEventId,
            tenantId,
            actorUserId,
            dryRun,
            sourceExternalIdPrefix,
            parsed.Value.Count,
            rows.Count(row => row.Status == "imported"),
            rows.Count(row => row.Status == "failed"),
            createdSubjectCount,
            updatedSubjectCount,
            createdGroupCount,
            addedMembershipCount,
            skippedMembershipCount,
            setManagerRelationshipCount,
            skippedManagerRelationshipCount,
            missingManagerReferenceCount,
            markedStaleSubjectCount,
            clearedStaleSubjectCount));
        graphImportRun?.Succeed(
            CreateDirectoryImportRunCountsJson(
                parsed.Value.Count,
                rows.Count(row => row.Status == "imported"),
                rows.Count(row => row.Status == "failed"),
                createdSubjectCount,
                updatedSubjectCount,
                createdGroupCount,
                addedMembershipCount,
                skippedMembershipCount,
                setManagerRelationshipCount,
                skippedManagerRelationshipCount,
                missingManagerReferenceCount,
                markedStaleSubjectCount,
                clearedStaleSubjectCount),
            CreateDirectoryImportRunWarningCategoriesJson(
                rows.Count(row => row.Status == "failed"),
                missingManagerReferenceCount,
                markedStaleSubjectCount),
            CreateDirectoryImportRunCheckpointJson(dryRun),
            DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SubjectDirectoryCsvImportResponse(
            tenantId,
            parsed.Value.Count,
            rows.Count(row => row.Status == "imported"),
            createdSubjectCount,
            updatedSubjectCount,
            createdGroupCount,
            addedMembershipCount,
            skippedMembershipCount,
            rows,
            dryRun,
            setManagerRelationshipCount,
            skippedManagerRelationshipCount,
            missingManagerReferenceCount,
            markedStaleSubjectCount,
            clearedStaleSubjectCount,
            importAuditEventId,
            graphImportRun?.Id));
    }

    public async Task<Result<MicrosoftGraphConsentRequestResponse>> CreateMicrosoftGraphConsentRequestAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateMicrosoftGraphConsentRequest request,
        CancellationToken cancellationToken)
    {
        var requestedScopes = NormalizeMicrosoftGraphConsentScopes(request.RequestedScopes);
        var now = DateTimeOffset.UtcNow;
        var rawState = CreateUrlSafeSecret();
        var rawNonce = CreateUrlSafeSecret();
        var stateHash = HashConsentSecret(rawState);
        var nonceHash = HashConsentSecret(rawNonce);
        var requestedScopesJson = JsonSerializer.Serialize(requestedScopes);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var connection = await db.DirectoryConnections
            .SingleOrDefaultAsync(
                row =>
                    row.TenantId == tenantId &&
                    row.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    row.DeletedAt == null,
                cancellationToken);

        if (connection is null)
        {
            connection = new DirectoryConnection(
                PlatformIds.NewId(),
                tenantId,
                DirectoryConnectionProviders.MicrosoftGraph,
                externalTenantId: null,
                displayName: "Microsoft Graph",
                primaryDomain: null,
                grantedScopes: "[]",
                status: DirectoryConnectionStatuses.PendingConsent,
                createdByUserId: actorUserId,
                observedAt: now);
            db.DirectoryConnections.Add(connection);
        }
        else
        {
            connection.MarkPendingConsent(now);
        }

        var consentRequest = new DirectoryConnectionConsentRequest(
            PlatformIds.NewId(),
            tenantId,
            DirectoryConnectionProviders.MicrosoftGraph,
            stateHash,
            nonceHash,
            now.Add(MicrosoftGraphConsentRequestLifetime),
            requestedScopesJson,
            connection.Id,
            actorUserId,
            now);
        db.DirectoryConnectionConsentRequests.Add(consentRequest);
        db.AuditEvents.Add(CreateMicrosoftGraphConsentAuditEvent(
            PlatformIds.NewId(),
            tenantId,
            actorUserId,
            consentRequest.Id,
            connection.Id,
            "requested",
            connection.Status,
            requestedScopes.Count,
            externalTenantIdPresent: false,
            failureCategory: null));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new MicrosoftGraphConsentRequestResponse(
            tenantId,
            consentRequest.Id,
            connection.Id,
            DirectoryConnectionProviders.MicrosoftGraph,
            consentRequest.Status,
            requestedScopes,
            consentRequest.ExpiresAt,
            rawState,
            rawNonce,
            MicrosoftGraphConsentCallbackPath));
    }

    public async Task<Result<MicrosoftGraphConsentCallbackResponse>> CompleteMicrosoftGraphConsentCallbackAsync(
        Guid tenantId,
        Guid actorUserId,
        CompleteMicrosoftGraphConsentCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var stateHash = HashConsentSecret(request.State);
        var nonceHash = string.IsNullOrWhiteSpace(request.Nonce)
            ? null
            : HashConsentSecret(request.Nonce);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var consentRequest = await db.DirectoryConnectionConsentRequests
            .SingleOrDefaultAsync(
                row =>
                    row.TenantId == tenantId &&
                    row.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    row.StateHash == stateHash,
                cancellationToken);

        if (consentRequest is null)
        {
            return Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.NotFound("directory_connection_consent.not_found", "Microsoft Graph consent request was not found."));
        }

        if (nonceHash is not null && consentRequest.NonceHash != nonceHash)
        {
            return Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.Forbidden("directory_connection_consent.invalid_nonce", "Microsoft Graph consent request did not match."));
        }

        if (consentRequest.Status != DirectoryConnectionConsentRequestStatuses.Pending)
        {
            return Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.Conflict("directory_connection_consent.not_pending", "Microsoft Graph consent request is no longer pending."));
        }

        var connection = consentRequest.DirectoryConnectionId.HasValue
            ? await db.DirectoryConnections.SingleOrDefaultAsync(
                row => row.Id == consentRequest.DirectoryConnectionId.Value && row.TenantId == tenantId,
                cancellationToken)
            : null;

        if (consentRequest.ExpiresAt <= now)
        {
            consentRequest.Expire(now);
            connection?.MarkConsentRequired(now);
            db.AuditEvents.Add(CreateMicrosoftGraphConsentAuditEvent(
                PlatformIds.NewId(),
                tenantId,
                actorUserId,
                consentRequest.Id,
                connection?.Id,
                "expired",
                connection?.Status ?? DirectoryConnectionStatuses.ConsentRequired,
                ReadStringArray(consentRequest.RequestedScopes).Count,
                externalTenantIdPresent: false,
                failureCategory: "expired"));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.Conflict("directory_connection_consent.expired", "Microsoft Graph consent request expired."));
        }

        var requestedScopes = ReadStringArray(consentRequest.RequestedScopes);
        var failureCategory = NormalizeConsentFailureCategory(request.Error);
        if (!request.AdminConsent || failureCategory is not null)
        {
            failureCategory ??= "admin_consent_denied";
            consentRequest.Fail(failureCategory, now);
            connection?.MarkConsentRequired(now);
            db.AuditEvents.Add(CreateMicrosoftGraphConsentAuditEvent(
                PlatformIds.NewId(),
                tenantId,
                actorUserId,
                consentRequest.Id,
                connection?.Id,
                "failed",
                connection?.Status ?? DirectoryConnectionStatuses.ConsentRequired,
                requestedScopes.Count,
                externalTenantIdPresent: false,
                failureCategory));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new MicrosoftGraphConsentCallbackResponse(
                tenantId,
                consentRequest.Id,
                connection?.Id,
                DirectoryConnectionProviders.MicrosoftGraph,
                consentRequest.Status,
                connection?.Status ?? DirectoryConnectionStatuses.ConsentRequired,
                Connected: false));
        }

        if (string.IsNullOrWhiteSpace(request.MicrosoftTenantId))
        {
            return Result.Failure<MicrosoftGraphConsentCallbackResponse>(
                Error.Validation(
                    "directory_connection_consent.microsoft_tenant_required",
                    "Microsoft tenant id is required when admin consent succeeds."));
        }

        if (connection is null)
        {
            connection = new DirectoryConnection(
                PlatformIds.NewId(),
                tenantId,
                DirectoryConnectionProviders.MicrosoftGraph,
                externalTenantId: null,
                displayName: "Microsoft Graph",
                primaryDomain: null,
                grantedScopes: "[]",
                status: DirectoryConnectionStatuses.PendingConsent,
                createdByUserId: actorUserId,
                observedAt: now);
            db.DirectoryConnections.Add(connection);
        }

        connection.Activate(
            request.MicrosoftTenantId,
            string.IsNullOrWhiteSpace(request.DisplayName) ? "Microsoft Graph" : request.DisplayName!,
            request.PrimaryDomain,
            consentRequest.RequestedScopes,
            now);
        consentRequest.Complete(now);
        db.AuditEvents.Add(CreateMicrosoftGraphConsentAuditEvent(
            PlatformIds.NewId(),
            tenantId,
            actorUserId,
            consentRequest.Id,
            connection.Id,
            "completed",
            connection.Status,
            requestedScopes.Count,
            externalTenantIdPresent: true,
            failureCategory: null));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new MicrosoftGraphConsentCallbackResponse(
            tenantId,
            consentRequest.Id,
            connection.Id,
            DirectoryConnectionProviders.MicrosoftGraph,
            consentRequest.Status,
            connection.Status,
            Connected: true));
    }

    public async Task<Result<DirectoryImportRuleResponse>> SaveMicrosoftGraphDirectoryImportRuleAsync(
        Guid tenantId,
        Guid actorUserId,
        SaveMicrosoftGraphImportRuleRequest request,
        CancellationToken cancellationToken)
    {
        var retainedFields = NormalizeMicrosoftGraphImportRetainedFields(request.RetainedFields);
        var retainedFieldsJson = JsonSerializer.Serialize(retainedFields);
        var stalePolicy = request.MarkMissingSubjectsStale
            ? DirectoryImportStalePolicies.MarkStale
            : DirectoryImportStalePolicies.None;
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var connection = await GetOrCreateMicrosoftGraphRuleConnectionAsync(
            tenantId,
            now,
            cancellationToken);
        var rule = new DirectoryImportRule(
            PlatformIds.NewId(),
            tenantId,
            connection.Id,
            request.Name,
            CreateMicrosoftGraphImportRuleDocumentJson(request.MarkMissingSubjectsStale),
            retainedFieldsJson,
            stalePolicy,
            DirectoryImportRuleStatuses.Active,
            createdByUserId: null,
            observedAt: now);
        db.DirectoryImportRules.Add(rule);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(CreateDirectoryImportRuleResponse(rule, retainedFields));
    }

    public async Task<Result<DirectoryImportRuleResponse>> ArchiveMicrosoftGraphDirectoryImportRuleAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var rule = await (
                from candidate in db.DirectoryImportRules
                join connection in db.DirectoryConnections
                    on new { Id = candidate.DirectoryConnectionId, candidate.TenantId }
                    equals new { connection.Id, connection.TenantId }
                where candidate.Id == ruleId &&
                    candidate.TenantId == tenantId &&
                    candidate.DeletedAt == null &&
                    connection.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                    connection.DeletedAt == null
                select candidate)
            .SingleOrDefaultAsync(cancellationToken);
        if (rule is null)
        {
            return Result.Failure<DirectoryImportRuleResponse>(
                Error.NotFound("directory_import_rule.not_found", "Graph import rule was not found."));
        }

        rule.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(CreateDirectoryImportRuleResponse(
            rule,
            ReadStringArray(rule.RetainedFields)));
    }

    public async Task<Result<SubjectGroupResponse>> CreateSubjectGroupAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateSubjectGroupRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        if (request.ParentGroupId.HasValue)
        {
            var parentExists = await db.SubjectGroups.AnyAsync(
                group =>
                    group.TenantId == tenantId &&
                    group.Id == request.ParentGroupId.Value &&
                    group.DeletedAt == null,
                cancellationToken);

            if (!parentExists)
            {
                return Result.Failure<SubjectGroupResponse>(
                    Error.NotFound("subject_group.parent_not_found", "Parent subject group was not found."));
            }
        }

        SubjectGroup group;
        try
        {
            group = new SubjectGroup(
                PlatformIds.NewId(),
                tenantId,
                request.Type,
                request.Name,
                parentGroupId: request.ParentGroupId,
                attributes: request.Attributes);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubjectGroupResponse>(CreateSubjectGroupValidationError(exception));
        }

        db.SubjectGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        var response = await LoadSubjectGroupResponseAsync(tenantId, group.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<SubjectGroupMembershipResponse>> AddSubjectGroupMemberAsync(
        Guid tenantId,
        Guid groupId,
        Guid actorUserId,
        AddSubjectGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var group = await db.SubjectGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == groupId && entity.DeletedAt == null,
                cancellationToken);
        if (group is null)
        {
            return Result.Failure<SubjectGroupMembershipResponse>(
                Error.NotFound("subject_group.not_found", "Subject group was not found."));
        }

        var subjectExists = await db.Subjects.AnyAsync(
            subject =>
                subject.TenantId == tenantId &&
                subject.Id == request.SubjectId &&
                subject.DeletedAt == null,
            cancellationToken);
        if (!subjectExists)
        {
            return Result.Failure<SubjectGroupMembershipResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        var membership = await db.SubjectMemberships.SingleOrDefaultAsync(
            entity => entity.GroupId == groupId && entity.SubjectId == request.SubjectId,
            cancellationToken);
        if (membership is null)
        {
            try
            {
                membership = new SubjectMembership(
                    request.SubjectId,
                    groupId,
                    request.RoleInGroup,
                    request.ValidFrom,
                    request.ValidTo);
            }
            catch (ArgumentException exception)
            {
                return Result.Failure<SubjectGroupMembershipResponse>(
                    Error.Validation("subject_membership.invalid", exception.Message));
            }

            db.SubjectMemberships.Add(membership);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SubjectGroupMembershipResponse(
            group.Id,
            group.Type,
            group.Name,
            membership.RoleInGroup,
            membership.ValidFrom,
            membership.ValidTo));
    }

    public async Task<Result<SubjectDirectoryItemResponse>> SetSubjectManagerAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        SetSubjectManagerRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var subjectExists = await db.Subjects.AnyAsync(
            subject => subject.TenantId == tenantId && subject.Id == subjectId && subject.DeletedAt == null,
            cancellationToken);
        if (!subjectExists)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(
                Error.NotFound("subject.not_found", "Subject was not found."));
        }

        if (request.ManagerSubjectId == subjectId)
        {
            return Result.Failure<SubjectDirectoryItemResponse>(
                Error.Validation("subject_manager.self", "A subject cannot manage itself."));
        }

        if (request.ManagerSubjectId.HasValue)
        {
            var managerExists = await db.Subjects.AnyAsync(
                subject =>
                    subject.TenantId == tenantId &&
                    subject.Id == request.ManagerSubjectId.Value &&
                    subject.DeletedAt == null,
                cancellationToken);

            if (!managerExists)
            {
                return Result.Failure<SubjectDirectoryItemResponse>(
                    Error.NotFound("subject_manager.not_found", "Manager subject was not found."));
            }
        }

        var effectiveDate = request.ValidFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var activeRelationships = await db.SubjectRelationships
            .Where(relationship =>
                relationship.TenantId == tenantId &&
                relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                relationship.RelatedSubjectId == subjectId &&
                relationship.ValidTo == null)
            .ToListAsync(cancellationToken);

        foreach (var relationship in activeRelationships)
        {
            if (request.ManagerSubjectId.HasValue && relationship.SubjectId == request.ManagerSubjectId.Value)
            {
                continue;
            }

            relationship.End(ClampEndDate(relationship.ValidFrom, effectiveDate));
        }

        if (request.ManagerSubjectId.HasValue &&
            activeRelationships.All(relationship => relationship.SubjectId != request.ManagerSubjectId.Value))
        {
            db.SubjectRelationships.Add(new SubjectRelationship(
                PlatformIds.NewId(),
                tenantId,
                request.ManagerSubjectId.Value,
                subjectId,
                SubjectRelationshipTypes.ManagerOf,
                validFrom: request.ValidFrom));
        }

        await db.SaveChangesAsync(cancellationToken);
        var response = await LoadSubjectDirectoryItemAsync(tenantId, subjectId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<TenantEmailTemplateSettingsResponse>> UpdateTenantEmailTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        string templateCode,
        string locale,
        UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeEmailTemplateCode(templateCode);
        if (normalizedTemplateCode is null)
        {
            return Result.Failure<TenantEmailTemplateSettingsResponse>(
                Error.Validation("email_template.template_code_invalid", "Email template code must be invitation or reminder."));
        }

        if (!EmailTemplateLocales.IsSupported(locale))
        {
            return Result.Failure<TenantEmailTemplateSettingsResponse>(
                Error.Validation("email_template.locale_invalid", "Email template locale must be en or hr-HR."));
        }

        var normalizedLocale = EmailTemplateLocales.Normalize(locale);
        var content = new EmailTemplateContent(
            normalizedTemplateCode,
            normalizedLocale,
            request.Subject?.Trim() ?? string.Empty,
            request.BodyText?.Trim() ?? string.Empty,
            IsBuiltInDefault: false);
        var validation = EmailTemplateValidator.Validate(content);
        if (!validation.IsValid)
        {
            return Result.Failure<TenantEmailTemplateSettingsResponse>(
                CreateEmailTemplateValidationError(validation));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var tenantExists = await db.Tenants.AnyAsync(
            tenant => tenant.Id == tenantId && tenant.DeletedAt == null,
            cancellationToken);
        if (!tenantExists)
        {
            return Result.Failure<TenantEmailTemplateSettingsResponse>(
                Error.NotFound("tenant.not_found", "Tenant was not found."));
        }

        var existing = await db.EmailTemplates.SingleOrDefaultAsync(
            template =>
                template.TenantId == tenantId &&
                template.TemplateCode == normalizedTemplateCode &&
                template.Locale == normalizedLocale,
            cancellationToken);
        var updatedAt = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.EmailTemplates.Add(new EmailTemplate(
                PlatformIds.NewId(),
                tenantId,
                normalizedTemplateCode,
                normalizedLocale,
                content.Subject,
                content.BodyText));
        }
        else
        {
            existing.UpdateContent(content.Subject, content.BodyText, updatedAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(TenantEmailTemplateSettingsFactory.FromContent(
            content,
            isCustom: true));
    }

    public async Task<Result<ResetEmailTemplateResponse>> ResetTenantEmailTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        string templateCode,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeEmailTemplateCode(templateCode);
        if (normalizedTemplateCode is null)
        {
            return Result.Failure<ResetEmailTemplateResponse>(
                Error.Validation("email_template.template_code_invalid", "Email template code must be invitation or reminder."));
        }

        if (!EmailTemplateLocales.IsSupported(locale))
        {
            return Result.Failure<ResetEmailTemplateResponse>(
                Error.Validation("email_template.locale_invalid", "Email template locale must be en or hr-HR."));
        }

        var normalizedLocale = EmailTemplateLocales.Normalize(locale);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var tenantExists = await db.Tenants.AnyAsync(
            tenant => tenant.Id == tenantId && tenant.DeletedAt == null,
            cancellationToken);
        if (!tenantExists)
        {
            return Result.Failure<ResetEmailTemplateResponse>(
                Error.NotFound("tenant.not_found", "Tenant was not found."));
        }

        var existing = await db.EmailTemplates.SingleOrDefaultAsync(
            template =>
                template.TenantId == tenantId &&
                template.TemplateCode == normalizedTemplateCode &&
                template.Locale == normalizedLocale,
            cancellationToken);
        if (existing is not null)
        {
            db.EmailTemplates.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ResetEmailTemplateResponse(
            TenantEmailTemplateSettingsFactory.FromContent(
                EmailTemplateDefaults.Get(normalizedTemplateCode, normalizedLocale),
                isCustom: false)));
    }

    private static string? NormalizeEmailTemplateCode(string templateCode)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
        {
            return null;
        }

        var normalized = templateCode.Trim().ToLowerInvariant();
        return EmailTemplateCodes.IsKnown(normalized) ? normalized : null;
    }

    private static Error CreateEmailTemplateValidationError(EmailTemplateValidationResult validation)
    {
        var issue = validation.Issues.Count > 0
            ? validation.Issues[0]
            : new EmailTemplateValidationIssue(
                "email_template.invalid",
                "Email template is invalid.");

        return Error.Validation(issue.Code, issue.Message);
    }

    private static CampaignSeriesArchiveStateResponse CreateArchiveStateResponse(
        Platform.Domain.Campaigns.CampaignSeries series)
    {
        return new CampaignSeriesArchiveStateResponse(
            series.Id,
            series.Archived,
            series.UpdatedAt,
            series.ArchivedAt,
            series.ArchivedByUserId,
            series.ArchiveReason);
    }

    private static Error CreateSampleReadOnlyError()
    {
        return Error.Conflict(
            "campaign_series.sample_read_only",
            "Sample studies are read-only. Duplicate the sample before changing it.");
    }

    private async Task<TenantMemberResponse> LoadTenantMemberAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await db.UserAccounts
            .AsNoTracking()
            .Where(entity =>
                entity.TenantId == tenantId &&
                entity.Id == userId &&
                entity.DeletedAt == null)
            .Select(entity => new
            {
                entity.Id,
                entity.Email,
                entity.Locale,
                entity.CreatedAt,
                entity.LastLoginAt
            })
            .SingleAsync(cancellationToken);
        var roles = await (
                from assignment in db.RoleAssignments.AsNoTracking()
                join role in db.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                where assignment.TenantId == tenantId &&
                    assignment.UserId == userId &&
                    assignment.ScopeType == RoleAssignmentScopes.Tenant
                orderby role.Code
                select new TenantMemberRoleResponse(
                    role.Id,
                    role.Code,
                    role.Name,
                    assignment.ScopeType,
                    assignment.ScopeId,
                    assignment.GrantedAt))
            .ToArrayAsync(cancellationToken);
        var roleIds = roles.Select(role => role.RoleId).ToArray();
        var permissions = roleIds.Length == 0
            ? []
            : await (
                    from rolePermission in db.RolePermissions.AsNoTracking()
                    join permission in db.Permissions.AsNoTracking()
                        on rolePermission.PermissionId equals permission.Id
                    where roleIds.Contains(rolePermission.RoleId)
                    select permission.Code)
                .Distinct()
                .OrderBy(code => code)
                .ToArrayAsync(cancellationToken);
        var hasActiveIdentity = await db.ExternalAuthIdentities
            .AsNoTracking()
            .AnyAsync(identity =>
                identity.TenantId == tenantId &&
                identity.UserId == userId &&
                identity.DisabledAt == null,
                cancellationToken);

        return new TenantMemberResponse(
            user.Id,
            user.Email,
            user.Locale,
            user.CreatedAt,
            user.LastLoginAt,
            roles,
            permissions,
            hasActiveIdentity
                ? TenantMemberIdentityStatuses.Active
                : TenantMemberIdentityStatuses.PendingProviderLink);
    }

    private async Task<SubjectDirectoryItemResponse> LoadSubjectDirectoryItemAsync(
        Guid tenantId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var subject = await db.Subjects
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.Id == subjectId && entity.DeletedAt == null)
            .Select(entity => new
            {
                entity.Id,
                entity.DisplayName,
                entity.Email,
                entity.ExternalId,
                entity.Locale,
                entity.Attributes
            })
            .SingleAsync(cancellationToken);
        var groups = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subjectGroup in db.SubjectGroups.AsNoTracking()
                    on membership.GroupId equals subjectGroup.Id
                where membership.SubjectId == subjectId &&
                    subjectGroup.TenantId == tenantId &&
                    subjectGroup.DeletedAt == null
                orderby subjectGroup.Name
                select new SubjectGroupMembershipResponse(
                    subjectGroup.Id,
                    subjectGroup.Type,
                    subjectGroup.Name,
                    membership.RoleInGroup,
                    membership.ValidFrom,
                    membership.ValidTo))
            .ToArrayAsync(cancellationToken);
        var manager = await (
                from relationship in db.SubjectRelationships.AsNoTracking()
                join managerSubject in db.Subjects.AsNoTracking()
                    on relationship.SubjectId equals managerSubject.Id
                where relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.RelatedSubjectId == subjectId &&
                    relationship.ValidTo == null &&
                    managerSubject.TenantId == tenantId &&
                    managerSubject.DeletedAt == null
                select new
                {
                    managerSubject.Id,
                    managerSubject.DisplayName
                })
            .FirstOrDefaultAsync(cancellationToken);
        var directReportCount = await db.SubjectRelationships
            .AsNoTracking()
            .CountAsync(
                relationship =>
                    relationship.TenantId == tenantId &&
                    relationship.RelationshipType == SubjectRelationshipTypes.ManagerOf &&
                    relationship.SubjectId == subjectId &&
                    relationship.ValidTo == null,
                cancellationToken);

        return new SubjectDirectoryItemResponse(
            subject.Id,
            subject.DisplayName,
            subject.Email,
            subject.ExternalId,
            subject.Locale,
            subject.Attributes,
            manager?.Id,
            manager?.DisplayName,
            directReportCount,
            groups);
    }

    private async Task<SubjectGroupResponse> LoadSubjectGroupResponseAsync(
        Guid tenantId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await db.SubjectGroups
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.Id == groupId && entity.DeletedAt == null)
            .Select(entity => new
            {
                entity.Id,
                entity.Type,
                entity.Name,
                entity.ParentGroupId,
                entity.Attributes
            })
            .SingleAsync(cancellationToken);
        var memberCount = await (
                from membership in db.SubjectMemberships.AsNoTracking()
                join subject in db.Subjects.AsNoTracking()
                    on membership.SubjectId equals subject.Id
                where membership.GroupId == groupId &&
                    subject.TenantId == tenantId &&
                    subject.DeletedAt == null
                select membership)
            .CountAsync(cancellationToken);

        return new SubjectGroupResponse(
            group.Id,
            group.Type,
            group.Name,
            group.ParentGroupId,
            group.Attributes,
            memberCount);
    }

    private static IReadOnlyList<string> NormalizeMicrosoftGraphConsentScopes(IReadOnlyList<string>? requestedScopes)
    {
        var sourceScopes = requestedScopes is { Count: > 0 }
            ? requestedScopes
            : MicrosoftGraphDirectoryConsentScopes.Default;
        var normalizedScopes = new List<string>();
        foreach (var requestedScope in sourceScopes)
        {
            if (string.IsNullOrWhiteSpace(requestedScope))
            {
                continue;
            }

            var normalizedScope = requestedScope.Trim();
            if (!MicrosoftGraphDirectoryConsentScopes.IsKnown(normalizedScope) ||
                normalizedScopes.Contains(normalizedScope, StringComparer.Ordinal))
            {
                continue;
            }

            normalizedScopes.Add(normalizedScope);
        }

        return normalizedScopes.Count > 0
            ? normalizedScopes
            : MicrosoftGraphDirectoryConsentScopes.Default.ToArray();
    }

    private static string CreateUrlSafeSecret()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashConsentSecret(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeConsentFailureCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new StringBuilder(value.Trim().ToLowerInvariant().Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-')
            {
                normalized.Append(character);
            }
            else if (normalized.Length == 0 || normalized[^1] != '_')
            {
                normalized.Append('_');
            }

            if (normalized.Length == DirectoryConnectionConsentRequest.FailureCategoryMaxLength)
            {
                break;
            }
        }

        var category = normalized.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(category) ? "microsoft_graph_error" : category;
    }

    private static IReadOnlyList<string> ReadStringArray(string json)
    {
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

    private static AuditEvent CreateMicrosoftGraphConsentAuditEvent(
        Guid id,
        Guid tenantId,
        Guid actorUserId,
        Guid consentRequestId,
        Guid? directoryConnectionId,
        string phase,
        string connectionStatus,
        int requestedScopeCount,
        bool externalTenantIdPresent,
        string? failureCategory)
    {
        var after = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
        {
            ["phase"] = phase,
            ["provider"] = DirectoryConnectionProviders.MicrosoftGraph,
            ["connection_status"] = connectionStatus,
            ["requested_scope_count"] = requestedScopeCount,
            ["external_tenant_id_present"] = externalTenantIdPresent,
            ["failure_category"] = failureCategory,
            ["directory_connection_id_present"] = directoryConnectionId.HasValue
        });

        return new AuditEvent(
            id,
            DateTimeOffset.UtcNow,
            tenantId,
            AuditActorTypes.User,
            actorUserId,
            correlationId: null,
            entityType: "MicrosoftGraphDirectoryConnectionConsent",
            entityId: consentRequestId.ToString("D"),
            changeKind: phase == "requested" ? AuditChangeKinds.Added : AuditChangeKinds.Modified,
            before: null,
            after,
            reason: "microsoft_graph_directory_consent");
    }

    private async Task<Result<DirectoryImportRun?>> CreateGraphDirectoryImportRunAsync(
        Guid tenantId,
        SubjectDirectoryCsvImportRequest request,
        string? sourceExternalIdPrefix,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var microsoftTenantId = ReadMicrosoftGraphTenantId(sourceExternalIdPrefix);
        if (microsoftTenantId is null)
        {
            return Result.Success<DirectoryImportRun?>(null);
        }

        if (!dryRun && !request.PreviewImportRunId.HasValue)
        {
            return Result.Success<DirectoryImportRun?>(null);
        }

        var now = DateTimeOffset.UtcNow;
        var connectionResult = await GetOrCreateMicrosoftGraphImportConnectionAsync(
            tenantId,
            microsoftTenantId,
            now,
            cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure<DirectoryImportRun?>(connectionResult.Error);
        }

        var connection = connectionResult.Value;
        if (request.DirectoryImportRuleId.HasValue)
        {
            var ruleExists = await db.DirectoryImportRules.AnyAsync(
                rule =>
                    rule.Id == request.DirectoryImportRuleId.Value &&
                    rule.TenantId == tenantId &&
                    rule.DirectoryConnectionId == connection.Id &&
                    rule.Status == DirectoryImportRuleStatuses.Active &&
                    rule.DeletedAt == null,
                cancellationToken);
            if (!ruleExists)
            {
                return Result.Failure<DirectoryImportRun?>(
                    Error.NotFound("directory_import_rule.not_found", "Graph import rule was not found."));
            }
        }

        Guid? previewRunId = null;
        if (!dryRun)
        {
            var previewRun = await db.DirectoryImportRuns.SingleOrDefaultAsync(
                run =>
                    run.Id == request.PreviewImportRunId!.Value &&
                    run.TenantId == tenantId &&
                    run.DirectoryConnectionId == connection.Id &&
                    run.Mode == DirectoryImportRunModes.Preview &&
                    run.Status == DirectoryImportRunStatuses.Succeeded,
                cancellationToken);
            if (previewRun is null)
            {
                return Result.Failure<DirectoryImportRun?>(
                    Error.Conflict(
                        "directory_import_run.preview_not_found",
                        "A completed Graph directory preview run is required before apply."));
            }

            previewRunId = previewRun.Id;
        }

        var importRun = new DirectoryImportRun(
            PlatformIds.NewId(),
            tenantId,
            connection.Id,
            dryRun ? DirectoryImportRunModes.Preview : DirectoryImportRunModes.Apply,
            CreateDirectoryImportRunRuleSnapshotJson(request.MarkMissingSubjectsStale),
            retainedFields: """["external_id","email","display_name","locale","group_type","group_name","role_in_group","manager_external_id"]""",
            counts: "{}",
            warningCategories: "[]",
            checkpoint: "{}",
            status: DirectoryImportRunStatuses.Queued,
            directoryImportRuleId: request.DirectoryImportRuleId,
            previewRunId,
            requestedByUserId: null,
            observedAt: now);
        importRun.Start(now);
        db.DirectoryImportRuns.Add(importRun);

        return Result.Success<DirectoryImportRun?>(importRun);
    }

    private async Task<Result<DirectoryConnection>> GetOrCreateMicrosoftGraphImportConnectionAsync(
        Guid tenantId,
        string microsoftTenantId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var connection = await db.DirectoryConnections.SingleOrDefaultAsync(
            row =>
                row.TenantId == tenantId &&
                row.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                row.DeletedAt == null,
            cancellationToken);
        if (connection is not null)
        {
            if (connection.ExternalTenantId is not null &&
                !string.Equals(connection.ExternalTenantId, microsoftTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<DirectoryConnection>(
                    Error.Validation(
                        "directory_import_run.connection_mismatch",
                        "Graph import source does not match the current Microsoft Graph connection."));
            }

            return Result.Success(connection);
        }

        connection = new DirectoryConnection(
            PlatformIds.NewId(),
            tenantId,
            DirectoryConnectionProviders.MicrosoftGraph,
            microsoftTenantId,
            "Microsoft Graph",
            primaryDomain: null,
            grantedScopes: "[]",
            status: DirectoryConnectionStatuses.ConsentRequired,
            createdByUserId: null,
            observedAt);
        db.DirectoryConnections.Add(connection);

        return Result.Success(connection);
    }

    private async Task<DirectoryConnection> GetOrCreateMicrosoftGraphRuleConnectionAsync(
        Guid tenantId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var connection = await db.DirectoryConnections.SingleOrDefaultAsync(
            row =>
                row.TenantId == tenantId &&
                row.Provider == DirectoryConnectionProviders.MicrosoftGraph &&
                row.DeletedAt == null,
            cancellationToken);
        if (connection is not null)
        {
            return connection;
        }

        connection = new DirectoryConnection(
            PlatformIds.NewId(),
            tenantId,
            DirectoryConnectionProviders.MicrosoftGraph,
            externalTenantId: null,
            displayName: "Microsoft Graph",
            primaryDomain: null,
            grantedScopes: "[]",
            status: DirectoryConnectionStatuses.ConsentRequired,
            createdByUserId: null,
            observedAt);
        db.DirectoryConnections.Add(connection);

        return connection;
    }

    private static IReadOnlyList<string> NormalizeMicrosoftGraphImportRetainedFields(
        IReadOnlyList<string>? retainedFields)
    {
        string[] allowed =
            [
                "external_id",
                "email",
                "display_name",
                "locale",
                "group_type",
                "group_name",
                "role_in_group",
                "manager_external_id"
            ];
        var allowedSet = allowed.ToHashSet(StringComparer.Ordinal);
        var source = retainedFields is { Count: > 0 }
            ? retainedFields
            : allowed;
        var normalized = new List<string>();
        foreach (var field in source)
        {
            var value = field.Trim();
            if (allowedSet.Contains(value) && !normalized.Contains(value, StringComparer.Ordinal))
            {
                normalized.Add(value);
            }
        }

        return normalized.Count > 0 ? normalized : allowed;
    }

    private static string CreateMicrosoftGraphImportRuleDocumentJson(bool markMissingSubjectsStale)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["source_kind"] = "msgraph",
            ["population"] = "all_users",
            ["mark_missing_subjects_stale"] = markMissingSubjectsStale
        });
    }

    private static DirectoryImportRuleResponse CreateDirectoryImportRuleResponse(
        DirectoryImportRule rule,
        IReadOnlyList<string> retainedFields)
    {
        return new DirectoryImportRuleResponse(
            rule.Id,
            rule.DirectoryConnectionId,
            rule.Name,
            rule.Status,
            rule.StalePolicy,
            retainedFields,
            rule.CreatedAt,
            rule.UpdatedAt);
    }

    private static string? ReadMicrosoftGraphTenantId(string? sourceExternalIdPrefix)
    {
        if (string.IsNullOrWhiteSpace(sourceExternalIdPrefix) ||
            !sourceExternalIdPrefix.StartsWith("msgraph:", StringComparison.OrdinalIgnoreCase) ||
            !sourceExternalIdPrefix.EndsWith(':'))
        {
            return null;
        }

        var tenantId = sourceExternalIdPrefix["msgraph:".Length..^1].Trim();
        return tenantId.Length > 0 ? tenantId : null;
    }

    private static string CreateDirectoryImportRunRuleSnapshotJson(bool markMissingSubjectsStale)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["source_kind"] = "msgraph",
            ["source_prefix_present"] = true,
            ["mark_missing_subjects_stale"] = markMissingSubjectsStale
        });
    }

    private static string CreateDirectoryImportRunCountsJson(
        int rowCount,
        int importedRowCount,
        int failedRowCount,
        int createdSubjectCount,
        int updatedSubjectCount,
        int createdGroupCount,
        int addedMembershipCount,
        int skippedMembershipCount,
        int setManagerRelationshipCount,
        int skippedManagerRelationshipCount,
        int missingManagerReferenceCount,
        int markedStaleSubjectCount,
        int clearedStaleSubjectCount)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["row_count"] = rowCount,
            ["imported_row_count"] = importedRowCount,
            ["failed_row_count"] = failedRowCount,
            ["created_subject_count"] = createdSubjectCount,
            ["updated_subject_count"] = updatedSubjectCount,
            ["created_group_count"] = createdGroupCount,
            ["added_membership_count"] = addedMembershipCount,
            ["skipped_membership_count"] = skippedMembershipCount,
            ["set_manager_relationship_count"] = setManagerRelationshipCount,
            ["skipped_manager_relationship_count"] = skippedManagerRelationshipCount,
            ["missing_manager_reference_count"] = missingManagerReferenceCount,
            ["marked_stale_subject_count"] = markedStaleSubjectCount,
            ["cleared_stale_subject_count"] = clearedStaleSubjectCount
        });
    }

    private static string CreateDirectoryImportRunWarningCategoriesJson(
        int failedRowCount,
        int missingManagerReferenceCount,
        int markedStaleSubjectCount)
    {
        var categories = new List<string>();
        if (failedRowCount > 0)
        {
            categories.Add("row_failed");
        }

        if (missingManagerReferenceCount > 0)
        {
            categories.Add("manager_reference_missing");
        }

        if (markedStaleSubjectCount > 0)
        {
            categories.Add("subject_marked_stale");
        }

        return JsonSerializer.Serialize(categories);
    }

    private static string CreateDirectoryImportRunCheckpointJson(bool dryRun)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["source_kind"] = "msgraph",
            ["completed"] = true,
            ["mode"] = dryRun ? DirectoryImportRunModes.Preview : DirectoryImportRunModes.Apply
        });
    }

    private static Result<string> NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > 320 || normalized.Contains(' ', StringComparison.Ordinal))
        {
            return Result.Failure<string>(
                Error.Validation("tenant_member.email_invalid", "Enter a valid email address."));
        }

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>(
                    Error.Validation("tenant_member.email_invalid", "Enter a valid email address."));
            }
        }
        catch (FormatException)
        {
            return Result.Failure<string>(
                Error.Validation("tenant_member.email_invalid", "Enter a valid email address."));
        }

        return Result.Success(normalized);
    }

    private static Result<string?> NormalizeOptionalSubjectEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Success<string?>(null);
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 320 || normalized.Contains(' ', StringComparison.Ordinal))
        {
            return Result.Failure<string?>(
                Error.Validation("subject.email_invalid", "Enter a valid subject email address."));
        }

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string?>(
                    Error.Validation("subject.email_invalid", "Enter a valid subject email address."));
            }
        }
        catch (FormatException)
        {
            return Result.Failure<string?>(
                Error.Validation("subject.email_invalid", "Enter a valid subject email address."));
        }

        return Result.Success<string?>(normalized);
    }

    private static Result<IReadOnlyList<SubjectDirectoryCsvRow>> ParseSubjectDirectoryCsv(string csvContent)
    {
        IReadOnlyList<IReadOnlyList<string>> table;
        try
        {
            table = ReadCsvTable(csvContent);
        }
        catch (FormatException exception)
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation("subject_directory_import.csv_invalid", exception.Message));
        }

        if (table.Count == 0)
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation("subject_directory_import.csv_empty", "CSV content needs a header row."));
        }

        var headers = table[0]
            .Select(NormalizeCsvHeader)
            .ToArray();
        if (headers.All(string.IsNullOrWhiteSpace))
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation("subject_directory_import.csv_empty", "CSV content needs named columns."));
        }

        var knownHeaders = new HashSet<string>(
            [
                "external_id",
                "email",
                "display_name",
                "locale",
                "group_type",
                "group_name",
                "role_in_group",
                "manager_external_id"
            ],
            StringComparer.OrdinalIgnoreCase);
        var unknownHeaders = headers
            .Where(header => !string.IsNullOrWhiteSpace(header) && !knownHeaders.Contains(header))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownHeaders.Length > 0)
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation(
                    "subject_directory_import.unknown_columns",
                    $"CSV contains unsupported columns: {string.Join(", ", unknownHeaders)}."));
        }

        if (!headers.Contains("external_id", StringComparer.OrdinalIgnoreCase) &&
            !headers.Contains("email", StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation(
                    "subject_directory_import.identity_column_required",
                    "CSV needs external_id or email so rows can be matched safely."));
        }

        var rows = new List<SubjectDirectoryCsvRow>();
        for (var index = 1; index < table.Count; index++)
        {
            var raw = table[index];
            if (raw.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var column = 0; column < headers.Length; column++)
            {
                var header = headers[column];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                values[header] = column < raw.Count ? raw[column] : string.Empty;
            }

            if (raw.Count > headers.Length)
            {
                values["__row_error"] = "Row has more values than the header row.";
            }

            rows.Add(new SubjectDirectoryCsvRow(index + 1, values));
        }

        if (rows.Count == 0)
        {
            return Result.Failure<IReadOnlyList<SubjectDirectoryCsvRow>>(
                Error.Validation("subject_directory_import.no_rows", "CSV needs at least one data row."));
        }

        return Result.Success<IReadOnlyList<SubjectDirectoryCsvRow>>(rows);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadCsvTable(string csvContent)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < csvContent.Length; index++)
        {
            var current = csvContent[index];
            if (current == '"')
            {
                if (inQuotes && index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (current == ',' && !inQuotes)
            {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if ((current == '\r' || current == '\n') && !inQuotes)
            {
                if (current == '\r' && index + 1 < csvContent.Length && csvContent[index + 1] == '\n')
                {
                    index++;
                }

                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = [];
                continue;
            }

            field.Append(current);
        }

        if (inQuotes)
        {
            throw new FormatException("CSV has an unterminated quoted field.");
        }

        row.Add(field.ToString());
        if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static string NormalizeCsvHeader(string header)
    {
        return header.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
    }

    private static SubjectDirectoryImportRowValues NormalizeImportRow(
        IReadOnlyDictionary<string, string> values)
    {
        var email = ImportText(values, "email");
        var emailResult = NormalizeOptionalSubjectEmail(email);
        return new SubjectDirectoryImportRowValues(
            ImportText(values, "external_id"),
            emailResult.IsSuccess ? emailResult.Value : email,
            ImportText(values, "display_name"),
            NormalizeLocale(ImportText(values, "locale") ?? "en"),
            ImportText(values, "group_type") ?? SubjectGroupTypes.Department,
            ImportText(values, "group_name"),
            ImportText(values, "role_in_group"),
            ImportText(values, "manager_external_id"),
            values.TryGetValue("__row_error", out var rowError) ? rowError : null,
            emailResult.IsFailure ? emailResult.Error.Message : null);
    }

    private static List<string> ValidateImportRow(SubjectDirectoryImportRowValues values)
    {
        var issues = new List<string>();
        if (values.RowError is not null)
        {
            issues.Add(values.RowError);
        }

        if (values.EmailError is not null)
        {
            issues.Add(values.EmailError);
        }

        if (values.ExternalId is null && values.Email is null)
        {
            issues.Add("Provide external_id or email so the person can be matched safely.");
        }

        if (values.DisplayName is { Length: > 256 })
        {
            issues.Add("display_name must be 256 characters or fewer.");
        }

        if (values.ExternalId is { Length: > 256 })
        {
            issues.Add("external_id must be 256 characters or fewer.");
        }

        if (values.Locale.Length > 16)
        {
            issues.Add("locale must be 16 characters or fewer.");
        }

        if (values.GroupName is not null && values.GroupType.Length > 64)
        {
            issues.Add("group_type must be 64 characters or fewer.");
        }

        if (values.GroupName is { Length: > 256 })
        {
            issues.Add("group_name must be 256 characters or fewer.");
        }

        if (values.RoleInGroup is { Length: > 64 })
        {
            issues.Add("role_in_group must be 64 characters or fewer.");
        }

        if (values.ManagerExternalId is { Length: > 256 })
        {
            issues.Add("manager_external_id must be 256 characters or fewer.");
        }

        if (values.ExternalId is not null &&
            values.ManagerExternalId is not null &&
            string.Equals(values.ExternalId, values.ManagerExternalId, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("manager_external_id cannot match external_id.");
        }

        return issues;
    }

    private static string? ImportText(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static string? NormalizeOptionalSourceExternalIdPrefix(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static (string Attributes, bool Changed) MarkDirectoryImportStale(
        string attributes,
        string sourceExternalIdPrefix)
    {
        var root = ParseAttributeObject(attributes);
        if (root.TryGetPropertyValue("directory_import_stale", out var staleNode) &&
            staleNode is not null &&
            staleNode.GetValueKind() == System.Text.Json.JsonValueKind.True &&
            root.TryGetPropertyValue("directory_import_stale_source", out var sourceNode) &&
            string.Equals(sourceNode?.GetValue<string>(), sourceExternalIdPrefix, StringComparison.Ordinal))
        {
            return (attributes, false);
        }

        root["directory_import_stale"] = true;
        root["directory_import_stale_source"] = sourceExternalIdPrefix;
        root["directory_import_stale_at"] = DateTimeOffset.UtcNow.ToString("O");
        return (root.ToJsonString(), true);
    }

    private static (string Attributes, bool Changed) ClearDirectoryImportStaleMarker(string attributes)
    {
        var root = ParseAttributeObject(attributes);
        var changed = root.Remove("directory_import_stale");
        changed = root.Remove("directory_import_stale_source") || changed;
        changed = root.Remove("directory_import_stale_at") || changed;

        return changed ? (root.ToJsonString(), true) : (attributes, false);
    }

    private static JsonObject ParseAttributeObject(string attributes)
    {
        return JsonNode.Parse(attributes)?.AsObject() ?? [];
    }

    private static AuditEvent CreateSubjectDirectoryImportAuditEvent(
        Guid auditEventId,
        Guid tenantId,
        Guid actorUserId,
        bool dryRun,
        string? sourceExternalIdPrefix,
        int rowCount,
        int importedRowCount,
        int failedRowCount,
        int createdSubjectCount,
        int updatedSubjectCount,
        int createdGroupCount,
        int addedMembershipCount,
        int skippedMembershipCount,
        int setManagerRelationshipCount,
        int skippedManagerRelationshipCount,
        int missingManagerReferenceCount,
        int markedStaleSubjectCount,
        int clearedStaleSubjectCount)
    {
        return new AuditEvent(
            auditEventId,
            DateTimeOffset.UtcNow,
            tenantId,
            AuditActorTypes.User,
            actorUserId,
            correlationId: null,
            entityType: "SubjectDirectoryImport",
            entityId: auditEventId.ToString("D"),
            AuditChangeKinds.Added,
            before: null,
            after: AuditJson.Create(new Dictionary<string, object?>
            {
                ["dry_run"] = dryRun,
                ["source_kind"] = sourceExternalIdPrefix is null
                    ? "csv"
                    : sourceExternalIdPrefix.StartsWith("msgraph:", StringComparison.OrdinalIgnoreCase)
                        ? "msgraph"
                        : "external_directory",
                ["source_prefix_present"] = sourceExternalIdPrefix is not null,
                ["row_count"] = rowCount,
                ["imported_row_count"] = importedRowCount,
                ["failed_row_count"] = failedRowCount,
                ["created_subject_count"] = createdSubjectCount,
                ["updated_subject_count"] = updatedSubjectCount,
                ["created_group_count"] = createdGroupCount,
                ["added_membership_count"] = addedMembershipCount,
                ["skipped_membership_count"] = skippedMembershipCount,
                ["set_manager_relationship_count"] = setManagerRelationshipCount,
                ["skipped_manager_relationship_count"] = skippedManagerRelationshipCount,
                ["missing_manager_reference_count"] = missingManagerReferenceCount,
                ["marked_stale_subject_count"] = markedStaleSubjectCount,
                ["cleared_stale_subject_count"] = clearedStaleSubjectCount
            }),
            reason: "subject_directory_import");
    }

    private static string SubjectGroupImportKey(string type, string name)
    {
        return $"{type.Trim().ToLowerInvariant()}|{name.Trim().ToLowerInvariant()}";
    }

    private static SubjectDirectoryCsvImportRowResponse CreateImportRowResponse(
        int rowNumber,
        string status,
        SubjectDirectoryImportRowValues values,
        string action,
        IReadOnlyList<string> issues)
    {
        return new SubjectDirectoryCsvImportRowResponse(
            rowNumber,
            status,
            values.ExternalId,
            values.Email,
            values.DisplayName,
            values.GroupName is null ? null : values.GroupType,
            values.GroupName,
            action,
            issues);
    }

    private static Error CreateSubjectValidationError(ArgumentException exception)
    {
        return exception.ParamName == "attributes"
            ? Error.Validation("subject.attributes_invalid", "Subject attributes must be a JSON object.")
            : Error.Validation("subject.invalid", exception.Message);
    }

    private static Error CreateSubjectGroupValidationError(ArgumentException exception)
    {
        return exception.ParamName == "attributes"
            ? Error.Validation("subject_group.attributes_invalid", "Subject group attributes must be a JSON object.")
            : Error.Validation("subject_group.invalid", exception.Message);
    }

    private static DateOnly ClampEndDate(DateOnly? validFrom, DateOnly requestedEnd)
    {
        return validFrom.HasValue && validFrom.Value > requestedEnd
            ? validFrom.Value
            : requestedEnd;
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        return roleCode.Trim().ToLowerInvariant();
    }

    private static string NormalizeLocale(string locale)
    {
        var normalized = locale.Trim().ToLowerInvariant();

        return string.IsNullOrWhiteSpace(normalized) ? "en" : normalized;
    }

    private static CampaignCloseStateResponse CreateCloseStateResponse(Campaign campaign)
    {
        return new CampaignCloseStateResponse(
            campaign.Id,
            campaign.Status,
            campaign.UpdatedAt,
            campaign.ClosedAt,
            campaign.ClosedByUserId,
            campaign.CloseReason);
    }

    private sealed record SubmittedSessionForScoreRemediation(Guid SessionId, Guid CampaignId);

    private sealed record SubjectDirectoryCsvRow(
        int RowNumber,
        IReadOnlyDictionary<string, string> Values);

    private sealed record SubjectDirectoryImportRowValues(
        string? ExternalId,
        string? Email,
        string? DisplayName,
        string Locale,
        string GroupType,
        string? GroupName,
        string? RoleInGroup,
        string? ManagerExternalId,
        string? RowError,
        string? EmailError);

    private sealed record SubjectMembershipImportKey(Guid SubjectId, Guid GroupId);
}
