using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class SimpleScoringEngineTests
{
    [Fact]
    public void Legacy_mean_operation_scores_numeric_answers()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "operations": [
                { "op": "mean", "items": ["q01", "q02", "q03"], "output": "total" }
              ]
            }
            """,
            [
                new SimpleScoreInput("q01", "3"),
                new SimpleScoreInput("q02", "4"),
                new SimpleScoreInput("q03", "5")
            ]);

        Assert.True(result.IsSuccess);
        var score = Assert.Single(result.Value);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4.0000m, score.Value);
        Assert.Equal(3, score.NValid);
    }

    [Fact]
    public void Legacy_sum_operation_scores_numeric_answers()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "operations": [
                { "op": "sum", "items": ["q01", "q02"], "output": "total" }
              ]
            }
            """,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "5")
            ]);

        Assert.True(result.IsSuccess);
        var score = Assert.Single(result.Value);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(7.0000m, score.Value);
        Assert.Equal(2, score.NValid);
    }

    [Fact]
    public void Graph_mean_scores_selected_answers()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.total",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                { "id": "total", "op": "mean", "input": "core_answers" }
              ],
              "outputs": [
                { "code": "total", "node": "total" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [
                new SimpleScoreInput("q01", "3"),
                new SimpleScoreInput("q02", "4"),
                new SimpleScoreInput("q03", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(4.0000m, score.Value);
        Assert.Equal(3, score.NValid);
    }

    [Fact]
    public void Graph_reverse_code_scores_explicit_items()
    {
        var result = SimpleScoringEngine.Evaluate(
            ReverseCodedGraphDocument,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "3"),
                new SimpleScoreInput("q03", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(2.0000m, score.Value);
        Assert.Equal(3, score.NValid);
    }

    [Fact]
    public void Graph_count_valid_outputs_number_of_scoreable_answers()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.valid",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                { "id": "valid_count", "op": "count_valid", "input": "core_answers" }
              ],
              "outputs": [
                { "code": "valid_count", "node": "valid_count" }
              ],
              "missing_data": {
                "defaults": { "strategy": "min_valid_count", "min_valid_count": 1 }
              }
            }
            """,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "", IsSkipped: true),
                new SimpleScoreInput("q03", "5", IsNa: true)
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("valid_count", score.DimensionCode);
        Assert.Equal(1.0000m, score.Value);
        Assert.Equal(1, score.NValid);
    }

    [Fact]
    public void Graph_weighted_mean_scores_selected_answers_with_item_weights()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-scoring.weighted",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                {
                  "id": "weighted_score",
                  "op": "weighted_mean",
                  "input": "core_answers",
                  "weights": { "q01": 2, "q02": 1, "q03": 1 }
                }
              ],
              "outputs": [
                { "code": "weighted_score", "node": "weighted_score" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "4"),
                new SimpleScoreInput("q03", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("weighted_score", score.DimensionCode);
        Assert.Equal(3.2500m, score.Value);
        Assert.Equal(3, score.NValid);
        Assert.Equal(3, score.NExpected);
    }

    [Fact]
    public void Graph_weighted_sum_honors_minimum_valid_missing_policy()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-scoring.weighted-sum",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                {
                  "id": "weighted_total",
                  "op": "weighted_sum",
                  "input": "core_answers",
                  "weights": { "q01": 2, "q02": 10 },
                  "missing_data": { "strategy": "min_valid_count", "min_valid_count": 1 }
                }
              ],
              "outputs": [
                { "code": "weighted_total", "node": "weighted_total" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [
                new SimpleScoreInput("q01", "3"),
                new SimpleScoreInput("q02", "", IsSkipped: true)
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("weighted_total", score.DimensionCode);
        Assert.Equal(6.0000m, score.Value);
        Assert.Equal(1, score.NValid);
        Assert.Equal(2, score.NExpected);
    }

    [Fact]
    public void Graph_normalizes_mixed_scale_vector_before_aggregation()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-scoring.normalized",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "risk_items", "kind": "answers", "items": ["agreement_protective", "discomfort"] }
              ],
              "nodes": [
                { "id": "risk_answers", "op": "select_answers", "input": "risk_items" },
                {
                  "id": "normalized_risk_answers",
                  "op": "normalize_0_100",
                  "input": "risk_answers",
                  "source_scales": {
                    "agreement_protective": { "min": 1, "max": 5, "reverse": true },
                    "discomfort": { "min": 0, "max": 10 }
                  }
                },
                { "id": "risk_score", "op": "mean", "input": "normalized_risk_answers" }
              ],
              "outputs": [
                { "code": "risk_score", "node": "risk_score" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [
                new SimpleScoreInput("agreement_protective", "5"),
                new SimpleScoreInput("discomfort", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("risk_score", score.DimensionCode);
        Assert.Equal(25.0000m, score.Value);
        Assert.Equal(2, score.NValid);
        Assert.Equal(2, score.NExpected);
    }

    [Fact]
    public void Graph_normalizes_scalar_scores_and_combines_scalar_nodes()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-scoring.composite",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "strain_items", "kind": "answers", "items": ["strain_1", "strain_2"] },
                { "id": "recovery_items", "kind": "answers", "items": ["recovery_1", "recovery_2"] }
              ],
              "nodes": [
                { "id": "strain_answers", "op": "select_answers", "input": "strain_items" },
                { "id": "strain_score", "op": "mean", "input": "strain_answers" },
                {
                  "id": "strain_0_100",
                  "op": "normalize_0_100",
                  "input": "strain_score",
                  "source_min": 1,
                  "source_max": 5
                },
                { "id": "recovery_answers", "op": "select_answers", "input": "recovery_items" },
                { "id": "recovery_score", "op": "mean", "input": "recovery_answers" },
                {
                  "id": "priority_index",
                  "op": "combine",
                  "inputs": ["strain_score", "recovery_score"],
                  "method": "weighted_mean",
                  "weights": { "strain_score": 2, "recovery_score": 1 }
                },
                {
                  "id": "strain_recovery_gap",
                  "op": "difference",
                  "left": "strain_score",
                  "right": "recovery_score"
                }
              ],
              "outputs": [
                { "code": "strain_0_100", "node": "strain_0_100" },
                { "code": "priority_index", "node": "priority_index" },
                { "code": "strain_recovery_gap", "node": "strain_recovery_gap" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [
                new SimpleScoreInput("strain_1", "4"),
                new SimpleScoreInput("strain_2", "5"),
                new SimpleScoreInput("recovery_1", "2"),
                new SimpleScoreInput("recovery_2", "3")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("strain_0_100", result.Value[0].DimensionCode);
        Assert.Equal(87.5000m, result.Value[0].Value);
        Assert.Equal("priority_index", result.Value[1].DimensionCode);
        Assert.Equal(3.8333m, result.Value[1].Value);
        Assert.Equal("strain_recovery_gap", result.Value[2].DimensionCode);
        Assert.Equal(2.0000m, result.Value[2].Value);
    }

    [Fact]
    public void Graph_require_all_missing_answer_returns_validation_error()
    {
        var result = SimpleScoringEngine.Evaluate(
            ReverseCodedGraphDocument,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "3")
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal("score.answer_missing", result.Error.Code);
    }

    [Fact]
    public void Graph_min_valid_count_allows_partial_missing_answers()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.partial",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01", "q02", "q03"] }
              ],
              "nodes": [
                { "id": "core_answers", "op": "select_answers", "input": "core_items" },
                { "id": "total", "op": "mean", "input": "core_answers" }
              ],
              "outputs": [
                { "code": "total", "node": "total" }
              ],
              "missing_data": {
                "defaults": { "strategy": "min_valid_count", "min_valid_count": 2 }
              }
            }
            """,
            [
                new SimpleScoreInput("q01", "2"),
                new SimpleScoreInput("q02", "4")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var score = Assert.Single(result.Value);
        Assert.Equal("total", score.DimensionCode);
        Assert.Equal(3.0000m, score.Value);
        Assert.Equal(2, score.NValid);
    }

    [Fact]
    public void Graph_aggregate_nodes_can_override_missing_policy()
    {
        var result = SimpleScoringEngine.Evaluate(
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
                  "op": "mean",
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
            [
                new SimpleScoreInput("q01", "3"),
                new SimpleScoreInput("q02", "4"),
                new SimpleScoreInput("q03", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("exhaustion", result.Value[0].DimensionCode);
        Assert.Equal(3.5000m, result.Value[0].Value);
        Assert.Equal(2, result.Value[0].NValid);
        Assert.Equal("recovery", result.Value[1].DimensionCode);
        Assert.Equal(5.0000m, result.Value[1].Value);
        Assert.Equal(1, result.Value[1].NValid);
        Assert.Equal(2, result.Value[1].NExpected);
    }

    [Fact]
    public void Graph_unknown_node_returns_validation_error()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.invalid",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": ["q01"] }
              ],
              "nodes": [
                { "id": "total", "op": "mean", "input": "missing_node" }
              ],
              "outputs": [
                { "code": "total", "node": "total" }
              ],
              "missing_data": {
                "defaults": { "strategy": "require_all" }
              }
            }
            """,
            [new SimpleScoreInput("q01", "2")]);

        Assert.True(result.IsFailure);
        Assert.Equal("score.node_unknown", result.Error.Code);
    }

    [Fact]
    public void Unsupported_operation_returns_validation_error()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "operations": [
                { "op": "median", "items": ["q01"], "output": "total" }
              ]
            }
            """,
            [new SimpleScoreInput("q01", "3")]);

        Assert.True(result.IsFailure);
        Assert.Equal("score.operation_unsupported", result.Error.Code);
    }

    [Fact]
    public void Missing_item_returns_validation_error()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "operations": [
                { "op": "mean", "items": ["q01", "q02"], "output": "total" }
              ]
            }
            """,
            [new SimpleScoreInput("q01", "3")]);

        Assert.True(result.IsFailure);
        Assert.Equal("score.answer_missing", result.Error.Code);
    }

    [Fact]
    public void Nonnumeric_answer_returns_validation_error()
    {
        var result = SimpleScoringEngine.Evaluate(
            """
            {
              "operations": [
                { "op": "mean", "items": ["q01"], "output": "total" }
              ]
            }
            """,
            [new SimpleScoreInput("q01", "\"not numeric\"")]);

        Assert.True(result.IsFailure);
        Assert.Equal("score.answer_not_numeric", result.Error.Code);
    }

    private const string ReverseCodedGraphDocument = """
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
}
