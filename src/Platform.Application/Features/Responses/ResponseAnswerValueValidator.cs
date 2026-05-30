using System.Globalization;
using System.Text.Json;
using Platform.Domain.Templates;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record ResponseAnswerQuestionContract(
    Guid Id,
    string Code,
    string Type,
    string Payload,
    int? ScaleMinValue = null,
    int? ScaleMaxValue = null,
    int? ScaleStep = null,
    bool ScaleNaAllowed = false);

public sealed record ResponseAnswerValueContract(
    Guid QuestionId,
    string? Value,
    string? Comment,
    bool IsSkipped,
    bool IsNa);

public static class ResponseAnswerValueValidator
{
    public static Result<bool> Validate(
        IEnumerable<ResponseAnswerQuestionContract> questions,
        IEnumerable<SaveAnswerRequest> answers)
    {
        return ValidateValues(
            questions,
            answers.Select(answer => new ResponseAnswerValueContract(
                answer.QuestionId,
                answer.Value,
                answer.Comment,
                answer.IsSkipped,
                answer.IsNa)));
    }

    public static Result<bool> ValidateSaved(
        IEnumerable<ResponseAnswerQuestionContract> questions,
        IEnumerable<ResponseAnswerValueContract> answers)
    {
        return ValidateValues(questions, answers);
    }

    private static Result<bool> ValidateValues(
        IEnumerable<ResponseAnswerQuestionContract> questions,
        IEnumerable<ResponseAnswerValueContract> answers)
    {
        var questionById = questions.ToDictionary(question => question.Id);

        foreach (var answer in answers)
        {
            if (!questionById.TryGetValue(answer.QuestionId, out var question))
            {
                return Failure("unknown", "references a question outside this campaign template");
            }

            if (answer.IsSkipped && answer.IsNa)
            {
                return Failure(question.Code, "cannot be both skipped and not applicable");
            }

            if ((answer.IsSkipped || answer.IsNa) &&
                (answer.Value is not null || !string.IsNullOrWhiteSpace(answer.Comment)))
            {
                return Failure(question.Code, "cannot carry a value or comment when marked skipped or not applicable");
            }

            if (answer.IsSkipped)
            {
                continue;
            }

            if (answer.IsNa)
            {
                if (!QuestionTypes.RequiresScale(question.Type) || !question.ScaleNaAllowed)
                {
                    return Failure(question.Code, "does not allow a not-applicable answer");
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(answer.Value))
            {
                continue;
            }

            JsonElement value;
            try
            {
                using var document = JsonDocument.Parse(answer.Value);
                value = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return Failure(question.Code, "must be valid JSON");
            }

            var result = question.Type switch
            {
                QuestionTypes.SingleChoice => ValidateSingleChoice(question, value),
                QuestionTypes.MultiChoice => ValidateChoiceArray(question, value, "multiple-choice", enforceExclusive: true),
                QuestionTypes.Ranking => ValidateChoiceArray(question, value, "ranking", enforceRankingLimit: true),
                QuestionTypes.Matrix => ValidateMatrix(question, value),
                QuestionTypes.Likert or QuestionTypes.Nps => ValidateScale(question, value),
                QuestionTypes.Number => ValidateNumber(question, value),
                QuestionTypes.Text => ValidateText(question, value),
                QuestionTypes.Date => ValidateDate(question, value),
                _ => Failure(question.Code, "uses an answer type that is not supported by the current response runtime")
            };

            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateSingleChoice(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return Failure(question.Code, "must be one selected option");
        }

        var selected = value.GetString();
        if (string.IsNullOrWhiteSpace(selected) ||
            !ReadOptionCodes(ParseObject(question.Payload), "options").Contains(selected.Trim()))
        {
            return Failure(question.Code, "uses an option that is not defined for this question");
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateChoiceArray(
        ResponseAnswerQuestionContract question,
        JsonElement value,
        string label,
        bool enforceExclusive = false,
        bool enforceRankingLimit = false)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Failure(question.Code, $"must be a {label} array");
        }

        var payload = ParseObject(question.Payload);
        var validOptions = ReadOptionCodes(payload, "options");
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(item.GetString()) ||
                !validOptions.Contains(item.GetString()!.Trim()) ||
                !selected.Add(item.GetString()!.Trim()))
            {
                return Failure(question.Code, $"uses a {label} option that is not defined for this question");
            }
        }

        if (selected.Count == 0)
        {
            return Failure(question.Code, $"must include at least one {label} option when a value is saved");
        }

        if (enforceExclusive)
        {
            var exclusiveOptions = ReadExclusiveOptionCodes(payload);
            if (selected.Count > 1 && selected.Any(exclusiveOptions.Contains))
            {
                return Failure(question.Code, "uses an exclusive option together with another option");
            }
        }

        if (enforceRankingLimit)
        {
            var topN = ReadRankingTopN(payload);
            if (topN.HasValue && selected.Count > topN.Value)
            {
                return Failure(question.Code, $"must include at most the top {topN.Value} ranked options");
            }
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateMatrix(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return Failure(question.Code, "must be a matrix row-to-column object");
        }

        var payload = ParseObject(question.Payload);
        if (!payload.TryGetProperty("matrix", out var matrix) ||
            matrix.ValueKind != JsonValueKind.Object)
        {
            return Failure(question.Code, "does not have matrix metadata");
        }

        var rows = ReadOptionCodes(matrix, "rows");
        var columns = ReadOptionCodes(matrix, "columns");
        var answeredRows = 0;

        foreach (var property in value.EnumerateObject())
        {
            if (!rows.Contains(property.Name) ||
                property.Value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(property.Value.GetString()) ||
                !columns.Contains(property.Value.GetString()!.Trim()))
            {
                return Failure(question.Code, "uses a matrix row or column that is not defined for this question");
            }

            answeredRows++;
        }

        if (answeredRows == 0)
        {
            return Failure(question.Code, "must include at least one answered matrix row when a value is saved");
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateScale(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDecimal(out var selected))
        {
            return Failure(question.Code, "must be a numeric scale value");
        }

        if (selected != decimal.Truncate(selected))
        {
            return Failure(question.Code, "must be a whole-number scale value");
        }

        if (question.ScaleMinValue.HasValue && selected < question.ScaleMinValue.Value)
        {
            return Failure(question.Code, "is below the configured scale minimum");
        }

        if (question.ScaleMaxValue.HasValue && selected > question.ScaleMaxValue.Value)
        {
            return Failure(question.Code, "is above the configured scale maximum");
        }

        if (question.ScaleMinValue.HasValue &&
            question.ScaleStep.HasValue &&
            question.ScaleStep.Value > 0 &&
            (selected - question.ScaleMinValue.Value) % question.ScaleStep.Value != 0)
        {
            return Failure(question.Code, "does not match the configured scale step");
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateNumber(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDecimal(out var number))
        {
            return Failure(question.Code, "must be a numeric value");
        }

        var payload = ParseObject(question.Payload);
        if (!payload.TryGetProperty("validation", out var validation) ||
            validation.ValueKind != JsonValueKind.Object)
        {
            return Result.Success(true);
        }

        var min = ReadNullableDecimal(validation, "min");
        if (min.HasValue && number < min.Value)
        {
            return Failure(question.Code, "is below the configured minimum");
        }

        var max = ReadNullableDecimal(validation, "max");
        if (max.HasValue && number > max.Value)
        {
            return Failure(question.Code, "is above the configured maximum");
        }

        if (ReadBoolean(validation, "integerOnly") && number != decimal.Truncate(number))
        {
            return Failure(question.Code, "must be a whole number");
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateText(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return Failure(question.Code, "must be text");
        }

        if (string.IsNullOrWhiteSpace(value.GetString()))
        {
            return Failure(question.Code, "must contain text when a text value is saved");
        }

        var payload = ParseObject(question.Payload);
        if (!payload.TryGetProperty("text", out var text) ||
            text.ValueKind != JsonValueKind.Object ||
            !text.TryGetProperty("maxLength", out var maxLength) ||
            maxLength.ValueKind != JsonValueKind.Number ||
            !maxLength.TryGetInt32(out var maxLengthValue))
        {
            return Result.Success(true);
        }

        if (maxLengthValue > 0 && value.GetString()!.Length > maxLengthValue)
        {
            return Failure(question.Code, "exceeds the configured text length");
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateDate(
        ResponseAnswerQuestionContract question,
        JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            return Failure(question.Code, "must be a date string");
        }

        var date = value.GetString()!.Trim();
        if (!TryParseIsoCalendarDate(date, out var parsedDate))
        {
            return Failure(question.Code, "must use YYYY-MM-DD date format");
        }

        var payload = ParseObject(question.Payload);
        if (!payload.TryGetProperty("validation", out var validation) ||
            validation.ValueKind != JsonValueKind.Object)
        {
            return Result.Success(true);
        }

        var minDate = ReadString(validation, "minDate");
        if (!string.IsNullOrWhiteSpace(minDate) &&
            TryParseIsoCalendarDate(minDate, out var parsedMinDate) &&
            parsedDate < parsedMinDate)
        {
            return Failure(question.Code, "is before the configured earliest date");
        }

        var maxDate = ReadString(validation, "maxDate");
        if (!string.IsNullOrWhiteSpace(maxDate) &&
            TryParseIsoCalendarDate(maxDate, out var parsedMaxDate) &&
            parsedDate > parsedMaxDate)
        {
            return Failure(question.Code, "is after the configured latest date");
        }

        return Result.Success(true);
    }

    private static JsonElement ParseObject(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.Clone()
                : JsonDocument.Parse("{}").RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
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

    private static HashSet<string> ReadExclusiveOptionCodes(JsonElement payload)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("options", out var options) ||
            options.ValueKind != JsonValueKind.Array)
        {
            return codes;
        }

        foreach (var option in options.EnumerateArray())
        {
            var code = ReadString(option, "code");
            if (!string.IsNullOrWhiteSpace(code) && ReadBoolean(option, "exclusive"))
            {
                codes.Add(code.Trim());
            }
        }

        return codes;
    }

    private static int? ReadRankingTopN(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("ranking", out var ranking) ||
            ranking.ValueKind != JsonValueKind.Object ||
            ReadString(ranking, "mode") != "top_n" ||
            !ranking.TryGetProperty("topN", out var topN) ||
            !topN.TryGetInt32(out var topNValue) ||
            topNValue < 1)
        {
            return null;
        }

        return topNValue;
    }

    private static decimal? ReadNullableDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.TryGetDecimal(out var value) ? value : null;
    }

    private static bool ReadBoolean(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind is JsonValueKind.True;
    }

    private static bool TryParseIsoCalendarDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static Result<bool> Failure(string questionCode, string reason)
    {
        return Result.Failure<bool>(
            Error.Validation(
                "answer.value_invalid",
                $"Question '{questionCode}' answer {reason}."));
    }
}
