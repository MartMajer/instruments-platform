using System.Text.Json;
using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Templates;
using Platform.SharedKernel;

namespace Platform.Application.Features.Setup;

public sealed record CreateTemplateVersionCommand(
    CreateTemplateVersionRequest Request) : IRequest<Result<TemplateVersionDetailResponse>>;

public sealed class CreateTemplateVersionValidator
    : AbstractValidator<CreateTemplateVersionCommand>
{
    public CreateTemplateVersionValidator()
    {
        RuleFor(command => command.Request.TemplateName).NotEmpty();
        RuleFor(command => command.Request.Semver).NotEmpty();
        RuleFor(command => command.Request.DefaultLocale).NotEmpty();
        RuleFor(command => command.Request.Sections).NotEmpty();
        RuleFor(command => command.Request.Questions).NotEmpty();
        RuleForEach(command => command.Request.Sections).ChildRules(section =>
        {
            section.RuleFor(value => value.Ordinal).GreaterThan(0);
            section.RuleFor(value => value.Code).NotEmpty();
            section.RuleFor(value => value.TitleDefault).NotEmpty();
        });
        RuleForEach(command => command.Request.Scales).ChildRules(scale =>
        {
            scale.RuleFor(value => value.Code).NotEmpty();
            scale.RuleFor(value => value.Type).NotEmpty();
            scale.RuleFor(value => value.MaxValue).GreaterThan(value => value.MinValue);
            scale.RuleFor(value => value.Step).GreaterThan(0);
            scale.RuleFor(value => value.Anchors).NotEmpty();
        });
        RuleForEach(command => command.Request.Questions).ChildRules(question =>
        {
            question.RuleFor(value => value.Ordinal).GreaterThan(0);
            question.RuleFor(value => value.Code).NotEmpty();
            question.RuleFor(value => value.Type).NotEmpty();
            question.RuleFor(value => value.TextDefault).NotEmpty();
            question.RuleFor(value => value.Payload).NotEmpty();
            question.RuleFor(value => value.MissingCodes).NotEmpty();
        });
        RuleFor(command => command.Request).Custom(ValidateQuestionPayloadContracts);
        RuleFor(command => command.Request).Custom(ValidateDisplayLogicRules);
    }

    private static void ValidateQuestionPayloadContracts(
        CreateTemplateVersionRequest request,
        ValidationContext<CreateTemplateVersionCommand> context)
    {
        foreach (var question in request.Questions.OrderBy(question => question.Ordinal))
        {
            using var document = ParsePayload(question.Payload, question.Code, context);
            if (document is null)
            {
                continue;
            }

            switch (question.Type)
            {
                case QuestionTypes.SingleChoice:
                case QuestionTypes.MultiChoice:
                case QuestionTypes.Ranking:
                    ValidateChoiceBackedQuestionPayload(question, document.RootElement, context);
                    break;
                case QuestionTypes.Matrix:
                    ValidateMatrixQuestionPayload(question, document.RootElement, context);
                    break;
            }
        }
    }

    private static void ValidateChoiceBackedQuestionPayload(
        CreateTemplateQuestionRequest question,
        JsonElement payload,
        ValidationContext<CreateTemplateVersionCommand> context)
    {
        var options = ReadChoiceOptionCodes(payload);
        if (options.Count < 2)
        {
            context.AddFailure(
                "Request.Questions",
                $"Question '{question.Code}' needs at least two answer options.");
            return;
        }

        if (!string.Equals(question.Type, QuestionTypes.Ranking, StringComparison.Ordinal))
        {
            return;
        }

        if (!payload.TryGetProperty("ranking", out var ranking) ||
            ranking.ValueKind != JsonValueKind.Object ||
            ReadString(ranking, "mode") != "top_n")
        {
            return;
        }

        if (!ranking.TryGetProperty("topN", out var topN) ||
            !topN.TryGetInt32(out var topNValue) ||
            topNValue < 1 ||
            topNValue > options.Count)
        {
            context.AddFailure(
                "Request.Questions",
                $"Question '{question.Code}' top-N ranking must be between 1 and the number of available options.");
        }
    }

    private static void ValidateMatrixQuestionPayload(
        CreateTemplateQuestionRequest question,
        JsonElement payload,
        ValidationContext<CreateTemplateVersionCommand> context)
    {
        if (!payload.TryGetProperty("matrix", out var matrix) ||
            matrix.ValueKind != JsonValueKind.Object ||
            ReadString(matrix, "mode") != "single")
        {
            context.AddFailure(
                "Request.Questions",
                $"Question '{question.Code}' matrix payload must declare single-select rows and columns.");
            return;
        }

        var rows = ReadOptionCodes(matrix, "rows");
        if (rows.Count < 1)
        {
            context.AddFailure(
                "Request.Questions",
                $"Question '{question.Code}' matrix needs at least one row.");
        }

        var columns = ReadOptionCodes(matrix, "columns");
        if (columns.Count < 2)
        {
            context.AddFailure(
                "Request.Questions",
                $"Question '{question.Code}' matrix needs at least two column options.");
        }
    }

    private static void ValidateDisplayLogicRules(
        CreateTemplateVersionRequest request,
        ValidationContext<CreateTemplateVersionCommand> context)
    {
        var questions = request.Questions
            .OrderBy(question => question.Ordinal)
            .ToArray();
        var questionsByCode = questions
            .Where(question => !string.IsNullOrWhiteSpace(question.Code))
            .GroupBy(question => NormalizeCode(question.Code))
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);

        foreach (var question in questions)
        {
            if (!TryReadDisplayLogicRule(question.Payload, out var rule, out var errorMessage))
            {
                context.AddFailure(
                    "Request.Questions",
                    $"Question '{question.Code}' {errorMessage}");
                continue;
            }

            if (rule is null)
            {
                continue;
            }

            if (!questionsByCode.TryGetValue(rule.SourceQuestionCode, out var sourceQuestion) ||
                sourceQuestion.Ordinal >= question.Ordinal)
            {
                context.AddFailure(
                    "Request.Questions",
                    $"Question '{question.Code}' display rule needs an earlier source question.");
                continue;
            }

            if (!string.Equals(sourceQuestion.Type, QuestionTypes.SingleChoice, StringComparison.Ordinal))
            {
                context.AddFailure(
                    "Request.Questions",
                    $"Question '{question.Code}' display rule source must be a single-choice question.");
                continue;
            }

            var sourceOptions = ReadChoiceOptionCodes(sourceQuestion.Payload);
            if (!sourceOptions.Contains(rule.ExpectedValue))
            {
                context.AddFailure(
                    "Request.Questions",
                    $"Question '{question.Code}' display rule needs one source answer.");
            }
        }
    }

    private static JsonDocument? ParsePayload(
        string payload,
        string questionCode,
        ValidationContext<CreateTemplateVersionCommand> context)
    {
        try
        {
            var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }

            document.Dispose();
            context.AddFailure("Request.Questions", $"Question '{questionCode}' payload must be a JSON object.");
            return null;
        }
        catch (JsonException)
        {
            context.AddFailure("Request.Questions", $"Question '{questionCode}' payload must be valid JSON.");
            return null;
        }
    }

    private static bool TryReadDisplayLogicRule(
        string payload,
        out DisplayLogicRule? rule,
        out string errorMessage)
    {
        rule = null;
        errorMessage = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "payload must be a JSON object.";
                return false;
            }

            if (!document.RootElement.TryGetProperty("displayLogic", out var displayLogic))
            {
                return true;
            }

            if (displayLogic.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "display rule must be a JSON object.";
                return false;
            }

            var mode = ReadString(displayLogic, "mode");
            var operatorName = ReadString(displayLogic, "operator");
            var sourceQuestionCode = ReadString(displayLogic, "sourceQuestionCode");
            var value = ReadString(displayLogic, "value");
            if (mode != "show_when" ||
                operatorName != "equals" ||
                string.IsNullOrWhiteSpace(sourceQuestionCode) ||
                string.IsNullOrWhiteSpace(value))
            {
                errorMessage = "display rule must use show_when/equals with a source question and source answer.";
                return false;
            }

            rule = new DisplayLogicRule(NormalizeCode(sourceQuestionCode), value.Trim());
            return true;
        }
        catch (JsonException)
        {
            errorMessage = "payload must be valid JSON.";
            return false;
        }
    }

    private static HashSet<string> ReadChoiceOptionCodes(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            return ReadChoiceOptionCodes(document.RootElement);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static HashSet<string> ReadChoiceOptionCodes(JsonElement payload)
    {
        return ReadOptionCodes(payload, "options");
    }

    private static HashSet<string> ReadOptionCodes(JsonElement payload, string propertyName)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var options) ||
            options.ValueKind != JsonValueKind.Array)
        {
            return codes;
        }

        foreach (var option in options.EnumerateArray())
        {
            var code = ReadString(option, "code");
            if (!string.IsNullOrWhiteSpace(code))
            {
                codes.Add(code.Trim());
            }
        }

        return codes;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed record DisplayLogicRule(string SourceQuestionCode, string ExpectedValue);
}

public sealed class CreateTemplateVersionHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ISetupWorkflowStore store)
    : IRequestHandler<CreateTemplateVersionCommand, Result<TemplateVersionDetailResponse>>
{
    public Task<Result<TemplateVersionDetailResponse>> Handle(
        CreateTemplateVersionCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateTemplateVersionAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.Request,
            cancellationToken);
    }
}
