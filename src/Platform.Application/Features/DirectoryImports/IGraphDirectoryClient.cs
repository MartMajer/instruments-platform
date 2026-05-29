namespace Platform.Application.Features.DirectoryImports;

public interface IGraphDirectoryClient
{
    Task<GraphDirectoryUserPage> ListUsersAsync(
        GraphDirectoryConnectionCredentials credentials,
        DirectoryImportPlan plan,
        CancellationToken cancellationToken);

    Task<GraphDirectoryGroupPage> ListGroupsAsync(
        GraphDirectoryConnectionCredentials credentials,
        CancellationToken cancellationToken);

    Task<GraphDirectoryUserPage> ListGroupMembersAsync(
        GraphDirectoryConnectionCredentials credentials,
        string groupId,
        IReadOnlyList<string> selectFields,
        CancellationToken cancellationToken);

    Task<GraphDirectoryManagerCandidate?> GetManagerAsync(
        GraphDirectoryConnectionCredentials credentials,
        string userGraphId,
        CancellationToken cancellationToken);
}
