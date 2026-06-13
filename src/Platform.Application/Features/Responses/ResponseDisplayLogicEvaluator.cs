using System.Text.Json;

namespace Platform.Application.Features.Responses;

public sealed record ResponseDisplayLogicQuestion(
    Guid Id,
    int Ordinal,
    string Code,
    bool Required,
    string Payload);

public sealed record ResponseDisplayLogicAnswer(
    Guid QuestionId,
    string? Value,
    bool IsSkipped,
    bool IsNa);

public sealed record ResponseDisplayLogicEvaluation(
    IReadOnlySet<Guid> VisibleQuestionIds,
    IReadOnlySet<Guid> HiddenQuestionIds,
    IReadOnlySet<Guid> RequiredVisibleQuestionIds);

public static class ResponseDisplayLogicEvaluator
{
    public static ResponseDisplayLogicEvaluation Evaluate(
        IEnumerable<ResponseDisplayLogicQuestion> questions,
        IEnumerable<ResponseDisplayLogicAnswer> answers)
    {
        var orderedQuestions = questions
            .OrderBy(question => question.Ordinal)
            .ToArray();
        var questionByCode = orderedQuestions.ToDictionary(
            question => question.Code.Trim().ToLowerInvariant(),
            question => question);
        var answersByQuestionId = answers
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last());
        var visibleQuestionIds = new HashSet<Guid>();
        var hiddenQuestionIds = new HashSet<Guid>();

        foreach (var question in orderedQuestions)
        {
            if (IsVisible(question, questionByCode, answersByQuestionId, visibleQuestionIds))
            {
                visibleQuestionIds.Add(question.Id);
            }
            else
            {
                hiddenQuestionIds.Add(question.Id);
            }
        }

        var requiredVisibleQuestionIds = orderedQuestions
            .Where(question => question.Required && visibleQuestionIds.Contains(question.Id))
            .Select(question => question.Id)
            .ToHashSet();

        return new ResponseDisplayLogicEvaluation(
            visibleQuestionIds,
            hiddenQuestionIds,
            requiredVisibleQuestionIds);
    }

    private static bool IsVisible(
        ResponseDisplayLogicQuestion question,
        IReadOnlyDictionary<string, ResponseDisplayLogicQuestion> questionByCode,
        IReadOnlyDictionary<Guid, ResponseDisplayLogicAnswer> answersByQuestionId,
        IReadOnlySet<Guid> visibleQuestionIds)
    {
        var rule = ReadRule(question.Payload);
        if (rule is null)
        {
            return true;
        }

        if (!questionByCode.TryGetValue(rule.SourceQuestionCode, out var sourceQuestion))
        {
            return true;
        }

        if (!visibleQuestionIds.Contains(sourceQuestion.Id))
        {
            return false;
        }

        if (!answersByQuestionId.TryGetValue(sourceQuestion.Id, out var sourceAnswer) ||
            sourceAnswer.IsSkipped ||
            sourceAnswer.IsNa)
        {
            return false;
        }

        if (rule.Operator is "contains" or "not_contains")
        {
            var selectedValues = ArrayValues(sourceAnswer.Value);
            if (selectedValues is null)
            {
                return false;
            }

            var containsExpectedValue = selectedValues.Contains(rule.ExpectedValue);
            return rule.Operator == "not_contains"
                ? !containsExpectedValue
                : containsExpectedValue;
        }

        var actualValue = ScalarValue(sourceAnswer.Value);
        if (actualValue is null)
        {
            return false;
        }

        var matchesExpectedValue = actualValue == rule.ExpectedValue;
        return rule.Operator == "not_equals"
            ? !matchesExpectedValue
            : matchesExpectedValue;
    }

    private static DisplayRule? ReadRule(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("displayLogic", out var displayLogic) ||
                displayLogic.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var operatorName = ReadString(displayLogic, "operator");
            if (ReadString(displayLogic, "mode") != "show_when" ||
                !IsSupportedOperator(operatorName))
            {
                return null;
            }

            var sourceQuestionCode = ReadString(displayLogic, "sourceQuestionCode");
            var expectedValue = ReadString(displayLogic, "value");
            if (string.IsNullOrWhiteSpace(sourceQuestionCode) || string.IsNullOrWhiteSpace(expectedValue))
            {
                return null;
            }

            return new DisplayRule(
                sourceQuestionCode.Trim().ToLowerInvariant(),
                operatorName!,
                expectedValue.Trim());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ScalarValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.String => document.RootElement.GetString(),
                JsonValueKind.Number => document.RootElement.GetRawText(),
                JsonValueKind.True => "True",
                JsonValueKind.False => "False",
                _ => null
            };
        }
        catch (JsonException)
        {
            return value.Trim();
        }
    }

    private static IReadOnlySet<string>? ArrayValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return document.RootElement
                .EnumerateArray()
                .Select(element => element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "True",
                    JsonValueKind.False => "False",
                    _ => null
                })
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .ToHashSet(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool IsSupportedOperator(string? operatorName)
    {
        return operatorName is "equals" or "not_equals" or "contains" or "not_contains";
    }

    private sealed record DisplayRule(string SourceQuestionCode, string Operator, string ExpectedValue);
}
