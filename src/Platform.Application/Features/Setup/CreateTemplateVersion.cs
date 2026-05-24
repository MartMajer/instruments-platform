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
        RuleFor(command => command.Request).Custom(ValidateDisplayLogicRules);
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
        var codes = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("options", out var options) ||
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
        }
        catch (JsonException)
        {
            return codes;
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
