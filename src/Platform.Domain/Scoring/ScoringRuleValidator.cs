using System.Text.Json;
using Platform.SharedKernel;

namespace Platform.Domain.Scoring;

public sealed record ScoringRuleValidationRequest(
    string RuleKey,
    string RuleVersion,
    string SchemaVersion,
    string EngineMinVersion,
    string Document,
    string Produces,
    string Compatibility);

public sealed record ScoringRuleValidationSummary(IReadOnlyList<string> ScoreCodes);

public static class ScoringRuleValidator
{
    private enum GraphValueType
    {
        Vector,
        Scalar
    }

    private sealed record GraphMissingPolicy(string Strategy, int? MinValidCount);

    private sealed record GraphInputMetadata(HashSet<string> Items)
    {
        public int Count => Items.Count;
    }

    public static Result<ScoringRuleValidationSummary> Validate(
        ScoringRuleValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documentResult = ParseObject(request.Document, "score.rule_invalid_json", "document");
        if (documentResult.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(documentResult.Error);
        }

        using var document = documentResult.Value;

        var producesResult = ParseObject(request.Produces, "score.rule_produces_invalid", "produces");
        if (producesResult.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(producesResult.Error);
        }

        using var produces = producesResult.Value;

        var compatibilityResult = ParseObject(
            request.Compatibility,
            "score.rule_compatibility_invalid",
            "compatibility");
        if (compatibilityResult.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(compatibilityResult.Error);
        }

        using var compatibility = compatibilityResult.Value;

        var scoreCodesResult = ReadProducesScores(produces.RootElement);
        if (scoreCodesResult.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(scoreCodesResult.Error);
        }

        var interpretationResult = ScoreInterpretationMetadataParser.Parse(
            produces.RootElement,
            scoreCodesResult.Value);
        if (interpretationResult.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(interpretationResult.Error);
        }

        Result<ScoringRuleValidationSummary> summaryResult;
        if (IsGraphRule(document.RootElement))
        {
            summaryResult = ValidateGraphMetadata(request, document.RootElement, scoreCodesResult.Value);
        }
        else if (IsLegacyOperationsRule(document.RootElement))
        {
            summaryResult = ValidateLegacyOperations(document.RootElement, scoreCodesResult.Value);
        }
        else
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation("score.rule_outputs_missing", "Scoring rule must declare score outputs."));
        }

        if (summaryResult.IsFailure)
        {
            return summaryResult;
        }

        var compatibilityValidation = ValidateCompatibility(
            compatibility.RootElement,
            summaryResult.Value.ScoreCodes);
        if (compatibilityValidation.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(compatibilityValidation.Error);
        }

        return summaryResult;
    }

    private static Result<ScoringRuleValidationSummary> ValidateGraphMetadata(
        ScoringRuleValidationRequest request,
        JsonElement document,
        IReadOnlyList<string> producesScores)
    {
        var ruleId = ReadRequiredString(
            document,
            "rule_id",
            "score.rule_metadata_missing",
            "Scoring rule document must declare rule_id.");
        if (ruleId.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(ruleId.Error);
        }

        var ruleVersion = ReadRequiredString(
            document,
            "rule_version",
            "score.rule_metadata_missing",
            "Scoring rule document must declare rule_version.");
        if (ruleVersion.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(ruleVersion.Error);
        }

        var schemaVersion = ReadRequiredString(
            document,
            "schema_version",
            "score.rule_metadata_missing",
            "Scoring rule document must declare schema_version.");
        if (schemaVersion.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(schemaVersion.Error);
        }

        var engineMinVersion = ReadRequiredString(
            document,
            "engine_min_version",
            "score.rule_metadata_missing",
            "Scoring rule document must declare engine_min_version.");
        if (engineMinVersion.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(engineMinVersion.Error);
        }

        if (NormalizeCode(request.RuleKey) != NormalizeCode(ruleId.Value) ||
            request.RuleVersion.Trim() != ruleVersion.Value.Trim() ||
            NormalizeSchemaVersion(request.SchemaVersion) != NormalizeSchemaVersion(schemaVersion.Value) ||
            NormalizeEngineVersion(request.EngineMinVersion) != NormalizeEngineVersion(engineMinVersion.Value))
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation(
                    "score.rule_metadata_mismatch",
                    "Scoring rule request metadata must match the scoring document metadata."));
        }

        var outputCodes = ValidateGraphShape(document);
        if (outputCodes.IsFailure)
        {
            return Result.Failure<ScoringRuleValidationSummary>(outputCodes.Error);
        }

        if (!SameCodeSet(outputCodes.Value, producesScores))
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation(
                    "score.rule_produces_mismatch",
                    "Scoring rule produces.scores must match declared outputs."));
        }

        return Result.Success(new ScoringRuleValidationSummary(producesScores));
    }

    private static Result<ScoringRuleValidationSummary> ValidateLegacyOperations(
        JsonElement document,
        IReadOnlyList<string> producesScores)
    {
        if (!document.TryGetProperty("operations", out var operations) ||
            operations.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation("score.operations_missing", "Scoring rule must declare operations."));
        }

        var outputCodes = new List<string>();
        foreach (var operation in operations.EnumerateArray())
        {
            if (operation.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<ScoringRuleValidationSummary>(
                    Error.Validation("score.operation_invalid", "Scoring operation must be a JSON object."));
            }

            var op = ReadRequiredString(
                operation,
                "op",
                "score.operation_unsupported",
                "Scoring operation must declare an op.");
            if (op.IsFailure)
            {
                return Result.Failure<ScoringRuleValidationSummary>(op.Error);
            }

            if (NormalizeCode(op.Value) is not ("mean" or "sum"))
            {
                return Result.Failure<ScoringRuleValidationSummary>(
                    Error.Validation(
                        "score.operation_unsupported",
                        $"Scoring operation '{op.Value}' is unsupported."));
            }

            var output = ReadRequiredString(
                operation,
                "output",
                "score.output_missing",
                "Scoring operation must declare an output.");
            if (output.IsFailure)
            {
                return Result.Failure<ScoringRuleValidationSummary>(output.Error);
            }

            if (!operation.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return Result.Failure<ScoringRuleValidationSummary>(
                    Error.Validation("score.items_missing", "Scoring operation must declare item codes."));
            }

            var itemCodes = ReadStringArray(items, "score.items_missing", "Scoring operation item code");
            if (itemCodes.IsFailure)
            {
                return Result.Failure<ScoringRuleValidationSummary>(itemCodes.Error);
            }

            if (itemCodes.Value.Count == 0)
            {
                return Result.Failure<ScoringRuleValidationSummary>(
                    Error.Validation("score.items_missing", "Scoring operation must declare at least one item."));
            }

            outputCodes.Add(NormalizeCode(output.Value));
        }

        if (outputCodes.Count == 0)
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation("score.operations_missing", "Scoring rule must declare at least one operation."));
        }

        if (!SameCodeSet(outputCodes, producesScores))
        {
            return Result.Failure<ScoringRuleValidationSummary>(
                Error.Validation(
                    "score.rule_produces_mismatch",
                    "Scoring rule produces.scores must match declared outputs."));
        }

        return Result.Success(new ScoringRuleValidationSummary(producesScores));
    }

    private static Result<bool> ValidateCompatibility(
        JsonElement compatibility,
        IReadOnlyList<string> scoreCodes)
    {
        var scoreCodeSet = scoreCodes.ToHashSet(StringComparer.Ordinal);
        foreach (var property in compatibility.EnumerateObject())
        {
            var propertyName = NormalizeCode(property.Name);
            if (propertyName == "outputs")
            {
                var outputsValidation = ValidateCompatibilityOutputs(property.Value, scoreCodeSet);
                if (outputsValidation.IsFailure)
                {
                    return outputsValidation;
                }

                continue;
            }

            if (propertyName is not ("output_equivalent_with" or "descriptive_only_with" or "incompatible_with"))
            {
                return CompatibilityFailure($"Scoring rule compatibility key '{property.Name}' is unsupported.");
            }

            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                return CompatibilityFailure(
                    $"Scoring rule compatibility '{property.Name}' must be an array.");
            }

            foreach (var entry in property.Value.EnumerateArray())
            {
                var entryValidation = ValidateCompatibilityEntry(entry, scoreCodeSet);
                if (entryValidation.IsFailure)
                {
                    return entryValidation;
                }
            }
        }

        return Result.Success(true);
    }

    /// <summary>
    /// Optional human labels for score outputs: an array of {code, label} where
    /// each code is one the rule actually produces. Reporting surfaces read
    /// these labels; they never influence scoring.
    /// </summary>
    private static Result<bool> ValidateCompatibilityOutputs(
        JsonElement outputs,
        HashSet<string> scoreCodes)
    {
        if (outputs.ValueKind != JsonValueKind.Array)
        {
            return CompatibilityFailure("Scoring rule compatibility 'outputs' must be an array.");
        }

        foreach (var entry in outputs.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return CompatibilityFailure("Scoring rule compatibility output must be a JSON object.");
            }

            var code = ReadRequiredString(
                entry,
                "code",
                "score.rule_compatibility_invalid",
                "Scoring rule compatibility output must declare code.");
            if (code.IsFailure)
            {
                return Result.Failure<bool>(code.Error);
            }

            if (!scoreCodes.Contains(NormalizeCode(code.Value)))
            {
                return CompatibilityFailure(
                    $"Scoring rule compatibility output '{code.Value}' is not a score this rule produces.");
            }

            var label = ReadRequiredString(
                entry,
                "label",
                "score.rule_compatibility_invalid",
                "Scoring rule compatibility output must declare label.");
            if (label.IsFailure)
            {
                return Result.Failure<bool>(label.Error);
            }
        }

        return Result.Success(true);
    }

    private static Result<bool> ValidateCompatibilityEntry(
        JsonElement entry,
        HashSet<string> scoreCodes)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return CompatibilityFailure("Scoring rule compatibility entry must be a JSON object.");
        }

        var ruleId = ReadRequiredString(
            entry,
            "rule_id",
            "score.rule_compatibility_invalid",
            "Scoring rule compatibility entry must declare rule_id.");
        if (ruleId.IsFailure)
        {
            return Result.Failure<bool>(ruleId.Error);
        }

        var versionRange = ReadRequiredString(
            entry,
            "rule_version_range",
            "score.rule_compatibility_invalid",
            "Scoring rule compatibility entry must declare rule_version_range.");
        if (versionRange.IsFailure)
        {
            return Result.Failure<bool>(versionRange.Error);
        }

        if (!HasNonEmptyString(entry, "evidence") &&
            !HasNonEmptyString(entry, "rationale"))
        {
            return CompatibilityFailure(
                "Scoring rule compatibility entry must declare evidence or rationale.");
        }

        if (!entry.TryGetProperty("scope", out var scope))
        {
            return CompatibilityFailure("Scoring rule compatibility entry must declare scope.");
        }

        return ValidateCompatibilityScope(scope, scoreCodes);
    }

    private static Result<bool> ValidateCompatibilityScope(
        JsonElement scope,
        HashSet<string> scoreCodes)
    {
        if (scope.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(scope.GetString()))
        {
            return NormalizeCode(scope.GetString()!) == "all_outputs"
                ? Result.Success(true)
                : CompatibilityFailure("Scoring rule compatibility scope string must be all_outputs.");
        }

        if (scope.ValueKind != JsonValueKind.Array)
        {
            return CompatibilityFailure("Scoring rule compatibility scope must be all_outputs or an array.");
        }

        var scopedScores = ReadStringArray(
            scope,
            "score.rule_compatibility_invalid",
            "Scoring rule compatibility scope score");
        if (scopedScores.IsFailure)
        {
            return Result.Failure<bool>(scopedScores.Error);
        }

        if (scopedScores.Value.Count == 0)
        {
            return CompatibilityFailure("Scoring rule compatibility scope array must not be empty.");
        }

        foreach (var scopedScore in scopedScores.Value)
        {
            if (!scoreCodes.Contains(scopedScore))
            {
                return CompatibilityFailure(
                    $"Scoring rule compatibility scope score '{scopedScore}' is not declared.");
            }
        }

        return Result.Success(true);
    }

    private static bool HasNonEmptyString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString());
    }

    private static Result<bool> CompatibilityFailure(string message)
    {
        return Result.Failure<bool>(Error.Validation("score.rule_compatibility_invalid", message));
    }

    private static Result<IReadOnlyList<string>> ValidateGraphShape(JsonElement document)
    {
        var inputs = ReadGraphInputs(document);
        if (inputs.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(inputs.Error);
        }

        var scales = ReadScaleDefinitions(document);
        if (scales.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(scales.Error);
        }

        var missingPolicy = ReadDefaultMissingPolicy(document);
        if (missingPolicy.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(missingPolicy.Error);
        }

        var nodes = ValidateGraphNodes(document, inputs.Value, scales.Value, missingPolicy.Value);
        if (nodes.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(nodes.Error);
        }

        return ReadGraphOutputCodes(document, nodes.Value);
    }

    private static Result<JsonDocument> ParseObject(
        string value,
        string errorCode,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JsonDocument>(
                Error.Validation(errorCode, $"Scoring rule {fieldName} must be a JSON object."));
        }

        try
        {
            var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return Result.Success(document);
            }

            document.Dispose();

            return Result.Failure<JsonDocument>(
                Error.Validation(errorCode, $"Scoring rule {fieldName} must be a JSON object."));
        }
        catch (JsonException)
        {
            return Result.Failure<JsonDocument>(
                Error.Validation(errorCode, $"Scoring rule {fieldName} must be valid JSON."));
        }
    }

    private static Result<IReadOnlyList<string>> ReadProducesScores(JsonElement produces)
    {
        if (!produces.TryGetProperty("scores", out var scores) ||
            scores.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces must declare scores."));
        }

        var result = ReadStringArray(scores, "score.rule_produces_invalid", "Scoring rule score code");
        if (result.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(result.Error);
        }

        return result.Value.Count == 0
            ? Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces.scores must not be empty."))
            : Result.Success<IReadOnlyList<string>>(result.Value);
    }

    private static Result<IReadOnlyList<string>> ReadGraphOutputCodes(
        JsonElement document,
        Dictionary<string, GraphValueType> nodeTypes)
    {
        if (!document.TryGetProperty("outputs", out var outputs) ||
            outputs.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_outputs_missing", "Scoring rule graph must declare outputs."));
        }

        var outputCodes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var output in outputs.EnumerateArray())
        {
            var code = ReadRequiredString(
                output,
                "code",
                "score.rule_outputs_missing",
                "Scoring rule output must declare a code.");
            if (code.IsFailure)
            {
                return Result.Failure<IReadOnlyList<string>>(code.Error);
            }

            var node = ReadRequiredString(
                output,
                "node",
                "score.rule_outputs_missing",
                "Scoring rule output must declare a node.");
            if (node.IsFailure)
            {
                return Result.Failure<IReadOnlyList<string>>(node.Error);
            }

            var normalizedNode = NormalizeCode(node.Value);
            if (!nodeTypes.TryGetValue(normalizedNode, out var nodeType))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "score.rule_output_unknown_node",
                        $"Scoring rule output node '{normalizedNode}' is unknown."));
            }

            if (nodeType != GraphValueType.Scalar)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "score.rule_output_type_invalid",
                        $"Scoring rule output node '{normalizedNode}' must produce a scalar score."));
            }

            var normalizedCode = NormalizeCode(code.Value);
            if (!seen.Add(normalizedCode))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation(
                        "score.rule_outputs_duplicate",
                        $"Scoring rule output '{normalizedCode}' is duplicated."));
            }

            outputCodes.Add(normalizedCode);
        }

        return outputCodes.Count == 0
            ? Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_outputs_missing", "Scoring rule graph must declare outputs."))
            : Result.Success<IReadOnlyList<string>>(outputCodes);
    }

    private static Result<Dictionary<string, GraphInputMetadata>> ReadGraphInputs(JsonElement document)
    {
        if (!document.TryGetProperty("inputs", out var inputs) ||
            inputs.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<Dictionary<string, GraphInputMetadata>>(
                Error.Validation("score.inputs_missing", "Scoring rule graph must declare inputs."));
        }

        var inputMetadata = new Dictionary<string, GraphInputMetadata>(StringComparer.Ordinal);
        foreach (var input in inputs.EnumerateArray())
        {
            var id = ReadRequiredString(
                input,
                "id",
                "score.input_invalid",
                "Scoring rule input must declare an id.");
            if (id.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(id.Error);
            }

            var normalizedId = NormalizeCode(id.Value);
            if (inputMetadata.ContainsKey(normalizedId))
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(
                    Error.Validation("score.input_duplicate", $"Scoring rule input '{normalizedId}' is duplicated."));
            }

            var kind = ReadRequiredString(
                input,
                "kind",
                "score.input_kind_unsupported",
                "Scoring rule input must declare a kind.");
            if (kind.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(kind.Error);
            }

            if (NormalizeCode(kind.Value) != "answers")
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(
                    Error.Validation(
                        "score.input_kind_unsupported",
                        $"Scoring rule input kind '{kind.Value}' is unsupported."));
            }

            if (!input.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(
                    Error.Validation("score.items_missing", "Scoring rule answer input must declare items."));
            }

            var itemCodes = ReadStringArray(items, "score.items_missing", "Scoring rule item code");
            if (itemCodes.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(itemCodes.Error);
            }

            if (itemCodes.Value.Count == 0)
            {
                return Result.Failure<Dictionary<string, GraphInputMetadata>>(
                    Error.Validation("score.items_missing", "Scoring rule answer input items must not be empty."));
            }

            inputMetadata.Add(
                normalizedId,
                new GraphInputMetadata(itemCodes.Value.Select(NormalizeCode).ToHashSet(StringComparer.Ordinal)));
        }

        return inputMetadata.Count == 0
            ? Result.Failure<Dictionary<string, GraphInputMetadata>>(
                Error.Validation("score.inputs_missing", "Scoring rule graph must declare inputs."))
            : Result.Success(inputMetadata);
    }

    private static Result<Dictionary<string, GraphValueType>> ValidateGraphNodes(
        JsonElement document,
        Dictionary<string, GraphInputMetadata> inputItemCounts,
        HashSet<string> scaleIds,
        GraphMissingPolicy defaultMissingPolicy)
    {
        if (!document.TryGetProperty("nodes", out var nodes) ||
            nodes.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<Dictionary<string, GraphValueType>>(
                Error.Validation("score.nodes_missing", "Scoring rule graph must declare nodes."));
        }

        var nodeTypes = new Dictionary<string, GraphValueType>(StringComparer.Ordinal);
        var vectorItemCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in nodes.EnumerateArray())
        {
            var id = ReadRequiredString(
                node,
                "id",
                "score.node_invalid",
                "Scoring rule node must declare an id.");
            if (id.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphValueType>>(id.Error);
            }

            var normalizedId = NormalizeCode(id.Value);
            if (nodeTypes.ContainsKey(normalizedId))
            {
                return Result.Failure<Dictionary<string, GraphValueType>>(
                    Error.Validation("score.node_duplicate", $"Scoring rule node '{normalizedId}' is duplicated."));
            }

            var op = ReadRequiredString(
                node,
                "op",
                "score.operation_unsupported",
                "Scoring rule node must declare an operation.");
            if (op.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphValueType>>(op.Error);
            }

            var input = ReadRequiredString(
                node,
                "input",
                "score.node_unknown",
                "Scoring rule node must declare an input.");
            if (input.IsFailure)
            {
                return Result.Failure<Dictionary<string, GraphValueType>>(input.Error);
            }

            var inputRef = NormalizeCode(input.Value);
            var normalizedOp = NormalizeCode(op.Value);
            switch (normalizedOp)
            {
                case "select_answers":
                    if (!inputItemCounts.TryGetValue(inputRef, out var selectedItemCount))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.input_unknown", $"Scoring rule input '{inputRef}' is unknown."));
                    }

                    nodeTypes.Add(normalizedId, GraphValueType.Vector);
                    vectorItemCounts.Add(normalizedId, selectedItemCount.Count);
                    break;

                case "map_choice_scores":
                    if (!inputItemCounts.TryGetValue(inputRef, out var mappedInput))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.input_unknown", $"Scoring rule input '{inputRef}' is unknown."));
                    }

                    var choiceMap = ValidateChoiceScoreMap(node, mappedInput);
                    if (choiceMap.IsFailure)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(choiceMap.Error);
                    }

                    nodeTypes.Add(normalizedId, GraphValueType.Vector);
                    vectorItemCounts.Add(normalizedId, mappedInput.Count);
                    break;

                case "reverse_code":
                    var scale = ReadRequiredString(
                        node,
                        "scale",
                        "score.scale_missing",
                        "Scoring rule reverse_code node must declare a scale.");
                    if (scale.IsFailure)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(scale.Error);
                    }

                    var scaleId = NormalizeCode(scale.Value);
                    if (!scaleIds.Contains(scaleId))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.scale_missing", $"Scoring rule scale '{scaleId}' is unknown."));
                    }

                    var source = "explicit_list";
                    if (node.TryGetProperty("reverse_flag_source", out var sourceElement) &&
                        sourceElement.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(sourceElement.GetString()))
                    {
                        source = NormalizeCode(sourceElement.GetString()!);
                    }

                    if (source is not ("all" or "explicit_list"))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation(
                                "score.reverse_source_unsupported",
                                $"Scoring rule reverse flag source '{source}' is unsupported."));
                    }

                    if (!TryReadNodeType(nodeTypes, inputRef, out var reverseInputType))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    if (reverseInputType != GraphValueType.Vector)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation(
                                "score.node_type_invalid",
                                $"Scoring rule node '{normalizedId}' requires a vector input."));
                    }

                    if (!vectorItemCounts.TryGetValue(inputRef, out var reverseItemCount))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    nodeTypes.Add(normalizedId, GraphValueType.Vector);
                    vectorItemCounts.Add(normalizedId, reverseItemCount);
                    break;

                case "mean":
                case "sum":
                case "count_valid":
                    if (!TryReadNodeType(nodeTypes, inputRef, out var aggregateInputType))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    if (aggregateInputType != GraphValueType.Vector)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation(
                                "score.node_type_invalid",
                                $"Scoring rule node '{normalizedId}' requires a vector input."));
                    }

                    if (!vectorItemCounts.TryGetValue(inputRef, out var aggregateItemCount))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    if (normalizedOp is "mean" or "sum")
                    {
                        var aggregateMissingPolicy = ValidateNodeMissingPolicy(
                            node,
                            defaultMissingPolicy,
                            aggregateItemCount,
                            normalizedId);
                        if (aggregateMissingPolicy.IsFailure)
                        {
                            return Result.Failure<Dictionary<string, GraphValueType>>(aggregateMissingPolicy.Error);
                        }
                    }

                    nodeTypes.Add(normalizedId, GraphValueType.Scalar);
                    break;

                case "subscale_aggregate":
                    var aggregator = ReadRequiredString(
                        node,
                        "aggregator",
                        "score.aggregator_missing",
                        "Scoring rule subscale_aggregate node must declare an aggregator.");
                    if (aggregator.IsFailure)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(aggregator.Error);
                    }

                    var normalizedAggregator = NormalizeCode(aggregator.Value);
                    if (normalizedAggregator is not "mean" and not "sum")
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation(
                                "score.aggregator_unsupported",
                                $"Scoring rule subscale aggregator '{normalizedAggregator}' is unsupported."));
                    }

                    if (!TryReadNodeType(nodeTypes, inputRef, out var subscaleInputType))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    if (subscaleInputType != GraphValueType.Vector)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation(
                                "score.node_type_invalid",
                                $"Scoring rule node '{normalizedId}' requires a vector input."));
                    }

                    if (!vectorItemCounts.TryGetValue(inputRef, out var subscaleItemCount))
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(
                            Error.Validation("score.node_unknown", $"Scoring rule node '{inputRef}' is unknown."));
                    }

                    var subscaleMissingPolicy = ValidateNodeMissingPolicy(
                        node,
                        defaultMissingPolicy,
                        subscaleItemCount,
                        normalizedId);
                    if (subscaleMissingPolicy.IsFailure)
                    {
                        return Result.Failure<Dictionary<string, GraphValueType>>(subscaleMissingPolicy.Error);
                    }

                    nodeTypes.Add(normalizedId, GraphValueType.Scalar);
                    break;

                default:
                    return Result.Failure<Dictionary<string, GraphValueType>>(
                        Error.Validation(
                            "score.operation_unsupported",
                            $"Scoring rule operation '{op.Value}' is unsupported."));
            }
        }

        return nodeTypes.Count == 0
            ? Result.Failure<Dictionary<string, GraphValueType>>(
                Error.Validation("score.nodes_missing", "Scoring rule graph must declare nodes."))
            : Result.Success(nodeTypes);
    }

    private static Result<bool> ValidateChoiceScoreMap(
        JsonElement node,
        GraphInputMetadata input)
    {
        if (!node.TryGetProperty("option_scores", out var optionScores) ||
            optionScores.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "score.choice_scores_missing",
                    "map_choice_scores must declare option_scores."));
        }

        var mappedItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var itemScoreMap in optionScores.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(itemScoreMap.Name))
            {
                return Result.Failure<bool>(
                    Error.Validation("score.choice_scores_invalid", "Choice score item code must not be empty."));
            }

            var itemCode = NormalizeCode(itemScoreMap.Name);
            if (!input.Items.Contains(itemCode))
            {
                return Result.Failure<bool>(
                    Error.Validation(
                        "score.choice_score_item_unknown",
                        $"Choice score item '{itemCode}' is not part of the mapped input."));
            }

            if (!mappedItems.Add(itemCode))
            {
                return Result.Failure<bool>(
                    Error.Validation(
                        "score.choice_scores_invalid",
                        $"Choice score item '{itemCode}' is duplicated."));
            }

            if (itemScoreMap.Value.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<bool>(
                    Error.Validation(
                        "score.choice_scores_invalid",
                        $"Choice score item '{itemCode}' must map option codes to numeric scores."));
            }

            var optionCount = 0;
            foreach (var optionScore in itemScoreMap.Value.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(optionScore.Name) ||
                    optionScore.Value.ValueKind != JsonValueKind.Number ||
                    !optionScore.Value.TryGetDecimal(out _))
                {
                    return Result.Failure<bool>(
                        Error.Validation(
                            "score.choice_scores_invalid",
                            $"Choice score item '{itemCode}' must map non-empty option codes to numeric scores."));
                }

                optionCount += 1;
            }

            if (optionCount == 0)
            {
                return Result.Failure<bool>(
                    Error.Validation(
                        "score.choice_scores_invalid",
                        $"Choice score item '{itemCode}' must include at least one option score."));
            }
        }

        return mappedItems.Count == 0
            ? Result.Failure<bool>(
                Error.Validation(
                    "score.choice_scores_missing",
                    "map_choice_scores must declare at least one mapped choice item."))
            : Result.Success(true);
    }

    private static Result<HashSet<string>> ReadScaleDefinitions(JsonElement document)
    {
        var scaleIds = new HashSet<string>(StringComparer.Ordinal);
        if (!document.TryGetProperty("scale_defaults", out var scales))
        {
            return Result.Success(scaleIds);
        }

        if (scales.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<HashSet<string>>(
                Error.Validation("score.scale_invalid", "Scoring rule scale_defaults must be a JSON object."));
        }

        foreach (var scale in scales.EnumerateObject())
        {
            var scaleId = NormalizeCode(scale.Name);
            if (!scaleIds.Add(scaleId))
            {
                return Result.Failure<HashSet<string>>(
                    Error.Validation("score.scale_invalid", $"Scoring rule scale '{scaleId}' is duplicated."));
            }

            if (scale.Value.ValueKind != JsonValueKind.Object ||
                !scale.Value.TryGetProperty("min", out var min) ||
                !scale.Value.TryGetProperty("max", out var max) ||
                min.ValueKind != JsonValueKind.Number ||
                max.ValueKind != JsonValueKind.Number ||
                !min.TryGetDecimal(out var minValue) ||
                !max.TryGetDecimal(out var maxValue) ||
                minValue >= maxValue)
            {
                return Result.Failure<HashSet<string>>(
                    Error.Validation(
                        "score.scale_invalid",
                        $"Scoring rule scale '{scaleId}' must declare numeric min and max bounds."));
            }
        }

        return Result.Success(scaleIds);
    }

    private static Result<GraphMissingPolicy> ReadDefaultMissingPolicy(JsonElement document)
    {
        if (!document.TryGetProperty("missing_data", out var missingData))
        {
            return Result.Success(new GraphMissingPolicy("require_all", null));
        }

        if (missingData.ValueKind != JsonValueKind.Object ||
            !missingData.TryGetProperty("defaults", out var defaults) ||
            defaults.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<GraphMissingPolicy>(
                Error.Validation("score.missing_policy_invalid", "Scoring rule missing_data.defaults must be an object."));
        }

        return ReadMissingPolicy(defaults, "Scoring rule missing_data.defaults");
    }

    private static Result<bool> ValidateNodeMissingPolicy(
        JsonElement node,
        GraphMissingPolicy defaultMissingPolicy,
        int availableItemCount,
        string nodeId)
    {
        var missingPolicy = defaultMissingPolicy;
        if (node.TryGetProperty("missing_data", out var missingData))
        {
            if (missingData.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<bool>(
                    Error.Validation("score.missing_policy_invalid", "Scoring rule node missing_data must be an object."));
            }

            var nodeMissingPolicy = ReadMissingPolicy(missingData, "Scoring rule node missing_data");
            if (nodeMissingPolicy.IsFailure)
            {
                return Result.Failure<bool>(nodeMissingPolicy.Error);
            }

            missingPolicy = nodeMissingPolicy.Value;
        }

        if (missingPolicy.Strategy == "min_valid_count" &&
            missingPolicy.MinValidCount.GetValueOrDefault() > availableItemCount)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "score.missing_policy_invalid",
                    $"Scoring rule node '{nodeId}' min_valid_count cannot exceed its {availableItemCount} available item(s)."));
        }

        return Result.Success(true);
    }

    private static Result<GraphMissingPolicy> ReadMissingPolicy(JsonElement policy, string subject)
    {
        var strategy = "require_all";
        if (policy.TryGetProperty("strategy", out var strategyElement))
        {
            if (strategyElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(strategyElement.GetString()))
            {
                return Result.Failure<GraphMissingPolicy>(
                    Error.Validation(
                        "score.missing_policy_invalid",
                        $"{subject}.strategy must be a non-empty string."));
            }

            strategy = NormalizeCode(strategyElement.GetString()!);
        }

        return strategy switch
        {
            "require_all" => Result.Success(new GraphMissingPolicy(strategy, null)),
            "min_valid_count" => ReadMinValidCountPolicy(policy),
            _ => Result.Failure<GraphMissingPolicy>(
                Error.Validation(
                    "score.missing_policy_unsupported",
                    $"Scoring rule missing-data strategy '{strategy}' is unsupported."))
        };
    }

    private static Result<GraphMissingPolicy> ReadMinValidCountPolicy(JsonElement policy)
    {
        return policy.TryGetProperty("min_valid_count", out var count) &&
            count.TryGetInt32(out var minValidCount) &&
            minValidCount > 0
            ? Result.Success(new GraphMissingPolicy("min_valid_count", minValidCount))
            : Result.Failure<GraphMissingPolicy>(
                Error.Validation(
                    "score.missing_policy_invalid",
                    "Scoring rule min_valid_count must be a positive integer."));
    }

    private static bool TryReadNodeType(
        Dictionary<string, GraphValueType> nodeTypes,
        string nodeId,
        out GraphValueType graphValueType)
    {
        return nodeTypes.TryGetValue(nodeId, out graphValueType);
    }

    private static Result<List<string>> ReadStringArray(
        JsonElement array,
        string errorCode,
        string itemName)
    {
        var values = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(item.GetString()))
            {
                return Result.Failure<List<string>>(
                    Error.Validation(errorCode, $"{itemName} must be a non-empty string."));
            }

            var normalized = NormalizeCode(item.GetString()!);
            if (!seen.Add(normalized))
            {
                return Result.Failure<List<string>>(
                    Error.Validation(errorCode, $"{itemName} '{normalized}' is duplicated."));
            }

            values.Add(normalized);
        }

        return Result.Success(values);
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

    private static bool IsGraphRule(JsonElement document)
    {
        return document.TryGetProperty("inputs", out _) ||
            document.TryGetProperty("nodes", out _) ||
            document.TryGetProperty("outputs", out _);
    }

    private static bool IsLegacyOperationsRule(JsonElement document)
    {
        return document.TryGetProperty("operations", out _);
    }

    private static bool SameCodeSet(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right)
    {
        return left.Order(StringComparer.Ordinal).SequenceEqual(
            right.Order(StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private static string NormalizeSchemaVersion(string value)
    {
        var normalized = NormalizeCode(value);

        return normalized == "scoring-rule/v1" ? "1.0.0" : normalized;
    }

    private static string NormalizeEngineVersion(string value)
    {
        var normalized = NormalizeCode(value);

        return normalized == "engine/v1" ? "1.0.0" : normalized;
    }

    private static string NormalizeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim().ToLowerInvariant();
    }
}
