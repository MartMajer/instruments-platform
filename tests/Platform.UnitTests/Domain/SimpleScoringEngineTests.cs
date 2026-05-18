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
