using System.Text.Json;
using Platform.Application.Features.Responses;
using Platform.Domain.Templates;

namespace Platform.Application.Features.TestData;

public static class TestDataSimulatorAnswerFactory
{
    private const decimal OutcomeMinimum = 0m;
    private const decimal OutcomeMaximum = 10m;

    public static IReadOnlyList<TestDataSimulatorAnswer> CreateAnswers(
        IReadOnlyList<TestDataSimulatorQuestion> questions,
        CreateCampaignTestResponsesRequest request,
        int respondentIndex)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(request);

        var answers = new List<TestDataSimulatorAnswer>(questions.Count);
        var targetOutcome = Clamp(request.TargetOutcome, OutcomeMinimum, OutcomeMaximum);
        var adjustedOutcome = AdjustOutcome(targetOutcome, request.Variation, respondentIndex);

        foreach (var question in questions)
        {
            answers.Add(CreateAnswer(question, request, adjustedOutcome, respondentIndex));
        }

        return ApplyDisplayLogic(questions, answers);
    }

    private static IReadOnlyList<TestDataSimulatorAnswer> ApplyDisplayLogic(
        IReadOnlyList<TestDataSimulatorQuestion> questions,
        IReadOnlyList<TestDataSimulatorAnswer> answers)
    {
        var evaluation = ResponseDisplayLogicEvaluator.Evaluate(
            questions.Select((question, index) => new ResponseDisplayLogicQuestion(
                question.Id,
                index + 1,
                question.Code,
                question.Required,
                question.Payload)),
            answers.Select(answer => new ResponseDisplayLogicAnswer(
                answer.QuestionId,
                answer.Value,
                answer.IsSkipped,
                answer.IsNa)));

        if (evaluation.HiddenQuestionIds.Count == 0)
        {
            return answers;
        }

        return answers
            .Select(answer => evaluation.HiddenQuestionIds.Contains(answer.QuestionId)
                ? answer with { Value = null, Comment = null, IsSkipped = true, IsNa = false }
                : answer)
            .ToArray();
    }

    private static TestDataSimulatorAnswer CreateAnswer(
        TestDataSimulatorQuestion question,
        CreateCampaignTestResponsesRequest request,
        decimal adjustedOutcome,
        int respondentIndex)
    {
        return question.Type switch
        {
            QuestionTypes.Likert or QuestionTypes.Nps => CreateScaleAnswer(question, adjustedOutcome),
            QuestionTypes.Number => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(decimal.Round(adjustedOutcome, 1))),
            QuestionTypes.SingleChoice => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(ChooseSingleOption(question.Payload, adjustedOutcome))),
            QuestionTypes.MultiChoice => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(ChooseMultipleOptions(question.Payload, adjustedOutcome))),
            QuestionTypes.Ranking => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(ChooseRanking(question.Payload, adjustedOutcome))),
            QuestionTypes.Text => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                request.IncludeComments
                    ? JsonSerializer.Serialize(
                        $"Simulated response {respondentIndex + 1}: target outcome {request.TargetOutcome:0.#}/10.")
                    : null,
                request.IncludeComments ? "simulated_test_data" : null,
                IsSkipped: !request.IncludeComments && !question.Required),
            QuestionTypes.Date => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(DateOnly.FromDateTime(DateTime.UtcNow.Date).ToString("yyyy-MM-dd"))),
            QuestionTypes.Matrix => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                JsonSerializer.Serialize(ChooseMatrix(question.Payload, adjustedOutcome))),
            QuestionTypes.File => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                question.Required ? "[]" : null,
                IsSkipped: !question.Required),
            QuestionTypes.Pairwise => new TestDataSimulatorAnswer(question.Id, question.Code, "{}"),
            _ => new TestDataSimulatorAnswer(
                question.Id,
                question.Code,
                question.Required ? "{}" : null,
                IsSkipped: !question.Required)
        };
    }

    private static TestDataSimulatorAnswer CreateScaleAnswer(
        TestDataSimulatorQuestion question,
        decimal adjustedOutcome)
    {
        var min = question.ScaleMinValue ?? (question.Type == QuestionTypes.Nps ? 0 : 1);
        var max = question.ScaleMaxValue ?? (question.Type == QuestionTypes.Nps ? 10 : 5);
        var ratio = adjustedOutcome / OutcomeMaximum;
        var value = min + (ratio * (max - min));
        var rounded = (int)decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        var clamped = Math.Clamp(rounded, min, max);

        return new TestDataSimulatorAnswer(
            question.Id,
            question.Code,
            JsonSerializer.Serialize(clamped));
    }

    private static decimal AdjustOutcome(decimal targetOutcome, string variation, int respondentIndex)
    {
        var offset = variation.Trim().ToLowerInvariant() switch
        {
            "tight" => 0m,
            "noisy" => ((respondentIndex % 7) - 3) * 0.8m,
            _ => ((respondentIndex % 5) - 2) * 0.35m
        };

        return Clamp(targetOutcome + offset, OutcomeMinimum, OutcomeMaximum);
    }

    private static string ChooseSingleOption(string payload, decimal adjustedOutcome)
    {
        var options = ReadOptionCodes(payload);
        if (options.Count == 0)
        {
            return "selected";
        }

        return ChooseOptionByOutcome(options, adjustedOutcome);
    }

    private static IReadOnlyList<string> ChooseMultipleOptions(string payload, decimal adjustedOutcome)
    {
        var options = ReadOptionCodes(payload);
        if (options.Count == 0)
        {
            return ["selected"];
        }

        var ratio = adjustedOutcome / OutcomeMaximum;
        var count = Math.Clamp((int)Math.Ceiling((double)(ratio * options.Count)), 1, options.Count);

        return options.Take(count).ToArray();
    }

    private static IReadOnlyList<string> ChooseRanking(string payload, decimal adjustedOutcome)
    {
        var options = ReadOptionCodes(payload);
        if (options.Count == 0)
        {
            return ["selected"];
        }

        return adjustedOutcome >= 5m
            ? options.ToArray()
            : options.Reverse().ToArray();
    }

    private static IReadOnlyDictionary<string, string> ChooseMatrix(string payload, decimal adjustedOutcome)
    {
        var rows = ReadMatrixCodes(payload, "rows");
        var columns = ReadMatrixCodes(payload, "columns");
        if (rows.Count == 0 || columns.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var selectedColumn = ChooseOptionByOutcome(columns, adjustedOutcome);
        return rows.ToDictionary(row => row, _ => selectedColumn);
    }

    private static string ChooseOptionByOutcome(IReadOnlyList<string> options, decimal adjustedOutcome)
    {
        var ratio = adjustedOutcome / OutcomeMaximum;
        var index = (int)Math.Ceiling((double)(ratio * options.Count)) - 1;
        index = Math.Clamp(index, 0, options.Count - 1);

        return options[index];
    }

    private static IReadOnlyList<string> ReadOptionCodes(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("options", out var optionsElement) ||
                optionsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var options = new List<string>();
            foreach (var option in optionsElement.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (option.TryGetProperty("code", out var codeElement) &&
                    codeElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(codeElement.GetString()))
                {
                    options.Add(codeElement.GetString()!.Trim());
                    continue;
                }

                if (option.TryGetProperty("value", out var valueElement) &&
                    valueElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(valueElement.GetString()))
                {
                    options.Add(valueElement.GetString()!.Trim());
                }
            }

            return options;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ReadMatrixCodes(string payload, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("matrix", out var matrixElement) ||
                matrixElement.ValueKind != JsonValueKind.Object ||
                !matrixElement.TryGetProperty(propertyName, out var optionsElement) ||
                optionsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var options = new List<string>();
            foreach (var option in optionsElement.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (option.TryGetProperty("code", out var codeElement) &&
                    codeElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(codeElement.GetString()))
                {
                    options.Add(codeElement.GetString()!.Trim());
                    continue;
                }

                if (option.TryGetProperty("value", out var valueElement) &&
                    valueElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(valueElement.GetString()))
                {
                    options.Add(valueElement.GetString()!.Trim());
                }
            }

            return options;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
