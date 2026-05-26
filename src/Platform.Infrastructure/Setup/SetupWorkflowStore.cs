using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Platform.Application.Features.Notifications;
using Platform.Application.Features.Responses;
using Platform.Application.Features.Setup;
using Platform.Application.Outbox;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Instruments;
using Platform.Domain.Outbox;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Infrastructure.Campaigns.RespondentRules;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Notifications;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Setup;

public sealed class SetupWorkflowStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IOutboxEventBuffer? outboxEventBuffer = null,
    RespondentRuleResolver? respondentRuleResolver = null,
    IOptions<EmailDeliveryOptions>? emailDeliveryOptions = null) : ISetupWorkflowStore
{
    private static readonly JsonSerializerOptions LaunchSnapshotJsonOptions = new(JsonSerializerDefaults.Web);
    private const string DefaultConsentLocale = "en";
    private const string DefaultConsentVersion = "1.0.0";
    private const string DefaultConsentTitle = "Default participant disclosure";
    private const string DefaultConsentBody =
        """
        This setup disclosure is a platform-generated placeholder for internal pilot use.

        By continuing, the participant confirms the tenant may collect anonymous survey responses, process them in the platform, and use them for the configured study or report workflow.

        Replace this text with tenant-reviewed disclosure language before production use. Contact: support@example.invalid.
        """;
    private const string DefaultRequiredConsentGrants = """["data_processing","research_participation"]""";
    private const string DefaultOptionalConsentGrants = "[]";
    private const string DefaultPolicyVersion = "1.0.0";
    private const int DefaultRetainForYears = 1;
    private const string DefaultPublicationLimits = """{"status":"proof_default_not_legal_advice"}""";
    private const string DefaultAppliesToDimensions = """["score","subscale","demographic","wave_comparison"]""";
    private const string EmailInvitationAssignmentRole = "invited_respondent";
    private const string NotificationAggregateType = "notification";
    private const string InvitationEmailQueuedEventType = "InvitationEmailQueued";
    private const int MaxInvitationBatchRecipients = 500;
    private readonly EmailDeliveryOptions _emailDeliveryOptions =
        emailDeliveryOptions?.Value ?? new EmailDeliveryOptions();
    private readonly RespondentRuleResolver _respondentRuleResolver =
        respondentRuleResolver ?? new RespondentRuleResolver(db);

    public async Task<Result<InstrumentSummaryResponse>> CreatePrivateInstrumentImportAsync(
        Guid tenantId,
        CreatePrivateInstrumentImportRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        Instrument instrument;
        try
        {
            instrument = Instrument.CreateTenantImport(
                PlatformIds.NewId(),
                tenantId,
                request.Code,
                request.Version,
                request.FullName,
                request.Domain,
                request.ProvenanceNote,
                request.RightsStatus,
                request.ValidityLabel,
                request.LicenseType,
                request.CitationApa);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<InstrumentSummaryResponse>(
                Error.Validation("instrument.invalid", exception.Message));
        }

        var duplicateExists = await db.Instruments
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.TenantId == tenantId &&
                    existing.Code == instrument.Code &&
                    existing.Version == instrument.Version,
                cancellationToken);

        if (duplicateExists)
        {
            return Result.Failure<InstrumentSummaryResponse>(
                DuplicateInstrumentCodeVersion(instrument.Code, instrument.Version));
        }

        db.Instruments.Add(instrument);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsInstrumentCodeVersionDuplicate(exception))
        {
            db.Entry(instrument).State = EntityState.Detached;

            return Result.Failure<InstrumentSummaryResponse>(
                DuplicateInstrumentCodeVersion(instrument.Code, instrument.Version));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToInstrumentSummary(instrument));
    }

    public async Task<IReadOnlyList<InstrumentSummaryResponse>> ListInstrumentsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var instruments = await db.Instruments
            .AsNoTracking()
            .OrderBy(instrument => instrument.Code)
            .ThenBy(instrument => instrument.Version)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return instruments.Select(ToInstrumentSummary).ToArray();
    }

    public Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionAsync(
        Guid tenantId,
        Guid? actorId,
        CreateTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        return CreateTemplateVersionCoreAsync(tenantId, actorId, request, cancellationToken);
    }

    public async Task<Result<TemplateVersionDetailResponse>> GetTemplateVersionAsync(
        Guid tenantId,
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var version = await db.TemplateVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == templateVersionId, cancellationToken);

        if (version is null)
        {
            return Result.Failure<TemplateVersionDetailResponse>(
                Error.NotFound("template_version.not_found", "Template version was not found."));
        }

        var template = await db.SurveyTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == version.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure<TemplateVersionDetailResponse>(
                Error.NotFound("template.not_found", "Template was not found."));
        }

        var sections = await db.TemplateSections
            .AsNoTracking()
            .Where(section => section.TemplateVersionId == templateVersionId)
            .OrderBy(section => section.Ordinal)
            .ToListAsync(cancellationToken);
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scale.TemplateVersionId == templateVersionId)
            .OrderBy(scale => scale.Code)
            .ToListAsync(cancellationToken);
        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == templateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToTemplateVersionDetail(template, version, sections, scales, questions));
    }

    public async Task<Result<SetupIdResponse>> CreateScoringRuleAsync(
        Guid tenantId,
        CreateScoringRuleRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var version = await db.TemplateVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == request.TemplateVersionId, cancellationToken);

        if (version is null)
        {
            return Result.Failure<SetupIdResponse>(
                Error.NotFound("template_version.not_found", "Template version was not found."));
        }

        var template = await db.SurveyTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == version.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure<SetupIdResponse>(
                Error.NotFound("template.not_found", "Template was not found."));
        }

        if (version.IsGlobal || template.TenantId != tenantId)
        {
            return Result.Failure<SetupIdResponse>(
                Error.Forbidden("scoring_rule.global_template", "Scoring rules can only be created for tenant-owned template versions."));
        }

        var validation = ScoringRuleValidator.Validate(new ScoringRuleValidationRequest(
            request.RuleKey,
            request.RuleVersion,
            request.SchemaVersion,
            request.EngineMinVersion,
            request.Document,
            request.Produces,
            request.Compatibility));
        if (validation.IsFailure)
        {
            return Result.Failure<SetupIdResponse>(validation.Error);
        }

        ScoringRule scoringRule;
        try
        {
            scoringRule = ScoringRule.CreateDraft(
                PlatformIds.NewId(),
                request.TemplateVersionId,
                request.RuleKey,
                request.RuleVersion,
                request.SchemaVersion,
                request.EngineMinVersion,
                ComputeSha256Hex(request.Document),
                request.Document,
                request.Produces,
                request.Compatibility);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SetupIdResponse>(
                Error.Validation("scoring_rule.invalid", exception.Message));
        }

        db.ScoringRules.Add(scoringRule);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SetupIdResponse(scoringRule.Id));
    }

    public async Task<Result<SetupIdResponse>> CreateCampaignSeriesAsync(
        Guid tenantId,
        CreateCampaignSeriesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        CampaignSeries series;
        try
        {
            var brief = request.StudyBrief;
            series = new CampaignSeries(
                PlatformIds.NewId(),
                tenantId,
                request.Name,
                RandomNumberGenerator.GetBytes(32),
                studyPurpose: brief?.Purpose,
                studyAudience: brief?.Audience,
                studyDesignType: brief?.DesignType,
                studyIntendedUse: brief?.IntendedUse,
                studyInterpretationBoundary: brief?.InterpretationBoundary,
                studyOwnerNotes: brief?.OwnerNotes);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SetupIdResponse>(
                Error.Validation("campaign_series.invalid", exception.Message));
        }

        var createdAt = DateTimeOffset.UtcNow;

        db.CampaignSeries.Add(series);
        db.ConsentDocuments.Add(new ConsentDocument(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            DefaultConsentLocale,
            DefaultConsentVersion,
            DefaultConsentTitle,
            DefaultConsentBody,
            DefaultRequiredConsentGrants,
            DefaultOptionalConsentGrants,
            createdAt));
        db.RetentionPolicies.Add(new RetentionPolicy(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            DefaultPolicyVersion,
            DefaultRetainForYears,
            RetentionPolicy.ResponseSubmittedAt,
            RetentionPolicy.Anonymize,
            DateOnly.FromDateTime(createdAt.UtcDateTime).AddYears(DefaultRetainForYears),
            DefaultPublicationLimits,
            createdAt));
        db.DisclosurePolicies.Add(new DisclosurePolicy(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            DefaultPolicyVersion,
            DisclosurePolicy.MinimumKMin,
            DisclosurePolicy.HideCell,
            DefaultAppliesToDimensions,
            createdAt));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SetupIdResponse(series.Id));
    }

    public async Task<Result<CampaignDraftResponse>> CreateCampaignAsync(
        Guid tenantId,
        Guid? actorId,
        CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorId,
            cancellationToken: cancellationToken);

        var templateVersionExists = await db.TemplateVersions
            .AsNoTracking()
            .AnyAsync(version => version.Id == request.TemplateVersionId, cancellationToken);

        if (!templateVersionExists)
        {
            return Result.Failure<CampaignDraftResponse>(
                Error.NotFound("template_version.not_found", "Template version was not found."));
        }

        if (request.CampaignSeriesId.HasValue)
        {
            var seriesExists = await db.CampaignSeries
                .AsNoTracking()
                .AnyAsync(
                    series => series.Id == request.CampaignSeriesId.Value &&
                        series.TenantId == tenantId,
                    cancellationToken);

            if (!seriesExists)
            {
                return Result.Failure<CampaignDraftResponse>(
                    Error.NotFound("campaign_series.not_found", "Campaign series was not found."));
            }
        }

        var createdBy = await GetPersistedActorIdAsync(actorId, cancellationToken);

        Campaign campaign;
        try
        {
            campaign = new Campaign(
                PlatformIds.NewId(),
                tenantId,
                request.TemplateVersionId,
                request.Name,
                request.ResponseIdentityMode,
                campaignSeriesId: request.CampaignSeriesId,
                startAt: request.StartAt,
                endAt: request.EndAt,
                schedule: request.Schedule,
                defaultLocale: request.DefaultLocale,
                createdBy: createdBy);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<CampaignDraftResponse>(
                Error.Validation("campaign.invalid", exception.Message));
        }

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToCampaignDraft(campaign));
    }

    public async Task<Result<LaunchReadinessResponse>> GetLaunchReadinessAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<LaunchReadinessResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var readiness = await EvaluateLaunchReadinessAsync(campaign, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToLaunchReadinessResponse(campaign.Id, readiness.Issues));
    }

    public async Task<Result<CampaignRespondentRuleListResponse>> ListCampaignRespondentRulesAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignRespondentRuleListResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var response = await BuildCampaignRespondentRuleListResponseAsync(
            tenantId,
            campaign,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CampaignRespondentRuleListResponse>> UpdateCampaignRespondentRulesAsync(
        Guid tenantId,
        Guid campaignId,
        UpdateCampaignRespondentRulesRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        if (request.Rules is null)
        {
            return Result.Failure<CampaignRespondentRuleListResponse>(
                Error.Validation("respondent_rule.rules_required", "Respondent rules are required."));
        }

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignRespondentRuleListResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (campaign.Status is not (CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            return Result.Failure<CampaignRespondentRuleListResponse>(
                Error.Conflict(
                    "respondent_rule.campaign_not_editable",
                    "Respondent rules can only be changed before campaign launch."));
        }

        foreach (var rule in request.Rules)
        {
            var validation = await _respondentRuleResolver.ResolveAsync(
                new RespondentRuleResolutionRequest(
                    tenantId,
                    campaign.Id,
                    campaign.CampaignSeriesId,
                    rule.Rule),
                cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<CampaignRespondentRuleListResponse>(validation.Error);
            }
        }

        var existingRules = await db.RespondentRules
            .Where(rule => rule.CampaignId == campaign.Id)
            .OrderBy(rule => rule.Ordinal)
            .ToListAsync(cancellationToken);

        if (existingRules.Count > 0)
        {
            db.RespondentRules.RemoveRange(existingRules);
            await db.SaveChangesAsync(cancellationToken);
        }

        var newRules = new List<RespondentRule>(request.Rules.Count);
        for (var index = 0; index < request.Rules.Count; index++)
        {
            try
            {
                newRules.Add(new RespondentRule(
                    PlatformIds.NewId(),
                    campaign.Id,
                    index + 1,
                    request.Rules[index].Rule));
            }
            catch (ArgumentException exception)
            {
                return Result.Failure<CampaignRespondentRuleListResponse>(
                    Error.Validation("respondent_rule.rule_invalid", exception.Message));
            }
        }

        db.RespondentRules.AddRange(newRules);
        await db.SaveChangesAsync(cancellationToken);

        var response = await BuildCampaignRespondentRuleListResponseAsync(
            tenantId,
            campaign,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<CampaignAssignmentListResponse>> ListCampaignAssignmentsAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaignExists = await db.Campaigns
            .AsNoTracking()
            .AnyAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);
        if (!campaignExists)
        {
            return Result.Failure<CampaignAssignmentListResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var assignments = await db.Assignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.TenantId == tenantId &&
                assignment.CampaignId == campaignId)
            .OrderBy(assignment => assignment.CreatedAt)
            .ThenBy(assignment => assignment.Id)
            .ToListAsync(cancellationToken);

        var subjectIds = assignments
            .SelectMany(assignment => new[] { assignment.TargetSubjectId, assignment.RespondentSubjectId })
            .Where(subjectId => subjectId.HasValue)
            .Select(subjectId => subjectId!.Value)
            .Distinct()
            .ToArray();
        var subjects = subjectIds.Length == 0
            ? new Dictionary<Guid, CampaignAssignmentSubjectResponse>()
            : await db.Subjects
                .AsNoTracking()
                .Where(subject =>
                    subject.TenantId == tenantId &&
                    subjectIds.Contains(subject.Id))
                .ToDictionaryAsync(
                    subject => subject.Id,
                    ToCampaignAssignmentSubject,
                    cancellationToken);

        var response = new CampaignAssignmentListResponse(
            campaignId,
            assignments.Count,
            assignments
                .Select(assignment => new CampaignAssignmentResponse(
                    assignment.Id,
                    assignment.Role,
                    assignment.Status,
                    assignment.Anonymous,
                    assignment.TargetSubjectId,
                    assignment.TargetSubjectId.HasValue &&
                        subjects.TryGetValue(assignment.TargetSubjectId.Value, out var target)
                            ? target
                            : null,
                    assignment.RespondentSubjectId,
                    assignment.RespondentSubjectId.HasValue &&
                        subjects.TryGetValue(assignment.RespondentSubjectId.Value, out var respondent)
                            ? respondent
                            : null,
                    assignment.DueAt,
                    assignment.CreatedAt))
                .ToArray());

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task<Result<LaunchCampaignResponse>> LaunchCampaignAsync(
        Guid tenantId,
        Guid? actorId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshotExists = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.CampaignId == campaignId, cancellationToken);

        if (snapshotExists)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Conflict("campaign.already_launched", "Campaign already has a launch snapshot."));
        }

        var readiness = await EvaluateLaunchReadinessAsync(campaign, cancellationToken);
        if (readiness.Issues.Any(issue => issue.Severity == "blocker"))
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Validation("campaign.launch_blocked", "Campaign has launch readiness blockers."));
        }

        if (readiness.ScoringRule is null)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Validation("scoring_rule.missing", "Template version must have a scoring rule before launch."));
        }

        if (readiness.ConsentDocument is null)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Validation("consent_document.missing", "Campaign must have a usable consent document before launch."));
        }

        if (readiness.RetentionPolicy is null)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Validation("retention_policy.missing", "Campaign must have a usable retention policy before launch."));
        }

        if (readiness.DisclosurePolicy is null)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Validation("disclosure_policy.missing", "Campaign must have a usable disclosure policy before launch."));
        }

        var launchedAt = DateTimeOffset.UtcNow;
        var launchedBy = await GetPersistedActorIdAsync(actorId, cancellationToken);

        var materializedAssignments = await MaterializeAssignmentsFromRespondentRulesAsync(
            tenantId,
            campaign,
            cancellationToken);
        if (materializedAssignments.IsFailure)
        {
            return Result.Failure<LaunchCampaignResponse>(materializedAssignments.Error);
        }

        var readinessResponse = ToLaunchReadinessResponse(campaign.Id, readiness.Issues);
        var launchPacket = BuildLaunchPacket(
            campaign,
            readiness,
            readinessResponse,
            materializedAssignments.Value,
            launchedAt,
            launchedBy);

        var snapshot = new CampaignLaunchSnapshot(
            PlatformIds.NewId(),
            tenantId,
            campaign.Id,
            campaign.CampaignSeriesId,
            campaign.TemplateVersionId,
            readiness.ScoringRule.Id,
            campaign.ResponseIdentityMode,
            campaign.DefaultLocale,
            readiness.TemplateQuestionCount,
            readiness.ScoringRule.DocumentHash,
            JsonSerializer.Serialize(readinessResponse, LaunchSnapshotJsonOptions),
            launchedAt,
            launchedBy: launchedBy,
            consentDocumentId: readiness.ConsentDocument.Id,
            retentionPolicyId: readiness.RetentionPolicy.Id,
            disclosurePolicyId: readiness.DisclosurePolicy.Id,
            launchPacket: launchPacket);

        await RemoveExternalEmailRespondentRulesAfterMaterializationAsync(
            campaign.Id,
            cancellationToken);

        try
        {
            campaign.Launch(launchedAt);
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<LaunchCampaignResponse>(
                Error.Conflict("campaign.not_launchable", exception.Message));
        }

        db.CampaignLaunchSnapshots.Add(snapshot);
        db.Assignments.AddRange(materializedAssignments.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsLaunchSnapshotDuplicate(exception))
        {
            db.Entry(snapshot).State = EntityState.Detached;

            return Result.Failure<LaunchCampaignResponse>(
                Error.Conflict("campaign.already_launched", "Campaign already has a launch snapshot."));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new LaunchCampaignResponse(
            campaign.Id,
            campaign.Status,
            snapshot.Id,
            snapshot.TemplateVersionId,
            snapshot.ScoringRuleId,
            snapshot.RetentionPolicyId!.Value,
            snapshot.DisclosurePolicyId!.Value,
            snapshot.ResponseIdentityMode,
            snapshot.DefaultLocale,
            snapshot.LaunchedAt));
    }

    public async Task<Result<CampaignOpenLinkResponse>> CreateCampaignOpenLinkAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == campaignId,
                cancellationToken);

        if (campaign.Status != CampaignStatuses.Live || snapshot is null)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Validation(
                    "campaign.not_launched",
                    "Campaign must be launched before creating an open respondent link."));
        }

        if (snapshot.ResponseIdentityMode is not (
            ResponseIdentityModes.Anonymous or
            ResponseIdentityModes.AnonymousLongitudinal))
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Validation(
                    "open_link.identity_mode_not_supported",
                    "Open respondent links support anonymous response modes only."));
        }

        if (await CampaignHasAccessChannelAsync(
            tenantId,
            campaign.Id,
            InvitationTokenChannels.Email,
            cancellationToken))
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Conflict(
                    "open_link.invite_only_access_active",
                    "This campaign already has private email invitations. Open respondent links are disabled for invite-only collection."));
        }

        if (await CampaignHasAccessChannelAsync(
            tenantId,
            campaign.Id,
            InvitationTokenChannels.OpenLink,
            cancellationToken))
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Conflict(
                    "open_link.already_created",
                    "This campaign already has an open respondent link. Keep using the created link, or create a new wave if the link was lost."));
        }

        var issued = OpenLinkTokens.Issue(tenantId);
        var invitationToken = new InvitationToken(
            PlatformIds.NewId(),
            tenantId,
            campaign.Id,
            issued.TokenHash,
            InvitationTokenChannels.OpenLink);
        var assignment = Assignment.CreateAnonymous(
            PlatformIds.NewId(),
            tenantId,
            campaign.Id,
            "public_respondent",
            invitationToken.Id);

        db.InvitationTokens.Add(invitationToken);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignOpenLinkResponse(
            campaign.Id,
            assignment.Id,
            issued.RawToken,
            $"/r/{issued.RawToken}"));
    }

    public async Task<Result<CampaignOpenLinkResponse>> ReplaceCampaignOpenLinkAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == campaignId,
                cancellationToken);

        if (campaign.Status != CampaignStatuses.Live || snapshot is null)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Validation(
                    "campaign.not_launched",
                    "Campaign must be launched before replacing an open respondent link."));
        }

        if (snapshot.ResponseIdentityMode is not (
            ResponseIdentityModes.Anonymous or
            ResponseIdentityModes.AnonymousLongitudinal))
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Validation(
                    "open_link.identity_mode_not_supported",
                    "Open respondent links support anonymous response modes only."));
        }

        if (await CampaignHasAccessChannelAsync(
            tenantId,
            campaign.Id,
            InvitationTokenChannels.Email,
            cancellationToken))
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Conflict(
                    "open_link.invite_only_access_active",
                    "This campaign already has private email invitations. Open respondent links are disabled for invite-only collection."));
        }

        var existingOpenLinks = await (
                from assignment in db.Assignments.AsNoTracking()
                join invitationToken in db.InvitationTokens
                    on assignment.InviteTokenId equals invitationToken.Id
                where assignment.TenantId == tenantId &&
                    assignment.CampaignId == campaign.Id &&
                    assignment.Anonymous &&
                    invitationToken.TenantId == tenantId &&
                    invitationToken.CampaignId == campaign.Id &&
                    invitationToken.Channel == InvitationTokenChannels.OpenLink
                select new
                {
                    AssignmentId = assignment.Id,
                    InvitationToken = invitationToken
                })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (existingOpenLinks.Count == 0)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.NotFound(
                    "open_link.not_found",
                    "Create an open respondent link before replacing it."));
        }

        if (existingOpenLinks.Count > 1)
        {
            return Result.Failure<CampaignOpenLinkResponse>(
                Error.Conflict(
                    "open_link.multiple_active",
                    "This campaign has more than one open respondent link. Create a new wave before replacing links."));
        }

        var existing = existingOpenLinks[0];
        if (db.Entry(existing.InvitationToken).State == EntityState.Detached)
        {
            db.InvitationTokens.Attach(existing.InvitationToken);
        }

        var issued = OpenLinkTokens.Issue(tenantId);
        existing.InvitationToken.ReissueHash(issued.TokenHash);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignOpenLinkResponse(
            campaign.Id,
            existing.AssignmentId,
            issued.RawToken,
            $"/r/{issued.RawToken}"));
    }

    public async Task<Result<CampaignIdentifiedEntryResponse>> CreateCampaignIdentifiedEntryAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignIdentifiedEntryResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == campaignId,
                cancellationToken);

        if (campaign.Status != CampaignStatuses.Live || snapshot is null)
        {
            return Result.Failure<CampaignIdentifiedEntryResponse>(
                Error.Validation(
                    "campaign.not_launched",
                    "Campaign must be launched before creating an identified respondent entry."));
        }

        if (snapshot.ResponseIdentityMode != ResponseIdentityModes.Identified)
        {
            return Result.Failure<CampaignIdentifiedEntryResponse>(
                Error.Validation(
                    "identified_entry.identity_mode_not_supported",
                    "Identified respondent entries support identified campaigns only."));
        }

        var assignment = await db.Assignments
            .AsNoTracking()
            .Where(entity =>
                entity.TenantId == tenantId &&
                entity.CampaignId == campaign.Id &&
                !entity.Anonymous &&
                entity.Role == "self" &&
                entity.RespondentSubjectId != null)
            .OrderBy(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<CampaignIdentifiedEntryResponse>(
                Error.Validation(
                    "identified_entry.recipient_assignment_required",
                    "Save identified recipients before creating identified entry links."));
        }

        var issued = OpenLinkTokens.IssueIdentifiedEntry(tenantId);
        var invitationToken = new InvitationToken(
            PlatformIds.NewId(),
            tenantId,
            campaign.Id,
            issued.TokenHash,
            InvitationTokenChannels.IdentifiedEntry,
            assignmentId: assignment.Id);

        db.InvitationTokens.Add(invitationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignIdentifiedEntryResponse(
            campaign.Id,
            assignment.Id,
            assignment.RespondentSubjectId!.Value,
            issued.RawToken,
            $"/r/{issued.RawToken}"));
    }

    public async Task<Result<CampaignInvitationBatchResponse>> CreateCampaignInvitationBatchAsync(
        Guid tenantId,
        Guid campaignId,
        CreateCampaignInvitationBatchRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == campaignId,
                cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == campaignId,
                cancellationToken);

        if (campaign.Status != CampaignStatuses.Live || snapshot is null)
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.Validation(
                    "campaign.not_launched",
                    "Campaign must be launched before creating invitation batches."));
        }

        if (!SupportsAnonymousEmailInvitations(snapshot.ResponseIdentityMode))
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.Validation(
                    "invitation_batch.identity_mode_not_supported",
                    "Email invitation batches support anonymous and anonymous repeat-participation campaigns only."));
        }

        if (await CampaignHasAccessChannelAsync(
            tenantId,
            campaign.Id,
            InvitationTokenChannels.OpenLink,
            cancellationToken))
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.Conflict(
                    "invitation_batch.open_link_access_active",
                    "This campaign already has an open respondent link. Private email invitations are disabled for open-link collection."));
        }

        var normalizedRecipients = NormalizeInvitationRecipients(request.Recipients);
        if (normalizedRecipients.IsFailure)
        {
            return Result.Failure<CampaignInvitationBatchResponse>(normalizedRecipients.Error);
        }

        var suppressedRecipients = await LoadSuppressedEmailRecipientsAsync(
            tenantId,
            normalizedRecipients.Value,
            cancellationToken);
        if (suppressedRecipients.Count > 0)
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.Conflict(
                    "invitation_batch.recipient_suppressed",
                    $"{FormatCountLabel(suppressedRecipients.Count, "recipient")} suppressed and cannot be invited: {FormatRecipientListForError(suppressedRecipients)}."));
        }

        var duplicateRecipients = await db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.CampaignId == campaignId &&
                notification.Channel == NotificationChannels.Email &&
                notification.TemplateCode == Notification.InvitationTemplateCode &&
                (notification.Status == NotificationStatuses.Queued ||
                    notification.Status == NotificationStatuses.Sent ||
                    notification.Status == NotificationStatuses.Failed) &&
                normalizedRecipients.Value.Contains(notification.Recipient))
            .Select(notification => notification.Recipient)
            .Distinct()
            .OrderBy(recipient => recipient)
            .ToListAsync(cancellationToken);

        if (duplicateRecipients.Count > 0)
        {
            return Result.Failure<CampaignInvitationBatchResponse>(
                Error.Conflict(
                    "invitation_batch.recipient_already_exists",
                    $"{FormatCountLabel(duplicateRecipients.Count, "recipient")} already have invitations: {FormatRecipientListForError(duplicateRecipients)}. Use the existing invitation status, or retry only retryable failed invitations instead of creating another invite link."));
        }

        var invitations = new List<CampaignInvitationResponse>(normalizedRecipients.Value.Count);

        foreach (var recipient in normalizedRecipients.Value)
        {
            var issued = OpenLinkTokens.IssueInvitation(tenantId);
            var invitationToken = new InvitationToken(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                issued.TokenHash,
                InvitationTokenChannels.Email,
                recipient);
            var assignment = Assignment.CreateAnonymous(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                EmailInvitationAssignmentRole,
                invitationToken.Id);
            var notification = Notification.QueueEmailInvitation(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                assignment.Id,
                recipient);

            db.InvitationTokens.Add(invitationToken);
            db.Assignments.Add(assignment);
            db.Notifications.Add(notification);
            outboxEventBuffer?.Enqueue(CreateInvitationEmailQueuedMessage(notification, invitationToken, assignment));

            invitations.Add(new CampaignInvitationResponse(
                assignment.Id,
                invitationToken.Id,
                notification.Id,
                recipient,
                Token: null,
                RespondentPath: null,
                notification.Status));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CampaignInvitationBatchResponse(
            campaign.Id,
            request.Recipients.Count,
            invitations.Count,
            invitations));
    }

    private static InstrumentSummaryResponse ToInstrumentSummary(Instrument instrument)
    {
        return new InstrumentSummaryResponse(
            instrument.Id,
            instrument.Code,
            instrument.Version,
            instrument.FullName,
            instrument.RightsStatus,
            instrument.ValidityLabel,
            instrument.CanStartNewCampaign(DateTimeOffset.UtcNow));
    }

    private async Task<Result<TemplateVersionDetailResponse>> CreateTemplateVersionCoreAsync(
        Guid tenantId,
        Guid? actorId,
        CreateTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorId,
            cancellationToken: cancellationToken);

        if (request.InstrumentId.HasValue)
        {
            var instrumentExists = await db.Instruments
                .AsNoTracking()
                .AnyAsync(instrument => instrument.Id == request.InstrumentId.Value, cancellationToken);

            if (!instrumentExists)
            {
                return Result.Failure<TemplateVersionDetailResponse>(
                    Error.NotFound("instrument.not_found", "Instrument was not found."));
            }
        }

        var createdBy = await GetPersistedActorIdAsync(actorId, cancellationToken);
        var template = SurveyTemplate.CreateTenant(
            PlatformIds.NewId(),
            tenantId,
            request.TemplateName,
            createdBy: createdBy);
        var version = TemplateVersion.CreateTenantDraft(
            PlatformIds.NewId(),
            template.Id,
            request.Semver,
            request.DefaultLocale,
            request.InstrumentId);

        var sections = new List<TemplateSection>();
        var scales = new List<QuestionScale>();
        var questions = new List<TemplateQuestion>();

        try
        {
            foreach (var requestSection in request.Sections.OrderBy(section => section.Ordinal))
            {
                sections.Add(new TemplateSection(
                    PlatformIds.NewId(),
                    version.Id,
                    requestSection.Ordinal,
                    requestSection.Code,
                    requestSection.TitleDefault));
            }

            foreach (var requestScale in request.Scales.OrderBy(scale => scale.Code))
            {
                scales.Add(new QuestionScale(
                    PlatformIds.NewId(),
                    version.Id,
                    requestScale.Code,
                    requestScale.Type,
                    requestScale.MinValue,
                    requestScale.MaxValue,
                    requestScale.Step,
                    requestScale.NaAllowed,
                    requestScale.Anchors));
            }

            var sectionByCode = sections
                .Where(section => section.Code is not null)
                .ToDictionary(section => section.Code!, StringComparer.OrdinalIgnoreCase);
            var scaleByCode = scales.ToDictionary(scale => scale.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var requestQuestion in request.Questions.OrderBy(question => question.Ordinal))
            {
                var section = ResolveSection(requestQuestion, sections, sectionByCode);
                if (section is null)
                {
                    return Result.Failure<TemplateVersionDetailResponse>(
                        Error.Validation("template_section.not_found", "Question section was not found."));
                }

                var scaleId = ResolveScaleId(requestQuestion, scales, scaleByCode);
                if (QuestionTypes.RequiresScale(requestQuestion.Type) && !scaleId.HasValue)
                {
                    return Result.Failure<TemplateVersionDetailResponse>(
                        Error.Validation("scale.not_found", "Scale-backed questions require a scale."));
                }

                questions.Add(new TemplateQuestion(
                    PlatformIds.NewId(),
                    version.Id,
                    section.Id,
                    requestQuestion.Ordinal,
                    requestQuestion.Code,
                    requestQuestion.Type,
                    scaleId,
                    requestQuestion.TextDefault,
                    required: requestQuestion.Required,
                    reverseCoded: requestQuestion.ReverseCoded,
                    measurementLevel: requestQuestion.MeasurementLevel,
                    payload: requestQuestion.Payload,
                    missingCodes: requestQuestion.MissingCodes));
            }
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<TemplateVersionDetailResponse>(
                Error.Validation("template_version.invalid", exception.Message));
        }

        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(version);
        db.TemplateSections.AddRange(sections);
        db.QuestionScales.AddRange(scales);
        db.TemplateQuestions.AddRange(questions);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToTemplateVersionDetail(template, version, sections, scales, questions));
    }

    private async Task<Guid?> GetPersistedActorIdAsync(
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (!actorId.HasValue)
        {
            return null;
        }

        return await db.UserAccounts.AnyAsync(user => user.Id == actorId.Value, cancellationToken)
            ? actorId
            : null;
    }

    private static TemplateSection? ResolveSection(
        CreateTemplateQuestionRequest question,
        IReadOnlyList<TemplateSection> sections,
        IReadOnlyDictionary<string, TemplateSection> sectionByCode)
    {
        if (!string.IsNullOrWhiteSpace(question.SectionCode))
        {
            return sectionByCode.GetValueOrDefault(question.SectionCode);
        }

        return sections.Count == 1 ? sections[0] : null;
    }

    private static Guid? ResolveScaleId(
        CreateTemplateQuestionRequest question,
        IReadOnlyList<QuestionScale> scales,
        IReadOnlyDictionary<string, QuestionScale> scaleByCode)
    {
        if (!string.IsNullOrWhiteSpace(question.ScaleCode))
        {
            return scaleByCode.TryGetValue(question.ScaleCode, out var scale)
                ? scale.Id
                : null;
        }

        return scales.Count == 1 ? scales[0].Id : null;
    }

    private static TemplateVersionDetailResponse ToTemplateVersionDetail(
        SurveyTemplate template,
        TemplateVersion version,
        IReadOnlyList<TemplateSection> sections,
        IReadOnlyList<QuestionScale> scales,
        IReadOnlyList<TemplateQuestion> questions)
    {
        return new TemplateVersionDetailResponse(
            template.Id,
            version.Id,
            template.Name,
            version.Semver,
            version.Status,
            version.DefaultLocale,
            version.InstrumentId,
            sections
                .OrderBy(section => section.Ordinal)
                .Select(section => new TemplateSectionResponse(
                    section.Id,
                    section.Ordinal,
                    section.Code,
                    section.TitleDefault))
                .ToArray(),
            scales
                .OrderBy(scale => scale.Code)
                .Select(scale => new QuestionScaleResponse(
                    scale.Id,
                    scale.Code,
                    scale.Type,
                    scale.MinValue,
                    scale.MaxValue,
                    scale.Step,
                    scale.NaAllowed,
                    scale.Anchors))
                .ToArray(),
            questions
                .OrderBy(question => question.Ordinal)
                .Select(question => new TemplateQuestionResponse(
                    question.Id,
                    question.Ordinal,
                    question.Code,
                    question.Type,
                    question.ScaleId,
                    question.TextDefault,
                    question.Required,
                    question.ReverseCoded,
                    question.MeasurementLevel))
                .ToArray());
    }

    private static CampaignDraftResponse ToCampaignDraft(Campaign campaign)
    {
        return new CampaignDraftResponse(
            campaign.Id,
            campaign.CampaignSeriesId,
            campaign.TemplateVersionId,
            campaign.Name,
            campaign.Status,
            campaign.ResponseIdentityMode);
    }

    private static Result<IReadOnlyList<string>> NormalizeInvitationRecipients(
        IReadOnlyList<InvitationRecipientRequest>? recipients)
    {
        if (recipients is null || recipients.Count == 0)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation("invitation_batch.recipients_required", "At least one invitation recipient is required."));
        }

        if (recipients.Count > MaxInvitationBatchRecipients)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation(
                    "invitation_batch.too_many_recipients",
                    $"Invitation batches support at most {MaxInvitationBatchRecipients} recipients per request."));
        }

        var normalizedRecipients = new List<string>(recipients.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var recipient in recipients)
        {
            if (!TryNormalizeEmail(recipient.Email, out var normalized))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "invitation_batch.invalid_recipient",
                        "Every invitation recipient must be a valid email address."));
            }

            if (!seen.Add(normalized))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "invitation_batch.duplicate_recipient",
                        "Invitation recipients cannot contain duplicate email addresses."));
            }

            normalizedRecipients.Add(normalized);
        }

        return Result.Success<IReadOnlyList<string>>(normalizedRecipients);
    }

    private static bool TryNormalizeEmail(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 320 || trimmed.Contains('\r', StringComparison.Ordinal) ||
            trimmed.Contains('\n', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(trimmed);
            if (!string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            normalized = address.Address.ToLowerInvariant();
            return normalized.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task RemoveExternalEmailRespondentRulesAfterMaterializationAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var rules = await db.RespondentRules
            .Where(rule => rule.CampaignId == campaignId)
            .ToListAsync(cancellationToken);
        var externalEmailRules = rules
            .Where(rule => IsExternalEmailRespondentRule(rule.Rule))
            .ToArray();

        if (externalEmailRules.Length > 0)
        {
            db.RespondentRules.RemoveRange(externalEmailRules);
        }
    }

    private async Task<bool> CampaignHasAccessChannelAsync(
        Guid tenantId,
        Guid campaignId,
        string invitationTokenChannel,
        CancellationToken cancellationToken)
    {
        return await db.InvitationTokens
            .AsNoTracking()
            .Where(invitationToken =>
                invitationToken.TenantId == tenantId &&
                invitationToken.CampaignId == campaignId &&
                invitationToken.Channel == invitationTokenChannel)
            .AnyAsync(
                invitationToken => db.Assignments
                    .AsNoTracking()
                    .Any(assignment =>
                        assignment.TenantId == tenantId &&
                        assignment.CampaignId == campaignId &&
                        (assignment.InviteTokenId == invitationToken.Id ||
                            invitationToken.AssignmentId == assignment.Id)),
                cancellationToken);
    }

    private async Task<IReadOnlyList<string>> LoadSuppressedEmailRecipientsAsync(
        Guid tenantId,
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
        {
            return [];
        }

        var manuallySuppressedRecipients = await db.EmailSuppressions
            .AsNoTracking()
            .Where(suppression =>
                suppression.TenantId == tenantId &&
                suppression.ReleasedAt == null &&
                recipients.Contains(suppression.Recipient))
            .Select(suppression => suppression.Recipient)
            .Distinct()
            .ToListAsync(cancellationToken);

        return manuallySuppressedRecipients
            .Distinct(StringComparer.Ordinal)
            .OrderBy(recipient => recipient, StringComparer.Ordinal)
            .ToList();
    }

    private static OutboxMessage CreateInvitationEmailQueuedMessage(
        Notification notification,
        InvitationToken invitationToken,
        Assignment assignment)
    {
        return new OutboxMessage(
            notification.Id,
            NotificationAggregateType,
            InvitationEmailQueuedEventType,
            OutboxPayload.Create(new Dictionary<string, object?>
            {
                ["schema_version"] = 1,
                ["notification_id"] = notification.Id,
                ["campaign_id"] = notification.CampaignId,
                ["assignment_id"] = assignment.Id,
                ["invitation_token_id"] = invitationToken.Id,
                ["channel"] = notification.Channel,
                ["template_code"] = notification.TemplateCode
            }));
    }

    private async Task<CampaignRespondentRuleListResponse> BuildCampaignRespondentRuleListResponseAsync(
        Guid tenantId,
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        var rules = await db.RespondentRules
            .AsNoTracking()
            .Where(rule => rule.CampaignId == campaign.Id)
            .OrderBy(rule => rule.Ordinal)
            .ToListAsync(cancellationToken);
        var responses = new List<CampaignRespondentRuleResponse>(rules.Count);

        foreach (var rule in rules)
        {
            responses.Add(await BuildCampaignRespondentRuleResponseAsync(
                tenantId,
                campaign,
                rule,
                cancellationToken));
        }

        return new CampaignRespondentRuleListResponse(campaign.Id, responses);
    }

    private async Task<CampaignRespondentRuleResponse> BuildCampaignRespondentRuleResponseAsync(
        Guid tenantId,
        Campaign campaign,
        RespondentRule rule,
        CancellationToken cancellationToken)
    {
        var resolution = await _respondentRuleResolver.ResolveAsync(
            new RespondentRuleResolutionRequest(
                tenantId,
                campaign.Id,
                campaign.CampaignSeriesId,
                rule.Rule),
            cancellationToken);

        if (resolution.IsFailure)
        {
            return new CampaignRespondentRuleResponse(
                rule.Id,
                rule.Ordinal,
                rule.Rule,
                "invalid",
                string.Empty,
                TargetSubjectId: null,
                GroupId: null,
                AssignmentPairCount: 0,
                Issues:
                [
                    Blocker(
                        resolution.Error.Code,
                        resolution.Error.Message)
                ]);
        }

        var resolved = resolution.Value;

        return new CampaignRespondentRuleResponse(
            rule.Id,
            rule.Ordinal,
            rule.Rule,
            resolved.RuleKind,
            resolved.Role,
            resolved.TargetSubjectId,
            resolved.GroupId,
            resolved.Candidates.Count,
            resolved.Issues
                .Select(ToLaunchReadinessIssue)
                .ToArray());
    }

    private async Task AddRespondentRuleReadinessIssuesAsync(
        Campaign campaign,
        List<LaunchReadinessIssueResponse> issues,
        CancellationToken cancellationToken)
    {
        var rules = await db.RespondentRules
            .AsNoTracking()
            .Where(rule => rule.CampaignId == campaign.Id)
            .OrderBy(rule => rule.Ordinal)
            .ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            if (campaign.ResponseIdentityMode == ResponseIdentityModes.Identified)
            {
                issues.Add(Blocker(
                    "respondent_rule.no_recipients",
                    "Save identified recipients before launch."));
            }

            return;
        }

        if (!SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode) &&
            rules.Any(rule => IsExternalEmailRespondentRule(rule.Rule)))
        {
            issues.Add(Blocker(
                "respondent_rule.identity_mode_not_supported",
                "Specific email recipient lists are available for anonymous invite-only or repeat-participation waves only."));
            return;
        }

        if (SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode))
        {
            AddEmailDeliveryReadinessIssues(issues);
        }

        foreach (var rule in rules)
        {
            var resolution = await _respondentRuleResolver.ResolveAsync(
                new RespondentRuleResolutionRequest(
                    campaign.TenantId,
                    campaign.Id,
                    campaign.CampaignSeriesId,
                    rule.Rule),
                cancellationToken);

            if (resolution.IsFailure)
            {
                issues.Add(Blocker(resolution.Error.Code, resolution.Error.Message));
                continue;
            }

            issues.AddRange(resolution.Value.Issues.Select(ToLaunchReadinessIssue));

            if (SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode))
            {
                if (resolution.Value.Candidates.Count == 0)
                {
                    issues.Add(Blocker(
                        "respondent_rule.no_recipients",
                        "Saved audience rules must resolve at least one recipient before invite-only collection can start."));
                }

                if (resolution.Value.Candidates.Any(candidate =>
                    !TryNormalizeEmail(candidate.Respondent.Email, out _)))
                {
                    issues.Add(Blocker(
                        "respondent_rule.email_required",
                        "Every saved audience recipient needs an email address before invite-only collection can start."));
                }
            }
        }
    }

    private void AddEmailDeliveryReadinessIssues(List<LaunchReadinessIssueResponse> issues)
    {
        var readiness = EmailDeliveryReadinessEvaluator.Create(new EmailDeliveryReadinessConfiguration(
            _emailDeliveryOptions.Provider,
            _emailDeliveryOptions.ManagedProviderName,
            _emailDeliveryOptions.SenderDomainVerified.ToString(CultureInfo.InvariantCulture),
            _emailDeliveryOptions.VerifiedSenderDomain,
            _emailDeliveryOptions.FromAddress,
            _emailDeliveryOptions.PublicAppBaseUrl,
            _emailDeliveryOptions.InvitationFooterText,
            _emailDeliveryOptions.Smtp.Host,
            _emailDeliveryOptions.Smtp.Port.ToString(CultureInfo.InvariantCulture),
            _emailDeliveryOptions.Smtp.EnableSsl.ToString(CultureInfo.InvariantCulture),
            _emailDeliveryOptions.Smtp.UserName,
            _emailDeliveryOptions.Smtp.Password,
            _emailDeliveryOptions.ProviderWebhookSecret,
            _emailDeliveryOptions.AwsSes.SnsTopicArn));

        foreach (var issue in readiness.Issues)
        {
            if (issue.Code == "email_delivery.provider_webhook_disabled" &&
                issue.Severity != EmailDeliveryReadinessEvaluator.BlockingSeverity)
            {
                continue;
            }

            issues.Add(issue.Severity == EmailDeliveryReadinessEvaluator.BlockingSeverity
                ? Blocker(issue.Code, issue.Message)
                : Warning(issue.Code, issue.Message));
        }
    }

    private async Task<Result<IReadOnlyList<Assignment>>> MaterializeAssignmentsFromRespondentRulesAsync(
        Guid tenantId,
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        var rules = await db.RespondentRules
            .AsNoTracking()
            .Where(rule => rule.CampaignId == campaign.Id)
            .OrderBy(rule => rule.Ordinal)
            .ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            if (campaign.ResponseIdentityMode == ResponseIdentityModes.Identified)
            {
                return Result.Failure<IReadOnlyList<Assignment>>(
                    Error.Validation(
                        "respondent_rule.no_recipients",
                        "Save identified recipients before launch."));
            }

            return Result.Success<IReadOnlyList<Assignment>>([]);
        }

        if (!SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode) &&
            rules.Any(rule => IsExternalEmailRespondentRule(rule.Rule)))
        {
            return Result.Failure<IReadOnlyList<Assignment>>(
                Error.Validation(
                    "respondent_rule.identity_mode_not_supported",
                    "Specific email recipient lists are available for anonymous invite-only or repeat-participation waves only."));
        }

        if (SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode))
        {
            return await MaterializeAnonymousInvitationAssignmentsFromRespondentRulesAsync(
                tenantId,
                campaign,
                rules,
                cancellationToken);
        }

        if (campaign.ResponseIdentityMode != ResponseIdentityModes.Identified)
        {
            return Result.Failure<IReadOnlyList<Assignment>>(
                Error.Validation(
                    "respondent_rule.identity_mode_not_supported",
                    "Saved audience rules are not supported for this response mode."));
        }

        var existingPairs = await db.Assignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.TenantId == tenantId &&
                assignment.CampaignId == campaign.Id &&
                assignment.RespondentSubjectId != null)
            .Select(assignment => new AssignmentPairKey(
                assignment.TargetSubjectId,
                assignment.RespondentSubjectId!.Value))
            .ToListAsync(cancellationToken);
        var seenPairs = existingPairs.ToHashSet();
        var assignments = new List<Assignment>();

        foreach (var rule in rules)
        {
            var resolution = await _respondentRuleResolver.ResolveAsync(
                new RespondentRuleResolutionRequest(
                    tenantId,
                    campaign.Id,
                    campaign.CampaignSeriesId,
                    rule.Rule),
                cancellationToken);
            if (resolution.IsFailure)
            {
                return Result.Failure<IReadOnlyList<Assignment>>(resolution.Error);
            }

            foreach (var candidate in resolution.Value.Candidates)
            {
                var key = new AssignmentPairKey(candidate.Target?.Id, candidate.Respondent.Id);
                if (!seenPairs.Add(key))
                {
                    continue;
                }

                assignments.Add(Assignment.CreateIdentified(
                    PlatformIds.NewId(),
                    tenantId,
                    campaign.Id,
                    resolution.Value.Role,
                    candidate.Respondent.Id,
                    candidate.Target?.Id));
            }
        }

        return Result.Success<IReadOnlyList<Assignment>>(assignments);
    }

    private async Task<Result<IReadOnlyList<Assignment>>> MaterializeAnonymousInvitationAssignmentsFromRespondentRulesAsync(
        Guid tenantId,
        Campaign campaign,
        IReadOnlyList<RespondentRule> rules,
        CancellationToken cancellationToken)
    {
        var assignments = new List<Assignment>();
        var pendingRecipients = new List<(string Recipient, string Role)>();
        var seenRecipients = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in rules)
        {
            var resolution = await _respondentRuleResolver.ResolveAsync(
                new RespondentRuleResolutionRequest(
                    tenantId,
                    campaign.Id,
                    campaign.CampaignSeriesId,
                    rule.Rule),
                cancellationToken);
            if (resolution.IsFailure)
            {
                return Result.Failure<IReadOnlyList<Assignment>>(resolution.Error);
            }

            if (resolution.Value.Candidates.Count == 0)
            {
                return Result.Failure<IReadOnlyList<Assignment>>(
                    Error.Validation(
                        "respondent_rule.no_recipients",
                        "Saved audience rules must resolve at least one recipient before invite-only collection can start."));
            }

            foreach (var candidate in resolution.Value.Candidates)
            {
                if (!TryNormalizeEmail(candidate.Respondent.Email, out var recipient))
                {
                    return Result.Failure<IReadOnlyList<Assignment>>(
                        Error.Validation(
                            "respondent_rule.email_required",
                            "Every saved audience recipient needs an email address before invite-only collection can start."));
                }

                if (!seenRecipients.Add(recipient))
                {
                    continue;
                }

                pendingRecipients.Add((recipient, resolution.Value.Role));
            }
        }

        var suppressedRecipients = await LoadSuppressedEmailRecipientsAsync(
            tenantId,
            pendingRecipients.Select(recipient => recipient.Recipient).ToArray(),
            cancellationToken);
        if (suppressedRecipients.Count > 0)
        {
            return Result.Failure<IReadOnlyList<Assignment>>(
                Error.Conflict(
                    "respondent_rule.recipient_suppressed",
                    $"{FormatCountLabel(suppressedRecipients.Count, "saved recipient")} suppressed and cannot be invited: {FormatRecipientListForError(suppressedRecipients)}."));
        }

        foreach (var pendingRecipient in pendingRecipients)
        {
            var issued = OpenLinkTokens.IssueInvitation(tenantId);
            var invitationToken = new InvitationToken(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                issued.TokenHash,
                InvitationTokenChannels.Email,
                pendingRecipient.Recipient);
            var assignment = Assignment.CreateAnonymous(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                pendingRecipient.Role,
                invitationToken.Id);
            var notification = Notification.QueueEmailInvitation(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                assignment.Id,
                pendingRecipient.Recipient);

            db.InvitationTokens.Add(invitationToken);
            db.Notifications.Add(notification);
            outboxEventBuffer?.Enqueue(CreateInvitationEmailQueuedMessage(notification, invitationToken, assignment));
            assignments.Add(assignment);
        }

        return Result.Success<IReadOnlyList<Assignment>>(assignments);
    }

    private async Task<LaunchReadinessEvaluation> EvaluateLaunchReadinessAsync(
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        var issues = new List<LaunchReadinessIssueResponse>();
        var questionCount = 0;
        ScoringRule? scoringRule = null;
        ConsentDocument? consentDocument = null;
        RetentionPolicy? retentionPolicy = null;
        DisclosurePolicy? disclosurePolicy = null;
        var now = DateTimeOffset.UtcNow;

        if (campaign.Status is not (CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            issues.Add(Blocker(
                "campaign.status_not_launchable",
                "Campaign must be draft or scheduled before launch."));
        }

        if (!ResponseIdentityModes.IsKnown(campaign.ResponseIdentityMode))
        {
            issues.Add(Blocker(
                "identity.unknown",
                "Campaign response identity mode is unknown."));
        }

        var version = await db.TemplateVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaign.TemplateVersionId, cancellationToken);

        if (version is null)
        {
            issues.Add(Blocker(
                "template_version.missing",
                "Campaign template version was not found."));
        }
        else
        {
            var sectionCount = await db.TemplateSections
                .AsNoTracking()
                .CountAsync(section => section.TemplateVersionId == version.Id, cancellationToken);
            var questions = await db.TemplateQuestions
                .AsNoTracking()
                .Where(question => question.TemplateVersionId == version.Id)
                .OrderBy(question => question.Ordinal)
                .ToListAsync(cancellationToken);
            questionCount = questions.Count;
            scoringRule = await GetLaunchScoringRuleAsync(version.Id, cancellationToken);

            if (sectionCount == 0)
            {
                issues.Add(Blocker(
                    "template.no_sections",
                    "Template version must contain at least one section before launch."));
            }

            if (questionCount == 0)
            {
                issues.Add(Blocker(
                    "template.no_questions",
                    "Template version must contain at least one question before launch."));
            }

            if (scoringRule is null)
            {
                issues.Add(Blocker(
                    "scoring_rule.missing",
                    "Template version must have at least one draft or published scoring rule before launch."));
            }
            else if (questionCount > 0)
            {
                var scales = await db.QuestionScales
                    .AsNoTracking()
                    .Where(scale => scale.TemplateVersionId == version.Id)
                    .ToDictionaryAsync(scale => scale.Id, cancellationToken);
                var previewInputs = questions
                    .Select(question => new ScoringRuleLaunchPreviewInput(
                        question.Code,
                        BuildLaunchPreviewValue(question, scales)))
                    .ToArray();
                var preview = ScoringRuleLaunchPreview.Evaluate(scoringRule.Document, previewInputs);
                if (preview.IsFailure)
                {
                    issues.Add(Blocker(preview.Error.Code, preview.Error.Message));
                }
            }

            if (version.InstrumentId.HasValue)
            {
                var instrument = await db.Instruments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        entity => entity.Id == version.InstrumentId.Value,
                        cancellationToken);

                if (instrument is null)
                {
                    issues.Add(Blocker(
                        "instrument.missing",
                        "Linked instrument was not found."));
                }
                else if (!instrument.CanStartNewCampaign(DateTimeOffset.UtcNow))
                {
                    issues.Add(Blocker(
                        "instrument.not_launchable",
                        "Linked instrument cannot start a new campaign."));
                }
            }
        }

        if (!campaign.CampaignSeriesId.HasValue)
        {
            issues.Add(Blocker(
                "consent_document.missing",
                "Campaign must belong to a campaign series with a usable consent document before launch."));
            issues.Add(Blocker(
                "retention_policy.missing",
                "Campaign must belong to a campaign series with a usable retention policy before launch."));
            issues.Add(Blocker(
                "disclosure_policy.missing",
                "Campaign must belong to a campaign series with a usable disclosure policy before launch."));
        }
        else
        {
            consentDocument = await db.ConsentDocuments
                .AsNoTracking()
                .Where(
                    document => document.CampaignSeriesId == campaign.CampaignSeriesId.Value &&
                        document.Locale == campaign.DefaultLocale &&
                        document.PublishedAt <= now &&
                        (document.RetiredAt == null || document.RetiredAt > now))
                .OrderByDescending(document => document.PublishedAt)
                .ThenByDescending(document => document.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (consentDocument is null)
            {
                issues.Add(Blocker(
                    "consent_document.missing",
                    "Campaign series must have a usable consent document for the campaign locale before launch."));
            }

            retentionPolicy = await db.RetentionPolicies
                .AsNoTracking()
                .Where(
                    policy => policy.CampaignSeriesId == campaign.CampaignSeriesId.Value &&
                        policy.CreatedAt <= now &&
                        (policy.RetiredAt == null || policy.RetiredAt > now))
                .OrderByDescending(policy => policy.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (retentionPolicy is null)
            {
                issues.Add(Blocker(
                    "retention_policy.missing",
                    "Campaign series must have a usable retention policy before launch."));
            }

            disclosurePolicy = await db.DisclosurePolicies
                .AsNoTracking()
                .Where(
                    policy => policy.CampaignSeriesId == campaign.CampaignSeriesId.Value &&
                        policy.CreatedAt <= now &&
                        (policy.RetiredAt == null || policy.RetiredAt > now))
                .OrderByDescending(policy => policy.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (disclosurePolicy is null)
            {
                issues.Add(Blocker(
                    "disclosure_policy.missing",
                    "Campaign series must have a usable disclosure policy before launch."));
            }
        }

        await AddRespondentRuleReadinessIssuesAsync(campaign, issues, cancellationToken);

        return new LaunchReadinessEvaluation(
            issues,
            questionCount,
            version?.InstrumentId,
            scoringRule,
            consentDocument,
            retentionPolicy,
            disclosurePolicy);
    }

    private static string BuildLaunchPreviewValue(
        TemplateQuestion question,
        IReadOnlyDictionary<Guid, QuestionScale> scales)
    {
        if (question.ScaleId.HasValue &&
            scales.TryGetValue(question.ScaleId.Value, out var scale))
        {
            return scale.MinValue.ToString(CultureInfo.InvariantCulture);
        }

        return "1";
    }

    private Task<ScoringRule?> GetLaunchScoringRuleAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        return db.ScoringRules
            .AsNoTracking()
            .Where(
                rule => rule.TemplateVersionId == templateVersionId &&
                    (rule.Status == ScoringRuleStatuses.Published ||
                        rule.Status == ScoringRuleStatuses.Draft))
            .OrderByDescending(rule => rule.Status == ScoringRuleStatuses.Published)
            .ThenByDescending(rule => rule.PublishedAt ?? rule.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string BuildLaunchPacket(
        Campaign campaign,
        LaunchReadinessEvaluation readiness,
        LaunchReadinessResponse readinessResponse,
        IReadOnlyCollection<Assignment> materializedAssignments,
        DateTimeOffset launchedAt,
        Guid? launchedBy)
    {
        var packet = new Dictionary<string, object?>
        {
            ["schema_version"] = 1,
            ["template"] = new Dictionary<string, object?>
            {
                ["template_version_id"] = campaign.TemplateVersionId,
                ["default_locale"] = campaign.DefaultLocale,
                ["question_count"] = readiness.TemplateQuestionCount
            },
            ["instrument"] = new Dictionary<string, object?>
            {
                ["instrument_id"] = readiness.InstrumentId,
                ["lineage_status"] = readiness.InstrumentId.HasValue ? "linked_template_version" : "tenant_private_template"
            },
            ["scoring"] = new Dictionary<string, object?>
            {
                ["scoring_rule_id"] = readiness.ScoringRule?.Id,
                ["document_hash"] = readiness.ScoringRule?.DocumentHash
            },
            ["policies"] = new Dictionary<string, object?>
            {
                ["consent_document_id"] = readiness.ConsentDocument?.Id,
                ["retention_policy_id"] = readiness.RetentionPolicy?.Id,
                ["disclosure_policy_id"] = readiness.DisclosurePolicy?.Id
            },
            ["identity"] = new Dictionary<string, object?>
            {
                ["response_identity_mode"] = campaign.ResponseIdentityMode,
                ["default_locale"] = campaign.DefaultLocale
            },
            ["respondent_rules"] = new Dictionary<string, object?>
            {
                ["materialization"] = campaign.ResponseIdentityMode switch
                {
                    ResponseIdentityModes.Identified => "identified_assignments",
                    _ when SupportsAnonymousEmailInvitations(campaign.ResponseIdentityMode) &&
                        materializedAssignments.Count > 0 =>
                        "anonymous_invitation_assignments",
                    _ => "not_applicable"
                },
                ["materialized_assignment_count"] = materializedAssignments.Count
            },
            ["launch_readiness"] = new Dictionary<string, object?>
            {
                ["ready"] = readinessResponse.Ready,
                ["issue_count"] = readinessResponse.Issues.Count,
                ["issues"] = readinessResponse.Issues
            },
            ["provenance"] = new Dictionary<string, object?>
            {
                ["source"] = "runtime_launch",
                ["campaign_id"] = campaign.Id,
                ["campaign_series_id"] = campaign.CampaignSeriesId,
                ["launched_at"] = launchedAt,
                ["launched_by"] = launchedBy
            }
        };

        return JsonSerializer.Serialize(packet, LaunchSnapshotJsonOptions);
    }

    private static LaunchReadinessResponse ToLaunchReadinessResponse(
        Guid campaignId,
        IReadOnlyList<LaunchReadinessIssueResponse> issues)
    {
        return new LaunchReadinessResponse(
            campaignId,
            Ready: issues.All(issue => issue.Severity != "blocker"),
            issues);
    }

    private static LaunchReadinessIssueResponse Blocker(string code, string message)
    {
        return new LaunchReadinessIssueResponse(code, "blocker", message);
    }

    private static LaunchReadinessIssueResponse Warning(string code, string message)
    {
        return new LaunchReadinessIssueResponse(code, "warning", message);
    }

    private static LaunchReadinessIssueResponse ToLaunchReadinessIssue(RespondentRuleResolutionIssue issue)
    {
        return new LaunchReadinessIssueResponse(issue.Code, issue.Severity, issue.Message);
    }

    private static bool IsExternalEmailRespondentRule(string rule)
    {
        try
        {
            using var document = JsonDocument.Parse(rule);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("kind", out var kind) &&
                kind.ValueKind == JsonValueKind.String &&
                string.Equals(
                    kind.GetString(),
                    RespondentRuleResolver.ExternalEmails,
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool SupportsAnonymousEmailInvitations(string responseIdentityMode)
    {
        return responseIdentityMode is ResponseIdentityModes.Anonymous or ResponseIdentityModes.AnonymousLongitudinal;
    }

    private static string FormatRecipientListForError(IReadOnlyList<string> recipients)
    {
        const int maxVisibleRecipients = 10;
        var visible = recipients.Take(maxVisibleRecipients).ToArray();
        var suffix = recipients.Count > maxVisibleRecipients
            ? $" and {recipients.Count - maxVisibleRecipients} more"
            : string.Empty;

        return $"{string.Join(", ", visible)}{suffix}";
    }

    private static string FormatCountLabel(int count, string singularLabel)
    {
        return count == 1 ? $"1 {singularLabel} is" : $"{count} {singularLabel}s are";
    }

    private static CampaignAssignmentSubjectResponse ToCampaignAssignmentSubject(Subject subject)
    {
        return new CampaignAssignmentSubjectResponse(
            subject.Id,
            CreateAssignmentSubjectLabel(subject),
            subject.DisplayName,
            subject.Email,
            subject.ExternalId);
    }

    private static string CreateAssignmentSubjectLabel(Subject subject)
    {
        return NormalizeText(subject.DisplayName) ??
            NormalizeText(subject.Email) ??
            NormalizeText(subject.ExternalId) ??
            subject.Id.ToString("D");
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Error DuplicateInstrumentCodeVersion(string code, string version)
    {
        return Error.Conflict(
            "instrument.duplicate_code_version",
            $"An instrument with code '{code}' and version '{version}' already exists for this tenant.");
    }

    private static bool IsInstrumentCodeVersionDuplicate(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ix_instrument_tenant_id_code_version"
        };
    }

    private static bool IsLaunchSnapshotDuplicate(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ix_campaign_launch_snapshot_campaign_id"
        };
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record LaunchReadinessEvaluation(
        IReadOnlyList<LaunchReadinessIssueResponse> Issues,
        int TemplateQuestionCount,
        Guid? InstrumentId,
        ScoringRule? ScoringRule,
        ConsentDocument? ConsentDocument,
        RetentionPolicy? RetentionPolicy,
        DisclosurePolicy? DisclosurePolicy);

    private sealed record AssignmentPairKey(Guid? TargetSubjectId, Guid RespondentSubjectId);
}
