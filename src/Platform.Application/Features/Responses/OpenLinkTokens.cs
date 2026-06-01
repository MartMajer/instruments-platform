using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public static class OpenLinkTokens
{
    public const string Prefix = "opn";
    public const string InvitationPrefix = "inv";
    public const string IdentifiedEntryPrefix = "idn";
    public const string IdentifiedQueuePrefix = "idq";

    public static OpenLinkTokenIssue Issue(Guid tenantId)
    {
        return IssueWithPrefix(tenantId, Prefix);
    }

    public static OpenLinkTokenIssue IssueInvitation(Guid tenantId)
    {
        return IssueWithPrefix(tenantId, InvitationPrefix);
    }

    public static OpenLinkTokenIssue IssueIdentifiedEntry(Guid tenantId)
    {
        return IssueWithPrefix(tenantId, IdentifiedEntryPrefix);
    }

    public static OpenLinkTokenIssue IssueIdentifiedQueue(Guid tenantId)
    {
        return IssueWithPrefix(tenantId, IdentifiedQueuePrefix);
    }

    private static OpenLinkTokenIssue IssueWithPrefix(Guid tenantId, string prefix)
    {
        var secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var rawToken = $"{prefix}_{tenantId:N}_{secret}";

        return new OpenLinkTokenIssue(rawToken, Hash(rawToken));
    }

    public static Result<OpenLinkTokenTenant> ParseTenant(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return InvalidToken();
        }

        var parts = rawToken.Split('_', 3, StringSplitOptions.None);
        if (parts.Length != 3 ||
            parts[0] is not (Prefix or InvitationPrefix or IdentifiedEntryPrefix or IdentifiedQueuePrefix) ||
            !Guid.TryParseExact(parts[1], "N", out var tenantId) ||
            parts[2].Length < 43)
        {
            return InvalidToken();
        }

        return Result.Success(new OpenLinkTokenTenant(tenantId));
    }

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Result<OpenLinkTokenTenant> InvalidToken()
    {
        return Result.Failure<OpenLinkTokenTenant>(
            Error.Validation(
                "open_link.invalid_token",
                "Open-link token is invalid."));
    }
}

public sealed record OpenLinkTokenIssue(string RawToken, string TokenHash);

public sealed record OpenLinkTokenTenant(Guid TenantId);
