using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public static class WithdrawalRequestTokens
{
    public const string Prefix = "wdr";

    public static WithdrawalRequestTokenIssue Issue(Guid tenantId)
    {
        var secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var rawToken = $"{Prefix}_{tenantId:N}_{secret}";

        return new WithdrawalRequestTokenIssue(rawToken, Hash(rawToken));
    }

    public static Result<WithdrawalRequestTokenTenant> ParseTenant(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return InvalidToken();
        }

        var parts = rawToken.Split('_', 3, StringSplitOptions.None);
        if (parts.Length != 3 ||
            parts[0] != Prefix ||
            !Guid.TryParseExact(parts[1], "N", out var tenantId) ||
            parts[2].Length < 43)
        {
            return InvalidToken();
        }

        return Result.Success(new WithdrawalRequestTokenTenant(tenantId));
    }

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Result<WithdrawalRequestTokenTenant> InvalidToken()
    {
        return Result.Failure<WithdrawalRequestTokenTenant>(
            Error.Validation(
                "withdrawal_token.invalid",
                "Withdrawal token is invalid."));
    }
}

public sealed record WithdrawalRequestTokenIssue(string RawToken, string TokenHash);

public sealed record WithdrawalRequestTokenTenant(Guid TenantId);
