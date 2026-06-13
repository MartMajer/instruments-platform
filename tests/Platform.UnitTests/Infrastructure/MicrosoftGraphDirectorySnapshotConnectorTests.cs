using System.Net;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.ProductSurfaces;

namespace Platform.UnitTests.Infrastructure;

public sealed class MicrosoftGraphDirectorySnapshotConnectorTests
{
    [Fact]
    public async Task FetchSnapshotAsync_returns_validation_failure_when_secret_config_is_missing()
    {
        var connector = new MicrosoftGraphDirectorySnapshotConnector(
            new HttpClient(new FakeGraphHandler()),
            Options.Create(new MicrosoftGraphAdminConsentOptions
            {
                ClientId = "client-id"
            }));

        var result = await connector.FetchSnapshotAsync("tenant-001", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("microsoft_graph.connector_config_missing", result.Error.Code);
    }

    [Fact]
    public async Task FetchSnapshotAsync_reads_users_groups_memberships_and_managers_without_persisting_tokens()
    {
        var handler = new FakeGraphHandler();
        var connector = new MicrosoftGraphDirectorySnapshotConnector(
            new HttpClient(handler),
            Options.Create(new MicrosoftGraphAdminConsentOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            }));

        var result = await connector.FetchSnapshotAsync("tenant-001", CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal("tenant-001", result.Value.MicrosoftTenantId);
        var user = Assert.Single(result.Value.Users);
        Assert.Equal("user-001", user.Id);
        Assert.Equal("ana@example.test", user.Mail);
        Assert.Equal("Research", user.Department);
        var group = Assert.Single(result.Value.Groups);
        Assert.Equal("group-001", group.Id);
        var membership = Assert.Single(result.Value.Memberships);
        Assert.Equal("user-001", membership.UserId);
        Assert.Equal("group-001", membership.GroupId);
        var manager = Assert.Single(result.Value.ManagerRelationships!);
        Assert.Equal("user-001", manager.UserId);
        Assert.Equal("manager-001", manager.ManagerUserId);
        Assert.Contains(handler.Requests, request =>
            request.Method == HttpMethod.Post &&
            request.RequestUri!.AbsoluteUri == "https://login.microsoftonline.com/tenant-001/oauth2/v2.0/token");
        Assert.All(
            handler.Requests.Where(request => request.RequestUri!.Host == "graph.microsoft.com"),
            request => Assert.Equal("Bearer access-token", request.Headers.Authorization?.ToString()));
    }

    private sealed class FakeGraphHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(CloneRequest(request));
            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri.Query;
            if (request.Method == HttpMethod.Post && path == "/tenant-001/oauth2/v2.0/token")
            {
                return Json("""{"access_token":"access-token"}""");
            }

            if (path == "/v1.0/users" && query.Contains("$select=", StringComparison.Ordinal))
            {
                return Json(
                    """
                    {
                      "value": [
                        {
                          "id": "user-001",
                          "mail": "ana@example.test",
                          "userPrincipalName": "ana@example.test",
                          "displayName": "Ana Analyst",
                          "preferredLanguage": "en",
                          "department": "Research",
                          "jobTitle": "Analyst",
                          "employeeType": "Employee",
                          "officeLocation": "Zagreb",
                          "userType": "Member",
                          "accountEnabled": true
                        }
                      ]
                    }
                    """);
            }

            if (path == "/v1.0/groups")
            {
                return Json("""{"value":[{"id":"group-001","displayName":"Field Team"}]}""");
            }

            if (path == "/v1.0/groups/group-001/members")
            {
                return Json("""{"value":[{"id":"user-001"}]}""");
            }

            if (path == "/v1.0/users/user-001/manager")
            {
                return Json("""{"id":"manager-001"}""");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        private static Task<HttpResponseMessage> Json(string json)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
