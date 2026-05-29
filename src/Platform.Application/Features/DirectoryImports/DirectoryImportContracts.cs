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

public sealed record GraphDirectoryConnectionCredentials(
    Guid ConnectionId,
    string TenantId,
    string ClientId,
    string ClientSecret);

public sealed record GraphDirectoryUserPage(
    IReadOnlyList<GraphDirectoryUserCandidate> Users,
    string? NextLink);

public sealed record GraphDirectoryGroupPage(
    IReadOnlyList<GraphDirectoryGroupCandidate> Groups,
    string? NextLink);

public sealed record GraphDirectoryUserCandidate(
    string GraphUserId,
    string? Email,
    string? UserPrincipalName,
    string? DisplayName,
    string? Department,
    string? JobTitle,
    string? EmployeeType,
    string? OfficeLocation,
    string? PreferredLanguage,
    bool? AccountEnabled,
    string? UserType,
    IReadOnlyList<GraphDirectoryCandidateWarning> Warnings);

public sealed record GraphDirectoryGroupCandidate(
    string GraphGroupId,
    string? DisplayName,
    bool? MailEnabled,
    bool? SecurityEnabled,
    IReadOnlyList<string> GroupTypes);

public sealed record GraphDirectoryManagerCandidate(
    string UserGraphId,
    string ManagerGraphId,
    string? ManagerDisplayName,
    string? ManagerEmail,
    string? ManagerUserPrincipalName,
    IReadOnlyList<GraphDirectoryCandidateWarning> Warnings);

public sealed record GraphDirectoryCandidateWarning(string Code, string Message);

public static class GraphDirectoryCandidateWarningCodes
{
    public const string MissingEmail = "missing_email";
}
