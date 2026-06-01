using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.ProductSurfaces;
using Platform.Domain.Campaigns;
using Platform.Domain.Consent;
using Platform.Domain.Reports;
using Platform.Domain.Responses;
using Platform.Domain.Scoring;
using Platform.Domain.Subjects;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Scoring;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Infrastructure.ProductSurfaces;

public sealed class SampleStudySeeder(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    SubmittedResponseScoreMaterializer scoreMaterializer) : ISampleStudySeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int SampleDisclosureKMin = 5;
    private static readonly string[] SampleResultsMatrixColumns =
    [
        "result_scope",
        "result_scope_label",
        "campaign_series_id",
        "selected_campaign_id",
        "selected_campaign_name",
        "campaign_id",
        "campaign_name",
        "campaign_status",
        "campaign_data_finality",
        "campaign_closed_at",
        "group_type",
        "group_name",
        "dimension_code",
        "score_display_label",
        "score_calculation",
        "score_calculation_label",
        "score_range_min",
        "score_range_max",
        "disclosure",
        "submitted_response_count",
        "score_count",
        "n_valid_total",
        "n_expected_total",
        "missing_policy_status_summary",
        "mean",
        "median",
        "standard_deviation",
        "min",
        "max",
        "delta_from_previous_mean",
        "delta_from_first_mean",
        "comparison_state",
        "suppression_reason"
    ];

    public async Task<Result<EnsureSampleStudiesResponse>> EnsureAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var createdSeriesIds = new List<Guid>();

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            actorUserId,
            cancellationToken: cancellationToken);

        var existingSampleSeries = await db.CampaignSeries
            .AsNoTracking()
            .Where(series =>
                series.TenantId == tenantId &&
                series.StudyKind == CampaignSeriesStudyKinds.Sample)
            .Select(series => new ExistingSampleSeries(series.Id, series.Name))
            .ToListAsync(cancellationToken);
        var currentSampleNameSet = SampleStudySpecs.All
            .Select(spec => spec.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleSeries in existingSampleSeries.Where(series => !currentSampleNameSet.Contains(series.Name)))
        {
            await DeleteSampleStudyAsync(tenantId, staleSeries.Id, cancellationToken);
        }

        foreach (var spec in SampleStudySpecs.All)
        {
            var existingSeries = existingSampleSeries.SingleOrDefault(series =>
                string.Equals(series.Name, spec.Name, StringComparison.OrdinalIgnoreCase));
            if (existingSeries is not null)
            {
                if (await IsSampleStudyCurrentAsync(tenantId, existingSeries.Id, spec, cancellationToken))
                {
                    continue;
                }

                await DeleteSampleStudyAsync(tenantId, existingSeries.Id, cancellationToken);
            }

            var seriesId = await CreateSampleStudyAsync(
                tenantId,
                actorUserId,
                spec,
                cancellationToken);
            createdSeriesIds.Add(seriesId);
        }

        var sampleSeries = await db.CampaignSeries
            .AsNoTracking()
            .Where(series =>
                series.TenantId == tenantId &&
                series.StudyKind == CampaignSeriesStudyKinds.Sample)
            .Select(series => new ExistingSampleSeries(series.Id, series.Name))
            .ToListAsync(cancellationToken);

        foreach (var spec in SampleStudySpecs.All)
        {
            var series = sampleSeries.SingleOrDefault(
                item => string.Equals(item.Name, spec.Name, StringComparison.OrdinalIgnoreCase));
            if (series is not null)
            {
                await EnsureSeriesResponseExportAsync(
                    tenantId,
                    series.Id,
                    spec,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
                await EnsureSeriesResultsMatrixExportAsync(
                    tenantId,
                    series.Id,
                    spec,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new EnsureSampleStudiesResponse(
            tenantId,
            existingSampleSeries.Count,
            createdSeriesIds.Count,
            createdSeriesIds));
    }

    private async Task<bool> IsSampleStudyCurrentAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec,
        CancellationToken cancellationToken)
    {
        var campaignCount = await db.Campaigns
            .AsNoTracking()
            .CountAsync(campaign => campaign.TenantId == tenantId && campaign.CampaignSeriesId == campaignSeriesId, cancellationToken);
        if (campaignCount < spec.Waves.Count)
        {
            return false;
        }

        var expectedScoreCodes = spec.Scores
            .Select(score => score.Code)
            .ToHashSet(StringComparer.Ordinal);
        var scoreCodes = await (
                from score in db.Scores.AsNoTracking()
                join campaign in db.Campaigns.AsNoTracking()
                    on score.CampaignId equals campaign.Id
                where score.TenantId == tenantId &&
                    campaign.CampaignSeriesId == campaignSeriesId
                select score.DimensionCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (!expectedScoreCodes.IsSubsetOf(scoreCodes))
        {
            return false;
        }

        var campaignSeriesMarker = campaignSeriesId.ToString();
        var groupAttributes = await db.SubjectGroups
            .AsNoTracking()
            .Where(group => group.TenantId == tenantId)
            .Select(group => group.Attributes)
            .ToListAsync(cancellationToken);
        var groupCount = groupAttributes.Count(attributes =>
            attributes.Contains(campaignSeriesMarker, StringComparison.Ordinal));
        if (groupCount < spec.Groups.Count)
        {
            return false;
        }

        var responseExport = await db.ExportArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(artifact =>
                artifact.TenantId == tenantId &&
                artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
                cancellationToken);
        var matrixExport = await db.ExportArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(artifact =>
                artifact.TenantId == tenantId &&
                artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook,
                cancellationToken);

        return responseExport is not null &&
            IsRowLevelResponseExport(responseExport) &&
            matrixExport is not null &&
            IsSampleResultsMatrixExport(matrixExport);
    }

    private async Task DeleteSampleStudyAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        CancellationToken cancellationToken)
    {
        var series = await db.CampaignSeries
            .SingleOrDefaultAsync(entity =>
                entity.TenantId == tenantId &&
                entity.Id == campaignSeriesId &&
                entity.StudyKind == CampaignSeriesStudyKinds.Sample,
                cancellationToken);
        if (series is null)
        {
            return;
        }

        var campaigns = await db.Campaigns
            .Where(campaign => campaign.TenantId == tenantId && campaign.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken);
        var campaignIds = campaigns.Select(campaign => campaign.Id).ToArray();
        var templateVersionIds = campaigns
            .Select(campaign => campaign.TemplateVersionId)
            .Distinct()
            .ToArray();
        var templateIds = templateVersionIds.Length == 0
            ? []
            : await db.TemplateVersions
                .AsNoTracking()
                .Where(version => templateVersionIds.Contains(version.Id))
                .Select(version => version.TemplateId)
                .Distinct()
                .ToListAsync(cancellationToken);
        var assignmentIds = campaignIds.Length == 0
            ? []
            : await db.Assignments
                .AsNoTracking()
                .Where(assignment => campaignIds.Contains(assignment.CampaignId))
                .Select(assignment => assignment.Id)
                .ToListAsync(cancellationToken);
        var sessionIds = assignmentIds.Count == 0
            ? []
            : await db.ResponseSessions
                .AsNoTracking()
                .Where(session => assignmentIds.Contains(session.AssignmentId))
                .Select(session => session.Id)
                .ToListAsync(cancellationToken);
        var scoreRunIds = sessionIds.Count == 0
            ? []
            : await db.ScoreRuns
                .AsNoTracking()
                .Where(run => sessionIds.Contains(run.ResponseSessionId))
                .Select(run => run.Id)
                .ToListAsync(cancellationToken);
        var campaignSeriesMarker = campaignSeriesId.ToString();
        var subjectRows = await db.Subjects
            .AsNoTracking()
            .Where(subject => subject.TenantId == tenantId)
            .Select(subject => new { subject.Id, subject.Attributes })
            .ToListAsync(cancellationToken);
        var subjectIds = subjectRows
            .Where(subject => subject.Attributes.Contains(campaignSeriesMarker, StringComparison.Ordinal))
            .Select(subject => subject.Id)
            .ToList();
        var groupRows = await db.SubjectGroups
            .AsNoTracking()
            .Where(group => group.TenantId == tenantId)
            .Select(group => new { group.Id, group.Attributes })
            .ToListAsync(cancellationToken);
        var groupIds = groupRows
            .Where(group => group.Attributes.Contains(campaignSeriesMarker, StringComparison.Ordinal))
            .Select(group => group.Id)
            .ToList();

        if (sessionIds.Count > 0)
        {
            db.Answers.RemoveRange(await db.Answers
                .Where(answer => sessionIds.Contains(answer.SessionId))
                .ToListAsync(cancellationToken));
        }

        if (scoreRunIds.Count > 0)
        {
            db.Scores.RemoveRange(await db.Scores
                .Where(score => scoreRunIds.Contains(score.ScoreRunId))
                .ToListAsync(cancellationToken));
            db.ScoreRuns.RemoveRange(await db.ScoreRuns
                .Where(run => scoreRunIds.Contains(run.Id))
                .ToListAsync(cancellationToken));
        }

        if (sessionIds.Count > 0)
        {
            db.ResponseSessions.RemoveRange(await db.ResponseSessions
                .Where(session => sessionIds.Contains(session.Id))
                .ToListAsync(cancellationToken));
        }

        if (campaignIds.Length > 0)
        {
            db.InvitationTokens.RemoveRange(await db.InvitationTokens
                .Where(token => campaignIds.Contains(token.CampaignId))
                .ToListAsync(cancellationToken));
            db.Assignments.RemoveRange(await db.Assignments
                .Where(assignment => campaignIds.Contains(assignment.CampaignId))
                .ToListAsync(cancellationToken));
            db.CampaignLaunchSnapshots.RemoveRange(await db.CampaignLaunchSnapshots
                .Where(snapshot => campaignIds.Contains(snapshot.CampaignId))
                .ToListAsync(cancellationToken));
            db.ExportArtifacts.RemoveRange(await db.ExportArtifacts
                .Where(artifact =>
                    artifact.TenantId == tenantId &&
                    (artifact.CampaignSeriesId == campaignSeriesId ||
                        artifact.CampaignId.HasValue && campaignIds.Contains(artifact.CampaignId.Value)))
                .ToListAsync(cancellationToken));
        }

        if (subjectIds.Count > 0 || groupIds.Count > 0)
        {
            db.SubjectMemberships.RemoveRange(await db.SubjectMemberships
                .Where(membership =>
                    subjectIds.Contains(membership.SubjectId) ||
                    groupIds.Contains(membership.GroupId))
                .ToListAsync(cancellationToken));
        }

        if (subjectIds.Count > 0)
        {
            db.Subjects.RemoveRange(await db.Subjects
                .Where(subject => subjectIds.Contains(subject.Id))
                .ToListAsync(cancellationToken));
        }

        if (groupIds.Count > 0)
        {
            db.SubjectGroups.RemoveRange(await db.SubjectGroups
                .Where(group => groupIds.Contains(group.Id))
                .ToListAsync(cancellationToken));
        }

        db.ParticipantCodes.RemoveRange(await db.ParticipantCodes
            .Where(code => code.TenantId == tenantId && code.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken));
        db.ConsentDocuments.RemoveRange(await db.ConsentDocuments
            .Where(document => document.TenantId == tenantId && document.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken));
        db.RetentionPolicies.RemoveRange(await db.RetentionPolicies
            .Where(policy => policy.TenantId == tenantId && policy.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken));
        db.DisclosurePolicies.RemoveRange(await db.DisclosurePolicies
            .Where(policy => policy.TenantId == tenantId && policy.CampaignSeriesId == campaignSeriesId)
            .ToListAsync(cancellationToken));
        db.Campaigns.RemoveRange(campaigns);

        if (templateVersionIds.Length > 0)
        {
            db.ScoringRules.RemoveRange(await db.ScoringRules
                .Where(rule => templateVersionIds.Contains(rule.TemplateVersionId))
                .ToListAsync(cancellationToken));
            db.TemplateQuestions.RemoveRange(await db.TemplateQuestions
                .Where(question => templateVersionIds.Contains(question.TemplateVersionId))
                .ToListAsync(cancellationToken));
            db.QuestionScales.RemoveRange(await db.QuestionScales
                .Where(scale => templateVersionIds.Contains(scale.TemplateVersionId))
                .ToListAsync(cancellationToken));
            db.TemplateSections.RemoveRange(await db.TemplateSections
                .Where(section => templateVersionIds.Contains(section.TemplateVersionId))
                .ToListAsync(cancellationToken));
            db.TemplateVersions.RemoveRange(await db.TemplateVersions
                .Where(version => templateVersionIds.Contains(version.Id))
                .ToListAsync(cancellationToken));
        }

        if (templateIds.Count > 0)
        {
            db.SurveyTemplates.RemoveRange(await db.SurveyTemplates
                .Where(template => templateIds.Contains(template.Id))
                .ToListAsync(cancellationToken));
        }

        db.CampaignSeries.Remove(series);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> CreateSampleStudyAsync(
        Guid tenantId,
        Guid actorUserId,
        SampleStudySpec spec,
        CancellationToken cancellationToken)
    {
        var baseTime = DateTimeOffset.UtcNow.AddDays(-28);
        var series = new CampaignSeries(
            PlatformIds.NewId(),
            tenantId,
            spec.Name,
            HashBytes($"{tenantId:N}:{spec.Key}:series-salt"),
            studyKind: CampaignSeriesStudyKinds.Sample,
            sampleScenario: spec.SampleScenario,
            studyPurpose: $"Read-only sample showing the {spec.Name} workflow from setup through results.",
            studyAudience: "Synthetic respondents generated for product evaluation.",
            studyDesignType: spec.StudyDesignType,
            studyIntendedUse: CampaignSeriesStudyIntendedUseTypes.InternalReview,
            studyInterpretationBoundary: "Synthetic sample data only. Use it to learn the product flow, not as external evidence.",
            studyOwnerNotes: "Generated sample study for workspace onboarding.");
        var template = SurveyTemplate.CreateTenant(
            PlatformIds.NewId(),
            tenantId,
            $"{spec.Name} questionnaire",
            spec.TemplateDescription,
            createdBy: actorUserId);
        var templateVersion = TemplateVersion.CreateTenantDraft(
            PlatformIds.NewId(),
            template.Id,
            "1.0.0",
            "en");
        templateVersion.Publish(actorUserId, baseTime.AddDays(1));

        var section = new TemplateSection(
            PlatformIds.NewId(),
            templateVersion.Id,
            ordinal: 1,
            code: "core",
            titleDefault: spec.SectionTitle);
        var scale = new QuestionScale(
            PlatformIds.NewId(),
            templateVersion.Id,
            code: "agreement_1_7",
            type: ScaleTypes.Likert,
            minValue: 1,
            maxValue: 7,
            step: 1,
            naAllowed: false,
            anchors: """[{"value":1,"label":"Strongly disagree"},{"value":7,"label":"Strongly agree"}]""");
        var questions = spec.Questions
            .Select((question, index) => new TemplateQuestion(
                PlatformIds.NewId(),
                templateVersion.Id,
                section.Id,
                index + 1,
                question.Code,
                QuestionTypes.Likert,
                scale.Id,
                question.Text,
                required: true,
                measurementLevel: MeasurementLevels.Ordinal))
            .ToArray();

        var scoringDocument = BuildSampleScoringDocument(spec);
        var scoringRule = ScoringRule.CreateDraft(
            PlatformIds.NewId(),
            templateVersion.Id,
            $"{spec.Key}_score",
            "1.0.0",
            "graph-v1",
            "1.0.0",
            Sha256Hex(scoringDocument),
            scoringDocument,
            JsonSerializer.Serialize(new
            {
                scores = spec.Scores.Select(score => score.Code).ToArray(),
                outputs = spec.Scores.Select(score => new
                {
                    code = score.Code,
                    label = score.Label,
                    calculation = score.Calculation,
                    calculation_label = ScoreCalculationLabel(score.Calculation),
                    score_range = new
                    {
                        min = score.ScoreRangeMin,
                        max = score.ScoreRangeMax
                    }
                }).ToArray()
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                sample_study = true,
                interpretation = "Synthetic sample result outputs. Higher values indicate better self-reported conditions for this sample only.",
                outputs = spec.Scores.Select(score => new { score.Code, score.Label }).ToArray()
            }, JsonOptions));
        scoringRule.Publish(actorUserId, baseTime.AddDays(1).AddMinutes(5));

        var consent = new ConsentDocument(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            "en",
            "1.0.0",
            $"{spec.Name} sample consent",
            "Synthetic read-only sample data for product walkthroughs.",
            """["participation"]""",
            "[]",
            baseTime.AddDays(1));
        var retention = new RetentionPolicy(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            "1.0.0",
            retainForYears: 2,
            RetentionPolicy.WaveClosedAt,
            RetentionPolicy.Anonymize,
            DateOnly.FromDateTime(baseTime.AddYears(1).DateTime),
            """{"sample_study":true,"publication":"internal_demo_only"}""",
            baseTime.AddDays(1));
        var disclosure = new DisclosurePolicy(
            PlatformIds.NewId(),
            tenantId,
            series.Id,
            "1.0.0",
            kMin: 5,
            DisclosurePolicy.HideCell,
            """["overall"]""",
            baseTime.AddDays(1));

        db.CampaignSeries.Add(series);
        db.SurveyTemplates.Add(template);
        db.TemplateVersions.Add(templateVersion);
        db.TemplateSections.Add(section);
        db.QuestionScales.Add(scale);
        db.TemplateQuestions.AddRange(questions);
        db.ScoringRules.Add(scoringRule);
        db.ConsentDocuments.Add(consent);
        db.RetentionPolicies.Add(retention);
        db.DisclosurePolicies.Add(disclosure);
        await db.SaveChangesAsync(cancellationToken);

        var directory = CreateSampleDirectory(tenantId, series.Id, spec);
        db.SubjectGroups.AddRange(directory.Groups);
        db.Subjects.AddRange(directory.Subjects);
        db.SubjectMemberships.AddRange(directory.Memberships);
        await db.SaveChangesAsync(cancellationToken);

        var sessions = new List<ResponseSession>();
        var participantCodes = spec.ResponseIdentityMode == ResponseIdentityModes.AnonymousLongitudinal
            ? CreateParticipantCodes(tenantId, series.Id, spec.RespondentCount, baseTime.AddDays(2))
            : [];
        if (participantCodes.Count > 0)
        {
            db.ParticipantCodes.AddRange(participantCodes);
            await db.SaveChangesAsync(cancellationToken);
        }

        for (var waveIndex = 0; waveIndex < spec.Waves.Count; waveIndex++)
        {
            var wave = spec.Waves[waveIndex];
            var launchedAt = baseTime.AddDays(7 + (waveIndex * 7));
            var closedAt = launchedAt.AddDays(5);
            var campaign = new Campaign(
                PlatformIds.NewId(),
                tenantId,
                templateVersion.Id,
                wave.Name,
                spec.ResponseIdentityMode,
                campaignSeriesId: series.Id,
                createdBy: actorUserId);
            campaign.Launch(launchedAt);

            var launchSnapshot = new CampaignLaunchSnapshot(
                PlatformIds.NewId(),
                tenantId,
                campaign.Id,
                series.Id,
                templateVersion.Id,
                scoringRule.Id,
                spec.ResponseIdentityMode,
                "en",
                questions.Length,
                scoringRule.DocumentHash,
                """{"status":"ready","sample_study":true}""",
                launchedAt,
                actorUserId,
                consent.Id,
                retention.Id,
                disclosure.Id,
                """{"schema_version":1,"source":"sample_study_seed","template":{"status":"published"},"scoring":{"status":"published"},"policies":{"status":"configured"},"launch_readiness":{"status":"ready"}}""");

            db.Campaigns.Add(campaign);
            db.CampaignLaunchSnapshots.Add(launchSnapshot);

            for (var respondentIndex = 0; respondentIndex < spec.RespondentCount; respondentIndex++)
            {
                var assignmentPlan = CreateSampleAssignmentPlan(spec, directory, respondentIndex);
                var respondent = assignmentPlan.Respondent;
                InvitationToken? token = null;
                var assignment = spec.ResponseIdentityMode == ResponseIdentityModes.Identified
                    ? Assignment.CreateIdentified(
                        PlatformIds.NewId(),
                        tenantId,
                        campaign.Id,
                        assignmentPlan.Role,
                        respondent.SubjectId,
                        targetSubjectId: assignmentPlan.Target.SubjectId)
                    : Assignment.CreateAnonymous(
                        PlatformIds.NewId(),
                        tenantId,
                        campaign.Id,
                        assignmentPlan.Role,
                        (token = new InvitationToken(
                            PlatformIds.NewId(),
                            tenantId,
                            campaign.Id,
                            Sha256Hex($"{tenantId:N}:{spec.Key}:{waveIndex}:{respondentIndex}:token"),
                            InvitationTokenChannels.OpenLink,
                            expiresAt: closedAt.AddDays(30))).Id,
                        targetSubjectId: assignmentPlan.Target.SubjectId);
                var participantCodeId = participantCodes.Count == 0
                    ? (Guid?)null
                    : participantCodes[respondentIndex].Id;
                var session = new ResponseSession(
                    PlatformIds.NewId(),
                    tenantId,
                    assignment.Id,
                    "en",
                    participantCodeId,
                    startedAt: launchedAt.AddHours(2).AddMinutes(respondentIndex));
                var answers = questions
                    .Select((question, questionIndex) =>
                    {
                        var questionSpec = spec.Questions[questionIndex];
                        var targetMean = wave.TargetMeans.TryGetValue(questionSpec.ScoreCode, out var configuredMean)
                            ? configuredMean
                            : wave.TargetMeans.Values.DefaultIfEmpty(4.0).Average();

                        return new Answer(
                            PlatformIds.NewId(),
                            tenantId,
                            session.Id,
                            question.Id,
                            JsonSerializer.Serialize(CreateAnswerValue(
                                targetMean,
                                respondentIndex,
                                questionIndex,
                                waveIndex,
                                assignmentPlan.Target.GroupOffset)),
                            answeredAt: session.StartedAt?.AddMinutes(1 + questionIndex));
                    })
                    .ToArray();
                session.Submit(
                    launchedAt.AddHours(2).AddMinutes(respondentIndex + 6),
                    timeTakenMs: 120_000 + (respondentIndex * 1_000));

                if (token is not null)
                {
                    db.InvitationTokens.Add(token);
                }

                db.Assignments.Add(assignment);
                db.ResponseSessions.Add(session);
                db.Answers.AddRange(answers);
                sessions.Add(session);
            }

            campaign.Close("Synthetic sample wave closed.", actorUserId, closedAt);
            db.ExportArtifacts.Add(CreateCampaignReportExport(
                tenantId,
                series,
                campaign,
                spec,
                wave,
                closedAt));
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var session in sessions)
        {
            var scoreResult = await scoreMaterializer.MaterializeAsync(
                tenantId,
                session.Id,
                requireScoringRule: true,
                cancellationToken);

            if (scoreResult.IsFailure)
            {
                throw new InvalidOperationException(scoreResult.Error.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return series.Id;
    }

    private async Task EnsureSeriesResponseExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var existingArtifacts = await db.ExportArtifacts
            .Where(artifact =>
                artifact.TenantId == tenantId &&
                artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResponseCsvCodebook)
            .ToListAsync(cancellationToken);

        if (existingArtifacts.Count == 1 && IsRowLevelResponseExport(existingArtifacts[0]))
        {
            return;
        }

        db.ExportArtifacts.RemoveRange(existingArtifacts);
        db.ExportArtifacts.Add(await CreateSeriesResponseExportAsync(
            tenantId,
            campaignSeriesId,
            spec,
            completedAt,
            cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSeriesResultsMatrixExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var existingArtifacts = await db.ExportArtifacts
            .Where(artifact =>
                artifact.TenantId == tenantId &&
                artifact.TargetKind == ExportArtifactTargetKinds.CampaignSeries &&
                artifact.CampaignSeriesId == campaignSeriesId &&
                artifact.ArtifactType == ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook)
            .ToListAsync(cancellationToken);

        if (existingArtifacts.Count == 1 && IsSampleResultsMatrixExport(existingArtifacts[0]))
        {
            return;
        }

        db.ExportArtifacts.RemoveRange(existingArtifacts);
        db.ExportArtifacts.Add(await CreateSeriesResultsMatrixExportAsync(
            tenantId,
            campaignSeriesId,
            spec,
            completedAt,
            cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildSampleScoringDocument(SampleStudySpec spec)
    {
        var questionCodes = spec.Questions
            .Select(question => question.Code)
            .ToHashSet(StringComparer.Ordinal);
        var inputs = new List<object>();
        var nodes = new List<Dictionary<string, object?>>();

        foreach (var score in spec.Scores)
        {
            if (IsQuestionBackedSampleScore(score, questionCodes))
            {
                var inputId = $"{score.Code}_items";
                var answersNodeId = $"{score.Code}_answers";
                inputs.Add(new
                {
                    id = inputId,
                    kind = "answers",
                    items = score.QuestionCodes.ToArray()
                });
                nodes.Add(new Dictionary<string, object?>
                {
                    ["id"] = answersNodeId,
                    ["op"] = "select_answers",
                    ["input"] = inputId
                });

                var aggregateInputId = answersNodeId;
                if (IsNormalizedQuestionCalculation(score.Calculation))
                {
                    var normalizedNodeId = $"{score.Code}_normalized_answers";
                    nodes.Add(new Dictionary<string, object?>
                    {
                        ["id"] = normalizedNodeId,
                        ["op"] = "normalize_0_100",
                        ["input"] = answersNodeId,
                        ["source_scales"] = score.QuestionCodes.ToDictionary(
                            code => code,
                            _ => new { min = 1, max = 7 })
                    });
                    aggregateInputId = normalizedNodeId;
                }

                var aggregateNode = new Dictionary<string, object?>
                {
                    ["id"] = score.Code,
                    ["op"] = AggregateOperationForSampleCalculation(score.Calculation),
                    ["input"] = aggregateInputId
                };
                if (UsesSampleWeights(score.Calculation) && score.Weights is not null)
                {
                    aggregateNode["weights"] = score.Weights;
                }

                nodes.Add(aggregateNode);

                if (score.ScoreRangeMin != 0 || score.ScoreRangeMax != 100)
                {
                    nodes.Add(new Dictionary<string, object?>
                    {
                        ["id"] = $"{score.Code}_0_100",
                        ["op"] = "normalize_0_100",
                        ["input"] = score.Code,
                        ["source_min"] = score.ScoreRangeMin,
                        ["source_max"] = score.ScoreRangeMax
                    });
                }

                continue;
            }

            if (score.Calculation == "composite_weighted_mean" ||
                score.Calculation == "composite_weighted_sum")
            {
                var combineNode = new Dictionary<string, object?>
                {
                    ["id"] = score.Code,
                    ["op"] = "combine",
                    ["inputs"] = score.QuestionCodes.ToArray(),
                    ["method"] = score.Calculation == "composite_weighted_sum"
                        ? "weighted_sum"
                        : "weighted_mean"
                };
                if (score.Weights is not null)
                {
                    combineNode["weights"] = score.Weights;
                }

                nodes.Add(combineNode);
                continue;
            }

            if (score.Calculation == "difference" && score.QuestionCodes.Count == 2)
            {
                nodes.Add(new Dictionary<string, object?>
                {
                    ["id"] = score.Code,
                    ["op"] = "difference",
                    ["left"] = score.QuestionCodes[0],
                    ["right"] = score.QuestionCodes[1]
                });
                continue;
            }

            throw new InvalidOperationException($"Sample score '{score.Code}' has unsupported calculation '{score.Calculation}'.");
        }

        return JsonSerializer.Serialize(new
        {
            schema_version = "1.0.0",
            engine_min_version = "1.0.0",
            rule_id = $"sample.{spec.Key}",
            rule_version = "1.0.0",
            scale_defaults = new
            {
                agreement_1_7 = new
                {
                    min = 1,
                    max = 7
                }
            },
            inputs,
            nodes,
            outputs = spec.Scores.Select(score => new
            {
                code = score.Code,
                node = score.Code
            }).ToArray(),
            missing_data = new
            {
                defaults = new
                {
                    strategy = "require_all"
                }
            }
        }, JsonOptions);
    }

    private static bool IsQuestionBackedSampleScore(
        SampleScoreSpec score,
        IReadOnlySet<string> questionCodes)
    {
        return score.QuestionCodes.Count > 0 &&
            score.QuestionCodes.All(code => questionCodes.Contains(code));
    }

    private static bool IsNormalizedQuestionCalculation(string calculation)
    {
        return calculation is "normalized_mean_0_100" or
            "normalized_sum_0_100" or
            "normalized_weighted_mean_0_100" or
            "normalized_weighted_sum_0_100";
    }

    private static bool UsesSampleWeights(string calculation)
    {
        return calculation is "weighted_mean" or
            "weighted_sum" or
            "normalized_weighted_mean_0_100" or
            "normalized_weighted_sum_0_100";
    }

    private static string AggregateOperationForSampleCalculation(string calculation)
    {
        return calculation switch
        {
            "mean" or "normalized_mean_0_100" => "mean",
            "sum" or "normalized_sum_0_100" => "sum",
            "weighted_mean" or "normalized_weighted_mean_0_100" => "weighted_mean",
            "weighted_sum" or "normalized_weighted_sum_0_100" => "weighted_sum",
            _ => throw new InvalidOperationException($"Unsupported sample aggregate calculation '{calculation}'.")
        };
    }

    private static string ScoreCalculationLabel(string calculation)
    {
        return calculation switch
        {
            "mean" => "Mean score",
            "sum" => "Sum score",
            "weighted_mean" => "Weighted average",
            "weighted_sum" => "Weighted sum",
            "normalized_mean_0_100" => "Normalized 0-100 average",
            "normalized_sum_0_100" => "Normalized 0-100 sum",
            "normalized_weighted_mean_0_100" => "Normalized 0-100 weighted average",
            "normalized_weighted_sum_0_100" => "Normalized 0-100 weighted sum",
            "composite_weighted_mean" => "Composite weighted average",
            "composite_weighted_sum" => "Composite weighted sum",
            "difference" => "Difference score",
            "count_valid" => "Valid answer count",
            _ => calculation
        };
    }

    private static IReadOnlyList<ParticipantCode> CreateParticipantCodes(
        Guid tenantId,
        Guid campaignSeriesId,
        int respondentCount,
        DateTimeOffset firstSeenAt)
    {
        return Enumerable.Range(0, respondentCount)
            .Select(index => new ParticipantCode(
                PlatformIds.NewId(),
                tenantId,
                campaignSeriesId,
                HashBytes($"{tenantId:N}:{campaignSeriesId:N}:sample-participant:{index + 1:0000}"),
                ParticipantCode.MinimumArgon2MemoryKiB,
                ParticipantCode.MinimumArgon2Iterations,
                ParticipantCode.MinimumArgon2Parallelism,
                ParticipantCode.MinimumArgon2OutputBytes,
                firstSeenAt))
            .ToArray();
    }

    private static SampleDirectory CreateSampleDirectory(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec)
    {
        var groups = spec.Groups
            .Select(group => new SampleDirectoryGroup(
                group,
                new SubjectGroup(
                    PlatformIds.NewId(),
                    tenantId,
                    group.Type,
                    group.Name,
                    attributes: JsonSerializer.Serialize(new
                    {
                        sample_study = true,
                        campaign_series_id = campaignSeriesId,
                        scenario = spec.SampleScenario
                    }, JsonOptions))))
            .ToArray();
        var groupByName = groups.ToDictionary(group => group.Spec.Name, StringComparer.Ordinal);
        var subjects = new List<Subject>();
        var memberships = new List<SubjectMembership>();
        var respondents = new List<SampleRespondentProfile>();
        var respondentIndex = 0;

        foreach (var group in spec.Groups)
        {
            var directoryGroup = groupByName[group.Name];
            for (var index = 0; index < group.RespondentCount; index++)
            {
                respondentIndex++;
                var subjectId = PlatformIds.NewId();
                var displayName = respondentIndex <= (spec.SubjectDisplayNames?.Count ?? 0)
                    ? spec.SubjectDisplayNames![respondentIndex - 1]
                    : $"Sample respondent {respondentIndex:0000}";
                var subject = new Subject(
                    subjectId,
                    tenantId,
                    externalId: $"sample-{spec.Key}-{respondentIndex:0000}",
                    displayName: displayName,
                    attributes: JsonSerializer.Serialize(new
                    {
                        sample_study = true,
                        campaign_series_id = campaignSeriesId,
                        group = group.Name
                    }, JsonOptions));

                subjects.Add(subject);
                memberships.Add(new SubjectMembership(subjectId, directoryGroup.Entity.Id, SubjectGroupRoles.Member));
                respondents.Add(new SampleRespondentProfile(
                    subjectId,
                    group.Type,
                    group.Name,
                    group.MeanOffset));
            }
        }

        return new SampleDirectory(
            subjects,
            groups.Select(group => group.Entity).ToArray(),
            memberships,
            respondents);
    }

    private static SampleAssignmentPlan CreateSampleAssignmentPlan(
        SampleStudySpec spec,
        SampleDirectory directory,
        int respondentIndex)
    {
        var respondent = directory.Respondents[respondentIndex];
        if (spec.AssignmentScenario != SampleAssignmentScenarios.TargetAware360)
        {
            return new SampleAssignmentPlan(
                respondent,
                respondent,
                "respondent");
        }

        var targetPool = directory.Respondents
            .GroupBy(profile => profile.GroupName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        if (targetPool.Length == 0)
        {
            return new SampleAssignmentPlan(
                respondent,
                respondent,
                "respondent");
        }

        var targetIndex = respondentIndex % targetPool.Length;
        var target = targetPool[targetIndex];
        if (target.SubjectId == respondent.SubjectId && targetPool.Length > 1)
        {
            target = targetPool[(targetIndex + 1) % targetPool.Length];
        }

        var role = string.Equals(respondent.GroupName, target.GroupName, StringComparison.Ordinal)
            ? "direct_report"
            : "peer";

        return new SampleAssignmentPlan(
            respondent,
            target,
            role);
    }

    private static int CreateAnswerValue(
        double targetMean,
        int respondentIndex,
        int questionIndex,
        int waveIndex,
        double groupOffset)
    {
        var offset = ((respondentIndex * 7) + (questionIndex * 3) + waveIndex) % 7 - 3;
        var value = (int)Math.Round(targetMean + groupOffset + (offset * 0.22), MidpointRounding.AwayFromZero);

        return Math.Clamp(value, 1, 7);
    }

    private static ExportArtifact CreateCampaignReportExport(
        Guid tenantId,
        CampaignSeries series,
        Campaign campaign,
        SampleStudySpec spec,
        SampleWaveSpec wave,
        DateTimeOffset completedAt)
    {
        var content = string.Join(
            "\n",
            "study,wave,responses,target_outputs,status",
            CsvRow(series.Name, wave.Name, spec.RespondentCount.ToString(), FormatWaveTargets(wave), "closed"));

        return CreateInlineExport(
            tenantId,
            ExportArtifactTargetKinds.Campaign,
            campaign.Id,
            series.Id,
            ExportArtifactTypes.ReportProofCsvCodebook,
            $"{spec.Key}-{Slugify(wave.Name)}-report-proof.csv",
            rowCount: 1,
            content,
            metadataJson: JsonSerializer.Serialize(new
            {
                sampleStudy = true,
                study = series.Name,
                wave = wave.Name,
                purpose = "sample_report_review"
            }, JsonOptions),
            codebookJson: JsonSerializer.Serialize(new
            {
                columns = new[] { "study", "wave", "responses", "target_outputs", "status" }
            }, JsonOptions),
            completedAt);
    }

    private async Task<ExportArtifact> CreateSeriesResponseExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var series = await db.CampaignSeries
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == campaignSeriesId, cancellationToken);
        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.CampaignSeriesId == campaignSeriesId)
            .OrderBy(campaign => campaign.StartAt)
            .ThenBy(campaign => campaign.Name)
            .ThenBy(campaign => campaign.Id)
            .Select(campaign => new SampleCampaignExportRow(
                campaign.Id,
                campaign.TemplateVersionId,
                campaign.Name,
                campaign.StartAt))
            .ToListAsync(cancellationToken);
        var templateVersionId = campaigns
            .Select(campaign => campaign.TemplateVersionId)
            .FirstOrDefault();
        var questions = templateVersionId == Guid.Empty
            ? []
            : await db.TemplateQuestions
                .AsNoTracking()
                .Where(question => question.TemplateVersionId == templateVersionId)
                .OrderBy(question => question.Ordinal)
                .ThenBy(question => question.Code)
                .Select(question => new SampleQuestionExportRow(
                    question.Id,
                    question.Ordinal,
                    question.Code,
                    question.TextDefault))
                .ToListAsync(cancellationToken);
        var sessions = await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                join campaign in db.Campaigns.AsNoTracking()
                    on assignment.CampaignId equals campaign.Id
                where campaign.CampaignSeriesId == campaignSeriesId &&
                    session.SubmittedAt.HasValue
                select new SampleSessionExportRow(
                    session.Id,
                    campaign.Id,
                    campaign.Name,
                    campaign.StartAt,
                    session.SubmittedAt!.Value,
                    session.ParticipantCodeId,
                    assignment.TargetSubjectId ?? assignment.RespondentSubjectId))
            .ToListAsync(cancellationToken);
        var orderedSessions = sessions
            .OrderBy(session => session.WaveStartAt ?? DateTimeOffset.MinValue)
            .ThenBy(session => session.WaveName, StringComparer.Ordinal)
            .ThenBy(session => session.SubmittedAt)
            .ThenBy(session => session.Id)
            .ToList();
        var sessionIds = orderedSessions.Select(session => session.Id).ToArray();
        var answers = sessionIds.Length == 0
            ? []
            : await db.Answers
                .AsNoTracking()
                .Where(answer => sessionIds.Contains(answer.SessionId))
                .Select(answer => new SampleAnswerExportRow(
                    answer.SessionId,
                    answer.QuestionId,
                    answer.Value))
                .ToListAsync(cancellationToken);
        var scores = sessionIds.Length == 0
            ? []
            : await db.Scores
                .AsNoTracking()
                .Where(score => sessionIds.Contains(score.ResponseSessionId))
                .Select(score => new SampleScoreExportRow(
                    score.ResponseSessionId,
                    score.DimensionCode,
                    score.Value,
                    score.ComputedAt))
                .ToListAsync(cancellationToken);
        var answersBySessionAndQuestion = answers.ToDictionary(
            answer => (answer.SessionId, answer.QuestionId),
            answer => answer);
        var scoreBySessionAndDimension = scores
            .GroupBy(score => (score.SessionId, score.DimensionCode))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(score => score.ComputedAt)
                    .ThenByDescending(score => score.Value)
                    .First());
        var trajectoryKeys = orderedSessions
            .Where(session => session.ParticipantCodeId.HasValue)
            .Select(session => session.ParticipantCodeId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .Select((id, index) => new { Id = id, Key = $"T{index + 1:0000}" })
            .ToDictionary(item => item.Id, item => item.Key);
        var lines = new List<string>
        {
            "study,wave,response_key,trajectory_key,submitted_at,question_code,question_text,answer_value,score_output_code,score_value"
        };

        for (var sessionIndex = 0; sessionIndex < orderedSessions.Count; sessionIndex++)
        {
            var session = orderedSessions[sessionIndex];
            var responseKey = $"R{sessionIndex + 1:0000}";
            var trajectoryKey = session.ParticipantCodeId.HasValue &&
                trajectoryKeys.TryGetValue(session.ParticipantCodeId.Value, out var key)
                    ? key
                    : string.Empty;
            foreach (var question in questions)
            {
                var questionSpec = spec.Questions.SingleOrDefault(item =>
                    string.Equals(item.Code, question.Code, StringComparison.OrdinalIgnoreCase));
                var scoreOutputCode = questionSpec?.ScoreCode ?? string.Empty;
                var scoreValue = scoreBySessionAndDimension.TryGetValue((session.Id, scoreOutputCode), out var score)
                    ? score.Value.ToString("0.####", CultureInfo.InvariantCulture)
                    : string.Empty;
                answersBySessionAndQuestion.TryGetValue((session.Id, question.Id), out var answer);
                lines.Add(CsvRow(
                    series.Name,
                    session.WaveName,
                    responseKey,
                    trajectoryKey,
                    session.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    question.Code,
                    question.Text,
                    NormalizeAnswerValue(answer?.Value),
                    scoreOutputCode,
                    scoreValue));
            }
        }

        var content = string.Join("\n", lines);
        var rowCount = Math.Max(0, lines.Count - 1);

        return CreateInlineExport(
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: series.Id,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            $"{spec.Key}-responses.csv",
            rowCount,
            content,
            metadataJson: JsonSerializer.Serialize(new
            {
                sampleStudy = true,
                study = series.Name,
                purpose = "sample_response_rows",
                responses = orderedSessions.Count,
                rows = rowCount,
                identityMode = spec.ResponseIdentityMode
            }, JsonOptions),
            codebookJson: JsonSerializer.Serialize(new
            {
                columns = new[]
                {
                    new { name = "study", description = "Sample study name." },
                    new { name = "wave", description = "Collection round name." },
                    new { name = "response_key", description = "Local synthetic response key for this export." },
                    new { name = "trajectory_key", description = "Local synthetic repeated-response key when anonymous longitudinal linking is available." },
                    new { name = "submitted_at", description = "Response submission timestamp in UTC." },
                    new { name = "question_code", description = "Question variable code." },
                    new { name = "question_text", description = "Question wording shown to respondents." },
                    new { name = "answer_value", description = "Submitted answer value." },
                    new { name = "score_output_code", description = "Result output linked to this question row." },
                    new { name = "score_value", description = "Synthetic score value for the linked result output." }
                }
            }, JsonOptions),
            completedAt);
    }

    private async Task<ExportArtifact> CreateSeriesResultsMatrixExportAsync(
        Guid tenantId,
        Guid campaignSeriesId,
        SampleStudySpec spec,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var series = await db.CampaignSeries
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == campaignSeriesId, cancellationToken);
        var campaigns = await db.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.CampaignSeriesId == campaignSeriesId)
            .OrderBy(campaign => campaign.StartAt)
            .ThenBy(campaign => campaign.Name)
            .ThenBy(campaign => campaign.Id)
            .Select(campaign => new SampleCampaignResultsMatrixRow(
                campaign.Id,
                campaign.Name,
                campaign.Status,
                campaign.StartAt,
                campaign.ClosedAt))
            .ToListAsync(cancellationToken);
        var sessions = await (
                from session in db.ResponseSessions.AsNoTracking()
                join assignment in db.Assignments.AsNoTracking()
                    on session.AssignmentId equals assignment.Id
                join campaign in db.Campaigns.AsNoTracking()
                    on assignment.CampaignId equals campaign.Id
                where campaign.CampaignSeriesId == campaignSeriesId &&
                    session.SubmittedAt.HasValue
                select new SampleSessionExportRow(
                    session.Id,
                    campaign.Id,
                    campaign.Name,
                    campaign.StartAt,
                    session.SubmittedAt!.Value,
                    session.ParticipantCodeId,
                    assignment.TargetSubjectId ?? assignment.RespondentSubjectId))
            .ToListAsync(cancellationToken);
        var sessionIds = sessions.Select(session => session.Id).ToArray();
        var scores = sessionIds.Length == 0
            ? []
            : await db.Scores
                .AsNoTracking()
                .Where(score => sessionIds.Contains(score.ResponseSessionId))
                .Select(score => new SampleScoreExportRow(
                    score.ResponseSessionId,
                    score.DimensionCode,
                    score.Value,
                    score.ComputedAt))
                .ToListAsync(cancellationToken);
        var scoreBySessionAndDimension = scores
            .GroupBy(score => (score.SessionId, score.DimensionCode))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(score => score.ComputedAt)
                    .ThenByDescending(score => score.Value)
                    .First());
        var sessionsByCampaign = sessions
            .GroupBy(session => session.CampaignId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var selectedCampaign = campaigns.LastOrDefault();
        var lines = new List<string>
        {
            string.Join(",", SampleResultsMatrixColumns)
        };

        if (selectedCampaign is not null &&
            sessionsByCampaign.TryGetValue(selectedCampaign.Id, out var selectedCampaignSessions))
        {
            foreach (var scoreSpec in spec.Scores)
            {
                var selectedValues = GetScoreValues(
                    selectedCampaignSessions,
                    scoreSpec.Code,
                    scoreBySessionAndDimension);
                if (selectedValues.Length == 0)
                {
                    continue;
                }

                AddSampleResultsMatrixRow(
                    lines,
                    series,
                    selectedCampaign,
                    selectedCampaign,
                    "overall",
                    selectedCampaign.Name,
                    string.Empty,
                    string.Empty,
                    scoreSpec.Code,
                    scoreSpec,
                    selectedValues,
                    visible: selectedValues.Length >= SampleDisclosureKMin,
                    string.Empty,
                    string.Empty,
                    "selected",
                    selectedValues.Length >= SampleDisclosureKMin ? string.Empty : "insufficient_responses");
            }

            var subjectIds = selectedCampaignSessions
                .Select(session => session.SubjectId)
                .OfType<Guid>()
                .Distinct()
                .ToArray();
            var memberships = subjectIds.Length == 0
                ? []
                : await (
                        from membership in db.SubjectMemberships.AsNoTracking()
                        join subjectGroup in db.SubjectGroups.AsNoTracking()
                            on membership.GroupId equals subjectGroup.Id
                        where subjectIds.Contains(membership.SubjectId) &&
                            subjectGroup.TenantId == tenantId &&
                            subjectGroup.DeletedAt == null
                        select new SampleSubjectGroupMembershipExportRow(
                            membership.SubjectId,
                            subjectGroup.Type,
                            subjectGroup.Name))
                    .ToListAsync(cancellationToken);
            var membershipsBySubject = memberships
                .GroupBy(membership => membership.SubjectId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var scoreSpec in spec.Scores)
            {
                var groupRows = selectedCampaignSessions
                    .SelectMany(session =>
                    {
                        if (!session.SubjectId.HasValue ||
                            !membershipsBySubject.TryGetValue(session.SubjectId.Value, out var subjectMemberships) ||
                            !scoreBySessionAndDimension.TryGetValue((session.Id, scoreSpec.Code), out var score))
                        {
                            return Enumerable.Empty<SampleGroupScoreExportRow>();
                        }

                        return subjectMemberships
                            .GroupBy(membership => new { membership.GroupType, membership.GroupName })
                            .Select(group => new SampleGroupScoreExportRow(
                                group.Key.GroupType,
                                group.Key.GroupName,
                                score.Value));
                    })
                    .GroupBy(row => new { row.GroupType, row.GroupName })
                    .OrderBy(group => group.Key.GroupType, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(group => group.Key.GroupName, StringComparer.OrdinalIgnoreCase);

                foreach (var group in groupRows)
                {
                    var values = group.Select(row => row.Value).ToArray();
                    AddSampleResultsMatrixRow(
                        lines,
                        series,
                        selectedCampaign,
                        selectedCampaign,
                        "group",
                        group.Key.GroupName,
                        group.Key.GroupType,
                        group.Key.GroupName,
                        scoreSpec.Code,
                        scoreSpec,
                        values,
                        visible: values.Length >= SampleDisclosureKMin,
                        string.Empty,
                        string.Empty,
                        "selected",
                        values.Length >= SampleDisclosureKMin ? string.Empty : "insufficient_responses");
                }
            }
        }

        foreach (var scoreSpec in spec.Scores)
        {
            decimal? firstMean = null;
            decimal? previousMean = null;

            foreach (var campaign in campaigns)
            {
                var values = sessionsByCampaign.TryGetValue(campaign.Id, out var campaignSessions)
                    ? GetScoreValues(campaignSessions, scoreSpec.Code, scoreBySessionAndDimension)
                    : [];
                if (values.Length == 0)
                {
                    continue;
                }

                var visible = values.Length >= SampleDisclosureKMin;
                var mean = visible ? CalculateSampleMean(values) : (decimal?)null;
                var comparisonState = firstMean.HasValue ? "compared" : "baseline";
                var deltaFromPrevious = mean.HasValue && previousMean.HasValue
                    ? FormatSampleDecimal(mean.Value - previousMean.Value)
                    : string.Empty;
                var deltaFromFirst = mean.HasValue && firstMean.HasValue
                    ? FormatSampleDecimal(mean.Value - firstMean.Value)
                    : mean.HasValue ? "0" : string.Empty;
                firstMean ??= mean;
                previousMean = mean ?? previousMean;

                AddSampleResultsMatrixRow(
                    lines,
                    series,
                    selectedCampaign,
                    campaign,
                    "wave",
                    campaign.Name,
                    string.Empty,
                    string.Empty,
                    scoreSpec.Code,
                    scoreSpec,
                    values,
                    visible,
                    deltaFromPrevious,
                    deltaFromFirst,
                    comparisonState,
                    visible ? string.Empty : "insufficient_responses");
            }
        }

        var content = string.Join("\n", lines);
        var rowCount = Math.Max(0, lines.Count - 1);

        return CreateInlineExport(
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: series.Id,
            ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook,
            $"{spec.Key}-results-matrix.csv",
            rowCount,
            content,
            metadataJson: JsonSerializer.Serialize(new
            {
                sampleStudy = true,
                study = series.Name,
                purpose = "sample_results_matrix",
                rows = rowCount,
                identityMode = spec.ResponseIdentityMode
            }, JsonOptions),
            codebookJson: JsonSerializer.Serialize(new
            {
                artifactType = ExportArtifactTypes.CampaignSeriesResultsMatrixCsvCodebook,
                rowCount,
                rowShape = "aggregate rows by selected output, collection round, and directory group when available",
                resultScopes = new[] { "overall", "wave", "group" },
                columns = SampleResultsMatrixColumns.Select(column => new
                {
                    name = column,
                    description = "Sample Results matrix column."
                })
            }, JsonOptions),
            completedAt);
    }

    private static decimal[] GetScoreValues(
        IReadOnlyList<SampleSessionExportRow> sessions,
        string dimensionCode,
        IReadOnlyDictionary<(Guid SessionId, string DimensionCode), SampleScoreExportRow> scoresBySessionAndDimension)
    {
        return sessions
            .Where(session => scoresBySessionAndDimension.ContainsKey((session.Id, dimensionCode)))
            .Select(session => scoresBySessionAndDimension[(session.Id, dimensionCode)].Value)
            .ToArray();
    }

    private static void AddSampleResultsMatrixRow(
        List<string> lines,
        CampaignSeries series,
        SampleCampaignResultsMatrixRow? selectedCampaign,
        SampleCampaignResultsMatrixRow campaign,
        string resultScope,
        string resultScopeLabel,
        string groupType,
        string groupName,
        string dimensionCode,
        SampleScoreSpec scoreSpec,
        IReadOnlyCollection<decimal> values,
        bool visible,
        string deltaFromPreviousMean,
        string deltaFromFirstMean,
        string comparisonState,
        string suppressionReason)
    {
        var valueArray = values.ToArray();
        var showValues = visible && valueArray.Length > 0;

        lines.Add(CsvRow(
            resultScope,
            resultScopeLabel,
            series.Id.ToString(),
            selectedCampaign?.Id.ToString() ?? string.Empty,
            selectedCampaign?.Name ?? string.Empty,
            campaign.Id.ToString(),
            campaign.Name,
            campaign.Status,
            campaign.ClosedAt.HasValue ? "closed_wave" : "preliminary_live",
            campaign.ClosedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            groupType,
            groupName,
            dimensionCode,
            scoreSpec.Label,
            scoreSpec.Calculation,
            ScoreCalculationLabel(scoreSpec.Calculation),
            FormatSampleDecimal(scoreSpec.ScoreRangeMin),
            FormatSampleDecimal(scoreSpec.ScoreRangeMax),
            showValues ? "visible" : "suppressed",
            showValues ? valueArray.Length.ToString(CultureInfo.InvariantCulture) : string.Empty,
            showValues ? valueArray.Length.ToString(CultureInfo.InvariantCulture) : string.Empty,
            showValues ? valueArray.Length.ToString(CultureInfo.InvariantCulture) : string.Empty,
            showValues ? valueArray.Length.ToString(CultureInfo.InvariantCulture) : string.Empty,
            showValues ? "complete" : string.Empty,
            showValues ? FormatSampleDecimal(CalculateSampleMean(valueArray)) : string.Empty,
            showValues ? FormatSampleDecimal(CalculateSampleMedian(valueArray)) : string.Empty,
            showValues ? FormatSampleDecimal(CalculateSampleStandardDeviation(valueArray)) : string.Empty,
            showValues ? FormatSampleDecimal(valueArray.Min()) : string.Empty,
            showValues ? FormatSampleDecimal(valueArray.Max()) : string.Empty,
            showValues ? deltaFromPreviousMean : string.Empty,
            showValues ? deltaFromFirstMean : string.Empty,
            comparisonState,
            showValues ? string.Empty : suppressionReason));
    }

    private static bool IsRowLevelResponseExport(ExportArtifact artifact)
    {
        return artifact.Content?.StartsWith(
                "study,wave,response_key,trajectory_key,submitted_at,question_code,question_text,answer_value,score_output_code,score_value",
                StringComparison.Ordinal) == true &&
            artifact.MetadataJson.Contains("sample_response_rows", StringComparison.Ordinal);
    }

    private static bool IsSampleResultsMatrixExport(ExportArtifact artifact)
    {
        return artifact.Content?.StartsWith("result_scope,result_scope_label,campaign_series_id", StringComparison.Ordinal) == true &&
            artifact.Content.Contains("score_display_label", StringComparison.Ordinal) &&
            artifact.MetadataJson.Contains("sample_results_matrix", StringComparison.Ordinal);
    }

    private static ExportArtifact CreateInlineExport(
        Guid tenantId,
        string targetKind,
        Guid? campaignId,
        Guid? campaignSeriesId,
        string artifactType,
        string fileName,
        int rowCount,
        string content,
        string metadataJson,
        string codebookJson,
        DateTimeOffset completedAt)
    {
        return new ExportArtifact(
            PlatformIds.NewId(),
            tenantId,
            targetKind,
            campaignId,
            campaignSeriesId,
            artifactType,
            ExportArtifactStatuses.Succeeded,
            ExportArtifactFormats.CsvCodebook,
            fileName,
            "text/csv",
            rowCount,
            Encoding.UTF8.GetByteCount(content),
            Sha256Hex(content),
            metadataJson,
            content,
            codebookJson,
            createdAt: completedAt,
            completedAt: completedAt);
    }

    private static string CsvRow(params string[] values)
    {
        return string.Join(",", values.Select(CsvCell));
    }

    private static string CsvCell(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string NormalizeAnswerValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.String => document.RootElement.GetString() ?? string.Empty,
                JsonValueKind.Number => document.RootElement.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => document.RootElement.GetRawText()
            };
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static decimal CalculateSampleMean(IReadOnlyCollection<decimal> values)
    {
        return Math.Round(values.Average(), 4, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateSampleMedian(IReadOnlyCollection<decimal> values)
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

    private static decimal CalculateSampleStandardDeviation(IReadOnlyCollection<decimal> values)
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

    private static string FormatSampleDecimal(decimal value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string FormatWaveTargets(SampleWaveSpec wave)
    {
        return string.Join(
            "; ",
            wave.TargetMeans
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value.ToString("0.0", CultureInfo.InvariantCulture)}"));
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) ? character : '-');
        }

        return builder.ToString().Trim('-');
    }

    private static byte[] HashBytes(string value)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(value));
    }

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(HashBytes(value)).ToLowerInvariant();
    }
}

internal static class SampleAssignmentScenarios
{
    public const string Self = "self";
    public const string TargetAware360 = "target_aware_360";
}

internal static class SampleStudySpecs
{
    public static IReadOnlyList<SampleStudySpec> All { get; } =
    [
        new(
            "workload-recovery",
            "Workload recovery after staffing change",
            "Read-only sample showing four closed collection rounds after a workload intervention.",
            "Workload and recovery",
            CampaignSeriesSampleScenarios.Longitudinal,
            ResponseIdentityModes.AnonymousLongitudinal,
            CampaignSeriesStudyDesignTypes.RepeatedLinkedChange,
            [
                new(SubjectGroupTypes.Team, "Operations", 9, -0.25),
                new(SubjectGroupTypes.Team, "Field service", 7, -0.45),
                new(SubjectGroupTypes.Team, "Support desk", 8, 0.15),
                new(SubjectGroupTypes.Team, "Night shift", 4, -0.7)
            ],
            [
                new("workload_manageability", "Workload manageability", ["q01", "q02"]),
                new("recovery_capacity", "Recovery capacity", ["q03", "q04"]),
                new("role_clarity", "Role clarity", ["q05", "q06"]),
                new("support_access", "Support access", ["q07", "q08"])
            ],
            [
                new("q01", "workload_manageability", "I can complete priority work within the available time."),
                new("q02", "workload_manageability", "Unexpected work can be handled without constant overtime."),
                new("q03", "recovery_capacity", "I can recover enough between demanding work periods."),
                new("q04", "recovery_capacity", "Breaks and time away from work are realistic during busy weeks."),
                new("q05", "role_clarity", "I know which tasks matter most when everything feels urgent."),
                new("q06", "role_clarity", "Responsibilities are clear enough to avoid duplicated effort."),
                new("q07", "support_access", "My team has practical support when workload increases."),
                new("q08", "support_access", "It is easy to ask for help before workload becomes unmanageable.")
            ],
            [
                new("Baseline before staffing change", new Dictionary<string, double>
                {
                    ["workload_manageability"] = 3.8,
                    ["recovery_capacity"] = 3.5,
                    ["role_clarity"] = 4.1,
                    ["support_access"] = 3.9
                }),
                new("Peak workload week", new Dictionary<string, double>
                {
                    ["workload_manageability"] = 3.2,
                    ["recovery_capacity"] = 3.1,
                    ["role_clarity"] = 3.9,
                    ["support_access"] = 3.5
                }),
                new("After staffing change", new Dictionary<string, double>
                {
                    ["workload_manageability"] = 4.6,
                    ["recovery_capacity"] = 4.2,
                    ["role_clarity"] = 4.7,
                    ["support_access"] = 4.5
                }),
                new("Follow-up review", new Dictionary<string, double>
                {
                    ["workload_manageability"] = 5.2,
                    ["recovery_capacity"] = 4.9,
                    ["role_clarity"] = 5.1,
                    ["support_access"] = 5.0
                })
            ]),
        new(
            "ergonomics-risk",
            "Ergonomics risk and workstation fit",
            "Read-only sample showing workstation changes, directory groups, and hidden small-group cells.",
            "Ergonomics conditions",
            CampaignSeriesSampleScenarios.Completed,
            ResponseIdentityModes.Identified,
            CampaignSeriesStudyDesignTypes.RepeatedGroupTrend,
            [
                new(SubjectGroupTypes.Department, "Assembly line", 9, -0.55),
                new(SubjectGroupTypes.Department, "Office workstations", 7, 0.2),
                new(SubjectGroupTypes.Department, "Logistics", 6, -0.15),
                new(SubjectGroupTypes.Department, "Prototype lab", 4, -0.8)
            ],
            [
                new("posture_support", "Posture support", ["q01", "q02"]),
                new("equipment_fit", "Equipment fit", ["q03", "q04"]),
                new("repetitive_load_control", "Repetitive load control", ["q05", "q06"]),
                new("reporting_confidence", "Reporting confidence", ["q07", "q08"])
            ],
            [
                new("q01", "posture_support", "My workstation supports a comfortable working posture."),
                new("q02", "posture_support", "I can adjust my working position during the day."),
                new("q03", "equipment_fit", "Tools and equipment are adjusted to the task."),
                new("q04", "equipment_fit", "Shared equipment is available in a condition that supports safe work."),
                new("q05", "repetitive_load_control", "Repetitive or awkward movements are kept under control."),
                new("q06", "repetitive_load_control", "Tasks are rotated or redesigned before discomfort builds up."),
                new("q07", "reporting_confidence", "I know how to report and resolve ergonomics concerns."),
                new("q08", "reporting_confidence", "Reported ergonomics concerns receive a timely response.")
            ],
            [
                new("Initial workstation review", new Dictionary<string, double>
                {
                    ["posture_support"] = 4.0,
                    ["equipment_fit"] = 3.7,
                    ["repetitive_load_control"] = 3.4,
                    ["reporting_confidence"] = 4.1
                }),
                new("After equipment adjustment", new Dictionary<string, double>
                {
                    ["posture_support"] = 4.9,
                    ["equipment_fit"] = 5.1,
                    ["repetitive_load_control"] = 4.2,
                    ["reporting_confidence"] = 4.6
                }),
                new("Follow-up workstation review", new Dictionary<string, double>
                {
                    ["posture_support"] = 5.5,
                    ["equipment_fit"] = 5.6,
                    ["repetitive_load_control"] = 4.9,
                    ["reporting_confidence"] = 5.1
                })
            ]),
        new(
            "student-wellbeing",
            "Student wellbeing and assessment load",
            "Read-only sample showing repeated anonymous participation across a study term.",
            "Study load and recovery",
            CampaignSeriesSampleScenarios.Longitudinal,
            ResponseIdentityModes.AnonymousLongitudinal,
            CampaignSeriesStudyDesignTypes.RepeatedLinkedChange,
            [
                new(SubjectGroupTypes.Cohort, "First-year students", 10, -0.2),
                new(SubjectGroupTypes.Cohort, "Final-year students", 8, -0.45),
                new(SubjectGroupTypes.Cohort, "Part-time students", 7, 0.1),
                new(SubjectGroupTypes.Cohort, "Small seminar group", 4, -0.65)
            ],
            [
                new("study_load_manageability", "Study load manageability", ["q01", "q02"]),
                new("support_access", "Support access", ["q03", "q04"]),
                new("assessment_clarity", "Assessment clarity", ["q05", "q06"]),
                new("recovery_time", "Recovery time", ["q07", "q08"])
            ],
            [
                new("q01", "study_load_manageability", "I can keep up with the expected study workload."),
                new("q02", "study_load_manageability", "Weekly study demands feel realistic alongside other responsibilities."),
                new("q03", "support_access", "I know where to get support when study pressure rises."),
                new("q04", "support_access", "Support is available early enough to prevent problems from escalating."),
                new("q05", "assessment_clarity", "Assessment expectations are clear before I start the work."),
                new("q06", "assessment_clarity", "Assessment timing feels manageable across my courses."),
                new("q07", "recovery_time", "I have enough recovery time during the study week."),
                new("q08", "recovery_time", "I can disconnect from study tasks without falling behind.")
            ],
            [
                new("Term start", new Dictionary<string, double>
                {
                    ["study_load_manageability"] = 5.2,
                    ["support_access"] = 4.7,
                    ["assessment_clarity"] = 4.8,
                    ["recovery_time"] = 4.6
                }),
                new("Midterm pressure", new Dictionary<string, double>
                {
                    ["study_load_manageability"] = 4.5,
                    ["support_access"] = 4.3,
                    ["assessment_clarity"] = 4.2,
                    ["recovery_time"] = 4.0
                }),
                new("Exam week", new Dictionary<string, double>
                {
                    ["study_load_manageability"] = 3.4,
                    ["support_access"] = 3.8,
                    ["assessment_clarity"] = 3.2,
                    ["recovery_time"] = 3.3
                }),
                new("Post-exam recovery", new Dictionary<string, double>
                {
                    ["study_load_manageability"] = 4.8,
                    ["support_access"] = 4.5,
                    ["assessment_clarity"] = 4.4,
                    ["recovery_time"] = 4.2
                })
            ]),
        new(
            "leadership-360-feedback",
            "360 leadership feedback sample",
            "Read-only sample showing identified target-aware feedback where respondents answer about another person.",
            "Leadership behavior",
            CampaignSeriesSampleScenarios.Completed,
            ResponseIdentityModes.Identified,
            CampaignSeriesStudyDesignTypes.SingleWave,
            [
                new(SubjectGroupTypes.Department, "Operations leadership", 6, -0.25),
                new(SubjectGroupTypes.Department, "Academic services leadership", 6, 0.15),
                new(SubjectGroupTypes.Department, "Student support leadership", 6, 0.35),
                new(SubjectGroupTypes.Department, "Small executive cell", 4, -0.55)
            ],
            [
                new("direction_clarity", "Direction clarity", ["q01", "q02"]),
                new("psychological_safety", "Psychological safety", ["q03", "q04"]),
                new("coaching_support", "Coaching support", ["q05", "q06"]),
                new("follow_through", "Follow-through", ["q07", "q08"])
            ],
            [
                new("q01", "direction_clarity", "This person sets clear priorities when work is ambiguous."),
                new("q02", "direction_clarity", "This person explains decisions in a way people can act on."),
                new("q03", "psychological_safety", "This person makes it safe to raise concerns early."),
                new("q04", "psychological_safety", "This person responds constructively when people disagree."),
                new("q05", "coaching_support", "This person gives useful guidance without taking ownership away."),
                new("q06", "coaching_support", "This person helps people grow through practical feedback."),
                new("q07", "follow_through", "This person follows through on commitments made to the team."),
                new("q08", "follow_through", "This person removes blockers before they become chronic.")
            ],
            [
                new("Leadership feedback round", new Dictionary<string, double>
                {
                    ["direction_clarity"] = 4.8,
                    ["psychological_safety"] = 4.4,
                    ["coaching_support"] = 4.6,
                    ["follow_through"] = 4.2
                })
            ],
            AssignmentScenario: SampleAssignmentScenarios.TargetAware360,
            SubjectDisplayNames:
            [
                "Adele Vance",
                "Miriam Graham",
                "Diego Siciliani",
                "Megan Bowen",
                "Joni Sherman",
                "Isaiah Langer",
                "Patti Fernandez",
                "Alex Wilber",
                "Allan Deyoung",
                "Lynne Robbins",
                "Nestor Wilke",
                "Grady Archie",
                "Johanna Lorenz",
                "Pradeep Gupta",
                "Lee Gu",
                "Christie Cline",
                "Aadi Kapoor",
                "Lidia Holloway",
                "Cecil Folk",
                "Elvia Atkins",
                "Garret Vargas",
                "Katarina Novak"
            ]),
        new(
            "complex-scoring-showcase",
            "Complex scoring methods showcase",
            "Read-only sample showing a production-style scoring plan with weighted, normalized, composite, and difference outputs.",
            "Intervention readiness",
            CampaignSeriesSampleScenarios.Completed,
            ResponseIdentityModes.AnonymousLongitudinal,
            CampaignSeriesStudyDesignTypes.RepeatedLinkedChange,
            [
                new(SubjectGroupTypes.Team, "Pilot cohort", 9, -0.1),
                new(SubjectGroupTypes.Team, "Control cohort", 8, -0.35),
                new(SubjectGroupTypes.Team, "Coaching cohort", 7, 0.25),
                new(SubjectGroupTypes.Team, "Small advisory cell", 4, -0.55)
            ],
            [
                new(
                    "focus_stability",
                    "Focus stability",
                    ["q01", "q02", "q03"],
                    "normalized_weighted_mean_0_100",
                    0,
                    100,
                    new Dictionary<string, decimal>
                    {
                        ["q01"] = 2m,
                        ["q02"] = 1m,
                        ["q03"] = 1m
                    }),
                new(
                    "recovery_capacity",
                    "Recovery capacity",
                    ["q04", "q05"]),
                new(
                    "support_resource_total",
                    "Support resource total",
                    ["q06", "q07", "q08"],
                    "weighted_sum",
                    4,
                    28,
                    new Dictionary<string, decimal>
                    {
                        ["q06"] = 1m,
                        ["q07"] = 2m,
                        ["q08"] = 1m
                    }),
                new(
                    "readiness_index",
                    "Readiness index",
                    ["focus_stability", "recovery_capacity_0_100", "support_resource_total_0_100"],
                    "composite_weighted_mean",
                    0,
                    100,
                    new Dictionary<string, decimal>
                    {
                        ["focus_stability"] = 0.45m,
                        ["recovery_capacity_0_100"] = 0.35m,
                        ["support_resource_total_0_100"] = 0.20m
                    }),
                new(
                    "recovery_focus_gap",
                    "Recovery minus focus gap",
                    ["recovery_capacity_0_100", "focus_stability"],
                    "difference",
                    -100,
                    100)
            ],
            [
                new("q01", "focus_stability", "I can protect focused work time when priorities change."),
                new("q02", "focus_stability", "Interruptions are handled without losing track of critical tasks."),
                new("q03", "focus_stability", "I can tell which work should wait when demand exceeds capacity."),
                new("q04", "recovery_capacity", "I have enough recovery time after high-intensity work periods."),
                new("q05", "recovery_capacity", "The current rhythm leaves enough energy for the next workday."),
                new("q06", "support_resource_total", "I have access to practical help when workload increases."),
                new("q07", "support_resource_total", "The support I receive is strong enough to change outcomes."),
                new("q08", "support_resource_total", "Barriers are escalated before they become chronic issues.")
            ],
            [
                new("Before intervention", new Dictionary<string, double>
                {
                    ["focus_stability"] = 4.0,
                    ["recovery_capacity"] = 3.6,
                    ["support_resource_total"] = 3.8
                }),
                new("Intervention midpoint", new Dictionary<string, double>
                {
                    ["focus_stability"] = 4.7,
                    ["recovery_capacity"] = 4.2,
                    ["support_resource_total"] = 4.6
                }),
                new("Intervention review", new Dictionary<string, double>
                {
                    ["focus_stability"] = 5.3,
                    ["recovery_capacity"] = 4.9,
                    ["support_resource_total"] = 5.2
                })
            ])
    ];
}

internal sealed record SampleStudySpec(
    string Key,
    string Name,
    string TemplateDescription,
    string SectionTitle,
    string SampleScenario,
    string ResponseIdentityMode,
    string StudyDesignType,
    IReadOnlyList<SampleGroupSpec> Groups,
    IReadOnlyList<SampleScoreSpec> Scores,
    IReadOnlyList<SampleQuestionSpec> Questions,
    IReadOnlyList<SampleWaveSpec> Waves,
    string AssignmentScenario = SampleAssignmentScenarios.Self,
    IReadOnlyList<string>? SubjectDisplayNames = null)
{
    public int RespondentCount => Groups.Sum(group => group.RespondentCount);
}

internal sealed record SampleGroupSpec(
    string Type,
    string Name,
    int RespondentCount,
    double MeanOffset);

internal sealed record SampleScoreSpec(
    string Code,
    string Label,
    IReadOnlyList<string> QuestionCodes,
    string Calculation = "mean",
    decimal ScoreRangeMin = 1,
    decimal ScoreRangeMax = 7,
    IReadOnlyDictionary<string, decimal>? Weights = null);

internal sealed record SampleQuestionSpec(string Code, string ScoreCode, string Text);

internal sealed record SampleWaveSpec(
    string Name,
    IReadOnlyDictionary<string, double> TargetMeans);

internal sealed record ExistingSampleSeries(Guid Id, string Name);

internal sealed record SampleDirectory(
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<SubjectGroup> Groups,
    IReadOnlyList<SubjectMembership> Memberships,
    IReadOnlyList<SampleRespondentProfile> Respondents);

internal sealed record SampleDirectoryGroup(
    SampleGroupSpec Spec,
    SubjectGroup Entity);

internal sealed record SampleRespondentProfile(
    Guid SubjectId,
    string GroupType,
    string GroupName,
    double GroupOffset);

internal sealed record SampleAssignmentPlan(
    SampleRespondentProfile Respondent,
    SampleRespondentProfile Target,
    string Role);

internal sealed record SampleCampaignExportRow(
    Guid Id,
    Guid TemplateVersionId,
    string Name,
    DateTimeOffset? StartAt);

internal sealed record SampleCampaignResultsMatrixRow(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? ClosedAt);

internal sealed record SampleQuestionExportRow(
    Guid Id,
    int Ordinal,
    string Code,
    string Text);

internal sealed record SampleSessionExportRow(
    Guid Id,
    Guid CampaignId,
    string WaveName,
    DateTimeOffset? WaveStartAt,
    DateTimeOffset SubmittedAt,
    Guid? ParticipantCodeId,
    Guid? SubjectId);

internal sealed record SampleAnswerExportRow(
    Guid SessionId,
    Guid QuestionId,
    string? Value);

internal sealed record SampleScoreExportRow(
    Guid SessionId,
    string DimensionCode,
    decimal Value,
    DateTimeOffset ComputedAt);

internal sealed record SampleSubjectGroupMembershipExportRow(
    Guid SubjectId,
    string GroupType,
    string GroupName);

internal sealed record SampleGroupScoreExportRow(
    string GroupType,
    string GroupName,
    decimal Value);
