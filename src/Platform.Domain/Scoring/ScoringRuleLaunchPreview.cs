using System.Text.Json;
using Platform.SharedKernel;

namespace Platform.Domain.Scoring;

public sealed record ScoringRuleLaunchPreviewInput(
    string QuestionCode,
    string Value);

public sealed record ScoringRuleLaunchPreviewResult(
    int ReferencedItemCount,
    int OutputCount);

public static class ScoringRuleLaunchPreview
{
    public static Result<ScoringRuleLaunchPreviewResult> Evaluate(
        string scoringDocument,
        IReadOnlyList<ScoringRuleLaunchPreviewInput> templateInputs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scoringDocument);
        ArgumentNullException.ThrowIfNull(templateInputs);

        var previewInputByCode = new Dictionary<string, SimpleScoreInput>(StringComparer.Ordinal);
        foreach (var input in templateInputs)
        {
            if (string.IsNullOrWhiteSpace(input.QuestionCode))
            {
                return PreviewFailure();
            }

            var normalized = NormalizeCode(input.QuestionCode);
            previewInputByCode[normalized] = new SimpleScoreInput(normalized, input.Value);
        }

        IReadOnlyList<string> itemCodes;
        try
        {
            using var document = JsonDocument.Parse(scoringDocument);
            var itemCodesResult = ReadReferencedItemCodes(document.RootElement);
            if (itemCodesResult.IsFailure)
            {
                return Result.Failure<ScoringRuleLaunchPreviewResult>(itemCodesResult.Error);
            }

            itemCodes = itemCodesResult.Value;
        }
        catch (JsonException)
        {
            return PreviewFailure();
        }

        var missingItemExists = itemCodes
            .Distinct(StringComparer.Ordinal)
            .Any(itemCode => !previewInputByCode.ContainsKey(itemCode));
        if (missingItemExists)
        {
            return Result.Failure<ScoringRuleLaunchPreviewResult>(
                Error.Validation(
                    "scoring_rule.item_code_missing",
                    "Selected scoring rule references item codes that do not exist in the selected template."));
        }

        Result<IReadOnlyList<SimpleScoreOutput>> preview;
        try
        {
            preview = SimpleScoringEngine.Evaluate(
                scoringDocument,
                previewInputByCode.Values.ToArray());
        }
        catch (ArgumentException)
        {
            return PreviewFailure();
        }
        catch (JsonException)
        {
            return PreviewFailure();
        }

        return preview.IsFailure
            ? PreviewFailure()
            : Result.Success(new ScoringRuleLaunchPreviewResult(
                itemCodes.Distinct(StringComparer.Ordinal).Count(),
                preview.Value.Count));
    }

    private static Result<IReadOnlyList<string>> ReadReferencedItemCodes(JsonElement root)
    {
        if (root.TryGetProperty("operations", out var operations) &&
            operations.ValueKind == JsonValueKind.Array)
        {
            return ReadLegacyItemCodes(operations);
        }

        if (root.TryGetProperty("inputs", out var inputs) &&
            inputs.ValueKind == JsonValueKind.Array)
        {
            return ReadGraphItemCodes(inputs);
        }

        return PreviewFailure<IReadOnlyList<string>>();
    }

    private static Result<IReadOnlyList<string>> ReadLegacyItemCodes(JsonElement operations)
    {
        var itemCodes = new List<string>();
        foreach (var operation in operations.EnumerateArray())
        {
            if (operation.ValueKind != JsonValueKind.Object ||
                !operation.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return PreviewFailure<IReadOnlyList<string>>();
            }

            var parsed = ReadStringArray(items);
            if (parsed.IsFailure)
            {
                return Result.Failure<IReadOnlyList<string>>(parsed.Error);
            }

            itemCodes.AddRange(parsed.Value);
        }

        return itemCodes.Count == 0
            ? PreviewFailure<IReadOnlyList<string>>()
            : Result.Success<IReadOnlyList<string>>(itemCodes);
    }

    private static Result<IReadOnlyList<string>> ReadGraphItemCodes(JsonElement inputs)
    {
        var itemCodes = new List<string>();
        foreach (var input in inputs.EnumerateArray())
        {
            if (input.ValueKind != JsonValueKind.Object)
            {
                return PreviewFailure<IReadOnlyList<string>>();
            }

            if (!input.TryGetProperty("kind", out var kind) ||
                kind.ValueKind != JsonValueKind.String ||
                NormalizeCode(kind.GetString()!) != "answers")
            {
                continue;
            }

            if (!input.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return PreviewFailure<IReadOnlyList<string>>();
            }

            var parsed = ReadStringArray(items);
            if (parsed.IsFailure)
            {
                return Result.Failure<IReadOnlyList<string>>(parsed.Error);
            }

            itemCodes.AddRange(parsed.Value);
        }

        return itemCodes.Count == 0
            ? PreviewFailure<IReadOnlyList<string>>()
            : Result.Success<IReadOnlyList<string>>(itemCodes);
    }

    private static Result<IReadOnlyList<string>> ReadStringArray(JsonElement array)
    {
        var values = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(item.GetString()))
            {
                return PreviewFailure<IReadOnlyList<string>>();
            }

            values.Add(NormalizeCode(item.GetString()!));
        }

        return values.Count == 0
            ? PreviewFailure<IReadOnlyList<string>>()
            : Result.Success<IReadOnlyList<string>>(values);
    }

    private static Result<ScoringRuleLaunchPreviewResult> PreviewFailure()
    {
        return PreviewFailure<ScoringRuleLaunchPreviewResult>();
    }

    private static Result<T> PreviewFailure<T>()
    {
        return Result.Failure<T>(
            Error.Validation(
                "scoring_rule.preview_failed",
                "Selected scoring rule failed deterministic launch preview through the current scoring engine."));
    }

    private static string NormalizeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim().ToLowerInvariant();
    }
}
