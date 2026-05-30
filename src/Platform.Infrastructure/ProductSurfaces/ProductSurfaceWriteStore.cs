using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Auth;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
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
                attributes: SubjectDirectoryMetadata.EnsureSource(
                    request.Attributes,
                    SubjectDirectorySources.Manual));
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

    public async Task<Result<SubjectDirectoryItemResponse>> DeactivateSubjectAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        DeactivateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        return await SetSubjectDirectoryStatusCoreAsync(
            tenantId,
            subjectId,
            actorUserId,
            SubjectDirectoryStatuses.Deactivated,
            request.Reason,
            cancellationToken);
    }

    public async Task<Result<SubjectDirectoryItemResponse>> SetSubjectDirectoryStatusAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        SetSubjectDirectoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(status) || !SubjectDirectoryStatuses.IsMutable(status))
        {
            return Result.Failure<SubjectDirectoryItemResponse>(
                Error.Validation("subject.status_invalid", "Directory status is not supported."));
        }

        return await SetSubjectDirectoryStatusCoreAsync(
            tenantId,
            subjectId,
            actorUserId,
            status,
            request.Reason,
            cancellationToken);
    }

    private async Task<Result<SubjectDirectoryItemResponse>> SetSubjectDirectoryStatusCoreAsync(
        Guid tenantId,
        Guid subjectId,
        Guid actorUserId,
        string status,
        string? reason,
        CancellationToken cancellationToken)
    {
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
            subject.ReplaceAttributes(SubjectDirectoryMetadata.MarkStatus(
                subject.Attributes,
                status,
                actorUserId,
                DateTimeOffset.UtcNow,
                reason));
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

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);
        var dryRun = request.DryRun;

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

        foreach (var row in parsed.Value)
        {
            var values = NormalizeImportRow(row.Values);
            var issues = ValidateImportRow(values);
            var actions = new List<string>();

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
                    attributes: SubjectDirectoryMetadata.EnsureSource(
                        "{}",
                        SubjectDirectorySources.Csv));
                if (!dryRun)
                {
                    db.Subjects.Add(subject);
                }

                createdSubjectCount++;
                actions.Add("created_subject");
            }
            else
            {
                if (!dryRun)
                {
                    subject.ChangeDirectoryProfile(
                        values.DisplayName ?? subject.DisplayName,
                        values.Email ?? subject.Email,
                        values.ExternalId ?? subject.ExternalId,
                        values.Locale,
                        SubjectDirectoryMetadata.EnsureSource(
                            subject.Attributes,
                            SubjectDirectorySources.Csv));
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

            rows.Add(CreateImportRowResponse(row.RowNumber, "imported", values, string.Join(",", actions), []));
        }

        if (!dryRun)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

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
            dryRun));
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
        var metadata = SubjectDirectoryMetadata.From(subject.ExternalId, subject.Attributes);

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
            groups,
            metadata.Source,
            metadata.SourceLabel,
            metadata.Status,
            metadata.StatusLabel,
            metadata.Department,
            metadata.JobTitle,
            metadata.EmployeeType,
            metadata.OfficeLocation);
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
                "role_in_group"
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

        return issues;
    }

    private static string? ImportText(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
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
        string? RowError,
        string? EmailError);

    private sealed record SubjectMembershipImportKey(Guid SubjectId, Guid GroupId);
}
