using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Scoring;
using Platform.Application.Features.TestData;
using Platform.Domain.Campaigns;
using Platform.Domain.Responses;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.TestData;

public sealed class TestDataSimulatorStore(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    SubmittedResponseScoreMaterializer scoreMaterializer)
    : ITestDataSimulatorStore
{
    private const string PublicRespondentRole = "public_respondent";
    private const string SimulatedSource = "test_data_tool";

    public async Task<Result<CreateCampaignTestRecipientsResponse>> CreateCampaignTestRecipientsAsync(
        Guid tenantId,
        Guid? actorUserId,
        Guid campaignId,
        CreateCampaignTestRecipientsRequest request,
        CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue)
        {
            return Result.Failure<CreateCampaignTestRecipientsResponse>(
                Error.Forbidden("actor.required", "A signed-in setup manager is required."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(entity => entity.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<CreateCampaignTestRecipientsResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (campaign.TenantId != tenantId)
        {
            return Result.Failure<CreateCampaignTestRecipientsResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (campaign.Status is not (CampaignStatuses.Draft or CampaignStatuses.Scheduled))
        {
            return Result.Failure<CreateCampaignTestRecipientsResponse>(
                Error.Conflict(
                    "test_data.campaign_already_launched",
                    "Create test recipients before launching the collection wave."));
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedDomain = NormalizeTestEmailDomain(request.EmailDomain);
        var groupName = NormalizeGroupName(request.GroupName);
        var groupId = PlatformIds.NewId();
        var batchCode = campaign.Id.ToString("N");
        var attributes = JsonSerializer.Serialize(new
        {
            simulated_test_data = true,
            source = SimulatedSource,
            campaign_id = campaign.Id,
            created_at = now
        });
        var group = new SubjectGroup(
            groupId,
            tenantId,
            SubjectGroupTypes.Cohort,
            groupName,
            campaign.WorkspaceId,
            attributes: attributes);

        var subjects = new List<Subject>(request.Count);
        var memberships = new List<SubjectMembership>(request.Count);
        for (var index = 0; index < request.Count; index++)
        {
            var subjectId = PlatformIds.NewId();
            var displayName = CreateDisplayName(index);
            var email = $"test-{batchCode}-{index + 1:0000}@{normalizedDomain}";
            var subjectAttributes = JsonSerializer.Serialize(new
            {
                simulated_test_data = true,
                source = SimulatedSource,
                campaign_id = campaign.Id,
                group_id = groupId,
                ordinal = index + 1
            });

            subjects.Add(new Subject(
                subjectId,
                tenantId,
                campaign.WorkspaceId,
                externalId: $"test-{batchCode}-{index + 1:0000}",
                email: email,
                displayName: displayName,
                locale: NormalizeLocale(request.Locale),
                attributes: subjectAttributes));
            memberships.Add(new SubjectMembership(subjectId, groupId, "respondent"));
        }

        var existingRules = await db.RespondentRules
            .Where(rule => rule.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        db.RespondentRules.RemoveRange(existingRules);
        db.SubjectGroups.Add(group);
        db.Subjects.AddRange(subjects);
        db.SubjectMemberships.AddRange(memberships);
        db.RespondentRules.Add(new RespondentRule(
            PlatformIds.NewId(),
            campaign.Id,
            ordinal: 1,
            JsonSerializer.Serialize(new
            {
                kind = "all_in_group",
                role = "self",
                group_id = groupId
            })));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CreateCampaignTestRecipientsResponse(
            campaign.Id,
            group.Id,
            group.Name,
            subjects.Count,
            SavedRecipientRuleCount: 1,
            PreviewRecipientCount: subjects.Count));
    }

    public async Task<Result<CreateCampaignTestResponsesResponse>> CreateCampaignTestResponsesAsync(
        Guid tenantId,
        Guid? actorUserId,
        Guid campaignId,
        CreateCampaignTestResponsesRequest request,
        CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Forbidden("actor.required", "A signed-in setup manager is required."));
        }

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var campaign = await db.Campaigns
            .SingleOrDefaultAsync(entity => entity.Id == campaignId, cancellationToken);

        if (campaign is null || campaign.TenantId != tenantId)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        if (campaign.Status != CampaignStatuses.Live)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Conflict(
                    "test_data.campaign_not_live",
                    "Start collection before simulating submitted responses."));
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .Where(entity => entity.CampaignId == campaign.Id)
            .OrderByDescending(entity => entity.LaunchedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var templateVersionId = snapshot?.TemplateVersionId ?? campaign.TemplateVersionId;
        var questions = await LoadQuestionsAsync(templateVersionId, cancellationToken);
        if (questions.Count == 0)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Conflict(
                    "test_data.questions_missing",
                    "This campaign has no template questions to answer."));
        }

        var assignments = await db.Assignments
            .Where(assignment =>
                assignment.TenantId == tenantId &&
                assignment.CampaignId == campaign.Id &&
                assignment.Status != AssignmentStatuses.Cancelled &&
                assignment.Status != AssignmentStatuses.Expired)
            .OrderBy(assignment => assignment.CreatedAt)
            .ThenBy(assignment => assignment.Id)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Conflict(
                    "test_data.assignments_missing",
                    "Launch the wave with saved recipients before simulating submitted responses."));
        }

        var reusableOpenLinkAssignment = assignments.Count == 1 &&
            assignments[0].Anonymous &&
            string.Equals(assignments[0].Role, PublicRespondentRole, StringComparison.Ordinal);
        var assignmentIds = assignments.Select(assignment => assignment.Id).ToArray();
        var submittedAssignmentIds = reusableOpenLinkAssignment
            ? new HashSet<Guid>()
            : await db.ResponseSessions
                .AsNoTracking()
                .Where(session =>
                    assignmentIds.Contains(session.AssignmentId) &&
                    session.SubmittedAt.HasValue)
                .Select(session => session.AssignmentId)
                .ToHashSetAsync(cancellationToken);
        var queuedNotifications = await db.Notifications
            .Where(notification =>
                notification.TenantId == tenantId &&
                notification.CampaignId == campaign.Id &&
                assignmentIds.Contains(notification.AssignmentId) &&
                notification.Status == NotificationStatuses.Queued)
            .ToListAsync(cancellationToken);
        var notificationsByAssignment = queuedNotifications
            .GroupBy(notification => notification.AssignmentId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var candidates = reusableOpenLinkAssignment
            ? Enumerable.Repeat(assignments[0], request.ResponseCount).ToArray()
            : assignments
                .Where(assignment => !submittedAssignmentIds.Contains(assignment.Id))
                .Take(request.ResponseCount)
                .ToArray();

        if (candidates.Length == 0)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Conflict(
                    "test_data.no_available_assignments",
                    "All prepared assignments already have submitted responses."));
        }

        var now = DateTimeOffset.UtcNow;
        var campaignSeriesId = campaign.CampaignSeriesId ?? campaign.WorkspaceId;
        if (campaign.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal &&
            !campaignSeriesId.HasValue)
        {
            return Result.Failure<CreateCampaignTestResponsesResponse>(
                Error.Conflict(
                    "test_data.campaign_series_required",
                    "Linked anonymous test responses require a campaign series."));
        }

        var submittedResponseCount = 0;
        var answerCount = 0;
        var scoredResponseCount = 0;
        var markedEmailSentCount = 0;

        for (var index = 0; index < candidates.Length; index++)
        {
            var assignment = candidates[index];
            var participantCodeId = campaign.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal
                ? await ResolveOrCreateSimulatedParticipantCodeAsync(
                    tenantId,
                    campaignSeriesId!.Value,
                    index,
                    now,
                    cancellationToken)
                : (Guid?)null;
            var session = new ResponseSession(
                PlatformIds.NewId(),
                tenantId,
                assignment.Id,
                campaign.DefaultLocale,
                participantCodeId,
                startedAt: now.AddMinutes(-10).AddSeconds(index * 9),
                publicHandleHash: reusableOpenLinkAssignment ? $"simulated-{campaign.Id:N}-{index + 1:0000}" : null,
                publicHandleIssuedAt: reusableOpenLinkAssignment ? now : null);
            var answers = TestDataSimulatorAnswerFactory.CreateAnswers(questions, request, index)
                .Select(answer => new Answer(
                    PlatformIds.NewId(),
                    tenantId,
                    session.Id,
                    answer.QuestionId,
                    answer.Value,
                    answer.Comment,
                    answer.IsSkipped,
                    answer.IsNa,
                    now.AddSeconds(index * 9)))
                .ToArray();

            db.ResponseSessions.Add(session);
            db.Answers.AddRange(answers);
            await db.SaveChangesAsync(cancellationToken);

            session.Submit(now.AddSeconds(index * 11), 90_000 + (index * 1_500));
            if (notificationsByAssignment.TryGetValue(assignment.Id, out var notifications))
            {
                foreach (var notification in notifications)
                {
                    notification.MarkSent(now.AddSeconds(index));
                    markedEmailSentCount++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            var materialized = await scoreMaterializer.MaterializeAsync(
                tenantId,
                session.Id,
                requireScoringRule: false,
                cancellationToken);
            if (materialized.IsFailure)
            {
                return Result.Failure<CreateCampaignTestResponsesResponse>(materialized.Error);
            }

            if (materialized.Value.ScoreRunId.HasValue)
            {
                scoredResponseCount++;
            }

            await db.SaveChangesAsync(cancellationToken);

            submittedResponseCount++;
            answerCount += answers.Length;
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CreateCampaignTestResponsesResponse(
            campaign.Id,
            request.ResponseCount,
            submittedResponseCount,
            answerCount,
            scoredResponseCount,
            markedEmailSentCount,
            request.TargetOutcome,
            request.Variation));
    }

    private async Task<Guid> ResolveOrCreateSimulatedParticipantCodeAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        int respondentIndex,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        var hash = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                $"test-data-tool:{tenantId:N}:{campaignSeriesId:N}:{respondentIndex + 1:0000}"));
        var existingCodes = await db.ParticipantCodes
            .Where(code =>
                code.TenantId == tenantId &&
                code.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken);
        var existing = existingCodes.SingleOrDefault(code => code.Hash.SequenceEqual(hash));
        if (existing is not null)
        {
            existing.SeenAgain(seenAt);
            await db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var participantCode = new ParticipantCode(
            PlatformIds.NewId(),
            tenantId,
            campaignSeriesId,
            hash,
            ParticipantCode.MinimumArgon2MemoryKiB,
            ParticipantCode.MinimumArgon2Iterations,
            ParticipantCode.MinimumArgon2Parallelism,
            ParticipantCode.MinimumArgon2OutputBytes,
            seenAt);
        db.ParticipantCodes.Add(participantCode);
        await db.SaveChangesAsync(cancellationToken);

        return participantCode.Id;
    }

    private async Task<IReadOnlyList<TestDataSimulatorQuestion>> LoadQuestionsAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == templateVersionId)
            .OrderBy(question => question.Ordinal)
            .ToListAsync(cancellationToken);
        var scaleIds = questions
            .Where(question => question.ScaleId.HasValue)
            .Select(question => question.ScaleId!.Value)
            .Distinct()
            .ToArray();
        var scales = await db.QuestionScales
            .AsNoTracking()
            .Where(scale => scaleIds.Contains(scale.Id))
            .ToDictionaryAsync(scale => scale.Id, cancellationToken);
        var questionIds = questions.Select(question => question.Id).ToArray();
        var choiceOptions = await db.ChoiceOptions
            .AsNoTracking()
            .Where(option => questionIds.Contains(option.QuestionId))
            .OrderBy(option => option.Ordinal)
            .ToListAsync(cancellationToken);
        var choiceOptionsByQuestion = choiceOptions
            .GroupBy(option => option.QuestionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return questions
            .Select(question =>
            {
                QuestionScale? scale = null;
                if (question.ScaleId.HasValue)
                {
                    scales.TryGetValue(question.ScaleId.Value, out scale);
                }

                var payload = BuildQuestionPayload(question, choiceOptionsByQuestion);

                return new TestDataSimulatorQuestion(
                    question.Id,
                    question.Code,
                    question.Type,
                    question.Required,
                    scale?.MinValue,
                    scale?.MaxValue,
                    payload);
            })
            .ToArray();
    }

    private static string BuildQuestionPayload(
        TemplateQuestion question,
        IReadOnlyDictionary<Guid, ChoiceOption[]> choiceOptionsByQuestion)
    {
        if (PayloadHasOptions(question.Payload) ||
            !choiceOptionsByQuestion.TryGetValue(question.Id, out var options) ||
            options.Length == 0)
        {
            return question.Payload;
        }

        return JsonSerializer.Serialize(new
        {
            options = options.Select(option => new
            {
                code = option.Value,
                label = option.LabelDefault
            })
        });
    }

    private static bool PayloadHasOptions(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("options", out var options) &&
                options.ValueKind == JsonValueKind.Array &&
                options.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeGroupName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 128 ? trimmed : trimmed[..128];
    }

    private static string NormalizeTestEmailDomain(string value)
    {
        var normalized = value.Trim().TrimStart('@').ToLowerInvariant();
        return normalized.Length == 0 ? "test.validatedscale.local" : normalized;
    }

    private static string NormalizeLocale(string value)
    {
        var normalized = value.Trim();
        return normalized.Length == 0 ? "en" : normalized;
    }

    private static string CreateDisplayName(int index)
    {
        ReadOnlySpan<string> firstNames =
        [
            "Ada",
            "Bruno",
            "Carla",
            "Dario",
            "Ena",
            "Filip",
            "Greta",
            "Hana",
            "Ivan",
            "Jana"
        ];
        ReadOnlySpan<string> lastNames =
        [
            "Novak",
            "Horvat",
            "Maric",
            "Kovac",
            "Babic",
            "Jukic",
            "Peric",
            "Vidic",
            "Tomic",
            "Kralj"
        ];

        return $"{firstNames[index % firstNames.Length]} {lastNames[(index / firstNames.Length) % lastNames.Length]}";
    }
}
