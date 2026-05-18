using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Scoring;
using Platform.Domain.Campaigns;
using Platform.Domain.Scoring;
using Platform.Domain.Templates;
using Platform.Infrastructure.Data;
using Platform.SharedKernel;

namespace Platform.Infrastructure.Scoring;

public sealed class SubmittedResponseScoreMaterializer(ApplicationDbContext db)
{
    public async Task<Result<SubmittedResponseScoreMaterializationResult>> MaterializeAsync(
        Guid tenantId,
        Guid sessionId,
        bool requireScoringRule,
        CancellationToken cancellationToken)
    {
        var session = await db.ResponseSessions
            .SingleOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.NotFound("response_session.not_found", "Response session was not found."));
        }

        if (!session.SubmittedAt.HasValue)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.Conflict("response_session.not_submitted", "Response session must be submitted before scoring."));
        }

        var assignment = await db.Assignments
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == session.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.NotFound("assignment.not_found", "Response assignment was not found."));
        }

        var campaign = await db.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == assignment.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.NotFound("campaign.not_found", "Campaign was not found."));
        }

        var existingMaterialization = await GetExistingMaterializationAsync(session.Id, cancellationToken);
        if (existingMaterialization is not null)
        {
            return Result.Success(existingMaterialization);
        }

        var snapshot = await db.CampaignLaunchSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CampaignId == campaign.Id, cancellationToken);
        var templateVersionId = snapshot?.TemplateVersionId ?? campaign.TemplateVersionId;
        var scoringRule = await GetScoringRuleAsync(campaign, snapshot, cancellationToken);
        if (scoringRule is null)
        {
            return requireScoringRule
                ? Result.Failure<SubmittedResponseScoreMaterializationResult>(
                    Error.Validation(
                        "score.rule_missing",
                        "No draft or published scoring rule exists for this campaign template."))
                : Result.Success(new SubmittedResponseScoreMaterializationResult(
                    ScoreRunId: null,
                    SessionId: session.Id,
                    Scores: []));
        }

        var inputs = await GetScoreInputsAsync(session.Id, templateVersionId, cancellationToken);
        if (inputs.IsFailure)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(inputs.Error);
        }

        Result<IReadOnlyList<SimpleScoreOutput>> evaluated;
        try
        {
            evaluated = SimpleScoringEngine.Evaluate(scoringRule.Document, inputs.Value);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.Validation("score.rule_invalid", exception.Message));
        }
        catch (JsonException exception)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(
                Error.Validation("score.rule_invalid", exception.Message));
        }

        if (evaluated.IsFailure)
        {
            return Result.Failure<SubmittedResponseScoreMaterializationResult>(evaluated.Error);
        }

        var run = new ScoreRun(
            PlatformIds.NewId(),
            tenantId,
            campaign.Id,
            session.Id,
            scoringRule.Id,
            ScoreRunStatuses.Success);
        var scores = evaluated.Value
            .Select(output => new Score(
                PlatformIds.NewId(),
                tenantId,
                run.Id,
                campaign.Id,
                session.Id,
                output.DimensionCode,
                output.Value,
                output.NValid,
                output.NExpected ?? output.NValid,
                output.MissingPolicyStatus ?? ScoreMissingPolicyStatuses.Ok))
            .ToArray();

        db.ScoreRuns.Add(run);
        db.Scores.AddRange(scores);

        return Result.Success(new SubmittedResponseScoreMaterializationResult(
            run.Id,
            session.Id,
            ToResponses(scores)));
    }

    private async Task<Result<IReadOnlyList<SimpleScoreInput>>> GetScoreInputsAsync(
        Guid sessionId,
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        var questions = await db.TemplateQuestions
            .AsNoTracking()
            .Where(question => question.TemplateVersionId == templateVersionId)
            .ToDictionaryAsync(question => question.Id, question => question.Code, cancellationToken);
        var answers = await db.Answers
            .AsNoTracking()
            .Where(answer => answer.SessionId == sessionId)
            .ToListAsync(cancellationToken);
        var inputs = new List<SimpleScoreInput>();

        foreach (var answer in answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var code))
            {
                return Result.Failure<IReadOnlyList<SimpleScoreInput>>(
                    Error.Validation("score.question_missing", "One or more answers reference missing template questions."));
            }

            inputs.Add(new SimpleScoreInput(
                code,
                answer.Value,
                answer.IsSkipped,
                answer.IsNa));
        }

        return Result.Success<IReadOnlyList<SimpleScoreInput>>(inputs);
    }

    private async Task<ScoringRule?> GetScoringRuleAsync(
        Campaign campaign,
        CampaignLaunchSnapshot? snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot is not null)
        {
            return await db.ScoringRules
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    rule => rule.Id == snapshot.ScoringRuleId &&
                        rule.TemplateVersionId == snapshot.TemplateVersionId,
                    cancellationToken);
        }

        var rules = await db.ScoringRules
            .AsNoTracking()
            .Where(rule =>
                rule.TemplateVersionId == campaign.TemplateVersionId &&
                (rule.Status == ScoringRuleStatuses.Draft ||
                    rule.Status == ScoringRuleStatuses.Published))
            .ToListAsync(cancellationToken);

        return rules
            .OrderByDescending(rule => rule.Status == ScoringRuleStatuses.Published)
            .ThenByDescending(rule => rule.PublishedAt ?? rule.UpdatedAt)
            .FirstOrDefault();
    }

    private async Task<SubmittedResponseScoreMaterializationResult?> GetExistingMaterializationAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var latestRun = await db.ScoreRuns
            .AsNoTracking()
            .Where(run =>
                run.ResponseSessionId == sessionId &&
                run.Status == ScoreRunStatuses.Success)
            .OrderByDescending(run => run.RanAt)
            .ThenByDescending(run => run.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRun is null)
        {
            return null;
        }

        var scores = await db.Scores
            .AsNoTracking()
            .Where(score => score.ScoreRunId == latestRun.Id)
            .OrderBy(score => score.DimensionCode)
            .ToListAsync(cancellationToken);

        return new SubmittedResponseScoreMaterializationResult(
            latestRun.Id,
            sessionId,
            ToResponses(scores));
    }

    private static IReadOnlyList<ComputedScoreResponse> ToResponses(IReadOnlyList<Score> scores)
    {
        return scores
            .Select(score => new ComputedScoreResponse(
                score.DimensionCode,
                score.Value,
                score.NValid,
                score.NExpected,
                score.MissingPolicyStatus))
            .ToArray();
    }
}

public sealed record SubmittedResponseScoreMaterializationResult(
    Guid? ScoreRunId,
    Guid SessionId,
    IReadOnlyList<ComputedScoreResponse> Scores);
