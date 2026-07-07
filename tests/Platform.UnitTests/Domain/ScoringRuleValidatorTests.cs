using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class ScoringRuleValidatorTests
{
    [Fact]
    public void Valid_graph_rule_with_matching_metadata_passes()
    {
        var result = ScoringRuleValidator.Validate(ValidGraphRequest());

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Valid_graph_rule_accepts_current_v1_storage_aliases()
    {
        var request = ValidGraphRequest(
            schemaVersion: "scoring-rule/v1",
            engineMinVersion: "engine/v1");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Produces_scores_must_match_graph_outputs()
    {
        var request = ValidGraphRequest(produces: """{"scores":["other"]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_produces_mismatch", result.Error.Code);
    }

    [Fact]
    public void Missing_produces_scores_is_rejected()
    {
        var request = ValidGraphRequest(produces: "{}");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_produces_invalid", result.Error.Code);
    }

    [Fact]
    public void Valid_graph_rule_with_multiple_outputs_and_node_missing_policies_passes()
    {
        var request = ValidGraphRequest(
            ruleKey: "tenant-burnout.multi",
            document:
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.multi",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "exhaustion_items", "kind": "answers", "items": ["q01", "q02"] },
                { "id": "recovery_items", "kind": "answers", "items": ["q03", "q04"] }
              ],
              "nodes": [
                { "id": "exhaustion_answers", "op": "select_answers", "input": "exhaustion_items" },
                {
                  "id": "exhaustion_score",
                  "op": "mean",
                  "input": "exhaustion_answers",
                  "missing_data": { "strategy": "require_all" }
                },
                { "id": "recovery_answers", "op": "select_answers", "input": "recovery_items" },
                {
                  "id": "recovery_score",
                  "op": "sum",
                  "input": "recovery_answers",
                  "missing_data": { "strategy": "min_valid_count", "min_valid_count": 1 }
                }
              ],
              "outputs": [
                { "code": "exhaustion", "node": "exhaustion_score" },
                { "code": "recovery", "node": "recovery_score" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            produces: """{"scores":["exhaustion","recovery"]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["exhaustion", "recovery"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Valid_graph_rule_with_choice_score_mapping_passes()
    {
        var request = ValidGraphRequest(
            ruleKey: "tenant-choice.total",
            document: ChoiceScoringDocument,
            produces: """{"scores":["total"]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Choice_score_mapping_unknown_input_item_is_rejected()
    {
        var request = ValidGraphRequest(
            ruleKey: "tenant-choice.total",
            document: ReplaceRequired(ChoiceScoringDocument, "\"q01\": {", "\"q99\": {"),
            produces: """{"scores":["total"]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.choice_score_item_unknown", result.Error.Code);
    }

    [Fact]
    public void Produces_interpretation_metadata_matches_score_band()
    {
        var result = ScoreInterpretationMetadataParser.ParseProduces(ValidInterpretationProduces);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.NotNull(result.Value);
        var band = result.Value!.Match("total", 3.0m);
        Assert.NotNull(band);
        Assert.Equal("middle", band.Code);
        Assert.Equal("Middle tenant band", band.Label);
    }

    [Fact]
    public void Produces_interpretation_metadata_is_optional()
    {
        var result = ScoreInterpretationMetadataParser.ParseProduces("""{"scores":["total"]}""");

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Null(result.Value);
    }

    [Fact]
    public void Valid_tenant_attested_interpretation_metadata_passes()
    {
        var request = ValidGraphRequest(produces: ValidInterpretationProduces);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Interpretation_metadata_unknown_score_is_rejected()
    {
        var produces = ReplaceRequired(ValidInterpretationProduces, "\"total\": [", "\"other\": [");
        var request = ValidGraphRequest(produces: produces);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_interpretation_invalid", result.Error.Code);
    }

    [Fact]
    public void Interpretation_metadata_unsupported_status_is_rejected()
    {
        var produces = ReplaceRequired(
            ValidInterpretationProduces,
            "\"status\": \"tenant_attested\"",
            "\"status\": \"platform_canonical\"");
        var request = ValidGraphRequest(produces: produces);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_interpretation_invalid", result.Error.Code);
    }

    [Fact]
    public void Interpretation_metadata_overlapping_bands_are_rejected()
    {
        var produces = ReplaceRequired(ValidInterpretationProduces, "\"max\": 2.49", "\"max\": 2.50");
        var request = ValidGraphRequest(produces: produces);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_interpretation_invalid", result.Error.Code);
    }

    [Fact]
    public void Request_rule_key_must_match_document_rule_id()
    {
        var request = ValidGraphRequest(ruleKey: "tenant-burnout.other");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_metadata_mismatch", result.Error.Code);
    }

    [Fact]
    public void Valid_legacy_operations_rule_with_matching_produces_passes()
    {
        var request = ValidGraphRequest(document: ValidLegacyDocument);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Legacy_unsupported_operation_is_rejected()
    {
        var document = ReplaceRequired(ValidLegacyDocument, "\"op\": \"mean\"", "\"op\": \"median\"");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.operation_unsupported", result.Error.Code);
    }

    [Fact]
    public void Legacy_empty_items_are_rejected()
    {
        var document = ReplaceRequired(ValidLegacyDocument, """["q01", "q02", "q03"]""", "[]");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.items_missing", result.Error.Code);
    }

    [Fact]
    public void Compatibility_unknown_top_level_key_is_rejected()
    {
        var request = ValidGraphRequest(compatibility: """{"unknown_with":[]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Compatibility_scope_all_outputs_passes()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "output_equivalent_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": "all_outputs",
                  "evidence": "Synthetic tenant-private scoring rule unchanged."
                }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Compatibility_scope_array_with_known_score_passes()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "descriptive_only_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=2.0.0 <3.0.0",
                  "scope": ["total"],
                  "rationale": "Formula changed; display side-by-side only."
                }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Compatibility_scope_array_with_unknown_score_is_rejected()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "descriptive_only_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=2.0.0 <3.0.0",
                  "scope": ["other"],
                  "rationale": "Formula changed; display side-by-side only."
                }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Compatibility_outputs_with_labels_for_known_scores_pass()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "outputs": [
                { "code": "total", "label": "Total score (mean of items)" }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(["total"], result.Value.ScoreCodes);
    }

    [Fact]
    public void Compatibility_outputs_label_for_unknown_score_is_rejected()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "outputs": [
                { "code": "other", "label": "Not a score this rule produces" }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Compatibility_outputs_entry_without_label_is_rejected()
    {
        var request = ValidGraphRequest(
            compatibility: """{"outputs":[{"code":"total"}]}""");

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Compatibility_entry_missing_rule_id_is_rejected()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "output_equivalent_with": [
                {
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": "all_outputs",
                  "evidence": "Synthetic tenant-private scoring rule unchanged."
                }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Compatibility_entry_missing_evidence_and_rationale_is_rejected()
    {
        var request = ValidGraphRequest(
            compatibility:
            """
            {
              "output_equivalent_with": [
                {
                  "rule_id": "tenant-burnout.total",
                  "rule_version_range": ">=1.0.0 <2.0.0",
                  "scope": "all_outputs"
                }
              ]
            }
            """);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_compatibility_invalid", result.Error.Code);
    }

    [Fact]
    public void Duplicate_input_ids_are_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
            """,
            """
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] },
                { "id": "core_items", "kind": "answers", "items": ["q04"] }
              ],
            """);
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.input_duplicate", result.Error.Code);
    }

    [Fact]
    public void Input_items_must_not_be_empty()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }""",
            """{ "id": "core_items", "kind": "answers", "items": [] }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.items_missing", result.Error.Code);
    }

    [Fact]
    public void Duplicate_node_ids_are_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                {
                  "id": "scored_answers",
            """,
            """
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                {
                  "id": "scored_answers",
            """);
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.node_duplicate", result.Error.Code);
    }

    [Fact]
    public void Duplicate_output_codes_are_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """
              "outputs": [
                { "code": "total", "node": "total" }
              ],
            """,
            """
              "outputs": [
                { "code": "total", "node": "total" },
                { "code": "total", "node": "total" }
              ],
            """);
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_outputs_duplicate", result.Error.Code);
    }

    [Fact]
    public void Unknown_node_input_is_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "id": "total", "op": "mean", "input": "scored_answers" }""",
            """{ "id": "total", "op": "mean", "input": "missing_node" }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.node_unknown", result.Error.Code);
    }

    [Fact]
    public void Output_referencing_vector_node_is_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "code": "total", "node": "total" }""",
            """{ "code": "total", "node": "core_answers" }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.rule_output_type_invalid", result.Error.Code);
    }

    [Fact]
    public void Unsupported_operation_is_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "id": "total", "op": "mean", "input": "scored_answers" }""",
            """{ "id": "total", "op": "median", "input": "scored_answers" }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.operation_unsupported", result.Error.Code);
    }

    [Fact]
    public void Unsupported_reverse_flag_source_is_rejected()
    {
        var document = ReplaceRequired(ValidGraphDocument, "explicit_list", "tenant_profile");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.reverse_source_unsupported", result.Error.Code);
    }

    [Fact]
    public void Invalid_scale_defaults_are_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            "\"agreement\": { \"min\": 1, \"max\": 5 }",
            "\"agreement\": { \"min\": 5, \"max\": 1 }");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.scale_invalid", result.Error.Code);
    }

    [Fact]
    public void Unsupported_missing_data_strategy_is_rejected()
    {
        var document = ReplaceRequired(ValidGraphDocument, "require_all", "drop_everything");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.missing_policy_unsupported", result.Error.Code);
    }

    [Fact]
    public void Node_min_valid_count_above_available_item_count_is_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "id": "total", "op": "mean", "input": "scored_answers" }""",
            """{ "id": "total", "op": "mean", "input": "scored_answers", "missing_data": { "strategy": "min_valid_count", "min_valid_count": 4 } }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.missing_policy_invalid", result.Error.Code);
    }

    [Fact]
    public void Unsupported_subscale_aggregator_is_rejected()
    {
        var document = ReplaceRequired(
            ValidGraphDocument,
            """{ "id": "total", "op": "mean", "input": "scored_answers" }""",
            """{ "id": "total", "op": "subscale_aggregate", "input": "scored_answers", "aggregator": "median" }""");
        var request = ValidGraphRequest(document: document);

        var result = ScoringRuleValidator.Validate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("score.aggregator_unsupported", result.Error.Code);
    }

    private static ScoringRuleValidationRequest ValidGraphRequest(
        string ruleKey = "tenant-burnout.total",
        string ruleVersion = "1.0.0",
        string schemaVersion = "1.0.0",
        string engineMinVersion = "1.0.0",
        string? document = null,
        string produces = """{"scores":["total"]}""",
        string compatibility = "{}")
    {
        return new ScoringRuleValidationRequest(
            ruleKey,
            ruleVersion,
            schemaVersion,
            engineMinVersion,
            document ?? ValidGraphDocument,
            produces,
            compatibility);
    }

    private static string ReplaceRequired(string source, string oldValue, string newValue)
    {
        Assert.Contains(oldValue, source);

        return source.Replace(oldValue, newValue);
    }

    private const string ValidLegacyDocument = """
        {
          "operations": [
            { "op": "mean", "items": ["q01", "q02", "q03"], "output": "total" }
          ]
        }
        """;

    private const string ValidInterpretationProduces = """
        {
          "scores": ["total"],
          "interpretation": {
            "status": "tenant_attested",
            "source": "tenant_defined",
            "provenance": "Tenant-defined internal pilot bands; not validated or official.",
            "scores": {
              "total": [
                { "code": "lower", "label": "Lower tenant band", "min": 1, "max": 2.49 },
                { "code": "middle", "label": "Middle tenant band", "min": 2.5, "max": 3.49 },
                { "code": "higher", "label": "Higher tenant band", "min": 3.5, "max": 5 }
              ]
            }
          }
        }
        """;

    private const string ValidGraphDocument = """
        {
          "schema_version": "1.0.0",
          "engine_min_version": "1.0.0",
          "rule_id": "tenant-burnout.total",
          "rule_version": "1.0.0",
          "scale_defaults": {
            "agreement": { "min": 1, "max": 5 }
          },
          "inputs": [
            { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
          ],
          "nodes": [
            { "id": "core_answers", "op": "select_answers", "input": "core_items" },
            {
              "id": "scored_answers",
              "op": "reverse_code",
              "input": "core_answers",
              "scale": "agreement",
              "reverse_flag_source": "explicit_list",
              "explicit_reverse_items": ["q03"]
            },
            { "id": "total", "op": "mean", "input": "scored_answers" }
          ],
          "outputs": [
            { "code": "total", "node": "total" }
          ],
          "missing_data": {
            "defaults": { "strategy": "require_all" }
          }
        }
        """;

    private const string ChoiceScoringDocument = """
        {
          "schema_version": "1.0.0",
          "engine_min_version": "1.0.0",
          "rule_id": "tenant-choice.total",
          "rule_version": "1.0.0",
          "inputs": [
            { "id": "core_items", "kind": "answers", "items": ["q01", "q02"] }
          ],
          "nodes": [
            {
              "id": "core_scores",
              "op": "map_choice_scores",
              "input": "core_items",
              "option_scores": {
                "q01": { "o01": 0, "o02": 2, "o03": 4 }
              }
            },
            { "id": "total", "op": "mean", "input": "core_scores" }
          ],
          "outputs": [
            { "code": "total", "node": "total" }
          ],
          "missing_data": {
            "defaults": { "strategy": "require_all" }
          }
        }
        """;
}
