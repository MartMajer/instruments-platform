using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Features.Responses;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Responses;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Responses;

public sealed class ResponseCaptureStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    IParticipantCodeStore? participantCodeStore = null,
    ICurrentTenant? currentTenant = null,
    SubmittedResponseScoreMaterializer? scoreMaterializer = null) : IResponseCaptureStore
{
    private readonly SubmittedResponseScoreMaterializer submittedScoreMaterializer =
        scoreMaterializer ?? new SubmittedResponseScoreMaterializer(db);

    public async Task<Result<RespondentCampaignResponse>> GetCampaignAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<RespondentCampaignResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CampaignId == campaign.Id, cancellationToken);
        var templateVersionId = snapshot?.TemplateVersionId ?? campaign.TemplateVersionId;
        var responseIdentityMode = snapshot?.ResponseIdentityMode ?? campaign.ResponseIdentityMode;
        var defaultLocale = snapshot?.DefaultLocale ?? campaign.DefaultLocale;

        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == templateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);
        var scaleIds = questions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new RespondentCampaignResponse(
            campaign.Id,
            templateVersionId,
            campaign.Name,
            campaign.Status,
            responseIdentityMode,
            defaultLocale,
            questions.Select(question => ToQuestionResponse(question, scales)).ToArray()));
    }

    public async Task<Result<LabAssignmentResponse>> CreateLabAssignmentAsync(
        Guid tenantId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(entity => entity.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<LabAssignmentResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CampaignId == campaign.Id, cancellationToken);
        var responseIdentityMode = snapshot?.ResponseIdentityMode ?? campaign.ResponseIdentityMode;
        var defaultLocale = snapshot?.DefaultLocale ?? campaign.DefaultLocale;

        if (responseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal)
        {
            return Result.Failure<LabAssignmentResponse>(
                Error.Validation(
                    "response.participant_codes_not_implemented",
                    "Anonymous longitudinal response capture requires participant-code persistence, which is not implemented in R01."));
        }

        var existing = await db.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.CampaignId == campaign.Id)
            .OrderBy(assignment => assignment.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new LabAssignmentResponse(
                existing.Id,
                campaign.Id,
                responseIdentityMode));
        }

        Assignment assignment;
        if (responseIdentityMode == ResponseIdentityModes.Identified)
        {
            var subject = new Subject(
                PlatformIds.NewId(),
                tenantId,
                displayName: "Response lab respondent",
                locale: defaultLocale);
            db.Subjects.Add(subject);
            assignment = Assignment.CreateIdentified(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                "self",
                subject.Id);
        }
        else
        {
            var token = new InvitationToken(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                CreateTokenHash(),
                InvitationTokenChannels.OpenLink);
            db.InvitationTokens.Add(token);
            assignment = Assignment.CreateAnonymous(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                "self",
                token.Id);
        }

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new LabAssignmentResponse(
            assignment.Id,
            campaign.Id,
            responseIdentityMode));
    }

    public async Task<Result<ResponseSessionResponse>> CreateSessionAsync(
        Guid tenantId,
        CreateResponseSessionRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var assignment = await db.Assignments
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<ResponseSessionResponse>(
                Error.NotFound("assignment.not_found", "Assignment was not found."));
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == assignment.CampaignId, cancellationToken);
        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CampaignId == campaign.Id, cancellationToken);
        var responseIdentityMode = snapshot?.ResponseIdentityMode ?? campaign.ResponseIdentityMode;

        if (responseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal)
        {
            return Result.Failure<ResponseSessionResponse>(
                Error.Validation(
                    "response.participant_codes_not_implemented",
                    "Anonymous longitudinal response sessions require participant-code persistence, which is not implemented in R01."));
        }

        var existing = await db.ResponseSessions
            .AsNoTracking()
            .Where(session => session.AssignmentId == assignment.Id)
            .OrderBy(session => session.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(ToSessionResponse(existing));
        }

        ResponseSession session;
        try
        {
            session = new ResponseSession(
                PlatformIds.NewId(),
                tenantId,
                assignment.Id,
                request.Locale);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<ResponseSessionResponse>(
                Error.Validation("response_session.invalid", exception.Message));
        }

        db.ResponseSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToSessionResponse(session));
    }

    public async Task<Result<SaveAnswersResponse>> SaveAnswersAsync(
        Guid tenantId,
        Guid sessionId,
        SaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var session = await db.ResponseSessions
            .SingleOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure<SaveAnswersResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        try
        {
            session.EnsureCanAcceptAnswers();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<SaveAnswersResponse>(
                Error.Conflict("response_session.submitted", exception.Message));
        }

        var templateVersionId = await GetSessionTemplateVersionIdAsync(session, cancellationToken);
        var answersByQuestion = request.Answers
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last());
        var requestedQuestionIds = answersByQuestion.Keys.ToArray();
        var validQuestions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question =>
                question.TemplateVersionId == templateVersionId &&
                requestedQuestionIds.Contains(question.Id))
            .ToArrayAsync(cancellationToken);

        if (validQuestions.Length != requestedQuestionIds.Length)
        {
            return Result.Failure<SaveAnswersResponse>(
                Error.Validation("answer.question_not_found", "One or more answers reference questions outside this campaign template."));
        }

        var scaleIds = validQuestions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);
        var valueValidation = ResponseAnswerValueValidator.Validate(
            ToAnswerQuestionContracts(validQuestions, scales),
            answersByQuestion.Values);

        if (valueValidation.IsFailure)
        {
            return Result.Failure<SaveAnswersResponse>(valueValidation.Error);
        }

        var existingAnswers = await db.Answers
            .Where(answer =>
                answer.SessionId == session.Id &&
                requestedQuestionIds.Contains(answer.QuestionId))
            .ToDictionaryAsync(answer => answer.QuestionId, cancellationToken);

        try
        {
            foreach (var (questionId, answerRequest) in answersByQuestion)
            {
                if (existingAnswers.TryGetValue(questionId, out var existing))
                {
                    existing.UpdateValue(
                        session,
                        answerRequest.Value,
                        answerRequest.Comment,
                        answerRequest.IsSkipped,
                        answerRequest.IsNa);
                    continue;
                }

                db.Answers.Add(new Answer(
                    PlatformIds.NewId(),
                    tenantId,
                    session.Id,
                    questionId,
                    answerRequest.Value,
                    answerRequest.Comment,
                    answerRequest.IsSkipped,
                    answerRequest.IsNa));
            }
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SaveAnswersResponse>(
                Error.Validation("answer.invalid", exception.Message));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SaveAnswersResponse(session.Id, answersByQuestion.Count));
    }

    public async Task<Result<SubmitResponseSessionResponse>> SubmitSessionAsync(
        Guid tenantId,
        Guid sessionId,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var session = await db.ResponseSessions
            .SingleOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        if (session.SubmittedAt.HasValue)
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.Conflict("response_session.submitted", "Response session has already been submitted."));
        }

        var templateVersionId = await GetSessionTemplateVersionIdAsync(session, cancellationToken);
        var templateQuestions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question =>
                question.TemplateVersionId == templateVersionId)
            .ToArrayAsync(cancellationToken);
        var scaleIds = templateQuestions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);
        var savedAnswers = await db.Answers
            .Where(answer => answer.SessionId == session.Id)
            .ToArrayAsync(cancellationToken);
        var displayLogic = ResponseDisplayLogicEvaluator.Evaluate(
            templateQuestions.Select(question => new ResponseDisplayLogicQuestion(
                question.Id,
                question.Ordinal,
                question.Code,
                question.Required,
                question.Payload)),
            savedAnswers.Select(answer => new ResponseDisplayLogicAnswer(
                answer.QuestionId,
                answer.Value,
                answer.IsSkipped,
                answer.IsNa)));
        ApplyHiddenDisplayLogicAnswers(session, tenantId, savedAnswers, displayLogic.HiddenQuestionIds);

        var savedAnswerValidation = ResponseAnswerValueValidator.ValidateSaved(
            ToAnswerQuestionContracts(templateQuestions, scales),
            savedAnswers.Select(answer => new ResponseAnswerValueContract(
                answer.QuestionId,
                answer.Value,
                answer.Comment,
                answer.IsSkipped,
                answer.IsNa)));
        if (savedAnswerValidation.IsFailure)
        {
            return Result.Failure<SubmitResponseSessionResponse>(savedAnswerValidation.Error);
        }

        var requiredQuestionIds = displayLogic.RequiredVisibleQuestionIds;
        var answeredQuestionIds = savedAnswers
            .Where(answer =>
                answer.Value != null &&
                !answer.IsSkipped &&
                !answer.IsNa)
            .Select(answer => answer.QuestionId)
            .ToArray();

        if (requiredQuestionIds.Except(answeredQuestionIds).Any())
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.Validation(
                    "response.required_answers_missing",
                    "Required questions must be answered before submit."));
        }

        DateTimeOffset submittedAt;
        try
        {
            submittedAt = DateTimeOffset.UtcNow;
            session.Submit(submittedAt, request.TimeTakenMs);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.Validation("response_session.invalid", exception.Message));
        }

        await db.SaveChangesAsync(cancellationToken);

        var scoreMaterialized = await submittedScoreMaterializer.MaterializeAsync(
            tenantId,
            session.Id,
            requireScoringRule: false,
            cancellationToken);

        if (scoreMaterialized.IsFailure)
        {
            await db.Entry(session).ReloadAsync(cancellationToken);

            return Result.Failure<SubmitResponseSessionResponse>(scoreMaterialized.Error);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SubmitResponseSessionResponse(session.Id, submittedAt));
    }

    private void ApplyHiddenDisplayLogicAnswers(
        ResponseSession session,
        Guid tenantId,
        IReadOnlyCollection<Answer> savedAnswers,
        IReadOnlySet<Guid> hiddenQuestionIds)
    {
        if (hiddenQuestionIds.Count == 0)
        {
            return;
        }

        var answersByQuestionId = savedAnswers.ToDictionary(answer => answer.QuestionId);
        foreach (var questionId in hiddenQuestionIds)
        {
            if (answersByQuestionId.TryGetValue(questionId, out var existing))
            {
                existing.UpdateValue(session, value: null, comment: null, isSkipped: true, isNa: false);
                continue;
            }

            db.Answers.Add(new Answer(
                PlatformIds.NewId(),
                tenantId,
                session.Id,
                questionId,
                value: null,
                comment: null,
                isSkipped: true,
                isNa: false));
        }
    }

    private static ResponseAnswerQuestionContract[] ToAnswerQuestionContracts(
        IEnumerable<TemplateQuestion> questions,
        IReadOnlyDictionary<Guid, QuestionScale> scales)
    {
        return questions
            .Select(question =>
            {
                QuestionScale? scale = null;
                if (question.ScaleId.HasValue)
                {
                    scales.TryGetValue(question.ScaleId.Value, out scale);
                }

                return new ResponseAnswerQuestionContract(
                    question.Id,
                    question.Code,
                    question.Type,
                    question.Payload,
                    scale?.MinValue,
                    scale?.MaxValue,
                    scale?.Step,
                    scale?.NaAllowed ?? false);
            })
            .ToArray();
    }

    public async Task<Result<OpenLinkEntryResponse>> GetOpenLinkEntryAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<OpenLinkEntryResponse>();
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var resolved = await ResolveOpenLinkAsync(
            parsed.Value.TenantId,
            token,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<OpenLinkEntryResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(parsed.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<OpenLinkEntryResponse>(tenantContext.Error);
        }

        var consentDocument = await GetSnapshotConsentDocumentAsync(
            resolved.Value.Snapshot,
            cancellationToken);

        if (consentDocument is null)
        {
            return OpenLinkNotAvailable<OpenLinkEntryResponse>();
        }

        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == resolved.Value.Snapshot.TemplateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);
        var scaleIds = questions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new OpenLinkEntryResponse(
            resolved.Value.Campaign.Id,
            resolved.Value.Assignment.Id,
            resolved.Value.Snapshot.TemplateVersionId,
            resolved.Value.Campaign.Name,
            resolved.Value.Campaign.Status,
            resolved.Value.Snapshot.ResponseIdentityMode,
            resolved.Value.Snapshot.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal,
            resolved.Value.Snapshot.DefaultLocale,
            ToConsentDocumentResponse(consentDocument),
            questions.Select(question => ToQuestionResponse(question, scales)).ToArray()));
    }

    public async Task<Result<EmailInvitationUnsubscribeResponse>> UnsubscribeEmailInvitationAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<EmailInvitationUnsubscribeResponse>();
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var tokenHash = OpenLinkTokens.Hash(token);
        var invitationToken = await db.InvitationTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == parsed.Value.TenantId &&
                    entity.TokenHash == tokenHash &&
                    entity.Channel == InvitationTokenChannels.Email &&
                    entity.Recipient != null,
                cancellationToken);

        if (invitationToken is null || string.IsNullOrWhiteSpace(invitationToken.Recipient))
        {
            return OpenLinkNotAvailable<EmailInvitationUnsubscribeResponse>();
        }

        var suppressedAt = DateTimeOffset.UtcNow;
        var existingSuppression = await db.EmailSuppressions
            .SingleOrDefaultAsync(
                suppression =>
                    suppression.TenantId == parsed.Value.TenantId &&
                    suppression.Recipient == invitationToken.Recipient &&
                    suppression.ReleasedAt == null,
                cancellationToken);
        if (existingSuppression is null)
        {
            db.EmailSuppressions.Add(new EmailSuppression(
                PlatformIds.NewId(),
                parsed.Value.TenantId,
                invitationToken.Recipient,
                EmailSuppression.RecipientUnsubscribedReason,
                EmailSuppression.RespondentInvitationSource,
                note: null,
                suppressedAt));
        }

        var pendingNotifications = await db.Notifications
            .Where(notification =>
                notification.TenantId == parsed.Value.TenantId &&
                notification.Channel == NotificationChannels.Email &&
                notification.TemplateCode == Notification.InvitationTemplateCode &&
                notification.Recipient == invitationToken.Recipient &&
                (notification.Status == NotificationStatuses.Queued ||
                    notification.Status == NotificationStatuses.Failed))
            .ToListAsync(cancellationToken);
        var pendingNotificationIds = pendingNotifications
            .Select(notification => notification.Id)
            .ToArray();
        var preparedAttempts = new List<NotificationDeliveryAttempt>();
        if (pendingNotificationIds.Length > 0)
        {
            preparedAttempts = await db.NotificationDeliveryAttempts
                .Where(attempt =>
                    attempt.TenantId == parsed.Value.TenantId &&
                    pendingNotificationIds.Contains(attempt.NotificationId) &&
                    attempt.Status == NotificationDeliveryAttempt.PreparedStatus)
                .ToListAsync(cancellationToken);
        }
        foreach (var notification in pendingNotifications)
        {
            notification.MarkBounced(EmailSuppression.RecipientUnsubscribedReason, suppressedAt);
        }

        foreach (var attempt in preparedAttempts)
        {
            attempt.MarkFailed(EmailSuppression.RecipientUnsubscribedReason, suppressedAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new EmailInvitationUnsubscribeResponse("unsubscribed"));
    }

    public async Task<Result<ResponseSessionResponse>> CreateOpenLinkSessionAsync(
        string token,
        CreateOpenLinkSessionRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<ResponseSessionResponse>();
        }

        Guid? participantCodeId = null;

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
                         parsed.Value.TenantId,
                         cancellationToken: cancellationToken))
        {
            var resolved = await ResolveOpenLinkAsync(
                parsed.Value.TenantId,
                token,
                cancellationToken);

            if (resolved.IsFailure)
            {
                return Result.Failure<ResponseSessionResponse>(resolved.Error);
            }

            var tenantContext = EnsureOpenLinkTenantContext(parsed.Value.TenantId);
            if (tenantContext.IsFailure)
            {
                return Result.Failure<ResponseSessionResponse>(tenantContext.Error);
            }

            var consentDocument = await GetSnapshotConsentDocumentAsync(
                resolved.Value.Snapshot,
                cancellationToken);

            if (consentDocument is null)
            {
                return OpenLinkNotAvailable<ResponseSessionResponse>();
            }

            var acceptedGrants = ValidateConsentAcceptance(request, consentDocument);
            if (acceptedGrants.IsFailure)
            {
                return Result.Failure<ResponseSessionResponse>(acceptedGrants.Error);
            }

            var requiresParticipantCode = ValidateParticipantCodeRequest(
                resolved.Value,
                request.ParticipantCode);

            if (requiresParticipantCode.IsFailure)
            {
                return Result.Failure<ResponseSessionResponse>(requiresParticipantCode.Error);
            }

            if (!requiresParticipantCode.Value)
            {
                var created = await CreateOpenLinkSessionRecordAsync(
                    parsed.Value.TenantId,
                    resolved.Value,
                    consentDocument,
                    request,
                    acceptedGrants.Value,
                    participantCodeId,
                    consentSubjectId: null,
                    cancellationToken);

                if (created.IsFailure)
                {
                    return created;
                }

                await transaction.CommitAsync(cancellationToken);

                return created;
            }

            await transaction.CommitAsync(cancellationToken);
        }

        var participantCode = await ResolveParticipantCodeIdAsync(
            parsed.Value.TenantId,
            token,
            request.ParticipantCode!,
            cancellationToken);

        if (participantCode.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(participantCode.Error);
        }

        participantCodeId = participantCode.Value;

        await using var createTransaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var freshResolved = await ResolveOpenLinkAsync(
            parsed.Value.TenantId,
            token,
            cancellationToken);

        if (freshResolved.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(freshResolved.Error);
        }

        var freshConsentDocument = await GetSnapshotConsentDocumentAsync(
            freshResolved.Value.Snapshot,
            cancellationToken);

        if (freshConsentDocument is null)
        {
            return OpenLinkNotAvailable<ResponseSessionResponse>();
        }

        var freshAcceptedGrants = ValidateConsentAcceptance(request, freshConsentDocument);
        if (freshAcceptedGrants.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(freshAcceptedGrants.Error);
        }

        var freshRequiresParticipantCode = ValidateParticipantCodeRequest(
            freshResolved.Value,
            request.ParticipantCode);

        if (freshRequiresParticipantCode.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(freshRequiresParticipantCode.Error);
        }

        var createdWithParticipantCode = await CreateOpenLinkSessionRecordAsync(
            parsed.Value.TenantId,
            freshResolved.Value,
            freshConsentDocument,
            request,
            freshAcceptedGrants.Value,
            participantCodeId,
            consentSubjectId: null,
            cancellationToken);

        if (createdWithParticipantCode.IsFailure)
        {
            return createdWithParticipantCode;
        }

        await createTransaction.CommitAsync(cancellationToken);

        return createdWithParticipantCode;
    }

    public async Task<Result<OpenLinkEntryResponse>> GetIdentifiedEntryAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<OpenLinkEntryResponse>();
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var resolved = await ResolveIdentifiedEntryAsync(
            parsed.Value.TenantId,
            token,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<OpenLinkEntryResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(parsed.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<OpenLinkEntryResponse>(tenantContext.Error);
        }

        var consentDocument = await GetSnapshotConsentDocumentAsync(
            resolved.Value.Snapshot,
            cancellationToken);

        if (consentDocument is null)
        {
            return OpenLinkNotAvailable<OpenLinkEntryResponse>();
        }

        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == resolved.Value.Snapshot.TemplateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);
        var scaleIds = questions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new OpenLinkEntryResponse(
            resolved.Value.Campaign.Id,
            resolved.Value.Assignment.Id,
            resolved.Value.Snapshot.TemplateVersionId,
            resolved.Value.Campaign.Name,
            resolved.Value.Campaign.Status,
            resolved.Value.Snapshot.ResponseIdentityMode,
            RequiresParticipantCode: false,
            resolved.Value.Snapshot.DefaultLocale,
            ToConsentDocumentResponse(consentDocument),
            questions.Select(question => ToQuestionResponse(question, scales)).ToArray()));
    }

    public async Task<Result<ResponseSessionResponse>> CreateIdentifiedEntrySessionAsync(
        string token,
        CreateOpenLinkSessionRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<ResponseSessionResponse>();
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var resolved = await ResolveIdentifiedEntryAsync(
            parsed.Value.TenantId,
            token,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(parsed.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(tenantContext.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.ParticipantCode))
        {
            return Result.Failure<ResponseSessionResponse>(
                Error.Validation(
                    "participant_code.not_allowed",
                    "Participant code is not allowed for this response mode."));
        }

        var consentDocument = await GetSnapshotConsentDocumentAsync(
            resolved.Value.Snapshot,
            cancellationToken);

        if (consentDocument is null)
        {
            return OpenLinkNotAvailable<ResponseSessionResponse>();
        }

        var acceptedGrants = ValidateConsentAcceptance(request, consentDocument);
        if (acceptedGrants.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(acceptedGrants.Error);
        }

        var created = await CreateOpenLinkSessionRecordAsync(
            parsed.Value.TenantId,
            resolved.Value,
            consentDocument,
            request,
            acceptedGrants.Value,
            participantCodeId: null,
            consentSubjectId: resolved.Value.Assignment.RespondentSubjectId,
            cancellationToken);

        if (created.IsFailure)
        {
            return created;
        }

        var tokenMarkedUsed = await MarkIdentifiedEntryTokenUsedAsync(
            parsed.Value.TenantId,
            resolved.Value.InvitationTokenId,
            cancellationToken);
        if (tokenMarkedUsed.IsFailure)
        {
            return Result.Failure<ResponseSessionResponse>(tokenMarkedUsed.Error);
        }

        await transaction.CommitAsync(cancellationToken);

        return created;
    }

    public async Task<Result<SaveAnswersResponse>> SaveOpenLinkAnswersAsync(
        string token,
        Guid sessionId,
        SaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveOpenLinkSessionAsync(
            token,
            sessionId,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SaveAnswersResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<SaveAnswersResponse>(tenantContext.Error);
        }

        return await SaveAnswersAsync(
            resolved.Value.TenantId,
            sessionId,
            request,
            cancellationToken);
    }

    public async Task<Result<OpenLinkSessionDraftResponse>> GetOpenLinkSessionDraftAsync(
        string token,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveOpenLinkSessionAsync(
            token,
            sessionId,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<OpenLinkSessionDraftResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<OpenLinkSessionDraftResponse>(tenantContext.Error);
        }

        return await LoadSessionDraftAsync(
            resolved.Value,
            publicHandle: null,
            includeEntry: false,
            cancellationToken);
    }

    public async Task<Result<OpenLinkSessionDraftResponse>> GetPublicSessionDraftAsync(
        string handle,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePublicSessionAsync(
            handle,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<OpenLinkSessionDraftResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<OpenLinkSessionDraftResponse>(tenantContext.Error);
        }

        return await LoadSessionDraftAsync(
            resolved.Value,
            handle,
            includeEntry: true,
            cancellationToken);
    }

    public async Task<Result<SaveAnswersResponse>> SavePublicSessionAnswersAsync(
        string handle,
        SaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePublicSessionAsync(
            handle,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SaveAnswersResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<SaveAnswersResponse>(tenantContext.Error);
        }

        return await SaveAnswersAsync(
            resolved.Value.TenantId,
            resolved.Value.SessionId,
            request,
            cancellationToken);
    }

    private async Task<Result<OpenLinkSessionDraftResponse>> LoadSessionDraftAsync(
        ResolvedOpenLinkSession resolved,
        string? publicHandle,
        bool includeEntry,
        CancellationToken cancellationToken)
    {
        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            resolved.TenantId,
            cancellationToken: cancellationToken);

        var answers = await db.Answers
            .AsNoTracking()
            .Where(answer => answer.SessionId == resolved.SessionId)
            .OrderBy(answer => answer.AnsweredAt)
            .ThenBy(answer => answer.QuestionId)
            .Select(answer => new SavedAnswerResponse(
                answer.QuestionId,
                answer.Value,
                answer.Comment,
                answer.IsSkipped,
                answer.IsNa))
            .ToArrayAsync(cancellationToken);

        OpenLinkEntryResponse? entry = null;
        if (includeEntry)
        {
            entry = await LoadEntryForResolvedSessionAsync(
                resolved,
                cancellationToken);

            if (entry is null)
            {
                return PublicSessionNotAvailable<OpenLinkSessionDraftResponse>();
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new OpenLinkSessionDraftResponse(
            new ResponseSessionResponse(
                resolved.SessionId,
                resolved.AssignmentId,
                resolved.Locale,
                resolved.StartedAt,
                resolved.SubmittedAt,
                resolved.TimeTakenMs,
                publicHandle),
            answers,
            answers.Length,
            entry));
    }

    public async Task<Result<SubmitResponseSessionResponse>> SubmitOpenLinkSessionAsync(
        string token,
        Guid sessionId,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveOpenLinkSessionAsync(
            token,
            sessionId,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SubmitResponseSessionResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<SubmitResponseSessionResponse>(tenantContext.Error);
        }

        if (!resolved.Value.ConsentRecordId.HasValue)
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.Validation(
                    "response.consent_required",
                    "Consent must be accepted before submitting."));
        }

        if (resolved.Value.ParticipantCodeId.HasValue)
        {
            await using var duplicateCheckTransaction = await tenantDbScope.BeginTransactionAsync(
                resolved.Value.TenantId,
                cancellationToken: cancellationToken);

            if (await HasSubmittedResponseForParticipantCodeAsync(
                    resolved.Value.CampaignId,
                    resolved.Value.ParticipantCodeId.Value,
                    exceptSessionId: sessionId,
                    cancellationToken))
            {
                return Result.Failure<SubmitResponseSessionResponse>(ParticipantCodeAlreadySubmitted());
            }

            await duplicateCheckTransaction.CommitAsync(cancellationToken);
        }

        return await SubmitSessionAsync(
            resolved.Value.TenantId,
            sessionId,
            request,
            cancellationToken);
    }

    public async Task<Result<SubmitResponseSessionResponse>> SubmitPublicSessionAsync(
        string handle,
        SubmitResponseSessionRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePublicSessionAsync(
            handle,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SubmitResponseSessionResponse>(resolved.Error);
        }

        var tenantContext = EnsureOpenLinkTenantContext(resolved.Value.TenantId);
        if (tenantContext.IsFailure)
        {
            return Result.Failure<SubmitResponseSessionResponse>(tenantContext.Error);
        }

        if (!resolved.Value.ConsentRecordId.HasValue)
        {
            return Result.Failure<SubmitResponseSessionResponse>(
                Error.Validation(
                    "response.consent_required",
                    "Consent must be accepted before submitting."));
        }

        if (resolved.Value.ParticipantCodeId.HasValue)
        {
            await using var duplicateCheckTransaction = await tenantDbScope.BeginTransactionAsync(
                resolved.Value.TenantId,
                cancellationToken: cancellationToken);

            if (await HasSubmittedResponseForParticipantCodeAsync(
                    resolved.Value.CampaignId,
                    resolved.Value.ParticipantCodeId.Value,
                    exceptSessionId: resolved.Value.SessionId,
                    cancellationToken))
            {
                return Result.Failure<SubmitResponseSessionResponse>(ParticipantCodeAlreadySubmitted());
            }

            await duplicateCheckTransaction.CommitAsync(cancellationToken);
        }

        return await SubmitSessionAsync(
            resolved.Value.TenantId,
            resolved.Value.SessionId,
            request,
            cancellationToken);
    }

    private Result<bool> EnsureOpenLinkTenantContext(Guid tenantId)
    {
        if (currentTenant is null)
        {
            return Result.Success(true);
        }

        if (!currentTenant.HasTenant)
        {
            currentTenant.SetTenant(tenantId, "open_link_token");
            return Result.Success(true);
        }

        return currentTenant.TenantId == tenantId
            ? Result.Success(true)
            : Result.Failure<bool>(
                Error.Forbidden(
                    "open_link.tenant_mismatch",
                    "Open-link token tenant does not match the current tenant context."));
    }

    private async Task<Result<ResponseSessionResponse>> CreateOpenLinkSessionRecordAsync(
        Guid tenantId,
        ResolvedOpenLink resolved,
        ConsentDocument consentDocument,
        CreateOpenLinkSessionRequest request,
        IReadOnlyList<string> acceptedGrants,
        Guid? participantCodeId,
        Guid? consentSubjectId,
        CancellationToken cancellationToken)
    {
        if (participantCodeId.HasValue &&
            await HasSubmittedResponseForParticipantCodeAsync(
                resolved.Campaign.Id,
                participantCodeId.Value,
                exceptSessionId: null,
                cancellationToken))
        {
            return Result.Failure<ResponseSessionResponse>(ParticipantCodeAlreadySubmitted());
        }

        var acceptedAt = DateTimeOffset.UtcNow;
        var publicHandle = OpenLinkSessionHandles.Issue(tenantId);
        var consentRecord = new ConsentRecord(
            PlatformIds.NewId(),
            tenantId,
            consentDocument.Id,
            resolved.Campaign.Id,
            resolved.Assignment.Id,
            request.Locale,
            JsonSerializer.Serialize(acceptedGrants),
            acceptedAt,
            consentSubjectId);
        ResponseSession session;
        try
        {
            session = new ResponseSession(
                PlatformIds.NewId(),
                tenantId,
                resolved.Assignment.Id,
                request.Locale,
                participantCodeId: participantCodeId,
                consentRecordId: consentRecord.Id,
                publicHandleHash: publicHandle.HandleHash,
                publicHandleIssuedAt: acceptedAt);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<ResponseSessionResponse>(
                Error.Validation("response_session.invalid", exception.Message));
        }

        db.ConsentRecords.Add(consentRecord);
        db.ResponseSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToSessionResponse(session, publicHandle.RawHandle));
    }

    private async Task<Result<Guid>> ResolveParticipantCodeIdAsync(
        Guid tenantId,
        string token,
        string rawParticipantCode,
        CancellationToken cancellationToken)
    {
        Guid campaignSeriesId;

        await using (var transaction = await tenantDbScope.BeginTransactionAsync(
                         tenantId,
                         cancellationToken: cancellationToken))
        {
            var resolved = await ResolveOpenLinkAsync(
                tenantId,
                token,
                cancellationToken);

            if (resolved.IsFailure)
            {
                return Result.Failure<Guid>(resolved.Error);
            }

            if (!resolved.Value.Snapshot.CampaignSeriesId.HasValue)
            {
                return Result.Failure<Guid>(
                    Error.Validation(
                        "participant_code.campaign_series_required",
                        "Participant-code responses require a campaign series."));
            }

            campaignSeriesId = resolved.Value.Snapshot.CampaignSeriesId.Value;
            await transaction.CommitAsync(cancellationToken);
        }

        if (participantCodeStore is null)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "participant_code.resolution_unavailable",
                    "Participant-code resolution is unavailable."));
        }

        var participantCode = await participantCodeStore.ResolveAsync(
            tenantId,
            campaignSeriesId,
            rawParticipantCode,
            cancellationToken);

        return participantCode.IsSuccess
            ? Result.Success(participantCode.Value.Id)
            : Result.Failure<Guid>(participantCode.Error);
    }

    private static Result<string[]> ValidateConsentAcceptance(
        CreateOpenLinkSessionRequest request,
        ConsentDocument consentDocument)
    {
        if (request.AcceptedConsentDocumentId != consentDocument.Id)
        {
            return Result.Failure<string[]>(
                Error.Validation(
                    "consent.document_mismatch",
                    "Accepted consent document must match the launched campaign disclosure."));
        }

        var acceptedGrants = NormalizeAcceptedGrants(request.AcceptedGrants);
        var requiredGrants = ParseGrantArray(consentDocument.RequiredGrants);
        var acceptedGrantSet = acceptedGrants.ToHashSet(StringComparer.Ordinal);

        if (requiredGrants.Any(requiredGrant => !acceptedGrantSet.Contains(requiredGrant)))
        {
            return Result.Failure<string[]>(
                Error.Validation(
                    "consent.required_grants_missing",
                    "Required consent grants must be accepted before starting a response session."));
        }

        return Result.Success(acceptedGrants);
    }

    private static Result<bool> ValidateParticipantCodeRequest(
        ResolvedOpenLink resolved,
        string? rawParticipantCode)
    {
        if (resolved.Snapshot.ResponseIdentityMode == ResponseIdentityModes.Anonymous)
        {
            return string.IsNullOrWhiteSpace(rawParticipantCode)
                ? Result.Success(false)
                : Result.Failure<bool>(
                    Error.Validation(
                        "participant_code.not_allowed",
                        "Participant code is not allowed for this response mode."));
        }

        if (resolved.Snapshot.ResponseIdentityMode != ResponseIdentityModes.AnonymousLongitudinal)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "response.identity_mode_not_supported",
                    "This public response mode is not supported."));
        }

        if (string.IsNullOrWhiteSpace(rawParticipantCode))
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "participant_code.required",
                    "Participant code is required before starting this response."));
        }

        if (!resolved.Snapshot.CampaignSeriesId.HasValue)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "participant_code.campaign_series_required",
                    "Participant-code responses require a campaign series."));
        }

        return Result.Success(true);
    }

    private Task<bool> HasSubmittedResponseForParticipantCodeAsync(
        Guid campaignId,
        Guid participantCodeId,
        Guid? exceptSessionId,
        CancellationToken cancellationToken)
    {
        return (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking() on session.AssignmentId equals assignment.Id
            where assignment.CampaignId == campaignId &&
                session.ParticipantCodeId == participantCodeId &&
                session.SubmittedAt != null &&
                (!exceptSessionId.HasValue || session.Id != exceptSessionId.Value)
            select session.Id)
            .AnyAsync(cancellationToken);
    }

    private static Error ParticipantCodeAlreadySubmitted()
    {
        return Error.Conflict(
            "participant_code.already_submitted",
            "A response for this survey and participant code has already been submitted.");
    }

    private async Task<Result<ResolvedOpenLinkSession>> ResolveOpenLinkSessionAsync(
        string token,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkTokens.ParseTenant(token);
        if (parsed.IsFailure)
        {
            return OpenLinkNotAvailable<ResolvedOpenLinkSession>();
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var resolved = await ResolveOpenLinkAsync(
            parsed.Value.TenantId,
            token,
            cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<ResolvedOpenLinkSession>(resolved.Error);
        }

        var session = await db.ResponseSessions
            .AsNoTracking()
            .Where(
                session =>
                    session.Id == sessionId &&
                    session.AssignmentId == resolved.Value.Assignment.Id)
            .Select(session => new
            {
                session.Id,
                session.AssignmentId,
                session.Locale,
                session.StartedAt,
                session.SubmittedAt,
                session.TimeTakenMs,
                session.ParticipantCodeId,
                session.ConsentRecordId
            })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (session is null)
        {
            return Result.Failure<ResolvedOpenLinkSession>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ResolvedOpenLinkSession(
            parsed.Value.TenantId,
            resolved.Value.Campaign.Id,
            resolved.Value.Snapshot.TemplateVersionId,
            resolved.Value.Campaign.Name,
            resolved.Value.Campaign.Status,
            resolved.Value.Snapshot.ResponseIdentityMode,
            resolved.Value.Snapshot.DefaultLocale,
            resolved.Value.Snapshot.ConsentDocumentId,
            session.Id,
            session.AssignmentId,
            session.Locale,
            session.StartedAt,
            session.SubmittedAt,
            session.TimeTakenMs,
            session.ParticipantCodeId,
            session.ConsentRecordId));
    }

    private async Task<Result<ResolvedOpenLinkSession>> ResolvePublicSessionAsync(
        string handle,
        CancellationToken cancellationToken)
    {
        var parsed = OpenLinkSessionHandles.ParseTenant(handle);
        if (parsed.IsFailure)
        {
            return PublicSessionNotAvailable<ResolvedOpenLinkSession>();
        }

        var handleHash = OpenLinkSessionHandles.Hash(handle);

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            parsed.Value.TenantId,
            cancellationToken: cancellationToken);

        var resolved = await (
            from session in db.ResponseSessions.AsNoTracking()
            join assignment in db.Assignments.AsNoTracking()
                on session.AssignmentId equals assignment.Id
            join campaign in db.Campaigns.AsNoTracking()
                on assignment.CampaignId equals campaign.Id
            join snapshot in db.CampaignLaunchSnapshots.AsNoTracking()
                on campaign.Id equals snapshot.CampaignId
            join invitationToken in db.InvitationTokens.AsNoTracking()
                on assignment.InviteTokenId equals invitationToken.Id into invitationTokens
            from invitationToken in invitationTokens.DefaultIfEmpty()
            where session.PublicHandleHash == handleHash
            select new
            {
                CampaignId = campaign.Id,
                CampaignTenantId = campaign.TenantId,
                campaign.Name,
                campaign.Status,
                snapshot.TemplateVersionId,
                snapshot.ResponseIdentityMode,
                snapshot.DefaultLocale,
                snapshot.ConsentDocumentId,
                SessionId = session.Id,
                session.AssignmentId,
                session.Locale,
                session.StartedAt,
                session.SubmittedAt,
                session.TimeTakenMs,
                session.ParticipantCodeId,
                session.ConsentRecordId,
                assignment.Anonymous,
                assignment.RespondentSubjectId,
                Channel = invitationToken == null ? null : invitationToken.Channel,
                UsedAt = invitationToken == null ? null : invitationToken.UsedAt,
                ExpiresAt = invitationToken == null ? null : invitationToken.ExpiresAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (resolved is null ||
            resolved.CampaignTenantId != parsed.Value.TenantId ||
            resolved.Status != CampaignStatuses.Live ||
            !IsSupportedPublicSessionShape(
                resolved.ResponseIdentityMode,
                resolved.Anonymous,
                resolved.RespondentSubjectId,
                resolved.Channel,
                resolved.UsedAt,
                resolved.ExpiresAt))
        {
            return PublicSessionNotAvailable<ResolvedOpenLinkSession>();
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ResolvedOpenLinkSession(
            parsed.Value.TenantId,
            resolved.CampaignId,
            resolved.TemplateVersionId,
            resolved.Name,
            resolved.Status,
            resolved.ResponseIdentityMode,
            resolved.DefaultLocale,
            resolved.ConsentDocumentId,
            resolved.SessionId,
            resolved.AssignmentId,
            resolved.Locale,
            resolved.StartedAt,
            resolved.SubmittedAt,
            resolved.TimeTakenMs,
            resolved.ParticipantCodeId,
            resolved.ConsentRecordId));
    }

    private static bool IsSupportedPublicSessionShape(
        string responseIdentityMode,
        bool anonymous,
        Guid? respondentSubjectId,
        string? tokenChannel,
        DateTimeOffset? tokenUsedAt,
        DateTimeOffset? tokenExpiresAt)
    {
        if (responseIdentityMode == ResponseIdentityModes.Identified)
        {
            return !anonymous && respondentSubjectId.HasValue;
        }

        if (responseIdentityMode is not (
                ResponseIdentityModes.Anonymous or
                ResponseIdentityModes.AnonymousLongitudinal) ||
            !anonymous ||
            tokenChannel is not (
                InvitationTokenChannels.OpenLink or
                InvitationTokenChannels.Email) ||
            tokenUsedAt.HasValue ||
            tokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        return true;
    }

    private Task<ConsentDocument?> GetSnapshotConsentDocumentAsync(
        CampaignLaunchSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.ConsentDocumentId.HasValue)
        {
            return Task.FromResult<ConsentDocument?>(null);
        }

        return db.ConsentDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                document => document.Id == snapshot.ConsentDocumentId.Value,
                cancellationToken);
    }

    private async Task<Result<ResolvedOpenLink>> ResolveOpenLinkAsync(
        Guid tenantId,
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = OpenLinkTokens.Hash(token);
        var invitationToken = await db.InvitationTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.TokenHash == tokenHash &&
                    (entity.Channel == InvitationTokenChannels.OpenLink ||
                        entity.Channel == InvitationTokenChannels.Email),
                cancellationToken);

        if (invitationToken is null ||
            invitationToken.UsedAt.HasValue ||
            invitationToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return OpenLinkNotAvailable<ResolvedOpenLink>();
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == invitationToken.CampaignId,
                cancellationToken);
        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == invitationToken.CampaignId,
                cancellationToken);
        var assignmentQuery = db.Assignments
            .AsNoTracking()
            .Where(entity =>
                entity.TenantId == tenantId &&
                entity.CampaignId == invitationToken.CampaignId);
        var assignment = invitationToken.AssignmentId.HasValue
            ? await assignmentQuery.SingleOrDefaultAsync(
                entity => entity.Id == invitationToken.AssignmentId.Value,
                cancellationToken)
            : await assignmentQuery.SingleOrDefaultAsync(
                entity => entity.InviteTokenId == invitationToken.Id,
                cancellationToken);

        if (campaign is null ||
            snapshot is null ||
            assignment is null ||
            campaign.TenantId != tenantId ||
            campaign.Status != CampaignStatuses.Live ||
            snapshot.ResponseIdentityMode is not (
                ResponseIdentityModes.Anonymous or
                ResponseIdentityModes.AnonymousLongitudinal) ||
            !assignment.Anonymous)
        {
            return OpenLinkNotAvailable<ResolvedOpenLink>();
        }

        return Result.Success(new ResolvedOpenLink(
            campaign,
            snapshot,
            assignment,
            invitationToken.Id));
    }

    private async Task<Result<ResolvedOpenLink>> ResolveIdentifiedEntryAsync(
        Guid tenantId,
        string token,
        CancellationToken cancellationToken)
    {
        var tokenHash = OpenLinkTokens.Hash(token);
        var invitationToken = await db.InvitationTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.TokenHash == tokenHash &&
                    entity.Channel == InvitationTokenChannels.IdentifiedEntry,
                cancellationToken);

        if (invitationToken is null ||
            invitationToken.AssignmentId is null ||
            invitationToken.UsedAt.HasValue ||
            invitationToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return OpenLinkNotAvailable<ResolvedOpenLink>();
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == invitationToken.CampaignId,
                cancellationToken);
        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.CampaignId == invitationToken.CampaignId,
                cancellationToken);
        var assignment = await db.Assignments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId && entity.Id == invitationToken.AssignmentId.Value,
                cancellationToken);

        if (campaign is null ||
            snapshot is null ||
            assignment is null ||
            campaign.TenantId != tenantId ||
            invitationToken.TenantId != tenantId ||
            assignment.TenantId != tenantId ||
            assignment.CampaignId != campaign.Id ||
            campaign.Status != CampaignStatuses.Live ||
            snapshot.ResponseIdentityMode != ResponseIdentityModes.Identified ||
            assignment.Anonymous ||
            !assignment.RespondentSubjectId.HasValue)
        {
            return OpenLinkNotAvailable<ResolvedOpenLink>();
        }

        return Result.Success(new ResolvedOpenLink(
            campaign,
            snapshot,
            assignment,
            invitationToken.Id));
    }

    private async Task<Result<bool>> MarkIdentifiedEntryTokenUsedAsync(
        Guid tenantId,
        Guid invitationTokenId,
        CancellationToken cancellationToken)
    {
        var invitationToken = await db.InvitationTokens
            .SingleOrDefaultAsync(
                entity =>
                    entity.TenantId == tenantId &&
                    entity.Id == invitationTokenId &&
                    entity.Channel == InvitationTokenChannels.IdentifiedEntry,
                cancellationToken);

        if (invitationToken is null ||
            invitationToken.UsedAt.HasValue ||
            invitationToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return OpenLinkNotAvailable<bool>();
        }

        invitationToken.MarkUsed(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    private async Task<Guid> GetSessionTemplateVersionIdAsync(
        ResponseSession session,
        CancellationToken cancellationToken)
    {
        return await (
            from assignment in db.Assignments.AsNoTracking()
            join campaign in db.Campaigns.AsNoTracking() on assignment.CampaignId equals campaign.Id
            join snapshot in db.CampaignLaunchSnapshots.AsNoTracking()
                on campaign.Id equals snapshot.CampaignId into launchSnapshots
            from snapshot in launchSnapshots.DefaultIfEmpty()
            where assignment.Id == session.AssignmentId
            select snapshot == null
                ? campaign.TemplateVersionId
                : snapshot.TemplateVersionId)
            .SingleAsync(cancellationToken);
    }

    private async Task<OpenLinkEntryResponse?> LoadEntryForResolvedSessionAsync(
        ResolvedOpenLinkSession resolved,
        CancellationToken cancellationToken)
    {
        if (!resolved.ConsentDocumentId.HasValue)
        {
            return null;
        }

        var consentDocument = await db.ConsentDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                document => document.Id == resolved.ConsentDocumentId.Value,
                cancellationToken);

        if (consentDocument is null)
        {
            return null;
        }

        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == resolved.TemplateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);
        var scaleIds = questions
            .Select(question => question.ScaleId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);

        return new OpenLinkEntryResponse(
            resolved.CampaignId,
            resolved.AssignmentId,
            resolved.TemplateVersionId,
            resolved.CampaignName,
            resolved.CampaignStatus,
            resolved.ResponseIdentityMode,
            resolved.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal,
            resolved.DefaultLocale,
            ToConsentDocumentResponse(consentDocument),
            questions.Select(question => ToQuestionResponse(question, scales)).ToArray());
    }

    private static RespondentQuestionResponse ToQuestionResponse(
        TemplateQuestion question,
        IReadOnlyDictionary<Guid, QuestionScale> scales)
    {
        QuestionScale? scale = null;
        if (question.ScaleId.HasValue)
        {
            scales.TryGetValue(question.ScaleId.Value, out scale);
        }

        return new RespondentQuestionResponse(
            question.Id,
            question.Ordinal,
            question.Code,
            question.Type,
            question.TextDefault,
            question.Required,
            question.ScaleId,
            scale?.Code,
            scale?.MinValue,
            scale?.MaxValue,
            scale?.NaAllowed,
            scale?.Anchors,
            question.Payload);
    }

    private static ResponseSessionResponse ToSessionResponse(
        ResponseSession session,
        string? publicHandle = null)
    {
        return new ResponseSessionResponse(
            session.Id,
            session.AssignmentId,
            session.Locale,
            session.StartedAt,
            session.SubmittedAt,
            session.TimeTakenMs,
            publicHandle);
    }

    private static ConsentDocumentResponse ToConsentDocumentResponse(ConsentDocument document)
    {
        return new ConsentDocumentResponse(
            document.Id,
            document.Locale,
            document.Version,
            document.Title,
            document.BodyMarkdown,
            ParseGrantArray(document.RequiredGrants),
            ParseGrantArray(document.OptionalGrants));
    }

    private static string[] ParseGrantArray(string grantJson)
    {
        return JsonSerializer.Deserialize<string[]>(grantJson) ?? [];
    }

    private static string[] NormalizeAcceptedGrants(IReadOnlyList<string>? acceptedGrants)
    {
        return (acceptedGrants ?? [])
            .Where(grant => !string.IsNullOrWhiteSpace(grant))
            .Select(grant => grant.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string CreateTokenHash()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static Result<T> OpenLinkNotAvailable<T>()
    {
        return Result.Failure<T>(
            Error.NotFound(
                "open_link.not_available",
                "This link is no longer available."));
    }

    private static Result<T> PublicSessionNotAvailable<T>()
    {
        return Result.Failure<T>(
            Error.NotFound(
                "public_session.not_found",
                "This response session is no longer available."));
    }

    private sealed record ResolvedOpenLink(
        Campaign Campaign,
        CampaignLaunchSnapshot Snapshot,
        Assignment Assignment,
        Guid InvitationTokenId);

    private sealed record ResolvedOpenLinkSession(
        Guid TenantId,
        Guid CampaignId,
        Guid TemplateVersionId,
        string CampaignName,
        string CampaignStatus,
        string ResponseIdentityMode,
        string DefaultLocale,
        Guid? ConsentDocumentId,
        Guid SessionId,
        Guid AssignmentId,
        string Locale,
        DateTimeOffset? StartedAt,
        DateTimeOffset? SubmittedAt,
        int? TimeTakenMs,
        Guid? ParticipantCodeId,
        Guid? ConsentRecordId);
}
