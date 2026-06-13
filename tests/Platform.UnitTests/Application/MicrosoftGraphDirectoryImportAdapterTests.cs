using Platform.Application.Features.ProductSurfaces;

namespace Platform.UnitTests.Application;

public sealed class MicrosoftGraphDirectoryImportAdapterTests
{
    [Fact]
    public void CreateCsvImportPlan_uses_msgraph_external_ids_and_existing_directory_csv_boundary()
    {
        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            "tenant-123",
            [
                new MicrosoftGraphDirectoryImportUser(
                    "user-001",
                    "ANA@EXAMPLE.TEST",
                    "ana@tenant.test",
                    "Ana Analyst",
                    "HR",
                    "Research",
                    "Analyst",
                    "Employee",
                    "Zagreb",
                    "Member"),
                new MicrosoftGraphDirectoryImportUser(
                    "user-002",
                    "ivo@example.test",
                    null,
                    "Ivo Intern",
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Member")
            ],
            [
                new MicrosoftGraphDirectoryImportGroup("group-001", "Field Team")
            ],
            [
                new MicrosoftGraphDirectoryImportMembership("user-001", "group-001")
            ]);

        var result = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.True(result.Value.Request.DryRun);
        Assert.Equal(2, result.Value.IncludedUserCount);
        Assert.Equal(2, result.Value.IncludedMembershipCount);
        Assert.Empty(result.Value.Warnings);
        Assert.Contains(
            "msgraph:tenant-123:user-001,ana@example.test,Ana Analyst,hr,department,Research,member",
            result.Value.Request.CsvContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "msgraph:tenant-123:user-001,ana@example.test,Ana Analyst,hr,msgraph_group,Field Team,member",
            result.Value.Request.CsvContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "msgraph:tenant-123:user-002,ivo@example.test,Ivo Intern,en,,,",
            result.Value.Request.CsvContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreateCsvImportPlan_requires_explicit_upn_email_fallback()
    {
        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            "tenant-123",
            [
                new MicrosoftGraphDirectoryImportUser(
                    "user-001",
                    null,
                    "ana@tenant.test",
                    "Ana Analyst",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member")
            ],
            [],
            []);

        var defaultResult = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot);
        var fallbackResult = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(
            snapshot with { AllowUserPrincipalNameEmailFallback = true },
            dryRun: false);

        Assert.True(defaultResult.IsSuccess, defaultResult.Error.ToString());
        Assert.Contains(defaultResult.Value.Warnings, warning => warning.Code == "email_missing");
        Assert.Contains("msgraph:tenant-123:user-001,,Ana Analyst,en,,,", defaultResult.Value.Request.CsvContent);

        Assert.True(fallbackResult.IsSuccess, fallbackResult.Error.ToString());
        Assert.False(fallbackResult.Value.Request.DryRun);
        Assert.DoesNotContain(fallbackResult.Value.Warnings, warning => warning.Code == "email_missing");
        Assert.Contains(
            "msgraph:tenant-123:user-001,ana@tenant.test,Ana Analyst,en,,,",
            fallbackResult.Value.Request.CsvContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreateCsvImportPlan_emits_manager_external_ids_after_manager_rows()
    {
        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            "tenant-123",
            [
                new MicrosoftGraphDirectoryImportUser(
                    "employee-001",
                    "employee@example.test",
                    null,
                    "Ana Analyst",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member"),
                new MicrosoftGraphDirectoryImportUser(
                    "manager-001",
                    "manager@example.test",
                    null,
                    "Mira Manager",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member")
            ],
            [],
            [],
            ManagerRelationships:
            [
                new MicrosoftGraphDirectoryImportManagerRelationship("employee-001", "manager-001")
            ]);

        var result = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot);

        Assert.True(result.IsSuccess, result.Error.ToString());
        var csv = result.Value.Request.CsvContent;
        var managerRow = "msgraph:tenant-123:manager-001,manager@example.test,Mira Manager,en,,,,";
        var employeeRow =
            "msgraph:tenant-123:employee-001,employee@example.test,Ana Analyst,en,,,,msgraph:tenant-123:manager-001";

        Assert.Contains(managerRow, csv, StringComparison.Ordinal);
        Assert.Contains(employeeRow, csv, StringComparison.Ordinal);
        Assert.True(
            csv.IndexOf(managerRow, StringComparison.Ordinal) <
            csv.IndexOf(employeeRow, StringComparison.Ordinal));
    }

    [Fact]
    public void CreateCsvImportPlan_carries_explicit_stale_marker_policy()
    {
        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            "tenant-123",
            [
                new MicrosoftGraphDirectoryImportUser(
                    "user-001",
                    "ana@example.test",
                    null,
                    "Ana Analyst",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member")
            ],
            [],
            [],
            MarkMissingUsersStale: true);

        var result = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot, dryRun: false);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.False(result.Value.Request.DryRun);
        Assert.True(result.Value.Request.MarkMissingSubjectsStale);
        Assert.Equal("msgraph:tenant-123:", result.Value.Request.SourceExternalIdPrefix);
    }

    [Fact]
    public void CreateCsvImportPlan_warns_for_skipped_users_and_unknown_memberships_without_sensitive_values()
    {
        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            "tenant-123",
            [
                new MicrosoftGraphDirectoryImportUser(
                    "guest-001",
                    "guest@example.test",
                    null,
                    "Guest User",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Guest"),
                new MicrosoftGraphDirectoryImportUser(
                    "disabled-001",
                    "disabled@example.test",
                    null,
                    "Disabled User",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member",
                    AccountEnabled: false),
                new MicrosoftGraphDirectoryImportUser(
                    "user-001",
                    "ana@example.test",
                    null,
                    "Ana Analyst",
                    "en",
                    null,
                    null,
                    null,
                    null,
                    "Member")
            ],
            [],
            [
                new MicrosoftGraphDirectoryImportMembership("user-001", "missing-group"),
                new MicrosoftGraphDirectoryImportMembership("missing-user", "missing-group")
            ]);

        var result = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(1, result.Value.IncludedUserCount);
        Assert.Contains(result.Value.Warnings, warning => warning.Code == "guest_user_skipped");
        Assert.Contains(result.Value.Warnings, warning => warning.Code == "disabled_user_skipped");
        Assert.Contains(result.Value.Warnings, warning => warning.Code == "membership_group_missing");
        Assert.Contains(result.Value.Warnings, warning => warning.Code == "membership_user_missing");
        Assert.DoesNotContain("guest@example.test", string.Join("\n", result.Value.Warnings.Select(warning => warning.Message)));
    }
}
