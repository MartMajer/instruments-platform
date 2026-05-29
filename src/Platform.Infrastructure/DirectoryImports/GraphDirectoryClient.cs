using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Platform.Application.Features.DirectoryImports;

namespace Platform.Infrastructure.DirectoryImports;

public sealed class GraphDirectoryClient(HttpClient httpClient) : IGraphDirectoryClient
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private static readonly TimeSpan TokenRefreshSkew = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, CachedAccessToken> AccessTokenCache = new();

    public async Task<GraphDirectoryUserPage> ListUsersAsync(
        GraphDirectoryConnectionCredentials credentials,
        DirectoryImportPlan plan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(plan);

        var query = new Dictionary<string, string?>
        {
            ["$select"] = string.Join(",", plan.UserSelectFields),
            ["$top"] = "999"
        };

        if (!string.IsNullOrWhiteSpace(plan.UserFilter))
        {
            query["$filter"] = plan.UserFilter;
        }

        var request = CreateGraphGetRequest($"{GraphBaseUrl}/users{BuildQueryString(query)}");
        if (plan.RequiresAdvancedQuery)
        {
            request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");
        }

        using (request)
        {
            var json = await SendRequiredGraphRequestAsync(credentials, request, cancellationToken);
            return GraphDirectoryResponseMapper.MapUserPage(json);
        }
    }

    public async Task<GraphDirectoryGroupPage> ListGroupsAsync(
        GraphDirectoryConnectionCredentials credentials,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        using var request = CreateGraphGetRequest(
            $"{GraphBaseUrl}/groups{BuildQueryString(new Dictionary<string, string?>
            {
                ["$select"] = "id,displayName,mailEnabled,securityEnabled,groupTypes",
                ["$top"] = "999"
            })}");

        var json = await SendRequiredGraphRequestAsync(credentials, request, cancellationToken);
        return GraphDirectoryResponseMapper.MapGroupPage(json);
    }

    public async Task<GraphDirectoryUserPage> ListGroupMembersAsync(
        GraphDirectoryConnectionCredentials credentials,
        string groupId,
        IReadOnlyList<string> selectFields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentNullException.ThrowIfNull(selectFields);

        var pathGroupId = Uri.EscapeDataString(groupId);
        using var request = CreateGraphGetRequest(
            $"{GraphBaseUrl}/groups/{pathGroupId}/members/microsoft.graph.user{BuildQueryString(new Dictionary<string, string?>
            {
                ["$select"] = string.Join(",", selectFields),
                ["$top"] = "999"
            })}");

        var json = await SendRequiredGraphRequestAsync(credentials, request, cancellationToken);
        return GraphDirectoryResponseMapper.MapUserPage(json);
    }

    public async Task<GraphDirectoryManagerCandidate?> GetManagerAsync(
        GraphDirectoryConnectionCredentials credentials,
        string userGraphId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(userGraphId);

        var pathUserId = Uri.EscapeDataString(userGraphId);
        using var request = CreateGraphGetRequest(
            $"{GraphBaseUrl}/users/{pathUserId}/manager{BuildQueryString(new Dictionary<string, string?>
            {
                ["$select"] = "id,displayName,mail,userPrincipalName"
            })}");

        var json = await SendGraphRequestAsync(
            credentials,
            request,
            cancellationToken,
            allowNotFound: true);

        return json is null
            ? null
            : GraphDirectoryResponseMapper.MapManager(userGraphId, json);
    }

    private async Task<string> SendRequiredGraphRequestAsync(
        GraphDirectoryConnectionCredentials credentials,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await SendGraphRequestAsync(credentials, request, cancellationToken)
            ?? throw new InvalidOperationException("Microsoft Graph request returned no response body.");
    }

    private async Task<string?> SendGraphRequestAsync(
        GraphDirectoryConnectionCredentials credentials,
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        bool allowNotFound = false)
    {
        var accessToken = await GetAccessTokenAsync(credentials, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Microsoft Graph request failed with status {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(
        GraphDirectoryConnectionCredentials credentials,
        CancellationToken cancellationToken)
    {
        EnsureCredentials(credentials);

        var cacheKey = $"{credentials.ConnectionId:N}:{credentials.TenantId}";
        if (AccessTokenCache.TryGetValue(cacheKey, out var cached) &&
            cached.ExpiresAt > DateTimeOffset.UtcNow.Add(TokenRefreshSkew))
        {
            return cached.AccessToken;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(credentials.TenantId)}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = credentials.ClientId,
                ["client_secret"] = credentials.ClientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials"
            })
        };

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Microsoft identity token request failed with status {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var token = ParseAccessToken(json);
        AccessTokenCache[cacheKey] = token;
        return token.AccessToken;
    }

    private static CachedAccessToken ParseAccessToken(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var accessToken = root.TryGetProperty("access_token", out var accessTokenProperty) &&
            accessTokenProperty.ValueKind == JsonValueKind.String
                ? accessTokenProperty.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Microsoft identity token response did not include an access token.");
        }

        var expiresInSeconds = root.TryGetProperty("expires_in", out var expiresInProperty) &&
            expiresInProperty.ValueKind == JsonValueKind.Number &&
            expiresInProperty.TryGetInt32(out var parsedExpiresIn)
                ? parsedExpiresIn
                : 3600;

        return new CachedAccessToken(
            accessToken,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresInSeconds)));
    }

    private static HttpRequestMessage CreateGraphGetRequest(string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?> query)
    {
        var parts = query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToArray();

        return parts.Length == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static void EnsureCredentials(GraphDirectoryConnectionCredentials credentials)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.ClientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.ClientSecret);
    }

    private sealed record CachedAccessToken(string AccessToken, DateTimeOffset ExpiresAt);
}
