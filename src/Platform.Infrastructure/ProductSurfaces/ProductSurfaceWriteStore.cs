using System.Net.Mail;
using System.Security.Cryptography;
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
                retentionUntil: source.RetentionUntil);
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

        var seriesExists = await db.CampaignSeries
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == campaignSeriesId, cancellationToken);

        if (!seriesExists)
        {
            return Result.Failure<CampaignSeriesScoreRemediationResponse>(
                Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
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
}
