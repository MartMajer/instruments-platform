using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Platform.Application.Features.ProductSurfaces;
using Platform.SharedKernel;

namespace Platform.Infrastructure.ProductSurfaces;

public sealed class MicrosoftGraphDirectorySnapshotConnector(
    HttpClient httpClient,
    IOptions<MicrosoftGraphAdminConsentOptions> options)
    : IMicrosoftGraphDirectorySnapshotConnector
{
    private static readonly string[] UserSelectFields =
    [
        "id",
        "mail",
        "userPrincipalName",
        "displayName",
        "preferredLanguage",
        "department",
        "jobTitle",
        "employeeType",
        "officeLocation",
        "userType",
        "accountEnabled"
    ];

    public async Task<Result<MicrosoftGraphDirectoryImportSnapshot>> FetchSnapshotAsync(
        string microsoftTenantId,
        CancellationToken cancellationToken)
    {
        var current = options.Value;
        if (string.IsNullOrWhiteSpace(microsoftTenantId) ||
            string.IsNullOrWhiteSpace(current.ClientId) ||
            string.IsNullOrWhiteSpace(current.ClientSecret) ||
            string.IsNullOrWhiteSpace(current.TokenAuthorityTemplate) ||
            string.IsNullOrWhiteSpace(current.GraphBaseUrl))
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(
                Error.Validation(
                    "microsoft_graph.connector_config_missing",
                    "Microsoft Graph connector configuration is incomplete."));
        }

        var token = await RequestAccessTokenAsync(current, microsoftTenantId.Trim(), cancellationToken);
        if (token.IsFailure)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(token.Error);
        }

        var users = await ReadUsersAsync(current, token.Value, cancellationToken);
        if (users.IsFailure)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(users.Error);
        }

        var groups = await ReadGroupsAsync(current, token.Value, cancellationToken);
        if (groups.IsFailure)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(groups.Error);
        }

        var memberships = await ReadMembershipsAsync(current, token.Value, groups.Value, cancellationToken);
        if (memberships.IsFailure)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(memberships.Error);
        }

        var managers = await ReadManagerRelationshipsAsync(current, token.Value, users.Value, cancellationToken);
        if (managers.IsFailure)
        {
            return Result.Failure<MicrosoftGraphDirectoryImportSnapshot>(managers.Error);
        }

        return Result.Success(new MicrosoftGraphDirectoryImportSnapshot(
            microsoftTenantId.Trim(),
            users.Value.Select(user => new MicrosoftGraphDirectoryImportUser(
                user.Id,
                user.Mail,
                user.UserPrincipalName,
                user.DisplayName,
                user.PreferredLanguage,
                user.Department,
                user.JobTitle,
                user.EmployeeType,
                user.OfficeLocation,
                user.UserType,
                user.AccountEnabled ?? true)).ToArray(),
            groups.Value.Select(group => new MicrosoftGraphDirectoryImportGroup(
                group.Id,
                group.DisplayName)).ToArray(),
            memberships.Value,
            ManagerRelationships: managers.Value));
    }

    private async Task<Result<string>> RequestAccessTokenAsync(
        MicrosoftGraphAdminConsentOptions current,
        string microsoftTenantId,
        CancellationToken cancellationToken)
    {
        var tokenEndpoint = current.TokenAuthorityTemplate.Replace(
            "{tenantId}",
            Uri.EscapeDataString(microsoftTenantId),
            StringComparison.Ordinal);
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = current.ClientId!.Trim(),
                ["client_secret"] = current.ClientSecret!,
                ["grant_type"] = "client_credentials",
                ["scope"] = "https://graph.microsoft.com/.default"
            })
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<string>(
                Error.Validation("microsoft_graph.token_failed", "Microsoft Graph token request failed."));
        }

        var payload = await response.Content.ReadFromJsonAsync<GraphTokenResponse>(
            cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.AccessToken))
        {
            return Result.Failure<string>(
                Error.Validation("microsoft_graph.token_invalid", "Microsoft Graph token response was invalid."));
        }

        return Result.Success(payload.AccessToken);
    }

    private Task<Result<IReadOnlyList<GraphUserResponse>>> ReadUsersAsync(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var path = $"users?$select={string.Join(',', UserSelectFields)}";
        return ReadCollectionAsync<GraphUserResponse>(current, accessToken, path, current.MaxUsers, cancellationToken);
    }

    private Task<Result<IReadOnlyList<GraphGroupResponse>>> ReadGroupsAsync(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        CancellationToken cancellationToken)
    {
        return ReadCollectionAsync<GraphGroupResponse>(
            current,
            accessToken,
            "groups?$select=id,displayName",
            current.MaxGroups,
            cancellationToken);
    }

    private async Task<Result<IReadOnlyList<MicrosoftGraphDirectoryImportMembership>>> ReadMembershipsAsync(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        IReadOnlyList<GraphGroupResponse> groups,
        CancellationToken cancellationToken)
    {
        var memberships = new List<MicrosoftGraphDirectoryImportMembership>();
        foreach (var group in groups.Where(group => !string.IsNullOrWhiteSpace(group.Id)))
        {
            var result = await ReadCollectionAsync<GraphDirectoryObjectResponse>(
                current,
                accessToken,
                $"groups/{Uri.EscapeDataString(group.Id)}/members?$select=id",
                Math.Max(0, current.MaxMemberships - memberships.Count),
                cancellationToken);
            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyList<MicrosoftGraphDirectoryImportMembership>>(result.Error);
            }

            memberships.AddRange(result.Value
                .Where(member => !string.IsNullOrWhiteSpace(member.Id))
                .Select(member => new MicrosoftGraphDirectoryImportMembership(member.Id, group.Id)));
            if (memberships.Count >= current.MaxMemberships)
            {
                break;
            }
        }

        return Result.Success<IReadOnlyList<MicrosoftGraphDirectoryImportMembership>>(memberships);
    }

    private async Task<Result<IReadOnlyList<MicrosoftGraphDirectoryImportManagerRelationship>>> ReadManagerRelationshipsAsync(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        IReadOnlyList<GraphUserResponse> users,
        CancellationToken cancellationToken)
    {
        var relationships = new List<MicrosoftGraphDirectoryImportManagerRelationship>();
        foreach (var user in users.Where(user => !string.IsNullOrWhiteSpace(user.Id)))
        {
            using var request = CreateGraphRequest(
                current,
                accessToken,
                $"users/{Uri.EscapeDataString(user.Id)}/manager?$select=id");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<IReadOnlyList<MicrosoftGraphDirectoryImportManagerRelationship>>(
                    Error.Validation("microsoft_graph.manager_failed", "Microsoft Graph manager request failed."));
            }

            var manager = await response.Content.ReadFromJsonAsync<GraphDirectoryObjectResponse>(
                cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(manager?.Id))
            {
                relationships.Add(new MicrosoftGraphDirectoryImportManagerRelationship(user.Id, manager.Id));
            }
        }

        return Result.Success<IReadOnlyList<MicrosoftGraphDirectoryImportManagerRelationship>>(relationships);
    }

    private async Task<Result<IReadOnlyList<T>>> ReadCollectionAsync<T>(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        string relativePath,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var items = new List<T>();
        var nextPath = relativePath;
        while (!string.IsNullOrWhiteSpace(nextPath) && items.Count < maxItems)
        {
            using var request = CreateGraphRequest(current, accessToken, nextPath);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<IReadOnlyList<T>>(
                    Error.Validation("microsoft_graph.read_failed", "Microsoft Graph read request failed."));
            }

            var page = await response.Content.ReadFromJsonAsync<GraphCollectionResponse<T>>(
                cancellationToken: cancellationToken);
            if (page?.Value is null)
            {
                return Result.Failure<IReadOnlyList<T>>(
                    Error.Validation("microsoft_graph.response_invalid", "Microsoft Graph response was invalid."));
            }

            items.AddRange(page.Value.Take(Math.Max(0, maxItems - items.Count)));
            nextPath = items.Count >= maxItems ? null : page.NextLink;
        }

        return Result.Success<IReadOnlyList<T>>(items);
    }

    private HttpRequestMessage CreateGraphRequest(
        MicrosoftGraphAdminConsentOptions current,
        string accessToken,
        string pathOrUrl)
    {
        var url = Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(new Uri(current.GraphBaseUrl.TrimEnd('/') + "/"), pathOrUrl);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private sealed record GraphTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record GraphCollectionResponse<T>(
        [property: JsonPropertyName("value")] IReadOnlyList<T>? Value,
        [property: JsonPropertyName("@odata.nextLink")] string? NextLink);

    private sealed record GraphDirectoryObjectResponse(
        [property: JsonPropertyName("id")] string Id);

    private sealed record GraphGroupResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("displayName")] string DisplayName);

    private sealed record GraphUserResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("mail")] string? Mail,
        [property: JsonPropertyName("userPrincipalName")] string? UserPrincipalName,
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("preferredLanguage")] string? PreferredLanguage,
        [property: JsonPropertyName("department")] string? Department,
        [property: JsonPropertyName("jobTitle")] string? JobTitle,
        [property: JsonPropertyName("employeeType")] string? EmployeeType,
        [property: JsonPropertyName("officeLocation")] string? OfficeLocation,
        [property: JsonPropertyName("userType")] string? UserType,
        [property: JsonPropertyName("accountEnabled")] bool? AccountEnabled);
}
