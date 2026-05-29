using Platform.Domain.DirectoryImports;

namespace Platform.UnitTests.Domain;

public sealed class DirectoryImportEntitiesTests
{
    [Fact]
    public void Connection_requires_platform_tenant_and_external_microsoft_tenant()
    {
        Assert.Throws<ArgumentException>(() => new DirectoryConnection(
            Guid.NewGuid(),
            Guid.Empty,
            DirectoryConnectionProviders.MicrosoftGraph,
            "external-tenant",
            "Algebra",
            "algebra.example",
            """{"scopes":["User.Read.All"]}"""));

        Assert.Throws<ArgumentException>(() => new DirectoryConnection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DirectoryConnectionProviders.MicrosoftGraph,
            " ",
            "Algebra",
            "algebra.example",
            """{"scopes":["User.Read.All"]}"""));
    }

    [Fact]
    public void Connection_normalizes_safe_metadata()
    {
        var tenantId = Guid.NewGuid();

        var connection = new DirectoryConnection(
            Guid.NewGuid(),
            tenantId,
            " microsoft_graph ",
            " e78bc07c-063c-47b0-afd0-d08580b54187 ",
            " Algebra University ",
            " Algebra.hr ",
            """{ "scopes": ["User.Read.All", "GroupMember.Read.All"] }""");

        Assert.Equal(tenantId, connection.TenantId);
        Assert.Equal(DirectoryConnectionProviders.MicrosoftGraph, connection.Provider);
        Assert.Equal("e78bc07c-063c-47b0-afd0-d08580b54187", connection.ExternalTenantId);
        Assert.Equal("Algebra University", connection.DisplayName);
        Assert.Equal("algebra.hr", connection.PrimaryDomain);
        Assert.Equal("""{ "scopes": ["User.Read.All", "GroupMember.Read.All"] }""", connection.GrantedScopesJson);
        Assert.Equal(DirectoryConnectionStatuses.Active, connection.Status);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"User.Read.All\"")]
    [InlineData("{")]
    public void Connection_requires_granted_scopes_json_object(string invalidJson)
    {
        Assert.Throws<ArgumentException>(() => new DirectoryConnection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DirectoryConnectionProviders.MicrosoftGraph,
            "external-tenant",
            "Algebra",
            "algebra.example",
            invalidJson));
    }

    [Fact]
    public void Rule_stores_criteria_and_field_selection_as_json_objects()
    {
        var rule = new DirectoryImportRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Psychology students ",
            """{ "departments": ["Psychology"], "userTypes": ["Member"] }""",
            """{ "userFields": ["displayName", "mail", "department"] }""");

        Assert.Equal("Psychology students", rule.Name);
        Assert.Equal("""{ "departments": ["Psychology"], "userTypes": ["Member"] }""", rule.CriteriaJson);
        Assert.Equal("""{ "userFields": ["displayName", "mail", "department"] }""", rule.FieldSelectionJson);
        Assert.False(rule.MirrorMode);
        Assert.Null(rule.MirrorConfirmedAt);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"department\"")]
    public void Rule_rejects_non_object_json(string invalidJson)
    {
        Assert.Throws<ArgumentException>(() => new DirectoryImportRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Psychology students",
            invalidJson,
            "{}"));

        Assert.Throws<ArgumentException>(() => new DirectoryImportRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Psychology students",
            "{}",
            invalidJson));
    }

    [Fact]
    public void Rule_cannot_be_marked_mirror_mode_without_confirmation()
    {
        Assert.Throws<ArgumentException>(() => new DirectoryImportRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Full directory mirror",
            "{}",
            "{}",
            mirrorMode: true));

        var confirmedAt = DateTimeOffset.Parse("2026-05-29T18:00:00+00:00");
        var rule = new DirectoryImportRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Full directory mirror",
            "{}",
            "{}",
            mirrorMode: true,
            mirrorConfirmedAt: confirmedAt);

        Assert.True(rule.MirrorMode);
        Assert.Equal(confirmedAt, rule.MirrorConfirmedAt);
    }

    [Fact]
    public void Run_allows_happy_path_status_transitions()
    {
        var run = new DirectoryImportRun(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DirectoryImportRunModes.Preview,
            createdByUserId: Guid.NewGuid());

        var previewedAt = DateTimeOffset.Parse("2026-05-29T18:05:00+00:00");
        var applyingAt = previewedAt.AddMinutes(1);
        var appliedAt = previewedAt.AddMinutes(2);

        Assert.Equal(DirectoryImportRunStatuses.Planned, run.Status);

        run.MarkPreviewed("""{"createSubject":3,"noChange":1}""", previewedAt);
        run.StartApplying(applyingAt);
        run.MarkApplied("""{"applied":4}""", appliedAt);

        Assert.Equal(DirectoryImportRunStatuses.Applied, run.Status);
        Assert.Equal(appliedAt, run.FinishedAt);
        Assert.Equal("""{"applied":4}""", run.SummaryJson);
    }

    [Fact]
    public void Run_rejects_invalid_status_transitions()
    {
        var run = new DirectoryImportRun(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DirectoryImportRunModes.Preview);

        Assert.Throws<InvalidOperationException>(() => run.StartApplying(DateTimeOffset.UtcNow));

        run.MarkPreviewed("{}", DateTimeOffset.UtcNow);
        run.StartApplying(DateTimeOffset.UtcNow);
        run.MarkApplied("{}", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => run.MarkFailed(
            "late_failure",
            "{}",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Run_can_fail_from_planned_previewed_or_applying()
    {
        foreach (var statusSetup in new Action<DirectoryImportRun>[]
        {
            _ => { },
            run => run.MarkPreviewed("{}", DateTimeOffset.UtcNow),
            run =>
            {
                run.MarkPreviewed("{}", DateTimeOffset.UtcNow);
                run.StartApplying(DateTimeOffset.UtcNow);
            }
        })
        {
            var run = new DirectoryImportRun(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DirectoryImportRunModes.Preview);

            statusSetup(run);
            run.MarkFailed("graph_permission_denied", """{"status":"failed"}""", DateTimeOffset.UtcNow);

            Assert.Equal(DirectoryImportRunStatuses.Failed, run.Status);
            Assert.Equal("graph_permission_denied", run.ErrorCode);
        }
    }

    [Theory]
    [InlineData(DirectoryImportRunItemActions.CreateSubject)]
    [InlineData(DirectoryImportRunItemActions.UpdateSubject)]
    [InlineData(DirectoryImportRunItemActions.CreateGroup)]
    [InlineData(DirectoryImportRunItemActions.AddMembership)]
    [InlineData(DirectoryImportRunItemActions.SetManager)]
    [InlineData(DirectoryImportRunItemActions.DeactivateSubject)]
    [InlineData(DirectoryImportRunItemActions.NoChange)]
    [InlineData(DirectoryImportRunItemActions.Warning)]
    public void Run_item_accepts_known_actions(string action)
    {
        var item = new DirectoryImportRunItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user",
            "sha256:abc123",
            action,
            DirectoryImportRunItemStatuses.Planned,
            safeSummaryJson: """{"field":"displayName"}""");

        Assert.Equal(action, item.Action);
        Assert.Equal("""{"field":"displayName"}""", item.SafeSummaryJson);
    }

    [Fact]
    public void Run_item_rejects_unknown_action()
    {
        Assert.Throws<ArgumentException>(() => new DirectoryImportRunItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user",
            "sha256:abc123",
            "delete_everything",
            DirectoryImportRunItemStatuses.Planned,
            safeSummaryJson: "{}"));
    }
}
