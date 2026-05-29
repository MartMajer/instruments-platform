namespace Platform.Application.Features.DirectoryImports;

public sealed record DirectoryImportPlan(
    IReadOnlyList<string> UserSelectFields,
    string? UserFilter,
    bool RequiresAdvancedQuery,
    IReadOnlyList<DirectoryImportGroupMemberFetch> GroupMemberFetches,
    string ManagerFetchMode,
    IReadOnlyList<DirectoryImportLocalPostFilter> LocalPostFilters,
    IReadOnlyList<DirectoryImportPlanWarning> Warnings,
    bool MirrorMode);

public sealed record DirectoryImportGroupMemberFetch(string GroupId);

public sealed record DirectoryImportLocalPostFilter(string Kind, string Value);

public sealed record DirectoryImportPlanWarning(string Code, string Message);

public static class DirectoryImportManagerFetchModes
{
    public const string None = "none";
    public const string DirectManager = "direct_manager";
    public const string ManagerChain = "manager_chain";
}

public static class DirectoryImportLocalPostFilterKinds
{
    public const string JobTitleContains = "job_title_contains";
}
