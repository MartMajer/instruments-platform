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
        var existingSampleNameSet = existingSampleSeries
            .Select(series => series.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in SampleStudySpecs.All)
        {
            if (existingSampleNameSet.Contains(spec.Name))
            {
                continue;
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
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new EnsureSampleStudiesResponse(
            tenantId,
            existingSampleSeries.Count,
            createdSeriesIds.Count,
            createdSeriesIds));
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
            sampleScenario: spec.SampleScenario);
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

        var scoringDocument = JsonSerializer.Serialize(new
        {
            operations = new[]
            {
                new
                {
                    op = "mean",
                    items = spec.Questions.Select(question => question.Code).ToArray(),
                    output = "total"
                }
            }
        }, JsonOptions);
        var scoringRule = ScoringRule.CreateDraft(
            PlatformIds.NewId(),
            templateVersion.Id,
            $"{spec.Key}_score",
            "1.0.0",
            "simple-v1",
            "1.0.0",
            Sha256Hex(scoringDocument),
            scoringDocument,
            """{"scores":["total"]}""",
            """{"sample_study":true,"interpretation":"Synthetic sample score. Higher values indicate better self-reported conditions for this sample only."}""");
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
                var token = new InvitationToken(
                    PlatformIds.NewId(),
                    tenantId,
                    campaign.Id,
                    Sha256Hex($"{tenantId:N}:{spec.Key}:{waveIndex}:{respondentIndex}:token"),
                    InvitationTokenChannels.OpenLink,
                    expiresAt: closedAt.AddDays(30));
                var assignment = Assignment.CreateAnonymous(
                    PlatformIds.NewId(),
                    tenantId,
                    campaign.Id,
                    "respondent",
                    token.Id);
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
                    .Select((question, questionIndex) => new Answer(
                        PlatformIds.NewId(),
                        tenantId,
                        session.Id,
                        question.Id,
                        JsonSerializer.Serialize(CreateAnswerValue(wave.TargetMean, respondentIndex, questionIndex, waveIndex)),
                        answeredAt: session.StartedAt?.AddMinutes(1 + questionIndex)))
                    .ToArray();
                session.Submit(
                    launchedAt.AddHours(2).AddMinutes(respondentIndex + 6),
                    timeTakenMs: 120_000 + (respondentIndex * 1_000));

                db.InvitationTokens.Add(token);
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

    private static int CreateAnswerValue(
        double targetMean,
        int respondentIndex,
        int questionIndex,
        int waveIndex)
    {
        var offset = ((respondentIndex * 7) + (questionIndex * 3) + waveIndex) % 5 - 2;
        var value = (int)Math.Round(targetMean + (offset * 0.35), MidpointRounding.AwayFromZero);

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
            "study,wave,responses,target_mean,status",
            CsvRow(series.Name, wave.Name, spec.RespondentCount.ToString(), wave.TargetMean.ToString("0.0"), "closed"));

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
                columns = new[] { "study", "wave", "responses", "target_mean", "status" }
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
                    session.ParticipantCodeId))
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
                .Where(score =>
                    sessionIds.Contains(score.ResponseSessionId) &&
                    score.DimensionCode == "total")
                .Select(score => new SampleScoreExportRow(
                    score.ResponseSessionId,
                    score.Value,
                    score.ComputedAt))
                .ToListAsync(cancellationToken);
        var answersBySessionAndQuestion = answers.ToDictionary(
            answer => (answer.SessionId, answer.QuestionId),
            answer => answer);
        var scoreBySession = scores
            .GroupBy(score => score.SessionId)
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
            "study,wave,response_key,trajectory_key,submitted_at,question_code,question_text,answer_value,score_total"
        };

        for (var sessionIndex = 0; sessionIndex < orderedSessions.Count; sessionIndex++)
        {
            var session = orderedSessions[sessionIndex];
            var responseKey = $"R{sessionIndex + 1:0000}";
            var trajectoryKey = session.ParticipantCodeId.HasValue &&
                trajectoryKeys.TryGetValue(session.ParticipantCodeId.Value, out var key)
                    ? key
                    : string.Empty;
            var scoreTotal = scoreBySession.TryGetValue(session.Id, out var score)
                ? score.Value.ToString("0.####", CultureInfo.InvariantCulture)
                : string.Empty;

            foreach (var question in questions)
            {
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
                    scoreTotal));
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
                    new { name = "score_total", description = "Simple total mean score for the response." }
                }
            }, JsonOptions),
            completedAt);
    }

    private static bool IsRowLevelResponseExport(ExportArtifact artifact)
    {
        return artifact.Content?.StartsWith(
                "study,wave,response_key,trajectory_key,submitted_at,question_code,question_text,answer_value,score_total",
                StringComparison.Ordinal) == true &&
            artifact.MetadataJson.Contains("sample_response_rows", StringComparison.Ordinal);
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

internal static class SampleStudySpecs
{
    public static IReadOnlyList<SampleStudySpec> All { get; } =
    [
        new(
            "workload-recovery",
            "Workload and recovery pulse",
            "Read-only sample showing two closed collection rounds and response-linked change review.",
            "Workload and recovery",
            CampaignSeriesSampleScenarios.Longitudinal,
            ResponseIdentityModes.AnonymousLongitudinal,
            RespondentCount: 24,
            [
                new("q01", "I can recover enough between demanding work periods."),
                new("q02", "My current workload feels manageable."),
                new("q03", "I have enough control over how I pace my work."),
                new("q04", "My team has practical support when workload increases.")
            ],
            [
                new("Wave 1 baseline", 4.6),
                new("Wave 2 follow-up", 5.6)
            ]),
        new(
            "ergonomics-risk",
            "Ergonomics risk review",
            "Read-only sample showing a closed workplace ergonomics review with report exports.",
            "Ergonomics conditions",
            CampaignSeriesSampleScenarios.Completed,
            ResponseIdentityModes.Anonymous,
            RespondentCount: 18,
            [
                new("q01", "My workstation supports a comfortable working posture."),
                new("q02", "Tools and equipment are adjusted to the task."),
                new("q03", "Repetitive or awkward movements are kept under control."),
                new("q04", "I know how to report and resolve ergonomics concerns.")
            ],
            [
                new("Wave 1 review", 5.2)
            ]),
        new(
            "student-wellbeing",
            "Student wellbeing and study load",
            "Read-only sample showing repeated study-load measurement without identifying respondents.",
            "Study load and support",
            CampaignSeriesSampleScenarios.Longitudinal,
            ResponseIdentityModes.AnonymousLongitudinal,
            RespondentCount: 22,
            [
                new("q01", "I can keep up with the expected study workload."),
                new("q02", "I know where to get support when study pressure rises."),
                new("q03", "Assessment timing feels manageable across my courses."),
                new("q04", "I have enough recovery time during the study week.")
            ],
            [
                new("Wave 1 midpoint", 4.2),
                new("Wave 2 follow-up", 5.1)
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
    int RespondentCount,
    IReadOnlyList<SampleQuestionSpec> Questions,
    IReadOnlyList<SampleWaveSpec> Waves);

internal sealed record SampleQuestionSpec(string Code, string Text);

internal sealed record SampleWaveSpec(string Name, double TargetMean);

internal sealed record ExistingSampleSeries(Guid Id, string Name);

internal sealed record SampleCampaignExportRow(
    Guid Id,
    Guid TemplateVersionId,
    string Name,
    DateTimeOffset? StartAt);

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
    Guid? ParticipantCodeId);

internal sealed record SampleAnswerExportRow(
    Guid SessionId,
    Guid QuestionId,
    string? Value);

internal sealed record SampleScoreExportRow(
    Guid SessionId,
    decimal Value,
    DateTimeOffset ComputedAt);
