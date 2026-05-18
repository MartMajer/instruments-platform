using System.Globalization;
using System.Text.Json;
using Platform.SharedKernel;

namespace Platform.Domain.Scoring;

public static class SimpleScoringEngine
{
    public static Result<IReadOnlyList<SimpleScoreOutput>> Evaluate(
        string scoringDocument,
        IReadOnlyList<SimpleScoreInput> inputs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scoringDocument);
        ArgumentNullException.ThrowIfNull(inputs);

        using JsonDocument document = JsonDocument.Parse(scoringDocument);
        var answersByCode = inputs.ToDictionary(
            input => NormalizeCode(input.QuestionCode),
            input => input);

        if (document.RootElement.TryGetProperty("operations", out var operations) &&
            operations.ValueKind == JsonValueKind.Array)
        {
            return EvaluateLegacyOperations(operations, answersByCode);
        }

        if (document.RootElement.TryGetProperty("nodes", out _) ||
            document.RootElement.TryGetProperty("inputs", out _) ||
            document.RootElement.TryGetProperty("outputs", out _))
        {
            return EvaluateGraph(document.RootElement, answersByCode);
        }

        return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(
            Error.Validation("score.operations_missing", "Scoring document must contain an operations array."));
    }

    private static Result<IReadOnlyList<SimpleScoreOutput>> EvaluateLegacyOperations(
        JsonElement operations,
        IReadOnlyDictionary<string, SimpleScoreInput> answersByCode)
    {
        var outputs = new List<SimpleScoreOutput>();

        foreach (var operation in operations.EnumerateArray())
        {
            var operationResult = EvaluateLegacyOperation(operation, answersByCode);
            if (operationResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(operationResult.Error);
            }

            outputs.Add(operationResult.Value);
        }

        return Result.Success<IReadOnlyList<SimpleScoreOutput>>(outputs);
    }

    private static Result<SimpleScoreOutput> EvaluateLegacyOperation(
        JsonElement operation,
        IReadOnlyDictionary<string, SimpleScoreInput> answersByCode)
    {
        if (!operation.TryGetProperty("op", out var opElement) ||
            opElement.ValueKind != JsonValueKind.String)
        {
            return Result.Failure<SimpleScoreOutput>(
                Error.Validation("score.operation_missing", "Scoring operation must declare an op."));
        }

        var op = opElement.GetString();
        if (op is not ("mean" or "sum"))
        {
            return Result.Failure<SimpleScoreOutput>(
                Error.Validation("score.operation_unsupported", $"Scoring operation '{op}' is not supported."));
        }

        if (!operation.TryGetProperty("output", out var outputElement) ||
            outputElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(outputElement.GetString()))
        {
            return Result.Failure<SimpleScoreOutput>(
                Error.Validation("score.output_missing", "Scoring operation must declare an output."));
        }

        if (!operation.TryGetProperty("items", out var itemsElement) ||
            itemsElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<SimpleScoreOutput>(
                Error.Validation("score.items_missing", "Scoring operation must declare item codes."));
        }

        var values = new List<decimal>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(item.GetString()))
            {
                return Result.Failure<SimpleScoreOutput>(
                    Error.Validation("score.item_invalid", "Scoring item code must be a string."));
            }

            var code = NormalizeCode(item.GetString()!);
            if (!answersByCode.TryGetValue(code, out var input) ||
                string.IsNullOrWhiteSpace(input.Value) ||
                input.IsSkipped ||
                input.IsNa)
            {
                return Result.Failure<SimpleScoreOutput>(
                    Error.Validation("score.answer_missing", $"No scoreable answer exists for '{code}'."));
            }

            if (!TryParseNumericJson(input.Value, out var parsed))
            {
                return Result.Failure<SimpleScoreOutput>(
                    Error.Validation("score.answer_not_numeric", $"Answer for '{code}' is not numeric."));
            }

            values.Add(parsed);
        }

        if (values.Count == 0)
        {
            return Result.Failure<SimpleScoreOutput>(
                Error.Validation("score.items_missing", "Scoring operation must declare at least one item."));
        }

        var value = op == "mean"
            ? values.Sum() / values.Count
            : values.Sum();

        return Result.Success(new SimpleScoreOutput(
            outputElement.GetString()!.Trim(),
            Math.Round(value, 4, MidpointRounding.AwayFromZero),
            values.Count,
            values.Count,
            ScoreMissingPolicyStatuses.Ok));
    }

    private static Result<IReadOnlyList<SimpleScoreOutput>> EvaluateGraph(
        JsonElement root,
        IReadOnlyDictionary<string, SimpleScoreInput> answersByCode)
    {
        var inputsResult = ReadInputs(root);
        if (inputsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(inputsResult.Error);
        }

        var scalesResult = ReadScales(root);
        if (scalesResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(scalesResult.Error);
        }

        var missingPolicyResult = ReadMissingPolicy(root);
        if (missingPolicyResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(missingPolicyResult.Error);
        }

        if (!root.TryGetProperty("nodes", out var nodesElement) ||
            nodesElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(
                Error.Validation("score.nodes_missing", "Scoring graph must contain a nodes array."));
        }

        if (!root.TryGetProperty("outputs", out var outputsElement) ||
            outputsElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(
                Error.Validation("score.outputs_missing", "Scoring graph must contain an outputs array."));
        }

        var nodeValues = new Dictionary<string, ScoreNodeValue>(StringComparer.Ordinal);
        foreach (var node in nodesElement.EnumerateArray())
        {
            var nodeIdResult = ReadRequiredString(
                node,
                "id",
                "score.node_id_missing",
                "Scoring node must declare an id.");
            if (nodeIdResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(nodeIdResult.Error);
            }

            var nodeId = NormalizeCode(nodeIdResult.Value);
            if (nodeValues.ContainsKey(nodeId))
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(
                    Error.Validation("score.node_duplicate", $"Scoring node '{nodeId}' is duplicated."));
            }

            var nodeValueResult = EvaluateNode(
                node,
                inputsResult.Value,
                scalesResult.Value,
                missingPolicyResult.Value,
                answersByCode,
                nodeValues);
            if (nodeValueResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(nodeValueResult.Error);
            }

            nodeValues.Add(nodeId, nodeValueResult.Value);
        }

        var outputs = new List<SimpleScoreOutput>();
        foreach (var output in outputsElement.EnumerateArray())
        {
            var codeResult = ReadRequiredString(
                output,
                "code",
                "score.output_missing",
                "Scoring output must declare a code.");
            if (codeResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(codeResult.Error);
            }

            var nodeResult = ReadRequiredString(
                output,
                "node",
                "score.output_node_missing",
                "Scoring output must reference a node.");
            if (nodeResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(nodeResult.Error);
            }

            var resolved = ResolveNodeValue(nodeResult.Value, nodeValues);
            if (resolved.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(resolved.Error);
            }

            if (resolved.Value is not ScoreScalar scalar)
            {
                return Result.Failure<IReadOnlyList<SimpleScoreOutput>>(
                    Error.Validation("score.output_type_invalid", "Scoring output must reference a numeric node."));
            }

            outputs.Add(new SimpleScoreOutput(
                codeResult.Value.Trim(),
                Math.Round(scalar.Value, 4, MidpointRounding.AwayFromZero),
                scalar.N,
                scalar.NExpected,
                scalar.MissingPolicyStatus));
        }

        return Result.Success<IReadOnlyList<SimpleScoreOutput>>(outputs);
    }

    private static Result<ScoreNodeValue> EvaluateNode(
        JsonElement node,
        IReadOnlyDictionary<string, AnswerInput> inputs,
        IReadOnlyDictionary<string, ScaleDefinition> scales,
        MissingPolicy missingPolicy,
        IReadOnlyDictionary<string, SimpleScoreInput> answersByCode,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var opResult = ReadRequiredString(
            node,
            "op",
            "score.operation_missing",
            "Scoring node must declare an op.");
        if (opResult.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(opResult.Error);
        }

        return NormalizeCode(opResult.Value) switch
        {
            "select_answers" => EvaluateSelectAnswers(node, inputs, answersByCode),
            "reverse_code" => EvaluateReverseCode(node, scales, nodeValues),
            "mean" => EvaluateAggregate(node, "mean", missingPolicy, nodeValues),
            "sum" => EvaluateAggregate(node, "sum", missingPolicy, nodeValues),
            "subscale_aggregate" => EvaluateSubscaleAggregate(node, missingPolicy, nodeValues),
            "count_valid" => EvaluateCountValid(node, nodeValues),
            var op => Result.Failure<ScoreNodeValue>(
                Error.Validation("score.operation_unsupported", $"Scoring operation '{op}' is not supported."))
        };
    }

    private static Result<ScoreNodeValue> EvaluateSelectAnswers(
        JsonElement node,
        IReadOnlyDictionary<string, AnswerInput> inputs,
        IReadOnlyDictionary<string, SimpleScoreInput> answersByCode)
    {
        var inputResult = ReadRequiredString(
            node,
            "input",
            "score.input_missing",
            "select_answers must reference an input.");
        if (inputResult.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(inputResult.Error);
        }

        var inputId = NormalizeCode(inputResult.Value);
        if (!inputs.TryGetValue(inputId, out var input))
        {
            return Result.Failure<ScoreNodeValue>(
                Error.Validation("score.input_unknown", $"Scoring input '{inputId}' was not found."));
        }

        var entries = new List<ScoreVectorEntry>();
        foreach (var item in input.Items)
        {
            var code = NormalizeCode(item);
            if (!answersByCode.TryGetValue(code, out var answer) ||
                string.IsNullOrWhiteSpace(answer.Value) ||
                answer.IsSkipped ||
                answer.IsNa)
            {
                entries.Add(new ScoreVectorEntry(code, null));
                continue;
            }

            if (!TryParseNumericJson(answer.Value, out var parsed))
            {
                return Result.Failure<ScoreNodeValue>(
                    Error.Validation("score.answer_not_numeric", $"Answer for '{code}' is not numeric."));
            }

            entries.Add(new ScoreVectorEntry(code, parsed));
        }

        return Result.Success<ScoreNodeValue>(new ScoreVector(entries));
    }

    private static Result<ScoreNodeValue> EvaluateReverseCode(
        JsonElement node,
        IReadOnlyDictionary<string, ScaleDefinition> scales,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var input = ResolveInputVector(node, nodeValues);
        if (input.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(input.Error);
        }

        var scaleResult = ReadRequiredString(
            node,
            "scale",
            "score.scale_missing",
            "reverse_code must reference a scale.");
        if (scaleResult.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(scaleResult.Error);
        }

        var scaleId = NormalizeCode(scaleResult.Value);
        if (!scales.TryGetValue(scaleId, out var scale))
        {
            return Result.Failure<ScoreNodeValue>(
                Error.Validation("score.scale_missing", $"Scoring scale '{scaleId}' was not found."));
        }

        var source = "explicit_list";
        if (node.TryGetProperty("reverse_flag_source", out var sourceElement) &&
            sourceElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(sourceElement.GetString()))
        {
            source = NormalizeCode(sourceElement.GetString()!);
        }

        var explicitItems = ReadOptionalStringSet(node, "explicit_reverse_items");
        var reverseAll = source == "all";
        if (source is not ("all" or "explicit_list"))
        {
            return Result.Failure<ScoreNodeValue>(
                Error.Validation("score.reverse_source_unsupported", $"Reverse flag source '{source}' is not supported."));
        }

        var entries = input.Value.Entries
            .Select(entry =>
            {
                if (!entry.Value.HasValue ||
                    (!reverseAll && !explicitItems.Contains(entry.ItemCode)))
                {
                    return entry;
                }

                return entry with { Value = scale.Min + scale.Max - entry.Value.Value };
            })
            .ToArray();

        return Result.Success<ScoreNodeValue>(new ScoreVector(entries));
    }

    private static Result<ScoreNodeValue> EvaluateSubscaleAggregate(
        JsonElement node,
        MissingPolicy missingPolicy,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var aggregatorResult = ReadRequiredString(
            node,
            "aggregator",
            "score.aggregator_missing",
            "subscale_aggregate must declare an aggregator.");
        if (aggregatorResult.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(aggregatorResult.Error);
        }

        return NormalizeCode(aggregatorResult.Value) switch
        {
            "mean" => EvaluateAggregate(node, "mean", missingPolicy, nodeValues),
            "sum" => EvaluateAggregate(node, "sum", missingPolicy, nodeValues),
            var aggregator => Result.Failure<ScoreNodeValue>(
                Error.Validation("score.aggregator_unsupported", $"Subscale aggregator '{aggregator}' is not supported."))
        };
    }

    private static Result<ScoreNodeValue> EvaluateAggregate(
        JsonElement node,
        string aggregate,
        MissingPolicy missingPolicy,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var input = ResolveInputVector(node, nodeValues);
        if (input.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(input.Error);
        }

        var entries = input.Value.Entries;
        var values = entries
            .Where(entry => entry.Value.HasValue)
            .Select(entry => entry.Value!.Value)
            .ToArray();

        if (values.Length == 0 ||
            missingPolicy.Strategy == MissingPolicyStrategies.RequireAll &&
            values.Length != entries.Count ||
            missingPolicy.Strategy == MissingPolicyStrategies.MinValidCount &&
            values.Length < missingPolicy.MinValidCount.GetValueOrDefault())
        {
            return Result.Failure<ScoreNodeValue>(
                Error.Validation("score.answer_missing", "Not enough scoreable answers exist for this scoring operation."));
        }

        var value = aggregate == "mean"
            ? values.Sum() / values.Length
            : values.Sum();

        return Result.Success<ScoreNodeValue>(new ScoreScalar(
            value,
            values.Length,
            entries.Count,
            ScoreMissingPolicyStatuses.Ok));
    }

    private static Result<ScoreNodeValue> EvaluateCountValid(
        JsonElement node,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var input = ResolveInputVector(node, nodeValues);
        if (input.IsFailure)
        {
            return Result.Failure<ScoreNodeValue>(input.Error);
        }

        var count = input.Value.Entries.Count(entry => entry.Value.HasValue);

        return Result.Success<ScoreNodeValue>(new ScoreScalar(
            count,
            count,
            input.Value.Entries.Count,
            ScoreMissingPolicyStatuses.Ok));
    }

    private static Result<ScoreVector> ResolveInputVector(
        JsonElement node,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var inputResult = ReadRequiredString(
            node,
            "input",
            "score.input_missing",
            "Scoring operation must reference an input node.");
        if (inputResult.IsFailure)
        {
            return Result.Failure<ScoreVector>(inputResult.Error);
        }

        var resolved = ResolveNodeValue(inputResult.Value, nodeValues);
        if (resolved.IsFailure)
        {
            return Result.Failure<ScoreVector>(resolved.Error);
        }

        return resolved.Value is ScoreVector vector
            ? Result.Success(vector)
            : Result.Failure<ScoreVector>(
                Error.Validation("score.input_type_invalid", "Scoring operation must reference a vector node."));
    }

    private static Result<ScoreNodeValue> ResolveNodeValue(
        string nodeId,
        IReadOnlyDictionary<string, ScoreNodeValue> nodeValues)
    {
        var normalized = NormalizeCode(nodeId);

        return nodeValues.TryGetValue(normalized, out var nodeValue)
            ? Result.Success(nodeValue)
            : Result.Failure<ScoreNodeValue>(
                Error.Validation("score.node_unknown", $"Scoring node '{normalized}' was not found."));
    }

    private static Result<IReadOnlyDictionary<string, AnswerInput>> ReadInputs(JsonElement root)
    {
        if (!root.TryGetProperty("inputs", out var inputsElement) ||
            inputsElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                Error.Validation("score.inputs_missing", "Scoring graph must contain an inputs array."));
        }

        var inputs = new Dictionary<string, AnswerInput>(StringComparer.Ordinal);
        foreach (var input in inputsElement.EnumerateArray())
        {
            var idResult = ReadRequiredString(
                input,
                "id",
                "score.input_id_missing",
                "Scoring input must declare an id.");
            if (idResult.IsFailure)
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(idResult.Error);
            }

            var kindResult = ReadRequiredString(
                input,
                "kind",
                "score.input_kind_missing",
                "Scoring input must declare a kind.");
            if (kindResult.IsFailure)
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(kindResult.Error);
            }

            var inputId = NormalizeCode(idResult.Value);
            if (NormalizeCode(kindResult.Value) != "answers")
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                    Error.Validation("score.input_kind_unsupported", "Only answers inputs are supported."));
            }

            if (inputs.ContainsKey(inputId))
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                    Error.Validation("score.input_duplicate", $"Scoring input '{inputId}' is duplicated."));
            }

            if (!input.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                    Error.Validation("score.items_missing", "Answers input must declare item codes."));
            }

            var items = new List<string>();
            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(item.GetString()))
                {
                    return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                        Error.Validation("score.item_invalid", "Scoring item code must be a string."));
                }

                items.Add(NormalizeCode(item.GetString()!));
            }

            if (items.Count == 0)
            {
                return Result.Failure<IReadOnlyDictionary<string, AnswerInput>>(
                    Error.Validation("score.items_missing", "Answers input must declare at least one item."));
            }

            inputs.Add(inputId, new AnswerInput(items));
        }

        return Result.Success<IReadOnlyDictionary<string, AnswerInput>>(inputs);
    }

    private static Result<IReadOnlyDictionary<string, ScaleDefinition>> ReadScales(JsonElement root)
    {
        var scales = new Dictionary<string, ScaleDefinition>(StringComparer.Ordinal);
        if (!root.TryGetProperty("scale_defaults", out var scalesElement) ||
            scalesElement.ValueKind == JsonValueKind.Undefined)
        {
            return Result.Success<IReadOnlyDictionary<string, ScaleDefinition>>(scales);
        }

        if (scalesElement.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<IReadOnlyDictionary<string, ScaleDefinition>>(
                Error.Validation("score.scale_invalid", "Scoring scale defaults must be an object."));
        }

        foreach (var scaleProperty in scalesElement.EnumerateObject())
        {
            var scaleId = NormalizeCode(scaleProperty.Name);
            var scale = scaleProperty.Value;
            if (!scale.TryGetProperty("min", out var minElement) ||
                !minElement.TryGetDecimal(out var min) ||
                !scale.TryGetProperty("max", out var maxElement) ||
                !maxElement.TryGetDecimal(out var max) ||
                min >= max)
            {
                return Result.Failure<IReadOnlyDictionary<string, ScaleDefinition>>(
                    Error.Validation("score.scale_invalid", $"Scoring scale '{scaleId}' must declare min < max."));
            }

            scales.Add(scaleId, new ScaleDefinition(min, max));
        }

        return Result.Success<IReadOnlyDictionary<string, ScaleDefinition>>(scales);
    }

    private static Result<MissingPolicy> ReadMissingPolicy(JsonElement root)
    {
        if (!root.TryGetProperty("missing_data", out var missingData) ||
            !missingData.TryGetProperty("defaults", out var defaults))
        {
            return Result.Success(new MissingPolicy(MissingPolicyStrategies.RequireAll, null));
        }

        var strategy = MissingPolicyStrategies.RequireAll;
        if (defaults.TryGetProperty("strategy", out var strategyElement) &&
            strategyElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(strategyElement.GetString()))
        {
            strategy = NormalizeCode(strategyElement.GetString()!);
        }

        return strategy switch
        {
            MissingPolicyStrategies.RequireAll => Result.Success(new MissingPolicy(strategy, null)),
            MissingPolicyStrategies.MinValidCount => ReadMinValidCount(defaults),
            _ => Result.Failure<MissingPolicy>(
                Error.Validation("score.missing_policy_unsupported", $"Missing-data strategy '{strategy}' is not supported."))
        };
    }

    private static Result<MissingPolicy> ReadMinValidCount(JsonElement defaults)
    {
        if (!defaults.TryGetProperty("min_valid_count", out var countElement) ||
            !countElement.TryGetInt32(out var minValidCount) ||
            minValidCount < 1)
        {
            return Result.Failure<MissingPolicy>(
                Error.Validation("score.missing_policy_invalid", "min_valid_count must be a positive integer."));
        }

        return Result.Success(new MissingPolicy(MissingPolicyStrategies.MinValidCount, minValidCount));
    }

    private static HashSet<string> ReadOptionalStringSet(JsonElement element, string propertyName)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        if (!element.TryGetProperty(propertyName, out var arrayElement) ||
            arrayElement.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(item.GetString()))
            {
                values.Add(NormalizeCode(item.GetString()!));
            }
        }

        return values;
    }

    private static Result<string> ReadRequiredString(
        JsonElement element,
        string propertyName,
        string errorCode,
        string errorMessage)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString())
            ? Result.Success(property.GetString()!.Trim())
            : Result.Failure<string>(Error.Validation(errorCode, errorMessage));
    }

    private static bool TryParseNumericJson(string value, out decimal parsed)
    {
        parsed = 0m;

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Number => root.TryGetDecimal(out parsed),
                JsonValueKind.String => decimal.TryParse(
                    root.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out parsed),
                _ => false
            };
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return code.Trim().ToLowerInvariant();
    }

    private abstract record ScoreNodeValue;

    private sealed record ScoreVector(IReadOnlyList<ScoreVectorEntry> Entries) : ScoreNodeValue;

    private sealed record ScoreVectorEntry(string ItemCode, decimal? Value);

    private sealed record ScoreScalar(
        decimal Value,
        int N,
        int NExpected,
        string MissingPolicyStatus) : ScoreNodeValue;

    private sealed record AnswerInput(IReadOnlyList<string> Items);

    private sealed record ScaleDefinition(decimal Min, decimal Max);

    private sealed record MissingPolicy(string Strategy, int? MinValidCount);

    private static class MissingPolicyStrategies
    {
        public const string RequireAll = "require_all";
        public const string MinValidCount = "min_valid_count";
    }

}
