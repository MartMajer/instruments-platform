using System.Text;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record MicrosoftGraphDirectoryImportSnapshot(
    string MicrosoftTenantId,
    IReadOnlyList<MicrosoftGraphDirectoryImportUser> Users,
    IReadOnlyList<MicrosoftGraphDirectoryImportGroup> Groups,
    IReadOnlyList<MicrosoftGraphDirectoryImportMembership> Memberships,
    bool AllowUserPrincipalNameEmailFallback = false,
    bool ExcludeGuests = true,
    bool ExcludeDisabledAccounts = true,
    IReadOnlyList<MicrosoftGraphDirectoryImportManagerRelationship>? ManagerRelationships = null,
    bool MarkMissingUsersStale = false);

public sealed record MicrosoftGraphDirectoryImportUser(
    string Id,
    string? Mail,
    string? UserPrincipalName,
    string? DisplayName,
    string? PreferredLanguage,
    string? Department,
    string? JobTitle,
    string? EmployeeType,
    string? OfficeLocation,
    string? UserType,
    bool AccountEnabled = true);

public sealed record MicrosoftGraphDirectoryImportGroup(
    string Id,
    string DisplayName);

public sealed record MicrosoftGraphDirectoryImportMembership(
    string UserId,
    string GroupId);

public sealed record MicrosoftGraphDirectoryImportManagerRelationship(
    string UserId,
    string ManagerUserId);

public sealed record MicrosoftGraphDirectoryImportPlan(
    SubjectDirectoryCsvImportRequest Request,
    int IncludedUserCount,
    int IncludedMembershipCount,
    IReadOnlyList<MicrosoftGraphDirectoryImportWarning> Warnings);

public sealed record MicrosoftGraphDirectoryImportWarning(
    string Code,
    string Subject,
    string Message);

public static class MicrosoftGraphDirectoryImportAdapter
{
    private const string Header =
        "external_id,email,display_name,locale,group_type,group_name,role_in_group,manager_external_id";

    public static Result<MicrosoftGraphDirectoryImportPlan> CreateCsvImportPlan(
        MicrosoftGraphDirectoryImportSnapshot snapshot,
        bool dryRun = true)
    {
        var microsoftTenantId = NormalizeRequired(snapshot.MicrosoftTenantId);
        if (microsoftTenantId is null)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportPlan>(
                Error.Validation(
                    "microsoft_graph_import.tenant_required",
                    "Microsoft tenant id is required for Graph directory import."));
        }

        var warnings = new List<MicrosoftGraphDirectoryImportWarning>();
        var sourceExternalIdPrefix = $"msgraph:{microsoftTenantId}:";
        var groupsById = snapshot.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var membershipsByUserId = snapshot.Memberships
            .Where(membership => !string.IsNullOrWhiteSpace(membership.UserId))
            .GroupBy(membership => membership.UserId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var managerByUserId = (snapshot.ManagerRelationships ?? [])
            .Where(relationship =>
                !string.IsNullOrWhiteSpace(relationship.UserId) &&
                !string.IsNullOrWhiteSpace(relationship.ManagerUserId))
            .GroupBy(relationship => relationship.UserId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().ManagerUserId.Trim(), StringComparer.OrdinalIgnoreCase);
        var seenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var includedUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new StringBuilder();
        var includedMembershipCount = 0;
        rows.AppendLine(Header);

        foreach (var user in OrderUsersForManagerEdges(snapshot.Users, managerByUserId))
        {
            var userId = NormalizeRequired(user.Id);
            if (userId is null)
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "user_id_missing",
                    "user:unknown",
                    "Graph user row was skipped because it had no stable user id."));
                continue;
            }

            if (!seenUsers.Add(userId))
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "duplicate_user",
                    $"user:{userId}",
                    "Duplicate Graph user row was skipped."));
                continue;
            }

            if (snapshot.ExcludeDisabledAccounts && !user.AccountEnabled)
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "disabled_user_skipped",
                    $"user:{userId}",
                    "Disabled Graph user row was skipped."));
                continue;
            }

            if (snapshot.ExcludeGuests && string.Equals(user.UserType, "Guest", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "guest_user_skipped",
                    $"user:{userId}",
                    "Guest Graph user row was skipped."));
                continue;
            }

            var email = NormalizeEmailLike(user.Mail);
            if (email is null && snapshot.AllowUserPrincipalNameEmailFallback)
            {
                email = NormalizeEmailLike(user.UserPrincipalName);
            }

            if (email is null)
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "email_missing",
                    $"user:{userId}",
                    "Graph user row has no retained email value."));
            }

            var externalId = $"msgraph:{microsoftTenantId}:{userId}";
            var locale = NormalizeLocale(user.PreferredLanguage);
            var displayName = NormalizeOptional(user.DisplayName);
            var managerExternalId = ResolveManagerExternalId(
                microsoftTenantId,
                userId,
                managerByUserId,
                warnings);
            var emittedUserRow = false;

            if (NormalizeOptional(user.Department) is { } department)
            {
                AppendCsvRow(
                    rows,
                    externalId,
                    email,
                    displayName,
                    locale,
                    "department",
                    department,
                    "member",
                    managerExternalId);
                includedMembershipCount++;
                emittedUserRow = true;
            }

            if (membershipsByUserId.TryGetValue(userId, out var memberships))
            {
                foreach (var membership in memberships)
                {
                    var groupId = NormalizeRequired(membership.GroupId);
                    if (groupId is null || !groupsById.TryGetValue(groupId, out var group))
                    {
                        warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                            "membership_group_missing",
                            $"user:{userId}",
                            "Graph group membership was skipped because the group was not present in the snapshot."));
                        continue;
                    }

                    if (NormalizeRequired(group.DisplayName) is not { } groupName)
                    {
                        warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                            "group_name_missing",
                            $"group:{groupId}",
                            "Graph group membership was skipped because the group had no display name."));
                        continue;
                    }

                    AppendCsvRow(
                        rows,
                        externalId,
                        email,
                        displayName,
                        locale,
                        "msgraph_group",
                        groupName,
                        "member",
                        managerExternalId);
                    includedMembershipCount++;
                    emittedUserRow = true;
                }
            }

            if (!emittedUserRow)
            {
                AppendCsvRow(rows, externalId, email, displayName, locale, null, null, null, managerExternalId);
            }

            includedUserIds.Add(userId);
        }

        foreach (var membership in snapshot.Memberships)
        {
            var userId = NormalizeRequired(membership.UserId);
            if (userId is not null && !includedUserIds.Contains(userId) && !seenUsers.Contains(userId))
            {
                warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                    "membership_user_missing",
                    $"user:{userId}",
                    "Graph group membership was skipped because the user was not present in the snapshot."));
            }
        }

        return Result.Success(new MicrosoftGraphDirectoryImportPlan(
            new SubjectDirectoryCsvImportRequest(
                rows.ToString(),
                dryRun,
                sourceExternalIdPrefix,
                snapshot.MarkMissingUsersStale),
            includedUserIds.Count,
            includedMembershipCount,
            warnings));
    }

    private static void AppendCsvRow(
        StringBuilder rows,
        string externalId,
        string? email,
        string? displayName,
        string locale,
        string? groupType,
        string? groupName,
        string? roleInGroup,
        string? managerExternalId)
    {
        rows
            .Append(Csv(externalId))
            .Append(',')
            .Append(Csv(email))
            .Append(',')
            .Append(Csv(displayName))
            .Append(',')
            .Append(Csv(locale))
            .Append(',')
            .Append(Csv(groupType))
            .Append(',')
            .Append(Csv(groupName))
            .Append(',')
            .Append(Csv(roleInGroup))
            .Append(',')
            .Append(Csv(managerExternalId))
            .AppendLine();
    }

    private static IReadOnlyList<MicrosoftGraphDirectoryImportUser> OrderUsersForManagerEdges(
        IReadOnlyList<MicrosoftGraphDirectoryImportUser> users,
        IReadOnlyDictionary<string, string> managerByUserId)
    {
        var usersById = users
            .Where(user => !string.IsNullOrWhiteSpace(user.Id))
            .GroupBy(user => user.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var ordered = new List<MicrosoftGraphDirectoryImportUser>(users.Count);
        var orderedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visitingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
        {
            Visit(user);
        }

        return ordered;

        void Visit(MicrosoftGraphDirectoryImportUser user)
        {
            var userId = NormalizeRequired(user.Id);
            if (userId is null || orderedIds.Contains(userId))
            {
                return;
            }

            if (!visitingIds.Add(userId))
            {
                return;
            }

            if (managerByUserId.TryGetValue(userId, out var managerUserId) &&
                usersById.TryGetValue(managerUserId, out var manager))
            {
                Visit(manager);
            }

            visitingIds.Remove(userId);
            if (orderedIds.Add(userId))
            {
                ordered.Add(user);
            }
        }
    }

    private static string? ResolveManagerExternalId(
        string microsoftTenantId,
        string userId,
        IReadOnlyDictionary<string, string> managerByUserId,
        List<MicrosoftGraphDirectoryImportWarning> warnings)
    {
        if (!managerByUserId.TryGetValue(userId, out var managerUserId))
        {
            return null;
        }

        if (string.Equals(userId, managerUserId, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new MicrosoftGraphDirectoryImportWarning(
                "manager_self_reference",
                $"user:{userId}",
                "Graph manager relationship was skipped because the user manages itself."));
            return null;
        }

        return $"msgraph:{microsoftTenantId}:{managerUserId}";
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? value
            : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string? NormalizeRequired(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmailLike(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized is not null && normalized.Contains('@', StringComparison.Ordinal)
            ? normalized.ToLowerInvariant()
            : null;
    }

    private static string NormalizeLocale(string? value)
    {
        return NormalizeOptional(value)?.ToLowerInvariant() ?? "en";
    }
}
