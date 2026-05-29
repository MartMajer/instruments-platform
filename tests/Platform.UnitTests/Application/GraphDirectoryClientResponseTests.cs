using Platform.Application.Features.DirectoryImports;

namespace Platform.UnitTests.Application;

public sealed class GraphDirectoryClientResponseTests
{
    private const string UserPageJson = """
        {
          "value": [
            {
              "id": "user-1",
              "displayName": "Ana Kovac",
              "mail": "ana.kovac@example.edu",
              "userPrincipalName": "ana.kovac@tenant.example",
              "department": "Psychology",
              "jobTitle": "Associate professor",
              "employeeType": "Faculty",
              "officeLocation": "Zagreb",
              "preferredLanguage": "hr-HR",
              "accountEnabled": true,
              "userType": "Member"
            },
            {
              "id": "user-2",
              "displayName": "No Mail Student",
              "mail": null,
              "userPrincipalName": "student@tenant.example",
              "department": "Computer Science",
              "jobTitle": "Student",
              "employeeType": "Student",
              "officeLocation": "Remote",
              "preferredLanguage": "en-US",
              "accountEnabled": true,
              "userType": "Member"
            },
            {
              "id": "user-3",
              "displayName": "Missing Email",
              "mail": null,
              "userPrincipalName": null,
              "department": null,
              "jobTitle": null,
              "employeeType": null,
              "officeLocation": null,
              "preferredLanguage": null,
              "accountEnabled": false,
              "userType": "Guest"
            }
          ]
        }
        """;

    [Fact]
    public void Mapper_prefers_mail_over_user_principal_name_and_maps_selected_fields()
    {
        var users = GraphDirectoryResponseMapper.MapUsers(UserPageJson);

        var user = Assert.Single(users, candidate => candidate.GraphUserId == "user-1");
        Assert.Equal("ana.kovac@example.edu", user.Email);
        Assert.Equal("ana.kovac@tenant.example", user.UserPrincipalName);
        Assert.Equal("Ana Kovac", user.DisplayName);
        Assert.Equal("Psychology", user.Department);
        Assert.Equal("Associate professor", user.JobTitle);
        Assert.Equal("Faculty", user.EmployeeType);
        Assert.Equal("Zagreb", user.OfficeLocation);
        Assert.Equal("hr-HR", user.PreferredLanguage);
        Assert.True(user.AccountEnabled);
        Assert.Equal("Member", user.UserType);
        Assert.Empty(user.Warnings);
    }

    [Fact]
    public void Mapper_falls_back_to_user_principal_name_when_mail_is_missing()
    {
        var users = GraphDirectoryResponseMapper.MapUsers(UserPageJson);

        var user = Assert.Single(users, candidate => candidate.GraphUserId == "user-2");
        Assert.Equal("student@tenant.example", user.Email);
        Assert.Empty(user.Warnings);
    }

    [Fact]
    public void Mapper_preserves_missing_email_as_warning_candidate()
    {
        var users = GraphDirectoryResponseMapper.MapUsers(UserPageJson);

        var user = Assert.Single(users, candidate => candidate.GraphUserId == "user-3");
        Assert.Null(user.Email);
        Assert.Contains(user.Warnings, warning => warning.Code == "missing_email");
    }

    [Fact]
    public void Mapper_maps_manager_response_to_relationship_candidate()
    {
        const string managerJson = """
            {
              "id": "manager-1",
              "displayName": "Manager One",
              "mail": "manager.one@example.edu",
              "userPrincipalName": "manager.one@tenant.example"
            }
            """;

        var relationship = GraphDirectoryResponseMapper.MapManager("user-1", managerJson);

        Assert.NotNull(relationship);
        Assert.Equal("user-1", relationship.UserGraphId);
        Assert.Equal("manager-1", relationship.ManagerGraphId);
        Assert.Equal("Manager One", relationship.ManagerDisplayName);
        Assert.Equal("manager.one@example.edu", relationship.ManagerEmail);
    }
}
