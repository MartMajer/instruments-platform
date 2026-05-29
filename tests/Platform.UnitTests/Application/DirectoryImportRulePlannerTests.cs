using Platform.Application.Features.DirectoryImports;

namespace Platform.UnitTests.Application;

public sealed class DirectoryImportRulePlannerTests
{
    [Fact]
    public void Planner_builds_graph_safe_user_filter_and_select_fields()
    {
        var criteriaJson = """
            {
              "accountEnabled": true,
              "userTypes": ["Member"],
              "departments": ["Psychology"]
            }
            """;

        var plan = DirectoryImportRulePlanner.Plan(criteriaJson);

        Assert.Contains("id", plan.UserSelectFields);
        Assert.Contains("displayName", plan.UserSelectFields);
        Assert.Contains("mail", plan.UserSelectFields);
        Assert.Contains("userPrincipalName", plan.UserSelectFields);
        Assert.Contains("department", plan.UserSelectFields);
        Assert.Contains("jobTitle", plan.UserSelectFields);
        Assert.Contains("employeeType", plan.UserSelectFields);
        Assert.Contains("officeLocation", plan.UserSelectFields);
        Assert.Contains("preferredLanguage", plan.UserSelectFields);
        Assert.Contains("accountEnabled", plan.UserSelectFields);
        Assert.Contains("userType", plan.UserSelectFields);
        Assert.Equal("accountEnabled eq true and userType eq 'Member' and department eq 'Psychology'", plan.UserFilter);
        Assert.False(plan.RequiresAdvancedQuery);
        Assert.Empty(plan.LocalPostFilters);
        Assert.Equal(DirectoryImportManagerFetchModes.None, plan.ManagerFetchMode);
    }

    [Fact]
    public void Planner_separates_group_expansion_manager_fetch_and_local_contains_filter()
    {
        var criteriaJson = """
            {
              "groupIds": ["graph-group-id"],
              "jobTitleContains": "manager",
              "includeManagerChain": true
            }
            """;

        var plan = DirectoryImportRulePlanner.Plan(criteriaJson);

        var groupFetch = Assert.Single(plan.GroupMemberFetches);
        Assert.Equal("graph-group-id", groupFetch.GroupId);
        Assert.Equal(DirectoryImportManagerFetchModes.ManagerChain, plan.ManagerFetchMode);
        Assert.Contains(plan.LocalPostFilters, filter =>
            filter.Kind == DirectoryImportLocalPostFilterKinds.JobTitleContains &&
            filter.Value == "manager");
        Assert.Contains(plan.Warnings, warning => warning.Code == "job_title_contains_local_filter");
    }

    [Fact]
    public void Planner_excludes_guests_with_member_filter_when_no_user_type_is_present()
    {
        var criteriaJson = """
            {
              "excludeGuests": true
            }
            """;

        var plan = DirectoryImportRulePlanner.Plan(criteriaJson);

        Assert.Equal("userType eq 'Member'", plan.UserFilter);
    }

    [Fact]
    public void Planner_requires_explicit_confirmation_for_mirror_mode()
    {
        Assert.Throws<InvalidOperationException>(() => DirectoryImportRulePlanner.Plan(
            "{}",
            mirrorMode: true,
            mirrorConfirmedAt: null));

        var confirmed = DirectoryImportRulePlanner.Plan(
            "{}",
            mirrorMode: true,
            mirrorConfirmedAt: DateTimeOffset.Parse("2026-05-29T20:00:00+00:00"));

        Assert.True(confirmed.MirrorMode);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"department\"")]
    public void Planner_rejects_non_object_criteria_json(string invalidJson)
    {
        Assert.Throws<ArgumentException>(() => DirectoryImportRulePlanner.Plan(invalidJson));
    }
}
