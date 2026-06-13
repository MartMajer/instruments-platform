using Platform.Domain.Scoring;

namespace Platform.UnitTests.Domain;

public sealed class ScoringRuleLaunchPreviewTests
{
    [Fact]
    public void Graph_rule_with_missing_template_item_code_is_blocked()
    {
        var result = ScoringRuleLaunchPreview.Evaluate(
            GraphDocumentWithItems("""["q01", "q99"]"""),
            [new ScoringRuleLaunchPreviewInput("q01", "1")]);

        Assert.True(result.IsFailure);
        Assert.Equal("scoring_rule.item_code_missing", result.Error.Code);
    }

    [Fact]
    public void Legacy_rule_with_missing_template_item_code_is_blocked()
    {
        var result = ScoringRuleLaunchPreview.Evaluate(
            """
            {
              "operations": [
                { "op": "mean", "items": ["q01", "q99"], "output": "total" }
              ]
            }
            """,
            [new ScoringRuleLaunchPreviewInput("q01", "1")]);

        Assert.True(result.IsFailure);
        Assert.Equal("scoring_rule.item_code_missing", result.Error.Code);
    }

    [Fact]
    public void Valid_graph_rule_executes_preview_successfully()
    {
        var result = ScoringRuleLaunchPreview.Evaluate(
            GraphDocumentWithItems("""["q01", "q02"]"""),
            [
                new ScoringRuleLaunchPreviewInput("q01", "1"),
                new ScoringRuleLaunchPreviewInput("q02", "5")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(2, result.Value.ReferencedItemCount);
        Assert.Equal(1, result.Value.OutputCount);
    }

    [Fact]
    public void Valid_choice_score_graph_rule_executes_preview_successfully()
    {
        var result = ScoringRuleLaunchPreview.Evaluate(
            """
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
            """,
            [
                new ScoringRuleLaunchPreviewInput("q01", "\"o02\""),
                new ScoringRuleLaunchPreviewInput("q02", "4")
            ]);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(2, result.Value.ReferencedItemCount);
        Assert.Equal(1, result.Value.OutputCount);
    }

    [Fact]
    public void Engine_failure_is_reported_as_preview_failure()
    {
        var result = ScoringRuleLaunchPreview.Evaluate(
            """
            {
              "operations": [
                { "op": "median", "items": ["q01"], "output": "total" }
              ]
            }
            """,
            [new ScoringRuleLaunchPreviewInput("q01", "1")]);

        Assert.True(result.IsFailure);
        Assert.Equal("scoring_rule.preview_failed", result.Error.Code);
    }

    private static string GraphDocumentWithItems(string itemsJson)
    {
        return $$"""
            {
              "schema_version": "1.0.0",
              "engine_min_version": "1.0.0",
              "rule_id": "tenant-burnout.total",
              "rule_version": "1.0.0",
              "inputs": [
                { "id": "core_items", "kind": "answers", "items": {{itemsJson}} }
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
            """;
    }
}
