using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public static class OpenLinkSessionHandles
{
    public const string Prefix = "rsh";

    public static OpenLinkSessionHandleIssue Issue(Guid tenantId)
    {
        var secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var rawHandle = $"{Prefix}_{tenantId:N}_{secret}";

        return new OpenLinkSessionHandleIssue(rawHandle, Hash(rawHandle));
    }

    public static Result<OpenLinkSessionHandleTenant> ParseTenant(string rawHandle)
    {
        if (string.IsNullOrWhiteSpace(rawHandle))
        {
            return InvalidHandle();
        }

        var parts = rawHandle.Split('_', 3, StringSplitOptions.None);
        if (parts.Length != 3 ||
            parts[0] != Prefix ||
            !Guid.TryParseExact(parts[1], "N", out var tenantId) ||
            parts[2].Length < 43)
        {
            return InvalidHandle();
        }

        return Result.Success(new OpenLinkSessionHandleTenant(tenantId));
    }

    public static string Hash(string rawHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawHandle);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawHandle));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Result<OpenLinkSessionHandleTenant> InvalidHandle()
    {
        return Result.Failure<OpenLinkSessionHandleTenant>(
            Error.Validation(
                "public_session.invalid_handle",
                "Public response-session handle is invalid."));
    }
}

public sealed record OpenLinkSessionHandleIssue(string RawHandle, string HandleHash);

public sealed record OpenLinkSessionHandleTenant(Guid TenantId);
