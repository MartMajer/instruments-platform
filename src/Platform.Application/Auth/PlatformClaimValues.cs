using System.Security.Claims;

namespace Platform.Application.Auth;

internal static class PlatformClaimValues
{
    private static readonly char[] Separators = [' ', ','];

    public static IEnumerable<string> Read(ClaimsPrincipal principal, string claimType)
    {
        return principal
            .FindAll(claimType)
            .SelectMany(claim => claim.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            .Select(value => value.Trim())
            .Where(value => value.Length > 0);
    }
}
