using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        var existingSampleNames = await db.CampaignSeries
            .AsNoTracking()
            .Where(series =>
                series.TenantId == tenantId &&
                series.StudyKind == CampaignSeriesStudyKinds.Sample)
            .Select(series => series.Name)
            .ToListAsync(cancellationToken);
        var existingSampleNameSet = existingSampleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

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

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new EnsureSampleStudiesResponse(
            tenantId,
            existingSampleNames.Count,
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

        db.ExportArtifacts.Add(CreateSeriesResponseExport(
            tenantId,
            series,
            spec,
            sessions.Count,
            baseTime.AddDays(23)));
        await db.SaveChangesAsync(cancellationToken);

        return series.Id;
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

    private static ExportArtifact CreateSeriesResponseExport(
        Guid tenantId,
        CampaignSeries series,
        SampleStudySpec spec,
        int responseCount,
        DateTimeOffset completedAt)
    {
        var content = string.Join(
            "\n",
            "study,responses,identity_mode,scoring,status",
            CsvRow(series.Name, responseCount.ToString(), spec.ResponseIdentityMode, "total_mean_1_7", "sample_closed"));

        return CreateInlineExport(
            tenantId,
            ExportArtifactTargetKinds.CampaignSeries,
            campaignId: null,
            campaignSeriesId: series.Id,
            ExportArtifactTypes.CampaignSeriesResponseCsvCodebook,
            $"{spec.Key}-responses.csv",
            rowCount: responseCount,
            content,
            metadataJson: JsonSerializer.Serialize(new
            {
                sampleStudy = true,
                study = series.Name,
                purpose = "sample_response_dataset"
            }, JsonOptions),
            codebookJson: JsonSerializer.Serialize(new
            {
                columns = new[] { "study", "responses", "identity_mode", "scoring", "status" }
            }, JsonOptions),
            completedAt);
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
